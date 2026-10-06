using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;

internal static class GenericGuardianTypedCanaryOutcomeEvaluatorSelfTests
{
    internal static void Run()
    {
        var evaluator = new GenericGuardianTypedCanaryOutcomeEvaluator();

        Require(
            evaluator.Evaluate(
                Candidate(GuardianAnomalyKind.CpuContention),
                Frame(100, 10.0),
                Frame(102, 10.0)) == GenericGuardianSessionCanaryVerdict.Improved,
            "CPU canary must keep only when measured FPS improves by at least the existing 2% canary boundary and frame time does not worsen.");

        Require(
            evaluator.Evaluate(
                Candidate(GuardianAnomalyKind.GpuSaturation),
                Frame(100, 10.0),
                Frame(104, 9.5)) == GenericGuardianSessionCanaryVerdict.Improved,
            "GPU canary must use the same proven frame-performance outcome semantics as the specialized canary.");

        Require(
            evaluator.Evaluate(
                Candidate(GuardianAnomalyKind.CpuContention),
                Frame(100, 10.0),
                Frame(98, 9.0)) == GenericGuardianSessionCanaryVerdict.Regressive,
            "A measured FPS loss of at least 2% must be classified as regressive even when frame time does not worsen.");

        Require(
            evaluator.Evaluate(
                Candidate(GuardianAnomalyKind.GpuSaturation),
                Frame(100, 10.0),
                Frame(101.9, 9.0)) == GenericGuardianSessionCanaryVerdict.Inconclusive,
            "Sub-threshold measured FPS noise must remain inconclusive rather than being promoted to improvement.");

        Require(
            evaluator.Evaluate(
                Candidate(GuardianAnomalyKind.CpuContention),
                Frame(100, 10.0),
                Frame(103, 10.2)) == GenericGuardianSessionCanaryVerdict.Inconclusive,
            "FPS gain with worsened measured frame time must remain inconclusive rather than being kept.");

        Require(
            evaluator.Evaluate(
                Candidate(GuardianAnomalyKind.CpuContention),
                FrameWithoutFrameTime(100),
                FrameWithoutFrameTime(103)) == GenericGuardianSessionCanaryVerdict.Inconclusive,
            "Missing frame-time evidence must fail closed as inconclusive.");

        Require(
            evaluator.Evaluate(
                Candidate(GuardianAnomalyKind.CpuContention),
                Frame(100, 10.0, fpsQuality: TelemetryMetricQuality.Partial),
                Frame(103, 9.8)) == GenericGuardianSessionCanaryVerdict.Inconclusive,
            "Partial FPS evidence must not authorize a canary keep or regression verdict.");

        Require(
            evaluator.Evaluate(
                Candidate(GuardianAnomalyKind.GpuSaturation),
                Frame(100, 10.0, coverage: 0.74),
                Frame(103, 9.8)) == GenericGuardianSessionCanaryVerdict.Inconclusive,
            "Typed outcome evaluation must preserve the existing 75% causal-evidence completeness floor.");

        Require(
            evaluator.Evaluate(
                Candidate(GuardianAnomalyKind.CpuContention),
                Frame(0, 10.0),
                Frame(103, 9.8)) == GenericGuardianSessionCanaryVerdict.Inconclusive,
            "Non-positive FPS cannot establish a relative canary outcome.");

        Require(
            evaluator.Evaluate(
                Candidate(GuardianAnomalyKind.FrameTimeInstability),
                FramePacingFrame(100, 10.0, p99Ms: 18.0, stutterPercent: 8.0),
                FramePacingFrame(101, 9.9, p99Ms: 14.0, stutterPercent: 2.0))
            == GenericGuardianSessionCanaryVerdict.Improved,
            "Frame-pacing canary may keep only when the measured classifier instability clears (p99 < 1.55x average and stutter < 3%) without FPS/frame-time regression.");

        Require(
            evaluator.Evaluate(
                Candidate(GuardianAnomalyKind.FrameTimeInstability),
                FramePacingFrame(100, 10.0, p99Ms: 18.0, stutterPercent: 8.0),
                FramePacingFrame(101, 9.9, p99Ms: 17.0, stutterPercent: 4.0))
            == GenericGuardianSessionCanaryVerdict.Inconclusive,
            "Partial pacing improvement that remains above the classifier's instability boundary must not be kept.");

        Require(
            evaluator.Evaluate(
                Candidate(GuardianAnomalyKind.FrameTimeInstability),
                FramePacingFrame(100, 10.0, p99Ms: 18.0, stutterPercent: 8.0),
                FramePacingFrame(97.5, 9.5, p99Ms: 14.0, stutterPercent: 2.0))
            == GenericGuardianSessionCanaryVerdict.Regressive,
            "Cleared pacing evidence cannot justify KEEP when average FPS regresses by the existing 2% canary boundary.");

        var unsupportedFamilies = new[]
        {
            GuardianAnomalyKind.MemoryPressure,
            GuardianAnomalyKind.VramPressure,
            GuardianAnomalyKind.ThermalThrottling,
            GuardianAnomalyKind.NetworkInstability,
            GuardianAnomalyKind.Unknown,
            GuardianAnomalyKind.BackgroundLoad,
            GuardianAnomalyKind.RendererEngineStall,
            GuardianAnomalyKind.SchedulerImbalance,
            GuardianAnomalyKind.InputFrameLatencySpike
        };

        foreach (var family in unsupportedFamilies)
        {
            Require(
                evaluator.Evaluate(
                    Candidate(family),
                    Frame(100, 10.0),
                    Frame(110, 8.0)) == GenericGuardianSessionCanaryVerdict.Inconclusive,
                $"{family} must remain inconclusive until that family has an evidence-backed canary outcome contract.");
        }

        Console.WriteLine("PASS Track 6 typed Guardian canary outcome policy is conservative, family-bounded and evidence-complete");
    }

