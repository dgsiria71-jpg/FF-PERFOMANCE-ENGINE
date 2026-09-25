using FFPerformanceEngine.Core.SystemOptimization;

internal static class WindowsCpuCoreParkingPolicyMutationAdapterSelfTests
{
    private static readonly Guid Scheme = Guid.Parse("381b4222-f694-41f0-9685-ff5bb260df2e");
    private static readonly Guid ProcessorSubgroup = Guid.Parse("54533251-82be-4824-96c1-47b60b740d00");
    private static readonly Guid CoreParkingMinimumCores = Guid.Parse("0cc5b647-c1df-4637-891a-dec35c318583");

    internal static async Task RunAsync()
    {
        var api = new FakePowerSettingApi(Scheme, ac: 25, dc: 10);
        var adapter = new WindowsCpuCoreParkingPolicyMutationAdapter(api);

        var read = await adapter.ReadCurrentAsync();
        Require(read.Success && read.Value == "ac=25;dc=10",
            "Core parking reader must preserve exact AC/DC minimum-unparked percentages.");
        Require(api.LastSubgroup == ProcessorSubgroup && api.LastSetting == CoreParkingMinimumCores,
            "Core parking adapter must use the processor subgroup and CPMINCORES setting GUID.");

        Require(adapter.Validate("0", SystemOptimizationScope.Session).Success
                && adapter.Validate("100", SystemOptimizationScope.Persistent).Success,
            "Core parking minimum cores must accept the documented 0..100 percent range.");
        Require(!adapter.Validate("101", SystemOptimizationScope.Session).Success
                && !adapter.Validate("-1", SystemOptimizationScope.Session).Success,
            "Core parking must reject values outside 0..100 before touching Windows.");

        var snapshot = await adapter.SnapshotAsync();
        Require(snapshot.OriginalValue == "ac=25;dc=10"
                && snapshot.RestorePayload.Contains(Scheme.ToString("D"), StringComparison.OrdinalIgnoreCase),
            "Core parking snapshot must preserve the exact scheme and both AC/DC values.");

        var apply = await adapter.ApplyAsync("100");
        Require(apply.Success && api.AcValue == 100 && api.DcValue == 100 && api.ActivationCount == 1,
            "Core parking apply must set both AC/DC minimum-unparked percentages and reactivate the scheme.");
        Require(await adapter.VerifyAsync("100"),
            "Core parking verify must re-read both values after apply.");

        await adapter.RollbackAsync(snapshot);
        Require(api.AcValue == 25 && api.DcValue == 10 && api.ActivationCount == 2,
            "Core parking rollback must restore both original percentages and reactivate the original scheme.");

        var unavailable = new WindowsCpuCoreParkingPolicyMutationAdapter(new FailingPowerSettingApi());
        var unavailableRead = await unavailable.ReadCurrentAsync();
        Require(!unavailableRead.Success && unavailableRead.Value is null,
            "Core parking must remain unavailable when PowrProf cannot prove the current setting.");

        Console.WriteLine("PASS Track 2 Windows core parking minimum-cores PowrProf read/snapshot/apply/verify/rollback contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class FakePowerSettingApi(Guid scheme, uint ac, uint dc) : IWindowsPowerSettingApi
    {
        public uint AcValue { get; private set; } = ac;
        public uint DcValue { get; private set; } = dc;
        public int ActivationCount { get; private set; }
        public Guid LastSubgroup { get; private set; }
        public Guid LastSetting { get; private set; }

        public WindowsPowerApiResult<Guid> GetActiveScheme() => WindowsPowerApiResult<Guid>.Ok(scheme);

        public WindowsPowerApiResult<uint> ReadAcValue(Guid schemeId, Guid subgroup, Guid setting)
        {
            Track(schemeId, subgroup, setting);
            return WindowsPowerApiResult<uint>.Ok(AcValue);
        }

        public WindowsPowerApiResult<uint> ReadDcValue(Guid schemeId, Guid subgroup, Guid setting)
        {
            Track(schemeId, subgroup, setting);
            return WindowsPowerApiResult<uint>.Ok(DcValue);
        }

        public WindowsPowerApiResult WriteAcValue(Guid schemeId, Guid subgroup, Guid setting, uint value)
        {
            Track(schemeId, subgroup, setting);
            AcValue = value;
            return WindowsPowerApiResult.Ok();
        }

        public WindowsPowerApiResult WriteDcValue(Guid schemeId, Guid subgroup, Guid setting, uint value)
        {
            Track(schemeId, subgroup, setting);
            DcValue = value;
            return WindowsPowerApiResult.Ok();
        }

        public WindowsPowerApiResult SetActiveScheme(Guid schemeId)
        {
            Require(schemeId == scheme, "Core parking adapter must reactivate the exact scheme it changed.");
            ActivationCount++;
            return WindowsPowerApiResult.Ok();
        }

        private void Track(Guid schemeId, Guid subgroup, Guid setting)
        {
            Require(schemeId == scheme, "Core parking adapter must use the active scheme returned by PowrProf.");
            LastSubgroup = subgroup;
            LastSetting = setting;
        }
    }

    private sealed class FailingPowerSettingApi : IWindowsPowerSettingApi
    {
        public WindowsPowerApiResult<Guid> GetActiveScheme() => WindowsPowerApiResult<Guid>.Fail("unavailable");
        public WindowsPowerApiResult<uint> ReadAcValue(Guid schemeId, Guid subgroup, Guid setting) => WindowsPowerApiResult<uint>.Fail("unavailable");
        public WindowsPowerApiResult<uint> ReadDcValue(Guid schemeId, Guid subgroup, Guid setting) => WindowsPowerApiResult<uint>.Fail("unavailable");
        public WindowsPowerApiResult WriteAcValue(Guid schemeId, Guid subgroup, Guid setting, uint value) => WindowsPowerApiResult.Fail("unavailable");
        public WindowsPowerApiResult WriteDcValue(Guid schemeId, Guid subgroup, Guid setting, uint value) => WindowsPowerApiResult.Fail("unavailable");
        public WindowsPowerApiResult SetActiveScheme(Guid schemeId) => WindowsPowerApiResult.Fail("unavailable");
    }
}
