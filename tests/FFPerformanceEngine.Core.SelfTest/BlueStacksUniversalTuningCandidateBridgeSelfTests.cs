using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Workloads;

internal static class BlueStacksUniversalTuningCandidateBridgeSelfTests
{
    internal static void Run()
    {
        ProjectsGeneratedApplicableCandidatesOneToOne();
        RejectsCapturedRendererDriftThatCannotBeApplied();
        FiltersCandidatesByInstalledMutableSurface();
        UsesExactFreeFireMaxIdentityAndNamespace();
        FailsClosedWithoutResolvedSpecializationOrMatchingSnapshot();
        RejectsInvalidInvocation();

        Console.WriteLine("PASS Track 5 dynamic BlueStacks universal candidate binding contract");
    }

    private static void ProjectsGeneratedApplicableCandidatesOneToOne()
    {
        var environment = CreateEnvironment();
        var instance = CreateInstance();
        var captured = FullCapturedSettings(instance.Name);
        var engine = new AutoTunerEngine();
        var bridge = new BlueStacksUniversalTuningCandidateBridge(engine, CreateResolver());

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

        foreach (var dimension in projected.Dimensions)
        {
            var valuesInBindings = projected.Bindings
                .Select(binding => binding.UniversalCandidate.Values[dimension.Id])
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            Require(dimension.CandidateValues.SequenceEqual(valuesInBindings, StringComparer.Ordinal),
                "Descriptive dimension values must be exactly the first-seen values from surviving source bindings, not a separate option catalog.");
        }
    }

    private static void RejectsCapturedRendererDriftThatCannotBeApplied()
    {
        var environment = CreateEnvironment();
        var instance = CreateInstance();
        var captured = FullCapturedSettings(instance.Name);
        captured[$"bst.instance.{instance.Name}.graphics_renderer"] = "\"OpenGL\"";

        var projected = new BlueStacksUniversalTuningCandidateBridge(new AutoTunerEngine(), CreateResolver())
            .Build(environment, instance, GameKind.FreeFire, AutoTunerMode.Deep, captured);

        Require(projected.Bindings.Count == 0 && projected.Dimensions.Count == 0,
            "If the captured installed renderer differs from the generated non-Auto renderer, the bridge must fail closed because the current runtime cannot mutate renderer safely.");
    }

    private static void FiltersCandidatesByInstalledMutableSurface()
    {
        var environment = CreateEnvironment();
        var instance = CreateInstance();
        var bridge = new BlueStacksUniversalTuningCandidateBridge(new AutoTunerEngine(), CreateResolver());

        var withoutCpu = FullCapturedSettings(instance.Name);
        withoutCpu.Remove($"bst.instance.{instance.Name}.cpus");
        var cpuProjected = bridge.Build(environment, instance, GameKind.FreeFire, AutoTunerMode.Deep, withoutCpu);
        Require(cpuProjected.Bindings.Count > 0
                && cpuProjected.Bindings.All(binding => binding.SpecializedCandidate.CpuCores == instance.CpuCores),
            "Missing installed CPU mutation key must remove every generated candidate that changes CPU allocation while preserving representable baseline-CPU candidates.");

        var withoutRam = FullCapturedSettings(instance.Name);
        withoutRam.Remove($"bst.instance.{instance.Name}.ram");
        var ramProjected = bridge.Build(environment, instance, GameKind.FreeFire, AutoTunerMode.Deep, withoutRam);
        Require(ramProjected.Bindings.Count > 0
                && ramProjected.Bindings.All(binding => binding.SpecializedCandidate.RamMb == instance.RamMb),
            "Missing installed RAM mutation key must remove every generated candidate that changes RAM allocation.");

        var withoutFps = FullCapturedSettings(instance.Name);
        withoutFps.Remove($"bst.instance.{instance.Name}.max_fps");
        var fpsProjected = bridge.Build(environment, instance, GameKind.FreeFire, AutoTunerMode.Deep, withoutFps);
        Require(fpsProjected.Bindings.Count > 0
                && fpsProjected.Bindings.All(binding => binding.SpecializedCandidate.FpsTarget == instance.Fps),
            "Missing installed FPS mutation key must remove every generated candidate that changes the FPS target.");

        var incompleteResolution = FullCapturedSettings(instance.Name);
        incompleteResolution.Remove($"bst.instance.{instance.Name}.display_height");
        var resolutionProjected = bridge.Build(environment, instance, GameKind.FreeFire, AutoTunerMode.Deep, incompleteResolution);
        Require(resolutionProjected.Bindings.Count > 0
                && resolutionProjected.Bindings.All(binding => binding.SpecializedCandidate.Resolution == instance.Resolution),
            "Missing one half of the installed resolution mutation pair must remove every generated candidate that changes resolution.");
    }

