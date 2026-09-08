namespace FFPerformanceEngine.Core.Workloads;

public sealed record ResolvedGameCatalogEntry
{
    public required GameIdentity Identity { get; init; }
    public required IGameAdapter Adapter { get; init; }
}

public sealed record ResolvedGameCatalogResult
{
    public IReadOnlyList<ResolvedGameCatalogEntry> Games { get; init; } = Array.Empty<ResolvedGameCatalogEntry>();
    public IReadOnlyList<GameDiscoveryWarning> Warnings { get; init; } = Array.Empty<GameDiscoveryWarning>();
}

/// <summary>
/// Explicit application-facing discovery operation. Construction is side-effect
/// free; only DiscoverAsync runs catalog sources. Every resulting GameIdentity is
/// then bound to the single shared adapter resolver, preserving the catalog's
/// deterministic GameId ordering and warnings.
/// </summary>
public sealed class GameDiscoveryCoordinator
{
    private readonly LocalGameCatalogService _catalog;
    private readonly GameAdapterResolver _adapters;

    public GameDiscoveryCoordinator(
        LocalGameCatalogService catalog,
        GameAdapterResolver adapters)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _adapters = adapters ?? throw new ArgumentNullException(nameof(adapters));
    }

    public async Task<ResolvedGameCatalogResult> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        var catalog = await _catalog.DiscoverAsync(cancellationToken).ConfigureAwait(false);
        var games = catalog.Games
            .Select(identity => new ResolvedGameCatalogEntry
            {
                Identity = identity,
                Adapter = _adapters.Resolve(identity)
            })
            .ToArray();

        return new ResolvedGameCatalogResult
        {
            Games = games,
            Warnings = catalog.Warnings
        };
    }
}
