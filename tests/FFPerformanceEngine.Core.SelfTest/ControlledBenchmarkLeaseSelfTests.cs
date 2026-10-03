using System.Reflection;
using FFPerformanceEngine.Core.Services;

internal static class ControlledBenchmarkLeaseSelfTests
{
    internal static async Task RunAsync()
    {
        var coreAssembly = typeof(AutoTunerRunCoordinator).Assembly;
        var leaseInterface = coreAssembly.GetType("FFPerformanceEngine.Core.Services.IControlledBenchmarkLeaseManager");
        var leaseManagerType = coreAssembly.GetType("FFPerformanceEngine.Core.Services.ControlledBenchmarkLeaseManager");
        var guardianInterface = coreAssembly.GetType("FFPerformanceEngine.Core.Services.IControlledBenchmarkGuardian");

        Require(leaseInterface is not null,
            "Core must expose IControlledBenchmarkLeaseManager as the single authority for controlled benchmark ownership.");
        Require(leaseManagerType is not null,
            "Core must expose ControlledBenchmarkLeaseManager for global cross-workload benchmark exclusion.");
        Require(guardianInterface is not null,
            "Core must expose IControlledBenchmarkGuardian so benchmark ownership can suspend and reconcile Guardian.");
        Require(guardianInterface!.IsAssignableFrom(typeof(GuardianSessionHost)),
            "GuardianSessionHost must participate directly in controlled benchmark suspension/reconciliation.");

        var defaultConstructor = leaseManagerType!.GetConstructor(Type.EmptyTypes);
        Require(defaultConstructor is not null,
            "ControlledBenchmarkLeaseManager must support a Core-only default construction for non-UI callers/tests.");
        var guardianConstructor = leaseManagerType.GetConstructor([guardianInterface]);
        Require(guardianConstructor is not null,
            "ControlledBenchmarkLeaseManager must accept the Guardian benchmark participant used by AppServices.");
        var acquire = leaseManagerType.GetMethod(
            "AcquireAsync",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: [typeof(string), typeof(CancellationToken)],
            modifiers: null);
        Require(acquire is not null,
            "ControlledBenchmarkLeaseManager must expose AcquireAsync(owner, cancellationToken).");

        await GlobalOwnershipIsCrossManagerAndCancellationSafe(defaultConstructor!, acquire!).ConfigureAwait(false);
        await LeaseSuspendsAndRestoresGuardian(guardianConstructor!, acquire!).ConfigureAwait(false);
        await GuardianLifecycleChangesRemainDeferredDuringLease(guardianConstructor!, acquire!).ConfigureAwait(false);
        await AuthorityOwnedActivityIsGlobalAndDetectsTransientInterferenceAsync().ConfigureAwait(false);
        await FailedGuardianLifecycleCannotLeaveActivityStuckAsync().ConfigureAwait(false);

        Require(typeof(AutoTunerRunCoordinator).GetConstructors().Any(ctor =>
                ctor.GetParameters().Any(parameter => parameter.ParameterType == leaseInterface)),
            "AutoTunerRunCoordinator must accept the shared controlled benchmark lease authority.");
        Require(typeof(AutoTunerSessionService).GetConstructors().Any(ctor =>
                ctor.GetParameters().Any(parameter => parameter.ParameterType == leaseInterface)),
            "AutoTunerSessionService must pass the shared benchmark authority into every controlled run.");
        Require(typeof(ProfileChallengeRoundService).GetConstructors().Any(ctor =>
                ctor.GetParameters().Any(parameter => parameter.ParameterType == leaseInterface)),
            "ProfileChallengeRoundService must accept the same controlled benchmark lease authority.");

        Console.WriteLine("PASS global controlled benchmark lease, cancellation release, Guardian suspension/reconciliation, deferred lifecycle changes, and workload integration contract");
        Console.WriteLine("PASS Track 0 benchmark activity probe: global real ownership, monotonic transitions, no transient interference accepted, failure cleanup");
    }

