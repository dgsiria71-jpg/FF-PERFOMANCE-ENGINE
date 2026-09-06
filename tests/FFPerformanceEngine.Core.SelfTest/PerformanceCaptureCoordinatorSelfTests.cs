using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class PerformanceCaptureCoordinatorSelfTests
{
    public static async Task RunAsync()
    {
        var calls = 0;
        var capturedPid = 0;
        var capturedAt = new DateTimeOffset(2026, 9, 6, 2, 0, 0, TimeSpan.Zero);
        var measuredSample = new TelemetrySample
        {
            Timestamp = capturedAt,
            Fps = 144.5,
            OnePercentLow = 132.0,
            FrameTimeMs = 6.92,
            DataQuality = "Measured"
        };
        var timeline = new PerformanceTimelineBuffer(capacity: 4);
        var coordinator = new PerformanceCaptureCoordinator((processId, duration, cancellationToken) =>
        {
            calls++;
            capturedPid = processId;
            return Task.FromResult<TelemetrySample?>(measuredSample);
        }, timeline);

        var unavailable = await coordinator.CaptureAsync(null, TimeSpan.FromSeconds(2));
        Require(!unavailable.Captured && unavailable.Sample is null && calls == 0,
            "Performance capture must not invoke the frame provider without an exact Guardian binding.");
        Require(timeline.Snapshot().Count == 0,
            "Blocked Performance capture must not manufacture timeline telemetry.");

        var status = new GuardianLiveSessionStatus
        {
            Binding = new GuardianSessionBinding(4321, "Pie64"),
            Instance = new BlueStacksInstance { Name = "Pie64" }
        };
        var captured = await coordinator.CaptureAsync(status, TimeSpan.FromSeconds(2));
        Require(captured.Captured && captured.Sample?.Fps == 144.5,
            "A valid exact-PID capture must return the measured telemetry sample.");
        Require(calls == 1 && capturedPid == 4321 && captured.Target.ProcessId == 4321 && captured.Target.InstanceName == "Pie64",
            "Performance capture must forward only the exact Guardian-bound PID and instance to the frame provider.");

        var entries = timeline.Snapshot();
        Require(entries.Count == 1 && entries[0].Kind == PerformanceTimelineKind.Telemetry,
            "A successful Performance capture must append exactly one telemetry entry to the synchronized timeline.");
        Require(entries[0].Timestamp == capturedAt && entries[0].Telemetry == measuredSample,
            "Timeline integration must preserve the exact measured sample and timestamp without recomputing evidence.");

        Console.WriteLine("PASS Performance exact-PID capture coordinator and timeline integration");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
