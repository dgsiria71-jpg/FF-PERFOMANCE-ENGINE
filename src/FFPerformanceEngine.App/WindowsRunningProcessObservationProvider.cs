using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using FFPerformanceEngine.Core.Workloads;

namespace FFPerformanceEngine.App;

/// <summary>
/// Windows-only, read-only process enumerator for the Track 3 evidence plane.
/// This provider reports runtime facts only; it never decides whether a process
/// is a game and never derives a GameId from process/file names.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsRunningProcessObservationProvider : IRunningProcessObservationProvider
{
    public Task<IReadOnlyList<RunningProcessObservation>> ObserveAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var observedAt = DateTimeOffset.UtcNow;
        var result = new List<RunningProcessObservation>();

        foreach (var process in Process.GetProcesses().OrderBy(process => process.Id))
        {
            using (process)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (process.Id <= 0) continue;

                string? executablePath;
                try
                {
                    executablePath = process.MainModule?.FileName;
                }
                catch (Exception ex) when (IsProcessInspectionFailure(ex))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(executablePath)
                    || !Path.IsPathFullyQualified(executablePath))
                {
                    continue;
                }

                string? displayName = null;
                try
                {
                    displayName = process.ProcessName;
                }
                catch (Exception ex) when (IsProcessInspectionFailure(ex))
                {
                    // Optional evidence only. A proven executable path remains useful.
                }

                result.Add(new RunningProcessObservation
                {
                    ProcessId = process.Id,
                    ExecutablePath = executablePath,
                    DisplayName = displayName,
                    ObservedAtUtc = observedAt
                });
            }
        }

        IReadOnlyList<RunningProcessObservation> ordered = result
            .OrderBy(item => item.ProcessId)
            .ThenBy(item => item.ExecutablePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ExecutablePath, StringComparer.Ordinal)
            .ToArray();
        return Task.FromResult(ordered);
    }

    private static bool IsProcessInspectionFailure(Exception exception)
        => exception is InvalidOperationException
           or NotSupportedException
           or Win32Exception
           or UnauthorizedAccessException;
}
