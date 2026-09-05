using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public enum PerformanceTimelineKind
{
    Telemetry,
    Guardian,
    UserMarker,
    Profile,
    BlueStacks,
    Benchmark
}

public sealed record PerformanceTimelineEntry
{
    public DateTimeOffset Timestamp { get; init; }
    public PerformanceTimelineKind Kind { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public TelemetrySample? Telemetry { get; init; }
}

public sealed class PerformanceTimelineBuffer
{
    private readonly object _sync = new();
    private readonly Queue<PerformanceTimelineEntry> _entries = new();
    private readonly int _capacity;

    public PerformanceTimelineBuffer(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
    }

    public void AppendTelemetry(TelemetrySample sample)
    {
        ArgumentNullException.ThrowIfNull(sample);
        Append(new PerformanceTimelineEntry
        {
            Timestamp = sample.Timestamp,
            Kind = PerformanceTimelineKind.Telemetry,
            Title = "Telemetry",
            Detail = sample.DataQuality,
            Telemetry = sample
        });
    }

    public void AppendEvent(DateTimeOffset timestamp, PerformanceTimelineKind kind, string title, string detail)
    {
        Append(new PerformanceTimelineEntry
        {
            Timestamp = timestamp,
            Kind = kind,
            Title = title ?? string.Empty,
            Detail = detail ?? string.Empty
        });
    }

    public IReadOnlyList<PerformanceTimelineEntry> Snapshot()
    {
        lock (_sync)
            return _entries.OrderBy(entry => entry.Timestamp).ToArray();
    }

    public IReadOnlyList<PerformanceTimelineEntry> Snapshot(DateTimeOffset start, DateTimeOffset end)
    {
        if (end < start) throw new ArgumentOutOfRangeException(nameof(end), "Timeline end must not precede start.");
        lock (_sync)
        {
            return _entries
                .Where(entry => entry.Timestamp >= start && entry.Timestamp <= end)
                .OrderBy(entry => entry.Timestamp)
                .ToArray();
        }
    }

    private void Append(PerformanceTimelineEntry entry)
    {
        lock (_sync)
        {
            _entries.Enqueue(entry);
            while (_entries.Count > _capacity) _entries.Dequeue();
        }
    }
}
