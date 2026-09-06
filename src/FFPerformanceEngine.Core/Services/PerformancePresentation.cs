using System.Globalization;

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
            ProcessId = target.ProcessId is int processId && processId > 0
                ? processId.ToString(CultureInfo.InvariantCulture)
                : "—",
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

    private static string Metric(double? value, string format, string suffix)
        => value is double number && double.IsFinite(number)
            ? number.ToString(format, CultureInfo.InvariantCulture) + suffix
            : "—";
}
