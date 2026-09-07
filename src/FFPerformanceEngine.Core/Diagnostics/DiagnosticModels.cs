using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Diagnostics;

public enum HardwareComponentKind
{
    Cpu,
    Gpu,
    Memory,
    Storage,
    Display,
    Network,
    Other
}

public sealed record HardwareComponentSnapshot
{
    public string StableId { get; init; } = string.Empty;
    public HardwareComponentKind Kind { get; init; } = HardwareComponentKind.Other;
    public string Name { get; init; } = string.Empty;
    public string Vendor { get; init; } = string.Empty;
    public string Architecture { get; init; } = string.Empty;
    public long? CapacityBytes { get; init; }
    public int? LogicalProcessors { get; init; }
    public string Source { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();
}

public sealed record HardwareDiscoveryResult
{
    public IReadOnlyList<HardwareComponentSnapshot> Components { get; init; } = Array.Empty<HardwareComponentSnapshot>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

public enum CapabilityDomain
{
    Cpu,
    Memory,
    StorageIo,
    Graphics,
    Display,
    Network,
    Process,
    Service,
    ScheduledTask,
    Power,
    Input,
    System
}

public enum CapabilityPersistenceScope
{
    SessionOnly,
    PersistentAllowed,
    PersistentOnly
}

public enum CapabilityRiskLevel
{
    Safe,
    Low,
    Moderate,
    High,
    Critical
}

public enum CapabilityAvailability
{
    Unknown,
    Available,
    Unavailable,
    Blocked
}

public enum CapabilityValueKind
{
    Opaque,
    Boolean,
    Enumeration,
    Integer,
    Number
}

public sealed record CapabilityValueSchema
{
    public CapabilityValueKind Kind { get; init; } = CapabilityValueKind.Opaque;
    public IReadOnlyList<string> AllowedValues { get; init; } = Array.Empty<string>();
    public double? Minimum { get; init; }
    public double? Maximum { get; init; }
    public double? Step { get; init; }
    public string Unit { get; init; } = string.Empty;
}

public sealed record CapabilityPerformanceModel
{
    public string ExpectedCpuImpact { get; init; } = "Unknown";
    public string ExpectedGpuImpact { get; init; } = "Unknown";
    public string ExpectedMemoryImpact { get; init; } = "Unknown";
    public string ExpectedIoImpact { get; init; } = "Unknown";
    public string ExpectedLatencyImpact { get; init; } = "Unknown";
    public string ExpectedPowerImpact { get; init; } = "Unknown";
    public double Confidence { get; init; }
}

public sealed record CapabilityExecutionMetadata
{
    public string AdapterId { get; init; } = string.Empty;
    public bool SupportsRead { get; init; }
    public bool SupportsSnapshot { get; init; }
    public bool SupportsApply { get; init; }
    public bool SupportsVerification { get; init; }
    public bool SupportsRollback { get; init; }
}

public sealed record CapabilityEvidenceSummary
{
    public int ControlledSamples { get; init; }
    public int PassiveSamples { get; init; }
    public double Confidence { get; init; }
    public DateTimeOffset? LastMeasuredAt { get; init; }
}

public sealed class WindowsPerformanceCapability
{
    public string CapabilityId { get; set; } = string.Empty;
    public CapabilityDomain Domain { get; set; } = CapabilityDomain.System;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string? CurrentValue { get; set; }
    public IReadOnlyList<string> AvailableValues { get; set; } = Array.Empty<string>();
    public string? DefaultValue { get; set; }
    public string? RecommendedValue { get; set; }
    public CapabilityValueSchema ValueSchema { get; set; } = new();
    public CapabilityAvailability Availability { get; set; } = CapabilityAvailability.Unknown;

