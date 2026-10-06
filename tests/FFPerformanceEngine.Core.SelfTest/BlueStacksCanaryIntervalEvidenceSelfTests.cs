using System.Buffers.Binary;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;
using FFPerformanceEngine.Core.Workloads;

internal static class BlueStacksCanaryIntervalEvidenceSelfTests
{
    internal static async Task RunAsync()
    {
        await StableCalibratedIntervalProducesOpaqueComparableWindowAsync();
        await AnyMidIntervalVisualDriftFailsClosedAsync();
        RawAdbScreencapParserAcceptsKnown32BitLayoutsAndRejectsMalformedFrames();
        PackageVersionParserIsExactAndFailClosed();
        Console.WriteLine("PASS Track 6 calibrated BlueStacks interval evidence proves exact ADB scope across the full capture window");
    }

    private static async Task StableCalibratedIntervalProducesOpaqueComparableWindowAsync()
    {
        var calibration = Calibration();
        var key = new GenericGuardianCanarySessionKey(
            Guid.NewGuid(),
            calibration.GameId,
            4242,
            @"C:\Program Files\BlueStacks_nxt\HD-Player.exe");
        var target = Target(calibration.GameId, 4242);
        var probe = new ProbeDouble(calibration, driftAfterCall: null);
        var source = new BlueStacksCanaryContextEvidenceSource(
            key,
            calibration,
            probe,
            TimeSpan.FromMilliseconds(2),
            TimeSpan.FromMilliseconds(100));

        var intervalSource = (IGenericGuardianCanaryIntervalEvidenceSource)source;
        await using var interval = await intervalSource.BeginWindowAsync(target)
            ?? throw new InvalidOperationException("Stable calibrated source did not open an interval.");

        var started = DateTimeOffset.UtcNow;
        await Task.Delay(15);
        var completed = DateTimeOffset.UtcNow;
        var frame = TelemetryFrame();
        var window = await interval.CompleteWindowAsync(
            frame,
            started,
            completed);

        Require(window is not null
                && window.SessionEpoch == key.SessionEpoch
                && ReferenceEquals(window.Frame, frame)
                && window.SourceId == "bluestacks-calibrated:" + calibration.CalibrationId
                && window.ModeId == calibration.ModeId
                && window.SceneId == calibration.SceneId
                && window.LoadFingerprint == calibration.LoadFingerprint
                && window.EnvironmentFingerprint == calibration.EnvironmentFingerprint
                && window.StartedAt == started
                && window.EndedAt == completed
                && window.ControlledBenchmarkActive == false
                && window.WorkloadDriftDetected == false
                && window.OtherMutationDetected == false
                && probe.CallCount >= 2,
            "Stable interval must emit only opaque calibration-derived context bound to the real telemetry envelope.");

        var legacy = await source.CaptureWindowAsync(target, frame, started, completed);
        Require(legacy is null,
            "Calibrated production source must fail closed when called through the old post-hoc API; full-interval monitoring is mandatory.");
    }

    private static async Task AnyMidIntervalVisualDriftFailsClosedAsync()
    {
        var calibration = Calibration();
        var key = new GenericGuardianCanarySessionKey(
            Guid.NewGuid(),
            calibration.GameId,
            4242,
            @"C:\Program Files\BlueStacks_nxt\HD-Player.exe");
        var target = Target(calibration.GameId, 4242);
        var probe = new ProbeDouble(calibration, driftAfterCall: 2);
        var source = new BlueStacksCanaryContextEvidenceSource(
            key,
            calibration,
            probe,
            TimeSpan.FromMilliseconds(2),
            TimeSpan.FromMilliseconds(100));

        await using var interval = await ((IGenericGuardianCanaryIntervalEvidenceSource)source)
            .BeginWindowAsync(target)
            ?? throw new InvalidOperationException("Initial calibrated sample unexpectedly failed.");

        var started = DateTimeOffset.UtcNow;
        await Task.Delay(20);
        var completed = DateTimeOffset.UtcNow;
        var window = await interval.CompleteWindowAsync(
            TelemetryFrame(),
            started,
            completed);

        Require(window is null && probe.CallCount >= 2,
            "One mismatching visual/foreground/version sample anywhere in the interval must invalidate the whole evidence window.");
    }

