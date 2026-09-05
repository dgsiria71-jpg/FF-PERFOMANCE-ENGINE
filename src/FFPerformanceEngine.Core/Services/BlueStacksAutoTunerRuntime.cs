using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed record OwnedProcessStopResult(bool Success, string Message);

public interface IOwnedProcessController
{
    Task<OwnedProcessStopResult> StopVerifiedAsync(
        int processId,
        string expectedExecutablePath,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}

public sealed class OwnedProcessController : IOwnedProcessController
{
    public async Task<OwnedProcessStopResult> StopVerifiedAsync(
        int processId,
        string expectedExecutablePath,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (processId <= 0) return new(false, "Owned process id is invalid.");
        if (string.IsNullOrWhiteSpace(expectedExecutablePath)) return new(false, "Expected BlueStacks executable path is unavailable.");
        if (timeout < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));

        Process process;
        try
        {
            process = Process.GetProcessById(processId);
        }
        catch (ArgumentException)
        {
            return new(true, "Owned BlueStacks process already exited.");
        }

        using (process)
        {
            if (process.HasExited) return new(true, "Owned BlueStacks process already exited.");

            string? actualExecutable;
            try
            {
                actualExecutable = process.MainModule?.FileName;
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or NotSupportedException)
            {
                return new(false, $"Could not verify the executable for owned PID {processId}: {ex.Message}");
            }

            if (!PathsEqual(actualExecutable, expectedExecutablePath))
                return new(false, $"Refusing to stop PID {processId}: executable identity does not match the BlueStacks player started by this tuning session.");

            cancellationToken.ThrowIfCancellationRequested();

            var gracefulRequested = false;
            try { gracefulRequested = process.CloseMainWindow(); }
            catch (InvalidOperationException) { return new(true, "Owned BlueStacks process already exited."); }

            if (gracefulRequested && await WaitForExitAsync(process, timeout, cancellationToken).ConfigureAwait(false))
                return new(true, "Owned BlueStacks player closed gracefully.");

            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                return new(true, "Owned BlueStacks process already exited.");
            }
            catch (Exception ex) when (ex is Win32Exception or NotSupportedException)
            {
                return new(false, $"Verified BlueStacks process could not be stopped: {ex.Message}");
            }

