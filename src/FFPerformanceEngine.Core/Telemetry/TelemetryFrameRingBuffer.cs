namespace FFPerformanceEngine.Core.Telemetry;

public sealed class TelemetryFrameRingBuffer
{
    private readonly object _sync = new();
    private readonly Queue<Entry> _entries = new();
    private long _nextSequence;

    public TelemetryFrameRingBuffer(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity = capacity;
    }

    public int Capacity { get; }

    public int Count
    {
        get
        {
            lock (_sync)
                return _entries.Count;
        }
    }

    public void Append(TelemetryFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        lock (_sync)
        {
            _entries.Enqueue(new Entry(_nextSequence++, frame));
            while (_entries.Count > Capacity)
                _entries.Dequeue();
        }
    }

    public IReadOnlyList<TelemetryFrame> Snapshot()
    {
        lock (_sync)
            return SnapshotCore(_entries);
    }

    public IReadOnlyList<TelemetryFrame> Snapshot(
        DateTimeOffset startInclusive,
        DateTimeOffset endExclusive)
    {
        if (endExclusive < startInclusive)
            throw new ArgumentOutOfRangeException(
                nameof(endExclusive),
                "Telemetry window end must not precede the start.");

        lock (_sync)
        {
            return SnapshotCore(_entries.Where(entry =>
                entry.Frame.Timestamp >= startInclusive
                && entry.Frame.Timestamp < endExclusive));
        }
    }

    public void Clear()
    {
        lock (_sync)
            _entries.Clear();
    }

    private static IReadOnlyList<TelemetryFrame> SnapshotCore(IEnumerable<Entry> entries)
        => entries
            .OrderBy(entry => entry.Frame.Timestamp)
            .ThenBy(entry => entry.Sequence)
            .Select(entry => entry.Frame)
            .ToArray();

    private sealed record Entry(long Sequence, TelemetryFrame Frame);
}
