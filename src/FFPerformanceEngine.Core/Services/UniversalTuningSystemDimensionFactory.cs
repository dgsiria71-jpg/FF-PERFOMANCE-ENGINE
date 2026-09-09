using FFPerformanceEngine.Core.SystemOptimization;

namespace FFPerformanceEngine.Core.Services;

/// <summary>
/// Projects an already-authorized Windows capability candidate plan into the
/// neutral universal tuning search-space contract. The factory does not discover
/// capabilities, generate candidate values, mutate Windows or publish recommendations.
/// </summary>
public static class UniversalTuningSystemDimensionFactory
{
    public static UniversalTuningDimension? FromWindowsCandidatePlan(
        WindowsCapabilityCandidatePlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (!plan.CanExplore) return null;

        var capabilityId = plan.CapabilityId?.Trim().ToLowerInvariant() ?? string.Empty;
        if (capabilityId.Length == 0)
            throw new ArgumentException("A Ready Windows capability candidate plan requires a stable capability id.", nameof(plan));

        var ordered = plan.Candidates
            .OrderBy(candidate => candidate.ExplorationRank)
            .ToArray();

        var values = new List<string>(ordered.Length);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var candidate in ordered)
        {
            if (!seen.Add(candidate.TargetValue))
                throw new ArgumentException(
                    $"Windows capability candidate plan '{capabilityId}' contains duplicate target value '{candidate.TargetValue}'.",
                    nameof(plan));
            values.Add(candidate.TargetValue);
        }

        return new UniversalTuningDimension
        {
            Id = capabilityId,
            Scope = UniversalTuningDimensionScope.System,
            AuthorityId = capabilityId,
            CandidateValues = values.ToArray()
        };
    }
}
