using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;

internal static class GenericGuardianExperimentCoordinationSelfTests
{
    internal static async Task RunAsync()
    {
        await ExperimentBlocksControlledBenchmarkAndCancelledWaiterAsync();
        await ControlledBenchmarkBlocksExperimentAsync();
        await ExperimentBlocksOrdinarySystemOptimizationButAuthorizesOwnSessionAsync();
        await SystemOptimizationBlocksControlledBenchmarkAsync();
        await DisposedExperimentPermitCannotAuthorizeNestedTransactionAsync();
        Console.WriteLine("PASS Track 6 global experiment coordination: benchmark/DG exclusion, explicit nested canary permit, cancellation safety");
    }

    private static async Task ExperimentBlocksControlledBenchmarkAndCancelledWaiterAsync()
    {
        var coordinator = new PerformanceOperationCoordinationManager();
        var benchmark = new ControlledBenchmarkLeaseManager();
        var before = benchmark.SnapshotActivity();
        await using var experiment = await coordinator.AcquireGenericGuardianExperimentAsync("guardian-experiment");

        using var cts = new CancellationTokenSource();
        var waiting = benchmark.AcquireAsync("blocked-benchmark", cts.Token);
        await Task.Delay(75);
        Require(!waiting.IsCompleted,
            "A Guardian experiment lease must exclude controlled benchmarks for the whole experiment interval.");
        var whileWaiting = benchmark.SnapshotActivity();
        Require(whileWaiting.State == ControlledBenchmarkActivityState.Idle
                && whileWaiting.Generation == before.Generation,
            "A benchmark waiting on experiment ownership must not claim benchmark activity before it actually acquires the shared gate.");

        cts.Cancel();
        await RequireCancelledAsync(waiting);
        var afterCancel = benchmark.SnapshotActivity();
        Require(afterCancel.State == ControlledBenchmarkActivityState.Idle
                && afterCancel.Generation == before.Generation,
            "Cancellation while waiting for generic experiment exclusion must not fabricate benchmark activity or poison ownership.");

        await experiment.DisposeAsync();
        await using var subsequent = await benchmark.AcquireAsync("benchmark-after-cancel");
        Require(benchmark.SnapshotActivity().State == ControlledBenchmarkActivityState.Active,
            "A cancelled blocked benchmark must not poison later controlled benchmark acquisition.");
    }

    private static async Task ControlledBenchmarkBlocksExperimentAsync()
    {
        var coordinator = new PerformanceOperationCoordinationManager();
        var benchmark = new ControlledBenchmarkLeaseManager();
        var benchmarkLease = await benchmark.AcquireAsync("benchmark-first");
        var waitingExperiment = coordinator.AcquireGenericGuardianExperimentAsync("guardian-after-benchmark");
        await Task.Delay(75);
        Require(!waitingExperiment.IsCompleted,
            "An active controlled benchmark must exclude a generic Guardian experiment.");

        await benchmarkLease.DisposeAsync();
        await using var experiment = await waitingExperiment.WaitAsync(TimeSpan.FromSeconds(2));
        Require(experiment.IsActive,
            "Generic experiment ownership must become available after the controlled benchmark fully releases.");
    }

    private static async Task ExperimentBlocksOrdinarySystemOptimizationButAuthorizesOwnSessionAsync()
    {
        using var f = new Fixture(blockPersistentApply: false);
        var coordinator = new PerformanceOperationCoordinationManager();
        var baseline = f.Engine.SnapshotActivity();
        await using var experiment = await coordinator.AcquireGenericGuardianExperimentAsync("guardian-own-transaction");

        var ordinary = f.Engine.ApplyPersistentAsync(
            "ordinary-dg-operation",
            [new WindowsMutationRequest(Fixture.PersistentCapability, "ordinary")]);
        await Task.Delay(75);
        Require(!ordinary.IsCompleted,
            "Ordinary DG System Optimization must wait while a Guardian experiment owns global coordination.");
        var waitingSnapshot = f.Engine.SnapshotActivity();
        Require(SystemOptimizationActivitySnapshot.ProvesUninterruptedIdle(baseline, waitingSnapshot),
            "An ordinary DG waiter must not enter activity or hold the per-engine gate while blocked on global experiment ownership.");

        var session = await f.Engine.BeginSessionAsync(
            "guardian-owned-canary",
            [new WindowsMutationRequest(Fixture.SessionCapability, "canary")],
            experiment,
            CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));
        Require(f.SessionAdapter.State == "canary" && session is not null,
            "The exact active Guardian experiment permit must authorize its own nested session transaction without deadlock.");
        Require(!ordinary.IsCompleted,
            "An ordinary DG operation must stay excluded while the owning experiment continues.");

        await session.RestoreAsync().WaitAsync(TimeSpan.FromSeconds(2));
        Require(f.SessionAdapter.State == "session-old",
            "The experiment-owned session must be able to restore while the same coordination lease is still active.");

