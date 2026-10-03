using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using FFPerformanceEngine.Core.Models;
using Microsoft.Win32;

namespace FFPerformanceEngine.Core.Diagnostics;

public sealed class HardwareDiscoveryService
{
    public HardwareDiscoveryResult Discover(EnvironmentSnapshot environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        var components = new List<HardwareComponentSnapshot>();
        var warnings = new List<string>();

        components.Add(CreateCpu(environment, warnings));
        if (environment.MemoryTotalGb is double memoryGb && double.IsFinite(memoryGb) && memoryGb > 0)
            components.Add(CreateMemory(memoryGb));

        DiscoverStorage(components, warnings);
        if (OperatingSystem.IsWindows()) DiscoverWindowsGpu(components, warnings);

        var normalized = components
            .Where(component => !string.IsNullOrWhiteSpace(component.StableId))
            .GroupBy(component => component.StableId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(item => item.Confidence).First())
            .OrderBy(component => component.Kind)
            .ThenBy(component => component.StableId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new HardwareDiscoveryResult
        {
            Components = normalized,
            Warnings = warnings.ToArray()
        };
    }

    private static HardwareComponentSnapshot CreateCpu(EnvironmentSnapshot environment, ICollection<string> warnings)
    {
        var name = "CPU";
        var vendor = string.Empty;
        if (OperatingSystem.IsWindows())
        {
            try
            {
                (name, vendor) = ReadWindowsCpuIdentity();
            }
            catch (Exception exception) when (IsExpectedDiscoveryFailure(exception))
            {
                warnings.Add($"CPU identity enrichment unavailable: {exception.GetType().Name}.");
            }
        }

        return new HardwareComponentSnapshot
        {
            StableId = "cpu:primary",
            Kind = HardwareComponentKind.Cpu,
            Name = string.IsNullOrWhiteSpace(name) ? "CPU" : name.Trim(),
            Vendor = vendor.Trim(),
            Architecture = RuntimeInformation.OSArchitecture.ToString(),
            LogicalProcessors = environment.LogicalProcessors > 0 ? environment.LogicalProcessors : null,
            Source = "EnvironmentSnapshot + OS",
            Confidence = environment.LogicalProcessors > 0 ? 0.95 : 0.70,
            Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["osArchitecture"] = RuntimeInformation.OSArchitecture.ToString(),
                ["processArchitecture"] = RuntimeInformation.ProcessArchitecture.ToString()
            }
        };
    }

    private static HardwareComponentSnapshot CreateMemory(double memoryGb)
        => new()
        {
            StableId = "memory:physical",
            Kind = HardwareComponentKind.Memory,
            Name = "Physical memory",
            CapacityBytes = checked((long)Math.Round(memoryGb * 1024d * 1024d * 1024d, MidpointRounding.AwayFromZero)),
            Source = "EnvironmentSnapshot",
            Confidence = 0.95
        };

    private static void DiscoverStorage(ICollection<HardwareComponentSnapshot> components, ICollection<string> warnings)
    {
        try
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (!drive.IsReady || drive.DriveType is DriveType.CDRom or DriveType.NoRootDirectory) continue;
                    components.Add(new HardwareComponentSnapshot
                    {
                        StableId = $"storage:{drive.Name.TrimEnd('\\').ToUpperInvariant()}",
                        Kind = HardwareComponentKind.Storage,
                        Name = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? drive.Name : drive.VolumeLabel,
                        CapacityBytes = drive.TotalSize,
                        Source = "DriveInfo",
                        Confidence = 0.90,
                        Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["root"] = drive.Name,
                            ["driveType"] = drive.DriveType.ToString(),
                            ["format"] = drive.DriveFormat
                        }
                    });
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    warnings.Add($"Storage '{drive.Name}' could not be inspected: {exception.GetType().Name}.");
                }
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            warnings.Add($"Storage discovery unavailable: {exception.GetType().Name}.");
        }
    }

    [SupportedOSPlatform("windows")]
    private static (string Name, string Vendor) ReadWindowsCpuIdentity()
    {
        using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0", writable: false);
        var name = key?.GetValue("ProcessorNameString") as string;
        var vendor = key?.GetValue("VendorIdentifier") as string;
        return (name?.Trim() ?? "CPU", vendor?.Trim() ?? string.Empty);
    }

    [SupportedOSPlatform("windows")]
    private static void DiscoverWindowsGpu(ICollection<HardwareComponentSnapshot> components, ICollection<string> warnings)
    {
        try
        {
            using var videoMap = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\VIDEO", writable: false);
            if (videoMap is null) return;

            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var valueName in videoMap.GetValueNames().OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
            {
                if (videoMap.GetValue(valueName) is not string registryPath) continue;
                const string machinePrefix = @"\Registry\Machine\";
                if (!registryPath.StartsWith(machinePrefix, StringComparison.OrdinalIgnoreCase)) continue;

                var localPath = registryPath[machinePrefix.Length..];
                using var adapter = Registry.LocalMachine.OpenSubKey(localPath, writable: false);
                var description = adapter?.GetValue("DriverDesc") as string
                                  ?? adapter?.GetValue("HardwareInformation.AdapterString") as string;
                if (string.IsNullOrWhiteSpace(description) || !seenNames.Add(description.Trim())) continue;

                components.Add(new HardwareComponentSnapshot
                {
                    StableId = $"gpu:{NormalizeStableToken(description)}",
                    Kind = HardwareComponentKind.Gpu,
                    Name = description.Trim(),
                    Vendor = GuessGpuVendor(description),
                    Source = "Windows Registry",
                    Confidence = 0.86
                });
            }
        }
        catch (Exception exception) when (IsExpectedDiscoveryFailure(exception))
        {
            warnings.Add($"GPU discovery unavailable: {exception.GetType().Name}.");
        }
    }

    private static string GuessGpuVendor(string description)
    {
        if (description.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)) return "NVIDIA";
        if (description.Contains("AMD", StringComparison.OrdinalIgnoreCase) || description.Contains("Radeon", StringComparison.OrdinalIgnoreCase)) return "AMD";
        if (description.Contains("Intel", StringComparison.OrdinalIgnoreCase)) return "Intel";
        return string.Empty;
    }

    private static string NormalizeStableToken(string value)
    {
        var chars = value.Trim().ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();
        return string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
    }

    private static bool IsExpectedDiscoveryFailure(Exception exception)
        => exception is IOException
            or UnauthorizedAccessException
            or System.Security.SecurityException
            or InvalidOperationException;
}
