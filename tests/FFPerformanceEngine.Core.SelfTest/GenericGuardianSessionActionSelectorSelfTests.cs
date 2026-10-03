using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;

internal static class GenericGuardianSessionActionSelectorSelfTests
{
    public static void Run()
    {
        ExactActiveEvidenceSelectsOnlyMatchingLiveSafeCandidates();
        PreservesCallerOrderAndCandidateIdentity();
        UnknownAndUnavailableFamiliesFailClosed();
        NonActiveStatesFailClosed();
        NonHighConfidenceFailsClosed();
        NonExactTargetsFailClosed();
        NonLiveSafeActionsAreRejected();
        WorkloadAndFamilyMustMatchExactly();
        EmptyInputNeverSynthesizesAction();
        ResultCollectionIsReadOnlyAndInputIsUnchanged();
        Console.WriteLine("PASS Track 6 generic session action eligibility");
    }

    private static void ExactActiveEvidenceSelectsOnlyMatchingLiveSafeCandidates()
    {
        var eligible = Candidate("fixture:game", GuardianAnomalyKind.CpuContention, ActionSafety.LiveSafe, "cpu.live");
        var wrongFamily = Candidate("fixture:game", GuardianAnomalyKind.GpuSaturation, ActionSafety.LiveSafe, "gpu.live");
        var wrongGame = Candidate("fixture:other", GuardianAnomalyKind.CpuContention, ActionSafety.LiveSafe, "other.live");
        var unsafeCandidate = Candidate("fixture:game", GuardianAnomalyKind.CpuContention, ActionSafety.RestartRequired, "cpu.restart");

        var result = new GenericGuardianSessionActionSelector().SelectEligible(
            ActiveState(),
            GuardianAnomalyKind.CpuContention,
            [wrongFamily, eligible, wrongGame, unsafeCandidate]);

        Require(result.HasEligibleCandidates
                && result.EligibleCandidates.Count == 1
                && ReferenceEquals(result.EligibleCandidates[0], eligible),
            "Only an explicitly supplied matching LiveSafe candidate may enter generic live-session eligibility.");
    }

    private static void PreservesCallerOrderAndCandidateIdentity()
    {
        var first = Candidate("fixture:game", GuardianAnomalyKind.CpuContention, ActionSafety.LiveSafe, "first");
        var second = Candidate("fixture:game", GuardianAnomalyKind.CpuContention, ActionSafety.LiveSafe, "second");
        var third = Candidate("fixture:game", GuardianAnomalyKind.CpuContention, ActionSafety.LiveSafe, "third");

        var result = new GenericGuardianSessionActionSelector().SelectEligible(
            ActiveState(),
            GuardianAnomalyKind.CpuContention,
            [first, second, third]);

        Require(result.EligibleCandidates.Count == 3
                && ReferenceEquals(result.EligibleCandidates[0], first)
                && ReferenceEquals(result.EligibleCandidates[1], second)
                && ReferenceEquals(result.EligibleCandidates[2], third),
            "The first item-3 Slice must preserve caller order/object identity and must not introduce ranking or cloning.");
    }

    private static void UnknownAndUnavailableFamiliesFailClosed()
    {
        var selector = new GenericGuardianSessionActionSelector();
        var unknown = selector.SelectEligible(
            ActiveState(),
            GuardianAnomalyKind.Unknown,
            [Candidate("fixture:game", GuardianAnomalyKind.Unknown, ActionSafety.LiveSafe, "unknown")]);
        Require(!unknown.HasEligibleCandidates && unknown.EligibleCandidates.Count == 0,
            "Guardian Unknown must remain non-actionable fallback.");

        foreach (var family in new[]
                 {
                     GuardianAnomalyKind.BackgroundLoad,
                     GuardianAnomalyKind.RendererEngineStall,
                     GuardianAnomalyKind.SchedulerImbalance,
                     GuardianAnomalyKind.InputFrameLatencySpike
                 })
        {
            var result = selector.SelectEligible(
                ActiveState(),
                family,
                [Candidate("fixture:game", family, ActionSafety.LiveSafe, $"{family}.live")]);
            Require(!result.HasEligibleCandidates && result.EligibleCandidates.Count == 0,
                $"UnavailableEvidence family {family} must not produce a live-session action candidate.");
        }
    }