    private static void UsesExactFreeFireMaxIdentityAndNamespace()
    {
        var instance = CreateInstance();
        var projected = new BlueStacksUniversalTuningCandidateBridge(new AutoTunerEngine(), CreateResolver())
            .Build(CreateEnvironment(), instance, GameKind.FreeFireMax, AutoTunerMode.Adaptive, FullCapturedSettings(instance.Name));

        Require(projected.Identity.GameId == "garena.free-fire-max"
                && projected.Identity.LegacyGameKind == GameKind.FreeFireMax
                && projected.AdapterId == "bluestacks.free-fire-max",
            "Free Fire MAX universal candidate projection must use the existing exact FF MAX stable identity and specialized adapter authority.");
        Require(projected.Bindings.Count > 0
                && projected.Dimensions.All(dimension => dimension.Id.StartsWith("workload.bluestacks.free-fire-max.", StringComparison.Ordinal)
                                                      && dimension.AuthorityId == "bluestacks.free-fire-max")
                && projected.Bindings.All(binding => binding.UniversalCandidate.Values.Keys.All(id => id.StartsWith("workload.bluestacks.free-fire-max.", StringComparison.Ordinal))),
            "Free Fire MAX dimensions and exact bindings must stay inside the FF MAX adapter namespace.");
    }

    private static void FailsClosedWithoutResolvedSpecializationOrMatchingSnapshot()
    {
        var environment = CreateEnvironment();
        var instance = CreateInstance();
        var resolverWithoutFreeFire = new GameAdapterResolver(
        [
            new GenericGameAdapter(),
            BlueStacksFreeFireGameAdapter.For(GameKind.FreeFireMax)
        ]);
        var unresolved = new BlueStacksUniversalTuningCandidateBridge(new AutoTunerEngine(), resolverWithoutFreeFire)
            .Build(environment, instance, GameKind.FreeFire, AutoTunerMode.Deep, FullCapturedSettings(instance.Name));
        Require(unresolved.Bindings.Count == 0 && unresolved.Dimensions.Count == 0 && unresolved.AdapterId == "generic",
            "An unregistered requested Free Fire specialization must resolve to Generic and expose no BlueStacks universal candidate authority.");

        var mismatchedSnapshot = FullCapturedSettings("Android11");
        var mismatched = new BlueStacksUniversalTuningCandidateBridge(new AutoTunerEngine(), CreateResolver())
            .Build(environment, instance, GameKind.FreeFire, AutoTunerMode.Deep, mismatchedSnapshot);
        Require(mismatched.Bindings.Count == 0 && mismatched.Dimensions.Count == 0,
            "Captured settings from another BlueStacks instance must never authorize candidate bindings for the selected instance.");

        var empty = new BlueStacksUniversalTuningCandidateBridge(new AutoTunerEngine(), CreateResolver())
            .Build(environment, instance, GameKind.FreeFire, AutoTunerMode.Deep, new Dictionary<string, string>());
        Require(empty.Bindings.Count == 0 && empty.Dimensions.Count == 0,
            "Missing captured allow-listed settings must produce no universal BlueStacks candidate space.");
    }

    private static void RejectsInvalidInvocation()
    {
        var bridge = new BlueStacksUniversalTuningCandidateBridge(new AutoTunerEngine(), CreateResolver());
        var instance = CreateInstance();

        ExpectThrows<ArgumentOutOfRangeException>(
            () => bridge.Build(CreateEnvironment(), instance, GameKind.None, AutoTunerMode.Deep, FullCapturedSettings(instance.Name)),
            "Unsupported games must be rejected instead of borrowing Free Fire workload identity.");
        ExpectThrows<ArgumentException>(
            () => bridge.Build(CreateEnvironment(), instance with { Name = " " }, GameKind.FreeFire, AutoTunerMode.Deep, FullCapturedSettings(instance.Name)),
            "A named BlueStacks instance is required to bind captured config evidence to candidate space.");
    }

    private static EnvironmentSnapshot CreateEnvironment()
        => new()
        {
            LogicalProcessors = 8,
            MemoryTotalGb = 16
        };

    private static BlueStacksInstance CreateInstance()
        => new()
        {
            Name = "Pie64",
            CpuCores = 4,
            RamMb = 4096,
            Renderer = "Vulkan",
            Fps = 90,
            Resolution = "1920x1080"
        };

    private static GameAdapterResolver CreateResolver()
        => new(
        [
            new GenericGameAdapter(),
            BlueStacksFreeFireGameAdapter.For(GameKind.FreeFire),
            BlueStacksFreeFireGameAdapter.For(GameKind.FreeFireMax)
        ]);

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

    private static void ExpectThrows<TException>(Action action, string message)
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
