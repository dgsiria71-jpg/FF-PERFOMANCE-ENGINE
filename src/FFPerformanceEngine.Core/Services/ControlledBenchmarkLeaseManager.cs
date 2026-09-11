using System.Runtime.ExceptionServices;

namespace FFPerformanceEngine.Core.Services;

public sealed record ControlledBenchmarkGuardianState(
    bool WasRunning,
    string? InstanceName,
    TimeSpan Interval);

public interface IControlledBenchmarkGuardian
{
    Task<ControlledBenchmarkGuardianState> SuspendAsync(CancellationToken cancellationToken = default);
    Task ResumeAsync(ControlledBenchmarkGuardianState state, CancellationToken cancellationToken = default);
}

public interface IControlledBenchmarkLeaseManager
{
    Task<IAsyncDisposable> AcquireAsync(string owner, CancellationToken cancellationToken = default);
}

public sealed class ControlledBenchmarkLeaseManager : IControlledBenchmarkLeaseManager
{
    // Controlled benchmarks contend for machine-wide CPU/GPU/PresentMon resources,
    // so the gate is intentionally global instead of per BlueStacks instance.
    private static readonly SemaphoreSlim GlobalGate = new(1, 1);
    private readonly IControlledBenchmarkGuardian? _guardian;

    public ControlledBenchmarkLeaseManager()
    {
    }

    public ControlledBenchmarkLeaseManager(IControlledBenchmarkGuardian guardian)
        => _guardian = guardian ?? throw new ArgumentNullException(nameof(guardian));

    public async Task<IAsyncDisposable> AcquireAsync(
        string owner,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(owner))
            throw new ArgumentException("A controlled benchmark owner is required.", nameof(owner));

        // Cancellation happens before any Guardian/runtime mutation when the gate is busy.
        await GlobalGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        ControlledBenchmarkGuardianState? guardianState = null;
        try
        {
            if (_guardian is not null)
                guardianState = await _guardian.SuspendAsync(cancellationToken).ConfigureAwait(false);

            return new Lease(GlobalGate, _guardian, guardianState);
        }
        catch
        {
            GlobalGate.Release();
            throw;
        }
    }

    private sealed class Lease(
        SemaphoreSlim gate,
        IControlledBenchmarkGuardian? guardian,
        ControlledBenchmarkGuardianState? guardianState) : IAsyncDisposable
    {
        private int _disposed;

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

            Exception? resumeFailure = null;
            try
            {
                // User cancellation must never skip Guardian reconciliation.
                if (guardian is not null && guardianState is not null)
                    await guardian.ResumeAsync(guardianState, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                resumeFailure = exception;
            }
            finally
            {
                gate.Release();
            }

            if (resumeFailure is not null)
                ExceptionDispatchInfo.Capture(resumeFailure).Throw();
        }
    }
}
