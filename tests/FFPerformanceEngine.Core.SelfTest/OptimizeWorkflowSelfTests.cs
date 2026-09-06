using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class OptimizeWorkflowSelfTests
{
    public static async Task RunAsync()
    {
        AnalyzeRequiresSafeOwnedStartingConditions();
        await RunsGeneratedSessionForExactAnalyzedInstance();
        Console.WriteLine("PASS Optimize workflow readiness and full-session orchestration");
    }

    private static void AnalyzeRequiresSafeOwnedStartingConditions()
    {
        var environment = EnvironmentWith(Instance("Pie64"));
        var runner = new FakeSessionRunner();
        var readyProbe = new FakeOptimizeSystemProbe(environment, playerRunning: false, telemetryReady: true);
        var workflow = new OptimizeWorkflowService(new AutoTunerEngine(), runner, readyProbe);

        var ready = workflow.Analyze(GameKind.FreeFireMax, AutoTunerMode.Adaptive, "Pie64");
        Require(ready.CanStart, "A detected instance with telemetry available and no external player must be ready to tune.");
        Require(ready.Instance?.Name == "Pie64", "Readiness must bind the exact selected BlueStacks instance.");
        Require(ready.Candidates.Count > 0, "Readiness must precompute a real candidate set for the selected machine and mode.");

        var activePlayer = new OptimizeWorkflowService(
            new AutoTunerEngine(),
            runner,
            new FakeOptimizeSystemProbe(environment, playerRunning: true, telemetryReady: true))
            .Analyze(GameKind.FreeFireMax, AutoTunerMode.Adaptive, "Pie64");
        Require(!activePlayer.CanStart && activePlayer.Message.Contains("feche", StringComparison.OrdinalIgnoreCase),
            "Restart-required Auto Tuner must refuse to take ownership while an external BlueStacks player is already running.");

        var noTelemetry = new OptimizeWorkflowService(
            new AutoTunerEngine(),
            runner,
            new FakeOptimizeSystemProbe(environment, playerRunning: false, telemetryReady: false))
            .Analyze(GameKind.FreeFireMax, AutoTunerMode.Adaptive, "Pie64");
        Require(!noTelemetry.CanStart && noTelemetry.Message.Contains("PresentMon", StringComparison.OrdinalIgnoreCase),
            "Auto Tuner must not start without the real frame telemetry dependency.");

        var wrongInstance = workflow.Analyze(GameKind.FreeFire, AutoTunerMode.Deep, "Android11");
        Require(!wrongInstance.CanStart && wrongInstance.Instance is null,
            "An unknown requested instance must never silently fall back to a different BlueStacks instance.");
    }

    private static async Task RunsGeneratedSessionForExactAnalyzedInstance()
    {
        var instance = Instance("Pie64");
        var environment = EnvironmentWith(instance);
        var runner = new FakeSessionRunner();
        var workflow = new OptimizeWorkflowService(
            new AutoTunerEngine(),
            runner,
            new FakeOptimizeSystemProbe(environment, playerRunning: false, telemetryReady: true));
        var progress = new List<AutoTunerProgressPresentation>();

        var result = await workflow.RunAsync(
            GameKind.FreeFireMax,
            AutoTunerMode.Deep,
            "Pie64",
            visual => progress.Add(visual));

        Require(runner.CallCount == 1, "Optimize must execute exactly one generated Auto Tuner session per user start action.");
        Require(runner.LastInstance?.Name == "Pie64" && runner.LastGame == GameKind.FreeFireMax && runner.LastMode == AutoTunerMode.Deep,
            "Optimize must pass the exact selected instance, game, and mode into the real session service.");
        Require(progress.Count >= 2 && progress.First().Percent < progress.Last().Percent && progress.Last().Percent == 100,
            "Optimize must translate engine progress into monotonically useful user-facing progress ending at 100 percent.");
        Require(result.Session.ProfilesPersisted, "A completed validated session must disclose that its winner set was persisted.");
        Require(result.Recommended?.Kind == ProfileKind.Recommended, "Optimize completion must expose the recommended validated winner directly.");
        Require(result.Summary.Contains("Recomendado", StringComparison.OrdinalIgnoreCase) && result.Summary.Contains("Pie64", StringComparison.Ordinal),
            "Optimize completion summary must be based on measured result presentation, not generic success text.");
    }

    private static EnvironmentSnapshot EnvironmentWith(BlueStacksInstance instance) => new()
    {
        LogicalProcessors = 12,
        MemoryTotalGb = 16,
        BlueStacksDetected = true,
        Instances = [instance]
    };

    private static BlueStacksInstance Instance(string name) => new()
    {
        Name = name,
        CpuCores = 4,
        RamMb = 4096,
        Renderer = "OpenGL",
        Fps = 90,
        Resolution = "1280x720",
        AdbEnabled = true,
        AdbPort = 5555
    };

    private static PerformanceProfile Recommended() => new()
    {
        Name = "Recomendado",
        Kind = ProfileKind.Recommended,
        Game = GameKind.FreeFireMax,
        InstanceName = "Pie64",
        CpuCores = 6,
        RamMb = 6144,
        Renderer = "OpenGL",
        FpsTarget = 120,
        Resolution = "1600x900",
        Evidence = EvidenceLevel.Validated,
        Confidence = 0.96,
        AverageFps = 118,
        OnePercentLow = 107,
        FrameTimeMs = 8.4,
        LatencyMs = 8.1,
        StutterPercent = 0.7
    };

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class FakeOptimizeSystemProbe(EnvironmentSnapshot environment, bool playerRunning, bool telemetryReady) : IOptimizeSystemProbe
    {
        public EnvironmentSnapshot CaptureEnvironment() => environment;
        public bool IsPlayerRunning() => playerRunning;
        public bool IsFrameTelemetryReady() => telemetryReady;
    }

    private sealed class FakeSessionRunner : IAutoTunerSessionRunner
    {
        public int CallCount { get; private set; }
        public BlueStacksInstance? LastInstance { get; private set; }
        public GameKind LastGame { get; private set; }
        public AutoTunerMode LastMode { get; private set; }

        public Task<AutoTunerSessionResult> RunGeneratedAsync(
            EnvironmentSnapshot environment,
            BlueStacksInstance instance,
            GameKind game,
            AutoTunerMode mode,
            Action<AutoTunerRunProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastInstance = instance;
            LastGame = game;
            LastMode = mode;
            progress?.Invoke(new AutoTunerRunProgress(AutoTunerRunStage.ApplyingCandidate, 1, 1, "Applying candidate 1."));
            progress?.Invoke(new AutoTunerRunProgress(AutoTunerRunStage.Completed, 1, 1, "done"));

            var tuning = new TuningResult
            {
                Game = game,
                Mode = mode,
                Winners = [Recommended()],
                Evidence = [],
                Summary = "validated"
            };
            return Task.FromResult(new AutoTunerSessionResult(tuning, instance.Name, 1, true));
        }
    }
}
