using System.Runtime.InteropServices;

namespace FFPerformanceEngine.Core.SystemOptimization;

public readonly record struct WindowsPowerApiResult(bool Success, string Message)
{
    public static WindowsPowerApiResult Ok(string message = "ok") => new(true, message);
    public static WindowsPowerApiResult Fail(string message) => new(false, message);
}

public readonly record struct WindowsPowerApiResult<T>(bool Success, T Value, string Message)
{
    public static WindowsPowerApiResult<T> Ok(T value, string message = "ok") => new(true, value, message);
    public static WindowsPowerApiResult<T> Fail(string message) => new(false, default!, message);
}

/// <summary>
/// Narrow, testable boundary over the supported Windows PowrProf APIs used by
/// machine-global power-setting capabilities. Implementations must report
/// failure rather than infer or substitute values that Windows did not return.
/// </summary>
public interface IWindowsPowerSettingApi
{
    WindowsPowerApiResult<Guid> GetActiveScheme();
    WindowsPowerApiResult<uint> ReadAcValue(Guid schemeId, Guid subgroup, Guid setting);
    WindowsPowerApiResult<uint> ReadDcValue(Guid schemeId, Guid subgroup, Guid setting);
    WindowsPowerApiResult WriteAcValue(Guid schemeId, Guid subgroup, Guid setting, uint value);
    WindowsPowerApiResult WriteDcValue(Guid schemeId, Guid subgroup, Guid setting, uint value);
    WindowsPowerApiResult SetActiveScheme(Guid schemeId);
}

public sealed class WindowsPowerSettingApi : IWindowsPowerSettingApi
{
    private const uint ErrorSuccess = 0;

    public WindowsPowerApiResult<Guid> GetActiveScheme()
    {
        if (!OperatingSystem.IsWindows())
            return WindowsPowerApiResult<Guid>.Fail("PowrProf is available only on Windows.");

        nint pointer = 0;
        try
        {
            var code = NativeMethods.PowerGetActiveScheme(0, out pointer);
            if (code != ErrorSuccess || pointer == 0)
                return WindowsPowerApiResult<Guid>.Fail(FormatFailure("PowerGetActiveScheme", code));

            return WindowsPowerApiResult<Guid>.Ok(
                Marshal.PtrToStructure<Guid>(pointer),
                "active power scheme read");
        }
        catch (Exception ex) when (IsInteropFailure(ex))
        {
            return WindowsPowerApiResult<Guid>.Fail($"PowerGetActiveScheme unavailable: {ex.Message}");
        }
        finally
        {
            if (pointer != 0) _ = NativeMethods.LocalFree(pointer);
        }
    }

    public WindowsPowerApiResult<uint> ReadAcValue(Guid schemeId, Guid subgroup, Guid setting)
    {
        if (!OperatingSystem.IsWindows())
            return WindowsPowerApiResult<uint>.Fail("PowrProf is available only on Windows.");

        try
        {
            var code = NativeMethods.PowerReadACValueIndex(0, ref schemeId, ref subgroup, ref setting, out var value);
            return code == ErrorSuccess
                ? WindowsPowerApiResult<uint>.Ok(value, "AC power setting read")
                : WindowsPowerApiResult<uint>.Fail(FormatFailure("PowerReadACValueIndex", code));
        }
        catch (Exception ex) when (IsInteropFailure(ex))
        {
            return WindowsPowerApiResult<uint>.Fail($"PowerReadACValueIndex unavailable: {ex.Message}");
        }
    }

    public WindowsPowerApiResult<uint> ReadDcValue(Guid schemeId, Guid subgroup, Guid setting)
    {
        if (!OperatingSystem.IsWindows())
            return WindowsPowerApiResult<uint>.Fail("PowrProf is available only on Windows.");

        try
        {
            var code = NativeMethods.PowerReadDCValueIndex(0, ref schemeId, ref subgroup, ref setting, out var value);
            return code == ErrorSuccess
                ? WindowsPowerApiResult<uint>.Ok(value, "DC power setting read")
                : WindowsPowerApiResult<uint>.Fail(FormatFailure("PowerReadDCValueIndex", code));
        }
        catch (Exception ex) when (IsInteropFailure(ex))
        {
            return WindowsPowerApiResult<uint>.Fail($"PowerReadDCValueIndex unavailable: {ex.Message}");
        }
    }

