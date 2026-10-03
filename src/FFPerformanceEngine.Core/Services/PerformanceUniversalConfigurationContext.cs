using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Workloads;

namespace FFPerformanceEngine.Core.Services;

public sealed record PerformanceCapabilityValueSnapshot
{
    public string CapabilityId { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string SourceId { get; init; } = string.Empty;
}

public sealed record PerformanceUniversalConfigurationContext
{
    public int SchemaVersion { get; init; } = 1;
    public string GameId { get; init; } = string.Empty;
    public string AdapterId { get; init; } = string.Empty;
    public string? AdapterVersion { get; init; }
    public string? AdapterVersionSourceId { get; init; }
    public MachineEnvironmentFingerprintV2 Machine { get; init; } = new();
    public IReadOnlyList<PerformanceCapabilityValueSnapshot> CapabilityValues { get; init; }
        = Array.Empty<PerformanceCapabilityValueSnapshot>();
    public IReadOnlyDictionary<string, string> WorkloadConfiguration { get; init; }
        = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> DisplayDriverContext { get; init; }
        = new Dictionary<string, string>();

    public static PerformanceUniversalConfigurationContext? Capture(
        MachineContext machine,
        ResolvedGameCatalogResult catalog,
        string? requestedGameId,
        IEnumerable<string>? relevantCapabilityIds = null)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(catalog);

        var requested = NormalizeId(requestedGameId);
        if (string.IsNullOrWhiteSpace(requested)) return null;

        var matches = (catalog.Games ?? Array.Empty<ResolvedGameCatalogEntry>())
            .Where(entry => entry?.Identity is not null
                            && string.Equals(
                                NormalizeId(entry.Identity.GameId),
                                requested,
                                StringComparison.Ordinal))
            .ToArray();
        if (matches.Length != 1) return null;

        var match = matches[0];
        var gameId = NormalizeId(match.Identity.GameId);
        var adapterId = NormalizeId(match.Adapter?.AdapterId);
        if (string.IsNullOrWhiteSpace(gameId)
            || string.IsNullOrWhiteSpace(adapterId)
            || machine.Fingerprint is null
            || string.IsNullOrWhiteSpace(machine.Fingerprint.Id))
            return null;

        var requestedCapabilities = (relevantCapabilityIds ?? Array.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(NormalizeId)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        var availableCapabilities = machine.Capabilities ?? Array.Empty<WindowsPerformanceCapability>();
        var values = new List<PerformanceCapabilityValueSnapshot>();
        foreach (var requestedCapabilityId in requestedCapabilities)
        {
            var capabilityMatches = availableCapabilities
                .Where(capability => capability is not null
                                     && string.Equals(
                                         NormalizeId(capability.CapabilityId),
                                         requestedCapabilityId,
                                         StringComparison.Ordinal))
                .ToArray();
            if (capabilityMatches.Length != 1) continue;

            var capability = capabilityMatches[0];
            if (capability.Availability != CapabilityAvailability.Available
                || string.IsNullOrWhiteSpace(capability.CurrentValue))
                continue;

            values.Add(new PerformanceCapabilityValueSnapshot
            {
                CapabilityId = NormalizeId(capability.CapabilityId),
                Value = capability.CurrentValue.Trim(),
                SourceId = "machine-context"
            });
        }

        return new PerformanceUniversalConfigurationContext
        {
            SchemaVersion = 1,
            GameId = gameId,
            AdapterId = adapterId,
            Machine = CopyMachine(machine.Fingerprint),
            CapabilityValues = Array.AsReadOnly(values.ToArray()),
            WorkloadConfiguration = new Dictionary<string, string>(),
            DisplayDriverContext = new Dictionary<string, string>()
        };
    }

    public PerformanceUniversalConfigurationContext Rehydrate()
    {
        if (SchemaVersion != 1)
            throw new InvalidDataException($"Unsupported universal performance context schema version '{SchemaVersion}'.");

        var gameId = NormalizeId(GameId);
        var adapterId = NormalizeId(AdapterId);
        if (string.IsNullOrWhiteSpace(gameId))
            throw new InvalidDataException("Universal performance context has no stable GameId.");
        if (string.IsNullOrWhiteSpace(adapterId))
            throw new InvalidDataException("Universal performance context has no resolved AdapterId.");
        if (Machine is null || string.IsNullOrWhiteSpace(Machine.Id))
            throw new InvalidDataException("Universal performance context has no machine fingerprint.");

        var adapterVersion = NormalizeOptional(AdapterVersion);
        var adapterVersionSourceId = NormalizeOptionalId(AdapterVersionSourceId);
        if (adapterVersion is null && adapterVersionSourceId is not null)
            throw new InvalidDataException("Adapter version source cannot exist without an adapter version.");

        var capabilityValues = NormalizeCapabilities(CapabilityValues);
        var workloadConfiguration = NormalizeDictionary(WorkloadConfiguration, "workload configuration");
        var displayDriverContext = NormalizeDictionary(DisplayDriverContext, "display/driver context");

        return this with
        {
            SchemaVersion = 1,
            GameId = gameId,
            AdapterId = adapterId,
            AdapterVersion = adapterVersion,
            AdapterVersionSourceId = adapterVersionSourceId,
            Machine = CopyMachine(Machine),
            CapabilityValues = capabilityValues,
            WorkloadConfiguration = workloadConfiguration,
            DisplayDriverContext = displayDriverContext
        };
    }

