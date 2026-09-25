using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Workloads;

namespace FFPerformanceEngine.Core.Services;

/// <summary>
/// Read-only universal provenance for a generated winner profile that has
/// already been promoted by the specialized ProfileChallengeService. This
/// record grants no challenge, validation, winner, persistence or mutation
/// authority.
/// </summary>
public sealed record UniversalPromotedProfileProjection
{
    public required ProfileChallengeResult SpecializedResult { get; init; }
    public required PerformanceProfile PromotedProfile { get; init; }
    public required UniversalValidatedProfileProjection ChallengerProfile { get; init; }
    public required PerformanceComparisonHistoryRecord RevalidationRound { get; init; }
    public required UniversalTuningCandidate UniversalCandidate { get; init; }
    public required GameIdentity Identity { get; init; }
    public string AdapterId { get; init; } = string.Empty;
}

/// <summary>
/// Correlates an already-authorized specialized profile-challenge promotion
/// with the universal provenance previously proven for its Custom Validated
/// challenger. Challenge rounds currently carry no UniversalContext, so this
/// bridge never manufactures one and never uses universal metadata as
/// promotion authority.
/// </summary>
public static class BlueStacksUniversalProfileChallengeBridge
{
    public static UniversalPromotedProfileProjection? TryProjectPromotion(
        ProfileChallengeResult result,
        PerformanceProfile promotedProfile,
        UniversalValidatedProfileProjection challengerProfile,
        PerformanceComparisonHistoryRecord revalidationRound,
        BlueStacksUniversalTuningCandidateSpace candidateSpace)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(promotedProfile);
        ArgumentNullException.ThrowIfNull(challengerProfile);
        ArgumentNullException.ThrowIfNull(revalidationRound);
        ArgumentNullException.ThrowIfNull(candidateSpace);

        if (!result.Promoted
            || result.Status != ProfileChallengeStatus.Promoted
            || result.EvidenceRounds < 2
            || result.PromotedProfileId is null
            || result.PromotedProfileId != promotedProfile.Id
            || !ProfileChallengeEvaluator.IsWinnerRole(result.TargetKind))
            return null;

        if (promotedProfile.Kind != result.TargetKind
            || promotedProfile.Evidence != EvidenceLevel.Validated
            || promotedProfile.SourceComparisonId is null
            || promotedProfile.SourceComparisonId != revalidationRound.Id)
            return null;

        // Re-prove the complete upstream Custom Validated provenance instead of
        // trusting a caller-constructed universal projection object.
        var challengerReprojection = BlueStacksUniversalValidatedProfileBridge.TryProject(
            challengerProfile.SpecializedProfile,
            challengerProfile.SourceValidation,
            candidateSpace);
        if (challengerReprojection is null
            || !ReferenceEquals(challengerReprojection.SpecializedProfile, challengerProfile.SpecializedProfile)
            || !ReferenceEquals(challengerReprojection.SourceValidation, challengerProfile.SourceValidation)
            || !ReferenceEquals(challengerReprojection.UniversalCandidate, challengerProfile.UniversalCandidate))
            return null;

        PerformanceComparisonHistoryRecord round;
        PerformanceEvidenceSnapshot baseline;
        PerformanceEvidenceSnapshot candidate;
        PerformanceConfigurationSnapshot baselineConfiguration;
        PerformanceConfigurationSnapshot candidateConfiguration;
        try
        {
            round = revalidationRound.Rehydrate();
            baseline = PerformanceEvidenceSnapshot.Rehydrate(round.Baseline);
            candidate = PerformanceEvidenceSnapshot.Rehydrate(round.Candidate);
            baselineConfiguration = baseline.Configuration?.Rehydrate()
                ?? throw new InvalidDataException("Challenge revalidation baseline has no configuration.");
            candidateConfiguration = candidate.Configuration?.Rehydrate()
                ?? throw new InvalidDataException("Challenge revalidation candidate has no configuration.");
        }
        catch (Exception exception) when (exception is InvalidDataException or ArgumentException)
        {
            return null;
        }

        if (baseline.Quality != PerformanceEvidenceQuality.Measured
            || candidate.Quality != PerformanceEvidenceQuality.Measured
            || !baselineConfiguration.Environment.IsStructurallyEquivalentTo(candidateConfiguration.Environment))
            return null;

        var challenger = challengerProfile.SpecializedProfile;
        if (!ProfileMatchesConfiguration(challenger, candidateConfiguration)
            || !ProfileMatchesConfiguration(promotedProfile, candidateConfiguration))
            return null;

        if (string.IsNullOrWhiteSpace(challenger.EnvironmentFingerprint)
            || !string.Equals(challenger.EnvironmentFingerprint, candidateConfiguration.Environment.Id, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(promotedProfile.EnvironmentFingerprint, candidateConfiguration.Environment.Id, StringComparison.OrdinalIgnoreCase))
            return null;

        if (!Nullable.Equals(promotedProfile.AverageFps, candidate.AverageFps)
            || !Nullable.Equals(promotedProfile.OnePercentLow, ProfileChallengeEvaluator.OnePercentLow(candidate))
            || !Nullable.Equals(promotedProfile.FrameTimeMs, candidate.AverageFrameTimeMs)
            || !Nullable.Equals(promotedProfile.LatencyMs, candidate.AverageLatencyMs))
            return null;

        var identity = candidateSpace.Identity;
        if (identity is null
            || string.IsNullOrWhiteSpace(candidateSpace.AdapterId)
            || promotedProfile.Game != identity.LegacyGameKind)
            return null;

        return new UniversalPromotedProfileProjection
        {
            SpecializedResult = result,
            PromotedProfile = promotedProfile,
            ChallengerProfile = challengerProfile,
            RevalidationRound = revalidationRound,
            UniversalCandidate = challengerProfile.UniversalCandidate,
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
