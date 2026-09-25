using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class AutoTunerPresentationSelfTests
{
    [ModuleInitializer]
    internal static void Run()
    {
        var applying = AutoTunerPresentation.FromProgress(new AutoTunerRunProgress(
            AutoTunerRunStage.ApplyingCandidate, 2, 10, "Applying candidate 2."));
        Require(applying.Percent is >= 10 and < 20, "Candidate progress must include completed candidates plus the current stage fraction.");
        Require(applying.Title.Contains("candidato 2", StringComparison.OrdinalIgnoreCase), "Progress title must expose the current candidate without leaking implementation detail.");

        var validating = AutoTunerPresentation.FromProgress(new AutoTunerRunProgress(
            AutoTunerRunStage.ValidatingBenchmark, 2, 10, "Candidate 2 converged with 2 accepted repetitions."));
        Require(validating.Percent > applying.Percent, "Validation must advance the visible progress within the same candidate.");
        Require(validating.StageLabel.Contains("Validação", StringComparison.OrdinalIgnoreCase), "Validation stage must have a user-facing Portuguese label.");

        var restoring = AutoTunerPresentation.FromProgress(new AutoTunerRunProgress(
            AutoTunerRunStage.RestoringBaseline, 10, 10, "Restoring baseline."));
        Require(restoring.Percent is >= 96 and < 100, "Baseline restoration must remain visibly distinct from completion.");

        var completed = AutoTunerPresentation.FromProgress(new AutoTunerRunProgress(
            AutoTunerRunStage.Completed, 10, 10, "done"));
        Require(completed.Percent == 100, "Completed stage must render exactly 100 percent.");
        Require(completed.StageLabel.Contains("Conclu", StringComparison.OrdinalIgnoreCase), "Completed stage must have a user-facing label.");

        var winners = new List<PerformanceProfile>
        {
            Profile(ProfileKind.Recommended, "Recomendado", 118, 107, 8.4, 8.1, 0.7, 0.96),
            Profile(ProfileKind.MaximumFps, "Máximo FPS", 124, 101, 8.0, 9.0, 1.2, 0.93),
            Profile(ProfileKind.LowestLatency, "Menor Latência", 116, 105, 8.6, 6.9, 0.8, 0.95),
            Profile(ProfileKind.Stability, "Estabilidade", 114, 110, 8.8, 7.8, 0.3, 0.97),
            Profile(ProfileKind.Quality, "Qualidade", 108, 99, 9.2, 8.3, 0.6, 0.94)
        };
        var tuning = new TuningResult
        {
            Game = GameKind.FreeFireMax,
            Mode = AutoTunerMode.Adaptive,
            Winners = winners,
            Evidence = [],
            Summary = "5 validated winners"
        };
        var session = new AutoTunerSessionResult(tuning, "Pie64", 12, true);
        var summary = AutoTunerPresentation.Summarize(session);
        Require(summary.Contains("Recomendado", StringComparison.OrdinalIgnoreCase), "Result summary must lead with the recommended winner.");
        Require(summary.Contains("118", StringComparison.Ordinal), "Result summary must expose measured recommended FPS.");
        Require(summary.Contains("96%", StringComparison.Ordinal), "Result summary must expose confidence.");
        Require(summary.Contains("Pie64", StringComparison.Ordinal), "Result summary must disclose the tuned BlueStacks instance.");
        Require(summary.Contains("5", StringComparison.Ordinal), "Result summary must disclose the number of validated winner roles.");

        var inconclusive = AutoTunerPresentation.Summarize(new AutoTunerSessionResult(
            tuning with { Winners = [], Summary = "no winners" }, "Pie64", 12, false));
        Require(inconclusive.Contains("nenhum", StringComparison.OrdinalIgnoreCase), "Inconclusive runs must explicitly state that no validated winner was found.");
        Require(inconclusive.Contains("preserv", StringComparison.OrdinalIgnoreCase), "Inconclusive runs must explain that known-good profiles were preserved.");

        Console.WriteLine("PASS Auto Tuner progress and result presentation contract");
    }

    private static PerformanceProfile Profile(
        ProfileKind kind,
        string name,
        double fps,
        double low,
        double frameTime,
        double latency,
        double stutter,
        double confidence) => new()
    {
        Kind = kind,
        Name = name,
        Game = GameKind.FreeFireMax,
        InstanceName = "Pie64",
        CpuCores = 6,
        RamMb = 6144,
        Renderer = "OpenGL",
        FpsTarget = 120,
        Resolution = "1600x900",
        Evidence = EvidenceLevel.Validated,
        Confidence = confidence,
        AverageFps = fps,
        OnePercentLow = low,
        FrameTimeMs = frameTime,
        LatencyMs = latency,
        StutterPercent = stutter
    };

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
