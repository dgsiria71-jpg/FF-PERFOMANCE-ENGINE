namespace FFPerformanceEngine.Core.SystemOptimization;

public sealed class WindowsCpuCoreParkingPolicyMutationAdapter : WindowsPowerIndexMutationAdapter
{
    public const string Capability = "windows.cpu.core_parking_policy";

    public static readonly Guid ProcessorSettingsSubgroup = Guid.Parse("54533251-82be-4824-96c1-47b60b740d00");
    public static readonly Guid CoreParkingMinimumCoresSetting = Guid.Parse("0cc5b647-c1df-4637-891a-dec35c318583");

    public WindowsCpuCoreParkingPolicyMutationAdapter(IWindowsPowerSettingApi? api = null)
        : base(
            Capability,
            api ?? new WindowsPowerSettingApi(),
            ProcessorSettingsSubgroup,
            CoreParkingMinimumCoresSetting,
            minimum: 0,
            maximum: 100,
            displayName: "CPU core parking minimum cores")
    {
    }
}
