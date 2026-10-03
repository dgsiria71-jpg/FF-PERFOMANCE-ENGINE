using System.Diagnostics;
using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;
using FFPerformanceEngine.Core.Telemetry;

internal static class GenericGuardianWindowsSessionOwnerExecutorSelfTests
{
    private enum LossStage
    {
        None,
        BeforeExecution,
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
            Console.WriteLine("SKIP Track 6 executor OS-owned session continuity: Windows only");
            return;
        }

        foreach (var stage in Enum.GetValues<LossStage>())
        {
            using var f = new Fixture(stage);
            if (stage == LossStage.BeforeExecution) f.Owner.Reset();

            var result = await f.Executor.ExecuteAsync(f.Eligibility, f.Binding);

            if (stage == LossStage.None)
            {
                Require(result.Attempted && result.Kept && result.ActiveLease is not null
                        && f.Adapter.ApplyCount == 1 && f.EvaluatorCalls == 1,
                    "Unchanged OS-owned session may reach KEEP under the existing reversible canary rules.");
                await result.ActiveLease!.RestoreAsync();
                Require(f.Adapter.RollbackCount == 1 && f.Adapter.State == Fixture.Original,
                    "Clean KEEP remains exactly reversible.");
                continue;
            }

            if (stage is LossStage.BeforeExecution or LossStage.DuringBeforeCapture or LossStage.AfterBeforeEvidence)
            {
                Require(!result.Attempted && !result.Kept && result.ActiveLease is null
                        && f.Adapter.ApplyCount == 0 && f.Adapter.SnapshotCount == 0
                        && f.Adapter.State == Fixture.Original,
                    $"{stage}: loss of the owner-issued physical session before mutation must deny mutation.");
                continue;
            }

            Require(result.Attempted && !result.Kept && result.RolledBack
                    && result.Verdict == GenericGuardianSessionCanaryVerdict.Inconclusive
                    && result.ActiveLease is null
                    && f.Adapter.ApplyCount == 1 && f.Adapter.RollbackCount == 1
                    && f.Adapter.State == Fixture.Original,
                $"{stage}: loss of physical session after mutation must restore exact prior state and deny KEEP.");
        }

