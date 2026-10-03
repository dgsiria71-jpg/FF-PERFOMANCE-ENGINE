using FFPerformanceEngine.Core.Telemetry;

namespace FFPerformanceEngine.Core.Services;

/// <summary>
/// Owns a single physical Windows process session and its generated epoch.
/// The caller cannot supply a PID-only epoch: only the concrete OS probe can
/// originate one, and the same PID/path with a different creation time rotates
/// it. This does NOT prove scene/load comparability or restore retained leases;
/// the eventual host must restore them before resetting/rebinding ownership.
/// </summary>
public sealed class GenericGuardianWindowsSessionLifecycleCoordinator
{
    private readonly object _gate = new();
    private readonly GenericGuardianWindowsProcessLifetimeProbe _probe = new();
    private GenericGuardianWindowsProcessLifetimeSnapshot? _lifetime;
    private GenericGuardianCanarySessionKey? _current;

    /// <summary>
    /// Observe an already-resolved, Active/High exact running workload.
    /// Invalid states and missing OS proof retire the previous epoch.
    /// The source GameId must already have been resolved by Track 3; this
    /// coordinator never discovers games or invents stable identities.
    /// </summary>
    public GenericGuardianCanarySessionKey? Observe(GuardianWorkloadStateSnapshot? state)
    {
        lock (_gate)
        {
            var target = state?.Target;
            if (state?.State != GuardianWorkloadState.Active
                || state.Confidence != GuardianWorkloadStateConfidence.High
                || !ValidTarget(target))
            {
                Clear();
                return null;
            }

            var lifetime = _probe.Observe(target!.ProcessId!.Value, target.ExecutablePath);
            if (lifetime is null)
            {
                Clear();
                return null;
            }

            if (_current is not null && _lifetime is not null
                && Matches(_current, target)
                && SameLifetime(_lifetime, lifetime))
                return _current;

            // A new physical creation time, PID, path or GameId starts a NEW
            // epoch. Never reuse a caller-provided Guid across OS lifetimes.
            _lifetime = lifetime;
            _current = new GenericGuardianCanarySessionKey(
                Guid.NewGuid(), target.GameId!.Trim(), lifetime.ProcessId, lifetime.ExecutablePath);
            return _current;
        }
    }

    /// <summary>
    /// Accept only the identical object issued by this owner for the exact
    /// target and reverify PID/path/creation time with Windows. A cloned
    /// record, stale epoch or disappeared/reused PID cannot pass.
    /// </summary>
    public bool IsCurrent(GenericGuardianCanarySessionKey? key, TelemetryWorkloadTarget? target)
    {
        lock (_gate)
        {
            if (_current is null || _lifetime is null
                || !ReferenceEquals(key, _current) || !Matches(_current, target))
                return false;

            if (_probe.IsStillSameProcess(_lifetime, _current.ProcessId, _current.ExecutablePath))
                return true;

            Clear();
            return false;
        }
    }

    /// <summary>
    /// Retires the epoch; the host is separately responsible for restoring
    /// any outstanding Windows mutation lease BEFORE calling this method.
    /// </summary>
    public void Reset()
    {
        lock (_gate) Clear();
    }

    private void Clear()
    {
        _current = null;
        _lifetime = null;
    }

    private static bool ValidTarget(TelemetryWorkloadTarget? target)
        => target is not null
           && target.CanCaptureProcess
           && target.BindingQuality == TelemetryWorkloadBindingQuality.ExactRunningProcess
           && !string.IsNullOrWhiteSpace(target.GameId)
           && !string.IsNullOrWhiteSpace(target.ExecutablePath);

    private static bool Matches(GenericGuardianCanarySessionKey key, TelemetryWorkloadTarget? target)
        => ValidTarget(target)
           && key.ProcessId == target!.ProcessId
           && SameIdentity(key.GameId, target.GameId)
           && SameIdentity(key.ExecutablePath, target.ExecutablePath);

    private static bool SameLifetime(
        GenericGuardianWindowsProcessLifetimeSnapshot left,
        GenericGuardianWindowsProcessLifetimeSnapshot right)
        => left.ProcessId == right.ProcessId
           && left.CreatedAtUtc == right.CreatedAtUtc
           && SameIdentity(left.ExecutablePath, right.ExecutablePath);

    private static bool SameIdentity(string? left, string? right)
        => !string.IsNullOrWhiteSpace(left)
           && !string.IsNullOrWhiteSpace(right)
           && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
}
