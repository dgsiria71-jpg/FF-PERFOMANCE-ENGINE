using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Telemetry;

internal static class TelemetryMetricSchemaV2SelfTests
{
    private static string _stage = "not-started";

    [ModuleInitializer]
    internal static void Run()
    {
        try
        {
            RunCore();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Telemetry metric schema v2 self-test failed at stage '{_stage}'.",
                ex);
        }
    }

    private static void RunCore()
    {
        _stage = "standard-metrics";
        Require(TelemetryStandardMetrics.FrameFpsAverage.Id == "frame.fps.avg",
            "FPS average metric id must be canonical and stable.");
        Require(TelemetryStandardMetrics.SystemCpuUtilizationPercent.Id == "system.cpu.utilization.percent",
            "CPU utilization metric id must be canonical and stable.");
        Require(TelemetryStandardMetrics.All.Count == 17,
            "The first v2 standard catalog must expose the 17 approved metrics exactly.");
        Require(TelemetryStandardMetrics.All
                    .Select(item => item.Id)
                    .Distinct(StringComparer.Ordinal)
                    .Count() == TelemetryStandardMetrics.All.Count,
            "Standard metric ids must be unique.");

        _stage = "descriptor-validation";
        Expect<ArgumentException>(() => new TelemetryMetricDescriptor(
            "Frame FPS",
            TelemetryUnit.FramesPerSecond,
            TelemetryMetricDomain.Frame,
            TelemetryAggregationKind.Gauge));
        var normalized = new TelemetryMetricDescriptor(
            " custom.metric_1 ",
            TelemetryUnit.Count,
            TelemetryMetricDomain.Other,
            TelemetryAggregationKind.Gauge);
        Require(normalized.Id == "custom.metric_1",
            "Metric ids must be trimmed and normalized without changing valid namespace structure.");

        _stage = "observation-validation";
        Expect<ArgumentOutOfRangeException>(() => Observation(
            TelemetryStandardMetrics.FrameFpsAverage,
            double.NaN,
            TelemetryMetricQuality.Measured,
            1,
            "presentmon"));
        Expect<ArgumentOutOfRangeException>(() => Observation(
            TelemetryStandardMetrics.FrameFpsAverage,
            120,
            TelemetryMetricQuality.Measured,
            1.01,
            "presentmon"));
        Expect<ArgumentException>(() => Observation(
            TelemetryStandardMetrics.FrameFpsAverage,
            120,
            TelemetryMetricQuality.Unavailable,
            1,
            "presentmon"));
        Expect<ArgumentException>(() => Observation(
            TelemetryStandardMetrics.FrameFpsAverage,
            120,
            TelemetryMetricQuality.Measured,
            1,
            "Present Mon!"));

        var measured = Observation(
            TelemetryStandardMetrics.FrameFpsAverage,
            120,
            TelemetryMetricQuality.Measured,
            1,
            " PresentMon ");
        Require(measured.SourceId == "presentmon",
            "Source ids must be normalized to lowercase trimmed provenance.");

        var partial = Observation(
            TelemetryStandardMetrics.FrameTimeAverageMs,
            8.3,
            TelemetryMetricQuality.Partial,
            0.75,
            "legacy-bridge");

        _stage = "frame-quality";
        var now = new DateTimeOffset(2026, 9, 8, 20, 0, 0, TimeSpan.Zero);
        Require(new TelemetryFrame(now, Array.Empty<TelemetryMetricObservation>()).FrameQuality
                == TelemetryMetricQuality.Unavailable,
            "An empty frame must summarize as Unavailable.");
        Require(new TelemetryFrame(now, [measured]).FrameQuality
                == TelemetryMetricQuality.Measured,
            "A frame containing only measured metrics must summarize as Measured.");
        var mixed = new TelemetryFrame(now, [measured, partial]);
        Require(mixed.FrameQuality == TelemetryMetricQuality.Partial,
            "Any partial metric must keep the frame summary Partial.");
        Expect<ArgumentException>(() => new TelemetryFrame(now, [measured, measured]));

        _stage = "frame-lookup-and-copy";
        var input = new List<TelemetryMetricObservation> { partial, measured };
        var frame = new TelemetryFrame(now, input);
        input.Clear();
        Require(frame.Metrics.Count == 2,
            "TelemetryFrame must copy input metrics rather than retaining a mutable caller list.");
        Require(frame.Metrics.Select(item => item.Metric.Id).SequenceEqual(
            frame.Metrics.Select(item => item.Metric.Id).OrderBy(id => id, StringComparer.Ordinal)),
            "Frame metrics must be deterministic by canonical metric id.");
        Require(frame.TryGetMetric(" FRAME.FPS.AVG ", out var found)
                && ReferenceEquals(found, measured),
            "Metric lookup must normalize the requested id and return the stored observation.");
        Require(!frame.TryGetMetric("system.gpu.utilization.percent", out var absent)
                && absent is null,
            "Unavailable metrics must remain absent rather than becoming synthetic zero observations.");

        _stage = "complete";
        Console.WriteLine("PASS Track 4 telemetry metric schema v2 is typed, deterministic and non-fabricating");
    }

    private static TelemetryMetricObservation Observation(
        TelemetryMetricDescriptor descriptor,
        double value,
        TelemetryMetricQuality quality,
        double coverage,
        string sourceId)
        => new(
            descriptor,
            value,
            quality,
            coverage,
            sourceId,
            TelemetryMetricOrigin.Direct);

    private static void Expect<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name} was not thrown.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
