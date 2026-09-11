using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed class GuardianKnowledgeCollection
{
    public List<GuardianActionEvidence> Items { get; init; } = new();
}

public sealed class GuardianKnowledgeService
{
    private readonly JsonStore<GuardianKnowledgeCollection> _store;

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
}