        await experiment.DisposeAsync();
        var ordinaryResult = await ordinary.WaitAsync(TimeSpan.FromSeconds(2));
        Require(ordinaryResult.Success && f.PersistentAdapter.State == "ordinary",
            "Releasing the experiment must allow the waiting ordinary DG operation to proceed.");
    }

    private static async Task SystemOptimizationBlocksControlledBenchmarkAsync()
    {
        using var f = new Fixture(blockPersistentApply: true);
        var benchmark = new ControlledBenchmarkLeaseManager();
        var operation = f.Engine.ApplyPersistentAsync(
            "blocking-system-operation",
            [new WindowsMutationRequest(Fixture.PersistentCapability, "held")]);

        await f.PersistentAdapter.ApplyEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var benchmarkWaiter = benchmark.AcquireAsync("benchmark-after-system-op");
        await Task.Delay(75);
        Require(!benchmarkWaiter.IsCompleted,
            "A running DG System Optimization operation must exclude controlled benchmark ownership.");

        f.PersistentAdapter.ReleaseApply();
        _ = await operation.WaitAsync(TimeSpan.FromSeconds(2));
        await using var benchmarkLease = await benchmarkWaiter.WaitAsync(TimeSpan.FromSeconds(2));
        Require(benchmark.SnapshotActivity().State == ControlledBenchmarkActivityState.Active,
            "Controlled benchmark may begin only after the DG System Optimization operation releases shared coordination.");
    }

    private static async Task DisposedExperimentPermitCannotAuthorizeNestedTransactionAsync()
    {
        using var f = new Fixture(blockPersistentApply: false);
        var coordinator = new PerformanceOperationCoordinationManager();
        var expired = await coordinator.AcquireGenericGuardianExperimentAsync("expired");
        await expired.DisposeAsync();

        await RequireThrowsAsync<InvalidOperationException>(() => f.Engine.BeginSessionAsync(
            "stale-permit",
            [new WindowsMutationRequest(Fixture.SessionCapability, "must-not-apply")],
            expired,
            CancellationToken.None));
        Require(f.SessionAdapter.State == "session-old" && f.SessionAdapter.ApplyCount == 0,
            "A disposed experiment permit must never bypass global System Optimization coordination or mutate Windows state.");
    }

    private static async Task RequireCancelledAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            return;
        }
        throw new InvalidOperationException("Expected cancellation while waiting for shared performance coordination.");
    }

    private static async Task RequireThrowsAsync<T>(Func<Task> action)
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
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class Fixture : IDisposable
    {
        internal const string SessionCapability = "test.coordination.session";
        internal const string PersistentCapability = "test.coordination.persistent";
        private readonly string _root;

        internal Fixture(bool blockPersistentApply)
        {
            _root = Path.Combine(Path.GetTempPath(), "dg-coordination-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            SessionAdapter = new CoordinationAdapter(SessionCapability, "session-old", false);
            PersistentAdapter = new CoordinationAdapter(PersistentCapability, "persistent-old", blockPersistentApply);
            var registry = new WindowsPerformanceCapabilityRegistry([
                Capability(SessionCapability, CapabilityPersistenceScope.SessionOnly),
                Capability(PersistentCapability, CapabilityPersistenceScope.PersistentAllowed)
            ]);
            Engine = new SystemOptimizationTransactionEngine(
                registry,
                new WindowsCapabilityMutationAdapterRegistry([SessionAdapter, PersistentAdapter]),
                new SnapshotService(Path.Combine(_root, "snapshots.json")),
                new HistoryService(Path.Combine(_root, "history.json")));
        }

        internal SystemOptimizationTransactionEngine Engine { get; }
        internal CoordinationAdapter SessionAdapter { get; }
        internal CoordinationAdapter PersistentAdapter { get; }

        private static WindowsPerformanceCapability Capability(
            string id,
            CapabilityPersistenceScope persistence)
            => new()
            {
                CapabilityId = id,
                Name = id,
                Description = "coordination self-test",
                Domain = CapabilityDomain.System,
                Availability = CapabilityAvailability.Available,
                PersistenceScope = persistence,
                Safety = ActionSafety.LiveSafe,
                RiskLevel = CapabilityRiskLevel.Safe
            };

        public void Dispose()
        {
            PersistentAdapter.ReleaseApply();
            try { Directory.Delete(_root, true); } catch { }
        }
    }

    internal sealed class CoordinationAdapter(
        string capabilityId,
        string initialState,
        bool blockApply) : IWindowsCapabilityMutationAdapter
    {
        private readonly TaskCompletionSource<bool> _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string CapabilityId { get; } = capabilityId;
        public string State { get; private set; } = initialState;
        public int ApplyCount { get; private set; }
        internal TaskCompletionSource<bool> ApplyEntered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal void ReleaseApply() => _release.TrySetResult(true);

        public Task<WindowsCapabilityReadResult> ReadCurrentAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(WindowsCapabilityReadResult.Ok(State));

        public WindowsCapabilityValidationResult Validate(string targetValue, SystemOptimizationScope scope)
            => WindowsCapabilityValidationResult.Ok();

        public Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new WindowsCapabilityMutationSnapshot(CapabilityId, State, State));

        public async Task<WindowsCapabilityApplyResult> ApplyAsync(
            string targetValue,
            CancellationToken cancellationToken = default)
        {
            ApplyCount++;
            ApplyEntered.TrySetResult(true);
            if (blockApply) await _release.Task.WaitAsync(cancellationToken);
            State = targetValue;
            return WindowsCapabilityApplyResult.Ok();
        }

        public Task<bool> VerifyAsync(string targetValue, CancellationToken cancellationToken = default)
            => Task.FromResult(State == targetValue);

        public Task RollbackAsync(
            WindowsCapabilityMutationSnapshot snapshot,
            CancellationToken cancellationToken = default)
        {
            State = snapshot.OriginalValue ?? string.Empty;
            return Task.CompletedTask;
        }
    }
}
