using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Workloads;

internal static class UniversalTuningResultProjectionSelfTests
{
    internal static void Run()
    {
        ProjectsExistingEvidenceAndFiveWinnerRolesOneToOne();
        RejectsCrossWorkloadResultProjection();
        RejectsAdapterAuthorityMismatch();
        RejectsMissingOrAmbiguousCandidateBinding();
        RejectsWinnerWithoutExactSourceEvidence();
        PreservesObservedEvidenceWithoutInventingWinners();

        Console.WriteLine("PASS Track 5 universal tuning result/profile projection preserves specialized authority");
    }

    private static void ProjectsExistingEvidenceAndFiveWinnerRolesOneToOne()
    {
        var (engine, candidateSpace) = CreateFixture();
        Require(candidateSpace.Bindings.Count >= 3,
            "The projection fixture requires at least three exact applicable BlueStacks bindings.");
        var selected = candidateSpace.Bindings.Take(3).ToArray();
        var evidence = CreateValidatedEvidence(selected);
        var result = engine.SelectWinners(GameKind.FreeFire, AutoTunerMode.Deep, evidence);
        Require(result.Winners.Count == 5,
            "Existing AutoTunerEngine must remain the authority that creates the five generated winner roles for this fixture.");

        var projected = BlueStacksUniversalTuningResultBridge.Project(result, candidateSpace);

        Require(ReferenceEquals(projected.SpecializedResult, result),
            "Universal result projection must retain the exact existing specialized TuningResult as its source result.");
        Require(projected.Identity.GameId == candidateSpace.Identity.GameId
                && projected.Identity.AdapterId == candidateSpace.Identity.AdapterId
                && projected.Identity.LegacyGameKind == candidateSpace.Identity.LegacyGameKind,
            "Universal result projection must preserve the stable workload identity from the Slice 3 candidate space.");
        Require(projected.AdapterId == candidateSpace.AdapterId,
            "Universal result projection must preserve the exact resolved adapter authority from the candidate space.");
        Require(projected.Evidence.Count == result.Evidence.Count,
            "Universal result projection must neither add nor drop existing specialized evidence.");
        Require(projected.Winners.Count == result.Winners.Count,
            "Universal result projection must neither add nor drop existing specialized winners.");

        for (var index = 0; index < result.Evidence.Count; index++)
        {
            var sourceEvidence = result.Evidence[index];
            var projectedEvidence = projected.Evidence[index];
            Require(ReferenceEquals(projectedEvidence.SpecializedEvidence, sourceEvidence),
                "Universal evidence projection must retain the exact existing CandidateEvidence object instead of copying or recomputing evidence authority.");
            Require(projectedEvidence.SpecializedEvidence.Evidence == sourceEvidence.Evidence,
                "Universal evidence projection must preserve EvidenceLevel exactly and must never upgrade evidence.");

            var expectedBinding = candidateSpace.Bindings.Single(binding => binding.SpecializedCandidate == sourceEvidence.Candidate);
            Require(ReferenceEquals(projectedEvidence.UniversalCandidate, expectedBinding.UniversalCandidate),
                "Every projected evidence item must point to the exact UniversalTuningCandidate already bound to its specialized candidate in Slice 3.");
        }

        var expectedKinds = new[]
        {
            ProfileKind.MaximumFps,
            ProfileKind.LowestLatency,
            ProfileKind.Stability,
            ProfileKind.Quality,
            ProfileKind.Recommended
        };
        Require(projected.Winners.Select(winner => winner.SpecializedProfile.Kind).SequenceEqual(expectedKinds),
            "Universal projection must preserve the exact existing five generated winner roles and their order.");

        for (var index = 0; index < result.Winners.Count; index++)
        {
            var sourceProfile = result.Winners[index];
            var projectedWinner = projected.Winners[index];
            Require(ReferenceEquals(projectedWinner.SpecializedProfile, sourceProfile),
                "Universal winner projection must retain the exact existing PerformanceProfile instead of recreating profile authority.");
            Require(projectedWinner.SpecializedProfile.Evidence == EvidenceLevel.Validated,
                "Existing validated winner evidence must remain validated without being recalculated by the projection layer.");
            Require(result.Evidence.Contains(projectedWinner.SourceEvidence),
                "Every universal winner projection must identify an evidence item already present in the specialized result.");
            Require(ProfileMatchesCandidate(sourceProfile, projectedWinner.SourceEvidence.Candidate),
                "Winner source evidence must be the exact specialized candidate configuration copied into the existing winner profile.");

            var expectedBinding = candidateSpace.Bindings.Single(binding => binding.SpecializedCandidate == projectedWinner.SourceEvidence.Candidate);
            Require(ReferenceEquals(projectedWinner.UniversalCandidate, expectedBinding.UniversalCandidate),
                "Every universal winner must correlate to the exact Slice 3 universal candidate for its source evidence.");
        }
    }

