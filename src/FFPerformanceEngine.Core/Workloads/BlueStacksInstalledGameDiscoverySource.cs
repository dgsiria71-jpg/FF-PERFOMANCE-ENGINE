using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

namespace FFPerformanceEngine.Core.Workloads;

/// <summary>
/// Read-only Track 3 discovery source for the existing Free Fire / BlueStacks
/// specialization. It never starts an emulator instance: only ADB-enabled,
/// addressable instances are queried, and only exact supported package names
/// become catalog candidates.
/// </summary>
public sealed class BlueStacksInstalledGameDiscoverySource : IGameDiscoverySource
{
    private readonly BlueStacksService _blueStacks;
    private readonly BlueStacksAutomationService _automation;
    private readonly Func<IReadOnlyList<BlueStacksInstance>> _instancesProvider;

    public BlueStacksInstalledGameDiscoverySource(
        BlueStacksService blueStacks,
        BlueStacksAutomationService automation,
        Func<IReadOnlyList<BlueStacksInstance>>? instancesProvider = null)
    {
        _blueStacks = blueStacks ?? throw new ArgumentNullException(nameof(blueStacks));
        _automation = automation ?? throw new ArgumentNullException(nameof(automation));
        _instancesProvider = instancesProvider ?? _blueStacks.LoadInstances;
    }

    public string SourceId => "bluestacks-packages";
    public int Priority => 90;

    public async Task<IReadOnlyList<GameDiscoveryCandidate>> DiscoverAsync(
        CancellationToken cancellationToken = default)
    {
        var observations = new Dictionary<GameKind, SortedSet<string>>();
        var instances = _instancesProvider() ?? Array.Empty<BlueStacksInstance>();

        foreach (var instance in instances
                     .Where(instance => instance is not null)
                     .OrderBy(instance => instance.Name, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (instance.AdbEnabled == false || instance.AdbPort is not (>= 1 and <= 65535))
                continue;

            AutomationActionResult connected;
            try
            {
                connected = await _automation.ConnectAsync(instance, cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            if (!connected.Success) continue;

            IReadOnlyList<GameKind> games;
            try
            {
                games = await _automation.QueryInstalledGamesAsync(instance, cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            foreach (var game in games)
            {
                if (game is not (GameKind.FreeFire or GameKind.FreeFireMax)) continue;
                if (!observations.TryGetValue(game, out var instanceNames))
                    observations[game] = instanceNames = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(instance.Name)) instanceNames.Add(instance.Name.Trim());
            }
        }

        var candidates = new List<GameDiscoveryCandidate>(observations.Count);
        foreach (var pair in observations.OrderBy(pair => pair.Key))
        {
            var identity = LegacyGameIdentityBridge.FromGameKind(pair.Key);
            if (identity is null) continue;

            var package = BlueStacksAutomationService.PackageFor(pair.Key);
            var instanceList = string.Join(", ", pair.Value);
            candidates.Add(new GameDiscoveryCandidate
            {
                Identity = identity,
                Confidence = 0.99,
                Evidence = $"Installed Android package {package} observed via BlueStacks ADB on instance(s): {instanceList}"
            });
        }

        return candidates;
    }
}
