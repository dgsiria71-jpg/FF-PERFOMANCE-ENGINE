namespace FFPerformanceEngine.Core.SystemOptimization;

public enum WindowsCapabilityValidatedEvidenceSource
{
    ValidatedEvidence
}

public sealed record WindowsCapabilityValidationChallengePolicy
{
    public int MinimumFinalObservationCount { get; init; } = 3;
    public double MinimumFinalConsistency { get; init; } = 0.95;
}

public sealed record WindowsCapabilityValidatedEvidence
{
    public int SchemaVersion { get; init; } = 1;
    public Guid Id { get; init; } = Guid.NewGuid();
    public string CapabilityId { get; init; } = string.Empty;
    public string BaselineValue { get; init; } = string.Empty;
    public string CandidateTarget { get; init; } = string.Empty;
    public string MachineFingerprintId { get; init; } = string.Empty;
    public string WorkloadKey { get; init; } = string.Empty;
    public int ObservationCount { get; init; }
    public double Consistency { get; init; }
    public double? MeanFpsRelativeDelta { get; init; }
    public double? MeanFrameTimeRelativeImprovement { get; init; }
    public double? MeanLatencyRelativeImprovement { get; init; }
    public DateTimeOffset ValidatedAt { get; init; } = DateTimeOffset.UtcNow;
    public WindowsCapabilityValidatedEvidenceSource Source { get; init; }
        = WindowsCapabilityValidatedEvidenceSource.ValidatedEvidence;

    // Validation is evidence provenance, not a registry recommendation.
    public string? RecommendedValue => null;
}

/// <summary>
/// Performs the explicit fresh challenge required to advance a Windows
/// capability tuple from PendingValidation to ValidatedEvidence. The service
/// delegates the actual controlled A/B to the existing experiment coordinator,
/// so discovery, global lease, Guardian suspension, PresentMon targeting,
/// transaction rollback, Cost Map recording and evaluation remain single-source.
/// It never publishes RecommendedValue.
/// </summary>
public sealed class WindowsCapabilityValidationChallengeService
{
    private readonly Func<string, CancellationToken, Task<WindowsCapabilityCandidatePlan>> _plan;
    private readonly Func<WindowsCapabilityCandidate, CancellationToken, Task<WindowsCapabilityExperimentRunResult>> _run;
    private readonly Func<string> _machineFingerprintIdProvider;
    private readonly Func<string> _workloadKeyProvider;
    private readonly WindowsCapabilityValidationChallengePolicy _policy;

