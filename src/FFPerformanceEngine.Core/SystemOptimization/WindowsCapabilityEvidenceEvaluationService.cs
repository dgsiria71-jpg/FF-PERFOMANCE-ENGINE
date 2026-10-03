namespace FFPerformanceEngine.Core.SystemOptimization;

public enum WindowsCapabilityEvidenceVerdict
{
    InsufficientEvidence,
    Beneficial,
    Regressive,
    Mixed,
    Inconclusive
}

public sealed record WindowsCapabilityEvidenceEvaluationPolicy
{
    public int MinimumRepeatedRounds { get; init; } = 2;
    public double MeaningfulRelativeEffect { get; init; } = 0.015;
    public double CrossMetricConflictThreshold { get; init; } = 0.02;
    public double MajorOpposingEffect { get; init; } = 0.05;
}

public sealed record WindowsCapabilityEvidenceEvaluation
{
    public string CapabilityId { get; init; } = string.Empty;
    public string BaselineValue { get; init; } = string.Empty;
    public string CandidateTarget { get; init; } = string.Empty;
    public string MachineFingerprintId { get; init; } = string.Empty;
    public string WorkloadKey { get; init; } = string.Empty;
    public int ObservationCount { get; init; }
    public WindowsCapabilityEvidenceVerdict Verdict { get; init; }
    public double Consistency { get; init; }
    public double? MeanFpsRelativeDelta { get; init; }
    public double? MeanFrameTimeRelativeImprovement { get; init; }
    public double? MeanLatencyRelativeImprovement { get; init; }
    public DateTimeOffset? LastObservedAt { get; init; }

    // Evidence interpretation is intentionally separated from recommendation
    // publication. A later validation/recommendation authority may consume this
    // result, but this layer never creates a target recommendation itself.
    public string? RecommendedValue => null;
}

/// <summary>
/// Interprets repeated controlled capability observations for one exact
/// capability/baseline/target/machine/workload tuple. The evaluator is
/// deliberately conservative: opposing rounds or materially opposing metrics
/// remain Mixed/Inconclusive instead of being averaged into a false winner.
/// </summary>
public sealed class WindowsCapabilityEvidenceEvaluationService
{
    private readonly WindowsCapabilityPerformanceCostMapService _costMap;
    private readonly WindowsCapabilityEvidenceEvaluationPolicy _policy;

    public WindowsCapabilityEvidenceEvaluationService(
        WindowsCapabilityPerformanceCostMapService costMap,
        WindowsCapabilityEvidenceEvaluationPolicy? policy = null)
    {
        _costMap = costMap ?? throw new ArgumentNullException(nameof(costMap));
        _policy = policy ?? new WindowsCapabilityEvidenceEvaluationPolicy();
        if (_policy.MinimumRepeatedRounds < 2)
            throw new ArgumentOutOfRangeException(nameof(policy), "Repeated capability evidence requires at least two controlled rounds.");
        if (_policy.MeaningfulRelativeEffect <= 0 || _policy.MeaningfulRelativeEffect >= 1)
            throw new ArgumentOutOfRangeException(nameof(policy), "Meaningful relative effect threshold must be between zero and one.");
        if (_policy.CrossMetricConflictThreshold <= 0 || _policy.CrossMetricConflictThreshold >= 1)
            throw new ArgumentOutOfRangeException(nameof(policy), "Cross-metric conflict threshold must be between zero and one.");
        if (_policy.MajorOpposingEffect <= 0 || _policy.MajorOpposingEffect >= 1)
            throw new ArgumentOutOfRangeException(nameof(policy), "Major opposing effect threshold must be between zero and one.");
    }

