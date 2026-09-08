using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Telemetry;

internal static class TelemetryHierarchicalAggregationSelfTests
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
                $"Telemetry hierarchical aggregation self-test failed at stage '{_stage}'.",
                ex);
        }
    }

    private static void RunCore()
    {
        var start = new DateTimeOffset(2026, 9, 8, 22, 0, 0, TimeSpan.Zero);
        var end = start.AddSeconds(10);
        var metric = Descriptor("test.hierarchical", TelemetryAggregationKind.Gauge);

        _stage = "ten-complete-end-stamped-buckets";
        var completeChildren = Enumerable.Range(1, 10)
            .Select(index => Frame(
                start.AddSeconds(index),
                Observation(metric, index, TelemetryMetricQuality.Measured, 1, "sensor-a")))
            .ToArray();

        var complete = TelemetryFrameAggregator.AggregateChildWindows(
            start,
            end,
            TimeSpan.FromSeconds(1),
            completeChildren.Reverse());
        Require(complete.Timestamp == end,
            "Hierarchical aggregate timestamp must equal the parent exclusive end.");
        RequireMetric(complete, metric, 5.5, TelemetryMetricQuality.Measured, 1, "sensor-a");

        var completeForward = TelemetryFrameAggregator.AggregateChildWindows(
            start,
            end,
            TimeSpan.FromSeconds(1),
            completeChildren);
        RequireEquivalent(complete, completeForward,
            "Hierarchical aggregation must be independent of child enumeration order.");

        _stage = "temporal-presence-coverage";
        var nineChildren = Enumerable.Range(1, 9)
            .Select(index => Frame(
                start.AddSeconds(index),
                Observation(metric, index, TelemetryMetricQuality.Measured, 1, "sensor-a")))
            .Append(Frame(end))
            .ToArray();
        var temporalGap = TelemetryFrameAggregator.AggregateChildWindows(
            start,
            end,
            TimeSpan.FromSeconds(1),
            nineChildren);
        RequireMetric(temporalGap, metric, 5, TelemetryMetricQuality.Measured, 0.9, "sensor-a");

        _stage = "producer-and-temporal-coverage-compose";
        var partialCoverageChildren = Enumerable.Range(1, 9)
            .Select(index => Frame(
                start.AddSeconds(index),
                Observation(
                    metric,
                    index,
                    TelemetryMetricQuality.Measured,
                    index == 4 ? 0.5 : 1,
                    "sensor-a")))
            .Append(Frame(end))
            .ToArray();
        var compoundedCoverage = TelemetryFrameAggregator.AggregateChildWindows(
            start,
            end,
            TimeSpan.FromSeconds(1),
            partialCoverageChildren);
        RequireMetric(compoundedCoverage, metric, 5, TelemetryMetricQuality.Measured, 0.45, "sensor-a");

        _stage = "weakest-quality-preserved";
        var qualityChildren = Enumerable.Range(1, 10)
            .Select(index => Frame(
                start.AddSeconds(index),
                Observation(
                    metric,
                    index,
                    index == 7 ? TelemetryMetricQuality.Partial : TelemetryMetricQuality.Measured,
                    1,
                    "sensor-a")))
            .ToArray();
        var weakestQuality = TelemetryFrameAggregator.AggregateChildWindows(
            start,
            end,
            TimeSpan.FromSeconds(1),
            qualityChildren);
        RequireMetric(weakestQuality, metric, 5.5, TelemetryMetricQuality.Partial, 1, "sensor-a");

        _stage = "endpoint-semantics";
        var endpointMetric = Descriptor("test.endpoint", TelemetryAggregationKind.Sum);
        var endpoint = TelemetryFrameAggregator.AggregateChildWindows(
            start,
            end,
            TimeSpan.FromSeconds(1),
            [
                Frame(start, Observation(endpointMetric, 1000, TelemetryMetricQuality.Measured, 1, "sensor-a")),
                Frame(start.AddSeconds(1), Observation(endpointMetric, 2, TelemetryMetricQuality.Measured, 1, "sensor-a")),
                Frame(end, Observation(endpointMetric, 3, TelemetryMetricQuality.Measured, 1, "sensor-a")),
                Frame(end.AddSeconds(1), Observation(endpointMetric, 2000, TelemetryMetricQuality.Measured, 1, "sensor-a"))
            ]);
        RequireMetric(endpoint, endpointMetric, 5, TelemetryMetricQuality.Measured, 0.2, "sensor-a");

        _stage = "duplicate-child-window-rejected";
        RequireThrows<InvalidOperationException>(() =>
            TelemetryFrameAggregator.AggregateChildWindows(
                start,
                end,
                TimeSpan.FromSeconds(1),
                [
                    Frame(start.AddSeconds(1), Observation(metric, 1, TelemetryMetricQuality.Measured, 1, "a")),
                    Frame(start.AddSeconds(1), Observation(metric, 2, TelemetryMetricQuality.Measured, 1, "a"))
                ]),
            "Duplicate child-window end timestamps must be rejected to prevent double weighting.");

        _stage = "off-grid-child-rejected";
        RequireThrows<InvalidOperationException>(() =>
            TelemetryFrameAggregator.AggregateChildWindows(
                start,
                end,
                TimeSpan.FromSeconds(1),
                [Frame(start.AddMilliseconds(1500), Observation(metric, 1, TelemetryMetricQuality.Measured, 1, "a"))]),
            "Eligible child aggregate timestamps must align exactly to the declared child window.");

        _stage = "descriptor-conflict-still-rejected";
        var percentMetric = new TelemetryMetricDescriptor(
            "test.conflict.hierarchical",
            TelemetryUnit.Percent,
            TelemetryMetricDomain.Other,
            TelemetryAggregationKind.Gauge);
        var millisecondMetric = new TelemetryMetricDescriptor(
            "test.conflict.hierarchical",
            TelemetryUnit.Milliseconds,
            TelemetryMetricDomain.Other,
            TelemetryAggregationKind.Gauge);
        RequireThrows<InvalidOperationException>(() =>
            TelemetryFrameAggregator.AggregateChildWindows(
                start,
                end,
                TimeSpan.FromSeconds(1),
                [
                    Frame(start.AddSeconds(1), Observation(percentMetric, 1, TelemetryMetricQuality.Measured, 1, "a")),
                    Frame(start.AddSeconds(2), Observation(millisecondMetric, 2, TelemetryMetricQuality.Measured, 1, "a"))
                ]),
            "Hierarchical aggregation must preserve descriptor compatibility enforcement.");

        _stage = "empty-parent";
        var empty = TelemetryFrameAggregator.AggregateChildWindows(
            start,
            end,
            TimeSpan.FromSeconds(1),
            Array.Empty<TelemetryFrame>());
        Require(empty.Timestamp == end
                && empty.Metrics.Count == 0
                && empty.FrameQuality == TelemetryMetricQuality.Unavailable,
            "No child observations must produce an empty Unavailable parent frame.");

        _stage = "range-and-child-window-validation";
        RequireThrows<ArgumentOutOfRangeException>(() =>
            TelemetryFrameAggregator.AggregateChildWindows(
                start,
                end,
                TimeSpan.Zero,
                Array.Empty<TelemetryFrame>()),
            "Zero child duration must be rejected.");
        RequireThrows<ArgumentOutOfRangeException>(() =>
            TelemetryFrameAggregator.AggregateChildWindows(
                start,
                start,
                TimeSpan.FromSeconds(1),
                Array.Empty<TelemetryFrame>()),
            "Zero parent duration must be rejected.");
        RequireThrows<ArgumentException>(() =>
            TelemetryFrameAggregator.AggregateChildWindows(
                start,
                start.AddSeconds(10),
                TimeSpan.FromSeconds(3),
                Array.Empty<TelemetryFrame>()),
            "Parent duration must be an exact multiple of child duration.");
        RequireThrows<ArgumentNullException>(() =>
            TelemetryFrameAggregator.AggregateChildWindows(
                start,
                end,
                TimeSpan.FromSeconds(1),
                null!),
            "Null child enumeration must be rejected.");

        _stage = "complete";
        Console.WriteLine("PASS Track 4 hierarchical telemetry aggregation preserves end-stamped buckets and temporal coverage");
    }

    private static TelemetryMetricDescriptor Descriptor(string id, TelemetryAggregationKind aggregation)
        => new(id, TelemetryUnit.Percent, TelemetryMetricDomain.Other, aggregation);

    private static TelemetryMetricObservation Observation(
        TelemetryMetricDescriptor descriptor,
        double value,
        TelemetryMetricQuality quality,
        double coverage,
        string sourceId)
        => new(descriptor, value, quality, coverage, sourceId, TelemetryMetricOrigin.Derived);

    private static TelemetryFrame Frame(
        DateTimeOffset timestamp,
        params TelemetryMetricObservation[] metrics)
        => new(timestamp, metrics);

    private static void RequireMetric(
        TelemetryFrame frame,
        TelemetryMetricDescriptor descriptor,
        double expectedValue,
        TelemetryMetricQuality expectedQuality,
        double expectedCoverage,
        string expectedSource)
    {
        Require(frame.TryGetMetric(descriptor.Id, out var observation) && observation is not null,
            $"Expected hierarchical metric '{descriptor.Id}' is missing.");
        RequireClose(observation!.Value, expectedValue, $"{descriptor.Id} value");
        Require(observation.Quality == expectedQuality,
            $"{descriptor.Id} quality mismatch. Expected {expectedQuality}, got {observation.Quality}.");
        RequireClose(observation.Coverage, expectedCoverage, $"{descriptor.Id} coverage");
        Require(observation.SourceId == expectedSource,
            $"{descriptor.Id} source mismatch. Expected '{expectedSource}', got '{observation.SourceId}'.");
        Require(observation.Origin == TelemetryMetricOrigin.Derived,
            $"{descriptor.Id} hierarchical origin must remain Derived.");
    }

    private static void RequireEquivalent(TelemetryFrame left, TelemetryFrame right, string message)
    {
        Require(left.Timestamp == right.Timestamp && left.Metrics.Count == right.Metrics.Count, message);
        for (var i = 0; i < left.Metrics.Count; i++)
        {
            var a = left.Metrics[i];
            var b = right.Metrics[i];
            Require(a.Metric.Id == b.Metric.Id
                    && a.Metric.Unit == b.Metric.Unit
                    && a.Metric.Domain == b.Metric.Domain
                    && a.Metric.Aggregation == b.Metric.Aggregation
                    && Math.Abs(a.Value - b.Value) <= 0.000001
                    && a.Quality == b.Quality
                    && Math.Abs(a.Coverage - b.Coverage) <= 0.000001
                    && a.SourceId == b.SourceId
                    && a.Origin == b.Origin,
                message);
        }
    }

    private static void RequireClose(double actual, double expected, string label)
        => Require(Math.Abs(actual - expected) <= 0.000001,
            $"{label} mismatch. Expected {expected}, got {actual}.");

    private static void RequireThrows<TException>(Action action, string message)
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

        throw new InvalidOperationException(message);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
