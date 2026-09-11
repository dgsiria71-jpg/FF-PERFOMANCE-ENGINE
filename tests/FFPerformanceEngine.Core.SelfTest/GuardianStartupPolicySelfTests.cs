using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class GuardianStartupPolicySelfTests
{
    public static void Run()
    {
        DisabledGuardianNeverStarts();
        PersistedInstanceMustStillExist();
        SingleInstanceCanStartAutomatically();
        MultipleInstancesRequireAnExplicitSelection();
        Console.WriteLine("PASS Guardian startup instance selection policy");
    }

    private static void DisabledGuardianNeverStarts()
    {
        var settings = new AppSettings { GuardianEnabled = false, GuardianInstanceName = "Pie64" };
        Require(GuardianStartupPolicy.SelectInstanceName(settings, EnvironmentWith("Pie64")) is null,
            "Disabled Guardian must not start a background monitoring loop.");
    }

    private static void PersistedInstanceMustStillExist()
    {
        var settings = new AppSettings { GuardianEnabled = true, GuardianInstanceName = "Pie64" };
        Require(GuardianStartupPolicy.SelectInstanceName(settings, EnvironmentWith("Pie64", "Android11")) == "Pie64",
            "A persisted Guardian instance may be reused only when that exact instance still exists.");

        settings = settings with { GuardianInstanceName = "Missing" };
        Require(GuardianStartupPolicy.SelectInstanceName(settings, EnvironmentWith("Pie64", "Android11")) is null,
            "Guardian must not silently substitute another instance when the persisted target disappeared.");
    }

    private static void SingleInstanceCanStartAutomatically()
    {
        var settings = new AppSettings { GuardianEnabled = true };
        Require(GuardianStartupPolicy.SelectInstanceName(settings, EnvironmentWith("Pie64")) == "Pie64",
            "A single configured BlueStacks instance is unambiguous and may be selected automatically.");
    }

    private static void MultipleInstancesRequireAnExplicitSelection()
    {
        var settings = new AppSettings { GuardianEnabled = true };
        Require(GuardianStartupPolicy.SelectInstanceName(settings, EnvironmentWith("Pie64", "Android11")) is null,
            "Multiple configured BlueStacks instances must require an explicit Guardian selection.");
    }

    private static EnvironmentSnapshot EnvironmentWith(params string[] names) => new()
    {
        BlueStacksDetected = names.Length > 0,
        Instances = names.Select(name => new BlueStacksInstance { Name = name }).ToArray()
    };

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
