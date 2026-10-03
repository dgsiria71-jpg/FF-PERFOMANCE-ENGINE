using FFPerformanceEngine.Core.Diagnostics;

namespace FFPerformanceEngine.Core.SystemOptimization;

/// <summary>
/// Operational entry point for automatic persistent recommendation publication.
/// It refreshes concrete Windows capability state immediately before capturing
/// the current MachineContext, then delegates the final provenance/adapter gate
/// to <see cref="PersistentPcRecommendationCoordinator"/>.
/// Batch publication performs one discovery/capture pass for the complete set.
/// </summary>
public sealed class PersistentPcRecommendationService
{
    private readonly WindowsPerformanceCapabilityDiscoveryService _discovery;
    private readonly Func<MachineContext> _captureMachine;
    private readonly PersistentPcRecommendationCoordinator _coordinator;

    public PersistentPcRecommendationService(
        WindowsPerformanceCapabilityDiscoveryService discovery,
        Func<MachineContext> captureMachine,
        PersistentPcRecommendationCoordinator coordinator)
    {
        _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
        _captureMachine = captureMachine ?? throw new ArgumentNullException(nameof(captureMachine));
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
    }

    public async Task<CapabilityRecommendationPublicationResult> PublishAsync(
        string capabilityId,
        string targetValue,
        CapabilityRecommendationSummary recommendation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recommendation);

        await _discovery.RefreshAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var machine = _captureMachine();
        return _coordinator.Publish(machine, capabilityId, targetValue, recommendation);
    }

    public async Task<PersistentPcRecommendationBatchResult> PublishBatchAsync(
        IReadOnlyList<PersistentPcRecommendationCandidate> candidates,
        CancellationToken cancellationToken = default)
    {
        ValidateBatchInput(candidates);

        await _discovery.RefreshAsync(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var machine = _captureMachine();
        return _coordinator.PublishBatch(machine, candidates);
    }

    private static void ValidateBatchInput(IReadOnlyList<PersistentPcRecommendationCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (candidates.Count == 0)
            throw new ArgumentException("A recommendation batch requires at least one candidate.", nameof(candidates));

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            if (candidate is null)
                throw new ArgumentException("Recommendation batches cannot contain null candidates.", nameof(candidates));
            ArgumentNullException.ThrowIfNull(candidate.Recommendation);

            var id = candidate.CapabilityId?.Trim().ToLowerInvariant() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Every recommendation candidate requires a capability identity.", nameof(candidates));
            if (!seen.Add(id))
                throw new ArgumentException($"Duplicate recommendation capability '{id}' in the same batch.", nameof(candidates));
        }
    }
}
