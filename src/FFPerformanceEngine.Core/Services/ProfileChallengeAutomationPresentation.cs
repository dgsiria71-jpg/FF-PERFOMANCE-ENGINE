namespace FFPerformanceEngine.Core.Services;

public static class ProfileChallengeAutomationPresentation
{
    public static string Format(ProfileChallengeAutomationProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        var stage = StageName(progress.Stage);
        return progress.Stage is ProfileChallengeRoundStage.MeasuringBaseline or ProfileChallengeRoundStage.MeasuringCandidate
            ? $"{stage} · {progress.AcceptedSamples}/{progress.RequiredSamples} · {progress.Message}"
            : $"{stage} · {progress.Message}";
    }

    public static string StageName(ProfileChallengeRoundStage stage)
        => stage switch
        {
            ProfileChallengeRoundStage.Validating => "Validando",
            ProfileChallengeRoundStage.ApplyingBaseline => "Aplicando vencedor (A)",
            ProfileChallengeRoundStage.PreparingBaseline => "Preparando vencedor (A)",
            ProfileChallengeRoundStage.MeasuringBaseline => "Medindo vencedor (A)",
            ProfileChallengeRoundStage.CleaningBaseline => "Limpando vencedor (A)",
            ProfileChallengeRoundStage.ApplyingCandidate => "Aplicando Custom (B)",
            ProfileChallengeRoundStage.PreparingCandidate => "Preparando Custom (B)",
            ProfileChallengeRoundStage.MeasuringCandidate => "Medindo Custom (B)",
            ProfileChallengeRoundStage.CleaningCandidate => "Limpando Custom (B)",
            ProfileChallengeRoundStage.RestoringBaseline => "Restaurando configuração",
            ProfileChallengeRoundStage.SavingEvidence => "Salvando evidência",
            ProfileChallengeRoundStage.Completed => "Concluído",
            _ => stage.ToString()
        };
}
