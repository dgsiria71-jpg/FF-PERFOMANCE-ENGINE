using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed class GuardianKnowledgeCollection
{
    public List<GuardianActionEvidence> Items { get; init; } = new();
    public List<GenericGuardianActionReliability> GenericItems { get; init; } = new();
}

public sealed class GuardianKnowledgeService
{
    private readonly JsonStore<GuardianKnowledgeCollection> _store;
    private readonly SemaphoreSlim _genericWriteGate = new(1, 1);

    public GuardianKnowledgeService(string? path = null)
        => _store = new JsonStore<GuardianKnowledgeCollection>(path ?? Path.Combine(AppPaths.Root, "guardian-knowledge.json"));

    public async Task<GuardianActionEvidence?> GetAsync(string actionId, CancellationToken cancellationToken = default)
        => (await _store.LoadAsync(cancellationToken).ConfigureAwait(false)).Items.FirstOrDefault(x => string.Equals(x.ActionId, actionId, StringComparison.OrdinalIgnoreCase));

    public async Task<GuardianActionEvidence> RecordAsync(string actionId, bool success, double relativeFpsGain, CancellationToken cancellationToken = default)
    {
        var data = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
        var existing = data.Items.FirstOrDefault(x => string.Equals(x.ActionId, actionId, StringComparison.OrdinalIgnoreCase)) ?? new GuardianActionEvidence { ActionId = actionId };
        var successCount = existing.SuccessCount + (success ? 1 : 0);
        var failureCount = existing.FailureCount + (success ? 0 : 1);
        var newAverage = success
            ? ((existing.AverageRelativeFpsGain * existing.SuccessCount) + relativeFpsGain) / Math.Max(1, successCount)
            : existing.AverageRelativeFpsGain;
        var updated = existing with { SuccessCount = successCount, FailureCount = failureCount, AverageRelativeFpsGain = newAverage, UpdatedAt = DateTimeOffset.UtcNow };
        data.Items.RemoveAll(x => string.Equals(x.ActionId, actionId, StringComparison.OrdinalIgnoreCase));
        data.Items.Add(updated);
        await _store.SaveAsync(data, cancellationToken).ConfigureAwait(false);
        return updated;
    }

    public async Task<GenericGuardianActionReliability?> GetGenericAsync(
        GenericGuardianActionReliabilityKey key,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeKey(key);
        var data = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
        return data.GenericItems.FirstOrDefault(item => SameKey(item, normalized));
    }

    public async Task<IReadOnlyList<GenericGuardianActionReliability>> LoadGenericAsync(
        CancellationToken cancellationToken = default)
        => (await _store.LoadAsync(cancellationToken).ConfigureAwait(false))
            .GenericItems
            .OrderBy(item => item.GameId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Family)
            .ThenBy(item => item.ActionId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public async Task<GenericGuardianActionReliability> RecordGenericAsync(
        GenericGuardianActionReliabilityKey key,
        GenericGuardianSessionCanaryVerdict verdict,
        double? relativeFpsGain,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeKey(key);
        if (relativeFpsGain is double gain && !double.IsFinite(gain))
            throw new ArgumentOutOfRangeException(
                nameof(relativeFpsGain),
                "Relative FPS gain must be finite when supplied.");

        await _genericWriteGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var data = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
            var existing = data.GenericItems.FirstOrDefault(item => SameKey(item, normalized))
                           ?? new GenericGuardianActionReliability
                           {
                               GameId = normalized.GameId,
                               Family = normalized.Family,
                               ActionId = normalized.ActionId
                           };

            var successCount = existing.SuccessCount;
            var failureCount = existing.FailureCount;
            var inconclusiveCount = existing.InconclusiveCount;
            var averageGain = existing.AverageRelativeFpsGain;

            switch (verdict)
            {
                case GenericGuardianSessionCanaryVerdict.Improved:
                    successCount++;
                    if (relativeFpsGain is double measuredGain)
                    {
                        averageGain =
                            ((existing.AverageRelativeFpsGain * existing.SuccessCount)
                             + measuredGain)
                            / Math.Max(1, successCount);
                    }
                    break;

                case GenericGuardianSessionCanaryVerdict.Regressive:
                    failureCount++;
                    break;

                case GenericGuardianSessionCanaryVerdict.Inconclusive:
                    inconclusiveCount++;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(verdict));
            }

            var updated = existing with
            {
                SuccessCount = successCount,
                FailureCount = failureCount,
                InconclusiveCount = inconclusiveCount,
                AverageRelativeFpsGain = averageGain,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            data.GenericItems.RemoveAll(item => SameKey(item, normalized));
            data.GenericItems.Add(updated);
            await _store.SaveAsync(data, cancellationToken).ConfigureAwait(false);
            return updated;
        }
        finally
        {
            _genericWriteGate.Release();
        }
    }

    private static GenericGuardianActionReliabilityKey NormalizeKey(
        GenericGuardianActionReliabilityKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (string.IsNullOrWhiteSpace(key.GameId))
            throw new ArgumentException("Generic Guardian reliability requires GameId.", nameof(key));
        if (string.IsNullOrWhiteSpace(key.ActionId))
            throw new ArgumentException("Generic Guardian reliability requires ActionId.", nameof(key));
        if (key.Family == GuardianAnomalyKind.Unknown)
            throw new ArgumentException(
                "Generic Guardian reliability cannot be learned for Unknown family.",
                nameof(key));

        return new GenericGuardianActionReliabilityKey(
            key.GameId.Trim(),
            key.Family,
            key.ActionId.Trim());
    }

    private static bool SameKey(
        GenericGuardianActionReliability item,
        GenericGuardianActionReliabilityKey key)
        => item.Family == key.Family
           && string.Equals(item.GameId, key.GameId, StringComparison.OrdinalIgnoreCase)
           && string.Equals(item.ActionId, key.ActionId, StringComparison.OrdinalIgnoreCase);
}
