using System.Runtime.ExceptionServices;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.SystemOptimization;
using FFPerformanceEngine.Core.Telemetry;

namespace FFPerformanceEngine.Core.Services;

public enum GenericGuardianSessionCanaryVerdict
{
    Improved,
    Regressive,
    Inconclusive
}

/// <summary>
/// Binds one already-eligible Guardian candidate to exactly one explicit Windows
/// session mutation. The binding grants no authority unless the execution
/// boundary independently re-validates the candidate and workload state.
/// </summary>
public sealed record GenericGuardianWindowsSessionActionBinding
{
    public required GenericGuardianSessionActionCandidate Candidate { get; init; }
    public required WindowsMutationRequest Mutation { get; init; }
}

/// <summary>
/// Family-specific before/after interpretation is deliberately outside the
/// execution orchestrator. Different causal families require different typed
/// metrics, so this seam prevents one invented universal threshold.
/// </summary>
public interface IGenericGuardianSessionCanaryOutcomeEvaluator
{
    GenericGuardianSessionCanaryVerdict Evaluate(
        GenericGuardianSessionActionCandidate candidate,
        TelemetryFrame before,
        TelemetryFrame after);
}

/// <summary>
/// Owns a kept System Optimization session mutation. Disposing or explicitly
/// restoring the lease delegates exact rollback to the proven transaction engine.
/// </summary>
public sealed class GenericGuardianSessionCanaryLease : IAsyncDisposable
{
    private readonly SystemOptimizationSession _session;

    internal GenericGuardianSessionCanaryLease(SystemOptimizationSession session)
        => _session = session ?? throw new ArgumentNullException(nameof(session));

    public Guid TransactionId => _session.TransactionId;
    public Guid RestorePointId => _session.RestorePointId;
    public bool IsActive => !_session.IsRestored;

    public Task RestoreAsync(CancellationToken cancellationToken = default)
        => _session.RestoreAsync(cancellationToken);

    public ValueTask DisposeAsync() => _session.DisposeAsync();
}

public sealed record GenericGuardianSessionCanaryResult
{
    public required GenericGuardianSessionActionCandidate Candidate { get; init; }
    public bool Attempted { get; init; }
    public bool Kept { get; init; }
    public bool RolledBack { get; init; }
    public GenericGuardianSessionCanaryVerdict Verdict { get; init; } = GenericGuardianSessionCanaryVerdict.Inconclusive;
    public TelemetryFrame? Before { get; init; }
    public TelemetryFrame? After { get; init; }
    public GenericGuardianSessionCanaryLease? ActiveLease { get; init; }
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Reversible generic Guardian orchestration for one explicit Windows session
/// mutation. Snapshot/apply/verify/ownership/rollback remain owned by
/// SystemOptimizationTransactionEngine; typed workload evidence remains owned by
/// PerformanceCaptureCoordinator. This class never synthesizes actions, ranks
/// candidates, persists Guardian knowledge, or defines family-specific thresholds.
/// </summary>
public sealed class GenericGuardianWindowsSessionCanaryExecutor
{
    private readonly SystemOptimizationTransactionEngine _transactions;
    private readonly PerformanceCaptureCoordinator _capture;
    private readonly IGenericGuardianSessionCanaryOutcomeEvaluator _evaluator;
    private readonly TimeSpan _sampleDuration;

    public GenericGuardianWindowsSessionCanaryExecutor(
        SystemOptimizationTransactionEngine transactions,
        PerformanceCaptureCoordinator capture,
        IGenericGuardianSessionCanaryOutcomeEvaluator evaluator,
        TimeSpan? sampleDuration = null)
    {
        _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
        _capture = capture ?? throw new ArgumentNullException(nameof(capture));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        _sampleDuration = sampleDuration ?? TimeSpan.FromSeconds(2);
        if (_sampleDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(sampleDuration));
    }

    public async Task<GenericGuardianSessionCanaryResult> ExecuteAsync(
        GenericGuardianSessionActionEligibility eligibility,
        GenericGuardianWindowsSessionActionBinding binding,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eligibility);
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(binding.Candidate);
        ArgumentNullException.ThrowIfNull(binding.Mutation);

        var candidate = binding.Candidate;
        var preflightFailure = PreflightFailure(eligibility, binding);
        if (preflightFailure is not null)
            return NotAttempted(candidate, preflightFailure);

        var target = eligibility.State.Target;
        var beforeCapture = await _capture
            .CaptureWorkloadTypedAsync(target, _sampleDuration, cancellationToken)
            .ConfigureAwait(false);
        var before = AcceptFrame(beforeCapture, target);
        if (before is null)
        {
            return NotAttempted(
                candidate,
                "Typed before evidence is unavailable for the exact workload; Guardian canary will not mutate anything.");
        }

