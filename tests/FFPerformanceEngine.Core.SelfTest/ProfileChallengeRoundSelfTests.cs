using System.Reflection;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class ProfileChallengeRoundSelfTests
{
    public static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "ffpe-challenge-round-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var instance = new BlueStacksInstance
            {
                Name = "Pie64",
                AndroidVersion = "Pie 64-bit",
                CpuCores = 4,
                RamMb = 4096,
                Renderer = "Vulkan",
                Fps = 120,
                Resolution = "1600x900",
                Dpi = 240,
                AdbEnabled = true,
                AdbPort = 5555
            };
            var environment = new EnvironmentSnapshot
            {
                MachineName = "FFPE-ROUND-PC",
                WindowsDescription = "Windows 11 challenge round",
                LogicalProcessors = 16,
                MemoryTotalGb = 32,
                Is64BitOs = true,
                ActiveGame = GameKind.FreeFireMax,
                Instances = [instance]
            };
            var challengerInstance = instance with
            {
                CpuCores = 6,
                RamMb = 6144,
                Fps = 144,
                Resolution = "1920x1080"
            };
            var challengerConfiguration = PerformanceConfigurationSnapshot.Capture(
                environment with { Instances = [challengerInstance] },
                challengerInstance,
                GameKind.FreeFireMax)!;
            var createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);

            var incumbent = new PerformanceProfile
            {
                Name = "Recomendado",
                Kind = ProfileKind.Recommended,
                Game = GameKind.FreeFireMax,
                InstanceName = instance.Name,
                CpuCores = 4,
                RamMb = 4096,
                Renderer = "Vulkan",
                FpsTarget = 120,
                Resolution = "1600x900",
                Dpi = 240,
                Evidence = EvidenceLevel.Validated,
                Confidence = 0.95,
                CreatedAt = createdAt
            };
            var challenger = new PerformanceProfile
            {
                Name = "Custom Validated",
                Kind = ProfileKind.Custom,
                Game = GameKind.FreeFireMax,
                InstanceName = instance.Name,
                CpuCores = 6,
                RamMb = 6144,
                Renderer = "Vulkan",
                FpsTarget = 144,
                Resolution = "1920x1080",
                Dpi = 240,
                Evidence = EvidenceLevel.Validated,
                Confidence = 0.96,
                SourceComparisonId = Guid.NewGuid(),
                EnvironmentFingerprint = challengerConfiguration.Environment.Id,
                CreatedAt = createdAt
            };

            var profiles = new ProfileService(Path.Combine(root, "profiles.json"));
            var history = new HistoryService(Path.Combine(root, "history.json"));
            await profiles.SaveAsync([incumbent, challenger]);

            var fakeRuntime = new FakeRuntime(
                Samples(100, 10.0, 12.0),
                Samples(112, 8.9, 9.8));
            var factory = new FakeFactory(fakeRuntime);

            var serviceType = typeof(ProfileService).Assembly.GetType("FFPerformanceEngine.Core.Services.ProfileChallengeRoundService");
            Require(serviceType is not null,
                "Core must expose ProfileChallengeRoundService for one fully automated measured A/B round.");
            var constructor = serviceType!.GetConstructor([typeof(ProfileService), typeof(HistoryService), typeof(IAutoTunerRuntimeFactory)]);
            Require(constructor is not null,
                "Automated challenge round must depend on profiles, History, and the existing safe Auto Tuner runtime factory.");
            var service = constructor!.Invoke([profiles, history, factory]);
            var runAsync = serviceType.GetMethod(
                "RunAsync",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: [typeof(Guid), typeof(ProfileKind), typeof(EnvironmentSnapshot), typeof(BlueStacksInstance), typeof(CancellationToken)],
                modifiers: null);
            Require(runAsync is not null,
                "ProfileChallengeRoundService must expose RunAsync(challenger, role, environment, instance, cancellationToken).");

            var result = await InvokeTaskResultAsync(
                runAsync!, service, challenger.Id, ProfileKind.Recommended, environment, instance, CancellationToken.None);
            Require(result is not null && Read<bool>(result!, "Success"),
                "A fully measured physical A/B round must complete successfully.");
            Require(Read<int>(result!, "BaselineAcceptedSamples") == 2
                    && Read<int>(result!, "CandidateAcceptedSamples") == 2,
                "A and B must each require two accepted independent benchmark windows.");

            Require(fakeRuntime.Applied.Count == 2,
                "Automated round must apply exactly incumbent A then Custom B.");
            Require(CandidateMatches(fakeRuntime.Applied[0], incumbent)
                    && CandidateMatches(fakeRuntime.Applied[1], challenger),
                "Automated round must benchmark the exact profile tuning candidate for A and B.");
            Require(fakeRuntime.PreparedGames.SequenceEqual([GameKind.FreeFireMax, GameKind.FreeFireMax]),
                "Both sides must launch/prepare the same selected Free Fire game.");
            Require(fakeRuntime.CompleteCalls == 2 && fakeRuntime.RestoreCalls >= 1,
                "Every applied side must clean up, and the whole round must finish with an explicit baseline restore.");

            var comparisons = await history.LoadPerformanceComparisonsAsync();
            Require(comparisons.Count == 1,
                "One automated challenge round must persist exactly one A/B comparison.");
            var saved = comparisons.Single();
            Require(saved.ValidationStatus == PerformanceComparisonValidationStatus.Observed,
                "Automated challenge round must save observed evidence only and never auto-promote/auto-validate a profile.");
            Require(saved.Baseline.Quality == PerformanceEvidenceQuality.Measured
                    && saved.Candidate.Quality == PerformanceEvidenceQuality.Measured
                    && saved.Baseline.TelemetrySamples == 2
                    && saved.Candidate.TelemetrySamples == 2,
                "Saved A/B evidence must be fully measured and retain the two accepted benchmark windows per side.");
            Require(saved.Baseline.Configuration is not null
                    && saved.Candidate.Configuration is not null
                    && ConfigurationMatches(saved.Baseline.Configuration!, incumbent)
                    && ConfigurationMatches(saved.Candidate.Configuration!, challenger),
                "Saved evidence must bind exact incumbent and challenger configuration snapshots.");
            Require((await profiles.LoadAsync()).Single(profile => profile.Kind == ProfileKind.Recommended).Id == incumbent.Id,
                "Running an automated A/B round must not replace the winner.");
            Require(!(await history.LoadAsync()).Any(item => item.Kind == HistoryEventKind.Profile),
                "Running an automated A/B round must not emit a promotion event.");

            var failingRoot = Path.Combine(root, "failure");
            Directory.CreateDirectory(failingRoot);
            var failureProfiles = new ProfileService(Path.Combine(failingRoot, "profiles.json"));
            var failureHistory = new HistoryService(Path.Combine(failingRoot, "history.json"));
            await failureProfiles.SaveAsync([incumbent, challenger]);
            var failureRuntime = new FakeRuntime(
                Samples(100, 10.0, 12.0),
                [new TelemetrySample { Fps = 112, FrameTimeMs = 8.9, LatencyMs = 9.8, DataQuality = "Partial" }]);
            var failureService = constructor.Invoke([failureProfiles, failureHistory, new FakeFactory(failureRuntime)]);
            var failure = await InvokeTaskResultAsync(
                runAsync!, failureService, challenger.Id, ProfileKind.Recommended, environment, instance, CancellationToken.None);
            Require(failure is not null && !Read<bool>(failure!, "Success"),
                "A side without two acceptable measured windows must fail closed.");
            Require(failureRuntime.CompleteCalls >= 2 && failureRuntime.RestoreCalls >= 1,
                "Failed B measurement must still clean the applied side and restore the original BlueStacks baseline.");
            Require((await failureHistory.LoadPerformanceComparisonsAsync()).Count == 0,
                "A failed automated round must never persist partial A/B evidence.");

            Console.WriteLine("PASS automated physical A/B challenge round, exact configs, measured evidence, no promotion, and rollback-on-failure");
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    private static IReadOnlyList<TelemetrySample> Samples(double fps, double frameTime, double latency)
        =>
        [
            new TelemetrySample { Fps = fps - 1, FrameTimeMs = frameTime + 0.1, LatencyMs = latency + 0.1, DataQuality = "PresentMon · 240 frames" },
            new TelemetrySample { Fps = fps + 1, FrameTimeMs = frameTime - 0.1, LatencyMs = latency - 0.1, DataQuality = "PresentMon · 260 frames" }
        ];

    private static bool CandidateMatches(TuningCandidate candidate, PerformanceProfile profile)
        => candidate.CpuCores == profile.CpuCores
           && candidate.RamMb == profile.RamMb
           && string.Equals(candidate.Renderer, profile.Renderer, StringComparison.OrdinalIgnoreCase)
           && candidate.FpsTarget == profile.FpsTarget
           && string.Equals(candidate.Resolution, profile.Resolution, StringComparison.OrdinalIgnoreCase);

    private static bool ConfigurationMatches(PerformanceConfigurationSnapshot configuration, PerformanceProfile profile)
        => configuration.Game == profile.Game
           && string.Equals(configuration.InstanceName, profile.InstanceName, StringComparison.OrdinalIgnoreCase)
           && configuration.CpuCores == profile.CpuCores
           && configuration.RamMb == profile.RamMb
           && string.Equals(configuration.Renderer, profile.Renderer, StringComparison.OrdinalIgnoreCase)
           && configuration.FpsTarget == profile.FpsTarget
           && string.Equals(configuration.Resolution, profile.Resolution, StringComparison.OrdinalIgnoreCase)
           && configuration.Dpi == profile.Dpi;

    private sealed class FakeFactory(IAutoTunerRuntime runtime) : IAutoTunerRuntimeFactory
    {
        public IAutoTunerRuntime Create(BlueStacksInstance instance) => runtime;
    }

    private sealed class FakeRuntime : IAutoTunerRuntime
    {
        private readonly Queue<IReadOnlyList<TelemetrySample>> _sideSamples;
        private Queue<TelemetrySample> _current = new();

        public FakeRuntime(params IReadOnlyList<TelemetrySample>[] sides)
            => _sideSamples = new Queue<IReadOnlyList<TelemetrySample>>(sides);

        public List<TuningCandidate> Applied { get; } = new();
        public List<GameKind> PreparedGames { get; } = new();
        public int CompleteCalls { get; private set; }
        public int RestoreCalls { get; private set; }

        public Task<AutoTunerRuntimeResult> ApplyCandidateAsync(TuningCandidate candidate, CancellationToken cancellationToken = default)
        {
            Applied.Add(candidate);
            _current = _sideSamples.Count > 0
                ? new Queue<TelemetrySample>(_sideSamples.Dequeue())
                : new Queue<TelemetrySample>();
            return Task.FromResult(AutoTunerRuntimeResult.Ok("fake applied"));
        }

        public Task<AutoTunerRuntimeResult> PrepareGameAsync(GameKind game, CancellationToken cancellationToken = default)
        {
            PreparedGames.Add(game);
            return Task.FromResult(AutoTunerRuntimeResult.Ok("fake prepared"));
        }

        public Task<TelemetrySample?> CaptureBenchmarkAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<TelemetrySample?>(_current.Count > 0 ? _current.Dequeue() : null);

        public Task CompleteCandidateAsync(CancellationToken cancellationToken = default)
        {
            CompleteCalls++;
            return Task.CompletedTask;
        }

        public Task RestoreBaselineAsync(CancellationToken cancellationToken = default)
        {
            RestoreCalls++;
            return Task.CompletedTask;
        }
    }

    private static async Task<object?> InvokeTaskResultAsync(MethodInfo method, object? target, params object?[] args)
    {
        object? invocation;
        try { invocation = method.Invoke(target, args); }
        catch (TargetInvocationException exception) when (exception.InnerException is not null) { throw exception.InnerException; }
        Require(invocation is Task, $"{method.Name} must return Task or Task<T>.");
        await ((Task)invocation!).ConfigureAwait(false);
        return invocation!.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance)?.GetValue(invocation);
    }

    private static T Read<T>(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Require(property is not null, $"Expected public property {propertyName} on {instance.GetType().Name}.");
        return (T)property!.GetValue(instance)!;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
