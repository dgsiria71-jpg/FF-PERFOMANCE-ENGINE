namespace FFPerformanceEngine.Core.Models;

public sealed record AppSettings
{
    public ThemeMode Theme { get; init; } = ThemeMode.Clean;
    public string GlassStyle { get; init; } = "Deep";
    public AutoTunerMode DefaultTunerMode { get; init; } = AutoTunerMode.Adaptive;
    public bool KeepDeepAsDefault { get; init; }
    public GuardianMode GuardianMode { get; init; } = GuardianMode.Adaptive;
    public bool GuardianEnabled { get; init; } = true;
    public string? GuardianInstanceName { get; init; }
    public string MiniModeSize { get; init; } = "Mini";
    public bool MiniModeAlwaysOnTop { get; init; } = true;
    public bool MiniModeClickThroughAuto { get; init; } = true;
    public bool ArgbEnabled { get; init; } = true;
    public string ArgbPreset { get; init; } = "Adaptive";
    public string QuickAction { get; init; } = "QuickBoost";
    public GameKind PreferredGame { get; init; } = GameKind.None;
}
