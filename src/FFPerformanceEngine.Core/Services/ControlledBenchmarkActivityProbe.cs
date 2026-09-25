namespace FFPerformanceEngine.Core.Services;

public enum ControlledBenchmarkActivityState
{
    Unknown,
    Idle,
    Active
}

/// <summary>
/// Snapshot of the actual process-wide Track 0 controlled benchmark lease gate.
/// Generation increments on every successful gate acquisition and release,
/// including failed Guardian suspension/reconciliation. It is not persistent
/// and does not attest to external tools, scene identity or other mutations.
/// </summary>
public readonly record struct ControlledBenchmarkActivitySnapshot(
    long Generation,
    ControlledBenchmarkActivityState State)
{
    /// <summary>
    /// A completed benchmark between two idle reads changes Generation; it
    /// must never masquerade as an uninterrupted idle interval. Callers must
    /// bracket the entire physical measurement interval with these snapshots.
    /// This proves only no Track 0 lease transition within the two snapshots,
    /// not scene/load similarity or absence of external interference.
    /// </summary>
    public static bool ProvesUninterruptedIdle(
        ControlledBenchmarkActivitySnapshot before,
        ControlledBenchmarkActivitySnapshot after)
        => before.State == ControlledBenchmarkActivityState.Idle
           && after.State == ControlledBenchmarkActivityState.Idle
           && before.Generation == after.Generation;
}

/// <summary>
/// Read-only authority-owned process-local view; this is deliberately separate
/// from IControlledBenchmarkLeaseManager so existing lease consumers are not
/// given new mutation authority or required to implement new members.
/// </summary>
public interface IControlledBenchmarkActivityProbe
{
    ControlledBenchmarkActivitySnapshot SnapshotActivity();
}
