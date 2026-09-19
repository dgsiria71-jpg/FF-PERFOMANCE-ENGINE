using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Telemetry;

namespace FFPerformanceEngine.Core.Services;

/// <summary>
/// An explicitly owned process lifecycle. The stable GameId is not a PID;
/// SessionEpoch must change when a real process lifecycle changes, including
/// Windows PID reuse. Only the eventual session host can assign this epoch.
/// </summary>
public sealed record GenericGuardianCanarySessionKey(
    Guid SessionEpoch,
    string GameId,
    int ProcessId,
    string ExecutablePath);

public enum GenericGuardianCanaryAdmissionStatus
{
    Allowed,
    InCooldown,
    BudgetExhausted,
    Ineligible
}

public sealed record GenericGuardianCanaryAdmission(
    GenericGuardianCanaryAdmissionStatus Status,
    int RemainingAttempts,
    DateTimeOffset? CooldownUntil = null)
{
    public bool Allowed => Status == GenericGuardianCanaryAdmissionStatus.Allowed;
}

/// <summary>
/// Policy-only throttle for generic live-session canary attempts. This is NOT
/// the full historical multi-category recovery/graphics/Windows Action Budget.
/// Admission consumes one slot immediately, even if a subsequent canary fails;
/// no executor, host, learning, discovery or Windows mutation is composed here.
/// </summary>
public sealed class GenericGuardianSessionActionBudget
{
    private readonly object _gate = new();
    private readonly TimeSpan _cooldown;
    private readonly int _maxAttemptsPerSession;
    private readonly Func<DateTimeOffset> _clock;
    private readonly Dictionary<Guid, SessionCounters> _sessions = new();

    public GenericGuardianSessionActionBudget(
        TimeSpan cooldown,
        int maxAttemptsPerSession,
        Func<DateTimeOffset>? clock = null)
    {
        if (cooldown <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(cooldown));
        if (maxAttemptsPerSession <= 0) throw new ArgumentOutOfRangeException(nameof(maxAttemptsPerSession));
        _cooldown = cooldown;
        _maxAttemptsPerSession = maxAttemptsPerSession;
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
    }

    public GenericGuardianCanaryAdmission TryAdmit(
        GenericGuardianCanarySessionKey? session,
        GenericGuardianSessionActionEligibility? eligibility,
        GenericGuardianSessionActionCandidate? candidate)
    {
        if (!IsEligible(session, eligibility, candidate))
            return new GenericGuardianCanaryAdmission(
                GenericGuardianCanaryAdmissionStatus.Ineligible,
                _maxAttemptsPerSession);

        // IsEligible established these objects and fields as non-null.
        var exactSession = session!;
        var exactCandidate = candidate!;
        var gameId = Normalize(exactSession.GameId);
        var executablePath = Normalize(exactSession.ExecutablePath);
        var action = (exactCandidate.Family, Normalize(exactCandidate.Action.Id));

        lock (_gate)
        {
            _sessions.TryGetValue(exactSession.SessionEpoch, out var counters);
            if (counters is not null && !counters.Matches(gameId, exactSession.ProcessId, executablePath))
            {
                // An epoch cannot be silently rebound to another GameId/PID/path.
                return new GenericGuardianCanaryAdmission(
                    GenericGuardianCanaryAdmissionStatus.Ineligible,
                    _maxAttemptsPerSession - counters.Attempts);
            }

            var consumed = counters?.Attempts ?? 0;
            var remaining = _maxAttemptsPerSession - consumed;
            if (remaining <= 0)
                return new GenericGuardianCanaryAdmission(GenericGuardianCanaryAdmissionStatus.BudgetExhausted, 0);

            var now = _clock();
            if (counters is not null
                && counters.Cooldowns.TryGetValue(action, out var until)
                && now < until)
            {
                return new GenericGuardianCanaryAdmission(
                    GenericGuardianCanaryAdmissionStatus.InCooldown,
                    remaining,
                    until);
            }

            DateTimeOffset nextAllowedAt;
            try
            {
                nextAllowedAt = now.Add(_cooldown);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Invalid timestamp arithmetic must not consume an attempt.
                return new GenericGuardianCanaryAdmission(GenericGuardianCanaryAdmissionStatus.Ineligible, remaining);
            }

            if (counters is null)
            {
                counters = new SessionCounters(gameId, exactSession.ProcessId, executablePath);
                _sessions.Add(exactSession.SessionEpoch, counters);
            }

            counters.Attempts++;
            counters.Cooldowns[action] = nextAllowedAt;
            return new GenericGuardianCanaryAdmission(
                GenericGuardianCanaryAdmissionStatus.Allowed,
                _maxAttemptsPerSession - counters.Attempts,
                nextAllowedAt);
        }
    }

