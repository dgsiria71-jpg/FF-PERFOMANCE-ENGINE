namespace FFPerformanceEngine.Core.Services;

internal enum PerformanceOperationCoordinationKind
{
    GenericGuardianExperiment,
    ControlledBenchmark,
    SystemOptimization
}

/// <summary>
/// Process-wide ownership token for performance-sensitive mutation/measurement
/// intervals. The public acquisition surface is intentionally limited to a
/// generic Guardian experiment; controlled benchmarks and System Optimization
/// enter through their existing authorities.
///
/// Holding this lease is mutual exclusion inside the DG process only. It does
/// not cover Regedit, vendor/driver tools or unrelated external software.
/// </summary>
public sealed class PerformanceOperationCoordinationLease : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate;
    private int _disposed;

    internal PerformanceOperationCoordinationLease(
        SemaphoreSlim gate,
        PerformanceOperationCoordinationKind kind,
        string owner)
    {
        _gate = gate;
        Kind = kind;
        Owner = owner;
    }

    internal PerformanceOperationCoordinationKind Kind { get; }
    internal string Owner { get; }
    public bool IsActive => Volatile.Read(ref _disposed) == 0;

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
            _gate.Release();
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// One process-wide gate shared by generic Guardian experiments, controlled
/// benchmarks and DG System Optimization. A Guardian experiment may explicitly
/// authorize its own nested session transaction so the outer interval can stay
/// exclusive without deadlocking on itself.
/// </summary>
public sealed class PerformanceOperationCoordinationManager
{
    private static readonly SemaphoreSlim GlobalGate = new(1, 1);

    public Task<PerformanceOperationCoordinationLease> AcquireGenericGuardianExperimentAsync(
        string owner,
        CancellationToken cancellationToken = default)
        => AcquireAsync(PerformanceOperationCoordinationKind.GenericGuardianExperiment, owner, cancellationToken);

    internal Task<PerformanceOperationCoordinationLease> AcquireControlledBenchmarkAsync(
        string owner,
        CancellationToken cancellationToken)
        => AcquireAsync(PerformanceOperationCoordinationKind.ControlledBenchmark, owner, cancellationToken);

    internal Task<PerformanceOperationCoordinationLease> AcquireSystemOptimizationAsync(
        string owner,
        CancellationToken cancellationToken)
        => AcquireAsync(PerformanceOperationCoordinationKind.SystemOptimization, owner, cancellationToken);

    internal static void RequireActiveGenericGuardianExperiment(
        PerformanceOperationCoordinationLease? lease)
    {
        if (lease is null
            || !lease.IsActive
            || lease.Kind != PerformanceOperationCoordinationKind.GenericGuardianExperiment)
            throw new InvalidOperationException(
                "A current generic Guardian experiment coordination lease is required for nested System Optimization.");
    }

    private static async Task<PerformanceOperationCoordinationLease> AcquireAsync(
        PerformanceOperationCoordinationKind kind,
        string owner,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(owner))
            throw new ArgumentException("A performance operation owner is required.", nameof(owner));

        await GlobalGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new PerformanceOperationCoordinationLease(GlobalGate, kind, owner.Trim());
    }
}
