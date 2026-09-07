using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Diagnostics;

public sealed class WindowsPerformanceCapabilityRegistry
{
    private readonly Dictionary<string, WindowsPerformanceCapability> _capabilities;

    public WindowsPerformanceCapabilityRegistry()
        : this(CreateBuiltInCatalog())
    {
    }

    public WindowsPerformanceCapabilityRegistry(IEnumerable<WindowsPerformanceCapability> capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        _capabilities = new Dictionary<string, WindowsPerformanceCapability>(StringComparer.OrdinalIgnoreCase);
        foreach (var capability in capabilities)
        {
            ArgumentNullException.ThrowIfNull(capability);
            var id = NormalizeId(capability.CapabilityId);
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Every Windows performance capability requires a non-empty CapabilityId.", nameof(capabilities));
            if (_capabilities.ContainsKey(id))
                throw new ArgumentException($"Duplicate Windows performance capability '{id}'.", nameof(capabilities));
            _capabilities.Add(id, capability.CloneDescriptor().WithId(id));
        }
    }

    public IReadOnlyList<WindowsPerformanceCapability> GetAll()
        => _capabilities.Values
            .OrderBy(capability => capability.CapabilityId, StringComparer.OrdinalIgnoreCase)
            .Select(capability => capability.CloneDescriptor())
            .ToArray();

