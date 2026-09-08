using System.Runtime.Versioning;
using FFPerformanceEngine.Core.Workloads;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using Windows.Storage;

namespace FFPerformanceEngine.App;

/// <summary>
/// Windows-only adapter that turns the current user's installed package catalog
/// into neutral Core observations. Construction is side-effect free; package
/// enumeration and MicrosoftGame.config reads happen only on explicit discovery.
/// </summary>
[SupportedOSPlatform("windows10.0.19041")]
public sealed class WindowsMicrosoftStoreGamePackageProvider : IMicrosoftStoreGamePackageProvider
{
    private const ulong MaxMicrosoftGameConfigBytes = 4UL * 1024UL * 1024UL;

    public async Task<IReadOnlyList<MicrosoftStoreGamePackageObservation>> EnumerateAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var packageManager = new PackageManager();
        var packages = packageManager
            .FindPackagesForUser(string.Empty)
            .OrderBy(package => package.Id?.FamilyName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(package => package.Id?.FullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var observations = new List<MicrosoftStoreGamePackageObservation>(packages.Length);
        foreach (var package in packages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var observation = await ObservePackageAsync(package, cancellationToken)
                    .ConfigureAwait(false);
                if (observation is not null) observations.Add(observation);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (IsPackageObservationFailure(ex))
            {
                // One inaccessible, stale or partially registered package must not
                // suppress evidence from the rest of the current user's catalog.
            }
        }

        return observations
            .OrderBy(observation => observation.PackageFamilyName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(observation => observation.PackageFullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static async Task<MicrosoftStoreGamePackageObservation?> ObservePackageAsync(
        Package package,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var id = package.Id;
        if (id is null || string.IsNullOrWhiteSpace(id.FamilyName)) return null;

        StorageFolder? installedLocation = null;
        StorageFolder? effectiveLocation = null;

        try
        {
            installedLocation = package.InstalledLocation;
        }
        catch (Exception ex) when (IsPackageObservationFailure(ex))
        {
            // Keep the package observation possible when only the original
            // protected location is unavailable but an effective location exists.
        }

        try
        {
            effectiveLocation = package.EffectiveLocation;
        }
        catch (Exception ex) when (IsPackageObservationFailure(ex))
        {
            // EffectiveLocation is best-effort; InstalledLocation remains valid
            // provenance when mutable/external package metadata is unavailable.
        }

        var configXml = await TryReadMicrosoftGameConfigAsync(
                effectiveLocation,
                cancellationToken)
            .ConfigureAwait(false);

        if (configXml is null
            && installedLocation is not null
            && !SameStoragePath(installedLocation, effectiveLocation))
        {
            configXml = await TryReadMicrosoftGameConfigAsync(
                    installedLocation,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();

        return new MicrosoftStoreGamePackageObservation(
            PackageFamilyName: id.FamilyName ?? string.Empty,
            PackageFullName: id.FullName ?? string.Empty,
            PackageName: id.Name ?? string.Empty,
            DisplayName: TryReadDisplayName(package),
            InstalledPath: TryReadStoragePath(installedLocation),
            EffectivePath: TryReadStoragePath(effectiveLocation),
            MicrosoftGameConfigXml: configXml,
            IsFramework: package.IsFramework,
            IsResourcePackage: package.IsResourcePackage,
            IsBundle: package.IsBundle,
            IsOptional: package.IsOptional);
    }

    private static async Task<string?> TryReadMicrosoftGameConfigAsync(
        StorageFolder? folder,
        CancellationToken cancellationToken)
    {
        if (folder is null) return null;
        cancellationToken.ThrowIfCancellationRequested();

        IStorageItem? item;
        try
        {
            item = await folder.TryGetItemAsync("MicrosoftGame.config");
        }
        catch (Exception ex) when (IsPackageObservationFailure(ex))
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (item is not StorageFile file) return null;

        try
        {
            var properties = await file.GetBasicPropertiesAsync();
            cancellationToken.ThrowIfCancellationRequested();
            if (properties.Size == 0 || properties.Size > MaxMicrosoftGameConfigBytes)
                return null;

            var text = await FileIO.ReadTextAsync(file);
            cancellationToken.ThrowIfCancellationRequested();
            return text;
        }
        catch (Exception ex) when (IsPackageObservationFailure(ex))
        {
            return null;
        }
    }

    private static string? TryReadDisplayName(Package package)
    {
        try
        {
            return package.DisplayName;
        }
        catch (Exception ex) when (IsPackageObservationFailure(ex))
        {
            return null;
        }
    }

    private static string? TryReadStoragePath(StorageFolder? folder)
    {
        if (folder is null) return null;
        try
        {
            return folder.Path;
        }
        catch (Exception ex) when (IsPackageObservationFailure(ex))
        {
            return null;
        }
    }

    private static bool SameStoragePath(StorageFolder left, StorageFolder? right)
    {
        if (right is null) return false;
        var leftPath = TryReadStoragePath(left);
        var rightPath = TryReadStoragePath(right);
        return !string.IsNullOrWhiteSpace(leftPath)
               && !string.IsNullOrWhiteSpace(rightPath)
               && string.Equals(leftPath, rightPath, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPackageObservationFailure(Exception exception)
        => exception is UnauthorizedAccessException
           or IOException
           or InvalidOperationException
           or NotSupportedException
           or ArgumentException
           or System.Runtime.InteropServices.COMException;
}
