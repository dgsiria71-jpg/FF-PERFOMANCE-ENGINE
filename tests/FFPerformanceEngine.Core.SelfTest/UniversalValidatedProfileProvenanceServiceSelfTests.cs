using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Workloads;

internal static class UniversalValidatedProfileProvenanceServiceSelfTests
{
    internal static async Task RunAsync()
    {
        var tempRoot = Path.Combine(
            Path.GetTempPath(),
            "ffpe-universal-profile-provenance-service-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var fixture = CreateFixture();
            var profiles = new ProfileService(Path.Combine(tempRoot, "profiles.json"));
            var history = new HistoryService(Path.Combine(tempRoot, "history.json"));

            var observed = await history.SavePerformanceComparisonAsync(
                "Universal persisted profile source",
                PerformanceABComparison.Create(fixture.Baseline, fixture.Candidate));
            await history.RequestPerformanceValidationAsync(observed.Id);
            var validated = await history.CompletePerformanceValidationAsync(
                observed.Id,
                fixture.Validation);
            var custom = await profiles.CreateCustomFromValidatedComparisonAsync(
                validated,
                fixture.Environment,
                "Persisted universal Custom");

            var service = new UniversalValidatedProfileProvenanceService(
                profiles,
                history,
                fixture.CandidateBridge);

            await ResolvesPersistedCustomAcrossEquivalentModeOverlapAsync(
                fixture,
                service,
                custom,
                validated);
            await FailsClosedWhenCurrentCandidateSpaceCannotReproveOriginAsync(
                fixture,
                service,
                custom);
            await RejectsProfilesWithoutExactPersistedSpecializedAuthorityAsync(
                fixture,
                profiles,
                service,
                custom);

            Console.WriteLine(
                "PASS Track 5 application resolver re-proves persisted Custom universal provenance without inventing AutoTuner mode or capability");
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { }
        }
    }

    private static async Task ResolvesPersistedCustomAcrossEquivalentModeOverlapAsync(
        Fixture fixture,
        UniversalValidatedProfileProvenanceService service,
        PerformanceProfile custom,
        PerformanceComparisonHistoryRecord validated)
    {
        var adaptiveSpace = fixture.CandidateBridge.Build(
            fixture.Environment,
            fixture.Instance,
            GameKind.FreeFire,
            AutoTunerMode.Adaptive,
            fixture.CapturedSettings);
        var deepSpace = fixture.CandidateBridge.Build(
            fixture.Environment,
            fixture.Instance,
            GameKind.FreeFire,
            AutoTunerMode.Deep,
            fixture.CapturedSettings);

        var adaptiveValidation = BlueStacksUniversalValidatedPerformanceBridge.TryProject(
            validated,
            adaptiveSpace);
        var deepValidation = BlueStacksUniversalValidatedPerformanceBridge.TryProject(
            validated,
            deepSpace);

        Require(adaptiveValidation is not null && deepValidation is not null,
            "Fixture must prove that the same specialized validated candidate is reproducible in both Adaptive and Deep candidate spaces.");
        Require(CandidatesEquivalent(
                adaptiveValidation!.UniversalCandidate,
                deepValidation!.UniversalCandidate),
            "Equivalent mode overlap must describe the same universal candidate values before the application resolver is exercised.");

        var projection = await service.ResolveCurrentAsync(
            custom.Id,
            fixture.Environment,
            fixture.Instance,
            fixture.CapturedSettings);

        Require(projection is not null,
            "A persisted Custom Validated profile whose exact source remains reproducible in multiple equivalent modes must resolve to one universal provenance projection.");
        Require(projection!.SpecializedProfile.Id == custom.Id
                && projection.SpecializedProfile.Kind == ProfileKind.Custom
                && projection.SpecializedProfile.Evidence == EvidenceLevel.Validated,
            "The application resolver must return the persisted specialized Custom profile, not manufacture a generic profile authority.");
        Require(projection.SourceValidation.SpecializedRecord.Id == validated.Id,
            "Resolved universal provenance must point to the exact persisted specialized validation record identified by SourceComparisonId.");
        Require(string.Equals(
                    projection.Identity.GameId,
                    fixture.Identity.GameId,
                    StringComparison.Ordinal)
                && string.Equals(
                    projection.AdapterId,
                    fixture.Identity.AdapterId,
                    StringComparison.Ordinal),
            "Resolved provenance must preserve the stable GameId and resolved adapter from the proven Track 5 candidate bridge.");
        Require(CandidateMatchesInstance(projection.UniversalCandidate, fixture.Identity.AdapterId, fixture.Instance),
            "Resolved provenance must retain the exact universal candidate values corresponding to the persisted specialized profile configuration.");
    }

