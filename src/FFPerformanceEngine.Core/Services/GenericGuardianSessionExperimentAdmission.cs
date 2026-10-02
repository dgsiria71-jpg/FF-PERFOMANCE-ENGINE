using FFPerformanceEngine.Core.SystemOptimization;

namespace FFPerformanceEngine.Core.Services;

/// <summary>
/// Process-local admission coordinator for one generic Guardian live experiment.
/// Acquisition order is fixed: Track 0 measurement exclusion first, then DG
/// System Optimization exclusion. The returned lease blocks new controlled
/// benchmarks and non-admitted DG transactions until disposal.
///
/// This is exclusion only. It does not prove scene/mode/load, does not detect
/// external vendor/registry tools and does not suspend/reconcile the specialized
/// Guardian by itself.
/// </summary>
public sealed class GenericGuardianSessionExperimentAdmissionManager
{
    private readonly ControlledBenchmarkLeaseManager _benchmarks;
    private readonly SystemOptimizationTransactionEngine _transactions;

    public GenericGuardianSessionExperimentAdmissionManager(
        ControlledBenchmarkLeaseManager benchmarks,
        SystemOptimizationTransactionEngine transactions)
    {
        _benchmarks = benchmarks ?? throw new ArgumentNullException(nameof(benchmarks));
        _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
    }

    public async Task<GenericGuardianSessionExperimentAdmissionLease> AcquireAsync(
        string owner,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(owner))
            throw new ArgumentException("A generic Guardian experiment owner is required.", nameof(owner));

        var benchmarkExclusion = await _benchmarks
            .AcquireExperimentExclusionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var systemAdmission = await _transactions
                .AcquireExperimentAdmissionAsync(cancellationToken)
                .ConfigureAwait(false);

            return new GenericGuardianSessionExperimentAdmissionLease(
                _transactions,
                benchmarkExclusion,
                systemAdmission);
        }
        catch
        {
            await benchmarkExclusion.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}

/// <summary>
/// Owns both process-local exclusions. Only this unforgeable lease can route the
/// canary's own Track2 transaction through the DG admission gate. Disposal is
/// idempotent and releases in reverse acquisition order.
/// </summary>
public sealed class GenericGuardianSessionExperimentAdmissionLease : IAsyncDisposable
{
    private readonly SystemOptimizationTransactionEngine _transactions;
    private readonly IAsyncDisposable _benchmarkExclusion;
    private readonly SystemOptimizationExperimentAdmissionLease _systemAdmission;
    private int _disposed;

    internal GenericGuardianSessionExperimentAdmissionLease(
        SystemOptimizationTransactionEngine transactions,
        IAsyncDisposable benchmarkExclusion,
        SystemOptimizationExperimentAdmissionLease systemAdmission)
    {
        _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
        _benchmarkExclusion = benchmarkExclusion ?? throw new ArgumentNullException(nameof(benchmarkExclusion));
        _systemAdmission = systemAdmission ?? throw new ArgumentNullException(nameof(systemAdmission));
    }

    public bool IsActive => Volatile.Read(ref _disposed) == 0 && _systemAdmission.IsActive;

    public Task<SystemOptimizationSession> BeginSystemOptimizationSessionAsync(
        string label,
        IReadOnlyList<WindowsMutationRequest> mutations,
        CancellationToken cancellationToken = default)
    {
        if (!IsActive)
            throw new InvalidOperationException("The generic Guardian experiment admission lease is no longer active.");

        return _transactions.BeginSessionUnderExperimentAsync(
            label,
            mutations,
            _systemAdmission,
            cancellationToken);
    }

    internal Task RestoreSystemOptimizationSessionAsync(
        SystemOptimizationSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (!IsActive)
            throw new InvalidOperationException("The generic Guardian experiment admission lease is no longer active.");

        return session.RestoreUnderExperimentAsync(
            _systemAdmission,
            cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

        await _systemAdmission.DisposeAsync().ConfigureAwait(false);
        await _benchmarkExclusion.DisposeAsync().ConfigureAwait(false);
    }
}
