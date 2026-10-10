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
        await CalibratedWorkloadForegroundUsesExactScopeAuthorityAsync();
        await AdbFramebufferCaptureIsExactAndOcclusionIndependentAsync();
        Console.WriteLine("PASS Track 6 calibrated BlueStacks evidence brackets the physical interval, captures exact ADB framebuffer and fails closed");
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

    private static async Task CalibratedWorkloadForegroundUsesExactScopeAuthorityAsync()
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

        var scope = new FakeScopeProbe(true, false);
        var probe = new BlueStacksCalibratedWorkloadForegroundProbe(
            calibration,
            scope);

        Require(await probe.IsForegroundAsync(target) is true
                && await probe.IsForegroundAsync(target) is false
                && scope.CallCount == 2,
            "Calibrated BlueStacks workload foreground must delegate to the exact instance/package/version/Android-foreground scope authority and preserve its fail-closed result.");

        var wrongTarget = target with { GameId = "garena.free-fire-max" };
        Require(await probe.IsForegroundAsync(wrongTarget) is false
                && scope.CallCount == 2,
            "A calibrated BlueStacks workload foreground probe must reject a different stable GameId before consulting the calibrated scope.");
    }

    private static async Task AdbFramebufferCaptureIsExactAndOcclusionIndependentAsync()
    {
        var calibration = Calibration();
        var environment = new EnvironmentSnapshot
        {
            BlueStacksDetected = true,
            Instances =
            [
                new BlueStacksInstance
                {
                    Name = calibration.InstanceName,
                    AdbPort = calibration.AdbPort,
                    AdbEnabled = true
                }
            ]
        };
        var automation = new BlueStacksAutomationService(
            new BlueStacksService(),
            adbExecutableOverride: @"C:\BlueStacks\HD-Adb.exe");
        var backend = new FakeAdbRawScreenCapture(
            RawFrame(
                calibration.WindowWidth,
                calibration.WindowHeight,
                pixelFormat: 1,
                red: 10,
                green: 20,
                blue: 30));
        var capture = new BlueStacksAdbCanaryVisualFrameCapture(
            () => environment,
            automation,
            backend);

        var frame = await capture.CaptureAsync(4242, calibration);
        Require(frame is not null
                && frame.Width == calibration.WindowWidth
                && frame.Height == calibration.WindowHeight
                && frame.Bgra32[0] == 30
                && frame.Bgra32[1] == 20
                && frame.Bgra32[2] == 10
                && frame.Bgra32[3] == 255
                && backend.CallCount == 1
                && backend.AdbExecutable == @"C:\BlueStacks\HD-Adb.exe"
                && backend.Endpoint == "127.0.0.1:5555",
            "ADB framebuffer capture must bind the calibrated instance endpoint, parse the real 16-byte raw screencap header and convert RGBA pixels to BGRA without reading the Windows desktop.");


        var legacyRaw = new BlueStacksAdbCanaryVisualFrameCapture(
            () => environment,
            automation,
            new FakeAdbRawScreenCapture(
                RawFrame(
                    calibration.WindowWidth,
                    calibration.WindowHeight,
                    pixelFormat: 1,
                    red: 10,
                    green: 20,
                    blue: 30,
                    headerBytes: 12)));
        var legacyFrame = await legacyRaw.CaptureAsync(4242, calibration);
        Require(legacyFrame is not null
                && legacyFrame.Width == calibration.WindowWidth
                && legacyFrame.Height == calibration.WindowHeight
                && legacyFrame.Bgra32[0] == 30
                && legacyFrame.Bgra32[1] == 20
                && legacyFrame.Bgra32[2] == 10,
            "Legacy 12-byte Android RAW screencap must parse as exactly as 16-byte format.");

        var malformed = new BlueStacksAdbCanaryVisualFrameCapture(
            () => environment,
            automation,
            new FakeAdbRawScreenCapture(
                RawFrame(
                    calibration.WindowWidth,
                    calibration.WindowHeight,
                    pixelFormat: 1,
                    red: 10,
                    green: 20,
                    blue: 30,
                    headerBytes: 13)));
        Require(await malformed.CaptureAsync(4242, calibration) is null,
            "Malformed 13-byte RAW header must fail closed instead of shifting framebuffer pixels.");

        var unsupported = new BlueStacksAdbCanaryVisualFrameCapture(
            () => environment,
            automation,
            new FakeAdbRawScreenCapture(
                RawFrame(
                    calibration.WindowWidth,
                    calibration.WindowHeight,
                    pixelFormat: 2,
                    red: 10,
                    green: 20,
                    blue: 30)));
        Require(await unsupported.CaptureAsync(4242, calibration) is null,
            "Unknown Android screencap pixel formats must fail closed.");

        var duplicateScope = new BlueStacksAdbCanaryVisualFrameCapture(
            () => environment with
            {
                Instances =
                [
                    environment.Instances[0],
                    environment.Instances[0]
                ]
            },
            automation,
            backend);
        Require(await duplicateScope.CaptureAsync(4242, calibration) is null,
            "ADB framebuffer capture must require one exact calibrated BlueStacks instance/port.");
    }

    private static byte[] RawFrame(
        int width,
        int height,
        int pixelFormat,
        byte red,
        byte green,
        byte blue,
        int headerBytes = 16)
    {
        var raw = new byte[checked(headerBytes + width * height * 4)];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(raw.AsSpan(0, 4), width);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(raw.AsSpan(4, 4), height);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(raw.AsSpan(8, 4), pixelFormat);
        if (headerBytes >= 16)
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(raw.AsSpan(12, 4), 0);
        for (var i = headerBytes; i < raw.Length; i += 4)
        {
            raw[i] = red;
            raw[i + 1] = green;
            raw[i + 2] = blue;
            raw[i + 3] = 255;
        }
        return raw;
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

    private sealed class FakeAdbRawScreenCapture(byte[] raw)
        : IBlueStacksAdbRawScreenCapture
    {
        internal int CallCount { get; private set; }
        internal string? AdbExecutable { get; private set; }
        internal string? Endpoint { get; private set; }

        public Task<byte[]?> CaptureAsync(
            string adbExecutable,
            string endpoint,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            AdbExecutable = adbExecutable;
            Endpoint = endpoint;
            return Task.FromResult<byte[]?>(raw.ToArray());
        }
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
