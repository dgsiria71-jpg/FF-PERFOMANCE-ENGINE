using System.Globalization;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Workloads;

namespace FFPerformanceEngine.Core.Services;

public sealed record BlueStacksUniversalTuningCandidateBinding
{
    public required UniversalTuningCandidate UniversalCandidate { get; init; }
    public required TuningCandidate SpecializedCandidate { get; init; }
}

public sealed record BlueStacksUniversalTuningCandidateSpace
{
    public required GameIdentity Identity { get; init; }
    public string AdapterId { get; init; } = string.Empty;
    public IReadOnlyList<UniversalTuningDimension> Dimensions { get; init; } = Array.Empty<UniversalTuningDimension>();
    public IReadOnlyList<BlueStacksUniversalTuningCandidateBinding> Bindings { get; init; } = Array.Empty<BlueStacksUniversalTuningCandidateBinding>();
}

/// <summary>
/// Projects the existing dynamic BlueStacks/Free Fire Auto Tuner candidate set
/// into neutral Track 5 candidate bindings. The specialized generator remains the
/// source of truth and the runtime candidate planner remains the applicability
/// authority. Descriptive dimensions never authorize re-enumerating a Cartesian
/// space that the bounded specialized generator did not emit.
/// </summary>
public sealed class BlueStacksUniversalTuningCandidateBridge
{
    private readonly AutoTunerEngine _engine;
    private readonly GameAdapterResolver _resolver;

    public BlueStacksUniversalTuningCandidateBridge(
        AutoTunerEngine engine,
        GameAdapterResolver resolver)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public BlueStacksUniversalTuningCandidateSpace Build(
        EnvironmentSnapshot environment,
        BlueStacksInstance instance,
        GameKind game,
        AutoTunerMode mode,
        IReadOnlyDictionary<string, string> capturedSettings)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(capturedSettings);
        if (game is not (GameKind.FreeFire or GameKind.FreeFireMax))
            throw new ArgumentOutOfRangeException(nameof(game), game, "BlueStacks universal tuning projection requires Free Fire or Free Fire MAX.");
        if (string.IsNullOrWhiteSpace(instance.Name))
            throw new ArgumentException("A named BlueStacks instance is required for universal candidate projection.", nameof(instance));

        var identity = LegacyGameIdentityBridge.FromGameKind(game)
                       ?? throw new InvalidOperationException($"No stable legacy GameIdentity bridge exists for {game}.");
        var resolved = _resolver.Resolve(identity);
        var adapterId = NormalizeId(resolved.AdapterId);

        if (resolved is not BlueStacksFreeFireGameAdapter blueStacksAdapter
            || blueStacksAdapter.GameKind != game
            || !HasExecutableCandidateLifecycle(resolved.Capabilities))
            return Empty(identity, adapterId);

        var instancePrefix = $"bst.instance.{instance.Name}.";
        if (!capturedSettings.Keys.Any(key => key.StartsWith(instancePrefix, StringComparison.OrdinalIgnoreCase)))
            return Empty(identity, adapterId);

        var capturedRenderer = ReadCapturedValue(capturedSettings, instancePrefix, "graphics_renderer")
                               ?? ReadCapturedValue(capturedSettings, instancePrefix, "graphics_engine");

        var prefix = $"workload.{adapterId}.";
        var cpuId = prefix + "cpu-cores";
        var fpsId = prefix + "fps-target";
        var ramId = prefix + "ram-mb";
        var rendererId = prefix + "renderer";
        var resolutionId = prefix + "resolution";

        var cpuValues = new OrderedValues();
        var fpsValues = new OrderedValues();
        var ramValues = new OrderedValues();
        var rendererValues = new OrderedValues();
        var resolutionValues = new OrderedValues();
        var bindings = new List<BlueStacksUniversalTuningCandidateBinding>();
        var seenCandidates = new HashSet<CandidateKey>();

