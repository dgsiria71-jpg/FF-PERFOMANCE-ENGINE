using FFPerformanceEngine.Core.Diagnostics;

namespace FFPerformanceEngine.Core.SystemOptimization;

public enum CapabilityRecommendationPublicationDisposition
{
    Published,
    UnknownCapability,
    NotPersistent,
    Unavailable,
    UnsupportedSource,
    EnvironmentMismatch,
    LowConfidence,
    MissingAdapter,
    TargetRejected,
    InvalidRecommendation
}

public sealed record CapabilityRecommendationPublicationResult
{
    public CapabilityRecommendationPublicationDisposition Disposition { get; init; }
    public string CapabilityId { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;

    public bool IsPublished => Disposition == CapabilityRecommendationPublicationDisposition.Published;
}

public sealed record PersistentPcRecommendationCandidate(
    string CapabilityId,
    string TargetValue,
    CapabilityRecommendationSummary Recommendation);

public sealed record PersistentPcRecommendationBatchResult
{
    public bool IsPublished { get; init; }
    public string MachineFingerprintId { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public IReadOnlyList<CapabilityRecommendationPublicationResult> Results { get; init; }
        = Array.Empty<CapabilityRecommendationPublicationResult>();
}

/// <summary>
/// Sole automatic publication gate for recommendations consumed by the
/// persistent "Otimizar este PC" planner. It never chooses a value and never
/// mutates Windows. It only proves that an externally produced diagnostic or
/// evidence candidate belongs to the current machine and can be represented by
/// a concrete persistent mutation adapter before publishing it to the registry.
/// Batch publication validates the complete set first so a rejected candidate
/// cannot leave a mixed old/new recommendation state behind.
/// </summary>
public sealed class PersistentPcRecommendationCoordinator
{
    private readonly WindowsPerformanceCapabilityRegistry _registry;
    private readonly WindowsCapabilityMutationAdapterRegistry _adapters;
    private readonly double _minimumConfidence;

    public PersistentPcRecommendationCoordinator(
        WindowsPerformanceCapabilityRegistry registry,
        WindowsCapabilityMutationAdapterRegistry adapters,
        PersistentPcOptimizationPolicy? policy = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _adapters = adapters ?? throw new ArgumentNullException(nameof(adapters));
        var effectivePolicy = policy ?? new PersistentPcOptimizationPolicy();
        if (!double.IsFinite(effectivePolicy.MinimumRecommendationConfidence)
            || effectivePolicy.MinimumRecommendationConfidence is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(policy),
                "Minimum recommendation confidence must be a finite value between 0 and 1.");
        }

        _minimumConfidence = effectivePolicy.MinimumRecommendationConfidence;
    }

    public CapabilityRecommendationPublicationResult Publish(
        MachineContext machine,
        string capabilityId,
        string targetValue,
        CapabilityRecommendationSummary recommendation)
    {
        var result = Evaluate(machine, capabilityId, targetValue, recommendation);
        if (result.IsPublished)
            _registry.UpdateRecommendation(result.CapabilityId, targetValue, recommendation);
        return result;
    }

    public PersistentPcRecommendationBatchResult PublishBatch(
        MachineContext machine,
        IReadOnlyList<PersistentPcRecommendationCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(candidates);
        if (candidates.Count == 0)
            throw new ArgumentException("A recommendation batch requires at least one candidate.", nameof(candidates));

        EnsureUniqueCandidates(candidates);

        var evaluations = new CapabilityRecommendationPublicationResult[candidates.Count];
        for (var index = 0; index < candidates.Count; index++)
        {
            var candidate = candidates[index]
                ?? throw new ArgumentException("Recommendation batches cannot contain null candidates.", nameof(candidates));
            ArgumentNullException.ThrowIfNull(candidate.Recommendation);
            evaluations[index] = Evaluate(
                machine,
                candidate.CapabilityId,
                candidate.TargetValue,
                candidate.Recommendation);
        }

        var rejected = evaluations.FirstOrDefault(result => !result.IsPublished);
        if (rejected is not null)
        {
            return new PersistentPcRecommendationBatchResult
            {
                IsPublished = false,
                MachineFingerprintId = machine.Fingerprint.Id,
                Reason = $"Batch rejected before publication: {rejected.CapabilityId} · {rejected.Reason}",
                Results = evaluations
            };
        }

        // All gates have passed before the first registry write. UpdateRecommendation
        // repeats its own structural validation, preserving the registry as the final
        // descriptor authority without re-running Windows mutation logic.
        for (var index = 0; index < candidates.Count; index++)
        {
            var candidate = candidates[index];
            _registry.UpdateRecommendation(
                evaluations[index].CapabilityId,
                candidate.TargetValue,
                candidate.Recommendation);
        }

        return new PersistentPcRecommendationBatchResult
        {
            IsPublished = true,
            MachineFingerprintId = machine.Fingerprint.Id,
            Reason = $"Published {candidates.Count} persistent recommendation candidate(s) atomically after all gates passed.",
            Results = evaluations
        };
    }