    public WindowsPowerApiResult WriteAcValue(Guid schemeId, Guid subgroup, Guid setting, uint value)
    {
        if (!OperatingSystem.IsWindows())
            return WindowsPowerApiResult.Fail("PowrProf is available only on Windows.");

        try
        {
            var code = NativeMethods.PowerWriteACValueIndex(0, ref schemeId, ref subgroup, ref setting, value);
            return code == ErrorSuccess
                ? WindowsPowerApiResult.Ok("AC power setting written")
                : WindowsPowerApiResult.Fail(FormatFailure("PowerWriteACValueIndex", code));
        }
        catch (Exception ex) when (IsInteropFailure(ex))
        {
            return WindowsPowerApiResult.Fail($"PowerWriteACValueIndex unavailable: {ex.Message}");
        }
    }

    public WindowsPowerApiResult WriteDcValue(Guid schemeId, Guid subgroup, Guid setting, uint value)
    {
        if (!OperatingSystem.IsWindows())
            return WindowsPowerApiResult.Fail("PowrProf is available only on Windows.");

        try
        {
            var code = NativeMethods.PowerWriteDCValueIndex(0, ref schemeId, ref subgroup, ref setting, value);
            return code == ErrorSuccess
                ? WindowsPowerApiResult.Ok("DC power setting written")
                : WindowsPowerApiResult.Fail(FormatFailure("PowerWriteDCValueIndex", code));
        }
        catch (Exception ex) when (IsInteropFailure(ex))
        {
            return WindowsPowerApiResult.Fail($"PowerWriteDCValueIndex unavailable: {ex.Message}");
        }
    }

    public WindowsPowerApiResult SetActiveScheme(Guid schemeId)
    {
        if (!OperatingSystem.IsWindows())
            return WindowsPowerApiResult.Fail("PowrProf is available only on Windows.");

        try
        {
            var code = NativeMethods.PowerSetActiveScheme(0, ref schemeId);
            return code == ErrorSuccess
                ? WindowsPowerApiResult.Ok("power scheme activated")
                : WindowsPowerApiResult.Fail(FormatFailure("PowerSetActiveScheme", code));
        }
        catch (Exception ex) when (IsInteropFailure(ex))
        {
            return WindowsPowerApiResult.Fail($"PowerSetActiveScheme unavailable: {ex.Message}");
        }
    }

    private static bool IsInteropFailure(Exception ex)
        => ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException or MarshalDirectiveException;

    private static string FormatFailure(string operation, uint code)
        => $"{operation} failed with Win32 error code {code}.";

    private static class NativeMethods
    {
        [DllImport("powrprof.dll", ExactSpelling = true)]
        internal static extern uint PowerGetActiveScheme(nint userRootPowerKey, out nint activePolicyGuid);

        [DllImport("powrprof.dll", ExactSpelling = true)]
        internal static extern uint PowerReadACValueIndex(
            nint rootPowerKey,
            ref Guid schemeGuid,
            ref Guid subgroupOfPowerSettingsGuid,
            ref Guid powerSettingGuid,
            out uint acValueIndex);

        [DllImport("powrprof.dll", ExactSpelling = true)]
        internal static extern uint PowerReadDCValueIndex(
            nint rootPowerKey,
            ref Guid schemeGuid,
            ref Guid subgroupOfPowerSettingsGuid,
            ref Guid powerSettingGuid,
            out uint dcValueIndex);

        [DllImport("powrprof.dll", ExactSpelling = true)]
        internal static extern uint PowerWriteACValueIndex(
            nint rootPowerKey,
            ref Guid schemeGuid,
            ref Guid subgroupOfPowerSettingsGuid,
            ref Guid powerSettingGuid,
            uint acValueIndex);

        [DllImport("powrprof.dll", ExactSpelling = true)]
        internal static extern uint PowerWriteDCValueIndex(
            nint rootPowerKey,
            ref Guid schemeGuid,
            ref Guid subgroupOfPowerSettingsGuid,
            ref Guid powerSettingGuid,
            uint dcValueIndex);

        [DllImport("powrprof.dll", ExactSpelling = true)]
        internal static extern uint PowerSetActiveScheme(nint userRootPowerKey, ref Guid schemeGuid);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern nint LocalFree(nint memory);
    }
}
