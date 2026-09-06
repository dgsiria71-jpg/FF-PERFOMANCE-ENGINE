using System.Reflection;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class ProfileChallengeProgressSelfTests
{
    public static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "ffpe-challenge-progress-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var environment = new EnvironmentSnapshot
            {
                MachineName = "FFPE-PROGRESS-PC",
                WindowsDescription = "Windows 11 progress build",
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
            var challengerEnvironment = environment with { Instances = [challengerInstance] };
            var challengerConfiguration = PerformanceConfigurationSnapshot.Capture(challengerEnvironment, challengerInstance, GameKind.FreeFireMax)!;
            var createdAt = new DateTimeOffset(2026, 9, 6, 11, 0, 0, TimeSpan.Zero);

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
                Confidence = 0.95,
                CreatedAt = createdAt
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
                SourceComparisonId = Guid.NewGuid(),
                EnvironmentFingerprint = challengerConfiguration.Environment.Id,
                CreatedAt = createdAt
            };

            var profiles = new ProfileService(Path.Combine(root, "profiles.json"));
            var history = new HistoryService(Path.Combine(root, "history.json"));
            await profiles.SaveAsync([incumbent, challenger]);

            var type = typeof(ProfileService).Assembly.GetType("FFPerformanceEngine.Core.Services.ProfileChallengeProgressService");
            Require(type is not null, "Core must expose ProfileChallengeProgressService for non-mutating challenge inspection.");
            var constructor = type!.GetConstructor([typeof(ProfileService), typeof(HistoryService)]);
            Require(constructor is not null, "ProfileChallengeProgressService must read the persisted profile and comparison stores.");
            var getAsync = type.GetMethod(
                "GetAsync",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: [typeof(Guid), typeof(ProfileKind), typeof(EnvironmentSnapshot), typeof(CancellationToken)],
                modifiers: null);
            Require(getAsync is not null, "ProfileChallengeProgressService must expose GetAsync(challenger, role, environment, cancellationToken).");
            var service = constructor!.Invoke([profiles, history]);

            await history.SavePerformanceComparisonAsync(
                "Progress round 1",
                Comparison(incumbentConfiguration, challengerConfiguration, createdAt.AddMinutes(10)));

            var oneRound = await InvokeTaskResultAsync(
                getAsync!, service, challenger.Id, ProfileKind.Recommended, environment, CancellationToken.None);
            Require(oneRound is not null
                    && Read<int>(oneRound!, "EligibleRounds") == 1
                    && !Read<bool>(oneRound!, "CanPromote")
                    && Read<object>(oneRound!, "Status").ToString() == "AwaitingEvidence",
                "One eligible measured round must report 1/2 and remain read-only/not promotable.");

            await history.SavePerformanceComparisonAsync(
                "Progress round 2",
                Comparison(incumbentConfiguration, challengerConfiguration, createdAt.AddMinutes(20)));

            var ready = await InvokeTaskResultAsync(
                getAsync!, service, challenger.Id, ProfileKind.Recommended, environment, CancellationToken.None);
            Require(ready is not null
                    && Read<int>(ready!, "EligibleRounds") == 2
                    && Read<bool>(ready!, "CanPromote")
                    && Read<object>(ready!, "Status").ToString() == "ReadyToPromote",
                "Two latest compatible winning rounds must be reported as ready without promoting automatically.");

            var profilesAfterRead = await profiles.LoadAsync();
            Require(profilesAfterRead.Single(profile => profile.Kind == ProfileKind.Recommended).Id == incumbent.Id,
                "Read-only challenge progress must never replace the incumbent winner.");
            var eventsAfterRead = await history.LoadAsync();
            Require(!eventsAfterRead.Any(item => item.Kind == HistoryEventKind.Profile),
                "Read-only challenge progress must not append a profile-promotion History event.");

            var drifted = environment with { WindowsDescription = "Windows 11 changed build" };
            var drift = await InvokeTaskResultAsync(
                getAsync!, service, challenger.Id, ProfileKind.Recommended, drifted, CancellationToken.None);
            Require(drift is not null
                    && !Read<bool>(drift!, "CanPromote")
                    && Read<object>(drift!, "Status").ToString() == "EnvironmentDrift",
                "Current Windows environment drift must be visible in read-only challenge progress.");

            Console.WriteLine("PASS read-only profile challenge progress, 1/2 readiness, no mutation, and drift presentation contract");
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    private static PerformanceABComparison Comparison(
        PerformanceConfigurationSnapshot incumbent,
        PerformanceConfigurationSnapshot challenger,
        DateTimeOffset start)
        => PerformanceABComparison.Create(
            Snapshot("A · winner", incumbent,
                [99d, 100d, 101d, 100d], [10.1d, 10d, 9.9d, 10d], [12.1d, 12d, 11.9d, 12d], start),
            Snapshot("B · custom", challenger,
                [110d, 111d, 112d, 111d], [9d, 8.9d, 8.8d, 8.9d], [10d, 9.9d, 9.8d, 9.9d], start.AddMinutes(1)));

    private static PerformanceEvidenceSnapshot Snapshot(
        string name,
        PerformanceConfigurationSnapshot configuration,
        IReadOnlyList<double> fps,
        IReadOnlyList<double> frameTimes,
        IReadOnlyList<double> latency,
        DateTimeOffset start)
    {
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
