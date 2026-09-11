using FFPerformanceEngine.Core.Services;

namespace FFPerformanceEngine.App;

/// <summary>
/// Presentation-only view of an already-proven universal profile provenance.
/// It never resolves identity, validates evidence, rebuilds candidate space or
/// grants profile/winner/persistence authority.
/// </summary>
public sealed record UniversalProfileProvenancePresentation
{
    public bool IsVisible { get; init; }
    public string GameId { get; init; } = string.Empty;
    public string AdapterId { get; init; } = string.Empty;
    public IReadOnlyList<string> CandidateLines { get; init; } = Array.Empty<string>();

    public static UniversalProfileProvenancePresentation FromProjection(
        UniversalValidatedProfileProjection? projection)
    {
        if (projection is null)
            return new UniversalProfileProvenancePresentation();

        var candidateLines = projection.UniversalCandidate.Values
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key} = {pair.Value}")
            .ToArray();

        return new UniversalProfileProvenancePresentation
        {
            IsVisible = true,
            GameId = projection.Identity.GameId,
            AdapterId = projection.AdapterId,
            CandidateLines = candidateLines
        };
    }
}
