namespace FFPerformanceEngine.Core.Services;

public sealed class PerformanceTimelineEventRecorder
{
    private readonly PerformanceTimelineBuffer _timeline;
    private readonly object _sync = new();
    private string? _lastGuardianSignature;

    public PerformanceTimelineEventRecorder(PerformanceTimelineBuffer timeline)
        => _timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));

    public void RecordGuardianStatus(GuardianLiveSessionStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);

        var state = status.Cycle?.Observation.State.ToString()
            ?? (status.IsBound ? "Vinculado" : "Aguardando vínculo");
        var binding = status.Binding is GuardianSessionBinding bound
            ? $"{bound.ProcessId}:{bound.InstanceName}"
            : "—";
        var signature = string.Concat(state, "\u001f", binding, "\u001f", status.Message);

        lock (_sync)
        {
            if (string.Equals(_lastGuardianSignature, signature, StringComparison.Ordinal)) return;
            _lastGuardianSignature = signature;
        }

        _timeline.AppendEvent(
            status.Timestamp,
            PerformanceTimelineKind.Guardian,
            $"Guardian · {state}",
            status.Message);
    }

    public void RecordUserMarker(DateTimeOffset timestamp, string detail)
    {
        _timeline.AppendEvent(
            timestamp,
            PerformanceTimelineKind.UserMarker,
            "Marcador",
            detail ?? string.Empty);
    }
}