            return await WaitForExitAsync(process, timeout, cancellationToken).ConfigureAwait(false)
                ? new(true, "Owned BlueStacks player stopped after graceful shutdown timed out.")
                : new(false, "Verified BlueStacks process did not exit before the stop timeout.");
        }
    }

    internal static bool PathsEqual(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return false;
        try
        {
            return string.Equals(
                Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private static async Task<bool> WaitForExitAsync(Process process, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (process.HasExited) return true;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);
        try
        {
            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return process.HasExited;
        }
    }
}

public sealed record AutoTunerRuntimeOptions
{
    public TimeSpan StartupSettleDelay { get; init; } = TimeSpan.FromSeconds(8);
    public TimeSpan ForegroundTimeout { get; init; } = TimeSpan.FromSeconds(45);
    public TimeSpan BenchmarkDuration { get; init; } = TimeSpan.FromSeconds(12);
    public TimeSpan ProcessStopTimeout { get; init; } = TimeSpan.FromSeconds(5);
}

public interface IBlueStacksAutoTunerPlatform
{
    bool IsPlayerRunning();
    IReadOnlyDictionary<string, string> CaptureAllowedSettings(string instanceName);
    BlueStacksConfigWriteResult ApplyInstanceSettings(string instanceName, IReadOnlyDictionary<string, string> updates);
    BlueStacksConfigWriteResult RestoreBackup(string backupPath);
    string? FindPlayerExecutable();
    ProcessStartResult StartInstance(BlueStacksInstance instance);
    Task<AutomationActionResult> PrepareRunningGameAsync(
        BlueStacksInstance instance,
        GameKind game,
        TimeSpan foregroundTimeout,
        CancellationToken cancellationToken = default);
    Task<TelemetrySample?> CaptureBenchmarkAsync(TimeSpan duration, CancellationToken cancellationToken = default);
    Task<OwnedProcessStopResult> StopOwnedPlayerAsync(
        int processId,
        string expectedExecutablePath,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}

public sealed class BlueStacksAutoTunerPlatform : IBlueStacksAutoTunerPlatform
{
    private readonly BlueStacksService _blueStacks;
    private readonly BlueStacksAutomationService _automation;
    private readonly PresentMonService _presentMon;
    private readonly IOwnedProcessController _processController;

    public BlueStacksAutoTunerPlatform(
        BlueStacksService blueStacks,
        BlueStacksAutomationService automation,
        PresentMonService presentMon,
        IOwnedProcessController? processController = null)
    {
        _blueStacks = blueStacks ?? throw new ArgumentNullException(nameof(blueStacks));
        _automation = automation ?? throw new ArgumentNullException(nameof(automation));
        _presentMon = presentMon ?? throw new ArgumentNullException(nameof(presentMon));
        _processController = processController ?? new OwnedProcessController();
    }

    public bool IsPlayerRunning() => _blueStacks.IsPlayerRunning();

    public IReadOnlyDictionary<string, string> CaptureAllowedSettings(string instanceName)
        => _blueStacks.CaptureAllowedSettings(instanceName);

    public BlueStacksConfigWriteResult ApplyInstanceSettings(string instanceName, IReadOnlyDictionary<string, string> updates)
        => _blueStacks.ApplyInstanceSettings(instanceName, updates);

    public BlueStacksConfigWriteResult RestoreBackup(string backupPath)
        => _blueStacks.RestoreBackup(backupPath);

    public string? FindPlayerExecutable() => _blueStacks.FindPlayerExecutable();

    public ProcessStartResult StartInstance(BlueStacksInstance instance)
        => _automation.StartInstance(instance);

    public async Task<AutomationActionResult> PrepareRunningGameAsync(
        BlueStacksInstance instance,
        GameKind game,
        TimeSpan foregroundTimeout,
        CancellationToken cancellationToken = default)
    {
        var connected = await _automation.ConnectAsync(instance, cancellationToken).ConfigureAwait(false);
        if (!connected.Success) return connected;

        var launched = await _automation.LaunchGameAsync(instance, game, cancellationToken).ConfigureAwait(false);
        if (!launched.Success) return launched;

        var foreground = await _automation.WaitForForegroundGameAsync(instance, game, foregroundTimeout, cancellationToken).ConfigureAwait(false);
        return foreground
            ? new(true, $"{game} is foreground and ready for benchmark.")
            : new(false, $"{game} did not become foreground before the benchmark preparation timeout.");
    }

    public Task<TelemetrySample?> CaptureBenchmarkAsync(TimeSpan duration, CancellationToken cancellationToken = default)
        => _presentMon.CaptureAsync(duration, cancellationToken);

    public Task<OwnedProcessStopResult> StopOwnedPlayerAsync(
        int processId,
        string expectedExecutablePath,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
        => _processController.StopVerifiedAsync(processId, expectedExecutablePath, timeout, cancellationToken);
}

public sealed record BlueStacksCandidatePlan(
    IReadOnlyDictionary<string, string> Updates,
    IReadOnlyList<string> UnsupportedChanges)
{
    public bool CanApply => UnsupportedChanges.Count == 0;
}

public sealed class BlueStacksAutoTunerRuntime : IAutoTunerRuntime
{
    private readonly BlueStacksInstance _instance;
    private readonly IBlueStacksAutoTunerPlatform _platform;
    private readonly AutoTunerRuntimeOptions _options;
    private IReadOnlyDictionary<string, string>? _baselineSettings;
    private string? _baselineBackupPath;
    private bool _baselineDirty;
    private bool _candidateActive;
    private int? _ownedProcessId;
    private string? _ownedExecutablePath;

    public BlueStacksAutoTunerRuntime(
        BlueStacksInstance instance,
        IBlueStacksAutoTunerPlatform platform,
        AutoTunerRuntimeOptions? options = null)
    {
        _instance = instance ?? throw new ArgumentNullException(nameof(instance));
        _platform = platform ?? throw new ArgumentNullException(nameof(platform));
        _options = options ?? new AutoTunerRuntimeOptions();
        ValidateOptions(_options);
    }

    public async Task<AutoTunerRuntimeResult> ApplyCandidateAsync(TuningCandidate candidate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        cancellationToken.ThrowIfCancellationRequested();

        if (_ownedProcessId is not null)
            return AutoTunerRuntimeResult.Fail("The previous managed BlueStacks process has not been cleaned up yet.");
        if (_platform.IsPlayerRunning())
            return AutoTunerRuntimeResult.Fail("BlueStacks is already running outside this Auto Tuner session. Close the active App Player before starting restart-required tuning; FF Performance Engine will never kill an unowned player.");

        _baselineSettings ??= _platform.CaptureAllowedSettings(_instance.Name);
        if (_baselineSettings.Count == 0)
            return AutoTunerRuntimeResult.Fail($"No allow-listed BlueStacks settings were captured for instance '{_instance.Name}'.");

        var plan = BuildCandidatePlan(candidate, _instance, _baselineSettings);
        if (!plan.CanApply)
            return AutoTunerRuntimeResult.Fail("Candidate rejected: " + string.Join("; ", plan.UnsupportedChanges));

        var write = _platform.ApplyInstanceSettings(_instance.Name, plan.Updates);
        if (!write.Success)
            return AutoTunerRuntimeResult.Fail(write.Message);

        if (!string.IsNullOrWhiteSpace(write.BackupPath))
        {
            _baselineBackupPath ??= write.BackupPath;
            _baselineDirty = true;
        }

        _candidateActive = true;
        var changed = plan.Updates.Count;
        return AutoTunerRuntimeResult.Ok(changed == 0
            ? "Candidate matches the captured baseline; no restart-required configuration write was necessary."
            : $"Candidate applied with {changed} allow-listed BlueStacks setting change(s). Exact baseline rollback is armed.");
    }

    public async Task<AutoTunerRuntimeResult> PrepareGameAsync(GameKind game, CancellationToken cancellationToken = default)
    {
        if (!_candidateActive)
            return AutoTunerRuntimeResult.Fail("No Auto Tuner candidate is active.");
        if (game is not (GameKind.FreeFire or GameKind.FreeFireMax))
            return AutoTunerRuntimeResult.Fail("Select Free Fire or Free Fire MAX before preparing a benchmark.");

        if (_ownedProcessId is null)
        {
            if (_platform.IsPlayerRunning())
                return AutoTunerRuntimeResult.Fail("A BlueStacks player appeared outside the managed tuning session. Automatic control was stopped for safety.");

            var executable = _platform.FindPlayerExecutable();
            if (string.IsNullOrWhiteSpace(executable))
                return AutoTunerRuntimeResult.Fail("BlueStacks player executable was not found.");

            var started = _platform.StartInstance(_instance);
            if (!started.Success || started.ProcessId is null)
                return AutoTunerRuntimeResult.Fail($"BlueStacks instance '{_instance.Name}' could not be started: {started.Error ?? "process id unavailable"}");

            _ownedProcessId = started.ProcessId.Value;
            _ownedExecutablePath = executable;
            if (_options.StartupSettleDelay > TimeSpan.Zero)
                await Task.Delay(_options.StartupSettleDelay, cancellationToken).ConfigureAwait(false);
        }

        var prepared = await _platform.PrepareRunningGameAsync(_instance, game, _options.ForegroundTimeout, cancellationToken).ConfigureAwait(false);
        return prepared.Success
            ? AutoTunerRuntimeResult.Ok(prepared.Message)
            : AutoTunerRuntimeResult.Fail(prepared.Message);
    }

    public Task<TelemetrySample?> CaptureBenchmarkAsync(CancellationToken cancellationToken = default)
    {
        if (!_candidateActive || _ownedProcessId is null) return Task.FromResult<TelemetrySample?>(null);
        return _platform.CaptureBenchmarkAsync(_options.BenchmarkDuration, cancellationToken);
    }

    public async Task CompleteCandidateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await StopOwnedPlayerAsync(cancellationToken).ConfigureAwait(false);
            await RestoreDirtyBaselineAsync().ConfigureAwait(false);
        }
        finally
        {
            _candidateActive = false;
        }
    }

    public async Task RestoreBaselineAsync(CancellationToken cancellationToken = default)
    {
        await StopOwnedPlayerAsync(cancellationToken).ConfigureAwait(false);
        await RestoreDirtyBaselineAsync().ConfigureAwait(false);
        _candidateActive = false;
    }

    public static BlueStacksCandidatePlan BuildCandidatePlan(
        TuningCandidate candidate,
        BlueStacksInstance baselineInstance,
        IReadOnlyDictionary<string, string> capturedSettings)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(baselineInstance);
        ArgumentNullException.ThrowIfNull(capturedSettings);

        var updates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var unsupported = new List<string>();

        PlanIntegerChange("CPU allocation", "cpus", candidate.CpuCores, baselineInstance.CpuCores, 1, 256, capturedSettings, baselineInstance.Name, updates, unsupported);
        PlanIntegerChange("RAM allocation", "ram", candidate.RamMb, baselineInstance.RamMb, 512, 131072, capturedSettings, baselineInstance.Name, updates, unsupported);

        var baselineFps = ReadCapturedInt(capturedSettings, baselineInstance.Name, "max_fps")
                          ?? ReadCapturedInt(capturedSettings, baselineInstance.Name, "fps")
                          ?? baselineInstance.Fps;
        if (baselineFps != candidate.FpsTarget)
        {
            if (candidate.FpsTarget is < 30 or > 1000)
            {
                unsupported.Add($"FPS target {candidate.FpsTarget} is outside the safe planner range");
            }
            else
            {
                var fpsKey = HasShortKey(capturedSettings, baselineInstance.Name, "max_fps")
                    ? "max_fps"
                    : HasShortKey(capturedSettings, baselineInstance.Name, "fps") ? "fps" : null;
                if (fpsKey is null)
                {
                    unsupported.Add("installed BlueStacks build does not expose an allow-listed FPS key");
                }
                else
                {
                    updates[fpsKey] = candidate.FpsTarget.ToString(CultureInfo.InvariantCulture);
                    if (HasShortKey(capturedSettings, baselineInstance.Name, "enable_high_fps"))
                        updates["enable_high_fps"] = candidate.FpsTarget > 60 ? "1" : "0";
                }
            }
        }

        var baselineResolution = ReadCapturedResolution(capturedSettings, baselineInstance.Name) ?? baselineInstance.Resolution;
        if (!string.Equals(NormalizeResolution(baselineResolution), NormalizeResolution(candidate.Resolution), StringComparison.OrdinalIgnoreCase))
        {
            var parsed = ParseResolution(candidate.Resolution);
            if (parsed is null)
            {
                unsupported.Add($"resolution '{candidate.Resolution}' is invalid");
            }
            else if (HasShortKey(capturedSettings, baselineInstance.Name, "fb_width") && HasShortKey(capturedSettings, baselineInstance.Name, "fb_height"))
            {
                updates["fb_width"] = parsed.Value.Width.ToString(CultureInfo.InvariantCulture);
                updates["fb_height"] = parsed.Value.Height.ToString(CultureInfo.InvariantCulture);
            }
            else if (HasShortKey(capturedSettings, baselineInstance.Name, "display_width") && HasShortKey(capturedSettings, baselineInstance.Name, "display_height"))
            {
                updates["display_width"] = parsed.Value.Width.ToString(CultureInfo.InvariantCulture);
                updates["display_height"] = parsed.Value.Height.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                unsupported.Add("installed BlueStacks build does not expose a complete allow-listed resolution key pair");
            }
        }

        var baselineRenderer = baselineInstance.Renderer;
        if (!string.IsNullOrWhiteSpace(candidate.Renderer)
            && !string.Equals(candidate.Renderer, "Auto", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(candidate.Renderer, baselineRenderer, StringComparison.OrdinalIgnoreCase))
        {
            unsupported.Add("Renderer mutation is version-dependent and is not automatically changed without a verified BlueStacks adapter");
        }

        return new BlueStacksCandidatePlan(updates, unsupported);
    }

    private async Task StopOwnedPlayerAsync(CancellationToken cancellationToken)
    {
        if (_ownedProcessId is null) return;
        if (string.IsNullOrWhiteSpace(_ownedExecutablePath))
            throw new InvalidOperationException("Owned BlueStacks process identity is unavailable; refusing an unverified stop.");

        var processId = _ownedProcessId.Value;
        var result = await _platform.StopOwnedPlayerAsync(processId, _ownedExecutablePath, _options.ProcessStopTimeout, cancellationToken).ConfigureAwait(false);
        if (!result.Success)
            throw new InvalidOperationException(result.Message);

        _ownedProcessId = null;
        _ownedExecutablePath = null;
    }

    private Task RestoreDirtyBaselineAsync()
    {
        if (!_baselineDirty) return Task.CompletedTask;
        if (string.IsNullOrWhiteSpace(_baselineBackupPath))
            throw new InvalidOperationException("Baseline configuration is dirty but the exact backup path is unavailable.");

        var restored = _platform.RestoreBackup(_baselineBackupPath);
        if (!restored.Success)
            throw new InvalidOperationException($"BlueStacks baseline restore failed: {restored.Message}");

        _baselineDirty = false;
        return Task.CompletedTask;
    }

    private static void PlanIntegerChange(
        string label,
        string shortKey,
        int candidateValue,
        int? baselineValue,
        int minimum,
        int maximum,
        IReadOnlyDictionary<string, string> captured,
        string instanceName,
        Dictionary<string, string> updates,
        List<string> unsupported)
    {
        var actualBaseline = ReadCapturedInt(captured, instanceName, shortKey) ?? baselineValue;
        if (actualBaseline == candidateValue) return;
        if (candidateValue < minimum || candidateValue > maximum)
        {
            unsupported.Add($"{label} value {candidateValue} is outside the safe planner range");
            return;
        }
        if (!HasShortKey(captured, instanceName, shortKey))
        {
            unsupported.Add($"installed BlueStacks build does not expose allow-listed key '{shortKey}' for {label}");
            return;
        }
        updates[shortKey] = candidateValue.ToString(CultureInfo.InvariantCulture);
    }

    private static bool HasShortKey(IReadOnlyDictionary<string, string> captured, string instanceName, string shortKey)
        => captured.Keys.Any(x => x.StartsWith($"bst.instance.{instanceName}.", StringComparison.OrdinalIgnoreCase)
                                  && x.EndsWith($".{shortKey}", StringComparison.OrdinalIgnoreCase));

    private static string? ReadCapturedValue(IReadOnlyDictionary<string, string> captured, string instanceName, string shortKey)
    {
        var key = captured.Keys.FirstOrDefault(x => x.StartsWith($"bst.instance.{instanceName}.", StringComparison.OrdinalIgnoreCase)
                                                     && x.EndsWith($".{shortKey}", StringComparison.OrdinalIgnoreCase));
        return key is null ? null : captured[key].Trim().Trim('"');
    }

    private static int? ReadCapturedInt(IReadOnlyDictionary<string, string> captured, string instanceName, string shortKey)
        => int.TryParse(ReadCapturedValue(captured, instanceName, shortKey), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    private static string? ReadCapturedResolution(IReadOnlyDictionary<string, string> captured, string instanceName)
    {
        var width = ReadCapturedInt(captured, instanceName, "fb_width") ?? ReadCapturedInt(captured, instanceName, "display_width");
        var height = ReadCapturedInt(captured, instanceName, "fb_height") ?? ReadCapturedInt(captured, instanceName, "display_height");
        return width is not null && height is not null ? $"{width}x{height}" : null;
    }

    private static string? NormalizeResolution(string? value) => value?.Trim().ToLowerInvariant();

    private static (int Width, int Height)? ParseResolution(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var parts = value.Split('x', 'X');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var width) || !int.TryParse(parts[1], out var height)) return null;
        if (width < 640 || height < 480 || width > 7680 || height > 4320) return null;
        return (width, height);
    }

    private static void ValidateOptions(AutoTunerRuntimeOptions options)
    {
        if (options.StartupSettleDelay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(options.StartupSettleDelay));
        if (options.ForegroundTimeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(options.ForegroundTimeout));
        if (options.BenchmarkDuration < TimeSpan.FromSeconds(2)) throw new ArgumentOutOfRangeException(nameof(options.BenchmarkDuration));
        if (options.ProcessStopTimeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(options.ProcessStopTimeout));
    }
}
