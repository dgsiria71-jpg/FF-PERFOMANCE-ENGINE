using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public enum ProfileChallengeVerdict
{
    Inconclusive,
    IncumbentHolds,
    ChallengerWins
}

public enum ProfileChallengeStatus
{
    InsufficientEvidence,
    EnvironmentDrift,
    Inconclusive,
    IncumbentHeld,
    Promoted
}

public sealed record ProfileChallengeResult
{
    public bool Promoted { get; init; }
    public ProfileChallengeStatus Status { get; init; }
    public ProfileKind TargetKind { get; init; }
    public int EvidenceRounds { get; init; }
    public string Message { get; init; } = string.Empty;
    public Guid? PromotedProfileId { get; init; }
}

public static class ProfileChallengeEvaluator
{
    public static ProfileChallengeVerdict Evaluate(
        ProfileKind targetKind,
        PerformanceEvidenceSnapshot baseline,
        PerformanceEvidenceSnapshot challenger)
    {
        ArgumentNullException.ThrowIfNull(baseline);
        ArgumentNullException.ThrowIfNull(challenger);
        if (!IsWinnerRole(targetKind)) return ProfileChallengeVerdict.Inconclusive;

        var a = PerformanceEvidenceSnapshot.Rehydrate(baseline);
        var b = PerformanceEvidenceSnapshot.Rehydrate(challenger);
        if (a.Quality != PerformanceEvidenceQuality.Measured
            || b.Quality != PerformanceEvidenceQuality.Measured
            || a.TelemetrySamples < 2
            || b.TelemetrySamples < 2
            || a.Configuration is null
            || b.Configuration is null
            || !a.Configuration.Environment.IsStructurallyEquivalentTo(b.Configuration.Environment))
            return ProfileChallengeVerdict.Inconclusive;

        var fpsGain = RelativeGain(a.AverageFps, b.AverageFps);
        var frameTimeImprovement = RelativeImprovement(a.AverageFrameTimeMs, b.AverageFrameTimeMs);
        var latencyImprovement = RelativeImprovement(a.AverageLatencyMs, b.AverageLatencyMs);
        var oneLowGain = RelativeGain(OnePercentLow(a), OnePercentLow(b));

        return targetKind switch
        {
            ProfileKind.MaximumFps => WinsMaximumFps(fpsGain, frameTimeImprovement),
            ProfileKind.LowestLatency => WinsLowestLatency(fpsGain, latencyImprovement),
            ProfileKind.Stability => WinsStability(a, b, oneLowGain),
            ProfileKind.Quality => WinsQuality(a, b, fpsGain, frameTimeImprovement),
            ProfileKind.Recommended => WinsRecommended(fpsGain, oneLowGain, frameTimeImprovement, latencyImprovement),
            _ => ProfileChallengeVerdict.Inconclusive
        };
    }

    internal static double? OnePercentLow(PerformanceEvidenceSnapshot snapshot)
    {
        var values = snapshot.Interval.Points
            .Select(point => point.Fps)
            .Where(value => value is double number && double.IsFinite(number) && number > 0)
            .Select(value => value!.Value)
            .OrderBy(value => value)
            .ToArray();
        if (values.Length == 0) return null;
        var count = Math.Max(1, (int)Math.Ceiling(values.Length * 0.01));
        return values.Take(count).Average();
    }

    private static ProfileChallengeVerdict WinsMaximumFps(double? fpsGain, double? frameTimeImprovement)
        => fpsGain is >= 0.01 && (frameTimeImprovement is null or >= -0.02)
            ? ProfileChallengeVerdict.ChallengerWins
            : fpsGain is <= 0
                ? ProfileChallengeVerdict.IncumbentHolds
                : ProfileChallengeVerdict.Inconclusive;

    private static ProfileChallengeVerdict WinsLowestLatency(double? fpsGain, double? latencyImprovement)
        => latencyImprovement is >= 0.02 && (fpsGain is null or >= -0.02)
            ? ProfileChallengeVerdict.ChallengerWins
            : latencyImprovement is <= 0
                ? ProfileChallengeVerdict.IncumbentHolds
                : ProfileChallengeVerdict.Inconclusive;

