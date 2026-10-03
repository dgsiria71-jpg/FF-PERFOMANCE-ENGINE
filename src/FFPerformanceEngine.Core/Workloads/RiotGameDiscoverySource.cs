namespace FFPerformanceEngine.Core.Workloads;

/// <summary>
/// Read-only Riot product discovery based on the launcher's local product metadata.
/// Product identity comes from the metadata directory/file id (product.patchline),
/// while product_install_full_path proves the installed location. The product
/// settings file does not prove the gameplay executable or engine, so neither is
/// inferred here.
/// </summary>
public sealed class RiotGameDiscoverySource : IGameDiscoverySource
{
    private const string ProductSettingsSuffix = ".product_settings.yaml";
    private readonly Func<IReadOnlyList<string>> _metadataRootsProvider;

    public RiotGameDiscoverySource(Func<IReadOnlyList<string>>? metadataRootsProvider = null)
        => _metadataRootsProvider = metadataRootsProvider ?? DiscoverDefaultMetadataRoots;

    public string SourceId => "riot-product-metadata";
    public int Priority => 80;

    public Task<IReadOnlyList<GameDiscoveryCandidate>> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<string> roots;
        try
        {
            roots = _metadataRootsProvider() ?? Array.Empty<string>();
        }
        catch
        {
            return Task.FromResult<IReadOnlyList<GameDiscoveryCandidate>>(Array.Empty<GameDiscoveryCandidate>());
        }

        var candidates = new Dictionary<string, GameDiscoveryCandidate>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawRoot in roots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryNormalizeDirectory(rawRoot, out var root) || !Directory.Exists(root)) continue;

            string[] productDirectories;
            try
            {
                productDirectories = Directory.EnumerateDirectories(root, "*", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(path => path, StringComparer.Ordinal)
                    .ToArray();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var productDirectory in productDirectories)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!TryReadCandidate(productDirectory, out var candidate)) continue;
                candidates.TryAdd(candidate.Identity.GameId, candidate);
            }
        }

        IReadOnlyList<GameDiscoveryCandidate> ordered = candidates.Values
            .OrderBy(candidate => candidate.Identity.GameId, StringComparer.Ordinal)
            .ToArray();
        return Task.FromResult(ordered);
    }

    private static bool TryReadCandidate(
        string productDirectory,
        out GameDiscoveryCandidate candidate)
    {
        candidate = null!;

        var directoryProductId = Path.GetFileName(
            productDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(directoryProductId)) return false;

        string[] settingsFiles;
        try
        {
            settingsFiles = Directory.EnumerateFiles(
                    productDirectory,
                    "*.product_settings.yaml",
                    SearchOption.TopDirectoryOnly)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ThenBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }

        foreach (var settingsPath in settingsFiles)
        {
            var fileName = Path.GetFileName(settingsPath);
            if (!fileName.EndsWith(ProductSettingsSuffix, StringComparison.OrdinalIgnoreCase)) continue;

            var fileProductId = fileName[..^ProductSettingsSuffix.Length];
            if (!string.Equals(fileProductId, directoryProductId, StringComparison.OrdinalIgnoreCase))
                continue;

            IReadOnlyDictionary<string, string> scalars;
            try
            {
                scalars = ReadTopLevelScalars(File.ReadAllLines(settingsPath));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            if (!scalars.TryGetValue("product_install_full_path", out var rawInstallPath)
                || !TryNormalizeDirectory(rawInstallPath, out var installPath)
                || !Directory.Exists(installPath))
                continue;

            scalars.TryGetValue("shortcut_name", out var shortcutName);
            var displayName = DisplayNameFromShortcut(shortcutName);
            if (string.IsNullOrWhiteSpace(displayName)) displayName = directoryProductId;

            var normalizedProductId = directoryProductId.Trim().ToLowerInvariant();
            candidate = new GameDiscoveryCandidate
            {
                Identity = new GameIdentity
                {
                    GameId = $"riot:{normalizedProductId}",
                    Name = displayName,
                    Launcher = GameLauncherKind.Riot,
                    Engine = GameEngineKind.Unknown,
                    Executables = Array.Empty<string>(),
                    InstallPaths = [installPath],
                    AuxiliaryProcesses = Array.Empty<string>(),
                    AdapterId = "generic"
                },
                Confidence = 0.97,
                Evidence = $"Riot product metadata {Path.GetFileName(settingsPath)} · product {directoryProductId}"
            };
            return true;
        }

        return false;
    }

    internal static IReadOnlyDictionary<string, string> ReadTopLevelScalars(
        IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in lines)
        {
            if (string.IsNullOrWhiteSpace(rawLine)) continue;
            if (char.IsWhiteSpace(rawLine[0])) continue;

            var trimmed = rawLine.Trim();
            if (trimmed.StartsWith('#')) continue;

            var separator = trimmed.IndexOf(':');
            if (separator <= 0) continue;

            var key = trimmed[..separator].Trim();
            var rawValue = trimmed[(separator + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(rawValue)) continue;

            var value = ParseScalarValue(rawValue);
            if (!string.IsNullOrWhiteSpace(value)) values[key] = value;
        }

        return values;
    }

    private static string ParseScalarValue(string rawValue)
    {
        var value = rawValue.Trim();
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
        {
            value = value[1..^1]
                .Replace("\\\"", "\"", StringComparison.Ordinal)
                .Replace("\\\\", "\\", StringComparison.Ordinal);
        }
        else if (value.Length >= 2 && value[0] == '\'' && value[^1] == '\'')
        {
            value = value[1..^1].Replace("''", "'", StringComparison.Ordinal);
        }

        return value.Trim();
    }

    private static string DisplayNameFromShortcut(string? shortcutName)
    {
        if (string.IsNullOrWhiteSpace(shortcutName)) return string.Empty;
        var name = shortcutName.Trim();
        return string.Equals(Path.GetExtension(name), ".lnk", StringComparison.OrdinalIgnoreCase)
            ? Path.GetFileNameWithoutExtension(name).Trim()
            : name;
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

    private static IReadOnlyList<string> DiscoverDefaultMetadataRoots()
    {
        var commonApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        if (string.IsNullOrWhiteSpace(commonApplicationData)) return Array.Empty<string>();

        var metadataRoot = Path.Combine(commonApplicationData, "Riot Games", "Metadata");
        return Directory.Exists(metadataRoot) ? [metadataRoot] : Array.Empty<string>();
    }
}
