using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Workloads;

internal static class UniversalValidatedProfileProjectionSelfTests
{
    internal static async Task RunAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "ffpe-universal-profile-projection-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var fixture = CreateFixture();
            var profiles = new ProfileService(Path.Combine(tempRoot, "profiles.json"));
            var profile = await profiles.CreateCustomFromValidatedComparisonAsync(
                fixture.Record,
                fixture.Environment,
                "Validated universal Custom");

            ProjectsExactSpecializedProfileOrigin(fixture, profile);
            RejectsProfilesWithoutExactSpecializedOriginAuthority(fixture, profile);
            RejectsProfileConfigurationFingerprintAndMetricDrift(fixture, profile);
            RejectsFabricatedUniversalValidationProjection(fixture, profile);

            Console.WriteLine("PASS Track 5 universal validated profile projection preserves specialized profile-origin authority");
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { }
        }
    }

    private static void ProjectsExactSpecializedProfileOrigin(Fixture fixture, PerformanceProfile profile)
    {
        Require(profile.Kind == ProfileKind.Custom
                && profile.Evidence == EvidenceLevel.Validated
                && profile.SourceComparisonId == fixture.Record.Id,
            "Fixture must use the real specialized ProfileService explicit validated-Custom origin path.");

        var validationProjection = BlueStacksUniversalValidatedPerformanceBridge.TryProject(
            fixture.Record,
            fixture.CandidateSpace);
        Require(validationProjection is not null,
            "Fixture requires a valid already-authorized universal validation projection.");

        var projection = BlueStacksUniversalValidatedProfileBridge.TryProject(
            profile,
            validationProjection!,
            fixture.CandidateSpace);

        Require(projection is not null,
            "A Custom Validated profile explicitly originated from the same universally correlated validation must receive a read-only universal profile projection.");
        Require(ReferenceEquals(projection!.SpecializedProfile, profile),
            "Universal profile projection must retain the exact specialized persisted profile object instead of recreating a generic profile authority.");
        Require(ReferenceEquals(projection.SourceValidation, validationProjection),
            "Universal profile projection must retain the exact upstream universal validation projection as provenance.");
        Require(ReferenceEquals(projection.UniversalCandidate, validationProjection!.UniversalCandidate),
            "Universal profile projection must retain the exact Slice 3 UniversalTuningCandidate reference already proven by validation projection.");
        Require(ReferenceEquals(projection.Identity, fixture.CandidateSpace.Identity)
                && string.Equals(projection.AdapterId, fixture.CandidateSpace.AdapterId, StringComparison.Ordinal),
            "Universal profile projection must retain the exact stable workload identity and resolved adapter from the proven candidate space.");
    }

    private static void RejectsProfilesWithoutExactSpecializedOriginAuthority(
        Fixture fixture,
        PerformanceProfile profile)
    {
        var source = RequireValidationProjection(fixture);

        Require(BlueStacksUniversalValidatedProfileBridge.TryProject(
                    profile with { Kind = ProfileKind.Recommended },
                    source,
                    fixture.CandidateSpace) is null,
            "A generated winner profile must not masquerade as a Custom profile originated by the explicit validated-comparison path.");

        Require(BlueStacksUniversalValidatedProfileBridge.TryProject(
                    profile with { Evidence = EvidenceLevel.Observed },
                    source,
                    fixture.CandidateSpace) is null,
            "Observed profile metadata must never gain validated universal profile provenance.");

        Require(BlueStacksUniversalValidatedProfileBridge.TryProject(
                    profile with { SourceComparisonId = Guid.NewGuid() },
                    source,
                    fixture.CandidateSpace) is null,
            "Profile SourceComparisonId must point to the exact specialized History record behind the validation projection.");

        Require(BlueStacksUniversalValidatedProfileBridge.TryProject(
                    profile with { SourceComparisonId = null },
                    source,
                    fixture.CandidateSpace) is null,
            "A profile without auditable specialized source comparison must not receive universal validated-profile provenance.");
    }

    private static void RejectsProfileConfigurationFingerprintAndMetricDrift(
        Fixture fixture,
        PerformanceProfile profile)
    {
        var source = RequireValidationProjection(fixture);

        Require(BlueStacksUniversalValidatedProfileBridge.TryProject(
                    profile with { Dpi = profile.Dpi + 1 },
                    source,
                    fixture.CandidateSpace) is null,
            "Universal profile provenance must preserve the full specialized configuration, including DPI not represented by the universal candidate dimensions.");

        Require(BlueStacksUniversalValidatedProfileBridge.TryProject(
                    profile with { InstanceName = profile.InstanceName + "-other" },
                    source,
                    fixture.CandidateSpace) is null,
            "Universal profile provenance must preserve the exact specialized BlueStacks instance binding.");

        Require(BlueStacksUniversalValidatedProfileBridge.TryProject(
                    profile with { EnvironmentFingerprint = "tampered-fingerprint" },
                    source,
                    fixture.CandidateSpace) is null,
            "Universal profile provenance must preserve the exact specialized environment fingerprint copied by ProfileService.");

        Require(BlueStacksUniversalValidatedProfileBridge.TryProject(
                    profile with { AverageFps = (profile.AverageFps ?? 0) + 1 },
                    source,
                    fixture.CandidateSpace) is null,
            "A profile whose measured result no longer matches the separate validation capture must not be presented as the exact origin projection.");

        Require(BlueStacksUniversalValidatedProfileBridge.TryProject(
                    profile with { FrameTimeMs = (profile.FrameTimeMs ?? 0) + 0.5 },
                    source,
                    fixture.CandidateSpace) is null,
            "Frame-time provenance must remain the exact specialized validation result.");

        Require(BlueStacksUniversalValidatedProfileBridge.TryProject(
                    profile with { LatencyMs = (profile.LatencyMs ?? 0) + 0.5 },
                    source,
                    fixture.CandidateSpace) is null,
            "Latency provenance must remain the exact specialized validation result.");
    }

    private static void RejectsFabricatedUniversalValidationProjection(
        Fixture fixture,
        PerformanceProfile profile)
    {
        var source = RequireValidationProjection(fixture);
        var fabricated = source with
        {
            UniversalCandidate = new UniversalTuningCandidate
            {
                Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["workload.fabricated.value"] = "not-the-proven-candidate"
                }
            }
        };

        Require(BlueStacksUniversalValidatedProfileBridge.TryProject(
                    profile,
                    fabricated,
                    fixture.CandidateSpace) is null,
            "A caller must not be able to fabricate or substitute a universal candidate after the validated-History projection was proven.");

        var wrongIdentity = fixture.CandidateSpace.Identity with
        {
            GameId = "garena.free-fire-max",
            AdapterId = "bluestacks.free-fire-max",
            LegacyGameKind = GameKind.FreeFireMax
        };
        var wrongSpace = fixture.CandidateSpace with
        {
            Identity = wrongIdentity,
            AdapterId = wrongIdentity.AdapterId
        };
        Require(BlueStacksUniversalValidatedProfileBridge.TryProject(
                    profile,
                    source,
                    wrongSpace) is null,
            "Stable workload/adapter provenance must be revalidated against the exact candidate space rather than trusted from a caller-constructed projection.");
    }

    private static UniversalValidatedPerformanceProjection RequireValidationProjection(Fixture fixture)
        => BlueStacksUniversalValidatedPerformanceBridge.TryProject(fixture.Record, fixture.CandidateSpace)
           ?? throw new InvalidOperationException("Fixture requires a valid upstream universal validation projection.");

    private static Fixture CreateFixture()
    {
        var start = new DateTimeOffset(2026, 9, 10, 4, 0, 0, TimeSpan.Zero);
        var instance = new BlueStacksInstance
        {
            Name = "Pie64",
            AndroidVersion = "Pie 64-bit",
            CpuCores = 4,
            RamMb = 4096,
            Renderer = "Vulkan",
            Fps = 90,
            Resolution = "1920x1080",
            Dpi = 320
        };
        var environment = new EnvironmentSnapshot
        {
            MachineName = "DG-VALIDATED-PROFILE-TEST",
            WindowsDescription = "Windows 11 validated profile test",
            LogicalProcessors = 8,
            MemoryTotalGb = 16,
            Is64BitOs = true,
            BlueStacksDetected = true,
            Instances = [instance],
            ActiveGame = GameKind.FreeFire
        };
        var configuration = PerformanceConfigurationSnapshot.Capture(environment, instance, GameKind.FreeFire)
            ?? throw new InvalidOperationException("Fixture requires a complete specialized performance configuration.");

        var identity = LegacyGameIdentityBridge.FromGameKind(GameKind.FreeFire)
            ?? throw new InvalidOperationException("Fixture requires the stable Free Fire GameIdentity bridge.");
        var specializedCandidate = new TuningCandidate
        {
            CpuCores = configuration.CpuCores,
            RamMb = configuration.RamMb,
            Renderer = configuration.Renderer,
            FpsTarget = configuration.FpsTarget,
            Resolution = configuration.Resolution
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
        var binding = new BlueStacksUniversalTuningCandidateBinding
        {
            SpecializedCandidate = specializedCandidate,
            UniversalCandidate = universalCandidate
        };
        var candidateSpace = new BlueStacksUniversalTuningCandidateSpace
        {
            Identity = identity,
            AdapterId = identity.AdapterId,
            Bindings = [binding]
        };
        var universalContext = new PerformanceUniversalConfigurationContext
        {
            SchemaVersion = 1,
            GameId = identity.GameId,
            AdapterId = identity.AdapterId,
            Machine = new MachineEnvironmentFingerprintV2
            {
                SchemaVersion = 2,
                Id = "validated-profile-machine-v2",
                MachineName = environment.MachineName,
                WindowsDescription = environment.WindowsDescription,
                LogicalProcessors = environment.LogicalProcessors,
                MemoryTotalMb = environment.MemoryTotalGb * 1024L,
                Is64BitOs = environment.Is64BitOs,
                HardwareSignatures = ["CPU|TEST", "GPU|TEST"]
            },
            CapabilityValues = Array.Empty<PerformanceCapabilityValueSnapshot>(),
            WorkloadConfiguration = new Dictionary<string, string>(),
            DisplayDriverContext = new Dictionary<string, string>()
        }.Rehydrate();

        var baseline = PerformanceEvidenceSnapshot.Capture(
            "A · baseline",
            Interval(start, 100, 10, 11),
            start,
            configuration,
            universalContext);
        var candidate = PerformanceEvidenceSnapshot.Capture(
            "B · candidate",
            Interval(start.AddMinutes(1), 120, 8.3, 8.5),
            start.AddMinutes(1),
            configuration,
            universalContext);
        var validation = PerformanceEvidenceSnapshot.Capture(
            "Validation · independent",
            Interval(start.AddMinutes(2), 123, 8.1, 8.2),
            start.AddMinutes(2),
            configuration,
            universalContext);
        var record = new PerformanceComparisonHistoryRecord
        {
            Label = "Universal validated profile source",
            SavedAt = start.AddMinutes(1),
            Baseline = baseline,
            Candidate = candidate,
            ValidationStatus = PerformanceComparisonValidationStatus.Validated,
            ValidationEvidence = validation,
            ValidatedAt = start.AddMinutes(2)
        };
        Require(record.CanOriginateProfile,
            "Fixture specialized validation record must be eligible under the existing profile-origin authority.");

        return new Fixture(environment, candidateSpace, record);
    }

    private static PerformanceIntervalSummary Interval(
        DateTimeOffset start,
        double fps,
        double frameTimeMs,
        double latencyMs)
        => new()
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
        };

    private sealed record Fixture(
        EnvironmentSnapshot Environment,
        BlueStacksUniversalTuningCandidateSpace CandidateSpace,
        PerformanceComparisonHistoryRecord Record);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
