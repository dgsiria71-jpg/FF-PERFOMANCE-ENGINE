using System.Text.Json;

namespace FFPerformanceEngine.Core.SystemOptimization;

public static class SystemOptimizationSnapshotCodec
{
    public const string PayloadKey = "dg.system.transaction.v1";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    internal static string Encode(SystemOptimizationRestoreEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        return JsonSerializer.Serialize(envelope, JsonOptions);
    }

    internal static SystemOptimizationRestoreEnvelope Decode(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            throw new InvalidDataException("System optimization restore payload is empty.");

        var envelope = JsonSerializer.Deserialize<SystemOptimizationRestoreEnvelope>(payload, JsonOptions)
            ?? throw new InvalidDataException("System optimization restore payload could not be deserialized.");
        if (envelope.SchemaVersion != 1)
            throw new InvalidDataException($"Unsupported system optimization restore schema {envelope.SchemaVersion}.");
        if (envelope.TransactionId == Guid.Empty)
            throw new InvalidDataException("System optimization restore payload has no transaction identity.");
        if (envelope.Entries.Count == 0)
            throw new InvalidDataException("System optimization restore payload contains no mutations.");
        if (envelope.Entries.Any(entry => string.IsNullOrWhiteSpace(entry.CapabilityId)))
            throw new InvalidDataException("System optimization restore payload contains an invalid capability identity.");
        return envelope;
    }
}

internal sealed record SystemOptimizationRestoreEnvelope
{
    public int SchemaVersion { get; init; } = 1;
    public Guid TransactionId { get; init; }
    public string Label { get; init; } = string.Empty;
    public SystemOptimizationScope Scope { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public List<SystemOptimizationRestoreEntry> Entries { get; init; } = new();
}

internal sealed record SystemOptimizationRestoreEntry
{
    public string CapabilityId { get; init; } = string.Empty;
    public string TargetValue { get; init; } = string.Empty;
    public string? OriginalValue { get; init; }
    public string RestorePayload { get; init; } = string.Empty;
}