    private static async Task AuthorityOwnedActivityIsGlobalAndDetectsTransientInterferenceAsync()
    {
        var managerA = new ControlledBenchmarkLeaseManager();
        var managerB = new ControlledBenchmarkLeaseManager();
        Require(managerA is IControlledBenchmarkActivityProbe && managerB is IControlledBenchmarkActivityProbe,
            "The concrete global manager, not a fabricated caller boolean, must implement the read-only activity probe.");
        var baseline = managerA.SnapshotActivity();
        var steady = managerB.SnapshotActivity();
        Require(baseline.State == ControlledBenchmarkActivityState.Idle
                && ControlledBenchmarkActivitySnapshot.ProvesUninterruptedIdle(baseline, steady),
            "Two unmodified idle observations across manager instances should prove an uninterrupted idle epoch.");

        var lease = await managerB.AcquireAsync("activity-selftest").ConfigureAwait(false);
        var active = managerA.SnapshotActivity();
        Require(active.State == ControlledBenchmarkActivityState.Active
                && active.Generation != baseline.Generation
                && !ControlledBenchmarkActivitySnapshot.ProvesUninterruptedIdle(baseline, active),
            "One instance must observe the other instance's acquisition before any benchmark work can begin.");

        using var cancelled = new CancellationTokenSource();
        var cancelledWaiter = managerA.AcquireAsync("activity-cancelled", cancelled.Token);
        Require(!cancelledWaiter.IsCompleted,
            "The second manager cannot bypass global ownership while another lease is active.");
        cancelled.Cancel();
        await RequireCancellationAsync(cancelledWaiter).ConfigureAwait(false);
        var afterCancel = managerA.SnapshotActivity();
        Require(afterCancel.State == ControlledBenchmarkActivityState.Active
                && afterCancel.Generation == active.Generation,
            "A cancelled waiter may neither steal ownership nor fabricate an acquisition/release transition.");

        await lease.DisposeAsync().ConfigureAwait(false);
        var afterRelease = managerB.SnapshotActivity();
        Require(afterRelease.State == ControlledBenchmarkActivityState.Idle
                && afterRelease.Generation != active.Generation
                && !ControlledBenchmarkActivitySnapshot.ProvesUninterruptedIdle(baseline, afterRelease),
            "A completed benchmark that began and ended between idle samples must still be detected by its epoch.");
        var idleAgain = managerA.SnapshotActivity();
        Require(ControlledBenchmarkActivitySnapshot.ProvesUninterruptedIdle(afterRelease, idleAgain),
            "Uninterrupted idle observations with identical epochs remain comparable.");
        await lease.DisposeAsync().ConfigureAwait(false);
        Require(managerA.SnapshotActivity().Generation == idleAgain.Generation,
            "Idempotent disposal must not invent a second release event.");
    }

    private static async Task FailedGuardianLifecycleCannotLeaveActivityStuckAsync()
    {
        var manager = new ControlledBenchmarkLeaseManager(new ThrowingGuardian(throwOnSuspend: true));
        var baseline = manager.SnapshotActivity();
        await RequireFailureAsync(() => manager.AcquireAsync("failed-suspend"));
        var released = manager.SnapshotActivity();
        Require(released.State == ControlledBenchmarkActivityState.Idle
                && released.Generation != baseline.Generation
                && !ControlledBenchmarkActivitySnapshot.ProvesUninterruptedIdle(baseline, released),
            "Failed Guardian suspension must release global ownership and record the interrupted interval.");

        var resumeManager = new ControlledBenchmarkLeaseManager(new ThrowingGuardian(throwOnSuspend: false));
        var lease = await resumeManager.AcquireAsync("failed-resume").ConfigureAwait(false);
        Require(manager.SnapshotActivity().State == ControlledBenchmarkActivityState.Active,
            "Lease must stay marked active until all Guardian reconciliation is attempted.");
        await RequireFailureAsync(() => lease.DisposeAsync().AsTask());
        Require(resumeManager.SnapshotActivity().State == ControlledBenchmarkActivityState.Idle,
            "Even failed Guardian resume must never leak active-state or block a future global lease.");
        await using var subsequent = await new ControlledBenchmarkLeaseManager().AcquireAsync("after-failure").ConfigureAwait(false);
        Require(manager.SnapshotActivity().State == ControlledBenchmarkActivityState.Active,
            "A subsequent lease must remain possible after reconciliation failure.");
    }

