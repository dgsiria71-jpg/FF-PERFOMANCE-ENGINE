namespace FFPerformanceEngine.Core.Services;

public enum PerformanceIntervalWindowMode
{
    Session,
    Recent60Seconds
}

public sealed record PerformanceIntervalWindow
{
    public DateTimeOffset Start { get; init; }
    public DateTimeOffset End { get; init; }
}

public static class PerformanceIntervalWindowResolver
{
    private static readonly TimeSpan RecentWindow = TimeSpan.FromSeconds(60);

    public static PerformanceIntervalWindow? Resolve(
        IEnumerable<PerformanceTimelineEntry> entries,
        PerformanceIntervalWindowMode mode)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var timestamps = entries
            .Select(entry => entry.Timestamp)
            .OrderBy(timestamp => timestamp)
            .ToArray();

        if (timestamps.Length == 0) return null;

        var first = timestamps[0];
        var last = timestamps[^1];
        var start = mode switch
        {
            PerformanceIntervalWindowMode.Session => first,
            PerformanceIntervalWindowMode.Recent60Seconds => Max(first, last - RecentWindow),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };

        return new PerformanceIntervalWindow
        {
            Start = start,
            End = last
        };
    }

    private static DateTimeOffset Max(DateTimeOffset left, DateTimeOffset right)
        => left >= right ? left : right;
}
