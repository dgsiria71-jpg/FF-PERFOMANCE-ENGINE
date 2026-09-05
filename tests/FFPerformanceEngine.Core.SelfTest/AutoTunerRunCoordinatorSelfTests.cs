using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

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
            new TelemetrySample { Fps = 90, OnePercentLow = 80, FrameTimeMs = 11.1, StutterPercent = 1.8, LatencyMs = 10 },
            new TelemetrySample { Fps = 118, OnePercentLow = 108, FrameTimeMs = 8.5, StutterPercent = 0.8, LatencyMs = 8 });
        var coordinator = new AutoTunerRunCoordinator(new AutoTunerEngine(), runtime);
        var progress = new List<AutoTunerRunProgress>();

        var result = await coordinator.RunAsync(GameKind.FreeFireMax, AutoTunerMode.Adaptive, candidates, progress.Add);

        Require(result.Evidence.Count == 2, "Every successfully measured candidate must produce evidence.");
        Require(result.Evidence.All(x => x.Evidence == EvidenceLevel.Validated), "Successful real measurements must be marked validated by the coordinator.");
        Require(result.Winners.Count == 5, "A completed run with valid evidence must select all five winner roles.");
        Require(runtime.Events.SequenceEqual([
            "apply:4:4096:90:1280x720", "prepare:FreeFireMax", "capture", "complete",
            "apply:6:6144:120:1920x1080", "prepare:FreeFireMax", "capture", "complete",
            "restore"
        ]), "Candidate lifecycle ordering must be deterministic and baseline restoration must happen last.");
        Require(progress.Any(x => x.Stage == AutoTunerRunStage.Completed), "Run must emit a completed progress event.");

        var failingRuntime = new FakeRuntime(new TelemetrySample { Fps = 90 }) { FailPreparation = true };
        var failingCoordinator = new AutoTunerRunCoordinator(new AutoTunerEngine(), failingRuntime);
        var failed = await failingCoordinator.RunAsync(GameKind.FreeFire, AutoTunerMode.Adaptive, [candidates[0]]);
        Require(failed.Evidence.Count == 0, "Failed preparation must never fabricate benchmark evidence.");
        Require(failingRuntime.Events.Contains("complete"), "Candidate cleanup must run after preparation failure.");
        Require(failingRuntime.Events[^1] == "restore", "Baseline restoration must run even when candidate preparation fails.");

        Console.WriteLine("PASS end-to-end auto tuner candidate lifecycle");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class FakeRuntime(params TelemetrySample[] samples) : IAutoTunerRuntime
    {
        private readonly Queue<TelemetrySample> _samples = new(samples);
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
            Events.Add("capture");
            return Task.FromResult<TelemetrySample?>(_samples.Count == 0 ? null : _samples.Dequeue());
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
