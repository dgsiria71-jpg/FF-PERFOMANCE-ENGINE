using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class PerformancePresentationSelfTests
{
    public static void Run()
    {
        var unavailable = PerformancePresentation.FromCapture(new PerformanceCaptureResult
        {
            Target = new PerformanceCaptureTarget(),
            Message = "Waiting for exact binding"
        });
        Require(!unavailable.HasMeasurement && unavailable.Fps == "—" && unavailable.OnePercentLow == "—" && unavailable.FrameTime == "—" && unavailable.Latency == "—",
            "Unavailable Performance evidence must remain unavailable in the UI presentation.");
        Require(unavailable.ProcessId == "—" && unavailable.Instance == "—",
            "Performance presentation must not invent process or instance identity without an exact binding.");

        var measured = PerformancePresentation.FromCapture(new PerformanceCaptureResult
        {
            Target = new PerformanceCaptureTarget { ProcessId = 4321, InstanceName = "Pie64" },
            Sample = new TelemetrySample
            {
                Fps = 144.5,
                OnePercentLow = 132.2,
                PointOnePercentLow = 119.8,
                FrameTimeMs = 6.92,
                FrameTimeP95Ms = 8.25,
                FrameTimeP99Ms = 10.4,
                StutterPercent = 0.6,
                LatencyMs = 13.8,
                DataQuality = "PresentMon · 2400 frames"
            },
            Message = "Measured exact PID"
        });

        Require(measured.HasMeasurement && measured.ProcessId == "4321" && measured.Instance == "Pie64",
            "Measured Performance presentation must retain the exact capture target identity.");
        Require(measured.Fps.Contains("144.5") && measured.OnePercentLow.Contains("132.2") && measured.PointOnePercentLow.Contains("119.8"),
            "Measured FPS evidence must be presented from the real capture sample.");
        Require(measured.FrameTime.Contains("6.92") && measured.P95FrameTime.Contains("8.25") && measured.P99FrameTime.Contains("10.40") && measured.Latency.Contains("13.8"),
            "Measured frame-time and latency evidence must be presented without substitution.");
        Require(measured.DataQuality.Contains("PresentMon") && measured.Detail == "Measured exact PID",
            "Performance presentation must expose data quality and the coordinator result detail.");

        Console.WriteLine("PASS Performance evidence-only presentation contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
