namespace FFPerformanceEngine.Core.Workloads;

public sealed record ResolvedGameCatalogEntry
{
    public required GameIdentity Identity { get; init; }
    public required IGameAdapter Adapter { get; init; }
}

public sealed record ResolvedGameCatalogResult
{
    public IReadOnlyList<ResolvedGameCatalogEntry> Games { get; init; }
        = Array.Empty<ResolvedGameCatalogEntry>();
    public IReadOnlyList<GameDiscoveryWarning> Warnings { get; init; }
        = Array.Empty<GameDiscoveryWarning>();
    public IReadOnlyList<BoundGameEvidence> BoundEvidence { get; init; }
        = Array.Empty<BoundGameEvidence>();
    public IReadOnlyList<UnboundGameEvidence> UnboundEvidence { get; init; }
        = Array.Empty<UnboundGameEvidence>();
}

/// <summary>
/// Explicit application-facing discovery operation. Construction is side-effect
/// free; only DiscoverAsync runs configured sources. Stable identity discovery and
/// adapter resolution always run first. When an evidence plane is configured, its
/// non-authoritative observations run afterward and may bind only to identities
/// already proven by the catalog.
/// </summary>
public sealed class GameDiscoveryCoordinator
{
    private readonly LocalGameCatalogService _catalog;
    private readonly GameAdapterResolver _adapters;
    private readonly GameEvidenceCatalogService? _evidenceCatalog;
    private readonly GameEvidenceBinder? _evidenceBinder;

    public GameDiscoveryCoordinator(
        LocalGameCatalogService catalog,
        GameAdapterResolver adapters)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _adapters = adapters ?? throw new ArgumentNullException(nameof(adapters));
    }

    public GameDiscoveryCoordinator(
        LocalGameCatalogService catalog,
        GameAdapterResolver adapters,
        GameEvidenceCatalogService evidenceCatalog,
        GameEvidenceBinder evidenceBinder)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _adapters = adapters ?? throw new ArgumentNullException(nameof(adapters));
        _evidenceCatalog = evidenceCatalog ?? throw new ArgumentNullException(nameof(evidenceCatalog));
        _evidenceBinder = evidenceBinder ?? throw new ArgumentNullException(nameof(evidenceBinder));
    }

    public async Task<ResolvedGameCatalogResult> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var catalog = await _catalog.DiscoverAsync(cancellationToken).ConfigureAwait(false);
        var games = catalog.Games
            .Select(identity => new ResolvedGameCatalogEntry
            {
                Identity = identity,
                Adapter = _adapters.Resolve(identity)
            })
            .ToArray();

        GameEvidenceBindingResult bindings = new();
        IReadOnlyList<GameDiscoveryWarning> warnings = catalog.Warnings;

        if (_evidenceCatalog is not null && _evidenceBinder is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var evidence = await _evidenceCatalog.DiscoverAsync(cancellationToken).ConfigureAwait(false);
            bindings = _evidenceBinder.Bind(catalog.Games, evidence.Observations);
            warnings = catalog.Warnings
                .Concat(evidence.Warnings)
                .OrderBy(item => item.SourceId, StringComparer.Ordinal)
                .ThenBy(item => item.Message, StringComparer.Ordinal)
                .ToArray();
        }

        return new ResolvedGameCatalogResult
        {
            Games = games,
            Warnings = warnings,
            BoundEvidence = bindings.BoundEvidence,
            UnboundEvidence = bindings.UnboundEvidence
        };
    }
}
