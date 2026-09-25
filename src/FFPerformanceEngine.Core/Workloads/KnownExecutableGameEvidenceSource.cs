namespace FFPerformanceEngine.Core.Workloads;

public sealed record KnownExecutableObservation
{
    public string? ExecutablePath { get; init; }
    public string? RegistrationName { get; init; }
    public DateTimeOffset ObservedAtUtc { get; init; }
}

public interface IKnownExecutableObservationProvider
{
    Task<IReadOnlyList<KnownExecutableObservation>> ObserveAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Converts Windows App Paths registrations into static executable evidence.
/// A registered path can bind to an already-proven game install root, but this
/// source never creates or hints a durable GameId and never claims the process
/// is currently running.
/// </summary>
public sealed class KnownExecutableGameEvidenceSource : IGameEvidenceSource
{
    private readonly IKnownExecutableObservationProvider _provider;

    public KnownExecutableGameEvidenceSource(IKnownExecutableObservationProvider provider)
        => _provider = provider ?? throw new ArgumentNullException(nameof(provider));

    public string SourceId => "windows-app-paths";
    public int Priority => 30;

    public async Task<IReadOnlyList<GameEvidenceObservation>> ObserveAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var snapshots = await _provider.ObserveAsync(cancellationToken).ConfigureAwait(false)
            ?? Array.Empty<KnownExecutableObservation>();

        cancellationToken.ThrowIfCancellationRequested();

        var normalized = snapshots
            .Select((item, index) => new NormalizedKnownExecutable(
                item,
                index,
                NormalizeFullPath(item?.ExecutablePath)))
            .Where(item => item.Observation is not null && item.ExecutablePath is not null)
            .ToArray();

        return normalized
            .GroupBy(item => item.ExecutablePath!, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderBy(item => item.Observation!.ObservedAtUtc)
                .ThenBy(item => item.InputIndex)
                .First())
            .OrderBy(item => item.ExecutablePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ExecutablePath, StringComparer.Ordinal)
            .Select(item =>
            {
                var path = item.ExecutablePath!;
                var observation = item.Observation!;
                return new GameEvidenceObservation
                {
                    ObservationId = $"path:{path.ToLowerInvariant()}",
                    Kind = GameEvidenceKind.KnownExecutable,
                    Confidence = 0.92,
                    ObservedAtUtc = observation.ObservedAtUtc,
                    GameIdHint = null,
                    ExecutablePath = path,
                    ProcessId = null,
                    DisplayName = NormalizeOptional(observation.RegistrationName),
                    EvidenceText = $"Windows App Paths registered executable at {path}"
                };
            })
            .ToArray();
    }

    private static string? NormalizeFullPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Path.IsPathFullyQualified(value))
            return null;

        try
        {
            return Path.GetFullPath(value.Trim());
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private sealed record NormalizedKnownExecutable(
        KnownExecutableObservation? Observation,
        int InputIndex,
        string? ExecutablePath);
}
