using System.Diagnostics;
using System.Globalization;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Telemetry;

namespace FFPerformanceEngine.Core.Services;

public sealed class PresentMonService
{
    public string? FindExecutable()
    {
        var local = Path.Combine(AppPaths.Root, "tools", "PresentMon-2.5.1-x64.exe");
        if (File.Exists(local)) return local;
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(directory.Trim(), "PresentMon.exe");
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }

    public int? FindBlueStacksPlayerPid()
    {
        try
        {
            return Process.GetProcessesByName("HD-Player")
                .Where(p => !p.HasExited)
                .OrderByDescending(p => p.WorkingSet64)
                .Select(p => (int?)p.Id)
                .FirstOrDefault();
        }
        catch (InvalidOperationException) { return null; }
    }

    public async Task<TelemetrySample?> CaptureAsync(TimeSpan duration, CancellationToken cancellationToken = default)
    {
        var pid = FindBlueStacksPlayerPid();
        return pid is null ? null : await CaptureProcessAsync(pid.Value, duration, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TelemetrySample?> CaptureProcessAsync(int processId, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        var csv = await CaptureProcessCsvAsync(processId, duration, cancellationToken).ConfigureAwait(false);
        return csv is null ? null : ParseCsv(csv);
    }

    public async Task<TelemetryFrame?> CaptureProcessFrameAsync(
        int processId,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        var csv = await CaptureProcessCsvAsync(processId, duration, cancellationToken).ConfigureAwait(false);
        return csv is null ? null : ParseCsvFrame(csv);
    }

    public static IReadOnlyList<string> BuildCaptureArguments(int processId, TimeSpan duration, string outputPath)
    {
        if (processId <= 0) throw new ArgumentOutOfRangeException(nameof(processId));
        if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentException("Output path is required.", nameof(outputPath));
        var seconds = Math.Clamp((int)Math.Ceiling(duration.TotalSeconds), 2, 300);
        return [
            "--process_id", processId.ToString(CultureInfo.InvariantCulture),
            "--timed", seconds.ToString(CultureInfo.InvariantCulture),
            "--terminate_after_timed",
            "--output_file", outputPath,
            "--no_console_stats",
            "--exclude_dropped"
        ];
    }

    public TelemetrySample? ParseCsv(string csv)
    {
        var statistics = ParseCsvStatistics(csv);
        return statistics is null
            ? null
            : new TelemetrySample
            {
                Fps = statistics.FpsAverage,
                OnePercentLow = statistics.FpsLow1,
                PointOnePercentLow = statistics.FpsLow01,
                FrameTimeMs = statistics.FrameTimeAverageMs,
                FrameTimeP95Ms = statistics.FrameTimeP95Ms,
                FrameTimeP99Ms = statistics.FrameTimeP99Ms,
                StutterPercent = statistics.StutterPercent,
                LatencyMs = statistics.LatencyAverageMs,
                DataQuality = $"PresentMon · {statistics.AcceptedFrameRows} frames"
            };
    }

    public TelemetryFrame? ParseCsvFrame(string csv)
    {
        var statistics = ParseCsvStatistics(csv);
        if (statistics is null) return null;

        var frameCoverage = (double)statistics.AcceptedFrameRows / statistics.DataRowCount;
        var metrics = new List<TelemetryMetricObservation>(9)
        {
            Direct(TelemetryStandardMetrics.FrameFpsAverage, statistics.FpsAverage, frameCoverage),
            Direct(TelemetryStandardMetrics.FrameFpsLow1, statistics.FpsLow1, frameCoverage),
            Direct(TelemetryStandardMetrics.FrameFpsLow01, statistics.FpsLow01, frameCoverage),
            Direct(TelemetryStandardMetrics.FrameTimeAverageMs, statistics.FrameTimeAverageMs, frameCoverage),
            Direct(TelemetryStandardMetrics.FrameTimeP95Ms, statistics.FrameTimeP95Ms, frameCoverage),
            Direct(TelemetryStandardMetrics.FrameTimeP99Ms, statistics.FrameTimeP99Ms, frameCoverage),
            Direct(TelemetryStandardMetrics.FrameStutterPercent, statistics.StutterPercent, frameCoverage),
            Direct(TelemetryStandardMetrics.FrameAcceptedSampleCount, statistics.AcceptedFrameRows, 1d)
        };

        if (statistics.LatencyAverageMs is double latency)
        {
            var latencyCoverage = (double)statistics.AcceptedLatencyRows / statistics.DataRowCount;
            metrics.Add(Direct(TelemetryStandardMetrics.FrameLatencyAverageMs, latency, latencyCoverage));
        }

        return new TelemetryFrame(DateTimeOffset.UtcNow, metrics);
    }

    private async Task<string?> CaptureProcessCsvAsync(
        int processId,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        if (processId <= 0) throw new ArgumentOutOfRangeException(nameof(processId));
        var executable = FindExecutable();
        if (executable is null) return null;

        var captureDirectory = Path.Combine(AppPaths.Root, "captures");
        Directory.CreateDirectory(captureDirectory);
        CleanupCaptureDirectory(captureDirectory);
        var output = Path.Combine(captureDirectory, $"presentmon-{processId}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmssfff}.csv");
        var arguments = BuildCaptureArguments(processId, duration, output);
        var start = new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);

        using var process = Process.Start(start);
        if (process is null) return null;
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        if (process.ExitCode != 0 || !File.Exists(output)) return null;
        return File.ReadAllText(output);
    }

    private static PresentMonStatistics? ParseCsvStatistics(string csv)
    {
        var lines = csv.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) return null;
        var headers = SplitCsvLine(lines[0]);
        var intervalIndex = FindColumn(headers, "MsBetweenPresents", "DisplayedTime", "MsBetweenDisplayChange");
        var latencyIndex = FindColumn(headers, "DisplayLatency", "MsPCLatency", "MsRenderPresentLatency");
        if (intervalIndex < 0) return null;

        var frameTimes = new List<double>();
        var latencies = new List<double>();
        for (var i = 1; i < lines.Length; i++)
        {
            var columns = SplitCsvLine(lines[i]);
            if (intervalIndex >= columns.Count) continue;
            if (TryMetric(columns[intervalIndex], out var interval) && interval > 0.1 && interval < 1000) frameTimes.Add(interval);
            if (latencyIndex >= 0 && latencyIndex < columns.Count && TryMetric(columns[latencyIndex], out var latency) && latency >= 0 && latency < 1000) latencies.Add(latency);
        }
        if (frameTimes.Count < 2) return null;

        var fpsSamples = frameTimes.Select(x => 1000d / x).Where(x => x > 0 && x < 2000).OrderBy(x => x).ToArray();
        var sortedFrameTimes = frameTimes.OrderBy(x => x).ToArray();
        if (fpsSamples.Length < 2) return null;
        var medianFrameTime = Percentile(sortedFrameTimes, 0.50);
        var stutterThreshold = Math.Max(medianFrameTime * 1.5, medianFrameTime + 4.0);
        var stutterPercent = 100d * frameTimes.Count(x => x >= stutterThreshold) / frameTimes.Count;

        return new PresentMonStatistics(
            DataRowCount: lines.Length - 1,
            AcceptedFrameRows: frameTimes.Count,
            AcceptedLatencyRows: latencies.Count,
            FpsAverage: fpsSamples.Average(),
            FpsLow1: LowAverage(fpsSamples, 0.01),
            FpsLow01: LowAverage(fpsSamples, 0.001),
            FrameTimeAverageMs: frameTimes.Average(),
            FrameTimeP95Ms: Percentile(sortedFrameTimes, 0.95),
            FrameTimeP99Ms: Percentile(sortedFrameTimes, 0.99),
            StutterPercent: stutterPercent,
            LatencyAverageMs: latencies.Count > 0 ? latencies.Average() : null);
    }

