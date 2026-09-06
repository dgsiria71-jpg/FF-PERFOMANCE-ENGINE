using FFPerformanceEngine.Core.Interop;
using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed class TelemetryService
{
    private NativeCpuTimes? _previousCpu;

    public TelemetrySample CaptureSystemSample()
    {
        double? cpu = null;
        double? totalGb = null;
        double? usedGb = null;

        if (OperatingSystem.IsWindows())
        {
            try
            {
                if (NativeApi.GetCpuTimes(out var current) == 0)
                {
                    if (_previousCpu is NativeCpuTimes previous)
                    {
                        var idle = current.Idle100Ns - previous.Idle100Ns;
                        var kernel = current.Kernel100Ns - previous.Kernel100Ns;
                        var user = current.User100Ns - previous.User100Ns;
                        var total = kernel + user;
                        if (total > 0) cpu = Math.Clamp(100d * (1d - idle / (double)total), 0, 100);
                    }
                    _previousCpu = current;
                }

                if (NativeApi.GetMemoryInfo(out var memory) == 0)
                {
                    totalGb = memory.TotalPhysicalBytes / 1073741824d;
                    usedGb = (memory.TotalPhysicalBytes - memory.AvailablePhysicalBytes) / 1073741824d;
                }
            }
            catch (DllNotFoundException) { }
            catch (EntryPointNotFoundException) { }
        }

        return new TelemetrySample
        {
            CpuPercent = cpu,
            MemoryUsedGb = usedGb,
            MemoryTotalGb = totalGb,
            DataQuality = cpu is not null || totalGb is not null ? "System" : "Unavailable"
        };
    }

    public static TelemetrySample WithFrameMetrics(TelemetrySample system, double fps, double? latencyMs = null)
        => system with { Fps = fps, FrameTimeMs = fps > 0 ? 1000d / fps : null, LatencyMs = latencyMs, DataQuality = "Frame+System" };
}
