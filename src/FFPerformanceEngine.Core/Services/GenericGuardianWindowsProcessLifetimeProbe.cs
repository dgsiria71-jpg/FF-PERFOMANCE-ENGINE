using System.ComponentModel;
using System.Diagnostics;

namespace FFPerformanceEngine.Core.Services;

/// <summary>
/// An observation actually read from an open Windows process handle. Only the
/// Windows probe constructs this value; a caller-supplied Guid/PID/path cannot
/// stand in for a physical process creation time. This does NOT prove game
/// scene, mode, workload load or that another process is not benchmarking.
/// </summary>
public sealed class GenericGuardianWindowsProcessLifetimeSnapshot
{
    internal GenericGuardianWindowsProcessLifetimeSnapshot(
        int processId, string executablePath, DateTimeOffset createdAtUtc)
    {
        ProcessId = processId;
        ExecutablePath = executablePath;
        CreatedAtUtc = createdAtUtc;
    }

    public int ProcessId { get; }
    public string ExecutablePath { get; }
    public DateTimeOffset CreatedAtUtc { get; }
}

/// <summary>
/// Read-only OS-backed process lifetime observation for a future Guardian host.
/// A PID, even combined with a stable GameId and path, may be reused. The
/// owning host must verify this snapshot before AND after physical windows,
/// assign its own session epoch and separately prove the actual gameplay scene.
/// Access denied, exit, mismatched executable or unavailable creation time
/// always leaves lifecycle evidence unavailable.
/// </summary>
public sealed class GenericGuardianWindowsProcessLifetimeProbe
{
    public GenericGuardianWindowsProcessLifetimeSnapshot? Observe(
        int processId, string? expectedExecutablePath)
    {
        if (!OperatingSystem.IsWindows() || processId <= 0)
            return null;

        var expected = NormalizeFullPath(expectedExecutablePath);
        if (expected is null)
            return null;

        try
        {
            using var process = Process.GetProcessById(processId);
            if (process.HasExited)
                return null;

            var actual = NormalizeFullPath(process.MainModule?.FileName);
            if (actual is null || !string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                return null;

            var createdAtUtc = process.StartTime.ToUniversalTime();
            if (createdAtUtc == DateTime.MinValue || process.HasExited)
                return null;

            return new GenericGuardianWindowsProcessLifetimeSnapshot(
                processId, actual, new DateTimeOffset(createdAtUtc));
        }
        catch (Exception ex) when (ex is ArgumentException or Win32Exception
                                   or InvalidOperationException or NotSupportedException
                                   or UnauthorizedAccessException or IOException)
        {
            // Lack of permission is never positive evidence of process continuity.
            return null;
        }
    }

    public bool IsStillSameProcess(
        GenericGuardianWindowsProcessLifetimeSnapshot? previous,
        int processId,
        string? expectedExecutablePath)
    {
        if (previous is null || previous.ProcessId != processId)
            return false;

        var current = Observe(processId, expectedExecutablePath);
        return current is not null
               && current.CreatedAtUtc == previous.CreatedAtUtc
               && string.Equals(current.ExecutablePath, previous.ExecutablePath, StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeFullPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
            return null;

        try { return Path.GetFullPath(path.Trim()); }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException
                                   or PathTooLongException or IOException)
        {
            return null;
        }
    }
}
