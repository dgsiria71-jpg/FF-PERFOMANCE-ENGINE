using System.Globalization;
using FFPerformanceEngine.Core.Diagnostics;

namespace FFPerformanceEngine.Core.SystemOptimization;

public enum WindowsCapabilityCandidateSource
{
    ExplicitMetadata,
    SchemaGenerated
}

public enum WindowsCapabilityCandidatePlanDisposition
{
    Ready,
    Unavailable,
    MissingCurrentState,
    NoCandidateSpace
}

public sealed record WindowsCapabilityCandidate
{
    public string CapabilityId { get; init; } = string.Empty;
    public string TargetValue { get; init; } = string.Empty;
    public WindowsCapabilityCandidateSource Source { get; init; }
    public int ExplorationRank { get; init; }
}

public sealed record WindowsCapabilityCandidatePlan
{
    public string CapabilityId { get; init; } = string.Empty;
    public string? CurrentValue { get; init; }
    public WindowsCapabilityCandidatePlanDisposition Disposition { get; init; }
    public IReadOnlyList<WindowsCapabilityCandidate> Candidates { get; init; } = Array.Empty<WindowsCapabilityCandidate>();
    public string Reason { get; init; } = string.Empty;

    public bool CanExplore => Disposition == WindowsCapabilityCandidatePlanDisposition.Ready && Candidates.Count > 0;
}

public sealed record WindowsCapabilityCandidatePlannerPolicy
{
    public int MaxNumericCandidates { get; init; } = 5;
}

/// <summary>
/// Converts adapter-declared target metadata into a bounded exploration set.
/// It never mutates Windows and never publishes a recommendation. Generated
/// numeric points are search coverage only; evidence must decide whether any
/// target is beneficial.
/// </summary>
public sealed class WindowsCapabilityCandidatePlanner
{
    private readonly WindowsCapabilityCandidatePlannerPolicy _policy;

    public WindowsCapabilityCandidatePlanner(WindowsCapabilityCandidatePlannerPolicy? policy = null)
    {
        _policy = policy ?? new WindowsCapabilityCandidatePlannerPolicy();
        if (_policy.MaxNumericCandidates < 2)
            throw new ArgumentOutOfRangeException(nameof(policy), "Numeric candidate planning requires a budget of at least two points.");
    }

    public WindowsCapabilityCandidatePlan Build(WindowsPerformanceCapability capability)
    {
        ArgumentNullException.ThrowIfNull(capability);
        var id = NormalizeId(capability.CapabilityId);

        if (capability.Availability != CapabilityAvailability.Available)
            return Skip(id, capability.CurrentValue, WindowsCapabilityCandidatePlanDisposition.Unavailable,
                $"Capability runtime state is {capability.Availability}; exploration is blocked until discovery proves it Available.");

        if (capability.CurrentValue is null)
            return Skip(id, null, WindowsCapabilityCandidatePlanDisposition.MissingCurrentState,
                "Current capability state is unknown, so no controlled baseline can be formed.");

        var explicitValues = capability.AvailableValues
            .Concat(capability.ValueSchema.AllowedValues)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (explicitValues.Length > 0)
            return BuildFromValues(id, capability.CurrentValue, explicitValues, WindowsCapabilityCandidateSource.ExplicitMetadata);

        var generated = capability.ValueSchema.Kind switch
        {
            CapabilityValueKind.Boolean => new[] { "false", "true" },
            CapabilityValueKind.Integer => GenerateNumeric(capability.ValueSchema, integer: true),
            CapabilityValueKind.Number => GenerateNumeric(capability.ValueSchema, integer: false),
            _ => Array.Empty<string>()
        };

        if (generated.Count == 0)
            return Skip(id, capability.CurrentValue, WindowsCapabilityCandidatePlanDisposition.NoCandidateSpace,
                "The capability has no explicit alternatives and its value schema does not define a bounded candidate space.");

        return BuildFromValues(id, capability.CurrentValue, generated, WindowsCapabilityCandidateSource.SchemaGenerated);
    }

