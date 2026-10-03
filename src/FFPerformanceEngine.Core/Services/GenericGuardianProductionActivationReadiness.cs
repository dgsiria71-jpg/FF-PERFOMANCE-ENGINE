using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Workloads;

namespace FFPerformanceEngine.Core.Services;

public enum GenericGuardianProductionActivationStatus
{
    Ready,
    GameNotResolved,
    AdapterContextEvidenceUnsupported,
    ProductionEvidenceSourceMissing,
    BudgetPolicyMissing,
    ApprovedCandidatePolicyMissing,
    CandidatePolicyInvalid,
    AmbiguousCandidatePolicy,
    AuthorizedMutationMappingMissing
}

public sealed record GenericGuardianProductionActivationReadiness(
    GenericGuardianProductionActivationStatus Status,
    string Reason)
{
    public bool Ready => Status == GenericGuardianProductionActivationStatus.Ready;
}

/// <summary>
/// Explicit product registration that marks one evidence factory as production
/// provenance for one stable GameId + adapter identity. Registration alone does
/// not assert that a scene is currently known; the factory can still return null
/// for a particular OS-owned session and the runtime then fails closed.
/// </summary>
public sealed record GenericGuardianProductionCanaryEvidenceRegistration(
    string GameId,
    string AdapterId,
    Func<GenericGuardianCanarySessionKey, IGenericGuardianCanaryEvidenceSource?> Factory);

/// <summary>
/// Immutable production provenance registry. It intentionally has no default
/// entries. A TEST implementation of IGenericGuardianCanaryEvidenceSource is not
/// trusted unless an owning composition explicitly registers it here.
/// </summary>
public sealed class GenericGuardianProductionCanaryEvidenceRegistry
{
    private readonly IReadOnlyDictionary<string, GenericGuardianProductionCanaryEvidenceRegistration> _byGameId;

    public GenericGuardianProductionCanaryEvidenceRegistry(
        IEnumerable<GenericGuardianProductionCanaryEvidenceRegistration> registrations)
    {
        ArgumentNullException.ThrowIfNull(registrations);
        var map = new Dictionary<string, GenericGuardianProductionCanaryEvidenceRegistration>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var registration in registrations)
        {
            ArgumentNullException.ThrowIfNull(registration);
            ArgumentNullException.ThrowIfNull(registration.Factory);
            var gameId = Normalize(registration.GameId);
            var adapterId = Normalize(registration.AdapterId);
            if (gameId.Length == 0 || adapterId.Length == 0)
                throw new ArgumentException(
                    "Production canary evidence registration requires stable GameId and AdapterId.",
                    nameof(registrations));

            var snapshot = new GenericGuardianProductionCanaryEvidenceRegistration(
                gameId,
                adapterId,
                registration.Factory);
            if (!map.TryAdd(gameId, snapshot))
                throw new ArgumentException(
                    $"Duplicate production canary evidence registration for GameId '{gameId}'.",
                    nameof(registrations));
        }

        _byGameId = map;
    }

    public bool IsRegistered(string? gameId, string? adapterId)
    {
        var game = Normalize(gameId);
        var adapter = Normalize(adapterId);
        return game.Length > 0
               && adapter.Length > 0
               && _byGameId.TryGetValue(game, out var registration)
               && string.Equals(registration.AdapterId, adapter, StringComparison.OrdinalIgnoreCase);
    }

    public bool TryCreate(
        GenericGuardianCanarySessionKey? session,
        string? adapterId,
        out IGenericGuardianCanaryEvidenceSource? source)
    {
        source = null;
        if (session is null
            || session.SessionEpoch == Guid.Empty
            || session.ProcessId <= 0
            || string.IsNullOrWhiteSpace(session.GameId)
            || string.IsNullOrWhiteSpace(session.ExecutablePath))
            return false;

        var game = Normalize(session.GameId);
        var adapter = Normalize(adapterId);
        if (!_byGameId.TryGetValue(game, out var registration)
            || !string.Equals(registration.AdapterId, adapter, StringComparison.OrdinalIgnoreCase))
            return false;

        source = registration.Factory(session);
        return source is not null;
    }

    public IGenericGuardianCanaryEvidenceSource? CreateForSession(
        GenericGuardianCanarySessionKey session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var game = Normalize(session.GameId);
        return _byGameId.TryGetValue(game, out var registration)
            ? registration.Factory(session)
            : null;
    }

    private static string Normalize(string? value)
        => value?.Trim().ToLowerInvariant() ?? string.Empty;
}

/// <summary>
/// Read-only product activation gate. It proves only that all explicit product
/// prerequisites have been registered; it does not prove current scene context,
/// outcome causality or HIL benefit.
/// </summary>
public sealed class GenericGuardianProductionActivationReadinessGate
{
    private readonly GenericGuardianProductionCanaryEvidenceRegistry _evidence;

    public GenericGuardianProductionActivationReadinessGate(
        GenericGuardianProductionCanaryEvidenceRegistry evidence)
        => _evidence = evidence ?? throw new ArgumentNullException(nameof(evidence));

    public GenericGuardianProductionActivationReadiness Evaluate(
        GenericGuardianWindowsRuntimeLoopPlan plan,
        GenericGuardianSessionActionBudget? budget,
        GenericGuardianSessionMutationCatalog? mutationCatalog,
        IEnumerable<GenericGuardianSessionActionCandidate>? candidates)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(plan.Catalog);

        var gameId = Normalize(plan.GameId);
        var matches = plan.Catalog.Games
            .Where(entry => entry?.Identity is not null
                            && entry.Adapter is not null
                            && Normalize(entry.Identity.GameId) == gameId)
            .Take(2)
            .ToArray();
        if (gameId.Length == 0 || matches.Length != 1)
            return NotReady(
                GenericGuardianProductionActivationStatus.GameNotResolved,
                "Activation requires exactly one already-resolved stable GameId and adapter.");

