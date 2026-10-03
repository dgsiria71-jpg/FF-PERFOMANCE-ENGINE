using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

/// <summary>
/// Read-only application policy that re-proves the universal provenance of a
/// persisted specialized Custom Validated profile against the candidate space
/// that the current BlueStacks installation can still expose. It never creates,
/// validates, promotes, mutates or persists a profile and never infers an
/// AutoTuner mode that was not stored by the specialized authority.
/// </summary>
public sealed class UniversalValidatedProfileProvenanceService
{
    private readonly ProfileService _profiles;
    private readonly HistoryService _history;
    private readonly BlueStacksUniversalTuningCandidateBridge _candidateBridge;

    public UniversalValidatedProfileProvenanceService(
        ProfileService profiles,
        HistoryService history,
        BlueStacksUniversalTuningCandidateBridge candidateBridge)
    {
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _history = history ?? throw new ArgumentNullException(nameof(history));
        _candidateBridge = candidateBridge ?? throw new ArgumentNullException(nameof(candidateBridge));
    }

    public async Task<UniversalValidatedProfileProjection?> ResolveCurrentAsync(
        Guid profileId,
        EnvironmentSnapshot currentEnvironment,
        BlueStacksInstance currentInstance,
        IReadOnlyDictionary<string, string> capturedSettings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currentEnvironment);
        ArgumentNullException.ThrowIfNull(currentInstance);
        ArgumentNullException.ThrowIfNull(capturedSettings);
        cancellationToken.ThrowIfCancellationRequested();

        var profileMatches = (await _profiles.LoadAsync(cancellationToken).ConfigureAwait(false))
            .Where(profile => profile.Id == profileId)
            .Take(2)
            .ToArray();
        if (profileMatches.Length != 1) return null;

        var profile = profileMatches[0];
        if (profile.Kind != ProfileKind.Custom
            || profile.Evidence != EvidenceLevel.Validated
            || profile.SourceComparisonId is null
            || profile.Game is not (GameKind.FreeFire or GameKind.FreeFireMax)
            || string.IsNullOrWhiteSpace(profile.InstanceName)
            || !string.Equals(profile.InstanceName, currentInstance.Name, StringComparison.OrdinalIgnoreCase))
            return null;

        var recordMatches = (await _history.LoadPerformanceComparisonsAsync(cancellationToken).ConfigureAwait(false))
            .Where(record => record.Id == profile.SourceComparisonId.Value)
            .Take(2)
            .ToArray();
        if (recordMatches.Length != 1) return null;

        var record = recordMatches[0];
        PerformanceConfigurationSnapshot sourceConfiguration;
        try
        {
            sourceConfiguration = record.Candidate.Configuration?.Rehydrate()
                ?? throw new InvalidDataException("Validated profile source has no candidate configuration.");
        }
        catch (Exception exception) when (exception is InvalidDataException or ArgumentException)
        {
            return null;
        }

        if (!sourceConfiguration.Environment.IsStructurallyCompatible(currentEnvironment))
            return null;

        var currentEnvironmentMatches = currentEnvironment.Instances
            .Where(instance => string.Equals(instance.Name, currentInstance.Name, StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToArray();
        if (currentEnvironmentMatches.Length != 1) return null;

        var projections = new List<UniversalValidatedProfileProjection>();
        foreach (var mode in Enum.GetValues<AutoTunerMode>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            BlueStacksUniversalTuningCandidateSpace candidateSpace;
            try
            {
                candidateSpace = _candidateBridge.Build(
                    currentEnvironment,
                    currentInstance,
                    profile.Game,
                    mode,
                    capturedSettings);
            }
            catch (Exception exception) when (exception is InvalidDataException or ArgumentException or InvalidOperationException)
            {
                return null;
            }

            var sourceValidation = BlueStacksUniversalValidatedPerformanceBridge.TryProject(
                record,
                candidateSpace);
            if (sourceValidation is null) continue;

            var projection = BlueStacksUniversalValidatedProfileBridge.TryProject(
                profile,
                sourceValidation,
                candidateSpace);
            if (projection is not null) projections.Add(projection);
        }

        if (projections.Count == 0) return null;

        var first = projections[0];
        return projections.Skip(1).All(other => EquivalentProvenance(first, other))
            ? first
            : null;
    }

    private static bool EquivalentProvenance(
        UniversalValidatedProfileProjection left,
        UniversalValidatedProfileProjection right)
        => left.SpecializedProfile.Id == right.SpecializedProfile.Id
           && left.SourceValidation.SpecializedRecord.Id == right.SourceValidation.SpecializedRecord.Id
           && string.Equals(left.Identity.GameId, right.Identity.GameId, StringComparison.OrdinalIgnoreCase)
           && left.Identity.LegacyGameKind == right.Identity.LegacyGameKind
           && string.Equals(left.AdapterId, right.AdapterId, StringComparison.OrdinalIgnoreCase)
           && CandidatesEquivalent(left.UniversalCandidate, right.UniversalCandidate);

    private static bool CandidatesEquivalent(
        UniversalTuningCandidate left,
        UniversalTuningCandidate right)
        => left.Values.Count == right.Values.Count
           && left.Values.All(pair =>
               right.Values.TryGetValue(pair.Key, out var value)
               && string.Equals(pair.Value, value, StringComparison.OrdinalIgnoreCase));
}
