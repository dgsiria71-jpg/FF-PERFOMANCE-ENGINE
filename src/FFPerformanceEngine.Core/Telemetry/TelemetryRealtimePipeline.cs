namespace FFPerformanceEngine.Core.Telemetry;

public sealed record TelemetrySessionAggregateSnapshot
{
    public DateTimeOffset StartedAt { get; }
    public DateTimeOffset EndedAt { get; }
    public IReadOnlyList<TelemetryFrame> TenSecondAggregates { get; }

    public TelemetrySessionAggregateSnapshot(
        DateTimeOffset startedAt,
        DateTimeOffset endedAt,
        IEnumerable<TelemetryFrame> tenSecondAggregates)
    {
        ArgumentNullException.ThrowIfNull(tenSecondAggregates);
        if (endedAt < startedAt)
            throw new ArgumentOutOfRangeException(
                nameof(endedAt),
                "Telemetry session end must not precede its start.");

        StartedAt = startedAt;
        EndedAt = endedAt;
        TenSecondAggregates = Array.AsReadOnly(tenSecondAggregates.ToArray());
    }
}

public sealed class TelemetryRealtimePipeline
{
    private static readonly TimeSpan OneSecondWindow = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan TenSecondWindow = TimeSpan.FromSeconds(10);

    private readonly object _sync = new();
    private readonly TelemetryFrameRingBuffer _rawFrames;
    private readonly TelemetryFrameRingBuffer _oneSecondAggregates;
    private readonly TelemetryFrameRingBuffer _sessionTenSecondAggregates;

    private DateTimeOffset? _lastOneSecondEnd;
    private DateTimeOffset? _activeSessionStartedAt;
    private DateTimeOffset? _lastTenSecondEnd;

    public TelemetryRealtimePipeline(
        int rawCapacity,
        int oneSecondCapacity,
        int sessionTenSecondCapacity)
    {
        if (rawCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(rawCapacity));
        if (oneSecondCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(oneSecondCapacity));
        if (sessionTenSecondCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(sessionTenSecondCapacity));

        _rawFrames = new TelemetryFrameRingBuffer(rawCapacity);
        _oneSecondAggregates = new TelemetryFrameRingBuffer(oneSecondCapacity);
        _sessionTenSecondAggregates = new TelemetryFrameRingBuffer(sessionTenSecondCapacity);
    }

    public int RawFrameCount => _rawFrames.Count;
    public int OneSecondAggregateCount => _oneSecondAggregates.Count;
    public int SessionTenSecondAggregateCount => _sessionTenSecondAggregates.Count;

    public bool HasActiveSession
    {
        get
        {
            lock (_sync)
                return _activeSessionStartedAt is not null;
        }
    }

    public DateTimeOffset? ActiveSessionStartedAt
    {
        get
        {
            lock (_sync)
                return _activeSessionStartedAt;
        }
    }

    public IReadOnlyList<TelemetryFrame> SnapshotRawFrames()
        => ReadOnlySnapshot(_rawFrames);

    public IReadOnlyList<TelemetryFrame> SnapshotOneSecondAggregates()
        => ReadOnlySnapshot(_oneSecondAggregates);

    public IReadOnlyList<TelemetryFrame> SnapshotSessionTenSecondAggregates()
        => ReadOnlySnapshot(_sessionTenSecondAggregates);

    public void AppendRaw(TelemetryFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        lock (_sync)
        {
            if (_lastOneSecondEnd is DateTimeOffset finalizedEnd
                && frame.Timestamp < finalizedEnd)
            {
                throw new InvalidOperationException(
                    $"Telemetry frame '{frame.Timestamp:O}' is older than finalized one-second evidence ending at '{finalizedEnd:O}'.");
            }

            _rawFrames.Append(frame);
        }
    }

