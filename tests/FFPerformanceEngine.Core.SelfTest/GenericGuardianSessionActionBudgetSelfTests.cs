using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;

internal static class GenericGuardianSessionActionBudgetSelfTests
{
    internal static void Run()
    {
        AdmissionCooldownAndGlobalCeiling();
        DistinctSessionEpochAndExactReset();
        InvalidIdentityEligibilityAndActionFailClosed();
        ConcurrentAdmissionCannotOverrunBudget();
        Console.WriteLine("PASS Track 6 generic Guardian canary cooldown, exact session epoch and atomic Action Budget");
    }

    private static void AdmissionCooldownAndGlobalCeiling()
    {
        var now = new DateTimeOffset(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);
        var budget = new GenericGuardianSessionActionBudget(TimeSpan.FromSeconds(30), 2, () => now);
        var session = Key();
        var first = Candidate("cpu.first");
        var second = Candidate("cpu.second");
        var eligibility = Eligible(first, second);

        var admitted = budget.TryAdmit(session, eligibility, first);
        Require(admitted.Status == GenericGuardianCanaryAdmissionStatus.Allowed
                && admitted.RemainingAttempts == 1
                && admitted.CooldownUntil == now.AddSeconds(30),
            "First eligible canary consumes exactly one slot and opens explicit cooldown.");

        var cooling = budget.TryAdmit(session, eligibility, first);
        Require(cooling.Status == GenericGuardianCanaryAdmissionStatus.InCooldown
                && cooling.RemainingAttempts == 1
                && cooling.CooldownUntil == admitted.CooldownUntil,
            "Repeated same family/action during cooldown must never consume another global slot.");

        var different = budget.TryAdmit(session, eligibility, second);
        Require(different.Status == GenericGuardianCanaryAdmissionStatus.Allowed
                && different.RemainingAttempts == 0,
            "A distinct action may consume the next slot, but shares the same session-wide canary ceiling.");

        now = now.AddSeconds(31);
        var exhausted = budget.TryAdmit(session, eligibility, first);
        Require(exhausted.Status == GenericGuardianCanaryAdmissionStatus.BudgetExhausted
                && exhausted.RemainingAttempts == 0,
            "Exhaustion must remain authoritative after cooldown expires.");

        var repeatExhausted = budget.TryAdmit(session, eligibility, second);
        Require(repeatExhausted.Status == GenericGuardianCanaryAdmissionStatus.BudgetExhausted,
            "Exhausted budget must apply to every family/action, not only the first action.");

        var otherFamily = Candidate("gpu.third", GuardianAnomalyKind.GpuSaturation);
        var gpuEligibility = EligibleFor(GuardianAnomalyKind.GpuSaturation, session, otherFamily);
        Require(budget.TryAdmit(session, gpuEligibility, otherFamily).Status == GenericGuardianCanaryAdmissionStatus.BudgetExhausted,
            "Different anomaly families must also share the global per-session canary ceiling.");
    }

    private static void DistinctSessionEpochAndExactReset()
    {
        var now = new DateTimeOffset(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);
        var budget = new GenericGuardianSessionActionBudget(TimeSpan.FromSeconds(30), 1, () => now);
        var original = Key();
        var candidate = Candidate("cpu.first");
        var eligibility = Eligible(candidate);
        Require(budget.TryAdmit(original, eligibility, candidate).Status == GenericGuardianCanaryAdmissionStatus.Allowed,
            "Original session must admit one action.");

        var sameEpochWrongPid = original with { ProcessId = original.ProcessId + 1 };
        var mismatchedEligibility = EligibleFor(GuardianAnomalyKind.CpuContention, sameEpochWrongPid, candidate);
        Require(budget.TryAdmit(sameEpochWrongPid, mismatchedEligibility, candidate).Status == GenericGuardianCanaryAdmissionStatus.Ineligible,
            "The same session epoch must never be rebound to another PID or target.");
        Require(!budget.ResetSession(sameEpochWrongPid),
            "A mismatched target must not clear another session's quota.");
        Require(budget.TryAdmit(original, eligibility, candidate).Status == GenericGuardianCanaryAdmissionStatus.BudgetExhausted,
            "Rejected reassignment and reset must leave original quota unchanged.");

        var reusedPidNewEpoch = original with { SessionEpoch = Guid.NewGuid() };
        Require(budget.TryAdmit(reusedPidNewEpoch, eligibility, candidate).Status == GenericGuardianCanaryAdmissionStatus.Allowed,
            "A new explicitly owned lifecycle epoch may start a new session even when Windows reuses the PID.");
        var otherPid = Key(processId: original.ProcessId + 50);
        var otherEligibility = EligibleFor(GuardianAnomalyKind.CpuContention, otherPid, candidate);
        Require(budget.TryAdmit(otherPid, otherEligibility, candidate).Status == GenericGuardianCanaryAdmissionStatus.Allowed,
            "Distinct positive PID and epoch must have independent canary capacity.");

        Require(budget.ResetSession(original), "Exact session owner must be able to release its in-memory budget.");
        Require(budget.TryAdmit(reusedPidNewEpoch, eligibility, candidate).Status == GenericGuardianCanaryAdmissionStatus.BudgetExhausted,
            "Resetting one epoch must not reset another epoch sharing its GameId and PID.");
        Require(budget.TryAdmit(original, eligibility, candidate).Status == GenericGuardianCanaryAdmissionStatus.Allowed,
            "Reopened exact epoch after explicit reset has fresh capacity; caller must reset only at lifecycle end.");
    }

