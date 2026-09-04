using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed class BlueStacksService
{
    private static readonly string[] ProcessNames = ["HD-Player", "BlueStacks", "BstkSVC", "BlueStacksAppplayer"];
    private static readonly Regex InstanceKey = new(@"^bst\.instance\.(?<instance>[^.]+)\.(?<key>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string? FindConfigPath()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "BlueStacks_nxt", "bluestacks.conf"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "BlueStacks", "bluestacks.conf")
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
            Fps = ReadInt(kvp.Value, "fps"),
            Resolution = ReadResolution(kvp.Value)
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
        var prefix = $"bst.instance.{instanceName}.";
        var allowed = new HashSet<string>(["cpus", "ram", "fps", "graphics_renderer", "graphics_engine", "display_width", "display_height", "dpi"], StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadLines(path))
        {
            var equals = line.IndexOf('=');
            if (equals <= 0) continue;
            var key = line[..equals].Trim();
            if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            var shortKey = key[prefix.Length..];
            if (allowed.Contains(shortKey)) result[key] = line[(equals + 1)..].Trim();
        }
        return result;
    }

    private static int? ReadInt(Dictionary<string, string> values, string key)
        => values.TryGetValue(key, out var raw) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    private static string? ReadString(Dictionary<string, string> values, string key) => values.TryGetValue(key, out var value) ? value : null;

    private static string? ReadResolution(Dictionary<string, string> values)
    {
        var width = ReadInt(values, "display_width");
        var height = ReadInt(values, "display_height");
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
