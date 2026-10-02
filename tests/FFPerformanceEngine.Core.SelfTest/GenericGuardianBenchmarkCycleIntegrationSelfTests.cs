using System.Diagnostics;
using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;
using FFPerformanceEngine.Core.Telemetry;

internal static class GenericGuardianBenchmarkCycleIntegrationSelfTests
{
    private enum CompetitionStage
    {
        None,
        DuringBeforeCapture,
        AfterBeforeEvidence,
        DuringMutation,
        DuringAfterCapture,
        AfterAfterEvidence,
        DuringEvaluation
    }

    internal static async Task RunAsync()
    {
        foreach (var stage in Enum.GetValues<CompetitionStage>())
            await CompetingBenchmarkIsExcludedAsync(stage);

        await AlreadyActiveBenchmarkCancellationIsSafeAsync();
        Console.WriteLine("PASS Track 6 admitted canary excludes competing Track0 benchmarks through KEEP/rollback and cancellation");
    }

    private static async Task CompetingBenchmarkIsExcludedAsync(CompetitionStage stage)
    {
        using var f = new Fixture(stage);
        var result = await f.Executor.ExecuteAsync(f.Eligibility, f.Binding);

        Require(result.Attempted && result.Kept && result.ActiveLease is not null
                && f.Adapter.ApplyCount == 1 && f.EvaluatorCalls == 1,
            $"{stage}: admitted clean canary must reach KEEP while a competing Track0 request is excluded.");

        if (stage != CompetitionStage.None)
            Require(f.PendingBenchmark is { IsCompleted: false },
                $"{stage}: a Track0 request started inside the admitted experiment must remain blocked through KEEP return.");

        await result.ActiveLease!.RestoreAsync();
        if (f.PendingBenchmark is not null)
        {
            var competing = await f.PendingBenchmark.WaitAsync(TimeSpan.FromSeconds(5));
            await competing.DisposeAsync();
        }

        Require(f.Adapter.RollbackCount == 1 && f.Adapter.State == Fixture.Original,
            $"{stage}: restoring KEEP must restore exact state and release Track0 exclusion.");
    }

