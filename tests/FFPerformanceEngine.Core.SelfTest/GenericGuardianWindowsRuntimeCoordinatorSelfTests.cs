using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;
using FFPerformanceEngine.Core.Telemetry;
using FFPerformanceEngine.Core.Workloads;

internal static class GenericGuardianWindowsRuntimeCoordinatorSelfTests
{
    internal static async Task RunAsync()
    {
        await ConstructionIsIdleAndExplicitStartSerializesCyclesAsync();
        await CycleFailureStopsAndAutomaticallyResetsRuntimeAsync();
        await FailedFailureCleanupRemainsRetryableThroughExplicitStopAsync();
        await ActivationReadinessBlocksStartBeforeRuntimeCycleAsync();
        ConcreteReadinessRequiresAdapterEvidenceAndExplicitPolicy();
        Console.WriteLine("PASS Track 6 explicit generic runtime coordinator is idle-by-default, readiness-gated, serialized and cleanup-safe");
    }

    private static async Task ConstructionIsIdleAndExplicitStartSerializesCyclesAsync()
    {
        var runtime = new FakeRuntime(cycleDelay: TimeSpan.FromMilliseconds(35));
        await using var coordinator = new GenericGuardianWindowsRuntimeCoordinator(
            runtime,
            FixedReadiness.Ready);
        var plan = Plan(interval: TimeSpan.FromMilliseconds(5));

        await Task.Delay(75);
        Require(runtime.RunCount == 0
                && runtime.ResetCount == 0
                && !coordinator.IsRunning,
            "Constructing the continuous generic runtime coordinator must perform no cycle, reset or implicit startup work.");

        await coordinator.StartAsync(plan);
        await runtime.WaitForRunsAsync(2);

        Require(coordinator.IsRunning
                && runtime.RunCount >= 2
                && runtime.MaxConcurrentRuns == 1
                && ReferenceEquals(runtime.LastCatalog, plan.Catalog)
                && string.Equals(runtime.LastGameId, plan.GameId, StringComparison.Ordinal)
                && runtime.LastSystemOnline == plan.SystemOnline
                && runtime.LastObservationDuration == plan.ObservationDuration
                && ReferenceEquals(runtime.LastAnalysisContext, plan.AnalysisContext),
            "Explicit start must run the caller-supplied already-resolved plan one cycle at a time without discovery or overlap.");

        await coordinator.StopAsync();

        Require(!coordinator.IsRunning
                && runtime.ResetCount == 1
                && !runtime.HasRetainedState,
            "Explicit stop must cancel the schedule and protected-reset retained runtime state before returning.");
    }

    private static async Task CycleFailureStopsAndAutomaticallyResetsRuntimeAsync()
    {
        var runtime = new FakeRuntime(
            cycleDelay: TimeSpan.FromMilliseconds(5),
            failOnRun: 2);
        await using var coordinator = new GenericGuardianWindowsRuntimeCoordinator(
            runtime,
            FixedReadiness.Ready);

        await coordinator.StartAsync(Plan(TimeSpan.FromMilliseconds(5)));
        await runtime.WaitForRunsAsync(2);
        await WaitUntilAsync(() => !coordinator.IsRunning);

        Require(coordinator.LastFailure is InvalidOperationException
                && runtime.ResetCount == 1
                && !runtime.HasRetainedState,
            "A cycle failure must stop scheduling and automatically reset the protected runtime instead of leaving retained state orphaned.");

        await coordinator.StopAsync();
        Require(runtime.ResetCount == 2,
            "Explicit stop after a failed loop must retry/confirm runtime reset before allowing another start.");
    }

