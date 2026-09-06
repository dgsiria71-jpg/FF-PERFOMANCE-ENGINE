namespace FFPerformanceEngine.Core.Models;

public sealed record HistoryEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public HistoryEventKind Kind { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string? DetailsJson { get; init; }
}

public sealed record TuningSnapshot
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string Name { get; init; } = "Snapshot";
    public Dictionary<string, string> Values { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
