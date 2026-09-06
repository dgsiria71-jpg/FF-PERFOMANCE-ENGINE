namespace FFPerformanceEngine.Core.Models;

public sealed record EnvironmentSnapshot
{
    public string WindowsDescription { get; init; } = string.Empty;
    public string MachineName { get; init; } = Environment.MachineName;
    public int LogicalProcessors { get; init; } = Environment.ProcessorCount;
    public double? MemoryTotalGb { get; init; }
    public bool Is64BitOs { get; init; } = Environment.Is64BitOperatingSystem;
    public bool BlueStacksDetected { get; init; }
    public string? BlueStacksPath { get; init; }
    public IReadOnlyList<string> BlueStacksProcesses { get; init; } = Array.Empty<string>();
    public IReadOnlyList<BlueStacksInstance> Instances { get; init; } = Array.Empty<BlueStacksInstance>();
    public GameKind ActiveGame { get; init; } = GameKind.None;
}

public sealed record BlueStacksInstance
{
    public string Name { get; init; } = string.Empty;
    public string AndroidVersion { get; init; } = string.Empty;
    public int? CpuCores { get; init; }
    public int? RamMb { get; init; }
    public string? Renderer { get; init; }
    public int? Fps { get; init; }
    public string? Resolution { get; init; }
    public int? Dpi { get; init; }
    public int? AdbPort { get; init; }
    public bool? AdbEnabled { get; init; }
}
