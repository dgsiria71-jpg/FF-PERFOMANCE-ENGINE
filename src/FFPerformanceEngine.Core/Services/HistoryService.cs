using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed class HistoryCollection
{
    public List<HistoryEvent> Items { get; init; } = new();
}

public sealed class HistoryService
{
    private readonly JsonStore<HistoryCollection> _store;
    public HistoryService(string? path = null) => _store = new JsonStore<HistoryCollection>(path ?? AppPaths.History);

    public async Task<IReadOnlyList<HistoryEvent>> LoadAsync(CancellationToken cancellationToken = default)
        => (await _store.LoadAsync(cancellationToken).ConfigureAwait(false)).Items.OrderByDescending(x => x.Timestamp).ToList();

    public async Task AppendAsync(HistoryEvent item, CancellationToken cancellationToken = default)
    {
        var data = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
        data.Items.Add(item);
        if (data.Items.Count > 5000) data.Items.RemoveRange(0, data.Items.Count - 5000);
        await _store.SaveAsync(data, cancellationToken).ConfigureAwait(false);
    }
}
