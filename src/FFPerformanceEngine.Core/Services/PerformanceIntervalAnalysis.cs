using FFPerformanceEngine.Core.Telemetry;

namespace FFPerformanceEngine.Core.Services;

public sealed record PerformanceMetricEvidence
{
    public TelemetryMetricQuality Quality { get; init; }
    public double Coverage { get; init; }
    public string SourceId { get; init; } = string.Empty;
    public TelemetryMetricOrigin Origin { get; init; }
}

public sealed record PerformanceTimelinePoint
{
    public DateTimeOffset Timestamp { get; init; }
    public double? Fps { get; init; }
    public double? FrameTimeMs { get; init; }
    public double? LatencyMs { get; init; }
    public string DataQuality { get; init; } = string.Empty;
    public PerformanceMetricEvidence? FpsEvidence { get; init; }
    public PerformanceMetricEvidence? FrameTimeEvidence { get; init; }
    public PerformanceMetricEvidence? LatencyEvidence { get; init; }
}

public sealed record PerformanceIntervalSummary
{
    public DateTimeOffset Start { get; init; }
    public DateTimeOffset End { get; init; }
    public int TelemetrySamples { get; init; }
    public int FpsEvidenceSamples { get; init; }
    public int GuardianEvents { get; init; }
    public int UserMarkers { get; init; }
    public double? AverageFps { get; init; }
    public double? AverageFrameTimeMs { get; init; }
    public IReadOnlyList<PerformanceTimelinePoint> Points { get; init; } = Array.Empty<PerformanceTimelinePoint>();
}

public sealed record PerformanceIntervalComparison
{
    public required PerformanceIntervalSummary Baseline { get; init; }
    public required PerformanceIntervalSummary Candidate { get; init; }
    public double? AverageFpsDelta { get; init; }
    public double? AverageFrameTimeDeltaMs { get; init; }
}

public static class PerformanceIntervalAnalysis
{
    public static PerformanceIntervalSummary Analyze(
        IEnumerable<PerformanceTimelineEntry> entries,
        DateTimeOffset start,
        DateTimeOffset end)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (end < start) throw new ArgumentOutOfRangeException(nameof(end), "Interval end must not precede start.");

        var window = entries
            .Where(entry => entry.Timestamp >= start && entry.Timestamp <= end)
            .OrderBy(entry => entry.Timestamp)
            .ToArray();
        var points = window
            .Where(entry => entry.Kind == PerformanceTimelineKind.Telemetry
                            && (entry.TypedTelemetry is not null || entry.Telemetry is not null))
            .Select(ProjectPoint)
            .ToArray();
        var fpsValues = points
            .Where(point => point.Fps is double value && double.IsFinite(value))
            .Select(point => point.Fps!.Value)
            .ToArray();
        var frameTimes = points
            .Where(point => point.FrameTimeMs is double value && double.IsFinite(value))
            .Select(point => point.FrameTimeMs!.Value)
            .ToArray();

        return new PerformanceIntervalSummary
        {
            Start = start,
            End = end,
            TelemetrySamples = points.Length,
            FpsEvidenceSamples = fpsValues.Length,
            GuardianEvents = window.Count(entry => entry.Kind == PerformanceTimelineKind.Guardian),
            UserMarkers = window.Count(entry => entry.Kind == PerformanceTimelineKind.UserMarker),
            AverageFps = fpsValues.Length == 0 ? null : fpsValues.Average(),
            AverageFrameTimeMs = frameTimes.Length == 0 ? null : frameTimes.Average(),
            Points = points
        };
    }

    public static PerformanceIntervalComparison Compare(
        PerformanceIntervalSummary baseline,
        PerformanceIntervalSummary candidate)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(candidate);

        return new PerformanceIntervalComparison
        {
            Baseline = baseline,
            Candidate = candidate,
            AverageFpsDelta = Delta(baseline.AverageFps, candidate.AverageFps),
            AverageFrameTimeDeltaMs = Delta(baseline.AverageFrameTimeMs, candidate.AverageFrameTimeMs)
        };
    }

    private static PerformanceTimelinePoint ProjectPoint(PerformanceTimelineEntry entry)
    {
        if (entry.TypedTelemetry is { } frame)
        {
            var fps = ReadTypedMetric(frame, TelemetryStandardMetrics.FrameFpsAverage);
            var frameTime = ReadTypedMetric(frame, TelemetryStandardMetrics.FrameTimeAverageMs);
            var latency = ReadTypedMetric(frame, TelemetryStandardMetrics.FrameLatencyAverageMs);
            return new PerformanceTimelinePoint
            {
                Timestamp = entry.Timestamp,
                Fps = fps.Value,
                FrameTimeMs = frameTime.Value,
                LatencyMs = latency.Value,
                DataQuality = entry.Detail,
                FpsEvidence = fps.Evidence,
                FrameTimeEvidence = frameTime.Evidence,
                LatencyEvidence = latency.Evidence
            };
        }

        var sample = entry.Telemetry!;
        return new PerformanceTimelinePoint
        {
            Timestamp = sample.Timestamp,
            Fps = FiniteOrNull(sample.Fps),
            FrameTimeMs = FiniteOrNull(sample.FrameTimeMs),
            LatencyMs = FiniteOrNull(sample.LatencyMs),
            DataQuality = sample.DataQuality
        };
    }

    private static TypedMetricProjection ReadTypedMetric(
        TelemetryFrame frame,
        TelemetryMetricDescriptor descriptor)
    {
        if (!frame.TryGetMetric(descriptor.Id, out var observation) || observation is null)
            return new TypedMetricProjection(null, null);

        return new TypedMetricProjection(
            FiniteOrNull(observation.Value),
            new PerformanceMetricEvidence
            {
                Quality = observation.Quality,
                Coverage = observation.Coverage,
                SourceId = observation.SourceId,
                Origin = observation.Origin
            });
    }

    private static double? Delta(double? baseline, double? candidate)
        => baseline is double left && candidate is double right ? right - left : null;

    private static double? FiniteOrNull(double? value)
        => value is double number && double.IsFinite(number) ? number : null;

    private sealed record TypedMetricProjection(double? Value, PerformanceMetricEvidence? Evidence);
}
