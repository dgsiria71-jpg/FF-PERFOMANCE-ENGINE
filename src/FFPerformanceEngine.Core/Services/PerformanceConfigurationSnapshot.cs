using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed record PerformanceEnvironmentFingerprint
{
    public string Id { get; init; } = string.Empty;
    public string MachineName { get; init; } = string.Empty;
    public string WindowsDescription { get; init; } = string.Empty;
    public int LogicalProcessors { get; init; }
    public long? MemoryTotalMb { get; init; }
    public bool Is64BitOs { get; init; }
    public string InstanceName { get; init; } = string.Empty;
    public string AndroidVersion { get; init; } = string.Empty;
    public GameKind Game { get; init; }

    public static PerformanceEnvironmentFingerprint Capture(
        EnvironmentSnapshot environment,
        BlueStacksInstance instance,
        GameKind game)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(instance);
        if (game is not (GameKind.FreeFire or GameKind.FreeFireMax))
            throw new ArgumentOutOfRangeException(nameof(game), game, "Performance evidence must target Free Fire or Free Fire MAX.");
        if (string.IsNullOrWhiteSpace(instance.Name))
            throw new InvalidDataException("BlueStacks instance name is required for an environment fingerprint.");

        var fingerprint = new PerformanceEnvironmentFingerprint
        {
            MachineName = environment.MachineName?.Trim() ?? string.Empty,
            WindowsDescription = environment.WindowsDescription?.Trim() ?? string.Empty,
            LogicalProcessors = environment.LogicalProcessors,
            MemoryTotalMb = ToMemoryMb(environment.MemoryTotalGb),
            Is64BitOs = environment.Is64BitOs,
            InstanceName = instance.Name.Trim(),
            AndroidVersion = instance.AndroidVersion?.Trim() ?? string.Empty,
            Game = game
        };
        return fingerprint with { Id = ComputeId(fingerprint) };
    }

    public PerformanceEnvironmentFingerprint Rehydrate()
    {
        if (string.IsNullOrWhiteSpace(MachineName)
            || LogicalProcessors <= 0
            || string.IsNullOrWhiteSpace(InstanceName)
            || Game is not (GameKind.FreeFire or GameKind.FreeFireMax))
            throw new InvalidDataException("Stored performance environment fingerprint is incomplete.");

        var normalized = this with
        {
            MachineName = MachineName.Trim(),
            WindowsDescription = WindowsDescription?.Trim() ?? string.Empty,
            InstanceName = InstanceName.Trim(),
            AndroidVersion = AndroidVersion?.Trim() ?? string.Empty
        };
        return normalized with { Id = ComputeId(normalized) };
    }

    public bool IsStructurallyEquivalentTo(PerformanceEnvironmentFingerprint other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var left = Rehydrate();
        var right = other.Rehydrate();
        return string.Equals(left.MachineName, right.MachineName, StringComparison.OrdinalIgnoreCase)
            && left.LogicalProcessors == right.LogicalProcessors
            && MemoryMatches(left.MemoryTotalMb, right.MemoryTotalMb)
            && left.Is64BitOs == right.Is64BitOs
            && string.Equals(left.InstanceName, right.InstanceName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.AndroidVersion, right.AndroidVersion, StringComparison.OrdinalIgnoreCase)
            && left.Game == right.Game;
    }

    public bool IsStructurallyCompatible(EnvironmentSnapshot current)
    {
        ArgumentNullException.ThrowIfNull(current);
        var stored = Rehydrate();
        var instance = current.Instances.FirstOrDefault(item =>
            string.Equals(item.Name, stored.InstanceName, StringComparison.OrdinalIgnoreCase));
        if (instance is null) return false;

        var currentMemory = ToMemoryMb(current.MemoryTotalGb);
        return string.Equals(stored.MachineName, current.MachineName, StringComparison.OrdinalIgnoreCase)
            && stored.LogicalProcessors == current.LogicalProcessors
            && MemoryMatches(stored.MemoryTotalMb, currentMemory)
            && stored.Is64BitOs == current.Is64BitOs
            && string.Equals(stored.AndroidVersion, instance.AndroidVersion ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static long? ToMemoryMb(double? totalGb)
        => totalGb is double value && double.IsFinite(value) && value > 0
            ? (long)Math.Round(value * 1024d, MidpointRounding.AwayFromZero)
            : null;

    private static bool MemoryMatches(long? left, long? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return Math.Abs(left.Value - right.Value) <= 256;
    }

    private static string ComputeId(PerformanceEnvironmentFingerprint fingerprint)
    {
        var canonical = string.Join("\n",
            fingerprint.MachineName.ToUpperInvariant(),
            fingerprint.WindowsDescription,
            fingerprint.LogicalProcessors.ToString(CultureInfo.InvariantCulture),
            fingerprint.MemoryTotalMb?.ToString(CultureInfo.InvariantCulture) ?? "unknown",
            fingerprint.Is64BitOs ? "x64" : "x86",
            fingerprint.InstanceName.ToUpperInvariant(),
            fingerprint.AndroidVersion.ToUpperInvariant(),
            fingerprint.Game.ToString());
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}

public sealed record PerformanceConfigurationSnapshot
{
    public GameKind Game { get; init; }
    public string InstanceName { get; init; } = string.Empty;
    public int CpuCores { get; init; }
    public int RamMb { get; init; }
    public string Renderer { get; init; } = string.Empty;
    public int FpsTarget { get; init; }
    public string Resolution { get; init; } = string.Empty;
    public int Dpi { get; init; }
    public required PerformanceEnvironmentFingerprint Environment { get; init; }

    public static PerformanceConfigurationSnapshot? Capture(
        EnvironmentSnapshot environment,
        BlueStacksInstance instance,
        GameKind game)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(instance);
        if (game is not (GameKind.FreeFire or GameKind.FreeFireMax)) return null;
        if (string.IsNullOrWhiteSpace(instance.Name)
            || instance.CpuCores is not > 0
            || instance.RamMb is not >= 1024
            || string.IsNullOrWhiteSpace(instance.Renderer)
            || instance.Fps is not > 0
            || string.IsNullOrWhiteSpace(instance.Resolution)
            || instance.Dpi is not > 0)
            return null;
        if (!IsResolution(instance.Resolution)) return null;

        return new PerformanceConfigurationSnapshot
        {
            Game = game,
            InstanceName = instance.Name.Trim(),
            CpuCores = instance.CpuCores.Value,
            RamMb = instance.RamMb.Value,
            Renderer = instance.Renderer.Trim(),
            FpsTarget = instance.Fps.Value,
            Resolution = NormalizeResolution(instance.Resolution),
            Dpi = instance.Dpi.Value,
            Environment = PerformanceEnvironmentFingerprint.Capture(environment, instance, game)
        };
    }

    public PerformanceConfigurationSnapshot Rehydrate()
    {
        if (Game is not (GameKind.FreeFire or GameKind.FreeFireMax)
            || string.IsNullOrWhiteSpace(InstanceName)
            || CpuCores <= 0
            || RamMb < 1024
            || string.IsNullOrWhiteSpace(Renderer)
            || FpsTarget <= 0
            || !IsResolution(Resolution)
            || Dpi <= 0)
            throw new InvalidDataException("Stored performance configuration is incomplete.");

        var environment = Environment?.Rehydrate()
            ?? throw new InvalidDataException("Stored performance configuration has no environment fingerprint.");
        if (!string.Equals(environment.InstanceName, InstanceName, StringComparison.OrdinalIgnoreCase)
            || environment.Game != Game)
            throw new InvalidDataException("Stored performance configuration does not match its environment fingerprint.");

        return this with
        {
            InstanceName = InstanceName.Trim(),
            Renderer = Renderer.Trim(),
            Resolution = NormalizeResolution(Resolution),
            Environment = environment
        };
    }

    public bool IsEquivalentTo(PerformanceConfigurationSnapshot other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var left = Rehydrate();
        var right = other.Rehydrate();
        return left.Game == right.Game
            && string.Equals(left.InstanceName, right.InstanceName, StringComparison.OrdinalIgnoreCase)
            && left.CpuCores == right.CpuCores
            && left.RamMb == right.RamMb
            && string.Equals(left.Renderer, right.Renderer, StringComparison.OrdinalIgnoreCase)
            && left.FpsTarget == right.FpsTarget
            && string.Equals(left.Resolution, right.Resolution, StringComparison.OrdinalIgnoreCase)
            && left.Dpi == right.Dpi
            && left.Environment.IsStructurallyEquivalentTo(right.Environment);
    }

    private static bool IsResolution(string? resolution)
    {
        if (string.IsNullOrWhiteSpace(resolution)) return false;
        var parts = resolution.Split('x', 'X');
        return parts.Length == 2
            && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var width)
            && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var height)
            && width is >= 640 and <= 7680
            && height is >= 480 and <= 4320;
    }

    private static string NormalizeResolution(string resolution)
    {
        var parts = resolution.Split('x', 'X');
        var width = int.Parse(parts[0], CultureInfo.InvariantCulture);
        var height = int.Parse(parts[1], CultureInfo.InvariantCulture);
        return $"{width}x{height}";
    }
}