    private static void InvalidIdentityEligibilityAndActionFailClosed()
    {
        var now = new DateTimeOffset(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);
        var budget = new GenericGuardianSessionActionBudget(TimeSpan.FromSeconds(30), 1, () => now);
        var session = Key();
        var candidate = Candidate("cpu.first");
        var eligibility = Eligible(candidate);
        var invalid = new[]
        {
            session with { SessionEpoch = Guid.Empty },
            session with { GameId = "  " },
            session with { ProcessId = 0 },
            session with { ExecutablePath = " " },
            session with { GameId = "another.game" },
            session with { ProcessId = session.ProcessId + 1 },
            session with { ExecutablePath = @"C:\Games\other.exe" }
        };
        foreach (var bad in invalid)
            Require(budget.TryAdmit(bad, eligibility, candidate).Status == GenericGuardianCanaryAdmissionStatus.Ineligible,
                "Malformed or mismatched session target must never gain admission.");
        Require(!budget.ResetSession(session), "Rejected invalid sessions must not allocate quota state.");

        var badState = eligibility with { State = eligibility.State with { State = GuardianWorkloadState.Ready } };
        var badConfidence = eligibility with { State = eligibility.State with { Confidence = GuardianWorkloadStateConfidence.Low } };
        var badTarget = eligibility with { State = eligibility.State with { Target = TelemetryWorkloadTarget.SystemOnly } };
        var detachedCandidate = candidate with { };
        var unsafeCandidate = candidate with { Action = candidate.Action with { Safety = ActionSafety.RestartRequired } };
        var blankAction = candidate with { Action = candidate.Action with { Id = " " } };
        var wrongFamily = candidate with { Family = GuardianAnomalyKind.GpuSaturation };
        var unknownFamily = Candidate("unknown", GuardianAnomalyKind.Unknown);
        var unknownEligibility = new GenericGuardianSessionActionEligibility
        {
            State = eligibility.State,
            Family = GuardianAnomalyKind.Unknown,
            EligibleCandidates = Array.AsReadOnly(new[] { unknownFamily })
        };

        Require(budget.TryAdmit(session, badState, candidate).Status == GenericGuardianCanaryAdmissionStatus.Ineligible,
            "Non-active workload must fail closed.");
        Require(budget.TryAdmit(session, badConfidence, candidate).Status == GenericGuardianCanaryAdmissionStatus.Ineligible,
            "Non-high workload confidence must fail closed.");
        Require(budget.TryAdmit(session, badTarget, candidate).Status == GenericGuardianCanaryAdmissionStatus.Ineligible,
            "Nonexact capture target must fail closed.");
        Require(budget.TryAdmit(session, eligibility, detachedCandidate).Status == GenericGuardianCanaryAdmissionStatus.Ineligible,
            "Equivalent but uncontained candidate reference cannot spend budget.");
        Require(budget.TryAdmit(session, eligibility, unsafeCandidate).Status == GenericGuardianCanaryAdmissionStatus.Ineligible,
            "Unsafe candidate may not spend budget.");
        Require(budget.TryAdmit(session, eligibility with { EligibleCandidates = Array.AsReadOnly(new[] { blankAction }) }, blankAction).Status == GenericGuardianCanaryAdmissionStatus.Ineligible,
            "Blank action identity must not create a cooldown key.");
        Require(budget.TryAdmit(session, eligibility with { EligibleCandidates = Array.AsReadOnly(new[] { wrongFamily }) }, wrongFamily).Status == GenericGuardianCanaryAdmissionStatus.Ineligible,
            "Candidate family mismatch must fail closed.");
        Require(budget.TryAdmit(session, unknownEligibility, unknownFamily).Status == GenericGuardianCanaryAdmissionStatus.Ineligible,
            "Unknown/fallback anomaly cannot admit a canary.");

        Require(budget.TryAdmit(session, eligibility, candidate).Status == GenericGuardianCanaryAdmissionStatus.Allowed,
            "Invalid inputs must not consume the first valid session slot.");
        Require(budget.TryAdmit(session, eligibility, candidate).Status == GenericGuardianCanaryAdmissionStatus.BudgetExhausted,
            "The single valid admission consumes exactly one slot.");

        var overflow = new GenericGuardianSessionActionBudget(TimeSpan.FromDays(1), 1, () => DateTimeOffset.MaxValue);
        Require(overflow.TryAdmit(Key(), eligibility, candidate).Status == GenericGuardianCanaryAdmissionStatus.Ineligible,
            "An overflowing cooldown timestamp must fail closed rather than mutate session quota.");
        RequireThrows<ArgumentOutOfRangeException>(() => new GenericGuardianSessionActionBudget(TimeSpan.Zero, 1));
        RequireThrows<ArgumentOutOfRangeException>(() => new GenericGuardianSessionActionBudget(TimeSpan.FromSeconds(1), 0));
    }

