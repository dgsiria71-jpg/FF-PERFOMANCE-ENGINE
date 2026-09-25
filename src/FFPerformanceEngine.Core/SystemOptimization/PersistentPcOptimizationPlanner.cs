using FFPerformanceEngine.Core.Diagnostics;

namespace FFPerformanceEngine.Core.SystemOptimization;

public enum PersistentPcOptimizationDisposition
{
    Ready,
    NotPersistent,
    Unavailable,
    NoRecommendation,
    UnsupportedRecommendationSource,
    ManualOnly,
    EnvironmentMismatch,
    LowConfidence,
    MissingCurrentState,
    AlreadyAtTarget
}

public sealed record PersistentPcOptimizationPolicy
{
    public double MinimumRecommendationConfidence { get; init; } = 0.80;
}

public sealed record PersistentPcOptimizationPlanEntry
{
    public string CapabilityId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public PersistentPcOptimizationDisposition Disposition { get; init; }
    public string? ExpectedCurrentValue { get; init; }
    public string? TargetValue { get; init; }
    public CapabilityRecommendationSource RecommendationSource { get; init; } = CapabilityRecommendationSource.Unknown;
    public double RecommendationConfidence { get; init; }
    public CapabilityRiskLevel RiskLevel { get; init; } = CapabilityRiskLevel.Safe;
    public string Reason { get; init; } = string.Empty;

    public bool IsReady => Disposition == PersistentPcOptimizationDisposition.Ready;
}

public sealed record PersistentPcOptimizationPreview
{
    public Guid PlanId { get; init; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public string MachineFingerprintId { get; init; } = string.Empty;
    public IReadOnlyList<PersistentPcOptimizationPlanEntry> Entries { get; init; } = Array.Empty<PersistentPcOptimizationPlanEntry>();
    public IReadOnlyList<PersistentPcOptimizationPlanEntry> ReadyEntries { get; init; } = Array.Empty<PersistentPcOptimizationPlanEntry>();
    public IReadOnlyList<WindowsMutationRequest> Mutations { get; init; } = Array.Empty<WindowsMutationRequest>();

    public bool CanApply => Mutations.Count > 0;
}

/// <summary>
/// Produces a read-only persistent optimization preview. It never invents a
/// target value: only a recommendation already attached to the capability,
/// bound to the same machine fingerprint and carrying adequate confidence,
/// can become a mutation candidate.
/// </summary>
public sealed class PersistentPcOptimizationPlanner
{
    private readonly PersistentPcOptimizationPolicy _policy;

    public PersistentPcOptimizationPlanner(PersistentPcOptimizationPolicy? policy = null)
    {
        _policy = policy ?? new PersistentPcOptimizationPolicy();
        if (!double.IsFinite(_policy.MinimumRecommendationConfidence)
            || _policy.MinimumRecommendationConfidence is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(policy),
                "Minimum recommendation confidence must be a finite value between 0 and 1.");
        }
    }

    public PersistentPcOptimizationPreview BuildPreview(MachineContext machine)
    {
        ArgumentNullException.ThrowIfNull(machine);
        var fingerprintId = machine.Fingerprint.Id?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(fingerprintId))
            throw new InvalidOperationException("Persistent PC optimization requires a non-empty machine fingerprint.");

        var entries = machine.Capabilities
            .OrderBy(capability => capability.CapabilityId, StringComparer.OrdinalIgnoreCase)
            .Select(capability => Evaluate(capability, fingerprintId))
            .ToArray();
        var ready = entries.Where(entry => entry.IsReady).ToArray();
        var mutations = ready
            .Select(entry => new WindowsMutationRequest(entry.CapabilityId, entry.TargetValue!))
            .ToArray();

