using System.Diagnostics;
using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;
using FFPerformanceEngine.Core.Telemetry;

internal static class GenericGuardianSystemOptimizationActivityIntegrationSelfTests
{
    private enum Interference
    {
        None,
        AlreadyActive,
        DuringBeforeCapture,
        AfterBeforeEvidence,
        DuringMutation,
        DuringAfterCapture,
        AfterAfterEvidence,
        DuringEvaluation
    }

    internal static async Task RunAsync()
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("SKIP Track 6 Guardian DG transaction activity integration: Windows only");
            return;
        }

        foreach (var stage in Enum.GetValues<Interference>())
        {
            using var f = new Fixture(stage);
            Task<PersistentOptimizationResult>? heldForeign = null;
            if (stage == Interference.AlreadyActive)
            {
                f.ForeignAdapter.BlockApply = true;
                heldForeign = f.RunForeignAsync();
                await f.ForeignAdapter.ApplyEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            }

            GenericGuardianSessionCanaryResult result;
            try
            {
                result = await f.Executor.ExecuteAsync(f.Eligibility, f.Binding);
            }
            finally
            {
                f.ForeignAdapter.ReleaseApply();
                if (heldForeign is not null) _ = await heldForeign;
            }

            if (stage == Interference.None)
            {
                Require(result.Attempted && result.Kept && result.ActiveLease is not null
                        && f.OwnAdapter.ApplyCount == 1 && f.EvaluatorCalls == 1,
                    "The canary's own single DG transaction must be recognized as expected, not self-contamination.");
                await result.ActiveLease!.RestoreAsync();
                Require(f.OwnAdapter.RollbackCount == 1 && f.OwnAdapter.State == Fixture.Original,
                    "A clean kept canary remains exactly reversible.");
                continue;
            }

            if (stage is Interference.AlreadyActive or Interference.DuringBeforeCapture or Interference.AfterBeforeEvidence)
            {
                Require(!result.Attempted && !result.Kept && result.ActiveLease is null
                        && f.OwnAdapter.ApplyCount == 0 && f.OwnAdapter.SnapshotCount == 0
                        && f.EvaluatorCalls == 0 && f.OwnAdapter.State == Fixture.Original,
                    $"{stage}: another DG transaction before mutation must deny the canary transaction even when supplied evidence says OtherMutationDetected=false.");
                continue;
            }

            Require(result.Attempted && !result.Kept && result.RolledBack
                    && result.Verdict == GenericGuardianSessionCanaryVerdict.Inconclusive
                    && result.ActiveLease is null
                    && f.OwnAdapter.ApplyCount == 1 && f.OwnAdapter.RollbackCount == 1
                    && f.OwnAdapter.State == Fixture.Original,
                $"{stage}: another DG transaction after mutation begins must exact-rollback and never KEEP.");
        }

        Console.WriteLine("PASS Track 6 Guardian distinguishes its own DG transaction from process-wide DG interference");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class Fixture : IDisposable
    {
        internal const string Original = "balanced";
        private const string OwnCapability = "test.guardian.dgactivity.own";
        private const string ForeignCapability = "test.guardian.dgactivity.foreign";
        private const string Game = "test.guardian.dgactivity.game";
        private readonly string _root;
        private readonly Interference _stage;
        private int _captureCalls;
        private int _evidenceCalls;
        private int _foreignRuns;

        internal Fixture(Interference stage)
        {
            _stage = stage;
            _root = Path.Combine(Path.GetTempPath(), "dg-guardian-dgactivity-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);

            using var process = Process.GetCurrentProcess();
            var executable = process.MainModule?.FileName
                ?? throw new InvalidOperationException("Real Windows self-test executable is unavailable.");
            var target = new TelemetryWorkloadTarget
            {
                GameId = Game,
                ProcessId = process.Id,
                ExecutablePath = executable,
                BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess
            };
            var state = new GuardianWorkloadStateSnapshot
            {
                State = GuardianWorkloadState.Active,
                Confidence = GuardianWorkloadStateConfidence.High,
                Target = target
            };

            Owner = new GenericGuardianWindowsSessionLifecycleCoordinator();
            var key = Owner.Observe(state)
                ?? throw new InvalidOperationException("Windows failed to establish the real self-test process session.");

            ForeignAdapter = new AdapterDouble(ForeignCapability);
            ForeignTransactions = Engine(
                "foreign", ForeignCapability, ForeignAdapter, CapabilityPersistenceScope.PersistentAllowed);

            OwnAdapter = new AdapterDouble(OwnCapability, async () =>
            {
                if (_stage == Interference.DuringMutation) await RunForeignAsync();
            });
            var ownTransactions = Engine(
                "own", OwnCapability, OwnAdapter, CapabilityPersistenceScope.SessionOnly);

            var capture = new PerformanceCaptureCoordinator(
                (_, _, _) => Task.FromResult<TelemetrySample?>(null),
                typedCapture: CaptureAsync);
            var candidate = new GenericGuardianSessionActionCandidate
            {
                GameId = Game,
                Family = GuardianAnomalyKind.CpuContention,
                Action = new GuardianAction
                {
                    Id = "test.guardian.dgactivity.action",
                    Description = "Test only",
                    Safety = ActionSafety.LiveSafe
                }
            };
            var mutation = new WindowsMutationRequest(OwnCapability, "performance", Original);
            var catalog = new GenericGuardianSessionMutationCatalog([
                new GenericGuardianSessionMutationDefinition(
                    Game, candidate.Family, candidate.Action.Id, mutation)
            ]);
            var evidence = new EvidenceDouble(this, key.SessionEpoch);

            Executor = new GenericGuardianWindowsSessionCanaryExecutor(
                ownTransactions, capture, new ImprovedEvaluator(this), catalog,
                sampleDuration: TimeSpan.FromMilliseconds(20),
                evidenceSource: evidence,
                sessionKey: key,
                sessionOwner: Owner);

            Eligibility = new GenericGuardianSessionActionEligibility
            {
                State = state,
                Family = candidate.Family,
                EligibleCandidates = Array.AsReadOnly([candidate])
            };
            Binding = new GenericGuardianWindowsSessionActionBinding
            {
                Candidate = candidate,
                Mutation = mutation
            };
        }

        internal GenericGuardianWindowsSessionLifecycleCoordinator Owner { get; }
        internal AdapterDouble OwnAdapter { get; }
        internal AdapterDouble ForeignAdapter { get; }
        internal SystemOptimizationTransactionEngine ForeignTransactions { get; }
        internal GenericGuardianWindowsSessionCanaryExecutor Executor { get; }
        internal GenericGuardianSessionActionEligibility Eligibility { get; }
        internal GenericGuardianWindowsSessionActionBinding Binding { get; }
        internal int EvaluatorCalls { get; set; }

        internal Task<PersistentOptimizationResult> RunForeignAsync()
        {
            var value = "foreign-" + Interlocked.Increment(ref _foreignRuns).ToString();
            return ForeignTransactions.ApplyPersistentAsync(
                "foreign DG activity regression",
                [new WindowsMutationRequest(ForeignCapability, value)]);
        }

        private SystemOptimizationTransactionEngine Engine(
            string name,
            string capabilityId,
            AdapterDouble adapter,
            CapabilityPersistenceScope scope)
        {
            var registry = new WindowsPerformanceCapabilityRegistry([
                new WindowsPerformanceCapability
                {
                    CapabilityId = capabilityId,
                    Name = name,
                    Description = "DG activity integration self-test",
                    Domain = CapabilityDomain.System,
                    Availability = CapabilityAvailability.Available,
                    PersistenceScope = scope,
                    Safety = ActionSafety.LiveSafe,
                    RiskLevel = CapabilityRiskLevel.Safe
                }
            ]);
            return new SystemOptimizationTransactionEngine(
                registry,
                new WindowsCapabilityMutationAdapterRegistry([adapter]),
                new SnapshotService(Path.Combine(_root, name + "-snapshots.json")),
                new HistoryService(Path.Combine(_root, name + "-history.json")));
        }

        private async Task<TelemetryFrame?> CaptureAsync(int _, TimeSpan duration, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            _captureCalls++;
            if ((_stage == Interference.DuringBeforeCapture && _captureCalls == 1)
                || (_stage == Interference.DuringAfterCapture && _captureCalls == 2))
                _ = await RunForeignAsync();

            return new TelemetryFrame(DateTimeOffset.UtcNow, [
                new TelemetryMetricObservation(
                    TelemetryStandardMetrics.FrameFpsAverage,
                    _captureCalls == 1 ? 100d : 110d,
                    TelemetryMetricQuality.Measured, 1d,
                    "dg-activity-integration-test", TelemetryMetricOrigin.Direct)
            ]);
        }

        private sealed class EvidenceDouble(Fixture owner, Guid epoch) : IGenericGuardianCanaryEvidenceSource
        {
            public async Task<GenericGuardianCanaryComparisonWindow?> CaptureWindowAsync(
                TelemetryWorkloadTarget target,
                TelemetryFrame frame,
                DateTimeOffset captureStartedAt,
                DateTimeOffset captureCompletedAt,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                owner._evidenceCalls++;
                if ((owner._stage == Interference.AfterBeforeEvidence && owner._evidenceCalls == 1)
                    || (owner._stage == Interference.AfterAfterEvidence && owner._evidenceCalls == 2))
                    _ = await owner.RunForeignAsync();

                return new GenericGuardianCanaryComparisonWindow(
                    epoch, target,
                    "test-source", "test-mode", "test-scene", "test-load", "test-environment",
                    captureStartedAt, captureCompletedAt, frame,
                    ControlledBenchmarkActive: false,
                    WorkloadDriftDetected: false,
                    OtherMutationDetected: false);
            }
        }

        private sealed class ImprovedEvaluator(Fixture owner) : IGenericGuardianSessionCanaryOutcomeEvaluator
        {
            public GenericGuardianSessionCanaryVerdict Evaluate(
                GenericGuardianSessionActionCandidate candidate,
                TelemetryFrame before,
                TelemetryFrame after)
            {
                owner.EvaluatorCalls++;
                if (owner._stage == Interference.DuringEvaluation)
                    _ = owner.RunForeignAsync().GetAwaiter().GetResult();
                return GenericGuardianSessionCanaryVerdict.Improved;
            }
        }

        public void Dispose()
        {
            ForeignAdapter.ReleaseApply();
            try { Directory.Delete(_root, true); } catch { }
        }
    }

    internal sealed class AdapterDouble : IWindowsCapabilityMutationAdapter
    {
        private readonly Func<Task>? _onApply;
        private readonly TaskCompletionSource<bool> _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal AdapterDouble(string capabilityId, Func<Task>? onApply = null)
        {
            CapabilityId = capabilityId;
            _onApply = onApply;
        }

        public string CapabilityId { get; }
        public string State { get; private set; } = Fixture.Original;
        public int SnapshotCount { get; private set; }
        public int ApplyCount { get; private set; }
        public int RollbackCount { get; private set; }
        public bool BlockApply { get; set; }
        internal TaskCompletionSource<bool> ApplyEntered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal void ReleaseApply() => _release.TrySetResult(true);

        public Task<WindowsCapabilityReadResult> ReadCurrentAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(WindowsCapabilityReadResult.Ok(State));

        public WindowsCapabilityValidationResult Validate(string targetValue, SystemOptimizationScope scope)
            => WindowsCapabilityValidationResult.Ok();

        public Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(CancellationToken cancellationToken = default)
        {
            SnapshotCount++;
            return Task.FromResult(new WindowsCapabilityMutationSnapshot(CapabilityId, State, State));
        }

        public async Task<WindowsCapabilityApplyResult> ApplyAsync(
            string targetValue,
            CancellationToken cancellationToken = default)
        {
            ApplyCount++;
            ApplyEntered.TrySetResult(true);
            if (BlockApply) await _release.Task.WaitAsync(cancellationToken);
            if (_onApply is not null) await _onApply();
            State = targetValue;
            return WindowsCapabilityApplyResult.Ok();
        }

        public Task<bool> VerifyAsync(string targetValue, CancellationToken cancellationToken = default)
            => Task.FromResult(string.Equals(State, targetValue, StringComparison.Ordinal));

        public Task RollbackAsync(
            WindowsCapabilityMutationSnapshot snapshot,
            CancellationToken cancellationToken = default)
        {
            RollbackCount++;
            State = snapshot.OriginalValue ?? string.Empty;
            return Task.CompletedTask;
        }
    }
}
