using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Services;

internal static class PresentMonProcessTargetingSelfTests
{
    [ModuleInitializer]
    internal static void Run()
    {
        var arguments = PresentMonService.BuildCaptureArguments(4242, TimeSpan.FromSeconds(12), "C:/captures/sample.csv");

        Require(arguments.Contains("--process_id"), "PresentMon capture must select a process explicitly.");
        var processIndex = arguments.IndexOf("--process_id");
        Require(processIndex >= 0 && processIndex + 1 < arguments.Count && arguments[processIndex + 1] == "4242", "PresentMon must target the exact runtime-owned PID.");
        Require(arguments.Contains("--timed") && arguments.Contains("12"), "Timed capture duration must be encoded without shell string concatenation.");
        Require(arguments.Contains("--output_file") && arguments.Contains("C:/captures/sample.csv"), "Output path must remain one argument even when ProcessStartInfo.ArgumentList is used.");
        Require(arguments.Contains("--exclude_dropped"), "Dropped frames must stay excluded from tuning evidence.");
        Console.WriteLine("PASS explicit PresentMon process targeting");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}

internal static class IReadOnlyListExtensions
{
    public static int IndexOf(this IReadOnlyList<string> values, string value)
    {
        for (var i = 0; i < values.Count; i++)
            if (string.Equals(values[i], value, StringComparison.Ordinal)) return i;
        return -1;
    }
}
