using System.Globalization;

namespace FFPerformanceEngine.Core.Services;

public sealed record PerformanceTimelineDisplayRow
{
    public DateTimeOffset Timestamp { get; init; }
    public PerformanceTimelineKind Kind { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public string Metrics { get; init; } = "—";
}

public static class PerformanceTimelinePresentation
{
    public static IReadOnlyList<PerformanceTimelineDisplayRow> Recent(
        IEnumerable<PerformanceTimelineEntry> entries,
        int maxRows)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (maxRows <= 0) throw new ArgumentOutOfRangeException(nameof(maxRows));

        return entries
            .OrderBy(entry => entry.Timestamp)
            .TakeLast(maxRows)
            .Select(ToRow)
            .ToArray();
    }

    private static PerformanceTimelineDisplayRow ToRow(PerformanceTimelineEntry entry)
    {
        var metrics = new List<string>();
        var sample = entry.Telemetry;
        AddMetric(metrics, sample?.Fps, "0.0", " FPS");
        AddMetric(metrics, sample?.OnePercentLow, "0.0", " FPS 1% Low");
        AddMetric(metrics, sample?.FrameTimeMs, "0.00", " ms");
        AddMetric(metrics, sample?.LatencyMs, "0.0", " ms latência");

        return new PerformanceTimelineDisplayRow
        {
            Timestamp = entry.Timestamp,
            Kind = entry.Kind,
            Title = entry.Title,
            Detail = entry.Detail,
            Metrics = metrics.Count == 0 ? "—" : string.Join(" · ", metrics)
        };
    }

    private static void AddMetric(List<string> target, double? value, string format, string suffix)
    {
        if (value is not double number || !double.IsFinite(number)) return;
        target.Add(number.ToString(format, CultureInfo.InvariantCulture) + suffix);
    }
}
