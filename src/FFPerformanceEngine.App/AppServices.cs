using System.IO;
using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;
using FFPerformanceEngine.Core.SystemOptimization;
using FFPerformanceEngine.Core.Workloads;

namespace FFPerformanceEngine.App;

public sealed class AppServices : IAsyncDisposable
{
    private static readonly TimeSpan GuardianLoopInterval = TimeSpan.FromSeconds(2);
    private const int TelemetryRawFrameCapacity = 4096;
    private const int TelemetryOneSecondAggregateCapacity = 4096;
    private const int TelemetrySessionTenSecondAggregateCapacity = 4096;

    public BlueStacksService BlueStacks { get; } = new();
    public SettingsService SettingsService { get; } = new();
    public ProfileService Profiles { get; } = new();
    public HistoryService History { get; } = new();
    public SnapshotService Snapshots { get; } = new();
    public TelemetryService Telemetry { get; } = new();
    public PresentMonService PresentMon { get; } = new();
    public TelemetryRealtimePipeline TelemetryRealtime { get; }
    public ProcessorPowerTelemetrySource ProcessorPowerTelemetry { get; }
    public WddmGpuTelemetrySource WddmGpuTelemetry { get; }
    public AutoTunerEngine AutoTuner { get; } = new();
    public GuardianEngine Guardian { get; } = new();
    public ProcessTuningService ProcessTuning { get; } = new();
    public GuardianKnowledgeService GuardianKnowledge { get; } = new();
    public PerformanceTimelineBuffer PerformanceTimeline { get; } = new(capacity: 3600);
    public PerformanceComparisonSession PerformanceComparison { get; }
    public PerformanceWorkloadContextSelection PerformanceWorkloadContext { get; } = new();
    public EnvironmentProbe Environment { get; }
    public HardwareDiscoveryService HardwareDiscovery { get; }
    public WindowsPerformanceCapabilityRegistry WindowsCapabilities { get; }
    public WindowsCapabilityMutationAdapterRegistry WindowsMutationAdapters { get; }
    public WindowsPerformanceCapabilityDiscoveryService WindowsCapabilityDiscovery { get; }
    public WindowsCapabilityCandidatePlanner WindowsCapabilityCandidates { get; }
    public WindowsCapabilityPerformanceCostMapService WindowsCapabilityCostMap { get; }
    public WindowsCapabilityEvidenceEvaluationService WindowsCapabilityEvidenceEvaluator { get; }
    public WindowsCapabilityValidationGate WindowsCapabilityValidationGate { get; }
    public WindowsCapabilityValidatedEvidenceStore WindowsCapabilityValidatedEvidence { get; }
    public WindowsCapabilityValidationChallengeService WindowsCapabilityValidationChallenge { get; }
    public WindowsCapabilityValidationWorkflowService WindowsCapabilityValidationWorkflow { get; }
    public WindowsCapabilityValidatedRecommendationService WindowsCapabilityValidatedRecommendations { get; }
    public SystemOptimizationTransactionEngine SystemOptimizer { get; }
    public MachineContextService MachineContext { get; }
    public PersistentPcOptimizationPlanner PersistentPcPlanner { get; }
    public PersistentPcRecommendationCoordinator PersistentPcRecommendationCoordinator { get; }
    public PersistentPcRecommendationService PersistentPcRecommendations { get; }
    public PersistentPcOptimizationService PersistentPcOptimization { get; }
    public UniversalBottleneckAnalyzer BottleneckAnalyzer { get; }
    public UniversalDiagnosticService Diagnostics { get; }
    public BlueStacksAutomationService BlueStacksAutomation { get; }
    public BlueStacksInstalledGameDiscoverySource BlueStacksGameDiscovery { get; }
    public SteamGameDiscoverySource SteamGameDiscovery { get; }
    public EpicGameDiscoverySource EpicGameDiscovery { get; }
    public RiotGameDiscoverySource RiotGameDiscovery { get; }
    public BattleNetGameDiscoverySource BattleNetGameDiscovery { get; }
    public EaAppGameDiscoverySource EaAppGameDiscovery { get; }
    public UbisoftGameDiscoverySource UbisoftGameDiscovery { get; }
    public MicrosoftStoreGdkGameDiscoverySource MicrosoftStoreGameDiscovery { get; }
    public LocalGameCatalogService GameCatalog { get; }
    public GameAdapterResolver GameAdapters { get; }
    public BlueStacksUniversalTuningCandidateBridge UniversalTuningCandidates { get; }
    public UniversalValidatedProfileProvenanceService UniversalValidatedProfileProvenance { get; }
    public RunningProcessGameEvidenceSource RunningProcessGameEvidence { get; }
    public KnownExecutableGameEvidenceSource KnownExecutableGameEvidence { get; }
    public GameEvidenceCatalogService GameEvidenceCatalog { get; }
    public GameEvidenceBinder GameEvidenceBinder { get; }
    public GameDiscoveryCoordinator GameDiscovery { get; }
    public ProfileApplicationService ProfileApplication { get; }
    public ProfileChallengeService ProfileChallenges { get; }
    public ProfileChallengeProgressService ProfileChallengeProgress { get; }
    public ProfileChallengeRoundService ProfileChallengeRounds { get; }
    public GuardianCanaryService GuardianCanary { get; }
    public BlueStacksPlayerProcessProbe GuardianProcessProbe { get; }
    public WindowsRecentInputProbe GuardianRecentInput { get; }
    public GuardianPlayerBindingService GuardianBinding { get; }
    public GuardianSupervisorFactory GuardianSupervisorFactory { get; }
    public GuardianLiveSessionService GuardianLiveSession { get; }
    public GuardianSessionHost GuardianHost { get; }
    public ControlledBenchmarkLeaseManager ControlledBenchmarks { get; }
    public PerformanceTimelineEventRecorder PerformanceTimelineEvents { get; }
    public PerformanceCaptureCoordinator PerformanceCapture { get; }
    public GuardianBoundWindowsCapabilityBenchmarkProbe WindowsCapabilityBenchmarkProbe { get; }
    public WindowsCapabilityControlledBenchmarkService WindowsCapabilityBenchmarks { get; }
    public WindowsCapabilityExperimentCoordinator WindowsCapabilityExperiments { get; }
    public BlueStacksAutoTunerRuntimeFactory AutoTunerRuntimeFactory { get; }
    public AutoTunerSessionService AutoTunerSession { get; }
    public OptimizeSystemProbe OptimizeSystem { get; }
    public OptimizeWorkflowService OptimizeWorkflow { get; }
    public AppSettings Settings { get; private set; } = new();

