using System.Collections;
using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Telemetry;

internal static class TelemetryRealtimePipelineSelfTests
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
                $"Telemetry realtime pipeline self-test failed at stage '{_stage}'.",
                ex);
        }
    }

    private static void RunCore()
    {
        var start = new DateTimeOffset(2026, 9, 8, 22, 30, 0, TimeSpan.Zero);
        var metric = new TelemetryMetricDescriptor(
            "test.pipeline",
            TelemetryUnit.Percent,
            TelemetryMetricDomain.Other,
            TelemetryAggregationKind.Gauge);

        _stage = "capacity-validation";
        RequireThrows<ArgumentOutOfRangeException>(
            () => _ = new TelemetryRealtimePipeline(0, 1, 1),
            "Raw capacity must be positive.");
        RequireThrows<ArgumentOutOfRangeException>(
            () => _ = new TelemetryRealtimePipeline(1, 0, 1),
            "One-second aggregate capacity must be positive.");
        RequireThrows<ArgumentOutOfRangeException>(
            () => _ = new TelemetryRealtimePipeline(1, 1, 0),
            "Session ten-second capacity must be positive.");

        _stage = "raw-ingress-and-one-second-finalization";
        var pipeline = new TelemetryRealtimePipeline(32, 32, 16);
        pipeline.AppendRaw(Frame(
            start.AddMilliseconds(100),
            Observation(metric, 10, 1)));
        pipeline.AppendRaw(Frame(
            start.AddMilliseconds(800),
            Observation(metric, 30, 0.8)));
        Require(pipeline.RawFrames.Count == 2,
            "Raw ingress must append exact frames to the bounded raw store.");

        var oneSecond = pipeline.FinalizeOneSecond(start);
        Require(oneSecond.Timestamp == start.AddSeconds(1),
            "One-second finalization must preserve the exact bucket end timestamp.");
        RequireMetric(oneSecond, metric, 20, 0.8);
        Require(pipeline.OneSecondAggregates.Count == 1,
            "Finalized one-second bucket must be stored exactly once.");

        _stage = "late-data-and-exact-end";
        RequireThrows<InvalidOperationException>(
            () => pipeline.AppendRaw(Frame(start.AddMilliseconds(999), Observation(metric, 99, 1))),
            "Raw data older than an already-finalized one-second end must be rejected.");
        pipeline.AppendRaw(Frame(start.AddSeconds(1), Observation(metric, 40, 1)));
        Require(pipeline.RawFrames.Count == 3,
            "A frame exactly at the finalized end belongs to the next half-open window and must remain valid.");

        _stage = "one-second-exact-once-and-gap";
        RequireThrows<InvalidOperationException>(
            () => pipeline.FinalizeOneSecond(start),
            "Repeated one-second finalization must not double-weight evidence.");
        RequireThrows<InvalidOperationException>(
            () => pipeline.FinalizeOneSecond(start.AddMilliseconds(500)),
            "Overlapping one-second finalization must be rejected.");
        var secondBucket = pipeline.FinalizeOneSecond(start.AddSeconds(1));
        RequireMetric(secondBucket, metric, 40, 1);
        var gapBucket = pipeline.FinalizeOneSecond(start.AddSeconds(3));
        Require(gapBucket.Metrics.Count == 0
                && gapBucket.FrameQuality == TelemetryMetricQuality.Unavailable,
            "An explicitly finalized empty/gapped bucket must remain empty rather than inventing zeros.");
        RequireThrows<InvalidOperationException>(
            () => pipeline.FinalizeOneSecond(start.AddSeconds(2)),
            "A skipped older bucket cannot be finalized after the cursor has advanced past it.");

        _stage = "ten-second-requires-session";
        RequireThrows<InvalidOperationException>(
            () => pipeline.FinalizeTenSeconds(start),
            "Ten-second aggregation must require an explicit active session.");

        _stage = "complete-ten-second-session";
        var sessionPipeline = new TelemetryRealtimePipeline(64, 64, 8);
        sessionPipeline.BeginSession(start);
        Require(sessionPipeline.HasActiveSession
                && sessionPipeline.ActiveSessionStartedAt == start,
            "BeginSession must expose one explicit active session boundary.");
        RequireThrows<InvalidOperationException>(
            () => sessionPipeline.BeginSession(start.AddSeconds(1)),
            "A second session cannot silently replace an active session.");

        for (var index = 0; index < 10; index++)
        {
            sessionPipeline.AppendRaw(Frame(
                start.AddSeconds(index).AddMilliseconds(500),
                Observation(metric, index + 1, 1)));
        }
        for (var index = 0; index < 10; index++)
            sessionPipeline.FinalizeOneSecond(start.AddSeconds(index));

        var tenSeconds = sessionPipeline.FinalizeTenSeconds(start);
        RequireMetric(tenSeconds, metric, 5.5, 1);
        Require(tenSeconds.Timestamp == start.AddSeconds(10)
                && sessionPipeline.SessionTenSecondAggregates.Count == 1,
            "Ten-second session aggregate must be end-stamped and stored exactly once.");
        RequireThrows<InvalidOperationException>(
            () => sessionPipeline.FinalizeTenSeconds(start),
            "Repeated ten-second finalization must not double-weight session evidence.");
        RequireThrows<InvalidOperationException>(
            () => sessionPipeline.FinalizeTenSeconds(start.AddSeconds(10)),
            "Ten-second finalization cannot run ahead of finalized one-second evidence.");

        _stage = "session-completion-and-snapshot-isolation";
        RequireThrows<ArgumentOutOfRangeException>(
            () => sessionPipeline.CompleteSession(start.AddSeconds(5)),
            "Session completion cannot precede the latest finalized ten-second evidence.");
        var completed = sessionPipeline.CompleteSession(start.AddSeconds(10));
        Require(!sessionPipeline.HasActiveSession
                && sessionPipeline.ActiveSessionStartedAt is null,
            "CompleteSession must close the active session.");
        Require(completed.StartedAt == start
                && completed.EndedAt == start.AddSeconds(10)
                && completed.TenSecondAggregates.Count == 1,
            "Completed session snapshot must preserve exact session boundaries and aggregates.");
        RequireThrows<InvalidOperationException>(
            () => sessionPipeline.CompleteSession(start.AddSeconds(11)),
            "Completing without an active session must fail closed.");
        RequireThrows<NotSupportedException>(() =>
        {
            var list = (IList<TelemetryFrame>)completed.TenSecondAggregates;
            list[0] = Frame(start.AddSeconds(10));
        }, "Completed session aggregate collection must be immutable to the caller.");

        var previousRawCount = sessionPipeline.RawFrames.Count;
        var previousOneSecondCount = sessionPipeline.OneSecondAggregates.Count;
        sessionPipeline.BeginSession(start.AddSeconds(10));
        Require(sessionPipeline.SessionTenSecondAggregates.Count == 0,
            "Beginning a new session must clear only the bounded session aggregate store.");
        Require(sessionPipeline.RawFrames.Count == previousRawCount
                && sessionPipeline.OneSecondAggregates.Count == previousOneSecondCount,
            "Beginning a new session must preserve rolling raw and one-second stores.");
        Require(completed.TenSecondAggregates.Count == 1,
            "Completed session snapshot must stay detached from later session-store clearing.");

        _stage = "session-start-cannot-rewind-one-second-cursor";
        RequireThrows<InvalidOperationException>(
            () =>
            {
                var rewind = new TelemetryRealtimePipeline(8, 8, 8);
                rewind.FinalizeOneSecond(start);
                rewind.BeginSession(start.AddMilliseconds(500));
            },
            "A new session cannot start inside an already-finalized one-second window.");

        _stage = "temporal-gap-propagates-to-ten-second-coverage";
        var sparsePipeline = new TelemetryRealtimePipeline(64, 64, 8);
        sparsePipeline.BeginSession(start);
        for (var index = 0; index < 9; index++)
        {
            sparsePipeline.AppendRaw(Frame(
                start.AddSeconds(index).AddMilliseconds(500),
                Observation(metric, index + 1, index == 3 ? 0.5 : 1)));
        }
        for (var index = 0; index < 10; index++)
            sparsePipeline.FinalizeOneSecond(start.AddSeconds(index));

        var sparseTenSeconds = sparsePipeline.FinalizeTenSeconds(start);
        RequireMetric(sparseTenSeconds, metric, 5, 0.45);

        _stage = "complete";
        Console.WriteLine("PASS Track 4 realtime pipeline finalizes exact 1s/10s buckets with bounded explicit session state");
    }

    private static TelemetryMetricObservation Observation(
        TelemetryMetricDescriptor descriptor,
        double value,
        double coverage)
        => new(
            descriptor,
            value,
            TelemetryMetricQuality.Measured,
            coverage,
            "sensor-a",
            TelemetryMetricOrigin.Direct);

    private static TelemetryFrame Frame(
        DateTimeOffset timestamp,
        params TelemetryMetricObservation[] metrics)
        => new(timestamp, metrics);

    private static void RequireMetric(
        TelemetryFrame frame,
        TelemetryMetricDescriptor descriptor,
        double expectedValue,
        double expectedCoverage)
    {
        Require(frame.TryGetMetric(descriptor.Id, out var observation) && observation is not null,
            $"Expected pipeline metric '{descriptor.Id}' is missing.");
        Require(Math.Abs(observation!.Value - expectedValue) <= 0.000001,
            $"{descriptor.Id} value mismatch. Expected {expectedValue}, got {observation.Value}.");
        Require(Math.Abs(observation.Coverage - expectedCoverage) <= 0.000001,
            $"{descriptor.Id} coverage mismatch. Expected {expectedCoverage}, got {observation.Coverage}.");
        Require(observation.Quality == TelemetryMetricQuality.Measured,
            $"{descriptor.Id} quality must remain Measured for measured contributors.");
        Require(observation.Origin == TelemetryMetricOrigin.Derived,
            $"{descriptor.Id} aggregate must declare Derived origin.");
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
