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
/// Reversible generic Guardian orchestration for one explicitly registered
/// action-to-Windows-mutation binding. The comparison source must be vetted by
/// the future owning host; merely implementing its interface is NOT provenance.
/// No production scene source is registered, so without explicit proof this
/// executor fails closed before mutation. No automatic host activation exists.
/// </summary>
public sealed class GenericGuardianWindowsSessionCanaryExecutor
{
    private readonly SystemOptimizationTransactionEngine _transactions;
    private readonly PerformanceCaptureCoordinator _capture;
    private readonly IGenericGuardianSessionCanaryOutcomeEvaluator _evaluator;
    private readonly GenericGuardianSessionMutationCatalog _catalog;
    private readonly TimeSpan _sampleDuration;
    private readonly IGenericGuardianCanaryEvidenceSource? _evidenceSource;
    private readonly GenericGuardianCanarySessionKey? _sessionKey;
    private readonly GenericGuardianCanaryComparabilityPolicy _comparability;
    private readonly Func<DateTimeOffset> _clock;

    public GenericGuardianWindowsSessionCanaryExecutor(
        SystemOptimizationTransactionEngine transactions,
        PerformanceCaptureCoordinator capture,
        IGenericGuardianSessionCanaryOutcomeEvaluator evaluator,
        GenericGuardianSessionMutationCatalog catalog,
        TimeSpan? sampleDuration = null,
        IGenericGuardianCanaryEvidenceSource? evidenceSource = null,
        GenericGuardianCanarySessionKey? sessionKey = null,
        GenericGuardianCanaryComparabilityPolicy? comparability = null,
        Func<DateTimeOffset>? clock = null)
    {
        _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
        _capture = capture ?? throw new ArgumentNullException(nameof(capture));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _sampleDuration = sampleDuration ?? TimeSpan.FromSeconds(2);
        if (_sampleDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(sampleDuration));
        _evidenceSource = evidenceSource;
        _sessionKey = sessionKey;
        _comparability = comparability ?? new GenericGuardianCanaryComparabilityPolicy();
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
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

        // The exact workload and real lifecycle are already checked in preflight.
        // The executor, not the evidence provider, times the actual capture calls.
        var target = eligibility.State.Target;
        var beforeStartedAt = _clock();
        var beforeCapture = await _capture
            .CaptureWorkloadTypedAsync(target, _sampleDuration, cancellationToken)
            .ConfigureAwait(false);
        var beforeCompletedAt = _clock();
        var before = AcceptFrame(beforeCapture, target);
        if (before is null)
        {
            return NotAttempted(candidate,
                "Typed before evidence is unavailable for the exact workload; Guardian canary will not mutate anything.");
        }

        var beforeWindow = await _evidenceSource!.CaptureWindowAsync(
            target, before, beforeStartedAt, beforeCompletedAt, cancellationToken).ConfigureAwait(false);
        if (!AcceptWindow(beforeWindow, before, target, beforeStartedAt, beforeCompletedAt))
        {
            return NotAttempted(candidate,
                "Comparable before context is absent, unknown, contaminated or not bound to the actual capture; no mutation was attempted.");
        }

        SystemOptimizationSession? session = null;
        try
        {
            var mutationStartedAt = _clock();
            session = await _transactions.BeginSessionAsync(
                "DG Guardian session canary",
                [binding.Mutation],
                cancellationToken).ConfigureAwait(false);
            var mutationCompletedAt = _clock();

            var afterStartedAt = _clock();
            var afterCapture = await _capture
                .CaptureWorkloadTypedAsync(target, _sampleDuration, cancellationToken)
                .ConfigureAwait(false);
            var afterCompletedAt = _clock();
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

            var afterWindow = await _evidenceSource.CaptureWindowAsync(
                target, after, afterStartedAt, afterCompletedAt, cancellationToken).ConfigureAwait(false);
            if (!AcceptWindow(afterWindow, after, target, afterStartedAt, afterCompletedAt)
                || _comparability.Evaluate(beforeWindow, afterWindow, mutationStartedAt, mutationCompletedAt)
                   != GenericGuardianCanaryComparability.InScopeOnSuppliedEvidence)
            {
                await session.RestoreAsync(CancellationToken.None).ConfigureAwait(false);
                return new GenericGuardianSessionCanaryResult
                {
                    Candidate = candidate,
                    Attempted = true,
                    RolledBack = true,
                    Verdict = GenericGuardianSessionCanaryVerdict.Inconclusive,
                    Before = before,
                    After = after,
                    Reason = "Before/after comparison evidence is missing, changed, contaminated or temporally invalid; original session state was restored."
                };
            }

            // The policy establishes only structural consistency of context
            // supplied by an independently vetted source, never full causality.
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
                    Reason = "Supplied comparison passed and typed evaluator reported improvement; mutation remains reversible under an active lease."
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

    private bool AcceptWindow(
        GenericGuardianCanaryComparisonWindow? window,
        TelemetryFrame actualFrame,
        TelemetryWorkloadTarget expectedTarget,
        DateTimeOffset capturedFrom,
        DateTimeOffset capturedThrough)
        => window is not null
           && _sessionKey is not null
           && window.SessionEpoch == _sessionKey.SessionEpoch
           && ReferenceEquals(window.Frame, actualFrame)
           && window.StartedAt == capturedFrom
           && window.EndedAt == capturedThrough
           && TargetsMatch(window.Target, expectedTarget)
           && GenericGuardianCanaryComparabilityPolicy.IsValidWindow(window);

    private string? PreflightFailure(
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
            || !string.Equals(candidate.GameId.Trim(), target.GameId.Trim(), StringComparison.OrdinalIgnoreCase))
            return "Candidate stable GameId no longer matches the exact workload target.";
        if (!_catalog.IsAuthorized(candidate, binding.Mutation))
            return "Guardian action is not registered to this exact Windows capability, target value and expected-state precondition.";
        if (!_transactions.IsLiveSafeSessionCapability(binding.Mutation.CapabilityId))
            return "Guardian Windows session binding requires a currently Available, session-applicable LiveSafe capability; action metadata alone is insufficient.";

        // Capability and candidate validity cannot replace independent scene
        // evidence. A default/empty provider must never unlock mutation.
        if (_evidenceSource is null || _sessionKey is null
            || _sessionKey.SessionEpoch == Guid.Empty
            || _sessionKey.ProcessId != target.ProcessId
            || !SameIdentity(_sessionKey.GameId, target.GameId)
            || !SameIdentity(_sessionKey.ExecutablePath, target.ExecutablePath))
            return "Guardian comparison source or exact real session epoch is absent or mismatched; mutation denied.";

        return null;
    }

    private static bool SameIdentity(string? left, string? right)
        => !string.IsNullOrWhiteSpace(left)
           && !string.IsNullOrWhiteSpace(right)
           && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);

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
           && SameIdentity(left.GameId, right.GameId)
           && SameIdentity(left.ExecutablePath, right.ExecutablePath);

    private static GenericGuardianSessionCanaryVerdict NormalizeVerdict(
        GenericGuardianSessionCanaryVerdict verdict)
        => Enum.IsDefined(verdict) ? verdict : GenericGuardianSessionCanaryVerdict.Inconclusive;

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
