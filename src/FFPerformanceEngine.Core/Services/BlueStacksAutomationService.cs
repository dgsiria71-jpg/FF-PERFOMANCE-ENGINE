using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed record AutomationActionResult(bool Success, string Message, string StandardOutput = "", string StandardError = "");

public sealed class BlueStacksAutomationService
{
    private const string FreeFirePackage = "com.dts.freefireth";
    private const string FreeFireMaxPackage = "com.dts.freefiremax";
    private readonly BlueStacksService _blueStacks;
    private readonly IProcessExecutor _processExecutor;
    private readonly string? _adbExecutableOverride;
    private readonly string? _playerExecutableOverride;
    private readonly Func<bool> _playerRunningProbe;

    public BlueStacksAutomationService(
        BlueStacksService blueStacks,
        IProcessExecutor? processExecutor = null,
        string? adbExecutableOverride = null,
        string? playerExecutableOverride = null,
        Func<bool>? playerRunningProbe = null)
    {
        _blueStacks = blueStacks ?? throw new ArgumentNullException(nameof(blueStacks));
        _processExecutor = processExecutor ?? new ProcessExecutor();
        _adbExecutableOverride = adbExecutableOverride;
        _playerExecutableOverride = playerExecutableOverride;
        _playerRunningProbe = playerRunningProbe ?? _blueStacks.IsPlayerRunning;
    }

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

    public string? FindAdbExecutable()
    {
        if (!string.IsNullOrWhiteSpace(_adbExecutableOverride)) return _adbExecutableOverride;
        var player = ResolvePlayerExecutable();
        if (string.IsNullOrWhiteSpace(player)) return null;
        var directory = Path.GetDirectoryName(player);
        if (string.IsNullOrWhiteSpace(directory)) return null;
        var candidates = new[] { Path.Combine(directory, "HD-Adb.exe"), Path.Combine(directory, "adb.exe") };
        return candidates.FirstOrDefault(File.Exists);
    }

    public ProcessStartResult StartInstance(BlueStacksInstance instance)
    {
        var player = ResolvePlayerExecutable();
        if (string.IsNullOrWhiteSpace(player)) return new(false, null, "BlueStacks player executable was not found.");
        return _processExecutor.StartDetached(player, BuildPlayerArguments(instance));
    }

    public async Task<AutomationActionResult> ConnectAsync(BlueStacksInstance instance, CancellationToken cancellationToken = default)
    {
        if (instance.AdbEnabled == false) return new(false, "ADB is disabled for the selected BlueStacks instance.");
        var adb = FindAdbExecutable();
        if (string.IsNullOrWhiteSpace(adb)) return new(false, "BlueStacks ADB executable was not found.");
        var result = await _processExecutor.RunAsync(adb, BuildConnectArguments(instance), TimeSpan.FromSeconds(8), cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, result.Success ? "ADB connection ready." : "ADB connection failed.");
    }

    public async Task<GameKind> QueryForegroundGameAsync(BlueStacksInstance instance, CancellationToken cancellationToken = default)
    {
        if (instance.AdbEnabled == false) return GameKind.None;
        var adb = FindAdbExecutable();
        if (string.IsNullOrWhiteSpace(adb)) return GameKind.None;
        var result = await _processExecutor.RunAsync(adb, BuildForegroundQueryArguments(instance), TimeSpan.FromSeconds(6), cancellationToken).ConfigureAwait(false);
        return result.Success ? ParseForegroundGame(result.StandardOutput) : GameKind.None;
    }

    public async Task<AutomationActionResult> LaunchGameAsync(BlueStacksInstance instance, GameKind game, CancellationToken cancellationToken = default)
    {
        if (instance.AdbEnabled == false) return new(false, "ADB is disabled for the selected BlueStacks instance.");
        var adb = FindAdbExecutable();
        if (string.IsNullOrWhiteSpace(adb)) return new(false, "BlueStacks ADB executable was not found.");
        var result = await _processExecutor.RunAsync(adb, BuildLaunchGameArguments(instance, game), TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, result.Success ? $"{game} launch request sent." : $"{game} launch request failed.");
    }

    public async Task<bool> WaitForForegroundGameAsync(BlueStacksInstance instance, GameKind game, TimeSpan timeout, CancellationToken cancellationToken = default)
        => await WaitForForegroundGameAsync(instance, game, timeout, TimeSpan.FromMilliseconds(750), cancellationToken).ConfigureAwait(false);

    public async Task<AutomationActionResult> PrepareGameAsync(
        BlueStacksInstance instance,
        GameKind game,
        TimeSpan foregroundTimeout,
        TimeSpan startupDelay,
        TimeSpan pollInterval,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);
        if (game is not (GameKind.FreeFire or GameKind.FreeFireMax))
            return new(false, "Select Free Fire or Free Fire MAX before preparing the game session.");
        if (instance.AdbEnabled == false)
            return new(false, "ADB is disabled for the selected BlueStacks instance. Assisted mode is required.");

        if (!_playerRunningProbe())
        {
            var started = StartInstance(instance);
            if (!started.Success)
                return new(false, $"BlueStacks instance could not be started: {started.Error ?? "unknown error"}");
            if (startupDelay > TimeSpan.Zero)
                await Task.Delay(startupDelay, cancellationToken).ConfigureAwait(false);
        }

        var connected = await ConnectAsync(instance, cancellationToken).ConfigureAwait(false);
        if (!connected.Success) return connected;

        var launched = await LaunchGameAsync(instance, game, cancellationToken).ConfigureAwait(false);
        if (!launched.Success) return launched;

        var foreground = await WaitForForegroundGameAsync(instance, game, foregroundTimeout, pollInterval, cancellationToken).ConfigureAwait(false);
        return foreground
            ? new(true, $"{game} is running in the foreground and ready for measurement.")
            : new(false, $"{game} did not become the foreground app before the preparation timeout.");
    }

    private async Task<bool> WaitForForegroundGameAsync(
        BlueStacksInstance instance,
        GameKind game,
        TimeSpan timeout,
        TimeSpan pollInterval,
        CancellationToken cancellationToken)
    {
        if (timeout < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        if (pollInterval < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(pollInterval));

        var deadline = DateTimeOffset.UtcNow + timeout;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await QueryForegroundGameAsync(instance, cancellationToken).ConfigureAwait(false) == game) return true;
            if (DateTimeOffset.UtcNow >= deadline) break;
            if (pollInterval > TimeSpan.Zero)
                await Task.Delay(pollInterval, cancellationToken).ConfigureAwait(false);
        }
        while (DateTimeOffset.UtcNow < deadline);

        return false;
    }

    private string? ResolvePlayerExecutable()
        => !string.IsNullOrWhiteSpace(_playerExecutableOverride) ? _playerExecutableOverride : _blueStacks.FindPlayerExecutable();

    private static AutomationActionResult ToActionResult(ProcessExecutionResult result, string message)
        => new(result.Success, result.TimedOut ? message + " Command timed out." : message, result.StandardOutput, result.StandardError);
}
