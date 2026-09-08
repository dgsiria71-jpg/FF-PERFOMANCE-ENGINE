using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Telemetry;

internal static class TelemetryLegacyBridgeSelfTests
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
                $"Telemetry legacy bridge self-test failed at stage '{_stage}'.",
                ex);
        }
    }

    private static void RunCore()
    {
        var timestamp = new DateTimeOffset(2026, 9, 8, 20, 30, 0, TimeSpan.Zero);

        _stage = "presentmon-policy";
        var presentMon = TelemetryLegacyBridge.FromLegacy(new TelemetrySample
        {
            Timestamp = timestamp,
            Fps = 144,
            FrameTimeMs = 6.94,
            LatencyMs = 7.2,
            CpuPercent = 52,
            DataQuality = "PresentMon · 900 frames"
        });
        Require(presentMon.Timestamp == timestamp, "Legacy timestamp must be preserved exactly.");
        RequireMetric(presentMon, TelemetryStandardMetrics.FrameFpsAverage,
            TelemetryMetricQuality.Measured, "presentmon", TelemetryMetricOrigin.Direct);
        RequireMetric(presentMon, TelemetryStandardMetrics.FrameTimeAverageMs,
            TelemetryMetricQuality.Measured, "presentmon", TelemetryMetricOrigin.Direct);
        RequireMetric(presentMon, TelemetryStandardMetrics.FrameLatencyAverageMs,
            TelemetryMetricQuality.Measured, "presentmon", TelemetryMetricOrigin.Direct);
        RequireMetric(presentMon, TelemetryStandardMetrics.SystemCpuUtilizationPercent,
            TelemetryMetricQuality.Partial, "legacy-bridge", TelemetryMetricOrigin.Legacy);

        _stage = "system-policy";
        var system = TelemetryLegacyBridge.FromLegacy(new TelemetrySample
        {
            Timestamp = timestamp,
            CpuPercent = 33,
            MemoryUsedGb = 8,
            MemoryTotalGb = 16,
            GpuPercent = 71,
            CpuTemperatureC = 62,
            DataQuality = "System"
        });
        RequireMetric(system, TelemetryStandardMetrics.SystemCpuUtilizationPercent,
            TelemetryMetricQuality.Measured, "native-system", TelemetryMetricOrigin.Direct);
        RequireMetric(system, TelemetryStandardMetrics.SystemMemoryUsedGb,
            TelemetryMetricQuality.Measured, "native-system", TelemetryMetricOrigin.Direct);
        RequireMetric(system, TelemetryStandardMetrics.SystemMemoryTotalGb,
            TelemetryMetricQuality.Measured, "native-system", TelemetryMetricOrigin.Direct);
        RequireMetric(system, TelemetryStandardMetrics.SystemGpuUtilizationPercent,
            TelemetryMetricQuality.Partial, "legacy-bridge", TelemetryMetricOrigin.Legacy);
        RequireMetric(system, TelemetryStandardMetrics.CpuTemperatureCelsius,
            TelemetryMetricQuality.Partial, "legacy-bridge", TelemetryMetricOrigin.Legacy);

        _stage = "frame-system-policy";
        var frameSystem = TelemetryLegacyBridge.FromLegacy(new TelemetrySample
        {
            Timestamp = timestamp,
            Fps = 120,
            CpuPercent = 41,
            MemoryUsedGb = 10,
            DataQuality = "Frame+System"
        });
        RequireMetric(frameSystem, TelemetryStandardMetrics.SystemCpuUtilizationPercent,
            TelemetryMetricQuality.Measured, "native-system", TelemetryMetricOrigin.Direct);
        RequireMetric(frameSystem, TelemetryStandardMetrics.SystemMemoryUsedGb,
            TelemetryMetricQuality.Measured, "native-system", TelemetryMetricOrigin.Direct);
        RequireMetric(frameSystem, TelemetryStandardMetrics.FrameFpsAverage,
            TelemetryMetricQuality.Partial, "legacy-bridge", TelemetryMetricOrigin.Legacy);

        _stage = "legacy-measured-policy";
        var legacyMeasured = TelemetryLegacyBridge.FromLegacy(new TelemetrySample
        {
            Timestamp = timestamp,
            Fps = 165,
            FrameTimeP99Ms = 9.5,
            CpuPercent = 49,
            DataQuality = "Measured"
        });
        RequireMetric(legacyMeasured, TelemetryStandardMetrics.FrameFpsAverage,
            TelemetryMetricQuality.Measured, "legacy-measured", TelemetryMetricOrigin.Legacy);
        RequireMetric(legacyMeasured, TelemetryStandardMetrics.FrameTimeP99Ms,
            TelemetryMetricQuality.Measured, "legacy-measured", TelemetryMetricOrigin.Legacy);
        RequireMetric(legacyMeasured, TelemetryStandardMetrics.SystemCpuUtilizationPercent,
            TelemetryMetricQuality.Partial, "legacy-bridge", TelemetryMetricOrigin.Legacy);

        _stage = "unknown-policy";
        var unknown = TelemetryLegacyBridge.FromLegacy(new TelemetrySample
        {
            Timestamp = timestamp,
            Fps = 90,
            PingMs = 18,
            DataQuality = "Custom source detail"
        });
        RequireMetric(unknown, TelemetryStandardMetrics.FrameFpsAverage,
            TelemetryMetricQuality.Partial, "legacy-bridge", TelemetryMetricOrigin.Legacy);
        RequireMetric(unknown, TelemetryStandardMetrics.NetworkPingMs,
            TelemetryMetricQuality.Partial, "legacy-bridge", TelemetryMetricOrigin.Legacy);

        _stage = "nonfinite-filter";
        var filtered = TelemetryLegacyBridge.FromLegacy(new TelemetrySample
        {
            Timestamp = timestamp,
            Fps = double.NaN,
            FrameTimeMs = double.PositiveInfinity,
            LatencyMs = null,
            CpuPercent = 22,
            DataQuality = "System"
        });
        Require(filtered.Metrics.Count == 1,
            "Null/NaN/infinity legacy values must not create telemetry observations.");
        RequireMetric(filtered, TelemetryStandardMetrics.SystemCpuUtilizationPercent,
            TelemetryMetricQuality.Measured, "native-system", TelemetryMetricOrigin.Direct);

        _stage = "all-field-mapping";
        var all = TelemetryLegacyBridge.FromLegacy(new TelemetrySample
        {
            Timestamp = timestamp,
            Fps = 1,
            OnePercentLow = 2,
            PointOnePercentLow = 3,
            FrameTimeMs = 4,
            FrameTimeP95Ms = 5,
            FrameTimeP99Ms = 6,
            StutterPercent = 7,
            LatencyMs = 8,
            CpuPercent = 9,
            GpuPercent = 10,
            MemoryUsedGb = 11,
            MemoryTotalGb = 12,
            CpuTemperatureC = 13,
            GpuTemperatureC = 14,
            PingMs = 15,
            JitterMs = 16,
            PacketLossPercent = 17,
            DataQuality = "unknown"
        });
        Require(all.Metrics.Count == TelemetryStandardMetrics.All.Count,
            "Every legacy TelemetrySample field must map explicitly to the 17 approved v2 metrics.");
        Require(all.Metrics.All(item => item.Coverage == 1d),
            "Finite legacy values must carry full observation coverage in the compatibility bridge.");

        _stage = "legacy-source-compatibility";
        var stillLegacyCompatible = new TelemetrySample
        {
            Timestamp = timestamp,
            Fps = 60,
            DataQuality = "Partial"
        };
        Require(stillLegacyCompatible.Fps == 60 && stillLegacyCompatible.DataQuality == "Partial",
            "TelemetrySample must remain source-compatible and require no v2 fields.");

        _stage = "complete";
        Console.WriteLine("PASS Track 4 legacy telemetry bridge preserves provenance and never fabricates measurements");
    }

    private static void RequireMetric(
        TelemetryFrame frame,
        TelemetryMetricDescriptor descriptor,
        TelemetryMetricQuality quality,
        string sourceId,
        TelemetryMetricOrigin origin)
    {
        Require(frame.TryGetMetric(descriptor.Id, out var observation) && observation is not null,
            $"Expected metric '{descriptor.Id}' was not emitted.");
        Require(observation!.Quality == quality,
            $"Metric '{descriptor.Id}' quality mismatch. Expected {quality}, got {observation.Quality}.");
        Require(observation.SourceId == sourceId,
            $"Metric '{descriptor.Id}' source mismatch. Expected '{sourceId}', got '{observation.SourceId}'.");
        Require(observation.Origin == origin,
            $"Metric '{descriptor.Id}' origin mismatch. Expected {origin}, got {observation.Origin}.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
