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
        var acquire = leaseManagerType.GetMethod(
            "AcquireAsync",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: [typeof(string), typeof(CancellationToken)],
            modifiers: null);
        Require(acquire is not null,
            "ControlledBenchmarkLeaseManager must expose AcquireAsync(owner, cancellationToken).");

        var managerA = defaultConstructor!.Invoke([]);
        var managerB = defaultConstructor.Invoke([]);
        var leaseA = await InvokeTaskResultAsync(acquire!, managerA, "selftest-auto-tuner", CancellationToken.None);
        Require(leaseA is IAsyncDisposable,
            "AcquireAsync must return an async-disposable lease so every exit path releases ownership.");

        var pendingB = InvokeTask(acquire!, managerB, "selftest-profile-challenge", CancellationToken.None);
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
        await ((IAsyncDisposable)leaseB!).DisposeAsync().ConfigureAwait(false);

        Require(typeof(AutoTunerRunCoordinator).GetConstructors().Any(ctor =>
                ctor.GetParameters().Any(parameter => parameter.ParameterType == leaseInterface)),
            "AutoTunerRunCoordinator must accept the shared controlled benchmark lease authority.");
        Require(typeof(ProfileChallengeRoundService).GetConstructors().Any(ctor =>
                ctor.GetParameters().Any(parameter => parameter.ParameterType == leaseInterface)),
            "ProfileChallengeRoundService must accept the same controlled benchmark lease authority.");

        Console.WriteLine("PASS global controlled benchmark lease contract and Guardian ownership boundary");
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
}
