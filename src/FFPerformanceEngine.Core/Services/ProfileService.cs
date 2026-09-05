using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed class ProfileCollection
{
    public List<PerformanceProfile> Items { get; init; } = new();
}

public sealed class ProfileService
{
    private readonly JsonStore<ProfileCollection> _store;
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public ProfileService(string? path = null) => _store = new JsonStore<ProfileCollection>(path ?? AppPaths.Profiles);

    public async Task<IReadOnlyList<PerformanceProfile>> LoadAsync(CancellationToken cancellationToken = default)
        => (await _store.LoadAsync(cancellationToken).ConfigureAwait(false)).Items;

    public async Task SaveAsync(IEnumerable<PerformanceProfile> profiles, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _store.SaveAsync(new ProfileCollection { Items = profiles.ToList() }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public async Task ReplaceAutoTunerWinnersAsync(
        GameKind game,
        string instanceName,
        IEnumerable<PerformanceProfile> winners,
        CancellationToken cancellationToken = default)
    {
        if (game is not (GameKind.FreeFire or GameKind.FreeFireMax))
            throw new ArgumentOutOfRangeException(nameof(game), game, "Auto Tuner winners require Free Fire or Free Fire MAX.");
        if (string.IsNullOrWhiteSpace(instanceName))
            throw new ArgumentException("BlueStacks instance name is required.", nameof(instanceName));
        ArgumentNullException.ThrowIfNull(winners);

        var materialized = winners.ToList();
        if (materialized.Count == 0)
            throw new ArgumentException("At least one validated winner is required for replacement.", nameof(winners));

        foreach (var profile in materialized)
        {
            if (profile.Kind == ProfileKind.Custom)
                throw new InvalidOperationException("Custom profiles cannot be inserted into the Auto Tuner generated winner set.");
            if (profile.Game != game)
                throw new InvalidOperationException($"Winner '{profile.Name}' belongs to {profile.Game}, not {game}.");
            if (profile.Evidence != EvidenceLevel.Validated)
                throw new InvalidOperationException($"Winner '{profile.Name}' is not validated and cannot replace a known-good generated profile.");
        }

        var bound = materialized
            .Select(profile => profile with { InstanceName = instanceName })
            .ToList();

        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var data = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
            data.Items.RemoveAll(profile =>
                profile.Game == game
                && profile.Kind != ProfileKind.Custom
                && (string.Equals(profile.InstanceName, instanceName, StringComparison.OrdinalIgnoreCase)
                    || string.IsNullOrWhiteSpace(profile.InstanceName)));
            data.Items.AddRange(bound);
            await _store.SaveAsync(data, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeGate.Release();
        }
    }

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
