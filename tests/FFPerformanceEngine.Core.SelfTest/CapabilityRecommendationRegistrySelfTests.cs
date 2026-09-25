using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;

internal static class CapabilityRecommendationRegistrySelfTests
{
    internal static void Run()
    {
        const string id = "test.recommendation";
        var registry = new WindowsPerformanceCapabilityRegistry(
        [
            new WindowsPerformanceCapability
            {
                CapabilityId = id,
                Name = "Recommendation test",
                Description = "Recommendation registry self-test",
                Domain = CapabilityDomain.Power,
                Availability = CapabilityAvailability.Available,
                CurrentValue = "old",
                PersistenceScope = CapabilityPersistenceScope.PersistentAllowed,
                Safety = ActionSafety.LobbySafe,
                RiskLevel = CapabilityRiskLevel.Low,
                Dependencies = ["test.base"]
            },
            new WindowsPerformanceCapability
            {
                CapabilityId = "test.base",
                Name = "Base",
                Description = "dependency",
                Domain = CapabilityDomain.System,
                Availability = CapabilityAvailability.Available,
                CurrentValue = "base",
                PersistenceScope = CapabilityPersistenceScope.PersistentAllowed,
                Safety = ActionSafety.LiveSafe,
                RiskLevel = CapabilityRiskLevel.Safe
            }
        ]);

        var generatedAt = DateTimeOffset.UtcNow;
        registry.UpdateRecommendation(
            id,
            "new",
            new CapabilityRecommendationSummary
            {
                Source = CapabilityRecommendationSource.ValidatedEvidence,
                Confidence = 0.94,
                MachineFingerprintId = "machine-a",
                GeneratedAt = generatedAt
            });

        var updated = registry.GetAll().Single(item => item.CapabilityId == id);
        Require(updated.RecommendedValue == "new"
                && updated.Recommendation.Source == CapabilityRecommendationSource.ValidatedEvidence
                && Math.Abs(updated.Recommendation.Confidence - 0.94) < 0.0001
                && updated.Recommendation.MachineFingerprintId == "machine-a"
                && updated.Recommendation.GeneratedAt == generatedAt,
            "Recommendation publication must preserve target, provenance, confidence, fingerprint and generation timestamp.");
        Require(updated.Availability == CapabilityAvailability.Available && updated.CurrentValue == "old",
            "Publishing a recommendation must not mutate runtime availability/current state.");
        Require(updated.Dependencies.SequenceEqual(new[] { "test.base" }, StringComparer.OrdinalIgnoreCase),
            "Publishing a recommendation must preserve capability graph metadata.");

        // Returned descriptors are clones; caller mutation must not corrupt registry authority.
        updated.RecommendedValue = "tampered";
        updated.Recommendation = updated.Recommendation with { Confidence = 0.01 };
        var cloneCheck = registry.GetAll().Single(item => item.CapabilityId == id);
        Require(cloneCheck.RecommendedValue == "new" && Math.Abs(cloneCheck.Recommendation.Confidence - 0.94) < 0.0001,
            "GetAll recommendation data must remain clone-isolated from caller mutation.");

        RequireThrows<ArgumentOutOfRangeException>(() => registry.UpdateRecommendation(
            id,
            "bad",
            new CapabilityRecommendationSummary
            {
                Source = CapabilityRecommendationSource.Diagnostic,
                Confidence = 1.1,
                MachineFingerprintId = "machine-a",
                GeneratedAt = DateTimeOffset.UtcNow
            }));
        RequireThrows<ArgumentException>(() => registry.UpdateRecommendation(
            id,
            "bad",
            new CapabilityRecommendationSummary
            {
                Source = CapabilityRecommendationSource.Unknown,
                Confidence = 0.9,
                MachineFingerprintId = "machine-a",
                GeneratedAt = DateTimeOffset.UtcNow
            }));
        RequireThrows<ArgumentException>(() => registry.UpdateRecommendation(
            id,
            "bad",
            new CapabilityRecommendationSummary
            {
                Source = CapabilityRecommendationSource.ControlledEvidence,
                Confidence = 0.9,
                MachineFingerprintId = string.Empty,
                GeneratedAt = DateTimeOffset.UtcNow
            }));
        RequireThrows<ArgumentException>(() => registry.UpdateRecommendation(
            id,
            "bad",
            new CapabilityRecommendationSummary
            {
                Source = CapabilityRecommendationSource.ValidatedEvidence,
                Confidence = 0.9,
                MachineFingerprintId = "machine-a",
                GeneratedAt = null
            }));
        RequireThrows<ArgumentException>(() => registry.UpdateRecommendation(
            id,
            "   ",
            new CapabilityRecommendationSummary
            {
                Source = CapabilityRecommendationSource.Diagnostic,
                Confidence = 0.9,
                MachineFingerprintId = "machine-a",
                GeneratedAt = DateTimeOffset.UtcNow
            }));
        RequireThrows<KeyNotFoundException>(() => registry.UpdateRecommendation(
            "unknown.capability",
            "new",
            new CapabilityRecommendationSummary
            {
                Source = CapabilityRecommendationSource.Diagnostic,
                Confidence = 0.9,
                MachineFingerprintId = "machine-a",
                GeneratedAt = DateTimeOffset.UtcNow
            }));

        var afterRejectedUpdates = registry.GetAll().Single(item => item.CapabilityId == id);
        Require(afterRejectedUpdates.RecommendedValue == "new"
                && Math.Abs(afterRejectedUpdates.Recommendation.Confidence - 0.94) < 0.0001,
            "Rejected recommendation updates must be atomic and preserve the previous valid recommendation.");

        registry.ClearRecommendation(id);
        var cleared = registry.GetAll().Single(item => item.CapabilityId == id);
        Require(cleared.RecommendedValue is null
                && cleared.Recommendation.Source == CapabilityRecommendationSource.Unknown
                && cleared.Recommendation.Confidence == 0
                && string.IsNullOrEmpty(cleared.Recommendation.MachineFingerprintId)
                && cleared.Recommendation.GeneratedAt is null,
            "ClearRecommendation must atomically remove target and provenance without touching the capability descriptor.");
        Require(cleared.Availability == CapabilityAvailability.Available && cleared.CurrentValue == "old",
            "Clearing a recommendation must not alter proven runtime state.");

        Console.WriteLine("PASS Track 2 capability registry recommendation publish/clone/validate/clear authority contract");
    }

    private static void RequireThrows<TException>(Action operation) where TException : Exception
    {
        try
        {
            operation();
        }
        catch (TException)
        {
            return;
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Expected {typeof(TException).Name}, but received {exception.GetType().Name}.",
                exception);
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name}, but operation completed successfully.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
