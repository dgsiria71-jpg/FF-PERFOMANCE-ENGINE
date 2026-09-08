using System.Globalization;
using System.Text.RegularExpressions;

namespace FFPerformanceEngine.Core.Telemetry;

public static partial class WddmGpuPdhInstanceParser
{
    [GeneratedRegex(
        @"(?:^|_)luid_0x(?<high>[0-9a-f]{1,8})_0x(?<low>[0-9a-f]{1,8})_phys_(?<phys>[0-9]+)_eng_(?<eng>[0-9]+)(?:_|$)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture)]
    private static partial Regex PhysicalEnginePattern();

    public static bool TryParsePhysicalEngineKey(
        string? instanceName,
        out string? engineKey)
    {
        engineKey = null;
        if (string.IsNullOrWhiteSpace(instanceName)) return false;

        var match = PhysicalEnginePattern().Match(instanceName.Trim());
        if (!match.Success) return false;

        if (!uint.TryParse(
                match.Groups["high"].Value,
                NumberStyles.AllowHexSpecifier,
                CultureInfo.InvariantCulture,
                out var high)
            || !uint.TryParse(
                match.Groups["low"].Value,
                NumberStyles.AllowHexSpecifier,
                CultureInfo.InvariantCulture,
                out var low)
            || !uint.TryParse(
                match.Groups["phys"].Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var physicalIndex)
            || !uint.TryParse(
                match.Groups["eng"].Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var engineIndex))
        {
            return false;
        }

        engineKey = FormattableString.Invariant(
            $"luid:{high:x8}:{low:x8}/phys:{physicalIndex}/eng:{engineIndex}");
        return true;
    }
}
