using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public enum ProfileChallengeProgressStatus
{
    AwaitingEvidence,
    EnvironmentDrift,
    Inconclusive,
    IncumbentHeld,
    ReadyToPromote
}

public sealed record ProfileChallengeRoundProgress
{
    public Guid ComparisonId { get; init; }
    public DateTimeOffset CapturedAt { get; init; }
    public string Label { get; init; } = string.Empty;
    public ProfileChallengeVerdict Verdict { get; init; }
}

public sealed record ProfileChallengeProgress
{
    public ProfileChallengeProgressStatus Status { get; init; }
    public ProfileKind TargetKind { get; init; }
    public int EligibleRounds { get; init; }
    public int RequiredRounds { get; init; } = 2;
    public bool CanPromote { get; init; }
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<ProfileChallengeRoundProgress> RecentRounds { get; init; } = Array.Empty<ProfileChallengeRoundProgress>();
}

public sealed class ProfileChallengeProgressService
{
    private readonly ProfileService _profiles;
    private readonly HistoryService _history;

    public ProfileChallengeProgressService(ProfileService profiles, HistoryService history)
    {
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _history = history ?? throw new ArgumentNullException(nameof(history));
    }

    public async Task<ProfileChallengeProgress> GetAsync(
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

        var recent = rounds.TakeLast(2).ToArray();
        var roundProgress = recent
            .Select(record => new ProfileChallengeRoundProgress
            {
                ComparisonId = record.Id,
                CapturedAt = record.Candidate.CapturedAt,
                Label = record.Label,
                Verdict = ProfileChallengeEvaluator.Evaluate(targetKind, record.Baseline, record.Candidate)
            })
            .ToArray();

        if (rounds.Count < 2)
            return new ProfileChallengeProgress
            {
                Status = ProfileChallengeProgressStatus.AwaitingEvidence,
                TargetKind = targetKind,
                EligibleRounds = rounds.Count,
                Message = $"{rounds.Count}/2 rodada(s) A/B elegível(is). Capture e salve outra rodada totalmente medida para continuar.",
                RecentRounds = roundProgress
            };

        if (recent.Any(record => record.Candidate.Configuration is null
                                 || !record.Candidate.Configuration.Environment.IsStructurallyCompatible(currentEnvironment)))
            return new ProfileChallengeProgress
            {
                Status = ProfileChallengeProgressStatus.EnvironmentDrift,
                TargetKind = targetKind,
                EligibleRounds = rounds.Count,
                Message = "O ambiente atual mudou estruturalmente desde as rodadas medidas; o desafio precisa ser repetido neste ambiente.",
                RecentRounds = roundProgress
            };

        if (roundProgress.Any(round => round.Verdict == ProfileChallengeVerdict.Inconclusive))
            return new ProfileChallengeProgress
            {
                Status = ProfileChallengeProgressStatus.Inconclusive,
                TargetKind = targetKind,
                EligibleRounds = rounds.Count,
                Message = "Uma das duas rodadas mais recentes foi inconclusiva; o vencedor atual permanece protegido.",
                RecentRounds = roundProgress
            };

        if (roundProgress.Any(round => round.Verdict != ProfileChallengeVerdict.ChallengerWins))
            return new ProfileChallengeProgress
            {
                Status = ProfileChallengeProgressStatus.IncumbentHeld,
                TargetKind = targetKind,
                EligibleRounds = rounds.Count,
                Message = "O Custom não venceu as duas rodadas mais recentes; o vencedor atual permanece.",
                RecentRounds = roundProgress
            };

        return new ProfileChallengeProgress
        {
            Status = ProfileChallengeProgressStatus.ReadyToPromote,
            TargetKind = targetKind,
            EligibleRounds = rounds.Count,
            CanPromote = true,
            Message = "As duas rodadas A/B mais recentes são compatíveis, totalmente medidas e favoráveis ao Custom. Promoção explícita disponível.",
            RecentRounds = roundProgress
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
}