        Console.WriteLine("PASS Track 6 executor requires current OS-owned session before mutation and KEEP; invalidation restores");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class Fixture : IDisposable
    {
        internal const string Original = "balanced";
        private const string Capability = "test.owner.executor";
        private const string Game = "test.owner.executor.game";
        private const string TargetValue = "performance";
        private readonly string _root;
        private readonly LossStage _stage;
        private int _captureCalls;
        private int _evidenceCalls;

        internal Fixture(LossStage stage)
        {
            _stage = stage;
            _root = Path.Combine(Path.GetTempPath(), "dg-owner-executor-" + Guid.NewGuid().ToString("N"));
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

            Adapter = new AdapterDouble(() =>
            {
                if (_stage == LossStage.DuringMutation) Owner.Reset();
            });
            var registry = new WindowsPerformanceCapabilityRegistry([
                new WindowsPerformanceCapability
                {
                    CapabilityId = Capability,
                    Name = "Owner executor regression only",
                    Description = "Test only",
                    Domain = CapabilityDomain.System,
                    Availability = CapabilityAvailability.Available,
                    PersistenceScope = CapabilityPersistenceScope.SessionOnly,
                    Safety = ActionSafety.LiveSafe,
                    RiskLevel = CapabilityRiskLevel.Safe
                }
            ]);
            var transactions = new SystemOptimizationTransactionEngine(
                registry, new WindowsCapabilityMutationAdapterRegistry([Adapter]),
                new SnapshotService(Path.Combine(_root, "snapshots.json")),
                new HistoryService(Path.Combine(_root, "history.json")));
            var capture = new PerformanceCaptureCoordinator(
                (_, _, _) => Task.FromResult<TelemetrySample?>(null),
                typedCapture: CaptureAsync);
            var candidate = new GenericGuardianSessionActionCandidate
            {
                GameId = Game,
                Family = GuardianAnomalyKind.CpuContention,
                Action = new GuardianAction
                {
                    Id = "test.owner.executor.action",
                    Description = "Test only",
                    Safety = ActionSafety.LiveSafe
                }
            };
            var mutation = new WindowsMutationRequest(Capability, TargetValue, Original);
            var catalog = new GenericGuardianSessionMutationCatalog([
                new GenericGuardianSessionMutationDefinition(Game, candidate.Family, candidate.Action.Id, mutation)
            ]);
            var evidence = new EvidenceDouble(this, key.SessionEpoch);

            Executor = new GenericGuardianWindowsSessionCanaryExecutor(
                transactions, capture, new ImprovedEvaluator(this), catalog,
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
            Binding = new GenericGuardianWindowsSessionActionBinding { Candidate = candidate, Mutation = mutation };
        }

        internal GenericGuardianWindowsSessionLifecycleCoordinator Owner { get; }
        internal AdapterDouble Adapter { get; }
        internal GenericGuardianWindowsSessionCanaryExecutor Executor { get; }
        internal GenericGuardianSessionActionEligibility Eligibility { get; }
        internal GenericGuardianWindowsSessionActionBinding Binding { get; }
        internal int EvaluatorCalls { get; set; }

        private Task<TelemetryFrame?> CaptureAsync(int _, TimeSpan duration, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            _captureCalls++;
            if ((_stage == LossStage.DuringBeforeCapture && _captureCalls == 1)
                || (_stage == LossStage.DuringAfterCapture && _captureCalls == 2))
                Owner.Reset();

            return Task.FromResult<TelemetryFrame?>(new TelemetryFrame(DateTimeOffset.UtcNow, [
                new TelemetryMetricObservation(
                    TelemetryStandardMetrics.FrameFpsAverage,
                    _captureCalls == 1 ? 100d : 110d,
                    TelemetryMetricQuality.Measured, 1d,
                    "owner-executor-test", TelemetryMetricOrigin.Direct)
            ]));
        }

        private sealed class EvidenceDouble(Fixture owner, Guid epoch) : IGenericGuardianCanaryEvidenceSource
        {
            public Task<GenericGuardianCanaryComparisonWindow?> CaptureWindowAsync(
                TelemetryWorkloadTarget target, TelemetryFrame frame,
                DateTimeOffset captureStartedAt, DateTimeOffset captureCompletedAt,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                owner._evidenceCalls++;
                if ((owner._stage == LossStage.AfterBeforeEvidence && owner._evidenceCalls == 1)
                    || (owner._stage == LossStage.AfterAfterEvidence && owner._evidenceCalls == 2))
                    owner.Owner.Reset();

                return Task.FromResult<GenericGuardianCanaryComparisonWindow?>(
                    new GenericGuardianCanaryComparisonWindow(
                        epoch, target, "test-source", "test-mode", "test-scene",
                        "test-load", "test-environment",
                        captureStartedAt, captureCompletedAt, frame,
                        ControlledBenchmarkActive: false,
                        WorkloadDriftDetected: false,
                        OtherMutationDetected: false));
            }
        }

        private sealed class ImprovedEvaluator(Fixture owner) : IGenericGuardianSessionCanaryOutcomeEvaluator
        {
            public GenericGuardianSessionCanaryVerdict Evaluate(
                GenericGuardianSessionActionCandidate candidate, TelemetryFrame before, TelemetryFrame after)
            {
                owner.EvaluatorCalls++;
                if (owner._stage == LossStage.DuringEvaluation) owner.Owner.Reset();
                return GenericGuardianSessionCanaryVerdict.Improved;
            }
        }

        public void Dispose()
        {
            try { Directory.Delete(_root, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private sealed class AdapterDouble(Action onApply) : IWindowsCapabilityMutationAdapter
    {
        public string CapabilityId => "test.owner.executor";
        public string State { get; private set; } = Fixture.Original;
        public int SnapshotCount { get; private set; }
        public int ApplyCount { get; private set; }
        public int RollbackCount { get; private set; }

        public Task<WindowsCapabilityReadResult> ReadCurrentAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(WindowsCapabilityReadResult.Ok(State));

        public WindowsCapabilityValidationResult Validate(string targetValue, SystemOptimizationScope scope)
            => WindowsCapabilityValidationResult.Ok();

        public Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(CancellationToken cancellationToken = default)
        {
            SnapshotCount++;
            return Task.FromResult(new WindowsCapabilityMutationSnapshot(CapabilityId, State, State));
        }

        public Task<WindowsCapabilityApplyResult> ApplyAsync(string targetValue, CancellationToken cancellationToken = default)
        {
            ApplyCount++;
            State = targetValue;
            onApply();
            return Task.FromResult(WindowsCapabilityApplyResult.Ok());
        }

        public Task<bool> VerifyAsync(string targetValue, CancellationToken cancellationToken = default)
            => Task.FromResult(State == targetValue);

        public Task RollbackAsync(WindowsCapabilityMutationSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            RollbackCount++;
            State = snapshot.OriginalValue ?? string.Empty;
            return Task.CompletedTask;
        }
    }
}
