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
    public BlueStacksAutomationService BlueStacksAutomation { get; }
    public ProfileApplicationService ProfileApplication { get; }
    public GuardianCanaryService GuardianCanary { get; }
    public BlueStacksAutoTunerRuntimeFactory AutoTunerRuntimeFactory { get; }
    public AutoTunerSessionService AutoTunerSession { get; }
    public OptimizeSystemProbe OptimizeSystem { get; }
    public OptimizeWorkflowService OptimizeWorkflow { get; }
    public AppSettings Settings { get; private set; } = new();

    public AppServices()
    {
        Environment = new EnvironmentProbe(BlueStacks);
        BlueStacksAutomation = new BlueStacksAutomationService(BlueStacks);
        ProfileApplication = new ProfileApplicationService(BlueStacks, Snapshots, History);
        GuardianCanary = new GuardianCanaryService(Guardian, ProcessTuning, GuardianKnowledge, History);
        AutoTunerRuntimeFactory = new BlueStacksAutoTunerRuntimeFactory(BlueStacks, BlueStacksAutomation, PresentMon);
        AutoTunerSession = new AutoTunerSessionService(AutoTuner, AutoTunerRuntimeFactory, Profiles, History);
        OptimizeSystem = new OptimizeSystemProbe(Environment, BlueStacks, PresentMon);
        OptimizeWorkflow = new OptimizeWorkflowService(AutoTuner, AutoTunerSession, OptimizeSystem);
    }

    public async Task InitializeAsync()
    {
        Settings = await SettingsService.LoadAsync();
        Guardian.Mode = Settings.GuardianMode;
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        Settings = settings;
        Guardian.Mode = settings.GuardianMode;
        await SettingsService.SaveAsync(settings);
    }

    // Compatibility entry point used by existing pages while the app service graph evolves.
    public Task UpdateSettingsAsync(AppSettings settings) => SaveSettingsAsync(settings);
}
