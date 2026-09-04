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

    public static GameKind ParseForegroundGame(string? dumpsysOutput)
    {
        if (string.IsNullOrWhiteSpace(dumpsysOutput)) return GameKind.None;
        if (dumpsysOutput.Contains(FreeFireMaxPackage, StringComparison.OrdinalIgnoreCase)) return GameKind.FreeFireMax;
        if (dumpsysOutput.Contains(FreeFirePackage, StringComparison.OrdinalIgnoreCase)) return GameKind.FreeFire;
        return GameKind.None;
    }
}
