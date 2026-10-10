using System.Runtime.InteropServices;
using FFPerformanceEngine.Core.Telemetry;

namespace FFPerformanceEngine.App;

public sealed class WindowsProcessorPowerInfoProvider : IProcessorPowerInfoProvider
{
    private const int ProcessorInformation = 11;
    private const ushort AllProcessorGroups = 0xFFFF;
    private const uint MaximumSupportedProcessorCount = 4096;

    public ProcessorPowerSnapshot? Capture()
    {
        if (!OperatingSystem.IsWindows()) return null;

        var processorCount = GetActiveProcessorCount(AllProcessorGroups);
        if (processorCount == 0 || processorCount > MaximumSupportedProcessorCount)
            return null;

        try
        {
            var buffer = new ProcessorPowerInformation[checked((int)processorCount)];
            var structureSize = Marshal.SizeOf<ProcessorPowerInformation>();
            var byteLength = checked((uint)(structureSize * buffer.Length));
            var status = CallNtPowerInformation(
                ProcessorInformation,
                IntPtr.Zero,
                0,
                buffer,
                byteLength);
            if (status != 0) return null;

            var observations = buffer
                .Select(info => new ProcessorPowerObservation(
                    info.Number,
                    info.MaxMhz,
                    info.CurrentMhz,
                    info.MhzLimit))
                .ToArray();

            return new ProcessorPowerSnapshot(
                DateTimeOffset.UtcNow,
                checked((int)processorCount),
                observations);
        }
        catch (Exception ex) when (ex is OverflowException
                                   or DllNotFoundException
                                   or EntryPointNotFoundException
                                   or TypeLoadException)
        {
            return null;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessorPowerInformation
    {
        public uint Number;
        public uint MaxMhz;
        public uint CurrentMhz;
        public uint MhzLimit;
        public uint MaxIdleState;
        public uint CurrentIdleState;
    }

    [DllImport("kernel32.dll", SetLastError = false)]
    private static extern uint GetActiveProcessorCount(ushort groupNumber);

    [DllImport("powrprof.dll", SetLastError = false)]
    private static extern int CallNtPowerInformation(
        int informationLevel,
        IntPtr inputBuffer,
        uint inputBufferLength,
        [Out] ProcessorPowerInformation[] outputBuffer,
        uint outputBufferLength);
}
