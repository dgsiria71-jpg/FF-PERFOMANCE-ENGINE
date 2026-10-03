using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Workloads;

namespace FFPerformanceEngine.Core.Services;

/// <summary>
/// Read-only universal provenance for a specialized Custom Validated profile
/// that has already been explicitly originated by ProfileService from a
/// separately validated History comparison. This record grants no profile,
/// winner, validation, recommendation, persistence or mutation authority.
/// </summary>
public sealed record UniversalValidatedProfileProjection
{
    public required PerformanceProfile SpecializedProfile { get; init; }
    public required UniversalValidatedPerformanceProjection SourceValidation { get; init; }
    public required UniversalTuningCandidate UniversalCandidate { get; init; }
    public required GameIdentity Identity { get; init; }
    public string AdapterId { get; init; } = string.Empty;
}

/// <summary>
/// Correlates an existing specialized Custom Validated profile back to the
/// already-proven universal validation projection that originated it. The
/// specialized ProfileService remains the only profile-origin/persistence
/// authority; this bridge only proves exact provenance and fails closed.
/// </summary>
public static class BlueStacksUniversalValidatedProfileBridge
{
    public static UniversalValidatedProfileProjection? TryProject(
        PerformanceProfile profile,
        UniversalValidatedPerformanceProjection sourceValidation,
        BlueStacksUniversalTuningCandidateSpace candidateSpace)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(sourceValidation);
        ArgumentNullException.ThrowIfNull(candidateSpace);

        if (profile.Kind != ProfileKind.Custom
            || profile.Evidence != EvidenceLevel.Validated
            || profile.SourceComparisonId is null
            || profile.SourceComparisonId != sourceValidation.SpecializedRecord.Id)
            return null;

        // Never trust a caller-constructed projection object as authority. Re-run
        // the existing validated-History correlator against the exact candidate
        // space and require the same candidate object to be the proven binding.
        var reprojection = BlueStacksUniversalValidatedPerformanceBridge.TryProject(
            sourceValidation.SpecializedRecord,
            candidateSpace);
        if (reprojection is null
            || !ReferenceEquals(reprojection.SpecializedRecord, sourceValidation.SpecializedRecord)
            || !ReferenceEquals(reprojection.UniversalCandidate, sourceValidation.UniversalCandidate))
            return null;

        PerformanceComparisonHistoryRecord record;
        PerformanceConfigurationSnapshot configuration;
        PerformanceEvidenceSnapshot validation;
        try
        {
            record = sourceValidation.SpecializedRecord.Rehydrate();
            configuration = record.Candidate.Configuration?.Rehydrate()
                ?? throw new InvalidDataException("Validated profile source has no candidate configuration.");
            validation = record.ValidationEvidence is null
                ? throw new InvalidDataException("Validated profile source has no separate validation evidence.")
                : PerformanceEvidenceSnapshot.Rehydrate(record.ValidationEvidence);
        }
        catch (Exception exception) when (exception is InvalidDataException or ArgumentException)
        {
            return null;
        }

        if (!ProfileMatchesConfiguration(profile, configuration))
            return null;
        if (!string.Equals(
                profile.EnvironmentFingerprint,
                configuration.Environment.Id,
                StringComparison.OrdinalIgnoreCase))
            return null;
        if (!Nullable.Equals(profile.AverageFps, validation.AverageFps)
            || !Nullable.Equals(profile.FrameTimeMs, validation.AverageFrameTimeMs)
            || !Nullable.Equals(profile.LatencyMs, validation.AverageLatencyMs))
            return null;

        var identity = candidateSpace.Identity;
        if (identity is null || string.IsNullOrWhiteSpace(candidateSpace.AdapterId))
            return null;

        return new UniversalValidatedProfileProjection
        {
            SpecializedProfile = profile,
            SourceValidation = sourceValidation,
            UniversalCandidate = sourceValidation.UniversalCandidate,
            Identity = identity,
            AdapterId = candidateSpace.AdapterId
        };
    }

    private static bool ProfileMatchesConfiguration(
        PerformanceProfile profile,
        PerformanceConfigurationSnapshot configuration)
        => profile.Game == configuration.Game
           && string.Equals(profile.InstanceName, configuration.InstanceName, StringComparison.OrdinalIgnoreCase)
           && profile.CpuCores == configuration.CpuCores
           && profile.RamMb == configuration.RamMb
           && string.Equals(profile.Renderer, configuration.Renderer, StringComparison.OrdinalIgnoreCase)
           && profile.FpsTarget == configuration.FpsTarget
           && string.Equals(profile.Resolution, configuration.Resolution, StringComparison.OrdinalIgnoreCase)
           && profile.Dpi == configuration.Dpi;
}
