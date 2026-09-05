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
    {
        var binding = status?.Binding;
        if (binding is null || binding.ProcessId <= 0 || string.IsNullOrWhiteSpace(binding.InstanceName))
            return new();

        if (status?.Instance is { } instance
            && !string.Equals(instance.Name, binding.InstanceName, StringComparison.OrdinalIgnoreCase))
            return new();

        return new PerformanceCaptureTarget
        {
            ProcessId = binding.ProcessId,
            InstanceName = binding.InstanceName
        };
    }
}
