namespace FFPerformanceEngine.Core.Workloads;

/// <summary>
/// Associates non-authoritative evidence with already-proven stable game identities.
/// The binder never creates or mutates GameIdentity instances. Explicit GameId hints
/// have strict precedence; otherwise only unique, directory-boundary-safe install
/// path containment may bind an executable observation.
/// </summary>
public sealed class GameEvidenceBinder
{
    public GameEvidenceBindingResult Bind(
        IReadOnlyList<GameIdentity> games,
        IReadOnlyList<GameEvidenceSourceObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(games);
        ArgumentNullException.ThrowIfNull(observations);

        var bound = new List<BoundGameEvidence>();
        var unbound = new List<UnboundGameEvidence>();

        var gamesById = games
            .Where(game => game is not null && NormalizeGameId(game.GameId).Length > 0)
            .GroupBy(game => NormalizeGameId(game.GameId), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() == 1)
            .ToDictionary(
                group => group.Key,
                group => group.Single(),
                StringComparer.OrdinalIgnoreCase);

        foreach (var evidence in observations
                     .Where(item => item is not null && item.Observation is not null)
                     .OrderBy(item => item.SourceId, StringComparer.Ordinal)
                     .ThenBy(item => item.Observation.ObservationId, StringComparer.Ordinal))
        {
            var hint = NormalizeGameId(evidence.Observation.GameIdHint);
            if (hint.Length > 0)
            {
                if (gamesById.TryGetValue(hint, out var hinted))
                {
                    AddBound(
                        NormalizeGameId(hinted.GameId),
                        GameEvidenceBindingReason.ExactGameIdHint,
                        evidence,
                        bound);
                }
                else
                {
                    AddUnbound(
                        GameEvidenceUnboundReason.NoMatchingIdentity,
                        evidence,
                        unbound);
                }

                continue;
            }

            var rawExecutable = evidence.Observation.ExecutablePath;
            if (string.IsNullOrWhiteSpace(rawExecutable))
            {
                AddUnbound(
                    GameEvidenceUnboundReason.UnsupportedEvidence,
                    evidence,
                    unbound);
                continue;
            }

            var executable = NormalizeExecutablePath(rawExecutable);
            if (executable is null)
            {
                AddUnbound(
                    GameEvidenceUnboundReason.InvalidExecutablePath,
                    evidence,
                    unbound);
                continue;
            }

            var matches = games
                .Where(game => game is not null
                               && game.InstallPaths.Any(root => ContainsExecutable(root, executable)))
                .Select(game => NormalizeGameId(game.GameId))
                .Where(id => id.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();

            if (matches.Length == 0)
            {
                AddUnbound(
                    GameEvidenceUnboundReason.NoMatchingIdentity,
                    evidence,
                    unbound);
            }
            else if (matches.Length > 1)
            {
                AddUnbound(
                    GameEvidenceUnboundReason.AmbiguousInstallPath,
                    evidence,
                    unbound);
            }
            else
            {
                AddBound(
                    matches[0],
                    GameEvidenceBindingReason.UniqueInstallPathContainment,
                    evidence,
                    bound);
            }
        }

        return new GameEvidenceBindingResult
        {
            BoundEvidence = bound
                .OrderBy(item => item.SourceId, StringComparer.Ordinal)
                .ThenBy(item => item.Observation.ObservationId, StringComparer.Ordinal)
                .ThenBy(item => item.GameId, StringComparer.Ordinal)
                .ToArray(),
            UnboundEvidence = unbound
                .OrderBy(item => item.SourceId, StringComparer.Ordinal)
                .ThenBy(item => item.Observation.ObservationId, StringComparer.Ordinal)
                .ToArray()
        };
    }

    private static void AddBound(
        string gameId,
        GameEvidenceBindingReason reason,
        GameEvidenceSourceObservation evidence,
        ICollection<BoundGameEvidence> target)
        => target.Add(new BoundGameEvidence
        {
            GameId = gameId,
            BindingReason = reason,
            SourceId = evidence.SourceId,
            Priority = evidence.Priority,
            Observation = evidence.Observation
        });

    private static void AddUnbound(
        GameEvidenceUnboundReason reason,
        GameEvidenceSourceObservation evidence,
        ICollection<UnboundGameEvidence> target)
        => target.Add(new UnboundGameEvidence
        {
            UnboundReason = reason,
            SourceId = evidence.SourceId,
            Priority = evidence.Priority,
            Observation = evidence.Observation
        });

    private static string NormalizeGameId(string? value)
        => value?.Trim().ToLowerInvariant() ?? string.Empty;

    private static string? NormalizeExecutablePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Path.IsPathFullyQualified(value))
            return null;

        try
        {
            return Path.GetFullPath(value.Trim())
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private static string? NormalizeInstallRoot(string? value)
    {
        var normalized = NormalizeExecutablePath(value);
        return normalized?.TrimEnd(Path.DirectorySeparatorChar);
    }

    private static bool ContainsExecutable(string installRoot, string executable)
    {
        var root = NormalizeInstallRoot(installRoot);
        if (string.IsNullOrWhiteSpace(root)) return false;

        var prefix = root + Path.DirectorySeparatorChar;
        return executable.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
}
