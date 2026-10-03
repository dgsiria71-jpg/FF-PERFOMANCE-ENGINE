using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Telemetry;

internal static class TelemetryFrameAggregatorSelfTests
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
                $"Telemetry frame aggregator self-test failed at stage '{_stage}'.",
                ex);
        }
    }

    private static void RunCore()
    {
        var start = new DateTimeOffset(2026, 9, 8, 21, 30, 0, TimeSpan.Zero);
        var end = start.AddSeconds(1);

        var gauge = Descriptor("test.gauge", TelemetryAggregationKind.Gauge);
        var average = Descriptor("test.average", TelemetryAggregationKind.Average);
        var minimum = Descriptor("test.minimum", TelemetryAggregationKind.Minimum);
        var maximum = Descriptor("test.maximum", TelemetryAggregationKind.Maximum);
        var sum = Descriptor("test.sum", TelemetryAggregationKind.Sum);

        _stage = "aggregation-semantics-and-order-independence";
        var first = Frame(start.AddMilliseconds(100),
            Observation(gauge, 10, TelemetryMetricQuality.Measured, 0.9, "sensor-a"),
            Observation(average, 20, TelemetryMetricQuality.Measured, 1, "sensor-a"),
            Observation(minimum, 8, TelemetryMetricQuality.Measured, 1, "sensor-a"),
            Observation(maximum, 8, TelemetryMetricQuality.Measured, 1, "sensor-a"),
            Observation(sum, 8, TelemetryMetricQuality.Measured, 1, "sensor-a"));
        var second = Frame(start.AddMilliseconds(800),
            Observation(gauge, 30, TelemetryMetricQuality.Measured, 0.4, "sensor-a"),
            Observation(average, 40, TelemetryMetricQuality.Measured, 1, "sensor-a"),
            Observation(minimum, 3, TelemetryMetricQuality.Measured, 1, "sensor-a"),
            Observation(maximum, 12, TelemetryMetricQuality.Measured, 1, "sensor-a"),
            Observation(sum, 4, TelemetryMetricQuality.Measured, 1, "sensor-a"));

        var aggregate = TelemetryFrameAggregator.Aggregate(start, end, [second, first]);
        var aggregateReversed = TelemetryFrameAggregator.Aggregate(start, end, [first, second]);
        Require(aggregate.Timestamp == end && aggregateReversed.Timestamp == end,
            "Aggregate timestamp must equal the exclusive window end.");
        RequireMetric(aggregate, gauge, 20, TelemetryMetricQuality.Measured, 0.4, "sensor-a");
        RequireMetric(aggregate, average, 30, TelemetryMetricQuality.Measured, 1, "sensor-a");
        RequireMetric(aggregate, minimum, 3, TelemetryMetricQuality.Measured, 1, "sensor-a");
        RequireMetric(aggregate, maximum, 12, TelemetryMetricQuality.Measured, 1, "sensor-a");
        RequireMetric(aggregate, sum, 12, TelemetryMetricQuality.Measured, 1, "sensor-a");
        RequireEquivalent(aggregate, aggregateReversed,
            "Aggregation result must be independent of input enumeration order.");

        _stage = "absent-is-not-zero";
        var sparseMetric = Descriptor("test.sparse", TelemetryAggregationKind.Gauge);
        var sparse = TelemetryFrameAggregator.Aggregate(start, end,
        [
            Frame(start.AddMilliseconds(100), Observation(sparseMetric, 25, TelemetryMetricQuality.Measured, 1, "sensor-a")),
            Frame(start.AddMilliseconds(200), Observation(gauge, 50, TelemetryMetricQuality.Measured, 1, "sensor-a"))
        ]);
        RequireMetric(sparse, sparseMetric, 25, TelemetryMetricQuality.Measured, 1, "sensor-a");

        _stage = "weakest-quality-and-minimum-coverage";
        var qualityMetric = Descriptor("test.quality", TelemetryAggregationKind.Gauge);
        var qualityAggregate = TelemetryFrameAggregator.Aggregate(start, end,
        [
            Frame(start.AddMilliseconds(100), Observation(qualityMetric, 10, TelemetryMetricQuality.Measured, 0.9, "sensor-a")),
            Frame(start.AddMilliseconds(200), Observation(qualityMetric, 30, TelemetryMetricQuality.Partial, 0.4, "sensor-a"))
        ]);
        RequireMetric(qualityAggregate, qualityMetric, 20, TelemetryMetricQuality.Partial, 0.4, "sensor-a");

        _stage = "mixed-provenance";
        var mixedMetric = Descriptor("test.mixed", TelemetryAggregationKind.Average);
        var mixed = TelemetryFrameAggregator.Aggregate(start, end,
        [
            Frame(start.AddMilliseconds(100), Observation(mixedMetric, 10, TelemetryMetricQuality.Measured, 0.8, "sensor-a")),
            Frame(start.AddMilliseconds(200), Observation(mixedMetric, 20, TelemetryMetricQuality.Measured, 0.7, "sensor-b"))
        ]);
        RequireMetric(mixed, mixedMetric, 15, TelemetryMetricQuality.Measured, 0.7, "aggregate-mixed");

        _stage = "derived-origin";
        foreach (var metric in aggregate.Metrics.Concat(qualityAggregate.Metrics).Concat(mixed.Metrics))
        {
            Require(metric.Origin == TelemetryMetricOrigin.Derived,
                "Every aggregate metric must declare Derived origin.");
        }

        _stage = "descriptor-conflict";
        var conflictPercent = new TelemetryMetricDescriptor(
            "test.conflict", TelemetryUnit.Percent, TelemetryMetricDomain.System, TelemetryAggregationKind.Gauge);
        var conflictMs = new TelemetryMetricDescriptor(
            "test.conflict", TelemetryUnit.Milliseconds, TelemetryMetricDomain.System, TelemetryAggregationKind.Gauge);
        RequireThrows<InvalidOperationException>(() => TelemetryFrameAggregator.Aggregate(start, end,
        [
            Frame(start.AddMilliseconds(100), Observation(conflictPercent, 1, TelemetryMetricQuality.Measured, 1, "a")),
            Frame(start.AddMilliseconds(200), Observation(conflictMs, 2, TelemetryMetricQuality.Measured, 1, "a"))
        ]), "Same metric id with incompatible units must not be blended.");

        var conflictDomain = new TelemetryMetricDescriptor(
            "test.domain", TelemetryUnit.Percent, TelemetryMetricDomain.System, TelemetryAggregationKind.Gauge);
        var conflictDomainOther = new TelemetryMetricDescriptor(
            "test.domain", TelemetryUnit.Percent, TelemetryMetricDomain.Other, TelemetryAggregationKind.Gauge);
        RequireThrows<InvalidOperationException>(() => TelemetryFrameAggregator.Aggregate(start, end,
        [
            Frame(start.AddMilliseconds(100), Observation(conflictDomain, 1, TelemetryMetricQuality.Measured, 1, "a")),
            Frame(start.AddMilliseconds(200), Observation(conflictDomainOther, 2, TelemetryMetricQuality.Measured, 1, "a"))
        ]), "Same metric id with incompatible domains must not be blended.");

        var conflictAggregation = new TelemetryMetricDescriptor(
            "test.aggregation", TelemetryUnit.Percent, TelemetryMetricDomain.System, TelemetryAggregationKind.Gauge);
        var conflictAggregationOther = new TelemetryMetricDescriptor(
            "test.aggregation", TelemetryUnit.Percent, TelemetryMetricDomain.System, TelemetryAggregationKind.Sum);
        RequireThrows<InvalidOperationException>(() => TelemetryFrameAggregator.Aggregate(start, end,
        [
            Frame(start.AddMilliseconds(100), Observation(conflictAggregation, 1, TelemetryMetricQuality.Measured, 1, "a")),
            Frame(start.AddMilliseconds(200), Observation(conflictAggregationOther, 2, TelemetryMetricQuality.Measured, 1, "a"))
        ]), "Same metric id with incompatible aggregation semantics must not be blended.");

        _stage = "window-filtering";
        var windowMetric = Descriptor("test.window", TelemetryAggregationKind.Gauge);
        var windowed = TelemetryFrameAggregator.Aggregate(start, end,
        [
            Frame(start.AddTicks(-1), Observation(windowMetric, 1000, TelemetryMetricQuality.Measured, 1, "a")),
            Frame(start, Observation(windowMetric, 10, TelemetryMetricQuality.Measured, 1, "a")),
            Frame(end.AddTicks(-1), Observation(windowMetric, 30, TelemetryMetricQuality.Measured, 1, "a")),
            Frame(end, Observation(windowMetric, 2000, TelemetryMetricQuality.Measured, 1, "a"))
        ]);
        RequireMetric(windowed, windowMetric, 20, TelemetryMetricQuality.Measured, 1, "a");

        _stage = "empty-window";
        var empty = TelemetryFrameAggregator.Aggregate(start, end,
        [
            Frame(end, Observation(windowMetric, 123, TelemetryMetricQuality.Measured, 1, "a"))
        ]);
        Require(empty.Timestamp == end && empty.Metrics.Count == 0
                && empty.FrameQuality == TelemetryMetricQuality.Unavailable,
            "An empty aggregate window must return an empty Unavailable frame timestamped at end.");

        _stage = "range-validation";
        RequireThrows<ArgumentOutOfRangeException>(
            () => TelemetryFrameAggregator.Aggregate(start, start, Array.Empty<TelemetryFrame>()),
            "Zero-duration aggregation windows must be rejected.");
        RequireThrows<ArgumentOutOfRangeException>(
            () => TelemetryFrameAggregator.Aggregate(end, start, Array.Empty<TelemetryFrame>()),
            "Reversed aggregation windows must be rejected.");
        RequireThrows<ArgumentNullException>(
            () => TelemetryFrameAggregator.Aggregate(start, end, null!),
            "Null frame enumeration must be rejected.");

        _stage = "one-second-helper";
        var oneSecond = TelemetryFrameAggregator.AggregateOneSecond(start,
        [
            Frame(start.AddMilliseconds(500), Observation(gauge, 42, TelemetryMetricQuality.Measured, 1, "a"))
        ]);
        Require(oneSecond.Timestamp == start.AddSeconds(1),
            "One-second helper must timestamp the result exactly at start + 1 second.");
        RequireMetric(oneSecond, gauge, 42, TelemetryMetricQuality.Measured, 1, "a");

        _stage = "complete";
        Console.WriteLine("PASS Track 4 telemetry v2 aggregation is deterministic, typed and quality/coverage aware");
    }

    private static TelemetryMetricDescriptor Descriptor(string id, TelemetryAggregationKind aggregation)
        => new(id, TelemetryUnit.Percent, TelemetryMetricDomain.Other, aggregation);

    private static TelemetryMetricObservation Observation(
        TelemetryMetricDescriptor descriptor,
        double value,
        TelemetryMetricQuality quality,
        double coverage,
        string sourceId)
        => new(descriptor, value, quality, coverage, sourceId, TelemetryMetricOrigin.Direct);

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
            $"Expected aggregate metric '{descriptor.Id}' is missing.");
        RequireClose(observation!.Value, expectedValue, $"{descriptor.Id} value");
        Require(observation.Quality == expectedQuality,
            $"{descriptor.Id} quality mismatch. Expected {expectedQuality}, got {observation.Quality}.");
        RequireClose(observation.Coverage, expectedCoverage, $"{descriptor.Id} coverage");
        Require(observation.SourceId == expectedSource,
            $"{descriptor.Id} source mismatch. Expected '{expectedSource}', got '{observation.SourceId}'.");
        Require(observation.Origin == TelemetryMetricOrigin.Derived,
            $"{descriptor.Id} aggregate origin must be Derived.");
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
    {
        Require(Math.Abs(actual - expected) <= 0.000001,
            $"{label} mismatch. Expected {expected}, got {actual}.");
    }

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
