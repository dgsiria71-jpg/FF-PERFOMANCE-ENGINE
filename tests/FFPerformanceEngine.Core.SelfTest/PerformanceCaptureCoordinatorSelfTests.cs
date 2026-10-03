using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;

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
        var timeline = new PerformanceTimelineBuffer(capacity: 8);
        var typedCalls = 0;
        var typedCapturedPid = 0;
        var measuredFrame = new TelemetryFrame(capturedAt.AddSeconds(10),
        [
            Metric(TelemetryStandardMetrics.FrameFpsAverage, 165.5, 0.92),
            Metric(TelemetryStandardMetrics.FrameFpsLow1, 151.0, 0.92),
            Metric(TelemetryStandardMetrics.FrameFpsLow01, 145.0, 0.92),
            Metric(TelemetryStandardMetrics.FrameTimeAverageMs, 6.04, 0.92),
            Metric(TelemetryStandardMetrics.FrameTimeP95Ms, 7.20, 0.92),
            Metric(TelemetryStandardMetrics.FrameTimeP99Ms, 8.10, 0.92),
            Metric(TelemetryStandardMetrics.FrameStutterPercent, 0.7, 0.92),
            Metric(TelemetryStandardMetrics.FrameLatencyAverageMs, 7.5, 0.88)
        ]);
        var coordinator = new PerformanceCaptureCoordinator(
            (processId, duration, cancellationToken) =>
            {
                calls++;
                capturedPid = processId;
                return Task.FromResult<TelemetrySample?>(measuredSample);
            },
            timeline,
            (processId, duration, cancellationToken) =>
            {
                typedCalls++;
                typedCapturedPid = processId;
                return Task.FromResult<TelemetryFrame?>(measuredFrame);
            });

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

        var blockedTyped = await coordinator.CaptureTypedAsync(null, TimeSpan.FromSeconds(2));
        Require(!blockedTyped.Captured
                && blockedTyped.Frame is null
                && typedCalls == 0,
            "Typed Performance capture must use the same exact Guardian-binding gate before invoking PresentMon v2.");
        Require(timeline.Snapshot().Count == 1,
            "Blocked typed capture must not manufacture a timeline frame.");

        var typed = await coordinator.CaptureTypedAsync(status, TimeSpan.FromSeconds(2));
        Require(typed.Captured
                && ReferenceEquals(typed.Frame, measuredFrame)
                && typedCalls == 1
                && typedCapturedPid == 4321,
            "Typed Performance capture must forward the exact Guardian-bound PID and return the direct TelemetryFrame without a legacy bridge.");
        Require(calls == 1,
            "Typed Performance capture must never fall back to or double-invoke the legacy TelemetrySample provider.");

        entries = timeline.Snapshot();
        Require(entries.Count == 2
                && entries[^1].Telemetry is null
                && ReferenceEquals(entries[^1].TypedTelemetry, measuredFrame)
                && entries[^1].Timestamp == measuredFrame.Timestamp,
            "Typed capture must append exactly the direct TelemetryFrame to the same synchronized timeline.");

        var typedPresentation = PerformancePresentation.FromCapture(typed);
        Require(typedPresentation.HasMeasurement
                && typedPresentation.Fps == "165.5 FPS"
                && typedPresentation.OnePercentLow == "151.0 FPS"
                && typedPresentation.FrameTime == "6.04 ms"
                && typedPresentation.P99FrameTime == "8.10 ms"
                && typedPresentation.Latency == "7.5 ms"
                && typedPresentation.DataQuality == "Measured",
            "Performance presentation must consume direct typed frame metrics without parsing legacy DataQuality labels.");

        var legacyOnly = new PerformanceCaptureCoordinator(
            (processId, duration, cancellationToken) => Task.FromResult<TelemetrySample?>(measuredSample),
            new PerformanceTimelineBuffer(capacity: 2));
        var noTypedProvider = await legacyOnly.CaptureTypedAsync(status, TimeSpan.FromSeconds(2));
        Require(!noTypedProvider.Captured
                && noTypedProvider.Frame is null
                && noTypedProvider.Message.Contains("typed", StringComparison.OrdinalIgnoreCase),
            "A coordinator without a typed provider must fail closed instead of adapting legacy telemetry back into a TelemetryFrame.");

        await PerformanceUniversalCaptureCoordinatorSelfTests.RunAsync();

        Console.WriteLine("PASS Performance exact-PID legacy+typed capture coordinator and no-bridge timeline integration");
    }

    private static TelemetryMetricObservation Metric(
        TelemetryMetricDescriptor descriptor,
        double value,
        double coverage)
        => new(
            descriptor,
            value,
            TelemetryMetricQuality.Measured,
            coverage,
            "presentmon",
            TelemetryMetricOrigin.Direct);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
