using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;
using FFPerformanceEngine.Core.Workloads;

internal static class BlueStacksCalibratedCanaryEvidenceSourceSelfTests
{
    internal static async Task RunAsync()
    {
        var calibration = CreateCalibration();
        var target = new TelemetryWorkloadTarget
        {
            GameId = calibration.GameId,
            ProcessId = 4242,
            ExecutablePath = @"C:\Program Files\BlueStacks_nxt\HD-Player.exe",
            BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess
        };
        var session = new GenericGuardianCanarySessionKey(
            Guid.NewGuid(),
            target.GameId,
            target.ProcessId,
            target.ExecutablePath);

        var start = DateTimeOffset.UtcNow.AddSeconds(-2);
        var end = start.AddSeconds(1);
        var matching = MatchingFrame();

        var timeline = new FakeTimeline(
            prepare: true,
            samples:
            [
                Sample(start.AddMilliseconds(-50), calibration, matching),
                Sample(start.AddMilliseconds(400), calibration, matching),
                Sample(end.AddMilliseconds(20), calibration, matching)
            ]);

        await using var source = new BlueStacksCalibratedCanaryEvidenceSource(
            session,
            calibration,
            timeline,
            maxSampleGap: TimeSpan.FromMilliseconds(600));

        Require(await source.PrepareAsync(target),
            "Calibrated source must prepare only the exact owner-issued workload target.");

        var frame = new TelemetryFrame(
            start.AddMilliseconds(500),
            [
                new TelemetryMetricObservation(
                    TelemetryStandardMetrics.FrameFpsAverage,
                    100,
                    TelemetryMetricQuality.Measured,
                    1,
                    "visual-evidence-selftest",
                    TelemetryMetricOrigin.Direct)
            ]);
        var window = await source.CaptureWindowAsync(
            target,
            frame,
            start,
            end);

        Require(window is not null
                && window.SessionEpoch == session.SessionEpoch
                && window.SourceId == "bluestacks.visual-calibration:" + calibration.CalibrationId
                && window.ModeId == calibration.ModeId
                && window.SceneId == calibration.SceneId
                && window.LoadFingerprint == calibration.LoadFingerprint
                && window.EnvironmentFingerprint == calibration.EnvironmentFingerprint
                && window.ControlledBenchmarkActive == false
                && window.WorkloadDriftDetected == false
                && window.OtherMutationDetected == false,
            "Complete calibrated interval must project only opaque measured context ids and explicit clean process-local flags.");

        timeline.Samples =
        [
            Sample(start.AddMilliseconds(-50), calibration, matching),
            Sample(start.AddMilliseconds(350), calibration, DifferentHudFrame()),
            Sample(end.AddMilliseconds(20), calibration, matching)
        ];
        Require(await source.CaptureWindowAsync(target, frame, start, end) is null,
            "Any visually mismatched sample inside the physical capture interval must fail closed.");

        timeline.Samples =
        [
            Sample(start.AddMilliseconds(100), calibration, matching),
            Sample(end.AddMilliseconds(20), calibration, matching)
        ];
        Require(await source.CaptureWindowAsync(target, frame, start, end) is null,
            "Evidence without a sample bracketing the beginning of the physical capture interval must fail closed.");

        timeline.Samples =
        [
            Sample(start.AddMilliseconds(-50), calibration, matching),
            Sample(start.AddMilliseconds(900), calibration, matching),
            Sample(end.AddMilliseconds(20), calibration, matching)
        ];
        Require(await source.CaptureWindowAsync(target, frame, start, end) is null,
            "Evidence with a sampling gap larger than the caller-defined maximum must fail closed.");

        timeline.Samples =
        [
            Sample(start.AddMilliseconds(-50), calibration, matching),
            new BlueStacksCanaryContextSample(
                start.AddMilliseconds(400),
                GameKind.FreeFire,
                "9.9.9",
                matching),
            Sample(end.AddMilliseconds(20), calibration, matching)
        ];
        Require(await source.CaptureWindowAsync(target, frame, start, end) is null,
            "Package-version drift inside the interval must fail closed.");

        var wrongTarget = target with { GameId = "garena.free-fire-max" };
        Require(!await source.PrepareAsync(wrongTarget),
            "A calibration must never prepare for a different GameId.");

        Console.WriteLine("PASS Track 6 calibrated BlueStacks evidence requires complete stable visual/package/foreground interval coverage");
    }

    private static BlueStacksCanaryContextCalibration CreateCalibration()
    {
        var frameA = MatchingFrame();
        var frameB = MatchingFrame(worldBias: 70);
        return BlueStacksCanaryContextCalibration.Create(
            "garena.free-fire",
            "bluestacks.free-fire",
            GameKind.FreeFire,
            "Pie64",
            5555,
            "1.132.1",
            frameA.Width,
            frameA.Height,
            [
                new BlueStacksCanaryVisualRegion(0, 0, 45, 24),
                new BlueStacksCanaryVisualRegion(45, 0, 45, 24),
                new BlueStacksCanaryVisualRegion(0, 56, 45, 24),
                new BlueStacksCanaryVisualRegion(45, 56, 45, 24)
            ],
            [frameA, frameB]);
    }

    private static BlueStacksCanaryContextSample Sample(
        DateTimeOffset timestamp,
        BlueStacksCanaryContextCalibration calibration,
        BlueStacksCanaryVisualFrame frame)
        => new(
            timestamp,
            calibration.GameKind,
            calibration.PackageVersion,
            frame);

    private static BlueStacksCanaryVisualFrame MatchingFrame(int worldBias = 0)
        => Frame(worldBias, invertHud: false);

    private static BlueStacksCanaryVisualFrame DifferentHudFrame()
        => Frame(0, invertHud: true);

    private static BlueStacksCanaryVisualFrame Frame(int worldBias, bool invertHud)
    {
        const int width = 90;
        const int height = 80;
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var index = (y * width + x) * 4;
                var inHud = y < 24 || y >= 56;
                var structured = ((x / 5) + (y / 4)) % 2 == 0;
                var value = inHud
                    ? (byte)((structured ^ invertHud) ? 235 : 20)
                    : (byte)Math.Clamp(worldBias + (x + y) % 80, 0, 255);
                pixels[index] = value;
                pixels[index + 1] = value;
                pixels[index + 2] = value;
                pixels[index + 3] = 255;
            }
        }

        return new BlueStacksCanaryVisualFrame(width, height, pixels);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class FakeTimeline(
        bool prepare,
        IReadOnlyList<BlueStacksCanaryContextSample> samples)
        : IBlueStacksCanaryContextTimeline
    {
        internal IReadOnlyList<BlueStacksCanaryContextSample> Samples { get; set; } = samples;
        public Task<bool> PrepareAsync(
            TelemetryWorkloadTarget target,
            CancellationToken cancellationToken = default)
            => Task.FromResult(prepare);

        public Task<IReadOnlyList<BlueStacksCanaryContextSample>> ReadThroughAsync(
            DateTimeOffset completedAt,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Samples);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
