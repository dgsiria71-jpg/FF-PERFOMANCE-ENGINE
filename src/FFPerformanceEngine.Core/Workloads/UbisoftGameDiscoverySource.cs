using System.Globalization;
using System.Runtime.Versioning;
using System.Security;
using Microsoft.Win32;

namespace FFPerformanceEngine.Core.Workloads;

/// <summary>
/// One local Ubisoft Connect installation registration. InstallId is the numeric
/// key stored under Ubisoft\Launcher\Installs; it is deliberately treated as a
/// stable local installation identity, not as a claim about uplay:// launch IDs.
/// </summary>
public sealed record UbisoftInstallRegistration(
    string InstallId,
    string InstallDirectory,
    string? DisplayName = null,
    string? EvidenceKey = null);

/// <summary>
/// Read-only Ubisoft Connect installed-game discovery based on HKLM launcher
/// registration. The scanner never starts Ubisoft Connect and never guesses a
/// gameplay executable or engine from files inside the install directory.
/// </summary>
public sealed class UbisoftGameDiscoverySource : IGameDiscoverySource
{
    private const string InstallsSubKey = @"SOFTWARE\Ubisoft\Launcher\Installs";
    private const string UninstallSubKeyPrefix = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\UPlay Install ";

    private readonly Func<CancellationToken, IReadOnlyList<UbisoftInstallRegistration>> _registrationsProvider;

    public UbisoftGameDiscoverySource(
        Func<CancellationToken, IReadOnlyList<UbisoftInstallRegistration>>? registrationsProvider = null)
        => _registrationsProvider = registrationsProvider ?? ReadRegistryRegistrations;

    public string SourceId => "ubisoft-registry-installs";
    public int Priority => 80;

    public Task<IReadOnlyList<GameDiscoveryCandidate>> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var registrations = _registrationsProvider(cancellationToken)
            ?? Array.Empty<UbisoftInstallRegistration>();
        cancellationToken.ThrowIfCancellationRequested();

        var normalized = registrations
            .Select(TryNormalize)
            .Where(registration => registration is not null)
            .Cast<NormalizedRegistration>()
            .OrderBy(registration => registration.InstallIdValue)
            .ThenBy(registration => registration.InstallDirectory, StringComparer.OrdinalIgnoreCase)
            .ThenBy(registration => registration.InstallDirectory, StringComparer.Ordinal)
            .ThenBy(registration => registration.EvidenceKey, StringComparer.Ordinal)
            .ToArray();

        var candidates = normalized
            .GroupBy(registration => registration.InstallIdValue)
            .Select(CreateCandidate)
            .OrderBy(candidate => candidate.Identity.GameId, StringComparer.Ordinal)
            .ToArray();