    private static void RejectsCrossWorkloadResultProjection()
    {
        var (engine, candidateSpace) = CreateFixture();
        Require(candidateSpace.Bindings.Count >= 3,
            "Cross-workload projection fixture requires at least three exact bindings.");

        var evidence = CreateValidatedEvidence(candidateSpace.Bindings.Take(3).ToArray());
        var wrongWorkloadResult = engine.SelectWinners(
            GameKind.FreeFireMax,
            AutoTunerMode.Deep,
            evidence);

        RequireThrows<InvalidOperationException>(
            () => BlueStacksUniversalTuningResultBridge.Project(wrongWorkloadResult, candidateSpace),
            "A Free Fire MAX result must not be projected onto a Free Fire candidate space even when specialized candidate values happen to match.");
    }

    private static void RejectsAdapterAuthorityMismatch()
    {
        var (engine, candidateSpace) = CreateFixture();
        Require(candidateSpace.Bindings.Count >= 3,
            "Adapter mismatch fixture requires at least three exact bindings.");

        var evidence = CreateValidatedEvidence(candidateSpace.Bindings.Take(3).ToArray());
        var result = engine.SelectWinners(GameKind.FreeFire, AutoTunerMode.Deep, evidence);

        RequireThrows<InvalidOperationException>(
            () => BlueStacksUniversalTuningResultBridge.Project(
                result,
                candidateSpace with { AdapterId = "tampered-adapter" }),
            "Universal result projection must reject a candidate space whose adapter authority no longer matches the stable GameIdentity adapter authority.");
        RequireThrows<InvalidOperationException>(
            () => BlueStacksUniversalTuningResultBridge.Project(
                result,
                candidateSpace with { AdapterId = "   " }),
            "Universal result projection must reject a candidate space with blank adapter authority.");
    }

    private static void RejectsMissingOrAmbiguousCandidateBinding()
    {
        var (engine, candidateSpace) = CreateFixture();
        Require(candidateSpace.Bindings.Count >= 3,
            "Binding mismatch fixture requires at least three exact bindings.");
        var evidence = CreateValidatedEvidence(candidateSpace.Bindings.Take(3).ToArray());
        var result = engine.SelectWinners(GameKind.FreeFire, AutoTunerMode.Deep, evidence);
        var firstCandidate = evidence[0].Candidate;

        var missingBindingSpace = candidateSpace with
        {
            Bindings = candidateSpace.Bindings
                .Where(binding => binding.SpecializedCandidate != firstCandidate)
                .ToArray()
        };
        RequireThrows<InvalidOperationException>(
            () => BlueStacksUniversalTuningResultBridge.Project(result, missingBindingSpace),
            "Projection must fail closed when result evidence has no exact Slice 3 candidate binding.");

        var duplicateBinding = candidateSpace.Bindings.Single(binding => binding.SpecializedCandidate == firstCandidate);
        var ambiguousBindingSpace = candidateSpace with
        {
            Bindings = candidateSpace.Bindings.Concat([duplicateBinding]).ToArray()
        };
        RequireThrows<InvalidOperationException>(
            () => BlueStacksUniversalTuningResultBridge.Project(result, ambiguousBindingSpace),
            "Projection must fail closed when result evidence maps to more than one Slice 3 candidate binding.");
    }

    private static void RejectsWinnerWithoutExactSourceEvidence()
    {
        var (engine, candidateSpace) = CreateFixture();
        Require(candidateSpace.Bindings.Count >= 3,
            "Winner source fixture requires at least three exact bindings.");
        var evidence = CreateValidatedEvidence(candidateSpace.Bindings.Take(3).ToArray());
        var result = engine.SelectWinners(GameKind.FreeFire, AutoTunerMode.Deep, evidence);
        Require(result.Winners.Count == 5,
            "Winner source fixture requires the existing five generated winners.");

        var alteredWinner = result.Winners[0] with
        {
            FpsTarget = result.Winners[0].FpsTarget + 1
        };
        var tamperedResult = result with
        {
            Winners = new[] { alteredWinner }.Concat(result.Winners.Skip(1)).ToArray()
        };

        RequireThrows<InvalidOperationException>(
            () => BlueStacksUniversalTuningResultBridge.Project(tamperedResult, candidateSpace),
            "Projection must fail closed when a winner profile cannot be traced to exactly one source evidence configuration.");
    }

