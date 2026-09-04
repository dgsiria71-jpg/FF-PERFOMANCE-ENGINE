using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed class AutoTunerEngine
{
    public IReadOnlyList<TuningCandidate> GenerateCandidates(EnvironmentSnapshot environment, BlueStacksInstance? instance, AutoTunerMode mode)
    {
        var maxCores = Math.Max(2, environment.LogicalProcessors);
        var currentCores = instance?.CpuCores ?? Math.Min(4, maxCores);
        var currentRam = instance?.RamMb ?? 4096;
        var renderers = new[] { "Auto", "Vulkan", "OpenGL" };
        var coreOptions = mode == AutoTunerMode.Deep
            ? new[] { Math.Max(2, currentCores - 2), currentCores, Math.Min(maxCores, currentCores + 2), Math.Min(maxCores, currentCores + 4) }
            : new[] { currentCores, Math.Min(maxCores, currentCores + 2) };
        var ramOptions = mode == AutoTunerMode.Deep
            ? new[] { Math.Max(2048, currentRam - 2048), currentRam, currentRam + 2048 }
            : new[] { currentRam, currentRam + 2048 };

        return (from cores in coreOptions.Distinct()
                from ram in ramOptions.Distinct()
                from renderer in renderers
                select new TuningCandidate { CpuCores = cores, RamMb = ram, Renderer = renderer, FpsTarget = 90, Resolution = instance?.Resolution ?? "1920x1080" })
            .Distinct().Take(mode == AutoTunerMode.Deep ? 36 : 12).ToList();
    }

    public TuningResult SelectWinners(GameKind game, AutoTunerMode mode, IReadOnlyList<CandidateEvidence> evidence)
    {
        var valid = evidence.Where(x => x.Evidence == EvidenceLevel.Validated && x.Sample.Fps is not null).ToList();
        if (valid.Count == 0)
            return new TuningResult { Game = game, Mode = mode, Evidence = evidence, Summary = "No validated frame evidence is available; no winner was invented." };

        PerformanceProfile ToProfile(CandidateEvidence item, ProfileKind kind, string name) => new()
        {
            Name = name,
            Kind = kind,
            Game = game,
            CpuCores = item.Candidate.CpuCores,
            RamMb = item.Candidate.RamMb,
            Renderer = item.Candidate.Renderer,
            FpsTarget = item.Candidate.FpsTarget,
            Resolution = item.Candidate.Resolution,
            Evidence = item.Evidence,
            Confidence = item.Confidence,
            AverageFps = item.Sample.Fps,
            OnePercentLow = item.Sample.OnePercentLow,
            LatencyMs = item.Sample.LatencyMs,
            GpuTemperatureC = item.Sample.GpuTemperatureC
        };

        var maxFps = valid.OrderByDescending(x => x.Sample.Fps).First();
        var lowLatency = valid.Where(x => x.Sample.LatencyMs is not null).OrderBy(x => x.Sample.LatencyMs).FirstOrDefault() ?? maxFps;
        var stable = valid.OrderByDescending(x => x.Sample.OnePercentLow ?? x.Sample.Fps).First();
        var quality = valid.Where(x => x.Candidate.Resolution == "1920x1080").OrderByDescending(x => x.Sample.Fps).FirstOrDefault() ?? stable;
        var candidates = valid.Select(x => ToProfile(x, ProfileKind.Custom, "candidate")).ToList();
        var recommendedProfile = candidates.OrderByDescending(ProfileService.RecommendedScore).First();
        var recommendedEvidence = valid.First(x => x.Candidate.CpuCores == recommendedProfile.CpuCores && x.Candidate.RamMb == recommendedProfile.RamMb && x.Candidate.Renderer == recommendedProfile.Renderer);

        var winners = new[]
        {
            ToProfile(maxFps, ProfileKind.MaximumFps, "Máximo FPS"),
            ToProfile(lowLatency, ProfileKind.LowestLatency, "Menor Latência"),
            ToProfile(stable, ProfileKind.Stability, "Estabilidade"),
            ToProfile(quality, ProfileKind.Quality, "Qualidade"),
            ToProfile(recommendedEvidence, ProfileKind.Recommended, "Recomendado")
        };
        return new TuningResult { Game = game, Mode = mode, Evidence = evidence, Winners = winners, Summary = $"Selected {winners.Length} evidence-backed profile roles from {valid.Count} validated candidates." };
    }
}