    /// <summary>
    /// Called only by the future owning host AFTER active reversible leases have
    /// been restored at real session end; never use reset to refill an active run.
    /// A mismatched identity may not reset another lifecycle's budget.
    /// </summary>
    public bool ResetSession(GenericGuardianCanarySessionKey? session)
    {
        if (session is null
            || session.SessionEpoch == Guid.Empty
            || string.IsNullOrWhiteSpace(session.GameId)
            || session.ProcessId <= 0
            || string.IsNullOrWhiteSpace(session.ExecutablePath))
            return false;

        lock (_gate)
        {
            if (!_sessions.TryGetValue(session.SessionEpoch, out var counters)
                || !counters.Matches(Normalize(session.GameId), session.ProcessId, Normalize(session.ExecutablePath)))
                return false;

            return _sessions.Remove(session.SessionEpoch);
        }
    }

    private static bool IsEligible(
        GenericGuardianCanarySessionKey? session,
        GenericGuardianSessionActionEligibility? eligibility,
        GenericGuardianSessionActionCandidate? candidate)
    {
        if (session is null
            || eligibility is null
            || candidate is null
            || session.SessionEpoch == Guid.Empty
            || session.ProcessId <= 0
            || string.IsNullOrWhiteSpace(session.GameId)
            || string.IsNullOrWhiteSpace(session.ExecutablePath)
            || eligibility.State is not { State: GuardianWorkloadState.Active,
                                          Confidence: GuardianWorkloadStateConfidence.High } state)
            return false;

        var target = state.Target;
        if (target is null
            || target.BindingQuality != TelemetryWorkloadBindingQuality.ExactRunningProcess
            || !target.CanCaptureProcess
            || target.ProcessId != session.ProcessId
            || !EqualId(target.GameId, session.GameId)
            || !EqualId(target.ExecutablePath, session.ExecutablePath))
            return false;

        if (candidate.Action is null
            || candidate.Action.Safety != ActionSafety.LiveSafe
            || string.IsNullOrWhiteSpace(candidate.Action.Id)
            || candidate.Family != eligibility.Family
            || !GenericGuardianClassifierSupportCatalog.For(candidate.Family).CanClassify
            || !EqualId(candidate.GameId, session.GameId))
            return false;

        return eligibility.EligibleCandidates is not null
               && eligibility.EligibleCandidates.Any(item => ReferenceEquals(item, candidate));
    }

    private static bool EqualId(string? left, string? right)
        => !string.IsNullOrWhiteSpace(left)
           && !string.IsNullOrWhiteSpace(right)
           && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private sealed class SessionCounters(string gameId, int processId, string executablePath)
    {
        public int Attempts { get; set; }
        public Dictionary<(GuardianAnomalyKind Family, string ActionId), DateTimeOffset> Cooldowns { get; } = new();

        public bool Matches(string otherGameId, int otherProcessId, string otherExecutablePath)
            => processId == otherProcessId
               && string.Equals(gameId, otherGameId, StringComparison.Ordinal)
               && string.Equals(executablePath, otherExecutablePath, StringComparison.Ordinal);
    }
}