    public async Task<WindowsCapabilityEvidenceEvaluation> EvaluateAsync(
        string capabilityId,
        string baselineValue,
        string candidateTarget,
        string machineFingerprintId,
        string workloadKey,
        CancellationToken cancellationToken = default)
    {
        var id = NormalizeId(capabilityId);
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("A capability id is required.", nameof(capabilityId));
        if (string.IsNullOrWhiteSpace(baselineValue))
            throw new ArgumentException("A baseline value is required.", nameof(baselineValue));
        if (string.IsNullOrWhiteSpace(candidateTarget))
            throw new ArgumentException("A candidate target is required.", nameof(candidateTarget));
        if (string.IsNullOrWhiteSpace(machineFingerprintId))
            throw new ArgumentException("A machine fingerprint id is required.", nameof(machineFingerprintId));
        if (string.IsNullOrWhiteSpace(workloadKey))
            throw new ArgumentException("A workload key is required.", nameof(workloadKey));

        var fingerprint = machineFingerprintId.Trim();
        var workload = workloadKey.Trim();
        var observations = (await _costMap.LoadObservationsAsync(cancellationToken).ConfigureAwait(false))
            .Where(observation => string.Equals(observation.CapabilityId, id, StringComparison.OrdinalIgnoreCase)
                                  && string.Equals(observation.BaselineValue, baselineValue, StringComparison.Ordinal)
                                  && string.Equals(observation.CandidateTarget, candidateTarget, StringComparison.Ordinal)
                                  && string.Equals(observation.MachineFingerprintId, fingerprint, StringComparison.Ordinal)
                                  && string.Equals(observation.WorkloadKey, workload, StringComparison.Ordinal))
            .OrderBy(observation => observation.ObservedAt)
            .ToArray();

        var directions = observations.Select(ClassifyRound).ToArray();
        var verdict = EvaluateVerdict(directions);
        var dominantCount = directions.Length == 0
            ? 0
            : directions
                .GroupBy(direction => direction)
                .Max(group => group.Count());

        return new WindowsCapabilityEvidenceEvaluation
        {
            CapabilityId = id,
            BaselineValue = baselineValue,
            CandidateTarget = candidateTarget,
            MachineFingerprintId = fingerprint,
            WorkloadKey = workload,
            ObservationCount = observations.Length,
            Verdict = verdict,
            Consistency = observations.Length == 0 ? 0 : dominantCount / (double)observations.Length,
            MeanFpsRelativeDelta = Mean(observations.Select(observation => observation.FpsRelativeDelta)),
            MeanFrameTimeRelativeImprovement = Mean(observations.Select(observation => observation.FrameTimeRelativeImprovement)),
            MeanLatencyRelativeImprovement = Mean(observations.Select(observation => observation.LatencyRelativeImprovement)),
            LastObservedAt = observations.Length == 0 ? null : observations[^1].ObservedAt
        };
    }

    private WindowsCapabilityEvidenceVerdict EvaluateVerdict(IReadOnlyList<RoundDirection> directions)
    {
        if (directions.Count < _policy.MinimumRepeatedRounds)
            return WindowsCapabilityEvidenceVerdict.InsufficientEvidence;

        if (directions.All(direction => direction == RoundDirection.Beneficial))
            return WindowsCapabilityEvidenceVerdict.Beneficial;
        if (directions.All(direction => direction == RoundDirection.Regressive))
            return WindowsCapabilityEvidenceVerdict.Regressive;

        if (directions.Any(direction => direction == RoundDirection.Mixed)
            || (directions.Contains(RoundDirection.Beneficial)
                && directions.Contains(RoundDirection.Regressive)))
            return WindowsCapabilityEvidenceVerdict.Mixed;

        // A meaningful round mixed with an inconclusive round is not enough to
        // claim a stable direction. Preserve uncertainty until another controlled
        // round resolves it.
        return WindowsCapabilityEvidenceVerdict.Inconclusive;
    }

    private RoundDirection ClassifyRound(WindowsCapabilityCostObservation observation)
    {
        var effects = new[]
            {
                observation.FpsRelativeDelta,
                observation.FrameTimeRelativeImprovement,
                observation.LatencyRelativeImprovement
            }
            .Where(value => value is double finite && double.IsFinite(finite))
            .Select(value => value!.Value)
            .ToArray();

        if (effects.Length == 0) return RoundDirection.Inconclusive;

        var hasPositiveConflict = effects.Any(effect => effect >= _policy.CrossMetricConflictThreshold);
        var hasNegativeConflict = effects.Any(effect => effect <= -_policy.CrossMetricConflictThreshold);
        if (hasPositiveConflict && hasNegativeConflict)
            return RoundDirection.Mixed;

        var mean = effects.Average();
        if (mean >= _policy.MeaningfulRelativeEffect
            && effects.All(effect => effect > -_policy.MajorOpposingEffect))
            return RoundDirection.Beneficial;
        if (mean <= -_policy.MeaningfulRelativeEffect
            && effects.All(effect => effect < _policy.MajorOpposingEffect))
            return RoundDirection.Regressive;

        return RoundDirection.Inconclusive;
    }

    private static double? Mean(IEnumerable<double?> values)
    {
        var finite = values
            .Where(value => value is double number && double.IsFinite(number))
            .Select(value => value!.Value)
            .ToArray();
        return finite.Length == 0 ? null : finite.Average();
    }

    private static string NormalizeId(string? capabilityId)
        => capabilityId?.Trim().ToLowerInvariant() ?? string.Empty;

    private enum RoundDirection
    {
        Beneficial,
        Regressive,
        Mixed,
        Inconclusive
    }
}