    public TelemetryFrame FinalizeOneSecond(DateTimeOffset startInclusive)
    {
        var endExclusive = startInclusive.Add(OneSecondWindow);

        lock (_sync)
        {
            if (_lastOneSecondEnd is DateTimeOffset finalizedEnd
                && startInclusive < finalizedEnd)
            {
                throw new InvalidOperationException(
                    $"Telemetry one-second window starting at '{startInclusive:O}' overlaps evidence already finalized through '{finalizedEnd:O}'.");
            }

            var aggregate = TelemetryFrameAggregator.AggregateOneSecond(
                startInclusive,
                _rawFrames.Snapshot(startInclusive, endExclusive));
            _oneSecondAggregates.Append(aggregate);
            _lastOneSecondEnd = endExclusive;
            return aggregate;
        }
    }

    public void BeginSession(DateTimeOffset startedAt)
    {
        lock (_sync)
        {
            if (_activeSessionStartedAt is not null)
                throw new InvalidOperationException("A telemetry realtime session is already active.");

            if (_lastOneSecondEnd is DateTimeOffset finalizedEnd
                && startedAt < finalizedEnd)
            {
                throw new InvalidOperationException(
                    $"Telemetry session cannot start at '{startedAt:O}' before finalized one-second evidence ending at '{finalizedEnd:O}'.");
            }

            _sessionTenSecondAggregates.Clear();
            _activeSessionStartedAt = startedAt;
            _lastTenSecondEnd = startedAt;
        }
    }

    public TelemetryFrame FinalizeTenSeconds(DateTimeOffset startInclusive)
    {
        var endExclusive = startInclusive.Add(TenSecondWindow);

        lock (_sync)
        {
            if (_activeSessionStartedAt is not DateTimeOffset sessionStart)
                throw new InvalidOperationException("Telemetry ten-second aggregation requires an active session.");

            if (startInclusive < sessionStart)
                throw new InvalidOperationException("Telemetry ten-second aggregation cannot precede the active session start.");

            if (_lastTenSecondEnd is DateTimeOffset finalizedEnd
                && startInclusive < finalizedEnd)
            {
                throw new InvalidOperationException(
                    $"Telemetry ten-second window starting at '{startInclusive:O}' overlaps session evidence already finalized through '{finalizedEnd:O}'.");
            }

            if (_lastOneSecondEnd is not DateTimeOffset oneSecondEnd
                || oneSecondEnd < endExclusive)
            {
                throw new InvalidOperationException(
                    "Telemetry ten-second aggregation cannot run ahead of finalized one-second evidence.");
            }

            var aggregate = TelemetryFrameAggregator.AggregateChildWindows(
                startInclusive,
                endExclusive,
                OneSecondWindow,
                _oneSecondAggregates.Snapshot());
            _sessionTenSecondAggregates.Append(aggregate);
            _lastTenSecondEnd = endExclusive;
            return aggregate;
        }
    }

    public TelemetrySessionAggregateSnapshot CompleteSession(DateTimeOffset endedAt)
    {
        lock (_sync)
        {
            if (_activeSessionStartedAt is not DateTimeOffset sessionStart)
                throw new InvalidOperationException("No telemetry realtime session is active.");

            if (endedAt < sessionStart)
                throw new ArgumentOutOfRangeException(
                    nameof(endedAt),
                    "Telemetry session end must not precede the active session start.");

            if (_lastTenSecondEnd is DateTimeOffset finalizedEnd
                && endedAt < finalizedEnd)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(endedAt),
                    "Telemetry session end cannot precede finalized ten-second session evidence.");
            }

            var snapshot = new TelemetrySessionAggregateSnapshot(
                sessionStart,
                endedAt,
                _sessionTenSecondAggregates.Snapshot());

            _activeSessionStartedAt = null;
            _lastTenSecondEnd = null;
            return snapshot;
        }
    }

    private static IReadOnlyList<TelemetryFrame> ReadOnlySnapshot(TelemetryFrameRingBuffer buffer)
        => Array.AsReadOnly(buffer.Snapshot().ToArray());
}
