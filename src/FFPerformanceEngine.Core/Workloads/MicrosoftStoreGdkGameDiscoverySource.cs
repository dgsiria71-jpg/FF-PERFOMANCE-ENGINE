using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace FFPerformanceEngine.Core.Workloads;

/// <summary>
/// Platform observation for one Windows package. The Core deliberately receives
/// only data: WinRT PackageManager access remains outside this net8.0 assembly.
/// MicrosoftGameConfigXml is positive GDK game evidence when it validates.
/// </summary>
public sealed record MicrosoftStoreGamePackageObservation(
    string PackageFamilyName,
    string PackageFullName,
    string PackageName,
    string? DisplayName,
    string? InstalledPath,
    string? EffectivePath,
    string? MicrosoftGameConfigXml,
    bool IsFramework = false,
    bool IsResourcePackage = false,
    bool IsBundle = false,
    bool IsOptional = false);

/// <summary>
/// Windows-platform boundary used by the neutral Core. Implementations enumerate
/// current-user packages and acquire MicrosoftGame.config through supported OS
/// package/storage APIs; constructing a provider must not itself enumerate.
/// </summary>
public interface IMicrosoftStoreGamePackageProvider
{
    Task<IReadOnlyList<MicrosoftStoreGamePackageObservation>> EnumerateAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Precision-first Microsoft Store/Xbox PC discovery for GDK titles positively
/// identified by MicrosoftGame.config. Package Family Name is the stable local
/// package identity; versioned PackageFullName, StoreId, TitleId and executable
/// declarations remain evidence and never silently replace that identity.
/// </summary>
public sealed class MicrosoftStoreGdkGameDiscoverySource : IGameDiscoverySource
{
    private const long MaxGameConfigCharacters = 2 * 1024 * 1024;
    private readonly IMicrosoftStoreGamePackageProvider _provider;

    public MicrosoftStoreGdkGameDiscoverySource(IMicrosoftStoreGamePackageProvider provider)
        => _provider = provider ?? throw new ArgumentNullException(nameof(provider));

    public string SourceId => "microsoft-store-gdk";
    public int Priority => 80;

    public async Task<IReadOnlyList<GameDiscoveryCandidate>> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var observations = await _provider.EnumerateAsync(cancellationToken).ConfigureAwait(false)
            ?? Array.Empty<MicrosoftStoreGamePackageObservation>();
        cancellationToken.ThrowIfCancellationRequested();

        var proven = new List<ProvenPackage>();
        foreach (var observation in observations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (observation is null
                || observation.IsFramework
                || observation.IsResourcePackage
                || observation.IsBundle
                || observation.IsOptional)
            {
                continue;
            }

            var packageFamilyName = NormalizePackageFamilyName(observation.PackageFamilyName);
            if (packageFamilyName is null) continue;

            var installPath = NormalizeInstallPath(observation.EffectivePath)
                              ?? NormalizeInstallPath(observation.InstalledPath);
            if (installPath is null) continue;

            if (!TryParseMainGameConfig(observation.MicrosoftGameConfigXml, out var config))
                continue;

            proven.Add(new ProvenPackage(
                PackageFamilyName: packageFamilyName,
                PackageFullName: CleanEvidenceValue(observation.PackageFullName),
                PackageName: CleanEvidenceValue(observation.PackageName),
                DisplayName: NormalizeDisplayName(observation.DisplayName),
                InstallPath: installPath,
                Config: config));
        }

        return proven
            .GroupBy(package => package.PackageFamilyName, StringComparer.Ordinal)
            .Select(CreateCandidate)
            .OrderBy(candidate => candidate.Identity.GameId, StringComparer.Ordinal)
            .ToArray();
    }

