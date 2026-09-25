using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;

internal static class AutoTunerTypedValidationSelfTests
{
    internal static async Task RunAsync()
    {
        await UsesTypedPresentMonAndRejectsTooShortCapture();
        await RejectsMissingOrPartialTypedAuthority();
        await KeepsHighVarianceTypedCandidateObserved();
        await TypedCancellationStillForcesCleanupAndRestore();
        Console.WriteLine("PASS Auto Tuner typed PresentMon validation, repeatability, no-legacy authority and cancellation-safe cleanup");
    }

    private static async Task UsesTypedPresentMonAndRejectsTooShortCapture()
    {
        var runtime = new TypedValidationRuntime([
            Frame(100, acceptedFrames: 20),
            Frame(100, acceptedFrames: 1200),
            Frame(102, acceptedFrames: 1224)
        ]);
        var coordinator = new AutoTunerRunCoordinator(new AutoTunerEngine(), runtime, Policy());

        var result = await coordinator.RunAsync(GameKind.FreeFire, AutoTunerMode.Adaptive, [Candidate()]);

        Require(runtime.TypedCaptureCount == 3 && runtime.LegacyCaptureCount == 0,
            "Auto Tuner benchmark authority must consume typed TelemetryFrame captures directly and never fall back to legacy DataQuality text.");
        Require(result.Evidence.Count == 1 && result.Evidence[0].Evidence == EvidenceLevel.Validated,
            "Two stable accepted typed PresentMon captures must produce validated candidate evidence.");
        Require(result.Evidence[0].Sample.Fps is > 100 and < 102,
            "Compatibility CandidateEvidence must aggregate FPS from accepted typed captures rather than legacy bait telemetry.");
        Require(result.Evidence[0].Sample.DataQuality.Contains("2 accepted", StringComparison.OrdinalIgnoreCase),
            "Compatibility CandidateEvidence must continue disclosing accepted repetition count.");
    }

    private static async Task RejectsMissingOrPartialTypedAuthority()
    {
        await RequireRejectedAsync(
            Frame(100, acceptedFrames: null),
            "missing accepted-frame count must fail closed");
        await RequireRejectedAsync(
            Frame(100, acceptedFrames: 1000, fpsQuality: TelemetryMetricQuality.Partial),
            "Partial FPS must not become benchmark authority");
        await RequireRejectedAsync(
            Frame(100, acceptedFrames: 1000, countQuality: TelemetryMetricQuality.Partial),
            "Partial accepted-frame count must not become benchmark authority");
        await RequireRejectedAsync(
            Frame(100, acceptedFrames: 1000, countCoverage: 0.5),
            "accepted-frame count coverage below one must not masquerade as exact count authority");
    }

    private static async Task RequireRejectedAsync(TelemetryFrame frame, string contract)
    {
        var runtime = new TypedValidationRuntime([frame]);
        var coordinator = new AutoTunerRunCoordinator(
            new AutoTunerEngine(),
            runtime,
            new AutoTunerValidationPolicy
            {
                AdaptiveRequiredSamples = 1,
                DeepRequiredSamples = 1,
                MaxAttemptsPerCandidate = 1,
                MinimumPresentMonFrames = 120,
                MaximumFpsCoefficientOfVariation = 0.05
            });

        var result = await coordinator.RunAsync(GameKind.FreeFire, AutoTunerMode.Adaptive, [Candidate()]);

        Require(runtime.TypedCaptureCount == 1 && runtime.LegacyCaptureCount == 0,
            $"{contract}: rejection must be decided from the typed capture without legacy fallback.");
        Require(result.Evidence.Count == 0 && result.Winners.Count == 0,
            $"{contract}: rejected typed evidence must not create candidate evidence or a winner.");
    }

    private static async Task KeepsHighVarianceTypedCandidateObserved()
    {
        var runtime = new TypedValidationRuntime([
            Frame(60, 1000),
            Frame(120, 1000),
            Frame(70, 1000),
            Frame(130, 1000)
        ]);
        var coordinator = new AutoTunerRunCoordinator(new AutoTunerEngine(), runtime, Policy());

        var result = await coordinator.RunAsync(GameKind.FreeFireMax, AutoTunerMode.Adaptive, [Candidate()]);

        Require(runtime.TypedCaptureCount == 4 && runtime.LegacyCaptureCount == 0,
            "High-variance typed evidence must consume the repeat budget without legacy capture.");
        Require(result.Evidence.Count == 1 && result.Evidence[0].Evidence == EvidenceLevel.Observed,
            "A typed candidate that never converges must remain Observed.");
        Require(result.Winners.Count == 0,
            "Unstable typed evidence must never become an Auto Tuner winner.");
    }

