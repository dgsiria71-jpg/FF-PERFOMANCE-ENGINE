using System.Runtime.InteropServices;
using FFPerformanceEngine.Core.Telemetry;

namespace FFPerformanceEngine.App;

public sealed class WindowsWddmGpuUtilizationProvider : IWddmGpuUtilizationProvider
{
    private const string EnglishWildcardPath = @"\GPU Engine(*)\Utilization Percentage";
    private const uint ErrorSuccess = 0;
    private const uint PdhMoreData = 0x800007D2;
    private const uint PdhFmtDouble = 0x00000200;
    private const uint PdhCstatusValidData = 0x00000000;
    private const uint PdhCstatusNewData = 0x00000001;
    private const uint MaxCounterInfoBytes = 1 * 1024 * 1024;
    private const uint MaxExpandedPathChars = 4 * 1024 * 1024;
    private const int MaxExpandedPathCount = 16_384;
    private const uint MaxParsedPathBytes = 64 * 1024;
    private static readonly TimeSpan SampleInterval = TimeSpan.FromMilliseconds(100);

    public WddmGpuUtilizationSnapshot? Capture()
    {
        if (!OperatingSystem.IsWindows()) return null;

        try
        {
            var localizedWildcardPath = LocalizeWildcardPath();
            if (localizedWildcardPath is null) return null;

            var expandedPaths = ExpandWildcardPath(localizedWildcardPath);
            if (expandedPaths is null) return null;
            if (expandedPaths.Count == 0)
            {
                return new WddmGpuUtilizationSnapshot(
                    DateTimeOffset.UtcNow,
                    0,
                    Array.Empty<WddmGpuEngineObservation>());
            }

            return CaptureExpandedCounters(expandedPaths);
        }
        catch (Exception ex) when (ex is DllNotFoundException
                                   or EntryPointNotFoundException
                                   or ExternalException
                                   or OverflowException
                                   or OutOfMemoryException)
        {
            return null;
        }
    }

