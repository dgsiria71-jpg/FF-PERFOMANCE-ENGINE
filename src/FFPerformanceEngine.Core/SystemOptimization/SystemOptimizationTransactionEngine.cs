using System.Runtime.ExceptionServices;
using System.Text.Json;
using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

namespace FFPerformanceEngine.Core.SystemOptimization;

public sealed class SystemOptimizationTransactionEngine
{
    private readonly WindowsPerformanceCapabilityRegistry _capabilities;
    private readonly WindowsCapabilityMutationAdapterRegistry _adapters;
    private readonly SnapshotService _snapshots;
    private readonly HistoryService _history;
    private readonly SemaphoreSlim _transactionGate = new(1, 1);

    public SystemOptimizationTransactionEngine(
        WindowsPerformanceCapabilityRegistry capabilities,
        WindowsCapabilityMutationAdapterRegistry adapters,
        SnapshotService snapshots,
        HistoryService history)
    {
        _capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
        _adapters = adapters ?? throw new ArgumentNullException(nameof(adapters));
        _snapshots = snapshots ?? throw new ArgumentNullException(nameof(snapshots));
        _history = history ?? throw new ArgumentNullException(nameof(history));
    }

    public async Task<SystemOptimizationSession> BeginSessionAsync(
        string label,
        IReadOnlyList<WindowsMutationRequest> mutations,
        CancellationToken cancellationToken = default)
    {
        await _transactionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var prepared = await PrepareAsync(label, SystemOptimizationScope.Session, mutations, cancellationToken).ConfigureAwait(false);
            await ApplyPreparedAsync(prepared, cancellationToken).ConfigureAwait(false);
            return new SystemOptimizationSession(this, prepared.TransactionId, prepared.RestorePointId, prepared.Label);
        }
        finally
        {
            _transactionGate.Release();
        }
    }

    public async Task<PersistentOptimizationResult> ApplyPersistentAsync(
        string label,
        IReadOnlyList<WindowsMutationRequest> mutations,
        CancellationToken cancellationToken = default)
    {
        await _transactionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var prepared = await PrepareAsync(label, SystemOptimizationScope.Persistent, mutations, cancellationToken).ConfigureAwait(false);
            await ApplyPreparedAsync(prepared, cancellationToken).ConfigureAwait(false);
            return new PersistentOptimizationResult(
                true,
                prepared.TransactionId,
                prepared.RestorePointId,
                $"Persistent system optimization '{prepared.Label}' applied and verified.");
        }
        finally
        {
            _transactionGate.Release();
        }
    }

    public async Task<SystemOptimizationRestoreResult> RestoreAsync(
        Guid restorePointId,
        CancellationToken cancellationToken = default)
    {
        if (restorePointId == Guid.Empty) throw new ArgumentException("A restore point identity is required.", nameof(restorePointId));
        cancellationToken.ThrowIfCancellationRequested();

        await _transactionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var stored = (await _snapshots.LoadAsync(cancellationToken).ConfigureAwait(false))
                .FirstOrDefault(snapshot => snapshot.Id == restorePointId)
                ?? throw new KeyNotFoundException($"System optimization restore point '{restorePointId:D}' was not found.");
            if (!stored.Values.TryGetValue(SystemOptimizationSnapshotCodec.PayloadKey, out var payload))
                throw new InvalidDataException($"Snapshot '{restorePointId:D}' is not a DG system optimization restore point.");

            var envelope = SystemOptimizationSnapshotCodec.Decode(payload);
            var failures = await RollbackEnvelopeAsync(envelope).ConfigureAwait(false);
            if (failures.Count > 0)
            {
                await AppendHistoryAsync(
                    envelope,
                    restorePointId,
                    "restore-incomplete",
                    $"System optimization restore incomplete; {failures.Count} rollback error(s) require attention.",
                    CancellationToken.None).ConfigureAwait(false);
                throw new AggregateException("System optimization restore could not return every capability to its original state.", failures);
            }

            await AppendHistoryAsync(
                envelope,
                restorePointId,
                "restored",
                $"System optimization '{envelope.Label}' restored to its pre-transaction state.",
                CancellationToken.None).ConfigureAwait(false);

            return new SystemOptimizationRestoreResult(
                true,
                envelope.TransactionId,
                restorePointId,
                "Original Windows capability state restored and verified.");
        }
        finally
        {
            _transactionGate.Release();
        }
    }

    private async Task<PreparedTransaction> PrepareAsync(
        string label,
        SystemOptimizationScope scope,
        IReadOnlyList<WindowsMutationRequest> mutations,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("A system optimization transaction label is required.", nameof(label));
        ArgumentNullException.ThrowIfNull(mutations);
        if (mutations.Count == 0) throw new ArgumentException("At least one Windows capability mutation is required.", nameof(mutations));

        var requested = new Dictionary<string, WindowsMutationRequest>(StringComparer.OrdinalIgnoreCase);
        foreach (var mutation in mutations)
        {
            ArgumentNullException.ThrowIfNull(mutation);
            var id = NormalizeId(mutation.CapabilityId);
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Every mutation requires a CapabilityId.", nameof(mutations));
            if (!requested.TryAdd(id, mutation with { CapabilityId = id }))
                throw new InvalidOperationException($"Capability '{id}' appears more than once in the same transaction.");
        }

        var capabilityMap = _capabilities.GetAll().ToDictionary(item => item.CapabilityId, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in requested)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!capabilityMap.TryGetValue(pair.Key, out var capability))
                throw new InvalidOperationException($"Unknown Windows performance capability '{pair.Key}'.");
            if (capability.Availability != CapabilityAvailability.Available)
                throw new InvalidOperationException($"Windows performance capability '{pair.Key}' is {capability.Availability}; no mutation will be attempted until a concrete adapter verifies availability.");
            ValidateScope(capability, scope);

            var adapter = _adapters.GetRequired(pair.Key);
            var validation = adapter.Validate(pair.Value.TargetValue, scope);
            if (!validation.Success)
                throw new InvalidOperationException($"Windows capability '{pair.Key}' rejected target '{pair.Value.TargetValue}': {validation.Message}");

            var current = await adapter.ReadCurrentAsync(cancellationToken).ConfigureAwait(false);
            if (!current.Success)
                throw new InvalidOperationException($"Windows capability '{pair.Key}' current state could not be read: {current.Message}");
        }

        var plan = _capabilities.ResolvePlan(requested.Keys);
        if (!plan.IsValid)
            throw new InvalidOperationException("Windows capability graph rejected the transaction: " + string.Join("; ", plan.Issues.Select(issue => issue.Message)));

        var orderedIds = plan.OrderedCapabilities
            .Select(capability => capability.CapabilityId)
            .Where(requested.ContainsKey)
            .ToArray();
        if (orderedIds.Length != requested.Count)
            throw new InvalidOperationException("Windows capability planner did not resolve every requested mutation.");

        // Critical invariant: collect every original state before applying the first mutation.
        var entries = new List<PreparedMutation>(orderedIds.Length);
        foreach (var id in orderedIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var adapter = _adapters.GetRequired(id);
            var snapshot = await adapter.SnapshotAsync(cancellationToken).ConfigureAwait(false);
            if (!string.Equals(NormalizeId(snapshot.CapabilityId), id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Adapter '{id}' returned a snapshot for '{snapshot.CapabilityId}'.");
            entries.Add(new PreparedMutation(requested[id], adapter, snapshot));
        }

        var transactionId = Guid.NewGuid();
        var envelope = new SystemOptimizationRestoreEnvelope
        {
            TransactionId = transactionId,
            Label = label.Trim(),
            Scope = scope,
            CreatedAt = DateTimeOffset.UtcNow,
            Entries = entries.Select(entry => new SystemOptimizationRestoreEntry
            {
                CapabilityId = entry.Request.CapabilityId,
                TargetValue = entry.Request.TargetValue,
                OriginalValue = entry.Snapshot.OriginalValue,
                RestorePayload = entry.Snapshot.RestorePayload
            }).ToList()
        };
        var restorePoint = await _snapshots.CreateAsync(
            $"DG System Restore · {label.Trim()}",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [SystemOptimizationSnapshotCodec.PayloadKey] = SystemOptimizationSnapshotCodec.Encode(envelope)
            },
            cancellationToken).ConfigureAwait(false);

        return new PreparedTransaction(transactionId, restorePoint.Id, label.Trim(), scope, entries, envelope);
    }

    private async Task ApplyPreparedAsync(PreparedTransaction prepared, CancellationToken cancellationToken)
    {
        var applied = new List<PreparedMutation>(prepared.Entries.Count);
        Exception? primaryFailure = null;
        try
        {
            foreach (var entry in prepared.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Treat the step as potentially mutated before calling Apply: an adapter may
                // change external state and then throw. Rollback to the pre-captured snapshot
                // is therefore always safe and preferred over assuming nothing happened.
                applied.Add(entry);
                var apply = await entry.Adapter.ApplyAsync(entry.Request.TargetValue, cancellationToken).ConfigureAwait(false);
                if (!apply.Success)
                    throw new InvalidOperationException($"Windows capability '{entry.Request.CapabilityId}' failed to apply: {apply.Message}");

                var verified = await entry.Adapter.VerifyAsync(entry.Request.TargetValue, cancellationToken).ConfigureAwait(false);
                if (!verified)
                    throw new InvalidOperationException($"Windows capability '{entry.Request.CapabilityId}' did not verify the requested state after apply.");
            }
        }
        catch (Exception exception)
        {
            primaryFailure = exception;
        }

        if (primaryFailure is not null)
        {
            var rollbackFailures = await RollbackPreparedAsync(applied).ConfigureAwait(false);
            await AppendHistoryAsync(
                prepared.Envelope,
                prepared.RestorePointId,
                rollbackFailures.Count == 0 ? "rollback" : "rollback-incomplete",
                rollbackFailures.Count == 0
                    ? $"Rollback completed after system optimization apply/verification failure: {primaryFailure.Message}"
                    : $"Rollback incomplete after system optimization failure: {primaryFailure.Message}",
                CancellationToken.None).ConfigureAwait(false);

            if (rollbackFailures.Count > 0)
            {
                var all = new List<Exception> { primaryFailure };
                all.AddRange(rollbackFailures);
                throw new AggregateException("System optimization failed and rollback was incomplete.", all);
            }

            ExceptionDispatchInfo.Capture(primaryFailure).Throw();
        }

        await AppendHistoryAsync(
            prepared.Envelope,
            prepared.RestorePointId,
            "applied",
            $"System optimization '{prepared.Label}' applied and verified as a {prepared.Scope} transaction.",
            CancellationToken.None).ConfigureAwait(false);
    }

    private static async Task<List<Exception>> RollbackPreparedAsync(IReadOnlyList<PreparedMutation> applied)
    {
        var failures = new List<Exception>();
        for (var index = applied.Count - 1; index >= 0; index--)
        {
            var entry = applied[index];
            try
            {
                await entry.Adapter.RollbackAsync(entry.Snapshot, CancellationToken.None).ConfigureAwait(false);
                await VerifyRollbackAsync(entry.Adapter, entry.Snapshot).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                failures.Add(new InvalidOperationException($"Rollback failed for capability '{entry.Request.CapabilityId}'.", exception));
            }
        }
        return failures;
    }

    private async Task<List<Exception>> RollbackEnvelopeAsync(SystemOptimizationRestoreEnvelope envelope)
    {
        var failures = new List<Exception>();
        for (var index = envelope.Entries.Count - 1; index >= 0; index--)
        {
            var entry = envelope.Entries[index];
            try
            {
                var adapter = _adapters.GetRequired(entry.CapabilityId);
                var snapshot = new WindowsCapabilityMutationSnapshot(entry.CapabilityId, entry.OriginalValue, entry.RestorePayload);
                await adapter.RollbackAsync(snapshot, CancellationToken.None).ConfigureAwait(false);
                await VerifyRollbackAsync(adapter, snapshot).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                failures.Add(new InvalidOperationException($"Restore failed for capability '{entry.CapabilityId}'.", exception));
            }
        }
        return failures;
    }

    private static async Task VerifyRollbackAsync(
        IWindowsCapabilityMutationAdapter adapter,
        WindowsCapabilityMutationSnapshot snapshot)
    {
        var current = await adapter.ReadCurrentAsync(CancellationToken.None).ConfigureAwait(false);
        if (!current.Success)
            throw new InvalidOperationException($"Capability '{adapter.CapabilityId}' could not be read after rollback: {current.Message}");
        if (snapshot.OriginalValue is not null
            && !string.Equals(current.Value, snapshot.OriginalValue, StringComparison.Ordinal))
            throw new InvalidOperationException($"Capability '{adapter.CapabilityId}' rollback verification mismatch. Expected '{snapshot.OriginalValue}', read '{current.Value}'.");
    }

    private async Task AppendHistoryAsync(
        SystemOptimizationRestoreEnvelope envelope,
        Guid restorePointId,
        string status,
        string summary,
        CancellationToken cancellationToken)
    {
        await _history.AppendAsync(new HistoryEvent
        {
            Kind = HistoryEventKind.System,
            Title = envelope.Scope == SystemOptimizationScope.Persistent
                ? "DG · Otimizar este PC"
                : "DG · Otimização de sessão",
            Summary = summary,
            DetailsJson = JsonSerializer.Serialize(new
            {
                transactionId = envelope.TransactionId,
                restorePointId,
                scope = envelope.Scope.ToString(),
                status,
                capabilities = envelope.Entries.Select(entry => entry.CapabilityId).ToArray()
            })
        }, cancellationToken).ConfigureAwait(false);
    }

    private static void ValidateScope(WindowsPerformanceCapability capability, SystemOptimizationScope scope)
    {
        var allowed = scope switch
        {
            SystemOptimizationScope.Session => capability.PersistenceScope is CapabilityPersistenceScope.SessionOnly or CapabilityPersistenceScope.PersistentAllowed,
            SystemOptimizationScope.Persistent => capability.PersistenceScope is CapabilityPersistenceScope.PersistentAllowed or CapabilityPersistenceScope.PersistentOnly,
            _ => false
        };
        if (!allowed)
            throw new InvalidOperationException($"Capability '{capability.CapabilityId}' with scope {capability.PersistenceScope} cannot participate in a {scope} transaction.");
    }

    private static string NormalizeId(string? capabilityId)
        => capabilityId?.Trim().ToLowerInvariant() ?? string.Empty;

    private sealed record PreparedMutation(
        WindowsMutationRequest Request,
        IWindowsCapabilityMutationAdapter Adapter,
        WindowsCapabilityMutationSnapshot Snapshot);

    private sealed record PreparedTransaction(
        Guid TransactionId,
        Guid RestorePointId,
        string Label,
        SystemOptimizationScope Scope,
        IReadOnlyList<PreparedMutation> Entries,
        SystemOptimizationRestoreEnvelope Envelope);
}

public sealed class SystemOptimizationSession : IAsyncDisposable
{
    private readonly SystemOptimizationTransactionEngine _engine;
    private int _restored;

    internal SystemOptimizationSession(
        SystemOptimizationTransactionEngine engine,
        Guid transactionId,
        Guid restorePointId,
        string label)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        TransactionId = transactionId;
        RestorePointId = restorePointId;
        Label = label;
    }

    public Guid TransactionId { get; }
    public Guid RestorePointId { get; }
    public string Label { get; }
    public bool IsRestored => Volatile.Read(ref _restored) != 0;

    public async Task RestoreAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _restored, 1, 0) != 0) return;
        try
        {
            await _engine.RestoreAsync(RestorePointId, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            Volatile.Write(ref _restored, 0);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (IsRestored) return;
        await RestoreAsync(CancellationToken.None).ConfigureAwait(false);
    }
}
