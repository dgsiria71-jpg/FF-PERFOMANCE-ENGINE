using System.Runtime.InteropServices;

namespace FFPerformanceEngine.Core.Services;

public interface IRecentInputProbe
{
    bool HasRecentInput();
}

public sealed class WindowsRecentInputProbe : IRecentInputProbe
{
    private readonly TimeSpan _threshold;

    public WindowsRecentInputProbe(TimeSpan? threshold = null)
    {
        _threshold = threshold ?? TimeSpan.FromSeconds(5);
        if (_threshold < TimeSpan.Zero || _threshold.TotalMilliseconds > uint.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(threshold));
    }

    public bool HasRecentInput()
    {
        if (!OperatingSystem.IsWindows()) return false;

        var info = new LastInputInfo
        {
            Size = (uint)Marshal.SizeOf<LastInputInfo>()
        };

        if (!GetLastInputInfo(ref info)) return false;
        var currentTick = unchecked((uint)Environment.TickCount);
        return IsRecent(currentTick, info.Time, _threshold);
    }

    public static bool IsRecent(uint currentTick, uint lastInputTick, TimeSpan threshold)
    {
        if (threshold < TimeSpan.Zero || threshold.TotalMilliseconds > uint.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(threshold));

        var elapsed = unchecked(currentTick - lastInputTick);
        return elapsed <= (uint)Math.Ceiling(threshold.TotalMilliseconds);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint Size;
        public uint Time;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLastInputInfo(ref LastInputInfo inputInfo);
}
