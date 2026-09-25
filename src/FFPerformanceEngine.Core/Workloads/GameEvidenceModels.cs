namespace FFPerformanceEngine.Core.Workloads;

public enum GameEvidenceKind
{
    RunningProcess,
    KnownExecutable,
    InstalledApplication,
    IndependentLauncher,
    EmulatorRuntime,
    Other
}

public sealed record GameEvidenceObservation
{
    public string ObservationId { get; init; } = string.Empty;
    public GameEvidenceKind Kind { get; init; }
    public double Confidence { get; init; }
    public DateTimeOffset ObservedAtUtc { get; init; }
    public string? GameIdHint { get; init; }
    public string? ExecutablePath { get; init; }
    public int? ProcessId { get; init; }
    public string? DisplayName { get; init; }
    public string EvidenceText { get; init; } = string.Empty;
}

public interface IGameEvidenceSource
{
    string SourceId { get; }
    int Priority { get; }

    Task<IReadOnlyList<GameEvidenceObservation>> ObserveAsync(
        CancellationToken cancellationToken = default);
}

public sealed record GameEvidenceSourceObservation
{
    public string SourceId { get; init; } = string.Empty;
    public int Priority { get; init; }
    public required GameEvidenceObservation Observation { get; init; }
}

public sealed record GameEvidenceCatalogResult
{
    public IReadOnlyList<GameEvidenceSourceObservation> Observations { get; init; }
        = Array.Empty<GameEvidenceSourceObservation>();
    public IReadOnlyList<GameDiscoveryWarning> Warnings { get; init; }
        = Array.Empty<GameDiscoveryWarning>();
}

public enum GameEvidenceBindingReason
{
    ExactGameIdHint,
    UniqueInstallPathContainment
}

public enum GameEvidenceUnboundReason
{
    NoMatchingIdentity,
    AmbiguousInstallPath,
    InvalidExecutablePath,
    UnsupportedEvidence
}

public sealed record BoundGameEvidence
{
    public string GameId { get; init; } = string.Empty;
    public GameEvidenceBindingReason BindingReason { get; init; }
    public string SourceId { get; init; } = string.Empty;
    public int Priority { get; init; }
    public required GameEvidenceObservation Observation { get; init; }
}

public sealed record UnboundGameEvidence
{
    public GameEvidenceUnboundReason UnboundReason { get; init; }
    public string SourceId { get; init; } = string.Empty;
    public int Priority { get; init; }
    public required GameEvidenceObservation Observation { get; init; }
}

public sealed record GameEvidenceBindingResult
{
    public IReadOnlyList<BoundGameEvidence> BoundEvidence { get; init; }
        = Array.Empty<BoundGameEvidence>();
    public IReadOnlyList<UnboundGameEvidence> UnboundEvidence { get; init; }
        = Array.Empty<UnboundGameEvidence>();
}
