using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Workloads;

internal static class PersistedPromotedProfileProvenanceSelfTests
{
    internal static async Task RunAsync()
    {
        var tempRoot = Path.Combine(
            Path.GetTempPath(),
            "ffpe-persisted-promoted-profile-provenance-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var fixture = await CreateFixtureAsync(tempRoot);
            var profiles = new ProfileService(fixture.ProfilesPath);
            var history = new HistoryService(fixture.HistoryPath);
            var customResolver = new UniversalValidatedProfileProvenanceService(
                profiles,
                history,
                fixture.CandidateBridge);
            var service = new UniversalPersistedPromotedProfileProvenanceService(
                profiles,
                history,
                customResolver);

            var projection = await service.ResolveCurrentAsync(
                fixture.Promoted.Id,
                fixture.Environment,
                fixture.Instance,
                fixture.CapturedSettings);

            Require(projection is not null,
                "A winner already promoted by ProfileChallengeService must retain universal provenance after fresh profile/history services simulate application restart.");
            Require(projection!.PromotedProfile.Id == fixture.Promoted.Id
                    && projection.PromotedProfile.Kind == ProfileKind.Recommended
                    && projection.PromotedProfile.Evidence == EvidenceLevel.Validated,
                "Persisted promotion provenance must retain the exact current specialized winner profile and never manufacture a winner role.");
            Require(projection.PromotionReceipt.Event.Id == fixture.PromotionEvent.Id
                    && projection.PromotionReceipt.ChallengerProfileId == fixture.Custom.Id
                    && projection.PromotionReceipt.PreviousWinnerId == fixture.Incumbent.Id
                    && projection.PromotionReceipt.PromotedProfileId == fixture.Promoted.Id
                    && projection.PromotionReceipt.RevalidationComparisonId == fixture.Revalidation.Id
                    && projection.PromotionReceipt.TargetKind == ProfileKind.Recommended,
                "The universal resolver must bind to the exact durable specialized promotion receipt rather than reconstruct an unpersisted ProfileChallengeResult.");
            Require(projection.ChallengerProfile.SpecializedProfile.Id == fixture.Custom.Id
                    && projection.ChallengerProfile.SpecializedProfile.Kind == ProfileKind.Custom
                    && projection.ChallengerProfile.SpecializedProfile.Evidence == EvidenceLevel.Validated,
                "Persisted winner provenance must pass through the current re-proven Custom source, not infer universal provenance from winner metadata alone.");
            Require(projection.RevalidationRound.Id == fixture.Revalidation.Id
                    && ReferenceEquals(projection.UniversalCandidate, projection.ChallengerProfile.UniversalCandidate)
                    && string.Equals(projection.Identity.GameId, fixture.Identity.GameId, StringComparison.Ordinal)
                    && string.Equals(projection.AdapterId, fixture.Identity.AdapterId, StringComparison.Ordinal),
                "Persisted winner provenance must retain the exact revalidation id and exact currently proven universal candidate/identity/adapter of its Custom source.");

            var missingCapability = await service.ResolveCurrentAsync(
                fixture.Promoted.Id,
                fixture.Environment,
                fixture.Instance,
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
            Require(missingCapability is null,
                "A durable promotion receipt must not bypass missing current candidate capability; universal provenance must fail closed while specialized winner history remains valid.");

            var currentProfiles = await profiles.LoadAsync();
            var tamperedWinner = fixture.Promoted with
            {
                AverageFps = (fixture.Promoted.AverageFps ?? 0) + 1
            };
            await profiles.SaveAsync(currentProfiles
                .Where(profile => profile.Id != fixture.Promoted.Id)
                .Append(tamperedWinner));
            Require(await service.ResolveCurrentAsync(
                        fixture.Promoted.Id,
                        fixture.Environment,
                        fixture.Instance,
                        fixture.CapturedSettings) is null,
                "Winner metrics drifting from the exact persisted revalidation candidate must invalidate universal promotion provenance.");
            await profiles.SaveAsync(currentProfiles);

            var phantom = fixture.Promoted with
            {
                Id = Guid.NewGuid(),
                Name = "Phantom promoted winner"
            };
            await profiles.SaveAsync(currentProfiles.Append(phantom));
            Require(await service.ResolveCurrentAsync(
                        phantom.Id,
                        fixture.Environment,
                        fixture.Instance,
                        fixture.CapturedSettings) is null,
                "A validated generated-role profile with matching values but no exact durable promotion receipt must receive no persisted universal promotion provenance.");
            await profiles.SaveAsync(currentProfiles);

            await history.AppendAsync(new HistoryEvent
            {
                Kind = HistoryEventKind.Profile,
                Title = "Duplicate promotion receipt fixture",
                Summary = "Duplicate durable receipt must fail closed.",
                DetailsJson = fixture.PromotionEvent.DetailsJson
            });
            Require(await service.ResolveCurrentAsync(
                        fixture.Promoted.Id,
                        fixture.Environment,
                        fixture.Instance,
                        fixture.CapturedSettings) is null,
                "Multiple durable promotion receipts claiming the same promoted profile must be treated as ambiguous rather than choosing one arbitrarily.");

            Console.WriteLine(
                "PASS Track 5 persisted promoted winner provenance survives restart via exact durable specialized receipt without reconstructing ProfileChallengeResult");
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { }
        }
    }

    private static async Task<Fixture> CreateFixtureAsync(string root)
    {
        var profilesPath = Path.Combine(root, "profiles.json");
        var historyPath = Path.Combine(root, "history.json");
        var profiles = new ProfileService(profilesPath);
        var history = new HistoryService(historyPath);

        var instance = new BlueStacksInstance
        {
            Name = "Pie64",
            AndroidVersion = "Pie 64-bit",
            CpuCores = 2,
            RamMb = 4096,
            Renderer = "Vulkan",
            Fps = 90,
            Resolution = "1920x1080",
            Dpi = 320
        };
        var environment = new EnvironmentSnapshot
        {
            MachineName = "DG-PERSISTED-PROMOTION-TEST",
            WindowsDescription = "Windows 11 persisted promotion test",
            LogicalProcessors = 8,
            MemoryTotalGb = 16,
            Is64BitOs = true,
            BlueStacksDetected = true,
            Instances = [instance],
            ActiveGame = GameKind.FreeFire
        };
        var capturedSettings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["bst.instance.Pie64.cpus"] = "2",
            ["bst.instance.Pie64.ram"] = "4096",
            ["bst.instance.Pie64.graphics_renderer"] = "Vulkan",
            ["bst.instance.Pie64.fps"] = "90",
            ["bst.instance.Pie64.display_width"] = "1920",
            ["bst.instance.Pie64.display_height"] = "1080"
        };

        var identity = LegacyGameIdentityBridge.FromGameKind(GameKind.FreeFire)
            ?? throw new InvalidOperationException("Fixture requires the stable Free Fire GameIdentity bridge.");
        var adapters = new GameAdapterResolver(
        [
            new GenericGameAdapter(),
            BlueStacksFreeFireGameAdapter.For(GameKind.FreeFire),
            BlueStacksFreeFireGameAdapter.For(GameKind.FreeFireMax)
        ]);
        var candidateBridge = new BlueStacksUniversalTuningCandidateBridge(
            new AutoTunerEngine(),
            adapters);

        var customConfiguration = PerformanceConfigurationSnapshot.Capture(
            environment,
            instance,
            GameKind.FreeFire)
            ?? throw new InvalidOperationException("Fixture requires the Custom configuration.");
        var universalContext = new PerformanceUniversalConfigurationContext
        {
            SchemaVersion = 1,
            GameId = identity.GameId,
            AdapterId = identity.AdapterId,
            Machine = new MachineEnvironmentFingerprintV2
            {
                SchemaVersion = 2,
                Id = "persisted-promotion-machine-v2",
                MachineName = environment.MachineName,
                WindowsDescription = environment.WindowsDescription,
                LogicalProcessors = environment.LogicalProcessors,
                MemoryTotalMb = (long)Math.Round(environment.MemoryTotalGb.GetValueOrDefault() * 1024d),
                Is64BitOs = environment.Is64BitOs,
                HardwareSignatures = ["CPU|PERSISTED-PROMOTION", "GPU|PERSISTED-PROMOTION"]
            },
            CapabilityValues = Array.Empty<PerformanceCapabilityValueSnapshot>(),
            WorkloadConfiguration = new Dictionary<string, string>(),
            DisplayDriverContext = new Dictionary<string, string>()
        }.Rehydrate();

        var sourceStart = DateTimeOffset.UtcNow.AddMinutes(-10);
        var observed = await history.SavePerformanceComparisonAsync(
            "Persisted Custom source",
            PerformanceABComparison.Create(
                Snapshot("A · source", customConfiguration, 100, 10, 11, sourceStart, universalContext),
                Snapshot("B · source", customConfiguration, 120, 8.3, 8.5, sourceStart.AddMinutes(1), universalContext)));
        await history.RequestPerformanceValidationAsync(observed.Id);
        var validatedSource = await history.CompletePerformanceValidationAsync(
            observed.Id,
            Snapshot("Validation · source", customConfiguration, 123, 8.1, 8.2, sourceStart.AddMinutes(2), universalContext));
        var custom = await profiles.CreateCustomFromValidatedComparisonAsync(
            validatedSource,
            environment,
            "Persisted universal Custom challenger");

        var incumbentInstance = instance with
        {
            CpuCores = 1,
            RamMb = 2048,
            Fps = 60,
            Resolution = "1280x720"
        };
        var incumbentEnvironment = environment with { Instances = [incumbentInstance] };
        var incumbentConfiguration = PerformanceConfigurationSnapshot.Capture(
            incumbentEnvironment,
            incumbentInstance,
            GameKind.FreeFire)
            ?? throw new InvalidOperationException("Fixture requires incumbent configuration.");
        var incumbent = new PerformanceProfile
        {
            Name = "Recomendado",
            Kind = ProfileKind.Recommended,
            Game = GameKind.FreeFire,
            InstanceName = incumbentInstance.Name,
            CpuCores = incumbentConfiguration.CpuCores,
            RamMb = incumbentConfiguration.RamMb,
            Renderer = incumbentConfiguration.Renderer,
            FpsTarget = incumbentConfiguration.FpsTarget,
            Resolution = incumbentConfiguration.Resolution,
            Dpi = incumbentConfiguration.Dpi,
            Evidence = EvidenceLevel.Validated,
            Confidence = 0.90,
            AverageFps = 80,
            OnePercentLow = 78,
            FrameTimeMs = 12.5,
            LatencyMs = 15,
            CreatedAt = custom.CreatedAt.AddSeconds(-1)
        };
        await profiles.SaveAsync([incumbent, custom]);

        var challengeStart = custom.CreatedAt.AddMinutes(1);
        await history.SavePerformanceComparisonAsync(
            "Persisted challenge round 1",
            PerformanceABComparison.Create(
                Snapshot("A · incumbent 1", incumbentConfiguration, 80, 12.5, 15, challengeStart),
                Snapshot("B · challenger 1", customConfiguration, 120, 8.3, 9, challengeStart.AddMinutes(1))));
        var revalidation = await history.SavePerformanceComparisonAsync(
            "Persisted challenge round 2 · revalidation",
            PerformanceABComparison.Create(
                Snapshot("A · incumbent 2", incumbentConfiguration, 81, 12.3, 14.8, challengeStart.AddMinutes(10)),
                Snapshot("B · challenger 2", customConfiguration, 124, 8.0, 8.8, challengeStart.AddMinutes(11))));

        var result = await new ProfileChallengeService(profiles, history)
            .AssessAndPromoteLatestAsync(
                custom.Id,
                ProfileKind.Recommended,
                environment);
        Require(result.Promoted && result.Status == ProfileChallengeStatus.Promoted,
            "Fixture requires a real specialized promotion before restart simulation.");

        var reloadedProfiles = await profiles.LoadAsync();
        var promoted = reloadedProfiles.Single(profile => profile.Kind == ProfileKind.Recommended);
        Require(result.PromotedProfileId == promoted.Id
                && promoted.SourceComparisonId == revalidation.Id,
            "Fixture promoted winner must be the exact specialized profile sourced from the second measured challenge round.");

        var promotionEvents = (await history.LoadAsync())
            .Where(item => item.Kind == HistoryEventKind.Profile
                           && item.DetailsJson?.Contains(promoted.Id.ToString("D"), StringComparison.OrdinalIgnoreCase) == true)
            .ToArray();
        Require(promotionEvents.Length == 1,
            "Fixture requires exactly one durable specialized promotion event for the promoted profile.");

        return new Fixture(
            profilesPath,
            historyPath,
            environment,
            instance,
            capturedSettings,
            identity,
            candidateBridge,
            incumbent,
            custom,
            promoted,
            revalidation,
            promotionEvents[0]);
    }

