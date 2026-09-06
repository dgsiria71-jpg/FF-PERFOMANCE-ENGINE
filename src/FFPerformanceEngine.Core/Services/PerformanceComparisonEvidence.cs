using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public enum PerformanceEvidenceQuality
{
    Unavailable,
    Partial,
    Measured
}

public sealed record PerformanceEvidenceSnapshot
{
    public required string Name { get; init; }
    public DateTimeOffset CapturedAt { get; init; }
    public required PerformanceIntervalSummary Interval { get; init; }
    public PerformanceEvidenceQuality Quality { get; init; }
    public int TelemetrySamples { get; init; }
    public int FpsEvidenceSamples { get; init; }
    public int FrameTimeEvidenceSamples { get; init; }
    public int LatencyEvidenceSamples { get; init; }
    public double? AverageFps { get; init; }
    public double? AverageFrameTimeMs { get; init; }
    public double? AverageLatencyMs { get; init; }

    public static PerformanceEvidenceSnapshot Capture(
        string name,
        PerformanceIntervalSummary interval,
        DateTimeOffset capturedAt)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("A comparison snapshot name is required.", nameof(name));
        ArgumentNullException.ThrowIfNull(interval);

        var copiedPoints = interval.Points
            .Select(point => point with { })
            .ToArray();

        var fps = FiniteValues(copiedPoints.Select(point => point.Fps));
        var frameTimes = FiniteValues(copiedPoints.Select(point => point.FrameTimeMs));
        var latency = FiniteValues(copiedPoints.Select(point => point.LatencyMs));

        var copiedInterval = interval with
        {
            Points = Array.AsReadOnly(copiedPoints)
        };

        var hasFrameEvidence = fps.Length > 0 || frameTimes.Length > 0;
        var isFullyMeasured = copiedPoints.Length > 0
            && copiedPoints.All(point => string.Equals(point.DataQuality, "Measured", StringComparison.OrdinalIgnoreCase))
            && fps.Length == copiedPoints.Length
            && frameTimes.Length == copiedPoints.Length;

        return new PerformanceEvidenceSnapshot
        {
            Name = name.Trim(),
            CapturedAt = capturedAt,
            Interval = copiedInterval,
            Quality = !hasFrameEvidence
                ? PerformanceEvidenceQuality.Unavailable
                : isFullyMeasured
                    ? PerformanceEvidenceQuality.Measured
                    : PerformanceEvidenceQuality.Partial,
            TelemetrySamples = copiedPoints.Length,
            FpsEvidenceSamples = fps.Length,
            FrameTimeEvidenceSamples = frameTimes.Length,
            LatencyEvidenceSamples = latency.Length,
            AverageFps = fps.Length == 0 ? null : fps.Average(),
            AverageFrameTimeMs = frameTimes.Length == 0 ? null : frameTimes.Average(),
            AverageLatencyMs = latency.Length == 0 ? null : latency.Average()
        };
    }

    private static double[] FiniteValues(IEnumerable<double?> values)
        => values
            .Where(value => value is double number && double.IsFinite(number))
            .Select(value => value!.Value)
            .ToArray();
}

public sealed record PerformanceABComparison
{
    public required PerformanceEvidenceSnapshot Baseline { get; init; }
    public required PerformanceEvidenceSnapshot Candidate { get; init; }
    public required PerformanceIntervalComparison Metrics { get; init; }

    public static PerformanceABComparison Create(
        PerformanceEvidenceSnapshot baseline,
        PerformanceEvidenceSnapshot candidate)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(candidate);

        return new PerformanceABComparison
        {
            Baseline = baseline,
            Candidate = candidate,
            Metrics = PerformanceIntervalAnalysis.Compare(baseline.Interval, candidate.Interval)
        };
    }
}

public sealed class PerformanceComparisonSession
{
    private readonly object _gate = new();
    private PerformanceEvidenceSnapshot? _baseline;
    private PerformanceEvidenceSnapshot? _candidate;

    public PerformanceEvidenceSnapshot? Baseline
    {
        get { lock (_gate) return _baseline; }
    }

    public PerformanceEvidenceSnapshot? Candidate
    {
        get { lock (_gate) return _candidate; }
    }

    public PerformanceABComparison? CurrentComparison
    {
        get
        {
            lock (_gate)
            {
                return _baseline is not null && _candidate is not null
                    ? PerformanceABComparison.Create(_baseline, _candidate)
                    : null;
            }
        }
    }

    public PerformanceEvidenceSnapshot SetBaseline(string name, PerformanceIntervalSummary interval)
    {
        var snapshot = PerformanceEvidenceSnapshot.Capture(name, interval, DateTimeOffset.UtcNow);
        lock (_gate) _baseline = snapshot;
        return snapshot;
    }

    public PerformanceEvidenceSnapshot SetCandidate(string name, PerformanceIntervalSummary interval)
    {
        var snapshot = PerformanceEvidenceSnapshot.Capture(name, interval, DateTimeOffset.UtcNow);
        lock (_gate) _candidate = snapshot;
        return snapshot;
    }

    public void ClearBaseline()
    {
        lock (_gate) _baseline = null;
    }

    public void ClearCandidate()
    {
        lock (_gate) _candidate = null;
    }

    public void Clear()
    {
        lock (_gate)
        {
            _baseline = null;
            _candidate = null;
        }
    }
}

public sealed record PerformanceProfileEvidenceProjection
{
    public string SourceName { get; init; } = string.Empty;
    public PerformanceEvidenceQuality Quality { get; init; }
    public EvidenceLevel Evidence { get; init; } = EvidenceLevel.Unknown;
    public DateTimeOffset CapturedAt { get; init; }
    public DateTimeOffset Start { get; init; }
    public DateTimeOffset End { get; init; }
    public int TelemetrySamples { get; init; }
    public int FpsEvidenceSamples { get; init; }
    public int FrameTimeEvidenceSamples { get; init; }
    public int LatencyEvidenceSamples { get; init; }
    public double? AverageFps { get; init; }
    public double? FrameTimeMs { get; init; }
    public double? LatencyMs { get; init; }
}

public static class PerformanceProfileEvidenceBridge
{
    public static PerformanceProfileEvidenceProjection FromSnapshot(PerformanceEvidenceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new PerformanceProfileEvidenceProjection
        {
            SourceName = snapshot.Name,
            Quality = snapshot.Quality,
            Evidence = snapshot.Quality == PerformanceEvidenceQuality.Unavailable
                ? EvidenceLevel.Unknown
                : EvidenceLevel.Observed,
            CapturedAt = snapshot.CapturedAt,
            Start = snapshot.Interval.Start,
            End = snapshot.Interval.End,
            TelemetrySamples = snapshot.TelemetrySamples,
            FpsEvidenceSamples = snapshot.FpsEvidenceSamples,
            FrameTimeEvidenceSamples = snapshot.FrameTimeEvidenceSamples,
            LatencyEvidenceSamples = snapshot.LatencyEvidenceSamples,
            AverageFps = snapshot.AverageFps,
            FrameTimeMs = snapshot.AverageFrameTimeMs,
            LatencyMs = snapshot.AverageLatencyMs
        };
    }
}
