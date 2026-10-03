using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class ProfileChallengeIncumbentFreshnessSelfTests
{
    public static async Task RunAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "ffpe-incumbent-freshness-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var environment = new EnvironmentSnapshot
            {
                MachineName = "FFPE-INCUMBENT-PC",
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
            var incumbentConfiguration = PerformanceConfigurationSnapshot.Capture(environment, incumbentInstance, GameKind.FreeFireMax)!;
            var challengerConfiguration = PerformanceConfigurationSnapshot.Capture(
                environment with { Instances = [challengerInstance] }, challengerInstance, GameKind.FreeFireMax)!;

            var customCreatedAt = new DateTimeOffset(2026, 9, 6, 10, 0, 0, TimeSpan.Zero);
            var incumbentCreatedAt = new DateTimeOffset(2026, 9, 6, 10, 15, 0, TimeSpan.Zero);
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
                CreatedAt = incumbentCreatedAt
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
                CreatedAt = customCreatedAt
            };

            var profiles = new ProfileService(Path.Combine(tempRoot, "profiles.json"));
            var history = new HistoryService(Path.Combine(tempRoot, "history.json"));
            await profiles.SaveAsync([incumbent, challenger]);

            // This round is fresh relative to the Custom, but stale relative to the current incumbent.
            await history.SavePerformanceComparisonAsync(
                "Round against previous incumbent",
                Comparison(incumbentConfiguration, challengerConfiguration,
                    new DateTimeOffset(2026, 9, 6, 10, 10, 0, TimeSpan.Zero)));
            // Only this round was captured after the current incumbent existed.
            await history.SavePerformanceComparisonAsync(
                "Round against current incumbent",
                Comparison(incumbentConfiguration, challengerConfiguration,
                    new DateTimeOffset(2026, 9, 6, 10, 20, 0, TimeSpan.Zero)));

            var service = new ProfileChallengeService(profiles, history);
            var result = await service.AssessAndPromoteLatestAsync(
                challenger.Id,
                ProfileKind.Recommended,
                environment);

            Require(!result.Promoted
                    && result.Status == ProfileChallengeStatus.InsufficientEvidence
                    && result.EvidenceRounds == 1,
                "Challenge evidence must be newer than both the Custom and the current incumbent; stale rounds against an earlier incumbent cannot be replayed.");
            var reloaded = await profiles.LoadAsync();
            Require(reloaded.Single(profile => profile.Kind == ProfileKind.Recommended).Id == incumbent.Id,
                "The current incumbent must remain unchanged when only one fresh post-incumbent round exists.");

            Console.WriteLine("PASS profile challenge requires evidence newer than the current incumbent");
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { }
        }
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

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
