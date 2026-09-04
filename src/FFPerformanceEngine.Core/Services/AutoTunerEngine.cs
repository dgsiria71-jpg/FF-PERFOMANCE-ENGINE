using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed class AutoTunerEngine
{
    public IReadOnlyList<TuningCandidate> GenerateCandidates(EnvironmentSnapshot environment, BlueStacksInstance? instance, AutoTunerMode mode)
    {
        var maxCores = Math.Max(2, environment.LogicalProcessors);
        var currentCores = Math.Clamp(instance?.CpuCores ?? Math.Min(4, maxCores), 2, maxCores);
        var currentRam = Math.Max(2048, instance?.RamMb ?? 4096);
        var maxRam = environment.MemoryTotalGb is double totalGb
            ? Math.Max(2048, Math.Min((int)(totalGb * 1024 * 0.60), Math.Max(2048, (int)(totalGb * 1024) - 2048)))
            : currentRam + 4096;
        maxRam = Math.Max(currentRam, Math.Min(maxRam, 16384));

        var renderer = string.IsNullOrWhiteSpace(instance?.Renderer) ? "Auto" : instance.Renderer!;
        var currentFps = Math.Clamp(instance?.Fps ?? 90, 30, 240);
        var currentResolution = string.IsNullOrWhiteSpace(instance?.Resolution) ? "1920x1080" : instance.Resolution!;

        var coreOptions = mode == AutoTunerMode.Deep
            ? new[] { Math.Max(2, currentCores - 2), currentCores, Math.Min(maxCores, currentCores + 2), Math.Min(maxCores, currentCores + 4) }
            : new[] { currentCores, Math.Min(maxCores, currentCores + 2) };
        var ramOptions = mode == AutoTunerMode.Deep
            ? new[] { Math.Max(2048, currentRam - 2048), currentRam, Math.Min(maxRam, currentRam + 2048), Math.Min(maxRam, currentRam + 4096) }
            : new[] { currentRam, Math.Min(maxRam, currentRam + 2048) };
        var fpsOptions = mode == AutoTunerMode.Deep
            ? new[] { 60, 90, 120, 144, 165, 240, currentFps }
            : new[] { currentFps, Math.Min(120, Math.Max(90, currentFps)) };
        var resolutionOptions = mode == AutoTunerMode.Deep
            ? new[] { "1280x720", "1600x900", "1920x1080", currentResolution }
            : new[] { currentResolution };

        return (from cores in coreOptions.Distinct()
                from ram in ramOptions.Where(x => x <= maxRam).Distinct()
                from fps in fpsOptions.Distinct()
                from resolution in resolutionOptions.Distinct(StringComparer.OrdinalIgnoreCase)
                select new TuningCandidate { CpuCores = cores, RamMb = ram, Renderer = renderer, FpsTarget = fps, Resolution = resolution })
            .Distinct().Take(mode == AutoTunerMode.Deep ? 96 : 12).ToList();
    }

    public TuningResult SelectWinners(GameKind game, AutoTunerMode mode, IReadOnlyList<CandidateEvidence> evidence)
    {
        var valid = evidence.Where(x => x.Evidence == EvidenceLevel.Validated && x.Sample.Fps is > 0).ToList();
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
            StutterPercent = item.Sample.StutterPercent,
            GpuTemperatureC = item.Sample.GpuTemperatureC
        };

        var maxFps = valid.OrderByDescending(x => x.Sample.Fps).ThenByDescending(x => x.Sample.OnePercentLow).First();
        var lowLatency = valid.Where(x => x.Sample.LatencyMs is not null).OrderBy(x => x.Sample.LatencyMs).ThenByDescending(x => x.Sample.OnePercentLow).FirstOrDefault() ?? maxFps;
        var stable = valid.OrderByDescending(StabilityScore).First();
        var maxMeasuredFps = maxFps.Sample.Fps!.Value;
        var qualityPool = valid.Where(x => x.Sample.Fps >= maxMeasuredFps * 0.70 && (x.Sample.OnePercentLow ?? x.Sample.Fps) >= maxMeasuredFps * 0.55).ToList();
        var quality = (qualityPool.Count > 0 ? qualityPool : valid)
            .OrderByDescending(x => ResolutionPixels(x.Candidate.Resolution))
            .ThenByDescending(x => x.Sample.OnePercentLow ?? x.Sample.Fps)
            .First();
        var candidates = valid.Select(x => ToProfile(x, ProfileKind.Custom, "candidate")).ToList();
        var recommendedProfile = candidates.OrderByDescending(ProfileService.RecommendedScore).First();
        var recommendedEvidence = valid.First(x => CandidateMatchesProfile(x.Candidate, recommendedProfile));

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

    private static double StabilityScore(CandidateEvidence item)
    {
        var fps = item.Sample.Fps ?? 0;
        var low = item.Sample.OnePercentLow ?? fps * 0.75;
        var stutterPenalty = (item.Sample.StutterPercent ?? 0) * 4;
        return low * 0.75 + fps * 0.25 - stutterPenalty;
    }

    private static bool CandidateMatchesProfile(TuningCandidate candidate, PerformanceProfile profile)
        => candidate.CpuCores == profile.CpuCores && candidate.RamMb == profile.RamMb && candidate.FpsTarget == profile.FpsTarget
           && string.Equals(candidate.Renderer, profile.Renderer, StringComparison.OrdinalIgnoreCase)
           && string.Equals(candidate.Resolution, profile.Resolution, StringComparison.OrdinalIgnoreCase);

    private static long ResolutionPixels(string resolution)
    {
        var parts = resolution.Split('x', 'X');
        return parts.Length == 2 && int.TryParse(parts[0], out var width) && int.TryParse(parts[1], out var height)
            ? (long)width * height
            : 0;
    }
}
