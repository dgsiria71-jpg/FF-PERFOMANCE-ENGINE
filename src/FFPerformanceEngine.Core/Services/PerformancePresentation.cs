using System.Globalization;
using FFPerformanceEngine.Core.Telemetry;

namespace FFPerformanceEngine.Core.Services;

public sealed record PerformanceCapturePresentation
{
    public bool HasMeasurement { get; init; }
    public string Instance { get; init; } = "—";
    public string ProcessId { get; init; } = "—";
    public string Fps { get; init; } = "—";
    public string OnePercentLow { get; init; } = "—";
    public string PointOnePercentLow { get; init; } = "—";
    public string FrameTime { get; init; } = "—";
    public string P95FrameTime { get; init; } = "—";
    public string P99FrameTime { get; init; } = "—";
    public string Stutter { get; init; } = "—";
    public string Latency { get; init; } = "—";
    public string DataQuality { get; init; } = "—";
    public string Detail { get; init; } = string.Empty;
}

public static class PerformancePresentation
{
    public static PerformanceCapturePresentation FromCapture(PerformanceCaptureResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var sample = result.Sample;
        var target = result.Target;

        return new PerformanceCapturePresentation
        {
            HasMeasurement = sample is not null,
            Instance = string.IsNullOrWhiteSpace(target.InstanceName) ? "—" : target.InstanceName,
            ProcessId = ProcessId(target),
            Fps = Metric(sample?.Fps, "0.0", " FPS"),
            OnePercentLow = Metric(sample?.OnePercentLow, "0.0", " FPS"),
            PointOnePercentLow = Metric(sample?.PointOnePercentLow, "0.0", " FPS"),
            FrameTime = Metric(sample?.FrameTimeMs, "0.00", " ms"),
            P95FrameTime = Metric(sample?.FrameTimeP95Ms, "0.00", " ms"),
            P99FrameTime = Metric(sample?.FrameTimeP99Ms, "0.00", " ms"),
            Stutter = Metric(sample?.StutterPercent, "0.00", "%"),
            Latency = Metric(sample?.LatencyMs, "0.0", " ms"),
            DataQuality = string.IsNullOrWhiteSpace(sample?.DataQuality) ? "—" : sample!.DataQuality,
            Detail = result.Message
        };
    }

    public static PerformanceCapturePresentation FromCapture(PerformanceTypedCaptureResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var frame = result.Frame;
        var target = result.Target;

        return new PerformanceCapturePresentation
        {
            HasMeasurement = frame is not null,
            Instance = string.IsNullOrWhiteSpace(target.InstanceName) ? "—" : target.InstanceName,
            ProcessId = ProcessId(target),
            Fps = Metric(frame, TelemetryStandardMetrics.FrameFpsAverage, "0.0", " FPS"),
            OnePercentLow = Metric(frame, TelemetryStandardMetrics.FrameFpsLow1, "0.0", " FPS"),
            PointOnePercentLow = Metric(frame, TelemetryStandardMetrics.FrameFpsLow01, "0.0", " FPS"),
            FrameTime = Metric(frame, TelemetryStandardMetrics.FrameTimeAverageMs, "0.00", " ms"),
            P95FrameTime = Metric(frame, TelemetryStandardMetrics.FrameTimeP95Ms, "0.00", " ms"),
            P99FrameTime = Metric(frame, TelemetryStandardMetrics.FrameTimeP99Ms, "0.00", " ms"),
            Stutter = Metric(frame, TelemetryStandardMetrics.FrameStutterPercent, "0.00", "%"),
            Latency = Metric(frame, TelemetryStandardMetrics.FrameLatencyAverageMs, "0.0", " ms"),
            DataQuality = frame is null ? "—" : frame.FrameQuality.ToString(),
            Detail = result.Message
        };
    }

    private static string ProcessId(PerformanceCaptureTarget target)
        => target.ProcessId is int processId && processId > 0
            ? processId.ToString(CultureInfo.InvariantCulture)
            : "—";

    private static string Metric(
        TelemetryFrame? frame,
        TelemetryMetricDescriptor descriptor,
        string format,
        string suffix)
    {
        if (frame is null
            || !frame.TryGetMetric(descriptor.Id, out var observation)
            || observation is null)
            return "—";
        return Metric(observation.Value, format, suffix);
    }

    private static string Metric(double? value, string format, string suffix)
        => value is double number && double.IsFinite(number)
            ? number.ToString(format, CultureInfo.InvariantCulture) + suffix
            : "—";
}
