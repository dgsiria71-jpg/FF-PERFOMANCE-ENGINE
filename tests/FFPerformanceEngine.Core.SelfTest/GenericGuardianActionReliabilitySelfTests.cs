using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;

internal static class GenericGuardianActionReliabilitySelfTests
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "dg-guardian-reliability-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "guardian-knowledge.json");
            var knowledge = new GuardianKnowledgeService(path);

            var scopeA = new GenericGuardianActionReliabilityKey(
                "game.a",
                GuardianAnomalyKind.CpuContention,
                "action.same");
            var scopeB = new GenericGuardianActionReliabilityKey(
                "game.b",
                GuardianAnomalyKind.CpuContention,
                "action.same");

            await knowledge.RecordGenericAsync(
                scopeA,
                GenericGuardianSessionCanaryVerdict.Improved,
                relativeFpsGain: 0.05);
            await knowledge.RecordGenericAsync(
                scopeA,
                GenericGuardianSessionCanaryVerdict.Regressive,
                relativeFpsGain: -0.03);
            await knowledge.RecordGenericAsync(
                scopeA,
                GenericGuardianSessionCanaryVerdict.Inconclusive,
                relativeFpsGain: null);
            await knowledge.RecordGenericAsync(
                scopeB,
                GenericGuardianSessionCanaryVerdict.Improved,
                relativeFpsGain: 0.08);

            var a = await knowledge.GetGenericAsync(scopeA)
                ?? throw new InvalidOperationException("Scoped reliability A was not persisted.");
            var b = await knowledge.GetGenericAsync(scopeB)
                ?? throw new InvalidOperationException("Scoped reliability B was not persisted.");

            Require(
                a.SuccessCount == 1
                && a.FailureCount == 1
                && a.InconclusiveCount == 1
                && a.DecisiveAttempts == 2
                && Math.Abs(a.AverageRelativeFpsGain - 0.05) < 0.000001,
                "Generic reliability must keep Improved, Regressive and Inconclusive outcomes distinct.");
            Require(
                b.SuccessCount == 1
                && b.FailureCount == 0
                && b.InconclusiveCount == 0
                && Math.Abs(b.AverageRelativeFpsGain - 0.08) < 0.000001,
                "Same Action.Id in another GameId must not contaminate reliability.");
            Require(
                !a.IsValidated && !b.IsValidated,
                "Generic reliability must reuse the existing two-success/75% validation rule rather than validate from one success.");

            await knowledge.RecordGenericAsync(
                scopeB,
                GenericGuardianSessionCanaryVerdict.Improved,
                relativeFpsGain: 0.04);
            b = await knowledge.GetGenericAsync(scopeB)
                ?? throw new InvalidOperationException("Scoped reliability B disappeared.");
            Require(
                b.IsValidated
                && b.SuccessCount == 2
                && Math.Abs(b.AverageRelativeFpsGain - 0.06) < 0.000001,
                "Two decisive successes with sufficient rate must validate using the existing Guardian evidence rule.");

            var all = await knowledge.LoadGenericAsync();
            Require(
                all.Count == 2,
                "Generic reliability store must remain scoped and deduplicated by GameId/family/action.");

            Console.WriteLine("PASS Track 6 generic Guardian learned action reliability is local, scoped and verdict-aware");
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
