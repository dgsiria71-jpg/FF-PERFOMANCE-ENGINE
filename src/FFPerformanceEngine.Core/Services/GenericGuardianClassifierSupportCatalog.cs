using System.Collections.ObjectModel;

namespace FFPerformanceEngine.Core.Services;

public enum GuardianAnomalySupportLevel
{
    Fallback,
    EvidenceBacked,
    UnavailableEvidence
}

public sealed record GuardianAnomalySupportDescriptor
{
    public required GuardianAnomalyKind Family { get; init; }
    public required GuardianAnomalySupportLevel Support { get; init; }
    public required string Reason { get; init; }

    public bool CanClassify => Support == GuardianAnomalySupportLevel.EvidenceBacked;
}

/// <summary>
/// Read-only capability statement for the approved Guardian anomaly taxonomy.
/// This catalog does not inspect telemetry, classify observations or grant action authority.
/// </summary>
public static class GenericGuardianClassifierSupportCatalog
{
    private static readonly IReadOnlyList<GuardianAnomalySupportDescriptor> Items =
        new ReadOnlyCollection<GuardianAnomalySupportDescriptor>(
        [
            Fallback(
                GuardianAnomalyKind.Unknown,
                "Fallback when no causal anomaly is proven; Unknown is neither a proven healthy state nor proven causal diagnosis."),
            EvidenceBacked(
                GuardianAnomalyKind.CpuContention,
                "Evidence-backed through the existing typed universal bottleneck analyzer CPU classification."),
            EvidenceBacked(
                GuardianAnomalyKind.GpuSaturation,
                "Evidence-backed through the existing typed universal bottleneck analyzer GPU classification."),
            EvidenceBacked(
                GuardianAnomalyKind.MemoryPressure,
                "Evidence-backed through the existing typed universal bottleneck analyzer memory classification."),
            EvidenceBacked(
                GuardianAnomalyKind.VramPressure,
                "Evidence-backed through the existing typed universal bottleneck analyzer VRAM classification."),
            EvidenceBacked(
                GuardianAnomalyKind.FrameTimeInstability,
                "Evidence-backed through the existing typed universal bottleneck analyzer frame-pacing classification."),
            Unavailable(
                GuardianAnomalyKind.BackgroundLoad,
                "Dedicated causal evidence for background-load attribution is not currently available."),
            EvidenceBacked(
                GuardianAnomalyKind.ThermalThrottling,
                "Evidence-backed through the existing typed universal bottleneck analyzer thermal classification."),
            EvidenceBacked(
                GuardianAnomalyKind.NetworkInstability,
                "Evidence-backed through the existing typed universal bottleneck analyzer network classification."),
            Unavailable(
                GuardianAnomalyKind.RendererEngineStall,
                "Dedicated causal evidence for renderer/engine-stall attribution is not currently available."),
            Unavailable(
                GuardianAnomalyKind.SchedulerImbalance,
                "Dedicated causal evidence for scheduler-imbalance attribution is not currently available."),
            Unavailable(
                GuardianAnomalyKind.InputFrameLatencySpike,
                "Dedicated causal evidence for input/frame-latency-spike attribution is not currently available.")
        ]);

    public static IReadOnlyList<GuardianAnomalySupportDescriptor> All => Items;

    public static GuardianAnomalySupportDescriptor For(GuardianAnomalyKind family)
        => Items.Single(item => item.Family == family);

    private static GuardianAnomalySupportDescriptor EvidenceBacked(
        GuardianAnomalyKind family,
        string reason)
        => new()
        {
            Family = family,
            Support = GuardianAnomalySupportLevel.EvidenceBacked,
            Reason = reason
        };

    private static GuardianAnomalySupportDescriptor Unavailable(
        GuardianAnomalyKind family,
        string reason)
        => new()
        {
            Family = family,
            Support = GuardianAnomalySupportLevel.UnavailableEvidence,
            Reason = reason
        };

    private static GuardianAnomalySupportDescriptor Fallback(
        GuardianAnomalyKind family,
        string reason)
        => new()
        {
            Family = family,
            Support = GuardianAnomalySupportLevel.Fallback,
            Reason = reason
        };
}
