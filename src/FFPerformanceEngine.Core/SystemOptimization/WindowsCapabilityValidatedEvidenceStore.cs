using FFPerformanceEngine.Core.Services;

namespace FFPerformanceEngine.Core.SystemOptimization;

public sealed class WindowsCapabilityValidatedEvidenceCollection
{
    public List<WindowsCapabilityValidatedEvidence> Records { get; init; } = new();
}

/// <summary>
/// Durable local history of explicit Windows capability ValidatedEvidence.
/// Records remain evidence only: persistence here does not publish or mutate a
/// Windows capability recommendation. Exact tuple filtering prevents evidence
/// from another machine/workload from becoming the local latest result.
/// </summary>
public sealed class WindowsCapabilityValidatedEvidenceStore
{
    private const int MaxRecords = 2000;

    private readonly JsonStore<WindowsCapabilityValidatedEvidenceCollection> _store;
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public WindowsCapabilityValidatedEvidenceStore(string? path = null)
        => _store = new JsonStore<WindowsCapabilityValidatedEvidenceCollection>(
            path ?? Path.Combine(AppPaths.Root, "windows-capability-validated-evidence.json"));

    public async Task SaveAsync(
        WindowsCapabilityValidatedEvidence evidence,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        Validate(evidence);

        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var collection = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
            collection.Records.Add(Normalize(evidence));
            if (collection.Records.Count > MaxRecords)
            {
                collection.Records.RemoveRange(
                    0,
                    collection.Records.Count - MaxRecords);
            }

            await _store.SaveAsync(collection, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public async Task<IReadOnlyList<WindowsCapabilityValidatedEvidence>> LoadAsync(
        CancellationToken cancellationToken = default)
        => (await _store.LoadAsync(cancellationToken).ConfigureAwait(false))
            .Records
            .OrderByDescending(record => record.ValidatedAt)
            .ToArray();

    public async Task<WindowsCapabilityValidatedEvidence?> GetLatestAsync(
        string capabilityId,
        string baselineValue,
        string candidateTarget,
        string machineFingerprintId,
        string workloadKey,
        CancellationToken cancellationToken = default)
    {
        var id = NormalizeId(capabilityId);
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("A capability id is required.", nameof(capabilityId));
        if (string.IsNullOrWhiteSpace(baselineValue))
            throw new ArgumentException("A baseline value is required.", nameof(baselineValue));
        if (string.IsNullOrWhiteSpace(candidateTarget))
            throw new ArgumentException("A candidate target is required.", nameof(candidateTarget));
        if (string.IsNullOrWhiteSpace(machineFingerprintId))
            throw new ArgumentException("A machine fingerprint id is required.", nameof(machineFingerprintId));
        if (string.IsNullOrWhiteSpace(workloadKey))
            throw new ArgumentException("A workload key is required.", nameof(workloadKey));

        var machine = machineFingerprintId.Trim();
        var workload = workloadKey.Trim();
        var collection = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
        return collection.Records
            .Where(record => string.Equals(record.CapabilityId, id, StringComparison.OrdinalIgnoreCase)
                             && string.Equals(record.BaselineValue, baselineValue, StringComparison.Ordinal)
                             && string.Equals(record.CandidateTarget, candidateTarget, StringComparison.Ordinal)
                             && string.Equals(record.MachineFingerprintId, machine, StringComparison.OrdinalIgnoreCase)
                             && string.Equals(record.WorkloadKey, workload, StringComparison.Ordinal))
            .OrderByDescending(record => record.ValidatedAt)
            .FirstOrDefault();
    }

    private static void Validate(WindowsCapabilityValidatedEvidence evidence)
    {
        if (string.IsNullOrWhiteSpace(evidence.CapabilityId))
            throw new ArgumentException("ValidatedEvidence requires a capability id.", nameof(evidence));
        if (string.IsNullOrWhiteSpace(evidence.BaselineValue))
            throw new ArgumentException("ValidatedEvidence requires a baseline value.", nameof(evidence));
        if (string.IsNullOrWhiteSpace(evidence.CandidateTarget))
            throw new ArgumentException("ValidatedEvidence requires a candidate target.", nameof(evidence));
        if (string.IsNullOrWhiteSpace(evidence.MachineFingerprintId))
            throw new ArgumentException("ValidatedEvidence requires a machine fingerprint id.", nameof(evidence));
        if (string.IsNullOrWhiteSpace(evidence.WorkloadKey))
            throw new ArgumentException("ValidatedEvidence requires a workload key.", nameof(evidence));
        if (evidence.Source != WindowsCapabilityValidatedEvidenceSource.ValidatedEvidence)
            throw new ArgumentException("ValidatedEvidence source is invalid.", nameof(evidence));
        if (evidence.ObservationCount < 3)
            throw new ArgumentException("ValidatedEvidence requires at least three controlled observations.", nameof(evidence));
        if (!double.IsFinite(evidence.Consistency)
            || evidence.Consistency <= 0
            || evidence.Consistency > 1)
        {
            throw new ArgumentException("ValidatedEvidence consistency must be finite and within (0, 1].", nameof(evidence));
        }
        if (evidence.ValidatedAt == default)
            throw new ArgumentException("ValidatedEvidence requires a validation timestamp.", nameof(evidence));
    }

    private static WindowsCapabilityValidatedEvidence Normalize(WindowsCapabilityValidatedEvidence evidence)
        => evidence with
        {
            CapabilityId = NormalizeId(evidence.CapabilityId),
            MachineFingerprintId = evidence.MachineFingerprintId.Trim(),
            WorkloadKey = evidence.WorkloadKey.Trim()
        };

    private static string NormalizeId(string? capabilityId)
        => capabilityId?.Trim().ToLowerInvariant() ?? string.Empty;
}
