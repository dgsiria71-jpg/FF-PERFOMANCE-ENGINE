using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;

internal static class PresentMonTelemetryV2SelfTests
{
    private static string _stage = "not-started";

    [ModuleInitializer]
    internal static void Run()
    {
        try
        {
            RunCore();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"PresentMon telemetry v2 self-test failed at stage '{_stage}'.",
                ex);
        }
    }

    private static void RunCore()
    {
        const string csv = "MsBetweenPresents,DisplayLatency\n"
                           + "10,5\n"
                           + "20,7\n"
                           + "30,bad\n"
                           + "bad,9\n"
                           + "2000,11\n";

        var service = new PresentMonService();

        _stage = "legacy-v2-shared-statistics";
        var legacy = service.ParseCsv(csv)
                     ?? throw new InvalidOperationException("Legacy PresentMon parser unexpectedly returned null.");
        var frame = service.ParseCsvFrame(csv)
                    ?? throw new InvalidOperationException("V2 PresentMon parser unexpectedly returned null.");

        Require(legacy.DataQuality == "PresentMon · 3 frames",
            "Legacy PresentMon label must preserve the accepted-frame count used by existing A/B quality logic.");

        RequireMetric(frame, TelemetryStandardMetrics.FrameFpsAverage, legacy.Fps, 0.6);
        RequireMetric(frame, TelemetryStandardMetrics.FrameFpsLow1, legacy.OnePercentLow, 0.6);
        RequireMetric(frame, TelemetryStandardMetrics.FrameFpsLow01, legacy.PointOnePercentLow, 0.6);
        RequireMetric(frame, TelemetryStandardMetrics.FrameTimeAverageMs, legacy.FrameTimeMs, 0.6);
        RequireMetric(frame, TelemetryStandardMetrics.FrameTimeP95Ms, legacy.FrameTimeP95Ms, 0.6);
        RequireMetric(frame, TelemetryStandardMetrics.FrameTimeP99Ms, legacy.FrameTimeP99Ms, 0.6);
        RequireMetric(frame, TelemetryStandardMetrics.FrameStutterPercent, legacy.StutterPercent, 0.6);
        RequireMetric(frame, TelemetryStandardMetrics.FrameLatencyAverageMs, legacy.LatencyMs, 0.8);

        Require(frame.Metrics.Count == 8,
            "PresentMon v2 output must contain only the eight frame/latency metrics proven by this CSV.");
        Require(frame.FrameQuality == TelemetryMetricQuality.Measured,
            "Direct PresentMon observations remain Measured while coverage carries incomplete-row information.");

        _stage = "expected-statistics-preserved";
        RequireClose(legacy.Fps, (100d + 50d + (1000d / 30d)) / 3d, "average FPS");
        RequireClose(legacy.OnePercentLow, 1000d / 30d, "1% low");
        RequireClose(legacy.PointOnePercentLow, 1000d / 30d, "0.1% low");
        RequireClose(legacy.FrameTimeMs, 20d, "average frame time");
        RequireClose(legacy.FrameTimeP95Ms, 29d, "P95 frame time");
        RequireClose(legacy.FrameTimeP99Ms, 29.8d, "P99 frame time");
        RequireClose(legacy.StutterPercent, 100d / 3d, "stutter percent");
        RequireClose(legacy.LatencyMs, 8d, "average display latency");

        _stage = "no-fabricated-non-frame-channels";
        Require(!frame.TryGetMetric(TelemetryStandardMetrics.SystemCpuUtilizationPercent.Id, out _)
                && !frame.TryGetMetric(TelemetryStandardMetrics.SystemGpuUtilizationPercent.Id, out _)
                && !frame.TryGetMetric(TelemetryStandardMetrics.NetworkPingMs.Id, out _),
            "PresentMon v2 must not fabricate system/GPU/network metrics outside its accepted CSV channels.");

        _stage = "latency-optional";
        const string noLatencyCsv = "MsBetweenPresents\n10\n20\n";
        var noLatencyFrame = service.ParseCsvFrame(noLatencyCsv)
                             ?? throw new InvalidOperationException("Frame-only PresentMon CSV unexpectedly returned null.");
        Require(noLatencyFrame.Metrics.Count == 7,
            "Frame-only PresentMon CSV must emit seven frame metrics and omit latency.");
        Require(!noLatencyFrame.TryGetMetric(TelemetryStandardMetrics.FrameLatencyAverageMs.Id, out _),
            "Absent latency observations must remain absent rather than becoming zero.");

        _stage = "invalid-inputs";
        Require(service.ParseCsvFrame("Application,DisplayLatency\nfoo,5\nbar,6\n") is null,
            "CSV without a supported frame-interval column must return null.");
        Require(service.ParseCsvFrame("MsBetweenPresents,DisplayLatency\n10,5\nbad,6\n") is null,
            "CSV with fewer than two accepted frame rows must return null.");
        Require(service.ParseCsvFrame(string.Empty) is null,
            "Empty CSV must return null.");

        _stage = "capture-api-contract";
        Func<int, TimeSpan, CancellationToken, Task<TelemetryFrame?>> capture = service.CaptureProcessFrameAsync;
        Require(capture is not null,
            "PresentMon direct v2 process-capture API must exist without removing the legacy CaptureProcessAsync API.");
        Func<int, TimeSpan, CancellationToken, Task<FFPerformanceEngine.Core.Models.TelemetrySample?>> legacyCapture
            = service.CaptureProcessAsync;
        Require(legacyCapture is not null,
            "Legacy PresentMon CaptureProcessAsync API must remain callable.");

        _stage = "complete";
        Console.WriteLine("PASS Track 4 PresentMon parser exposes direct measured v2 telemetry with explicit accepted-row coverage");
    }

    private static void RequireMetric(
        TelemetryFrame frame,
        TelemetryMetricDescriptor descriptor,
        double? expectedValue,
        double expectedCoverage)
    {
        Require(expectedValue is double, $"Legacy expected value for '{descriptor.Id}' must be present.");
        Require(frame.TryGetMetric(descriptor.Id, out var observation) && observation is not null,
            $"Expected PresentMon v2 metric '{descriptor.Id}' is missing.");
        RequireClose(observation!.Value, expectedValue!.Value, descriptor.Id);
        RequireClose(observation.Coverage, expectedCoverage, $"{descriptor.Id} coverage");
        Require(observation.Quality == TelemetryMetricQuality.Measured,
            $"Metric '{descriptor.Id}' must be Measured direct evidence.");
        Require(observation.SourceId == "presentmon",
            $"Metric '{descriptor.Id}' must carry presentmon provenance.");
        Require(observation.Origin == TelemetryMetricOrigin.Direct,
            $"Metric '{descriptor.Id}' must carry Direct origin.");
    }

    private static void RequireClose(double? actual, double expected, string label)
    {
        Require(actual is double value && Math.Abs(value - expected) <= 0.000001,
            $"{label} mismatch. Expected {expected}, got {actual?.ToString() ?? "null"}.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
