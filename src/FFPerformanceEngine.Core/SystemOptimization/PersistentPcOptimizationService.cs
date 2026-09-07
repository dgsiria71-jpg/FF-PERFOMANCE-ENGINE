using FFPerformanceEngine.Core.Diagnostics;

namespace FFPerformanceEngine.Core.SystemOptimization;

public sealed class PersistentPcOptimizationDriftException : InvalidOperationException
{
    public PersistentPcOptimizationDriftException(string message) : base(message)
    {
    }
}

/// <summary>
/// Application service for the persistent "Otimizar este PC" flow. Analysis
/// always refreshes concrete capability state. Apply refreshes it again and
/// rebuilds the recommendation gate before entering the transactional mutation
/// engine, preventing a stale UI preview from becoming authority.
/// </summary>
public sealed class PersistentPcOptimizationService
{
    private readonly PersistentPcOptimizationPlanner _planner;
    private readonly WindowsPerformanceCapabilityDiscoveryService _discovery;
    private readonly Func<MachineContext> _captureMachine;
    private readonly SystemOptimizationTransactionEngine _transactions;

    public PersistentPcOptimizationService(
        PersistentPcOptimizationPlanner planner,
        WindowsPerformanceCapabilityDiscoveryService discovery,
        Func<MachineContext> captureMachine,
        SystemOptimizationTransactionEngine transactions)
    {
        _planner = planner ?? throw new ArgumentNullException(nameof(planner));
        _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
        _captureMachine = captureMachine ?? throw new ArgumentNullException(nameof(captureMachine));
        _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
    }

    public async Task<PersistentPcOptimizationPreview> AnalyzeAsync(
        CancellationToken cancellationToken = default)
    {
        await _discovery.RefreshAsync(cancellationToken).ConfigureAwait(false);
        return _planner.BuildPreview(_captureMachine());
    }

    public async Task<PersistentOptimizationResult> ApplyAsync(
        PersistentPcOptimizationPreview preview,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preview);
        ValidatePreviewIntegrity(preview);
        if (!preview.CanApply)
            throw new InvalidOperationException("Otimizar este PC preview contains no eligible persistent mutations.");

        await _discovery.RefreshAsync(cancellationToken).ConfigureAwait(false);
        var currentMachine = _captureMachine();

        if (!string.Equals(
                preview.MachineFingerprintId,
                currentMachine.Fingerprint.Id,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new PersistentPcOptimizationDriftException(
                "The machine/environment fingerprint changed after the persistent optimization preview was created. Re-analyze before applying.");
        }

        var currentPreview = _planner.BuildPreview(currentMachine);
        var currentReady = currentPreview.ReadyEntries.ToDictionary(
            entry => entry.CapabilityId,
            StringComparer.OrdinalIgnoreCase);

        foreach (var original in preview.ReadyEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!currentReady.TryGetValue(original.CapabilityId, out var current))
            {
                var currentEntry = currentPreview.Entries.FirstOrDefault(entry =>
                    string.Equals(entry.CapabilityId, original.CapabilityId, StringComparison.OrdinalIgnoreCase));
                var disposition = currentEntry?.Disposition.ToString() ?? "missing";
                throw new PersistentPcOptimizationDriftException(
                    $"Capability '{original.CapabilityId}' is no longer eligible for persistent optimization ({disposition}). Re-analyze before applying.");
            }

            if (!string.Equals(
                    original.ExpectedCurrentValue,
                    current.ExpectedCurrentValue,
                    StringComparison.Ordinal))
            {
                throw new PersistentPcOptimizationDriftException(
                    $"Capability '{original.CapabilityId}' changed from the analyzed state '{original.ExpectedCurrentValue}' to '{current.ExpectedCurrentValue}'. Re-analyze before applying.");
            }

            if (!string.Equals(original.TargetValue, current.TargetValue, StringComparison.Ordinal))
            {
                throw new PersistentPcOptimizationDriftException(
                    $"Capability '{original.CapabilityId}' recommendation changed after preview creation. Re-analyze before applying.");
            }
        }

        return await _transactions.ApplyPersistentAsync(
            $"Otimizar este PC · {preview.PlanId:N}",
            preview.Mutations,
            cancellationToken).ConfigureAwait(false);
    }

    public Task<SystemOptimizationRestoreResult> RestoreAsync(
        Guid restorePointId,
        CancellationToken cancellationToken = default)
        => _transactions.RestoreAsync(restorePointId, cancellationToken);

    private static void ValidatePreviewIntegrity(PersistentPcOptimizationPreview preview)
    {
        if (preview.PlanId == Guid.Empty)
            throw new InvalidDataException("Persistent optimization preview has no plan identity.");
        if (string.IsNullOrWhiteSpace(preview.MachineFingerprintId))
            throw new InvalidDataException("Persistent optimization preview has no machine fingerprint.");

        var ready = preview.ReadyEntries.ToDictionary(
            entry => entry.CapabilityId,
            StringComparer.OrdinalIgnoreCase);
        if (ready.Count != preview.ReadyEntries.Count)
            throw new InvalidDataException("Persistent optimization preview contains duplicate ready capabilities.");
        if (preview.Mutations.Count != ready.Count)
            throw new InvalidDataException("Persistent optimization preview mutation set does not match its ready entries.");

        foreach (var mutation in preview.Mutations)
        {
            if (!ready.TryGetValue(mutation.CapabilityId, out var entry)
                || !entry.IsReady
                || string.IsNullOrWhiteSpace(entry.TargetValue)
                || !string.Equals(entry.TargetValue, mutation.TargetValue, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Persistent optimization preview mutation '{mutation.CapabilityId}' is not backed by an identical ready entry.");
            }
        }
    }
}
