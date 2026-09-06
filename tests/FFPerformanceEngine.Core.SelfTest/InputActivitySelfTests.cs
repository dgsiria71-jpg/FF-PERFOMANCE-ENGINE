using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Services;

internal static class InputActivitySelfTests
{
    [ModuleInitializer]
    internal static void Run()
    {
        Require(InputActivityService.IsRecentTick(nowTick: 10_000, lastInputTick: 9_200, thresholdMs: 1_000), "Input inside threshold must be recent.");
        Require(!InputActivityService.IsRecentTick(nowTick: 10_000, lastInputTick: 8_000, thresholdMs: 1_000), "Input outside threshold must not be recent.");
        Require(InputActivityService.IsRecentTick(nowTick: 100, lastInputTick: uint.MaxValue - 200, thresholdMs: 500), "Tick wrap-around must not break recent input detection.");
        Require(InputActivityService.IsBlueStacksProcessName("HD-Player"), "HD-Player must be recognized as a BlueStacks player process.");
        Require(InputActivityService.IsBlueStacksProcessName("BlueStacksAppplayer"), "BlueStacksAppplayer must be recognized as a BlueStacks player process.");
        Require(!InputActivityService.IsBlueStacksProcessName("explorer"), "Unrelated foreground processes must not be treated as BlueStacks.");
        Console.WriteLine("PASS recent input and foreground process rules");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
