using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Workloads;

/// <summary>
/// Capability declaration for one game adapter. These flags describe adapter-
/// specific functionality that exists today; they are not promises that the
/// universal System Optimizer can mutate the game itself.
/// </summary>
public sealed record GameAdapterCapabilities
{
    public bool StateDetection { get; init; }
    public bool ConfigDiscovery { get; init; }
    public bool ConfigSnapshot { get; init; }
    public bool ConfigMutation { get; init; }
    public bool BenchmarkPreparation { get; init; }
    public bool TelemetryAnnotations { get; init; }
    public bool Rollback { get; init; }
}

public interface IGameAdapter
{
    string AdapterId { get; }
    int Priority { get; }
    bool IsGeneric { get; }
    GameAdapterCapabilities Capabilities { get; }
}

/// <summary>
/// Adapter-owned declaration of one workload-specific tuning dimension. The
/// candidate values are support/search-space metadata only; they carry no
/// recommendation, validation or persistence authority by themselves.
/// </summary>
public sealed record GameAdapterTuningDimensionDeclaration
{
    public string Id { get; init; } = string.Empty;
    public IReadOnlyList<string> CandidateValues { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Optional Track 5 capability for specialized adapters that can explicitly
/// declare workload tuning dimensions. IGameAdapter intentionally does not
/// inherit this interface so existing and generic adapters remain source-compatible.
/// </summary>
public interface IGameTuningDimensionProvider
{
    IReadOnlyList<GameAdapterTuningDimensionDeclaration> GetTuningDimensions(GameIdentity identity);
}

/// <summary>
/// Safe fallback for games without a specialized adapter. Generic Windows,
/// process, hardware and telemetry optimization lives in universal engines;
/// therefore this adapter intentionally does not claim game-config powers.
/// </summary>
public sealed class GenericGameAdapter : IGameAdapter
{
    public string AdapterId => "generic";
    public int Priority => 0;
    public bool IsGeneric => true;
    public GameAdapterCapabilities Capabilities { get; } = new();
}

/// <summary>
/// Compatibility declaration for the existing Free Fire / BlueStacks product
/// specialization. The operational services remain the current source of truth;
/// Track 3 only gives them a neutral adapter identity and truthful capability map.
/// </summary>
public sealed class BlueStacksFreeFireGameAdapter : IGameAdapter
{
    private static readonly GameAdapterCapabilities ExistingCapabilities = new()
    {
        StateDetection = true,
        ConfigDiscovery = true,
        ConfigSnapshot = true,
        ConfigMutation = true,
        BenchmarkPreparation = true,
        TelemetryAnnotations = true,
        Rollback = true
    };

    private BlueStacksFreeFireGameAdapter(string adapterId, GameKind gameKind)
    {
        AdapterId = adapterId;
        GameKind = gameKind;
    }

    public string AdapterId { get; }
    public GameKind GameKind { get; }
    public int Priority => 100;
    public bool IsGeneric => false;
    public GameAdapterCapabilities Capabilities => ExistingCapabilities;

    public static BlueStacksFreeFireGameAdapter For(GameKind gameKind)
        => gameKind switch
        {
            GameKind.FreeFire => new BlueStacksFreeFireGameAdapter("bluestacks.free-fire", gameKind),
            GameKind.FreeFireMax => new BlueStacksFreeFireGameAdapter("bluestacks.free-fire-max", gameKind),
            _ => throw new ArgumentOutOfRangeException(
                nameof(gameKind),
                gameKind,
                "Only Free Fire and Free Fire MAX are backed by the existing BlueStacks specialization.")
        };
}

/// <summary>
/// Deterministic adapter authority. A GameIdentity asks for a specialized adapter
/// by stable AdapterId; absence of that specialization falls back to exactly one
/// generic adapter. Resolution never guesses from executable names or paths.
/// </summary>
public sealed class GameAdapterResolver
{
    private readonly IReadOnlyDictionary<string, IGameAdapter> _adapters;
    private readonly IGameAdapter _generic;

    public GameAdapterResolver(IEnumerable<IGameAdapter> adapters)
    {
        ArgumentNullException.ThrowIfNull(adapters);

        var byId = new Dictionary<string, IGameAdapter>(StringComparer.OrdinalIgnoreCase);
        IGameAdapter? generic = null;

        foreach (var adapter in adapters)
        {
            if (adapter is null) continue;
            var id = NormalizeAdapterId(adapter.AdapterId);
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Game adapter id cannot be empty.", nameof(adapters));
            if (!byId.TryAdd(id, adapter))
                throw new ArgumentException($"Duplicate game adapter id '{id}'.", nameof(adapters));

            if (adapter.IsGeneric)
            {
                if (!string.Equals(id, "generic", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("The generic game adapter must use the stable id 'generic'.", nameof(adapters));
                generic = adapter;
            }
        }

        _generic = generic
                   ?? throw new ArgumentException("A generic game adapter fallback is required.", nameof(adapters));
        _adapters = byId;
    }

    public IGameAdapter Resolve(GameIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        if (string.IsNullOrWhiteSpace(identity.GameId))
            throw new ArgumentException("Game identity must contain a stable GameId.", nameof(identity));

        var requested = NormalizeAdapterId(identity.AdapterId);
        if (!string.IsNullOrWhiteSpace(requested)
            && _adapters.TryGetValue(requested, out var exact))
            return exact;

        return _generic;
    }

    private static string NormalizeAdapterId(string? value)
        => value?.Trim().ToLowerInvariant() ?? string.Empty;
}