    private static async Task FailedFailureCleanupRemainsRetryableThroughExplicitStopAsync()
    {
        var runtime = new FakeRuntime(
            cycleDelay: TimeSpan.FromMilliseconds(5),
            failOnRun: 1,
            resetFailures: 1);
        await using var coordinator = new GenericGuardianWindowsRuntimeCoordinator(
            runtime,
            FixedReadiness.Ready);

        await coordinator.StartAsync(Plan(TimeSpan.FromMilliseconds(5)));
        await runtime.WaitForRunsAsync(1);
        await WaitUntilAsync(() => !coordinator.IsRunning);

        Require(coordinator.LastFailure is AggregateException
                && runtime.ResetCount == 1
                && runtime.HasRetainedState,
            "If automatic failure cleanup itself fails, coordinator must expose the combined failure and preserve retryable runtime ownership.");

        await coordinator.StopAsync();

        Require(runtime.ResetCount == 2
                && !runtime.HasRetainedState,
            "Explicit stop must retry a previously failed cleanup and leave no retained state before a future start.");
    }

    private static async Task ActivationReadinessBlocksStartBeforeRuntimeCycleAsync()
    {
        var runtime = new FakeRuntime(TimeSpan.FromMilliseconds(5));
        var gate = new FixedReadiness(
            new GenericGuardianRuntimeActivationReadiness(
                GenericGuardianRuntimeActivationReadinessStatus.AdapterCanaryContextUnavailable,
                "test adapter has no canary context evidence"));
        await using var coordinator = new GenericGuardianWindowsRuntimeCoordinator(runtime, gate);

        await RequireThrowsAsync<InvalidOperationException>(
            () => coordinator.StartAsync(Plan(TimeSpan.FromMilliseconds(5))),
            "NotReady activation must reject scheduled Start.");

        Require(runtime.RunCount == 0
                && runtime.ResetCount == 0
                && !coordinator.IsRunning
                && coordinator.CompletedCycles == 0
                && coordinator.LastReadiness.Status
                    == GenericGuardianRuntimeActivationReadinessStatus.AdapterCanaryContextUnavailable,
            "Readiness rejection must occur before any runtime cycle/reset and expose the exact fail-closed reason.");
    }

    private static void ConcreteReadinessRequiresAdapterEvidenceAndExplicitPolicy()
    {
        const string gameId = "test:activation-ready";
        var candidate = new GenericGuardianSessionActionCandidate
        {
            GameId = gameId,
            Family = GuardianAnomalyKind.GpuSaturation,
            Action = new GuardianAction
            {
                Id = "test.activation.action",
                Description = "test only",
                Safety = ActionSafety.LiveSafe
            }
        };
        var mutationCatalog = new GenericGuardianSessionMutationCatalog([
            new GenericGuardianSessionMutationDefinition(
                gameId,
                candidate.Family,
                candidate.Action.Id,
                new WindowsMutationRequest("test.activation.capability", "on", "off"))
        ]);
        var budget = new GenericGuardianSessionActionBudget(
            TimeSpan.FromMinutes(1),
            maxAttemptsPerSession: 1);

        var ffPlan = PlanFor(
            gameId,
            BlueStacksFreeFireGameAdapter.For(GameKind.FreeFire));
        var ffGate = new GenericGuardianRuntimeActivationReadinessGate(
            [candidate],
            mutationCatalog,
            budget,
            new GenericGuardianCanaryEvidenceSourceRegistration(
                "bluestacks.free-fire",
                "test.ff.evidence",
                _ => new FakeEvidenceSource()));
        var ffReadiness = ffGate.Evaluate(ffPlan);
        Require(ffReadiness.Status
                    == GenericGuardianRuntimeActivationReadinessStatus.AdapterCanaryContextUnavailable,
            "Current Free Fire adapter must remain NotReady while CanaryContextEvidence=false even when all other policy objects are supplied.");

        var capablePlan = PlanFor(gameId, new CanaryReadyAdapter());
        var noEvidence = new GenericGuardianRuntimeActivationReadinessGate(
            [candidate],
            mutationCatalog,
            budget,
            evidenceRegistration: null).Evaluate(capablePlan);
        Require(noEvidence.Status
                    == GenericGuardianRuntimeActivationReadinessStatus.EvidenceSourceNotRegistered,
            "Canary-capable adapter alone must not start without an explicit evidence-source registration.");

        var registration = new GenericGuardianCanaryEvidenceSourceRegistration(
            "test.canary-ready",
            "test.activation.evidence",
            _ => new FakeEvidenceSource());

        var noCandidates = new GenericGuardianRuntimeActivationReadinessGate(
            Array.Empty<GenericGuardianSessionActionCandidate>(),
            mutationCatalog,
            budget,
            registration).Evaluate(capablePlan);
        Require(noCandidates.Status
                    == GenericGuardianRuntimeActivationReadinessStatus.NoApprovedCandidates,
            "Scheduled activation must require explicit candidate policy rather than inventing actions.");

        var missingBinding = new GenericGuardianRuntimeActivationReadinessGate(
            [candidate],
            new GenericGuardianSessionMutationCatalog(
                Array.Empty<GenericGuardianSessionMutationDefinition>()),
            budget,
            registration).Evaluate(capablePlan);
        Require(missingBinding.Status
                    == GenericGuardianRuntimeActivationReadinessStatus.MissingMutationBinding,
            "Every approved candidate must have an exact registered mutation binding before activation.");

        var ready = new GenericGuardianRuntimeActivationReadinessGate(
            [candidate],
            mutationCatalog,
            budget,
            registration).Evaluate(capablePlan);
        Require(ready.IsReady
                && ready.Status == GenericGuardianRuntimeActivationReadinessStatus.Ready,
            "A canary-capable adapter plus matching evidence registration, explicit candidates, exact catalog bindings and caller-defined budget may pass readiness.");
    }

