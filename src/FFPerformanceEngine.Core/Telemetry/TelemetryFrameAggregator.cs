namespace FFPerformanceEngine.Core.Telemetry;

public static class TelemetryFrameAggregator
{
    public static TelemetryFrame Aggregate(
        DateTimeOffset startInclusive,
        DateTimeOffset endExclusive,
        IEnumerable<TelemetryFrame> frames)
    {
        ArgumentNullException.ThrowIfNull(frames);
        if (endExclusive <= startInclusive)
            throw new ArgumentOutOfRangeException(
                nameof(endExclusive),
                "Telemetry aggregation window end must be after the start.");

        var inWindow = frames
            .Where(frame => frame is not null
                            && frame.Timestamp >= startInclusive
                            && frame.Timestamp < endExclusive)
            .ToArray();

        return AggregateFrames(endExclusive, inWindow, expectedChildCount: null);
    }

    public static TelemetryFrame AggregateOneSecond(
        DateTimeOffset startInclusive,
        IEnumerable<TelemetryFrame> frames)
        => Aggregate(startInclusive, startInclusive.AddSeconds(1), frames);

    public static TelemetryFrame AggregateChildWindows(
        DateTimeOffset startInclusive,
        DateTimeOffset endExclusive,
        TimeSpan childWindow,
        IEnumerable<TelemetryFrame> childAggregates)
    {
        ArgumentNullException.ThrowIfNull(childAggregates);
        if (endExclusive <= startInclusive)
            throw new ArgumentOutOfRangeException(
                nameof(endExclusive),
                "Telemetry parent aggregation window end must be after the start.");
        if (childWindow <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(
                nameof(childWindow),
                "Telemetry child aggregation window must be positive.");

        var parentTicks = (endExclusive - startInclusive).Ticks;
        var childTicks = childWindow.Ticks;
        if (parentTicks % childTicks != 0)
            throw new ArgumentException(
                "Telemetry parent aggregation duration must be an exact multiple of the child window.",
                nameof(childWindow));

        var expectedChildCount = parentTicks / childTicks;
        var eligible = childAggregates
            .Where(frame => frame is not null
                            && frame.Timestamp > startInclusive
                            && frame.Timestamp <= endExclusive)
            .ToArray();

        foreach (var frame in eligible)
        {
            var deltaTicks = (frame.Timestamp - startInclusive).Ticks;
            if (deltaTicks % childTicks != 0)
            {
                throw new InvalidOperationException(
                    $"Telemetry child aggregate timestamp '{frame.Timestamp:O}' is not aligned to the declared child window.");
            }
        }

        var duplicateTimestamp = eligible
            .GroupBy(frame => frame.Timestamp)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateTimestamp is not null)
        {
            throw new InvalidOperationException(
                $"Telemetry hierarchical aggregation contains duplicate child window end '{duplicateTimestamp.Key:O}'.");
        }

        return AggregateFrames(endExclusive, eligible, expectedChildCount);
    }

    private static TelemetryFrame AggregateFrames(
        DateTimeOffset resultTimestamp,
        IReadOnlyCollection<TelemetryFrame> frames,
        long? expectedChildCount)
    {
        var contributors = frames
            .SelectMany(frame => frame.Metrics.Select(observation => new Contributor(frame.Timestamp, observation)))
            .GroupBy(contributor => contributor.Observation.Metric.Id, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var temporalPresence = expectedChildCount is long expected
                    ? group.LongCount() / (double)expected
                    : 1d;
                return AggregateMetric(group, temporalPresence);
            })
            .ToArray();

        return new TelemetryFrame(resultTimestamp, contributors);
    }

    private static TelemetryMetricObservation AggregateMetric(
        IGrouping<string, Contributor> group,
        double coverageScale)
    {
        var ordered = group
            .OrderBy(contributor => contributor.Timestamp)
            .ThenBy(contributor => contributor.Observation.SourceId, StringComparer.Ordinal)
            .ThenBy(contributor => contributor.Observation.Origin)
            .ThenBy(contributor => contributor.Observation.Quality)
            .ThenBy(contributor => contributor.Observation.Coverage)
            .ThenBy(contributor => contributor.Observation.Value)
            .ToArray();

        var descriptor = ordered[0].Observation.Metric;
        foreach (var contributor in ordered.Skip(1))
        {
            var candidate = contributor.Observation.Metric;
            if (candidate.Unit != descriptor.Unit
                || candidate.Domain != descriptor.Domain
                || candidate.Aggregation != descriptor.Aggregation)
            {
                throw new InvalidOperationException(
                    $"Telemetry metric '{descriptor.Id}' has incompatible descriptor semantics in one aggregation window.");
            }
        }

        var values = ordered.Select(contributor => contributor.Observation.Value).ToArray();
        var value = descriptor.Aggregation switch
        {
            TelemetryAggregationKind.Gauge => values.Average(),
            TelemetryAggregationKind.Average => values.Average(),
            TelemetryAggregationKind.Minimum => values.Min(),
            TelemetryAggregationKind.Maximum => values.Max(),
            TelemetryAggregationKind.Sum => values.Sum(),
            _ => throw new InvalidOperationException(
                $"Telemetry metric '{descriptor.Id}' has unsupported aggregation semantics '{descriptor.Aggregation}'.")
        };

        var quality = ordered.Any(contributor =>
            contributor.Observation.Quality == TelemetryMetricQuality.Partial)
            ? TelemetryMetricQuality.Partial
            : TelemetryMetricQuality.Measured;

        var coverage = ordered.Min(contributor => contributor.Observation.Coverage) * coverageScale;
        var sources = ordered
            .Select(contributor => contributor.Observation.SourceId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var sourceId = sources.Length == 1 ? sources[0] : "aggregate-mixed";

        return new TelemetryMetricObservation(
            descriptor,
            value,
            quality,
            coverage,
            sourceId,
            TelemetryMetricOrigin.Derived);
    }

    private sealed record Contributor(
        DateTimeOffset Timestamp,
        TelemetryMetricObservation Observation);
}
