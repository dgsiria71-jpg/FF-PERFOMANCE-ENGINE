using System.Text.Json;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Workloads;

namespace FFPerformanceEngine.Core.Services;

/// <summary>
/// Parsed durable receipt emitted only after the specialized ProfileChallengeService
/// has already persisted a winner promotion. The receipt is correlation evidence;
/// it does not recreate ProfileChallengeResult or grant promotion authority.
/// </summary>
public sealed record UniversalPersistedPromotionReceipt
{
    public required HistoryEvent Event { get; init; }
    public Guid ChallengerProfileId { get; init; }
    public Guid PreviousWinnerId { get; init; }
    public Guid PromotedProfileId { get; init; }
    public Guid RevalidationComparisonId { get; init; }
    public ProfileKind TargetKind { get; init; }
}

/// <summary>
/// Read-only universal provenance for a generated winner that was already promoted
/// by the specialized challenge path and can still be correlated exactly after
/// restart to its durable receipt, revalidation round and current Custom provenance.
/// </summary>
public sealed record UniversalPersistedPromotedProfileProjection
{
    public required PerformanceProfile PromotedProfile { get; init; }
    public required UniversalPersistedPromotionReceipt PromotionReceipt { get; init; }
    public required UniversalValidatedProfileProjection ChallengerProfile { get; init; }
    public required PerformanceComparisonHistoryRecord RevalidationRound { get; init; }
    public required UniversalTuningCandidate UniversalCandidate { get; init; }
    public required GameIdentity Identity { get; init; }
    public string AdapterId { get; init; } = string.Empty;
}

/// <summary>
/// Re-proves current universal provenance for an already-persisted promoted winner.
/// The specialized persisted profile + History receipt remain the only durable claim
/// that a promotion happened. This service never re-evaluates challenge verdicts,
/// reconstructs ProfileChallengeResult, mutates profiles or writes History.
/// </summary>
public sealed class UniversalPersistedPromotedProfileProvenanceService
{
    private readonly ProfileService _profiles;
    private readonly HistoryService _history;
    private readonly UniversalValidatedProfileProvenanceService _customResolver;

    public UniversalPersistedPromotedProfileProvenanceService(
        ProfileService profiles,
        HistoryService history,
        UniversalValidatedProfileProvenanceService customResolver)
    {
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _history = history ?? throw new ArgumentNullException(nameof(history));
        _customResolver = customResolver ?? throw new ArgumentNullException(nameof(customResolver));
    }

    public async Task<UniversalPersistedPromotedProfileProjection?> ResolveCurrentAsync(
        Guid promotedProfileId,
        EnvironmentSnapshot currentEnvironment,
        BlueStacksInstance currentInstance,
        IReadOnlyDictionary<string, string> capturedSettings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currentEnvironment);
        ArgumentNullException.ThrowIfNull(currentInstance);
        ArgumentNullException.ThrowIfNull(capturedSettings);
        cancellationToken.ThrowIfCancellationRequested();

        var profiles = await _profiles.LoadAsync(cancellationToken).ConfigureAwait(false);
        var promotedMatches = profiles
            .Where(profile => profile.Id == promotedProfileId)
            .Take(2)
            .ToArray();
        if (promotedMatches.Length != 1) return null;

        var promoted = promotedMatches[0];
        if (!ProfileChallengeEvaluator.IsWinnerRole(promoted.Kind)
            || promoted.Evidence != EvidenceLevel.Validated
            || promoted.SourceComparisonId is null
            || promoted.Game is not (GameKind.FreeFire or GameKind.FreeFireMax)
            || string.IsNullOrWhiteSpace(promoted.InstanceName)
            || string.IsNullOrWhiteSpace(promoted.EnvironmentFingerprint)
            || !string.Equals(promoted.InstanceName, currentInstance.Name, StringComparison.OrdinalIgnoreCase))
            return null;

        var receiptMatches = (await _history.LoadAsync(cancellationToken).ConfigureAwait(false))
            .Select(TryParsePromotionReceipt)
            .Where(receipt => receipt?.PromotedProfileId == promoted.Id)
            .Take(2)
            .ToArray();
        if (receiptMatches.Length != 1 || receiptMatches[0] is not { } receipt)
            return null;

        if (receipt.TargetKind != promoted.Kind
            || receipt.PromotedProfileId != promoted.Id
            || receipt.RevalidationComparisonId != promoted.SourceComparisonId.Value
            || receipt.ChallengerProfileId == Guid.Empty
            || receipt.PreviousWinnerId == Guid.Empty
            || receipt.PromotedProfileId == Guid.Empty
            || receipt.RevalidationComparisonId == Guid.Empty
            || receipt.ChallengerProfileId == receipt.PromotedProfileId
            || receipt.PreviousWinnerId == receipt.PromotedProfileId
            || receipt.PreviousWinnerId == receipt.ChallengerProfileId)
            return null;

        var challengerMatches = profiles
            .Where(profile => profile.Id == receipt.ChallengerProfileId)
            .Take(2)
            .ToArray();
        if (challengerMatches.Length != 1) return null;

        var challenger = challengerMatches[0];
        if (challenger.Kind != ProfileKind.Custom
            || challenger.Evidence != EvidenceLevel.Validated
            || challenger.Game != promoted.Game
            || !string.Equals(challenger.InstanceName, promoted.InstanceName, StringComparison.OrdinalIgnoreCase))
            return null;