    public WindowsCapabilityValidationChallengeService(
        Func<string, CancellationToken, Task<WindowsCapabilityCandidatePlan>> plan,
        Func<WindowsCapabilityCandidate, CancellationToken, Task<WindowsCapabilityExperimentRunResult>> run,
        Func<string> machineFingerprintIdProvider,
        Func<string> workloadKeyProvider,
        WindowsCapabilityValidationChallengePolicy? policy = null)
    {
        _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        _run = run ?? throw new ArgumentNullException(nameof(run));
        _machineFingerprintIdProvider = machineFingerprintIdProvider ?? throw new ArgumentNullException(nameof(machineFingerprintIdProvider));
        _workloadKeyProvider = workloadKeyProvider ?? throw new ArgumentNullException(nameof(workloadKeyProvider));
        _policy = policy ?? new WindowsCapabilityValidationChallengePolicy();

        if (_policy.MinimumFinalObservationCount < 3)
            throw new ArgumentOutOfRangeException(nameof(policy), "Explicit validation requires at least three total controlled observations.");
        if (!double.IsFinite(_policy.MinimumFinalConsistency)
            || _policy.MinimumFinalConsistency <= 0
            || _policy.MinimumFinalConsistency > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(policy), "Final validation consistency must be finite and within (0, 1].");
        }
    }

    public async Task<WindowsCapabilityValidatedEvidence> ValidateAsync(
        WindowsCapabilityValidationDecision pending,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pending);
        if (pending.Disposition != WindowsCapabilityValidationDisposition.PendingValidation)
            throw new InvalidOperationException("Only PendingValidation Windows capability evidence may enter the explicit validation challenge.");
        ValidatePendingIdentity(pending);

        var fingerprint = RequireCurrent(_machineFingerprintIdProvider(), "machine fingerprint");
        if (!string.Equals(fingerprint, pending.MachineFingerprintId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Windows capability validation was invalidated by machine/environment fingerprint drift before the fresh challenge.");

        var workload = RequireCurrent(_workloadKeyProvider(), "workload identity");
        if (!string.Equals(workload, pending.WorkloadKey, StringComparison.Ordinal))
            throw new InvalidOperationException("Windows capability validation was invalidated by workload drift before the fresh challenge.");

        var plan = await _plan(pending.CapabilityId, cancellationToken).ConfigureAwait(false);
        if (!plan.CanExplore)
            throw new InvalidOperationException($"Windows capability validation candidate is no longer explorable: {plan.Reason}");
        if (!string.Equals(plan.CurrentValue, pending.BaselineValue, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"Windows capability validation baseline drifted from '{pending.BaselineValue}' to '{plan.CurrentValue}'.");

        var candidate = plan.Candidates.FirstOrDefault(item =>
            string.Equals(item.CapabilityId, pending.CapabilityId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(item.TargetValue, pending.CandidateTarget, StringComparison.Ordinal));
        if (candidate is null)
            throw new InvalidOperationException("Windows capability validation target is stale or no longer in the freshly supported candidate space.");

        var result = await _run(candidate, cancellationToken).ConfigureAwait(false);
        EnsureSameTuple(pending, result);

        // The explicit challenge must contribute new evidence; replaying the same
        // summary is not a validation event.
        if (result.Evaluation.ObservationCount <= pending.ObservationCount
            || result.Evaluation.ObservationCount < _policy.MinimumFinalObservationCount)
        {
            throw new InvalidOperationException(
                "Explicit Windows capability validation did not add enough fresh controlled evidence beyond the PendingValidation state.");
        }

        if (result.Evaluation.Verdict != WindowsCapabilityEvidenceVerdict.Beneficial
            || result.Validation.Disposition != WindowsCapabilityValidationDisposition.PendingValidation)
        {
            throw new InvalidOperationException(
                $"Fresh validation evidence no longer supports the candidate: verdict={result.Evaluation.Verdict}, gate={result.Validation.Disposition}.");
        }

        if (!double.IsFinite(result.Evaluation.Consistency)
            || result.Evaluation.Consistency < _policy.MinimumFinalConsistency)
        {
            throw new InvalidOperationException(
                $"Fresh validation evidence consistency {result.Evaluation.Consistency:P0} is below the {_policy.MinimumFinalConsistency:P0} final validation floor.");
        }

        // Re-check context after the controlled run as well. The experiment itself
        // validates its measurement target; this guards the durable evidence label
        // against an environment/workload transition immediately around completion.
        var finalFingerprint = RequireCurrent(_machineFingerprintIdProvider(), "machine fingerprint");
        var finalWorkload = RequireCurrent(_workloadKeyProvider(), "workload identity");
        if (!string.Equals(finalFingerprint, pending.MachineFingerprintId, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(finalWorkload, pending.WorkloadKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Machine or workload identity changed during the explicit Windows capability validation challenge.");
        }

        return new WindowsCapabilityValidatedEvidence
        {
            CapabilityId = pending.CapabilityId.Trim().ToLowerInvariant(),
            BaselineValue = pending.BaselineValue,
            CandidateTarget = pending.CandidateTarget,
            MachineFingerprintId = pending.MachineFingerprintId.Trim(),
            WorkloadKey = pending.WorkloadKey.Trim(),
            ObservationCount = result.Evaluation.ObservationCount,
            Consistency = result.Evaluation.Consistency,
            MeanFpsRelativeDelta = result.Evaluation.MeanFpsRelativeDelta,
            MeanFrameTimeRelativeImprovement = result.Evaluation.MeanFrameTimeRelativeImprovement,
            MeanLatencyRelativeImprovement = result.Evaluation.MeanLatencyRelativeImprovement,
            ValidatedAt = result.Evaluation.LastObservedAt ?? DateTimeOffset.UtcNow,
            Source = WindowsCapabilityValidatedEvidenceSource.ValidatedEvidence
        };
    }

    private static void EnsureSameTuple(
        WindowsCapabilityValidationDecision pending,
        WindowsCapabilityExperimentRunResult result)
    {
        if (!string.Equals(result.Evaluation.CapabilityId, pending.CapabilityId, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(result.Evaluation.BaselineValue, pending.BaselineValue, StringComparison.Ordinal)
            || !string.Equals(result.Evaluation.CandidateTarget, pending.CandidateTarget, StringComparison.Ordinal)
            || !string.Equals(result.Evaluation.MachineFingerprintId, pending.MachineFingerprintId, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(result.Evaluation.WorkloadKey, pending.WorkloadKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Explicit Windows capability validation returned evidence for a different tuple than the PendingValidation decision.");
        }
    }

    private static void ValidatePendingIdentity(WindowsCapabilityValidationDecision pending)
    {
        if (string.IsNullOrWhiteSpace(pending.CapabilityId)
            || string.IsNullOrWhiteSpace(pending.BaselineValue)
            || string.IsNullOrWhiteSpace(pending.CandidateTarget)
            || string.IsNullOrWhiteSpace(pending.MachineFingerprintId)
            || string.IsNullOrWhiteSpace(pending.WorkloadKey))
        {
            throw new ArgumentException("PendingValidation evidence is missing its exact validation tuple.", nameof(pending));
        }
    }

    private static string RequireCurrent(string? value, string label)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException($"Explicit Windows capability validation requires a current {label}.");
        return normalized;
    }
}
