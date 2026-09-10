using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Workloads;

namespace FFPerformanceEngine.Core.Services;

public sealed record UniversalTuningEvidenceProjection
{
    public required CandidateEvidence SpecializedEvidence { get; init; }
    public required UniversalTuningCandidate UniversalCandidate { get; init; }
}

public sealed record UniversalTuningWinnerProjection
{
    public required PerformanceProfile SpecializedProfile { get; init; }
    public required CandidateEvidence SourceEvidence { get; init; }
    public required UniversalTuningCandidate UniversalCandidate { get; init; }
}

public sealed record UniversalTuningResultProjection
{
    public required GameIdentity Identity { get; init; }
    public string AdapterId { get; init; } = string.Empty;
    public required TuningResult SpecializedResult { get; init; }
    public IReadOnlyList<UniversalTuningEvidenceProjection> Evidence { get; init; }
        = Array.Empty<UniversalTuningEvidenceProjection>();
    public IReadOnlyList<UniversalTuningWinnerProjection> Winners { get; init; }
        = Array.Empty<UniversalTuningWinnerProjection>();
}

/// <summary>
/// Adds neutral workload/candidate correlation to an already-authoritative
/// BlueStacks/Free Fire tuning result. This bridge never measures, scores,
/// validates, selects or persists anything; it only correlates existing result
/// objects to the exact candidate bindings produced by the Slice 3 bridge.
/// </summary>
public static class BlueStacksUniversalTuningResultBridge
{
    public static UniversalTuningResultProjection Project(
        TuningResult result,
        BlueStacksUniversalTuningCandidateSpace candidateSpace)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(candidateSpace);

        var evidence = result.Evidence
            .Select(item =>
            {
                var binding = candidateSpace.Bindings.Single(binding =>
                    binding.SpecializedCandidate == item.Candidate);
                return new UniversalTuningEvidenceProjection
                {
                    SpecializedEvidence = item,
                    UniversalCandidate = binding.UniversalCandidate
                };
            })
            .ToArray();

        var winners = result.Winners
            .Select(profile =>
            {
                var sourceEvidence = result.Evidence.Single(item =>
                    ProfileMatchesCandidate(profile, item.Candidate));
                var binding = candidateSpace.Bindings.Single(binding =>
                    binding.SpecializedCandidate == sourceEvidence.Candidate);

                return new UniversalTuningWinnerProjection
                {
                    SpecializedProfile = profile,
                    SourceEvidence = sourceEvidence,
                    UniversalCandidate = binding.UniversalCandidate
                };
            })
            .ToArray();

        return new UniversalTuningResultProjection
        {
            Identity = candidateSpace.Identity,
            AdapterId = candidateSpace.AdapterId,
            SpecializedResult = result,
            Evidence = Array.AsReadOnly(evidence),
            Winners = Array.AsReadOnly(winners)
        };
    }

    private static bool ProfileMatchesCandidate(
        PerformanceProfile profile,
        TuningCandidate candidate)
        => profile.CpuCores == candidate.CpuCores
           && profile.RamMb == candidate.RamMb
           && profile.FpsTarget == candidate.FpsTarget
           && string.Equals(profile.Renderer, candidate.Renderer, StringComparison.OrdinalIgnoreCase)
           && string.Equals(profile.Resolution, candidate.Resolution, StringComparison.OrdinalIgnoreCase);
}
