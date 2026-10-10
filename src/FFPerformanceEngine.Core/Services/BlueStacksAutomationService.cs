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

    public static IReadOnlyList<string> BuildInstalledPackagesArguments(BlueStacksInstance instance)
        => ["-s", EndpointFor(instance), "shell", "pm", "list", "packages"];

    public static IReadOnlyList<string> BuildPackageDetailsArguments(
        BlueStacksInstance instance,
        GameKind game)
        => ["-s", EndpointFor(instance), "shell", "dumpsys", "package", PackageFor(game)];

    public static string? ParsePackageVersion(string? dumpsysOutput)
    {
        if (string.IsNullOrWhiteSpace(dumpsysOutput)) return null;
        foreach (var rawLine in dumpsysOutput.Split(
                     ['\r', '\n'],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var line = rawLine.Trim();
            const string prefix = "versionName=";
            if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            var value = line[prefix.Length..].Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return null;
    }

    public static GameKind ParseForegroundGame(string? dumpsysOutput)
    {
        if (string.IsNullOrWhiteSpace(dumpsysOutput))
            return GameKind.None;

        // Window dumps include historical/inactive windows and stale package
        // mentions. Only the highest-priority available focus record may prove
        // foreground. An explicit non-game/null current focus must not fall back
        // to an older mFocusedApp or resumed activity.
        var lines = dumpsysOutput.Split(
            ['\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var key in new[]
                 {
                     "mCurrentFocus",
                     "mFocusedApp",
                     "topResumedActivity",
                     "mResumedActivity"
                 })
        {
            var matching = lines
                .Where(line => line.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase)
                               || line.StartsWith(key + ":", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matching.Length == 0) continue;

            GameKind? observed = null;
            foreach (var record in matching)
            {
                var component = System.Text.RegularExpressions.Regex.Match(
                    record,
                    @"\bu[0-9]+\s+(com\.dts\.(?:freefiremax|freefireth))/",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
                if (!component.Success)
                    return GameKind.None;

                var package = component.Groups[1].Value;
                var game = string.Equals(package, FreeFireMaxPackage,
                    StringComparison.OrdinalIgnoreCase)
                    ? GameKind.FreeFireMax
                    : GameKind.FreeFire;
                if (observed is not null && observed.Value != game)
                    return GameKind.None;
                observed = game;
            }

            return observed ?? GameKind.None;
        }

        return GameKind.None;
    }

    public static IReadOnlyList<GameKind> ParseInstalledGames(string? packageListOutput)
    {
        if (string.IsNullOrWhiteSpace(packageListOutput)) return Array.Empty<GameKind>();

        var freeFire = false;
        var freeFireMax = false;
        foreach (var rawLine in packageListOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var line = rawLine.Trim();
            const string prefix = "package:";
            if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            var package = line[prefix.Length..].Trim();
            if (string.Equals(package, FreeFirePackage, StringComparison.OrdinalIgnoreCase)) freeFire = true;
            else if (string.Equals(package, FreeFireMaxPackage, StringComparison.OrdinalIgnoreCase)) freeFireMax = true;
        }

        var games = new List<GameKind>(2);
        if (freeFire) games.Add(GameKind.FreeFire);
        if (freeFireMax) games.Add(GameKind.FreeFireMax);
        return games;
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

    public async Task<IReadOnlyList<GameKind>> QueryInstalledGamesAsync(BlueStacksInstance instance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);
        if (instance.AdbEnabled == false) return Array.Empty<GameKind>();
        var adb = FindAdbExecutable();
        if (string.IsNullOrWhiteSpace(adb)) return Array.Empty<GameKind>();

        var result = await _processExecutor.RunAsync(
            adb,
            BuildInstalledPackagesArguments(instance),
            TimeSpan.FromSeconds(8),
            cancellationToken).ConfigureAwait(false);
        if (result.Success)
            return ParseInstalledGames(result.StandardOutput);

        // Some BlueStacks HD-Adb builds close the shell while enumerating the
        // full package list even though exact package dumps remain available.
        // Fallback is deliberately limited to the two supported exact package
        // identities and requires a real versionName from dumpsys package.
        var installed = new List<GameKind>(2);
        foreach (var game in new[] { GameKind.FreeFire, GameKind.FreeFireMax })
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await QueryPackageVersionAsync(instance, game, cancellationToken).ConfigureAwait(false) is not null)
                installed.Add(game);
        }

        return installed;
    }

    public async Task<string?> QueryPackageVersionAsync(
        BlueStacksInstance instance,
        GameKind game,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);
        if (instance.AdbEnabled == false) return null;
        if (game is not (GameKind.FreeFire or GameKind.FreeFireMax)) return null;
        var adb = FindAdbExecutable();
        if (string.IsNullOrWhiteSpace(adb)) return null;
        var result = await _processExecutor.RunAsync(
            adb,
            BuildPackageDetailsArguments(instance, game),
            TimeSpan.FromSeconds(8),
            cancellationToken).ConfigureAwait(false);
        return result.Success ? ParsePackageVersion(result.StandardOutput) : null;
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
