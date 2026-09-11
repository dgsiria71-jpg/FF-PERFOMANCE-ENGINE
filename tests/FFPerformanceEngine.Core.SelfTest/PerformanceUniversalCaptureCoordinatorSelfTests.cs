using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;

internal static class PerformanceUniversalCaptureCoordinatorSelfTests
{
    internal static async Task RunAsync()
    {
        var typedCalls = 0;
        var legacyCalls = 0;
        var capturedPid = 0;
        var timestamp = new DateTimeOffset(2026, 9, 9, 17, 20, 0, TimeSpan.Zero);
        var frame = new TelemetryFrame(timestamp,
        [
            new TelemetryMetricObservation(
                TelemetryStandardMetrics.FrameFpsAverage,
                240,
                TelemetryMetricQuality.Measured,
                1,
                "presentmon",
                TelemetryMetricOrigin.Direct),
            new TelemetryMetricObservation(
                TelemetryStandardMetrics.FrameTimeAverageMs,
                4.17,
                TelemetryMetricQuality.Measured,
                1,
                "presentmon",
                TelemetryMetricOrigin.Direct)
        ]);
        var timeline = new PerformanceTimelineBuffer(capacity: 8);
        var coordinator = new PerformanceCaptureCoordinator(
            (processId, duration, cancellationToken) =>
            {
                legacyCalls++;
                return Task.FromResult<TelemetrySample?>(new TelemetrySample { Fps = 1 });
            },
            timeline,
            (processId, duration, cancellationToken) =>
            {
                typedCalls++;
                capturedPid = processId;
                return Task.FromResult<TelemetryFrame?>(frame);
            });

        var unavailable = new TelemetryWorkloadTarget
        {
            GameId = "steam:730",
            BindingQuality = TelemetryWorkloadBindingQuality.UnavailableRunningProcess
        };
        var blocked = await coordinator.CaptureWorkloadTypedAsync(unavailable, TimeSpan.FromSeconds(2));
        Require(!blocked.Captured
                && blocked.Frame is null
                && typedCalls == 0
                && legacyCalls == 0
                && timeline.Snapshot().Count == 0,
            "Unavailable universal workload target must fail closed before invoking any telemetry provider or timeline append.");

        var ambiguous = new TelemetryWorkloadTarget
        {
            GameId = "steam:730",
            BindingQuality = TelemetryWorkloadBindingQuality.AmbiguousRunningProcess
        };
        var ambiguousResult = await coordinator.CaptureWorkloadTypedAsync(ambiguous, TimeSpan.FromSeconds(2));
        Require(!ambiguousResult.Captured
                && typedCalls == 0
                && legacyCalls == 0
                && timeline.Snapshot().Count == 0,
            "Ambiguous universal workload target must never guess a process or fall back to legacy telemetry.");

        var executablePath = Path.GetFullPath(
            Path.Combine(Path.GetTempPath(), "DG-Universal-Capture", "cs2.exe"));
        var exact = new TelemetryWorkloadTarget
        {
            GameId = "steam:730",
            ProcessId = 7730,
            ExecutablePath = executablePath,
            BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess
        };
        var captured = await coordinator.CaptureWorkloadTypedAsync(exact, TimeSpan.FromSeconds(2));
        Require(captured.Captured
                && ReferenceEquals(captured.Frame, frame)
                && captured.Target.GameId == "steam:730"
                && captured.Target.ProcessId == 7730
                && typedCalls == 1
                && capturedPid == 7730,
            "Exact universal workload target must invoke direct typed capture for exactly the resolved PID and return the same TelemetryFrame.");
        Require(legacyCalls == 0,
            "Universal typed capture must never fall back to the legacy TelemetrySample provider.");

        var entries = timeline.Snapshot();
        Require(entries.Count == 1
                && entries[0].Telemetry is null
                && ReferenceEquals(entries[0].TypedTelemetry, frame)
                && entries[0].Timestamp == timestamp,
            "Successful universal capture must append exactly the direct typed frame to the synchronized Performance timeline.");

        var legacyOnly = new PerformanceCaptureCoordinator(
            (processId, duration, cancellationToken) => Task.FromResult<TelemetrySample?>(new TelemetrySample { Fps = 1 }),
            new PerformanceTimelineBuffer(capacity: 2));
        var noTypedProvider = await legacyOnly.CaptureWorkloadTypedAsync(exact, TimeSpan.FromSeconds(2));
        Require(!noTypedProvider.Captured
                && noTypedProvider.Frame is null
                && noTypedProvider.Message.Contains("typed", StringComparison.OrdinalIgnoreCase),
            "Exact universal target without a typed provider must fail closed instead of adapting legacy telemetry.");

        Console.WriteLine("PASS Track 4 universal Performance capture uses exact selected workload PID and direct typed telemetry only");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
