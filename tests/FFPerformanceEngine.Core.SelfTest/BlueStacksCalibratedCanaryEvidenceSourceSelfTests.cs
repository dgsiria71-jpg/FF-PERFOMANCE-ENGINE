using System.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;
using FFPerformanceEngine.Core.Workloads;

internal static class BlueStacksCalibratedCanaryEvidenceSourceSelfTests
{
    internal static async Task RunAsync()
    {
        await IntervalSourceBracketsTelemetryWithMeasuredCalibrationAsync();
        await ChangedVisualOrScopeFailsClosedAsync();
        Console.WriteLine("PASS Track 6 calibrated BlueStacks evidence brackets the physical interval and fails closed");
    }

    private static async Task IntervalSourceBracketsTelemetryWithMeasuredCalibrationAsync()
    {
        var calibration = Calibration();
        using var process = Process.GetCurrentProcess();
        var target = new TelemetryWorkloadTarget
        {
            GameId = calibration.GameId,
            ProcessId = process.Id,
            ExecutablePath = process.MainModule?.FileName ?? Environment.ProcessPath ?? "self-test.exe",
            BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess
        };
        var session = new GenericGuardianCanarySessionKey(
            Guid.NewGuid(), target.GameId, target.ProcessId!.Value, target.ExecutablePath);
        var capture = new FakeVisualCapture([ReferenceFrame(), ReferenceFrame()]);
        var scope = new FakeScopeProbe(true, true);
        var source = new BlueStacksCalibratedCanaryEvidenceSource(
            session, calibration, capture, scope);

        var interval = await source.BeginWindowAsync(target);
        Require(interval is not null && capture.CallCount == 1 && scope.CallCount == 1,
            "Interval evidence must capture/validate its leading boundary before telemetry begins.");

        var started = DateTimeOffset.UtcNow;
        var frame = TelemetryFrame();
        var completed = started + TimeSpan.FromMilliseconds(25);
        var window = await interval!.CompleteWindowAsync(
            frame, started, completed);

        Require(window is not null
                && capture.CallCount == 2
                && scope.CallCount == 2
                && window.SessionEpoch == session.SessionEpoch
                && ReferenceEquals(window.Frame, frame)
                && window.StartedAt == started
                && window.EndedAt == completed
                && window.ModeId == calibration.ModeId
                && window.SceneId == calibration.SceneId
                && window.LoadFingerprint == calibration.LoadFingerprint
                && window.EnvironmentFingerprint == calibration.EnvironmentFingerprint
                && window.ControlledBenchmarkActive is false
                && window.WorkloadDriftDetected is false
                && window.OtherMutationDetected is false,
            "A stable calibrated start/end boundary must emit the exact opaque context tuple for the measured interval.");
    }

    private static async Task ChangedVisualOrScopeFailsClosedAsync()
    {
        var calibration = Calibration();
        using var process = Process.GetCurrentProcess();
        var target = new TelemetryWorkloadTarget
        {
            GameId = calibration.GameId,
            ProcessId = process.Id,
            ExecutablePath = process.MainModule?.FileName ?? Environment.ProcessPath ?? "self-test.exe",
            BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess
        };
        var session = new GenericGuardianCanarySessionKey(
            Guid.NewGuid(), target.GameId, target.ProcessId!.Value, target.ExecutablePath);

        var changed = new BlueStacksCalibratedCanaryEvidenceSource(
            session,
            calibration,
            new FakeVisualCapture([ReferenceFrame(), ChangedFrame()]),
            new FakeScopeProbe(true, true));
        var changedInterval = await changed.BeginWindowAsync(target);
        Require(changedInterval is not null, "Leading calibrated visual boundary should be accepted.");
        var changedWindow = await changedInterval!.CompleteWindowAsync(
            TelemetryFrame(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(10));
        Require(changedWindow is null,
            "A changed trailing HUD fingerprint must make comparison context unknown.");

        var drift = new BlueStacksCalibratedCanaryEvidenceSource(
            session,
            calibration,
            new FakeVisualCapture([ReferenceFrame(), ReferenceFrame()]),
            new FakeScopeProbe(true, false));
        var driftInterval = await drift.BeginWindowAsync(target);
        Require(driftInterval is not null, "Leading scope may be valid.");
        var driftWindow = await driftInterval!.CompleteWindowAsync(
            TelemetryFrame(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(10));
        Require(driftWindow is null,
            "Instance/package/foreground drift at the trailing boundary must fail closed.");
    }

    private static BlueStacksCanaryContextCalibration Calibration()
        => BlueStacksCanaryContextCalibration.Create(
            "garena.free-fire",
            "bluestacks.free-fire",
            GameKind.FreeFire,
            "Pie64",
            5555,
            "1.132.1",
            180,
            100,
            Regions(),
            [ReferenceFrame(), ReferenceFrame()]);

    private static BlueStacksCanaryVisualRegion[] Regions() =>
    [
        new(0, 0, 90, 40),
        new(90, 0, 90, 40),
        new(0, 60, 90, 40),
        new(90, 60, 90, 40)
    ];

    private static BlueStacksCanaryVisualFrame ReferenceFrame()
        => Frame(false);

    private static BlueStacksCanaryVisualFrame ChangedFrame()
        => Frame(true);

    private static BlueStacksCanaryVisualFrame Frame(bool flip)
    {
        const int width = 180, height = 100;
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var i = (y * width + x) * 4;
            var structure = ((x / 10) + (y / 10)) % 2 == 0;
            var value = (byte)((structure ^ flip) ? 225 : 25);
            pixels[i] = pixels[i + 1] = pixels[i + 2] = value;
            pixels[i + 3] = 255;
        }
        return new(width, height, pixels);
    }

    private static TelemetryFrame TelemetryFrame()
        => new(
            DateTimeOffset.UtcNow,
            [
                new TelemetryMetricObservation(
                    TelemetryStandardMetrics.FrameFpsAverage,
                    100,
                    TelemetryMetricQuality.Measured,
                    1,
                    "calibrated-evidence-selftest",
                    TelemetryMetricOrigin.Direct)
            ]);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class FakeVisualCapture(IReadOnlyList<BlueStacksCanaryVisualFrame> frames)
        : IBlueStacksCanaryVisualFrameCapture
    {
        private int _index;
        internal int CallCount { get; private set; }

        public Task<BlueStacksCanaryVisualFrame?> CaptureAsync(
            int processId,
            BlueStacksCanaryContextCalibration calibration,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            var index = Math.Min(_index++, frames.Count - 1);
            return Task.FromResult<BlueStacksCanaryVisualFrame?>(frames[index]);
        }
    }

    private sealed class FakeScopeProbe(params bool[] results)
        : IBlueStacksCanaryContextScopeProbe
    {
        private int _index;
        internal int CallCount { get; private set; }

        public Task<bool> MatchesAsync(
            BlueStacksCanaryContextCalibration calibration,
            TelemetryWorkloadTarget exactTarget,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            var index = Math.Min(_index++, results.Length - 1);
            return Task.FromResult(results[index]);
        }
    }
}
