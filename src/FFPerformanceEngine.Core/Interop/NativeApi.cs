using System.Runtime.InteropServices;

namespace FFPerformanceEngine.Core.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct NativeMemoryInfo
{
    public ulong TotalPhysicalBytes;
    public ulong AvailablePhysicalBytes;
}

[StructLayout(LayoutKind.Sequential)]
public struct NativeCpuTimes
{
    public ulong Idle100Ns;
    public ulong Kernel100Ns;
    public ulong User100Ns;
}

public static partial class NativeApi
{
    private const string LibraryName = "ffpe_native";

    [LibraryImport(LibraryName, EntryPoint = "ffpe_abi_version")]
    public static partial int AbiVersion();

    [LibraryImport(LibraryName, EntryPoint = "ffpe_get_memory_info")]
    public static partial int GetMemoryInfo(out NativeMemoryInfo info);

    [LibraryImport(LibraryName, EntryPoint = "ffpe_get_cpu_times")]
    public static partial int GetCpuTimes(out NativeCpuTimes times);

    [LibraryImport(LibraryName, EntryPoint = "ffpe_set_process_priority")]
    public static partial int SetProcessPriority(uint processId, int priorityClass);
}
