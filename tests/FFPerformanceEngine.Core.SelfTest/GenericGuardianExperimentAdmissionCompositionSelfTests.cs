using System.Diagnostics;
using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;
using FFPerformanceEngine.Core.Telemetry;

internal static class GenericGuardianExperimentAdmissionCompositionSelfTests
{
    internal static async Task RunAsync()
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("SKIP Track 6 admitted executor/lifecycle composition: Windows only");
            return;
        }

        await KeepHoldsExclusionThroughHostTransferAsync();
        Console.WriteLine("PASS Track 6 admitted canary holds Track0/DG exclusion through KEEP transfer and lifecycle restore");
    }

    private static async Task KeepHoldsExclusionThroughHostTransferAsync()
    {
        using var f = new Fixture();
        await using var host = new GenericGuardianWindowsSessionHostLifecycle(f.Owner);
        var key = await host.ObserveAsync(f.State)
            ?? throw new InvalidOperationException("Lifecycle did not issue the real Windows session key.");

        f.AttachExecutor(key);
        GenericGuardianSessionCanaryResult? result = null;
        IAsyncDisposable? competingBenchmarkLease = null;
        try
        {
            result = await f.Executor!.ExecuteAsync(f.Eligibility!, f.Binding!);
            Require(result.Attempted && result.Kept && result.ActiveLease is not null,
                "Clean admitted experiment must reach KEEP.");

            Require(f.CompetingBenchmark is { IsCompleted: false },
                "Track0 benchmark started during BEFORE must remain excluded until KEEP ownership transfers.");
            Require(f.CompetingDgOperation is { IsCompleted: false },
                "Unrelated DG transaction started during BEFORE must remain excluded until KEEP ownership transfers.");

            Require(await host.RetainAsync(key, result),
                "Owner lifecycle must accept the exact current KEEP lease.");

            competingBenchmarkLease = await f.CompetingBenchmark!
                .WaitAsync(TimeSpan.FromSeconds(5));
            _ = await f.CompetingDgOperation!
                .WaitAsync(TimeSpan.FromSeconds(5));

            Require(f.CanaryAdapter.State == Fixture.Mutated
                    && host.RetainedLeaseCount == 1,
                "KEEP transfer releases experiment exclusion but preserves the reversible session mutation under host ownership.");

            await host.ResetAsync();
            Require(f.CanaryAdapter.State == Fixture.Original
                    && f.CanaryAdapter.RollbackCount == 1
                    && host.RetainedLeaseCount == 0,
                "Lifecycle reset must restore the retained canary after experiment admission has been released.");
        }
        finally
        {
            if (competingBenchmarkLease is not null)
                await competingBenchmarkLease.DisposeAsync();
            else if (f.CompetingBenchmark is { IsCompletedSuccessfully: true })
                await (await f.CompetingBenchmark).DisposeAsync();

            if (result?.ActiveLease is { IsActive: true } orphan)
                await orphan.RestoreAsync();
        }
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

            BenchmarkAuthority = new ControlledBenchmarkLeaseManager();
            CompetingBenchmarkAuthority = new ControlledBenchmarkLeaseManager();
            CanaryAdapter = new AdapterDouble(CanaryCapability, Original);
            OtherAdapter = new AdapterDouble(OtherCapability, "idle");
            CanaryTransactions = Engine("canary", CanaryCapability, CanaryAdapter, CapabilityPersistenceScope.SessionOnly);
            OtherTransactions = Engine("other", OtherCapability, OtherAdapter, CapabilityPersistenceScope.PersistentAllowed);

            using var process = Process.GetCurrentProcess();
            var executable = process.MainModule?.FileName
                ?? throw new InvalidOperationException("Real Windows self-test executable is unavailable.");
            State = new GuardianWorkloadStateSnapshot
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
            Owner = new GenericGuardianWindowsSessionLifecycleCoordinator();
        }

        internal GenericGuardianWindowsSessionLifecycleCoordinator Owner { get; }
        internal GuardianWorkloadStateSnapshot State { get; }
        internal ControlledBenchmarkLeaseManager BenchmarkAuthority { get; }
        internal ControlledBenchmarkLeaseManager CompetingBenchmarkAuthority { get; }
        internal SystemOptimizationTransactionEngine CanaryTransactions { get; }
        internal SystemOptimizationTransactionEngine OtherTransactions { get; }
        internal AdapterDouble CanaryAdapter { get; }
        internal AdapterDouble OtherAdapter { get; }
        internal Task<IAsyncDisposable>? CompetingBenchmark { get; private set; }
        internal Task<PersistentOptimizationResult>? CompetingDgOperation { get; private set; }
        internal GenericGuardianWindowsSessionCanaryExecutor? Executor { get; private set; }
        internal GenericGuardianSessionActionEligibility? Eligibility { get; private set; }
        internal GenericGuardianWindowsSessionActionBinding? Binding { get; private set; }

        internal void AttachExecutor(GenericGuardianCanarySessionKey key)
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
                CanaryTransactions,
                capture,
                new ImprovedEvaluator(),
                catalog,
                sampleDuration: TimeSpan.FromMilliseconds(20),
                evidenceSource: new EvidenceSource(key.SessionEpoch),
                sessionKey: key,
                benchmarkAuthority: BenchmarkAuthority,
                sessionOwner: Owner);

            Eligibility = new GenericGuardianSessionActionEligibility
            {
                State = State,
                Family = candidate.Family,
                EligibleCandidates = Array.AsReadOnly([candidate])
            };
            Binding = new GenericGuardianWindowsSessionActionBinding
            {
                Candidate = candidate,
                Mutation = mutation
            };
        }

        private Task<TelemetryFrame?> CaptureAsync(
            int _,
            TimeSpan __,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _captureCalls++;
            if (_captureCalls == 1)
            {
                CompetingBenchmark = CompetingBenchmarkAuthority.AcquireAsync("competing-track0");
                CompetingDgOperation = OtherTransactions.ApplyPersistentAsync(
                    "competing-dg",
                    [new WindowsMutationRequest(OtherCapability, "external")]);
            }

            return Task.FromResult<TelemetryFrame?>(new TelemetryFrame(
                DateTimeOffset.UtcNow,
                [
                    new TelemetryMetricObservation(
                        TelemetryStandardMetrics.FrameFpsAverage,
                        _captureCalls == 1 ? 100d : 110d,
                        TelemetryMetricQuality.Measured,
                        1d,
                        "admission-composition-test",
                        TelemetryMetricOrigin.Direct)
                ]));
        }

        private SystemOptimizationTransactionEngine Engine(
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
            try { Directory.Delete(_root, true); } catch { }
        }

        private sealed class EvidenceSource(Guid epoch) : IGenericGuardianCanaryEvidenceSource
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

        private sealed class ImprovedEvaluator : IGenericGuardianSessionCanaryOutcomeEvaluator
        {
            public GenericGuardianSessionCanaryVerdict Evaluate(
                GenericGuardianSessionActionCandidate candidate,
                TelemetryFrame before,
                TelemetryFrame after)
                => GenericGuardianSessionCanaryVerdict.Improved;
        }
    }

    private sealed class AdapterDouble(string capabilityId, string initialState)
        : IWindowsCapabilityMutationAdapter
    {
        public string CapabilityId { get; } = capabilityId;
        public string State { get; private set; } = initialState;
        public int ApplyCount { get; private set; }
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
            ApplyCount++;
            State = targetValue;
            return Task.FromResult(WindowsCapabilityApplyResult.Ok());
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
