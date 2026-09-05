using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class AutoTunerValidationSelfTests
{
    [ModuleInitializer]
    internal static void Run()
    {
        RunAsync().GetAwaiter().GetResult();
    }

    private static async Task RunAsync()
    {
        await RepeatsRejectedCaptureAndValidatesStableSamples();
        await KeepsHighVarianceCandidateOutOfWinnerSelection();
        await CleanupIgnoresCanceledRunToken();
        Console.WriteLine("PASS repeatable benchmark validation and cancellation-safe cleanup");
    }

    private static async Task RepeatsRejectedCaptureAndValidatesStableSamples()
    {
        var runtime = new ValidationRuntime([
            new TelemetrySample { Fps = 100, OnePercentLow = 85, FrameTimeMs = 10, DataQuality = "PresentMon · 20 frames" },
            new TelemetrySample { Fps = 100, OnePercentLow = 88, FrameTimeMs = 10.0, FrameTimeP95Ms = 11.4, StutterPercent = 1.2, DataQuality = "PresentMon · 1200 frames" },
            new TelemetrySample { Fps = 102, OnePercentLow = 89, FrameTimeMs = 9.8, FrameTimeP95Ms = 11.0, StutterPercent = 1.0, DataQuality = "PresentMon · 1224 frames" }
        ]);
        var policy = new AutoTunerValidationPolicy
        {
            AdaptiveRequiredSamples = 2,
            DeepRequiredSamples = 3,
            MaxAttemptsPerCandidate = 4,
            MinimumPresentMonFrames = 120,
            MaximumFpsCoefficientOfVariation = 0.05
        };
        var coordinator = new AutoTunerRunCoordinator(new AutoTunerEngine(), runtime, policy);

        var result = await coordinator.RunAsync(GameKind.FreeFire, AutoTunerMode.Adaptive, [Candidate()]);

        Require(runtime.CaptureCount == 3, "A contaminated/too-short capture must be discarded and automatically repeated.");
        Require(result.Evidence.Count == 1, "Stable repeated samples must produce one aggregated candidate evidence record.");
        Require(result.Evidence[0].Evidence == EvidenceLevel.Validated, "Stable repeated measurements must be validated.");
        Require(result.Evidence[0].Sample.Fps is > 100 and < 102, "Validated evidence must aggregate accepted repetitions rather than using one lucky capture.");
        Require(result.Evidence[0].Sample.DataQuality.Contains("2 accepted", StringComparison.OrdinalIgnoreCase), "Aggregated sample must disclose repetition count in data quality.");
    }

    private static async Task KeepsHighVarianceCandidateOutOfWinnerSelection()
    {
        var runtime = new ValidationRuntime([
            Valid(60), Valid(120), Valid(70), Valid(130)
        ]);
        var policy = new AutoTunerValidationPolicy
        {
            AdaptiveRequiredSamples = 2,
            DeepRequiredSamples = 3,
            MaxAttemptsPerCandidate = 4,
            MinimumPresentMonFrames = 120,
            MaximumFpsCoefficientOfVariation = 0.05
        };
        var coordinator = new AutoTunerRunCoordinator(new AutoTunerEngine(), runtime, policy);

        var result = await coordinator.RunAsync(GameKind.FreeFireMax, AutoTunerMode.Adaptive, [Candidate()]);

        Require(runtime.CaptureCount == 4, "High variance must consume the allowed repeat budget before the candidate is classified.");
        Require(result.Evidence.Count == 1 && result.Evidence[0].Evidence == EvidenceLevel.Observed, "A candidate that never converges must remain observed, not validated.");
        Require(result.Winners.Count == 0, "Unstable evidence must never become a winner profile.");
    }

    private static async Task CleanupIgnoresCanceledRunToken()
    {
        using var cts = new CancellationTokenSource();
        var runtime = new ValidationRuntime([Valid(90)]) { CancelOnCapture = cts };
        var coordinator = new AutoTunerRunCoordinator(new AutoTunerEngine(), runtime, new AutoTunerValidationPolicy
        {
            AdaptiveRequiredSamples = 1,
            DeepRequiredSamples = 1,
            MaxAttemptsPerCandidate = 1
        });

        try
        {
            await coordinator.RunAsync(GameKind.FreeFire, AutoTunerMode.Adaptive, [Candidate()], cancellationToken: cts.Token);
            throw new InvalidOperationException("Expected cancellation was not propagated.");
        }
        catch (OperationCanceledException)
        {
        }

        Require(runtime.CompleteCalled, "Candidate cleanup must run when benchmark cancellation interrupts a run.");
        Require(!runtime.CompleteReceivedCanceledToken, "Safety cleanup must use a non-canceled token so rollback cannot be skipped by user cancellation.");
        Require(runtime.RestoreCalled, "Final baseline restoration must run after cancellation.");
    }

    private static TelemetrySample Valid(double fps) => new()
    {
        Fps = fps,
        OnePercentLow = fps * 0.85,
        FrameTimeMs = 1000d / fps,
        FrameTimeP95Ms = 1000d / (fps * 0.80),
        StutterPercent = 1.0,
        DataQuality = "PresentMon · 1000 frames"
    };

    private static TuningCandidate Candidate() => new()
    {
        CpuCores = 4,
        RamMb = 4096,
        Renderer = "Auto",
        FpsTarget = 90,
        Resolution = "1280x720"
    };

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class ValidationRuntime(IEnumerable<TelemetrySample> samples) : IAutoTunerRuntime
    {
        private readonly Queue<TelemetrySample> _samples = new(samples);
        public int CaptureCount { get; private set; }
        public bool CompleteCalled { get; private set; }
        public bool CompleteReceivedCanceledToken { get; private set; }
        public bool RestoreCalled { get; private set; }
        public CancellationTokenSource? CancelOnCapture { get; init; }

        public Task<AutoTunerRuntimeResult> ApplyCandidateAsync(TuningCandidate candidate, CancellationToken cancellationToken = default)
            => Task.FromResult(AutoTunerRuntimeResult.Ok("applied"));

        public Task<AutoTunerRuntimeResult> PrepareGameAsync(GameKind game, CancellationToken cancellationToken = default)
            => Task.FromResult(AutoTunerRuntimeResult.Ok("prepared"));

        public Task<TelemetrySample?> CaptureBenchmarkAsync(CancellationToken cancellationToken = default)
        {
            CaptureCount++;
            if (CancelOnCapture is not null)
            {
                CancelOnCapture.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
            }
            return Task.FromResult<TelemetrySample?>(_samples.Count == 0 ? null : _samples.Dequeue());
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
}