    private static void ConcurrentAdmissionCannotOverrunBudget()
    {
        var now = new DateTimeOffset(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);
        var budget = new GenericGuardianSessionActionBudget(TimeSpan.FromMinutes(1), 3, () => now);
        var session = Key();
        var candidates = Enumerable.Range(0, 32).Select(i => Candidate($"cpu.concurrent-{i}")).ToArray();
        var eligibility = Eligible(candidates);
        var decisions = Task.WhenAll(candidates.Select(candidate => Task.Run(() => budget.TryAdmit(session, eligibility, candidate))))
            .GetAwaiter().GetResult();
        Require(decisions.Count(decision => decision.Status == GenericGuardianCanaryAdmissionStatus.Allowed) == 3,
            "Parallel unique actions must admit at most the three explicitly configured canaries.");
        Require(decisions.All(decision => decision.RemainingAttempts is >= 0 and <= 2)
                && decisions.Count(decision => decision.Status == GenericGuardianCanaryAdmissionStatus.BudgetExhausted) == 29,
            "Atomic global budget must never go negative or admit a fourth parallel action.");
    }

    private static GenericGuardianCanarySessionKey Key(int processId = 4242) => new(
        Guid.NewGuid(),
        "fixture:game",
        processId,
        Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DG-Guardian-Budget", "game.exe")));

    private static GenericGuardianSessionActionCandidate Candidate(
        string id,
        GuardianAnomalyKind family = GuardianAnomalyKind.CpuContention)
        => new()
        {
            GameId = "fixture:game",
            Family = family,
            Action = new GuardianAction { Id = id, Description = id, Safety = ActionSafety.LiveSafe }
        };

    private static GenericGuardianSessionActionEligibility Eligible(params GenericGuardianSessionActionCandidate[] candidates)
        => EligibleFor(GuardianAnomalyKind.CpuContention, KeyForTarget(), candidates);

    private static GenericGuardianCanarySessionKey KeyForTarget()
        => new(Guid.Empty, "fixture:game", 4242,
            Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DG-Guardian-Budget", "game.exe")));

    private static GenericGuardianSessionActionEligibility EligibleFor(
        GuardianAnomalyKind family,
        GenericGuardianCanarySessionKey session,
        params GenericGuardianSessionActionCandidate[] candidates)
        => new()
        {
            State = new GuardianWorkloadStateSnapshot
            {
                State = GuardianWorkloadState.Active,
                Confidence = GuardianWorkloadStateConfidence.High,
                Target = new TelemetryWorkloadTarget
                {
                    GameId = session.GameId,
                    ProcessId = session.ProcessId,
                    ExecutablePath = session.ExecutablePath,
                    BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess
                }
            },
            Family = family,
            EligibleCandidates = Array.AsReadOnly(candidates)
        };

    private static void RequireThrows<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException($"Expected exception {typeof(T).Name}.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
