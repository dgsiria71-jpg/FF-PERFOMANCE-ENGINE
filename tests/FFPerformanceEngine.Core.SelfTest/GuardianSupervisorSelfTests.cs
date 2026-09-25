using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class GuardianSupervisorSelfTests
{
    public static async Task RunAsync()
    {
        RequiresUnambiguousPlayerBinding();
        await UsesValidatedBaselineAndExactBoundPid();
        await SuppressesRepeatedCanariesDuringCooldown();
        await DoesNotActWithoutMatchingValidatedBaseline();
        Console.WriteLine("PASS Guardian exact-PID supervision, validated baseline, and cooldown");
    }

    private static void RequiresUnambiguousPlayerBinding()
    {
        var none = new GuardianPlayerBindingService(new FakePlayerProcessProbe([])).TryBind("Pie64");
        Require(!none.Success && none.Binding is null, "Guardian must not invent a process binding when no BlueStacks player is running.");

        var ambiguous = new GuardianPlayerBindingService(new FakePlayerProcessProbe([101, 202])).TryBind("Pie64");
        Require(!ambiguous.Success && ambiguous.Message.Contains("amb", StringComparison.OrdinalIgnoreCase),
            "Guardian must refuse an ambiguous multi-player process set instead of choosing the largest process heuristically.");

        var exact = new GuardianPlayerBindingService(new FakePlayerProcessProbe([4242])).TryBind("Pie64");
        Require(exact.Success && exact.Binding?.ProcessId == 4242 && exact.Binding.InstanceName == "Pie64",
            "A single player process may be bound exactly to the selected session instance.");
    }

    private static async Task UsesValidatedBaselineAndExactBoundPid()
    {
        var root = TempRoot();
        try
        {
            var profiles = new ProfileService(Path.Combine(root, "profiles.json"));
            await profiles.SaveAsync([Baseline("Pie64", GameKind.FreeFireMax, 120)]);
            var engine = new GuardianEngine { Mode = GuardianMode.Adaptive };
            var source = new FakeObservationSource(Match(GameKind.FreeFireMax, 80));
            var executor = new FakeCanaryExecutor(new GuardianCanaryResult { Attempted = true, Kept = true, Message = "kept" });
            var now = new DateTimeOffset(2026, 9, 5, 1, 0, 0, TimeSpan.Zero);
            var supervisor = new GuardianSupervisor(engine, profiles, source, executor, () => now, TimeSpan.FromSeconds(30));

            var cycle = await supervisor.ObserveOnceAsync(new GuardianSessionBinding(4242, "Pie64"));

            Require(engine.State == GameState.Match, "Guardian engine state must follow the multimodal observation before evaluating an action.");
            Require(cycle.Baseline?.AverageFps == 120, "Guardian must use the validated profile for the exact game and BlueStacks instance as its expected baseline.");
            Require(cycle.Decision.ShouldAct, "A persistent 33 percent FPS degradation in Adaptive mode must be eligible for a LiveSafe canary.");
            Require(executor.CallCount == 1 && executor.LastProcessId == 4242,
                "Guardian must execute the canary on the exact bound PID, never on a rediscovered arbitrary HD-Player process.");
            Require(executor.LastExpectedFps == 120 && cycle.Canary?.Kept == true,
                "The canary executor must receive the measured validated FPS baseline and surface its keep/rollback result.");
        }
        finally
        {
            TryDelete(root);
        }
    }

    private static async Task SuppressesRepeatedCanariesDuringCooldown()
    {
        var root = TempRoot();
        try
        {
            var profiles = new ProfileService(Path.Combine(root, "profiles.json"));
            await profiles.SaveAsync([Baseline("Pie64", GameKind.FreeFire, 100)]);
            var engine = new GuardianEngine { Mode = GuardianMode.Aggressive };
            var source = new FakeObservationSource(Match(GameKind.FreeFire, 70));
            var executor = new FakeCanaryExecutor(new GuardianCanaryResult { Attempted = true, Kept = false, RolledBack = true, Message = "reverted" });
            var now = new DateTimeOffset(2026, 9, 5, 1, 0, 0, TimeSpan.Zero);
            var supervisor = new GuardianSupervisor(engine, profiles, source, executor, () => now, TimeSpan.FromSeconds(45));
            var binding = new GuardianSessionBinding(9001, "Pie64");

            var first = await supervisor.ObserveOnceAsync(binding);
            var second = await supervisor.ObserveOnceAsync(binding);

            Require(first.Canary?.RolledBack == true, "A failed canary must report that the live change was rolled back.");
            Require(second.InCooldown && executor.CallCount == 1,
                "Guardian must cool down after a canary attempt so it cannot thrash the same live setting repeatedly.");

            now = now.AddSeconds(46);
            _ = await supervisor.ObserveOnceAsync(binding);
            Require(executor.CallCount == 2, "Guardian may re-evaluate the action after the configured cooldown expires.");
        }
        finally
        {
            TryDelete(root);
        }
    }

    private static async Task DoesNotActWithoutMatchingValidatedBaseline()
    {
        var root = TempRoot();
        try
        {
            var profiles = new ProfileService(Path.Combine(root, "profiles.json"));
            await profiles.SaveAsync([
                Baseline("Android11", GameKind.FreeFireMax, 150),
                Baseline("Pie64", GameKind.FreeFire, 120)
            ]);
            var engine = new GuardianEngine { Mode = GuardianMode.Adaptive };
            var source = new FakeObservationSource(Match(GameKind.FreeFireMax, 50));
            var executor = new FakeCanaryExecutor(new GuardianCanaryResult { Attempted = true, Kept = true });
            var supervisor = new GuardianSupervisor(engine, profiles, source, executor);

            var cycle = await supervisor.ObserveOnceAsync(new GuardianSessionBinding(77, "Pie64"));

            Require(cycle.Baseline is null && !cycle.Decision.ShouldAct && executor.CallCount == 0,
                "Guardian must not borrow a baseline from another game or another BlueStacks instance just to justify an intervention.");
        }
        finally
        {
            TryDelete(root);
        }
    }

    private static PerformanceProfile Baseline(string instance, GameKind game, double fps) => new()
    {
        Name = "Recomendado",
        Kind = ProfileKind.Recommended,
        Game = game,
        InstanceName = instance,
        Evidence = EvidenceLevel.Validated,
        Confidence = 0.96,
        AverageFps = fps,
        OnePercentLow = fps * 0.9,
        FrameTimeMs = 1000d / fps,
        CpuCores = 6,
        RamMb = 6144,
        Renderer = "OpenGL",
        FpsTarget = (int)Math.Round(fps),
        Resolution = "1600x900"
    };

    private static SessionStateObservation Match(GameKind game, double fps) => new()
    {
        State = GameState.Match,
        ActiveGame = game,
        Signals = new GameStateSignals
        {
            BlueStacksRunning = true,
            ForegroundGame = game,
            RecentInput = true,
            Fps = fps,
            FrameTimeVarianceMs = 2
        },
        Telemetry = new TelemetrySample
        {
            Fps = fps,
            OnePercentLow = fps * 0.85,
            FrameTimeMs = 1000d / fps,
            FrameTimeP95Ms = 1000d / (fps * 0.82),
            FrameTimeP99Ms = 1000d / (fps * 0.78),
            DataQuality = "PresentMon · 1000 frames"
        }
    };

    private static string TempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "ffpe-guardian-selftest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void TryDelete(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class FakePlayerProcessProbe(IReadOnlyList<int> processIds) : IBlueStacksPlayerProcessProbe
    {
        public IReadOnlyList<int> GetRunningPlayerProcessIds() => processIds;
    }

    private sealed class FakeObservationSource(SessionStateObservation observation) : IGuardianObservationSource
    {
        public Task<SessionStateObservation> CaptureAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(observation);
    }

    private sealed class FakeCanaryExecutor(GuardianCanaryResult result) : IGuardianCanaryExecutor
    {
        public int CallCount { get; private set; }
        public int LastProcessId { get; private set; }
        public double LastExpectedFps { get; private set; }

        public Task<GuardianCanaryResult> ExecuteAboveNormalPriorityAsync(int processId, double expectedFps, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastProcessId = processId;
            LastExpectedFps = expectedFps;
            return Task.FromResult(result);
        }
    }
}