    private static GameDiscoveryCandidate CreateCandidate(
        IGrouping<string, ProvenPackage> group)
    {
        var packages = group
            .OrderBy(package => package.PackageFullName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(package => package.PackageFullName, StringComparer.Ordinal)
            .ToArray();

        var displayNames = packages
            .Select(package => package.DisplayName)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var identityNames = packages
            .Select(package => package.Config.IdentityName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var installPaths = packages
            .Select(package => package.InstallPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var packageFullNames = packages
            .Select(package => package.PackageFullName)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var packageNames = packages
            .Select(package => package.PackageName)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var executableNames = packages
            .SelectMany(package => package.Config.ExecutableNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var storeIds = packages
            .Select(package => package.Config.StoreId)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var titleIds = packages
            .Select(package => package.Config.TitleId)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var publishers = packages
            .Select(package => package.Config.Publisher)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(value => value, StringComparer.Ordinal)
            .ToArray();

        var evidenceParts = new List<string>
        {
            "MicrosoftGame.config",
            $"PFN {group.Key}"
        };
        if (packageFullNames.Length > 0)
            evidenceParts.Add($"PackageFullName [{string.Join(" | ", packageFullNames)}]");
        if (packageNames.Length > 0)
            evidenceParts.Add($"PackageName [{string.Join(" | ", packageNames)}]");
        evidenceParts.Add($"Identity.Name [{string.Join(" | ", identityNames)}]");
        evidenceParts.Add($"Publisher [{string.Join(" | ", publishers)}]");
        if (storeIds.Length > 0)
            evidenceParts.Add($"StoreId [{string.Join(" | ", storeIds)}]");
        if (titleIds.Length > 0)
            evidenceParts.Add($"TitleId [{string.Join(" | ", titleIds)}]");
        evidenceParts.Add($"Configured executables [{string.Join(" | ", executableNames)}]");

        return new GameDiscoveryCandidate
        {
            Identity = new GameIdentity
            {
                GameId = "xbox:" + group.Key,
                Name = displayNames.FirstOrDefault() ?? identityNames[0],
                Launcher = GameLauncherKind.Xbox,
                Engine = GameEngineKind.Unknown,
                // The GDK config proves executable declarations, but it does not
                // prove that this process can directly access/launch the physical
                // file in a protected package. Preserve names in evidence only.
                Executables = Array.Empty<string>(),
                InstallPaths = installPaths,
                AuxiliaryProcesses = Array.Empty<string>(),
                AdapterId = "generic"
            },
            Confidence = 0.99,
            Evidence = string.Join(" · ", evidenceParts)
        };
    }

    private static bool TryParseMainGameConfig(string? xml, out ParsedGameConfig config)
    {
        config = null!;
        if (string.IsNullOrWhiteSpace(xml) || xml.Length > MaxGameConfigCharacters) return false;

        XDocument document;
        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreWhitespace = true,
                MaxCharactersInDocument = MaxGameConfigCharacters
            };
            using var text = new StringReader(xml);
            using var reader = XmlReader.Create(text, settings);
            document = XDocument.Load(reader, LoadOptions.None);
        }
        catch (XmlException)
        {
            return false;
        }

        var game = document.Root;
        if (game is null || !NameEquals(game, "Game")) return false;

        // AllowedProducts is DLC ownership metadata. This source intentionally
        // catalogs playable base GDK packages only; DLC gets a separate model.
        if (game.Elements().Any(element => NameEquals(element, "AllowedProducts")))
            return false;

        var identity = game.Elements().FirstOrDefault(element => NameEquals(element, "Identity"));
        var identityName = ReadAttribute(identity, "Name");
        var publisher = ReadAttribute(identity, "Publisher");
        if (!IsEvidenceToken(identityName, 256) || !IsEvidenceToken(publisher, 1024)) return false;

        var executableList = game.Elements().FirstOrDefault(element => NameEquals(element, "ExecutableList"));
        var executableNames = executableList?
            .Elements()
            .Where(element => NameEquals(element, "Executable"))
            .Select(element => ReadAttribute(element, "Name"))
            .Where(value => IsSafeExecutableDeclaration(value))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ThenBy(value => value, StringComparer.Ordinal)
            .ToArray()
            ?? Array.Empty<string>();
        if (executableNames.Length == 0) return false;

        var storeIdRaw = ReadElementValue(game, "StoreId");
        var storeId = IsStoreId(storeIdRaw) ? storeIdRaw : null;
        var titleIdRaw = ReadElementValue(game, "TitleId");
        var titleId = IsTitleId(titleIdRaw) ? titleIdRaw!.ToUpperInvariant() : null;

        config = new ParsedGameConfig(
            identityName!,
            publisher!,
            executableNames,
            storeId,
            titleId);
        return true;
    }

    private static string? NormalizePackageFamilyName(string? value)
    {
        var cleaned = CleanEvidenceValue(value);
        if (!IsEvidenceToken(cleaned, 256)) return null;
        if (cleaned!.Any(char.IsWhiteSpace)) return null;

        var separator = cleaned.LastIndexOf('_');
        if (separator <= 0 || separator == cleaned.Length - 1) return null;
        return cleaned.ToLowerInvariant();
    }

    private static string? NormalizeInstallPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try
        {
            var trimmed = value.Trim();
            if (!Path.IsPathFullyQualified(trimmed)) return null;
            return Path.GetFullPath(trimmed)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private static string? NormalizeDisplayName(string? value)
    {
        var cleaned = CleanEvidenceValue(value);
        if (!IsEvidenceToken(cleaned, 512)) return null;
        return cleaned!.StartsWith("ms-resource:", StringComparison.OrdinalIgnoreCase)
            ? null
            : cleaned;
    }

    private static string? CleanEvidenceValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var cleaned = value.Trim();
        return cleaned.Any(char.IsControl) ? null : cleaned;
    }

    private static bool IsEvidenceToken(string? value, int maxLength)
        => !string.IsNullOrWhiteSpace(value)
           && value.Length <= maxLength
           && !value.Any(char.IsControl);

    private static bool IsSafeExecutableDeclaration(string? value)
    {
        if (!IsEvidenceToken(value, 260)) return false;
        var name = value!.Trim();
        if (Path.IsPathRooted(name)) return false;
        if (name.Contains("..", StringComparison.Ordinal)) return false;
        return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsStoreId(string? value)
        => value is { Length: 12 }
           && value.All(character => char.IsAsciiLetterOrDigit(character));

    private static bool IsTitleId(string? value)
        => value is { Length: 8 }
           && value.All(Uri.IsHexDigit);

    private static string? ReadAttribute(XElement? element, string name)
        => element?.Attributes()
            .FirstOrDefault(attribute => string.Equals(
                attribute.Name.LocalName,
                name,
                StringComparison.OrdinalIgnoreCase))
            ?.Value.Trim();

    private static string? ReadElementValue(XElement parent, string name)
        => parent.Elements()
            .FirstOrDefault(element => NameEquals(element, name))
            ?.Value.Trim();

    private static bool NameEquals(XElement element, string name)
        => string.Equals(element.Name.LocalName, name, StringComparison.OrdinalIgnoreCase);

    private sealed record ParsedGameConfig(
        string IdentityName,
        string Publisher,
        IReadOnlyList<string> ExecutableNames,
        string? StoreId,
        string? TitleId);

    private sealed record ProvenPackage(
        string PackageFamilyName,
        string? PackageFullName,
        string? PackageName,
        string? DisplayName,
        string InstallPath,
        ParsedGameConfig Config);
}