    private static async Task FailsClosedWhenCurrentCandidateSpaceCannotReproveOriginAsync(
        Fixture fixture,
        UniversalValidatedProfileProvenanceService service,
        PerformanceProfile custom)
    {
        var noAllowList = await service.ResolveCurrentAsync(
            custom.Id,
            fixture.Environment,
            fixture.Instance,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        Require(noAllowList is null,
            "Missing current BlueStacks allow-listed capability must remain missing; historical specialized validity must not be converted into current universal candidate support.");

        var otherInstance = fixture.Instance with { Name = "OtherPie64" };
        var wrongInstance = await service.ResolveCurrentAsync(
            custom.Id,
            fixture.Environment,
            otherInstance,
            fixture.CapturedSettings);
        Require(wrongInstance is null,
            "A different current BlueStacks instance must not inherit universal provenance from a persisted profile bound to another instance.");

        var rendererDriftInstance = fixture.Instance with { Renderer = "DirectX" };
        var rendererDriftSettings = new Dictionary<string, string>(fixture.CapturedSettings, StringComparer.OrdinalIgnoreCase)
        {
            ["bst.instance.Pie64.graphics_renderer"] = "DirectX"
        };
        var rendererDrift = await service.ResolveCurrentAsync(
            custom.Id,
            fixture.Environment,
            rendererDriftInstance,
            rendererDriftSettings);
        Require(rendererDrift is null,
            "A current candidate space with renderer drift must not be treated as the persisted universal candidate provenance.");
    }

    private static async Task RejectsProfilesWithoutExactPersistedSpecializedAuthorityAsync(
        Fixture fixture,
        ProfileService profiles,
        UniversalValidatedProfileProvenanceService service,
        PerformanceProfile custom)
    {
        var wrongSource = custom with
        {
            Id = Guid.NewGuid(),
            Name = "Wrong source",
            SourceComparisonId = Guid.NewGuid()
        };
        var observed = custom with
        {
            Id = Guid.NewGuid(),
            Name = "Observed impostor",
            Evidence = EvidenceLevel.Observed
        };
        var generated = custom with
        {
            Id = Guid.NewGuid(),
            Name = "Generated impostor",
            Kind = ProfileKind.Recommended
        };
        await profiles.SaveAsync([custom, wrongSource, observed, generated]);

        Require(await service.ResolveCurrentAsync(
                    wrongSource.Id,
                    fixture.Environment,
                    fixture.Instance,
                    fixture.CapturedSettings) is null,
            "A persisted Custom whose SourceComparisonId does not resolve to exactly one specialized validation record must receive no universal projection.");
        Require(await service.ResolveCurrentAsync(
                    observed.Id,
                    fixture.Environment,
                    fixture.Instance,
                    fixture.CapturedSettings) is null,
            "Observed profile evidence must never be upgraded by the application provenance resolver.");
        Require(await service.ResolveCurrentAsync(
                    generated.Id,
                    fixture.Environment,
                    fixture.Instance,
                    fixture.CapturedSettings) is null,
            "Generated winner roles are outside this persisted-Custom resolver and must not be inferred through the Custom validated-profile path.");
    }

    private static Fixture CreateFixture()
    {
        var start = new DateTimeOffset(2026, 9, 10, 5, 0, 0, TimeSpan.Zero);
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
            MachineName = "DG-PROVENANCE-RESOLUTION-TEST",
            WindowsDescription = "Windows 11 provenance resolution test",
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

        var configuration = PerformanceConfigurationSnapshot.Capture(
            environment,
            instance,
            GameKind.FreeFire)
            ?? throw new InvalidOperationException("Fixture requires a complete specialized performance configuration.");
        var universalContext = new PerformanceUniversalConfigurationContext
        {
            SchemaVersion = 1,
            GameId = identity.GameId,
            AdapterId = identity.AdapterId,
            Machine = new MachineEnvironmentFingerprintV2
            {
                SchemaVersion = 2,
                Id = "profile-provenance-resolution-machine-v2",
                MachineName = environment.MachineName,
                WindowsDescription = environment.WindowsDescription,
                LogicalProcessors = environment.LogicalProcessors,
                MemoryTotalMb = (long)Math.Round(environment.MemoryTotalGb.GetValueOrDefault() * 1024d),
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

        return new Fixture(
            environment,
            instance,
            capturedSettings,
            identity,
            candidateBridge,
            baseline,
            candidate,
            validation);
    }

    private static bool CandidateMatchesInstance(
        UniversalTuningCandidate candidate,
        string adapterId,
        BlueStacksInstance instance)
    {
        var prefix = $"workload.{adapterId}.";
        return candidate.Values.TryGetValue(prefix + "cpu-cores", out var cpu)
               && cpu == instance.CpuCores?.ToString()
               && candidate.Values.TryGetValue(prefix + "ram-mb", out var ram)
               && ram == instance.RamMb?.ToString()
               && candidate.Values.TryGetValue(prefix + "renderer", out var renderer)
               && string.Equals(renderer, instance.Renderer, StringComparison.OrdinalIgnoreCase)
               && candidate.Values.TryGetValue(prefix + "fps-target", out var fps)
               && fps == instance.Fps?.ToString()
               && candidate.Values.TryGetValue(prefix + "resolution", out var resolution)
               && string.Equals(resolution, instance.Resolution, StringComparison.OrdinalIgnoreCase);
    }

    private static bool CandidatesEquivalent(
        UniversalTuningCandidate left,
        UniversalTuningCandidate right)
        => left.Values.Count == right.Values.Count
           && left.Values.All(pair =>
               right.Values.TryGetValue(pair.Key, out var value)
               && string.Equals(pair.Value, value, StringComparison.OrdinalIgnoreCase));

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
        BlueStacksInstance Instance,
        IReadOnlyDictionary<string, string> CapturedSettings,
        GameIdentity Identity,
        BlueStacksUniversalTuningCandidateBridge CandidateBridge,
        PerformanceEvidenceSnapshot Baseline,
        PerformanceEvidenceSnapshot Candidate,
        PerformanceEvidenceSnapshot Validation);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
