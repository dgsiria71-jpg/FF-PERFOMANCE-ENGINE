using FFPerformanceEngine.Core.Services;

namespace FFPerformanceEngine.Core.SystemOptimization;

public enum WindowsCapabilityBenchmarkPhase
{
    Baseline,
    Candidate
}

public sealed record WindowsCapabilityBenchmarkContext
{
    public WindowsCapabilityBenchmarkPhase Phase { get; init; }
    public string CapabilityId { get; init; } = string.Empty;
    public string BaselineValue { get; init; } = string.Empty;
    public string CandidateTarget { get; init; } = string.Empty;
}

public interface IWindowsCapabilityBenchmarkProbe
{
    Task<PerformanceIntervalSummary> CaptureAsync(
        WindowsCapabilityBenchmarkContext context,
        CancellationToken cancellationToken = default);
}

public sealed record WindowsCapabilityControlledBenchmarkResult
{
    public string CapabilityId { get; init; } = string.Empty;
    public string BaselineValue { get; init; } = string.Empty;
    public string CandidateTarget { get; init; } = string.Empty;
    public Guid TransactionId { get; init; }
    public Guid RestorePointId { get; init; }
    public required PerformanceABComparison Comparison { get; init; }
}

/// <summary>
/// Runs one Windows capability A/B experiment under the machine-wide controlled
/// benchmark lease. The candidate is applied only through a reversible session
/// transaction and the exact baseline state is restored before the result can
/// escape. This service produces observed controlled evidence only; it never
/// publishes a persistent recommendation.
/// </summary>
public sealed class WindowsCapabilityControlledBenchmarkService
{
    private readonly SystemOptimizationTransactionEngine _transactions;
    private readonly WindowsCapabilityMutationAdapterRegistry _adapters;
    private readonly IControlledBenchmarkLeaseManager _leases;
    private readonly IWindowsCapabilityBenchmarkProbe _probe;

    public WindowsCapabilityControlledBenchmarkService(
        SystemOptimizationTransactionEngine transactions,
        WindowsCapabilityMutationAdapterRegistry adapters,
        IControlledBenchmarkLeaseManager leases,
        IWindowsCapabilityBenchmarkProbe probe)
    {
        _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
        _adapters = adapters ?? throw new ArgumentNullException(nameof(adapters));
        _leases = leases ?? throw new ArgumentNullException(nameof(leases));
        _probe = probe ?? throw new ArgumentNullException(nameof(probe));
    }

    public async Task<WindowsCapabilityControlledBenchmarkResult> RunAsync(
        WindowsCapabilityCandidate candidate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var capabilityId = NormalizeId(candidate.CapabilityId);
        if (string.IsNullOrWhiteSpace(capabilityId))
            throw new ArgumentException("A controlled Windows capability benchmark requires a capability id.", nameof(candidate));
        if (string.IsNullOrWhiteSpace(candidate.TargetValue))
            throw new ArgumentException("A controlled Windows capability benchmark requires a candidate target.", nameof(candidate));

        await using var lease = await _leases.AcquireAsync(
            $"windows-capability-ab:{capabilityId}",
            cancellationToken).ConfigureAwait(false);

        var adapter = _adapters.GetRequired(capabilityId);
        var baselineRead = await adapter.ReadCurrentAsync(cancellationToken).ConfigureAwait(false);
        if (!baselineRead.Success || baselineRead.Value is null)
            throw new InvalidOperationException(
                $"Windows capability '{capabilityId}' baseline state could not be proven: {baselineRead.Message}");

        var baselineValue = baselineRead.Value;
        var baselineContext = new WindowsCapabilityBenchmarkContext
        {
            Phase = WindowsCapabilityBenchmarkPhase.Baseline,
            CapabilityId = capabilityId,
            BaselineValue = baselineValue,
            CandidateTarget = candidate.TargetValue
        };
        var baselineInterval = await _probe.CaptureAsync(baselineContext, cancellationToken).ConfigureAwait(false);
        var baselineEvidence = PerformanceEvidenceSnapshot.Capture(
            $"A · Windows baseline · {capabilityId}",
            baselineInterval,
            DateTimeOffset.UtcNow);
        EnsureMeasured(baselineEvidence, WindowsCapabilityBenchmarkPhase.Baseline);

        SystemOptimizationSession? session = null;
        try
        {
            session = await _transactions.BeginSessionAsync(
                $"DG Controlled A/B · {capabilityId}",
                [new WindowsMutationRequest(capabilityId, candidate.TargetValue, baselineValue)],
                cancellationToken).ConfigureAwait(false);

            var candidateContext = new WindowsCapabilityBenchmarkContext
            {
                Phase = WindowsCapabilityBenchmarkPhase.Candidate,
                CapabilityId = capabilityId,
                BaselineValue = baselineValue,
                CandidateTarget = candidate.TargetValue
            };
            var candidateInterval = await _probe.CaptureAsync(candidateContext, cancellationToken).ConfigureAwait(false);
            var candidateEvidence = PerformanceEvidenceSnapshot.Capture(
                $"B · Windows candidate · {capabilityId}",
                candidateInterval,
                DateTimeOffset.UtcNow);
            EnsureMeasured(candidateEvidence, WindowsCapabilityBenchmarkPhase.Candidate);

            return new WindowsCapabilityControlledBenchmarkResult
            {
                CapabilityId = capabilityId,
                BaselineValue = baselineValue,
                CandidateTarget = candidate.TargetValue,
                TransactionId = session.TransactionId,
                RestorePointId = session.RestorePointId,
                Comparison = PerformanceABComparison.Create(baselineEvidence, candidateEvidence)
            };
        }
        finally
        {
            if (session is not null && !session.IsRestored)
                await session.RestoreAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private static void EnsureMeasured(
        PerformanceEvidenceSnapshot snapshot,
        WindowsCapabilityBenchmarkPhase phase)
    {
        if (snapshot.Quality != PerformanceEvidenceQuality.Measured)
            throw new InvalidOperationException(
                $"Controlled Windows capability benchmark {phase} evidence must be fully measured; observed {snapshot.Quality}.");
    }

    private static string NormalizeId(string? capabilityId)
        => capabilityId?.Trim().ToLowerInvariant() ?? string.Empty;
}
