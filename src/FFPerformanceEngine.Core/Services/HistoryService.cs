using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed class HistoryCollection
{
    public List<HistoryEvent> Items { get; init; } = new();
    public List<PerformanceComparisonHistoryRecord> PerformanceComparisons { get; init; } = new();
}

public sealed class HistoryService
{
    private const int MaxEvents = 5000;
    private const int MaxPerformanceComparisons = 500;

    private readonly JsonStore<HistoryCollection> _store;
    private readonly SemaphoreSlim _writeGate = new(1, 1);

    public HistoryService(string? path = null)
        => _store = new JsonStore<HistoryCollection>(path ?? AppPaths.History);

    public async Task<IReadOnlyList<HistoryEvent>> LoadAsync(CancellationToken cancellationToken = default)
        => (await _store.LoadAsync(cancellationToken).ConfigureAwait(false))
            .Items
            .OrderByDescending(x => x.Timestamp)
            .ToList();

    public async Task<IReadOnlyList<PerformanceComparisonHistoryRecord>> LoadPerformanceComparisonsAsync(
        CancellationToken cancellationToken = default)
    {
        var data = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
        return data.PerformanceComparisons
            .Select(TryRehydrate)
            .Where(record => record is not null)
            .Select(record => record!)
            .OrderByDescending(record => record.SavedAt)
            .ToList();
    }

    public async Task AppendAsync(HistoryEvent item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);
        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var data = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
            AppendEvent(data, item);
            await _store.SaveAsync(data, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public async Task<PerformanceComparisonHistoryRecord> SavePerformanceComparisonAsync(
        string label,
        PerformanceABComparison comparison,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("A historical comparison label is required.", nameof(label));
        ArgumentNullException.ThrowIfNull(comparison);

        var normalized = PerformanceABComparison.Create(comparison.Baseline, comparison.Candidate);
        var record = new PerformanceComparisonHistoryRecord
        {
            Label = label.Trim(),
            SavedAt = DateTimeOffset.UtcNow,
            Baseline = normalized.Baseline,
            Candidate = normalized.Candidate,
            ValidationStatus = PerformanceComparisonValidationStatus.Observed
        }.Rehydrate();

        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var data = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
            data.PerformanceComparisons.Add(record);
            TrimComparisons(data);
            AppendEvent(data, new HistoryEvent
            {
                Kind = HistoryEventKind.Benchmark,
                Title = "Comparação A/B salva",
                Summary = $"{record.Label} · evidência observada · validação necessária antes de originar perfil.",
                DetailsJson = $"{{\"comparisonId\":\"{record.Id:D}\",\"validationStatus\":\"{record.ValidationStatus}\"}}"
            });
            await _store.SaveAsync(data, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeGate.Release();
        }

        return record;
    }

    public async Task<PerformanceComparisonHistoryRecord> RequestPerformanceValidationAsync(
        Guid comparisonId,
        CancellationToken cancellationToken = default)
    {
        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var data = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
            var index = FindComparisonIndex(data, comparisonId);
            var existing = data.PerformanceComparisons[index].Rehydrate();
            if (existing.ValidationStatus == PerformanceComparisonValidationStatus.Validated)
                return existing;

            var pending = existing with
            {
                ValidationStatus = PerformanceComparisonValidationStatus.PendingValidation,
                ValidationEvidence = null,
                ValidatedAt = null
            };
            data.PerformanceComparisons[index] = pending;
            AppendEvent(data, new HistoryEvent
            {
                Kind = HistoryEventKind.Benchmark,
                Title = "Validação A/B solicitada",
                Summary = $"{pending.Label} · aguardando uma nova captura medida e independente.",
                DetailsJson = $"{{\"comparisonId\":\"{pending.Id:D}\",\"validationStatus\":\"{pending.ValidationStatus}\"}}"
            });
            await _store.SaveAsync(data, cancellationToken).ConfigureAwait(false);
            return pending;
        }
        finally
        {
            _writeGate.Release();
        }
    }

    public async Task<PerformanceComparisonHistoryRecord> CompletePerformanceValidationAsync(
        Guid comparisonId,
        PerformanceEvidenceSnapshot validationEvidence,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(validationEvidence);
        var normalizedValidation = PerformanceEvidenceSnapshot.Rehydrate(validationEvidence);
        if (normalizedValidation.Quality != PerformanceEvidenceQuality.Measured)
            throw new InvalidOperationException("Validation requires fully measured frame evidence; partial or unavailable telemetry cannot be promoted.");

        await _writeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var data = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
            var index = FindComparisonIndex(data, comparisonId);
            var existing = data.PerformanceComparisons[index].Rehydrate();
            if (existing.ValidationStatus != PerformanceComparisonValidationStatus.PendingValidation)
                throw new InvalidOperationException("Historical comparison must explicitly enter PendingValidation before validation can complete.");
            if (normalizedValidation.CapturedAt <= existing.Candidate.CapturedAt)
                throw new InvalidOperationException("Validation evidence must come from a later independent capture, not the original candidate snapshot.");
            if (existing.Candidate.Configuration is null)
                throw new InvalidOperationException("The original candidate has no exact configuration snapshot. Capture a new B before requesting profile validation.");
            if (normalizedValidation.Configuration is null)
                throw new InvalidOperationException("Validation evidence has no exact configuration snapshot. Re-capture B with a bound BlueStacks configuration.");
            if (!existing.Candidate.Configuration.IsEquivalentTo(normalizedValidation.Configuration))
                throw new InvalidOperationException("Validation was measured under a different BlueStacks tuning configuration or structurally different environment.");

            var validated = existing with
            {
                ValidationStatus = PerformanceComparisonValidationStatus.Validated,
                ValidationEvidence = normalizedValidation,
                ValidatedAt = DateTimeOffset.UtcNow
            };
            data.PerformanceComparisons[index] = validated;
            AppendEvent(data, new HistoryEvent
            {
                Kind = HistoryEventKind.Benchmark,
                Title = "Comparação A/B validada",
                Summary = $"{validated.Label} · nova captura medida confirmou a mesma configuração · evidência elegível para originar perfil.",
                DetailsJson = $"{{\"comparisonId\":\"{validated.Id:D}\",\"validationStatus\":\"{validated.ValidationStatus}\",\"environmentFingerprint\":\"{validated.Candidate.Configuration.Environment.Id}\"}}"
            });
            await _store.SaveAsync(data, cancellationToken).ConfigureAwait(false);
            return validated;
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private static PerformanceComparisonHistoryRecord? TryRehydrate(PerformanceComparisonHistoryRecord record)
    {
        try
        {
            return record.Rehydrate();
        }
        catch (Exception exception) when (exception is InvalidDataException or ArgumentException)
        {
            return null;
        }
    }

    private static int FindComparisonIndex(HistoryCollection data, Guid comparisonId)
    {
        var index = data.PerformanceComparisons.FindIndex(record => record.Id == comparisonId);
        if (index < 0)
            throw new KeyNotFoundException($"Historical performance comparison '{comparisonId:D}' was not found.");
        return index;
    }

    private static void AppendEvent(HistoryCollection data, HistoryEvent item)
    {
        data.Items.Add(item);
        if (data.Items.Count > MaxEvents)
            data.Items.RemoveRange(0, data.Items.Count - MaxEvents);
    }

    private static void TrimComparisons(HistoryCollection data)
    {
        if (data.PerformanceComparisons.Count > MaxPerformanceComparisons)
            data.PerformanceComparisons.RemoveRange(0, data.PerformanceComparisons.Count - MaxPerformanceComparisons);
    }
}
