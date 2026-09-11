namespace FFPerformanceEngine.Core.SystemOptimization;

public enum WindowsCapabilityValidationDisposition
{
    NotEligible,
    PendingValidation
}

public sealed record WindowsCapabilityValidationPolicy
{
    public int MinimumObservationCount { get; init; } = 2;
    public double MinimumConsistency { get; init; } = 0.95;
}

public sealed record WindowsCapabilityValidationDecision
{
    public string CapabilityId { get; init; } = string.Empty;
    public string BaselineValue { get; init; } = string.Empty;
    public string CandidateTarget { get; init; } = string.Empty;
    public string MachineFingerprintId { get; init; } = string.Empty;
    public string WorkloadKey { get; init; } = string.Empty;
    public int ObservationCount { get; init; }
    public double Consistency { get; init; }
    public WindowsCapabilityEvidenceVerdict EvidenceVerdict { get; init; }
    public WindowsCapabilityValidationDisposition Disposition { get; init; }
    public string Reason { get; init; } = string.Empty;

    // PendingValidation is deliberately not a recommendation. Only the later
    // validation/recommendation authority may publish a persistent target.
    public string? RecommendedValue => null;
}

/// <summary>
/// Advances repeated controlled Windows capability evidence only as far as
/// PendingValidation. This gate performs no mutation, persistence or
/// recommendation publication and therefore cannot silently promote evidence.
/// </summary>
public sealed class WindowsCapabilityValidationGate
{
    private readonly WindowsCapabilityValidationPolicy _policy;

    public WindowsCapabilityValidationGate(WindowsCapabilityValidationPolicy? policy = null)
    {
        _policy = policy ?? new WindowsCapabilityValidationPolicy();
        if (_policy.MinimumObservationCount < 2)
            throw new ArgumentOutOfRangeException(nameof(policy), "Pending validation requires at least two independent controlled observations.");
        if (!double.IsFinite(_policy.MinimumConsistency)
            || _policy.MinimumConsistency <= 0
            || _policy.MinimumConsistency > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(policy), "Validation consistency must be finite and within (0, 1].");
        }
    }

    public WindowsCapabilityValidationDecision Evaluate(WindowsCapabilityEvidenceEvaluation evaluation)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        ValidateIdentity(evaluation);

        var pending = evaluation.Verdict == WindowsCapabilityEvidenceVerdict.Beneficial
                      && evaluation.ObservationCount >= _policy.MinimumObservationCount
                      && double.IsFinite(evaluation.Consistency)
                      && evaluation.Consistency >= _policy.MinimumConsistency;

        return new WindowsCapabilityValidationDecision
        {
            CapabilityId = evaluation.CapabilityId.Trim().ToLowerInvariant(),
            BaselineValue = evaluation.BaselineValue,
            CandidateTarget = evaluation.CandidateTarget,
            MachineFingerprintId = evaluation.MachineFingerprintId.Trim(),
            WorkloadKey = evaluation.WorkloadKey.Trim(),
            ObservationCount = evaluation.ObservationCount,
            Consistency = evaluation.Consistency,
            EvidenceVerdict = evaluation.Verdict,
            Disposition = pending
                ? WindowsCapabilityValidationDisposition.PendingValidation
                : WindowsCapabilityValidationDisposition.NotEligible,
            Reason = pending
                ? $"Repeated controlled evidence is beneficial and consistent ({evaluation.ObservationCount} rounds, {evaluation.Consistency:P0}); explicit validation is required before recommendation."
                : BuildIneligibleReason(evaluation)
        };
    }

    private string BuildIneligibleReason(WindowsCapabilityEvidenceEvaluation evaluation)
    {
        if (evaluation.Verdict != WindowsCapabilityEvidenceVerdict.Beneficial)
            return $"Evidence verdict is {evaluation.Verdict}; only repeated beneficial evidence may enter PendingValidation.";
        if (evaluation.ObservationCount < _policy.MinimumObservationCount)
            return $"Only {evaluation.ObservationCount} controlled observation(s) are available; {_policy.MinimumObservationCount} are required.";
        if (!double.IsFinite(evaluation.Consistency) || evaluation.Consistency < _policy.MinimumConsistency)
            return $"Evidence consistency {evaluation.Consistency:P0} is below the {_policy.MinimumConsistency:P0} validation floor.";
        return "Evidence is not eligible for PendingValidation.";
    }

    private static void ValidateIdentity(WindowsCapabilityEvidenceEvaluation evaluation)
    {
        if (string.IsNullOrWhiteSpace(evaluation.CapabilityId))
            throw new ArgumentException("Validation evidence requires a capability id.", nameof(evaluation));
        if (string.IsNullOrWhiteSpace(evaluation.BaselineValue))
            throw new ArgumentException("Validation evidence requires a baseline value.", nameof(evaluation));
        if (string.IsNullOrWhiteSpace(evaluation.CandidateTarget))
            throw new ArgumentException("Validation evidence requires a candidate target.", nameof(evaluation));
        if (string.IsNullOrWhiteSpace(evaluation.MachineFingerprintId))
            throw new ArgumentException("Validation evidence requires a machine fingerprint identity.", nameof(evaluation));
        if (string.IsNullOrWhiteSpace(evaluation.WorkloadKey))
            throw new ArgumentException("Validation evidence requires a workload identity.", nameof(evaluation));
    }
}
