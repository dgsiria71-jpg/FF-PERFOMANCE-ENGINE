using FFPerformanceEngine.Core.Diagnostics;

namespace FFPerformanceEngine.Core.SystemOptimization;

public enum PersistentPcOptimizationPresentationStatus
{
    Ready,
    Skipped
}

public sealed record PersistentPcOptimizationPresentationEntry
{
    public string CapabilityId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public PersistentPcOptimizationPresentationStatus Status { get; init; }
    public PersistentPcOptimizationDisposition Disposition { get; init; }
    public string? CurrentValue { get; init; }
    public string? TargetValue { get; init; }
    public string ChangeText { get; init; } = string.Empty;
    public CapabilityRecommendationSource Source { get; init; }
    public double Confidence { get; init; }
    public CapabilityRiskLevel RiskLevel { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public sealed record PersistentPcOptimizationPresentation
{
    public Guid PlanId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string MachineFingerprintId { get; init; } = string.Empty;
    public int TotalCount { get; init; }
    public int ReadyCount { get; init; }
    public int SkippedCount { get; init; }
    public bool CanApply { get; init; }
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<PersistentPcOptimizationPresentationEntry> Entries { get; init; } = Array.Empty<PersistentPcOptimizationPresentationEntry>();

    /// <summary>
    /// Converts an already-authoritative planner preview into detached UI data.
    /// This method intentionally does not re-evaluate capabilities or policy.
    /// </summary>
    public static PersistentPcOptimizationPresentation Create(PersistentPcOptimizationPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);

        var entries = preview.Entries
            .Select(entry => new PersistentPcOptimizationPresentationEntry
            {
                CapabilityId = entry.CapabilityId,
                Name = entry.Name,
                Status = entry.IsReady
                    ? PersistentPcOptimizationPresentationStatus.Ready
                    : PersistentPcOptimizationPresentationStatus.Skipped,
                Disposition = entry.Disposition,
                CurrentValue = entry.ExpectedCurrentValue,
                TargetValue = entry.TargetValue,
                ChangeText = $"{Display(entry.ExpectedCurrentValue)} → {Display(entry.TargetValue)}",
                Source = entry.RecommendationSource,
                Confidence = entry.RecommendationConfidence,
                RiskLevel = entry.RiskLevel,
                Reason = entry.Reason
            })
            .ToArray();

        var readyCount = entries.Count(entry => entry.Status == PersistentPcOptimizationPresentationStatus.Ready);
        var skippedCount = entries.Length - readyCount;

        return new PersistentPcOptimizationPresentation
        {
            PlanId = preview.PlanId,
            CreatedAt = preview.CreatedAt,
            MachineFingerprintId = preview.MachineFingerprintId,
            TotalCount = entries.Length,
            ReadyCount = readyCount,
            SkippedCount = skippedCount,
            CanApply = preview.CanApply,
            Summary = $"{readyCount} ready · {skippedCount} skipped",
            Entries = entries
        };
    }

    private static string Display(string? value)
        => string.IsNullOrWhiteSpace(value) ? "—" : value;
}
