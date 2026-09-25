using System.Runtime.ExceptionServices;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Telemetry;

namespace FFPerformanceEngine.Core.Services;

public enum AutoTunerRunStage
{
    ApplyingCandidate,
    PreparingGame,
    Benchmarking,
    ValidatingBenchmark,
    CleaningCandidate,
    RestoringBaseline,
    Completed
}

public sealed record AutoTunerRunProgress(
    AutoTunerRunStage Stage,
    int CandidateIndex,
    int CandidateCount,
    string Message);

public sealed record AutoTunerRuntimeResult(bool Success, string Message)
{
    public static AutoTunerRuntimeResult Ok(string message) => new(true, message);
    public static AutoTunerRuntimeResult Fail(string message) => new(false, message);
}

public interface IAutoTunerRuntime
{
    Task<AutoTunerRuntimeResult> ApplyCandidateAsync(TuningCandidate candidate, CancellationToken cancellationToken = default);
    Task<AutoTunerRuntimeResult> PrepareGameAsync(GameKind game, CancellationToken cancellationToken = default);

    // Kept temporarily for source compatibility with callers that still consume the
    // legacy sample surface. AutoTunerRunCoordinator never uses this method as
    // benchmark authority.
    Task<TelemetrySample?> CaptureBenchmarkAsync(CancellationToken cancellationToken = default);

    // Runtimes that have not migrated to typed telemetry fail closed. There is no
    // implicit conversion from DataQuality text to v2 evidence.
    Task<TelemetryFrame?> CaptureBenchmarkFrameAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<TelemetryFrame?>(null);

    Task CompleteCandidateAsync(CancellationToken cancellationToken = default);
    Task RestoreBaselineAsync(CancellationToken cancellationToken = default);
}

public sealed record AutoTunerValidationPolicy
{
    public int AdaptiveRequiredSamples { get; init; } = 2;
    public int DeepRequiredSamples { get; init; } = 3;
    public int MaxAttemptsPerCandidate { get; init; } = 5;
    public int MinimumPresentMonFrames { get; init; } = 120;
    public double MaximumFpsCoefficientOfVariation { get; init; } = 0.08;

    public int RequiredSamples(AutoTunerMode mode)
        => mode == AutoTunerMode.Deep ? DeepRequiredSamples : AdaptiveRequiredSamples;
}

public sealed class AutoTunerRunCoordinator
{
    private readonly AutoTunerEngine _engine;
    private readonly IAutoTunerRuntime _runtime;
    private readonly AutoTunerValidationPolicy _validation;
    private readonly IControlledBenchmarkLeaseManager _benchmarkLeases;

    public AutoTunerRunCoordinator(AutoTunerEngine engine, IAutoTunerRuntime runtime)
        : this(engine, runtime, new AutoTunerValidationPolicy(), new ControlledBenchmarkLeaseManager())
    {
    }

    public AutoTunerRunCoordinator(AutoTunerEngine engine, IAutoTunerRuntime runtime, AutoTunerValidationPolicy validation)
        : this(engine, runtime, validation, new ControlledBenchmarkLeaseManager())
    {
    }

    public AutoTunerRunCoordinator(
        AutoTunerEngine engine,
        IAutoTunerRuntime runtime,
        IControlledBenchmarkLeaseManager benchmarkLeases)
        : this(engine, runtime, new AutoTunerValidationPolicy(), benchmarkLeases)
    {
    }

