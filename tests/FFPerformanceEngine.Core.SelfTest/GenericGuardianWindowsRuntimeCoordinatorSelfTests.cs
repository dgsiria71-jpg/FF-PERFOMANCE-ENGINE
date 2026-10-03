using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Workloads;

internal static class GenericGuardianWindowsRuntimeCoordinatorSelfTests
{
    internal static async Task RunAsync()
    {
        await ConstructionIsIdleAndExplicitStartSerializesCyclesAsync();
        await CycleFailureStopsAndAutomaticallyResetsRuntimeAsync();
        await FailedFailureCleanupRemainsRetryableThroughExplicitStopAsync();
        Console.WriteLine("PASS Track 6 explicit generic runtime coordinator is idle-by-default, serialized and cleanup-safe");
    }

    private static async Task ConstructionIsIdleAndExplicitStartSerializesCyclesAsync()
    {
        var runtime = new FakeRuntime(cycleDelay: TimeSpan.FromMilliseconds(35));
        await using var coordinator = new GenericGuardianWindowsRuntimeCoordinator(runtime);
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
        await using var coordinator = new GenericGuardianWindowsRuntimeCoordinator(runtime);

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
        await using var coordinator = new GenericGuardianWindowsRuntimeCoordinator(runtime);

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
