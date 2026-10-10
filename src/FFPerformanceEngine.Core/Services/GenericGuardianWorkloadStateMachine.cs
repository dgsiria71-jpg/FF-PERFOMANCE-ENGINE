using FFPerformanceEngine.Core.Telemetry;
using FFPerformanceEngine.Core.Workloads;

namespace FFPerformanceEngine.Core.Services;

public enum GuardianWorkloadState
{
    Unresolved,
    Offline,
    Desktop,
    Starting,
    Ready,
    Active,
    Ending
}

public enum GuardianWorkloadStateConfidence
{
    Unknown,
    Low,
    Medium,
    High
}

public sealed record GenericGuardianWorkloadSignals
{
    public bool SystemOnline { get; init; } = true;
    public bool IsForeground { get; init; }
    public bool HasRenderActivity { get; init; }
    public bool HasRecentInput { get; init; }
}

public sealed record GuardianWorkloadStateSnapshot
{
    public GuardianWorkloadState State { get; init; } = GuardianWorkloadState.Unresolved;
    public GuardianWorkloadStateConfidence Confidence { get; init; } = GuardianWorkloadStateConfidence.Unknown;
    public GameIdentity? Identity { get; init; }
    public IGameAdapter? Adapter { get; init; }
    public TelemetryWorkloadTarget Target { get; init; } = TelemetryWorkloadTarget.SystemOnly;
}

/// <summary>
/// Track 6 generic Guardian workload lifecycle over already-resolved Track 3/4
/// identity and runtime evidence. This type performs no discovery, telemetry
/// capture, mutation, persistence, profile selection or canary execution.
/// </summary>
public sealed class GenericGuardianWorkloadStateMachine
{
    private readonly TelemetryWorkloadTargetResolver _targetResolver;
    private string? _trackedGameId;
    private int? _trackedProcessId;
    private GuardianWorkloadState _state = GuardianWorkloadState.Unresolved;

    public GenericGuardianWorkloadStateMachine(TelemetryWorkloadTargetResolver targetResolver)
        => _targetResolver = targetResolver ?? throw new ArgumentNullException(nameof(targetResolver));

    public GuardianWorkloadStateSnapshot Observe(
        ResolvedGameCatalogResult catalog,
        string? requestedGameId,
        GenericGuardianWorkloadSignals signals)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(signals);

        var target = _targetResolver.Resolve(catalog, requestedGameId);
        var workload = ResolveExactWorkload(catalog, requestedGameId);
        if (workload is null)
        {
            ResetTransitionMemory();
            return Snapshot(
                GuardianWorkloadState.Unresolved,
                GuardianWorkloadStateConfidence.Unknown,
                null,
                null,
                target);
        }

        var canonicalGameId = workload.Identity.GameId.Trim();
        var normalizedGameId = NormalizeId(canonicalGameId);

        if (!signals.SystemOnline)
        {
            _trackedGameId = normalizedGameId;
            _trackedProcessId = null;
            _state = GuardianWorkloadState.Offline;
            return Snapshot(
                _state,
                GuardianWorkloadStateConfidence.High,
                workload.Identity,
                workload.Adapter,
                UnavailableTarget(canonicalGameId));
        }

        if (target.BindingQuality is TelemetryWorkloadBindingQuality.UnknownGame
            or TelemetryWorkloadBindingQuality.AmbiguousRunningProcess)
        {
            ResetTransitionMemory();
            return Snapshot(
                GuardianWorkloadState.Unresolved,
                GuardianWorkloadStateConfidence.Unknown,
                workload.Identity,
                workload.Adapter,
                target);
        }

        if (target.BindingQuality != TelemetryWorkloadBindingQuality.ExactRunningProcess
            || !target.CanCaptureProcess
            || target.ProcessId is not int processId)
        {
            var wasLiveLifecycle = string.Equals(_trackedGameId, normalizedGameId, StringComparison.Ordinal)
                                   && _trackedProcessId is > 0
                                   && _state is GuardianWorkloadState.Starting
                                       or GuardianWorkloadState.Ready
                                       or GuardianWorkloadState.Active;

            _trackedGameId = normalizedGameId;
            _trackedProcessId = null;
            _state = wasLiveLifecycle
                ? GuardianWorkloadState.Ending
                : GuardianWorkloadState.Desktop;

            return Snapshot(
                _state,
                wasLiveLifecycle
                    ? GuardianWorkloadStateConfidence.Low
                    : GuardianWorkloadStateConfidence.Medium,
                workload.Identity,
                workload.Adapter,
                target);
        }

        var newProcessLifecycle = !string.Equals(_trackedGameId, normalizedGameId, StringComparison.Ordinal)
                                  || _trackedProcessId != processId
                                  || _state is GuardianWorkloadState.Unresolved
                                      or GuardianWorkloadState.Offline
                                      or GuardianWorkloadState.Desktop
                                      or GuardianWorkloadState.Ending;

        _trackedGameId = normalizedGameId;
        _trackedProcessId = processId;

        if (newProcessLifecycle)
        {
            _state = GuardianWorkloadState.Starting;
            return Snapshot(
                _state,
                GuardianWorkloadStateConfidence.Medium,
                workload.Identity,
                workload.Adapter,
                target);
        }

        var active = signals.HasRenderActivity && (signals.IsForeground || signals.HasRecentInput);
        _state = active
            ? GuardianWorkloadState.Active
            : GuardianWorkloadState.Ready;

        return Snapshot(
            _state,
            active
                ? GuardianWorkloadStateConfidence.High
                : GuardianWorkloadStateConfidence.Medium,
            workload.Identity,
            workload.Adapter,
            target);
    }

    public void Reset() => ResetTransitionMemory();

    private static ResolvedGameCatalogEntry? ResolveExactWorkload(
        ResolvedGameCatalogResult catalog,
        string? requestedGameId)
    {
        var requested = NormalizeId(requestedGameId);
        if (requested.Length == 0) return null;

        var matches = (catalog.Games ?? Array.Empty<ResolvedGameCatalogEntry>())
            .Where(entry => entry?.Identity is not null
                            && NormalizeId(entry.Identity.GameId) == requested)
            .ToArray();

        if (matches.Length != 1) return null;
        var match = matches[0];
        return match.Adapter is null || string.IsNullOrWhiteSpace(match.Identity.GameId)
            ? null
            : match;
    }

    private static GuardianWorkloadStateSnapshot Snapshot(
        GuardianWorkloadState state,
        GuardianWorkloadStateConfidence confidence,
        GameIdentity? identity,
        IGameAdapter? adapter,
        TelemetryWorkloadTarget target)
        => new()
        {
            State = state,
            Confidence = confidence,
            Identity = identity,
            Adapter = adapter,
            Target = target
        };

    private static TelemetryWorkloadTarget UnavailableTarget(string gameId)
        => new()
        {
            GameId = gameId,
            BindingQuality = TelemetryWorkloadBindingQuality.UnavailableRunningProcess
        };

    private void ResetTransitionMemory()
    {
        _trackedGameId = null;
        _trackedProcessId = null;
        _state = GuardianWorkloadState.Unresolved;
    }

    private static string NormalizeId(string? value)
        => value?.Trim().ToLowerInvariant() ?? string.Empty;
}
