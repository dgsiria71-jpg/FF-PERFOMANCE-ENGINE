using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;

internal static class UniversalTuningSearchSpaceSelfTests
{
    internal static void Run()
    {
        RejectInvalidDimensions();
        EmptySpaceProducesNoCandidate();
        DeterministicCartesianProduct();
        CandidateBudgetIsDeterministic();
        LegacyBlueStacksCandidateRemainsUnchanged();
        WindowsCapabilityPlanBridgeIsCapabilityHonest();

        Console.WriteLine("PASS Track 5 universal tuning search-space contract");
    }

    private static void RejectInvalidDimensions()
    {
        var planner = new UniversalTuningSearchSpacePlanner();

        RequireThrows<ArgumentException>(() => planner.Build(
        [
            Dimension(" ", UniversalTuningDimensionScope.System, "windows.power.active_policy", "performance")
        ]), "Blank dimension ids must be rejected rather than normalized into invented identity.");

        RequireThrows<ArgumentException>(() => planner.Build(
        [
            Dimension("system.power", UniversalTuningDimensionScope.System, " ", "performance")
        ]), "Blank authority ids must be rejected because every search dimension needs explicit provenance.");

        RequireThrows<ArgumentException>(() => planner.Build(
        [
            Dimension("system.power", UniversalTuningDimensionScope.System, "windows.power.active_policy")
        ]), "A dimension without explicit candidate values must be rejected rather than receiving defaults.");

        RequireThrows<ArgumentException>(() => planner.Build(
        [
            Dimension("system.power", UniversalTuningDimensionScope.System, "windows.power.active_policy", "performance", " ")
        ]), "Blank candidate values must be rejected instead of silently removed.");

        RequireThrows<ArgumentException>(() => planner.Build(
        [
            Dimension("System.Power", UniversalTuningDimensionScope.System, "windows.power.active_policy", "performance"),
            Dimension("system.power", UniversalTuningDimensionScope.System, "windows.power.active_policy", "balanced")
        ]), "Dimension ids must be unique case-insensitively.");

        RequireThrows<ArgumentException>(() => planner.Build(
        [
            Dimension("system.power", UniversalTuningDimensionScope.System, "windows.power.active_policy", "performance", "performance")
        ]), "Duplicate values in one dimension must be rejected rather than silently deduplicated.");
    }

    private static void EmptySpaceProducesNoCandidate()
    {
        var planner = new UniversalTuningSearchSpacePlanner();
        var candidates = planner.Build(Array.Empty<UniversalTuningDimension>());
        Require(candidates.Count == 0,
            "Zero declared dimensions must produce zero candidates; an empty/default tuning candidate must never be invented.");
    }

    private static void DeterministicCartesianProduct()
    {
        var planner = new UniversalTuningSearchSpacePlanner();
        var candidates = planner.Build(
        [
            // Intentionally supplied out of id order. The planner owns deterministic dimension ordering,
            // while each authority retains the exact declared value order.
            Dimension("workload.renderer", UniversalTuningDimensionScope.Workload, "adapter:test-game", "vulkan", "directx"),
            Dimension("system.power", UniversalTuningDimensionScope.System, "windows.power.active_policy", "balanced", "performance")
        ]);

        Require(candidates.Count == 4,
            "Two dimensions with two explicit values each must produce exactly four Cartesian candidates.");

        var signatures = candidates.Select(Signature).ToArray();
        Require(signatures.SequenceEqual(new[]
        {
            "system.power=balanced|workload.renderer=vulkan",
            "system.power=balanced|workload.renderer=directx",
            "system.power=performance|workload.renderer=vulkan",
            "system.power=performance|workload.renderer=directx"
        }, StringComparer.Ordinal),
            "Search-space ordering must sort dimensions by id and vary the last sorted dimension fastest without reordering authority-declared values.");

        Require(candidates.All(candidate => candidate.Values.Count == 2
                                            && candidate.Values.Keys.All(key => key is "system.power" or "workload.renderer")),
            "Universal candidates must contain only explicitly declared dimensions; the planner cannot attach hidden/default tuning axes.");
        Require(candidates.SelectMany(candidate => candidate.Values.Values)
                .All(value => value is "balanced" or "performance" or "vulkan" or "directx"),
            "Universal candidates must preserve declared option values exactly and never invent a value.");
    }

    private static void CandidateBudgetIsDeterministic()
    {
        var planner = new UniversalTuningSearchSpacePlanner(
            new UniversalTuningSearchSpacePolicy { MaxCandidates = 3 });
        var candidates = planner.Build(
        [
            Dimension("system.a", UniversalTuningDimensionScope.System, "capability:a", "a1", "a2"),
            Dimension("system.b", UniversalTuningDimensionScope.System, "capability:b", "b1", "b2")
        ]);

        Require(candidates.Count == 3,
            "MaxCandidates must bound the Cartesian search space exactly when the raw product is larger.");
        Require(candidates.Select(Signature).SequenceEqual(new[]
        {
            "system.a=a1|system.b=b1",
            "system.a=a1|system.b=b2",
            "system.a=a2|system.b=b1"
        }, StringComparer.Ordinal),
            "Candidate budgeting must truncate deterministic enumeration and must never randomize the explored prefix.");
    }

    private static void LegacyBlueStacksCandidateRemainsUnchanged()
    {
        var legacy = new TuningCandidate
        {
            CpuCores = 6,
            RamMb = 6144,
            Renderer = "Vulkan",
            FpsTarget = 120,
            Resolution = "1920x1080"
        };

        Require(legacy.CpuCores == 6
                && legacy.RamMb == 6144
                && legacy.Renderer == "Vulkan"
                && legacy.FpsTarget == 120
                && legacy.Resolution == "1920x1080",
            "Track 5 search-space foundations must be additive and keep the existing BlueStacks TuningCandidate source-compatible.");
    }

