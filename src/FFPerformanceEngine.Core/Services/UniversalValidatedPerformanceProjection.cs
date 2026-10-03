using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

/// <summary>
/// Read-only universal correlation for a comparison that has already been
/// authorized by the existing specialized History/profile-origin chain.
/// This record grants no validation, winner, profile or persistence authority.
/// </summary>
public sealed record UniversalValidatedPerformanceProjection
{
    public required PerformanceComparisonHistoryRecord SpecializedRecord { get; init; }
    public required UniversalTuningCandidate UniversalCandidate { get; init; }
}

/// <summary>
/// Correlates an already-authorized BlueStacks/Free Fire validation record to
/// the exact Slice 3 universal candidate binding. Universal metadata is never a
/// substitute for specialized validation authority: when any additive context
/// is missing, stale, mismatched or ambiguous, no projection is returned.
/// </summary>
public static class BlueStacksUniversalValidatedPerformanceBridge
{
    public static UniversalValidatedPerformanceProjection? TryProject(
        PerformanceComparisonHistoryRecord record,
        BlueStacksUniversalTuningCandidateSpace candidateSpace)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(candidateSpace);

        // This is intentionally the first authority gate. Universal metadata
        // can only narrow an already-authorized specialized record; it can never
        // make a record eligible for profile origin.
        if (!CanOriginateProfile(record)) return null;

        PerformanceComparisonHistoryRecord normalized;
        try
        {
            normalized = record.Rehydrate();
        }
        catch (Exception exception) when (exception is InvalidDataException or ArgumentException)
        {
            return null;
        }

        if (!CanOriginateProfile(normalized)) return null;
        if (normalized.Candidate.Quality != PerformanceEvidenceQuality.Measured
            || normalized.ValidationEvidence?.Quality != PerformanceEvidenceQuality.Measured)
            return null;
        if (normalized.ValidationEvidence.CapturedAt <= normalized.Candidate.CapturedAt)
            return null;

        var candidateConfiguration = normalized.Candidate.Configuration;
        var validationConfiguration = normalized.ValidationEvidence.Configuration;
        if (candidateConfiguration is null
            || validationConfiguration is null
            || !ConfigurationsEquivalent(candidateConfiguration, validationConfiguration))
            return null;

        var candidateContext = RehydrateContext(normalized.Candidate.UniversalContext);
        var validationContext = RehydrateContext(normalized.ValidationEvidence.UniversalContext);
        if (candidateContext is null
            || validationContext is null
            || !candidateContext.IsEquivalentTo(validationContext))
            return null;

        var identity = candidateSpace.Identity;
        if (identity is null) return null;

        var identityGameId = NormalizeId(identity.GameId);
        var identityAdapterId = NormalizeId(identity.AdapterId);
        var spaceAdapterId = NormalizeId(candidateSpace.AdapterId);
        if (string.IsNullOrWhiteSpace(identityGameId)
            || string.IsNullOrWhiteSpace(identityAdapterId)
            || string.IsNullOrWhiteSpace(spaceAdapterId)
            || !string.Equals(identityAdapterId, spaceAdapterId, StringComparison.Ordinal))
            return null;

        if (identity.LegacyGameKind != candidateConfiguration.Game)
            return null;
        if (!string.Equals(candidateContext.GameId, identityGameId, StringComparison.Ordinal)
            || !string.Equals(candidateContext.AdapterId, spaceAdapterId, StringComparison.Ordinal))
            return null;

        var matches = (candidateSpace.Bindings ?? Array.Empty<BlueStacksUniversalTuningCandidateBinding>())
            .Where(binding => binding is not null
                              && binding.UniversalCandidate is not null
                              && SpecializedCandidateMatchesConfiguration(
                                  binding.SpecializedCandidate,
                                  candidateConfiguration))
            .ToArray();
        if (matches.Length != 1) return null;

        return new UniversalValidatedPerformanceProjection
        {
            SpecializedRecord = record,
            UniversalCandidate = matches[0].UniversalCandidate
        };
    }

    private static bool CanOriginateProfile(PerformanceComparisonHistoryRecord record)
    {
        try
        {
            return record.CanOriginateProfile;
        }
        catch (Exception exception) when (exception is InvalidDataException or ArgumentException)
        {
            return false;
        }
    }

    private static bool ConfigurationsEquivalent(
        PerformanceConfigurationSnapshot candidate,
        PerformanceConfigurationSnapshot validation)
    {
        try
        {
            return candidate.IsEquivalentTo(validation);
        }
        catch (Exception exception) when (exception is InvalidDataException or ArgumentException)
        {
            return false;
        }
    }

    private static PerformanceUniversalConfigurationContext? RehydrateContext(
        PerformanceUniversalConfigurationContext? context)
    {
        if (context is null) return null;
        try
        {
            return context.Rehydrate();
        }
        catch (Exception exception) when (exception is InvalidDataException or ArgumentException)
        {
            return null;
        }
    }

    private static bool SpecializedCandidateMatchesConfiguration(
        TuningCandidate candidate,
        PerformanceConfigurationSnapshot configuration)
        => candidate.CpuCores == configuration.CpuCores
           && candidate.RamMb == configuration.RamMb
           && candidate.FpsTarget == configuration.FpsTarget
           && string.Equals(candidate.Renderer, configuration.Renderer, StringComparison.OrdinalIgnoreCase)
           && string.Equals(candidate.Resolution, configuration.Resolution, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeId(string? value)
        => value?.Trim().ToLowerInvariant() ?? string.Empty;
}