    private static void RawAdbScreencapParserAcceptsKnown32BitLayoutsAndRejectsMalformedFrames()
    {
        var rgba = RawFrame(width: 2, height: 1, format: 1, headerBytes: 12,
            [10,20,30,255, 40,50,60,128]);
        var parsedRgba = BlueStacksAdbRawScreencapParser.Parse(rgba);
        Require(parsedRgba is not null
                && parsedRgba.Width == 2
                && parsedRgba.Height == 1
                && parsedRgba.Bgra32.SequenceEqual(new byte[]
                {
                    30,20,10,255,
                    60,50,40,128
                }),
            "ADB RGBA_8888 raw screencap must convert deterministically to calibration BGRA32.");

        var bgra = RawFrame(width: 1, height: 1, format: 5, headerBytes: 16,
            [7,8,9,10]);
        var parsedBgra = BlueStacksAdbRawScreencapParser.Parse(bgra);
        Require(parsedBgra is not null
                && parsedBgra.Bgra32.SequenceEqual(new byte[] { 7,8,9,10 }),
            "ADB BGRA_8888 with modern 16-byte header must be accepted without channel corruption.");

        Require(BlueStacksAdbRawScreencapParser.Parse([1,2,3]) is null
                && BlueStacksAdbRawScreencapParser.Parse(
                    RawFrame(1, 1, format: 99, headerBytes: 12, [1,2,3,4])) is null,
            "Malformed or unsupported raw screencap formats must fail closed.");
    }

    private static void PackageVersionParserIsExactAndFailClosed()
    {
        const string dump = """
            Packages:
              Package [com.dts.freefireth] (123):
                versionCode=201234 minSdk=23 targetSdk=35
                versionName=1.132.1
        """;
        Require(BlueStacksAutomationService.ParsePackageVersion(dump) == "1.132.1"
                && BlueStacksAutomationService.ParsePackageVersion("versionCode=1") is null,
            "Package version evidence must use explicit versionName and never infer from another package field.");
    }

    private static byte[] RawFrame(
        int width,
        int height,
        uint format,
        int headerBytes,
        byte[] pixels)
    {
        var bytes = new byte[headerBytes + pixels.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0, 4), (uint)width);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4, 4), (uint)height);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8, 4), format);
        if (headerBytes == 16)
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12, 4), 0);
        pixels.CopyTo(bytes, headerBytes);
        return bytes;
    }

    private static BlueStacksCanaryContextCalibration Calibration()
    {
        var regions = new[]
        {
            new BlueStacksCanaryVisualRegion(0, 0, 90, 40),
            new BlueStacksCanaryVisualRegion(90, 0, 90, 40),
            new BlueStacksCanaryVisualRegion(0, 60, 90, 40),
            new BlueStacksCanaryVisualRegion(90, 60, 90, 40)
        };
        return BlueStacksCanaryContextCalibration.Create(
            "garena.free-fire",
            "bluestacks.free-fire",
            GameKind.FreeFire,
            "Pie64",
            5555,
            "1.132.1",
            180,
            100,
            regions,
            [VisualFrame(false), VisualFrame(false)]);
    }

    private static BlueStacksCanaryVisualFrame VisualFrame(bool drift)
    {
        const int width = 180;
        const int height = 100;
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var i = (y * width + x) * 4;
            var structure = ((x / 10) + (y / 10)) % 2 == 0;
            var value = (byte)((structure ^ drift) ? 225 : 25);
            pixels[i] = value;
            pixels[i + 1] = value;
            pixels[i + 2] = value;
            pixels[i + 3] = 255;
        }
        return new BlueStacksCanaryVisualFrame(width, height, pixels);
    }

    private static TelemetryWorkloadTarget Target(string gameId, int pid)
        => new()
        {
            GameId = gameId,
            ProcessId = pid,
            ExecutablePath = @"C:\Program Files\BlueStacks_nxt\HD-Player.exe",
            BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess
        };

    private static TelemetryFrame TelemetryFrame()
        => new(DateTimeOffset.UtcNow,
        [
            new TelemetryMetricObservation(
                TelemetryStandardMetrics.FrameFpsAverage,
                100,
                TelemetryMetricQuality.Measured,
                1,
                "interval-evidence-test",
                TelemetryMetricOrigin.Direct)
        ]);

    private sealed class ProbeDouble(
        BlueStacksCanaryContextCalibration calibration,
        int? driftAfterCall) : IBlueStacksCanaryContextProbe
    {
        private int _calls;
        public int CallCount => Volatile.Read(ref _calls);

        public Task<BlueStacksCanaryContextSample?> CaptureAsync(
            BlueStacksCanaryContextCalibration requested,
            TelemetryWorkloadTarget exactTarget,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var call = Interlocked.Increment(ref _calls);
            var drift = driftAfterCall is int threshold && call >= threshold;
            return Task.FromResult<BlueStacksCanaryContextSample?>(new(
                DateTimeOffset.UtcNow,
                calibration.InstanceName,
                calibration.AdbPort,
                drift ? "9.999.9" : calibration.PackageVersion,
                calibration.GameKind,
                VisualFrame(drift)));
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
