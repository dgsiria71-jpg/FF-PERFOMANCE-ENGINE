using FFPerformanceEngine.Core.Diagnostics;

namespace FFPerformanceEngine.Core.SystemOptimization;

public sealed record WindowsCapabilityExperimentRunResult
{
    public required WindowsCapabilityControlledBenchmarkResult Benchmark { get; init; }
    public required WindowsCapabilityCostObservation Observation { get; init; }
    public required WindowsCapabilityCostSummary CostSummary { get; init; }
    public required WindowsCapabilityEvidenceEvaluation Evaluation { get; init; }
    public required WindowsCapabilityValidationDecision Validation { get; init; }
}

/// <summary>
/// Coordinates one Windows capability experiment without becoming a recommendation
/// authority. Capability discovery is refreshed immediately before planning and
/// again immediately before execution so stale candidates are rejected before any
/// controlled mutation/measurement. Only restored controlled A/B results are stored
/// in the local Performance Cost Map. Repeated evidence is evaluated and may advance
/// only as far as PendingValidation; this coordinator never publishes a recommendation.
/// </summary>
public sealed class WindowsCapabilityExperimentCoordinator
{
    private readonly Func<CancellationToken, Task<IReadOnlyList<WindowsPerformanceCapability>>> _refreshCapabilities;
    private readonly WindowsCapabilityCandidatePlanner _candidatePlanner;
    private readonly Func<WindowsCapabilityCandidate, CancellationToken, Task<WindowsCapabilityControlledBenchmarkResult>> _runBenchmark;
    private readonly WindowsCapabilityPerformanceCostMapService _costMap;
    private readonly Func<string> _machineFingerprintIdProvider;
    private readonly Func<string> _workloadKeyProvider;
    private readonly WindowsCapabilityEvidenceEvaluationService _evidenceEvaluator;
    private readonly WindowsCapabilityValidationGate _validationGate;

    public WindowsCapabilityExperimentCoordinator(
        Func<CancellationToken, Task<IReadOnlyList<WindowsPerformanceCapability>>> refreshCapabilities,
        WindowsCapabilityCandidatePlanner candidatePlanner,
        Func<WindowsCapabilityCandidate, CancellationToken, Task<WindowsCapabilityControlledBenchmarkResult>> runBenchmark,
        WindowsCapabilityPerformanceCostMapService costMap,
        Func<string> machineFingerprintIdProvider,
        Func<string> workloadKeyProvider,
        WindowsCapabilityEvidenceEvaluationService? evidenceEvaluator = null,
        WindowsCapabilityValidationGate? validationGate = null)
    {
        _refreshCapabilities = refreshCapabilities ?? throw new ArgumentNullException(nameof(refreshCapabilities));
        _candidatePlanner = candidatePlanner ?? throw new ArgumentNullException(nameof(candidatePlanner));
        _runBenchmark = runBenchmark ?? throw new ArgumentNullException(nameof(runBenchmark));
        _costMap = costMap ?? throw new ArgumentNullException(nameof(costMap));
        _machineFingerprintIdProvider = machineFingerprintIdProvider ?? throw new ArgumentNullException(nameof(machineFingerprintIdProvider));
        _workloadKeyProvider = workloadKeyProvider ?? throw new ArgumentNullException(nameof(workloadKeyProvider));
        _evidenceEvaluator = evidenceEvaluator ?? new WindowsCapabilityEvidenceEvaluationService(costMap);
        _validationGate = validationGate ?? new WindowsCapabilityValidationGate();
    }

    public async Task<WindowsCapabilityCandidatePlan> PlanAsync(
        string capabilityId,
        CancellationToken cancellationToken = default)
    {
        var id = NormalizeId(capabilityId);
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("A Windows capability experiment requires a capability id.", nameof(capabilityId));

        var capabilities = await _refreshCapabilities(cancellationToken).ConfigureAwait(false);
        var capability = capabilities.FirstOrDefault(item =>
            string.Equals(NormalizeId(item.CapabilityId), id, StringComparison.Ordinal));
        if (capability is null)
            throw new KeyNotFoundException($"Windows capability '{id}' was not found after runtime discovery refresh.");

        return _candidatePlanner.Build(capability);
    }

    public async Task<WindowsCapabilityExperimentRunResult> RunAsync(
        WindowsCapabilityCandidate candidate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var capabilityId = NormalizeId(candidate.CapabilityId);
        if (string.IsNullOrWhiteSpace(capabilityId))
            throw new ArgumentException("A Windows capability experiment requires a capability id.", nameof(candidate));
        if (string.IsNullOrWhiteSpace(candidate.TargetValue))
            throw new ArgumentException("A Windows capability experiment requires a candidate target.", nameof(candidate));

        // Refresh + re-plan immediately before the controlled run. The candidate
        // received by callers is only a proposal from a prior snapshot and must
        // still exist in the currently supported exploration space.
        var currentPlan = await PlanAsync(capabilityId, cancellationToken).ConfigureAwait(false);
        var currentCandidate = currentPlan.Candidates.FirstOrDefault(item =>
            string.Equals(NormalizeId(item.CapabilityId), capabilityId, StringComparison.Ordinal)
            && string.Equals(item.TargetValue, candidate.TargetValue, StringComparison.Ordinal));
        if (currentCandidate is null)
        {
            throw new InvalidOperationException(
                $"Windows capability candidate '{capabilityId}={candidate.TargetValue}' is stale or no longer in the currently supported exploration space.");
        }

        var machineFingerprintId = _machineFingerprintIdProvider()?.Trim();
        if (string.IsNullOrWhiteSpace(machineFingerprintId))
            throw new InvalidOperationException("Windows capability experiment requires a current machine fingerprint identity.");
        var workloadKey = _workloadKeyProvider()?.Trim();
        if (string.IsNullOrWhiteSpace(workloadKey))
            throw new InvalidOperationException("Windows capability experiment requires a current workload identity.");

        var benchmark = await _runBenchmark(currentCandidate, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(NormalizeId(benchmark.CapabilityId), capabilityId, StringComparison.Ordinal)
            || !string.Equals(benchmark.CandidateTarget, currentCandidate.TargetValue, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Controlled Windows benchmark returned evidence for a different capability or target than the freshly validated experiment candidate.");
        }

        var observation = await _costMap.RecordAsync(
            benchmark,
            machineFingerprintId,
            workloadKey,
            cancellationToken).ConfigureAwait(false);
        var summary = await _costMap.GetSummaryAsync(
            benchmark.CapabilityId,
            benchmark.BaselineValue,
            benchmark.CandidateTarget,
            machineFingerprintId,
            workloadKey,
            cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Windows capability Performance Cost Map did not retain the just-recorded controlled observation.");

        var evaluation = await _evidenceEvaluator.EvaluateAsync(
            benchmark.CapabilityId,
            benchmark.BaselineValue,
            benchmark.CandidateTarget,
            machineFingerprintId,
            workloadKey,
            cancellationToken).ConfigureAwait(false);
        var validation = _validationGate.Evaluate(evaluation);

        return new WindowsCapabilityExperimentRunResult
        {
            Benchmark = benchmark,
            Observation = observation,
            CostSummary = summary,
            Evaluation = evaluation,
            Validation = validation
        };
    }

    private static string NormalizeId(string? capabilityId)
        => capabilityId?.Trim().ToLowerInvariant() ?? string.Empty;
}
