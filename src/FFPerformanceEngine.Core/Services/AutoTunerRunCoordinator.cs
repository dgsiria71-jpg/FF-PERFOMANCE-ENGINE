using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using FFPerformanceEngine.Core.Models;

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
    Task<TelemetrySample?> CaptureBenchmarkAsync(CancellationToken cancellationToken = default);
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
    private static readonly Regex PresentMonFrameCount = new(@"(?<count>\d+)\s+frames?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly AutoTunerEngine _engine;
    private readonly IAutoTunerRuntime _runtime;
    private readonly AutoTunerValidationPolicy _validation;

    public AutoTunerRunCoordinator(AutoTunerEngine engine, IAutoTunerRuntime runtime)
        : this(engine, runtime, new AutoTunerValidationPolicy())
    {
    }

    public AutoTunerRunCoordinator(AutoTunerEngine engine, IAutoTunerRuntime runtime, AutoTunerValidationPolicy validation)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _validation = validation ?? throw new ArgumentNullException(nameof(validation));
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

                    var accepted = new List<TelemetrySample>(_validation.RequiredSamples(mode));
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

                        var sample = await _runtime.CaptureBenchmarkAsync(cancellationToken).ConfigureAwait(false);
                        if (!IsAcceptableCapture(sample, _validation.MinimumPresentMonFrames, out var rejectionReason))
                        {
                            progress?.Invoke(new(
                                AutoTunerRunStage.ValidatingBenchmark,
                                candidateNumber,
                                candidates.Count,
                                $"Capture {attempts} rejected and will be repeated: {rejectionReason}"));
                            continue;
                        }

                        accepted.Add(sample!);
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

                    var aggregated = AggregateSamples(accepted, attempts, stable);
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

                        // User cancellation must never cancel rollback/cleanup after a restart-required candidate was applied.
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

    private static bool IsAcceptableCapture(TelemetrySample? sample, int minimumPresentMonFrames, out string reason)
    {
        if (sample is null)
        {
            reason = "telemetry capture returned no sample";
            return false;
        }
        if (sample.Fps is null or <= 0 || double.IsNaN(sample.Fps.Value) || double.IsInfinity(sample.Fps.Value))
        {
            reason = "FPS evidence is unavailable or invalid";
            return false;
        }
        if (string.Equals(sample.DataQuality, "Unavailable", StringComparison.OrdinalIgnoreCase))
        {
            reason = "telemetry source marked the capture unavailable";
            return false;
        }

        var match = PresentMonFrameCount.Match(sample.DataQuality ?? string.Empty);
        if (match.Success && int.TryParse(match.Groups["count"].Value, out var frameCount) && frameCount < minimumPresentMonFrames)
        {
            reason = $"capture contains only {frameCount} PresentMon frames; minimum is {minimumPresentMonFrames}";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static TelemetrySample AggregateSamples(IReadOnlyList<TelemetrySample> samples, int attempts, bool stable)
    {
        var coefficient = FpsCoefficientOfVariation(samples);
        return new TelemetrySample
        {
            Timestamp = samples.Max(x => x.Timestamp),
            Fps = Average(samples.Select(x => x.Fps)),
            OnePercentLow = Average(samples.Select(x => x.OnePercentLow)),
            PointOnePercentLow = Average(samples.Select(x => x.PointOnePercentLow)),
            FrameTimeMs = Average(samples.Select(x => x.FrameTimeMs)),
            FrameTimeP95Ms = Average(samples.Select(x => x.FrameTimeP95Ms)),
            FrameTimeP99Ms = Average(samples.Select(x => x.FrameTimeP99Ms)),
            StutterPercent = Average(samples.Select(x => x.StutterPercent)),
            LatencyMs = Average(samples.Select(x => x.LatencyMs)),
            CpuPercent = Average(samples.Select(x => x.CpuPercent)),
            GpuPercent = Average(samples.Select(x => x.GpuPercent)),
            MemoryUsedGb = Average(samples.Select(x => x.MemoryUsedGb)),
            MemoryTotalGb = Average(samples.Select(x => x.MemoryTotalGb)),
            CpuTemperatureC = Average(samples.Select(x => x.CpuTemperatureC)),
            GpuTemperatureC = Average(samples.Select(x => x.GpuTemperatureC)),
            PingMs = Average(samples.Select(x => x.PingMs)),
            JitterMs = Average(samples.Select(x => x.JitterMs)),
            PacketLossPercent = Average(samples.Select(x => x.PacketLossPercent)),
            DataQuality = $"{(stable ? "Validated" : "Observed")} repeatability · {samples.Count} accepted / {attempts} attempts · FPS CV {coefficient:P1}"
        };
    }

    private static double FpsCoefficientOfVariation(IReadOnlyList<TelemetrySample> samples)
    {
        var values = samples.Select(x => x.Fps).Where(x => x is > 0).Select(x => x!.Value).ToArray();
        if (values.Length <= 1) return 0;
        var mean = values.Average();
        if (mean <= 0) return double.PositiveInfinity;
        var variance = values.Select(x => Math.Pow(x - mean, 2)).Average();
        return Math.Sqrt(variance) / mean;
    }

    private static double CalculateConfidence(IReadOnlyList<TelemetrySample> samples, bool stable)
    {
        var aggregate = AggregateSamples(samples, samples.Count, stable);
        var confidence = stable ? 0.86 : 0.52;
        confidence += Math.Min(samples.Count, 4) * 0.025;
        if (aggregate.OnePercentLow is > 0) confidence += 0.025;
        if (aggregate.FrameTimeP95Ms is > 0) confidence += 0.015;
        if (aggregate.FrameTimeP99Ms is > 0) confidence += 0.01;
        if (aggregate.LatencyMs is > 0) confidence += 0.01;
        confidence -= Math.Min(0.30, FpsCoefficientOfVariation(samples) * 0.75);
        return Math.Clamp(confidence, 0.40, 0.99);
    }

    private static double? Average(IEnumerable<double?> values)
    {
        var available = values.Where(x => x.HasValue && !double.IsNaN(x.Value) && !double.IsInfinity(x.Value)).Select(x => x!.Value).ToArray();
        return available.Length == 0 ? null : available.Average();
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
