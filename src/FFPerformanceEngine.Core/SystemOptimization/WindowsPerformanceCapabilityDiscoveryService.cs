using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.SystemOptimization;

/// <summary>
/// Resolves runtime availability/current state for the Track 1 Windows
/// capability catalog using only concrete mutation adapters that can prove
/// their state against the running OS. Descriptor metadata remains owned by
/// <see cref="WindowsPerformanceCapabilityRegistry"/>.
/// </summary>
public sealed class WindowsPerformanceCapabilityDiscoveryService
{
    private readonly WindowsPerformanceCapabilityRegistry _capabilities;
    private readonly WindowsCapabilityMutationAdapterRegistry _adapters;

    public WindowsPerformanceCapabilityDiscoveryService(
        WindowsPerformanceCapabilityRegistry capabilities,
        WindowsCapabilityMutationAdapterRegistry adapters)
    {
        _capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
        _adapters = adapters ?? throw new ArgumentNullException(nameof(adapters));
    }

    public async Task<IReadOnlyList<WindowsPerformanceCapability>> RefreshAsync(
        CancellationToken cancellationToken = default)
    {
        foreach (var capability in _capabilities.GetAll())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_adapters.TryGet(capability.CapabilityId, out var adapter))
            {
                _capabilities.UpdateRuntimeState(
                    capability.CapabilityId,
                    CapabilityAvailability.Unknown,
                    currentValue: null);
                continue;
            }

            WindowsCapabilityReadResult read;
            try
            {
                read = await adapter.ReadCurrentAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                // Discovery is deliberately fail-closed. A concrete adapter that
                // unexpectedly fails cannot make the capability look usable.
                _capabilities.UpdateRuntimeState(
                    capability.CapabilityId,
                    CapabilityAvailability.Unavailable,
                    currentValue: null);
                continue;
            }

            _capabilities.UpdateRuntimeState(
                capability.CapabilityId,
                read.Success ? CapabilityAvailability.Available : CapabilityAvailability.Unavailable,
                read.Success ? read.Value : null);
        }

        return _capabilities.GetAll();
    }
}
