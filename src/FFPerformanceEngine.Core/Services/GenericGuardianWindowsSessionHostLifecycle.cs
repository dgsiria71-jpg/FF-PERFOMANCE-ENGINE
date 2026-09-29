using FFPerformanceEngine.Core.Telemetry;

namespace FFPerformanceEngine.Core.Services;

/// <summary>
/// Owns the lifecycle boundary for one generic Guardian Windows session.
/// The OS-backed epoch and every retained reversible canary lease are kept
/// together so a session can never be reset/rebound/disposed before retained
/// Windows state has been restored.
///
/// This is NOT the full generic Guardian runtime host. It does not classify,
/// admit, execute, schedule, provide scene evidence or exclude concurrent
/// benchmarks/transactions. It only closes lifecycle ownership/cleanup.
/// </summary>
public sealed class GenericGuardianWindowsSessionHostLifecycle : IAsyncDisposable
{
    private readonly GenericGuardianWindowsSessionLifecycleCoordinator _sessionOwner = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly List<GenericGuardianSessionCanaryLease> _retainedLeases = new();
    private GenericGuardianCanarySessionKey? _current;
    private int _retainedLeaseCount;
    private bool _disposed;

    public GenericGuardianCanarySessionKey? CurrentSession => Volatile.Read(ref _current);
    public int RetainedLeaseCount => Volatile.Read(ref _retainedLeaseCount);

    /// <summary>
    /// Observes one already-resolved workload state. An unchanged, still-live
    /// physical process keeps its exact owner-issued key. Any lifecycle change
    /// first restores all retained leases; only after successful cleanup may
    /// the old coordinator epoch be reset and a new one be issued.
    /// </summary>
    public async Task<GenericGuardianCanarySessionKey?> ObserveAsync(
        GuardianWorkloadStateSnapshot? state,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var current = _current;
            if (current is not null
                && IsEligibleState(state)
                && MatchesIdentity(current, state!.Target)
                && _sessionOwner.IsCurrent(current, state.Target))
                return current;

            if (current is not null || _retainedLeases.Count != 0)
                await EndCurrentSessionCoreAsync().ConfigureAwait(false);

            var next = _sessionOwner.Observe(state);
            Volatile.Write(ref _current, next);
            return next;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Transfers one executor KEEP result to lifecycle ownership. Only the exact
    /// current owner-issued key may transfer an active Improved lease. If the
    /// transfer is rejected, an active lease is restored immediately so callers
    /// cannot accidentally orphan a Windows mutation.
    /// </summary>
    public async Task<bool> RetainAsync(
        GenericGuardianCanarySessionKey? session,
        GenericGuardianSessionCanaryResult? result)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var lease = result?.ActiveLease;
        if (lease is null) return false;

        await _gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var current = _current;
            var validTransfer = result!.Attempted
                                && result.Kept
                                && result.Verdict == GenericGuardianSessionCanaryVerdict.Improved
                                && lease.IsActive
                                && current is not null
                                && ReferenceEquals(session, current)
                                && _sessionOwner.IsCurrent(current, TargetFrom(current));

            if (!validTransfer)
            {
                if (lease.IsActive)
                    await lease.RestoreAsync(CancellationToken.None).ConfigureAwait(false);
                return false;
            }

            if (_retainedLeases.Any(item => item.RestorePointId == lease.RestorePointId))
                return true;

            _retainedLeases.Add(lease);
            Volatile.Write(ref _retainedLeaseCount, _retainedLeases.Count);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Restores all retained leases before retiring the current OS-backed epoch.
    /// Once teardown starts, caller cancellation cannot interrupt restoration.
    /// </summary>
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            await EndCurrentSessionCoreAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        await _gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        var disposeGate = false;
        try
        {
            if (_disposed) return;
            await EndCurrentSessionCoreAsync().ConfigureAwait(false);
            _disposed = true;
            disposeGate = true;
        }
        finally
        {
            _gate.Release();
            if (disposeGate) _gate.Dispose();
        }
    }

    private async Task EndCurrentSessionCoreAsync()
    {
        if (_retainedLeases.Count != 0)
        {
            var failures = new List<Exception>();
            for (var index = _retainedLeases.Count - 1; index >= 0; index--)
            {
                var lease = _retainedLeases[index];
                if (!lease.IsActive)
                {
                    _retainedLeases.RemoveAt(index);
                    continue;
                }

                try
                {
                    await lease.RestoreAsync(CancellationToken.None).ConfigureAwait(false);
                    _retainedLeases.RemoveAt(index);
                }
                catch (Exception exception)
                {
                    failures.Add(new InvalidOperationException(
                        $"Failed to restore retained Guardian canary lease '{lease.RestorePointId:D}'.",
                        exception));
                }
            }

            Volatile.Write(ref _retainedLeaseCount, _retainedLeases.Count);
            if (failures.Count != 0)
                throw new AggregateException(
                    "Generic Guardian session teardown could not restore every retained canary lease; the old session was not rebound or reset.",
                    failures);
        }

        _sessionOwner.Reset();
        Volatile.Write(ref _current, null);
    }

    private static bool IsEligibleState(GuardianWorkloadStateSnapshot? state)
        => state is
        {
            State: GuardianWorkloadState.Active,
            Confidence: GuardianWorkloadStateConfidence.High,
            Target: not null
        };

    private static bool MatchesIdentity(
        GenericGuardianCanarySessionKey session,
        TelemetryWorkloadTarget? target)
        => target is not null
           && target.BindingQuality == TelemetryWorkloadBindingQuality.ExactRunningProcess
           && target.CanCaptureProcess
           && session.ProcessId == target.ProcessId
           && SameIdentity(session.GameId, target.GameId)
           && SameIdentity(session.ExecutablePath, target.ExecutablePath);

    private static TelemetryWorkloadTarget TargetFrom(GenericGuardianCanarySessionKey session)
        => new()
        {
            GameId = session.GameId,
            ProcessId = session.ProcessId,
            ExecutablePath = session.ExecutablePath,
            BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess
        };

    private static bool SameIdentity(string? left, string? right)
        => !string.IsNullOrWhiteSpace(left)
           && !string.IsNullOrWhiteSpace(right)
           && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
}
