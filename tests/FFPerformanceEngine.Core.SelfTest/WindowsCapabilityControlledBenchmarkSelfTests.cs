using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;

internal static class WindowsCapabilityControlledBenchmarkSelfTests
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "ffpe-windows-ab-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            const string id = "test.windows.ab";
            var registry = new WindowsPerformanceCapabilityRegistry(
            [
                new WindowsPerformanceCapability
                {
                    CapabilityId = id,
                    Name = "controlled A/B capability",
                    Domain = CapabilityDomain.Cpu,
                    Availability = CapabilityAvailability.Available,
                    CurrentValue = "baseline",
                    PersistenceScope = CapabilityPersistenceScope.PersistentAllowed,
                    Safety = ActionSafety.LobbySafe
                }
            ]);
            var adapter = new StatefulAdapter(id, "baseline");
            var adapters = new WindowsCapabilityMutationAdapterRegistry([adapter]);
            var transaction = new SystemOptimizationTransactionEngine(
                registry,
                adapters,
                new SnapshotService(Path.Combine(root, "snapshots.json")),
                new HistoryService(Path.Combine(root, "history.json")));
            var lease = new RecordingLeaseManager();
            var probe = new StateAwareProbe(adapter);
            var service = new WindowsCapabilityControlledBenchmarkService(
                transaction,
                adapters,
                lease,
                probe);

            var candidate = new WindowsCapabilityCandidate
            {
                CapabilityId = id,
                TargetValue = "candidate",
                Source = WindowsCapabilityCandidateSource.ExplicitMetadata,
                ExplorationRank = 1
            };

            var result = await service.RunAsync(candidate);

            Require(result.CapabilityId == id
                    && result.BaselineValue == "baseline"
                    && result.CandidateTarget == "candidate",
                "Controlled Windows A/B must bind the exact capability, freshly read baseline state and candidate target into its result.");
            Require(result.Comparison.Baseline.Quality == PerformanceEvidenceQuality.Measured
                    && result.Comparison.Candidate.Quality == PerformanceEvidenceQuality.Measured
                    && Math.Abs((result.Comparison.Metrics.AverageFpsDelta ?? 0) - 20) < 0.001,
                "Controlled Windows A/B must return fully measured frozen PerformanceABComparison evidence from baseline and candidate windows.");
            Require(probe.ObservedStates.SequenceEqual(new[] { "baseline", "candidate" }, StringComparer.Ordinal),
                "The benchmark probe must measure A before mutation and B only while the candidate transaction is active.");
            Require(adapter.State == "baseline",
                "A successful controlled capability benchmark must restore the exact pre-test Windows state before returning.");
            Require(lease.AcquireCount == 1 && lease.DisposeCount == 1,
                "The complete A→B→restore sequence must live under one global controlled benchmark lease.");
            Require(registry.GetAll().Single().RecommendedValue is null
                    && registry.GetAll().Single().Recommendation.Source == CapabilityRecommendationSource.Unknown,
                "Producing controlled A/B evidence must not silently publish a persistent recommendation.");

            probe.ThrowOnCandidate = true;
            var failed = false;
            try
            {
                await service.RunAsync(candidate);
            }
            catch (InvalidOperationException exception) when (exception.Message.Contains("candidate probe failure", StringComparison.Ordinal))
            {
                failed = true;
            }

            Require(failed,
                "The test setup must observe the candidate measurement failure instead of swallowing it.");
            Require(adapter.State == "baseline",
                "Candidate measurement failure must still restore the exact baseline Windows state.");
            Require(lease.AcquireCount == 2 && lease.DisposeCount == 2,
                "A failed controlled benchmark must also release the global benchmark lease after recovery.");

            Console.WriteLine("PASS Track 2 Windows capability controlled A/B lease/transaction/evidence/restore contract");
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    private sealed class StatefulAdapter(string capabilityId, string initialState) : IWindowsCapabilityMutationAdapter
    {
        public string CapabilityId { get; } = capabilityId;
        public string State { get; private set; } = initialState;

        public Task<WindowsCapabilityReadResult> ReadCurrentAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(WindowsCapabilityReadResult.Ok(State));
        }

        public WindowsCapabilityValidationResult Validate(string targetValue, SystemOptimizationScope scope)
            => scope == SystemOptimizationScope.Session && targetValue is "baseline" or "candidate"
                ? WindowsCapabilityValidationResult.Ok()
                : WindowsCapabilityValidationResult.Fail("unsupported test target");

        public Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new WindowsCapabilityMutationSnapshot(CapabilityId, State, State));
        }

        public Task<WindowsCapabilityApplyResult> ApplyAsync(string targetValue, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            State = targetValue;
            return Task.FromResult(WindowsCapabilityApplyResult.Ok());
        }

        public Task<bool> VerifyAsync(string targetValue, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(string.Equals(State, targetValue, StringComparison.Ordinal));
        }

        public Task RollbackAsync(WindowsCapabilityMutationSnapshot snapshot, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            State = snapshot.RestorePayload;
            return Task.CompletedTask;
        }
    }

    private sealed class StateAwareProbe(StatefulAdapter adapter) : IWindowsCapabilityBenchmarkProbe
    {
        private int _captureIndex;
        public List<string> ObservedStates { get; } = new();
        public bool ThrowOnCandidate { get; set; }

        public Task<PerformanceIntervalSummary> CaptureAsync(
            WindowsCapabilityBenchmarkContext context,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ObservedStates.Add(adapter.State);
            if (context.Phase == WindowsCapabilityBenchmarkPhase.Candidate && ThrowOnCandidate)
                throw new InvalidOperationException("candidate probe failure");

            var fps = adapter.State == "candidate" ? 120d : 100d;
            var frameTime = adapter.State == "candidate" ? 8.33 : 10.0;
            var start = new DateTimeOffset(2026, 9, 7, 14, 0, _captureIndex++ * 2, TimeSpan.Zero);
            var entries = new[]
            {
                Telemetry(start, fps, frameTime),
                Telemetry(start.AddSeconds(1), fps, frameTime)
            };
            return Task.FromResult(PerformanceIntervalAnalysis.Analyze(entries, start, start.AddSeconds(1)));
        }

        private static PerformanceTimelineEntry Telemetry(DateTimeOffset timestamp, double fps, double frameTime)
            => new()
            {
                Timestamp = timestamp,
                Kind = PerformanceTimelineKind.Telemetry,
                Title = "Windows controlled benchmark",
                Detail = "Measured",
                Telemetry = new TelemetrySample
                {
                    Timestamp = timestamp,
                    Fps = fps,
                    FrameTimeMs = frameTime,
                    LatencyMs = 5,
                    DataQuality = "Measured"
                }
            };
    }

    private sealed class RecordingLeaseManager : IControlledBenchmarkLeaseManager
    {
        public int AcquireCount { get; private set; }
        public int DisposeCount { get; private set; }

        public Task<IAsyncDisposable> AcquireAsync(string owner, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(owner)) throw new InvalidOperationException("owner missing");
            AcquireCount++;
            return Task.FromResult<IAsyncDisposable>(new Lease(this));
        }

        private sealed class Lease(RecordingLeaseManager owner) : IAsyncDisposable
        {
            private int _disposed;

            public ValueTask DisposeAsync()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0) owner.DisposeCount++;
                return ValueTask.CompletedTask;
            }
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
