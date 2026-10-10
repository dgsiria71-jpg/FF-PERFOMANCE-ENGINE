using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.SystemOptimization;

internal static class WindowsCapabilityCandidatePlannerSelfTests
{
    internal static void Run()
    {
        var planner = new WindowsCapabilityCandidatePlanner(
            new WindowsCapabilityCandidatePlannerPolicy { MaxNumericCandidates = 5 });

        var discrete = new WindowsPerformanceCapability
        {
            CapabilityId = "test.mode",
            Name = "mode",
            Availability = CapabilityAvailability.Available,
            CurrentValue = "balanced",
            AvailableValues = ["balanced", "performance", "extreme"],
            ValueSchema = new CapabilityValueSchema
            {
                Kind = CapabilityValueKind.Enumeration,
                AllowedValues = ["balanced", "performance", "extreme"]
            },
            RecommendedValue = "extreme",
            Recommendation = new CapabilityRecommendationSummary
            {
                Source = CapabilityRecommendationSource.ValidatedEvidence,
                Confidence = 0.91,
                MachineFingerprintId = "machine-a",
                GeneratedAt = DateTimeOffset.UtcNow
            }
        };

        var discretePlan = planner.Build(discrete);
        Require(discretePlan.Disposition == WindowsCapabilityCandidatePlanDisposition.Ready,
            "An available enumerated capability with alternatives must produce a candidate plan.");
        Require(discretePlan.Candidates.Select(candidate => candidate.TargetValue)
                .SequenceEqual(new[] { "performance", "extreme" }, StringComparer.Ordinal),
            "Candidate planning must preserve declared discrete order while excluding the exact current state.");
        Require(discretePlan.Candidates.All(candidate => candidate.Source == WindowsCapabilityCandidateSource.ExplicitMetadata),
            "Explicit adapter values must remain distinguishable from schema-generated exploration points.");
        Require(discrete.RecommendedValue == "extreme"
                && discrete.Recommendation.Source == CapabilityRecommendationSource.ValidatedEvidence,
            "Candidate planning must be read-only and must never publish, clear or rewrite recommendations.");

        var numeric = new WindowsPerformanceCapability
        {
            CapabilityId = "test.numeric",
            Name = "numeric",
            Availability = CapabilityAvailability.Available,
            CurrentValue = "50",
            ValueSchema = new CapabilityValueSchema
            {
                Kind = CapabilityValueKind.Integer,
                Minimum = 0,
                Maximum = 100,
                Step = 1,
                Unit = "percent"
            }
        };

        var numericPlan = planner.Build(numeric);
        Require(numericPlan.Disposition == WindowsCapabilityCandidatePlanDisposition.Ready,
            "A bounded integer capability must expose a deterministic exploration plan even when metadata is intentionally compact.");
        Require(numericPlan.Candidates.Select(candidate => candidate.TargetValue)
                .SequenceEqual(new[] { "0", "25", "75", "100" }, StringComparer.Ordinal),
            "Large integer ranges must be sampled deterministically across the full supported range and exclude the exact current point.");
        Require(numericPlan.Candidates.All(candidate => candidate.Source == WindowsCapabilityCandidateSource.SchemaGenerated),
            "Generated numeric exploration points must carry schema-generated provenance.");
        Require(numericPlan.Candidates.Count <= 5,
            "Candidate planning must honor the configured exploration budget.");

        var unavailable = planner.Build(new WindowsPerformanceCapability
        {
            CapabilityId = "test.unavailable",
            Availability = CapabilityAvailability.Unavailable,
            CurrentValue = "1",
            AvailableValues = ["0", "1"],
            ValueSchema = new CapabilityValueSchema { Kind = CapabilityValueKind.Integer, Minimum = 0, Maximum = 1, Step = 1 }
        });
        Require(unavailable.Disposition == WindowsCapabilityCandidatePlanDisposition.Unavailable
                && unavailable.Candidates.Count == 0,
            "Unavailable capabilities must fail closed before producing exploration candidates.");

        var missingState = planner.Build(new WindowsPerformanceCapability
        {
            CapabilityId = "test.missing-current",
            Availability = CapabilityAvailability.Available,
            CurrentValue = null,
            AvailableValues = ["off", "on"],
            ValueSchema = new CapabilityValueSchema { Kind = CapabilityValueKind.Enumeration, AllowedValues = ["off", "on"] }
        });
        Require(missingState.Disposition == WindowsCapabilityCandidatePlanDisposition.MissingCurrentState
                && missingState.Candidates.Count == 0,
            "Candidate planning must not invent a baseline when the current capability state is unknown.");

        var opaque = planner.Build(new WindowsPerformanceCapability
        {
            CapabilityId = "test.opaque",
            Availability = CapabilityAvailability.Available,
            CurrentValue = "opaque-current",
            ValueSchema = new CapabilityValueSchema { Kind = CapabilityValueKind.Opaque }
        });
        Require(opaque.Disposition == WindowsCapabilityCandidatePlanDisposition.NoCandidateSpace
                && opaque.Candidates.Count == 0,
            "Opaque capabilities without explicit adapter values must not receive invented candidates.");

        Console.WriteLine("PASS Track 2 Windows capability candidate planner supported-space/no-recommendation contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
