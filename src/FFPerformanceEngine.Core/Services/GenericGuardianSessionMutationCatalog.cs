using FFPerformanceEngine.Core.SystemOptimization;

namespace FFPerformanceEngine.Core.Services;

/// <summary>
/// One explicit action-to-mutation declaration from the owning Core composition.
/// This is configuration authority only, never evidence that the action helps.
/// No generic production mappings are inferred or installed by default.
/// </summary>
public sealed record GenericGuardianSessionMutationDefinition(
    string GameId,
    GuardianAnomalyKind Family,
    string ActionId,
    WindowsMutationRequest Mutation);

/// <summary>
/// Fail-closed, immutable mapping scoped to stable game, anomaly family and
/// action identity. The caller cannot substitute a different Windows capability,
/// target value or expected-state precondition after the mapping is created.
/// Registration provenance must be established by the future trusted host.
/// </summary>
public sealed class GenericGuardianSessionMutationCatalog
{
    private readonly Dictionary<(string GameId, GuardianAnomalyKind Family, string ActionId), WindowsMutationRequest> _entries = new();

    public GenericGuardianSessionMutationCatalog(IEnumerable<GenericGuardianSessionMutationDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        foreach (var definition in definitions)
        {
            ArgumentNullException.ThrowIfNull(definition);
            ArgumentNullException.ThrowIfNull(definition.Mutation);
            if (string.IsNullOrWhiteSpace(definition.GameId)
                || string.IsNullOrWhiteSpace(definition.ActionId)
                || string.IsNullOrWhiteSpace(definition.Mutation.CapabilityId)
                || definition.Mutation.TargetValue is null)
                throw new ArgumentException("Every Guardian action mapping requires explicit game, action, capability and target value.", nameof(definitions));

            var key = (Normalize(definition.GameId), definition.Family, Normalize(definition.ActionId));
            // Snapshot the caller's definition; do not rely on a mutable collection.
            var mutation = new WindowsMutationRequest(
                definition.Mutation.CapabilityId.Trim(),
                definition.Mutation.TargetValue,
                definition.Mutation.ExpectedCurrentValue);
            if (!_entries.TryAdd(key, mutation))
                throw new ArgumentException("Duplicate Guardian action mapping; ambiguous mutation authority is forbidden.", nameof(definitions));
        }
    }

    public bool IsAuthorized(GenericGuardianSessionActionCandidate? candidate, WindowsMutationRequest? requested)
    {
        if (candidate?.Action is null || requested is null
            || string.IsNullOrWhiteSpace(candidate.GameId)
            || string.IsNullOrWhiteSpace(candidate.Action.Id)
            || string.IsNullOrWhiteSpace(requested.CapabilityId)
            || requested.TargetValue is null)
            return false;

        return _entries.TryGetValue(
                   (Normalize(candidate.GameId), candidate.Family, Normalize(candidate.Action.Id)),
                   out var registered)
               && string.Equals(registered.CapabilityId, requested.CapabilityId.Trim(), StringComparison.OrdinalIgnoreCase)
               && string.Equals(registered.TargetValue, requested.TargetValue, StringComparison.Ordinal)
               && string.Equals(registered.ExpectedCurrentValue, requested.ExpectedCurrentValue, StringComparison.Ordinal);
    }

    public bool TryBind(
        GenericGuardianSessionActionCandidate? candidate,
        out GenericGuardianWindowsSessionActionBinding? binding)
    {
        binding = null;
        if (candidate?.Action is null
            || string.IsNullOrWhiteSpace(candidate.GameId)
            || string.IsNullOrWhiteSpace(candidate.Action.Id)
            || !_entries.TryGetValue(
                (Normalize(candidate.GameId), candidate.Family, Normalize(candidate.Action.Id)),
                out var mutation))
            return false;

        binding = new GenericGuardianWindowsSessionActionBinding
        {
            Candidate = candidate,
            Mutation = mutation
        };
        return true;
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();
}
