using FFPerformanceEngine.Core.Services;

namespace FFPerformanceEngine.Core.SystemOptimization;

public enum WindowsCapabilityCostEvidenceMaturity
{
    Observed,
    Repeated
}

public sealed record WindowsCapabilityCostObservation
{
    public int SchemaVersion { get; init; } = 1;
    public Guid Id { get; init; } = Guid.NewGuid();
    public string CapabilityId { get; init; } = string.Empty;
    public string BaselineValue { get; init; } = string.Empty;
    public string CandidateTarget { get; init; } = string.Empty;
    public string MachineFingerprintId { get; init; } = string.Empty;
    public string WorkloadKey { get; init; } = string.Empty;
    public Guid TransactionId { get; init; }
    public Guid RestorePointId { get; init; }
    public DateTimeOffset ObservedAt { get; init; }
    public int BaselineSamples { get; init; }
    public int CandidateSamples { get; init; }
    public double? BaselineAverageFps { get; init; }
    public double? CandidateAverageFps { get; init; }
    public double? FpsDelta { get; init; }
    public double? FpsRelativeDelta { get; init; }
    public double? BaselineAverageFrameTimeMs { get; init; }
    public double? CandidateAverageFrameTimeMs { get; init; }
    public double? FrameTimeDeltaMs { get; init; }
    public double? FrameTimeRelativeImprovement { get; init; }
    public double? BaselineAverageLatencyMs { get; init; }
    public double? CandidateAverageLatencyMs { get; init; }
    public double? LatencyDeltaMs { get; init; }
    public double? LatencyRelativeImprovement { get; init; }
}

public sealed record WindowsCapabilityCostSummary
{
    public string CapabilityId { get; init; } = string.Empty;
    public string BaselineValue { get; init; } = string.Empty;
    public string CandidateTarget { get; init; } = string.Empty;
    public string MachineFingerprintId { get; init; } = string.Empty;
    public string WorkloadKey { get; init; } = string.Empty;
    public int ObservationCount { get; init; }
    public WindowsCapabilityCostEvidenceMaturity Maturity { get; init; }
    public DateTimeOffset? LastObservedAt { get; init; }
    public double? MeanFpsRelativeDelta { get; init; }
    public double? MeanFrameTimeRelativeImprovement { get; init; }
    public double? MeanLatencyRelativeImprovement { get; init; }

    // Cost-map evidence is deliberately not a recommendation authority.
    public string? RecommendedValue => null;
}

public sealed class WindowsCapabilityCostMapCollection
{
    public List<WindowsCapabilityCostObservation> Observations { get; init; } = new();
}

/// <summary>
/// Local measured knowledge store for controlled Windows capability A/B rounds.
/// It records raw and normalized effects for an exact machine/workload/target
/// tuple. Repetition only raises evidence maturity from Observed to Repeated;
/// this service never selects a winner or publishes RecommendedValue.
/// </summary>
public sealed class WindowsCapabilityPerformanceCostMapService
{
    private const int MaxObservations = 5000;

    private readonly JsonStore<WindowsCapabilityCostMapCollection> _store;
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public WindowsCapabilityPerformanceCostMapService(string? path = null)
        => _store = new JsonStore<WindowsCapabilityCostMapCollection>(
            path ?? Path.Combine(AppPaths.Root, "windows-capability-cost-map.json"));

    public async Task<WindowsCapabilityCostObservation> RecordAsync(
        WindowsCapabilityControlledBenchmarkResult result,
        string machineFingerprintId,
        string workloadKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);
        var capabilityId = NormalizeId(result.CapabilityId);
        if (string.IsNullOrWhiteSpace(capabilityId))
            throw new ArgumentException("A Windows capability cost observation requires a capability id.", nameof(result));
        if (string.IsNullOrWhiteSpace(result.BaselineValue))
            throw new ArgumentException("A Windows capability cost observation requires a proven baseline value.", nameof(result));
        if (string.IsNullOrWhiteSpace(result.CandidateTarget))
            throw new ArgumentException("A Windows capability cost observation requires a candidate target.", nameof(result));
        if (string.IsNullOrWhiteSpace(machineFingerprintId))
            throw new ArgumentException("A Windows capability cost observation requires a machine fingerprint id.", nameof(machineFingerprintId));
        if (string.IsNullOrWhiteSpace(workloadKey))
            throw new ArgumentException("A Windows capability cost observation requires a workload key.", nameof(workloadKey));

        var comparison = PerformanceABComparison.Create(
            result.Comparison.Baseline,
            result.Comparison.Candidate);
        EnsureControlledMeasured(comparison.Baseline, "baseline");
        EnsureControlledMeasured(comparison.Candidate, "candidate");

