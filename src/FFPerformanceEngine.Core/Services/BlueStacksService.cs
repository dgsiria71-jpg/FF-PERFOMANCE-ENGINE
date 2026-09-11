using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed record BlueStacksConfigWriteResult(bool Success, bool RequiresPlayerStop, string Message, string? BackupPath = null);

public sealed class BlueStacksService
{
    private static readonly string[] ProcessNames = ["HD-Player", "BlueStacks", "BstkSVC", "BlueStacksAppplayer"];
    private static readonly string[] PlayerProcessNames = ["HD-Player", "BlueStacksAppplayer"];
    private static readonly Regex InstanceKey = new(@"^bst\.instance\.(?<instance>[^.]+)\.(?<key>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex SafeInstanceName = new(@"^[A-Za-z0-9_-]+$", RegexOptions.Compiled);
    private static readonly Regex SafeValue = new(@"^[A-Za-z0-9_.:+-]+$", RegexOptions.Compiled);

    private static readonly HashSet<string> CapturableSettings = new(
        ["cpus", "ram", "fps", "max_fps", "enable_high_fps", "enable_vsync", "graphics_renderer", "graphics_engine", "display_width", "display_height", "fb_width", "fb_height", "dpi"],
        StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> MutableSettings = new(
        ["cpus", "ram", "fps", "max_fps", "enable_high_fps", "enable_vsync", "display_width", "display_height", "fb_width", "fb_height", "dpi"],
        StringComparer.OrdinalIgnoreCase);

    public string? FindConfigPath()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "BlueStacks_nxt", "bluestacks.conf"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "BlueStacks", "bluestacks.conf")
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    public string? FindPlayerExecutable()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var candidates = new[]
        {
            Path.Combine(programFiles, "BlueStacks_nxt", "HD-Player.exe"),
            Path.Combine(programFiles, "BlueStacks", "HD-Player.exe"),
            Path.Combine(programFilesX86, "BlueStacks_nxt", "HD-Player.exe"),
            Path.Combine(programFilesX86, "BlueStacks", "HD-Player.exe")
        };
        return candidates.FirstOrDefault(File.Exists);
    }

    public IReadOnlyList<string> DetectProcesses()
    {
        var found = new List<string>();
        foreach (var name in ProcessNames)
        {
            try
            {
                if (Process.GetProcessesByName(name).Length > 0) found.Add(name);
            }
            catch (InvalidOperationException) { }
        }
        return found;
    }

    public bool IsPlayerRunning()
    {
        foreach (var name in PlayerProcessNames)
        {
            try
            {
                if (Process.GetProcessesByName(name).Any(p => !p.HasExited)) return true;
            }
            catch (InvalidOperationException) { }
        }
        return false;
    }

    public IReadOnlyList<BlueStacksInstance> ParseConfig(string text)
    {
        var raw = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var sourceLine in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (sourceLine.StartsWith('#')) continue;
            var equals = sourceLine.IndexOf('=');
            if (equals <= 0) continue;
            var key = sourceLine[..equals].Trim();
            var value = sourceLine[(equals + 1)..].Trim().Trim('"');
            var match = InstanceKey.Match(key);
            if (!match.Success) continue;
            var instance = match.Groups["instance"].Value;
            var setting = match.Groups["key"].Value;
            if (!raw.TryGetValue(instance, out var settings)) raw[instance] = settings = new(StringComparer.OrdinalIgnoreCase);
            settings[setting] = value;
        }

        return raw.Select(kvp => new BlueStacksInstance
        {
            Name = kvp.Key,
            AndroidVersion = GuessAndroid(kvp.Key, kvp.Value),
            CpuCores = ReadInt(kvp.Value, "cpus"),
            RamMb = ReadInt(kvp.Value, "ram"),
            Renderer = ReadString(kvp.Value, "graphics_renderer") ?? ReadString(kvp.Value, "graphics_engine"),
            Fps = ReadInt(kvp.Value, "max_fps") ?? ReadInt(kvp.Value, "fps"),
            Resolution = ReadResolution(kvp.Value),
            Dpi = ReadInt(kvp.Value, "dpi"),
            AdbPort = ReadInt(kvp.Value, "adb_port"),
            AdbEnabled = ReadBool(kvp.Value, "enable_adb")
        }).OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public IReadOnlyList<BlueStacksInstance> LoadInstances()
    {
        var path = FindConfigPath();
        if (path is null) return Array.Empty<BlueStacksInstance>();
        try { return ParseConfig(File.ReadAllText(path)); }
        catch (IOException) { return Array.Empty<BlueStacksInstance>(); }
        catch (UnauthorizedAccessException) { return Array.Empty<BlueStacksInstance>(); }
    }

    public Dictionary<string, string> CaptureAllowedSettings(string instanceName)
    {
        var path = FindConfigPath();
        if (path is null) return new(StringComparer.OrdinalIgnoreCase);
        return CaptureAllowedSettingsFromText(File.ReadAllText(path), instanceName);
    }

