using System.IO;
using System.Runtime.Versioning;
using System.Security;
using FFPerformanceEngine.Core.Workloads;
using Microsoft.Win32;

namespace FFPerformanceEngine.App;

/// <summary>
/// Read-only Windows App Paths enumerator. The default value of an App Paths
/// registration is treated only as evidence of a known executable when it is a
/// fully-qualified path to a file that currently exists. Registry key names and
/// paths are never promoted to durable game identity here.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsAppPathsKnownExecutableObservationProvider : IKnownExecutableObservationProvider
{
    private const string AppPathsKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";

    public Task<IReadOnlyList<KnownExecutableObservation>> ObserveAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var observedAt = DateTimeOffset.UtcNow;
        var observations = new List<KnownExecutableObservation>();
        var views = Environment.Is64BitOperatingSystem
            ? new[] { RegistryView.Registry64, RegistryView.Registry32 }
            : new[] { RegistryView.Registry32 };

        foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
        {
            foreach (var view in views)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ObserveRegistryView(hive, view, observedAt, observations, cancellationToken);
            }
        }

        IReadOnlyList<KnownExecutableObservation> ordered = observations
            .OrderBy(item => item.ExecutablePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ExecutablePath, StringComparer.Ordinal)
            .ThenBy(item => item.RegistrationName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.RegistrationName, StringComparer.Ordinal)
            .ToArray();
        return Task.FromResult(ordered);
    }

    private static void ObserveRegistryView(
        RegistryHive hive,
        RegistryView view,
        DateTimeOffset observedAt,
        ICollection<KnownExecutableObservation> observations,
        CancellationToken cancellationToken)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var appPaths = baseKey.OpenSubKey(AppPathsKey, writable: false);
            if (appPaths is null) return;

            string[] subKeyNames;
            try
            {
                subKeyNames = appPaths.GetSubKeyNames();
            }
            catch (Exception ex) when (IsRegistryReadFailure(ex))
            {
                return;
            }

            foreach (var subKeyName in subKeyNames
                         .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(name => name, StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(subKeyName)) continue;

                try
                {
                    using var registration = appPaths.OpenSubKey(subKeyName, writable: false);
                    if (registration is null) continue;

                    var rawPath = registration.GetValue(null) as string;
                    var executablePath = NormalizeExistingExecutable(rawPath);
                    if (executablePath is null) continue;

                    observations.Add(new KnownExecutableObservation
                    {
                        ExecutablePath = executablePath,
                        RegistrationName = subKeyName.Trim(),
                        ObservedAtUtc = observedAt
                    });
                }
                catch (Exception ex) when (IsRegistryReadFailure(ex))
                {
                    // One inaccessible/stale registration must not suppress other
                    // App Paths evidence from the same hive/view.
                }
            }
        }
        catch (Exception ex) when (IsRegistryReadFailure(ex))
        {
            // A protected/unavailable hive or view is isolated from all others.
        }
    }

    private static string? NormalizeExistingExecutable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Path.IsPathFullyQualified(value.Trim()))
            return null;

        try
        {
            var normalized = Path.GetFullPath(value.Trim());
            return File.Exists(normalized) ? normalized : null;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private static bool IsRegistryReadFailure(Exception exception)
        => exception is SecurityException
           or UnauthorizedAccessException
           or IOException
           or ArgumentException;
}
