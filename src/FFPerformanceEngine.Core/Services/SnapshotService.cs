using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed class SnapshotCollection
{
    public List<TuningSnapshot> Items { get; init; } = new();
}

public sealed class SnapshotService
{
    private readonly JsonStore<SnapshotCollection> _store;
    public SnapshotService(string? path = null) => _store = new JsonStore<SnapshotCollection>(path ?? AppPaths.Snapshots);

    public async Task<TuningSnapshot> CreateAsync(string name, IDictionary<string, string> values, CancellationToken cancellationToken = default)
    {
        var data = await _store.LoadAsync(cancellationToken).ConfigureAwait(false);
        var snapshot = new TuningSnapshot { Name = name, Values = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase) };
        data.Items.Add(snapshot);
        await _store.SaveAsync(data, cancellationToken).ConfigureAwait(false);
        return snapshot;
    }

    public async Task<IReadOnlyList<TuningSnapshot>> LoadAsync(CancellationToken cancellationToken = default)
        => (await _store.LoadAsync(cancellationToken).ConfigureAwait(false)).Items.OrderByDescending(x => x.Timestamp).ToList();
}
