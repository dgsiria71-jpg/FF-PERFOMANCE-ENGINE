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
/// Every physical capture and the intervening mutation are now checked against
/// the REAL process-wide Track 0 benchmark generation. This is observation,
/// not global exclusion or scene/load proof. No production scene source is
/// registered and no automatic generic host activation exists.
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
    private readonly ControlledBenchmarkLeaseManager _benchmarkAuthority;
    private readonly GenericGuardianControlledBenchmarkIntervalCapture _benchmarkCapture;

    public GenericGuardianWindowsSessionCanaryExecutor(
        SystemOptimizationTransactionEngine transactions,
        PerformanceCaptureCoordinator capture,
        IGenericGuardianSessionCanaryOutcomeEvaluator evaluator,
        GenericGuardianSessionMutationCatalog catalog,
        TimeSpan? sampleDuration = null,
        IGenericGuardianCanaryEvidenceSource? evidenceSource = null,
        GenericGuardianCanarySessionKey? sessionKey = null,
        GenericGuardianCanaryComparabilityPolicy? comparability = null,
        Func<DateTimeOffset>? clock = null,
        ControlledBenchmarkLeaseManager? benchmarkAuthority = null)
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
        // Concrete sealed manager; all instances share one static global gate/generation.
        // A caller cannot substitute a forged IControlledBenchmarkActivityProbe.
        _benchmarkAuthority = benchmarkAuthority ?? new ControlledBenchmarkLeaseManager();
        _benchmarkCapture = new GenericGuardianControlledBenchmarkIntervalCapture(_benchmarkAuthority);
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

        // The monitor reads the real authority IMMEDIATELY around the capture delegate.
        // A generation change includes a benchmark that acquired and released in between.
        var target = eligibility.State.Target;
        var beforeStartedAt = _clock();
        var beforeObservation = await _benchmarkCapture.CaptureAsync(
            token => _capture.CaptureWorkloadTypedAsync(target, _sampleDuration, token),
            cancellationToken).ConfigureAwait(false);
        var beforeCompletedAt = _clock();
        if (!beforeObservation.Attempted || !beforeObservation.UninterruptedIdle)
            return NotAttempted(candidate,
                "Track 0 controlled benchmark was active or changed during the before capture; no Windows mutation was attempted.");

        var baseline = beforeObservation.Before;
        var before = beforeObservation.Value is null
            ? null : AcceptFrame(beforeObservation.Value, target);
        if (before is null)
            return NotAttempted(candidate,
                "Typed before evidence is unavailable for the exact workload; Guardian canary will not mutate anything.");

        var beforeWindow = await _evidenceSource!.CaptureWindowAsync(
            target, before, beforeStartedAt, beforeCompletedAt, cancellationToken).ConfigureAwait(false);
        if (!AcceptWindow(beforeWindow, before, target, beforeStartedAt, beforeCompletedAt))
            return NotAttempted(candidate,
                "Comparable before context is absent, unknown, contaminated or not bound to the actual capture; no mutation was attempted.");

        if (!BenchmarkUninterruptedSince(baseline))
            return NotAttempted(candidate,
                "Track 0 controlled benchmark changed after before capture and before mutation; no mutation was attempted.");

        SystemOptimizationSession? session = null;
        try
        {
            var mutationStartedAt = _clock();
            session = await _transactions.BeginSessionAsync(
                "DG Guardian session canary",
                [binding.Mutation],
                cancellationToken).ConfigureAwait(false);
            var mutationCompletedAt = _clock();

            if (!BenchmarkUninterruptedSince(baseline))
                return await RestoreContaminatedAsync(session, candidate, before, null,
                    "Track 0 benchmark changed during the mutation; original Windows state was restored.")
                    .ConfigureAwait(false);

            var afterStartedAt = _clock();
            var afterObservation = await _benchmarkCapture.CaptureAsync(
                token => _capture.CaptureWorkloadTypedAsync(target, _sampleDuration, token),
                cancellationToken).ConfigureAwait(false);
            var afterCompletedAt = _clock();
            if (!afterObservation.Attempted || !afterObservation.UninterruptedIdle
                || !BenchmarkUninterruptedSince(baseline))
                return await RestoreContaminatedAsync(session, candidate, before, null,
                    "Track 0 benchmark was active or changed during/between canary captures; original state was restored.")
                    .ConfigureAwait(false);

            var after = afterObservation.Value is null
                ? null : AcceptFrame(afterObservation.Value, target);
            if (after is null)
                return await RestoreContaminatedAsync(session, candidate, before, null,
                    "Typed after evidence is unavailable; the session mutation was restored because improvement cannot be proven.")
                    .ConfigureAwait(false);

            var afterWindow = await _evidenceSource.CaptureWindowAsync(
                target, after, afterStartedAt, afterCompletedAt, cancellationToken).ConfigureAwait(false);
            if (!AcceptWindow(afterWindow, after, target, afterStartedAt, afterCompletedAt)
                || _comparability.Evaluate(beforeWindow, afterWindow, mutationStartedAt, mutationCompletedAt)
                   != GenericGuardianCanaryComparability.InScopeOnSuppliedEvidence)
                return await RestoreContaminatedAsync(session, candidate, before, after,
                    "Before/after context is missing, changed, contaminated or temporally invalid; original state was restored.")
                    .ConfigureAwait(false);

            // Source-supplied false flags cannot override the real global lease authority.
            if (!BenchmarkUninterruptedSince(baseline))
                return await RestoreContaminatedAsync(session, candidate, before, after,
                    "Track 0 benchmark changed after comparison evidence; original state was restored.")
                    .ConfigureAwait(false);

            var verdict = NormalizeVerdict(_evaluator.Evaluate(candidate, before, after));
            // The evaluator itself could be interrupted. Recheck immediately before
            // transferring ownership, though only the future host can EXCLUDE a race.
            if (!BenchmarkUninterruptedSince(baseline))
                return await RestoreContaminatedAsync(session, candidate, before, after,
                    "Track 0 benchmark changed during outcome evaluation; original state was restored.")
                    .ConfigureAwait(false);

            if (verdict == GenericGuardianSessionCanaryVerdict.Improved)
            {
                var lease = new GenericGuardianSessionCanaryLease(session);
                session = null;
                return new GenericGuardianSessionCanaryResult
                {
                    Candidate = candidate,
                    Attempted = true,
                    Kept = true,
                    Verdict = verdict,
                    Before = before,
                    After = after,
                    ActiveLease = lease,
                    Reason = "Supplied scene comparison and real Track 0 idle generation passed; improvement remains reversible, but external interference/causality is not proven."
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

    private bool BenchmarkUninterruptedSince(ControlledBenchmarkActivitySnapshot baseline)
        => ControlledBenchmarkActivitySnapshot.ProvesUninterruptedIdle(
            baseline, _benchmarkAuthority.SnapshotActivity());

    private static async Task<GenericGuardianSessionCanaryResult> RestoreContaminatedAsync(
        SystemOptimizationSession session,
        GenericGuardianSessionActionCandidate candidate,
        TelemetryFrame before,
        TelemetryFrame? after,
        string reason)
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
            Reason = reason
        };
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
