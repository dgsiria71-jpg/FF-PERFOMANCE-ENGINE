using FFPerformanceEngine.Core.SystemOptimization;

internal static class WindowsCpuBoostPolicyMutationAdapterSelfTests
{
    private static readonly Guid Scheme = Guid.Parse("381b4222-f694-41f0-9685-ff5bb260df2e");
    private static readonly Guid ProcessorSubgroup = Guid.Parse("54533251-82be-4824-96c1-47b60b740d00");
    private static readonly Guid PerfBoostMode = Guid.Parse("be337238-0d82-4146-a960-4f3749d470c7");

    internal static async Task RunAsync()
    {
        var api = new FakePowerSettingApi(Scheme, ac: 1, dc: 3);
        var adapter = new WindowsCpuBoostPolicyMutationAdapter(api);

        var read = await adapter.ReadCurrentAsync();
        Require(read.Success && string.Equals(read.Value, "ac=1;dc=3", StringComparison.Ordinal),
            "CPU boost reader must preserve the exact AC/DC indexes from the active scheme.");
        Require(api.LastSubgroup == ProcessorSubgroup && api.LastSetting == PerfBoostMode,
            "CPU boost adapter must address the documented processor subgroup and PERFBOOSTMODE GUIDs.");

        Require(adapter.Validate("2", SystemOptimizationScope.Session).Success
                && adapter.Validate("4", SystemOptimizationScope.Persistent).Success,
            "CPU boost mode indexes in the supported 0..4 range must be accepted for session and persistent transactions.");
        Require(!adapter.Validate("5", SystemOptimizationScope.Session).Success
                && !adapter.Validate("aggressive", SystemOptimizationScope.Session).Success,
            "CPU boost policy must reject out-of-range or textual targets before touching Windows.");

        var snapshot = await adapter.SnapshotAsync();
        Require(snapshot.OriginalValue == "ac=1;dc=3"
                && snapshot.RestorePayload.Contains(Scheme.ToString("D"), StringComparison.OrdinalIgnoreCase),
            "CPU boost snapshot must bind the exact active scheme and both original AC/DC values.");

        var apply = await adapter.ApplyAsync("2");
        Require(apply.Success && api.AcValue == 2 && api.DcValue == 2 && api.ActivationCount == 1,
            "CPU boost apply must set AC and DC indexes and reactivate the same scheme exactly once.");
        Require(await adapter.VerifyAsync("2"),
            "CPU boost verify must re-read both AC/DC indexes instead of trusting the write result.");

        await adapter.RollbackAsync(snapshot);
        Require(api.AcValue == 1 && api.DcValue == 3 && api.ActivationCount == 2,
            "CPU boost rollback must restore both original AC/DC values and reactivate the original scheme.");
        var restored = await adapter.ReadCurrentAsync();
        Require(restored.Success && restored.Value == "ac=1;dc=3",
            "CPU boost rollback must be externally verifiable through the normal read path.");

        var unavailable = new WindowsCpuBoostPolicyMutationAdapter(new FailingPowerSettingApi());
        var unavailableRead = await unavailable.ReadCurrentAsync();
        Require(!unavailableRead.Success && unavailableRead.Value is null,
            "If PowrProf cannot prove active scheme/settings, CPU boost must report unavailable instead of inventing a value.");

        await WindowsCpuCoreParkingPolicyMutationAdapterSelfTests.RunAsync();

        Console.WriteLine("PASS Track 2 Windows CPU boost policy PowrProf read/snapshot/apply/verify/rollback contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class FakePowerSettingApi(Guid scheme, uint ac, uint dc) : IWindowsPowerSettingApi
    {
        public Guid Scheme { get; } = scheme;
        public uint AcValue { get; private set; } = ac;
        public uint DcValue { get; private set; } = dc;
        public int ActivationCount { get; private set; }
        public Guid LastSubgroup { get; private set; }
        public Guid LastSetting { get; private set; }

        public WindowsPowerApiResult<Guid> GetActiveScheme()
            => WindowsPowerApiResult<Guid>.Ok(Scheme);

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
            Require(schemeId == Scheme, "CPU boost adapter must reactivate the scheme it actually changed.");
            ActivationCount++;
            return WindowsPowerApiResult.Ok();
        }

        private void Track(Guid schemeId, Guid subgroup, Guid setting)
        {
            Require(schemeId == Scheme, "CPU boost adapter must read/write the active scheme returned by PowrProf.");
            LastSubgroup = subgroup;
            LastSetting = setting;
        }
    }

    private sealed class FailingPowerSettingApi : IWindowsPowerSettingApi
    {
        public WindowsPowerApiResult<Guid> GetActiveScheme() => WindowsPowerApiResult<Guid>.Fail("no active scheme");
        public WindowsPowerApiResult<uint> ReadAcValue(Guid schemeId, Guid subgroup, Guid setting) => WindowsPowerApiResult<uint>.Fail("unavailable");
        public WindowsPowerApiResult<uint> ReadDcValue(Guid schemeId, Guid subgroup, Guid setting) => WindowsPowerApiResult<uint>.Fail("unavailable");
        public WindowsPowerApiResult WriteAcValue(Guid schemeId, Guid subgroup, Guid setting, uint value) => WindowsPowerApiResult.Fail("unavailable");
        public WindowsPowerApiResult WriteDcValue(Guid schemeId, Guid subgroup, Guid setting, uint value) => WindowsPowerApiResult.Fail("unavailable");
        public WindowsPowerApiResult SetActiveScheme(Guid schemeId) => WindowsPowerApiResult.Fail("unavailable");
    }
}
