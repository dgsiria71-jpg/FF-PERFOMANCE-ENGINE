using FFPerformanceEngine.Core.Telemetry;

namespace FFPerformanceEngine.Core.Services;

/// <summary>
/// InScopeOnSuppliedEvidence is only a structural consistency finding. It is
/// NOT a causal performance verdict and does NOT authenticate the evidence
/// source. The future host must supply independent adapter scene proof,
/// trustworthy capture/mutation timestamps and controlled-benchmark state.
/// </summary>
public enum GenericGuardianCanaryComparability
{
    Inconclusive,
    InScopeOnSuppliedEvidence
}

/// <summary>
/// Snapshot of one measured interval and independently supplied scene/load
/// context. No default context or inferred scene identity exists. All fields
/// must originate from proven owning sources before enabling a generic host.
/// </summary>
public sealed record GenericGuardianCanaryComparisonWindow(
    Guid SessionEpoch,
    TelemetryWorkloadTarget Target,
    string SourceId,
    string ModeId,
    string SceneId,
    string LoadFingerprint,
    string EnvironmentFingerprint,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    TelemetryFrame Frame,
    bool? ControlledBenchmarkActive,
    bool? WorkloadDriftDetected,
    bool? OtherMutationDetected);

/// <summary>
/// Read-only comparison gate. Unknown scene, load, external mutation or
/// benchmark activity always fails closed. This does not provide the missing
/// physical observers or authorize executor KEEP, profiles, or learning.
/// </summary>
public sealed class GenericGuardianCanaryComparabilityPolicy
{
    public GenericGuardianCanaryComparability Evaluate(
        GenericGuardianCanaryComparisonWindow? before,
        GenericGuardianCanaryComparisonWindow? after,
        DateTimeOffset mutationStartedAt,
        DateTimeOffset mutationCompletedAt)
    {
        if (before is null || after is null
            || !IsValidWindow(before) || !IsValidWindow(after)
            || mutationStartedAt == DateTimeOffset.MinValue
            || mutationCompletedAt == DateTimeOffset.MinValue
            || mutationStartedAt >= mutationCompletedAt
            || before.EndedAt > mutationStartedAt
            || mutationCompletedAt > after.StartedAt
            || before.SessionEpoch != after.SessionEpoch
            || !SameTarget(before.Target, after.Target)
            || !SameContext(before, after))
            return GenericGuardianCanaryComparability.Inconclusive;

        return GenericGuardianCanaryComparability.InScopeOnSuppliedEvidence;
    }

    private static bool IsValidWindow(GenericGuardianCanaryComparisonWindow window)
    {
        if (window.SessionEpoch == Guid.Empty
            || window.Target is null
            || !window.Target.CanCaptureProcess
            || window.Target.BindingQuality != TelemetryWorkloadBindingQuality.ExactRunningProcess
            || string.IsNullOrWhiteSpace(window.Target.GameId)
            || string.IsNullOrWhiteSpace(window.Target.ExecutablePath)
            || window.Frame is null
            || window.Frame.FrameQuality == TelemetryMetricQuality.Unavailable
            || window.StartedAt == DateTimeOffset.MinValue
            || window.EndedAt == DateTimeOffset.MinValue
            || window.StartedAt >= window.EndedAt
            || window.Frame.Timestamp < window.StartedAt
            || window.Frame.Timestamp > window.EndedAt
            || window.ControlledBenchmarkActive is not false
            || window.WorkloadDriftDetected is not false
            || window.OtherMutationDetected is not false)
            return false;

        return !string.IsNullOrWhiteSpace(window.SourceId)
               && !string.IsNullOrWhiteSpace(window.ModeId)
               && !string.IsNullOrWhiteSpace(window.SceneId)
               && !string.IsNullOrWhiteSpace(window.LoadFingerprint)
               && !string.IsNullOrWhiteSpace(window.EnvironmentFingerprint);
    }

    private static bool SameTarget(TelemetryWorkloadTarget before, TelemetryWorkloadTarget after)
        => before.ProcessId == after.ProcessId
           && SameIdentity(before.GameId, after.GameId)
           && SameIdentity(before.ExecutablePath, after.ExecutablePath);

    private static bool SameIdentity(string? left, string? right)
        => !string.IsNullOrWhiteSpace(left)
           && !string.IsNullOrWhiteSpace(right)
           && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool SameContext(
        GenericGuardianCanaryComparisonWindow before,
        GenericGuardianCanaryComparisonWindow after)
        => string.Equals(before.SourceId, after.SourceId, StringComparison.Ordinal)
           && string.Equals(before.ModeId, after.ModeId, StringComparison.Ordinal)
           && string.Equals(before.SceneId, after.SceneId, StringComparison.Ordinal)
           && string.Equals(before.LoadFingerprint, after.LoadFingerprint, StringComparison.Ordinal)
           && string.Equals(before.EnvironmentFingerprint, after.EnvironmentFingerprint, StringComparison.Ordinal);
}