    private WindowsCapabilityCandidatePlan BuildFromValues(
        string capabilityId,
        string currentValue,
        IReadOnlyList<string> values,
        WindowsCapabilityCandidateSource source)
    {
        var candidates = new List<WindowsCapabilityCandidate>();
        foreach (var value in values)
        {
            if (IsCurrentValue(currentValue, value)) continue;
            candidates.Add(new WindowsCapabilityCandidate
            {
                CapabilityId = capabilityId,
                TargetValue = value,
                Source = source,
                ExplorationRank = candidates.Count + 1
            });
        }

        return candidates.Count == 0
            ? Skip(capabilityId, currentValue, WindowsCapabilityCandidatePlanDisposition.NoCandidateSpace,
                "Every declared candidate is already equivalent to the current state.")
            : new WindowsCapabilityCandidatePlan
            {
                CapabilityId = capabilityId,
                CurrentValue = currentValue,
                Disposition = WindowsCapabilityCandidatePlanDisposition.Ready,
                Candidates = candidates.ToArray(),
                Reason = source == WindowsCapabilityCandidateSource.ExplicitMetadata
                    ? "Candidate targets come from adapter-declared metadata; no target is recommended by this plan."
                    : "Candidate targets are deterministic bounded exploration points derived from the adapter schema; no target is recommended by this plan."
            };
    }

    private IReadOnlyList<string> GenerateNumeric(CapabilityValueSchema schema, bool integer)
    {
        if (schema.Minimum is not double minimum
            || schema.Maximum is not double maximum
            || schema.Step is not double step
            || !double.IsFinite(minimum)
            || !double.IsFinite(maximum)
            || !double.IsFinite(step)
            || step <= 0
            || minimum > maximum)
            return Array.Empty<string>();

        if (integer && (!IsIntegral(minimum) || !IsIntegral(maximum) || !IsIntegral(step)))
            return Array.Empty<string>();

        var rawSteps = Math.Floor((maximum - minimum) / step + 1e-9);
        if (!double.IsFinite(rawSteps) || rawSteps < 0 || rawSteps > long.MaxValue - 1)
            return Array.Empty<string>();

        var totalSteps = (long)rawSteps;
        var totalPoints = totalSteps + 1;
        var selectedCount = (int)Math.Min(totalPoints, _policy.MaxNumericCandidates);
        if (selectedCount <= 0) return Array.Empty<string>();

        var indices = new SortedSet<long>();
        if (selectedCount == 1)
        {
            indices.Add(0);
        }
        else
        {
            for (var i = 0; i < selectedCount; i++)
            {
                var index = (long)Math.Round(
                    i * (double)totalSteps / (selectedCount - 1),
                    MidpointRounding.AwayFromZero);
                indices.Add(Math.Clamp(index, 0, totalSteps));
            }
        }

        return indices
            .Select(index => minimum + index * step)
            .Select(value => FormatNumeric(value, integer))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsCurrentValue(string currentValue, string targetValue)
    {
        if (string.Equals(currentValue, targetValue, StringComparison.Ordinal)) return true;
        if (double.TryParse(currentValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var current)
            && double.TryParse(targetValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var target)
            && double.IsFinite(current)
            && double.IsFinite(target))
            return Math.Abs(current - target) <= 1e-9;
        return false;
    }

    private static bool IsIntegral(double value)
        => Math.Abs(value - Math.Round(value)) <= 1e-9;

    private static string FormatNumeric(double value, bool integer)
        => integer
            ? Math.Round(value).ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("G17", CultureInfo.InvariantCulture);

    private static WindowsCapabilityCandidatePlan Skip(
        string capabilityId,
        string? currentValue,
        WindowsCapabilityCandidatePlanDisposition disposition,
        string reason)
        => new()
        {
            CapabilityId = capabilityId,
            CurrentValue = currentValue,
            Disposition = disposition,
            Candidates = Array.Empty<WindowsCapabilityCandidate>(),
            Reason = reason
        };

    private static string NormalizeId(string? capabilityId)
        => capabilityId?.Trim().ToLowerInvariant() ?? string.Empty;
}
