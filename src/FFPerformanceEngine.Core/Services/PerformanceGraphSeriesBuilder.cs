namespace FFPerformanceEngine.Core.Services;

public enum PerformanceGraphMetric
{
    Fps,
    FrameTime,
    Latency
}

public sealed record PerformanceGraphPoint
{
    public DateTimeOffset Timestamp { get; init; }
    public double Value { get; init; }
    public string DataQuality { get; init; } = string.Empty;
}

public sealed record PerformanceGraphSegment
{
    public IReadOnlyList<PerformanceGraphPoint> Points { get; init; } = Array.Empty<PerformanceGraphPoint>();
}

public static class PerformanceGraphSeriesBuilder
{
    public static IReadOnlyList<PerformanceGraphSegment> Build(
        IEnumerable<PerformanceTimelinePoint> points,
        PerformanceGraphMetric metric,
        TimeSpan maxGap)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (maxGap <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(maxGap));

        var segments = new List<PerformanceGraphSegment>();
        var current = new List<PerformanceGraphPoint>();

        foreach (var point in points.OrderBy(point => point.Timestamp))
        {
            var value = MetricValue(point, metric);
            if (value is not double measured || !double.IsFinite(measured))
            {
                FlushCurrent();
                continue;
            }

            if (current.Count > 0 && point.Timestamp - current[^1].Timestamp > maxGap)
                FlushCurrent();

            current.Add(new PerformanceGraphPoint
            {
                Timestamp = point.Timestamp,
                Value = measured,
                DataQuality = point.DataQuality
            });
        }

        FlushCurrent();
        return segments;

        void FlushCurrent()
        {
            if (current.Count == 0) return;
            segments.Add(new PerformanceGraphSegment { Points = current.ToArray() });
            current = new List<PerformanceGraphPoint>();
        }
    }

    private static double? MetricValue(PerformanceTimelinePoint point, PerformanceGraphMetric metric)
        => metric switch
        {
            PerformanceGraphMetric.Fps => point.Fps,
            PerformanceGraphMetric.FrameTime => point.FrameTimeMs,
            PerformanceGraphMetric.Latency => point.LatencyMs,
            _ => throw new ArgumentOutOfRangeException(nameof(metric))
        };
}