        return new PersistentPcOptimizationPreview
        {
            PlanId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            MachineFingerprintId = fingerprintId,
            Entries = entries,
            ReadyEntries = ready,
            Mutations = mutations
        };
    }

    private PersistentPcOptimizationPlanEntry Evaluate(
        WindowsPerformanceCapability capability,
        string fingerprintId)
    {
        ArgumentNullException.ThrowIfNull(capability);
        var recommendation = capability.Recommendation ?? new CapabilityRecommendationSummary();
        var target = string.IsNullOrWhiteSpace(capability.RecommendedValue)
            ? null
            : capability.RecommendedValue.Trim();
        var current = capability.CurrentValue;

        if (capability.PersistenceScope == CapabilityPersistenceScope.SessionOnly)
            return Entry(capability, PersistentPcOptimizationDisposition.NotPersistent, current, target,
                "Capability is session-only and cannot be part of Otimizar este PC.");

        if (capability.Availability != CapabilityAvailability.Available)
            return Entry(capability, PersistentPcOptimizationDisposition.Unavailable, current, target,
                $"Capability runtime state is {capability.Availability}; persistent mutation is blocked.");

        if (target is null)
            return Entry(capability, PersistentPcOptimizationDisposition.NoRecommendation, current, null,
                "No explicit recommended value is attached to this capability.");

        if (recommendation.Source == CapabilityRecommendationSource.Manual)
            return Entry(capability, PersistentPcOptimizationDisposition.ManualOnly, current, target,
                "Manual/Expert recommendation is not eligible for automatic persistent optimization.");

        if (!IsAutomaticRecommendationSource(recommendation.Source))
            return Entry(capability, PersistentPcOptimizationDisposition.UnsupportedRecommendationSource, current, target,
                "Recommendation provenance is not eligible for automatic persistent optimization. ControlledEvidence must complete PendingValidation and a fresh validation challenge before becoming ValidatedEvidence.");

        if (!string.Equals(
                recommendation.MachineFingerprintId?.Trim(),
                fingerprintId,
                StringComparison.OrdinalIgnoreCase))
        {
            return Entry(capability, PersistentPcOptimizationDisposition.EnvironmentMismatch, current, target,
                "Recommendation belongs to a different machine/environment fingerprint.");
        }

        if (!double.IsFinite(recommendation.Confidence)
            || recommendation.Confidence < _policy.MinimumRecommendationConfidence)
        {
            return Entry(capability, PersistentPcOptimizationDisposition.LowConfidence, current, target,
                $"Recommendation confidence {recommendation.Confidence:0.###} is below the {_policy.MinimumRecommendationConfidence:0.###} policy gate.");
        }

        if (current is null)
            return Entry(capability, PersistentPcOptimizationDisposition.MissingCurrentState, null, target,
                "Current capability state was not proven, so preview-to-apply drift cannot be checked safely.");

        if (string.Equals(current, target, StringComparison.Ordinal))
            return Entry(capability, PersistentPcOptimizationDisposition.AlreadyAtTarget, current, target,
                "Capability is already at the recommended state.");

        return Entry(capability, PersistentPcOptimizationDisposition.Ready, current, target,
            "Validated evidence/diagnostic recommendation is eligible for a persistent reversible transaction.");
    }

    private static bool IsAutomaticRecommendationSource(CapabilityRecommendationSource source)
        => source is CapabilityRecommendationSource.Diagnostic
            or CapabilityRecommendationSource.ValidatedEvidence;

    private static PersistentPcOptimizationPlanEntry Entry(
        WindowsPerformanceCapability capability,
        PersistentPcOptimizationDisposition disposition,
        string? current,
        string? target,
        string reason)
        => new()
        {
            CapabilityId = capability.CapabilityId?.Trim().ToLowerInvariant() ?? string.Empty,
            Name = capability.Name ?? string.Empty,
            Disposition = disposition,
            ExpectedCurrentValue = current,
            TargetValue = target,
            RecommendationSource = capability.Recommendation?.Source ?? CapabilityRecommendationSource.Unknown,
            RecommendationConfidence = capability.Recommendation?.Confidence ?? 0,
            RiskLevel = capability.RiskLevel,
            Reason = reason
        };
}
