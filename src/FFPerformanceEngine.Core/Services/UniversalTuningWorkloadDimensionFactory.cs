using FFPerformanceEngine.Core.Workloads;

namespace FFPerformanceEngine.Core.Services;

/// <summary>
/// Projects workload-specific tuning declarations from the authoritative resolved
/// Game Adapter into the neutral Track 5 search-space model. This factory is pure:
/// it performs no discovery, configuration mutation, benchmark or recommendation.
/// </summary>
public sealed class UniversalTuningWorkloadDimensionFactory
{
    private readonly GameAdapterResolver _resolver;

    public UniversalTuningWorkloadDimensionFactory(GameAdapterResolver resolver)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public IReadOnlyList<UniversalTuningDimension> Build(GameIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        var adapter = _resolver.Resolve(identity);
        if (adapter.IsGeneric || adapter is not IGameTuningDimensionProvider provider)
            return Array.Empty<UniversalTuningDimension>();

        var capabilities = adapter.Capabilities;
        if (!capabilities.ConfigDiscovery
            || !capabilities.ConfigSnapshot
            || !capabilities.ConfigMutation
            || !capabilities.Rollback)
        {
            // Fail closed before consulting provider metadata. An adapter that
            // cannot discover, snapshot, mutate and roll back its configuration
            // cannot contribute explorable game-config dimensions yet.
            return Array.Empty<UniversalTuningDimension>();
        }

        var adapterId = NormalizeIdentity(adapter.AdapterId);
        if (adapterId.Length == 0)
            throw new ArgumentException("Resolved game adapter requires a stable non-empty AdapterId.", nameof(identity));

        var declarations = provider.GetTuningDimensions(identity)
            ?? throw new InvalidOperationException(
                $"Game adapter '{adapterId}' returned null tuning dimension declarations.");
        if (declarations.Count == 0)
            return Array.Empty<UniversalTuningDimension>();

        var localIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var accepted = new List<UniversalTuningDimension>(declarations.Count);

        foreach (var declaration in declarations)
        {
            if (declaration is null)
                throw new ArgumentException(
                    $"Game adapter '{adapterId}' returned a null tuning dimension declaration.",
                    nameof(identity));

            var localId = NormalizeIdentity(declaration.Id);
            if (localId.Length == 0)
                throw new ArgumentException(
                    $"Game adapter '{adapterId}' returned a tuning dimension without a stable local id.",
                    nameof(identity));
            if (!localIds.Add(localId))
                throw new ArgumentException(
                    $"Game adapter '{adapterId}' returned duplicate tuning dimension id '{localId}'.",
                    nameof(identity));

            if (declaration.CandidateValues is null || declaration.CandidateValues.Count == 0)
                throw new ArgumentException(
                    $"Game adapter '{adapterId}' tuning dimension '{localId}' requires at least one explicit candidate value.",
                    nameof(identity));

            var values = new List<string>(declaration.CandidateValues.Count);
            var seenValues = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in declaration.CandidateValues)
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException(
                        $"Game adapter '{adapterId}' tuning dimension '{localId}' contains a blank candidate value.",
                        nameof(identity));
                if (!seenValues.Add(value))
                    throw new ArgumentException(
                        $"Game adapter '{adapterId}' tuning dimension '{localId}' contains duplicate candidate value '{value}'.",
                        nameof(identity));
                values.Add(value);
            }

            accepted.Add(new UniversalTuningDimension
            {
                Id = $"workload.{adapterId}.{localId}",
                Scope = UniversalTuningDimensionScope.Workload,
                AuthorityId = adapterId,
                CandidateValues = values.ToArray()
            });
        }

        return accepted
            .OrderBy(dimension => dimension.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static string NormalizeIdentity(string? value)
        => value?.Trim().ToLowerInvariant() ?? string.Empty;
}
