using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;

internal static class PerformancePresentationSelfTests
{
    public static void Run()
    {
        var unavailable = PerformancePresentation.FromCapture(new PerformanceCaptureResult
        {
            Target = new PerformanceCaptureTarget(),
            Message = "Waiting for exact binding"
        });
        Require(!unavailable.HasMeasurement && unavailable.Fps == "—" && unavailable.OnePercentLow == "—" && unavailable.FrameTime == "—" && unavailable.Latency == "—",
            "Unavailable Performance evidence must remain unavailable in the UI presentation.");
        Require(unavailable.ProcessId == "—" && unavailable.Instance == "—",
            "Performance presentation must not invent process or instance identity without an exact binding.");

        var measured = PerformancePresentation.FromCapture(new PerformanceCaptureResult
        {
            Target = new PerformanceCaptureTarget { ProcessId = 4321, InstanceName = "Pie64" },
            Sample = new TelemetrySample
            {
                Fps = 144.5,
                OnePercentLow = 132.2,
                PointOnePercentLow = 119.8,
                FrameTimeMs = 6.92,
                FrameTimeP95Ms = 8.25,
                FrameTimeP99Ms = 10.4,
                StutterPercent = 0.6,
                LatencyMs = 13.8,
                DataQuality = "PresentMon · 2400 frames"
            },
            Message = "Measured exact PID"
        });

        Require(measured.HasMeasurement && measured.ProcessId == "4321" && measured.Instance == "Pie64",
            "Measured Performance presentation must retain the exact capture target identity.");
        Require(measured.Fps.Contains("144.5") && measured.OnePercentLow.Contains("132.2") && measured.PointOnePercentLow.Contains("119.8"),
            "Measured FPS evidence must be presented from the real capture sample.");
        Require(measured.FrameTime.Contains("6.92") && measured.P95FrameTime.Contains("8.25") && measured.P99FrameTime.Contains("10.40") && measured.Latency.Contains("13.8"),
            "Measured frame-time and latency evidence must be presented without substitution.");
        Require(measured.DataQuality.Contains("PresentMon") && measured.Detail == "Measured exact PID",
            "Performance presentation must expose data quality and the coordinator result detail.");

        var workloadFrame = new TelemetryFrame(
            new DateTimeOffset(2026, 9, 9, 17, 25, 0, TimeSpan.Zero),
        [
            Metric(TelemetryStandardMetrics.FrameFpsAverage, 240.0, 1),
            Metric(TelemetryStandardMetrics.FrameFpsLow1, 220.0, 1),
            Metric(TelemetryStandardMetrics.FrameFpsLow01, 205.0, 1),
            Metric(TelemetryStandardMetrics.FrameTimeAverageMs, 4.17, 1),
            Metric(TelemetryStandardMetrics.FrameTimeP95Ms, 5.10, 1),
            Metric(TelemetryStandardMetrics.FrameTimeP99Ms, 6.20, 1),
            Metric(TelemetryStandardMetrics.FrameStutterPercent, 0.25, 1),
            Metric(TelemetryStandardMetrics.FrameLatencyAverageMs, 6.5, 1)
        ]);
        var workload = PerformancePresentation.FromCapture(new PerformanceWorkloadTypedCaptureResult
        {
            Target = new TelemetryWorkloadTarget
            {
                GameId = "steam:730",
                ProcessId = 7730,
                ExecutablePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DG-Universal-Capture", "cs2.exe")),
                BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess
            },
            Frame = workloadFrame,
            Message = "Measured typed telemetry for workload steam:730, PID 7730."
        });
        Require(workload.HasMeasurement
                && workload.Instance == "—"
                && workload.ProcessId == "7730"
                && workload.Fps == "240.0 FPS"
                && workload.OnePercentLow == "220.0 FPS"
                && workload.FrameTime == "4.17 ms"
                && workload.P99FrameTime == "6.20 ms"
                && workload.Latency == "6.5 ms"
                && workload.DataQuality == "Measured",
            "Universal Performance presentation must reuse typed frame formatting while keeping BlueStacks instance identity absent.");
        Require(workload.Detail.Contains("steam:730", StringComparison.Ordinal)
                && workload.Detail.Contains("7730", StringComparison.Ordinal),
            "Universal Performance presentation must disclose the stable GameId and exact PID only through proven target detail.");

        Console.WriteLine("PASS Performance evidence-only presentation contract");
    }

    private static TelemetryMetricObservation Metric(
        TelemetryMetricDescriptor descriptor,
        double value,
        double coverage)
        => new(
            descriptor,
            value,
            TelemetryMetricQuality.Measured,
            coverage,
            "presentmon",
            TelemetryMetricOrigin.Direct);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
