using System.Diagnostics;
using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;
using FFPerformanceEngine.Core.Telemetry;

internal static class GenericGuardianSystemOptimizationActivityIntegrationSelfTests
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
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("SKIP Track 6 DG System Optimization admitted executor integration: Windows only");
            return;
        }

        foreach (var stage in Enum.GetValues<CompetitionStage>())
            await CompetingDgOperationIsExcludedAsync(stage);

        await AlreadyActiveDgOperationCancellationIsSafeAsync();
        Console.WriteLine("PASS Track 6 admitted canary excludes unrelated DG operations through KEEP/rollback and cancellation");
    }

    private static async Task CompetingDgOperationIsExcludedAsync(CompetitionStage stage)
    {
        using var f = new Fixture(stage, blockOtherApply: false);
        var result = await f.Executor.ExecuteAsync(f.Eligibility, f.Binding);

        Require(result.Attempted && result.Kept && result.ActiveLease is not null
                && f.CanaryAdapter.ApplyCount == 1 && f.EvaluatorCalls == 1,
            $"{stage}: admitted canary must reach KEEP while unrelated DG operations are excluded.");

        if (stage != CompetitionStage.None)
            Require(f.PendingInterference is { IsCompleted: false },
                $"{stage}: unrelated DG operation started inside the experiment must remain blocked through KEEP return.");

        await result.ActiveLease!.RestoreAsync();
        if (f.PendingInterference is not null)
            _ = await f.PendingInterference.WaitAsync(TimeSpan.FromSeconds(5));

        Require(f.CanaryAdapter.RollbackCount == 1 && f.CanaryAdapter.State == Fixture.Original,
            $"{stage}: restoring KEEP must restore exact canary state and release DG exclusion.");
    }

    private static async Task AlreadyActiveDgOperationCancellationIsSafeAsync()
    {
        using var f = new Fixture(CompetitionStage.None, blockOtherApply: true);
        var blocking = f.StartBlockingInterference();
        await f.InterferenceAdapter.ApplyEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
        try
        {
            _ = await f.Executor.ExecuteAsync(f.Eligibility, f.Binding, cancellation.Token);
            throw new InvalidOperationException("Expected admitted executor to wait behind the active DG operation until cancellation.");
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            f.InterferenceAdapter.ReleaseApply();
            _ = await blocking.WaitAsync(TimeSpan.FromSeconds(5));
        }

        Require(f.CaptureCalls == 0 && f.CanaryAdapter.ApplyCount == 0 && f.CanaryAdapter.SnapshotCount == 0,
            "Cancellation while waiting for DG admission must occur before capture or canary mutation.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class Fixture : IDisposable
    {
        internal const string Original = "balanced";
        private const string CanaryCapability = "test.system-activity.canary";
        private const string InterferenceCapability = "test.system-activity.other";
        private const string Game = "test.system-activity.game";
        private readonly string _root;
        private readonly CompetitionStage _stage;
        private int _captureCalls;
        private int _evidenceCalls;
        private int _interferenceCounter;

        internal Fixture(CompetitionStage stage, bool blockOtherApply)
        {
            _stage = stage;
            _root = Path.Combine(Path.GetTempPath(), "dg-system-activity-executor-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);

            InterferenceAdapter = new AdapterDouble(
                InterferenceCapability,
                "old",
                blockApply: blockOtherApply);
            InterferenceEngine = CreateEngine(
                "other",
                InterferenceCapability,
                InterferenceAdapter,
                CapabilityPersistenceScope.PersistentAllowed);

            CanaryAdapter = new AdapterDouble(
                CanaryCapability,
                Original,
                onApply: () =>
                {
                    if (_stage == CompetitionStage.DuringMutation) StartCompetition();
                    return Task.CompletedTask;
                });
            Transactions = CreateEngine(
                "canary",
                CanaryCapability,
                CanaryAdapter,
                CapabilityPersistenceScope.SessionOnly);

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
                ?? throw new InvalidOperationException("Windows failed to establish real self-test process ownership.");

            var capture = new PerformanceCaptureCoordinator(
                (_, _, _) => Task.FromResult<TelemetrySample?>(null),
                typedCapture: CaptureAsync);
            var candidate = new GenericGuardianSessionActionCandidate
            {
                GameId = Game,
                Family = GuardianAnomalyKind.CpuContention,
                Action = new GuardianAction
                {
                    Id = "test.system-activity.action",
                    Description = "Test only",
                    Safety = ActionSafety.LiveSafe
                }
            };
            var mutation = new WindowsMutationRequest(CanaryCapability, "performance", Original);
            var catalog = new GenericGuardianSessionMutationCatalog([
                new GenericGuardianSessionMutationDefinition(Game, candidate.Family, candidate.Action.Id, mutation)
            ]);

            Executor = new GenericGuardianWindowsSessionCanaryExecutor(
                Transactions,
                capture,
                new ImprovedEvaluator(this),
                catalog,
                sampleDuration: TimeSpan.FromMilliseconds(20),
                evidenceSource: new EvidenceDouble(this, key.SessionEpoch),
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

        internal SystemOptimizationTransactionEngine Transactions { get; }
        internal SystemOptimizationTransactionEngine InterferenceEngine { get; }
        internal AdapterDouble CanaryAdapter { get; }
        internal AdapterDouble InterferenceAdapter { get; }
        internal GenericGuardianWindowsSessionLifecycleCoordinator Owner { get; }
        internal GenericGuardianWindowsSessionCanaryExecutor Executor { get; }
        internal GenericGuardianSessionActionEligibility Eligibility { get; }
        internal GenericGuardianWindowsSessionActionBinding Binding { get; }
        internal Task<PersistentOptimizationResult>? PendingInterference { get; private set; }
        internal int CaptureCalls => _captureCalls;
        internal int EvaluatorCalls { get; set; }

        internal Task<PersistentOptimizationResult> StartBlockingInterference()
            => InterferenceEngine.ApplyPersistentAsync(
                "blocking external DG transaction",
                [new WindowsMutationRequest(
                    InterferenceCapability,
                    "held-" + Interlocked.Increment(ref _interferenceCounter))]);

        private void StartCompetition()
            => PendingInterference ??= InterferenceEngine.ApplyPersistentAsync(
                "intervening DG transaction",
                [new WindowsMutationRequest(
                    InterferenceCapability,
                    "value-" + Interlocked.Increment(ref _interferenceCounter))]);

        private Task<TelemetryFrame?> CaptureAsync(int _, TimeSpan __, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            _captureCalls++;
            if ((_stage == CompetitionStage.DuringBeforeCapture && _captureCalls == 1)
                || (_stage == CompetitionStage.DuringAfterCapture && _captureCalls == 2))
                StartCompetition();

            return Task.FromResult<TelemetryFrame?>(new TelemetryFrame(
                DateTimeOffset.UtcNow,
                [
                    new TelemetryMetricObservation(
                        TelemetryStandardMetrics.FrameFpsAverage,
                        _captureCalls == 1 ? 100d : 110d,
                        TelemetryMetricQuality.Measured,
                        1d,
                        "system-activity-test",
                        TelemetryMetricOrigin.Direct)
                ]));
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
                    Description = "system activity executor test",
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
            InterferenceAdapter.ReleaseApply();
            try { Directory.Delete(_root, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
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
    }

    internal sealed class AdapterDouble : IWindowsCapabilityMutationAdapter
    {
        private readonly Func<Task>? _onApply;
        private readonly bool _blockApply;
        private readonly TaskCompletionSource<bool> _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal AdapterDouble(
            string capabilityId,
            string initialState,
            Func<Task>? onApply = null,
            bool blockApply = false)
        {
            CapabilityId = capabilityId;
            State = initialState;
            _onApply = onApply;
            _blockApply = blockApply;
        }

        public string CapabilityId { get; }
        public string State { get; private set; }
        public int SnapshotCount { get; private set; }
        public int ApplyCount { get; private set; }
        public int RollbackCount { get; private set; }
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
            if (_blockApply) await _release.Task.WaitAsync(cancellationToken);
            State = targetValue;
            if (_onApply is not null) await _onApply();
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
