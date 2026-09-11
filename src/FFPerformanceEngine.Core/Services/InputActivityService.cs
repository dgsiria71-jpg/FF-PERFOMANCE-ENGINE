using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FFPerformanceEngine.Core.Services;

public sealed class InputActivityService
{
    private readonly TimeSpan _defaultThreshold;

    public InputActivityService(TimeSpan? defaultThreshold = null)
    {
        _defaultThreshold = defaultThreshold ?? TimeSpan.FromSeconds(2);
    }

    public bool IsRecentBlueStacksInteraction()
        => IsRecentBlueStacksInteraction(_defaultThreshold);

    public bool IsRecentBlueStacksInteraction(TimeSpan threshold)
    {
        if (!OperatingSystem.IsWindows() || threshold < TimeSpan.Zero) return false;
        if (!TryGetLastInputTick(out var lastInputTick)) return false;
        var nowTick = unchecked((uint)Environment.TickCount);
        if (!IsRecentTick(nowTick, lastInputTick, ToThresholdMilliseconds(threshold))) return false;
        return IsBlueStacksForeground();
    }

    public bool IsBlueStacksForeground()
    {
        if (!OperatingSystem.IsWindows()) return false;
        var window = GetForegroundWindow();
        if (window == IntPtr.Zero) return false;
        _ = GetWindowThreadProcessId(window, out var processId);
        if (processId == 0) return false;

        try
        {
            using var process = Process.GetProcessById(unchecked((int)processId));
            return IsBlueStacksProcessName(process.ProcessName);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    public static bool IsRecentTick(uint nowTick, uint lastInputTick, uint thresholdMs)
    {
        var elapsed = unchecked(nowTick - lastInputTick);
        return elapsed <= thresholdMs;
    }

    public static bool IsBlueStacksProcessName(string? processName)
        => string.Equals(processName, "HD-Player", StringComparison.OrdinalIgnoreCase)
           || string.Equals(processName, "BlueStacksAppplayer", StringComparison.OrdinalIgnoreCase)
           || string.Equals(processName, "BlueStacks", StringComparison.OrdinalIgnoreCase);

    private static bool TryGetLastInputTick(out uint lastInputTick)
    {
        var info = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() };
        if (!GetLastInputInfo(ref info))
        {
            lastInputTick = 0;
            return false;
        }
        lastInputTick = info.Time;
        return true;
    }

    private static uint ToThresholdMilliseconds(TimeSpan threshold)
    {
        var milliseconds = Math.Clamp(threshold.TotalMilliseconds, 0, uint.MaxValue);
        return (uint)Math.Ceiling(milliseconds);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint Size;
        public uint Time;
    }

    [DllImport("user32.dll", SetLastError = false)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLastInputInfo(ref LastInputInfo info);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
}
