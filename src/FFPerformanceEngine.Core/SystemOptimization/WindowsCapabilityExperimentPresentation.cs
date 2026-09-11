namespace FFPerformanceEngine.Core.SystemOptimization;

/// <summary>
/// Read-only presentation contract for the Windows capability experiment UI.
/// Action availability is derived exclusively from Core validation state:
/// PendingValidation may be explicitly validated; only durable ValidatedEvidence
/// may be offered to the recommendation authority.
/// </summary>
public sealed record WindowsCapabilityExperimentPresentation
{
    public string CapabilityId { get; init; } = string.Empty;
    public string BaselineValue { get; init; } = string.Empty;
    public string CandidateTarget { get; init; } = string.Empty;
    public string StateLabel { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public WindowsCapabilityEvidenceVerdict Verdict { get; init; }
    public int ObservationCount { get; init; }
    public double Consistency { get; init; }
    public double FpsDeltaPercent { get; init; }
    public double FrameTimeImprovementPercent { get; init; }
    public double LatencyImprovementPercent { get; init; }
    public bool CanValidate { get; init; }
    public bool CanPublishRecommendation { get; init; }
    public WindowsCapabilityValidationDecision? PendingValidation { get; init; }
    public WindowsCapabilityValidatedEvidence? ValidatedEvidence { get; init; }

    public static WindowsCapabilityExperimentPresentation FromRun(
        WindowsCapabilityExperimentRunResult run)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(run.Evaluation);
        ArgumentNullException.ThrowIfNull(run.Validation);

        var canValidate = run.Validation.Disposition == WindowsCapabilityValidationDisposition.PendingValidation;
        return new WindowsCapabilityExperimentPresentation
        {
            CapabilityId = run.Evaluation.CapabilityId,
            BaselineValue = run.Evaluation.BaselineValue,
            CandidateTarget = run.Evaluation.CandidateTarget,
            StateLabel = canValidate
                ? "PENDING VALIDATION"
                : StateFor(run.Evaluation.Verdict),
            Reason = run.Validation.Reason,
            Verdict = run.Evaluation.Verdict,
            ObservationCount = run.Evaluation.ObservationCount,
            Consistency = NormalizeConsistency(run.Evaluation.Consistency),
            FpsDeltaPercent = Percent(run.Evaluation.MeanFpsRelativeDelta),
            FrameTimeImprovementPercent = Percent(run.Evaluation.MeanFrameTimeRelativeImprovement),
            LatencyImprovementPercent = Percent(run.Evaluation.MeanLatencyRelativeImprovement),
            CanValidate = canValidate,
            CanPublishRecommendation = false,
            PendingValidation = canValidate ? run.Validation : null,
            ValidatedEvidence = null
        };
    }

    public static WindowsCapabilityExperimentPresentation FromValidated(
        WindowsCapabilityValidatedEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.Source != WindowsCapabilityValidatedEvidenceSource.ValidatedEvidence)
            throw new ArgumentException("Experiment presentation requires explicit ValidatedEvidence provenance.", nameof(evidence));
        if (string.IsNullOrWhiteSpace(evidence.CapabilityId)
            || string.IsNullOrWhiteSpace(evidence.BaselineValue)
            || string.IsNullOrWhiteSpace(evidence.CandidateTarget)
            || evidence.ObservationCount < 3
            || !double.IsFinite(evidence.Consistency)
            || evidence.Consistency <= 0
            || evidence.Consistency > 1)
        {
            throw new ArgumentException("ValidatedEvidence is structurally incomplete for presentation.", nameof(evidence));
        }

        return new WindowsCapabilityExperimentPresentation
        {
            CapabilityId = evidence.CapabilityId,
            BaselineValue = evidence.BaselineValue,
            CandidateTarget = evidence.CandidateTarget,
            StateLabel = "VALIDATED EVIDENCE",
            Reason = $"Fresh controlled validation completed with {evidence.ObservationCount} observations at {evidence.Consistency:P0} consistency.",
            Verdict = WindowsCapabilityEvidenceVerdict.Beneficial,
            ObservationCount = evidence.ObservationCount,
            Consistency = evidence.Consistency,
            FpsDeltaPercent = Percent(evidence.MeanFpsRelativeDelta),
            FrameTimeImprovementPercent = Percent(evidence.MeanFrameTimeRelativeImprovement),
            LatencyImprovementPercent = Percent(evidence.MeanLatencyRelativeImprovement),
            CanValidate = false,
            CanPublishRecommendation = true,
            PendingValidation = null,
            ValidatedEvidence = evidence
        };
    }

    private static string StateFor(WindowsCapabilityEvidenceVerdict verdict)
        => verdict switch
        {
            WindowsCapabilityEvidenceVerdict.Beneficial => "BENEFICIAL · NOT ELIGIBLE",
            WindowsCapabilityEvidenceVerdict.Regressive => "REGRESSIVE",
            WindowsCapabilityEvidenceVerdict.Mixed => "MIXED",
            WindowsCapabilityEvidenceVerdict.Inconclusive => "INCONCLUSIVE",
            _ => "INSUFFICIENT EVIDENCE"
        };

    private static double Percent(double? value)
        => value is double finite && double.IsFinite(finite)
            ? finite * 100d
            : 0d;

    private static double NormalizeConsistency(double value)
        => double.IsFinite(value) ? Math.Clamp(value, 0d, 1d) : 0d;
}
