using FFPerformanceEngine.Core.Diagnostics;

namespace FFPerformanceEngine.Core.SystemOptimization;

/// <summary>
/// Operational entry point for automatic persistent recommendation publication.
/// It refreshes concrete Windows capability state immediately before capturing
/// the current MachineContext, then delegates the final provenance/adapter gate
/// to <see cref="PersistentPcRecommendationCoordinator"/>.
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
}
