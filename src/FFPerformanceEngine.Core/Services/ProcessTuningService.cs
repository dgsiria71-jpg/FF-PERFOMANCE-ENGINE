using System.Diagnostics;
using FFPerformanceEngine.Core.Interop;
using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.Core.Services;

public sealed class ProcessTuningService
{
    public const int NormalPriorityClass = 0x20;
    public const int AboveNormalPriorityClass = 0x8000;
    public const int HighPriorityClass = 0x80;

    public int? FindBlueStacksPlayerPid()
    {
        try
        {
            return Process.GetProcessesByName("HD-Player")
                .Where(x => !x.HasExited)
                .OrderByDescending(x => x.WorkingSet64)
                .Select(x => (int?)x.Id)
                .FirstOrDefault();
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception) { return null; }
    }

    public ProcessPrioritySnapshot? CapturePriority(int processId)
    {
        if (!OperatingSystem.IsWindows()) return null;
        try
        {
            return NativeApi.GetProcessPriority((uint)processId, out var priority) == 0
                ? new ProcessPrioritySnapshot(processId, priority)
                : null;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException) { return null; }
    }

    public bool ApplyPriority(int processId, int priorityClass)
    {
        if (!OperatingSystem.IsWindows()) return false;
        try { return NativeApi.SetProcessPriority((uint)processId, priorityClass) == 0; }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException) { return false; }
    }

    public bool Restore(ProcessPrioritySnapshot snapshot) => ApplyPriority(snapshot.ProcessId, snapshot.PriorityClass);
}
