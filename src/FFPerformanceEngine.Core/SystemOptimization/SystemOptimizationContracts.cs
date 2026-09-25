namespace FFPerformanceEngine.Core.SystemOptimization;

public enum SystemOptimizationScope
{
    Session,
    Persistent
}

/// <summary>
/// Describes one requested Windows capability mutation. ExpectedCurrentValue is
/// an optional compare-and-set precondition: when supplied, the transaction
/// engine must prove that exact state again before it may create a durable
/// restore point or apply the target.
/// </summary>
public sealed record WindowsMutationRequest(
    string CapabilityId,
    string TargetValue,
    string? ExpectedCurrentValue = null);

public sealed record WindowsCapabilityReadResult(bool Success, string? Value, string Message)
{
    public static WindowsCapabilityReadResult Ok(string? value, string message = "read") => new(true, value, message);
    public static WindowsCapabilityReadResult Fail(string message) => new(false, null, message);
}

public sealed record WindowsCapabilityValidationResult(bool Success, string Message)
{
    public static WindowsCapabilityValidationResult Ok(string message = "valid") => new(true, message);
    public static WindowsCapabilityValidationResult Fail(string message) => new(false, message);
}

public sealed record WindowsCapabilityMutationSnapshot(
    string CapabilityId,
    string? OriginalValue,
    string RestorePayload);

public sealed record WindowsCapabilityApplyResult(bool Success, string Message)
{
    public static WindowsCapabilityApplyResult Ok(string message = "applied") => new(true, message);
    public static WindowsCapabilityApplyResult Fail(string message) => new(false, message);
}

public interface IWindowsCapabilityMutationAdapter
{
    string CapabilityId { get; }

    Task<WindowsCapabilityReadResult> ReadCurrentAsync(CancellationToken cancellationToken = default);

    WindowsCapabilityValidationResult Validate(
        string targetValue,
        SystemOptimizationScope scope);

    Task<WindowsCapabilityMutationSnapshot> SnapshotAsync(CancellationToken cancellationToken = default);

    Task<WindowsCapabilityApplyResult> ApplyAsync(
        string targetValue,
        CancellationToken cancellationToken = default);

    Task<bool> VerifyAsync(
        string targetValue,
        CancellationToken cancellationToken = default);

    Task RollbackAsync(
        WindowsCapabilityMutationSnapshot snapshot,
        CancellationToken cancellationToken = default);
}

public sealed class WindowsCapabilityMutationAdapterRegistry
{
    private readonly Dictionary<string, IWindowsCapabilityMutationAdapter> _adapters;

    public WindowsCapabilityMutationAdapterRegistry(IEnumerable<IWindowsCapabilityMutationAdapter> adapters)
    {
        ArgumentNullException.ThrowIfNull(adapters);
        _adapters = new Dictionary<string, IWindowsCapabilityMutationAdapter>(StringComparer.OrdinalIgnoreCase);
        foreach (var adapter in adapters)
        {
            ArgumentNullException.ThrowIfNull(adapter);
            var id = Normalize(adapter.CapabilityId);
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Every Windows mutation adapter requires a non-empty CapabilityId.", nameof(adapters));
            if (!_adapters.TryAdd(id, adapter))
                throw new ArgumentException($"Duplicate Windows mutation adapter '{id}'.", nameof(adapters));
        }
    }

    public bool TryGet(string capabilityId, out IWindowsCapabilityMutationAdapter adapter)
        => _adapters.TryGetValue(Normalize(capabilityId), out adapter!);

    public IWindowsCapabilityMutationAdapter GetRequired(string capabilityId)
        => TryGet(capabilityId, out var adapter)
            ? adapter
            : throw new InvalidOperationException($"No Windows mutation adapter is registered for capability '{capabilityId}'.");

    public IReadOnlyList<string> CapabilityIds
        => _adapters.Keys.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();

    private static string Normalize(string? capabilityId)
        => capabilityId?.Trim().ToLowerInvariant() ?? string.Empty;
}

public sealed record PersistentOptimizationResult(
    bool Success,
    Guid TransactionId,
    Guid RestorePointId,
    string Message);

public sealed record SystemOptimizationRestoreResult(
    bool Success,
    Guid TransactionId,
    Guid RestorePointId,
    string Message);
