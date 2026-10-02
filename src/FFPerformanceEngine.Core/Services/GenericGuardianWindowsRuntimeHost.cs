using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.SystemOptimization;
using FFPerformanceEngine.Core.Workloads;

namespace FFPerformanceEngine.Core.Services;

public sealed record GenericGuardianWindowsRuntimeCycleResult
{
    public required GenericGuardianWorkloadObservation Observation { get; init; }
    public required GenericGuardianBottleneckClassification Classification { get; init; }
    public required GenericGuardianSessionActionEligibility Eligibility { get; init; }
    public GenericGuardianCanarySessionKey? Session { get; init; }
    public GenericGuardianCanaryAdmission? Admission { get; init; }
    public GenericGuardianSessionCanaryResult? Canary { get; init; }
    public bool Retained { get; init; }
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Concrete on-demand owner for one generic Guardian Windows runtime.
///
/// This composes the already-proven read-only workload observation, classifier,
/// eligibility, caller-configured action budget/catalog, admitted reversible
/// canary executor and OS-backed session lifecycle. It owns no discovery and
/// invents no candidate, mutation, budget limit or scene evidence.
///
/// The supplied ControlledBenchmarkLeaseManager is intentionally reused by the
/// executor. Production composition must therefore pass the same application
/// authority that is connected to the specialized GuardianSessionHost.
/// </summary>
public sealed class GenericGuardianWindowsRuntimeHost : IAsyncDisposable
{
    private readonly GenericGuardianWorkloadObservationService _observation;
    private readonly GenericGuardianBottleneckClassifier _classifier;
    private readonly GenericGuardianSessionActionSelector _selector;
    private readonly GenericGuardianSessionActionBudget _budget;
    private readonly GenericGuardianSessionMutationCatalog _catalog;
    private readonly IReadOnlyList<GenericGuardianSessionActionCandidate> _candidates;
    private readonly SystemOptimizationTransactionEngine _transactions;
    private readonly PerformanceCaptureCoordinator _capture;
    private readonly IGenericGuardianSessionCanaryOutcomeEvaluator _evaluator;
    private readonly ControlledBenchmarkLeaseManager _benchmarkAuthority;
    private readonly GenericGuardianSessionExperimentAdmissionManager _teardownAdmission;
    private readonly Func<GenericGuardianCanarySessionKey, IGenericGuardianCanaryEvidenceSource?> _evidenceSourceFactory;
    private readonly TimeSpan _canarySampleDuration;
    private readonly GenericGuardianWindowsSessionLifecycleCoordinator _sessionOwner;
    private readonly GenericGuardianWindowsSessionHostLifecycle _lifecycle;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    public GenericGuardianWindowsRuntimeHost(
        GenericGuardianWorkloadObservationService observation,
        GenericGuardianBottleneckClassifier classifier,
        GenericGuardianSessionActionSelector selector,
        GenericGuardianSessionActionBudget budget,
        GenericGuardianSessionMutationCatalog catalog,
        IEnumerable<GenericGuardianSessionActionCandidate> candidates,
        SystemOptimizationTransactionEngine transactions,
        PerformanceCaptureCoordinator capture,
        IGenericGuardianSessionCanaryOutcomeEvaluator evaluator,
        ControlledBenchmarkLeaseManager benchmarkAuthority,
        Func<GenericGuardianCanarySessionKey, IGenericGuardianCanaryEvidenceSource?> evidenceSourceFactory,
        TimeSpan canarySampleDuration)
    {
        _observation = observation ?? throw new ArgumentNullException(nameof(observation));
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        _budget = budget ?? throw new ArgumentNullException(nameof(budget));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        ArgumentNullException.ThrowIfNull(candidates);
        _candidates = Array.AsReadOnly(candidates.ToArray());
        _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
        _capture = capture ?? throw new ArgumentNullException(nameof(capture));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        _benchmarkAuthority = benchmarkAuthority ?? throw new ArgumentNullException(nameof(benchmarkAuthority));
        _teardownAdmission = new GenericGuardianSessionExperimentAdmissionManager(
            _benchmarkAuthority,
            _transactions);
        _evidenceSourceFactory = evidenceSourceFactory ?? throw new ArgumentNullException(nameof(evidenceSourceFactory));
        if (canarySampleDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(canarySampleDuration));
        _canarySampleDuration = canarySampleDuration;

        _sessionOwner = new GenericGuardianWindowsSessionLifecycleCoordinator();
        _lifecycle = new GenericGuardianWindowsSessionHostLifecycle(_sessionOwner);
    }

    public GenericGuardianCanarySessionKey? CurrentSession => _lifecycle.CurrentSession;
    public int RetainedLeaseCount => _lifecycle.RetainedLeaseCount;

