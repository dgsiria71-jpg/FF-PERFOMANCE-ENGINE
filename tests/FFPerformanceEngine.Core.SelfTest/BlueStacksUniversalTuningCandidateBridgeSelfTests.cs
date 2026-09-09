using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Workloads;

internal static class BlueStacksUniversalTuningCandidateBridgeSelfTests
{
    internal static void Run()
    {
        ProjectsGeneratedApplicableCandidatesOneToOne();

        Console.WriteLine("PASS Track 5 dynamic BlueStacks universal candidate binding contract");
    }

    private static void ProjectsGeneratedApplicableCandidatesOneToOne()
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
        var captured = FullCapturedSettings(instance.Name);
        var engine = new AutoTunerEngine();
        var resolver = new GameAdapterResolver(
        [
            new GenericGameAdapter(),
            BlueStacksFreeFireGameAdapter.For(GameKind.FreeFire),
            BlueStacksFreeFireGameAdapter.For(GameKind.FreeFireMax)
        ]);
        var bridge = new BlueStacksUniversalTuningCandidateBridge(engine, resolver);

        var source = engine.GenerateCandidates(environment, instance, AutoTunerMode.Deep);
        var expected = source
            .Where(candidate => BlueStacksAutoTunerRuntime.BuildCandidatePlan(candidate, instance, captured).CanApply)
            .ToArray();

        var projected = bridge.Build(
            environment,
            instance,
            GameKind.FreeFire,
            AutoTunerMode.Deep,
            captured);

        var expectedIdentity = LegacyGameIdentityBridge.FromGameKind(GameKind.FreeFire)
                               ?? throw new InvalidOperationException("Free Fire legacy identity bridge is required.");
        Require(projected.Identity.GameId == expectedIdentity.GameId
                && projected.Identity.AdapterId == expectedIdentity.AdapterId
                && projected.Identity.LegacyGameKind == expectedIdentity.LegacyGameKind
                && projected.Identity.Launcher == expectedIdentity.Launcher
                && projected.Identity.Engine == expectedIdentity.Engine,
            "BlueStacks universal projection must preserve the existing stable Free Fire GameIdentity rather than reconstructing workload identity from runtime evidence.");
        Require(projected.AdapterId == "bluestacks.free-fire",
            "BlueStacks universal projection authority must be the exact resolved Free Fire adapter id.");

        Require(projected.Bindings.Count == expected.Length,
            "BlueStacks universal bindings must contain exactly the generated candidates that the existing runtime candidate planner can represent.");
        Require(projected.Bindings.Count <= source.Count && source.Count <= 96,
            "Universal projection must preserve the existing Deep generator budget and may only filter source candidates, never add candidates.");
        Require(projected.Bindings.Select(binding => binding.SpecializedCandidate).SequenceEqual(expected),
            "Universal projection must preserve the surviving legacy TuningCandidate order exactly.");

        var expectedIds = new[]
        {
            "workload.bluestacks.free-fire.cpu-cores",
            "workload.bluestacks.free-fire.fps-target",
            "workload.bluestacks.free-fire.ram-mb",
            "workload.bluestacks.free-fire.renderer",
            "workload.bluestacks.free-fire.resolution"
        };
        Require(projected.Dimensions.Select(dimension => dimension.Id).SequenceEqual(expectedIds, StringComparer.Ordinal),
            "BlueStacks descriptive workload dimensions must use the stable adapter namespace and deterministic id order.");
        Require(projected.Dimensions.All(dimension => dimension.Scope == UniversalTuningDimensionScope.Workload
                                                      && dimension.AuthorityId == "bluestacks.free-fire"),
            "Every BlueStacks universal workload dimension must carry only the resolved adapter authority.");

        Require(projected.Bindings.Count > 0,
            "The fully allow-listed test instance must expose at least one applicable generated candidate.");
        var first = projected.Bindings[0];
        var specialized = first.SpecializedCandidate;
        var values = first.UniversalCandidate.Values;
        Require(values.Count == 5,
            "Every exact BlueStacks binding must encode the five fields of its specialized TuningCandidate and no hidden axis.");
        Require(values["workload.bluestacks.free-fire.cpu-cores"] == specialized.CpuCores.ToString(System.Globalization.CultureInfo.InvariantCulture)
                && values["workload.bluestacks.free-fire.ram-mb"] == specialized.RamMb.ToString(System.Globalization.CultureInfo.InvariantCulture)
                && values["workload.bluestacks.free-fire.renderer"] == specialized.Renderer
                && values["workload.bluestacks.free-fire.fps-target"] == specialized.FpsTarget.ToString(System.Globalization.CultureInfo.InvariantCulture)
                && values["workload.bluestacks.free-fire.resolution"] == specialized.Resolution,
            "Universal candidate values must losslessly represent the exact specialized candidate without normalizing renderer/resolution text.");

        foreach (var binding in projected.Bindings)
        {
            var candidate = binding.SpecializedCandidate;
            Require(source.Contains(candidate),
                "Every universal binding must correlate to a candidate emitted by the existing AutoTunerEngine generator.");
            Require(BlueStacksAutoTunerRuntime.BuildCandidatePlan(candidate, instance, captured).CanApply,
                "Generated-but-unrepresentable BlueStacks candidates must not enter the universal binding set.");
        }

        var universalKeys = projected.Bindings
            .Select(binding => CanonicalUniversalKey(binding.UniversalCandidate))
            .ToArray();
        Require(universalKeys.Distinct(StringComparer.Ordinal).Count() == universalKeys.Length,
            "One-to-one projection must never collapse distinct specialized candidates onto the same universal candidate.");
    }

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

    private static string CanonicalUniversalKey(UniversalTuningCandidate candidate)
        => string.Join(
            "|",
            candidate.Values
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{pair.Key}={pair.Value}"));

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
