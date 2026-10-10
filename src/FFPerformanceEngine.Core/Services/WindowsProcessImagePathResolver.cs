using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace FFPerformanceEngine.Core.Services;

/// <summary>
/// Resolves one physical Windows process image path without requiring the broad
/// module-enumeration rights used by Process.MainModule. MainModule remains the
/// preferred path; PROCESS_QUERY_LIMITED_INFORMATION is a strict fallback for
/// processes such as BlueStacks HD-Player that expose lifetime/start-time but
/// return no MainModule filename to an unelevated caller.
/// </summary>
public sealed class WindowsProcessImagePathResolver
{
    private const uint ProcessQueryLimitedInformation = 0x1000;
    private const int MaximumWindowsPathCharacters = 32768;

    public string? Resolve(int processId)
    {
        if (!OperatingSystem.IsWindows() || processId <= 0)
            return null;

        try
        {
            using var process = Process.GetProcessById(processId);
            if (process.HasExited)
                return null;

            var mainModule = NormalizeFullPath(process.MainModule?.FileName);
            if (mainModule is not null)
                return mainModule;
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or Win32Exception
            or InvalidOperationException
            or NotSupportedException
            or UnauthorizedAccessException
            or IOException)
        {
        }

        return ResolveWithLimitedInformation(processId);
    }

    public string? ResolveWithLimitedInformation(int processId)
    {
        if (!OperatingSystem.IsWindows() || processId <= 0)
            return null;

        using var handle = OpenProcess(
            ProcessQueryLimitedInformation,
            inheritHandle: false,
            processId);
        if (handle.IsInvalid)
            return null;

        var capacity = MaximumWindowsPathCharacters;
        var builder = new StringBuilder(capacity);
        if (!QueryFullProcessImageName(
                handle,
                flags: 0,
                builder,
                ref capacity))
            return null;

        return NormalizeFullPath(builder.ToString());
    }

    private static string? NormalizeFullPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
            return null;

        try
        {
            return Path.GetFullPath(path.Trim());
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or NotSupportedException
            or PathTooLongException
            or IOException)
        {
            return null;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeProcessHandle OpenProcess(
        uint desiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
        int processId);

    [DllImport(
        "kernel32.dll",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryFullProcessImageName(
        SafeProcessHandle process,
        int flags,
        StringBuilder executablePath,
        ref int size);
}
