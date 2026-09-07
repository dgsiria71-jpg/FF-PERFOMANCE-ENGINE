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

/// <summary>
/// Sole automatic publication gate for recommendations consumed by the
/// persistent "Otimizar este PC" planner. It never chooses a value and never
/// mutates Windows. It only proves that an externally produced diagnostic or
/// evidence candidate belongs to the current machine and can be represented by
/// a concrete persistent mutation adapter before publishing it to the registry.
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

        _registry.UpdateRecommendation(id, targetValue, recommendation);
        return new CapabilityRecommendationPublicationResult
        {
            Disposition = CapabilityRecommendationPublicationDisposition.Published,
            CapabilityId = id,
            Reason = "Recommendation passed persistent machine, provenance, confidence and adapter validation gates."
        };
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
