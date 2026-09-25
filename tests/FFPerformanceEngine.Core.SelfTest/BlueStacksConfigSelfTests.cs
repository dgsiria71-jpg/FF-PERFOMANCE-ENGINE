using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class BlueStacksConfigSelfTests
{
    [ModuleInitializer]
    internal static void Run()
    {
        const string config = """
            # keep this comment
            bst.instance.Pie64.cpus="6"
            bst.instance.Pie64.ram="6144"
            bst.instance.Pie64.max_fps="90"
            bst.instance.Pie64.enable_high_fps="1"
            bst.instance.Pie64.fb_width="1920"
            bst.instance.Pie64.fb_height="1080"
            bst.instance.Pie64.dpi="240"
            bst.instance.Pie64.graphics_renderer="gl"
            bst.instance.Android11.cpus="4"
            bst.instance.Android11.ram="4096"
            """;

        var edited = BlueStacksService.UpdateInstanceConfigText(config, "Pie64", new Dictionary<string, string>
        {
            ["cpus"] = "8",
            ["ram"] = "8192",
            ["max_fps"] = "144"
        });
        var service = new BlueStacksService();
        var instances = service.ParseConfig(edited);
        var pie = instances.Single(x => x.Name == "Pie64");
        var android11 = instances.Single(x => x.Name == "Android11");
        Require(pie.CpuCores == 8 && pie.RamMb == 8192 && pie.Fps == 144, "BlueStacks editor must update the selected instance.");
        Require(android11.CpuCores == 4 && android11.RamMb == 4096, "BlueStacks editor must not modify other instances.");
        Require(edited.Contains("# keep this comment", StringComparison.Ordinal), "BlueStacks editor must preserve unrelated configuration.");

        var rejected = false;
        try { _ = BlueStacksService.UpdateInstanceConfigText(config, "Pie64", new Dictionary<string, string> { ["enable_root_access"] = "1" }); }
        catch (ArgumentException) { rejected = true; }
        Require(rejected, "BlueStacks editor must reject settings outside the mutation allow-list.");

        var captured = BlueStacksService.CaptureAllowedSettingsFromText(config, "Pie64");
        var profile = new PerformanceProfile
        {
            Name = "Validated",
            Kind = ProfileKind.Recommended,
            Evidence = EvidenceLevel.Validated,
            CpuCores = 8,
            RamMb = 8192,
            FpsTarget = 144,
            Resolution = "1600x900",
            Dpi = 240,
            Renderer = "Vulkan"
        };
        var planService = new ProfileApplicationService(service, new SnapshotService(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json")), new HistoryService(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json")));
        var plan = planService.BuildPlan(profile, pie, captured);
        Require(plan.RestartRequiredSettings["max_fps"] == "144", "Profile plan must select the existing max_fps alias.");
        Require(plan.RestartRequiredSettings["fb_width"] == "1600" && plan.RestartRequiredSettings["fb_height"] == "900", "Profile plan must select existing framebuffer resolution aliases.");
        Require(plan.AssistedChanges.Count == 1, "Renderer mutation must remain assisted when raw mapping is unknown.");

        Console.WriteLine("PASS BlueStacks configuration editing and profile application planning");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
