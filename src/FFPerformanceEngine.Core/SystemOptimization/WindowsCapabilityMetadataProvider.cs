using FFPerformanceEngine.Core.Diagnostics;

namespace FFPerformanceEngine.Core.SystemOptimization;

/// <summary>
/// Optional descriptor metadata exposed by a concrete Windows capability adapter.
/// This describes the target space the adapter can validate; it is not a
/// recommendation and must never populate RecommendedValue by itself.
/// </summary>
public interface IWindowsCapabilityMetadataProvider
{
    CapabilityValueSchema ValueSchema { get; }
    IReadOnlyList<string> AvailableValues { get; }
}
