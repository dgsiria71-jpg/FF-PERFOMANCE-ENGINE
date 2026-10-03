using System.Runtime.Versioning;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Win32;

namespace FFPerformanceEngine.Core.Workloads;

/// <summary>
/// Read-only EA App / Origin installation discovery based on each installed
/// game's __Installer/installerdata.xml. The first launcher-native contentID is
/// the stable local identity; the complete contentID list remains provenance.
/// Display names and install folders are evidence only.
/// </summary>
public sealed class EaAppGameDiscoverySource : IGameDiscoverySource
{
    private const long MaxInstallerDataCharacters = 2 * 1024 * 1024;
    private readonly Func<IReadOnlyList<string>> _installDirectoriesProvider;

    public EaAppGameDiscoverySource(Func<IReadOnlyList<string>>? installDirectoriesProvider = null)
        => _installDirectoriesProvider = installDirectoriesProvider ?? DiscoverDefaultInstallDirectories;

    public string SourceId => "ea-installerdata";
    public int Priority => 80;

    public Task<IReadOnlyList<GameDiscoveryCandidate>> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<string> rawDirectories;
        try
        {
            rawDirectories = _installDirectoriesProvider() ?? Array.Empty<string>();
        }
        catch
        {
            return Task.FromResult<IReadOnlyList<GameDiscoveryCandidate>>(Array.Empty<GameDiscoveryCandidate>());
        }

        var directories = rawDirectories
            .Select(TryNormalizeDirectory)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var candidates = new Dictionary<string, GameDiscoveryCandidate>(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in directories)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Directory.Exists(directory)) continue;

            var manifestPath = Path.Combine(directory, "__Installer", "installerdata.xml");
            if (!File.Exists(manifestPath)) continue;
            if (!TryReadCandidate(directory, manifestPath, out var candidate)) continue;

            candidates.TryAdd(candidate.Identity.GameId, candidate);
        }

        IReadOnlyList<GameDiscoveryCandidate> ordered = candidates.Values
            .OrderBy(candidate => candidate.Identity.GameId, StringComparer.Ordinal)
            .ToArray();
        return Task.FromResult(ordered);
    }

    private static bool TryReadCandidate(
        string installDirectory,
        string manifestPath,
        out GameDiscoveryCandidate candidate)
    {
        candidate = null!;

        XDocument document;
        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreWhitespace = true,
                MaxCharactersInDocument = MaxInstallerDataCharacters
            };
            using var stream = File.OpenRead(manifestPath);
            using var reader = XmlReader.Create(stream, settings);
            document = XDocument.Load(reader, LoadOptions.None);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException)
        {
            return false;
        }

        var contentIds = document
            .Descendants()
            .Where(element => string.Equals(element.Name.LocalName, "contentID", StringComparison.OrdinalIgnoreCase))
            .Select(element => element.Value.Trim())
            .Where(value => IsUsableContentId(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (contentIds.Length == 0) return false;

        var primaryContentId = contentIds[0];
        var gameId = "ea:" + primaryContentId.ToLowerInvariant();
        var title = ReadGameTitle(document)
                    ?? Path.GetFileName(installDirectory.TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar))
                    ?? primaryContentId;
        if (string.IsNullOrWhiteSpace(title)) title = primaryContentId;

        candidate = new GameDiscoveryCandidate
        {
            Identity = new GameIdentity
            {
                GameId = gameId,
                Name = title.Trim(),
                Launcher = GameLauncherKind.EA,
                Engine = GameEngineKind.Unknown,
                Executables = Array.Empty<string>(),
                InstallPaths = [installDirectory],
                AuxiliaryProcesses = Array.Empty<string>(),
                AdapterId = "generic"
            },
            Confidence = 0.98,
            Evidence = $"EA installerdata.xml · primary {primaryContentId} · contentIDs [{string.Join(", ", contentIds)}]"
        };
        return true;
    }

    private static string? ReadGameTitle(XDocument document)
    {
        var titles = document
            .Descendants()
            .Where(element => string.Equals(element.Name.LocalName, "gameTitle", StringComparison.OrdinalIgnoreCase))
            .Select(element => new
            {
                Locale = element.Attributes()
                    .FirstOrDefault(attribute => string.Equals(
                        attribute.Name.LocalName,
                        "locale",
                        StringComparison.OrdinalIgnoreCase))
                    ?.Value.Trim(),
                Value = element.Value.Trim()
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .ToArray();

        return titles.FirstOrDefault(item => string.Equals(item.Locale, "en_US", StringComparison.OrdinalIgnoreCase))?.Value
               ?? titles.FirstOrDefault()?.Value;
    }

    private static bool IsUsableContentId(string value)
        => !string.IsNullOrWhiteSpace(value)
           && value.Length <= 256
           && value.All(character => !char.IsControl(character));

    private static string? TryNormalizeDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        try
        {
            return Path.GetFullPath(path.Trim())
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private static IReadOnlyList<string> DiscoverDefaultInstallDirectories()
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddLibraryChildren(candidates, Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "EA Games"));
        AddLibraryChildren(candidates, Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "EA Games"));
        AddLibraryChildren(candidates, Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Origin Games"));

        if (OperatingSystem.IsWindows()) AddRegistryInstallDirectories(candidates);

        return candidates
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static void AddLibraryChildren(HashSet<string> candidates, string libraryRoot)
    {
        if (string.IsNullOrWhiteSpace(libraryRoot) || !Directory.Exists(libraryRoot)) return;
        try
        {
            foreach (var child in Directory.EnumerateDirectories(libraryRoot, "*", SearchOption.TopDirectoryOnly))
            {
                var normalized = TryNormalizeDirectory(child);
                if (!string.IsNullOrWhiteSpace(normalized)) candidates.Add(normalized);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // One inaccessible library must not fail the entire discovery source.
        }
    }

    [SupportedOSPlatform("windows")]
    private static void AddRegistryInstallDirectories(HashSet<string> candidates)
    {
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                foreach (var rootName in new[] { @"SOFTWARE\EA Games", @"SOFTWARE\Origin Games" })
                {
                    using var root = localMachine.OpenSubKey(rootName, writable: false);
                    if (root is null) continue;

                    string[] subKeyNames;
                    try { subKeyNames = root.GetSubKeyNames(); }
                    catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                    {
                        continue;
                    }

                    foreach (var subKeyName in subKeyNames)
                    {
                        try
                        {
                            using var gameKey = root.OpenSubKey(subKeyName, writable: false);
                            var rawPath = gameKey?.GetValue("Install Dir") as string
                                          ?? gameKey?.GetValue("InstallDir") as string;
                            var normalized = TryNormalizeDirectory(rawPath);
                            if (!string.IsNullOrWhiteSpace(normalized)) candidates.Add(normalized);
                        }
                        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                        {
                            // Registry evidence is opportunistic; continue with other games.
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                // Another registry view or default filesystem roots may still work.
            }
        }
    }
}
