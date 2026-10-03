using FFPerformanceEngine.Core.Telemetry;

namespace FFPerformanceEngine.Core.Services;

/// <summary>
/// Capability-honest typed outcome policy for generic Guardian session canaries.
/// Only CPU/GPU families currently have a proven frame-performance outcome
/// contract. Every other family remains inconclusive until dedicated typed
/// before/after semantics exist for that causal family.
/// </summary>
public sealed class GenericGuardianTypedCanaryOutcomeEvaluator : IGenericGuardianSessionCanaryOutcomeEvaluator
{
    private const double MinimumRelativeFpsChange = 0.02;
    private const double MinimumCausalCoverage = 0.75;

    public GenericGuardianSessionCanaryVerdict Evaluate(
        GenericGuardianSessionActionCandidate candidate,
        TelemetryFrame before,
        TelemetryFrame after)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        if (candidate.Family is not (GuardianAnomalyKind.CpuContention or GuardianAnomalyKind.GpuSaturation))
            return GenericGuardianSessionCanaryVerdict.Inconclusive;

        if (!TryGetMeasuredMetric(before, TelemetryStandardMetrics.FrameFpsAverage, out var beforeFps)
            || !TryGetMeasuredMetric(after, TelemetryStandardMetrics.FrameFpsAverage, out var afterFps)
            || !TryGetMeasuredMetric(before, TelemetryStandardMetrics.FrameTimeAverageMs, out var beforeFrameTime)
            || !TryGetMeasuredMetric(after, TelemetryStandardMetrics.FrameTimeAverageMs, out var afterFrameTime)
            || beforeFps <= 0
            || afterFps <= 0
            || beforeFrameTime <= 0
            || afterFrameTime <= 0)
        {
            return GenericGuardianSessionCanaryVerdict.Inconclusive;
        }

        var relativeFpsChange = (afterFps - beforeFps) / beforeFps;
        if (relativeFpsChange <= -MinimumRelativeFpsChange)
            return GenericGuardianSessionCanaryVerdict.Regressive;

        if (relativeFpsChange >= MinimumRelativeFpsChange
            && afterFrameTime <= beforeFrameTime)
        {
            return GenericGuardianSessionCanaryVerdict.Improved;
        }

        return GenericGuardianSessionCanaryVerdict.Inconclusive;
    }

    private static bool TryGetMeasuredMetric(
        TelemetryFrame frame,
        TelemetryMetricDescriptor descriptor,
        out double value)
    {
        value = default;
        if (!frame.TryGetMetric(descriptor.Id, out var observation)
            || observation is null
            || observation.Quality != TelemetryMetricQuality.Measured
            || observation.Coverage < MinimumCausalCoverage
            || !double.IsFinite(observation.Value))
        {
            return false;
        }

        value = observation.Value;
        return true;
    }
}
