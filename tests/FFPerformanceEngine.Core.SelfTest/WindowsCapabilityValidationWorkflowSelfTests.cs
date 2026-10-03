using FFPerformanceEngine.Core.SystemOptimization;

internal static class WindowsCapabilityValidationWorkflowSelfTests
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "ffpe-validation-workflow-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var store = new WindowsCapabilityValidatedEvidenceStore(Path.Combine(root, "validated.json"));
            var challengeCalls = 0;
            var expected = new WindowsCapabilityValidatedEvidence
            {
                CapabilityId = "windows.cpu.boost_policy",
                BaselineValue = "balanced",
                CandidateTarget = "aggressive",
                MachineFingerprintId = "machine-a",
                WorkloadKey = "bluestacks:pie64:free-fire-max",
                ObservationCount = 3,
                Consistency = 1.0,
                MeanFpsRelativeDelta = 0.05,
                MeanFrameTimeRelativeImprovement = 0.04,
                MeanLatencyRelativeImprovement = 0.03,
                ValidatedAt = DateTimeOffset.UtcNow
            };

            var workflow = new WindowsCapabilityValidationWorkflowService(
                async (pending, token) =>
                {
                    token.ThrowIfCancellationRequested();
                    await Task.Yield();
                    challengeCalls++;
                    Require(pending.Disposition == WindowsCapabilityValidationDisposition.PendingValidation,
                        "Validation workflow must pass the original PendingValidation decision to the fresh challenge.");
                    return expected;
                },
                store);

            var pending = new WindowsCapabilityValidationDecision
            {
                CapabilityId = expected.CapabilityId,
                BaselineValue = expected.BaselineValue,
                CandidateTarget = expected.CandidateTarget,
                MachineFingerprintId = expected.MachineFingerprintId,
                WorkloadKey = expected.WorkloadKey,
                ObservationCount = 2,
                Consistency = 1.0,
                EvidenceVerdict = WindowsCapabilityEvidenceVerdict.Beneficial,
                Disposition = WindowsCapabilityValidationDisposition.PendingValidation,
                Reason = "ready"
            };

            var validated = await workflow.ValidateAndPersistAsync(pending);
            Require(challengeCalls == 1 && validated.Id == expected.Id,
                "Validation workflow must execute the fresh challenge exactly once and return its validated evidence.");

            var persisted = await store.GetLatestAsync(
                expected.CapabilityId,
                expected.BaselineValue,
                expected.CandidateTarget,
                expected.MachineFingerprintId,
                expected.WorkloadKey);
            Require(persisted?.Id == expected.Id,
                "Validation workflow must not return ValidatedEvidence before the exact evidence record is durable.");

            var failingStore = new WindowsCapabilityValidatedEvidenceStore(Path.Combine(root, "invalid", "validated.json"));
            var invalidWorkflow = new WindowsCapabilityValidationWorkflowService(
                (_, _) => Task.FromResult(expected with { MachineFingerprintId = "" }),
                failingStore);
            await RequireThrowsAsync<ArgumentException>(() => invalidWorkflow.ValidateAndPersistAsync(pending));
            Require((await failingStore.LoadAsync()).Count == 0,
                "A validation result rejected by the durable store must never appear as persisted ValidatedEvidence.");

            Console.WriteLine("PASS Track 2 Windows capability validation workflow challenge+durable-store contract");
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

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