    private static GenericGuardianWindowsRuntimeLoopPlan PlanFor(
        string gameId,
        IGameAdapter adapter)
        => new()
        {
            Catalog = new ResolvedGameCatalogResult
            {
                Games =
                [
                    new ResolvedGameCatalogEntry
                    {
                        Identity = new GameIdentity
                        {
                            GameId = gameId,
                            Name = gameId,
                            AdapterId = adapter.AdapterId
                        },
                        Adapter = adapter
                    }
                ]
            },
            GameId = gameId,
            SystemOnline = true,
            ObservationDuration = TimeSpan.FromMilliseconds(10),
            AnalysisContext = new BottleneckAnalysisContext(),
            Interval = TimeSpan.FromMilliseconds(10)
        };

    private static async Task RequireThrowsAsync<T>(
        Func<Task> action,
        string message)
        where T : Exception
    {
        try
        {
            await action();
        }
        catch (T)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    private sealed class FixedReadiness(
        GenericGuardianRuntimeActivationReadiness result)
        : IGenericGuardianRuntimeActivationReadinessGate
    {
        internal static FixedReadiness Ready { get; } = new(
            new GenericGuardianRuntimeActivationReadiness(
                GenericGuardianRuntimeActivationReadinessStatus.Ready,
                "test readiness"));

        public GenericGuardianRuntimeActivationReadiness Evaluate(
            GenericGuardianWindowsRuntimeLoopPlan plan)
            => result;
    }

    private sealed class CanaryReadyAdapter : IGameAdapter
    {
        public string AdapterId => "test.canary-ready";
        public int Priority => 100;
        public bool IsGeneric => false;
        public GameAdapterCapabilities Capabilities { get; } = new()
        {
            CanaryContextEvidence = true
        };
    }

    private sealed class FakeEvidenceSource : IGenericGuardianCanaryEvidenceSource
    {
        public Task<GenericGuardianCanaryComparisonWindow?> CaptureWindowAsync(
            TelemetryWorkloadTarget target,
            TelemetryFrame frame,
            DateTimeOffset captureStartedAt,
            DateTimeOffset captureCompletedAt,
            CancellationToken cancellationToken = default)
            => Task.FromResult<GenericGuardianCanaryComparisonWindow?>(null);
    }

    private static GenericGuardianWindowsRuntimeLoopPlan Plan(TimeSpan interval)
        => new()
        {
            Catalog = new ResolvedGameCatalogResult(),
            GameId = "test.explicit.generic.runtime",
            SystemOnline = true,
            ObservationDuration = TimeSpan.FromMilliseconds(10),
            AnalysisContext = new BottleneckAnalysisContext(),
            Interval = interval
        };

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(5);
        while (!predicate())
        {
            if (DateTimeOffset.UtcNow >= deadline)
                throw new TimeoutException("Timed out waiting for generic runtime coordinator state.");
            await Task.Delay(10);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class FakeRuntime(
        TimeSpan cycleDelay,
        int? failOnRun = null,
        int resetFailures = 0) : IGenericGuardianWindowsRuntime
    {
        private readonly object _sync = new();
        private readonly List<TaskCompletionSource> _runWaiters = [];
        private int _activeRuns;
        private int _resetFailures = resetFailures;

        public GenericGuardianCanarySessionKey? CurrentSession => null;
        public int RetainedLeaseCount => HasRetainedState ? 1 : 0;
        internal int RunCount { get; private set; }
        internal int ResetCount { get; private set; }
        internal int MaxConcurrentRuns { get; private set; }
        internal bool HasRetainedState { get; private set; }
        internal ResolvedGameCatalogResult? LastCatalog { get; private set; }
        internal string? LastGameId { get; private set; }
        internal bool LastSystemOnline { get; private set; }
        internal TimeSpan LastObservationDuration { get; private set; }
        internal BottleneckAnalysisContext? LastAnalysisContext { get; private set; }

        public async Task<GenericGuardianWindowsRuntimeCycleResult> RunCycleAsync(
            ResolvedGameCatalogResult catalog,
            string requestedGameId,
            bool systemOnline,
            TimeSpan observationCaptureDuration,
            BottleneckAnalysisContext analysisContext,
            CancellationToken cancellationToken = default)
        {
            int run;
            lock (_sync)
            {
                RunCount++;
                run = RunCount;
                _activeRuns++;
                MaxConcurrentRuns = Math.Max(MaxConcurrentRuns, _activeRuns);
                HasRetainedState = true;
                LastCatalog = catalog;
                LastGameId = requestedGameId;
                LastSystemOnline = systemOnline;
                LastObservationDuration = observationCaptureDuration;
                LastAnalysisContext = analysisContext;
                foreach (var waiter in _runWaiters) waiter.TrySetResult();
                _runWaiters.Clear();
            }

            try
            {
                await Task.Delay(cycleDelay, cancellationToken);
                if (failOnRun == run)
                    throw new InvalidOperationException("intentional scheduled generic runtime cycle failure");

                var observation = new GenericGuardianWorkloadObservation
                {
                    State = new GuardianWorkloadStateSnapshot(),
                    Signals = new GenericGuardianWorkloadSignals()
                };
                var classification = new GenericGuardianBottleneckClassification
                {
                    Observation = observation,
                    Analysis = new BottleneckAnalysisResult
                    {
                        Primary = BottleneckKind.Unknown,
                        Confidence = 0,
                        Candidates = Array.Empty<BottleneckCandidate>(),
                        Signals = Array.Empty<string>()
                    }
                };
                return new GenericGuardianWindowsRuntimeCycleResult
                {
                    Observation = observation,
                    Classification = classification,
                    Eligibility = new GenericGuardianSessionActionEligibility
                    {
                        State = observation.State,
                        Family = GuardianAnomalyKind.Unknown,
                        EligibleCandidates = Array.Empty<GenericGuardianSessionActionCandidate>()
                    },
                    Reason = "fake scheduled cycle"
                };
            }
            finally
            {
                lock (_sync) _activeRuns--;
            }
        }

        public Task ResetAsync(CancellationToken cancellationToken = default)
        {
            ResetCount++;
            if (_resetFailures > 0)
            {
                _resetFailures--;
                throw new InvalidOperationException("intentional scheduled generic runtime reset failure");
            }

            HasRetainedState = false;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            HasRetainedState = false;
            return ValueTask.CompletedTask;
        }

        internal async Task WaitForRunsAsync(int count)
        {
            Task waiter;
            lock (_sync)
            {
                if (RunCount >= count) return;
                var source = new TaskCompletionSource(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                _runWaiters.Add(source);
                waiter = source.Task;
            }

            await waiter.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }
}
