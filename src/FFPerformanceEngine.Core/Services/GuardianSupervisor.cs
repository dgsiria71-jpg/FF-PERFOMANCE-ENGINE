using System.Diagnostics;
using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public interface IBlueStacksPlayerProcessProbe
{
    IReadOnlyList<int> GetRunningPlayerProcessIds();
}

public sealed class BlueStacksPlayerProcessProbe : IBlueStacksPlayerProcessProbe
{
    private static readonly string[] ProcessNames = ["HD-Player", "BlueStacksAppplayer"];

    public IReadOnlyList<int> GetRunningPlayerProcessIds()
    {
        var processIds = new HashSet<int>();
        try
        {
            foreach (var processName in ProcessNames)
            {
                foreach (var process in Process.GetProcessesByName(processName))
                {
                    using (process)
                    {
                        if (!process.HasExited) processIds.Add(process.Id);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return Array.Empty<int>();
        }

        return processIds.OrderBy(id => id).ToList();
    }
}

public sealed record GuardianSessionBinding(int ProcessId, string InstanceName);

public sealed record GuardianPlayerBindingResult
{
    public bool Success { get; init; }
    public GuardianSessionBinding? Binding { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class GuardianPlayerBindingService
{
    private readonly IBlueStacksPlayerProcessProbe _processProbe;

    public GuardianPlayerBindingService(IBlueStacksPlayerProcessProbe processProbe)
        => _processProbe = processProbe ?? throw new ArgumentNullException(nameof(processProbe));

    public GuardianPlayerBindingResult TryBind(string instanceName)
    {
        if (string.IsNullOrWhiteSpace(instanceName))
            return new() { Message = "A named BlueStacks instance is required before Guardian can bind a live process." };

        var processIds = _processProbe.GetRunningPlayerProcessIds()
            .Where(processId => processId > 0)
            .Distinct()
            .ToList();

        return processIds.Count switch
        {
            0 => new GuardianPlayerBindingResult
            {
                Message = "No BlueStacks player process is currently available for Guardian binding."
            },
            1 => new GuardianPlayerBindingResult
            {
                Success = true,
                Binding = new GuardianSessionBinding(processIds[0], instanceName),
                Message = $"Guardian bound to BlueStacks PID {processIds[0]} for instance {instanceName}."
            },
            _ => new GuardianPlayerBindingResult
            {
                Message = $"Ambiguous BlueStacks process set ({processIds.Count} players). Guardian refused to choose an arbitrary PID."
            }
        };
    }
}

public interface IGuardianObservationSource
{
    Task<SessionStateObservation> CaptureAsync(CancellationToken cancellationToken = default);
}

public interface IGuardianCanaryExecutor
{
    Task<GuardianCanaryResult> ExecuteAboveNormalPriorityAsync(
        int processId,
        double expectedFps,
        CancellationToken cancellationToken = default);
}

public interface IGuardianCycleRunner
{
    Task<GuardianCycleResult> ObserveOnceAsync(
        GuardianSessionBinding binding,
        CancellationToken cancellationToken = default);
}

public sealed record GuardianCycleResult
{
    public required SessionStateObservation Observation { get; init; }
    public PerformanceProfile? Baseline { get; init; }
    public required GuardianDecision Decision { get; init; }
    public GuardianCanaryResult? Canary { get; init; }
    public bool InCooldown { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed class GuardianSupervisor : IGuardianCycleRunner
{
    private readonly GuardianEngine _guardian;
    private readonly ProfileService _profiles;
    private readonly IGuardianObservationSource _observationSource;
    private readonly IGuardianCanaryExecutor _canaryExecutor;
    private readonly Func<DateTimeOffset> _clock;
    private readonly TimeSpan _cooldown;
    private readonly SemaphoreSlim _cycleGate = new(1, 1);
    private DateTimeOffset? _cooldownUntil;

    public GuardianSupervisor(
        GuardianEngine guardian,
        ProfileService profiles,
        IGuardianObservationSource observationSource,
        IGuardianCanaryExecutor canaryExecutor,
        Func<DateTimeOffset>? clock = null,
        TimeSpan? cooldown = null)
    {
        _guardian = guardian ?? throw new ArgumentNullException(nameof(guardian));
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _observationSource = observationSource ?? throw new ArgumentNullException(nameof(observationSource));
        _canaryExecutor = canaryExecutor ?? throw new ArgumentNullException(nameof(canaryExecutor));
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _cooldown = cooldown ?? TimeSpan.FromSeconds(45);
        if (_cooldown < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(cooldown));
    }

    public async Task<GuardianCycleResult> ObserveOnceAsync(
        GuardianSessionBinding binding,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(binding);
        if (binding.ProcessId <= 0) throw new ArgumentOutOfRangeException(nameof(binding), "Guardian requires a positive bound process ID.");
        if (string.IsNullOrWhiteSpace(binding.InstanceName)) throw new ArgumentException("Guardian requires a named BlueStacks instance.", nameof(binding));

        await _cycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var observation = await _observationSource.CaptureAsync(cancellationToken).ConfigureAwait(false);
            _guardian.SetState(observation.State);

            var baseline = await FindBaselineAsync(observation.ActiveGame, binding.InstanceName, cancellationToken).ConfigureAwait(false);
            if (baseline?.AverageFps is not double expectedFps || expectedFps <= 0)
            {
                return NoAction(observation, baseline,
                    "No validated FPS baseline matches the observed game and exact BlueStacks instance.");
            }

            var sample = observation.Telemetry;
            if (sample?.Fps is null)
                return NoAction(observation, baseline, "Current frame evidence is unavailable; Guardian will observe without changing anything.");

            var action = new GuardianAction
            {
                Id = GuardianCanaryService.AboveNormalPriorityActionId,
                Description = "BlueStacks process priority → AboveNormal",
                Safety = ActionSafety.LiveSafe,
                MinimumConfidence = 0.85
            };
            var decision = _guardian.Evaluate(expectedFps, sample, action);
            if (!decision.ShouldAct)
            {
                return new GuardianCycleResult
                {
                    Observation = observation,
                    Baseline = baseline,
                    Decision = decision,
                    Message = decision.Reason
                };
            }

            var now = _clock();
            if (_cooldownUntil is DateTimeOffset until && now < until)
            {
                return new GuardianCycleResult
                {
                    Observation = observation,
                    Baseline = baseline,
                    Decision = decision,
                    InCooldown = true,
                    Message = $"Guardian detected degradation but the live action is cooling down until {until:O}."
                };
            }

            var canary = await _canaryExecutor.ExecuteAboveNormalPriorityAsync(binding.ProcessId, expectedFps, cancellationToken).ConfigureAwait(false);
            _cooldownUntil = now + _cooldown;
            return new GuardianCycleResult
            {
                Observation = observation,
                Baseline = baseline,
                Decision = decision,
                Canary = canary,
                Message = canary.Message
            };
        }
        finally
        {
            _cycleGate.Release();
        }
    }

    private async Task<PerformanceProfile?> FindBaselineAsync(
        GameKind game,
        string instanceName,
        CancellationToken cancellationToken)
    {
        if (game is not (GameKind.FreeFire or GameKind.FreeFireMax)) return null;

        var candidates = (await _profiles.LoadAsync(cancellationToken).ConfigureAwait(false))
            .Where(profile =>
                profile.Game == game
                && profile.Evidence == EvidenceLevel.Validated
                && profile.AverageFps is > 0
                && string.Equals(profile.InstanceName, instanceName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (candidates.Count == 0) return null;
        return candidates
            .OrderByDescending(profile => profile.Kind == ProfileKind.Recommended)
            .ThenByDescending(profile => profile.Confidence)
            .ThenByDescending(ProfileService.RecommendedScore)
            .First();
    }

    private static GuardianCycleResult NoAction(
        SessionStateObservation observation,
        PerformanceProfile? baseline,
        string reason)
        => new()
        {
            Observation = observation,
            Baseline = baseline,
            Decision = new GuardianDecision { Reason = reason },
            Message = reason
        };
}