    private static TelemetryMetricObservation Direct(
        TelemetryMetricDescriptor descriptor,
        double value,
        double coverage)
        => new(
            descriptor,
            value,
            TelemetryMetricQuality.Measured,
            coverage,
            "presentmon",
            TelemetryMetricOrigin.Direct);

    private static int FindColumn(IReadOnlyList<string> headers, params string[] names)
    {
        foreach (var name in names)
        {
            for (var i = 0; i < headers.Count; i++)
                if (string.Equals(headers[i].Trim(), name, StringComparison.OrdinalIgnoreCase)) return i;
        }
        return -1;
    }

    private static bool TryMetric(string raw, out double value)
        => double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

    private static double LowAverage(double[] sortedAscending, double fraction)
    {
        var count = Math.Clamp((int)Math.Ceiling(sortedAscending.Length * fraction), 1, sortedAscending.Length);
        return sortedAscending.Take(count).Average();
    }

    private static double Percentile(double[] sortedAscending, double p)
    {
        var position = Math.Clamp((sortedAscending.Length - 1) * p, 0, sortedAscending.Length - 1);
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        if (lower == upper) return sortedAscending[lower];
        var weight = position - lower;
        return sortedAscending[lower] * (1 - weight) + sortedAscending[upper] * weight;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                else quoted = !quoted;
            }
            else if (c == ',' && !quoted) { result.Add(current.ToString()); current.Clear(); }
            else current.Append(c);
        }
        result.Add(current.ToString());
        return result;
    }

    private static void CleanupCaptureDirectory(string directory)
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-7);
            foreach (var file in Directory.EnumerateFiles(directory, "presentmon-*.csv").Select(x => new FileInfo(x)).OrderByDescending(x => x.CreationTimeUtc).Skip(200))
                file.Delete();
            foreach (var file in Directory.EnumerateFiles(directory, "presentmon-*.csv").Select(x => new FileInfo(x)).Where(x => x.CreationTimeUtc < cutoff))
                file.Delete();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    private sealed record PresentMonStatistics(
        int DataRowCount,
        int AcceptedFrameRows,
        int AcceptedLatencyRows,
        double FpsAverage,
        double FpsLow1,
        double FpsLow01,
        double FrameTimeAverageMs,
        double FrameTimeP95Ms,
        double FrameTimeP99Ms,
        double StutterPercent,
        double? LatencyAverageMs);
}