    private static void WindowsCapabilityPlanBridgeIsCapabilityHonest()
    {
        var ready = new WindowsCapabilityCandidatePlan
        {
            CapabilityId = "windows.power.active_policy",
            CurrentValue = "balanced",
            Disposition = WindowsCapabilityCandidatePlanDisposition.Ready,
            Candidates =
            [
                new WindowsCapabilityCandidate
                {
                    CapabilityId = "windows.power.active_policy",
                    TargetValue = "extreme",
                    ExplorationRank = 2,
                    Source = WindowsCapabilityCandidateSource.ExplicitMetadata
                },
                new WindowsCapabilityCandidate
                {
                    CapabilityId = "windows.power.active_policy",
                    TargetValue = "performance",
                    ExplorationRank = 1,
                    Source = WindowsCapabilityCandidateSource.ExplicitMetadata
                }
            ]
        };

        var dimension = UniversalTuningSystemDimensionFactory.FromWindowsCandidatePlan(ready);
        Require(dimension is not null,
            "A Ready Windows capability plan with explicit candidates must become one universal system dimension.");
        Require(dimension!.Scope == UniversalTuningDimensionScope.System,
            "Windows capability exploration must enter the universal search space as a System dimension.");
        Require(dimension.Id == "windows.power.active_policy"
                && dimension.AuthorityId == "windows.power.active_policy",
            "The Windows capability id must remain both dimension identity and explicit authority identity.");
        Require(dimension.CandidateValues.SequenceEqual(new[] { "performance", "extreme" }, StringComparer.Ordinal),
            "The bridge must order plan targets by ExplorationRank while preserving exact TargetValue text.");

        foreach (var disposition in new[]
                 {
                     WindowsCapabilityCandidatePlanDisposition.Unavailable,
                     WindowsCapabilityCandidatePlanDisposition.MissingCurrentState,
                     WindowsCapabilityCandidatePlanDisposition.NoCandidateSpace
                 })
        {
            var blocked = UniversalTuningSystemDimensionFactory.FromWindowsCandidatePlan(new WindowsCapabilityCandidatePlan
            {
                CapabilityId = "windows.test.blocked",
                CurrentValue = disposition == WindowsCapabilityCandidatePlanDisposition.MissingCurrentState ? null : "current",
                Disposition = disposition,
                Candidates =
                [
                    new WindowsCapabilityCandidate
                    {
                        CapabilityId = "windows.test.blocked",
                        TargetValue = "target",
                        ExplorationRank = 1,
                        Source = WindowsCapabilityCandidateSource.ExplicitMetadata
                    }
                ]
            });
            Require(blocked is null,
                $"Windows candidate plans with disposition {disposition} must remain absent from universal tuning search space.");
        }

        var emptyReady = UniversalTuningSystemDimensionFactory.FromWindowsCandidatePlan(new WindowsCapabilityCandidatePlan
        {
            CapabilityId = "windows.test.empty",
            CurrentValue = "current",
            Disposition = WindowsCapabilityCandidatePlanDisposition.Ready,
            Candidates = Array.Empty<WindowsCapabilityCandidate>()
        });
        Require(emptyReady is null,
            "A nominally Ready plan without candidates must remain absent instead of becoming an empty/default dimension.");

        RequireThrows<ArgumentException>(() => UniversalTuningSystemDimensionFactory.FromWindowsCandidatePlan(
            new WindowsCapabilityCandidatePlan
            {
                CapabilityId = " ",
                CurrentValue = "current",
                Disposition = WindowsCapabilityCandidatePlanDisposition.Ready,
                Candidates =
                [
                    new WindowsCapabilityCandidate
                    {
                        CapabilityId = " ",
                        TargetValue = "target",
                        ExplorationRank = 1,
                        Source = WindowsCapabilityCandidateSource.ExplicitMetadata
                    }
                ]
            }), "A Ready capability plan without stable capability identity must be rejected, not converted into anonymous tuning authority.");

        RequireThrows<ArgumentException>(() => UniversalTuningSystemDimensionFactory.FromWindowsCandidatePlan(
            new WindowsCapabilityCandidatePlan
            {
                CapabilityId = "windows.test.duplicate",
                CurrentValue = "current",
                Disposition = WindowsCapabilityCandidatePlanDisposition.Ready,
                Candidates =
                [
                    new WindowsCapabilityCandidate
                    {
                        CapabilityId = "windows.test.duplicate",
                        TargetValue = "target",
                        ExplorationRank = 1,
                        Source = WindowsCapabilityCandidateSource.ExplicitMetadata
                    },
                    new WindowsCapabilityCandidate
                    {
                        CapabilityId = "windows.test.duplicate",
                        TargetValue = "target",
                        ExplorationRank = 2,
                        Source = WindowsCapabilityCandidateSource.SchemaGenerated
                    }
                ]
            }), "Duplicate Windows target values must be rejected so the bridge never silently rewrites producer search intent.");
    }

    private static UniversalTuningDimension Dimension(
        string id,
        UniversalTuningDimensionScope scope,
        string authorityId,
        params string[] values)
        => new()
        {
            Id = id,
            Scope = scope,
            AuthorityId = authorityId,
            CandidateValues = values
        };

    private static string Signature(UniversalTuningCandidate candidate)
        => string.Join("|", candidate.Values.Select(pair => $"{pair.Key}={pair.Value}"));

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
