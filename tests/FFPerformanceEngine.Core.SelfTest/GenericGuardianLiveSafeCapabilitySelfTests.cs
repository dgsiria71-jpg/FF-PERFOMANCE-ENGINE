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
            var executor = new GenericGuardianWindowsSessionCanaryExecutor(
                transactions, capture, new GenericGuardianTypedCanaryOutcomeEvaluator(),
                TimeSpan.FromMilliseconds(50));
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

            foreach (var forbidden in new[] { "test.lobby", "test.restart", "test.unknown" })
            {
                var result = await executor.ExecuteAsync(eligibility, new GenericGuardianWindowsSessionActionBinding
                {
                    Candidate = candidate,
                    Mutation = new WindowsMutationRequest(forbidden, "performance")
                });
                Require(!result.Attempted && !result.Kept && result.ActiveLease is null,
                    $"LiveSafe action metadata must not authorize unsafe or unknown capability {forbidden}.");
                Require(captures == 0,
                    $"Unsafe or unknown capability {forbidden} must fail preflight before any measurement or mutation.");
            }

            var allowed = await executor.ExecuteAsync(eligibility, new GenericGuardianWindowsSessionActionBinding
            {
                Candidate = candidate,
                Mutation = new WindowsMutationRequest("test.live", "performance")
            });
            Require(!allowed.Attempted && captures == 1,
                "A genuinely LiveSafe known capability must reach typed before-capture; absent evidence must still block mutation.");
            Console.WriteLine("PASS Track 6 Guardian capability safety: unsafe and unknown Windows mutations fail before capture");
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