    private CapabilityRecommendationPublicationResult Evaluate(
        MachineContext machine,
        string capabilityId,
        string targetValue,
        CapabilityRecommendationSummary recommendation)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(recommendation);

        var id = NormalizeId(capabilityId);
        var fingerprintId = machine.Fingerprint.Id?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(fingerprintId))
            throw new InvalidOperationException("Persistent recommendation publication requires a non-empty current machine fingerprint.");

        var capability = _registry.GetAll().FirstOrDefault(item =>
            string.Equals(item.CapabilityId, id, StringComparison.OrdinalIgnoreCase));
        if (capability is null)
            return Reject(CapabilityRecommendationPublicationDisposition.UnknownCapability, id,
                $"Unknown Windows performance capability '{id}'.");

        if (capability.PersistenceScope == CapabilityPersistenceScope.SessionOnly)
            return Reject(CapabilityRecommendationPublicationDisposition.NotPersistent, id,
                "Session-only capabilities cannot receive automatic persistent recommendations.");

        if (capability.Availability != CapabilityAvailability.Available)
            return Reject(CapabilityRecommendationPublicationDisposition.Unavailable, id,
                $"Capability runtime state is {capability.Availability}; publication is blocked until discovery proves it Available.");

        if (!IsAutomaticSource(recommendation.Source))
            return Reject(CapabilityRecommendationPublicationDisposition.UnsupportedSource, id,
                $"Recommendation source '{recommendation.Source}' is not authorized for automatic persistent publication.");

        if (!double.IsFinite(recommendation.Confidence)
            || recommendation.Confidence < 0
            || recommendation.Confidence > 1
            || string.IsNullOrWhiteSpace(recommendation.MachineFingerprintId)
            || recommendation.GeneratedAt is null)
        {
            return Reject(CapabilityRecommendationPublicationDisposition.InvalidRecommendation, id,
                "Recommendation provenance is incomplete or contains an invalid confidence value.");
        }

        if (recommendation.Confidence < _minimumConfidence)
            return Reject(CapabilityRecommendationPublicationDisposition.LowConfidence, id,
                $"Recommendation confidence {recommendation.Confidence:0.###} is below the {_minimumConfidence:0.###} persistent publication gate.");

        if (!string.Equals(
                recommendation.MachineFingerprintId.Trim(),
                fingerprintId,
                StringComparison.OrdinalIgnoreCase))
        {
            return Reject(CapabilityRecommendationPublicationDisposition.EnvironmentMismatch, id,
                "Recommendation belongs to a different machine/environment fingerprint.");
        }

        if (!_adapters.TryGet(id, out var adapter))
            return Reject(CapabilityRecommendationPublicationDisposition.MissingAdapter, id,
                "No concrete mutation adapter exists for this persistent recommendation.");

        if (string.IsNullOrWhiteSpace(targetValue))
            return Reject(CapabilityRecommendationPublicationDisposition.TargetRejected, id,
                "A persistent recommendation target cannot be empty.");

        var validation = adapter.Validate(targetValue, SystemOptimizationScope.Persistent);
        if (!validation.Success)
            return Reject(CapabilityRecommendationPublicationDisposition.TargetRejected, id,
                $"Concrete adapter rejected target '{targetValue}': {validation.Message}");

        return new CapabilityRecommendationPublicationResult
        {
            Disposition = CapabilityRecommendationPublicationDisposition.Published,
            CapabilityId = id,
            Reason = "Recommendation passed persistent machine, provenance, confidence and adapter validation gates."
        };
    }

    private static void EnsureUniqueCandidates(IReadOnlyList<PersistentPcRecommendationCandidate> candidates)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            if (candidate is null)
                throw new ArgumentException("Recommendation batches cannot contain null candidates.", nameof(candidates));
            var id = NormalizeId(candidate.CapabilityId);
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Every recommendation candidate requires a capability identity.", nameof(candidates));
            if (!seen.Add(id))
                throw new ArgumentException($"Duplicate recommendation capability '{id}' in the same batch.", nameof(candidates));
        }
    }

    private static bool IsAutomaticSource(CapabilityRecommendationSource source)
        => source is CapabilityRecommendationSource.Diagnostic
            or CapabilityRecommendationSource.ControlledEvidence
            or CapabilityRecommendationSource.ValidatedEvidence;

    private static CapabilityRecommendationPublicationResult Reject(
        CapabilityRecommendationPublicationDisposition disposition,
        string capabilityId,
        string reason)
        => new()
        {
            Disposition = disposition,
            CapabilityId = capabilityId,
            Reason = reason
        };

    private static string NormalizeId(string? capabilityId)
        => capabilityId?.Trim().ToLowerInvariant() ?? string.Empty;
}
