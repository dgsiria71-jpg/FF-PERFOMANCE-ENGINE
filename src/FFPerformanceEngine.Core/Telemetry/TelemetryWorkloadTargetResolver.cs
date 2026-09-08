using FFPerformanceEngine.Core.Workloads;

namespace FFPerformanceEngine.Core.Telemetry;

public enum TelemetryWorkloadBindingQuality
{
    SystemOnly,
    ExactRunningProcess,
    AmbiguousRunningProcess,
    UnavailableRunningProcess,
    UnknownGame
}

public sealed record TelemetryWorkloadTarget
{
    public string? GameId { get; init; }
    public int? ProcessId { get; init; }
    public string? ExecutablePath { get; init; }
    public TelemetryWorkloadBindingQuality BindingQuality { get; init; }

    public bool CanCaptureProcess
        => BindingQuality == TelemetryWorkloadBindingQuality.ExactRunningProcess
           && ProcessId is > 0
           && !string.IsNullOrWhiteSpace(ExecutablePath);

    public static TelemetryWorkloadTarget SystemOnly { get; } = new()
    {
        BindingQuality = TelemetryWorkloadBindingQuality.SystemOnly
    };
}

public sealed class TelemetryWorkloadTargetResolver
{
    public TelemetryWorkloadTarget Resolve(
        ResolvedGameCatalogResult catalog,
        string? requestedGameId)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        if (string.IsNullOrWhiteSpace(requestedGameId))
            return UnknownGame();

        var requested = requestedGameId.Trim();
        var matches = (catalog.Games ?? Array.Empty<ResolvedGameCatalogEntry>())
            .Where(entry => entry?.Identity is not null
                            && !string.IsNullOrWhiteSpace(entry.Identity.GameId)
                            && string.Equals(
                                entry.Identity.GameId.Trim(),
                                requested,
                                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matches.Length != 1)
            return UnknownGame();

        var canonicalGameId = matches[0].Identity.GameId.Trim();
        var valid = new List<ProcessPathEvidence>();

        foreach (var bound in catalog.BoundEvidence ?? Array.Empty<BoundGameEvidence>())
        {
            if (bound?.Observation is null) continue;
            if (!string.Equals(bound.GameId?.Trim(), canonicalGameId, StringComparison.OrdinalIgnoreCase)) continue;
            if (bound.Observation.Kind != GameEvidenceKind.RunningProcess) continue;
            if (bound.Observation.ProcessId is not int processId || processId <= 0) continue;
            if (!TryNormalizePath(bound.Observation.ExecutablePath, out var executablePath)) continue;

            valid.Add(new ProcessPathEvidence(processId, executablePath));
        }

        if (valid.Count == 0)
            return Unavailable(canonicalGameId);

        var pidGroups = valid
            .GroupBy(item => item.ProcessId)
            .OrderBy(group => group.Key)
            .Select(group => new ProcessEvidenceGroup(
                group.Key,
                group.Select(item => item.ExecutablePath)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(path => path, StringComparer.Ordinal)
                    .ToArray()))
            .ToArray();

        if (pidGroups.Any(group => group.Paths.Count != 1))
            return Ambiguous(canonicalGameId);

        if (pidGroups.Length != 1)
            return Ambiguous(canonicalGameId);

        var exact = pidGroups[0];
        return new TelemetryWorkloadTarget
        {
            GameId = canonicalGameId,
            ProcessId = exact.ProcessId,
            ExecutablePath = exact.Paths[0],
            BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess
        };
    }

    private static bool TryNormalizePath(string? path, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(path)) return false;

        var candidate = path.Trim();
        try
        {
            if (!Path.IsPathFullyQualified(candidate)) return false;
            normalized = Path.GetFullPath(candidate);
            return !string.IsNullOrWhiteSpace(normalized) && Path.IsPathFullyQualified(normalized);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            normalized = string.Empty;
            return false;
        }
    }

    private static TelemetryWorkloadTarget UnknownGame()
        => new()
        {
            BindingQuality = TelemetryWorkloadBindingQuality.UnknownGame
        };

    private static TelemetryWorkloadTarget Unavailable(string gameId)
        => new()
        {
            GameId = gameId,
            BindingQuality = TelemetryWorkloadBindingQuality.UnavailableRunningProcess
        };

    private static TelemetryWorkloadTarget Ambiguous(string gameId)
        => new()
        {
            GameId = gameId,
            BindingQuality = TelemetryWorkloadBindingQuality.AmbiguousRunningProcess
        };

    private sealed record ProcessPathEvidence(int ProcessId, string ExecutablePath);
    private sealed record ProcessEvidenceGroup(int ProcessId, IReadOnlyList<string> Paths);
}
