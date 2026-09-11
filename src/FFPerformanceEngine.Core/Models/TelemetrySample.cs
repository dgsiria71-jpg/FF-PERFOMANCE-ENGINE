namespace FFPerformanceEngine.Core.Models;

public sealed record TelemetrySample
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public double? Fps { get; init; }
    public double? OnePercentLow { get; init; }
    public double? PointOnePercentLow { get; init; }
    public double? FrameTimeMs { get; init; }
    public double? FrameTimeP95Ms { get; init; }
    public double? FrameTimeP99Ms { get; init; }
    public double? StutterPercent { get; init; }
    public double? LatencyMs { get; init; }
    public double? CpuPercent { get; init; }
    public double? GpuPercent { get; init; }
    public double? MemoryUsedGb { get; init; }
    public double? MemoryTotalGb { get; init; }
    public double? CpuTemperatureC { get; init; }
    public double? GpuTemperatureC { get; init; }
    public double? PingMs { get; init; }
    public double? JitterMs { get; init; }
    public double? PacketLossPercent { get; init; }
    public string DataQuality { get; init; } = "Partial";
}
