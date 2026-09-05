using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

namespace FFPerformanceEngine.App;

public sealed class AppServices : IAsyncDisposable
{
    private static readonly TimeSpan GuardianLoopInterval = TimeSpan.FromSeconds(2);

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
    public BlueStacksPlayerProcessProbe GuardianProcessProbe { get; }
    public WindowsRecentInputProbe GuardianRecentInput { get; }
    public GuardianPlayerBindingService GuardianBinding { get; }
    public GuardianSupervisorFactory GuardianSupervisorFactory { get; }
    public GuardianLiveSessionService GuardianLiveSession { get; }
    public GuardianSessionHost GuardianHost { get; }
    public PerformanceCaptureCoordinator PerformanceCapture { get; }
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

        GuardianProcessProbe = new BlueStacksPlayerProcessProbe();
        GuardianRecentInput = new WindowsRecentInputProbe();
        GuardianBinding = new GuardianPlayerBindingService(GuardianProcessProbe);
        GuardianSupervisorFactory = new GuardianSupervisorFactory(
            Guardian,
            Profiles,
            BlueStacksAutomation,
            PresentMon,
            GuardianCanary,
            GuardianProcessProbe,
            GuardianRecentInput);
        GuardianLiveSession = new GuardianLiveSessionService(
            Environment.Capture,
            GuardianBinding,
            GuardianSupervisorFactory);
        GuardianHost = new GuardianSessionHost(GuardianLiveSession);
        PerformanceCapture = new PerformanceCaptureCoordinator(
            (processId, duration, cancellationToken) => PresentMon.CaptureProcessAsync(processId, duration, cancellationToken));

        AutoTunerRuntimeFactory = new BlueStacksAutoTunerRuntimeFactory(BlueStacks, BlueStacksAutomation, PresentMon);
        AutoTunerSession = new AutoTunerSessionService(AutoTuner, AutoTunerRuntimeFactory, Profiles, History);
        OptimizeSystem = new OptimizeSystemProbe(Environment, BlueStacks, PresentMon);
        OptimizeWorkflow = new OptimizeWorkflowService(AutoTuner, AutoTunerSession, OptimizeSystem);
    }

    public async Task InitializeAsync()
    {
        Settings = await SettingsService.LoadAsync().ConfigureAwait(false);
        Guardian.Mode = Settings.GuardianMode;
        await ReconcileGuardianHostAsync(Settings).ConfigureAwait(false);
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Settings = settings;
        Guardian.Mode = settings.GuardianMode;
        await SettingsService.SaveAsync(settings).ConfigureAwait(false);
        await ReconcileGuardianHostAsync(settings).ConfigureAwait(false);
    }

    public EnvironmentSnapshot CaptureEnvironment() => Environment.Capture();

    public async ValueTask DisposeAsync()
    {
        await GuardianHost.DisposeAsync().ConfigureAwait(false);
    }

    // Compatibility entry point used by existing pages while the app service graph evolves.
    public Task UpdateSettingsAsync(AppSettings settings) => SaveSettingsAsync(settings);

    private async Task ReconcileGuardianHostAsync(AppSettings settings)
    {
        var environment = Environment.Capture();
        var instanceName = GuardianStartupPolicy.SelectInstanceName(settings, environment);

        if (!settings.GuardianEnabled || string.IsNullOrWhiteSpace(instanceName))
        {
            if (GuardianHost.IsRunning) await GuardianHost.StopAsync().ConfigureAwait(false);
            return;
        }

        await GuardianHost.StartAsync(instanceName, GuardianLoopInterval).ConfigureAwait(false);
    }
}