    public AutoTunerRunCoordinator(
        AutoTunerEngine engine,
        IAutoTunerRuntime runtime,
        AutoTunerValidationPolicy validation,
        IControlledBenchmarkLeaseManager benchmarkLeases)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _validation = validation ?? throw new ArgumentNullException(nameof(validation));
        _benchmarkLeases = benchmarkLeases ?? throw new ArgumentNullException(nameof(benchmarkLeases));
        ValidatePolicy(_validation);
    }

    public async Task<TuningResult> RunAsync(
        GameKind game,
        AutoTunerMode mode,
        IReadOnlyList<TuningCandidate> candidates,
        Action<AutoTunerRunProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (game is not (GameKind.FreeFire or GameKind.FreeFireMax))
            throw new ArgumentOutOfRangeException(nameof(game), game, "Auto Tuner requires Free Fire or Free Fire MAX.");

        await using var benchmarkLease = await _benchmarkLeases
            .AcquireAsync($"Auto Tuner · {game}", cancellationToken)
            .ConfigureAwait(false);

        var evidence = new List<CandidateEvidence>(candidates.Count);
        Exception? primaryFailure = null;
        Exception? restoreFailure = null;

        try
        {
            for (var index = 0; index < candidates.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var candidate = candidates[index];
                var candidateNumber = index + 1;
                var applied = false;

                progress?.Invoke(new(
                    AutoTunerRunStage.ApplyingCandidate,
                    candidateNumber,
                    candidates.Count,
                    $"Applying candidate {candidateNumber} of {candidates.Count}."));

                var apply = await _runtime.ApplyCandidateAsync(candidate, cancellationToken).ConfigureAwait(false);
                if (!apply.Success) continue;
                applied = true;

                try
                {
                    progress?.Invoke(new(
                        AutoTunerRunStage.PreparingGame,
                        candidateNumber,
                        candidates.Count,
                        $"Preparing {game} for candidate {candidateNumber}."));

                    var prepared = await _runtime.PrepareGameAsync(game, cancellationToken).ConfigureAwait(false);
                    if (!prepared.Success) continue;

                    var accepted = new List<TelemetryFrame>(_validation.RequiredSamples(mode));
                    var attempts = 0;
                    var stable = false;
                    var requiredSamples = _validation.RequiredSamples(mode);

                    while (attempts < _validation.MaxAttemptsPerCandidate)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        attempts++;
                        progress?.Invoke(new(
                            AutoTunerRunStage.Benchmarking,
                            candidateNumber,
                            candidates.Count,
                            $"Measuring candidate {candidateNumber}: capture {attempts} of {_validation.MaxAttemptsPerCandidate}."));

                        var frame = await _runtime.CaptureBenchmarkFrameAsync(cancellationToken).ConfigureAwait(false);
                        if (!IsAcceptableCapture(frame, _validation.MinimumPresentMonFrames, out var rejectionReason))
                        {
                            progress?.Invoke(new(
                                AutoTunerRunStage.ValidatingBenchmark,
                                candidateNumber,
                                candidates.Count,
                                $"Capture {attempts} rejected and will be repeated: {rejectionReason}"));
                            continue;
                        }

                        accepted.Add(frame!);
                        stable = accepted.Count >= requiredSamples
                                 && FpsCoefficientOfVariation(accepted) <= _validation.MaximumFpsCoefficientOfVariation;

                        progress?.Invoke(new(
                            AutoTunerRunStage.ValidatingBenchmark,
                            candidateNumber,
                            candidates.Count,
                            stable
                                ? $"Candidate {candidateNumber} converged with {accepted.Count} accepted repetition(s)."
                                : $"Candidate {candidateNumber}: {accepted.Count}/{requiredSamples} accepted repetition(s), FPS CV {FpsCoefficientOfVariation(accepted):P1}."));

                        if (stable) break;
                    }

                    if (accepted.Count == 0) continue;

                    var aggregated = AggregateFrames(accepted, attempts, stable);
                    evidence.Add(new CandidateEvidence
                    {
                        Candidate = candidate,
                        Sample = aggregated,
                        Evidence = stable ? EvidenceLevel.Validated : EvidenceLevel.Observed,
                        Confidence = CalculateConfidence(accepted, stable)
                    });
                }
                finally
                {
                    if (applied)
                    {
                        progress?.Invoke(new(
                            AutoTunerRunStage.CleaningCandidate,
                            candidateNumber,
                            candidates.Count,
                            $"Cleaning candidate {candidateNumber}."));

                        await _runtime.CompleteCandidateAsync(CancellationToken.None).ConfigureAwait(false);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            primaryFailure = ex;
        }
        finally
        {
            progress?.Invoke(new(
                AutoTunerRunStage.RestoringBaseline,
                candidates.Count,
                candidates.Count,
                "Restoring pre-tuning baseline."));

            try
            {
                await _runtime.RestoreBaselineAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                restoreFailure = ex;
            }
        }

        if (restoreFailure is not null)
        {
            if (primaryFailure is not null)
                throw new AggregateException("Auto Tuner failed and the pre-tuning baseline could not be restored.", primaryFailure, restoreFailure);
            throw new InvalidOperationException("Auto Tuner could not restore the pre-tuning baseline.", restoreFailure);
        }

        if (primaryFailure is not null)
            ExceptionDispatchInfo.Capture(primaryFailure).Throw();

        var result = _engine.SelectWinners(game, mode, evidence);
        progress?.Invoke(new(
            AutoTunerRunStage.Completed,
            candidates.Count,
            candidates.Count,
            result.Summary));
        return result;
    }

    private static bool IsAcceptableCapture(
        TelemetryFrame? frame,
        int minimumPresentMonFrames,
        out string reason)
    {
        if (frame is null)
        {
            reason = "typed telemetry capture returned no frame";
            return false;
        }

        if (frame.FrameQuality != TelemetryMetricQuality.Measured)
        {
            reason = "typed telemetry frame is not fully measured";
            return false;
        }

        if (!TryGetDirectPresentMon(frame, TelemetryStandardMetrics.FrameFpsAverage, out var fps)
            || fps.Value <= 0d
            || fps.Coverage <= 0d)
        {
            reason = "measured direct PresentMon FPS evidence is unavailable or invalid";
            return false;
        }

        if (!TryGetDirectPresentMon(frame, TelemetryStandardMetrics.FrameAcceptedSampleCount, out var count))
        {
            reason = "exact accepted PresentMon frame count is unavailable";
            return false;
        }

        if (count.Coverage != 1d
            || count.Value < 0d
            || Math.Abs(count.Value - Math.Round(count.Value)) > 0.000001d)
        {
            reason = "accepted PresentMon frame count is not exact";
            return false;
        }

        var acceptedFrames = (long)Math.Round(count.Value);
        if (acceptedFrames < minimumPresentMonFrames)
        {
            reason = $"capture contains only {acceptedFrames} accepted PresentMon frames; minimum is {minimumPresentMonFrames}";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static bool TryGetDirectPresentMon(
        TelemetryFrame frame,
        TelemetryMetricDescriptor descriptor,
        out TelemetryMetricObservation observation)
    {
        observation = null!;
        if (!frame.TryGetMetric(descriptor.Id, out var found) || found is null) return false;
        if (!double.IsFinite(found.Value)
            || found.Quality != TelemetryMetricQuality.Measured
            || found.SourceId != "presentmon"
            || found.Origin != TelemetryMetricOrigin.Direct)
            return false;

        observation = found;
        return true;
    }

    private static TelemetrySample AggregateFrames(
        IReadOnlyList<TelemetryFrame> frames,
        int attempts,
        bool stable)
    {
        var coefficient = FpsCoefficientOfVariation(frames);
        return new TelemetrySample
        {
            Timestamp = frames.Max(x => x.Timestamp),
            Fps = AverageMetric(frames, TelemetryStandardMetrics.FrameFpsAverage),
            OnePercentLow = AverageMetric(frames, TelemetryStandardMetrics.FrameFpsLow1),
            PointOnePercentLow = AverageMetric(frames, TelemetryStandardMetrics.FrameFpsLow01),
            FrameTimeMs = AverageMetric(frames, TelemetryStandardMetrics.FrameTimeAverageMs),
            FrameTimeP95Ms = AverageMetric(frames, TelemetryStandardMetrics.FrameTimeP95Ms),
            FrameTimeP99Ms = AverageMetric(frames, TelemetryStandardMetrics.FrameTimeP99Ms),
            StutterPercent = AverageMetric(frames, TelemetryStandardMetrics.FrameStutterPercent),
            LatencyMs = AverageMetric(frames, TelemetryStandardMetrics.FrameLatencyAverageMs),
            CpuPercent = AverageMetric(frames, TelemetryStandardMetrics.SystemCpuUtilizationPercent),
            GpuPercent = AverageMetric(frames, TelemetryStandardMetrics.SystemGpuUtilizationPercent),
            MemoryUsedGb = AverageMetric(frames, TelemetryStandardMetrics.SystemMemoryUsedGb),
            MemoryTotalGb = AverageMetric(frames, TelemetryStandardMetrics.SystemMemoryTotalGb),
            CpuTemperatureC = AverageMetric(frames, TelemetryStandardMetrics.CpuTemperatureCelsius),
            GpuTemperatureC = AverageMetric(frames, TelemetryStandardMetrics.GpuTemperatureCelsius),
            PingMs = AverageMetric(frames, TelemetryStandardMetrics.NetworkPingMs),
            JitterMs = AverageMetric(frames, TelemetryStandardMetrics.NetworkJitterMs),
            PacketLossPercent = AverageMetric(frames, TelemetryStandardMetrics.NetworkPacketLossPercent),
            DataQuality = $"{(stable ? "Validated" : "Observed")} repeatability · {frames.Count} accepted / {attempts} attempts · FPS CV {coefficient:P1}"
        };
    }

    private static double FpsCoefficientOfVariation(IReadOnlyList<TelemetryFrame> frames)
    {
        var values = frames
            .Select(frame => ReadMeasuredValue(frame, TelemetryStandardMetrics.FrameFpsAverage))
            .Where(value => value is > 0d)
            .Select(value => value!.Value)
            .ToArray();
        if (values.Length <= 1) return 0d;
        var mean = values.Average();
        if (mean <= 0d) return double.PositiveInfinity;
        var variance = values.Select(value => Math.Pow(value - mean, 2)).Average();
        return Math.Sqrt(variance) / mean;
    }

    private static double CalculateConfidence(IReadOnlyList<TelemetryFrame> frames, bool stable)
    {
        var aggregate = AggregateFrames(frames, frames.Count, stable);
        var confidence = stable ? 0.86 : 0.52;
        confidence += Math.Min(frames.Count, 4) * 0.025;
        if (aggregate.OnePercentLow is > 0) confidence += 0.025;
        if (aggregate.FrameTimeP95Ms is > 0) confidence += 0.015;
        if (aggregate.FrameTimeP99Ms is > 0) confidence += 0.01;
        if (aggregate.LatencyMs is > 0) confidence += 0.01;
        confidence -= Math.Min(0.30, FpsCoefficientOfVariation(frames) * 0.75);
        return Math.Clamp(confidence, 0.40, 0.99);
    }

    private static double? AverageMetric(
        IReadOnlyList<TelemetryFrame> frames,
        TelemetryMetricDescriptor descriptor)
    {
        var values = frames
            .Select(frame => ReadMeasuredValue(frame, descriptor))
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToArray();
        return values.Length == 0 ? null : values.Average();
    }

    private static double? ReadMeasuredValue(
        TelemetryFrame frame,
        TelemetryMetricDescriptor descriptor)
    {
        if (!frame.TryGetMetric(descriptor.Id, out var observation) || observation is null) return null;
        return observation.Quality == TelemetryMetricQuality.Measured && double.IsFinite(observation.Value)
            ? observation.Value
            : null;
    }

    private static void ValidatePolicy(AutoTunerValidationPolicy policy)
    {
        if (policy.AdaptiveRequiredSamples <= 0) throw new ArgumentOutOfRangeException(nameof(policy.AdaptiveRequiredSamples));
        if (policy.DeepRequiredSamples <= 0) throw new ArgumentOutOfRangeException(nameof(policy.DeepRequiredSamples));
        if (policy.MaxAttemptsPerCandidate <= 0) throw new ArgumentOutOfRangeException(nameof(policy.MaxAttemptsPerCandidate));
        if (policy.MaxAttemptsPerCandidate < Math.Max(policy.AdaptiveRequiredSamples, policy.DeepRequiredSamples))
            throw new ArgumentException("MaxAttemptsPerCandidate must allow the configured required sample count.", nameof(policy));
        if (policy.MinimumPresentMonFrames < 0) throw new ArgumentOutOfRangeException(nameof(policy.MinimumPresentMonFrames));
        if (policy.MaximumFpsCoefficientOfVariation is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(policy.MaximumFpsCoefficientOfVariation));
    }
}