        var revalidationMatches = (await _history.LoadPerformanceComparisonsAsync(cancellationToken).ConfigureAwait(false))
            .Where(record => record.Id == receipt.RevalidationComparisonId)
            .Take(2)
            .ToArray();
        if (revalidationMatches.Length != 1) return null;

        var revalidation = revalidationMatches[0];
        PerformanceEvidenceSnapshot baseline;
        PerformanceEvidenceSnapshot candidate;
        PerformanceConfigurationSnapshot baselineConfiguration;
        PerformanceConfigurationSnapshot candidateConfiguration;
        try
        {
            var normalized = revalidation.Rehydrate();
            baseline = PerformanceEvidenceSnapshot.Rehydrate(normalized.Baseline);
            candidate = PerformanceEvidenceSnapshot.Rehydrate(normalized.Candidate);
            baselineConfiguration = baseline.Configuration?.Rehydrate()
                ?? throw new InvalidDataException("Persisted promotion revalidation baseline has no configuration.");
            candidateConfiguration = candidate.Configuration?.Rehydrate()
                ?? throw new InvalidDataException("Persisted promotion revalidation candidate has no configuration.");
        }
        catch (Exception exception) when (exception is InvalidDataException or ArgumentException)
        {
            return null;
        }

        if (baseline.Quality != PerformanceEvidenceQuality.Measured
            || candidate.Quality != PerformanceEvidenceQuality.Measured
            || !baselineConfiguration.Environment.IsStructurallyEquivalentTo(candidateConfiguration.Environment)
            || !candidateConfiguration.Environment.IsStructurallyCompatible(currentEnvironment)
            || !ProfileMatchesConfiguration(challenger, candidateConfiguration)
            || !ProfileMatchesConfiguration(promoted, candidateConfiguration))
            return null;

        if (!string.Equals(challenger.EnvironmentFingerprint, candidateConfiguration.Environment.Id, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(promoted.EnvironmentFingerprint, candidateConfiguration.Environment.Id, StringComparison.OrdinalIgnoreCase)
            || !Nullable.Equals(promoted.AverageFps, candidate.AverageFps)
            || !Nullable.Equals(promoted.OnePercentLow, ProfileChallengeEvaluator.OnePercentLow(candidate))
            || !Nullable.Equals(promoted.FrameTimeMs, candidate.AverageFrameTimeMs)
            || !Nullable.Equals(promoted.LatencyMs, candidate.AverageLatencyMs))
            return null;

        var challengerProjection = await _customResolver.ResolveCurrentAsync(
            receipt.ChallengerProfileId,
            currentEnvironment,
            currentInstance,
            capturedSettings,
            cancellationToken).ConfigureAwait(false);
        if (challengerProjection is null
            || challengerProjection.SpecializedProfile.Id != challenger.Id
            || challengerProjection.SpecializedProfile.SourceComparisonId != challenger.SourceComparisonId
            || challengerProjection.Identity.LegacyGameKind != promoted.Game
            || string.IsNullOrWhiteSpace(challengerProjection.Identity.GameId)
            || string.IsNullOrWhiteSpace(challengerProjection.AdapterId))
            return null;

        return new UniversalPersistedPromotedProfileProjection
        {
            PromotedProfile = promoted,
            PromotionReceipt = receipt,
            ChallengerProfile = challengerProjection,
            RevalidationRound = revalidation,
            UniversalCandidate = challengerProjection.UniversalCandidate,
            Identity = challengerProjection.Identity,
            AdapterId = challengerProjection.AdapterId
        };
    }

    private static UniversalPersistedPromotionReceipt? TryParsePromotionReceipt(HistoryEvent item)
    {
        if (item.Kind != HistoryEventKind.Profile || string.IsNullOrWhiteSpace(item.DetailsJson))
            return null;

        try
        {
            using var document = JsonDocument.Parse(item.DetailsJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !TryReadGuid(root, "challengerProfileId", out var challengerProfileId)
                || !TryReadGuid(root, "previousWinnerId", out var previousWinnerId)
                || !TryReadGuid(root, "promotedProfileId", out var promotedProfileId)
                || !TryReadGuid(root, "revalidationComparisonId", out var revalidationComparisonId)
                || !root.TryGetProperty("targetKind", out var targetKindElement)
                || targetKindElement.ValueKind != JsonValueKind.String
                || !Enum.TryParse<ProfileKind>(targetKindElement.GetString(), ignoreCase: false, out var targetKind)
                || !ProfileChallengeEvaluator.IsWinnerRole(targetKind))
                return null;

            return new UniversalPersistedPromotionReceipt
            {
                Event = item,
                ChallengerProfileId = challengerProfileId,
                PreviousWinnerId = previousWinnerId,
                PromotedProfileId = promotedProfileId,
                RevalidationComparisonId = revalidationComparisonId,
                TargetKind = targetKind
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TryReadGuid(JsonElement root, string propertyName, out Guid value)
    {
        value = Guid.Empty;
        return root.TryGetProperty(propertyName, out var element)
               && element.ValueKind == JsonValueKind.String
               && Guid.TryParseExact(element.GetString(), "D", out value);
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
