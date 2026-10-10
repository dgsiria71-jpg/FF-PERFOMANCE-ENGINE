using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

namespace FFPerformanceEngine.Core.Workloads;

/// <summary>
/// BlueStacks-specific runtime evidence that binds an already-proven stable
/// Free Fire identity to one physical Windows player process only when:
/// - exactly one BlueStacks player process exists,
/// - its image path is resolvable,
/// - one addressable BlueStacks instance reports an exact supported package
///   in the Android foreground.
///
/// This source never creates GameIdentity values and never classifies by
/// executable/process name. Ambiguity or unavailable evidence returns no
/// observation so the universal binder remains fail-closed.
/// </summary>
public sealed class BlueStacksForegroundGameProcessEvidenceSource : IGameEvidenceSource
{
    private readonly BlueStacksAutomationService _automation;
    private readonly IBlueStacksPlayerProcessProbe _processProbe;
    private readonly Func<IReadOnlyList<BlueStacksInstance>> _instancesProvider;
    private readonly Func<int, string?> _processPathResolver;

    public BlueStacksForegroundGameProcessEvidenceSource(
        BlueStacksService blueStacks,
        BlueStacksAutomationService automation,
        IBlueStacksPlayerProcessProbe processProbe,
        Func<IReadOnlyList<BlueStacksInstance>>? instancesProvider = null,
        Func<int, string?>? processPathResolver = null)
    {
        ArgumentNullException.ThrowIfNull(blueStacks);
        _automation = automation ?? throw new ArgumentNullException(nameof(automation));
        _processProbe = processProbe ?? throw new ArgumentNullException(nameof(processProbe));
        _instancesProvider = instancesProvider ?? blueStacks.LoadInstances;
        _processPathResolver = processPathResolver
            ?? new WindowsProcessImagePathResolver().Resolve;
    }

    public string SourceId => "bluestacks-foreground-process";
    public int Priority => 80;

    public async Task<IReadOnlyList<GameEvidenceObservation>> ObserveAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var processIds = _processProbe.GetRunningPlayerProcessIds()
            .Where(processId => processId > 0)
            .Distinct()
            .OrderBy(processId => processId)
            .Take(2)
            .ToArray();
        if (processIds.Length != 1)
            return Array.Empty<GameEvidenceObservation>();

        var processId = processIds[0];
        string? executablePath;
        try
        {
            executablePath = NormalizeFullPath(_processPathResolver(processId));
        }
        catch
        {
            return Array.Empty<GameEvidenceObservation>();
        }

        if (executablePath is null)
            return Array.Empty<GameEvidenceObservation>();

        IReadOnlyList<BlueStacksInstance> instances;
        try
        {
            instances = _instancesProvider() ?? Array.Empty<BlueStacksInstance>();
        }
        catch
        {
            return Array.Empty<GameEvidenceObservation>();
        }

        var observedAt = DateTimeOffset.UtcNow;
        var observations = new List<GameEvidenceObservation>();

        foreach (var instance in instances
                     .Where(instance => instance is not null)
                     .OrderBy(instance => instance.Name, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(instance.Name)
                || instance.AdbEnabled == false
                || instance.AdbPort is not (>= 1 and <= 65535))
                continue;

            try
            {
                var connected = await _automation
                    .ConnectAsync(instance, cancellationToken)
                    .ConfigureAwait(false);
                if (!connected.Success)
                    continue;

                var game = await _automation
                    .QueryForegroundGameAsync(instance, cancellationToken)
                    .ConfigureAwait(false);
                if (game is not (GameKind.FreeFire or GameKind.FreeFireMax))
                    continue;

                var identity = LegacyGameIdentityBridge.FromGameKind(game);
                if (identity is null || string.IsNullOrWhiteSpace(identity.GameId))
                    continue;

                observations.Add(new GameEvidenceObservation
                {
                    ObservationId =
                        $"bluestacks:{instance.Name.Trim()}:{identity.GameId}:{processId}",
                    Kind = GameEvidenceKind.RunningProcess,
                    Confidence = 0.99,
                    ObservedAtUtc = observedAt,
                    GameIdHint = identity.GameId,
                    ExecutablePath = executablePath,
                    ProcessId = processId,
                    DisplayName = $"BlueStacks {instance.Name.Trim()}",
                    EvidenceText =
                        $"{game} exact Android package is foreground on BlueStacks instance {instance.Name.Trim()} and exactly one player process PID {processId} is running at {executablePath}."
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is IOException
                or UnauthorizedAccessException
                or InvalidOperationException
                or ArgumentException
                or System.ComponentModel.Win32Exception)
            {
                // One inaccessible/stale instance does not justify a binding.
            }
        }

        return observations
            .OrderBy(observation => observation.ObservationId, StringComparer.Ordinal)
            .ToArray();
    }

    private static string? NormalizeFullPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Path.IsPathFullyQualified(value))
            return null;

        try
        {
            return Path.GetFullPath(value.Trim());
        }
        catch (Exception exception) when (
            exception is ArgumentException
            or NotSupportedException
            or PathTooLongException
            or IOException)
        {
            return null;
        }
    }
}
