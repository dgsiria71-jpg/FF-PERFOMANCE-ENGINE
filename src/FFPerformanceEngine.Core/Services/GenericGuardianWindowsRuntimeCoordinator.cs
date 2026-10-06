using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Workloads;

namespace FFPerformanceEngine.Core.Services;

public interface IGenericGuardianWindowsRuntime : IAsyncDisposable
{
    GenericGuardianCanarySessionKey? CurrentSession { get; }
    int RetainedLeaseCount { get; }

    Task<GenericGuardianWindowsRuntimeCycleResult> RunCycleAsync(
        ResolvedGameCatalogResult catalog,
        string requestedGameId,
        bool systemOnline,
        TimeSpan observationCaptureDuration,
        BottleneckAnalysisContext analysisContext,
        CancellationToken cancellationToken = default);

    Task ResetAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Immutable caller-supplied schedule input for the generic Guardian runtime.
/// The coordinator owns no discovery, policy, action mapping, budget or evidence.
/// </summary>
public sealed record GenericGuardianWindowsRuntimeLoopPlan
{
    public required ResolvedGameCatalogResult Catalog { get; init; }
    public required string GameId { get; init; }
    public required bool SystemOnline { get; init; }
    public required TimeSpan ObservationDuration { get; init; }
    public required BottleneckAnalysisContext AnalysisContext { get; init; }
    public required TimeSpan Interval { get; init; }
}

/// <summary>
/// Explicitly started/stopped scheduler around the already-protected on-demand
/// generic Guardian runtime. Construction is inert. One cycle is awaited to
/// completion before the next delay begins, so cycles cannot overlap.
///
/// A normal Stop cancels scheduling then resets retained runtime state using
/// non-cancelable cleanup. A cycle failure also attempts that cleanup
/// automatically and records the failure instead of leaving an unobserved task.
/// If cleanup itself fails, ownership remains in the runtime and a later
/// explicit Stop retries ResetAsync before a new Start is permitted.
/// </summary>
public sealed class GenericGuardianWindowsRuntimeCoordinator : IAsyncDisposable
{
    private readonly IGenericGuardianWindowsRuntime _runtime;
    private readonly IGenericGuardianRuntimeActivationReadinessGate _readinessGate;
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private CancellationTokenSource? _runCancellation;
    private Task? _runTask;
    private GenericGuardianWindowsRuntimeCycleResult? _lastResult;
    private Exception? _lastFailure;
    private GenericGuardianRuntimeActivationReadiness _lastReadiness =
        GenericGuardianRuntimeActivationReadiness.NotEvaluated;
    private long _completedCycles;
    private bool _disposed;

    public GenericGuardianWindowsRuntimeCoordinator(IGenericGuardianWindowsRuntime runtime)
        : this(runtime, new GenericGuardianRuntimeNotConfiguredReadinessGate())
    {
    }

    public GenericGuardianWindowsRuntimeCoordinator(
        IGenericGuardianWindowsRuntime runtime,
        IGenericGuardianRuntimeActivationReadinessGate readinessGate)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _readinessGate = readinessGate ?? throw new ArgumentNullException(nameof(readinessGate));
    }

    public bool IsRunning
    {
        get
        {
            var task = Volatile.Read(ref _runTask);
            return task is { IsCompleted: false };
        }
    }

    public GenericGuardianWindowsRuntimeCycleResult? LastResult
        => Volatile.Read(ref _lastResult);

    public Exception? LastFailure
        => Volatile.Read(ref _lastFailure);

    public GenericGuardianRuntimeActivationReadiness LastReadiness
        => Volatile.Read(ref _lastReadiness);

    public long CompletedCycles
        => Interlocked.Read(ref _completedCycles);

    public async Task StartAsync(
        GenericGuardianWindowsRuntimeLoopPlan plan,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ValidatePlan(plan);

        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_runTask is not null)
            {
                throw new InvalidOperationException(
                    _runTask.IsCompleted
                        ? "The previous generic Guardian schedule ended. Call StopAsync to complete/reset lifecycle cleanup before starting again."
                        : "The generic Guardian schedule is already running.");
            }

            var readiness = _readinessGate.Evaluate(plan)
                ?? throw new InvalidOperationException(
                    "Generic Guardian activation-readiness gate returned no result.");
            Volatile.Write(ref _lastReadiness, readiness);
            if (!readiness.IsReady)
            {
                throw new InvalidOperationException(
                    $"Generic Guardian scheduled activation is NotReady: {readiness.Status}. {readiness.Reason}");
            }

            Volatile.Write(ref _lastResult, null);
            Volatile.Write(ref _lastFailure, null);
            Interlocked.Exchange(ref _completedCycles, 0);

            var cancellation = new CancellationTokenSource();
            _runCancellation = cancellation;
            _runTask = RunLoopAsync(plan, cancellation.Token);
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
            ObjectDisposedException.ThrowIf(_disposed, this);

            var runCancellation = _runCancellation;
            var runTask = _runTask;
            runCancellation?.Cancel();

            if (runTask is not null)
                await runTask.ConfigureAwait(false);

            try
            {
                await _runtime.ResetAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception cleanupFailure)
            {
                RecordCleanupFailure(cleanupFailure);
                throw;
            }

            runCancellation?.Dispose();
            _runCancellation = null;
            _runTask = null;
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        // StopAsync intentionally performs/reties protected runtime reset first.
        // If it fails, disposal is aborted so the caller can retry ownership cleanup.
        await StopAsync(CancellationToken.None).ConfigureAwait(false);

        await _lifecycleGate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        var disposeGate = false;
        try
        {
            if (_disposed) return;
            await _runtime.DisposeAsync().ConfigureAwait(false);
            _disposed = true;
            disposeGate = true;
        }
        finally
        {
            _lifecycleGate.Release();
            if (disposeGate) _lifecycleGate.Dispose();
        }
    }

    private async Task RunLoopAsync(
        GenericGuardianWindowsRuntimeLoopPlan plan,
        CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await _runtime.RunCycleAsync(
                    plan.Catalog,
                    plan.GameId,
                    plan.SystemOnline,
                    plan.ObservationDuration,
                    plan.AnalysisContext,
                    cancellationToken).ConfigureAwait(false);

                Volatile.Write(ref _lastResult, result);
                Interlocked.Increment(ref _completedCycles);

                await Task.Delay(plan.Interval, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception cycleFailure)
        {
            Exception recorded = cycleFailure;
            try
            {
                await _runtime.ResetAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception cleanupFailure)
            {
                recorded = new AggregateException(
                    "Generic Guardian scheduled cycle failed and protected runtime cleanup also failed.",
                    cycleFailure,
                    cleanupFailure);
            }

            Volatile.Write(ref _lastFailure, recorded);
        }
    }

    private void RecordCleanupFailure(Exception cleanupFailure)
    {
        var prior = Volatile.Read(ref _lastFailure);
        Volatile.Write(
            ref _lastFailure,
            prior is null
                ? cleanupFailure
                : new AggregateException(
                    "Generic Guardian schedule already failed and explicit cleanup also failed.",
                    prior,
                    cleanupFailure));
    }

    private static void ValidatePlan(GenericGuardianWindowsRuntimeLoopPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(plan.Catalog);
        ArgumentNullException.ThrowIfNull(plan.AnalysisContext);
        if (string.IsNullOrWhiteSpace(plan.GameId))
            throw new ArgumentException("A stable GameId is required.", nameof(plan));
        if (plan.ObservationDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(plan), "ObservationDuration must be positive.");
        if (plan.Interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(plan), "Interval must be positive.");
    }
}