    private static GenericGuardianSessionActionCandidate Candidate(GuardianAnomalyKind family)
        => new()
        {
            GameId = "generic.game",
            Family = family,
            Action = new GuardianAction
            {
                Id = "test.live-safe",
                Description = "test",
                Safety = ActionSafety.LiveSafe,
                MinimumConfidence = 0.85
            }
        };

    private static TelemetryFrame Frame(
        double fps,
        double frameTimeMs,
        TelemetryMetricQuality fpsQuality = TelemetryMetricQuality.Measured,
        double coverage = 1.0)
        => new(
            new DateTimeOffset(2026, 9, 11, 9, 0, 0, TimeSpan.Zero),
            new[]
            {
                Metric(TelemetryStandardMetrics.FrameFpsAverage, fps, fpsQuality, coverage),
                Metric(TelemetryStandardMetrics.FrameTimeAverageMs, frameTimeMs, TelemetryMetricQuality.Measured, coverage)
            });

    private static TelemetryFrame FramePacingFrame(
        double fps,
        double frameTimeMs,
        double p99Ms,
        double stutterPercent,
        double coverage = 1.0)
        => new(
            new DateTimeOffset(2026, 9, 11, 9, 0, 0, TimeSpan.Zero),
            new[]
            {
                Metric(TelemetryStandardMetrics.FrameFpsAverage, fps, TelemetryMetricQuality.Measured, coverage),
                Metric(TelemetryStandardMetrics.FrameTimeAverageMs, frameTimeMs, TelemetryMetricQuality.Measured, coverage),
                Metric(TelemetryStandardMetrics.FrameTimeP99Ms, p99Ms, TelemetryMetricQuality.Measured, coverage),
                Metric(TelemetryStandardMetrics.FrameStutterPercent, stutterPercent, TelemetryMetricQuality.Measured, coverage)
            });

    private static TelemetryFrame FrameWithoutFrameTime(double fps)
        => new(
            new DateTimeOffset(2026, 9, 11, 9, 0, 0, TimeSpan.Zero),
            new[]
            {
                Metric(TelemetryStandardMetrics.FrameFpsAverage, fps, TelemetryMetricQuality.Measured, 1.0)
            });

    private static TelemetryMetricObservation Metric(
        TelemetryMetricDescriptor descriptor,
        double value,
        TelemetryMetricQuality quality,
        double coverage)
        => new(
            descriptor,
            value,
            quality,
            coverage,
            "guardian-canary-outcome-selftest",
            TelemetryMetricOrigin.Direct);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
