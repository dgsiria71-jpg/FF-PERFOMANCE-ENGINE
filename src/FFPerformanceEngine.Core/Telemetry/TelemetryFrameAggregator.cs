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

        var contributors = inWindow
            .SelectMany(frame => frame.Metrics.Select(observation => new Contributor(frame.Timestamp, observation)))
            .GroupBy(contributor => contributor.Observation.Metric.Id, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(AggregateMetric)
            .ToArray();

        return new TelemetryFrame(endExclusive, contributors);
    }

    public static TelemetryFrame AggregateOneSecond(
        DateTimeOffset startInclusive,
        IEnumerable<TelemetryFrame> frames)
        => Aggregate(startInclusive, startInclusive.AddSeconds(1), frames);

    private static TelemetryMetricObservation AggregateMetric(
        IGrouping<string, Contributor> group)
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

        var coverage = ordered.Min(contributor => contributor.Observation.Coverage);
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