        SystemOptimizationSession? session = null;
        try
        {
            session = await _transactions.BeginSessionAsync(
                "DG Guardian session canary",
                [binding.Mutation],
                cancellationToken).ConfigureAwait(false);

            var afterCapture = await _capture
                .CaptureWorkloadTypedAsync(target, _sampleDuration, cancellationToken)
                .ConfigureAwait(false);
            var after = AcceptFrame(afterCapture, target);
            if (after is null)
            {
                await session.RestoreAsync(CancellationToken.None).ConfigureAwait(false);
                return new GenericGuardianSessionCanaryResult
                {
                    Candidate = candidate,
                    Attempted = true,
                    RolledBack = true,
                    Verdict = GenericGuardianSessionCanaryVerdict.Inconclusive,
                    Before = before,
                    Reason = "Typed after evidence is unavailable; the session mutation was restored because improvement cannot be proven."
                };
            }

            var verdict = NormalizeVerdict(_evaluator.Evaluate(candidate, before, after));
            if (verdict == GenericGuardianSessionCanaryVerdict.Improved)
            {
                var lease = new GenericGuardianSessionCanaryLease(session);
                session = null; // Ownership is intentionally transferred to the kept lease.
                return new GenericGuardianSessionCanaryResult
                {
                    Candidate = candidate,
                    Attempted = true,
                    Kept = true,
                    Verdict = verdict,
                    Before = before,
                    After = after,
                    ActiveLease = lease,
                    Reason = "Typed canary evaluator proved improvement; session mutation remains active under a reversible lease."
                };
            }

            await session.RestoreAsync(CancellationToken.None).ConfigureAwait(false);
            return new GenericGuardianSessionCanaryResult
            {
                Candidate = candidate,
                Attempted = true,
                RolledBack = true,
                Verdict = verdict,
                Before = before,
                After = after,
                Reason = verdict == GenericGuardianSessionCanaryVerdict.Regressive
                    ? "Typed canary evaluator reported regression; original session state was restored."
                    : "Typed canary evidence was inconclusive; original session state was restored."
            };
        }
        catch (Exception primaryFailure)
        {
            if (session is not null && !session.IsRestored)
            {
                try
                {
                    await session.RestoreAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception restoreFailure)
                {
                    throw new AggregateException(
                        "Guardian session canary failed and exact rollback also failed.",
                        primaryFailure,
                        restoreFailure);
                }
            }

            ExceptionDispatchInfo.Capture(primaryFailure).Throw();
            throw;
        }
    }

    private static string? PreflightFailure(
        GenericGuardianSessionActionEligibility eligibility,
        GenericGuardianWindowsSessionActionBinding binding)
    {
        var state = eligibility.State;
        if (state is null)
            return "Guardian eligibility has no workload state snapshot.";
        if (state.State != GuardianWorkloadState.Active)
            return $"Guardian workload state {state.State} is not Active.";
        if (state.Confidence != GuardianWorkloadStateConfidence.High)
            return $"Guardian workload confidence {state.Confidence} is not High.";

        var target = state.Target;
        if (target is null
            || target.BindingQuality != TelemetryWorkloadBindingQuality.ExactRunningProcess
            || !target.CanCaptureProcess
            || string.IsNullOrWhiteSpace(target.GameId))
            return "Guardian workload target is not one exact capturable stable workload.";

        var candidate = binding.Candidate;
        var eligibleCandidates = eligibility.EligibleCandidates;
        if (eligibleCandidates is null
            || !eligibleCandidates.Any(item => ReferenceEquals(item, candidate)))
            return "The supplied candidate is not the exact candidate object authorized by the eligibility result.";

        if (candidate.Action is null || candidate.Action.Safety != ActionSafety.LiveSafe)
            return "Guardian session canary executes only explicitly eligible LiveSafe actions.";
        if (candidate.Family != eligibility.Family)
            return "Candidate anomaly family no longer matches the eligibility result.";
        if (!GenericGuardianClassifierSupportCatalog.For(eligibility.Family).CanClassify)
            return "Guardian anomaly family is not evidence-backed and cannot enter session canary execution.";
        if (string.IsNullOrWhiteSpace(candidate.GameId)
            || !string.Equals(
                candidate.GameId.Trim(),
                target.GameId.Trim(),
                StringComparison.OrdinalIgnoreCase))
            return "Candidate stable GameId no longer matches the exact workload target.";
        if (string.IsNullOrWhiteSpace(binding.Mutation.CapabilityId))
            return "Guardian Windows session binding requires one explicit capability id.";

        return null;
    }

    private static TelemetryFrame? AcceptFrame(
        PerformanceWorkloadTypedCaptureResult capture,
        TelemetryWorkloadTarget expectedTarget)
    {
        if (capture.Frame is not TelemetryFrame frame
            || frame.FrameQuality == TelemetryMetricQuality.Unavailable)
            return null;

        return TargetsMatch(capture.Target, expectedTarget) ? frame : null;
    }

    private static bool TargetsMatch(
        TelemetryWorkloadTarget left,
        TelemetryWorkloadTarget right)
        => left.BindingQuality == TelemetryWorkloadBindingQuality.ExactRunningProcess
           && right.BindingQuality == TelemetryWorkloadBindingQuality.ExactRunningProcess
           && left.CanCaptureProcess
           && right.CanCaptureProcess
           && left.ProcessId == right.ProcessId
           && string.Equals(left.GameId?.Trim(), right.GameId?.Trim(), StringComparison.OrdinalIgnoreCase)
           && string.Equals(left.ExecutablePath?.Trim(), right.ExecutablePath?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static GenericGuardianSessionCanaryVerdict NormalizeVerdict(
        GenericGuardianSessionCanaryVerdict verdict)
        => Enum.IsDefined(verdict)
            ? verdict
            : GenericGuardianSessionCanaryVerdict.Inconclusive;

    private static GenericGuardianSessionCanaryResult NotAttempted(
        GenericGuardianSessionActionCandidate candidate,
        string reason)
        => new()
        {
            Candidate = candidate,
            Verdict = GenericGuardianSessionCanaryVerdict.Inconclusive,
            Reason = reason
        };
}
