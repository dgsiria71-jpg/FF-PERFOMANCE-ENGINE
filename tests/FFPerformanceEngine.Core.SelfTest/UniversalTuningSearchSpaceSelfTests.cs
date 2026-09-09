using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class UniversalTuningSearchSpaceSelfTests
{
    internal static void Run()
    {
        RejectInvalidDimensions();
        EmptySpaceProducesNoCandidate();
        DeterministicCartesianProduct();
        CandidateBudgetIsDeterministic();
        LegacyBlueStacksCandidateRemainsUnchanged();

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
