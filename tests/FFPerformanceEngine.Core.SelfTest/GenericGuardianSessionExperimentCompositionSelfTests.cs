using System.Diagnostics;
using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;
using FFPerformanceEngine.Core.Telemetry;

internal static class GenericGuardianSessionExperimentCompositionSelfTests
{
    internal static async Task RunAsync()
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("SKIP Track 6 admitted host/executor composition: Windows only");
            return;
        }

        await AdmissionSpansBeforeMutationAfterAndKeepTransferAsync();
        Console.WriteLine("PASS Track 6 lifecycle/executor composition holds admission through KEEP transfer and releases competitors afterward");
    }

    private static async Task AdmissionSpansBeforeMutationAfterAndKeepTransferAsync()
    {
        using var f = new Fixture();
        var state = f.ActiveState();
        var key = await f.Host.ObserveAsync(state)
            ?? throw new InvalidOperationException("Host did not establish the real Windows test session.");
        f.BuildExecutor(key, state);

        var hosted = await f.Host.ExecuteAndRetainAsync(
            f.Admission,
            f.Executor!,
            f.Eligibility!,
            f.Binding!);

        Require(hosted.Attempted && hosted.Kept
                && hosted.Verdict == GenericGuardianSessionCanaryVerdict.Improved
                && hosted.ActiveLease is null,
            "Hosted KEEP must transfer lease ownership into the lifecycle instead of leaking the raw lease to its caller.");
        Require(f.Host.RetainedLeaseCount == 1
                && f.CanaryAdapter.State == Fixture.Mutated,
            "Lifecycle must retain the successful canary mutation after admitted KEEP.");
        Require(f.CompetitorsWereBlockedDuringEvaluator,
            "Track0 and non-admitted DG competitors must still be blocked during outcome evaluation before KEEP transfer.");

        var benchmarkLease = await (f.CompetingBenchmark
            ?? throw new InvalidOperationException("Evaluator did not start benchmark competitor."))
            .WaitAsync(TimeSpan.FromSeconds(5));
        await benchmarkLease.DisposeAsync();
        _ = await (f.CompetingDg
            ?? throw new InvalidOperationException("Evaluator did not start DG competitor."))
            .WaitAsync(TimeSpan.FromSeconds(5));

        Require(f.OtherAdapter.State == "external",
            "Competitors must resume only after hosted execution transfers KEEP and releases experiment admission.");

        await f.Host.ResetAsync();
        Require(f.Host.RetainedLeaseCount == 0
                && f.CanaryAdapter.State == Fixture.Original
                && f.CanaryAdapter.RollbackCount == 1,
            "Session reset must restore the host-owned admitted KEEP exactly.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class Fixture : IDisposable
    {
        internal const string Original = "balanced";
        internal const string Mutated = "performance";
        private const string CanaryCapability = "test.composition.canary";
        private const string OtherCapability = "test.composition.other";
        private const string Game = "test.composition.game";
        private readonly string _root;
        private int _captureCalls;

        internal Fixture()
        {
            _root = Path.Combine(Path.GetTempPath(), "dg-admission-composition-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);

            Coordinator = new GenericGuardianWindowsSessionLifecycleCoordinator();
            Host = new GenericGuardianWindowsSessionHostLifecycle(Coordinator);
            BenchmarkAuthority = new ControlledBenchmarkLeaseManager();
            CompetingBenchmarkAuthority = new ControlledBenchmarkLeaseManager();

            CanaryAdapter = new AdapterDouble(CanaryCapability, Original);
            OtherAdapter = new AdapterDouble(OtherCapability, "idle");
            Transactions = CreateEngine(
                "canary", CanaryCapability, CanaryAdapter, CapabilityPersistenceScope.SessionOnly);
            OtherTransactions = CreateEngine(
                "other", OtherCapability, OtherAdapter, CapabilityPersistenceScope.PersistentAllowed);
            Admission = new GenericGuardianSessionExperimentAdmissionManager(BenchmarkAuthority, Transactions);
        }

        internal GenericGuardianWindowsSessionLifecycleCoordinator Coordinator { get; }
        internal GenericGuardianWindowsSessionHostLifecycle Host { get; }
        internal ControlledBenchmarkLeaseManager BenchmarkAuthority { get; }
        internal ControlledBenchmarkLeaseManager CompetingBenchmarkAuthority { get; }
        internal SystemOptimizationTransactionEngine Transactions { get; }
        internal SystemOptimizationTransactionEngine OtherTransactions { get; }
        internal GenericGuardianSessionExperimentAdmissionManager Admission { get; }
        internal AdapterDouble CanaryAdapter { get; }
        internal AdapterDouble OtherAdapter { get; }
        internal GenericGuardianWindowsSessionCanaryExecutor? Executor { get; private set; }
        internal GenericGuardianSessionActionEligibility? Eligibility { get; private set; }
        internal GenericGuardianWindowsSessionActionBinding? Binding { get; private set; }
        internal Task<IAsyncDisposable>? CompetingBenchmark { get; set; }
        internal Task<PersistentOptimizationResult>? CompetingDg { get; set; }
        internal bool CompetitorsWereBlockedDuringEvaluator { get; set; }

        internal GuardianWorkloadStateSnapshot ActiveState()
        {
            using var process = Process.GetCurrentProcess();
            var executable = process.MainModule?.FileName
                ?? throw new InvalidOperationException("Real Windows test executable unavailable.");
            return new GuardianWorkloadStateSnapshot
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
        }

        internal void BuildExecutor(
            GenericGuardianCanarySessionKey key,
            GuardianWorkloadStateSnapshot state)
        {
            var capture = new PerformanceCaptureCoordinator(
                (_, _, _) => Task.FromResult<TelemetrySample?>(null),
                typedCapture: CaptureAsync);
            var candidate = new GenericGuardianSessionActionCandidate
            {
                GameId = Game,
                Family = GuardianAnomalyKind.CpuContention,
                Action = new GuardianAction
                {
                    Id = "test.composition.action",
                    Description = "Test only",
                    Safety = ActionSafety.LiveSafe
                }
            };
            var mutation = new WindowsMutationRequest(CanaryCapability, Mutated, Original);
            var catalog = new GenericGuardianSessionMutationCatalog([
                new GenericGuardianSessionMutationDefinition(
                    Game, candidate.Family, candidate.Action.Id, mutation)
            ]);

            Executor = new GenericGuardianWindowsSessionCanaryExecutor(
                Transactions,
                capture,
                new ImprovedEvaluator(this),
                catalog,
                sampleDuration: TimeSpan.FromMilliseconds(20),
                evidenceSource: new EvidenceDouble(key.SessionEpoch),
                sessionKey: key,
                benchmarkAuthority: BenchmarkAuthority,
                sessionOwner: Coordinator);
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

        private Task<TelemetryFrame?> CaptureAsync(int _, TimeSpan __, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            _captureCalls++;
            return Task.FromResult<TelemetryFrame?>(new TelemetryFrame(
                DateTimeOffset.UtcNow,
                [new TelemetryMetricObservation(
                    TelemetryStandardMetrics.FrameFpsAverage,
                    _captureCalls == 1 ? 100d : 110d,
                    TelemetryMetricQuality.Measured,
                    1d,
                    "composition-test",
                    TelemetryMetricOrigin.Direct)]));
        }

        private SystemOptimizationTransactionEngine CreateEngine(
            string name,
            string capabilityId,
            AdapterDouble adapter,
            CapabilityPersistenceScope persistence)
        {
            var registry = new WindowsPerformanceCapabilityRegistry([
                new WindowsPerformanceCapability
                {
                    CapabilityId = capabilityId,
                    Name = name,
                    Description = "composition self-test",
                    Domain = CapabilityDomain.System,
                    Availability = CapabilityAvailability.Available,
                    PersistenceScope = persistence,
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

        public void Dispose()
        {
            Host.DisposeAsync().AsTask().GetAwaiter().GetResult();
            try { Directory.Delete(_root, true); } catch { }
        }

        private sealed class EvidenceDouble(Guid epoch) : IGenericGuardianCanaryEvidenceSource
        {
            public Task<GenericGuardianCanaryComparisonWindow?> CaptureWindowAsync(
                TelemetryWorkloadTarget target,
                TelemetryFrame frame,
                DateTimeOffset captureStartedAt,
                DateTimeOffset captureCompletedAt,
                CancellationToken cancellationToken = default)
                => Task.FromResult<GenericGuardianCanaryComparisonWindow?>(
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

        private sealed class ImprovedEvaluator(Fixture owner) : IGenericGuardianSessionCanaryOutcomeEvaluator
        {
            public GenericGuardianSessionCanaryVerdict Evaluate(
                GenericGuardianSessionActionCandidate candidate,
                TelemetryFrame before,
                TelemetryFrame after)
            {
                owner.CompetingBenchmark =
                    owner.CompetingBenchmarkAuthority.AcquireAsync("composition-competing-benchmark");
                owner.CompetingDg = owner.OtherTransactions.ApplyPersistentAsync(
                    "composition competing DG",
                    [new WindowsMutationRequest(OtherCapability, "external")]);

                owner.CompetitorsWereBlockedDuringEvaluator =
                    !owner.CompetingBenchmark.IsCompleted
                    && !owner.CompetingDg.IsCompleted;
                return GenericGuardianSessionCanaryVerdict.Improved;
            }
        }
    }

    private sealed class AdapterDouble(
        string capabilityId,
        string initialState) : IWindowsCapabilityMutationAdapter
    {
        public string CapabilityId { get; } = capabilityId;
        public string State { get; private set; } = initialState;
        public int RollbackCount { get; private set; }

        public Task<WindowsCapabilityReadResult> ReadCurrentAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(WindowsCapabilityReadResult.Ok(State));

        public WindowsCapabilityValidationResult Validate(string targetValue, SystemOptimizationScope scope)
            => WindowsCapabilityValidationResult.Ok();

        public Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new WindowsCapabilityMutationSnapshot(CapabilityId, State, State));

        public Task<WindowsCapabilityApplyResult> ApplyAsync(
            string targetValue,
            CancellationToken cancellationToken = default)
        {
            State = targetValue;
            return Task.FromResult(WindowsCapabilityApplyResult.Ok());
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
