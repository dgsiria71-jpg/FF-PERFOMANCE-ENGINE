using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class SessionStateMonitorSelfTests
{
    [ModuleInitializer]
    internal static void Run()
    {
        RunAsync().GetAwaiter().GetResult();
    }

    private static async Task RunAsync()
    {
        var foregroundCalls = 0;
        var desktop = new SessionStateMonitor(
            new GameStateDetector(),
            playerRunningProbe: () => false,
            foregroundGameProbe: _ => { foregroundCalls++; return Task.FromResult(GameKind.FreeFireMax); },
            frameProbe: _ => Task.FromResult<TelemetrySample?>(new TelemetrySample { Fps = 90 }),
            recentInputProbe: () => true);

        var desktopObservation = await desktop.CaptureAsync();
        Require(desktopObservation.State == GameState.Desktop, "Session monitor must report Desktop when BlueStacks is not running.");
        Require(foregroundCalls == 0, "Session monitor must not query ADB when BlueStacks is not running.");

        var match = new SessionStateMonitor(
            new GameStateDetector(),
            playerRunningProbe: () => true,
            foregroundGameProbe: _ => Task.FromResult(GameKind.FreeFireMax),
            frameProbe: _ => Task.FromResult<TelemetrySample?>(new TelemetrySample { Fps = 90, FrameTimeP95Ms = 12.0, FrameTimeP99Ms = 13.5 }),
            recentInputProbe: () => true);

        var matchObservation = await match.CaptureAsync();
        Require(matchObservation.State == GameState.Match, "Sustained game rendering plus recent input must classify as Match.");
        Require(matchObservation.ActiveGame == GameKind.FreeFireMax, "Session monitor must preserve the observed foreground game.");
        Require(matchObservation.Signals.Fps == 90, "Session monitor must preserve measured frame evidence.");

        var lobby = new SessionStateMonitor(
            new GameStateDetector(),
            playerRunningProbe: () => true,
            foregroundGameProbe: _ => Task.FromResult(GameKind.FreeFire),
            frameProbe: _ => Task.FromResult<TelemetrySample?>(new TelemetrySample { Fps = 30 }),
            recentInputProbe: () => false);

        var lobbyObservation = await lobby.CaptureAsync();
        Require(lobbyObservation.State == GameState.Lobby, "Foreground game without recent interaction evidence must remain Lobby.");

        Console.WriteLine("PASS live session state monitoring behavior");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
