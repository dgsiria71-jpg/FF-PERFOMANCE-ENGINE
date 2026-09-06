using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class AutoTunerSessionPersistenceSelfTests
{
    internal static async Task RunAsync()
    {
        await ReplacesOnlyMatchingGeneratedWinnerSet();
        await PersistsValidatedSessionWinnersWithInstanceBinding();
        await KeepsPriorProfilesWhenSessionProducesNoValidatedWinner();
        Console.WriteLine("PASS atomic Auto Tuner session and winner persistence");
    }

    private static async Task ReplacesOnlyMatchingGeneratedWinnerSet()
    {
        var root = TempRoot();
        try
        {
            var profiles = new ProfileService(Path.Combine(root, "profiles.json"));
            var custom = Profile("Custom FF", ProfileKind.Custom, GameKind.FreeFire, "Pie64", 72);
            var oldRecommended = Profile("Old recommended", ProfileKind.Recommended, GameKind.FreeFire, "Pie64", 80);
            var otherGame = Profile("MAX recommended", ProfileKind.Recommended, GameKind.FreeFireMax, "Pie64", 95);
            var otherInstance = Profile("Android11 recommended", ProfileKind.Recommended, GameKind.FreeFire, "Android11", 88);
            await profiles.SaveAsync([custom, oldRecommended, otherGame, otherInstance]);

            var newRecommended = Profile("Recommended", ProfileKind.Recommended, GameKind.FreeFire, string.Empty, 120);
            var newFps = Profile("Maximum FPS", ProfileKind.MaximumFps, GameKind.FreeFire, string.Empty, 135);
            await profiles.ReplaceAutoTunerWinnersAsync(GameKind.FreeFire, "Pie64", [newRecommended, newFps]);

            var saved = await profiles.LoadAsync();
            Require(saved.Any(x => x.Id == custom.Id), "Custom profiles must never be deleted by Auto Tuner winner replacement.");
            Require(saved.Any(x => x.Id == otherGame.Id), "Winner profiles for the other game must be preserved.");
            Require(saved.Any(x => x.Id == otherInstance.Id), "Winner profiles for another BlueStacks instance must be preserved.");
            Require(!saved.Any(x => x.Id == oldRecommended.Id), "The previous generated winner set for the same game and instance must be replaced atomically.");
            var replacements = saved.Where(x => x.Game == GameKind.FreeFire && x.InstanceName == "Pie64" && x.Kind != ProfileKind.Custom).ToList();
            Require(replacements.Count == 2, "Exactly the new generated winner set must exist for the tuned game and instance.");
            Require(replacements.All(x => x.Evidence == EvidenceLevel.Validated), "Only validated profiles may enter the generated winner set.");
        }
        finally
        {
            TryDelete(root);
        }
    }

    private static async Task PersistsValidatedSessionWinnersWithInstanceBinding()
    {
        var root = TempRoot();
        try
        {
            var profiles = new ProfileService(Path.Combine(root, "profiles.json"));
            var history = new HistoryService(Path.Combine(root, "history.json"));
            var factory = new FakeRuntimeFactory(() => new FakeRuntime([
                ValidSample(118), ValidSample(119)
            ]));
            var service = new AutoTunerSessionService(new AutoTunerEngine(), factory, profiles, history);
            var instance = Instance("Pie64");

            var result = await service.RunCandidatesAsync(instance, GameKind.FreeFireMax, AutoTunerMode.Adaptive, [Candidate()]);

            Require(result.Tuning.Winners.Count == 5, "A stable candidate must produce the five winner roles even when one configuration wins multiple roles.");
            Require(result.Tuning.Winners.All(x => x.InstanceName == "Pie64"), "Persisted winners must be bound to the exact BlueStacks instance that was benchmarked.");
            Require(result.ProfilesPersisted, "A validated winner set must be persisted after baseline restoration completes.");
            var saved = await profiles.LoadAsync();
            Require(saved.Count == 5 && saved.All(x => x.InstanceName == "Pie64" && x.Game == GameKind.FreeFireMax), "Saved profiles must match the completed game/instance tuning session.");
            var events = await history.LoadAsync();
            Require(events.Any(x => x.Kind == HistoryEventKind.Optimization && x.Summary.Contains("5", StringComparison.Ordinal)), "Completed optimization must leave an auditable history event with winner count.");
            Require(factory.CreatedFor.SequenceEqual(["Pie64"]), "The runtime factory must receive the selected BlueStacks instance exactly once.");
        }
        finally
        {
            TryDelete(root);
        }
    }

    private static async Task KeepsPriorProfilesWhenSessionProducesNoValidatedWinner()
    {
        var root = TempRoot();
        try
        {
            var profiles = new ProfileService(Path.Combine(root, "profiles.json"));
            var old = Profile("Known good", ProfileKind.Recommended, GameKind.FreeFire, "Pie64", 100);
            await profiles.SaveAsync([old]);
            var history = new HistoryService(Path.Combine(root, "history.json"));
            var factory = new FakeRuntimeFactory(() => new FakeRuntime([
                ValidSample(60), ValidSample(120), ValidSample(62), ValidSample(118), ValidSample(61)
            ]));
            var service = new AutoTunerSessionService(
                new AutoTunerEngine(),
                factory,
                profiles,
                history,
                new AutoTunerValidationPolicy
                {
                    AdaptiveRequiredSamples = 2,
                    DeepRequiredSamples = 3,
                    MaxAttemptsPerCandidate = 5,
                    MaximumFpsCoefficientOfVariation = 0.05
                });

            var result = await service.RunCandidatesAsync(Instance("Pie64"), GameKind.FreeFire, AutoTunerMode.Adaptive, [Candidate()]);

            Require(result.Tuning.Winners.Count == 0, "A candidate that never converges must not produce a winner.");
            Require(!result.ProfilesPersisted, "A run without validated winners must not overwrite the known-good profile set.");
            var saved = await profiles.LoadAsync();
            Require(saved.Count == 1 && saved[0].Id == old.Id, "Known-good profiles must survive an inconclusive tuning session unchanged.");
        }
        finally
        {
            TryDelete(root);
        }
    }

    private static string TempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "ffpe-selftest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void TryDelete(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static PerformanceProfile Profile(string name, ProfileKind kind, GameKind game, string instance, double fps) => new()
    {
        Name = name,
        Kind = kind,
        Game = game,
        InstanceName = instance,
        CpuCores = 4,
        RamMb = 4096,
        Renderer = "OpenGL",
        FpsTarget = (int)Math.Round(fps),
        Resolution = "1280x720",
        Evidence = EvidenceLevel.Validated,
        Confidence = 0.95,
        AverageFps = fps,
        OnePercentLow = fps * 0.88,
        StutterPercent = 1.0
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

    private static TuningCandidate Candidate() => new()
    {
        CpuCores = 4,
        RamMb = 4096,
        Renderer = "OpenGL",
        FpsTarget = 120,
        Resolution = "1280x720"
    };

    private static TelemetrySample ValidSample(double fps) => new()
    {
        Fps = fps,
        OnePercentLow = fps * 0.88,
        FrameTimeMs = 1000d / fps,
        FrameTimeP95Ms = 1000d / (fps * 0.82),
        StutterPercent = 0.8,
        LatencyMs = 8,
        DataQuality = "PresentMon · 1200 frames"
    };

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class FakeRuntimeFactory(Func<IAutoTunerRuntime> create) : IAutoTunerRuntimeFactory
    {
        public List<string> CreatedFor { get; } = [];

        public IAutoTunerRuntime Create(BlueStacksInstance instance)
        {
            CreatedFor.Add(instance.Name);
            return create();
        }
    }

    private sealed class FakeRuntime(IEnumerable<TelemetrySample> samples) : IAutoTunerRuntime
    {
        private readonly Queue<TelemetrySample> _samples = new(samples);

        public Task<AutoTunerRuntimeResult> ApplyCandidateAsync(TuningCandidate candidate, CancellationToken cancellationToken = default)
            => Task.FromResult(AutoTunerRuntimeResult.Ok("applied"));

        public Task<AutoTunerRuntimeResult> PrepareGameAsync(GameKind game, CancellationToken cancellationToken = default)
            => Task.FromResult(AutoTunerRuntimeResult.Ok("prepared"));

        public Task<TelemetrySample?> CaptureBenchmarkAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<TelemetrySample?>(_samples.Count == 0 ? null : _samples.Dequeue());

        public Task CompleteCandidateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RestoreBaselineAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