    private static void PreservesObservedEvidenceWithoutInventingWinners()
    {
        var (engine, candidateSpace) = CreateFixture();
        Require(candidateSpace.Bindings.Count > 0,
            "Observed-only fixture requires at least one exact candidate binding.");
        var observed = new CandidateEvidence
        {
            Candidate = candidateSpace.Bindings[0].SpecializedCandidate,
            Evidence = EvidenceLevel.Observed,
            Confidence = 0.99,
            Sample = new TelemetrySample
            {
                Fps = 144,
                OnePercentLow = 130,
                LatencyMs = 7,
                FrameTimeMs = 6.9
            }
        };
        var result = engine.SelectWinners(
            GameKind.FreeFire,
            AutoTunerMode.Deep,
            [observed]);
        Require(result.Winners.Count == 0,
            "Existing AutoTunerEngine must not create winners from Observed-only evidence.");

        var projected = BlueStacksUniversalTuningResultBridge.Project(result, candidateSpace);

        Require(projected.Winners.Count == 0,
            "Universal correlation metadata must not invent a winner when specialized authority emitted none.");
        Require(projected.Evidence.Count == 1
                && ReferenceEquals(projected.Evidence[0].SpecializedEvidence, observed)
                && projected.Evidence[0].SpecializedEvidence.Evidence == EvidenceLevel.Observed,
            "Universal projection must preserve Observed evidence exactly and never upgrade it to Validated.");
    }

    private static (AutoTunerEngine Engine, BlueStacksUniversalTuningCandidateSpace CandidateSpace) CreateFixture()
    {
        var environment = new EnvironmentSnapshot
        {
            LogicalProcessors = 8,
            MemoryTotalGb = 16
        };
        var instance = new BlueStacksInstance
        {
            Name = "Pie64",
            CpuCores = 4,
            RamMb = 4096,
            Renderer = "Vulkan",
            Fps = 90,
            Resolution = "1920x1080"
        };
        var engine = new AutoTunerEngine();
        var candidateSpace = new BlueStacksUniversalTuningCandidateBridge(engine, CreateResolver()).Build(
            environment,
            instance,
            GameKind.FreeFire,
            AutoTunerMode.Deep,
            FullCapturedSettings(instance.Name));
        return (engine, candidateSpace);
    }

    private static GameAdapterResolver CreateResolver()
        => new(
        [
            new GenericGameAdapter(),
            BlueStacksFreeFireGameAdapter.For(GameKind.FreeFire),
            BlueStacksFreeFireGameAdapter.For(GameKind.FreeFireMax)
        ]);

    private static CandidateEvidence[] CreateValidatedEvidence(
        IReadOnlyList<BlueStacksUniversalTuningCandidateBinding> selected)
        =>
        [
            new()
            {
                Candidate = selected[0].SpecializedCandidate,
                Evidence = EvidenceLevel.Validated,
                Confidence = 0.94,
                Sample = new TelemetrySample
                {
                    Fps = 100,
                    OnePercentLow = 88,
                    LatencyMs = 12,
                    FrameTimeMs = 10,
                    StutterPercent = 1.5,
                    GpuTemperatureC = 60
                }
            },
            new()
            {
                Candidate = selected[1].SpecializedCandidate,
                Evidence = EvidenceLevel.Validated,
                Confidence = 0.96,
                Sample = new TelemetrySample
                {
                    Fps = 112,
                    OnePercentLow = 91,
                    LatencyMs = 10,
                    FrameTimeMs = 8.9,
                    StutterPercent = 1.2,
                    GpuTemperatureC = 63
                }
            },
            new()
            {
                Candidate = selected[2].SpecializedCandidate,
                Evidence = EvidenceLevel.Validated,
                Confidence = 0.98,
                Sample = new TelemetrySample
                {
                    Fps = 106,
                    OnePercentLow = 101,
                    LatencyMs = 8,
                    FrameTimeMs = 9.4,
                    StutterPercent = 0.5,
                    GpuTemperatureC = 61
                }
            }
        ];

    private static Dictionary<string, string> FullCapturedSettings(string instanceName)
        => new(StringComparer.OrdinalIgnoreCase)
        {
            [$"bst.instance.{instanceName}.cpus"] = "\"4\"",
            [$"bst.instance.{instanceName}.ram"] = "\"4096\"",
            [$"bst.instance.{instanceName}.max_fps"] = "\"90\"",
            [$"bst.instance.{instanceName}.enable_high_fps"] = "\"1\"",
            [$"bst.instance.{instanceName}.display_width"] = "\"1920\"",
            [$"bst.instance.{instanceName}.display_height"] = "\"1080\"",
            [$"bst.instance.{instanceName}.graphics_renderer"] = "\"Vulkan\""
        };

    private static bool ProfileMatchesCandidate(PerformanceProfile profile, TuningCandidate candidate)
        => profile.CpuCores == candidate.CpuCores
           && profile.RamMb == candidate.RamMb
           && profile.FpsTarget == candidate.FpsTarget
           && string.Equals(profile.Renderer, candidate.Renderer, StringComparison.Ordinal)
           && string.Equals(profile.Resolution, candidate.Resolution, StringComparison.Ordinal);

    private static void RequireThrows<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
