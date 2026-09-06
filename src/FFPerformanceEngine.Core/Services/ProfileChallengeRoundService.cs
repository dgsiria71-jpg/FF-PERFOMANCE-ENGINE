using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public enum ProfileChallengeRoundStage
{
    Validating,
    ApplyingBaseline,
    PreparingBaseline,
    MeasuringBaseline,
    CleaningBaseline,
    ApplyingCandidate,
    PreparingCandidate,
    MeasuringCandidate,
    CleaningCandidate,
    RestoringBaseline,
    SavingEvidence,
    Completed
}

public sealed record ProfileChallengeAutomationProgress(
    ProfileChallengeRoundStage Stage,
    string Message,
    int AcceptedSamples = 0,
    int RequiredSamples = 2);

public sealed record ProfileChallengeRoundResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int BaselineAcceptedSamples { get; init; }
    public int CandidateAcceptedSamples { get; init; }
    public Guid? ComparisonId { get; init; }
}

public sealed record ProfileChallengeRoundPolicy
{
    public int RequiredSamplesPerSide { get; init; } = 2;
    public int MaxAttemptsPerSide { get; init; } = 4;
    public int MinimumPresentMonFrames { get; init; } = 120;
    public double MaximumFpsCoefficientOfVariation { get; init; } = 0.08;
}

