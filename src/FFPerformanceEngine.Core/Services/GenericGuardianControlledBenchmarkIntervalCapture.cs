namespace FFPerformanceEngine.Core.Services;

/// <summary>
/// Read-only observation of one physical capture delegate. The only accepted
/// authority is the real process-wide ControlledBenchmarkLeaseManager: a caller
/// cannot turn an arbitrary 'false' interference flag into a clean observation.
/// This is not an exclusion lease and does not prove scene/load equivalence or
/// the absence of external benchmarking and non-Track-0 mutations.
/// </summary>
public sealed class GenericGuardianControlledBenchmarkIntervalCapture
{
    private readonly ControlledBenchmarkLeaseManager _authority;

    public GenericGuardianControlledBenchmarkIntervalCapture(ControlledBenchmarkLeaseManager authority)
        => _authority = authority ?? throw new ArgumentNullException(nameof(authority));

    public async Task<GenericGuardianControlledBenchmarkCaptureResult<T>> CaptureAsync<T>(
        Func<CancellationToken, Task<T>> capture,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(capture);
        cancellationToken.ThrowIfCancellationRequested();

        // Read IMMEDIATELY before entering the physical capture delegate.
        // An already-active controlled benchmark must not be sampled.
        var before = _authority.SnapshotActivity();
        if (before.State != ControlledBenchmarkActivityState.Idle)
            return new GenericGuardianControlledBenchmarkCaptureResult<T>(
                attempted: false, value: default, before, before);

        // A benchmark that acquires AND releases while the delegate is running
        // changes the generation even though both readings will say Idle.
        // Capture exceptions/cancellation propagate unchanged; this observer
        // has no mutating cleanup or result to promote on that path.
        var value = await capture(cancellationToken).ConfigureAwait(false);
        var after = _authority.SnapshotActivity();
        return new GenericGuardianControlledBenchmarkCaptureResult<T>(
            attempted: true, value, before, after);
    }
}

/// <summary>
/// Result construction is restricted to the monitor. Even clean windows are
/// NOT permission for a canary KEEP: the owning executor must also compare
/// the FIRST before snapshot with the LAST after snapshot across mutation,
/// validate its real session/scene/source and retain exact rollback ownership.
/// </summary>
public sealed class GenericGuardianControlledBenchmarkCaptureResult<T>
{
    internal GenericGuardianControlledBenchmarkCaptureResult(
        bool attempted,
        T? value,
        ControlledBenchmarkActivitySnapshot before,
        ControlledBenchmarkActivitySnapshot after)
    {
        Attempted = attempted;
        Value = value;
        Before = before;
        After = after;
    }

    public bool Attempted { get; }
    public T? Value { get; }
    public ControlledBenchmarkActivitySnapshot Before { get; }
    public ControlledBenchmarkActivitySnapshot After { get; }

    public bool UninterruptedIdle => Attempted
        && ControlledBenchmarkActivitySnapshot.ProvesUninterruptedIdle(Before, After);
}