    private static async Task RequireFailureAsync(Func<Task> operation)
    {
        try { await operation().ConfigureAwait(false); }
        catch (InvalidOperationException) { return; }
        throw new InvalidOperationException("Expected injected Guardian lifecycle failure to propagate.");
    }

    private static async Task GlobalOwnershipIsCrossManagerAndCancellationSafe(ConstructorInfo defaultConstructor, MethodInfo acquire)
    {
        var managerA = defaultConstructor.Invoke([]);
        var managerB = defaultConstructor.Invoke([]);
        var leaseA = await InvokeTaskResultAsync(acquire, managerA, "selftest-auto-tuner", CancellationToken.None).ConfigureAwait(false);
        Require(leaseA is IAsyncDisposable,
            "AcquireAsync must return an async-disposable lease so every exit path releases ownership.");

        var pendingB = InvokeTask(acquire, managerB, "selftest-profile-challenge", CancellationToken.None);
        await Task.Delay(75).ConfigureAwait(false);
        Require(!pendingB.IsCompleted,
            "Controlled benchmark ownership must be global across manager instances, not per BlueStacks instance or caller.");

        await ((IAsyncDisposable)leaseA!).DisposeAsync().ConfigureAwait(false);
        var completed = await Task.WhenAny(pendingB, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
        Require(ReferenceEquals(completed, pendingB) && pendingB.IsCompletedSuccessfully,
            "Releasing the first global benchmark lease must unblock the next controlled workload.");
        var leaseB = ReadTaskResult(pendingB);
        Require(leaseB is IAsyncDisposable,
            "The second controlled workload must receive ownership after the first lease is released.");

        using var cancelled = new CancellationTokenSource();
        var waitingCancelled = InvokeTask(acquire, managerA, "selftest-cancelled", cancelled.Token);
        await Task.Delay(50).ConfigureAwait(false);
        cancelled.Cancel();
        await RequireCancellationAsync(waitingCancelled).ConfigureAwait(false);

        await ((IAsyncDisposable)leaseB!).DisposeAsync().ConfigureAwait(false);
        var finalLease = await InvokeTaskResultAsync(acquire, managerA, "selftest-after-cancel", CancellationToken.None).ConfigureAwait(false);
        Require(finalLease is IAsyncDisposable,
            "A cancelled waiter must never poison or consume the global benchmark lease.");
        await ((IAsyncDisposable)finalLease!).DisposeAsync().ConfigureAwait(false);
    }

    private static async Task LeaseSuspendsAndRestoresGuardian(ConstructorInfo guardianConstructor, MethodInfo acquire)
    {
        var runner = new FakeLiveRunner();
        await using var host = new GuardianSessionHost(runner);
        await host.StartAsync("Pie64", TimeSpan.FromMilliseconds(25)).ConfigureAwait(false);
        await runner.WaitForStartsAsync(1).ConfigureAwait(false);
        Require(host.IsRunning, "Guardian precondition must be an active live-session loop.");

        var manager = guardianConstructor.Invoke([host]);
        var lease = await InvokeTaskResultAsync(acquire, manager, "selftest-guardian-suspension", CancellationToken.None).ConfigureAwait(false);
        Require(lease is IAsyncDisposable,
            "Guardian-connected manager must still return an async-disposable benchmark lease.");
        Require(!host.IsRunning && runner.CancelledInstances.Contains("Pie64"),
            "Acquiring controlled benchmark ownership must stop Guardian before benchmark work can start.");
        Require(runner.ResetCount == 1,
            "Suspending Guardian must reset the active binding/cooldown runner so benchmark telemetry cannot overlap stale intervention state.");

        await ((IAsyncDisposable)lease!).DisposeAsync().ConfigureAwait(false);
        await runner.WaitForStartsAsync(2).ConfigureAwait(false);
        Require(host.IsRunning && string.Equals(host.InstanceName, "Pie64", StringComparison.OrdinalIgnoreCase),
            "Releasing controlled benchmark ownership must reconcile Guardian back to the exact pre-benchmark instance.");
    }

    private static async Task GuardianLifecycleChangesRemainDeferredDuringLease(ConstructorInfo guardianConstructor, MethodInfo acquire)
    {
        var runner = new FakeLiveRunner();
        await using var host = new GuardianSessionHost(runner);
        await host.StartAsync("Pie64", TimeSpan.FromMilliseconds(25)).ConfigureAwait(false);
        await runner.WaitForStartsAsync(1).ConfigureAwait(false);

        var manager = guardianConstructor.Invoke([host]);
        var lease = await InvokeTaskResultAsync(acquire, manager, "selftest-guardian-lifecycle", CancellationToken.None).ConfigureAwait(false);
        Require(lease is IAsyncDisposable && !host.IsRunning,
            "Guardian must be suspended before exercising lifecycle changes during a controlled benchmark.");

        await host.StartAsync("Android11", TimeSpan.FromMilliseconds(40)).ConfigureAwait(false);
        await Task.Delay(75).ConfigureAwait(false);
        Require(!host.IsRunning && runner.StartCount == 1,
            "A settings/lifecycle StartAsync during a controlled benchmark must be deferred instead of contaminating the measurement.");

        await host.StopAsync().ConfigureAwait(false);
        await ((IAsyncDisposable)lease!).DisposeAsync().ConfigureAwait(false);
        await Task.Delay(75).ConfigureAwait(false);
        Require(!host.IsRunning && host.InstanceName is null && runner.StartCount == 1,
            "If Guardian is explicitly stopped while suspended, lease release must not resurrect the pre-benchmark Guardian session.");
    }

    private static async Task RequireCancellationAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
            throw new InvalidOperationException("A cancelled benchmark waiter must complete with cancellation.");
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static Task InvokeTask(MethodInfo method, object target, params object?[] args)
    {
        object? invocation;
        try { invocation = method.Invoke(target, args); }
        catch (TargetInvocationException exception) when (exception.InnerException is not null) { throw exception.InnerException; }
        Require(invocation is Task, $"{method.Name} must return Task or Task<T>.");
        return (Task)invocation!;
    }

    private static async Task<object?> InvokeTaskResultAsync(MethodInfo method, object target, params object?[] args)
    {
        var task = InvokeTask(method, target, args);
        await task.ConfigureAwait(false);
        return ReadTaskResult(task);
    }

    private static object? ReadTaskResult(Task task)
        => task.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance)?.GetValue(task);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class ThrowingGuardian(bool throwOnSuspend) : IControlledBenchmarkGuardian
    {
        public Task<ControlledBenchmarkGuardianState> SuspendAsync(CancellationToken cancellationToken = default)
            => throwOnSuspend
                ? Task.FromException<ControlledBenchmarkGuardianState>(new InvalidOperationException("suspend failure"))
                : Task.FromResult(new ControlledBenchmarkGuardianState(false, null, TimeSpan.Zero));

        public Task ResumeAsync(ControlledBenchmarkGuardianState state, CancellationToken cancellationToken = default)
            => Task.FromException(new InvalidOperationException("resume failure"));
    }

    private sealed class FakeLiveRunner : IGuardianLiveSessionRunner
    {
        private readonly object _sync = new();
        private readonly List<TaskCompletionSource> _startWaiters = [];

        public int StartCount { get; private set; }
        public int ResetCount { get; private set; }
        public List<string> CancelledInstances { get; } = [];

        public async Task RunAsync(
            string instanceName,
            TimeSpan interval,
            Action<GuardianLiveSessionStatus> publish,
            CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                StartCount++;
                foreach (var waiter in _startWaiters) waiter.TrySetResult();
                _startWaiters.Clear();
            }

            publish(new GuardianLiveSessionStatus { Message = "live" });
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                CancelledInstances.Add(instanceName);
            }
        }

        public Task ResetAsync(CancellationToken cancellationToken = default)
        {
            ResetCount++;
            return Task.CompletedTask;
        }

        public async Task WaitForStartsAsync(int count)
        {
            Task waiter;
            lock (_sync)
            {
                if (StartCount >= count) return;
                var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _startWaiters.Add(source);
                waiter = source.Task;
            }
            await waiter.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }
    }
}
