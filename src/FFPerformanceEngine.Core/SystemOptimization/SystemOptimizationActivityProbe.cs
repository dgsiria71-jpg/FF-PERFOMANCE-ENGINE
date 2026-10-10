namespace FFPerformanceEngine.Core.SystemOptimization;

public enum SystemOptimizationActivityState
{
    Idle,
    Active
}

/// <summary>
/// Process-wide read-only observation of DG System Optimization operations.
/// Generation advances when an operation enters and leaves the active set.
/// This observes only transactions executed by this process; it does not prove
/// absence of registry edits, vendor tools, drivers or other external changes.
/// </summary>
public sealed record SystemOptimizationActivitySnapshot(
    long Generation,
    int ActiveOperations)
{
    public SystemOptimizationActivityState State
        => ActiveOperations == 0
            ? SystemOptimizationActivityState.Idle
            : SystemOptimizationActivityState.Active;

    public static bool ProvesUninterruptedIdle(
        SystemOptimizationActivitySnapshot before,
        SystemOptimizationActivitySnapshot after)
        => before is not null
           && after is not null
           && before.State == SystemOptimizationActivityState.Idle
           && after.State == SystemOptimizationActivityState.Idle
           && before.Generation == after.Generation;
}

public interface ISystemOptimizationActivityProbe
{
    SystemOptimizationActivitySnapshot SnapshotActivity();
}
