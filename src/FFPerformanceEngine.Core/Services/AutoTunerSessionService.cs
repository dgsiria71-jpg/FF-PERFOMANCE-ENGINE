using System.Collections.Concurrent;
using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public interface IAutoTunerRuntimeFactory
{
    IAutoTunerRuntime Create(BlueStacksInstance instance);
}

public interface IAutoTunerSessionRunner
{
    Task<AutoTunerSessionResult> RunGeneratedAsync(
        EnvironmentSnapshot environment,
        BlueStacksInstance instance,
        GameKind game,
        AutoTunerMode mode,
        Action<AutoTunerRunProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public sealed class BlueStacksAutoTunerRuntimeFactory : IAutoTunerRuntimeFactory
{
    private readonly BlueStacksService _blueStacks;
    private readonly BlueStacksAutomationService _automation;
    private readonly PresentMonService _presentMon;
    private readonly IOwnedProcessController? _processController;
    private readonly AutoTunerRuntimeOptions? _runtimeOptions;

    public BlueStacksAutoTunerRuntimeFactory(
        BlueStacksService blueStacks,
        BlueStacksAutomationService automation,
        PresentMonService presentMon,
        IOwnedProcessController? processController = null,
        AutoTunerRuntimeOptions? runtimeOptions = null)
    {
        _blueStacks = blueStacks ?? throw new ArgumentNullException(nameof(blueStacks));
        _automation = automation ?? throw new ArgumentNullException(nameof(automation));
        _presentMon = presentMon ?? throw new ArgumentNullException(nameof(presentMon));
        _processController = processController;
        _runtimeOptions = runtimeOptions;
    }

    public IAutoTunerRuntime Create(BlueStacksInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        var platform = new BlueStacksAutoTunerPlatform(_blueStacks, _automation, _presentMon, _processController);
        return new BlueStacksAutoTunerRuntime(instance, platform, _runtimeOptions);
    }
}

public sealed record AutoTunerSessionResult(
    TuningResult Tuning,
    string InstanceName,
    int CandidateCount,
    bool ProfilesPersisted);

public sealed class AutoTunerSessionService : IAutoTunerSessionRunner
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> InstanceGates = new(StringComparer.OrdinalIgnoreCase);
    private readonly AutoTunerEngine _engine;
    private readonly IAutoTunerRuntimeFactory _runtimeFactory;
    private readonly ProfileService _profiles;
    private readonly HistoryService _history;
    private readonly AutoTunerValidationPolicy? _validationPolicy;

    public AutoTunerSessionService(
        AutoTunerEngine engine,
        IAutoTunerRuntimeFactory runtimeFactory,
        ProfileService profiles,
        HistoryService history,
        AutoTunerValidationPolicy? validationPolicy = null)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _runtimeFactory = runtimeFactory ?? throw new ArgumentNullException(nameof(runtimeFactory));
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _history = history ?? throw new ArgumentNullException(nameof(history));
        _validationPolicy = validationPolicy;
    }

    public Task<AutoTunerSessionResult> RunGeneratedAsync(
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
        return RunCandidatesAsync(instance, game, mode, candidates, progress, cancellationToken);
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
        if (string.IsNullOrWhiteSpace(instance.Name)) throw new ArgumentException("A named BlueStacks instance is required for a persistent tuning session.", nameof(instance));
        if (game is not (GameKind.FreeFire or GameKind.FreeFireMax)) throw new ArgumentOutOfRangeException(nameof(game), "Select Free Fire or Free Fire MAX.");

        var gate = InstanceGates.GetOrAdd(instance.Name, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var runtime = _runtimeFactory.Create(instance);
            var coordinator = new AutoTunerRunCoordinator(_engine, runtime, _validationPolicy);
            var tuning = await coordinator.RunAsync(game, mode, candidates, progress, cancellationToken).ConfigureAwait(false);
            var boundTuning = BindInstance(tuning, instance.Name);
            var persisted = false;

            if (boundTuning.Winners.Count > 0 && boundTuning.Winners.All(x => x.Evidence == EvidenceLevel.Validated))
            {
                await _profiles.ReplaceAutoTunerWinnersAsync(game, instance.Name, boundTuning.Winners, CancellationToken.None).ConfigureAwait(false);
                persisted = true;
            }

            await _history.AppendAsync(new HistoryEvent
            {
                Kind = HistoryEventKind.Optimization,
                Title = persisted ? "Auto Tuner optimization completed" : "Auto Tuner optimization inconclusive",
                Summary = persisted
                    ? $"{boundTuning.Winners.Count} validated winner roles persisted for {game} on instance {instance.Name} from {candidates.Count} candidate(s)."
                    : $"No validated winner set replaced the known-good profiles for {game} on instance {instance.Name}. {candidates.Count} candidate(s) evaluated.",
                DetailsJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    game,
                    mode,
                    instance = instance.Name,
                    candidates = candidates.Count,
                    winnerKinds = boundTuning.Winners.Select(x => x.Kind.ToString()).ToArray(),
                    persisted
                })
            }, CancellationToken.None).ConfigureAwait(false);

            return new AutoTunerSessionResult(boundTuning, instance.Name, candidates.Count, persisted);
        }
        finally
        {
            gate.Release();
        }
    }

    private static TuningResult BindInstance(TuningResult tuning, string instanceName)
    {
        if (tuning.Winners.Count == 0) return tuning;
        return tuning with
        {
            Winners = tuning.Winners.Select(profile => profile with { InstanceName = instanceName }).ToList()
        };
    }
}
