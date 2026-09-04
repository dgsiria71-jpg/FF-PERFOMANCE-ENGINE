using System.Runtime.InteropServices;
using FFPerformanceEngine.Core.Interop;
using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed class EnvironmentProbe
{
    private readonly BlueStacksService _blueStacks;
    public EnvironmentProbe(BlueStacksService blueStacks) => _blueStacks = blueStacks;

    public EnvironmentSnapshot Capture()
    {
        double? totalGb = null;
        if (OperatingSystem.IsWindows())
        {
            try
            {
                if (NativeApi.GetMemoryInfo(out var info) == 0) totalGb = info.TotalPhysicalBytes / 1073741824d;
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
        }

        var processes = _blueStacks.DetectProcesses();
        return new EnvironmentSnapshot
        {
            WindowsDescription = RuntimeInformation.OSDescription,
            LogicalProcessors = Environment.ProcessorCount,
            MemoryTotalGb = totalGb,
            BlueStacksDetected = processes.Count > 0 || _blueStacks.FindConfigPath() is not null,
            BlueStacksPath = _blueStacks.FindConfigPath(),
            BlueStacksProcesses = processes,
            Instances = _blueStacks.LoadInstances(),
            ActiveGame = DetectGame()
        };
    }

    private static GameKind DetectGame()
    {
        return GameKind.None;
    }
}
