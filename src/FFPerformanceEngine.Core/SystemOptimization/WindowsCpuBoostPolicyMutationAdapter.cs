namespace FFPerformanceEngine.Core.SystemOptimization;

public sealed class WindowsCpuBoostPolicyMutationAdapter : WindowsPowerIndexMutationAdapter
{
    public const string Capability = "windows.cpu.boost_policy";

    public static readonly Guid ProcessorSettingsSubgroup = Guid.Parse("54533251-82be-4824-96c1-47b60b740d00");
    public static readonly Guid PerformanceBoostModeSetting = Guid.Parse("be337238-0d82-4146-a960-4f3749d470c7");

    public WindowsCpuBoostPolicyMutationAdapter(IWindowsPowerSettingApi? api = null)
        : base(
            Capability,
            api ?? new WindowsPowerSettingApi(),
            ProcessorSettingsSubgroup,
            PerformanceBoostModeSetting,
            minimum: 0,
            maximum: 4,
            displayName: "CPU boost policy")
    {
    }
}
