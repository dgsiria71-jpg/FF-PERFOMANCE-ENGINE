namespace FFPerformanceEngine.Core.Telemetry;

public sealed record TelemetryFrame
{
    private readonly IReadOnlyDictionary<string, TelemetryMetricObservation> _byMetricId;

    public DateTimeOffset Timestamp { get; }
    public IReadOnlyList<TelemetryMetricObservation> Metrics { get; }
    public TelemetryMetricQuality FrameQuality { get; }

    public TelemetryFrame(
        DateTimeOffset timestamp,
        IEnumerable<TelemetryMetricObservation> metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        var copied = metrics.ToArray();
        if (copied.Any(item => item is null))
            throw new ArgumentException("Telemetry frame metrics cannot contain null observations.", nameof(metrics));

        var duplicate = copied
            .GroupBy(item => item.Metric.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new ArgumentException(
                $"Telemetry frame contains duplicate metric id '{duplicate.Key}'.",
                nameof(metrics));

        var ordered = copied
            .OrderBy(item => item.Metric.Id, StringComparer.Ordinal)
            .ToArray();

        Timestamp = timestamp;
        Metrics = Array.AsReadOnly(ordered);
        _byMetricId = ordered.ToDictionary(item => item.Metric.Id, StringComparer.Ordinal);
        FrameQuality = ordered.Length == 0
            ? TelemetryMetricQuality.Unavailable
            : ordered.All(item => item.Quality == TelemetryMetricQuality.Measured)
                ? TelemetryMetricQuality.Measured
                : TelemetryMetricQuality.Partial;
    }

    public bool TryGetMetric(string metricId, out TelemetryMetricObservation? observation)
    {
        observation = null;
        if (string.IsNullOrWhiteSpace(metricId)) return false;

        string normalized;
        try
        {
            normalized = TelemetryIdentifierRules.NormalizeMetricId(metricId, nameof(metricId));
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (!_byMetricId.TryGetValue(normalized, out var found)) return false;
        observation = found;
        return true;
    }
}
