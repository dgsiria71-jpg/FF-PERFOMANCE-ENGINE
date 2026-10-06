using FFPerformanceEngine.Core.Telemetry;

internal static class WorkloadTelemetryFrameComposerSelfTests
{
    internal static void Run()
    {
        var timestamp = new DateTimeOffset(2026, 10, 6, 16, 30, 0, TimeSpan.Zero);
        var present = new TelemetryFrame(timestamp, [
            new TelemetryMetricObservation(
                TelemetryStandardMetrics.FrameFpsAverage,
                91,
                TelemetryMetricQuality.Measured,
                0.95,
                "presentmon",
                TelemetryMetricOrigin.Direct),
            new TelemetryMetricObservation(
                TelemetryStandardMetrics.FrameAcceptedSampleCount,
                120,
                TelemetryMetricQuality.Measured,
                1,
                "presentmon",
                TelemetryMetricOrigin.Direct)
        ]);
        var gpu = new TelemetryFrame(timestamp.AddMilliseconds(100), [
            new TelemetryMetricObservation(
                TelemetryStandardMetrics.SystemGpuUtilizationPercent,
                72,
                TelemetryMetricQuality.Measured,
                1,
                "windows-wddm-pdh",
                TelemetryMetricOrigin.Derived)
        ]);

        var merged = WorkloadTelemetryFrameComposer.Compose(present, gpu);

        Require(merged.Timestamp == present.Timestamp,
            "Workload composite must retain the primary PresentMon measurement timestamp.");
        Require(merged.TryGetMetric(TelemetryStandardMetrics.FrameFpsAverage.Id, out var fps)
                && fps is { Value: 91, SourceId: "presentmon" },
            "PresentMon frame metrics must remain unchanged.");
        Require(merged.TryGetMetric(TelemetryStandardMetrics.SystemGpuUtilizationPercent.Id, out var gpuMetric)
                && gpuMetric is { Value: 72, Quality: TelemetryMetricQuality.Measured, Coverage: 1 },
            "Measured WDDM GPU utilization must be added to the workload frame.");
        Require(merged.TryGetMetric(TelemetryStandardMetrics.FrameAcceptedSampleCount.Id, out var accepted)
                && accepted is { Value: 120 },
            "Direct render-activity proof must remain available after composition.");

        var duplicateSupplemental = new TelemetryFrame(timestamp, [
            new TelemetryMetricObservation(
                TelemetryStandardMetrics.FrameFpsAverage,
                1,
                TelemetryMetricQuality.Measured,
                1,
                "supplemental",
                TelemetryMetricOrigin.Derived)
        ]);
        var duplicateMerged = WorkloadTelemetryFrameComposer.Compose(present, duplicateSupplemental);
        Require(duplicateMerged.TryGetMetric(TelemetryStandardMetrics.FrameFpsAverage.Id, out var retained)
                && retained is { Value: 91, SourceId: "presentmon" },
            "Supplemental telemetry must never replace an already-measured primary workload metric.");

        Console.WriteLine("PASS Track 6 workload telemetry composition preserves PresentMon render evidence and adds measured WDDM GPU context");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
