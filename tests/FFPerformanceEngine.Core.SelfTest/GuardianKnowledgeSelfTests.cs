using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Models;

internal static class GuardianKnowledgeSelfTests
{
    [ModuleInitializer]
    internal static void Run()
    {
        var evidence = new GuardianActionEvidence { ActionId = "test", SuccessCount = 2, FailureCount = 0, AverageRelativeFpsGain = 0.05 };
        if (!evidence.IsValidated || evidence.SuccessRate != 1.0) throw new InvalidOperationException("Guardian action evidence validation rule regressed.");
        var insufficient = evidence with { SuccessCount = 1 };
        if (insufficient.IsValidated) throw new InvalidOperationException("Guardian action must not validate from a single success.");
        Console.WriteLine("PASS Guardian local evidence threshold");
    }
}
