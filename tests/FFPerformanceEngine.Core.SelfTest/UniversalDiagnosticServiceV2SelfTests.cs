using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Telemetry;

internal static class UniversalDiagnosticServiceV2SelfTests
{
    internal static void Run()
    {
        var machineContext = new MachineContextService(
            new HardwareDiscoveryService(),
            new WindowsPerformanceCapabilityRegistry());
        var analyzer = new UniversalBottleneckAnalyzer();
        var service = new UniversalDiagnosticService(machineContext, analyzer);
        var environment = new EnvironmentSnapshot
        {
            MachineName = "DG-TYPED-DIAGNOSTIC-PC",
            WindowsDescription = "Windows 11 typed diagnostics",
            LogicalProcessors = 16,
            MemoryTotalGb = 32,
            Is64BitOs = true
        };
        var timestamp = new DateTimeOffset(2026, 9, 9, 1, 30, 0, TimeSpan.Zero);
        var context = new BottleneckAnalysisContext
        {
            TargetFps = 120,
            CriticalThreadCpuPercent = 54
        };

        var measuredGpuFrame = new TelemetryFrame(timestamp,
        [
            Metric(TelemetryStandardMetrics.FrameFpsAverage, 80),
            Metric(TelemetryStandardMetrics.SystemGpuUtilizationPercent, 99)
        ]);
        var snapshot = service.Analyze(environment, measuredGpuFrame, context);
        Require(snapshot.Bottleneck.Primary == BottleneckKind.Gpu,
            "Typed UniversalDiagnosticService must delegate TelemetryFrame evidence directly to the fail-closed typed bottleneck analyzer.");
        Require(snapshot.Machine.Environment.MachineName == environment.MachineName
                && snapshot.Machine.Fingerprint.Id.Length == 64,
            "Typed UniversalDiagnosticService must compose the existing MachineContext capture rather than create a parallel machine identity.");

        var partialGpuFrame = new TelemetryFrame(timestamp.AddSeconds(1),
        [
            Metric(TelemetryStandardMetrics.FrameFpsAverage, 80),
            Metric(TelemetryStandardMetrics.SystemGpuUtilizationPercent, 99,
                quality: TelemetryMetricQuality.Partial)
        ]);
        var partial = service.Analyze(environment, partialGpuFrame, context);
        Require(partial.Bottleneck.Candidates.All(candidate => candidate.Kind != BottleneckKind.Gpu),
            "Typed diagnostics must preserve per-metric Partial quality and must not round-trip through legacy DataQuality semantics.");

        RequireThrows<ArgumentNullException>(
            () => service.Analyze(null!, measuredGpuFrame, context),
            "Typed UniversalDiagnosticService must reject a null environment.");
        RequireThrows<ArgumentNullException>(
            () => service.Analyze(environment, (TelemetryFrame)null!, context),
            "Typed UniversalDiagnosticService must reject a null telemetry frame.");
        RequireThrows<ArgumentNullException>(
            () => service.Analyze(environment, measuredGpuFrame, null!),
            "Typed UniversalDiagnosticService must reject a null bottleneck context.");

        Console.WriteLine("PASS Track 4 UniversalDiagnosticService consumes typed telemetry directly without a legacy quality round-trip");
        PerformanceTypedEvidenceSelfTests.Run();
        PerformanceTypedEvidenceHistorySelfTests.RunAsync().GetAwaiter().GetResult();
    }

    private static TelemetryMetricObservation Metric(
        TelemetryMetricDescriptor descriptor,
        double value,
        TelemetryMetricQuality quality = TelemetryMetricQuality.Measured)
        => new(
            descriptor,
            value,
            quality,
            1,
            "diagnostic-v2-selftest",
            TelemetryMetricOrigin.Direct);

    private static void RequireThrows<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