    public static Dictionary<string, string> CaptureAllowedSettingsFromText(string text, string instanceName)
    {
        ValidateInstanceName(instanceName);
        var prefix = $"bst.instance.{instanceName}.";
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var equals = line.IndexOf('=');
            if (equals <= 0) continue;
            var key = line[..equals].Trim();
            if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            var shortKey = key[prefix.Length..];
            if (CapturableSettings.Contains(shortKey)) result[key] = line[(equals + 1)..].Trim();
        }
        return result;
    }

    public static string UpdateInstanceConfigText(string text, string instanceName, IReadOnlyDictionary<string, string> updates)
    {
        ValidateInstanceName(instanceName);
        if (updates.Count == 0) return text;
        foreach (var pair in updates)
        {
            if (!MutableSettings.Contains(pair.Key)) throw new ArgumentException($"BlueStacks setting '{pair.Key}' is not allow-listed for mutation.", nameof(updates));
            if (string.IsNullOrWhiteSpace(pair.Value) || !SafeValue.IsMatch(pair.Value)) throw new ArgumentException($"BlueStacks setting '{pair.Key}' contains an invalid value.", nameof(updates));
        }

        var newline = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var endsWithNewline = text.EndsWith("\r\n", StringComparison.Ordinal) || text.EndsWith('\n');
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').ToList();
        if (endsWithNewline && lines.Count > 0 && lines[^1].Length == 0) lines.RemoveAt(lines.Count - 1);

        var prefix = $"bst.instance.{instanceName}.";
        var remaining = new Dictionary<string, string>(updates, StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var equals = line.IndexOf('=');
            if (equals <= 0) continue;
            var fullKey = line[..equals].Trim();
            if (!fullKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            var shortKey = fullKey[prefix.Length..];
            if (!remaining.Remove(shortKey, out var value)) continue;
            lines[index] = $"{fullKey}=\"{value}\"";
        }

        foreach (var pair in remaining.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            lines.Add($"{prefix}{pair.Key}=\"{pair.Value}\"");

        var result = string.Join(newline, lines);
        return endsWithNewline ? result + newline : result;
    }

    public BlueStacksConfigWriteResult ApplyInstanceSettings(string instanceName, IReadOnlyDictionary<string, string> updates)
    {
        if (IsPlayerRunning()) return new(false, true, "BlueStacks App Player is running. Restart-required settings were not changed.");
        var path = FindConfigPath();
        if (path is null) return new(false, false, "BlueStacks configuration file was not found.");

        try
        {
            var original = File.ReadAllText(path);
            var updated = UpdateInstanceConfigText(original, instanceName, updates);
            if (string.Equals(original, updated, StringComparison.Ordinal)) return new(true, false, "No configuration change was necessary.");

            var backup = path + $".ffpe-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}.bak";
            File.Copy(path, backup, false);
            var temp = path + ".ffpe.tmp";
            File.WriteAllText(temp, updated, new UTF8Encoding(false));
            File.Move(temp, path, true);
            return new(true, false, "BlueStacks configuration updated. Changes take effect on the next instance launch.", backup);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return new(false, false, ex.Message);
        }
    }

    public BlueStacksConfigWriteResult RestoreBackup(string backupPath)
    {
        if (IsPlayerRunning()) return new(false, true, "BlueStacks App Player is running. Restore was not performed.");
        var path = FindConfigPath();
        if (path is null) return new(false, false, "BlueStacks configuration file was not found.");
        var expectedDirectory = Path.GetFullPath(Path.GetDirectoryName(path)!);
        var candidate = Path.GetFullPath(backupPath);
        if (!candidate.StartsWith(expectedDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(candidate))
            return new(false, false, "Backup path is invalid or unavailable.");

        try
        {
            var temp = path + ".ffpe.restore.tmp";
            File.Copy(candidate, temp, true);
            File.Move(temp, path, true);
            return new(true, false, "BlueStacks configuration restored from backup.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new(false, false, ex.Message);
        }
    }

    private static void ValidateInstanceName(string instanceName)
    {
        if (string.IsNullOrWhiteSpace(instanceName) || !SafeInstanceName.IsMatch(instanceName))
            throw new ArgumentException("BlueStacks instance name contains unsupported characters.", nameof(instanceName));
    }

    private static int? ReadInt(Dictionary<string, string> values, string key)
        => values.TryGetValue(key, out var raw) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    private static bool? ReadBool(Dictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out var raw)) return null;
        if (raw == "1") return true;
        if (raw == "0") return false;
        return bool.TryParse(raw, out var parsed) ? parsed : null;
    }

    private static string? ReadString(Dictionary<string, string> values, string key) => values.TryGetValue(key, out var value) ? value : null;

    private static string? ReadResolution(Dictionary<string, string> values)
    {
        var width = ReadInt(values, "fb_width") ?? ReadInt(values, "display_width");
        var height = ReadInt(values, "fb_height") ?? ReadInt(values, "display_height");
        return width is not null && height is not null ? $"{width}x{height}" : null;
    }

    private static string GuessAndroid(string instance, Dictionary<string, string> values)
    {
        var explicitVersion = ReadString(values, "android_version");
        if (!string.IsNullOrWhiteSpace(explicitVersion)) return explicitVersion;
        if (instance.Contains("Pie", StringComparison.OrdinalIgnoreCase)) return "Pie 64-bit";
        if (instance.Contains("Rvc", StringComparison.OrdinalIgnoreCase) || instance.Contains("Android11", StringComparison.OrdinalIgnoreCase)) return "Android 11";
        if (instance.Contains("Nougat64", StringComparison.OrdinalIgnoreCase)) return "Nougat 64-bit";
        return "Unknown";
    }
}
