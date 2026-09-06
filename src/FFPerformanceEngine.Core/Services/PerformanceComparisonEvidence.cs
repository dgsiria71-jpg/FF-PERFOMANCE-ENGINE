using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public enum PerformanceEvidenceQuality
{
    Unavailable,
    Partial,
    Measured
}

public enum PerformanceComparisonValidationStatus
{
    Observed,
    PendingValidation,
    Validated
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
    public PerformanceConfigurationSnapshot? Configuration { get; init; }

    public static PerformanceEvidenceSnapshot Capture(
        string name,
        PerformanceIntervalSummary interval,
        DateTimeOffset capturedAt)
        => CaptureCore(name, interval, capturedAt, null);

    public static PerformanceEvidenceSnapshot Capture(
        string name,
        PerformanceIntervalSummary interval,
        DateTimeOffset capturedAt,
        PerformanceConfigurationSnapshot configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return CaptureCore(name, interval, capturedAt, configuration.Rehydrate());
    }

    private static PerformanceEvidenceSnapshot CaptureCore(
        string name,
        PerformanceIntervalSummary interval,
        DateTimeOffset capturedAt,
        PerformanceConfigurationSnapshot? configuration)
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
        double? averageFps = fps.Length == 0 ? null : fps.Average();
        double? averageFrameTime = frameTimes.Length == 0 ? null : frameTimes.Average();
        double? averageLatency = latency.Length == 0 ? null : latency.Average();

        var copiedInterval = interval with
        {
            TelemetrySamples = copiedPoints.Length,
            FpsEvidenceSamples = fps.Length,
            AverageFps = averageFps,
            AverageFrameTimeMs = averageFrameTime,
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
            AverageFps = averageFps,
            AverageFrameTimeMs = averageFrameTime,
            AverageLatencyMs = averageLatency,
            Configuration = configuration
        };
    }

    public static PerformanceEvidenceSnapshot Rehydrate(PerformanceEvidenceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var configuration = snapshot.Configuration?.Rehydrate();
        return configuration is null
            ? Capture(snapshot.Name, snapshot.Interval, snapshot.CapturedAt)
            : Capture(snapshot.Name, snapshot.Interval, snapshot.CapturedAt, configuration);
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

        var normalizedBaseline = PerformanceEvidenceSnapshot.Rehydrate(baseline);
        var normalizedCandidate = PerformanceEvidenceSnapshot.Rehydrate(candidate);
        return new PerformanceABComparison
        {
            Baseline = normalizedBaseline,
            Candidate = normalizedCandidate,
            Metrics = PerformanceIntervalAnalysis.Compare(normalizedBaseline.Interval, normalizedCandidate.Interval)
        };
    }
}

public sealed record PerformanceComparisonHistoryRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Label { get; init; } = string.Empty;
    public DateTimeOffset SavedAt { get; init; } = DateTimeOffset.UtcNow;
    public required PerformanceEvidenceSnapshot Baseline { get; init; }
    public required PerformanceEvidenceSnapshot Candidate { get; init; }
    public PerformanceComparisonValidationStatus ValidationStatus { get; init; } = PerformanceComparisonValidationStatus.Observed;
    public PerformanceEvidenceSnapshot? ValidationEvidence { get; init; }
    public DateTimeOffset? ValidatedAt { get; init; }

    public PerformanceIntervalComparison Metrics
        => PerformanceIntervalAnalysis.Compare(Baseline.Interval, Candidate.Interval);

    public bool CanOriginateProfile
        => ValidationStatus == PerformanceComparisonValidationStatus.Validated
           && ValidationEvidence?.Quality == PerformanceEvidenceQuality.Measured
           && Candidate.Configuration is { } candidateConfiguration
           && ValidationEvidence.Configuration is { } validationConfiguration
           && candidateConfiguration.IsEquivalentTo(validationConfiguration);

    public PerformanceABComparison AsComparison()
        => PerformanceABComparison.Create(Baseline, Candidate);

    public PerformanceComparisonHistoryRecord Rehydrate()
    {
        if (string.IsNullOrWhiteSpace(Label))
            throw new InvalidDataException("Historical comparison label is missing.");

        var baseline = PerformanceEvidenceSnapshot.Rehydrate(Baseline);
        var candidate = PerformanceEvidenceSnapshot.Rehydrate(Candidate);
        var validation = ValidationEvidence is null
            ? null
            : PerformanceEvidenceSnapshot.Rehydrate(ValidationEvidence);

        var status = ValidationStatus;
        var validatedAt = ValidatedAt;
        if (status == PerformanceComparisonValidationStatus.Validated
            && validation?.Quality != PerformanceEvidenceQuality.Measured)
        {
            status = PerformanceComparisonValidationStatus.PendingValidation;
            validatedAt = null;
            validation = null;
        }

        return this with
        {
            Label = Label.Trim(),
            Baseline = baseline,
            Candidate = candidate,
            ValidationStatus = status,
            ValidationEvidence = validation,
            ValidatedAt = validatedAt
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

    public PerformanceEvidenceSnapshot SetBaseline(
        string name,
        PerformanceIntervalSummary interval,
        PerformanceConfigurationSnapshot configuration)
    {
        var snapshot = PerformanceEvidenceSnapshot.Capture(name, interval, DateTimeOffset.UtcNow, configuration);
        lock (_gate) _baseline = snapshot;
        return snapshot;
    }

    public PerformanceEvidenceSnapshot SetCandidate(string name, PerformanceIntervalSummary interval)
    {
        var snapshot = PerformanceEvidenceSnapshot.Capture(name, interval, DateTimeOffset.UtcNow);
        lock (_gate) _candidate = snapshot;
        return snapshot;
    }

    public PerformanceEvidenceSnapshot SetCandidate(
        string name,
        PerformanceIntervalSummary interval,
        PerformanceConfigurationSnapshot configuration)
    {
        var snapshot = PerformanceEvidenceSnapshot.Capture(name, interval, DateTimeOffset.UtcNow, configuration);
        lock (_gate) _candidate = snapshot;
        return snapshot;
    }

    public PerformanceEvidenceSnapshot SetBaseline(PerformanceEvidenceSnapshot snapshot)
    {
        var normalized = PerformanceEvidenceSnapshot.Rehydrate(snapshot);
        lock (_gate) _baseline = normalized;
        return normalized;
    }

    public PerformanceEvidenceSnapshot SetCandidate(PerformanceEvidenceSnapshot snapshot)
    {
        var normalized = PerformanceEvidenceSnapshot.Rehydrate(snapshot);
        lock (_gate) _candidate = normalized;
        return normalized;
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
        var normalized = PerformanceEvidenceSnapshot.Rehydrate(snapshot);

        return new PerformanceProfileEvidenceProjection
        {
            SourceName = normalized.Name,
            Quality = normalized.Quality,
            Evidence = normalized.Quality == PerformanceEvidenceQuality.Unavailable
                ? EvidenceLevel.Unknown
                : EvidenceLevel.Observed,
            CapturedAt = normalized.CapturedAt,
            Start = normalized.Interval.Start,
            End = normalized.Interval.End,
            TelemetrySamples = normalized.TelemetrySamples,
            FpsEvidenceSamples = normalized.FpsEvidenceSamples,
            FrameTimeEvidenceSamples = normalized.FrameTimeEvidenceSamples,
            LatencyEvidenceSamples = normalized.LatencyEvidenceSamples,
            AverageFps = normalized.AverageFps,
            FrameTimeMs = normalized.AverageFrameTimeMs,
            LatencyMs = normalized.AverageLatencyMs
        };
    }

    public static PerformanceProfileEvidenceProjection FromValidatedRecord(PerformanceComparisonHistoryRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var normalized = record.Rehydrate();
        if (normalized.ValidationStatus != PerformanceComparisonValidationStatus.Validated
            || normalized.ValidationEvidence?.Quality != PerformanceEvidenceQuality.Measured)
            throw new InvalidOperationException("Historical comparison has not completed explicit measured validation.");

        return FromSnapshot(normalized.ValidationEvidence) with
        {
            SourceName = $"{normalized.Label} · validação",
            Evidence = EvidenceLevel.Validated
        };
    }
}
