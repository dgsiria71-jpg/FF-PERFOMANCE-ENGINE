using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;

internal static class PresentMonTypedCaptureMetadataSelfTests
{
    internal static void Run()
    {
        const string csv = "MsBetweenPresents,DisplayLatency\n"
                           + "10,5\n"
                           + "20,7\n"
                           + "30,bad\n"
                           + "bad,9\n"
                           + "2000,11\n";

        var frame = new PresentMonService().ParseCsvFrame(csv)
                    ?? throw new InvalidOperationException("PresentMon typed metadata test requires a valid frame.");

        Require(frame.TryGetMetric("frame.samples.accepted.count", out var accepted) && accepted is not null,
            "PresentMon typed telemetry must expose the exact accepted frame-row count as a typed metric instead of hiding it in legacy DataQuality text.");
        Require(accepted!.Value == 3,
            "Accepted-frame count must come from the same parser statistics used by legacy PresentMon evidence.");
        Require(accepted.Metric.Unit == TelemetryUnit.Count
                && accepted.Metric.Domain == TelemetryMetricDomain.Frame,
            "Accepted-frame count must be a typed frame/count metric.");
        Require(accepted.Quality == TelemetryMetricQuality.Measured
                && accepted.Coverage == 1
                && accepted.SourceId == "presentmon"
                && accepted.Origin == TelemetryMetricOrigin.Direct,
            "The count itself is directly measured metadata and must not be confused with frame-row coverage.");

        Require(frame.TryGetMetric(TelemetryStandardMetrics.FrameFpsAverage.Id, out var fps) && fps is not null,
            "PresentMon typed metadata test requires FPS evidence.");
        Require(Math.Abs(fps!.Coverage - 0.6) < 0.000001,
            "Frame metric coverage must remain accepted rows divided by data rows; it is not a substitute for absolute sample count.");

        Console.WriteLine("PASS Track 4 PresentMon typed telemetry exposes exact accepted-frame count separately from coverage");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