    private static async Task AlreadyActiveBenchmarkCancellationIsSafeAsync()
    {
        using var f = new Fixture(CompetitionStage.None);
        await using var held = await f.Authority.AcquireAsync("preexisting-track0");

        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        try
        {
            _ = await f.Executor.ExecuteAsync(f.Eligibility, f.Binding, cancellation.Token);
            throw new InvalidOperationException("Expected admitted executor to remain blocked behind the active Track0 lease until cancellation.");
        }
        catch (OperationCanceledException)
        {
        }

        Require(f.CaptureCalls == 0 && f.Adapter.ApplyCount == 0 && f.Adapter.SnapshotCount == 0,
            "Cancellation while waiting for Track0 experiment exclusion must occur before capture or Windows mutation.");
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
        private readonly CompetitionStage _stage;
        private int _evidenceCalls;

        internal Fixture(CompetitionStage stage)
        {
            _stage = stage;
            _root = Path.Combine(Path.GetTempPath(), "dg-cycle-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            Authority = new ControlledBenchmarkLeaseManager();
            Adapter = new AdapterDouble(() =>
            {
                if (_stage == CompetitionStage.DuringMutation) StartCompetition();
                return Task.CompletedTask;
            });

            var registry = new WindowsPerformanceCapabilityRegistry([
                new WindowsPerformanceCapability
                {
                    CapabilityId = Capability,
                    Name = "Cycle regression only",
                    Description = "Test only",
                    Domain = CapabilityDomain.System,
                    Availability = CapabilityAvailability.Available,
                    PersistenceScope = CapabilityPersistenceScope.SessionOnly,
                    Safety = ActionSafety.LiveSafe,
                    RiskLevel = CapabilityRiskLevel.Safe
                }
            ]);
            var transactions = new SystemOptimizationTransactionEngine(
                registry,
                new WindowsCapabilityMutationAdapterRegistry([Adapter]),
                new SnapshotService(Path.Combine(_root, "snapshots.json")),
                new HistoryService(Path.Combine(_root, "history.json")));

            using var process = Process.GetCurrentProcess();
            var executable = process.MainModule?.FileName
                ?? throw new InvalidOperationException("Real Windows benchmark-cycle self-test executable is unavailable.");
            var state = new GuardianWorkloadStateSnapshot
            {
                State = GuardianWorkloadState.Active,
                Confidence = GuardianWorkloadStateConfidence.High,
                Target = new TelemetryWorkloadTarget
                {
                    GameId = Game,
                    ProcessId = process.Id,
                    ExecutablePath = executable,
                    BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess
                }
            };

            SessionOwner = new GenericGuardianWindowsSessionLifecycleCoordinator();
            var key = SessionOwner.Observe(state)
                ?? throw new InvalidOperationException("Windows failed to establish the benchmark-cycle self-test process session.");

            var capture = new PerformanceCaptureCoordinator(
                (_, _, _) => Task.FromResult<TelemetrySample?>(null),
                typedCapture: CaptureAsync);
            var candidate = new GenericGuardianSessionActionCandidate
            {
                GameId = Game,
                Family = GuardianAnomalyKind.CpuContention,
                Action = new GuardianAction
                {
                    Id = "test.cycle.action",
                    Description = "Test only",
                    Safety = ActionSafety.LiveSafe
                }
            };
            var mutation = new WindowsMutationRequest(Capability, TargetValue, Original);
            var catalog = new GenericGuardianSessionMutationCatalog([
                new GenericGuardianSessionMutationDefinition(Game, candidate.Family, candidate.Action.Id, mutation)
            ]);

            Executor = new GenericGuardianWindowsSessionCanaryExecutor(
                transactions,
                capture,
                new ImprovedEvaluator(this),
                catalog,
                sampleDuration: TimeSpan.FromMilliseconds(20),
                evidenceSource: new EvidenceDouble(this, key.SessionEpoch),
                sessionKey: key,
                benchmarkAuthority: Authority,
                sessionOwner: SessionOwner);

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

        internal ControlledBenchmarkLeaseManager Authority { get; }
        internal GenericGuardianWindowsSessionLifecycleCoordinator SessionOwner { get; }
        internal AdapterDouble Adapter { get; }
        internal GenericGuardianWindowsSessionCanaryExecutor Executor { get; }
        internal GenericGuardianSessionActionEligibility Eligibility { get; }
        internal GenericGuardianWindowsSessionActionBinding Binding { get; }
        internal Task<IAsyncDisposable>? PendingBenchmark { get; private set; }
        internal int CaptureCalls { get; private set; }
        internal int EvaluatorCalls { get; set; }

        private Task<TelemetryFrame?> CaptureAsync(int _, TimeSpan __, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            CaptureCalls++;
            if ((_stage == CompetitionStage.DuringBeforeCapture && CaptureCalls == 1)
                || (_stage == CompetitionStage.DuringAfterCapture && CaptureCalls == 2))
                StartCompetition();

            return Task.FromResult<TelemetryFrame?>(new TelemetryFrame(
                DateTimeOffset.UtcNow,
                [
                    new TelemetryMetricObservation(
                        TelemetryStandardMetrics.FrameFpsAverage,
                        CaptureCalls == 1 ? 100d : 110d,
                        TelemetryMetricQuality.Measured,
                        1d,
                        "cycle-test-only",
                        TelemetryMetricOrigin.Direct)
                ]));
        }

        private void StartCompetition()
            => PendingBenchmark ??= Authority.AcquireAsync("cycle-selftest-competing-track0");

        public void Dispose()
        {
            try { Directory.Delete(_root, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private sealed class ImprovedEvaluator(Fixture owner) : IGenericGuardianSessionCanaryOutcomeEvaluator
        {
            public GenericGuardianSessionCanaryVerdict Evaluate(
                GenericGuardianSessionActionCandidate candidate,
                TelemetryFrame before,
                TelemetryFrame after)
            {
                owner.EvaluatorCalls++;
                if (owner._stage == CompetitionStage.DuringEvaluation)
                    owner.StartCompetition();
                return GenericGuardianSessionCanaryVerdict.Improved;
            }
        }

        private sealed class EvidenceDouble(Fixture owner, Guid epoch) : IGenericGuardianCanaryEvidenceSource
        {
            public Task<GenericGuardianCanaryComparisonWindow?> CaptureWindowAsync(
                TelemetryWorkloadTarget target,
                TelemetryFrame frame,
                DateTimeOffset captureStartedAt,
                DateTimeOffset captureCompletedAt,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                owner._evidenceCalls++;
                if ((owner._stage == CompetitionStage.AfterBeforeEvidence && owner._evidenceCalls == 1)
                    || (owner._stage == CompetitionStage.AfterAfterEvidence && owner._evidenceCalls == 2))
                    owner.StartCompetition();

                return Task.FromResult<GenericGuardianCanaryComparisonWindow?>(
                    new GenericGuardianCanaryComparisonWindow(
                        epoch,
                        target,
                        "test-source",
                        "test-mode",
                        "test-scene",
                        "test-load",
                        "test-environment",
                        captureStartedAt,
                        captureCompletedAt,
                        frame,
                        ControlledBenchmarkActive: false,
                        WorkloadDriftDetected: false,
                        OtherMutationDetected: false));
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

        public async Task<WindowsCapabilityApplyResult> ApplyAsync(
            string targetValue,
            CancellationToken cancellationToken = default)
        {
            ApplyCount++;
            State = targetValue;
            await onApply();
            return WindowsCapabilityApplyResult.Ok();
        }

        public Task<bool> VerifyAsync(string targetValue, CancellationToken cancellationToken = default)
            => Task.FromResult(State == targetValue);

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