    public IReadOnlyList<CapabilityGraphIssue> ValidateGraph()
    {
        var issues = new List<CapabilityGraphIssue>();
        foreach (var capability in _capabilities.Values)
        {
            foreach (var dependency in capability.Dependencies)
            {
                var normalized = NormalizeId(dependency);
                if (!_capabilities.ContainsKey(normalized))
                    issues.Add(Issue("missing_dependency", $"Capability '{capability.CapabilityId}' depends on unknown capability '{dependency}'.", capability.CapabilityId, dependency));
                if (string.Equals(capability.CapabilityId, normalized, StringComparison.OrdinalIgnoreCase))
                    issues.Add(Issue("self_dependency", $"Capability '{capability.CapabilityId}' cannot depend on itself.", capability.CapabilityId));
            }

            foreach (var conflict in capability.Conflicts)
            {
                var normalized = NormalizeId(conflict);
                if (!_capabilities.ContainsKey(normalized))
                    issues.Add(Issue("missing_conflict_target", $"Capability '{capability.CapabilityId}' references unknown conflict '{conflict}'.", capability.CapabilityId, conflict));
                if (string.Equals(capability.CapabilityId, normalized, StringComparison.OrdinalIgnoreCase))
                    issues.Add(Issue("self_conflict", $"Capability '{capability.CapabilityId}' cannot conflict with itself.", capability.CapabilityId));
            }
        }

        var state = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var path = new Stack<string>();
        foreach (var id in _capabilities.Keys.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
            DetectCycle(id, state, path, issues);

        return Deduplicate(issues);
    }

    public CapabilityPlanResolution ResolvePlan(IEnumerable<string> capabilityIds)
    {
        ArgumentNullException.ThrowIfNull(capabilityIds);
        var requested = capabilityIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(NormalizeId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var issues = new List<CapabilityGraphIssue>();
        var ordered = new List<WindowsPerformanceCapability>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var id in requested)
            VisitForPlan(id, visited, visiting, ordered, issues);

        var resolvedIds = ordered.Select(capability => capability.CapabilityId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var capability in ordered)
        {
            foreach (var conflict in capability.Conflicts.Select(NormalizeId))
            {
                if (!resolvedIds.Contains(conflict)) continue;
                issues.Add(Issue(
                    "conflict",
                    $"Capability '{capability.CapabilityId}' conflicts with '{conflict}'.",
                    capability.CapabilityId,
                    conflict));
            }
        }

        var finalIssues = Deduplicate(issues);
        return new CapabilityPlanResolution
        {
            IsValid = finalIssues.Count == 0,
            OrderedCapabilities = finalIssues.Count == 0
                ? ordered.Select(capability => capability.CloneDescriptor()).ToArray()
                : Array.Empty<WindowsPerformanceCapability>(),
            Issues = finalIssues
        };
    }

    private void VisitForPlan(
        string id,
        ISet<string> visited,
        ISet<string> visiting,
        ICollection<WindowsPerformanceCapability> ordered,
        ICollection<CapabilityGraphIssue> issues)
    {
        if (visited.Contains(id)) return;
        if (!_capabilities.TryGetValue(id, out var capability))
        {
            issues.Add(Issue("unknown_capability", $"Unknown capability '{id}'.", id));
            return;
        }
        if (!visiting.Add(id))
        {
            issues.Add(Issue("dependency_cycle", $"Dependency cycle detected at '{id}'.", id));
            return;
        }

        foreach (var dependency in capability.Dependencies.Select(NormalizeId))
            VisitForPlan(dependency, visited, visiting, ordered, issues);

        visiting.Remove(id);
        if (issues.Any(issue => issue.Code == "dependency_cycle" || issue.Code == "unknown_capability")) return;
        if (visited.Add(id)) ordered.Add(capability);
    }

    private void DetectCycle(
        string id,
        IDictionary<string, int> state,
        Stack<string> path,
        ICollection<CapabilityGraphIssue> issues)
    {
        if (state.TryGetValue(id, out var existing))
        {
            if (existing == 1)
            {
                var cycle = path.Reverse().SkipWhile(item => !string.Equals(item, id, StringComparison.OrdinalIgnoreCase)).Append(id).ToArray();
                issues.Add(new CapabilityGraphIssue
                {
                    Code = "dependency_cycle",
                    Message = $"Dependency cycle detected: {string.Join(" -> ", cycle)}.",
                    CapabilityIds = cycle
                });
            }
            return;
        }

        state[id] = 1;
        path.Push(id);
        foreach (var dependency in _capabilities[id].Dependencies.Select(NormalizeId))
        {
            if (_capabilities.ContainsKey(dependency)) DetectCycle(dependency, state, path, issues);
        }
        path.Pop();
        state[id] = 2;
    }

    private static IReadOnlyList<CapabilityGraphIssue> Deduplicate(IEnumerable<CapabilityGraphIssue> issues)
        => issues
            .GroupBy(issue => $"{issue.Code}|{string.Join('|', issue.CapabilityIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase))}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(issue => issue.Code, StringComparer.OrdinalIgnoreCase)
            .ThenBy(issue => issue.Message, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static CapabilityGraphIssue Issue(string code, string message, params string[] ids)
        => new()
        {
            Code = code,
            Message = message,
            CapabilityIds = ids.Select(NormalizeId).ToArray()
        };

    private static string NormalizeId(string? id) => id?.Trim().ToLowerInvariant() ?? string.Empty;

    private static IReadOnlyList<WindowsPerformanceCapability> CreateBuiltInCatalog()
        =>
        [
            Seed("windows.power.active_policy", CapabilityDomain.Power, "Active power policy", "Selects the effective Windows power policy used as the parent for processor power capabilities.", CapabilityPersistenceScope.PersistentAllowed, ActionSafety.LobbySafe),
            Seed("windows.cpu.boost_policy", CapabilityDomain.Cpu, "CPU boost policy", "Represents processor boost behavior exposed through supported Windows/platform power controls.", CapabilityPersistenceScope.PersistentAllowed, ActionSafety.LobbySafe, dependencies: ["windows.power.active_policy"]),
            Seed("windows.cpu.core_parking_policy", CapabilityDomain.Cpu, "Core parking policy", "Represents supported Windows core parking policy without assuming that disabling parking is universally beneficial.", CapabilityPersistenceScope.PersistentAllowed, ActionSafety.LobbySafe, dependencies: ["windows.power.active_policy"]),
            Seed("windows.cpu.scheduler_policy", CapabilityDomain.Cpu, "Scheduler policy", "Represents workload scheduling/topology policy selected by a concrete scheduler adapter.", CapabilityPersistenceScope.SessionOnly, ActionSafety.LiveSafe),
            Seed("windows.cpu.process_priority", CapabilityDomain.Process, "Workload process priority", "Represents process priority for the active workload and remains session-scoped.", CapabilityPersistenceScope.SessionOnly, ActionSafety.LiveSafe),
            Seed("windows.io.process_priority", CapabilityDomain.StorageIo, "Workload I/O priority", "Represents workload I/O priority where a concrete adapter can read and safely change it.", CapabilityPersistenceScope.SessionOnly, ActionSafety.LiveSafe),
            Seed("windows.background.process_containment", CapabilityDomain.Process, "Background process containment", "Represents reversible reduction or suspension of classified nonessential background work.", CapabilityPersistenceScope.SessionOnly, ActionSafety.LiveSafe),
            Seed("windows.tasks.session_deferral", CapabilityDomain.ScheduledTask, "Scheduled task session deferral", "Represents reversible deferral of eligible scheduled tasks during a performance session.", CapabilityPersistenceScope.SessionOnly, ActionSafety.LobbySafe),
            Seed("windows.graphics.gpu_preference", CapabilityDomain.Graphics, "GPU preference", "Represents the Windows/driver-exposed GPU preference for a workload.", CapabilityPersistenceScope.PersistentAllowed, ActionSafety.LobbySafe),
            Seed("windows.memory.background_pressure", CapabilityDomain.Memory, "Background memory pressure policy", "Represents reversible containment of background memory consumers; it is not an indiscriminate RAM cleaner.", CapabilityPersistenceScope.SessionOnly, ActionSafety.LiveSafe),
            Seed("windows.display.presentation_policy", CapabilityDomain.Display, "Presentation policy", "Represents supported display/presentation behavior selected by a display capability adapter.", CapabilityPersistenceScope.SessionOnly, ActionSafety.LiveSafe)
        ];

    private static WindowsPerformanceCapability Seed(
        string id,
        CapabilityDomain domain,
        string name,
        string description,
        CapabilityPersistenceScope persistence,
        ActionSafety safety,
        IReadOnlyList<string>? dependencies = null)
        => new()
        {
            CapabilityId = id,
            Domain = domain,
            Name = name,
            Description = description,
            PersistenceScope = persistence,
            Safety = safety,
            RiskLevel = CapabilityRiskLevel.Safe,
            Availability = CapabilityAvailability.Unknown,
            CurrentValue = null,
            Dependencies = dependencies ?? Array.Empty<string>(),
            Execution = new CapabilityExecutionMetadata(),
            Evidence = new CapabilityEvidenceSummary(),
            PerformanceModel = new CapabilityPerformanceModel()
        };
}

internal static class WindowsPerformanceCapabilityCloneExtensions
{
    internal static WindowsPerformanceCapability WithId(this WindowsPerformanceCapability capability, string id)
    {
        capability.CapabilityId = id;
        return capability;
    }
}