        return Task.FromResult<IReadOnlyList<GameDiscoveryCandidate>>(candidates);
    }

    private static NormalizedRegistration? TryNormalize(UbisoftInstallRegistration? registration)
    {
        if (registration is null
            || !ulong.TryParse(
                registration.InstallId?.Trim(),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var installIdValue)
            || installIdValue == 0
            || string.IsNullOrWhiteSpace(registration.InstallDirectory))
        {
            return null;
        }

        string installDirectory;
        try
        {
            var rawPath = registration.InstallDirectory.Trim();
            if (!Path.IsPathFullyQualified(rawPath)) return null;

            installDirectory = Path.GetFullPath(rawPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(installDirectory) || !Directory.Exists(installDirectory)) return null;

        var displayName = string.IsNullOrWhiteSpace(registration.DisplayName)
            ? null
            : registration.DisplayName.Trim();
        var evidenceKey = string.IsNullOrWhiteSpace(registration.EvidenceKey)
            ? $@"HKLM\...\Ubisoft\Launcher\Installs\{installIdValue.ToString(CultureInfo.InvariantCulture)}"
            : registration.EvidenceKey.Trim();

        return new NormalizedRegistration(
            installIdValue,
            installDirectory,
            displayName,
            evidenceKey);
    }

    private static GameDiscoveryCandidate CreateCandidate(
        IGrouping<ulong, NormalizedRegistration> group)
    {
        var installId = group.Key.ToString(CultureInfo.InvariantCulture);
        var installPaths = group
            .Select(registration => registration.InstallDirectory)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var displayNames = group
            .Select(registration => registration.DisplayName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var evidenceKeys = group
            .Select(registration => registration.EvidenceKey)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
            .ThenBy(key => key, StringComparer.Ordinal)
            .ToArray();

        var name = displayNames.FirstOrDefault() ?? $"Ubisoft game {installId}";
        var evidence = $"Ubisoft registry install id {installId}";
        if (evidenceKeys.Length > 0)
            evidence += $" · keys [{string.Join(" | ", evidenceKeys)}]";
        if (displayNames.Length > 0)
            evidence += $" · DisplayName [{string.Join(" | ", displayNames)}]";

        return new GameDiscoveryCandidate
        {
            Identity = new GameIdentity
            {
                GameId = $"ubisoft:{installId}",
                Name = name,
                Launcher = GameLauncherKind.Ubisoft,
                Engine = GameEngineKind.Unknown,
                Executables = Array.Empty<string>(),
                InstallPaths = installPaths,
                AuxiliaryProcesses = Array.Empty<string>(),
                AdapterId = "generic"
            },
            Confidence = displayNames.Length > 0 ? 0.98 : 0.96,
            Evidence = evidence
        };
    }

    private static IReadOnlyList<UbisoftInstallRegistration> ReadRegistryRegistrations(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) return Array.Empty<UbisoftInstallRegistration>();
        return ReadWindowsRegistryRegistrations(cancellationToken);
    }

    [SupportedOSPlatform("windows")]
    private static IReadOnlyList<UbisoftInstallRegistration> ReadWindowsRegistryRegistrations(
        CancellationToken cancellationToken)
    {
        var registrations = new List<UbisoftInstallRegistration>();

        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            cancellationToken.ThrowIfCancellationRequested();

            RegistryKey? localMachine = null;
            RegistryKey? installs = null;
            try
            {
                localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                installs = localMachine.OpenSubKey(InstallsSubKey, writable: false);
                if (installs is null) continue;

                string[] installIds;
                try
                {
                    installIds = installs.GetSubKeyNames()
                        .OrderBy(value => value, StringComparer.Ordinal)
                        .ToArray();
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or SecurityException)
                {
                    continue;
                }

                foreach (var installId in installIds)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        using var installKey = installs.OpenSubKey(installId, writable: false);
                        var installDirectory = installKey?.GetValue("InstallDir") as string;
                        if (string.IsNullOrWhiteSpace(installDirectory)) continue;

                        var displayName = ReadDisplayName(localMachine, installId);
                        registrations.Add(new UbisoftInstallRegistration(
                            installId,
                            installDirectory,
                            displayName,
                            $@"HKLM[{view}]\{InstallsSubKey}\{installId}"));
                    }
                    catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or SecurityException)
                    {
                        // One inaccessible/stale game registration must not hide
                        // other Ubisoft installations from the local catalog.
                    }
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or SecurityException)
            {
                // The other registry view may still contain valid registrations.
            }
            finally
            {
                installs?.Dispose();
                localMachine?.Dispose();
            }
        }

        return registrations;
    }

    [SupportedOSPlatform("windows")]
    private static string? ReadDisplayName(RegistryKey localMachine, string installId)
    {
        try
        {
            using var uninstall = localMachine.OpenSubKey(
                UninstallSubKeyPrefix + installId,
                writable: false);
            return uninstall?.GetValue("DisplayName") as string;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or SecurityException)
        {
            return null;
        }
    }

    private sealed record NormalizedRegistration(
        ulong InstallIdValue,
        string InstallDirectory,
        string? DisplayName,
        string EvidenceKey);
}