    private static ProfileChallengeVerdict WinsStability(
        PerformanceEvidenceSnapshot baseline,
        PerformanceEvidenceSnapshot challenger,
        double? oneLowGain)
    {
        var aVariation = FrameTimeCoefficientOfVariation(baseline);
        var bVariation = FrameTimeCoefficientOfVariation(challenger);
        if (oneLowGain is null || aVariation is null || bVariation is null)
            return ProfileChallengeVerdict.Inconclusive;

        var variabilityImprovement = aVariation.Value <= 0
            ? (bVariation.Value <= 0 ? 0 : double.NegativeInfinity)
            : (aVariation.Value - bVariation.Value) / aVariation.Value;
        if ((variabilityImprovement >= 0.05 && oneLowGain >= 0)
            || (oneLowGain >= 0.03 && variabilityImprovement >= -0.02))
            return ProfileChallengeVerdict.ChallengerWins;
        if (oneLowGain < 0 && variabilityImprovement < 0)
            return ProfileChallengeVerdict.IncumbentHolds;
        return ProfileChallengeVerdict.Inconclusive;
    }

    private static ProfileChallengeVerdict WinsQuality(
        PerformanceEvidenceSnapshot baseline,
        PerformanceEvidenceSnapshot challenger,
        double? fpsGain,
        double? frameTimeImprovement)
    {
        var aPixels = ResolutionPixels(baseline.Configuration!.Resolution);
        var bPixels = ResolutionPixels(challenger.Configuration!.Resolution);
        if (aPixels <= 0 || bPixels <= 0) return ProfileChallengeVerdict.Inconclusive;

        if (bPixels > aPixels
            && fpsGain is >= -0.05
            && frameTimeImprovement is null or >= -0.05)
            return ProfileChallengeVerdict.ChallengerWins;
        if (bPixels < aPixels && fpsGain is <= 0)
            return ProfileChallengeVerdict.IncumbentHolds;

        return bPixels == aPixels
            ? WinsRecommended(fpsGain, RelativeGain(OnePercentLow(baseline), OnePercentLow(challenger)), frameTimeImprovement,
                RelativeImprovement(baseline.AverageLatencyMs, challenger.AverageLatencyMs))
            : ProfileChallengeVerdict.Inconclusive;
    }

    private static ProfileChallengeVerdict WinsRecommended(
        double? fpsGain,
        double? oneLowGain,
        double? frameTimeImprovement,
        double? latencyImprovement)
    {
        if (HasMajorRegression(fpsGain, oneLowGain, frameTimeImprovement, latencyImprovement))
            return ProfileChallengeVerdict.IncumbentHolds;

        var weighted = new List<(double Value, double Weight)>();
        Add(weighted, fpsGain, 0.35);
        Add(weighted, oneLowGain, 0.25);
        Add(weighted, frameTimeImprovement, 0.20);
        Add(weighted, latencyImprovement, 0.20);
        if (weighted.Count == 0) return ProfileChallengeVerdict.Inconclusive;

        var score = weighted.Sum(item => item.Value * item.Weight) / weighted.Sum(item => item.Weight);
        if (score >= 0.015) return ProfileChallengeVerdict.ChallengerWins;
        if (score <= 0) return ProfileChallengeVerdict.IncumbentHolds;
        return ProfileChallengeVerdict.Inconclusive;
    }

    private static bool HasMajorRegression(params double?[] changes)
        => changes.Any(change => change is < -0.05);

    private static void Add(List<(double Value, double Weight)> values, double? value, double weight)
    {
        if (value is double finite && double.IsFinite(finite)) values.Add((finite, weight));
    }

    private static double? RelativeGain(double? baseline, double? candidate)
        => baseline is double a && candidate is double b && double.IsFinite(a) && double.IsFinite(b) && a > 0
            ? (b - a) / a
            : null;

    private static double? RelativeImprovement(double? baseline, double? candidate)
        => baseline is double a && candidate is double b && double.IsFinite(a) && double.IsFinite(b) && a > 0
            ? (a - b) / a
            : null;

    private static double? FrameTimeCoefficientOfVariation(PerformanceEvidenceSnapshot snapshot)
    {
        var values = snapshot.Interval.Points
            .Select(point => point.FrameTimeMs)
            .Where(value => value is double number && double.IsFinite(number) && number > 0)
            .Select(value => value!.Value)
            .ToArray();
        if (values.Length < 2) return null;
        var mean = values.Average();
        if (mean <= 0) return null;
        var variance = values.Sum(value => Math.Pow(value - mean, 2)) / values.Length;
        return Math.Sqrt(variance) / mean;
    }

    private static long ResolutionPixels(string resolution)
    {
        var parts = resolution.Split('x', 'X');
        return parts.Length == 2
               && int.TryParse(parts[0], out var width)
               && int.TryParse(parts[1], out var height)
            ? (long)width * height
            : 0;
    }

