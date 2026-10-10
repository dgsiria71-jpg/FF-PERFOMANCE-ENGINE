using System.Collections.ObjectModel;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Telemetry;

namespace FFPerformanceEngine.Core.Services;

/// <summary>
/// Explicit declaration of one already-known action candidate for one stable
/// workload identity and one Guardian anomaly family. Declaration alone grants
/// no execution, validation or persistence authority.
/// </summary>
public sealed record GenericGuardianSessionActionCandidate
{
    public required string GameId { get; init; }
    public required GuardianAnomalyKind Family { get; init; }
    public required GuardianAction Action { get; init; }
}

public sealed record GenericGuardianSessionActionEligibility
{
    public required GuardianWorkloadStateSnapshot State { get; init; }
    public required GuardianAnomalyKind Family { get; init; }
    public required IReadOnlyList<GenericGuardianSessionActionCandidate> EligibleCandidates { get; init; }
    public string Reason { get; init; } = string.Empty;

    public bool HasEligibleCandidates => EligibleCandidates.Count > 0;
}

/// <summary>
/// Read-only Track 6 session-action eligibility boundary. It filters only
/// explicitly supplied candidates and never ranks, synthesizes or executes an
/// action.
/// </summary>
public sealed class GenericGuardianSessionActionSelector
{
    public GenericGuardianSessionActionEligibility SelectEligible(
        GuardianWorkloadStateSnapshot state,
        GuardianAnomalyKind family,
        IEnumerable<GenericGuardianSessionActionCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(candidates);

        if (state.State != GuardianWorkloadState.Active)
            return Empty(state, family, $"Workload state {state.State} is not Active; live-session action eligibility is unavailable.");

        if (state.Confidence != GuardianWorkloadStateConfidence.High)
            return Empty(state, family, $"Active workload confidence {state.Confidence} is not High; live-session action eligibility fails closed.");

        var target = state.Target;
        if (target is null
            || target.BindingQuality != TelemetryWorkloadBindingQuality.ExactRunningProcess
            || !target.CanCaptureProcess
            || string.IsNullOrWhiteSpace(target.GameId))
        {
            return Empty(state, family, "Active workload does not retain one exact capturable stable GameId target; live-session action eligibility fails closed.");
        }

        var support = GenericGuardianClassifierSupportCatalog.For(family);
        if (!support.CanClassify)
            return Empty(state, family, $"Guardian anomaly family {family} is {support.Support} and is not actionable in the live-session eligibility boundary.");

        var targetGameId = target.GameId.Trim();
        var eligible = candidates
            .Where(candidate => candidate is not null
                                && candidate.Action is not null
                                && candidate.Action.Safety == ActionSafety.LiveSafe
                                && candidate.Family == family
                                && !string.IsNullOrWhiteSpace(candidate.GameId)
                                && string.Equals(
                                    candidate.GameId.Trim(),
                                    targetGameId,
                                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new GenericGuardianSessionActionEligibility
        {
            State = state,
            Family = family,
            EligibleCandidates = new ReadOnlyCollection<GenericGuardianSessionActionCandidate>(eligible),
            Reason = eligible.Count == 0
                ? "No explicitly supplied LiveSafe candidate matches the exact workload and evidence-backed anomaly family."
                : $"{eligible.Count} explicitly supplied LiveSafe candidate(s) match the exact workload and evidence-backed anomaly family."
        };
    }

    private static GenericGuardianSessionActionEligibility Empty(
        GuardianWorkloadStateSnapshot state,
        GuardianAnomalyKind family,
        string reason)
        => new()
        {
            State = state,
            Family = family,
            EligibleCandidates = Array.AsReadOnly(Array.Empty<GenericGuardianSessionActionCandidate>()),
            Reason = reason
        };
}
