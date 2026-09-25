using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Workloads;

public enum GameLauncherKind
{
    Unknown,
    Standalone,
    Steam,
    Epic,
    Riot,
    BattleNet,
    EA,
    Ubisoft,
    Xbox,
    BlueStacks,
    Emulator
}

public enum GameEngineKind
{
    Unknown,
    Source,
    Unreal,
    Unity,
    Custom,
    AndroidEmulated
}

public sealed record GameDiscoverySourceEvidence
{
    public string SourceId { get; init; } = string.Empty;
    public int Priority { get; init; }
    public double Confidence { get; init; }
    public string Evidence { get; init; } = string.Empty;
}

/// <summary>
/// Stable, launcher-neutral identity for one installed/discovered game or
/// interactive workload. Existing GameKind values remain a compatibility bridge;
/// new discovery and adapter code binds to GameId instead of extending that enum.
/// </summary>
public sealed record GameIdentity
{
    public string GameId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public GameLauncherKind Launcher { get; init; } = GameLauncherKind.Unknown;
    public GameEngineKind Engine { get; init; } = GameEngineKind.Unknown;
    public IReadOnlyList<string> Executables { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> InstallPaths { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> AuxiliaryProcesses { get; init; } = Array.Empty<string>();
    public string AdapterId { get; init; } = "generic";
    public GameKind? LegacyGameKind { get; init; }
    public IReadOnlyList<GameDiscoverySourceEvidence> Sources { get; init; } = Array.Empty<GameDiscoverySourceEvidence>();
}

public sealed record GameDiscoveryCandidate
{
    public required GameIdentity Identity { get; init; }
    public double Confidence { get; init; }
    public string Evidence { get; init; } = string.Empty;
}

public interface IGameDiscoverySource
{
    string SourceId { get; }
    int Priority { get; }

    Task<IReadOnlyList<GameDiscoveryCandidate>> DiscoverAsync(
        CancellationToken cancellationToken = default);
}

public sealed record GameDiscoveryWarning
{
    public string SourceId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public sealed record LocalGameCatalogDiscoveryResult
{
    public IReadOnlyList<GameIdentity> Games { get; init; } = Array.Empty<GameIdentity>();
    public IReadOnlyList<GameDiscoveryWarning> Warnings { get; init; } = Array.Empty<GameDiscoveryWarning>();
}

/// <summary>
/// Aggregates independent discovery sources into one deterministic local catalog.
/// Identity equality is intentionally based only on the stable GameId. Shared
/// executable names, install folders or launcher processes are evidence, never an
/// identity key, so unrelated games cannot be merged accidentally.
/// </summary>
public sealed class LocalGameCatalogService
{
    private readonly IReadOnlyList<IGameDiscoverySource> _sources;

    public LocalGameCatalogService(IEnumerable<IGameDiscoverySource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        _sources = sources
            .Where(source => source is not null)
            .OrderByDescending(source => source.Priority)
            .ThenBy(source => NormalizeSourceId(source.SourceId), StringComparer.Ordinal)
            .ToArray();
    }

    public async Task<LocalGameCatalogDiscoveryResult> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        var observations = new List<CandidateObservation>();
        var warnings = new List<GameDiscoveryWarning>();

        foreach (var source in _sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sourceId = NormalizeSourceId(source.SourceId);
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                warnings.Add(new GameDiscoveryWarning
                {
                    SourceId = "unknown-source",
                    Message = "A discovery source has no stable SourceId and was skipped."
                });
                continue;
            }

            IReadOnlyList<GameDiscoveryCandidate> candidates;
            try
            {
                candidates = await source.DiscoverAsync(cancellationToken).ConfigureAwait(false)
                    ?? Array.Empty<GameDiscoveryCandidate>();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                warnings.Add(new GameDiscoveryWarning
                {
                    SourceId = sourceId,
                    Message = ex.Message
                });
                continue;
            }

            foreach (var candidate in candidates)
            {
                if (candidate?.Identity is null) continue;
                var gameId = NormalizeGameId(candidate.Identity.GameId);
                if (string.IsNullOrWhiteSpace(gameId)) continue;

                observations.Add(new CandidateObservation(
                    sourceId,
                    source.Priority,
                    NormalizeConfidence(candidate.Confidence),
                    candidate.Evidence?.Trim() ?? string.Empty,
                    NormalizeIdentity(candidate.Identity, gameId)));
            }
        }

        var games = observations
            .GroupBy(observation => observation.Identity.GameId, StringComparer.OrdinalIgnoreCase)
            .Select(MergeIdentity)
            .OrderBy(game => game.GameId, StringComparer.Ordinal)
            .ToArray();

        return new LocalGameCatalogDiscoveryResult
        {
            Games = games,
            Warnings = warnings
                .OrderBy(warning => warning.SourceId, StringComparer.Ordinal)
                .ThenBy(warning => warning.Message, StringComparer.Ordinal)
                .ToArray()
        };
    }

    private static GameIdentity MergeIdentity(IGrouping<string, CandidateObservation> group)
    {
        var ordered = group
            .OrderByDescending(observation => observation.Priority)
            .ThenByDescending(observation => observation.Confidence)
            .ThenBy(observation => observation.SourceId, StringComparer.Ordinal)
            .ToArray();

        var canonical = ordered[0].Identity;
        var launcher = ordered
            .Select(observation => observation.Identity.Launcher)
            .FirstOrDefault(value => value != GameLauncherKind.Unknown);
        var engine = ordered
            .Select(observation => observation.Identity.Engine)
            .FirstOrDefault(value => value != GameEngineKind.Unknown);
        var adapterId = ordered
            .Select(observation => observation.Identity.AdapterId?.Trim())
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)
                                     && !string.Equals(value, "generic", StringComparison.OrdinalIgnoreCase))
            ?? "generic";
        var legacyGameKind = ordered
            .Select(observation => observation.Identity.LegacyGameKind)
            .FirstOrDefault(value => value.HasValue);

