using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.SystemOptimization;

internal static class WindowsCapabilityValidatedRecommendationSelfTests
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "ffpe-validated-recommendation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var store = new WindowsCapabilityValidatedEvidenceStore(Path.Combine(root, "validated.json"));
            var fingerprint = "machine-a";
            var workload = "bluestacks:pie64:free-fire-max";
            var baseline = "balanced";
            var targetSupported = true;
            var publishCount = 0;
            CapabilityRecommendationSummary? publishedRecommendation = null;
            string? publishedCapability = null;
            string? publishedTarget = null;

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
                publishedCapability = capabilityId;
                publishedTarget = target;
                publishedRecommendation = recommendation;
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

            var evidence = Evidence(DateTimeOffset.UtcNow.AddMinutes(-1));
            await store.SaveAsync(evidence);

            var result = await service.PublishAsync(evidence);
            Require(result.IsPublished && publishCount == 1,
                "The latest exact ValidatedEvidence must publish exactly once after all fresh gates pass.");
            Require(publishedCapability == evidence.CapabilityId
                    && publishedTarget == evidence.CandidateTarget,
                "ValidatedEvidence promotion must preserve the exact capability and target tuple.");
            Require(publishedRecommendation is not null
                    && publishedRecommendation.Source == CapabilityRecommendationSource.ValidatedEvidence
                    && Math.Abs(publishedRecommendation.Confidence - evidence.Consistency) < 0.0001
                    && publishedRecommendation.MachineFingerprintId == evidence.MachineFingerprintId
                    && publishedRecommendation.GeneratedAt == evidence.ValidatedAt,
                "ValidatedEvidence promotion must preserve validation provenance instead of synthesizing ControlledEvidence or a fresh unbound timestamp.");

            fingerprint = "machine-b";
            await RequireThrowsAsync<InvalidOperationException>(() => service.PublishAsync(evidence));
            Require(publishCount == 1,
                "Machine fingerprint drift must reject promotion before recommendation publication.");
            fingerprint = "machine-a";

            workload = "bluestacks:pie64:free-fire";
            await RequireThrowsAsync<InvalidOperationException>(() => service.PublishAsync(evidence));
            Require(publishCount == 1,
                "Workload drift must reject promotion before recommendation publication.");
            workload = "bluestacks:pie64:free-fire-max";

            baseline = "aggressive";
            await RequireThrowsAsync<InvalidOperationException>(() => service.PublishAsync(evidence));
            Require(publishCount == 1,
                "Current baseline drift must reject promotion before recommendation publication.");
            baseline = "balanced";

            targetSupported = false;
            await RequireThrowsAsync<InvalidOperationException>(() => service.PublishAsync(evidence));
            Require(publishCount == 1,
                "A target removed from the freshly supported candidate space must not become a recommendation.");
            targetSupported = true;

            var newer = Evidence(DateTimeOffset.UtcNow);
            await store.SaveAsync(newer);
            await RequireThrowsAsync<InvalidOperationException>(() => service.PublishAsync(evidence));
            Require(publishCount == 1,
                "Older ValidatedEvidence must become stale as soon as a newer validation exists for the same exact tuple.");

            var latestResult = await service.PublishAsync(newer);
            Require(latestResult.IsPublished && publishCount == 2,
                "The newest ValidatedEvidence for the exact tuple must remain promotable after freshness checks.");

            var unpersisted = Evidence(DateTimeOffset.UtcNow.AddMinutes(1));
            await RequireThrowsAsync<InvalidOperationException>(() => service.PublishAsync(unpersisted));
            Require(publishCount == 2,
                "In-memory ValidatedEvidence that was never durably stored must never publish a persistent recommendation.");

            Console.WriteLine("PASS Track 2 latest ValidatedEvidence -> fresh persistent recommendation promotion contract");
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

    private static async Task RequireThrowsAsync<TException>(Func<Task> action) where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name}, but the operation completed successfully.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
