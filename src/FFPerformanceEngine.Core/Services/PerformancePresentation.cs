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
        return new PerformanceCapturePresentation { Detail = result.Message };
    }
}
