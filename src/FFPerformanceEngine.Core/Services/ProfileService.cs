using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed class ProfileCollection
{
    public List<PerformanceProfile> Items { get; init; } = new();
}

public sealed class ProfileService
{
    private readonly JsonStore<ProfileCollection> _store;
    public ProfileService(string? path = null) => _store = new JsonStore<ProfileCollection>(path ?? AppPaths.Profiles);

    public async Task<IReadOnlyList<PerformanceProfile>> LoadAsync(CancellationToken cancellationToken = default)
        => (await _store.LoadAsync(cancellationToken).ConfigureAwait(false)).Items;

    public async Task SaveAsync(IEnumerable<PerformanceProfile> profiles, CancellationToken cancellationToken = default)
        => await _store.SaveAsync(new ProfileCollection { Items = profiles.ToList() }, cancellationToken).ConfigureAwait(false);

    public static double RecommendedScore(PerformanceProfile profile)
    {
        if (profile.Evidence == EvidenceLevel.Unknown || profile.AverageFps is null) return double.NegativeInfinity;
        var fps = profile.AverageFps.Value;
        var low = profile.OnePercentLow ?? fps * 0.8;
        var latency = profile.LatencyMs ?? 20;
        var stutter = profile.StutterPercent ?? 5;
        var thermalPenalty = Math.Max(0, (profile.GpuTemperatureC ?? 60) - 75) * 0.8;
        return fps * 0.30 + low * 0.40 - latency * 1.5 - stutter * 6 - thermalPenalty + profile.Confidence * 15;
    }
}