        var observation = new WindowsCapabilityCostObservation
        {
            CapabilityId = capabilityId,
            BaselineValue = result.BaselineValue,
            CandidateTarget = result.CandidateTarget,
            MachineFingerprintId = machineFingerprintId.Trim(),
            WorkloadKey = workloadKey.Trim(),
            TransactionId = result.TransactionId,
            RestorePointId = result.RestorePointId,
            ObservedAt = comparison.Candidate.CapturedAt >= comparison.Baseline.CapturedAt
                ? comparison.Candidate.CapturedAt
                : comparison.Baseline.CapturedAt,
            BaselineSamples = comparison.Baseline.TelemetrySamples,
            CandidateSamples = comparison.Candidate.TelemetrySamples,
            BaselineAverageFps = comparison.Baseline.AverageFps,
            CandidateAverageFps = comparison.Candidate.AverageFps,
            FpsDelta = Delta(comparison.Baseline.AverageFps, comparison.Candidate.AverageFps),
            FpsRelativeDelta = RelativeGain(comparison.Baseline.AverageFps, comparison.Candidate.AverageFps),
            BaselineAverageFrameTimeMs = comparison.Baseline.AverageFrameTimeMs,
            CandidateAverageFrameTimeMs = comparison.Candidate.AverageFrameTimeMs,
            FrameTimeDeltaMs = Delta(comparison.Baseline.AverageFrameTimeMs, comparison.Candidate.AverageFrameTimeMs),
            FrameTimeRelativeImprovement = RelativeImprovement(
                comparison.Baseline.AverageFrameTimeMs,
                comparison.Candidate.AverageFrameTimeMs),
            BaselineAverageLatencyMs = comparison.Baseline.AverageLatencyMs,
            CandidateAverageLatencyMs = comparison.Candidate.AverageLatencyMs,
            LatencyDeltaMs = Delta(comparison.Baseline.AverageLatencyMs, comparison.Candidate.AverageLatencyMs),
            LatencyRelativeImprovement = RelativeImprovement(
                comparison.Baseline.AverageLatencyMs,
                comparison.Candidate.AverageLatencyMs)
        };

        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var data = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
            data.Observations.Add(observation);
            if (data.Observations.Count > MaxObservations)
            {
                data.Observations.RemoveRange(
                    0,
                    data.Observations.Count - MaxObservations);
            }
            await _store.SaveAsync(data, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeGate.Release();
        }

        return observation;
    }

    public async Task<IReadOnlyList<WindowsCapabilityCostObservation>> LoadObservationsAsync(
        CancellationToken cancellationToken = default)
        => (await _store.LoadAsync(cancellationToken).ConfigureAwait(false))
            .Observations
            .OrderByDescending(observation => observation.ObservedAt)
            .ToArray();

    public async Task<WindowsCapabilityCostSummary?> GetSummaryAsync(
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

        var data = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
        var matching = data.Observations
            .Where(observation => string.Equals(observation.CapabilityId, id, StringComparison.OrdinalIgnoreCase)
                                  && string.Equals(observation.BaselineValue, baselineValue, StringComparison.Ordinal)
                                  && string.Equals(observation.CandidateTarget, candidateTarget, StringComparison.Ordinal)
                                  && string.Equals(observation.MachineFingerprintId, machineFingerprintId.Trim(), StringComparison.Ordinal)
                                  && string.Equals(observation.WorkloadKey, workloadKey.Trim(), StringComparison.Ordinal))
            .OrderBy(observation => observation.ObservedAt)
            .ToArray();
        if (matching.Length == 0) return null;

        return new WindowsCapabilityCostSummary
        {
            CapabilityId = id,
            BaselineValue = baselineValue,
            CandidateTarget = candidateTarget,
            MachineFingerprintId = machineFingerprintId.Trim(),
            WorkloadKey = workloadKey.Trim(),
            ObservationCount = matching.Length,
            Maturity = matching.Length >= 2
                ? WindowsCapabilityCostEvidenceMaturity.Repeated
                : WindowsCapabilityCostEvidenceMaturity.Observed,
            LastObservedAt = matching[^1].ObservedAt,
            MeanFpsRelativeDelta = Mean(matching.Select(observation => observation.FpsRelativeDelta)),
            MeanFrameTimeRelativeImprovement = Mean(matching.Select(observation => observation.FrameTimeRelativeImprovement)),
            MeanLatencyRelativeImprovement = Mean(matching.Select(observation => observation.LatencyRelativeImprovement))
        };
    }

    private static void EnsureControlledMeasured(PerformanceEvidenceSnapshot snapshot, string side)
    {
        if (snapshot.Quality != PerformanceEvidenceQuality.Measured || snapshot.TelemetrySamples < 2)
        {
            throw new InvalidOperationException(
                $"Windows capability Performance Cost Map accepts only repeated fully measured controlled {side} evidence.");
        }
    }

    private static double? Delta(double? baseline, double? candidate)
        => baseline is double left
           && candidate is double right
           && double.IsFinite(left)
           && double.IsFinite(right)
            ? right - left
            : null;

    private static double? RelativeGain(double? baseline, double? candidate)
        => baseline is double left
           && candidate is double right
           && double.IsFinite(left)
           && double.IsFinite(right)
           && left > 0
            ? (right - left) / left
            : null;

    private static double? RelativeImprovement(double? baseline, double? candidate)
        => baseline is double left
           && candidate is double right
           && double.IsFinite(left)
           && double.IsFinite(right)
           && left > 0
            ? (left - right) / left
            : null;

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
}