        foreach (var candidate in _engine.GenerateCandidates(environment, instance, mode))
        {
            var plan = BlueStacksAutoTunerRuntime.BuildCandidatePlan(candidate, instance, capturedSettings);
            if (!plan.CanApply) continue;
            if (!string.IsNullOrWhiteSpace(capturedRenderer)
                && !string.Equals(candidate.Renderer, "Auto", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(candidate.Renderer, capturedRenderer, StringComparison.OrdinalIgnoreCase))
                continue;

            var cpu = candidate.CpuCores.ToString(CultureInfo.InvariantCulture);
            var ram = candidate.RamMb.ToString(CultureInfo.InvariantCulture);
            var fps = candidate.FpsTarget.ToString(CultureInfo.InvariantCulture);
            var renderer = candidate.Renderer;
            var resolution = candidate.Resolution;

            var key = new CandidateKey(candidate.CpuCores, candidate.RamMb, renderer, candidate.FpsTarget, resolution);
            if (!seenCandidates.Add(key))
                throw new InvalidOperationException("The specialized BlueStacks generator emitted duplicate candidates that would collapse universal candidate identity.");

            cpuValues.Add(cpu);
            fpsValues.Add(fps);
            ramValues.Add(ram);
            rendererValues.Add(renderer);
            resolutionValues.Add(resolution);

            bindings.Add(new BlueStacksUniversalTuningCandidateBinding
            {
                SpecializedCandidate = candidate,
                UniversalCandidate = new UniversalTuningCandidate
                {
                    Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        [cpuId] = cpu,
                        [fpsId] = fps,
                        [ramId] = ram,
                        [rendererId] = renderer,
                        [resolutionId] = resolution
                    }
                }
            });
        }

        if (bindings.Count == 0) return Empty(identity, adapterId);

        return new BlueStacksUniversalTuningCandidateSpace
        {
            Identity = identity,
            AdapterId = adapterId,
            Dimensions =
            [
                Dimension(cpuId, adapterId, cpuValues.Values),
                Dimension(fpsId, adapterId, fpsValues.Values),
                Dimension(ramId, adapterId, ramValues.Values),
                Dimension(rendererId, adapterId, rendererValues.Values),
                Dimension(resolutionId, adapterId, resolutionValues.Values)
            ],
            Bindings = bindings.ToArray()
        };
    }

    private static UniversalTuningDimension Dimension(
        string id,
        string authorityId,
        IReadOnlyList<string> values)
        => new()
        {
            Id = id,
            Scope = UniversalTuningDimensionScope.Workload,
            AuthorityId = authorityId,
            CandidateValues = values
        };

    private static BlueStacksUniversalTuningCandidateSpace Empty(GameIdentity identity, string adapterId)
        => new()
        {
            Identity = identity,
            AdapterId = adapterId,
            Dimensions = Array.Empty<UniversalTuningDimension>(),
            Bindings = Array.Empty<BlueStacksUniversalTuningCandidateBinding>()
        };

    private static bool HasExecutableCandidateLifecycle(GameAdapterCapabilities capabilities)
        => capabilities.ConfigDiscovery
           && capabilities.ConfigSnapshot
           && capabilities.ConfigMutation
           && capabilities.BenchmarkPreparation
           && capabilities.Rollback;

    private static string? ReadCapturedValue(
        IReadOnlyDictionary<string, string> capturedSettings,
        string instancePrefix,
        string settingName)
    {
        foreach (var pair in capturedSettings)
        {
            if (!pair.Key.StartsWith(instancePrefix, StringComparison.OrdinalIgnoreCase)) continue;
            var shortKey = pair.Key[instancePrefix.Length..];
            if (!string.Equals(shortKey, settingName, StringComparison.OrdinalIgnoreCase)) continue;

            var value = pair.Value?.Trim().Trim('"');
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return null;
    }

    private static string NormalizeId(string? value)
        => value?.Trim().ToLowerInvariant() ?? string.Empty;

    private readonly record struct CandidateKey(
        int CpuCores,
        int RamMb,
        string Renderer,
        int FpsTarget,
        string Resolution);

    private sealed class OrderedValues
    {
        private readonly HashSet<string> _seen = new(StringComparer.Ordinal);
        private readonly List<string> _values = [];

        public IReadOnlyList<string> Values => _values.ToArray();

        public void Add(string value)
        {
            if (!_seen.Add(value)) return;
            _values.Add(value);
        }
    }
}
