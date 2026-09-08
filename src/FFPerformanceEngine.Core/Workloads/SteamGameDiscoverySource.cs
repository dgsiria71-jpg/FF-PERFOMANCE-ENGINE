using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace FFPerformanceEngine.Core.Workloads;

/// <summary>
/// Read-only Steam launcher scanner. Steam app manifests prove app identity,
/// display name and install directory; they do not prove an executable, engine,
/// or a specialized DG adapter, so those fields intentionally remain unknown.
/// </summary>
public sealed partial class SteamGameDiscoverySource : IGameDiscoverySource
{
    private readonly Func<IReadOnlyList<string>> _steamRootsProvider;

    public SteamGameDiscoverySource(Func<IReadOnlyList<string>>? steamRootsProvider = null)
        => _steamRootsProvider = steamRootsProvider ?? DiscoverDefaultSteamRoots;

    public string SourceId => "steam-manifests";
    public int Priority => 80;

    public Task<IReadOnlyList<GameDiscoveryCandidate>> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var libraries = DiscoverLibraries(cancellationToken);
        var candidates = new Dictionary<string, GameDiscoveryCandidate>(StringComparer.Ordinal);

        foreach (var library in libraries.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var steamApps = Path.Combine(library, "steamapps");
            if (!Directory.Exists(steamApps)) continue;

            IEnumerable<string> manifests;
            try
            {
                manifests = Directory.EnumerateFiles(
                    steamApps,
                    "appmanifest_*.acf",
                    SearchOption.TopDirectoryOnly).ToArray();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var manifestPath in manifests.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!TryReadManifest(manifestPath, library, out var candidate)) continue;
                candidates.TryAdd(candidate.Identity.GameId, candidate);
            }
        }

        IReadOnlyList<GameDiscoveryCandidate> ordered = candidates.Values
            .OrderBy(candidate => candidate.Identity.GameId, StringComparer.Ordinal)
            .ToArray();
        return Task.FromResult(ordered);
    }

    private IReadOnlyList<string> DiscoverLibraries(CancellationToken cancellationToken)
    {
        var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        IReadOnlyList<string> roots;
        try
        {
            roots = _steamRootsProvider() ?? Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }

        foreach (var rawRoot in roots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryNormalizeDirectory(rawRoot, out var root) || !Directory.Exists(root)) continue;
            libraries.Add(root);

            var libraryFolders = Path.Combine(root, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(libraryFolders)) continue;

            string text;
            try
            {
                text = File.ReadAllText(libraryFolders);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (Match match in LibraryPathRegex().Matches(text))
            {
                var rawPath = UnescapeVdf(match.Groups["value"].Value);
                if (TryNormalizeDirectory(rawPath, out var library) && Directory.Exists(library))
                    libraries.Add(library);
            }
        }

        return libraries.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static bool TryReadManifest(
        string manifestPath,
        string library,
        out GameDiscoveryCandidate candidate)
    {
        candidate = null!;

        string text;
        try
        {
            text = File.ReadAllText(manifestPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }

        var appIdText = ReadValue(text, "appid");
        var name = ReadValue(text, "name")?.Trim();
        var installDir = ReadValue(text, "installdir")?.Trim();
        if (!ulong.TryParse(appIdText, out var appId) || appId == 0 || string.IsNullOrWhiteSpace(name))
            return false;

        var installPaths = Array.Empty<string>();
        if (!string.IsNullOrWhiteSpace(installDir)
            && TryResolveInstallPath(library, installDir, out var resolvedInstallPath)
            && Directory.Exists(resolvedInstallPath))
        {
            installPaths = [resolvedInstallPath];
        }

        var stableId = $"steam:{appId}";
        candidate = new GameDiscoveryCandidate
        {
            Identity = new GameIdentity
            {
                GameId = stableId,
                Name = name,
                Launcher = GameLauncherKind.Steam,
                Engine = GameEngineKind.Unknown,
                InstallPaths = installPaths,
                Executables = Array.Empty<string>(),
                AuxiliaryProcesses = Array.Empty<string>(),
                AdapterId = "generic"
            },
            Confidence = 0.98,
            Evidence = $"Steam manifest {Path.GetFileName(manifestPath)} · appid {appId}"
        };
        return true;
    }

    private static string? ReadValue(string text, string key)
    {
        foreach (Match match in KeyValueRegex().Matches(text))
        {
            if (string.Equals(match.Groups["key"].Value, key, StringComparison.OrdinalIgnoreCase))
                return UnescapeVdf(match.Groups["value"].Value);
        }

        return null;
    }

    private static bool TryResolveInstallPath(string library, string installDir, out string installPath)
    {
        installPath = string.Empty;
        try
        {
            var common = Path.GetFullPath(Path.Combine(library, "steamapps", "common"));
            var commonPrefix = common.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                               + Path.DirectorySeparatorChar;
            var candidate = Path.GetFullPath(Path.Combine(common, installDir));
            if (!candidate.StartsWith(commonPrefix, StringComparison.OrdinalIgnoreCase)) return false;
            installPath = candidate;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
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

    private static IReadOnlyList<string> DiscoverDefaultSteamRoots()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (OperatingSystem.IsWindows())
        {
            TryAddRegistrySteamPath(roots, Registry.CurrentUser, @"Software\Valve\Steam", "SteamPath");
            TryAddRegistrySteamPath(roots, Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath");
            TryAddRegistrySteamPath(roots, Registry.LocalMachine, @"SOFTWARE\Valve\Steam", "InstallPath");
        }

        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        if (!string.IsNullOrWhiteSpace(programFilesX86)) roots.Add(Path.Combine(programFilesX86, "Steam"));
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!string.IsNullOrWhiteSpace(programFiles)) roots.Add(Path.Combine(programFiles, "Steam"));

        return roots.Where(Directory.Exists).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static void TryAddRegistrySteamPath(
        ISet<string> roots,
        RegistryKey hive,
        string subKey,
        string valueName)
    {
        try
        {
            using var key = hive.OpenSubKey(subKey, writable: false);
            if (key?.GetValue(valueName) is string path && !string.IsNullOrWhiteSpace(path))
                roots.Add(path.Trim());
        }
        catch
        {
            // Registry discovery is only one independent hint; conventional
            // paths and any caller-provided roots remain available.
        }
    }

    private static string UnescapeVdf(string value)
        => value.Replace("\\\\", "\\", StringComparison.Ordinal)
            .Replace("\\\"", "\"", StringComparison.Ordinal);

    [GeneratedRegex("\\\"path\\\"\\s*\\\"(?<value>(?:\\\\.|[^\\\"])*)\\\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LibraryPathRegex();

    [GeneratedRegex("\\\"(?<key>[^\\\"]+)\\\"\\s*\\\"(?<value>(?:\\\\.|[^\\\"])*)\\\"", RegexOptions.CultureInvariant)]
    private static partial Regex KeyValueRegex();
}
