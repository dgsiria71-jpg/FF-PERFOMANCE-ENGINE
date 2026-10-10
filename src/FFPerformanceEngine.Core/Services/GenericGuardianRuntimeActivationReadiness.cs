using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Workloads;

namespace FFPerformanceEngine.Core.Services;

public enum GenericGuardianRuntimeActivationReadinessStatus
{
    NotEvaluated,
    Ready,
    GameNotResolved,
    AdapterCanaryContextUnavailable,
    EvidenceSourceNotRegistered,
    NoApprovedCandidates,
    InvalidCandidatePolicy,
    MissingMutationBinding
}

public sealed record GenericGuardianRuntimeActivationReadiness(
    GenericGuardianRuntimeActivationReadinessStatus Status,
    string Reason)
{
    public bool IsReady => Status == GenericGuardianRuntimeActivationReadinessStatus.Ready;

    public static GenericGuardianRuntimeActivationReadiness NotEvaluated { get; } =
        new(
            GenericGuardianRuntimeActivationReadinessStatus.NotEvaluated,
            "Generic Guardian scheduled activation readiness has not been evaluated.");
}

public interface IGenericGuardianRuntimeActivationReadinessGate
{
    GenericGuardianRuntimeActivationReadiness Evaluate(
        GenericGuardianWindowsRuntimeLoopPlan plan);
}

/// <summary>
/// Explicit registration of one adapter-owned production canary comparison
/// source. Registration identifies the adapter/source authority; it does not
/// make an adapter truthful by itself. Activation still requires the adapter's
/// own CanaryContextEvidence capability declaration.
/// </summary>
public sealed class GenericGuardianCanaryEvidenceSourceRegistration
{
    private readonly Func<GenericGuardianCanarySessionKey, IGenericGuardianCanaryEvidenceSource?> _factory;