        return new GameIdentity
        {
            GameId = canonical.GameId,
            Name = ordered
                .Select(observation => observation.Identity.Name?.Trim())
                .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name))
                ?? canonical.GameId,
            Launcher = launcher,
            Engine = engine,
            Executables = UnionStrings(ordered.SelectMany(observation => observation.Identity.Executables)),
            InstallPaths = UnionStrings(ordered.SelectMany(observation => observation.Identity.InstallPaths)),
            AuxiliaryProcesses = UnionStrings(ordered.SelectMany(observation => observation.Identity.AuxiliaryProcesses)),
            AdapterId = adapterId,
            LegacyGameKind = legacyGameKind,
            Sources = ordered
                .Select(observation => new GameDiscoverySourceEvidence
                {
                    SourceId = observation.SourceId,
                    Priority = observation.Priority,
                    Confidence = observation.Confidence,
                    Evidence = observation.Evidence
                })
                .GroupBy(source => source.SourceId, StringComparer.OrdinalIgnoreCase)
                .Select(grouped => grouped
                    .OrderByDescending(source => source.Priority)
                    .ThenByDescending(source => source.Confidence)
                    .First())
                .OrderByDescending(source => source.Priority)
                .ThenBy(source => source.SourceId, StringComparer.Ordinal)
                .ToArray()
        };
    }

    private static GameIdentity NormalizeIdentity(GameIdentity identity, string gameId)
        => new()
        {
            GameId = gameId,
            Name = identity.Name?.Trim() ?? string.Empty,
            Launcher = identity.Launcher,
            Engine = identity.Engine,
            Executables = UnionStrings(identity.Executables),
            InstallPaths = UnionStrings(identity.InstallPaths),
            AuxiliaryProcesses = UnionStrings(identity.AuxiliaryProcesses),
            AdapterId = string.IsNullOrWhiteSpace(identity.AdapterId)
                ? "generic"
                : identity.AdapterId.Trim().ToLowerInvariant(),
            LegacyGameKind = identity.LegacyGameKind,
            Sources = Array.Empty<GameDiscoverySourceEvidence>()
        };

    private static IReadOnlyList<string> UnionStrings(IEnumerable<string>? values)
        => (values ?? Array.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(value => value, StringComparer.Ordinal)
            .ToArray();

    private static string NormalizeGameId(string? value)
        => value?.Trim().ToLowerInvariant() ?? string.Empty;

    private static string NormalizeSourceId(string? value)
        => value?.Trim().ToLowerInvariant() ?? string.Empty;

    private static double NormalizeConfidence(double value)
        => double.IsFinite(value) ? Math.Clamp(value, 0, 1) : 0;

    private sealed record CandidateObservation(
        string SourceId,
        int Priority,
        double Confidence,
        string Evidence,
        GameIdentity Identity);
}

/// <summary>
/// Compatibility seam from the current Free Fire/BlueStacks product into the
/// neutral Track 3 GameIdentity model. Existing GameKind behavior is preserved
/// while specialized adapters can be selected by stable AdapterId.
/// </summary>
public static class LegacyGameIdentityBridge
{
    public static GameIdentity? FromGameKind(GameKind gameKind)
        => gameKind switch
        {
            GameKind.FreeFire => Create(
                GameKind.FreeFire,
                "garena.free-fire",
                "Free Fire",
                "bluestacks.free-fire"),
            GameKind.FreeFireMax => Create(
                GameKind.FreeFireMax,
                "garena.free-fire-max",
                "Free Fire MAX",
                "bluestacks.free-fire-max"),
            _ => null
        };

    private static GameIdentity Create(
        GameKind legacyKind,
        string gameId,
        string name,
        string adapterId)
        => new()
        {
            GameId = gameId,
            Name = name,
            Launcher = GameLauncherKind.BlueStacks,
            Engine = GameEngineKind.AndroidEmulated,
            Executables = ["HD-Player.exe"],
            AuxiliaryProcesses = ["HD-MultiInstanceManager.exe"],
            AdapterId = adapterId,
            LegacyGameKind = legacyKind,
            Sources =
            [
                new GameDiscoverySourceEvidence
                {
                    SourceId = "legacy-bluestacks",
                    Priority = 100,
                    Confidence = 1.0,
                    Evidence = "Existing specialized Free Fire/BlueStacks integration"
                }
            ]
        };
}
