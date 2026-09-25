namespace FFPerformanceEngine.Core.Workloads;

/// <summary>
/// Executes non-authoritative evidence sources deterministically. Evidence is
/// normalized, deduplicated and provenance-wrapped, but this service never creates
/// or mutates a GameIdentity.
/// </summary>
public sealed class GameEvidenceCatalogService
{
    private readonly IReadOnlyList<IGameEvidenceSource> _sources;

    public GameEvidenceCatalogService(IEnumerable<IGameEvidenceSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        _sources = sources
            .Where(source => source is not null)
            .OrderByDescending(source => source.Priority)
            .ThenBy(source => NormalizeSourceId(source.SourceId), StringComparer.Ordinal)
            .ToArray();
    }

    public async Task<GameEvidenceCatalogResult> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var accepted = new List<AcceptedObservation>();
        var warnings = new List<GameDiscoveryWarning>();
        var inputOrder = 0;

        foreach (var source in _sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sourceId = NormalizeSourceId(source.SourceId);
            if (sourceId.Length == 0)
            {
                warnings.Add(new GameDiscoveryWarning
                {
                    SourceId = "unknown-source",
                    Message = "An evidence source has no stable SourceId and was skipped."
                });
                continue;
            }

            IReadOnlyList<GameEvidenceObservation> observations;
            try
            {
                observations = await source.ObserveAsync(cancellationToken).ConfigureAwait(false)
                    ?? Array.Empty<GameEvidenceObservation>();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                warnings.Add(new GameDiscoveryWarning
                {
                    SourceId = sourceId,
                    Message = ex.Message
                });
                continue;
            }

            foreach (var observation in observations)
            {
                var currentOrder = inputOrder++;
                if (observation is null) continue;

                var observationId = NormalizeObservationId(observation.ObservationId);
                if (observationId.Length == 0) continue;

                accepted.Add(new AcceptedObservation(
                    sourceId,
                    source.Priority,
                    currentOrder,
                    observation with
                    {
                        ObservationId = observationId,
                        Confidence = NormalizeConfidence(observation.Confidence),
                        GameIdHint = NormalizeOptional(observation.GameIdHint),
                        ExecutablePath = NormalizeOptional(observation.ExecutablePath),
                        DisplayName = NormalizeOptional(observation.DisplayName),
                        EvidenceText = observation.EvidenceText?.Trim() ?? string.Empty
                    }));
            }
        }

        var normalized = accepted
            .GroupBy(item => (item.SourceId, item.Observation.ObservationId))
            .Select(group => group
                .OrderByDescending(item => item.Observation.Confidence)
                .ThenBy(item => item.InputOrder)
                .First())
            .OrderByDescending(item => item.Priority)
            .ThenBy(item => item.SourceId, StringComparer.Ordinal)
            .ThenBy(item => item.Observation.ObservationId, StringComparer.Ordinal)
            .Select(item => new GameEvidenceSourceObservation
            {
                SourceId = item.SourceId,
                Priority = item.Priority,
                Observation = item.Observation
            })
            .ToArray();

        return new GameEvidenceCatalogResult
        {
            Observations = normalized,
            Warnings = warnings
                .OrderBy(warning => warning.SourceId, StringComparer.Ordinal)
                .ThenBy(warning => warning.Message, StringComparer.Ordinal)
                .ToArray()
        };
    }

    private static string NormalizeSourceId(string? value)
        => value?.Trim().ToLowerInvariant() ?? string.Empty;

    private static string NormalizeObservationId(string? value)
        => value?.Trim().ToLowerInvariant() ?? string.Empty;

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static double NormalizeConfidence(double value)
        => double.IsFinite(value) ? Math.Clamp(value, 0, 1) : 0;

    private sealed record AcceptedObservation(
        string SourceId,
        int Priority,
        int InputOrder,
        GameEvidenceObservation Observation);
}