    public GenericGuardianCanaryEvidenceSourceRegistration(
        string adapterId,
        string sourceId,
        Func<GenericGuardianCanarySessionKey, IGenericGuardianCanaryEvidenceSource?> factory)
    {
        if (string.IsNullOrWhiteSpace(adapterId))
            throw new ArgumentException("A stable adapter id is required.", nameof(adapterId));
        if (string.IsNullOrWhiteSpace(sourceId))
            throw new ArgumentException("A stable evidence source id is required.", nameof(sourceId));

        AdapterId = adapterId.Trim();
        SourceId = sourceId.Trim();
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public string AdapterId { get; }
    public string SourceId { get; }

    public IGenericGuardianCanaryEvidenceSource? Create(
        GenericGuardianCanarySessionKey session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return _factory(session);
    }

    internal bool MatchesAdapter(string? adapterId)
        => !string.IsNullOrWhiteSpace(adapterId)
           && string.Equals(
               AdapterId,
               adapterId.Trim(),
               StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Start-time fail-closed gate for the scheduled generic Guardian runtime.
/// It validates only explicit production registration: the exact resolved game
/// adapter must declare CanaryContextEvidence, a matching evidence source must
/// be registered, at least one explicit LiveSafe supported candidate must exist,
/// and every candidate for this GameId must have an exact catalog binding.
///
/// The caller-supplied GenericGuardianSessionActionBudget is required at
/// construction, so no hidden/default attempt policy can enter this gate.
/// Readiness does not prove performance benefit or causal validity; HIL remains
/// a later activation prerequisite.
/// </summary>
public sealed class GenericGuardianRuntimeActivationReadinessGate
    : IGenericGuardianRuntimeActivationReadinessGate
{
    private readonly IReadOnlyList<GenericGuardianSessionActionCandidate> _candidates;
    private readonly GenericGuardianSessionMutationCatalog _mutationCatalog;
    private readonly GenericGuardianCanaryEvidenceSourceRegistration? _evidenceRegistration;

    public GenericGuardianRuntimeActivationReadinessGate(
        IEnumerable<GenericGuardianSessionActionCandidate> candidates,
        GenericGuardianSessionMutationCatalog mutationCatalog,
        GenericGuardianSessionActionBudget budget,
        GenericGuardianCanaryEvidenceSourceRegistration? evidenceRegistration)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        _candidates = Array.AsReadOnly(candidates.ToArray());
        _mutationCatalog = mutationCatalog ?? throw new ArgumentNullException(nameof(mutationCatalog));
        _ = budget ?? throw new ArgumentNullException(nameof(budget));
        _evidenceRegistration = evidenceRegistration;
    }

    public GenericGuardianRuntimeActivationReadiness Evaluate(
        GenericGuardianWindowsRuntimeLoopPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(plan.Catalog);

        var games = plan.Catalog.Games
            .Where(game => game is not null
                           && EqualId(game.Identity?.GameId, plan.GameId))
            .Take(2)
            .ToArray();
        if (games.Length != 1)
        {
            return NotReady(
                GenericGuardianRuntimeActivationReadinessStatus.GameNotResolved,
                "The scheduled GameId must resolve to exactly one caller-supplied catalog entry.");
        }

        var game = games[0];
        var adapter = game.Adapter;
        if (adapter is null || !adapter.Capabilities.CanaryContextEvidence)
        {
            return NotReady(
                GenericGuardianRuntimeActivationReadinessStatus.AdapterCanaryContextUnavailable,
                $"Adapter '{adapter?.AdapterId ?? "(missing)"}' does not declare production canary context evidence.");
        }

        if (_evidenceRegistration is null
            || !_evidenceRegistration.MatchesAdapter(adapter.AdapterId))
        {
            return NotReady(
                GenericGuardianRuntimeActivationReadinessStatus.EvidenceSourceNotRegistered,
                $"No registered canary evidence source matches adapter '{adapter.AdapterId}'.");
        }

        var candidates = _candidates
            .Where(candidate => candidate is not null
                                && EqualId(candidate.GameId, plan.GameId))
            .ToArray();
        if (candidates.Length == 0)
        {
            return NotReady(
                GenericGuardianRuntimeActivationReadinessStatus.NoApprovedCandidates,
                "No explicit generic Guardian candidate policy is registered for the scheduled GameId.");
        }

        foreach (var candidate in candidates)
        {
            if (candidate.Action is null
                || string.IsNullOrWhiteSpace(candidate.Action.Id)
                || candidate.Action.Safety != ActionSafety.LiveSafe
                || !GenericGuardianClassifierSupportCatalog.For(candidate.Family).CanClassify)
            {
                return NotReady(
                    GenericGuardianRuntimeActivationReadinessStatus.InvalidCandidatePolicy,
                    "Every activation candidate must be explicit LiveSafe and belong to a supported classifier family.");
            }

            if (!_mutationCatalog.TryBind(candidate, out var binding) || binding is null)
            {
                return NotReady(
                    GenericGuardianRuntimeActivationReadinessStatus.MissingMutationBinding,
                    $"Candidate '{candidate.Action.Id}' has no exact registered Windows mutation binding.");
            }
        }

        return new GenericGuardianRuntimeActivationReadiness(
            GenericGuardianRuntimeActivationReadinessStatus.Ready,
            $"Adapter '{adapter.AdapterId}', evidence source '{_evidenceRegistration.SourceId}', explicit candidate/catalog policy and caller-defined budget are registered.");
    }

    private static GenericGuardianRuntimeActivationReadiness NotReady(
        GenericGuardianRuntimeActivationReadinessStatus status,
        string reason)
        => new(status, reason);

    private static bool EqualId(string? left, string? right)
        => !string.IsNullOrWhiteSpace(left)
           && !string.IsNullOrWhiteSpace(right)
           && string.Equals(
               left.Trim(),
               right.Trim(),
               StringComparison.OrdinalIgnoreCase);
}

internal sealed class GenericGuardianRuntimeNotConfiguredReadinessGate
    : IGenericGuardianRuntimeActivationReadinessGate
{
    public GenericGuardianRuntimeActivationReadiness Evaluate(
        GenericGuardianWindowsRuntimeLoopPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        return new GenericGuardianRuntimeActivationReadiness(
            GenericGuardianRuntimeActivationReadinessStatus.EvidenceSourceNotRegistered,
            "No scheduled generic Guardian activation-readiness policy was supplied.");
    }
}