    public AppServices()
    {
        Environment = new EnvironmentProbe(BlueStacks);

        // Track 4 composes one bounded realtime telemetry authority. These values
        // are memory-count limits only; they intentionally make no retention-time
        // promise because collector frequency is a separate runtime policy.
        TelemetryRealtime = new TelemetryRealtimePipeline(
            TelemetryRawFrameCapacity,
            TelemetryOneSecondAggregateCapacity,
            TelemetrySessionTenSecondAggregateCapacity);

        // Hardware telemetry is composed but remains completely on-demand. Merely
        // constructing AppServices performs no processor-power or WDDM/PDH sample.
        ProcessorPowerTelemetry = new ProcessorPowerTelemetrySource(
            new WindowsProcessorPowerInfoProvider());
        WddmGpuTelemetry = new WddmGpuTelemetrySource(
            new WindowsWddmGpuUtilizationProvider());

        // Track 1 is a universal bridge over the existing EnvironmentSnapshot,
        // not a second machine model. Every higher-level subsystem shares these
        // exact discovery, capability, fingerprint and bottleneck services.
        HardwareDiscovery = new HardwareDiscoveryService();
        WindowsCapabilities = new WindowsPerformanceCapabilityRegistry();

        // Track 2 composes one adapter registry for both discovery and mutation.
        // A capability becomes Available only after its concrete adapter proves
        // current state; the transaction engine consumes that same proven catalog.
        var powerSettingApi = new WindowsPowerSettingApi();
        WindowsMutationAdapters = new WindowsCapabilityMutationAdapterRegistry(
        [
            new WindowsPowerPolicyMutationAdapter(),
            new WindowsCpuBoostPolicyMutationAdapter(powerSettingApi),
            new WindowsCpuCoreParkingPolicyMutationAdapter(powerSettingApi)
        ]);
        WindowsCapabilityDiscovery = new WindowsPerformanceCapabilityDiscoveryService(
            WindowsCapabilities,
            WindowsMutationAdapters);
        WindowsCapabilityCandidates = new WindowsCapabilityCandidatePlanner();
        WindowsCapabilityCostMap = new WindowsCapabilityPerformanceCostMapService();
        WindowsCapabilityEvidenceEvaluator = new WindowsCapabilityEvidenceEvaluationService(
            WindowsCapabilityCostMap);
        WindowsCapabilityValidationGate = new WindowsCapabilityValidationGate();
        WindowsCapabilityValidatedEvidence = new WindowsCapabilityValidatedEvidenceStore();
        SystemOptimizer = new SystemOptimizationTransactionEngine(
            WindowsCapabilities,
            WindowsMutationAdapters,
            Snapshots,
            History);

        MachineContext = new MachineContextService(HardwareDiscovery, WindowsCapabilities);

        // "Otimizar este PC" uses one shared recommendation authority and one
        // shared transactional authority. Producers may propose targets, but only
        // the recommendation service can publish automatic persistent targets;
        // it refreshes OS state first and binds evidence to the current machine.
        PersistentPcPlanner = new PersistentPcOptimizationPlanner();
        PersistentPcRecommendationCoordinator = new PersistentPcRecommendationCoordinator(
            WindowsCapabilities,
            WindowsMutationAdapters);
        PersistentPcRecommendations = new PersistentPcRecommendationService(
            WindowsCapabilityDiscovery,
            CaptureMachineContext,
            PersistentPcRecommendationCoordinator);

        BottleneckAnalyzer = new UniversalBottleneckAnalyzer();
        Diagnostics = new UniversalDiagnosticService(MachineContext, BottleneckAnalyzer);

        BlueStacksAutomation = new BlueStacksAutomationService(BlueStacks);

        // Track 3 stays side-effect free at composition time. The catalog, sources
        // and resolver are shared application authorities, but neither BlueStacks
        // package discovery nor launcher metadata scans run until DiscoverGamesAsync
        // is explicitly requested by a workflow.
        BlueStacksGameDiscovery = new BlueStacksInstalledGameDiscoverySource(
            BlueStacks,
            BlueStacksAutomation);
        SteamGameDiscovery = new SteamGameDiscoverySource();
        EpicGameDiscovery = new EpicGameDiscoverySource();
        RiotGameDiscovery = new RiotGameDiscoverySource();
        BattleNetGameDiscovery = new BattleNetGameDiscoverySource();
        EaAppGameDiscovery = new EaAppGameDiscoverySource();
        UbisoftGameDiscovery = new UbisoftGameDiscoverySource();
        MicrosoftStoreGameDiscovery = new MicrosoftStoreGdkGameDiscoverySource(
            new WindowsMicrosoftStoreGamePackageProvider());
        GameCatalog = new LocalGameCatalogService(
        [
            BlueStacksGameDiscovery,
            SteamGameDiscovery,
            EpicGameDiscovery,
            RiotGameDiscovery,
            BattleNetGameDiscovery,
            EaAppGameDiscovery,
            UbisoftGameDiscovery,
            MicrosoftStoreGameDiscovery
        ]);
        GameAdapters = new GameAdapterResolver(
        [
            new GenericGameAdapter(),
            BlueStacksFreeFireGameAdapter.For(GameKind.FreeFire),
            BlueStacksFreeFireGameAdapter.For(GameKind.FreeFireMax)
        ]);
        UniversalTuningCandidates = new BlueStacksUniversalTuningCandidateBridge(
            AutoTuner,
            GameAdapters);
        UniversalValidatedProfileProvenance = new UniversalValidatedProfileProvenanceService(
            Profiles,
            History,
            UniversalTuningCandidates);
        RunningProcessGameEvidence = new RunningProcessGameEvidenceSource(
            new WindowsRunningProcessObservationProvider());
        KnownExecutableGameEvidence = new KnownExecutableGameEvidenceSource(
            new WindowsAppPathsKnownExecutableObservationProvider());
        GameEvidenceCatalog = new GameEvidenceCatalogService(
        [
            RunningProcessGameEvidence,
            KnownExecutableGameEvidence
        ]);
        GameEvidenceBinder = new GameEvidenceBinder();
        GameDiscovery = new GameDiscoveryCoordinator(
            GameCatalog,
            GameAdapters,
            GameEvidenceCatalog,
            GameEvidenceBinder);

        ProfileApplication = new ProfileApplicationService(BlueStacks, Snapshots, History);
        ProfileChallenges = new ProfileChallengeService(Profiles, History);
        ProfileChallengeProgress = new ProfileChallengeProgressService(Profiles, History);
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

        // One application-level authority owns every controlled measurement and
        // persistent Windows mutation. The global gate protects machine-wide
        // CPU/GPU/PresentMon evidence while Guardian is suspended/reconciled.
        ControlledBenchmarks = new ControlledBenchmarkLeaseManager(GuardianHost);
        PersistentPcOptimization = new PersistentPcOptimizationService(
            PersistentPcPlanner,
            WindowsCapabilityDiscovery,
            CaptureMachineContext,
            SystemOptimizer,
            ControlledBenchmarks);

        // Universal workload context remains completely on-demand. The selector
        // starts empty and is populated only by an explicit caller that already
        // owns a resolved Track 3 catalog + stable GameId.
        PerformanceComparison = new PerformanceComparisonSession(
            CapturePerformanceConfiguration,
            CapturePerformanceUniversalContext);
        PerformanceTimelineEvents = new PerformanceTimelineEventRecorder(PerformanceTimeline);
        GuardianHost.StatusChanged += GuardianHost_StatusChanged;
        PerformanceCapture = new PerformanceCaptureCoordinator(
            (processId, duration, cancellationToken) => PresentMon.CaptureProcessAsync(processId, duration, cancellationToken),
            PerformanceTimeline,
            (processId, duration, cancellationToken) => CaptureProcessTelemetryFrameAsync(processId, duration, cancellationToken));

        // Windows controlled experimentation reuses the exact same Guardian binding,
        // process probe, PresentMon capture, global benchmark lease, transaction
        // engine and machine fingerprint authorities. Evidence interpretation and
        // PendingValidation gating are also single shared services: the experiment
        // coordinator cannot bypass or fork their policy.
        WindowsCapabilityBenchmarkProbe = new GuardianBoundWindowsCapabilityBenchmarkProbe(
            () => GuardianHost.CurrentStatus,
            PerformanceCapture,
            GuardianProcessProbe);
        WindowsCapabilityBenchmarks = new WindowsCapabilityControlledBenchmarkService(
            SystemOptimizer,
            WindowsMutationAdapters,
            ControlledBenchmarks,
            WindowsCapabilityBenchmarkProbe);
        WindowsCapabilityExperiments = new WindowsCapabilityExperimentCoordinator(
            cancellationToken => WindowsCapabilityDiscovery.RefreshAsync(cancellationToken),
            WindowsCapabilityCandidates,
            (candidate, cancellationToken) => WindowsCapabilityBenchmarks.RunAsync(candidate, cancellationToken),
            WindowsCapabilityCostMap,
            () => CaptureMachineContext().Fingerprint.Id,
            CaptureWindowsBenchmarkWorkloadKey,
            WindowsCapabilityEvidenceEvaluator,
            WindowsCapabilityValidationGate);

        // Explicit validation is a separate stage after repeated beneficial A/B.
        // The fresh challenge reuses the experiment coordinator, then the workflow
        // makes the result durable before exposing ValidatedEvidence. Only the
        // validated recommendation bridge may translate that durable evidence into
        // a persistent recommendation, and it rechecks the same tuple immediately
        // before the recommendation service performs its own fresh discovery.
        WindowsCapabilityValidationChallenge = new WindowsCapabilityValidationChallengeService(
            (capabilityId, cancellationToken) => WindowsCapabilityExperiments.PlanAsync(capabilityId, cancellationToken),
            (candidate, cancellationToken) => WindowsCapabilityExperiments.RunAsync(candidate, cancellationToken),
            () => CaptureMachineContext().Fingerprint.Id,
            CaptureWindowsBenchmarkWorkloadKey);
        WindowsCapabilityValidationWorkflow = new WindowsCapabilityValidationWorkflowService(
            WindowsCapabilityValidationChallenge,
            WindowsCapabilityValidatedEvidence);
        WindowsCapabilityValidatedRecommendations = new WindowsCapabilityValidatedRecommendationService(
            WindowsCapabilityValidatedEvidence,
            (capabilityId, cancellationToken) => WindowsCapabilityExperiments.PlanAsync(capabilityId, cancellationToken),
            () => CaptureMachineContext().Fingerprint.Id,
            CaptureWindowsBenchmarkWorkloadKey,
            (capabilityId, targetValue, recommendation, cancellationToken) =>
                PersistentPcRecommendations.PublishAsync(
                    capabilityId,
                    targetValue,
                    recommendation,
                    cancellationToken));

        AutoTunerRuntimeFactory = new BlueStacksAutoTunerRuntimeFactory(BlueStacks, BlueStacksAutomation, PresentMon);
        ProfileChallengeRounds = new ProfileChallengeRoundService(
            Profiles,
            History,
            AutoTunerRuntimeFactory,
            ControlledBenchmarks);
        AutoTunerSession = new AutoTunerSessionService(
            AutoTuner,
            AutoTunerRuntimeFactory,
            Profiles,
            History,
            validationPolicy: null,
            benchmarkLeases: ControlledBenchmarks);
        OptimizeSystem = new OptimizeSystemProbe(Environment, BlueStacks, PresentMon);
        OptimizeWorkflow = new OptimizeWorkflowService(AutoTuner, AutoTunerSession, OptimizeSystem);
    }

