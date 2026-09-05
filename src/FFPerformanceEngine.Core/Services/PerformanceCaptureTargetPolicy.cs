namespace FFPerformanceEngine.Core.Services;

public sealed record PerformanceCaptureTarget
{
    public int? ProcessId { get; init; }
    public string? InstanceName { get; init; }
    public bool CanCapture => ProcessId is > 0 && !string.IsNullOrWhiteSpace(InstanceName);
}

public static class PerformanceCaptureTargetPolicy
{
    public static PerformanceCaptureTarget FromGuardianStatus(GuardianLiveSessionStatus? status)
        => new();
}