    internal static bool IsWinnerRole(ProfileKind kind)
        => kind is ProfileKind.Recommended
            or ProfileKind.MaximumFps
            or ProfileKind.LowestLatency
            or ProfileKind.Stability
            or ProfileKind.Quality;
}

public sealed class ProfileChallengeService
{
    private readonly ProfileService _profiles;
    private readonly HistoryService _history;

    public ProfileChallengeService(ProfileService profiles, HistoryService history)
    {
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _history = history ?? throw new ArgumentNullException(nameof(history));
    }

    public async Task<ProfileChallengeResult> AssessAndPromoteLatestAsync(
        Guid challengerProfileId,
        ProfileKind targetKind,
        EnvironmentSnapshot currentEnvironment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currentEnvironment);
        if (!ProfileChallengeEvaluator.IsWinnerRole(targetKind))
            throw new ArgumentOutOfRangeException(nameof(targetKind), targetKind, "Custom profiles may challenge only generated winner roles.");

        var profiles = await _profiles.LoadAsync(cancellationToken).ConfigureAwait(false);
        var challenger = profiles.FirstOrDefault(profile => profile.Id == challengerProfileId)
            ?? throw new KeyNotFoundException($"Challenger profile '{challengerProfileId:D}' was not found.");
        if (challenger.Kind != ProfileKind.Custom || challenger.Evidence != EvidenceLevel.Validated)
            throw new InvalidOperationException("Only a validated Custom profile may challenge an existing winner.");
        if (challenger.SourceComparisonId is null || string.IsNullOrWhiteSpace(challenger.EnvironmentFingerprint))
            throw new InvalidOperationException("The challenger has no auditable validated source configuration.");

        var incumbent = profiles.FirstOrDefault(profile =>
            profile.Kind == targetKind
            && profile.Evidence == EvidenceLevel.Validated
            && profile.Game == challenger.Game
            && string.Equals(profile.InstanceName, challenger.InstanceName, StringComparison.OrdinalIgnoreCase));
        if (incumbent is null)
            throw new InvalidOperationException($"There is no validated {targetKind} winner for the challenger's game and BlueStacks instance.");

        var evidenceNotBefore = challenger.CreatedAt > incumbent.CreatedAt
            ? challenger.CreatedAt
            : incumbent.CreatedAt;
        var comparisons = await _history.LoadPerformanceComparisonsAsync(cancellationToken).ConfigureAwait(false);
        var rounds = comparisons
            .Where(record => record.Baseline.CapturedAt >= evidenceNotBefore
                             && record.Candidate.CapturedAt >= evidenceNotBefore
                             && IsMatchingMeasuredRound(record, incumbent, challenger))
            .OrderBy(record => record.Candidate.CapturedAt)
            .ToList();

        if (rounds.Count < 2)
            return new ProfileChallengeResult
            {
                Status = ProfileChallengeStatus.InsufficientEvidence,
                TargetKind = targetKind,
                EvidenceRounds = rounds.Count,
                Message = "Two independent fully measured A/B rounds are required before a winner can be replaced."
            };

        var selected = rounds.TakeLast(2).ToArray();
        if (selected[1].Candidate.CapturedAt <= selected[0].Candidate.CapturedAt)
            return new ProfileChallengeResult
            {
                Status = ProfileChallengeStatus.InsufficientEvidence,
                TargetKind = targetKind,
                EvidenceRounds = rounds.Count,
                Message = "The revalidation round must be later than the first measured challenge round."
            };

        if (selected.Any(round => round.Candidate.Configuration is null
                                  || !round.Candidate.Configuration.Environment.IsStructurallyCompatible(currentEnvironment)))
            return new ProfileChallengeResult
            {
                Status = ProfileChallengeStatus.EnvironmentDrift,
                TargetKind = targetKind,
                EvidenceRounds = rounds.Count,
                Message = "The current machine or BlueStacks environment has structurally drifted from the measured challenge evidence."
            };

        var verdicts = selected
            .Select(round => ProfileChallengeEvaluator.Evaluate(targetKind, round.Baseline, round.Candidate))
            .ToArray();
        if (verdicts.Any(verdict => verdict == ProfileChallengeVerdict.Inconclusive))
            return new ProfileChallengeResult
            {
                Status = ProfileChallengeStatus.Inconclusive,
                TargetKind = targetKind,
                EvidenceRounds = rounds.Count,
                Message = "At least one measured challenge round was inconclusive; the incumbent was preserved."
            };
        if (verdicts.Any(verdict => verdict != ProfileChallengeVerdict.ChallengerWins))
            return new ProfileChallengeResult
            {
                Status = ProfileChallengeStatus.IncumbentHeld,
                TargetKind = targetKind,
                EvidenceRounds = rounds.Count,
                Message = "The challenger did not win both measured rounds; the incumbent was preserved."
            };

