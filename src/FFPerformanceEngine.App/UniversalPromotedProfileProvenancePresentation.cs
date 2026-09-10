using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

namespace FFPerformanceEngine.App;

/// <summary>
/// Presentation-only view of an already-proven persisted promoted-winner
/// universal provenance. It copies the resolved winner/profile identity and
/// candidate values but never parses promotion receipts, reconstructs challenge
/// state, revalidates evidence or grants winner/persistence authority.
/// </summary>
public sealed record UniversalPromotedProfileProvenancePresentation
{
    public bool IsVisible { get; init; }
    public Guid ProfileId { get; init; }
    public string ProfileName { get; init; } = string.Empty;
    public ProfileKind? ProfileKind { get; init; }
    public string GameId { get; init; } = string.Empty;
    public string AdapterId { get; init; } = string.Empty;
    public IReadOnlyList<string> CandidateLines { get; init; } = Array.Empty<string>();

    public static UniversalPromotedProfileProvenancePresentation FromProjection(
        UniversalPersistedPromotedProfileProjection? projection)
    {
        if (projection is null)
            return new UniversalPromotedProfileProvenancePresentation();

        var candidateLines = projection.UniversalCandidate.Values
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key} = {pair.Value}")
            .ToArray();

        return new UniversalPromotedProfileProvenancePresentation
        {
            IsVisible = true,
            ProfileId = projection.PromotedProfile.Id,
            ProfileName = projection.PromotedProfile.Name,
            ProfileKind = projection.PromotedProfile.Kind,
            GameId = projection.Identity.GameId,
            AdapterId = projection.AdapterId,
            CandidateLines = candidateLines
        };
    }
}
