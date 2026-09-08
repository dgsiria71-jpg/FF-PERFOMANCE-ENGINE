using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Telemetry;

internal static class WddmGpuPdhInstanceParserSelfTests
{
    private static string _stage = "not-started";

    [ModuleInitializer]
    internal static void Run()
    {
        try
        {
            RunCore();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"WDDM GPU PDH instance parser self-test failed at stage '{_stage}'.",
                ex);
        }
    }

    private static void RunCore()
    {
        Trace("canonical-parse");
        Require(WddmGpuPdhInstanceParser.TryParsePhysicalEngineKey(
                "pid_1234_luid_0x00000000_0x0000ABCD_phys_0_eng_3_engtype_3D",
                out var key)
                && key == "luid:00000000:0000abcd/phys:0/eng:3",
            "A documented-observed GPU Engine instance shape must canonicalize LUID/physical/engine identity.");

        Trace("process-and-engine-type-do-not-change-physical-key");
        Require(WddmGpuPdhInstanceParser.TryParsePhysicalEngineKey(
                "pid_9999_luid_0x00000000_0x0000abcd_phys_0_eng_3_engtype_Copy",
                out var sameKey)
                && sameKey == key,
            "PID and engine-type labels must not split contributions that identify the same physical engine.");

        Trace("case-and-whitespace");
        Require(WddmGpuPdhInstanceParser.TryParsePhysicalEngineKey(
                "  PID_1_LUID_0x0000000A_0x0000000B_PHYS_2_ENG_12_ENGTYPE_VideoDecode  ",
                out var normalized)
                && normalized == "luid:0000000a:0000000b/phys:2/eng:12",
            "Parser must be culture-invariant and case-insensitive while returning a stable lowercase key.");

        Trace("strict-required-tokens");
        RequireRejected("missing-luid", "pid_1_phys_0_eng_1_engtype_3D");
        RequireRejected("missing-phys", "pid_1_luid_0x0_0x1_eng_1_engtype_3D");
        RequireRejected("missing-eng", "pid_1_luid_0x0_0x1_phys_0_engtype_3D");
        RequireRejected("malformed-luid", "pid_1_luid_x0_0x1_phys_0_eng_1");
        RequireRejected("negative-phys", "pid_1_luid_0x0_0x1_phys_-1_eng_1");
        RequireRejected("negative-eng", "pid_1_luid_0x0_0x1_phys_0_eng_-1");

        Trace("numeric-bounds");
        RequireRejected("high-luid-overflow", "pid_1_luid_0x100000000_0x1_phys_0_eng_1");
        RequireRejected("low-luid-overflow", "pid_1_luid_0x0_0x100000000_phys_0_eng_1");
        RequireRejected("phys-overflow", "pid_1_luid_0x0_0x1_phys_4294967296_eng_1");
        RequireRejected("eng-overflow", "pid_1_luid_0x0_0x1_phys_0_eng_4294967296");

        Trace("null-and-blank");
        Require(!WddmGpuPdhInstanceParser.TryParsePhysicalEngineKey(null, out var nullKey)
                && nullKey is null,
            "Null instance name must be rejected without a fabricated key.");
        Require(!WddmGpuPdhInstanceParser.TryParsePhysicalEngineKey("   ", out var blankKey)
                && blankKey is null,
            "Blank instance name must be rejected without a fabricated key.");

        Trace("complete");
        Console.WriteLine("PASS Track 4 WDDM PDH instance parsing groups only explicitly identified physical GPU engines");
    }

    private static void RequireRejected(string label, string value)
    {
        Console.WriteLine($"TRACE WDDM parser rejected-case entering: {label}");
        Require(!WddmGpuPdhInstanceParser.TryParsePhysicalEngineKey(value, out var key)
                && key is null,
            $"Malformed or incomplete WDDM instance '{value}' must be rejected.");
        Console.WriteLine($"TRACE WDDM parser rejected-case completed: {label}");
    }

    private static void Trace(string stage)
    {
        _stage = stage;
        Console.WriteLine($"TRACE WDDM parser stage: {stage}");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