public sealed class ProfileChallengeRoundService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> InstanceGates = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Regex PresentMonFrameCount = new(@"(?<count>\d+)\s+frames?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly ProfileService _profiles;
    private readonly HistoryService _history;
    private readonly IAutoTunerRuntimeFactory _runtimeFactory;
    private readonly ProfileChallengeRoundPolicy _policy;

    public ProfileChallengeRoundService(
        ProfileService profiles,
        HistoryService history,
        IAutoTunerRuntimeFactory runtimeFactory)
        : this(profiles, history, runtimeFactory, new ProfileChallengeRoundPolicy())
    {
    }

    public ProfileChallengeRoundService(
        ProfileService profiles,
        HistoryService history,
        IAutoTunerRuntimeFactory runtimeFactory,
        ProfileChallengeRoundPolicy policy)
    {
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _history = history ?? throw new ArgumentNullException(nameof(history));
        _runtimeFactory = runtimeFactory ?? throw new ArgumentNullException(nameof(runtimeFactory));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        ValidatePolicy(_policy);
    }

    public Task<ProfileChallengeRoundResult> RunAsync(
        Guid challengerProfileId,
        ProfileKind targetKind,
        EnvironmentSnapshot environment,
        BlueStacksInstance instance,
        CancellationToken cancellationToken = default)
        => RunAsync(challengerProfileId, targetKind, environment, instance, null, cancellationToken);

    public async Task<ProfileChallengeRoundResult> RunAsync(
        Guid challengerProfileId,
        ProfileKind targetKind,
        EnvironmentSnapshot environment,
        BlueStacksInstance instance,
        Action<ProfileChallengeAutomationProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(instance);
        if (!ProfileChallengeEvaluator.IsWinnerRole(targetKind))
            throw new ArgumentOutOfRangeException(nameof(targetKind), targetKind, "Custom profiles may challenge only generated winner roles.");
        if (string.IsNullOrWhiteSpace(instance.Name))
            throw new ArgumentException("A named BlueStacks instance is required for an automated challenge round.", nameof(instance));

        progress?.Invoke(new(ProfileChallengeRoundStage.Validating, "Validando perfis, ambiente e compatibilidade da rodada A/B."));
        var profiles = await _profiles.LoadAsync(cancellationToken).ConfigureAwait(false);
        var challenger = profiles.FirstOrDefault(profile => profile.Id == challengerProfileId)
            ?? throw new KeyNotFoundException($"Challenger profile '{challengerProfileId:D}' was not found.");
        if (challenger.Kind != ProfileKind.Custom || challenger.Evidence != EvidenceLevel.Validated)
            throw new InvalidOperationException("Only a validated Custom profile may run an automated winner challenge round.");
        if (challenger.SourceComparisonId is null || string.IsNullOrWhiteSpace(challenger.EnvironmentFingerprint))
            throw new InvalidOperationException("The challenger has no auditable validated source configuration.");

        var incumbent = profiles.FirstOrDefault(profile =>
            profile.Kind == targetKind
            && profile.Evidence == EvidenceLevel.Validated
            && profile.Game == challenger.Game
            && string.Equals(profile.InstanceName, challenger.InstanceName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"There is no validated {targetKind} winner for the challenger's game and BlueStacks instance.");

        if (!string.Equals(instance.Name, challenger.InstanceName, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The selected BlueStacks instance does not match the challenger profile.");
        if (challenger.Game is not (GameKind.FreeFire or GameKind.FreeFireMax))
            throw new InvalidOperationException("Automated challenge rounds require Free Fire or Free Fire MAX.");
        if (challenger.Dpi != instance.Dpi || incumbent.Dpi != instance.Dpi)
            throw new InvalidOperationException("This automated runtime does not mutate DPI yet. Both profiles must use the currently detected instance DPI for exact evidence binding.");

        var challengerEnvironment = PerformanceEnvironmentFingerprint.Capture(environment, ProfileInstance(instance, challenger), challenger.Game);
        if (!string.Equals(challengerEnvironment.Id, challenger.EnvironmentFingerprint, StringComparison.OrdinalIgnoreCase)
            || !challengerEnvironment.IsStructurallyCompatible(environment))
            throw new InvalidOperationException("The current machine/Windows/BlueStacks environment no longer matches the validated Custom profile. Revalidate before running a challenge.");

        var gate = InstanceGates.GetOrAdd(instance.Name, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        var runtime = _runtimeFactory.Create(instance);
        var baselineAccepted = Array.Empty<TelemetrySample>();
        var candidateAccepted = Array.Empty<TelemetrySample>();
        try
        {
            var baselineResult = await MeasureSideAsync(
                runtime,
                incumbent,
                challenger.Game,
                ProfileChallengeRoundStage.ApplyingBaseline,
                ProfileChallengeRoundStage.PreparingBaseline,
                ProfileChallengeRoundStage.MeasuringBaseline,
                ProfileChallengeRoundStage.CleaningBaseline,
                progress,
                cancellationToken).ConfigureAwait(false);
            baselineAccepted = baselineResult.Samples;
            if (!baselineResult.Success)
                return Fail(baselineResult.Message, baselineAccepted.Length, 0);

            var candidateResult = await MeasureSideAsync(
                runtime,
                challenger,
                challenger.Game,
                ProfileChallengeRoundStage.ApplyingCandidate,
                ProfileChallengeRoundStage.PreparingCandidate,
                ProfileChallengeRoundStage.MeasuringCandidate,
                ProfileChallengeRoundStage.CleaningCandidate,
                progress,
                cancellationToken).ConfigureAwait(false);
            candidateAccepted = candidateResult.Samples;
            if (!candidateResult.Success)
                return Fail(candidateResult.Message, baselineAccepted.Length, candidateAccepted.Length);

            var baselineConfiguration = CaptureProfileConfiguration(environment, instance, incumbent);
            var candidateConfiguration = CaptureProfileConfiguration(environment, instance, challenger);
            var baselineEvidence = CreateEvidence($"A · {incumbent.Name}", baselineAccepted, baselineConfiguration);
            var candidateEvidence = CreateEvidence($"B · {challenger.Name}", candidateAccepted, candidateConfiguration);
            if (baselineEvidence.Quality != PerformanceEvidenceQuality.Measured
                || candidateEvidence.Quality != PerformanceEvidenceQuality.Measured)
                return Fail("The accepted benchmark windows could not be normalized into fully measured A/B evidence.", baselineAccepted.Length, candidateAccepted.Length);

            progress?.Invoke(new(ProfileChallengeRoundStage.SavingEvidence, "Salvando a rodada A/B observada no History.", _policy.RequiredSamplesPerSide, _policy.RequiredSamplesPerSide));
            var comparison = PerformanceABComparison.Create(baselineEvidence, candidateEvidence);
            var label = $"Desafio {WinnerName(targetKind)} · {incumbent.Name} vs {challenger.Name} · {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}";
            var saved = await _history.SavePerformanceComparisonAsync(label, comparison, CancellationToken.None).ConfigureAwait(false);

            progress?.Invoke(new(ProfileChallengeRoundStage.Completed, "Rodada A/B medida e salva. Nenhum vencedor foi alterado.", _policy.RequiredSamplesPerSide, _policy.RequiredSamplesPerSide));
            return new ProfileChallengeRoundResult
            {
                Success = true,
                Message = "Uma rodada A/B totalmente medida foi salva. Execute uma segunda rodada independente; a promoção continuará explícita.",
                BaselineAcceptedSamples = baselineAccepted.Length,
                CandidateAcceptedSamples = candidateAccepted.Length,
                ComparisonId = saved.Id
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            return Fail($"Automated challenge round stopped safely: {exception.Message}", baselineAccepted.Length, candidateAccepted.Length);
        }
        finally
        {
            try
            {
                progress?.Invoke(new(ProfileChallengeRoundStage.RestoringBaseline, "Restaurando a configuração original do BlueStacks."));
                await runtime.RestoreBaselineAsync(CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                gate.Release();
            }
        }
    }

    private async Task<(bool Success, string Message, TelemetrySample[] Samples)> MeasureSideAsync(
        IAutoTunerRuntime runtime,
        PerformanceProfile profile,
        GameKind game,
        ProfileChallengeRoundStage applyingStage,
        ProfileChallengeRoundStage preparingStage,
        ProfileChallengeRoundStage measuringStage,
        ProfileChallengeRoundStage cleaningStage,
        Action<ProfileChallengeAutomationProgress>? progress,
        CancellationToken cancellationToken)
    {
        var applied = false;
        var accepted = new List<TelemetrySample>(_policy.RequiredSamplesPerSide);
        try
        {
            progress?.Invoke(new(applyingStage, $"Aplicando {profile.Name}."));
            var apply = await runtime.ApplyCandidateAsync(ToCandidate(profile), cancellationToken).ConfigureAwait(false);
            if (!apply.Success) return (false, $"{profile.Name} could not be applied: {apply.Message}", accepted.ToArray());
            applied = true;

            progress?.Invoke(new(preparingStage, $"Abrindo/preparando {game} para {profile.Name}."));
            var prepared = await runtime.PrepareGameAsync(game, cancellationToken).ConfigureAwait(false);
            if (!prepared.Success) return (false, $"{profile.Name} game preparation failed: {prepared.Message}", accepted.ToArray());

            for (var attempt = 1; attempt <= _policy.MaxAttemptsPerSide && accepted.Count < _policy.RequiredSamplesPerSide; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Invoke(new(measuringStage,
                    $"Medindo {profile.Name}: tentativa {attempt}/{_policy.MaxAttemptsPerSide} · {accepted.Count}/{_policy.RequiredSamplesPerSide} aceita(s).",
                    accepted.Count,
                    _policy.RequiredSamplesPerSide));
                var sample = await runtime.CaptureBenchmarkAsync(cancellationToken).ConfigureAwait(false);
                if (!IsAcceptableCapture(sample)) continue;
                accepted.Add(sample!);
            }

            if (accepted.Count < _policy.RequiredSamplesPerSide)
                return (false, $"{profile.Name} did not produce {_policy.RequiredSamplesPerSide} acceptable PresentMon windows.", accepted.ToArray());
            if (FpsCoefficientOfVariation(accepted) > _policy.MaximumFpsCoefficientOfVariation)
                return (false, $"{profile.Name} benchmark windows were too variable for a controlled A/B round.", accepted.ToArray());

            return (true, $"{profile.Name} measured.", accepted.ToArray());
        }
        finally
        {
            if (applied)
            {
                progress?.Invoke(new(cleaningStage, $"Encerrando a sessão controlada de {profile.Name} e revertendo o candidato."));
                await runtime.CompleteCandidateAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    private bool IsAcceptableCapture(TelemetrySample? sample)
    {
        if (sample is null
            || sample.Fps is not double fps || !double.IsFinite(fps) || fps <= 0
            || sample.FrameTimeMs is not double frameTime || !double.IsFinite(frameTime) || frameTime <= 0
            || string.IsNullOrWhiteSpace(sample.DataQuality))
            return false;

        var match = PresentMonFrameCount.Match(sample.DataQuality);
        return match.Success
               && int.TryParse(match.Groups["count"].Value, out var frames)
               && frames >= _policy.MinimumPresentMonFrames;
    }

    private static PerformanceEvidenceSnapshot CreateEvidence(
        string name,
        IReadOnlyList<TelemetrySample> samples,
        PerformanceConfigurationSnapshot configuration)
    {
        var points = samples.Select(sample => new PerformanceTimelinePoint
        {
            Timestamp = sample.Timestamp,
            Fps = sample.Fps,
            FrameTimeMs = sample.FrameTimeMs,
            LatencyMs = sample.LatencyMs,
            DataQuality = "Measured"
        }).ToArray();
        var start = points.Min(point => point.Timestamp);
        var end = points.Max(point => point.Timestamp);
        var interval = new PerformanceIntervalSummary
        {
            Start = start,
            End = end,
            TelemetrySamples = points.Length,
            FpsEvidenceSamples = points.Count(point => point.Fps is double value && double.IsFinite(value)),
            AverageFps = points.Where(point => point.Fps is double value && double.IsFinite(value)).Average(point => point.Fps!.Value),
            AverageFrameTimeMs = points.Where(point => point.FrameTimeMs is double value && double.IsFinite(value)).Average(point => point.FrameTimeMs!.Value),
            Points = points
        };
        return PerformanceEvidenceSnapshot.Capture(name, interval, DateTimeOffset.UtcNow, configuration);
    }

    private static PerformanceConfigurationSnapshot CaptureProfileConfiguration(
        EnvironmentSnapshot environment,
        BlueStacksInstance detectedInstance,
        PerformanceProfile profile)
        => PerformanceConfigurationSnapshot.Capture(environment, ProfileInstance(detectedInstance, profile), profile.Game)
           ?? throw new InvalidOperationException($"{profile.Name} could not be represented as an exact performance configuration snapshot.");

    private static BlueStacksInstance ProfileInstance(BlueStacksInstance detected, PerformanceProfile profile)
        => detected with
        {
            CpuCores = profile.CpuCores,
            RamMb = profile.RamMb,
            Renderer = profile.Renderer,
            Fps = profile.FpsTarget,
            Resolution = profile.Resolution,
            Dpi = profile.Dpi
        };

    private static TuningCandidate ToCandidate(PerformanceProfile profile)
        => new()
        {
            CpuCores = profile.CpuCores,
            RamMb = profile.RamMb,
            Renderer = profile.Renderer,
            FpsTarget = profile.FpsTarget,
            Resolution = profile.Resolution
        };

    private static double FpsCoefficientOfVariation(IReadOnlyList<TelemetrySample> samples)
    {
        var values = samples
            .Select(sample => sample.Fps)
            .Where(value => value is double finite && double.IsFinite(finite) && finite > 0)
            .Select(value => value!.Value)
            .ToArray();
        if (values.Length < 2) return double.PositiveInfinity;
        var mean = values.Average();
        if (mean <= 0) return double.PositiveInfinity;
        var variance = values.Sum(value => Math.Pow(value - mean, 2)) / values.Length;
        return Math.Sqrt(variance) / mean;
    }

    private static ProfileChallengeRoundResult Fail(string message, int baselineAccepted, int candidateAccepted)
        => new()
        {
            Success = false,
            Message = message,
            BaselineAcceptedSamples = baselineAccepted,
            CandidateAcceptedSamples = candidateAccepted
        };

    private static string WinnerName(ProfileKind kind)
        => kind switch
        {
            ProfileKind.Recommended => "Recomendado",
            ProfileKind.MaximumFps => "Máximo FPS",
            ProfileKind.LowestLatency => "Menor Latência",
            ProfileKind.Stability => "Estabilidade",
            ProfileKind.Quality => "Qualidade",
            _ => kind.ToString()
        };

    private static void ValidatePolicy(ProfileChallengeRoundPolicy policy)
    {
        if (policy.RequiredSamplesPerSide < 2) throw new ArgumentOutOfRangeException(nameof(policy.RequiredSamplesPerSide));
        if (policy.MaxAttemptsPerSide < policy.RequiredSamplesPerSide) throw new ArgumentOutOfRangeException(nameof(policy.MaxAttemptsPerSide));
        if (policy.MinimumPresentMonFrames < 2) throw new ArgumentOutOfRangeException(nameof(policy.MinimumPresentMonFrames));
        if (!double.IsFinite(policy.MaximumFpsCoefficientOfVariation) || policy.MaximumFpsCoefficientOfVariation <= 0 || policy.MaximumFpsCoefficientOfVariation > 1)
            throw new ArgumentOutOfRangeException(nameof(policy.MaximumFpsCoefficientOfVariation));
    }
}
