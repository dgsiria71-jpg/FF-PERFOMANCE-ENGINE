using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.SystemOptimization;

internal static class WindowsCapabilityValidatedEvidenceResumeSelfTests
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "ffpe-validated-resume-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var store = new WindowsCapabilityValidatedEvidenceStore(Path.Combine(root, "validated.json"));
            var fingerprint = "machine-a";
            var workload = "bluestacks:pie64:free-fire-max";
            var baseline = "balanced";
            var targetSupported = true;
            var publishCount = 0;

            Task<WindowsCapabilityCandidatePlan> Plan(string capabilityId, CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                return Task.FromResult(new WindowsCapabilityCandidatePlan
                {
                    CapabilityId = capabilityId,
                    CurrentValue = baseline,
                    Disposition = WindowsCapabilityCandidatePlanDisposition.Ready,
                    Candidates = targetSupported
                        ?
                        [
                            new WindowsCapabilityCandidate
                            {
                                CapabilityId = capabilityId,
                                TargetValue = "aggressive",
                                Source = WindowsCapabilityCandidateSource.ExplicitMetadata,
                                ExplorationRank = 1
                            }
                        ]
                        : Array.Empty<WindowsCapabilityCandidate>(),
                    Reason = "fresh candidate plan"
                });
            }

            Task<CapabilityRecommendationPublicationResult> Publish(
                string capabilityId,
                string target,
                CapabilityRecommendationSummary recommendation,
                CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                publishCount++;
                return Task.FromResult(new CapabilityRecommendationPublicationResult
                {
                    Disposition = CapabilityRecommendationPublicationDisposition.Published,
                    CapabilityId = capabilityId,
                    Reason = "published"
                });
            }

            var service = new WindowsCapabilityValidatedRecommendationService(
                store,
                Plan,
                () => fingerprint,
                () => workload,
                Publish);

            var older = Evidence(DateTimeOffset.UtcNow.AddMinutes(-2));
            var newer = Evidence(DateTimeOffset.UtcNow.AddMinutes(-1));
            await store.SaveAsync(older);
            await store.SaveAsync(newer);

            var resumed = await service.ResolveCurrentAsync(older.CapabilityId, older.CandidateTarget);
            Require(resumed?.Id == newer.Id,
                "Resume must return the newest durable ValidatedEvidence for the currently supported exact tuple.");
            Require(publishCount == 0,
                "Resolving durable ValidatedEvidence for UI resume must be read-only and never publish a recommendation.");

            fingerprint = "machine-b";
            Require(await service.ResolveCurrentAsync(older.CapabilityId, older.CandidateTarget) is null,
                "Machine fingerprint drift must hide durable evidence from resume instead of surfacing a stale publish action.");
            fingerprint = "machine-a";

            workload = "bluestacks:pie64:free-fire";
            Require(await service.ResolveCurrentAsync(older.CapabilityId, older.CandidateTarget) is null,
                "Workload drift must hide durable evidence from resume.");
            workload = "bluestacks:pie64:free-fire-max";

            baseline = "aggressive";
            Require(await service.ResolveCurrentAsync(older.CapabilityId, older.CandidateTarget) is null,
                "Baseline drift must hide durable evidence from resume.");
            baseline = "balanced";

            targetSupported = false;
            Require(await service.ResolveCurrentAsync(older.CapabilityId, older.CandidateTarget) is null,
                "A target removed from the fresh candidate space must hide durable evidence from resume.");
            targetSupported = true;

            Require(await service.ResolveCurrentAsync(older.CapabilityId, "unsupported") is null,
                "Resume must not substitute another target's evidence when the selected target has no exact durable match.");
            Require(publishCount == 0,
                "All resume rejection paths must remain read-only.");

            Console.WriteLine("PASS Track 2 durable ValidatedEvidence safe resume exact-context contract");
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    private static WindowsCapabilityValidatedEvidence Evidence(DateTimeOffset validatedAt)
        => new()
        {
            CapabilityId = "windows.cpu.boost_policy",
            BaselineValue = "balanced",
            CandidateTarget = "aggressive",
            MachineFingerprintId = "machine-a",
            WorkloadKey = "bluestacks:pie64:free-fire-max",
            ObservationCount = 3,
            Consistency = 0.98,
            MeanFpsRelativeDelta = 0.05,
            MeanFrameTimeRelativeImprovement = 0.04,
            MeanLatencyRelativeImprovement = 0.03,
            ValidatedAt = validatedAt,
            Source = WindowsCapabilityValidatedEvidenceSource.ValidatedEvidence
        };

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
