using System.Globalization;
using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed record AutoTunerProgressPresentation(
    int Percent,
    string StageLabel,
    string Title,
    string Detail);

public static class AutoTunerPresentation
{
    public static AutoTunerProgressPresentation FromProgress(AutoTunerRunProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        if (progress.Stage == AutoTunerRunStage.Completed)
        {
            return new AutoTunerProgressPresentation(
                100,
                "Concluído",
                "Otimização concluída",
                progress.Message);
        }

        if (progress.Stage == AutoTunerRunStage.RestoringBaseline)
        {
            return new AutoTunerProgressPresentation(
                98,
                "Restauração segura",
                "Restaurando configuração de referência",
                progress.Message);
        }

        var candidateCount = Math.Max(1, progress.CandidateCount);
        var candidateIndex = Math.Clamp(progress.CandidateIndex, 1, candidateCount);
        var stageFraction = progress.Stage switch
        {
            AutoTunerRunStage.ApplyingCandidate => 0.10,
            AutoTunerRunStage.PreparingGame => 0.28,
            AutoTunerRunStage.Benchmarking => 0.55,
            AutoTunerRunStage.ValidatingBenchmark => 0.78,
            AutoTunerRunStage.CleaningCandidate => 0.94,
            _ => 0.0
        };

        const double candidateProgressBudget = 96.0;
        var candidateWidth = candidateProgressBudget / candidateCount;
        var rawPercent = ((candidateIndex - 1) * candidateWidth) + (candidateWidth * stageFraction);
        var percent = Math.Clamp((int)Math.Floor(rawPercent), 0, 95);

        return new AutoTunerProgressPresentation(
            percent,
            StageLabel(progress.Stage),
            $"Testando candidato {candidateIndex} de {candidateCount}",
            progress.Message);
    }

    public static string Summarize(AutoTunerSessionResult session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var winners = session.Tuning.Winners
            .Where(profile => profile.Evidence == EvidenceLevel.Validated)
            .ToList();

        if (winners.Count == 0)
        {
            return $"Otimização concluída na instância {session.InstanceName}, mas nenhum perfil vencedor validado foi encontrado. " +
                   "Os perfis conhecidos foram preservados e nenhuma configuração inconclusiva substituiu a última configuração confiável.";
        }

        var recommended = winners.FirstOrDefault(profile => profile.Kind == ProfileKind.Recommended)
                          ?? winners.OrderByDescending(ProfileService.RecommendedScore).First();
        var fps = recommended.AverageFps is double averageFps
            ? averageFps.ToString("0.#", CultureInfo.InvariantCulture)
            : "indisponível";
        var confidence = $"{Math.Round(Math.Clamp(recommended.Confidence, 0, 1) * 100, MidpointRounding.AwayFromZero):0}%";

        return $"Recomendado · {fps} FPS · confiança {confidence} · instância {session.InstanceName}. " +
               $"{winners.Count} perfil(is) vencedor(es) validado(s) em {session.CandidateCount} candidato(s).";
    }

    private static string StageLabel(AutoTunerRunStage stage) => stage switch
    {
        AutoTunerRunStage.ApplyingCandidate => "Aplicando candidato",
        AutoTunerRunStage.PreparingGame => "Preparando jogo",
        AutoTunerRunStage.Benchmarking => "Benchmark",
        AutoTunerRunStage.ValidatingBenchmark => "Validação",
        AutoTunerRunStage.CleaningCandidate => "Finalizando candidato",
        AutoTunerRunStage.RestoringBaseline => "Restauração segura",
        AutoTunerRunStage.Completed => "Concluído",
        _ => "Otimização"
    };
}
