using System.Reflection;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class ProfileChallengePromotionSelfTests
{
    public static async Task RunAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "ffpe-profile-challenge-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var environment = new EnvironmentSnapshot
            {
                MachineName = "FFPE-CHALLENGE-PC",
                WindowsDescription = "Windows 11 test",
                LogicalProcessors = 16,
                MemoryTotalGb = 32,
                Is64BitOs = true,
                ActiveGame = GameKind.FreeFireMax
            };

            var incumbentInstance = new BlueStacksInstance
            {
                Name = "Pie64",
                AndroidVersion = "Pie 64-bit",
                CpuCores = 4,
                RamMb = 4096,
                Renderer = "Vulkan",
                Fps = 120,
                Resolution = "1600x900",
                Dpi = 240
            };
            var challengerInstance = incumbentInstance with
            {
                CpuCores = 6,
                RamMb = 6144,
                Fps = 144,
                Resolution = "1920x1080"
            };
            environment = environment with { Instances = [incumbentInstance] };

            var incumbentConfiguration = PerformanceConfigurationSnapshot.Capture(environment, incumbentInstance, GameKind.FreeFireMax)!;
            var challengerConfiguration = PerformanceConfigurationSnapshot.Capture(
                environment with { Instances = [challengerInstance] },
                challengerInstance,
                GameKind.FreeFireMax)!;

            var sourceComparisonId = Guid.NewGuid();
            var incumbent = new PerformanceProfile
            {
                Name = "Recomendado",
                Kind = ProfileKind.Recommended,
                Game = GameKind.FreeFireMax,
                InstanceName = "Pie64",
                CpuCores = 4,
                RamMb = 4096,
                Renderer = "Vulkan",
                FpsTarget = 120,
                Resolution = "1600x900",
                Dpi = 240,
                Evidence = EvidenceLevel.Validated,
                Confidence = 0.94,
                AverageFps = 100,
                FrameTimeMs = 10,
                LatencyMs = 12
            };
            var challenger = new PerformanceProfile
            {
                Name = "Custom Validated",
                Kind = ProfileKind.Custom,
                Game = GameKind.FreeFireMax,
                InstanceName = "Pie64",
                CpuCores = 6,
                RamMb = 6144,
                Renderer = "Vulkan",
                FpsTarget = 144,
                Resolution = "1920x1080",
                Dpi = 240,
                Evidence = EvidenceLevel.Validated,
                Confidence = 0.96,
                AverageFps = 110,
                FrameTimeMs = 9,
                LatencyMs = 10,
                SourceComparisonId = sourceComparisonId,
                EnvironmentFingerprint = challengerConfiguration.Environment.Id
            };

            var profilesPath = Path.Combine(tempRoot, "profiles.json");
            var historyPath = Path.Combine(tempRoot, "history.json");
            var profiles = new ProfileService(profilesPath);
            var history = new HistoryService(historyPath);
            await profiles.SaveAsync([incumbent, challenger]);

            var serviceType = typeof(ProfileService).Assembly.GetType("FFPerformanceEngine.Core.Services.ProfileChallengeService");
            Require(serviceType is not null,
                "Core must expose ProfileChallengeService for measured custom-vs-winner competition.");
            var constructor = serviceType!.GetConstructor([typeof(ProfileService), typeof(HistoryService)]);
            Require(constructor is not null,
                "ProfileChallengeService must depend on the persisted profile and history stores.");
            var service = constructor!.Invoke([profiles, history]);
            var assessAndPromote = serviceType.GetMethod(
                "AssessAndPromoteLatestAsync",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: [typeof(Guid), typeof(ProfileKind), typeof(EnvironmentSnapshot), typeof(CancellationToken)],
                modifiers: null);
            Require(assessAndPromote is not null,
                "Profile challenge must expose one explicit evidence-gated assess/promote operation.");

            var evaluatorType = typeof(ProfileService).Assembly.GetType("FFPerformanceEngine.Core.Services.ProfileChallengeEvaluator");
            Require(evaluatorType is not null,
                "Core must expose the role-specific ProfileChallengeEvaluator.");
            var evaluateRound = evaluatorType!.GetMethod(
                "Evaluate",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: [typeof(ProfileKind), typeof(PerformanceEvidenceSnapshot), typeof(PerformanceEvidenceSnapshot)],
                modifiers: null);
            Require(evaluateRound is not null,
                "ProfileChallengeEvaluator must evaluate a fully measured A/B round for a target winner role.");

            var roleBaseline = Snapshot(
                "A role",
                incumbentConfiguration,
                new[] { 98d, 100d, 102d, 100d },
                new[] { 10.2d, 10.0d, 9.8d, 10.0d },
                new[] { 12.2d, 12.0d, 11.8d, 12.0d },
                new DateTimeOffset(2026, 9, 6, 9, 0, 0, TimeSpan.Zero));
            var roleCandidate = Snapshot(
                "B role",
                challengerConfiguration,
                new[] { 109d, 111d, 110d, 112d },
                new[] { 9.1d, 8.9d, 9.0d, 8.8d },
                new[] { 10.2d, 10.0d, 9.8d, 9.9d },
                new DateTimeOffset(2026, 9, 6, 9, 1, 0, TimeSpan.Zero));

            foreach (var role in new[]
                     {
                         ProfileKind.Recommended,
                         ProfileKind.MaximumFps,
                         ProfileKind.LowestLatency,
                         ProfileKind.Stability,
                         ProfileKind.Quality
                     })
            {
                var verdict = evaluateRound!.Invoke(null, [role, roleBaseline, roleCandidate]);
                Require(verdict?.ToString() == "ChallengerWins",
                    $"A clearly superior fully measured challenger must be able to win the {role} role under its role-specific rule.");
            }

            var round1 = await history.SavePerformanceComparisonAsync(
                "Challenge round 1",
                PerformanceABComparison.Create(
                    Snapshot(
                        "A · Recomendado",
                        incumbentConfiguration,
                        new[] { 99d, 100d, 101d, 100d },
                        new[] { 10.1d, 10.0d, 9.9d, 10.0d },
                        new[] { 12.1d, 12.0d, 11.9d, 12.0d },
                        new DateTimeOffset(2026, 9, 6, 9, 10, 0, TimeSpan.Zero)),
                    Snapshot(
                        "B · Custom",
                        challengerConfiguration,
                        new[] { 109d, 110d, 111d, 110d },
                        new[] { 9.1d, 9.0d, 8.9d, 9.0d },
                        new[] { 10.1d, 10.0d, 9.9d, 10.0d },
                        new DateTimeOffset(2026, 9, 6, 9, 11, 0, TimeSpan.Zero))));

            var oneRound = await InvokeTaskResultAsync(
                assessAndPromote!, service, challenger.Id, ProfileKind.Recommended, environment, CancellationToken.None);
            Require(oneRound is not null
                    && !Read<bool>(oneRound!, "Promoted")
                    && Read<int>(oneRound!, "EvidenceRounds") == 1,
                "One measured A/B round must never be enough to replace an existing winner.");

            var round2 = await history.SavePerformanceComparisonAsync(
                "Challenge round 2 · revalidation",
                PerformanceABComparison.Create(
                    Snapshot(
                        "A · Recomendado revalidation",
                        incumbentConfiguration,
                        new[] { 100d, 101d, 100d, 101d },
                        new[] { 10.0d, 9.9d, 10.0d, 9.9d },
                        new[] { 12.2d, 12.1d, 12.0d, 12.1d },
                        new DateTimeOffset(2026, 9, 6, 9, 20, 0, TimeSpan.Zero)),
                    Snapshot(
                        "B · Custom revalidation",
                        challengerConfiguration,
                        new[] { 111d, 112d, 113d, 112d },
                        new[] { 8.9d, 8.8d, 8.7d, 8.8d },
                        new[] { 9.9d, 9.8d, 9.7d, 9.8d },
                        new DateTimeOffset(2026, 9, 6, 9, 21, 0, TimeSpan.Zero))));

            var driftedEnvironment = environment with { MachineName = "DIFFERENT-PC" };
            var driftBlocked = await InvokeTaskResultAsync(
                assessAndPromote!, service, challenger.Id, ProfileKind.Recommended, driftedEnvironment, CancellationToken.None);
            Require(driftBlocked is not null
                    && !Read<bool>(driftBlocked!, "Promoted")
                    && Read<object>(driftBlocked!, "Status").ToString() == "EnvironmentDrift",
                "Structural environment drift must block winner promotion even when two historical rounds measured an improvement.");

            var promotedResult = await InvokeTaskResultAsync(
                assessAndPromote!, service, challenger.Id, ProfileKind.Recommended, environment, CancellationToken.None);
            Require(promotedResult is not null
                    && Read<bool>(promotedResult!, "Promoted")
                    && Read<int>(promotedResult!, "EvidenceRounds") >= 2
                    && Read<object>(promotedResult!, "Status").ToString() == "Promoted",
                "Two independent fully measured winning rounds in the compatible environment must permit explicit promotion.");

            var reloaded = await new ProfileService(profilesPath).LoadAsync();
            var promoted = reloaded.Single(profile => profile.Kind == ProfileKind.Recommended);
            var preservedCustom = reloaded.Single(profile => profile.Id == challenger.Id);
            Require(reloaded.Count == 2 && preservedCustom.Kind == ProfileKind.Custom,
                "Promotion must replace only the target winner role and preserve the validated Custom source profile.");
            Require(promoted.Id != challenger.Id
                    && promoted.Evidence == EvidenceLevel.Validated
                    && promoted.CpuCores == challenger.CpuCores
                    && promoted.RamMb == challenger.RamMb
                    && promoted.Renderer == challenger.Renderer
                    && promoted.FpsTarget == challenger.FpsTarget
                    && promoted.Resolution == challenger.Resolution
                    && promoted.Dpi == challenger.Dpi,
                "Promoted winner must be a distinct validated generated-role profile carrying the exact challenger configuration.");
            Require(promoted.SourceComparisonId == round2.Id
                    && promoted.EnvironmentFingerprint == challengerConfiguration.Environment.Id,
                "Promoted winner must retain the revalidation round and environment fingerprint as auditable provenance.");
            Require(promoted.AverageFps is > 111.9 and < 112.1
                    && promoted.FrameTimeMs is > 8.7 and < 8.9
                    && promoted.LatencyMs is > 9.7 and < 9.9,
                "Promoted winner metrics must come from the measured revalidation round, never copied from stale profile metadata.");

            var events = await history.LoadAsync();
            Require(events.Any(item => item.Kind == HistoryEventKind.Profile
                                       && item.Title.Contains("promovido", StringComparison.OrdinalIgnoreCase)),
                "Successful winner promotion must leave an auditable Profile event in History.");

            Console.WriteLine("PASS measured two-round custom profile challenge, drift gate, role evaluation, and winner promotion");
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { }
        }
    }

    private static PerformanceEvidenceSnapshot Snapshot(
        string name,
        PerformanceConfigurationSnapshot configuration,
        IReadOnlyList<double> fps,
        IReadOnlyList<double> frameTimes,
        IReadOnlyList<double> latency,
        DateTimeOffset start)
    {
        Require(fps.Count == frameTimes.Count && fps.Count == latency.Count && fps.Count > 0,
            "Synthetic measured snapshot arrays must have equal non-zero length.");
        var points = Enumerable.Range(0, fps.Count)
            .Select(index => new PerformanceTimelinePoint
            {
                Timestamp = start.AddSeconds(index),
                Fps = fps[index],
                FrameTimeMs = frameTimes[index],
                LatencyMs = latency[index],
                DataQuality = "Measured"
            })
            .ToArray();
        return PerformanceEvidenceSnapshot.Capture(
            name,
            new PerformanceIntervalSummary
            {
                Start = start,
                End = start.AddSeconds(points.Length - 1),
                TelemetrySamples = points.Length,
                FpsEvidenceSamples = points.Length,
                AverageFps = fps.Average(),
                AverageFrameTimeMs = frameTimes.Average(),
                Points = points
            },
            start.AddSeconds(points.Length),
            configuration);
    }

    private static async Task<object?> InvokeTaskResultAsync(MethodInfo method, object? target, params object?[] args)
    {
        object? invocation;
        try
        {
            invocation = method.Invoke(target, args);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw exception.InnerException;
        }

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