    private static string? LocalizeWildcardPath()
    {
        IntPtr query = IntPtr.Zero;
        try
        {
            if (PdhOpenQueryW(null, UIntPtr.Zero, out query) != ErrorSuccess || query == IntPtr.Zero)
                return null;

            if (PdhAddEnglishCounterW(
                    query,
                    EnglishWildcardPath,
                    UIntPtr.Zero,
                    out var counter) != ErrorSuccess
                || counter == IntPtr.Zero)
            {
                return null;
            }

            uint requiredBytes = 0;
            var status = PdhGetCounterInfoW(counter, false, ref requiredBytes, IntPtr.Zero);
            if (status != PdhMoreData
                || requiredBytes == 0
                || requiredBytes > MaxCounterInfoBytes)
            {
                return null;
            }

            var buffer = Marshal.AllocHGlobal(checked((int)requiredBytes));
            try
            {
                status = PdhGetCounterInfoW(counter, false, ref requiredBytes, buffer);
                if (status != ErrorSuccess) return null;

                var info = Marshal.PtrToStructure<PdhCounterInfoPrefix>(buffer);
                return info.FullPath == IntPtr.Zero
                    ? null
                    : Marshal.PtrToStringUni(info.FullPath);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        finally
        {
            if (query != IntPtr.Zero) _ = PdhCloseQuery(query);
        }
    }

    private static IReadOnlyList<string>? ExpandWildcardPath(string localizedWildcardPath)
    {
        uint requiredChars = 0;
        var status = PdhExpandWildCardPathW(
            null,
            localizedWildcardPath,
            IntPtr.Zero,
            ref requiredChars,
            0);
        if (status != PdhMoreData
            || requiredChars == 0
            || requiredChars > MaxExpandedPathChars)
        {
            return status == ErrorSuccess && requiredChars == 0
                ? Array.Empty<string>()
                : null;
        }

        var bytes = checked((int)requiredChars * sizeof(char));
        var buffer = Marshal.AllocHGlobal(bytes);
        try
        {
            status = PdhExpandWildCardPathW(
                null,
                localizedWildcardPath,
                buffer,
                ref requiredChars,
                0);
            if (status != ErrorSuccess) return null;

            var paths = ReadMultiSz(buffer, checked((int)requiredChars));
            return paths.Count <= MaxExpandedPathCount ? paths : null;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static WddmGpuUtilizationSnapshot? CaptureExpandedCounters(
        IReadOnlyList<string> expandedPaths)
    {
        IntPtr query = IntPtr.Zero;
        try
        {
            if (PdhOpenQueryW(null, UIntPtr.Zero, out query) != ErrorSuccess || query == IntPtr.Zero)
                return null;

            var counters = new List<CounterBinding>(expandedPaths.Count);
            foreach (var path in expandedPaths)
            {
                if (string.IsNullOrWhiteSpace(path)) continue;
                if (PdhAddCounterW(query, path, UIntPtr.Zero, out var counter) != ErrorSuccess
                    || counter == IntPtr.Zero)
                {
                    continue;
                }

                var parsed = ParseCounterPath(path);
                if (parsed is null) continue;
                if (!WddmGpuPdhInstanceParser.TryParsePhysicalEngineKey(
                        parsed.Value.InstanceName,
                        out var engineKey)
                    || engineKey is null)
                {
                    continue;
                }

                var instanceKey = string.Create(
                    System.Globalization.CultureInfo.InvariantCulture,
                    $"{parsed.Value.InstanceName.Trim().ToLowerInvariant()}#{parsed.Value.InstanceIndex}");
                counters.Add(new CounterBinding(counter, engineKey, instanceKey));
            }

            if (PdhCollectQueryData(query) != ErrorSuccess) return null;
            Thread.Sleep(SampleInterval);
            if (PdhCollectQueryData(query) != ErrorSuccess) return null;

            var observations = new List<WddmGpuEngineObservation>(counters.Count);
            foreach (var binding in counters)
            {
                if (PdhGetFormattedCounterValue(
                        binding.Counter,
                        PdhFmtDouble,
                        IntPtr.Zero,
                        out var formatted) != ErrorSuccess)
                {
                    continue;
                }

                if (formatted.CStatus != PdhCstatusValidData
                    && formatted.CStatus != PdhCstatusNewData)
                {
                    continue;
                }

                if (!double.IsFinite(formatted.DoubleValue)) continue;

                observations.Add(new WddmGpuEngineObservation(
                    binding.EngineKey,
                    binding.InstanceKey,
                    formatted.DoubleValue));
            }

            return new WddmGpuUtilizationSnapshot(
                DateTimeOffset.UtcNow,
                expandedPaths.Count,
                observations);
        }
        finally
        {
            if (query != IntPtr.Zero) _ = PdhCloseQuery(query);
        }
    }

    private static ParsedPath? ParseCounterPath(string path)
    {
        uint requiredBytes = 0;
        var status = PdhParseCounterPathW(path, IntPtr.Zero, ref requiredBytes, 0);
        if (status != PdhMoreData
            || requiredBytes == 0
            || requiredBytes > MaxParsedPathBytes)
        {
            return null;
        }

        var buffer = Marshal.AllocHGlobal(checked((int)requiredBytes));
        try
        {
            status = PdhParseCounterPathW(path, buffer, ref requiredBytes, 0);
            if (status != ErrorSuccess) return null;

            var elements = Marshal.PtrToStructure<PdhCounterPathElements>(buffer);
            if (elements.InstanceName == IntPtr.Zero) return null;

            var instanceName = Marshal.PtrToStringUni(elements.InstanceName);
            return string.IsNullOrWhiteSpace(instanceName)
                ? null
                : new ParsedPath(instanceName, elements.InstanceIndex);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static IReadOnlyList<string> ReadMultiSz(IntPtr buffer, int charCapacity)
    {
        var result = new List<string>();
        var offsetChars = 0;
        while (offsetChars < charCapacity)
        {
            var start = IntPtr.Add(buffer, checked(offsetChars * sizeof(char)));
            var value = Marshal.PtrToStringUni(start);
            if (string.IsNullOrEmpty(value)) break;

            result.Add(value);
            offsetChars = checked(offsetChars + value.Length + 1);
            if (result.Count > MaxExpandedPathCount) break;
        }

        return result;
    }

    private readonly record struct CounterBinding(
        IntPtr Counter,
        string EngineKey,
        string InstanceKey);

    private readonly record struct ParsedPath(string InstanceName, uint InstanceIndex);

    [StructLayout(LayoutKind.Sequential)]
    private struct PdhCounterInfoPrefix
    {
        public uint Length;
        public uint Type;
        public uint Version;
        public uint Status;
        public int Scale;
        public int DefaultScale;
        public UIntPtr UserData;
        public UIntPtr QueryUserData;
        public IntPtr FullPath;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PdhCounterPathElements
    {
        public IntPtr MachineName;
        public IntPtr ObjectName;
        public IntPtr InstanceName;
        public IntPtr ParentInstance;
        public uint InstanceIndex;
        public IntPtr CounterName;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct PdhFmtCounterValue
    {
        [FieldOffset(0)]
        public uint CStatus;

        [FieldOffset(8)]
        public double DoubleValue;
    }

    [DllImport("pdh.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern uint PdhOpenQueryW(
        string? dataSource,
        UIntPtr userData,
        out IntPtr query);

    [DllImport("pdh.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern uint PdhAddEnglishCounterW(
        IntPtr query,
        string fullCounterPath,
        UIntPtr userData,
        out IntPtr counter);

    [DllImport("pdh.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern uint PdhAddCounterW(
        IntPtr query,
        string fullCounterPath,
        UIntPtr userData,
        out IntPtr counter);

    [DllImport("pdh.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern uint PdhGetCounterInfoW(
        IntPtr counter,
        [MarshalAs(UnmanagedType.Bool)] bool retrieveExplainText,
        ref uint bufferSize,
        IntPtr buffer);

    [DllImport("pdh.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern uint PdhExpandWildCardPathW(
        string? dataSource,
        string wildcardPath,
        IntPtr expandedPathList,
        ref uint pathListLength,
        uint flags);

    [DllImport("pdh.dll", CharSet = CharSet.Unicode, SetLastError = false)]
    private static extern uint PdhParseCounterPathW(
        string fullPath,
        IntPtr counterPathElements,
        ref uint bufferSize,
        uint flags);

    [DllImport("pdh.dll", SetLastError = false)]
    private static extern uint PdhCollectQueryData(IntPtr query);

    [DllImport("pdh.dll", SetLastError = false)]
    private static extern uint PdhGetFormattedCounterValue(
        IntPtr counter,
        uint format,
        IntPtr counterType,
        out PdhFmtCounterValue value);

    [DllImport("pdh.dll", SetLastError = false)]
    private static extern uint PdhCloseQuery(IntPtr query);
}