        var workload = matches[0];
        if (!workload.Adapter.Capabilities.CanaryContextEvidence)
            return NotReady(
                GenericGuardianProductionActivationStatus.AdapterContextEvidenceUnsupported,
                $"Adapter '{workload.Adapter.AdapterId}' does not declare production canary context evidence.");

        if (!_evidence.IsRegistered(gameId, workload.Adapter.AdapterId))
            return NotReady(
                GenericGuardianProductionActivationStatus.ProductionEvidenceSourceMissing,
                "No production canary evidence source is registered for the exact GameId/adapter pair.");

        if (budget is null)
            return NotReady(
                GenericGuardianProductionActivationStatus.BudgetPolicyMissing,
                "An explicit caller-owned per-session action budget policy is required.");

        if (mutationCatalog is null || candidates is null)
            return NotReady(
                GenericGuardianProductionActivationStatus.ApprovedCandidatePolicyMissing,
                "Explicit approved candidate and action-to-mutation policy is required.");

        var gameCandidates = candidates
            .Where(candidate => candidate is not null
                                && Normalize(candidate.GameId) == gameId)
            .ToArray();
        if (gameCandidates.Length == 0)
            return NotReady(
                GenericGuardianProductionActivationStatus.ApprovedCandidatePolicyMissing,
                "No explicit production action candidate is registered for the exact GameId.");

        foreach (var candidate in gameCandidates)
        {
            if (candidate.Action is null
                || string.IsNullOrWhiteSpace(candidate.Action.Id)
                || candidate.Action.Safety != ActionSafety.LiveSafe
                || !GenericGuardianClassifierSupportCatalog.For(candidate.Family).CanClassify)
            {
                return NotReady(
                    GenericGuardianProductionActivationStatus.CandidatePolicyInvalid,
                    "Every production candidate must be LiveSafe, named and belong to an evidence-backed supported anomaly family.");
            }
        }

        if (gameCandidates
            .GroupBy(candidate => candidate.Family)
            .Any(group => group.Count() != 1))
        {
            return NotReady(
                GenericGuardianProductionActivationStatus.AmbiguousCandidatePolicy,
                "More than one production candidate for the same anomaly family requires a ranking policy that does not exist.");
        }

        foreach (var candidate in gameCandidates)
        {
            if (!mutationCatalog.TryBind(candidate, out var binding)
                || binding is null)
            {
                return NotReady(
                    GenericGuardianProductionActivationStatus.AuthorizedMutationMappingMissing,
                    $"Candidate '{candidate.Action.Id}' has no exact authorized action-to-mutation binding.");
            }
        }

        return new GenericGuardianProductionActivationReadiness(
            GenericGuardianProductionActivationStatus.Ready,
            $"Exact workload '{workload.Identity.GameId}' has adapter context capability, registered production evidence and explicit unambiguous bound policy.");
    }

    private static GenericGuardianProductionActivationReadiness NotReady(
        GenericGuardianProductionActivationStatus status,
        string reason)
        => new(status, reason);

    private static string Normalize(string? value)
        => value?.Trim().ToLowerInvariant() ?? string.Empty;
}

public interface IGenericGuardianWindowsRuntimeScheduler : IAsyncDisposable
{
    bool IsRunning { get; }

    Task StartAsync(
        GenericGuardianWindowsRuntimeLoopPlan plan,
        CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Product-facing activation boundary. The low-level scheduler is never started
/// unless the immutable production readiness gate returns Ready for the exact
/// plan and the exact policy objects used to construct this controller.
/// </summary>
public sealed class GenericGuardianWindowsRuntimeActivationController : IAsyncDisposable
{
    private readonly IGenericGuardianWindowsRuntimeScheduler _scheduler;
    private readonly GenericGuardianProductionActivationReadinessGate _readiness;
    private readonly GenericGuardianSessionActionBudget _budget;
    private readonly GenericGuardianSessionMutationCatalog _mutationCatalog;
    private readonly IReadOnlyList<GenericGuardianSessionActionCandidate> _candidates;

    public GenericGuardianWindowsRuntimeActivationController(
        IGenericGuardianWindowsRuntimeScheduler scheduler,
        GenericGuardianProductionActivationReadinessGate readiness,
        GenericGuardianSessionActionBudget budget,
        GenericGuardianSessionMutationCatalog mutationCatalog,
        IEnumerable<GenericGuardianSessionActionCandidate> candidates)
    {
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _readiness = readiness ?? throw new ArgumentNullException(nameof(readiness));
        _budget = budget ?? throw new ArgumentNullException(nameof(budget));
        _mutationCatalog = mutationCatalog ?? throw new ArgumentNullException(nameof(mutationCatalog));
        ArgumentNullException.ThrowIfNull(candidates);
        _candidates = Array.AsReadOnly(candidates.Where(candidate => candidate is not null).ToArray());
    }

    public bool IsRunning => _scheduler.IsRunning;

    public GenericGuardianProductionActivationReadiness Assess(
        GenericGuardianWindowsRuntimeLoopPlan plan)
        => _readiness.Evaluate(plan, _budget, _mutationCatalog, _candidates);

    public async Task<GenericGuardianProductionActivationReadiness> StartAsync(
        GenericGuardianWindowsRuntimeLoopPlan plan,
        CancellationToken cancellationToken = default)
    {
        var readiness = Assess(plan);
        if (!readiness.Ready) return readiness;

        await _scheduler.StartAsync(plan, cancellationToken).ConfigureAwait(false);
        return readiness;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
        => _scheduler.StopAsync(cancellationToken);

    public ValueTask DisposeAsync() => _scheduler.DisposeAsync();
}
