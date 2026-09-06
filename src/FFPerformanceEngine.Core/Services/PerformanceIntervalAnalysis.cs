namespace FFPerformanceEngine.Core.Services;

public sealed record PerformanceTimelinePoint
{
    public DateTimeOffset Timestamp { get; init; }
    public double? Fps { get; init; }
    public double? FrameTimeMs { get; init; }
    public double? LatencyMs { get; init; }
    public string DataQuality { get; init; } = string.Empty;
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
        var telemetry = window
            .Where(entry => entry.Kind == PerformanceTimelineKind.Telemetry && entry.Telemetry is not null)
            .Select(entry => entry.Telemetry!)
            .ToArray();
        var fpsValues = telemetry
            .Where(sample => sample.Fps is double value && double.IsFinite(value))
            .Select(sample => sample.Fps!.Value)
            .ToArray();
        var frameTimes = telemetry
            .Where(sample => sample.FrameTimeMs is double value && double.IsFinite(value))
            .Select(sample => sample.FrameTimeMs!.Value)
            .ToArray();

        return new PerformanceIntervalSummary
        {
            Start = start,
            End = end,
            TelemetrySamples = telemetry.Length,
            FpsEvidenceSamples = fpsValues.Length,
            GuardianEvents = window.Count(entry => entry.Kind == PerformanceTimelineKind.Guardian),
            UserMarkers = window.Count(entry => entry.Kind == PerformanceTimelineKind.UserMarker),
            AverageFps = fpsValues.Length == 0 ? null : fpsValues.Average(),
            AverageFrameTimeMs = frameTimes.Length == 0 ? null : frameTimes.Average(),
            Points = telemetry.Select(sample => new PerformanceTimelinePoint
            {
                Timestamp = sample.Timestamp,
                Fps = FiniteOrNull(sample.Fps),
                FrameTimeMs = FiniteOrNull(sample.FrameTimeMs),
                LatencyMs = FiniteOrNull(sample.LatencyMs),
                DataQuality = sample.DataQuality
            }).ToArray()
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

    private static double? Delta(double? baseline, double? candidate)
        => baseline is double left && candidate is double right ? right - left : null;

    private static double? FiniteOrNull(double? value)
        => value is double number && double.IsFinite(number) ? number : null;
}
