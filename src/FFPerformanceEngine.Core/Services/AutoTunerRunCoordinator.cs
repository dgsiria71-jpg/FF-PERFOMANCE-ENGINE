using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public enum AutoTunerRunStage
{
    ApplyingCandidate,
    PreparingGame,
    Benchmarking,
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

public sealed class AutoTunerRunCoordinator
{
    private readonly AutoTunerEngine _engine;
    private readonly IAutoTunerRuntime _runtime;

    public AutoTunerRunCoordinator(AutoTunerEngine engine, IAutoTunerRuntime runtime)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
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

                    progress?.Invoke(new(
                        AutoTunerRunStage.Benchmarking,
                        candidateNumber,
                        candidates.Count,
                        $"Measuring candidate {candidateNumber}."));

                    var sample = await _runtime.CaptureBenchmarkAsync(cancellationToken).ConfigureAwait(false);
                    if (sample?.Fps is not > 0) continue;

                    evidence.Add(new CandidateEvidence
                    {
                        Candidate = candidate,
                        Sample = sample,
                        Evidence = EvidenceLevel.Validated,
                        Confidence = CalculateConfidence(sample)
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
                        await _runtime.CompleteCandidateAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }
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
            throw new InvalidOperationException("Auto Tuner could not restore the pre-tuning baseline.", restoreFailure);

        var result = _engine.SelectWinners(game, mode, evidence);
        progress?.Invoke(new(
            AutoTunerRunStage.Completed,
            candidates.Count,
            candidates.Count,
            result.Summary));
        return result;
    }

    private static double CalculateConfidence(TelemetrySample sample)
    {
        var confidence = 0.90;
        if (sample.OnePercentLow is > 0) confidence += 0.03;
        if (sample.FrameTimeP95Ms is > 0) confidence += 0.02;
        if (sample.FrameTimeP99Ms is > 0) confidence += 0.02;
        if (!string.Equals(sample.DataQuality, "Unavailable", StringComparison.OrdinalIgnoreCase)) confidence += 0.01;
        return Math.Min(0.99, confidence);
    }
}
