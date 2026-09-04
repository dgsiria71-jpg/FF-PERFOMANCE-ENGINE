using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

namespace FFPerformanceEngine.App;

public sealed class AppServices
{
    public BlueStacksService BlueStacks { get; } = new();
    public SettingsService SettingsService { get; } = new();
    public ProfileService Profiles { get; } = new();
    public HistoryService History { get; } = new();
    public SnapshotService Snapshots { get; } = new();
    public TelemetryService Telemetry { get; } = new();
    public PresentMonService PresentMon { get; } = new();
    public AutoTunerEngine AutoTuner { get; } = new();
    public GuardianEngine Guardian { get; } = new();
    public ProcessTuningService ProcessTuning { get; } = new();
    public GuardianKnowledgeService GuardianKnowledge { get; } = new();
    public EnvironmentProbe Environment { get; }
    public ProfileApplicationService ProfileApplication { get; }
    public GuardianCanaryService GuardianCanary { get; }
    public AppSettings Settings { get; private set; } = new();

    public AppServices()
    {
        Environment = new EnvironmentProbe(BlueStacks);
        ProfileApplication = new ProfileApplicationService(BlueStacks, Snapshots, History);
        GuardianCanary = new GuardianCanaryService(Guardian, ProcessTuning, GuardianKnowledge, History);
    }

    public async Task InitializeAsync()
    {
        Settings = await SettingsService.LoadAsync();
        Guardian.Mode = Settings.GuardianMode;
    }

    public async Task UpdateSettingsAsync(AppSettings settings)
    {
        Settings = settings;
        Guardian.Mode = settings.GuardianMode;
        await SettingsService.SaveAsync(settings);
    }
}
