using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;

internal static class AutoTunerRunCoordinatorSelfTests
{
    [ModuleInitializer]
    internal static void Run()
    {
        RunAsync().GetAwaiter().GetResult();
    }

    private static async Task RunAsync()
    {
        var candidates = new[]
        {
            new TuningCandidate { CpuCores = 4, RamMb = 4096, Renderer = "Auto", FpsTarget = 90, Resolution = "1280x720" },
            new TuningCandidate { CpuCores = 6, RamMb = 6144, Renderer = "Auto", FpsTarget = 120, Resolution = "1920x1080" }
        };
        var runtime = new FakeRuntime(
            Frame(90, 80, 11.1, 10),
            Frame(91, 81, 11.0, 9.8),
            Frame(118, 108, 8.5, 8),
            Frame(119, 109, 8.4, 7.9));
        var coordinator = new AutoTunerRunCoordinator(new AutoTunerEngine(), runtime);
        var progress = new List<AutoTunerRunProgress>();

        var result = await coordinator.RunAsync(GameKind.FreeFireMax, AutoTunerMode.Adaptive, candidates, progress.Add);

        Require(result.Evidence.Count == 2, "Every successfully measured candidate must produce evidence.");
        Require(result.Evidence.All(x => x.Evidence == EvidenceLevel.Validated), "Stable repeated real measurements must be marked validated by the coordinator.");
        Require(result.Winners.Count == 5, "A completed run with valid evidence must select all five winner roles.");
        Require(runtime.Events.SequenceEqual([
            "apply:4:4096:90:1280x720", "prepare:FreeFireMax", "capture", "capture", "complete",
            "apply:6:6144:120:1920x1080", "prepare:FreeFireMax", "capture", "capture", "complete",
            "restore"
        ]), "Candidate lifecycle ordering must be deterministic, typed adaptive validation must repeat stable measurements, and baseline restoration must happen last.");
        Require(!runtime.Events.Contains("legacy-capture"), "Auto Tuner coordinator must never use legacy TelemetrySample benchmark authority.");
        Require(progress.Any(x => x.Stage == AutoTunerRunStage.Completed), "Run must emit a completed progress event.");

        var failingRuntime = new FakeRuntime(Frame(90, 80, 11.1, 10)) { FailPreparation = true };
        var failingCoordinator = new AutoTunerRunCoordinator(new AutoTunerEngine(), failingRuntime);
        var failed = await failingCoordinator.RunAsync(GameKind.FreeFire, AutoTunerMode.Adaptive, [candidates[0]]);
        Require(failed.Evidence.Count == 0, "Failed preparation must never fabricate benchmark evidence.");
        Require(failingRuntime.Events.Contains("complete"), "Candidate cleanup must run after preparation failure.");
        Require(failingRuntime.Events[^1] == "restore", "Baseline restoration must run even when candidate preparation fails.");

        Console.WriteLine("PASS end-to-end auto tuner candidate lifecycle");
    }

    private static TelemetryFrame Frame(double fps, double low1, double frameTimeMs, double latencyMs)
        => new(
            DateTimeOffset.UtcNow,
            [
                Direct(TelemetryStandardMetrics.FrameFpsAverage, fps),
                Direct(TelemetryStandardMetrics.FrameFpsLow1, low1),
                Direct(TelemetryStandardMetrics.FrameTimeAverageMs, frameTimeMs),
                Direct(TelemetryStandardMetrics.FrameTimeP95Ms, frameTimeMs * 1.1),
                Direct(TelemetryStandardMetrics.FrameStutterPercent, 1d),
                Direct(TelemetryStandardMetrics.FrameLatencyAverageMs, latencyMs),
                Direct(TelemetryStandardMetrics.FrameAcceptedSampleCount, 1200d)
            ]);

    private static TelemetryMetricObservation Direct(TelemetryMetricDescriptor descriptor, double value)
        => new(
            descriptor,
            value,
            TelemetryMetricQuality.Measured,
            1d,
            "presentmon",
            TelemetryMetricOrigin.Direct);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class FakeRuntime(params TelemetryFrame[] frames) : IAutoTunerRuntime
    {
        private readonly Queue<TelemetryFrame> _frames = new(frames);
        public List<string> Events { get; } = [];
        public bool FailPreparation { get; init; }

        public Task<AutoTunerRuntimeResult> ApplyCandidateAsync(TuningCandidate candidate, CancellationToken cancellationToken = default)
        {
            Events.Add($"apply:{candidate.CpuCores}:{candidate.RamMb}:{candidate.FpsTarget}:{candidate.Resolution}");
            return Task.FromResult(AutoTunerRuntimeResult.Ok("candidate applied"));
        }

        public Task<AutoTunerRuntimeResult> PrepareGameAsync(GameKind game, CancellationToken cancellationToken = default)
        {
            Events.Add($"prepare:{game}");
            return Task.FromResult(FailPreparation ? AutoTunerRuntimeResult.Fail("prepare failed") : AutoTunerRuntimeResult.Ok("prepared"));
        }

        public Task<TelemetrySample?> CaptureBenchmarkAsync(CancellationToken cancellationToken = default)
        {
            Events.Add("legacy-capture");
            return Task.FromResult<TelemetrySample?>(new TelemetrySample { Fps = 777, DataQuality = "PresentMon · 9999 frames" });
        }

        public Task<TelemetryFrame?> CaptureBenchmarkFrameAsync(CancellationToken cancellationToken = default)
        {
            Events.Add("capture");
            return Task.FromResult<TelemetryFrame?>(_frames.Count == 0 ? null : _frames.Dequeue());
        }

        public Task CompleteCandidateAsync(CancellationToken cancellationToken = default)
        {
            Events.Add("complete");
            return Task.CompletedTask;
        }

        public Task RestoreBaselineAsync(CancellationToken cancellationToken = default)
        {
            Events.Add("restore");
            return Task.CompletedTask;
        }
    }
}
