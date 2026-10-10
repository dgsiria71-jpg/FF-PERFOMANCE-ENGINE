using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class BlueStacksAutomationSelfTests
{
    [ModuleInitializer]
    internal static void Run()
    {
        const string config = """
            bst.instance.Pie64.cpus="6"
            bst.instance.Pie64.ram="6144"
            bst.instance.Pie64.adb_port="5565"
            bst.instance.Pie64.enable_adb="1"
            """;

        var blueStacks = new BlueStacksService();
        var instance = blueStacks.ParseConfig(config).Single();
        Require(instance.AdbPort == 5565, "BlueStacks parser must expose the per-instance ADB port.");
        Require(instance.AdbEnabled == true, "BlueStacks parser must expose whether ADB is enabled.");

        Require(BlueStacksAutomationService.PackageFor(GameKind.FreeFire) == "com.dts.freefireth", "Free Fire package mapping must be exact.");
        Require(BlueStacksAutomationService.PackageFor(GameKind.FreeFireMax) == "com.dts.freefiremax", "Free Fire MAX package mapping must be exact.");
        Require(BlueStacksAutomationService.EndpointFor(instance) == "127.0.0.1:5565", "ADB endpoint must be derived from the instance port.");

        var playerArgs = BlueStacksAutomationService.BuildPlayerArguments(instance);
        Require(playerArgs.SequenceEqual(["--instance", "Pie64"]), "Player launch arguments must target exactly the selected instance.");

        var connectArgs = BlueStacksAutomationService.BuildConnectArguments(instance);
        Require(connectArgs.SequenceEqual(["connect", "127.0.0.1:5565"]), "ADB connect arguments must target only the selected instance endpoint.");

        var launchArgs = BlueStacksAutomationService.BuildLaunchGameArguments(instance, GameKind.FreeFireMax);
        Require(launchArgs.SequenceEqual(["-s", "127.0.0.1:5565", "shell", "monkey", "-p", "com.dts.freefiremax", "-c", "android.intent.category.LAUNCHER", "1"]), "Game launch must use an explicit package and selected ADB endpoint.");

        var foregroundArgs = BlueStacksAutomationService.BuildForegroundQueryArguments(instance);
        Require(foregroundArgs.SequenceEqual(["-s", "127.0.0.1:5565", "shell", "dumpsys", "window", "windows"]), "Foreground query must be read-only and scoped to the selected instance.");

        const string freeFireWindow = "mCurrentFocus=Window{42 u0 com.dts.freefireth/com.dts.freefireth.FFMainActivity}";
        const string maxWindow = "mResumedActivity: ActivityRecord{1 u0 com.dts.freefiremax/com.dts.freefiremax.FFMainActivity t2}";
        Require(BlueStacksAutomationService.ParseForegroundGame(freeFireWindow) == GameKind.FreeFire, "Foreground parser must detect Free Fire.");
        Require(BlueStacksAutomationService.ParseForegroundGame(maxWindow) == GameKind.FreeFireMax, "Foreground parser must detect Free Fire MAX.");
        Require(BlueStacksAutomationService.ParseForegroundGame("mCurrentFocus=Window{42 u0 com.android.launcher/com.android.launcher2.Launcher}") == GameKind.None, "Foreground parser must not invent a game.");
        const string staleMax =
            "mCurrentFocus=Window{9 u0 com.android.launcher/.Launcher}" + "\n"
            + "Window #3 Window{1 u0 com.dts.freefiremax/.FFMainActivity}" + "\n"
            + "mFocusedApp=ActivityRecord{12 u0 com.dts.freefiremax/.FFMainActivity}";
        Require(BlueStacksAutomationService.ParseForegroundGame(staleMax) == GameKind.None,
            "A historical Free Fire window or stale mFocusedApp must never override an explicit non-game current focus.");
        Require(BlueStacksAutomationService.ParseForegroundGame(
            "mCurrentFocus=Window{9 u0 com.other.example/.Main} freefiremax com.dts.freefireth") == GameKind.None,
            "Unrelated focus with an embedded game package must never authorize game foreground.");
        Require(BlueStacksAutomationService.ParseForegroundGame(
            "Window #3 Window{1 u0 com.dts.freefireth/.FFMainActivity}") == GameKind.None,
            "Stale window-list entries are not foreground evidence.");
        Require(BlueStacksAutomationService.ParseForegroundGame(
            "mCurrentFocus=null" + "\n"
            + "mResumedActivity: ActivityRecord{9 u0 com.dts.freefireth/.FFMainActivity}") == GameKind.None,
            "Explicit null current focus must fail closed instead of falling back to stale resumed activity.");
        Require(BlueStacksAutomationService.ParseForegroundGame(
            "mFocusedApp=AppWindowToken{9 ActivityRecord{2 u0 com.dts.freefiremax/.FFMainActivity}}")
            == GameKind.FreeFireMax,
            "A focused-app record is a permissible fallback only when there is no current-focus record.");
        Require(BlueStacksAutomationService.ParseForegroundGame(
            "mCurrentFocus=Window{9 u0 com.dts.freefiremax.evil/.Main}")
            == GameKind.None,
            "Package identifiers must match the exact supported component, not prefixes.");


        var detector = new GameStateDetector();
        Require(detector.Infer(new GameStateSignals { BlueStacksRunning = false }) == GameState.Desktop, "No player process must classify as Desktop.");
        Require(detector.Infer(new GameStateSignals { BlueStacksRunning = true, ForegroundGame = GameKind.None }) == GameState.BlueStacksReady, "Running player without game must classify as BlueStacksReady.");
        Require(detector.Infer(new GameStateSignals { BlueStacksRunning = true, ForegroundGame = GameKind.FreeFireMax, RecentInput = false, Fps = 30 }) == GameState.Lobby, "Foreground game without interaction evidence must remain Lobby.");
        Require(detector.Infer(new GameStateSignals { BlueStacksRunning = true, ForegroundGame = GameKind.FreeFireMax, RecentInput = true, Fps = 90, FrameTimeVarianceMs = 2.5 }) == GameState.Match, "Foreground game plus sustained rendering and input must classify as Match.");

        Console.WriteLine("PASS BlueStacks ADB automation and conservative state detection contracts");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
