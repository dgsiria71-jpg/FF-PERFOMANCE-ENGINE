using FFPerformanceEngine.Core.Diagnostics;

namespace FFPerformanceEngine.Core.SystemOptimization;

/// <summary>
/// Explicit bridge from durable ValidatedEvidence to persistent recommendation
/// publication. It does not perform experiments and it does not mutate Windows.
/// Only the latest durable record for the exact tuple may cross this boundary,
/// and the machine, workload, baseline and supported candidate space are checked
/// again immediately before delegating to the existing recommendation authority.
/// </summary>
public sealed class WindowsCapabilityValidatedRecommendationService
{
    private readonly WindowsCapabilityValidatedEvidenceStore _store;
    private readonly Func<string, CancellationToken, Task<WindowsCapabilityCandidatePlan>> _plan;
    private readonly Func<string> _machineFingerprintIdProvider;
    private readonly Func<string> _workloadKeyProvider;
    private readonly Func<string, string, CapabilityRecommendationSummary, CancellationToken, Task<CapabilityRecommendationPublicationResult>> _publish;

    public WindowsCapabilityValidatedRecommendationService(
        WindowsCapabilityValidatedEvidenceStore store,
        Func<string, CancellationToken, Task<WindowsCapabilityCandidatePlan>> plan,
        Func<string> machineFingerprintIdProvider,
        Func<string> workloadKeyProvider,
        Func<string, string, CapabilityRecommendationSummary, CancellationToken, Task<CapabilityRecommendationPublicationResult>> publish)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        _machineFingerprintIdProvider = machineFingerprintIdProvider ?? throw new ArgumentNullException(nameof(machineFingerprintIdProvider));
        _workloadKeyProvider = workloadKeyProvider ?? throw new ArgumentNullException(nameof(workloadKeyProvider));
        _publish = publish ?? throw new ArgumentNullException(nameof(publish));
    }

    /// <summary>
    /// Resolves the newest durable ValidatedEvidence that is still compatible
    /// with the exact current machine/workload/baseline/candidate space. This is
    /// a read-only resume seam for UI/session reconstruction; it never publishes
    /// a recommendation and returns null when any freshness/context gate drifts.
    /// </summary>
    public async Task<WindowsCapabilityValidatedEvidence?> ResolveCurrentAsync(
        string capabilityId,
        string candidateTarget,
        CancellationToken cancellationToken = default)
    {
        var id = capabilityId?.Trim().ToLowerInvariant() ?? string.Empty;
        var target = candidateTarget?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("A capability id is required to resume ValidatedEvidence.", nameof(capabilityId));
        if (string.IsNullOrWhiteSpace(target))
            throw new ArgumentException("A candidate target is required to resume ValidatedEvidence.", nameof(candidateTarget));

        var fingerprint = RequireCurrent(_machineFingerprintIdProvider(), "machine fingerprint");
        var workload = RequireCurrent(_workloadKeyProvider(), "workload identity");

        var plan = await _plan(id, cancellationToken).ConfigureAwait(false);
        if (!SupportsTarget(plan, id, target) || string.IsNullOrWhiteSpace(plan.CurrentValue))
            return null;

        var latest = await _store.GetLatestAsync(
                id,
                plan.CurrentValue,
                target,
                fingerprint,
                workload,
                cancellationToken)
            .ConfigureAwait(false);
        if (latest is null || !IsStructurallyValid(latest))
            return null;

        var finalFingerprint = RequireCurrent(_machineFingerprintIdProvider(), "machine fingerprint");
        var finalWorkload = RequireCurrent(_workloadKeyProvider(), "workload identity");
        if (!string.Equals(finalFingerprint, fingerprint, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(finalWorkload, workload, StringComparison.Ordinal))
            return null;

        // Re-plan after the durable read so the UI cannot surface a publish action
        // from evidence whose baseline or supported target changed during resume.
        var finalPlan = await _plan(id, cancellationToken).ConfigureAwait(false);
        if (!SupportsTarget(finalPlan, id, target)
            || !string.Equals(finalPlan.CurrentValue, latest.BaselineValue, StringComparison.Ordinal))
            return null;

        return latest;
    }

    public async Task<CapabilityRecommendationPublicationResult> PublishAsync(
        WindowsCapabilityValidatedEvidence evidence,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        ValidateIdentity(evidence);

        var fingerprint = RequireCurrent(_machineFingerprintIdProvider(), "machine fingerprint");
        if (!string.Equals(fingerprint, evidence.MachineFingerprintId.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("ValidatedEvidence recommendation promotion was invalidated by machine/environment fingerprint drift.");

        var workload = RequireCurrent(_workloadKeyProvider(), "workload identity");
        if (!string.Equals(workload, evidence.WorkloadKey.Trim(), StringComparison.Ordinal))
            throw new InvalidOperationException("ValidatedEvidence recommendation promotion was invalidated by workload drift.");

        await EnsureLatestAsync(evidence, cancellationToken).ConfigureAwait(false);

        var plan = await _plan(evidence.CapabilityId, cancellationToken).ConfigureAwait(false);
        if (!plan.CanExplore)
            throw new InvalidOperationException($"ValidatedEvidence target is no longer explorable: {plan.Reason}");
        if (!string.Equals(plan.CurrentValue, evidence.BaselineValue, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"ValidatedEvidence baseline drifted from '{evidence.BaselineValue}' to '{plan.CurrentValue}'.");
        }

        var candidateStillSupported = plan.Candidates.Any(candidate =>
            string.Equals(candidate.CapabilityId, evidence.CapabilityId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(candidate.TargetValue, evidence.CandidateTarget, StringComparison.Ordinal));
        if (!candidateStillSupported)
            throw new InvalidOperationException("ValidatedEvidence target is stale or no longer in the freshly supported candidate space.");

        var finalFingerprint = RequireCurrent(_machineFingerprintIdProvider(), "machine fingerprint");
        var finalWorkload = RequireCurrent(_workloadKeyProvider(), "workload identity");
        if (!string.Equals(finalFingerprint, evidence.MachineFingerprintId.Trim(), StringComparison.OrdinalIgnoreCase)
            || !string.Equals(finalWorkload, evidence.WorkloadKey.Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Machine or workload identity changed while preparing ValidatedEvidence recommendation promotion.");
        }

        // Close the window where another validation for the same exact tuple
        // could supersede this record while fresh capability discovery ran.
        await EnsureLatestAsync(evidence, cancellationToken).ConfigureAwait(false);

        var recommendation = new CapabilityRecommendationSummary
        {
            Source = CapabilityRecommendationSource.ValidatedEvidence,
            Confidence = evidence.Consistency,
            MachineFingerprintId = evidence.MachineFingerprintId.Trim(),
            GeneratedAt = evidence.ValidatedAt
        };

        return await _publish(
                evidence.CapabilityId.Trim().ToLowerInvariant(),
                evidence.CandidateTarget,
                recommendation,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task EnsureLatestAsync(
        WindowsCapabilityValidatedEvidence evidence,
        CancellationToken cancellationToken)
    {
        var latest = await _store.GetLatestAsync(
                evidence.CapabilityId,
                evidence.BaselineValue,
                evidence.CandidateTarget,
                evidence.MachineFingerprintId,
                evidence.WorkloadKey,
                cancellationToken)
            .ConfigureAwait(false);

        if (latest is null)
            throw new InvalidOperationException("ValidatedEvidence must be durably stored before it can publish a persistent recommendation.");
        if (latest.Id != evidence.Id)
            throw new InvalidOperationException("ValidatedEvidence is stale because a newer validation exists for the same exact tuple.");
    }

    private static bool SupportsTarget(
        WindowsCapabilityCandidatePlan plan,
        string capabilityId,
        string candidateTarget)
        => plan.CanExplore
           && plan.Candidates.Any(candidate =>
               string.Equals(candidate.CapabilityId, capabilityId, StringComparison.OrdinalIgnoreCase)
               && string.Equals(candidate.TargetValue, candidateTarget, StringComparison.Ordinal));

    private static bool IsStructurallyValid(WindowsCapabilityValidatedEvidence evidence)
        => evidence.Source == WindowsCapabilityValidatedEvidenceSource.ValidatedEvidence
           && !string.IsNullOrWhiteSpace(evidence.CapabilityId)
           && !string.IsNullOrWhiteSpace(evidence.BaselineValue)
           && !string.IsNullOrWhiteSpace(evidence.CandidateTarget)
           && !string.IsNullOrWhiteSpace(evidence.MachineFingerprintId)
           && !string.IsNullOrWhiteSpace(evidence.WorkloadKey)
           && evidence.ObservationCount >= 3
           && double.IsFinite(evidence.Consistency)
           && evidence.Consistency > 0
           && evidence.Consistency <= 1
           && evidence.ValidatedAt != default;

    private static void ValidateIdentity(WindowsCapabilityValidatedEvidence evidence)
    {
        if (!IsStructurallyValid(evidence))
            throw new ArgumentException("ValidatedEvidence provenance is structurally invalid for recommendation promotion.", nameof(evidence));
    }

    private static string RequireCurrent(string? value, string label)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException($"ValidatedEvidence recommendation promotion requires a current {label}.");
        return normalized;
    }
}
