using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;

internal static class ProfileChallengeTypedValidationSelfTests
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "ffpe-challenge-typed-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var instance = new BlueStacksInstance
            {
                Name = "Pie64",
                AndroidVersion = "Pie 64-bit",
                CpuCores = 4,
                RamMb = 4096,
                Renderer = "Vulkan",
                Fps = 120,
                Resolution = "1600x900",
                Dpi = 240,
                AdbEnabled = true,
                AdbPort = 5555
            };
            var environment = new EnvironmentSnapshot
            {
                MachineName = "FFPE-TYPED-CHALLENGE-PC",
                WindowsDescription = "Windows 11 typed challenge",
                LogicalProcessors = 16,
                MemoryTotalGb = 32,
                Is64BitOs = true,
                ActiveGame = GameKind.FreeFireMax,
                Instances = [instance]
            };
            var challengerInstance = instance with
            {
                CpuCores = 6,
                RamMb = 6144,
                Fps = 144,
                Resolution = "1920x1080"
            };
            var challengerConfiguration = PerformanceConfigurationSnapshot.Capture(
                environment with { Instances = [challengerInstance] },
                challengerInstance,
                GameKind.FreeFireMax)!;
            var createdAt = DateTimeOffset.UtcNow.AddMinutes(-5);

            var incumbent = new PerformanceProfile
            {
                Name = "Recomendado",
                Kind = ProfileKind.Recommended,
                Game = GameKind.FreeFireMax,
                InstanceName = instance.Name,
                CpuCores = 4,
                RamMb = 4096,
                Renderer = "Vulkan",
                FpsTarget = 120,
                Resolution = "1600x900",
                Dpi = 240,
                Evidence = EvidenceLevel.Validated,
                Confidence = 0.95,
                CreatedAt = createdAt
            };
            var challenger = new PerformanceProfile
            {
                Name = "Custom Typed",
                Kind = ProfileKind.Custom,
                Game = GameKind.FreeFireMax,
                InstanceName = instance.Name,
                CpuCores = 6,
                RamMb = 6144,
                Renderer = "Vulkan",
                FpsTarget = 144,
                Resolution = "1920x1080",
                Dpi = 240,
                Evidence = EvidenceLevel.Validated,
                Confidence = 0.96,
                SourceComparisonId = Guid.NewGuid(),
                EnvironmentFingerprint = challengerConfiguration.Environment.Id,
                CreatedAt = createdAt
            };

            var profiles = new ProfileService(Path.Combine(root, "profiles.json"));
            var history = new HistoryService(Path.Combine(root, "history.json"));
            await profiles.SaveAsync([incumbent, challenger]);

            var runtime = new TypedRuntime(
                Frames(100, 10.0, 12.0),
                Frames(112, 8.9, 9.8));
            var service = new ProfileChallengeRoundService(
                profiles,
                history,
                new FakeFactory(runtime));

            var result = await service.RunAsync(
                challenger.Id,
                ProfileKind.Recommended,
                environment,
                instance,
                CancellationToken.None);

            Require(result.Success,
                "A controlled challenge with two stable direct typed PresentMon windows per side must succeed.");
            Require(runtime.TypedCaptureCount == 4,
                "Profile Challenge must consume exactly two typed benchmark windows for A and two for B.");
            Require(runtime.LegacyCaptureCount == 0,
                "Profile Challenge benchmark authority must never call legacy TelemetrySample/DataQuality capture.");
            Require(result.BaselineAcceptedSamples == 2 && result.CandidateAcceptedSamples == 2,
                "Typed A/B challenge must retain the existing two-window acceptance contract.");

            var comparisons = await history.LoadPerformanceComparisonsAsync();
            Require(comparisons.Count == 1
                    && comparisons[0].ValidationStatus == PerformanceComparisonValidationStatus.Observed
                    && comparisons[0].Baseline.Quality == PerformanceEvidenceQuality.Measured
                    && comparisons[0].Candidate.Quality == PerformanceEvidenceQuality.Measured,
                "Typed challenge evidence must remain measured-but-observed and must not auto-promote a winner.");
            Require((await profiles.LoadAsync()).Single(profile => profile.Kind == ProfileKind.Recommended).Id == incumbent.Id,
                "Migrating challenge measurement authority must not change the incumbent winner.");

            Console.WriteLine("PASS Profile Challenge uses direct typed PresentMon benchmark authority without legacy DataQuality parsing");
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    private static IReadOnlyList<TelemetryFrame> Frames(double fps, double frameTime, double latency)
        =>
        [
            Frame(fps - 1, frameTime + 0.1, latency + 0.1, 240),
            Frame(fps + 1, frameTime - 0.1, latency - 0.1, 260)
        ];

    private static TelemetryFrame Frame(double fps, double frameTime, double latency, double acceptedFrames)
        => new(
            DateTimeOffset.UtcNow,
            [
                Direct(TelemetryStandardMetrics.FrameFpsAverage, fps),
                Direct(TelemetryStandardMetrics.FrameTimeAverageMs, frameTime),
                Direct(TelemetryStandardMetrics.FrameLatencyAverageMs, latency),
                Direct(TelemetryStandardMetrics.FrameAcceptedSampleCount, acceptedFrames)
            ]);

    private static TelemetryMetricObservation Direct(TelemetryMetricDescriptor descriptor, double value)
        => new(
            descriptor,
            value,
            TelemetryMetricQuality.Measured,
            1d,
            "presentmon",
            TelemetryMetricOrigin.Direct);

    private sealed class FakeFactory(IAutoTunerRuntime runtime) : IAutoTunerRuntimeFactory
    {
        public IAutoTunerRuntime Create(BlueStacksInstance instance) => runtime;
    }

    private sealed class TypedRuntime(params IReadOnlyList<TelemetryFrame>[] sides) : IAutoTunerRuntime
    {
        private readonly Queue<IReadOnlyList<TelemetryFrame>> _sides = new(sides);
        private Queue<TelemetryFrame> _currentTyped = new();
        private Queue<TelemetrySample> _currentLegacy = new();

        public int TypedCaptureCount { get; private set; }
        public int LegacyCaptureCount { get; private set; }

        public Task<AutoTunerRuntimeResult> ApplyCandidateAsync(TuningCandidate candidate, CancellationToken cancellationToken = default)
        {
            var next = _sides.Count > 0 ? _sides.Dequeue() : Array.Empty<TelemetryFrame>();
            _currentTyped = new Queue<TelemetryFrame>(next);
            _currentLegacy = new Queue<TelemetrySample>(next.Select(frame => LegacyBait(frame)));
            return Task.FromResult(AutoTunerRuntimeResult.Ok("typed fake applied"));
        }

        public Task<AutoTunerRuntimeResult> PrepareGameAsync(GameKind game, CancellationToken cancellationToken = default)
            => Task.FromResult(AutoTunerRuntimeResult.Ok("typed fake prepared"));

        public Task<TelemetrySample?> CaptureBenchmarkAsync(CancellationToken cancellationToken = default)
        {
            LegacyCaptureCount++;
            return Task.FromResult<TelemetrySample?>(_currentLegacy.Count > 0 ? _currentLegacy.Dequeue() : null);
        }

        public Task<TelemetryFrame?> CaptureBenchmarkFrameAsync(CancellationToken cancellationToken = default)
        {
            TypedCaptureCount++;
            return Task.FromResult<TelemetryFrame?>(_currentTyped.Count > 0 ? _currentTyped.Dequeue() : null);
        }

        public Task CompleteCandidateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RestoreBaselineAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        private static TelemetrySample LegacyBait(TelemetryFrame frame)
        {
            frame.TryGetMetric(TelemetryStandardMetrics.FrameFpsAverage.Id, out var fps);
            frame.TryGetMetric(TelemetryStandardMetrics.FrameTimeAverageMs.Id, out var frameTime);
            frame.TryGetMetric(TelemetryStandardMetrics.FrameLatencyAverageMs.Id, out var latency);
            return new TelemetrySample
            {
                Timestamp = frame.Timestamp,
                Fps = fps?.Value ?? 777,
                FrameTimeMs = frameTime?.Value ?? 1.2,
                LatencyMs = latency?.Value,
                DataQuality = "PresentMon · 9999 frames"
            };
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
