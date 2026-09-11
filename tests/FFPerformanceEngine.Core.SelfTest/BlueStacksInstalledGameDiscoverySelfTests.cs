using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Workloads;

internal static class BlueStacksInstalledGameDiscoverySelfTests
{
    [ModuleInitializer]
    internal static void Run()
        => RunAsync().GetAwaiter().GetResult();

    internal static async Task RunAsync()
    {
        const string config = """
            bst.instance.Pie64.adb_port="5565"
            bst.instance.Pie64.enable_adb="1"
            bst.instance.Android11.adb_port="5566"
            bst.instance.Android11.enable_adb="1"
            bst.instance.Disabled.adb_port="5567"
            bst.instance.Disabled.enable_adb="0"
            """;

        var blueStacks = new BlueStacksService();
        var instances = blueStacks.ParseConfig(config);
        var executor = new FakeProcessExecutor();
        var automation = new BlueStacksAutomationService(
            blueStacks,
            executor,
            adbExecutableOverride: @"C:\BlueStacks\HD-Adb.exe",
            playerExecutableOverride: @"C:\BlueStacks\HD-Player.exe");
        var source = new BlueStacksInstalledGameDiscoverySource(
            blueStacks,
            automation,
            () => instances);

        var candidates = await source.DiscoverAsync();

        Require(source.SourceId == "bluestacks-packages" && source.Priority >= 80,
            "BlueStacks package discovery must expose a stable authoritative source identity.");
        Require(candidates.Count == 2,
            "Installed package discovery must return only supported games that were actually observed through ADB.");

        var ff = candidates.Single(candidate => candidate.Identity.GameId == "garena.free-fire");
        Require(ff.Identity.LegacyGameKind == GameKind.FreeFire
                && ff.Identity.AdapterId == "bluestacks.free-fire"
                && ff.Identity.Launcher == GameLauncherKind.BlueStacks,
            "Free Fire package evidence must reuse the existing specialized BlueStacks identity/adapter bridge.");
        Require(ff.Evidence.Contains("Pie64", StringComparison.Ordinal)
                && !ff.Evidence.Contains("Android11", StringComparison.Ordinal),
            "Per-game discovery evidence must name only the instances where that exact package was observed.");

        var ffMax = candidates.Single(candidate => candidate.Identity.GameId == "garena.free-fire-max");
        Require(ffMax.Identity.LegacyGameKind == GameKind.FreeFireMax
                && ffMax.Identity.AdapterId == "bluestacks.free-fire-max",
            "Free Fire MAX package evidence must preserve the specialized adapter identity.");
        Require(ffMax.Evidence.Contains("Pie64", StringComparison.Ordinal)
                && ffMax.Evidence.Contains("Android11", StringComparison.Ordinal),
            "The source must aggregate the same installed game across multiple BlueStacks instances instead of duplicating catalog identities.");

        var packageArgs = BlueStacksAutomationService.BuildInstalledPackagesArguments(instances.Single(instance => instance.Name == "Pie64"));
        Require(packageArgs.SequenceEqual(["-s", "127.0.0.1:5565", "shell", "pm", "list", "packages"]),
            "Installed-package queries must be read-only and scoped to the selected BlueStacks instance endpoint.");

        var parsed = BlueStacksAutomationService.ParseInstalledGames("""
            package:com.dts.freefireth
            package:com.dts.freefiremax
            package:com.dts.freefiremax.fake
            package:com.example.other
            """);
        Require(parsed.SequenceEqual([GameKind.FreeFire, GameKind.FreeFireMax]),
            "Package parsing must require exact supported package names and must not false-match package prefixes.");

        Require(executor.Calls.Any(call => call.Arguments.SequenceEqual(["connect", "127.0.0.1:5565"])),
            "Discovery must explicitly connect to an enabled instance before querying its packages.");
        Require(executor.Calls.Any(call => call.Arguments.SequenceEqual(["connect", "127.0.0.1:5566"])),
            "Discovery must evaluate each ADB-enabled instance independently.");
        Require(!executor.Calls.Any(call => call.Arguments.Any(argument => argument.Contains("5567", StringComparison.Ordinal))),
            "Discovery must not probe an instance whose BlueStacks configuration explicitly disables ADB.");

        Console.WriteLine("PASS Track 3 BlueStacks installed-package discovery reuses exact FF/FF MAX identities without fabrication");
    }

    private sealed class FakeProcessExecutor : IProcessExecutor
    {
        internal List<(string FileName, IReadOnlyList<string> Arguments)> Calls { get; } = [];

        public Task<ProcessExecutionResult> RunAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls.Add((fileName, arguments.ToArray()));

            if (arguments.Count >= 1 && string.Equals(arguments[0], "connect", StringComparison.Ordinal))
                return Task.FromResult(new ProcessExecutionResult(0, "connected", string.Empty));

            if (arguments.SequenceEqual(["-s", "127.0.0.1:5565", "shell", "pm", "list", "packages"]))
                return Task.FromResult(new ProcessExecutionResult(
                    0,
                    "package:com.dts.freefireth\npackage:com.dts.freefiremax\npackage:com.dts.freefiremax.fake\n",
                    string.Empty));

            if (arguments.SequenceEqual(["-s", "127.0.0.1:5566", "shell", "pm", "list", "packages"]))
                return Task.FromResult(new ProcessExecutionResult(
                    0,
                    "package:com.dts.freefiremax\npackage:com.example.other\n",
                    string.Empty));

            return Task.FromResult(new ProcessExecutionResult(1, string.Empty, "unexpected command"));
        }

        public ProcessStartResult StartDetached(string fileName, IReadOnlyList<string> arguments)
            => new(false, null, "Discovery must not start BlueStacks instances.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