        var revalidation = selected[1];
        var evidence = PerformanceEvidenceSnapshot.Rehydrate(revalidation.Candidate);
        var configuration = evidence.Configuration!.Rehydrate();
        var promoted = new PerformanceProfile
        {
            Name = WinnerName(targetKind),
            Kind = targetKind,
            Game = configuration.Game,
            InstanceName = configuration.InstanceName,
            CpuCores = configuration.CpuCores,
            RamMb = configuration.RamMb,
            Renderer = configuration.Renderer,
            FpsTarget = configuration.FpsTarget,
            Resolution = configuration.Resolution,
            Dpi = configuration.Dpi,
            GuardianMode = challenger.GuardianMode,
            Evidence = EvidenceLevel.Validated,
            Confidence = challenger.Confidence,
            AverageFps = evidence.AverageFps,
            OnePercentLow = ProfileChallengeEvaluator.OnePercentLow(evidence),
            FrameTimeMs = evidence.AverageFrameTimeMs,
            LatencyMs = evidence.AverageLatencyMs,
            SourceComparisonId = revalidation.Id,
            EnvironmentFingerprint = configuration.Environment.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _profiles.ReplaceValidatedWinnerRoleAsync(promoted, cancellationToken).ConfigureAwait(false);
        await _history.AppendAsync(new HistoryEvent
        {
            Kind = HistoryEventKind.Profile,
            Title = $"Perfil vencedor promovido: {promoted.Name}",
            Summary = $"{challenger.Name} venceu duas rodadas A/B medidas contra {incumbent.Name}; {targetKind} foi substituído sem alterar o perfil Custom de origem.",
            DetailsJson = $"{{\"challengerProfileId\":\"{challenger.Id:D}\",\"previousWinnerId\":\"{incumbent.Id:D}\",\"promotedProfileId\":\"{promoted.Id:D}\",\"revalidationComparisonId\":\"{revalidation.Id:D}\",\"targetKind\":\"{targetKind}\"}}"
        }, cancellationToken).ConfigureAwait(false);

        return new ProfileChallengeResult
        {
            Promoted = true,
            Status = ProfileChallengeStatus.Promoted,
            TargetKind = targetKind,
            EvidenceRounds = rounds.Count,
            PromotedProfileId = promoted.Id,
            Message = $"{challenger.Name} venceu as duas rodadas medidas e foi promovido para {promoted.Name}."
        };
    }

    private static bool IsMatchingMeasuredRound(
        PerformanceComparisonHistoryRecord record,
        PerformanceProfile incumbent,
        PerformanceProfile challenger)
    {
        PerformanceEvidenceSnapshot baseline;
        PerformanceEvidenceSnapshot candidate;
        try
        {
            baseline = PerformanceEvidenceSnapshot.Rehydrate(record.Baseline);
            candidate = PerformanceEvidenceSnapshot.Rehydrate(record.Candidate);
        }
        catch (Exception exception) when (exception is InvalidDataException or ArgumentException)
        {
            return false;
        }

        if (baseline.Quality != PerformanceEvidenceQuality.Measured
            || candidate.Quality != PerformanceEvidenceQuality.Measured
            || baseline.Configuration is null
            || candidate.Configuration is null)
            return false;

        var a = baseline.Configuration.Rehydrate();
        var b = candidate.Configuration.Rehydrate();
        return ConfigurationMatchesProfile(a, incumbent)
            && ConfigurationMatchesProfile(b, challenger)
            && a.Environment.IsStructurallyEquivalentTo(b.Environment)
            && string.Equals(b.Environment.Id, challenger.EnvironmentFingerprint, StringComparison.OrdinalIgnoreCase);
    }

    private static bool ConfigurationMatchesProfile(
        PerformanceConfigurationSnapshot configuration,
        PerformanceProfile profile)
        => configuration.Game == profile.Game
           && string.Equals(configuration.InstanceName, profile.InstanceName, StringComparison.OrdinalIgnoreCase)
           && configuration.CpuCores == profile.CpuCores
           && configuration.RamMb == profile.RamMb
           && string.Equals(configuration.Renderer, profile.Renderer, StringComparison.OrdinalIgnoreCase)
           && configuration.FpsTarget == profile.FpsTarget
           && string.Equals(configuration.Resolution, profile.Resolution, StringComparison.OrdinalIgnoreCase)
           && configuration.Dpi == profile.Dpi;

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
}