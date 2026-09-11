using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Workloads;

internal static class UniversalPromotedProfileProjectionSelfTests
{
    internal static async Task RunAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "ffpe-universal-promoted-profile-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var fixture = await CreateFixtureAsync(tempRoot);
            ProjectsAlreadyAuthorizedSpecializedPromotion(fixture);
            RejectsUnpromotedOrMismatchedSpecializedResult(fixture);
            RejectsPromotedProfileDrift(fixture);
            RejectsWrongRevalidationOrFabricatedUniversalSource(fixture);

            Console.WriteLine("PASS Track 5 universal promoted profile projection preserves specialized challenge authority without fabricated round context");
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { }
        }
    }

    private static void ProjectsAlreadyAuthorizedSpecializedPromotion(Fixture fixture)
    {
        Require(fixture.Result.Promoted
                && fixture.Result.Status == ProfileChallengeStatus.Promoted
                && fixture.Result.PromotedProfileId == fixture.Promoted.Id,
            "Fixture must use a real successful ProfileChallengeService promotion.");
        Require(fixture.Revalidation.Baseline.UniversalContext is null
                && fixture.Revalidation.Candidate.UniversalContext is null,
            "Real challenge-round compatibility path must remain without fabricated UniversalContext.");

        var projection = BlueStacksUniversalProfileChallengeBridge.TryProjectPromotion(
            fixture.Result,
            fixture.Promoted,
            fixture.ChallengerProjection,
            fixture.Revalidation,
            fixture.CandidateSpace);

        Require(projection is not null,
            "An already-authorized specialized promotion must be able to retain the proven Custom profile universal provenance read-only.");
        Require(ReferenceEquals(projection!.SpecializedResult, fixture.Result),
            "Universal promotion projection must retain the exact specialized ProfileChallengeResult.");
        Require(ReferenceEquals(projection.PromotedProfile, fixture.Promoted),
            "Universal promotion projection must retain the exact specialized promoted winner profile.");
        Require(ReferenceEquals(projection.ChallengerProfile, fixture.ChallengerProjection),
            "Universal promotion projection must retain the exact already-proven Custom profile provenance.");
        Require(ReferenceEquals(projection.RevalidationRound, fixture.Revalidation),
            "Universal promotion projection must retain the exact specialized revalidation round used as the promoted profile source.");
        Require(ReferenceEquals(projection.UniversalCandidate, fixture.ChallengerProjection.UniversalCandidate),
            "Promotion must retain the exact universal candidate already proven for the Custom profile rather than derive one from challenge-round metadata.");
        Require(ReferenceEquals(projection.Identity, fixture.CandidateSpace.Identity)
                && string.Equals(projection.AdapterId, fixture.CandidateSpace.AdapterId, StringComparison.Ordinal),
            "Promotion projection must retain the exact stable workload identity and adapter from the proven candidate space.");
    }

    private static void RejectsUnpromotedOrMismatchedSpecializedResult(Fixture fixture)
    {
        Require(BlueStacksUniversalProfileChallengeBridge.TryProjectPromotion(
                    fixture.Result with { Promoted = false },
                    fixture.Promoted,
                    fixture.ChallengerProjection,
                    fixture.Revalidation,
                    fixture.CandidateSpace) is null,
            "Universal metadata must not turn an unpromoted specialized result into a promotion.");

        Require(BlueStacksUniversalProfileChallengeBridge.TryProjectPromotion(
                    fixture.Result with { Status = ProfileChallengeStatus.IncumbentHeld },
                    fixture.Promoted,
                    fixture.ChallengerProjection,
                    fixture.Revalidation,
                    fixture.CandidateSpace) is null,
            "IncumbentHeld must remain incumbent-held even if universal provenance exists.");

        Require(BlueStacksUniversalProfileChallengeBridge.TryProjectPromotion(
                    fixture.Result with { PromotedProfileId = Guid.NewGuid() },
                    fixture.Promoted,
                    fixture.ChallengerProjection,
                    fixture.Revalidation,
                    fixture.CandidateSpace) is null,
            "ProfileChallengeResult must identify the exact promoted specialized profile.");

        Require(BlueStacksUniversalProfileChallengeBridge.TryProjectPromotion(
                    fixture.Result with { TargetKind = ProfileKind.MaximumFps },
                    fixture.Promoted,
                    fixture.ChallengerProjection,
                    fixture.Revalidation,
                    fixture.CandidateSpace) is null,
            "Universal projection must not change the winner role authorized by the specialized challenge result.");
    }

    private static void RejectsPromotedProfileDrift(Fixture fixture)
    {
        Require(BlueStacksUniversalProfileChallengeBridge.TryProjectPromotion(
                    fixture.Result,
                    fixture.Promoted with { Evidence = EvidenceLevel.Observed },
                    fixture.ChallengerProjection,
                    fixture.Revalidation,
                    fixture.CandidateSpace) is null,
            "Observed winner metadata must never gain validated promotion provenance.");

        Require(BlueStacksUniversalProfileChallengeBridge.TryProjectPromotion(
                    fixture.Result,
                    fixture.Promoted with { Dpi = fixture.Promoted.Dpi + 1 },
                    fixture.ChallengerProjection,
                    fixture.Revalidation,
                    fixture.CandidateSpace) is null,
            "Promoted winner must preserve the exact specialized challenger configuration including DPI.");

        Require(BlueStacksUniversalProfileChallengeBridge.TryProjectPromotion(
                    fixture.Result,
                    fixture.Promoted with { EnvironmentFingerprint = "tampered" },
                    fixture.ChallengerProjection,
                    fixture.Revalidation,
                    fixture.CandidateSpace) is null,
            "Promoted winner must preserve the exact revalidation environment fingerprint.");

        Require(BlueStacksUniversalProfileChallengeBridge.TryProjectPromotion(
                    fixture.Result,
                    fixture.Promoted with { AverageFps = (fixture.Promoted.AverageFps ?? 0) + 1 },
                    fixture.ChallengerProjection,
                    fixture.Revalidation,
                    fixture.CandidateSpace) is null,
            "Promoted FPS must come from the exact specialized revalidation evidence.");

        Require(BlueStacksUniversalProfileChallengeBridge.TryProjectPromotion(
                    fixture.Result,
                    fixture.Promoted with { OnePercentLow = (fixture.Promoted.OnePercentLow ?? 0) + 1 },
                    fixture.ChallengerProjection,
                    fixture.Revalidation,
                    fixture.CandidateSpace) is null,
            "Promoted one-percent-low must come from the exact specialized revalidation evidence.");
    }

    private static void RejectsWrongRevalidationOrFabricatedUniversalSource(Fixture fixture)
    {
        Require(BlueStacksUniversalProfileChallengeBridge.TryProjectPromotion(
                    fixture.Result,
                    fixture.Promoted,
                    fixture.ChallengerProjection,
                    fixture.FirstRound,
                    fixture.CandidateSpace) is null,
            "A different challenge round must not replace the exact revalidation record referenced by the promoted profile.");

        var fabricatedSource = fixture.ChallengerProjection with
        {
            UniversalCandidate = new UniversalTuningCandidate
            {
                Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["workload.fabricated.value"] = "not-proven"
                }
            }
        };
        Require(BlueStacksUniversalProfileChallengeBridge.TryProjectPromotion(
                    fixture.Result,
                    fixture.Promoted,
                    fabricatedSource,
                    fixture.Revalidation,
                    fixture.CandidateSpace) is null,
            "Caller-constructed universal candidate substitution must fail closed.");

        var wrongIdentity = fixture.CandidateSpace.Identity with
        {
            GameId = "garena.free-fire",
            AdapterId = "bluestacks.free-fire",
            LegacyGameKind = GameKind.FreeFire
        };
        var wrongSpace = fixture.CandidateSpace with
        {
            Identity = wrongIdentity,
            AdapterId = wrongIdentity.AdapterId
        };
        Require(BlueStacksUniversalProfileChallengeBridge.TryProjectPromotion(
                    fixture.Result,
                    fixture.Promoted,
                    fixture.ChallengerProjection,
                    fixture.Revalidation,
                    wrongSpace) is null,
            "Cross-workload candidate-space substitution must fail closed.");
    }

    private static async Task<Fixture> CreateFixtureAsync(string root)
    {
        var detectedInstance = new BlueStacksInstance
        {
            Name = "Pie64",
            AndroidVersion = "Pie 64-bit",
            CpuCores = 4,
            RamMb = 4096,
            Renderer = "Vulkan",
            Fps = 120,
            Resolution = "1600x900",
            Dpi = 320
        };
        var challengerInstance = detectedInstance with
        {
            CpuCores = 6,
            RamMb = 6144,
            Fps = 144,
            Resolution = "1920x1080"
        };
        var environment = new EnvironmentSnapshot
        {
            MachineName = "DG-UNIVERSAL-PROMOTION-TEST",
            WindowsDescription = "Windows 11 universal promotion test",
            LogicalProcessors = 16,
            MemoryTotalGb = 32,
            Is64BitOs = true,
            BlueStacksDetected = true,
            Instances = [detectedInstance],
            ActiveGame = GameKind.FreeFireMax
        };
        var challengerEnvironment = environment with { Instances = [challengerInstance] };
        var incumbentConfiguration = PerformanceConfigurationSnapshot.Capture(
            environment,
            detectedInstance,
            GameKind.FreeFireMax)
            ?? throw new InvalidOperationException("Fixture requires incumbent configuration.");
        var challengerConfiguration = PerformanceConfigurationSnapshot.Capture(
            challengerEnvironment,
            challengerInstance,
            GameKind.FreeFireMax)
            ?? throw new InvalidOperationException("Fixture requires challenger configuration.");

        var identity = LegacyGameIdentityBridge.FromGameKind(GameKind.FreeFireMax)
            ?? throw new InvalidOperationException("Fixture requires stable Free Fire MAX identity.");
        var specializedCandidate = new TuningCandidate
        {
            CpuCores = challengerConfiguration.CpuCores,
            RamMb = challengerConfiguration.RamMb,
            Renderer = challengerConfiguration.Renderer,
            FpsTarget = challengerConfiguration.FpsTarget,
            Resolution = challengerConfiguration.Resolution
        };
        var universalCandidate = new UniversalTuningCandidate
        {
            Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [$"workload.{identity.AdapterId}.cpu-cores"] = specializedCandidate.CpuCores.ToString(),
                [$"workload.{identity.AdapterId}.ram-mb"] = specializedCandidate.RamMb.ToString(),
                [$"workload.{identity.AdapterId}.renderer"] = specializedCandidate.Renderer,
                [$"workload.{identity.AdapterId}.fps-target"] = specializedCandidate.FpsTarget.ToString(),
                [$"workload.{identity.AdapterId}.resolution"] = specializedCandidate.Resolution
            }
        };
        var candidateSpace = new BlueStacksUniversalTuningCandidateSpace
        {
            Identity = identity,
            AdapterId = identity.AdapterId,
            Bindings =
            [
                new BlueStacksUniversalTuningCandidateBinding
                {
                    SpecializedCandidate = specializedCandidate,
                    UniversalCandidate = universalCandidate
                }
            ]
        };
        var universalContext = new PerformanceUniversalConfigurationContext
        {
            SchemaVersion = 1,
            GameId = identity.GameId,
            AdapterId = identity.AdapterId,
            Machine = new MachineEnvironmentFingerprintV2
            {
                SchemaVersion = 2,
                Id = "universal-promotion-machine-v2",
                MachineName = environment.MachineName,
                WindowsDescription = environment.WindowsDescription,
                LogicalProcessors = environment.LogicalProcessors,
                MemoryTotalMb = (long)Math.Round(environment.MemoryTotalGb.GetValueOrDefault() * 1024d),
                Is64BitOs = environment.Is64BitOs,
                HardwareSignatures = ["CPU|PROMOTION-TEST", "GPU|PROMOTION-TEST"]
            },
            CapabilityValues = Array.Empty<PerformanceCapabilityValueSnapshot>(),
            WorkloadConfiguration = new Dictionary<string, string>(),
            DisplayDriverContext = new Dictionary<string, string>()
        }.Rehydrate();

        var sourceStart = DateTimeOffset.UtcNow.AddMinutes(-5);
        var sourceRecord = new PerformanceComparisonHistoryRecord
        {
            Label = "Universal Custom source",
            SavedAt = sourceStart.AddMinutes(1),
            Baseline = Snapshot("A source", challengerConfiguration, 108, 9.2, 10.2, sourceStart, universalContext),
            Candidate = Snapshot("B source", challengerConfiguration, 112, 8.8, 9.8, sourceStart.AddMinutes(1), universalContext),
            ValidationStatus = PerformanceComparisonValidationStatus.Validated,
            ValidationEvidence = Snapshot("Validation source", challengerConfiguration, 113, 8.7, 9.7, sourceStart.AddMinutes(2), universalContext),
            ValidatedAt = sourceStart.AddMinutes(2)
        };
        Require(sourceRecord.CanOriginateProfile,
            "Fixture source record must satisfy the existing specialized profile-origin authority.");

        var profiles = new ProfileService(Path.Combine(root, "profiles.json"));
        var challenger = await profiles.CreateCustomFromValidatedComparisonAsync(
            sourceRecord,
            challengerEnvironment,
            "Custom Validated universal challenger");
        var incumbent = new PerformanceProfile
        {
            Name = "Recomendado",
            Kind = ProfileKind.Recommended,
            Game = GameKind.FreeFireMax,
            InstanceName = detectedInstance.Name,
            CpuCores = incumbentConfiguration.CpuCores,
            RamMb = incumbentConfiguration.RamMb,
            Renderer = incumbentConfiguration.Renderer,
            FpsTarget = incumbentConfiguration.FpsTarget,
            Resolution = incumbentConfiguration.Resolution,
            Dpi = incumbentConfiguration.Dpi,
            Evidence = EvidenceLevel.Validated,
            Confidence = 0.95,
            AverageFps = 100,
            OnePercentLow = 98,
            FrameTimeMs = 10,
            LatencyMs = 12,
            CreatedAt = challenger.CreatedAt.AddSeconds(-1)
        };
        await profiles.SaveAsync([incumbent, challenger]);

        var sourceValidation = BlueStacksUniversalValidatedPerformanceBridge.TryProject(sourceRecord, candidateSpace)
            ?? throw new InvalidOperationException("Fixture requires upstream universal validated-History projection.");
        var challengerProjection = BlueStacksUniversalValidatedProfileBridge.TryProject(
            challenger,
            sourceValidation,
            candidateSpace)
            ?? throw new InvalidOperationException("Fixture requires upstream universal validated-profile projection.");

        var history = new HistoryService(Path.Combine(root, "history.json"));
        var challengeStart = challenger.CreatedAt.AddMinutes(1);
        var firstRound = await history.SavePerformanceComparisonAsync(
            "Challenge round 1",
            PerformanceABComparison.Create(
                Snapshot("A · incumbent 1", incumbentConfiguration, 100, 10, 12, challengeStart),
                Snapshot("B · challenger 1", challengerConfiguration, 112, 8.8, 9.8, challengeStart.AddMinutes(1))));
        var revalidation = await history.SavePerformanceComparisonAsync(
            "Challenge round 2 · revalidation",
            PerformanceABComparison.Create(
                Snapshot("A · incumbent 2", incumbentConfiguration, 101, 9.9, 12, challengeStart.AddMinutes(10)),
                Snapshot("B · challenger 2", challengerConfiguration, 114, 8.6, 9.6, challengeStart.AddMinutes(11))));

        Require(firstRound.Baseline.UniversalContext is null
                && firstRound.Candidate.UniversalContext is null
                && revalidation.Baseline.UniversalContext is null
                && revalidation.Candidate.UniversalContext is null,
            "Fixture challenge rounds must model the real specialized path with absent universal context.");

        var service = new ProfileChallengeService(profiles, history);
        var result = await service.AssessAndPromoteLatestAsync(
            challenger.Id,
            ProfileKind.Recommended,
            environment);
        Require(result.Promoted && result.Status == ProfileChallengeStatus.Promoted,
            "Fixture requires real specialized two-round promotion authority.");

        var reloaded = await profiles.LoadAsync();
        var promoted = reloaded.Single(profile => profile.Kind == ProfileKind.Recommended);
        Require(promoted.SourceComparisonId == revalidation.Id,
            "Fixture promoted profile must point to the exact second challenge round.");

        return new Fixture(
            candidateSpace,
            result,
            promoted,
            challengerProjection,
            firstRound,
            revalidation);
    }

    private static PerformanceEvidenceSnapshot Snapshot(
        string name,
        PerformanceConfigurationSnapshot configuration,
        double fps,
        double frameTimeMs,
        double latencyMs,
        DateTimeOffset start,
        PerformanceUniversalConfigurationContext? universalContext = null)
    {
        var points = new[]
        {
            new PerformanceTimelinePoint
            {
                Timestamp = start,
                Fps = fps - 1,
                FrameTimeMs = frameTimeMs + 0.1,
                LatencyMs = latencyMs + 0.1,
                DataQuality = "Measured"
            },
            new PerformanceTimelinePoint
            {
                Timestamp = start.AddSeconds(1),
                Fps = fps,
                FrameTimeMs = frameTimeMs,
                LatencyMs = latencyMs,
                DataQuality = "Measured"
            },
            new PerformanceTimelinePoint
            {
                Timestamp = start.AddSeconds(2),
                Fps = fps + 1,
                FrameTimeMs = frameTimeMs - 0.1,
                LatencyMs = latencyMs - 0.1,
                DataQuality = "Measured"
            }
        };
        var interval = new PerformanceIntervalSummary
        {
            Start = start,
            End = start.AddSeconds(2),
            TelemetrySamples = points.Length,
            FpsEvidenceSamples = points.Length,
            AverageFps = points.Average(point => point.Fps!.Value),
            AverageFrameTimeMs = points.Average(point => point.FrameTimeMs!.Value),
            Points = points
        };
        return universalContext is null
            ? PerformanceEvidenceSnapshot.Capture(name, interval, start.AddSeconds(3), configuration)
            : PerformanceEvidenceSnapshot.Capture(name, interval, start.AddSeconds(3), configuration, universalContext);
    }

    private sealed record Fixture(
        BlueStacksUniversalTuningCandidateSpace CandidateSpace,
        ProfileChallengeResult Result,
        PerformanceProfile Promoted,
        UniversalValidatedProfileProjection ChallengerProjection,
        PerformanceComparisonHistoryRecord FirstRound,
        PerformanceComparisonHistoryRecord Revalidation);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
