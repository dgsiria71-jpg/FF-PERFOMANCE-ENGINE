using System.Text.Json;
using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public interface IAutoTunerRuntimeFactory
{
    IAutoTunerRuntime Create(BlueStacksInstance instance);
}

public sealed class BlueStacksAutoTunerRuntimeFactory : IAutoTunerRuntimeFactory
{
    private readonly BlueStacksService _blueStacks;
    private readonly BlueStacksAutomationService _automation;
    private readonly PresentMonService _presentMon;
    private readonly AutoTunerRuntimeOptions _options;
    private readonly IOwnedProcessController _processController;

    public BlueStacksAutoTunerRuntimeFactory(
        BlueStacksService blueStacks,
        BlueStacksAutomationService automation,
        PresentMonService presentMon,
        AutoTunerRuntimeOptions? options = null,
        IOwnedProcessController? processController = null)
    {
        _blueStacks = blueStacks ?? throw new ArgumentNullException(nameof(blueStacks));
        _automation = automation ?? throw new ArgumentNullException(nameof(automation));
        _presentMon = presentMon ?? throw new ArgumentNullException(nameof(presentMon));
        _options = options ?? new AutoTunerRuntimeOptions();
        _processController = processController ?? new OwnedProcessController();
    }

    public IAutoTunerRuntime Create(BlueStacksInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        var platform = new BlueStacksAutoTunerPlatform(_blueStacks, _automation, _presentMon, _processController);
        return new BlueStacksAutoTunerRuntime(instance, platform, _options);
    }
}

public sealed record AutoTunerSessionResult(
    TuningResult Tuning,
    string InstanceName,
    int CandidateCount,
    bool ProfilesPersisted);

public sealed class AutoTunerSessionService
{
    private readonly AutoTunerEngine _engine;
    private readonly IAutoTunerRuntimeFactory _runtimeFactory;
    private readonly ProfileService _profiles;
    private readonly HistoryService _history;
    private readonly AutoTunerValidationPolicy _validation;
    private readonly SemaphoreSlim _runGate = new(1, 1);

    public AutoTunerSessionService(
        AutoTunerEngine engine,
        IAutoTunerRuntimeFactory runtimeFactory,
        ProfileService profiles,
        HistoryService history,
        AutoTunerValidationPolicy? validation = null)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _runtimeFactory = runtimeFactory ?? throw new ArgumentNullException(nameof(runtimeFactory));
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _history = history ?? throw new ArgumentNullException(nameof(history));
        _validation = validation ?? new AutoTunerValidationPolicy();
    }

    public async Task<AutoTunerSessionResult> RunGeneratedAsync(
        EnvironmentSnapshot environment,
        BlueStacksInstance instance,
        GameKind game,
        AutoTunerMode mode,
        Action<AutoTunerRunProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(instance);
        var candidates = _engine.GenerateCandidates(environment, instance, mode);
        return await RunCandidatesAsync(instance, game, mode, candidates, progress, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AutoTunerSessionResult> RunCandidatesAsync(
        BlueStacksInstance instance,
        GameKind game,
        AutoTunerMode mode,
        IReadOnlyList<TuningCandidate> candidates,
        Action<AutoTunerRunProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(candidates);
        if (string.IsNullOrWhiteSpace(instance.Name))
            throw new ArgumentException("Selected BlueStacks instance does not have a stable name.", nameof(instance));
        if (game is not (GameKind.FreeFire or GameKind.FreeFireMax))
            throw new ArgumentOutOfRangeException(nameof(game), game, "Auto Tuner requires Free Fire or Free Fire MAX.");
        if (candidates.Count == 0)
            throw new ArgumentException("Auto Tuner requires at least one candidate.", nameof(candidates));

        if (!await _runGate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("Another Auto Tuner session is already running. Restart-required tuning sessions cannot overlap.");

        try
        {
            var runtime = _runtimeFactory.Create(instance);
            var coordinator = new AutoTunerRunCoordinator(_engine, runtime, _validation);
            var tuning = await coordinator.RunAsync(game, mode, candidates, progress, cancellationToken).ConfigureAwait(false);

            var boundWinners = tuning.Winners
                .Select(profile => profile with { InstanceName = instance.Name })
                .ToList();
            var boundTuning = tuning with { Winners = boundWinners };

            var persisted = false;
            if (boundWinners.Count > 0)
            {
                if (boundWinners.Any(profile => profile.Evidence != EvidenceLevel.Validated))
                    throw new InvalidOperationException("Auto Tuner returned an unvalidated winner; refusing to persist the generated winner set.");

                await _profiles.ReplaceAutoTunerWinnersAsync(game, instance.Name, boundWinners, cancellationToken).ConfigureAwait(false);
                persisted = true;
            }

            var summary = persisted
                ? $"{boundWinners.Count} vencedor(es) validados · modo {mode} · instância {instance.Name} · {candidates.Count} candidato(s) explorado(s)."
                : $"0 vencedores validados · modo {mode} · instância {instance.Name} · {candidates.Count} candidato(s) explorado(s); perfis conhecidos foram preservados.";

            await _history.AppendAsync(new HistoryEvent
            {
                Kind = HistoryEventKind.Optimization,
                Title = $"Auto Tuner concluído: {DisplayGame(game)}",
                Summary = summary,
                DetailsJson = JsonSerializer.Serialize(new
                {
                    game = game.ToString(),
                    mode = mode.ToString(),
                    instance = instance.Name,
                    candidates = candidates.Count,
                    evidence = boundTuning.Evidence.Count,
                    validatedEvidence = boundTuning.Evidence.Count(x => x.Evidence == EvidenceLevel.Validated),
                    winners = boundWinners.Select(x => new
                    {
                        kind = x.Kind.ToString(),
                        x.Name,
                        x.Confidence,
                        x.AverageFps,
                        x.OnePercentLow,
                        x.LatencyMs,
                        x.StutterPercent
                    }).ToArray(),
                    profilesPersisted = persisted
                })
            }, cancellationToken).ConfigureAwait(false);

            return new(boundTuning, instance.Name, candidates.Count, persisted);
        }
        finally
        {
            _runGate.Release();
        }
    }

    private static string DisplayGame(GameKind game) => game switch
    {
        GameKind.FreeFire => "Free Fire",
        GameKind.FreeFireMax => "Free Fire MAX",
        _ => game.ToString()
    };
}
