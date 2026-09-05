namespace FFPerformanceEngine.Core.Models;

public sealed record PerformanceProfile
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = "Recommended";
    public ProfileKind Kind { get; init; } = ProfileKind.Recommended;
    public GameKind Game { get; init; } = GameKind.FreeFire;
    public string InstanceName { get; init; } = string.Empty;
    public int CpuCores { get; init; }
    public int RamMb { get; init; }
    public string Renderer { get; init; } = "Auto";
    public int FpsTarget { get; init; } = 90;
    public string Resolution { get; init; } = "1920x1080";
    public int Dpi { get; init; } = 240;
    public GuardianMode GuardianMode { get; init; } = GuardianMode.Adaptive;
    public EvidenceLevel Evidence { get; init; } = EvidenceLevel.Unknown;
    public double Confidence { get; init; }
    public double? AverageFps { get; init; }
    public double? OnePercentLow { get; init; }
    public double? FrameTimeMs { get; init; }
    public double? LatencyMs { get; init; }
    public double? StutterPercent { get; init; }
    public double? GpuTemperatureC { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
