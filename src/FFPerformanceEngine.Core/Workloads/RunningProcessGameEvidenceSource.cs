namespace FFPerformanceEngine.Core.Workloads;

public sealed record RunningProcessObservation
{
    public int ProcessId { get; init; }
    public string? ExecutablePath { get; init; }
    public string? DisplayName { get; init; }
    public DateTimeOffset ObservedAtUtc { get; init; }
}

public interface IRunningProcessObservationProvider
{
    Task<IReadOnlyList<RunningProcessObservation>> ObserveAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Converts generic running-process snapshots into transient game evidence. This
/// source deliberately performs no game classification: PID and executable path
/// are runtime facts only and never become a stable GameId here.
/// </summary>
public sealed class RunningProcessGameEvidenceSource : IGameEvidenceSource
{
    private readonly IRunningProcessObservationProvider _provider;

    public RunningProcessGameEvidenceSource(IRunningProcessObservationProvider provider)
        => _provider = provider ?? throw new ArgumentNullException(nameof(provider));

    public string SourceId => "windows-running-process";
    public int Priority => 40;

    public async Task<IReadOnlyList<GameEvidenceObservation>> ObserveAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var snapshots = await _provider.ObserveAsync(cancellationToken).ConfigureAwait(false)
            ?? Array.Empty<RunningProcessObservation>();

        cancellationToken.ThrowIfCancellationRequested();

        return snapshots
            .Where(item => item is not null && item.ProcessId > 0)
            .Select(item => new NormalizedProcessObservation(
                item,
                NormalizeFullPath(item.ExecutablePath)))
            .Where(item => item.ExecutablePath is not null)
            .OrderBy(item => item.Observation.ProcessId)
            .ThenBy(item => item.ExecutablePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ExecutablePath, StringComparer.Ordinal)
            .Select(item => new GameEvidenceObservation
            {
                ObservationId = $"pid:{item.Observation.ProcessId}",
                Kind = GameEvidenceKind.RunningProcess,
                Confidence = 0.98,
                ObservedAtUtc = item.Observation.ObservedAtUtc,
                GameIdHint = null,
                ExecutablePath = item.ExecutablePath,
                ProcessId = item.Observation.ProcessId,
                DisplayName = NormalizeOptional(item.Observation.DisplayName),
                EvidenceText = $"Running process PID {item.Observation.ProcessId} at {item.ExecutablePath}"
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

    private sealed record NormalizedProcessObservation(
        RunningProcessObservation Observation,
        string? ExecutablePath);
}