    private static void NonActiveStatesFailClosed()
    {
        var selector = new GenericGuardianSessionActionSelector();
        var candidate = Candidate("fixture:game", GuardianAnomalyKind.CpuContention, ActionSafety.LiveSafe, "cpu.live");

        foreach (var state in Enum.GetValues<GuardianWorkloadState>().Where(value => value != GuardianWorkloadState.Active))
        {
            var result = selector.SelectEligible(
                State(state, GuardianWorkloadStateConfidence.High, ExactTarget()),
                GuardianAnomalyKind.CpuContention,
                [candidate]);
            Require(!result.HasEligibleCandidates,
                $"Non-Active Guardian workload state {state} must be ineligible for live-session actions.");
        }
    }

    private static void NonHighConfidenceFailsClosed()
    {
        var selector = new GenericGuardianSessionActionSelector();
        var candidate = Candidate("fixture:game", GuardianAnomalyKind.CpuContention, ActionSafety.LiveSafe, "cpu.live");

        foreach (var confidence in new[]
                 {
                     GuardianWorkloadStateConfidence.Unknown,
                     GuardianWorkloadStateConfidence.Low,
                     GuardianWorkloadStateConfidence.Medium
                 })
        {
            var result = selector.SelectEligible(
                State(GuardianWorkloadState.Active, confidence, ExactTarget()),
                GuardianAnomalyKind.CpuContention,
                [candidate]);
            Require(!result.HasEligibleCandidates,
                $"Active state with {confidence} confidence must not authorize generic live-session action eligibility.");
        }
    }

