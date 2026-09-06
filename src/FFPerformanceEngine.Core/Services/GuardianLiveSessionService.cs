using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public interface IGuardianSupervisorFactory
{
    IGuardianCycleRunner Create(GuardianSessionBinding binding, BlueStacksInstance instance);
}

public sealed class GuardianSupervisorFactory : IGuardianSupervisorFactory
{
    private readonly GuardianEngine _guardian;
    private readonly ProfileService _profiles;
    private readonly BlueStacksAutomationService _automation;
    private readonly PresentMonService _presentMon;
    private readonly GuardianCanaryService _canary;
    private readonly IBlueStacksPlayerProcessProbe _processProbe;
    private readonly IRecentInputProbe _recentInput;
    private readonly TimeSpan _observationSampleDuration;
    private readonly TimeSpan _canarySampleDuration;

    public GuardianSupervisorFactory(
        GuardianEngine guardian,
        ProfileService profiles,
        BlueStacksAutomationService automation,
        PresentMonService presentMon,
        GuardianCanaryService canary,
        IBlueStacksPlayerProcessProbe processProbe,
        IRecentInputProbe recentInput,
        TimeSpan? observationSampleDuration = null,
        TimeSpan? canarySampleDuration = null)
    {
        _guardian = guardian ?? throw new ArgumentNullException(nameof(guardian));
        _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
        _automation = automation ?? throw new ArgumentNullException(nameof(automation));
        _presentMon = presentMon ?? throw new ArgumentNullException(nameof(presentMon));
        _canary = canary ?? throw new ArgumentNullException(nameof(canary));
        _processProbe = processProbe ?? throw new ArgumentNullException(nameof(processProbe));
        _recentInput = recentInput ?? throw new ArgumentNullException(nameof(recentInput));
        _observationSampleDuration = observationSampleDuration ?? TimeSpan.FromSeconds(2);
        _canarySampleDuration = canarySampleDuration ?? TimeSpan.FromSeconds(3);
        if (_observationSampleDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(observationSampleDuration));
        if (_canarySampleDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(canarySampleDuration));
    }

    public IGuardianCycleRunner Create(GuardianSessionBinding binding, BlueStacksInstance instance)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(instance);
        if (binding.ProcessId <= 0) throw new ArgumentOutOfRangeException(nameof(binding));
        if (!string.Equals(binding.InstanceName, instance.Name, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Guardian process binding and BlueStacks instance do not match.");

        var monitor = new SessionStateMonitor(
            new GameStateDetector(),
            () => _processProbe.GetRunningPlayerProcessIds().Contains(binding.ProcessId),
            token => _automation.QueryForegroundGameAsync(instance, token),
            token => _presentMon.CaptureProcessAsync(binding.ProcessId, _observationSampleDuration, token),
            _recentInput.HasRecentInput);
        var executor = new GuardianCanaryExecutor(_canary, _presentMon, _canarySampleDuration);
        return new GuardianSupervisor(_guardian, _profiles, monitor, executor);
    }
}

public sealed record GuardianLiveSessionStatus
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public BlueStacksInstance? Instance { get; init; }
    public GuardianSessionBinding? Binding { get; init; }
    public GuardianCycleResult? Cycle { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool IsBound => Binding is not null;
}

public interface IGuardianLiveSessionRunner
{
    Task RunAsync(
        string instanceName,
        TimeSpan interval,
        Action<GuardianLiveSessionStatus> publish,
        CancellationToken cancellationToken = default);

    Task ResetAsync(CancellationToken cancellationToken = default);
}

public sealed class GuardianLiveSessionService : IGuardianLiveSessionRunner
{
    private readonly Func<EnvironmentSnapshot> _environmentProbe;
    private readonly GuardianPlayerBindingService _bindingService;
    private readonly IGuardianSupervisorFactory _supervisorFactory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private GuardianSessionBinding? _currentBinding;
    private BlueStacksInstance? _currentInstance;
    private IGuardianCycleRunner? _currentRunner;

    public GuardianLiveSessionService(
        Func<EnvironmentSnapshot> environmentProbe,
        GuardianPlayerBindingService bindingService,
        IGuardianSupervisorFactory supervisorFactory)
    {
        _environmentProbe = environmentProbe ?? throw new ArgumentNullException(nameof(environmentProbe));
        _bindingService = bindingService ?? throw new ArgumentNullException(nameof(bindingService));
        _supervisorFactory = supervisorFactory ?? throw new ArgumentNullException(nameof(supervisorFactory));
    }

