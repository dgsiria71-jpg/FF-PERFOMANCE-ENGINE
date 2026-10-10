using System.Runtime.CompilerServices;

internal static class PerformanceWorkloadContextSelectionSelfTestBootstrap
{
    [ModuleInitializer]
    internal static void Initialize()
        => PerformanceWorkloadContextSelectionSelfTests.Run();
}
