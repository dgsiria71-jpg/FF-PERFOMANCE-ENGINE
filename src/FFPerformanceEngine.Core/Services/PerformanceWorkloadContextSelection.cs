using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Workloads;

namespace FFPerformanceEngine.Core.Services;

/// <summary>
/// Application-owned selection seam for universal performance evidence.
/// The selector never runs discovery and never derives identity from runtime
/// process evidence. It only accepts an already-resolved Track 3 catalog plus
/// one stable GameId explicitly chosen by the caller.
/// </summary>
public sealed class PerformanceWorkloadContextSelection
{
    private readonly object _gate = new();
    private ResolvedGameCatalogResult? _selectedCatalog;
    private string? _selectedGameId;
    private IReadOnlyList<string> _relevantCapabilityIds = Array.Empty<string>();

    public string? SelectedGameId
    {
        get
        {
            lock (_gate) return _selectedGameId;
        }
    }

    public bool TrySelect(
        ResolvedGameCatalogResult catalog,
        string? requestedGameId,
        IEnumerable<string>? relevantCapabilityIds = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var requested = NormalizeId(requestedGameId);
        var matches = string.IsNullOrWhiteSpace(requested)
            ? Array.Empty<ResolvedGameCatalogEntry>()
            : (catalog.Games ?? Array.Empty<ResolvedGameCatalogEntry>())
                .Where(entry => entry?.Identity is not null
                                && string.Equals(
                                    NormalizeId(entry.Identity.GameId),
                                    requested,
                                    StringComparison.Ordinal))
                .ToArray();

        if (matches.Length != 1)
        {
            Clear();
            return false;
        }

        var match = matches[0];
        var canonicalGameId = NormalizeId(match.Identity.GameId);
        var resolvedAdapterId = NormalizeId(match.Adapter?.AdapterId);
        if (string.IsNullOrWhiteSpace(canonicalGameId)
            || string.IsNullOrWhiteSpace(resolvedAdapterId))
        {
            Clear();
            return false;
        }

        var capabilityIds = (relevantCapabilityIds ?? Array.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(NormalizeId)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        var selectedCatalog = new ResolvedGameCatalogResult
        {
            Games =
            [
                new ResolvedGameCatalogEntry
                {
                    Identity = match.Identity with { GameId = canonicalGameId },
                    Adapter = match.Adapter
                }
            ]
        };

        lock (_gate)
        {
            _selectedCatalog = selectedCatalog;
            _selectedGameId = canonicalGameId;
            _relevantCapabilityIds = Array.AsReadOnly(capabilityIds);
        }

        return true;
    }

    public PerformanceUniversalConfigurationContext? Capture(MachineContext machine)
    {
        ArgumentNullException.ThrowIfNull(machine);

        ResolvedGameCatalogResult? catalog;
        string? gameId;
        IReadOnlyList<string> relevantCapabilityIds;
        lock (_gate)
        {
            catalog = _selectedCatalog;
            gameId = _selectedGameId;
            relevantCapabilityIds = _relevantCapabilityIds;
        }

        return catalog is null || string.IsNullOrWhiteSpace(gameId)
            ? null
            : PerformanceUniversalConfigurationContext.Capture(
                machine,
                catalog,
                gameId,
                relevantCapabilityIds);
    }

    public void Clear()
    {
        lock (_gate)
        {
            _selectedCatalog = null;
            _selectedGameId = null;
            _relevantCapabilityIds = Array.Empty<string>();
        }
    }

    private static string NormalizeId(string? value)
        => value?.Trim().ToLowerInvariant() ?? string.Empty;
}