    public bool IsEquivalentTo(PerformanceUniversalConfigurationContext? other)
    {
        if (other is null) return false;

        try
        {
            var left = Rehydrate();
            var right = other.Rehydrate();
            return left.SchemaVersion == right.SchemaVersion
                   && string.Equals(left.GameId, right.GameId, StringComparison.Ordinal)
                   && string.Equals(left.AdapterId, right.AdapterId, StringComparison.Ordinal)
                   && string.Equals(left.AdapterVersion, right.AdapterVersion, StringComparison.Ordinal)
                   && string.Equals(left.AdapterVersionSourceId, right.AdapterVersionSourceId, StringComparison.Ordinal)
                   && string.Equals(left.Machine.Id, right.Machine.Id, StringComparison.Ordinal)
                   && left.CapabilityValues.SequenceEqual(right.CapabilityValues)
                   && DictionariesEqual(left.WorkloadConfiguration, right.WorkloadConfiguration)
                   && DictionariesEqual(left.DisplayDriverContext, right.DisplayDriverContext);
        }
        catch (Exception exception) when (exception is InvalidDataException or ArgumentException)
        {
            return false;
        }
    }

    private static IReadOnlyList<PerformanceCapabilityValueSnapshot> NormalizeCapabilities(
        IReadOnlyList<PerformanceCapabilityValueSnapshot>? values)
    {
        var normalized = (values ?? Array.Empty<PerformanceCapabilityValueSnapshot>())
            .Select(value =>
            {
                if (value is null)
                    throw new InvalidDataException("Universal performance context contains a null capability value.");

                var capabilityId = NormalizeId(value.CapabilityId);
                var currentValue = value.Value?.Trim() ?? string.Empty;
                var sourceId = NormalizeId(value.SourceId);
                if (string.IsNullOrWhiteSpace(capabilityId)
                    || string.IsNullOrWhiteSpace(currentValue)
                    || string.IsNullOrWhiteSpace(sourceId))
                    throw new InvalidDataException("Universal performance context contains an incomplete capability value.");

                return new PerformanceCapabilityValueSnapshot
                {
                    CapabilityId = capabilityId,
                    Value = currentValue,
                    SourceId = sourceId
                };
            })
            .OrderBy(value => value.CapabilityId, StringComparer.Ordinal)
            .ToArray();

        if (normalized
            .GroupBy(value => value.CapabilityId, StringComparer.Ordinal)
            .Any(group => group.Count() != 1))
            throw new InvalidDataException("Universal performance context contains duplicate capability ids.");

        return Array.AsReadOnly(normalized);
    }

    private static IReadOnlyDictionary<string, string> NormalizeDictionary(
        IReadOnlyDictionary<string, string>? values,
        string name)
    {
        var normalized = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in values ?? new Dictionary<string, string>())
        {
            var key = NormalizeId(pair.Key);
            var value = pair.Value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException($"Universal performance context contains incomplete {name} evidence.");
            if (!normalized.TryAdd(key, value))
                throw new InvalidDataException($"Universal performance context contains duplicate {name} keys.");
        }

        return new Dictionary<string, string>(normalized, StringComparer.Ordinal);
    }

    private static MachineEnvironmentFingerprintV2 CopyMachine(MachineEnvironmentFingerprintV2 fingerprint)
        => fingerprint with
        {
            Id = fingerprint.Id?.Trim() ?? string.Empty,
            MachineName = fingerprint.MachineName?.Trim() ?? string.Empty,
            WindowsDescription = fingerprint.WindowsDescription?.Trim() ?? string.Empty,
            HardwareSignatures = (fingerprint.HardwareSignatures ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ThenBy(value => value, StringComparer.Ordinal)
                .ToArray()
        };

    private static bool DictionariesEqual(
        IReadOnlyDictionary<string, string> left,
        IReadOnlyDictionary<string, string> right)
        => left.Count == right.Count
           && left.All(pair => right.TryGetValue(pair.Key, out var value)
                               && string.Equals(pair.Value, value, StringComparison.Ordinal));

    private static string NormalizeId(string? value)
        => value?.Trim().ToLowerInvariant() ?? string.Empty;

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeOptionalId(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : NormalizeId(value);
}
