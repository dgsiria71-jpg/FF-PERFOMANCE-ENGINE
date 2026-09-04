namespace FFPerformanceEngine.Core.Models;

public sealed record TuningCandidate
{
    public int CpuCores { get; init; }
    public int RamMb { get; init; }
    public string Renderer { get; init; } = "Auto";
    public int FpsTarget { get; init; }
    public string Resolution { get; init; } = "1920x1080";
}

public sealed record CandidateEvidence
{
    public required TuningCandidate Candidate { get; init; }
    public required TelemetrySample Sample { get; init; }
    public EvidenceLevel Evidence { get; init; } = EvidenceLevel.Unknown;
    public double Confidence { get; init; }
}

public sealed record TuningResult
{
    public AutoTunerMode Mode { get; init; }
    public GameKind Game { get; init; }
    public IReadOnlyList<PerformanceProfile> Winners { get; init; } = Array.Empty<PerformanceProfile>();
    public IReadOnlyList<CandidateEvidence> Evidence { get; init; } = Array.Empty<CandidateEvidence>();
    public string Summary { get; init; } = string.Empty;
}
