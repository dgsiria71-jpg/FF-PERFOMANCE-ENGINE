using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Telemetry;

namespace FFPerformanceEngine.Core.Diagnostics;

public sealed class UniversalDiagnosticService
{
    private readonly MachineContextService _machineContext;
    private readonly UniversalBottleneckAnalyzer _bottleneckAnalyzer;

    public UniversalDiagnosticService(
        MachineContextService machineContext,
        UniversalBottleneckAnalyzer bottleneckAnalyzer)
    {
        _machineContext = machineContext ?? throw new ArgumentNullException(nameof(machineContext));
        _bottleneckAnalyzer = bottleneckAnalyzer ?? throw new ArgumentNullException(nameof(bottleneckAnalyzer));
    }

    public UniversalDiagnosticSnapshot Analyze(
        EnvironmentSnapshot environment,
        TelemetrySample sample,
        BottleneckAnalysisContext context)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(sample);
        ArgumentNullException.ThrowIfNull(context);

        var machine = _machineContext.Capture(environment);
        var bottleneck = _bottleneckAnalyzer.Analyze(sample, context);
        return new UniversalDiagnosticSnapshot
        {
            CapturedAt = DateTimeOffset.UtcNow,
            Machine = machine,
            Bottleneck = bottleneck
        };
    }

    public UniversalDiagnosticSnapshot Analyze(
        EnvironmentSnapshot environment,
        TelemetryFrame frame,
        BottleneckAnalysisContext context)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(context);

        var machine = _machineContext.Capture(environment);
        var bottleneck = _bottleneckAnalyzer.Analyze(frame, context);
        return new UniversalDiagnosticSnapshot
        {
            CapturedAt = DateTimeOffset.UtcNow,
            Machine = machine,
            Bottleneck = bottleneck
        };
    }
}
