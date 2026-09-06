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

    public async Task<PerformanceProfile> CreateCustomFromValidatedComparisonAsync(
        PerformanceComparisonHistoryRecord record,
        EnvironmentSnapshot currentEnvironment,
        string name,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(currentEnvironment);
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Custom profile name is required.", nameof(name));

        var normalized = record.Rehydrate();
        if (!normalized.CanOriginateProfile
            || normalized.Candidate.Configuration is null
            || normalized.ValidationEvidence is null)
            throw new InvalidOperationException("Only a comparison with separate measured validation and matching exact configuration can originate a profile.");

        var configuration = normalized.Candidate.Configuration.Rehydrate();
        if (!configuration.Environment.IsStructurallyCompatible(currentEnvironment))
            throw new InvalidOperationException("The current machine or BlueStacks instance no longer matches the environment that produced this validated candidate.");

        var validation = PerformanceEvidenceSnapshot.Rehydrate(normalized.ValidationEvidence);
        var created = new PerformanceProfile
        {
            Name = name.Trim(),
            Kind = ProfileKind.Custom,
            Game = configuration.Game,
            InstanceName = configuration.InstanceName,
            CpuCores = configuration.CpuCores,
            RamMb = configuration.RamMb,
            Renderer = configuration.Renderer,
            FpsTarget = configuration.FpsTarget,
            Resolution = configuration.Resolution,
            Dpi = configuration.Dpi,
            Evidence = EvidenceLevel.Validated,
            AverageFps = validation.AverageFps,
            FrameTimeMs = validation.AverageFrameTimeMs,
            LatencyMs = validation.AverageLatencyMs,
            SourceComparisonId = normalized.Id,
            EnvironmentFingerprint = configuration.Environment.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var data = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
            var existing = data.Items.FirstOrDefault(profile =>
                profile.Kind == ProfileKind.Custom
                && profile.SourceComparisonId == normalized.Id);
            if (existing is not null) return existing;

            data.Items.Add(created);
            await _store.SaveAsync(data, cancellationToken).ConfigureAwait(false);
            return created;
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public async Task ReplaceValidatedWinnerRoleAsync(
        PerformanceProfile winner,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(winner);
        if (winner.Kind == ProfileKind.Custom)
            throw new InvalidOperationException("A Custom profile cannot directly occupy a generated winner role.");
        if (winner.Kind is not (ProfileKind.Recommended or ProfileKind.MaximumFps or ProfileKind.LowestLatency or ProfileKind.Stability or ProfileKind.Quality))
            throw new InvalidOperationException($"Profile kind {winner.Kind} is not a generated winner role.");
        if (winner.Evidence != EvidenceLevel.Validated)
            throw new InvalidOperationException("Only validated evidence may replace a generated winner role.");
        if (winner.Game is not (GameKind.FreeFire or GameKind.FreeFireMax))
            throw new InvalidOperationException("Winner role replacement requires Free Fire or Free Fire MAX.");
        if (string.IsNullOrWhiteSpace(winner.InstanceName))
            throw new InvalidOperationException("Winner role replacement requires a BlueStacks instance binding.");

        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var data = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
            data.Items.RemoveAll(profile =>
                profile.Kind == winner.Kind
                && profile.Game == winner.Game
                && profile.Kind != ProfileKind.Custom
                && (string.Equals(profile.InstanceName, winner.InstanceName, StringComparison.OrdinalIgnoreCase)
                    || string.IsNullOrWhiteSpace(profile.InstanceName)));
            data.Items.Add(winner);
            await _store.SaveAsync(data, cancellationToken).ConfigureAwait(false);
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
