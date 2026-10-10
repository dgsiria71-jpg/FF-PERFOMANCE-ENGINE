using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class ProfileChallengeFreshnessSelfTests
{
    public static async Task RunAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "ffpe-profile-challenge-freshness-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            await OldEvidenceMustNotCountAsync(Path.Combine(tempRoot, "old-evidence"));
            await WindowsBuildDriftMustBlockAsync(Path.Combine(tempRoot, "windows-drift"));
            Console.WriteLine("PASS profile challenge freshness and Windows environment drift gates");
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { }
        }
    }

    private static async Task OldEvidenceMustNotCountAsync(string root)
    {
        Directory.CreateDirectory(root);
        var setup = CreateSetup(root, new DateTimeOffset(2026, 9, 6, 10, 0, 0, TimeSpan.Zero));

        await setup.History.SavePerformanceComparisonAsync(
            "Old matching evidence",
            Comparison(setup.IncumbentConfiguration, setup.ChallengerConfiguration,
                new DateTimeOffset(2026, 9, 6, 9, 30, 0, TimeSpan.Zero)));
        await setup.History.SavePerformanceComparisonAsync(
            "Fresh challenge round 1",
            Comparison(setup.IncumbentConfiguration, setup.ChallengerConfiguration,
                new DateTimeOffset(2026, 9, 6, 10, 10, 0, TimeSpan.Zero)));

        var result = await setup.Service.AssessAndPromoteLatestAsync(
            setup.Challenger.Id,
            ProfileKind.Recommended,
            setup.Environment);
        Require(!result.Promoted
                && result.Status == ProfileChallengeStatus.InsufficientEvidence
                && result.EvidenceRounds == 1,
            "A/B evidence captured before the Custom profile existed must not count toward its two-round winner challenge.");

        var profiles = await setup.Profiles.LoadAsync();
        Require(profiles.Single(profile => profile.Kind == ProfileKind.Recommended).Id == setup.Incumbent.Id,
            "Stale pre-profile evidence must never replace the incumbent winner.");
    }

    private static async Task WindowsBuildDriftMustBlockAsync(string root)
    {
        Directory.CreateDirectory(root);
        var setup = CreateSetup(root, new DateTimeOffset(2026, 9, 6, 10, 0, 0, TimeSpan.Zero));

        await setup.History.SavePerformanceComparisonAsync(
            "Fresh challenge round 1",
            Comparison(setup.IncumbentConfiguration, setup.ChallengerConfiguration,
                new DateTimeOffset(2026, 9, 6, 10, 10, 0, TimeSpan.Zero)));
        await setup.History.SavePerformanceComparisonAsync(
            "Fresh challenge round 2",
            Comparison(setup.IncumbentConfiguration, setup.ChallengerConfiguration,
                new DateTimeOffset(2026, 9, 6, 10, 20, 0, TimeSpan.Zero)));

        var drifted = setup.Environment with { WindowsDescription = "Windows 11 changed build" };
        var result = await setup.Service.AssessAndPromoteLatestAsync(
            setup.Challenger.Id,
            ProfileKind.Recommended,
            drifted);
        Require(!result.Promoted && result.Status == ProfileChallengeStatus.EnvironmentDrift,
            "A Windows environment/build change must invalidate historical winner-promotion evidence until the challenge is re-run.");

        var profiles = await setup.Profiles.LoadAsync();
        Require(profiles.Single(profile => profile.Kind == ProfileKind.Recommended).Id == setup.Incumbent.Id,
            "Windows drift must preserve the incumbent winner.");
    }

    private static ChallengeSetup CreateSetup(string root, DateTimeOffset challengerCreatedAt)
    {
        var environment = new EnvironmentSnapshot
        {
            MachineName = "FFPE-FRESHNESS-PC",
            WindowsDescription = "Windows 11 stable build",
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
        var incumbentConfiguration = PerformanceConfigurationSnapshot.Capture(
            environment,
            incumbentInstance,
            GameKind.FreeFireMax)!;
        var challengerConfiguration = PerformanceConfigurationSnapshot.Capture(
            environment with { Instances = [challengerInstance] },
            challengerInstance,
            GameKind.FreeFireMax)!;

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
            CreatedAt = challengerCreatedAt.AddHours(-1)
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
            CreatedAt = challengerCreatedAt
        };

        var profiles = new ProfileService(Path.Combine(root, "profiles.json"));
        profiles.SaveAsync([incumbent, challenger]).GetAwaiter().GetResult();
        var history = new HistoryService(Path.Combine(root, "history.json"));
        return new ChallengeSetup(
            environment,
            incumbent,
            challenger,
            incumbentConfiguration,
            challengerConfiguration,
            profiles,
            history,
            new ProfileChallengeService(profiles, history));
    }

    private static PerformanceABComparison Comparison(
        PerformanceConfigurationSnapshot incumbent,
        PerformanceConfigurationSnapshot challenger,
        DateTimeOffset start)
        => PerformanceABComparison.Create(
            Snapshot("A", incumbent, new[] { 99d, 100d, 101d, 100d }, new[] { 10.1d, 10d, 9.9d, 10d }, new[] { 12.1d, 12d, 11.9d, 12d }, start),
            Snapshot("B", challenger, new[] { 110d, 111d, 112d, 111d }, new[] { 9d, 8.9d, 8.8d, 8.9d }, new[] { 10d, 9.9d, 9.8d, 9.9d }, start.AddMinutes(1)));

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

    private sealed record ChallengeSetup(
        EnvironmentSnapshot Environment,
        PerformanceProfile Incumbent,
        PerformanceProfile Challenger,
        PerformanceConfigurationSnapshot IncumbentConfiguration,
        PerformanceConfigurationSnapshot ChallengerConfiguration,
        ProfileService Profiles,
        HistoryService History,
        ProfileChallengeService Service);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
