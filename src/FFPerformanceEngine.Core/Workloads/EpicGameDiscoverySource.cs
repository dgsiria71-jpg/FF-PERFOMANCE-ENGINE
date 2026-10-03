using System.Text.Json;

namespace FFPerformanceEngine.Core.Workloads;

/// <summary>
/// Read-only Epic Games Launcher discovery source. Only local .item manifests
/// explicitly classified as games are accepted. Stable identity comes from the
/// catalog namespace + catalog item id; display names and install folders are
/// evidence only and never identity keys.
/// </summary>
public sealed class EpicGameDiscoverySource : IGameDiscoverySource
{
    private readonly Func<IReadOnlyList<string>> _manifestDirectoriesProvider;

    public EpicGameDiscoverySource(Func<IReadOnlyList<string>>? manifestDirectoriesProvider = null)
        => _manifestDirectoriesProvider = manifestDirectoriesProvider ?? DiscoverDefaultManifestDirectories;

    public string SourceId => "epic-manifests";
    public int Priority => 80;

    public Task<IReadOnlyList<GameDiscoveryCandidate>> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<string> directories;
        try
        {
            directories = _manifestDirectoriesProvider() ?? Array.Empty<string>();
        }
        catch
        {
            return Task.FromResult<IReadOnlyList<GameDiscoveryCandidate>>(Array.Empty<GameDiscoveryCandidate>());
        }

        var candidates = new Dictionary<string, GameDiscoveryCandidate>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawDirectory in directories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryNormalizeDirectory(rawDirectory, out var directory) || !Directory.Exists(directory))
                continue;

            string[] manifests;
            try
            {
                manifests = Directory.EnumerateFiles(directory, "*.item", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(path => path, StringComparer.Ordinal)
                    .ToArray();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var manifestPath in manifests)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!TryReadCandidate(manifestPath, out var candidate)) continue;
                candidates.TryAdd(candidate.Identity.GameId, candidate);
            }
        }

        IReadOnlyList<GameDiscoveryCandidate> ordered = candidates.Values
            .OrderBy(candidate => candidate.Identity.GameId, StringComparer.Ordinal)
            .ToArray();
        return Task.FromResult(ordered);
    }

    private static bool TryReadCandidate(string manifestPath, out GameDiscoveryCandidate candidate)
    {
        candidate = null!;

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return false;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !IsGameManifest(root)) return false;

            var catalogNamespace = ReadString(root, "CatalogNamespace")?.Trim();
            var catalogItemId = ReadString(root, "CatalogItemId")?.Trim();
            var displayName = ReadString(root, "DisplayName")?.Trim();
            var appName = ReadString(root, "AppName")?.Trim();
            var installLocation = ReadString(root, "InstallLocation")?.Trim();

            if (string.IsNullOrWhiteSpace(catalogNamespace)
                || string.IsNullOrWhiteSpace(catalogItemId)
                || string.IsNullOrWhiteSpace(displayName)
                || string.IsNullOrWhiteSpace(installLocation))
                return false;

            if (!TryNormalizeDirectory(installLocation, out var normalizedInstall)
                || !Directory.Exists(normalizedInstall))
                return false;

            var gameId = BuildGameId(catalogNamespace, catalogItemId);
            if (string.IsNullOrWhiteSpace(gameId)) return false;

            var executables = ResolveExecutableEvidence(
                normalizedInstall,
                ReadString(root, "LaunchExecutable"));

            candidate = new GameDiscoveryCandidate
            {
                Identity = new GameIdentity
                {
                    GameId = gameId,
                    Name = displayName,
                    Launcher = GameLauncherKind.Epic,
                    Engine = GameEngineKind.Unknown,
                    Executables = executables,
                    InstallPaths = [normalizedInstall],
                    AuxiliaryProcesses = Array.Empty<string>(),
                    AdapterId = "generic"
                },
                Confidence = 0.98,
                Evidence = $"Epic manifest {Path.GetFileName(manifestPath)} · catalog {catalogNamespace}:{catalogItemId} · app {appName ?? "unknown"}"
            };
            return true;
        }
    }

    private static bool IsGameManifest(JsonElement root)
    {
        if (root.TryGetProperty("AppCategories", out var categories)
            && categories.ValueKind == JsonValueKind.Array)
        {
            foreach (var category in categories.EnumerateArray())
            {
                if (category.ValueKind == JsonValueKind.String
                    && string.Equals(category.GetString()?.Trim(), "games", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        var technicalType = ReadString(root, "TechnicalType");
        if (!string.IsNullOrWhiteSpace(technicalType))
        {
            foreach (var token in technicalType.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (string.Equals(token, "games", StringComparison.OrdinalIgnoreCase)) return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<string> ResolveExecutableEvidence(
        string installDirectory,
        string? launchExecutable)
    {
        if (string.IsNullOrWhiteSpace(launchExecutable)) return Array.Empty<string>();

        try
        {
            var installRoot = Path.GetFullPath(installDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var installPrefix = installRoot + Path.DirectorySeparatorChar;
            var rawExecutable = launchExecutable.Trim()
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var executablePath = Path.IsPathRooted(rawExecutable)
                ? Path.GetFullPath(rawExecutable)
                : Path.GetFullPath(Path.Combine(installRoot, rawExecutable));

            if (!executablePath.StartsWith(installPrefix, StringComparison.OrdinalIgnoreCase)
                || !File.Exists(executablePath))
                return Array.Empty<string>();

            var fileName = Path.GetFileName(executablePath);
            return string.IsNullOrWhiteSpace(fileName) ? Array.Empty<string>() : [fileName];
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return Array.Empty<string>();
        }
    }

    private static string BuildGameId(string catalogNamespace, string catalogItemId)
        => $"epic:{NormalizeIdentityPart(catalogNamespace)}:{NormalizeIdentityPart(catalogItemId)}";

    private static string NormalizeIdentityPart(string value)
        => value.Trim().ToLowerInvariant();

    private static string? ReadString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
            return null;
        return value.GetString();
    }

    private static bool TryNormalizeDirectory(string? path, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(path)) return false;
        try
        {
            normalized = Path.GetFullPath(path.Trim());
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private static IReadOnlyList<string> DiscoverDefaultManifestDirectories()
    {
        var commonApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        if (string.IsNullOrWhiteSpace(commonApplicationData)) return Array.Empty<string>();

        var manifestDirectory = Path.Combine(
            commonApplicationData,
            "Epic",
            "EpicGamesLauncher",
            "Data",
            "Manifests");
        return Directory.Exists(manifestDirectory) ? [manifestDirectory] : Array.Empty<string>();
    }
}
