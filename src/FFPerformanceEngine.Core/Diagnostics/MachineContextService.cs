using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Diagnostics;

public sealed class MachineContextService
{
    private readonly HardwareDiscoveryService _hardwareDiscovery;
    private readonly WindowsPerformanceCapabilityRegistry _capabilities;

    public MachineContextService(
        HardwareDiscoveryService hardwareDiscovery,
        WindowsPerformanceCapabilityRegistry capabilities)
    {
        _hardwareDiscovery = hardwareDiscovery ?? throw new ArgumentNullException(nameof(hardwareDiscovery));
        _capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
    }

    public MachineContext Capture(EnvironmentSnapshot environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        var hardware = _hardwareDiscovery.Discover(environment);
        var fingerprint = CaptureFingerprint(environment, hardware);
        return new MachineContext
        {
            SchemaVersion = 2,
            CapturedAt = DateTimeOffset.UtcNow,
            Environment = environment,
            Hardware = hardware,
            Capabilities = _capabilities.GetAll(),
            Fingerprint = fingerprint
        };
    }

    private static MachineEnvironmentFingerprintV2 CaptureFingerprint(
        EnvironmentSnapshot environment,
        HardwareDiscoveryResult hardware)
    {
        var memoryTotalMb = ToMemoryMb(environment.MemoryTotalGb);
        var hardwareSignatures = hardware.Components
            .Where(component => component.Kind is HardwareComponentKind.Cpu or HardwareComponentKind.Gpu or HardwareComponentKind.Memory)
            .Select(StableHardwareSignature)
            .Where(signature => !string.IsNullOrWhiteSpace(signature))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(signature => signature, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var fingerprint = new MachineEnvironmentFingerprintV2
        {
            SchemaVersion = 2,
            MachineName = environment.MachineName?.Trim() ?? string.Empty,
            WindowsDescription = environment.WindowsDescription?.Trim() ?? string.Empty,
            LogicalProcessors = environment.LogicalProcessors,
            MemoryTotalMb = memoryTotalMb,
            Is64BitOs = environment.Is64BitOs,
            HardwareSignatures = hardwareSignatures
        };
        return fingerprint with { Id = ComputeId(fingerprint) };
    }

    private static string StableHardwareSignature(HardwareComponentSnapshot component)
    {
        var stable = component.Kind switch
        {
            HardwareComponentKind.Cpu => string.Join('|',
                "CPU",
                component.Name.Trim().ToUpperInvariant(),
                component.Vendor.Trim().ToUpperInvariant(),
                component.Architecture.Trim().ToUpperInvariant(),
                component.LogicalProcessors?.ToString(CultureInfo.InvariantCulture) ?? "unknown"),
            HardwareComponentKind.Gpu => string.Join('|',
                "GPU",
                component.Name.Trim().ToUpperInvariant(),
                component.Vendor.Trim().ToUpperInvariant(),
                component.CapacityBytes?.ToString(CultureInfo.InvariantCulture) ?? "unknown"),
            HardwareComponentKind.Memory => string.Join('|',
                "MEMORY",
                component.CapacityBytes?.ToString(CultureInfo.InvariantCulture) ?? "unknown"),
            _ => string.Empty
        };
        return stable;
    }

    private static string ComputeId(MachineEnvironmentFingerprintV2 fingerprint)
    {
        var canonical = string.Join("\n",
            "DG-MACHINE-CONTEXT-V2",
            fingerprint.MachineName.ToUpperInvariant(),
            fingerprint.WindowsDescription,
            fingerprint.LogicalProcessors.ToString(CultureInfo.InvariantCulture),
            fingerprint.MemoryTotalMb?.ToString(CultureInfo.InvariantCulture) ?? "unknown",
            fingerprint.Is64BitOs ? "x64" : "x86",
            string.Join("\n", fingerprint.HardwareSignatures.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static long? ToMemoryMb(double? totalGb)
        => totalGb is double value && double.IsFinite(value) && value > 0
            ? (long)Math.Round(value * 1024d, MidpointRounding.AwayFromZero)
            : null;
}