    private static void NonExactTargetsFailClosed()
    {
        var selector = new GenericGuardianSessionActionSelector();
        var candidate = Candidate("fixture:game", GuardianAnomalyKind.CpuContention, ActionSafety.LiveSafe, "cpu.live");
        var targets = new[]
        {
            TelemetryWorkloadTarget.SystemOnly,
            new TelemetryWorkloadTarget { GameId = "fixture:game", BindingQuality = TelemetryWorkloadBindingQuality.UnavailableRunningProcess },
            new TelemetryWorkloadTarget { GameId = "fixture:game", BindingQuality = TelemetryWorkloadBindingQuality.AmbiguousRunningProcess },
            new TelemetryWorkloadTarget { GameId = "fixture:game", BindingQuality = TelemetryWorkloadBindingQuality.UnknownGame },
            new TelemetryWorkloadTarget { GameId = "fixture:game", ProcessId = 7272, BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess },
            new TelemetryWorkloadTarget { GameId = "fixture:game", ExecutablePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DG-Guardian-Actions", "game.exe")), BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess }
        };

        foreach (var target in targets)
        {
            var result = selector.SelectEligible(
                State(GuardianWorkloadState.Active, GuardianWorkloadStateConfidence.High, target),
                GuardianAnomalyKind.CpuContention,
                [candidate]);
            Require(!result.HasEligibleCandidates,
                $"Target quality {target.BindingQuality} with CanCaptureProcess={target.CanCaptureProcess} must fail closed unless exact/capturable.");
        }
    }

    private static void NonLiveSafeActionsAreRejected()
    {
        var selector = new GenericGuardianSessionActionSelector();
        foreach (var safety in Enum.GetValues<ActionSafety>().Where(value => value != ActionSafety.LiveSafe))
        {
            var result = selector.SelectEligible(
                ActiveState(),
                GuardianAnomalyKind.CpuContention,
                [Candidate("fixture:game", GuardianAnomalyKind.CpuContention, safety, safety.ToString())]);
            Require(!result.HasEligibleCandidates,
                $"Action safety {safety} must not enter live-session eligibility in this Slice.");
        }
    }

    private static void WorkloadAndFamilyMustMatchExactly()
    {
        var selector = new GenericGuardianSessionActionSelector();
        var result = selector.SelectEligible(
            ActiveState(),
            GuardianAnomalyKind.CpuContention,
            [
                Candidate(" fixture:GAME ", GuardianAnomalyKind.CpuContention, ActionSafety.LiveSafe, "canonical-match"),
                Candidate("fixture:other", GuardianAnomalyKind.CpuContention, ActionSafety.LiveSafe, "wrong-game"),
                Candidate("fixture:game", GuardianAnomalyKind.GpuSaturation, ActionSafety.LiveSafe, "wrong-family")
            ]);

        Require(result.EligibleCandidates.Count == 1
                && result.EligibleCandidates[0].Action.Id == "canonical-match",
            "Candidate compatibility must use the exact stable GameId after trim/case normalization and exact anomaly family equality.");
    }

    private static void EmptyInputNeverSynthesizesAction()
    {
        var result = new GenericGuardianSessionActionSelector().SelectEligible(
            ActiveState(),
            GuardianAnomalyKind.CpuContention,
            Array.Empty<GenericGuardianSessionActionCandidate>());

        Require(!result.HasEligibleCandidates && result.EligibleCandidates.Count == 0,
            "The selector must never synthesize an action from an anomaly when no candidate was explicitly supplied.");
    }

    private static void ResultCollectionIsReadOnlyAndInputIsUnchanged()
    {
        var first = Candidate("fixture:game", GuardianAnomalyKind.CpuContention, ActionSafety.LiveSafe, "first");
        var second = Candidate("fixture:game", GuardianAnomalyKind.CpuContention, ActionSafety.LiveSafe, "second");
        var source = new List<GenericGuardianSessionActionCandidate> { first, second };
        var sourceSnapshot = source.ToArray();

        var result = new GenericGuardianSessionActionSelector().SelectEligible(
            ActiveState(),
            GuardianAnomalyKind.CpuContention,
            source);

        Require(source.Count == 2
                && ReferenceEquals(source[0], sourceSnapshot[0])
                && ReferenceEquals(source[1], sourceSnapshot[1]),
            "Eligibility selection must not reorder, replace or remove caller candidates.");
        Require(result.EligibleCandidates is IList<GenericGuardianSessionActionCandidate> list && list.IsReadOnly,
            "Eligible candidate output must be genuinely read-only.");

        var mutationBlocked = false;
        try
        {
            ((IList<GenericGuardianSessionActionCandidate>)result.EligibleCandidates).Add(first);
        }
        catch (NotSupportedException)
        {
            mutationBlocked = true;
        }

        Require(mutationBlocked,
            "Ordinary collection mutation must be rejected by the eligibility result.");
    }

    private static GenericGuardianSessionActionCandidate Candidate(
        string gameId,
        GuardianAnomalyKind family,
        ActionSafety safety,
        string actionId)
        => new()
        {
            GameId = gameId,
            Family = family,
            Action = new GuardianAction
            {
                Id = actionId,
                Description = actionId,
                Safety = safety,
                MinimumConfidence = 0.85
            }
        };

    private static GuardianWorkloadStateSnapshot ActiveState()
        => State(GuardianWorkloadState.Active, GuardianWorkloadStateConfidence.High, ExactTarget());

    private static GuardianWorkloadStateSnapshot State(
        GuardianWorkloadState state,
        GuardianWorkloadStateConfidence confidence,
        TelemetryWorkloadTarget target)
        => new()
        {
            State = state,
            Confidence = confidence,
            Target = target
        };

    private static TelemetryWorkloadTarget ExactTarget()
        => new()
        {
            GameId = "fixture:game",
            ProcessId = 7272,
            ExecutablePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DG-Guardian-Actions", "game.exe")),
            BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess
        };

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
