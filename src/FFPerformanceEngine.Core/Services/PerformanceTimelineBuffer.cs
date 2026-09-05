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
    public PerformanceTimelineBuffer(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
    }

    public void AppendTelemetry(TelemetrySample sample) => ArgumentNullException.ThrowIfNull(sample);

    public void AppendEvent(DateTimeOffset timestamp, PerformanceTimelineKind kind, string title, string detail)
    {
    }

    public IReadOnlyList<PerformanceTimelineEntry> Snapshot() => Array.Empty<PerformanceTimelineEntry>();

    public IReadOnlyList<PerformanceTimelineEntry> Snapshot(DateTimeOffset start, DateTimeOffset end)
        => Array.Empty<PerformanceTimelineEntry>();
}