    private static PerformanceEvidenceSnapshot Snapshot(
        string name,
        PerformanceConfigurationSnapshot configuration,
        double fps,
        double frameTimeMs,
        double latencyMs,
        DateTimeOffset start,
        PerformanceUniversalConfigurationContext? universalContext = null)
        => PerformanceEvidenceSnapshot.Capture(
            name,
            new PerformanceIntervalSummary
            {
                Start = start,
                End = start.AddSeconds(1),
                TelemetrySamples = 2,
                FpsEvidenceSamples = 2,
                AverageFps = fps,
                AverageFrameTimeMs = frameTimeMs,
                Points =
                [
                    new PerformanceTimelinePoint
                    {
                        Timestamp = start,
                        Fps = fps,
                        FrameTimeMs = frameTimeMs,
                        LatencyMs = latencyMs,
                        DataQuality = "Measured"
                    },
                    new PerformanceTimelinePoint
                    {
                        Timestamp = start.AddSeconds(1),
                        Fps = fps,
                        FrameTimeMs = frameTimeMs,
                        LatencyMs = latencyMs,
                        DataQuality = "Measured"
                    }
                ]
            },
            start.AddSeconds(2),
            configuration,
            universalContext);

    private sealed record Fixture(
        string ProfilesPath,
        string HistoryPath,
        EnvironmentSnapshot Environment,
        BlueStacksInstance Instance,
        IReadOnlyDictionary<string, string> CapturedSettings,
        GameIdentity Identity,
        BlueStacksUniversalTuningCandidateBridge CandidateBridge,
        PerformanceProfile Incumbent,
        PerformanceProfile Custom,
        PerformanceProfile Promoted,
        PerformanceComparisonHistoryRecord Revalidation,
        HistoryEvent PromotionEvent);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