    public async Task InitializeAsync()
    {
        // Discovery is read-only. It proves which Track 2 capabilities are
        // actually usable on this Windows installation before any UI/workflow
        // is allowed to request a mutation.
        await WindowsCapabilityDiscovery.RefreshAsync().ConfigureAwait(false);

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

    public MachineContext CaptureMachineContext()
        => MachineContext.Capture(Environment.Capture());

    public TelemetryFrame CaptureSystemTelemetryFrame()
    {
        var frame = Telemetry.CaptureSystemFrame();
        TelemetryRealtime.AppendRaw(frame);
        return frame;
    }

    public TelemetryFrame CaptureProcessorPowerTelemetryFrame()
    {
        var frame = ProcessorPowerTelemetry.Capture();
        TelemetryRealtime.AppendRaw(frame);
        return frame;
    }

    public TelemetryFrame CaptureGpuTelemetryFrame()
    {
        var frame = WddmGpuTelemetry.Capture();
        TelemetryRealtime.AppendRaw(frame);
        return frame;
    }

    public async Task<TelemetryFrame?> CaptureProcessTelemetryFrameAsync(
        int processId,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        var frame = await PresentMon
            .CaptureProcessFrameAsync(processId, duration, cancellationToken)
            .ConfigureAwait(false);
        if (frame is not null) TelemetryRealtime.AppendRaw(frame);
        return frame;
    }

    public Task<IReadOnlyList<WindowsPerformanceCapability>> RefreshWindowsCapabilitiesAsync(
        CancellationToken cancellationToken = default)
        => WindowsCapabilityDiscovery.RefreshAsync(cancellationToken);

    public Task<ResolvedGameCatalogResult> DiscoverGamesAsync(
        CancellationToken cancellationToken = default)
        => GameDiscovery.DiscoverAsync(cancellationToken);

    public bool SelectPerformanceWorkloadContext(
        ResolvedGameCatalogResult catalog,
        string gameId,
        IEnumerable<string>? relevantCapabilityIds = null)
        => PerformanceWorkloadContext.TrySelect(
            catalog,
            gameId,
            relevantCapabilityIds);

    public void ClearPerformanceWorkloadContext()
        => PerformanceWorkloadContext.Clear();

    public Task<WindowsCapabilityCandidatePlan> PlanWindowsCapabilityExperimentAsync(
        string capabilityId,
        CancellationToken cancellationToken = default)
        => WindowsCapabilityExperiments.PlanAsync(capabilityId, cancellationToken);

    public Task<WindowsCapabilityExperimentRunResult> RunWindowsCapabilityExperimentAsync(
        WindowsCapabilityCandidate candidate,
        CancellationToken cancellationToken = default)
        => WindowsCapabilityExperiments.RunAsync(candidate, cancellationToken);

    public Task<WindowsCapabilityValidatedEvidence> ValidateWindowsCapabilityEvidenceAsync(
        WindowsCapabilityValidationDecision pending,
        CancellationToken cancellationToken = default)
        => WindowsCapabilityValidationWorkflow.ValidateAndPersistAsync(pending, cancellationToken);

    public Task<IReadOnlyList<WindowsCapabilityValidatedEvidence>> LoadValidatedWindowsCapabilityEvidenceAsync(
        CancellationToken cancellationToken = default)
        => WindowsCapabilityValidatedEvidence.LoadAsync(cancellationToken);

    public Task<WindowsCapabilityValidatedEvidence?> ResolveCurrentValidatedWindowsCapabilityEvidenceAsync(
        string capabilityId,
        string candidateTarget,
        CancellationToken cancellationToken = default)
        => WindowsCapabilityValidatedRecommendations.ResolveCurrentAsync(
            capabilityId,
            candidateTarget,
            cancellationToken);

    public Task<CapabilityRecommendationPublicationResult> PublishValidatedWindowsCapabilityRecommendationAsync(
        WindowsCapabilityValidatedEvidence evidence,
        CancellationToken cancellationToken = default)
        => WindowsCapabilityValidatedRecommendations.PublishAsync(evidence, cancellationToken);

    public Task<CapabilityRecommendationPublicationResult> PublishPersistentRecommendationAsync(
        string capabilityId,
        string targetValue,
        CapabilityRecommendationSummary recommendation,
        CancellationToken cancellationToken = default)
        => PersistentPcRecommendations.PublishAsync(
            capabilityId,
            targetValue,
            recommendation,
            cancellationToken);

    public async Task<UniversalValidatedProfileProjection?> ResolveCurrentUniversalValidatedProfileProvenanceAsync(
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<PerformanceProfile> profiles;
        try
        {
            profiles = await Profiles.LoadAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException
                                          or UnauthorizedAccessException
                                          or System.Text.Json.JsonException)
        {
            return null;
        }

        var matches = profiles
            .Where(profile => profile.Id == profileId)
            .Take(2)
            .ToArray();
        if (matches.Length != 1) return null;

        var profile = matches[0];
        if (profile.Kind != ProfileKind.Custom
            || profile.Evidence != EvidenceLevel.Validated
            || profile.SourceComparisonId is null
            || string.IsNullOrWhiteSpace(profile.InstanceName))
            return null;

        var environment = Environment.Capture();
        var instances = environment.Instances
            .Where(instance => string.Equals(
                instance.Name,
                profile.InstanceName,
                StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToArray();
        if (instances.Length != 1) return null;

        IReadOnlyDictionary<string, string> capturedSettings;
        try
        {
            capturedSettings = BlueStacks.CaptureAllowedSettings(instances[0].Name);
        }
        catch (Exception exception) when (exception is IOException
                                          or UnauthorizedAccessException
                                          or ArgumentException)
        {
            return null;
        }
        if (capturedSettings.Count == 0) return null;

        try
        {
            return await UniversalValidatedProfileProvenance.ResolveCurrentAsync(
                profileId,
                environment,
                instances[0],
                capturedSettings,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is IOException
                                          or UnauthorizedAccessException
                                          or System.Text.Json.JsonException
                                          or InvalidDataException
                                          or ArgumentException)
        {
            return null;
        }
    }

    public UniversalDiagnosticSnapshot AnalyzeCurrentMachine(
        TelemetrySample sample,
        BottleneckAnalysisContext context)
        => Diagnostics.Analyze(Environment.Capture(), sample, context);

    public UniversalDiagnosticSnapshot AnalyzeCurrentMachine(
        TelemetryFrame frame,
        BottleneckAnalysisContext context)
        => Diagnostics.Analyze(Environment.Capture(), frame, context);

    public async ValueTask DisposeAsync()
    {
        GuardianHost.StatusChanged -= GuardianHost_StatusChanged;
        await GuardianHost.DisposeAsync().ConfigureAwait(false);
    }

    // Compatibility entry point used by existing pages while the app service graph evolves.
    public Task UpdateSettingsAsync(AppSettings settings) => SaveSettingsAsync(settings);

    private PerformanceConfigurationSnapshot? CapturePerformanceConfiguration()
    {
        var target = PerformanceCaptureTargetPolicy.FromGuardianStatus(GuardianHost.CurrentStatus);
        if (!target.CanCapture || string.IsNullOrWhiteSpace(target.InstanceName)) return null;

        var environment = Environment.Capture();
        var instance = environment.Instances.FirstOrDefault(item =>
            string.Equals(item.Name, target.InstanceName, StringComparison.OrdinalIgnoreCase));
        return instance is null
            ? null
            : PerformanceConfigurationSnapshot.Capture(environment, instance, environment.ActiveGame);
    }

    private PerformanceUniversalConfigurationContext? CapturePerformanceUniversalContext()
        => PerformanceWorkloadContext.Capture(CaptureMachineContext());

    private string CaptureWindowsBenchmarkWorkloadKey()
    {
        var target = PerformanceCaptureTargetPolicy.FromGuardianStatus(GuardianHost.CurrentStatus);
        if (!target.CanCapture || string.IsNullOrWhiteSpace(target.InstanceName))
            throw new InvalidOperationException(
                "Windows capability experiment requires an exact Guardian-bound workload before controlled measurement can start.");

        var environment = Environment.Capture();
        return $"bluestacks:{target.InstanceName.Trim().ToLowerInvariant()}:{environment.ActiveGame.ToString().ToLowerInvariant()}";
    }

    private void GuardianHost_StatusChanged(object? sender, GuardianLiveSessionStatus status)
        => PerformanceTimelineEvents.RecordGuardianStatus(status);

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