namespace FFPerformanceEngine.Core.Services;

public enum UniversalTuningDimensionScope
{
    System,
    Workload
}

public sealed record UniversalTuningDimension
{
    public string Id { get; init; } = string.Empty;
    public UniversalTuningDimensionScope Scope { get; init; }
    public string AuthorityId { get; init; } = string.Empty;
    public IReadOnlyList<string> CandidateValues { get; init; } = Array.Empty<string>();
}

public sealed record UniversalTuningCandidate
{
    public IReadOnlyDictionary<string, string> Values { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed record UniversalTuningSearchSpacePolicy
{
    public int MaxCandidates { get; init; } = 96;
}

/// <summary>
/// Builds a bounded deterministic tuning search space exclusively from dimensions
/// and values supplied by explicit authorities. It does not discover capabilities,
/// mutate the machine, score candidates or publish recommendation authority.
/// </summary>
public sealed class UniversalTuningSearchSpacePlanner
{
    private readonly UniversalTuningSearchSpacePolicy _policy;

    public UniversalTuningSearchSpacePlanner(UniversalTuningSearchSpacePolicy? policy = null)
    {
        _policy = policy ?? new UniversalTuningSearchSpacePolicy();
        if (_policy.MaxCandidates <= 0)
            throw new ArgumentOutOfRangeException(nameof(policy), "Universal tuning search-space budget must be greater than zero.");
    }

    public IReadOnlyList<UniversalTuningCandidate> Build(
        IReadOnlyList<UniversalTuningDimension> dimensions)
    {
        ArgumentNullException.ThrowIfNull(dimensions);
        if (dimensions.Count == 0) return Array.Empty<UniversalTuningCandidate>();

        var validated = new List<UniversalTuningDimension>(dimensions.Count);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var dimension in dimensions)
        {
            ArgumentNullException.ThrowIfNull(dimension);

            var id = dimension.Id?.Trim() ?? string.Empty;
            if (id.Length == 0)
                throw new ArgumentException("Every universal tuning dimension requires a non-empty id.", nameof(dimensions));
            if (!ids.Add(id))
                throw new ArgumentException($"Duplicate universal tuning dimension id '{id}'.", nameof(dimensions));

            var authorityId = dimension.AuthorityId?.Trim() ?? string.Empty;
            if (authorityId.Length == 0)
                throw new ArgumentException($"Universal tuning dimension '{id}' requires an explicit authority id.", nameof(dimensions));

            if (dimension.CandidateValues is null || dimension.CandidateValues.Count == 0)
                throw new ArgumentException($"Universal tuning dimension '{id}' requires at least one explicit candidate value.", nameof(dimensions));

            var values = new List<string>(dimension.CandidateValues.Count);
            var seenValues = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in dimension.CandidateValues)
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException($"Universal tuning dimension '{id}' contains a blank candidate value.", nameof(dimensions));
                if (!seenValues.Add(value))
                    throw new ArgumentException($"Universal tuning dimension '{id}' contains duplicate candidate value '{value}'.", nameof(dimensions));
                values.Add(value);
            }

            validated.Add(dimension with
            {
                Id = id,
                AuthorityId = authorityId,
                CandidateValues = values.ToArray()
            });
        }

        var ordered = validated
            .OrderBy(dimension => dimension.Id, StringComparer.OrdinalIgnoreCase)
            .ThenBy(dimension => dimension.Id, StringComparer.Ordinal)
            .ToArray();

        var results = new List<UniversalTuningCandidate>(Math.Min(_policy.MaxCandidates, 96));
        var current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Enumerate(ordered, dimensionIndex: 0, current, results);
        return results.ToArray();
    }

    private void Enumerate(
        IReadOnlyList<UniversalTuningDimension> dimensions,
        int dimensionIndex,
        IDictionary<string, string> current,
        ICollection<UniversalTuningCandidate> results)
    {
        if (results.Count >= _policy.MaxCandidates) return;
        if (dimensionIndex >= dimensions.Count)
        {
            results.Add(new UniversalTuningCandidate
            {
                Values = new Dictionary<string, string>(current, StringComparer.OrdinalIgnoreCase)
            });
            return;
        }

        var dimension = dimensions[dimensionIndex];
        foreach (var value in dimension.CandidateValues)
        {
            current[dimension.Id] = value;
            Enumerate(dimensions, dimensionIndex + 1, current, results);
            if (results.Count >= _policy.MaxCandidates) break;
        }

        current.Remove(dimension.Id);
    }
}
