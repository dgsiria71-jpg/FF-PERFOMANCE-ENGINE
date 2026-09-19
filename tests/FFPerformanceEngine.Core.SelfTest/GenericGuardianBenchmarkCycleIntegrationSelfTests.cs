using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;
using FFPerformanceEngine.Core.Telemetry;

internal static class GenericGuardianBenchmarkCycleIntegrationSelfTests
{
    private enum Interference
    {
        None, AlreadyActive, DuringBeforeCapture, AfterBeforeEvidence,
        DuringMutation, DuringAfterCapture, AfterAfterEvidence
    }

    internal static async Task RunAsync()
    {
        foreach (var stage in Enum.GetValues<Interference>())
        {
            using var fixture = new Fixture(stage);
            IAsyncDisposable? held = stage == Interference.AlreadyActive
                ? await fixture.Authority.AcquireAsync("canary-cycle-active-selftest") : null;
            GenericGuardianSessionCanaryResult result;
            try
            {
                result = await fixture.Executor.ExecuteAsync(fixture.Eligibility, fixture.Binding);
            }
            finally
            {
                if (held is not null) await held.DisposeAsync();
            }

            if (stage == Interference.None)
            {
                Require(result.Attempted && result.Kept && result.ActiveLease is not null
                        && fixture.EvaluatorCalls == 1 && fixture.Adapter.ApplyCount == 1,
                    "Only the uninterrupted, test-owned scene and benchmark-free cycle may KEEP.");
                await result.ActiveLease!.RestoreAsync();
                Require(fixture.Adapter.RollbackCount == 1 && fixture.Adapter.State == Fixture.Original,
                    "A clean KEEP must retain exact reversible ownership.");
                continue;
            }

            if (stage is Interference.AlreadyActive or Interference.DuringBeforeCapture
                or Interference.AfterBeforeEvidence)
            {
                Require(!result.Attempted && !result.Kept && result.ActiveLease is null
                        && fixture.Adapter.ApplyCount == 0 && fixture.Adapter.SnapshotCount == 0
                        && fixture.EvaluatorCalls == 0 && fixture.Adapter.State == Fixture.Original,
                    $"{stage}: any interference before mutation must deny mutation, irrespective of fake source flags.");
                if (stage == Interference.AlreadyActive)
                    Require(fixture.CaptureCalls == 0, "An already-active Track 0 lease must block the first capture.");
            }
            else
            {
                Require(result.Attempted && !result.Kept && result.RolledBack
                        && result.Verdict == GenericGuardianSessionCanaryVerdict.Inconclusive
                        && result.ActiveLease is null && fixture.EvaluatorCalls == 0
                        && fixture.Adapter.ApplyCount == 1 && fixture.Adapter.RollbackCount == 1
                        && fixture.Adapter.State == Fixture.Original,
                    $"{stage}: interference after mutation must restore and bypass evaluator/KEEP.");
            }
        }
        Console.WriteLine("PASS Track 6 real authority generation guards complete canary cycle and restores interference before KEEP");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class Fixture : IDisposable
    {
        internal const string Original = "balanced";
        private const string Capability = "test.cycle.capability";
        private const string Game = "test.cycle.game";
        private const string TargetValue = "performance";
        private readonly string _root;
        private readonly Interference _stage;

        internal Fixture(Interference stage)
        {
            _stage = stage;
            _root = Path.Combine(Path.GetTempPath(), "dg-cycle-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            Authority = new ControlledBenchmarkLeaseManager();
            Adapter = new AdapterDouble(async () =>
            {
                if (_stage == Interference.DuringMutation) await InterfereAsync();
            });
            var registry = new WindowsPerformanceCapabilityRegistry([
                new WindowsPerformanceCapability
                {
                    CapabilityId = Capability,
                    Name = "Cycle regression only", Description = "Test only",
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
                GameId = Game, Family = GuardianAnomalyKind.CpuContention,
                Action = new GuardianAction { Id = "test.cycle.action", Description = "Test only", Safety = ActionSafety.LiveSafe }
            };
            var mutation = new WindowsMutationRequest(Capability, TargetValue, Original);
            var catalog = new GenericGuardianSessionMutationCatalog([
                new GenericGuardianSessionMutationDefinition(Game, candidate.Family, candidate.Action.Id, mutation)
            ]);
            var key = new GenericGuardianCanarySessionKey(Guid.NewGuid(), Game, 4242, @"C:\Games\cycle.exe");
            var evidence = new EvidenceDouble(this, key.SessionEpoch);
            Executor = new GenericGuardianWindowsSessionCanaryExecutor(
                transactions, capture, new ImprovedEvaluator(this), catalog,
                sampleDuration: TimeSpan.FromMilliseconds(50), evidenceSource: evidence,
                sessionKey: key, benchmarkAuthority: Authority);
            Eligibility = new GenericGuardianSessionActionEligibility
            {
                State = new GuardianWorkloadStateSnapshot
                {
                    State = GuardianWorkloadState.Active, Confidence = GuardianWorkloadStateConfidence.High,
                    Target = new TelemetryWorkloadTarget
                    {
                        GameId = Game, ProcessId = 4242, ExecutablePath = @"C:\Games\cycle.exe",
                        BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess
                    }
                },
                Family = candidate.Family, EligibleCandidates = Array.AsReadOnly([candidate])
            };
            Binding = new GenericGuardianWindowsSessionActionBinding { Candidate = candidate, Mutation = mutation };
        }

        internal ControlledBenchmarkLeaseManager Authority { get; }
        internal AdapterDouble Adapter { get; }
        internal GenericGuardianWindowsSessionCanaryExecutor Executor { get; }
        internal GenericGuardianSessionActionEligibility Eligibility { get; }
        internal GenericGuardianWindowsSessionActionBinding Binding { get; }
        internal int CaptureCalls { get; private set; }
        internal int EvaluatorCalls { get; set; }

        private async Task<TelemetryFrame?> CaptureAsync(int _, TimeSpan duration, CancellationToken token)
        {
            CaptureCalls++;
            if ((_stage == Interference.DuringBeforeCapture && CaptureCalls == 1)
                || (_stage == Interference.DuringAfterCapture && CaptureCalls == 2))
                await InterfereAsync();
            return new TelemetryFrame(DateTimeOffset.UtcNow, [
                new TelemetryMetricObservation(TelemetryStandardMetrics.FrameFpsAverage,
                    CaptureCalls == 1 ? 100d : 110d,
                    TelemetryMetricQuality.Measured, 1d, "cycle-test-only", TelemetryMetricOrigin.Direct)
            ]);
        }

        internal async Task InterfereAsync()
        {
            await using var lease = await Authority.AcquireAsync("cycle-selftest-interference");
        }

        public void Dispose()
        {
            try { Directory.Delete(_root, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private sealed class ImprovedEvaluator(Fixture owner) : IGenericGuardianSessionCanaryOutcomeEvaluator
        {
            public GenericGuardianSessionCanaryVerdict Evaluate(
                GenericGuardianSessionActionCandidate candidate, TelemetryFrame before, TelemetryFrame after)
            {
                owner.EvaluatorCalls++;
                return GenericGuardianSessionCanaryVerdict.Improved;
            }
        }

        private sealed class EvidenceDouble(Fixture owner, Guid epoch) : IGenericGuardianCanaryEvidenceSource
        {
            private int _calls;
            public async Task<GenericGuardianCanaryComparisonWindow?> CaptureWindowAsync(
                TelemetryWorkloadTarget target, TelemetryFrame frame,
                DateTimeOffset captureStartedAt, DateTimeOffset captureCompletedAt,
                CancellationToken cancellationToken = default)
            {
                _calls++;
                if ((owner._stage == Interference.AfterBeforeEvidence && _calls == 1)
                    || (owner._stage == Interference.AfterAfterEvidence && _calls == 2))
                    await owner.InterfereAsync();
                // Deliberately lies about interference: executor MUST use real Track 0 authority instead.
                return new GenericGuardianCanaryComparisonWindow(epoch, target, "test-source",
                    "test-mode", "test-scene", "test-load", "test-environment",
                    captureStartedAt, captureCompletedAt, frame,
                    ControlledBenchmarkActive: false, WorkloadDriftDetected: false, OtherMutationDetected: false);
            }
        }
    }

    private sealed class AdapterDouble(Func<Task> onApply) : IWindowsCapabilityMutationAdapter
    {
        public string CapabilityId => "test.cycle.capability";
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
        public async Task<WindowsCapabilityApplyResult> ApplyAsync(string targetValue, CancellationToken cancellationToken = default)
        {
            ApplyCount++;
            State = targetValue;
            await onApply();
            return WindowsCapabilityApplyResult.Ok();
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