    public IReadOnlyList<string> WindowsRequirements { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> HardwareRequirements { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> DriverRequirements { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> WorkloadRequirements { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Dependencies { get; set; } = Array.Empty<string>();

    public CapabilityPersistenceScope PersistenceScope { get; set; } = CapabilityPersistenceScope.SessionOnly;
    public ActionSafety Safety { get; set; } = ActionSafety.Experimental;
    public bool RebootRequired { get; set; }
    public CapabilityPerformanceModel PerformanceModel { get; set; } = new();

    public CapabilityRiskLevel RiskLevel { get; set; } = CapabilityRiskLevel.Safe;
    public IReadOnlyList<string> Conflicts { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> ForbiddenCombinations { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> EmergencyRollbackConditions { get; set; } = Array.Empty<string>();

    public CapabilityExecutionMetadata Execution { get; set; } = new();
    public CapabilityEvidenceSummary Evidence { get; set; } = new();

    internal WindowsPerformanceCapability CloneDescriptor()
        => new()
        {
            CapabilityId = CapabilityId,
            Domain = Domain,
            Name = Name,
            Description = Description,
            CurrentValue = CurrentValue,
            AvailableValues = AvailableValues.ToArray(),
            DefaultValue = DefaultValue,
            RecommendedValue = RecommendedValue,
            ValueSchema = ValueSchema with { AllowedValues = ValueSchema.AllowedValues.ToArray() },
            Availability = Availability,
            WindowsRequirements = WindowsRequirements.ToArray(),
            HardwareRequirements = HardwareRequirements.ToArray(),
            DriverRequirements = DriverRequirements.ToArray(),
            WorkloadRequirements = WorkloadRequirements.ToArray(),
            Dependencies = Dependencies.ToArray(),
            PersistenceScope = PersistenceScope,
            Safety = Safety,
            RebootRequired = RebootRequired,
            PerformanceModel = PerformanceModel,
            RiskLevel = RiskLevel,
            Conflicts = Conflicts.ToArray(),
            ForbiddenCombinations = ForbiddenCombinations.ToArray(),
            EmergencyRollbackConditions = EmergencyRollbackConditions.ToArray(),
            Execution = Execution,
            Evidence = Evidence
        };
}

public sealed record CapabilityGraphIssue
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<string> CapabilityIds { get; init; } = Array.Empty<string>();
}

public sealed record CapabilityPlanResolution
{
    public bool IsValid { get; init; }
    public IReadOnlyList<WindowsPerformanceCapability> OrderedCapabilities { get; init; } = Array.Empty<WindowsPerformanceCapability>();
    public IReadOnlyList<CapabilityGraphIssue> Issues { get; init; } = Array.Empty<CapabilityGraphIssue>();
}

public sealed record MachineEnvironmentFingerprintV2
{
    public int SchemaVersion { get; init; } = 2;
    public string Id { get; init; } = string.Empty;
    public string MachineName { get; init; } = string.Empty;
    public string WindowsDescription { get; init; } = string.Empty;
    public int LogicalProcessors { get; init; }
    public long? MemoryTotalMb { get; init; }
    public bool Is64BitOs { get; init; }
    public IReadOnlyList<string> HardwareSignatures { get; init; } = Array.Empty<string>();

    public bool IsCompatibleWithLegacy(Services.PerformanceEnvironmentFingerprint legacy)
    {
        ArgumentNullException.ThrowIfNull(legacy);
        var old = legacy.Rehydrate();
        return string.Equals(MachineName, old.MachineName, StringComparison.OrdinalIgnoreCase)
               && string.Equals(WindowsDescription, old.WindowsDescription, StringComparison.OrdinalIgnoreCase)
               && LogicalProcessors == old.LogicalProcessors
               && MemoryMatches(MemoryTotalMb, old.MemoryTotalMb)
               && Is64BitOs == old.Is64BitOs;
    }

    internal static bool MemoryMatches(long? left, long? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return Math.Abs(left.Value - right.Value) <= 256;
    }
}

public sealed record MachineContext
{
    public int SchemaVersion { get; init; } = 2;
    public DateTimeOffset CapturedAt { get; init; } = DateTimeOffset.UtcNow;
    public required EnvironmentSnapshot Environment { get; init; }
    public required HardwareDiscoveryResult Hardware { get; init; }
    public IReadOnlyList<WindowsPerformanceCapability> Capabilities { get; init; } = Array.Empty<WindowsPerformanceCapability>();
    public required MachineEnvironmentFingerprintV2 Fingerprint { get; init; }
}

public enum BottleneckKind
{
    Unknown,
    None,
    Cpu,
    Gpu,
    Memory,
    Vram,
    StorageIo,
    Thermal,
    Power,
    Network,
    FramePacing
}

public sealed class BottleneckAnalysisContext
{
    public double? TargetFps { get; set; }
    public double? TargetFrameTimeMs { get; set; }
    public double? CriticalThreadCpuPercent { get; set; }
    public double? MemoryPressurePercent { get; set; }
    public double? VramPressurePercent { get; set; }
    public double? StorageBusyPercent { get; set; }
    public double? ThermalHeadroomC { get; set; }
    public bool? IsThermallyThrottled { get; set; }
    public bool? IsPowerLimited { get; set; }
}

public sealed record BottleneckCandidate
{
    public BottleneckKind Kind { get; init; }
    public double Confidence { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public sealed record BottleneckAnalysisResult
{
    public BottleneckKind Primary { get; init; } = BottleneckKind.Unknown;
    public double Confidence { get; init; }
    public IReadOnlyList<BottleneckCandidate> Candidates { get; init; } = Array.Empty<BottleneckCandidate>();
    public IReadOnlyList<string> Signals { get; init; } = Array.Empty<string>();
}

public sealed record UniversalDiagnosticSnapshot
{
    public DateTimeOffset CapturedAt { get; init; } = DateTimeOffset.UtcNow;
    public required MachineContext Machine { get; init; }
    public required BottleneckAnalysisResult Bottleneck { get; init; }
}
