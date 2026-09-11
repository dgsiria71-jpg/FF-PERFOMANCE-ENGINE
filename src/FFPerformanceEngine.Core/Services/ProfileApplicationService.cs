using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed record ProfileApplicationPlan(
    string InstanceName,
    IReadOnlyDictionary<string, string> RestartRequiredSettings,
    IReadOnlyList<string> AssistedChanges,
    bool RequiresBlueStacksRestart);

public sealed record ProfileApplicationResult(bool Success, bool RequiresPlayerStop, string Message, TuningSnapshot? Snapshot = null, string? BackupPath = null);

public sealed class ProfileApplicationService
{
    private readonly BlueStacksService _blueStacks;
    private readonly SnapshotService _snapshots;
    private readonly HistoryService _history;

    public ProfileApplicationService(BlueStacksService blueStacks, SnapshotService snapshots, HistoryService history)
    {
        _blueStacks = blueStacks;
        _snapshots = snapshots;
        _history = history;
    }

    public ProfileApplicationPlan BuildPlan(PerformanceProfile profile, BlueStacksInstance instance, IReadOnlyDictionary<string, string> capturedSettings)
    {
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["cpus"] = Math.Max(1, profile.CpuCores).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["ram"] = Math.Max(1024, profile.RamMb).ToString(System.Globalization.CultureInfo.InvariantCulture),
            [ChooseExistingKey(capturedSettings, instance.Name, "max_fps", "fps")] = Math.Max(30, profile.FpsTarget).ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        if (profile.FpsTarget > 60 && HasShortKey(capturedSettings, instance.Name, "enable_high_fps")) settings["enable_high_fps"] = "1";

        var resolution = ParseResolution(profile.Resolution);
        if (resolution is { } size)
        {
            settings[ChooseExistingKey(capturedSettings, instance.Name, "fb_width", "display_width")] = size.Width.ToString(System.Globalization.CultureInfo.InvariantCulture);
            settings[ChooseExistingKey(capturedSettings, instance.Name, "fb_height", "display_height")] = size.Height.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        if (profile.Dpi is >= 10 and <= 999) settings["dpi"] = profile.Dpi.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var assisted = new List<string>();
        if (!string.IsNullOrWhiteSpace(profile.Renderer) && !string.Equals(profile.Renderer, "Auto", StringComparison.OrdinalIgnoreCase) && !string.Equals(profile.Renderer, instance.Renderer, StringComparison.OrdinalIgnoreCase))
            assisted.Add($"Renderer: {instance.Renderer ?? "unknown"} → {profile.Renderer} (assisted; raw renderer mapping is version-dependent)");

        return new(instance.Name, settings, assisted, settings.Count > 0 || assisted.Count > 0);
    }

    public async Task<ProfileApplicationResult> ApplyAsync(PerformanceProfile profile, BlueStacksInstance instance, CancellationToken cancellationToken = default)
    {
        if (profile.Evidence != EvidenceLevel.Validated)
            return new(false, false, "Only validated profiles can be applied automatically.");

        var captured = _blueStacks.CaptureAllowedSettings(instance.Name);
        var plan = BuildPlan(profile, instance, captured);
        if (_blueStacks.IsPlayerRunning())
            return new(false, true, "BlueStacks is running. Restart-required profile settings were queued rather than forced mid-session.");

        var snapshot = await _snapshots.CreateAsync($"Before applying {profile.Name}", captured, cancellationToken).ConfigureAwait(false);
        var write = _blueStacks.ApplyInstanceSettings(instance.Name, plan.RestartRequiredSettings);
        if (!write.Success)
            return new(false, write.RequiresPlayerStop, write.Message, snapshot, write.BackupPath);

        await _history.AppendAsync(new HistoryEvent
        {
            Kind = HistoryEventKind.Profile,
            Title = $"Profile applied: {profile.Name}",
            Summary = plan.AssistedChanges.Count == 0
                ? write.Message
                : $"{write.Message} {plan.AssistedChanges.Count} assisted change(s) remain pending."
        }, cancellationToken).ConfigureAwait(false);

        return new(true, false, plan.AssistedChanges.Count == 0 ? write.Message : $"{write.Message} Renderer remains assisted for this BlueStacks build.", snapshot, write.BackupPath);
    }

    private static bool HasShortKey(IReadOnlyDictionary<string, string> captured, string instanceName, string shortKey)
        => captured.Keys.Any(x => x.EndsWith($".{shortKey}", StringComparison.OrdinalIgnoreCase) && x.StartsWith($"bst.instance.{instanceName}.", StringComparison.OrdinalIgnoreCase));

    private static string ChooseExistingKey(IReadOnlyDictionary<string, string> captured, string instanceName, string preferred, string fallback)
        => HasShortKey(captured, instanceName, preferred) ? preferred : HasShortKey(captured, instanceName, fallback) ? fallback : preferred;

    private static (int Width, int Height)? ParseResolution(string value)
    {
        var parts = value.Split('x', 'X');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var width) || !int.TryParse(parts[1], out var height)) return null;
        if (width < 640 || height < 480 || width > 7680 || height > 4320) return null;
        return (width, height);
    }
}
