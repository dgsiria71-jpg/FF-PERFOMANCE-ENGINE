using FFPerformanceEngine.Core.SystemOptimization;

internal static class WindowsCapabilityValidatedEvidenceStoreSelfTests
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "ffpe-validated-evidence-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "validated.json");
            var store = new WindowsCapabilityValidatedEvidenceStore(path);
            var evidence = Evidence("machine-a", "workload-a", DateTimeOffset.UtcNow.AddMinutes(-1), 3, 1.0);

            await store.SaveAsync(evidence);
            var loaded = await store.GetLatestAsync(
                evidence.CapabilityId,
                evidence.BaselineValue,
                evidence.CandidateTarget,
                "machine-a",
                "workload-a");
            Require(loaded is not null
                    && loaded.Id == evidence.Id
                    && loaded.Source == WindowsCapabilityValidatedEvidenceSource.ValidatedEvidence
                    && loaded.ObservationCount == 3,
                "ValidatedEvidence must persist and reload with its exact evidence identity/provenance.");
            if (loaded is null)
                throw new InvalidOperationException("ValidatedEvidence unexpectedly disappeared after the persistence assertion.");
            Require(loaded.RecommendedValue is null,
                "Persisted ValidatedEvidence must remain evidence, not a recommendation.");

            var newer = Evidence("machine-a", "workload-a", DateTimeOffset.UtcNow, 4, 1.0);
            await store.SaveAsync(newer);
            var latest = await store.GetLatestAsync(
                evidence.CapabilityId,
                evidence.BaselineValue,
                evidence.CandidateTarget,
                "machine-a",
                "workload-a");
            Require(latest?.Id == newer.Id && latest.ObservationCount == 4,
                "Latest validated evidence for the exact tuple must be retrievable without deleting historical validation records.");

            await store.SaveAsync(Evidence("machine-b", "workload-a", DateTimeOffset.UtcNow.AddSeconds(1), 5, 1.0));
            var machineA = await store.GetLatestAsync(
                evidence.CapabilityId,
                evidence.BaselineValue,
                evidence.CandidateTarget,
                "machine-a",
                "workload-a");
            Require(machineA?.Id == newer.Id,
                "Validated evidence from another machine fingerprint must never replace the local tuple's latest record.");

            var reloadedStore = new WindowsCapabilityValidatedEvidenceStore(path);
            var all = await reloadedStore.LoadAsync();
            Require(all.Count == 3,
                "ValidatedEvidence history must survive store recreation for History/freshness inspection.");

            await RequireThrowsAsync<ArgumentException>(() => store.SaveAsync(evidence with { MachineFingerprintId = "" }));
            Require((await store.LoadAsync()).Count == 3,
                "Invalid validated evidence must be rejected atomically without corrupting persisted history.");

            Console.WriteLine("PASS Track 2 Windows capability ValidatedEvidence durable exact-tuple store contract");
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    private static WindowsCapabilityValidatedEvidence Evidence(
        string machine,
        string workload,
        DateTimeOffset validatedAt,
        int observations,
        double consistency)
        => new()
        {
            CapabilityId = "windows.cpu.boost_policy",
            BaselineValue = "balanced",
            CandidateTarget = "aggressive",
            MachineFingerprintId = machine,
            WorkloadKey = workload,
            ObservationCount = observations,
            Consistency = consistency,
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