    public async Task<GuardianLiveSessionStatus> ObserveOnceAsync(
        string instanceName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceName))
            return new GuardianLiveSessionStatus { Message = "Selecione uma instância BlueStacks para o Guardian." };

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var environment = _environmentProbe();
            var instance = environment.Instances.FirstOrDefault(item =>
                string.Equals(item.Name, instanceName, StringComparison.OrdinalIgnoreCase));
            if (instance is null)
            {
                ResetBinding();
                return new GuardianLiveSessionStatus
                {
                    Message = $"A instância BlueStacks '{instanceName}' não está disponível. Guardian não escolherá outra instância silenciosamente."
                };
            }

            var bindingResult = _bindingService.TryBind(instance.Name);
            if (!bindingResult.Success || bindingResult.Binding is null)
            {
                ResetBinding();
                return new GuardianLiveSessionStatus
                {
                    Instance = instance,
                    Message = bindingResult.Message
                };
            }

            var binding = bindingResult.Binding;
            if (_currentRunner is null || _currentBinding != binding || !Equals(_currentInstance, instance))
            {
                _currentBinding = binding;
                _currentInstance = instance;
                _currentRunner = _supervisorFactory.Create(binding, instance);
            }

            var cycle = await _currentRunner.ObserveOnceAsync(binding, cancellationToken).ConfigureAwait(false);
            return new GuardianLiveSessionStatus
            {
                Instance = instance,
                Binding = binding,
                Cycle = cycle,
                Message = string.IsNullOrWhiteSpace(cycle.Message) ? bindingResult.Message : cycle.Message
            };
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RunAsync(
        string instanceName,
        TimeSpan interval,
        Action<GuardianLiveSessionStatus> publish,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceName)) throw new ArgumentException("A BlueStacks instance name is required.", nameof(instanceName));
        if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));
        ArgumentNullException.ThrowIfNull(publish);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var status = await ObserveOnceAsync(instanceName, cancellationToken).ConfigureAwait(false);
                publish(status);
                if (cancellationToken.IsCancellationRequested) break;
                await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ResetBinding();
        }
        finally
        {
            _gate.Release();
        }
    }

    private void ResetBinding()
    {
        _currentBinding = null;
        _currentInstance = null;
        _currentRunner = null;
    }
}

public sealed class GuardianSessionHost : IAsyncDisposable
{
    private readonly IGuardianLiveSessionRunner _runner;
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private CancellationTokenSource? _runCancellation;
    private Task? _runTask;
    private string? _instanceName;
    private GuardianLiveSessionStatus? _currentStatus;
    private bool _disposed;

    public GuardianSessionHost(IGuardianLiveSessionRunner runner)
        => _runner = runner ?? throw new ArgumentNullException(nameof(runner));

    public event EventHandler<GuardianLiveSessionStatus>? StatusChanged;

    public GuardianLiveSessionStatus? CurrentStatus => Volatile.Read(ref _currentStatus);
    public string? InstanceName => Volatile.Read(ref _instanceName);

    public bool IsRunning
    {
        get
        {
            var task = Volatile.Read(ref _runTask);
            return task is { IsCompleted: false };
        }
    }

    public async Task StartAsync(
        string instanceName,
        TimeSpan interval,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (string.IsNullOrWhiteSpace(instanceName)) throw new ArgumentException("A BlueStacks instance name is required.", nameof(instanceName));
        if (interval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));

        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (IsRunning && string.Equals(_instanceName, instanceName, StringComparison.OrdinalIgnoreCase)) return;

            await StopCoreAsync(resetRunner: true, CancellationToken.None).ConfigureAwait(false);

            _instanceName = instanceName;
            var runCancellation = new CancellationTokenSource();
            _runCancellation = runCancellation;
            _runTask = _runner.RunAsync(instanceName, interval, Publish, runCancellation.Token);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await StopCoreAsync(resetRunner: true, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await _lifecycleGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed) return;
            await StopCoreAsync(resetRunner: true, CancellationToken.None).ConfigureAwait(false);
            _disposed = true;
        }
        finally
        {
            _lifecycleGate.Release();
            _lifecycleGate.Dispose();
        }
    }

    private void Publish(GuardianLiveSessionStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);
        Volatile.Write(ref _currentStatus, status);
        StatusChanged?.Invoke(this, status);
    }

    private async Task StopCoreAsync(bool resetRunner, CancellationToken cancellationToken)
    {
        var runCancellation = _runCancellation;
        var runTask = _runTask;
        var hadActiveSession = runCancellation is not null || runTask is not null || !string.IsNullOrWhiteSpace(_instanceName);
        _runCancellation = null;
        _runTask = null;
        _instanceName = null;

        if (runCancellation is not null)
        {
            runCancellation.Cancel();
            if (runTask is not null)
            {
                try
                {
                    await runTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (runCancellation.IsCancellationRequested)
                {
                }
            }
            runCancellation.Dispose();
        }

        if (resetRunner && hadActiveSession) await _runner.ResetAsync(cancellationToken).ConfigureAwait(false);
    }
}
