using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Telemetry;

internal static class WddmGpuPdhInstanceParserSelfTests
{
    [ModuleInitializer]
    internal static void Run()
    {
        Require(WddmGpuPdhInstanceParser.TryParsePhysicalEngineKey(
                "pid_1234_luid_0x00000000_0x0000ABCD_phys_0_eng_3_engtype_3D",
                out var key)
                && key == "luid:00000000:0000abcd/phys:0/eng:3",
            "A documented-observed GPU Engine instance shape must canonicalize LUID/physical/engine identity.");

        Require(WddmGpuPdhInstanceParser.TryParsePhysicalEngineKey(
                "pid_9999_luid_0x00000000_0x0000abcd_phys_0_eng_3_engtype_Copy",
                out var sameKey)
                && sameKey == key,
            "PID and engine-type labels must not split contributions that identify the same physical engine.");

        Require(WddmGpuPdhInstanceParser.TryParsePhysicalEngineKey(
                "  PID_1_LUID_0x0000000A_0x0000000B_PHYS_2_ENG_12_ENGTYPE_VideoDecode  ",
                out var normalized)
                && normalized == "luid:0000000a:0000000b/phys:2/eng:12",
            "Parser must be culture-invariant and case-insensitive while returning a stable lowercase key.");

        RequireRejected("pid_1_phys_0_eng_1_engtype_3D");
        RequireRejected("pid_1_luid_0x0_0x1_eng_1_engtype_3D");
        RequireRejected("pid_1_luid_0x0_0x1_phys_0_engtype_3D");
        RequireRejected("pid_1_luid_x0_0x1_phys_0_eng_1");
        RequireRejected("pid_1_luid_0x0_0x1_phys_-1_eng_1");
        RequireRejected("pid_1_luid_0x0_0x1_phys_0_eng_-1");
        RequireRejected("pid_1_luid_0x100000000_0x1_phys_0_eng_1");
        RequireRejected("pid_1_luid_0x0_0x100000000_phys_0_eng_1");
        RequireRejected("pid_1_luid_0x0_0x1_phys_4294967296_eng_1");
        RequireRejected("pid_1_luid_0x0_0x1_phys_0_eng_4294967296");

        Require(!WddmGpuPdhInstanceParser.TryParsePhysicalEngineKey(null, out var nullKey)
                && nullKey is null,
            "Null instance name must be rejected without a fabricated key.");
        Require(!WddmGpuPdhInstanceParser.TryParsePhysicalEngineKey("   ", out var blankKey)
                && blankKey is null,
            "Blank instance name must be rejected without a fabricated key.");

        Console.WriteLine("PASS Track 4 WDDM PDH instance parsing groups only explicitly identified physical GPU engines");
    }

    private static void RequireRejected(string value)
    {
        Require(!WddmGpuPdhInstanceParser.TryParsePhysicalEngineKey(value, out var key)
                && key is null,
            $"Malformed or incomplete WDDM instance '{value}' must be rejected.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
