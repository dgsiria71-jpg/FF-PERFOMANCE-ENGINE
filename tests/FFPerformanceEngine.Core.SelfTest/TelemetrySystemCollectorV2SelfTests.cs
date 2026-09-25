using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;

internal static class TelemetrySystemCollectorV2SelfTests
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
                $"System telemetry v2 self-test failed at stage '{_stage}'.",
                ex);
        }
    }

    private static void RunCore()
    {
        var timestamp = new DateTimeOffset(2026, 9, 8, 21, 0, 0, TimeSpan.Zero);

        _stage = "direct-adapter";
        var frame = SystemTelemetryFrameAdapter.Create(new SystemTelemetrySnapshot
        {
            Timestamp = timestamp,
            CpuPercent = 42.5,
            MemoryUsedGb = 7.25,
            MemoryTotalGb = 15.75
        });

        Require(frame.Timestamp == timestamp, "System v2 adapter must preserve the observation timestamp.");
        Require(frame.Metrics.Count == 3, "System v2 adapter must emit exactly CPU + used memory + total memory when all are finite.");
        RequireDirect(frame, TelemetryStandardMetrics.SystemCpuUtilizationPercent, 42.5);
        RequireDirect(frame, TelemetryStandardMetrics.SystemMemoryUsedGb, 7.25);
        RequireDirect(frame, TelemetryStandardMetrics.SystemMemoryTotalGb, 15.75);
        Require(frame.FrameQuality == TelemetryMetricQuality.Measured,
            "A frame containing only direct measured native system metrics must summarize as Measured.");

        _stage = "nonfinite-filter";
        var filtered = SystemTelemetryFrameAdapter.Create(new SystemTelemetrySnapshot
        {
            Timestamp = timestamp,
            CpuPercent = double.NaN,
            MemoryUsedGb = double.PositiveInfinity,
            MemoryTotalGb = null
        });
        Require(filtered.Metrics.Count == 0 && filtered.FrameQuality == TelemetryMetricQuality.Unavailable,
            "Null/NaN/infinity native-system values must be absent instead of becoming fabricated observations.");

        _stage = "no-unapproved-channels";
        Require(!frame.TryGetMetric(TelemetryStandardMetrics.SystemGpuUtilizationPercent.Id, out _)
                && !frame.TryGetMetric(TelemetryStandardMetrics.CpuTemperatureCelsius.Id, out _)
                && !frame.TryGetMetric(TelemetryStandardMetrics.NetworkPingMs.Id, out _),
            "Current native system collector must not fabricate GPU, thermal or network metrics.");

        _stage = "service-additive-api";
        var service = new TelemetryService();
        var legacy = service.CaptureSystemSample();
        Require(legacy is not null, "Legacy CaptureSystemSample API must remain callable.");

        var liveFrame = service.CaptureSystemFrame();
        foreach (var observation in liveFrame.Metrics)
        {
            Require(observation.Metric.Id == TelemetryStandardMetrics.SystemCpuUtilizationPercent.Id
                    || observation.Metric.Id == TelemetryStandardMetrics.SystemMemoryUsedGb.Id
                    || observation.Metric.Id == TelemetryStandardMetrics.SystemMemoryTotalGb.Id,
                $"CaptureSystemFrame emitted unsupported current native metric '{observation.Metric.Id}'.");
            Require(observation.Quality == TelemetryMetricQuality.Measured
                    && observation.SourceId == "native-system"
                    && observation.Origin == TelemetryMetricOrigin.Direct
                    && observation.Coverage == 1d,
                "Every live native system v2 metric must have direct native-system measured provenance and coverage 1.");
        }

        _stage = "complete";
        Console.WriteLine("PASS Track 4 native CPU/physical-memory collector exposes direct typed v2 telemetry without inventing channels");
    }

    private static void RequireDirect(
        TelemetryFrame frame,
        TelemetryMetricDescriptor descriptor,
        double expectedValue)
    {
        Require(frame.TryGetMetric(descriptor.Id, out var observation) && observation is not null,
            $"Expected native system metric '{descriptor.Id}' is missing.");
        Require(observation!.Value == expectedValue,
            $"Metric '{descriptor.Id}' value mismatch. Expected {expectedValue}, got {observation.Value}.");
        Require(observation.Quality == TelemetryMetricQuality.Measured,
            $"Metric '{descriptor.Id}' must be Measured.");
        Require(observation.Coverage == 1d,
            $"Metric '{descriptor.Id}' must carry full observation coverage.");
        Require(observation.SourceId == "native-system",
            $"Metric '{descriptor.Id}' must use native-system provenance.");
        Require(observation.Origin == TelemetryMetricOrigin.Direct,
            $"Metric '{descriptor.Id}' must be Direct origin.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