    public async Task<GenericGuardianWindowsRuntimeCycleResult> RunCycleAsync(
        ResolvedGameCatalogResult catalog,
        string requestedGameId,
        bool systemOnline,
        TimeSpan observationCaptureDuration,
        BottleneckAnalysisContext analysisContext,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(catalog);
        if (string.IsNullOrWhiteSpace(requestedGameId))
            throw new ArgumentException("A stable requested GameId is required.", nameof(requestedGameId));
        if (observationCaptureDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(observationCaptureDuration));
        ArgumentNullException.ThrowIfNull(analysisContext);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var observation = await _observation.ObserveAsync(
                catalog,
                requestedGameId,
                systemOnline,
                observationCaptureDuration,
                cancellationToken).ConfigureAwait(false);

            var previousSession = _lifecycle.CurrentSession;
            GenericGuardianCanarySessionKey? session;
            if (_lifecycle.RequiresTeardown(observation.State))
            {
                await using var teardownAdmission = await _teardownAdmission
                    .AcquireAsync("DG Guardian runtime rebind teardown", cancellationToken)
                    .ConfigureAwait(false);
                session = await _lifecycle
                    .ObserveUnderExperimentAsync(
                        observation.State,
                        teardownAdmission,
                        CancellationToken.None)
                    .ConfigureAwait(false);
            }
            else
            {
                session = await _lifecycle
                    .ObserveAsync(observation.State, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (previousSession is not null && !ReferenceEquals(previousSession, session))
                _budget.ResetSession(previousSession);

            var classification = _classifier.Classify(observation, analysisContext);
            var eligibility = _selector.SelectEligible(
                observation.State,
                classification.Family,
                _candidates);

            if (session is null)
                return Result(
                    observation,
                    classification,
                    eligibility,
                    null,
                    null,
                    null,
                    false,
                    "No current owner-issued Active/High exact Windows session exists; no live canary is allowed.");

            if (eligibility.EligibleCandidates.Count != 1)
                return Result(
                    observation,
                    classification,
                    eligibility,
                    session,
                    null,
                    null,
                    false,
                    eligibility.EligibleCandidates.Count == 0
                        ? "No explicitly registered LiveSafe candidate is uniquely eligible; no canary is allowed."
                        : "Multiple LiveSafe candidates are eligible and no ranking policy is proven; runtime fails closed without spending budget.");

            var candidate = eligibility.EligibleCandidates[0];
            if (!_catalog.TryBind(candidate, out var binding) || binding is null)
                return Result(
                    observation,
                    classification,
                    eligibility,
                    session,
                    null,
                    null,
                    false,
                    "The unique eligible action has no exact trusted action-to-mutation catalog binding; no budget was spent.");

            var evidenceSource = _evidenceSourceFactory(session);
            if (evidenceSource is null)
                return Result(
                    observation,
                    classification,
                    eligibility,
                    session,
                    null,
                    null,
                    false,
                    "No adapter-owned canary context evidence source is available for the exact session; no budget was spent.");

            var admission = _budget.TryAdmit(session, eligibility, candidate);
            if (!admission.Allowed)
                return Result(
                    observation,
                    classification,
                    eligibility,
                    session,
                    admission,
                    null,
                    false,
                    $"Session action budget denied the canary with status {admission.Status}.");

            var executor = new GenericGuardianWindowsSessionCanaryExecutor(
                _transactions,
                _capture,
                _evaluator,
                _catalog,
                _canarySampleDuration,
                evidenceSource,
                session,
                benchmarkAuthority: _benchmarkAuthority,
                sessionOwner: _sessionOwner);

            var canary = await executor.ExecuteAsync(
                eligibility,
                binding,
                cancellationToken).ConfigureAwait(false);

            var retained = false;
            if (canary.Kept && canary.ActiveLease is not null)
                retained = await _lifecycle.RetainAsync(session, canary).ConfigureAwait(false);

            return Result(
                observation,
                classification,
                eligibility,
                session,
                admission,
                canary,
                retained,
                retained
                    ? "Improved canary KEEP transferred to exact lifecycle ownership."
                    : canary.Reason);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var previousSession = _lifecycle.CurrentSession;
            if (previousSession is not null || _lifecycle.RetainedLeaseCount != 0)
            {
                await using var admission = await _teardownAdmission
                    .AcquireAsync("DG Guardian runtime teardown", cancellationToken)
                    .ConfigureAwait(false);
                await _lifecycle
                    .ResetUnderExperimentAsync(admission, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            else
            {
                await _lifecycle.ResetAsync(cancellationToken).ConfigureAwait(false);
            }

            if (previousSession is not null)
                _budget.ResetSession(previousSession);
            _observation.Reset();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        await _gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        var disposeGate = false;
        try
        {
            if (_disposed) return;

            var previousSession = _lifecycle.CurrentSession;
            if (previousSession is not null || _lifecycle.RetainedLeaseCount != 0)
            {
                await using var admission = await _teardownAdmission
                    .AcquireAsync(
                        "DG Guardian runtime dispose teardown",
                        CancellationToken.None)
                    .ConfigureAwait(false);
                await _lifecycle
                    .DisposeUnderExperimentAsync(admission)
                    .ConfigureAwait(false);
            }
            else
            {
                await _lifecycle.DisposeAsync().ConfigureAwait(false);
            }

            if (previousSession is not null)
                _budget.ResetSession(previousSession);
            _observation.Reset();

            _disposed = true;
            disposeGate = true;
        }
        finally
        {
            _gate.Release();
            if (disposeGate) _gate.Dispose();
        }
    }

    private static GenericGuardianWindowsRuntimeCycleResult Result(
        GenericGuardianWorkloadObservation observation,
        GenericGuardianBottleneckClassification classification,
        GenericGuardianSessionActionEligibility eligibility,
        GenericGuardianCanarySessionKey? session,
        GenericGuardianCanaryAdmission? admission,
        GenericGuardianSessionCanaryResult? canary,
        bool retained,
        string reason)
        => new()
        {
            Observation = observation,
            Classification = classification,
            Eligibility = eligibility,
            Session = session,
            Admission = admission,
            Canary = canary,
            Retained = retained,
            Reason = reason
        };
}
