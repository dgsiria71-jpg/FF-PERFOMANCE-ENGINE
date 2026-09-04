using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public static class BlueStacksAutomationService
{
    private const string FreeFirePackage = "com.dts.freefireth";
    private const string FreeFireMaxPackage = "com.dts.freefiremax";

    public static string PackageFor(GameKind game) => game switch
    {
        GameKind.FreeFire => FreeFirePackage,
        GameKind.FreeFireMax => FreeFireMaxPackage,
        _ => throw new ArgumentOutOfRangeException(nameof(game), game, "Only Free Fire and Free Fire MAX have Android package mappings.")
    };

    public static string EndpointFor(BlueStacksInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        if (instance.AdbPort is not (>= 1 and <= 65535))
            throw new InvalidOperationException($"BlueStacks instance '{instance.Name}' does not expose a valid ADB port.");
        return $"127.0.0.1:{instance.AdbPort.Value}";
    }

    public static IReadOnlyList<string> BuildPlayerArguments(BlueStacksInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        if (string.IsNullOrWhiteSpace(instance.Name))
            throw new InvalidOperationException("BlueStacks instance name is unavailable.");
        return ["--instance", instance.Name];
    }

    public static IReadOnlyList<string> BuildConnectArguments(BlueStacksInstance instance)
        => ["connect", EndpointFor(instance)];

    public static IReadOnlyList<string> BuildLaunchGameArguments(BlueStacksInstance instance, GameKind game)
        => ["-s", EndpointFor(instance), "shell", "monkey", "-p", PackageFor(game), "-c", "android.intent.category.LAUNCHER", "1"];

    public static IReadOnlyList<string> BuildForegroundQueryArguments(BlueStacksInstance instance)
        => ["-s", EndpointFor(instance), "shell", "dumpsys", "window", "windows"];

    public static GameKind ParseForegroundGame(string? dumpsysOutput)
    {
        if (string.IsNullOrWhiteSpace(dumpsysOutput)) return GameKind.None;
        if (dumpsysOutput.Contains(FreeFireMaxPackage, StringComparison.OrdinalIgnoreCase)) return GameKind.FreeFireMax;
        if (dumpsysOutput.Contains(FreeFirePackage, StringComparison.OrdinalIgnoreCase)) return GameKind.FreeFire;
        return GameKind.None;
    }
}
