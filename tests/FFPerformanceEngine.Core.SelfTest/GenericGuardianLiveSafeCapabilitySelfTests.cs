using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;
using FFPerformanceEngine.Core.Telemetry;

internal static class GenericGuardianLiveSafeCapabilitySelfTests
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "dg-guardian-safety-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            const string gameId = "game.safety-test";
            var descriptors = new[]
            {
                Capability("test.live", ActionSafety.LiveSafe),
                Capability("test.other-live", ActionSafety.LiveSafe),
                Capability("test.lobby", ActionSafety.LobbySafe),
                Capability("test.restart", ActionSafety.RestartRequired)
            };
            var registry = new WindowsPerformanceCapabilityRegistry(descriptors);
            var transactions = new SystemOptimizationTransactionEngine(
                registry,
                new WindowsCapabilityMutationAdapterRegistry(Array.Empty<IWindowsCapabilityMutationAdapter>()),
                new SnapshotService(Path.Combine(root, "snapshots.json")),
                new HistoryService(Path.Combine(root, "history.json")));
            var captures = 0;
            var capture = new PerformanceCaptureCoordinator(
                (_, _, _) => Task.FromResult<TelemetrySample?>(null),
                typedCapture: (_, _, _) =>
                {
                    captures++;
                    return Task.FromResult<TelemetryFrame?>(null);
                });
            var candidate = new GenericGuardianSessionActionCandidate
            {
                GameId = gameId,
                Family = GuardianAnomalyKind.CpuContention,
                Action = new GuardianAction
                {
                    Id = "test.explicit-live-action",
                    Description = "Explicitly declared live-safe action",
                    Safety = ActionSafety.LiveSafe
                }
            };
            var catalog = new GenericGuardianSessionMutationCatalog(
            [
                new GenericGuardianSessionMutationDefinition(
                    gameId, GuardianAnomalyKind.CpuContention, candidate.Action.Id,
                    new WindowsMutationRequest("test.live", "performance"))
            ]);
            var executor = new GenericGuardianWindowsSessionCanaryExecutor(
                transactions, capture, new GenericGuardianTypedCanaryOutcomeEvaluator(),
                catalog, TimeSpan.FromMilliseconds(50));
            var eligibility = new GenericGuardianSessionActionEligibility
            {
                State = new GuardianWorkloadStateSnapshot
                {
                    State = GuardianWorkloadState.Active,
                    Confidence = GuardianWorkloadStateConfidence.High,
                    Target = new TelemetryWorkloadTarget
                    {
                        GameId = gameId,
                        ProcessId = 4242,
                        ExecutablePath = @"C:\Games\safety-test.exe",
                        BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess
                    }
                },
                Family = GuardianAnomalyKind.CpuContention,
                EligibleCandidates = Array.AsReadOnly(new[] { candidate })
            };

            foreach (var forbidden in new[] { "test.other-live", "test.lobby", "test.restart", "test.unknown" })
            {
                var result = await executor.ExecuteAsync(eligibility, new GenericGuardianWindowsSessionActionBinding
                {
                    Candidate = candidate,
                    Mutation = new WindowsMutationRequest(forbidden, "performance")
                });
                Require(!result.Attempted && !result.Kept && result.ActiveLease is null,
                    $"Different, unsafe or unknown capability {forbidden} must not execute this action.");
                Require(captures == 0,
                    $"Unbound capability {forbidden} must fail preflight BEFORE measurement; captures={captures}.");
            }

            foreach (var mutation in new[]
            {
                new WindowsMutationRequest("test.live", "different-target"),
                new WindowsMutationRequest("test.live", "performance", "fabricated-current")
            })
            {
                var result = await executor.ExecuteAsync(eligibility, new GenericGuardianWindowsSessionActionBinding
                {
                    Candidate = candidate, Mutation = mutation
                });
                Require(!result.Attempted && captures == 0,
                    "Different target or tampered expected-state precondition must be rejected before capture.");
            }

            var exactMutation = new WindowsMutationRequest("test.live", "performance");
            Require(catalog.IsAuthorized(candidate, exactMutation)
                    && transactions.IsLiveSafeSessionCapability(exactMutation.CapabilityId),
                "Exactly registered action and actual LiveSafe session capability remain independently authorized.");
            var allowed = await executor.ExecuteAsync(eligibility, new GenericGuardianWindowsSessionActionBinding
            {
                Candidate = candidate,
                Mutation = exactMutation
            });
            Require(!allowed.Attempted && !allowed.Kept && allowed.ActiveLease is null && captures == 0,
                "Even the authorized LiveSafe mutation must not capture without the independent comparison source and lifecycle epoch.");
            Console.WriteLine("PASS Track 6 Guardian action mapping rejects unrelated/unsafe/altered mutation and defaults closed without comparison evidence");
        }
        finally
        {
            try { Directory.Delete(root, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static WindowsPerformanceCapability Capability(string id, ActionSafety safety) => new()
    {
        CapabilityId = id,
        Name = id,
        Description = "Guardian capability authorization regression",
        Domain = CapabilityDomain.System,
        Availability = CapabilityAvailability.Available,
        PersistenceScope = CapabilityPersistenceScope.SessionOnly,
        Safety = safety,
        RiskLevel = CapabilityRiskLevel.Safe
    };

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