    private static async Task TypedCancellationStillForcesCleanupAndRestore()
    {
        using var cts = new CancellationTokenSource();
        var runtime = new TypedValidationRuntime([Frame(90, 1000)]) { CancelOnTypedCapture = cts };
        var coordinator = new AutoTunerRunCoordinator(
            new AutoTunerEngine(),
            runtime,
            new AutoTunerValidationPolicy
            {
                AdaptiveRequiredSamples = 1,
                DeepRequiredSamples = 1,
                MaxAttemptsPerCandidate = 1,
                MinimumPresentMonFrames = 120
            });

        var canceled = false;
        try
        {
            await coordinator.RunAsync(GameKind.FreeFire, AutoTunerMode.Adaptive, [Candidate()], cancellationToken: cts.Token);
        }
        catch (OperationCanceledException)
        {
            canceled = true;
        }

        Require(canceled, "Cancellation from the typed benchmark path must propagate to the caller.");
        Require(runtime.CompleteCalled,
            "Candidate cleanup must run when typed benchmark cancellation interrupts a run.");
        Require(!runtime.CompleteReceivedCanceledToken,
            "Candidate cleanup must continue using a non-canceled token after typed benchmark cancellation.");
        Require(runtime.RestoreCalled,
            "Final baseline restoration must run after typed benchmark cancellation.");
    }

    private static AutoTunerValidationPolicy Policy()
        => new()
        {
            AdaptiveRequiredSamples = 2,
            DeepRequiredSamples = 3,
            MaxAttemptsPerCandidate = 4,
            MinimumPresentMonFrames = 120,
            MaximumFpsCoefficientOfVariation = 0.05
        };

    private static TuningCandidate Candidate()
        => new()
        {
            CpuCores = 4,
            RamMb = 4096,
            Renderer = "Auto",
            FpsTarget = 90,
            Resolution = "1280x720"
        };

    private static TelemetryFrame Frame(
        double fps,
        double? acceptedFrames,
        TelemetryMetricQuality fpsQuality = TelemetryMetricQuality.Measured,
        TelemetryMetricQuality countQuality = TelemetryMetricQuality.Measured,
        double countCoverage = 1d)
    {
        var metrics = new List<TelemetryMetricObservation>
        {
            Direct(TelemetryStandardMetrics.FrameFpsAverage, fps, fpsQuality, 1d),
            Direct(TelemetryStandardMetrics.FrameFpsLow1, fps * 0.85, TelemetryMetricQuality.Measured, 1d),
            Direct(TelemetryStandardMetrics.FrameTimeAverageMs, 1000d / fps, TelemetryMetricQuality.Measured, 1d),
            Direct(TelemetryStandardMetrics.FrameTimeP95Ms, 1000d / (fps * 0.80), TelemetryMetricQuality.Measured, 1d),
            Direct(TelemetryStandardMetrics.FrameStutterPercent, 1d, TelemetryMetricQuality.Measured, 1d)
        };
        if (acceptedFrames is double count)
        {
            metrics.Add(Direct(
                TelemetryStandardMetrics.FrameAcceptedSampleCount,
                count,
                countQuality,
                countCoverage));
        }

        return new TelemetryFrame(DateTimeOffset.UtcNow, metrics);
    }

    private static TelemetryMetricObservation Direct(
        TelemetryMetricDescriptor descriptor,
        double value,
        TelemetryMetricQuality quality,
        double coverage)
        => new(
            descriptor,
            value,
            quality,
            coverage,
            "presentmon",
            TelemetryMetricOrigin.Direct);

    private sealed class TypedValidationRuntime(IEnumerable<TelemetryFrame?> frames) : IAutoTunerRuntime
    {
        private readonly Queue<TelemetryFrame?> _frames = new(frames);

        public int TypedCaptureCount { get; private set; }
        public int LegacyCaptureCount { get; private set; }
        public bool CompleteCalled { get; private set; }
        public bool CompleteReceivedCanceledToken { get; private set; }
        public bool RestoreCalled { get; private set; }
        public CancellationTokenSource? CancelOnTypedCapture { get; init; }

        public Task<AutoTunerRuntimeResult> ApplyCandidateAsync(
            TuningCandidate candidate,
            CancellationToken cancellationToken = default)
            => Task.FromResult(AutoTunerRuntimeResult.Ok("applied"));

        public Task<AutoTunerRuntimeResult> PrepareGameAsync(
            GameKind game,
            CancellationToken cancellationToken = default)
            => Task.FromResult(AutoTunerRuntimeResult.Ok("prepared"));

        public Task<TelemetrySample?> CaptureBenchmarkAsync(CancellationToken cancellationToken = default)
        {
            LegacyCaptureCount++;
            return Task.FromResult<TelemetrySample?>(new TelemetrySample
            {
                Timestamp = DateTimeOffset.UtcNow,
                Fps = 777,
                OnePercentLow = 770,
                FrameTimeMs = 1.2,
                DataQuality = "PresentMon · 9999 frames"
            });
        }

        public Task<TelemetryFrame?> CaptureBenchmarkFrameAsync(CancellationToken cancellationToken = default)
        {
            TypedCaptureCount++;
            if (CancelOnTypedCapture is not null)
            {
                CancelOnTypedCapture.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
            }

            return Task.FromResult(_frames.Count == 0 ? null : _frames.Dequeue());
        }

        public Task CompleteCandidateAsync(CancellationToken cancellationToken = default)
        {
            CompleteCalled = true;
            CompleteReceivedCanceledToken = cancellationToken.IsCancellationRequested;
            return Task.CompletedTask;
        }

        public Task RestoreBaselineAsync(CancellationToken cancellationToken = default)
        {
            RestoreCalled = true;
            return Task.CompletedTask;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
