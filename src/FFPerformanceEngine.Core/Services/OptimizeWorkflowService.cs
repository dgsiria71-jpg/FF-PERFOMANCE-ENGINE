using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public interface IOptimizeSystemProbe
{
    EnvironmentSnapshot CaptureEnvironment();
    bool IsPlayerRunning();
    bool IsFrameTelemetryReady();
}

public sealed class OptimizeSystemProbe : IOptimizeSystemProbe
{
    private readonly EnvironmentProbe _environment;
    private readonly BlueStacksService _blueStacks;
    private readonly PresentMonService _presentMon;

    public OptimizeSystemProbe(EnvironmentProbe environment, BlueStacksService blueStacks, PresentMonService presentMon)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _blueStacks = blueStacks ?? throw new ArgumentNullException(nameof(blueStacks));
        _presentMon = presentMon ?? throw new ArgumentNullException(nameof(presentMon));
    }

    public EnvironmentSnapshot CaptureEnvironment() => _environment.Capture();
    public bool IsPlayerRunning() => _blueStacks.IsPlayerRunning();
    public bool IsFrameTelemetryReady() => _presentMon.FindExecutable() is not null;
}

public sealed record OptimizeReadiness
{
    public required EnvironmentSnapshot Environment { get; init; }
    public BlueStacksInstance? Instance { get; init; }
    public IReadOnlyList<TuningCandidate> Candidates { get; init; } = Array.Empty<TuningCandidate>();
    public bool CanStart { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed record OptimizeWorkflowResult
{
    public required AutoTunerSessionResult Session { get; init; }
    public required string Summary { get; init; }
    public PerformanceProfile? Recommended { get; init; }
}

public sealed class OptimizeWorkflowService
{
    private readonly AutoTunerEngine _engine;
    private readonly IAutoTunerSessionRunner _sessionRunner;
    private readonly IOptimizeSystemProbe _systemProbe;

    public OptimizeWorkflowService(
        AutoTunerEngine engine,
        IAutoTunerSessionRunner sessionRunner,
        IOptimizeSystemProbe systemProbe)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _sessionRunner = sessionRunner ?? throw new ArgumentNullException(nameof(sessionRunner));
        _systemProbe = systemProbe ?? throw new ArgumentNullException(nameof(systemProbe));
    }

    public OptimizeReadiness Analyze(GameKind game, AutoTunerMode mode, string? instanceName = null)
    {
        var environment = _systemProbe.CaptureEnvironment();
        if (game is not (GameKind.FreeFire or GameKind.FreeFireMax))
            return NotReady(environment, "Selecione Free Fire ou Free Fire MAX antes de iniciar a otimização.");

        if (!environment.BlueStacksDetected || environment.Instances.Count == 0)
            return NotReady(environment, "BlueStacks não foi detectado com uma instância configurada.");

        BlueStacksInstance? instance;
        if (!string.IsNullOrWhiteSpace(instanceName))
        {
            instance = environment.Instances.FirstOrDefault(x => string.Equals(x.Name, instanceName, StringComparison.OrdinalIgnoreCase));
            if (instance is null)
                return NotReady(environment, $"A instância BlueStacks '{instanceName}' não está disponível. Nenhuma outra instância será escolhida silenciosamente.");
        }
        else
        {
            instance = environment.Instances.First();
        }

        var candidates = _engine.GenerateCandidates(environment, instance, mode);
        if (candidates.Count == 0)
            return NotReady(environment, "Nenhum candidato seguro pôde ser gerado para esta configuração.", instance, candidates);

        if (_systemProbe.IsPlayerRunning())
            return NotReady(
                environment,
                "Feche o BlueStacks App Player ativo antes de iniciar. A otimização nunca encerrará uma instância que não pertença à sessão do Auto Tuner.",
                instance,
                candidates);

        if (!_systemProbe.IsFrameTelemetryReady())
            return NotReady(
                environment,
                "PresentMon não está disponível. Instale a dependência de telemetria antes de iniciar para que nenhum vencedor seja escolhido sem medição real.",
                instance,
                candidates);

        return new OptimizeReadiness
        {
            Environment = environment,
            Instance = instance,
            Candidates = candidates,
            CanStart = true,
            Message = $"Pronto para testar {candidates.Count} candidato(s) com rollback automático na instância {instance.Name}."
        };
    }

    public async Task<OptimizeWorkflowResult> RunAsync(
        GameKind game,
        AutoTunerMode mode,
        string? instanceName = null,
        Action<AutoTunerProgressPresentation>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var readiness = Analyze(game, mode, instanceName);
        if (!readiness.CanStart || readiness.Instance is null)
            throw new InvalidOperationException(readiness.Message);

        Action<AutoTunerRunProgress>? engineProgress = progress is null
            ? null
            : item => progress(AutoTunerPresentation.FromProgress(item));

        var session = await _sessionRunner.RunGeneratedAsync(
            readiness.Environment,
            readiness.Instance,
            game,
            mode,
            engineProgress,
            cancellationToken).ConfigureAwait(false);

        var recommended = session.Tuning.Winners
            .FirstOrDefault(x => x.Kind == ProfileKind.Recommended && x.Evidence == EvidenceLevel.Validated);

        return new OptimizeWorkflowResult
        {
            Session = session,
            Recommended = recommended,
            Summary = AutoTunerPresentation.Summarize(session)
        };
    }

    private static OptimizeReadiness NotReady(
        EnvironmentSnapshot environment,
        string message,
        BlueStacksInstance? instance = null,
        IReadOnlyList<TuningCandidate>? candidates = null)
        => new()
        {
            Environment = environment,
            Instance = instance,
            Candidates = candidates ?? Array.Empty<TuningCandidate>(),
            CanStart = false,
            Message = message
        };
}
