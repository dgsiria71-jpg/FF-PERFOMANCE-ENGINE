using FFPerformanceEngine.Core.Telemetry;

namespace FFPerformanceEngine.Core.Services;

/// <summary>
/// Capability-honest typed outcome policy for generic Guardian session canaries.
/// CPU/GPU use the proven average frame-performance contract. Frame-time
/// instability uses the same measured pacing thresholds as the classifier and
/// keeps only when that instability clears without average-performance regression.
/// Every other family remains inconclusive until dedicated typed semantics exist.
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

        if (candidate.Family == GuardianAnomalyKind.FrameTimeInstability)
            return EvaluateFramePacing(before, after);

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

    private static GenericGuardianSessionCanaryVerdict EvaluateFramePacing(
        TelemetryFrame before,
        TelemetryFrame after)
    {
        if (!TryGetMeasuredMetric(before, TelemetryStandardMetrics.FrameFpsAverage, out var beforeFps)
            || !TryGetMeasuredMetric(after, TelemetryStandardMetrics.FrameFpsAverage, out var afterFps)
            || !TryGetMeasuredMetric(before, TelemetryStandardMetrics.FrameTimeAverageMs, out var beforeFrameTime)
            || !TryGetMeasuredMetric(after, TelemetryStandardMetrics.FrameTimeAverageMs, out var afterFrameTime)
            || !TryGetMeasuredMetric(before, TelemetryStandardMetrics.FrameTimeP99Ms, out var beforeP99)
            || !TryGetMeasuredMetric(after, TelemetryStandardMetrics.FrameTimeP99Ms, out var afterP99)
            || !TryGetMeasuredMetric(before, TelemetryStandardMetrics.FrameStutterPercent, out var beforeStutter)
            || !TryGetMeasuredMetric(after, TelemetryStandardMetrics.FrameStutterPercent, out var afterStutter)
            || beforeFps <= 0
            || afterFps <= 0
            || beforeFrameTime <= 0
            || afterFrameTime <= 0
            || beforeP99 <= 0
            || afterP99 <= 0
            || beforeStutter < 0
            || afterStutter < 0)
        {
            return GenericGuardianSessionCanaryVerdict.Inconclusive;
        }

        var relativeFpsChange = (afterFps - beforeFps) / beforeFps;
        if (relativeFpsChange <= -MinimumRelativeFpsChange)
            return GenericGuardianSessionCanaryVerdict.Regressive;

        var beforeUnstable =
            beforeStutter >= 3d
            || beforeP99 >= beforeFrameTime * 1.55d;
        if (!beforeUnstable)
            return GenericGuardianSessionCanaryVerdict.Inconclusive;

        var afterStable =
            afterStutter < 3d
            && afterP99 < afterFrameTime * 1.55d;
        if (afterStable && afterFrameTime <= beforeFrameTime)
            return GenericGuardianSessionCanaryVerdict.Improved;

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
