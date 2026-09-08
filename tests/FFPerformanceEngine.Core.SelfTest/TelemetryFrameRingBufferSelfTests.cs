using FFPerformanceEngine.Core.Telemetry;

internal static class TelemetryFrameRingBufferSelfTests
{
    private static string _stage = "not-started";

    internal static void Run()
    {
        try
        {
            RunCore();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Telemetry frame ring buffer self-test failed at stage '{_stage}'.",
                ex);
        }
    }

    private static void RunCore()
    {
        var t0 = new DateTimeOffset(2026, 9, 8, 21, 0, 0, TimeSpan.Zero);

        _stage = "capacity-validation";
        RequireThrows<ArgumentOutOfRangeException>(() => new TelemetryFrameRingBuffer(0),
            "Capacity zero must be rejected.");
        RequireThrows<ArgumentOutOfRangeException>(() => new TelemetryFrameRingBuffer(-1),
            "Negative capacity must be rejected.");

        _stage = "fifo-eviction-and-timestamp-order";
        var buffer = new TelemetryFrameRingBuffer(2);
        var lateFirst = CreateFrame(t0.AddSeconds(2), 10);
        var earlySecond = CreateFrame(t0, 20);
        var middleThird = CreateFrame(t0.AddSeconds(1), 30);
        buffer.Append(lateFirst);
        buffer.Append(earlySecond);
        buffer.Append(middleThird);

        Require(buffer.Capacity == 2, "Capacity must be exposed unchanged.");
        Require(buffer.Count == 2, "Count must never exceed configured capacity.");
        var bounded = buffer.Snapshot();
        Require(bounded.Count == 2, "Snapshot must contain the retained frames only.");
        Require(ReferenceEquals(bounded[0], earlySecond) && ReferenceEquals(bounded[1], middleThird),
            "FIFO must evict the first insertion while snapshots remain timestamp ordered.");
        Require(!bounded.Any(frame => ReferenceEquals(frame, lateFirst)),
            "The oldest insertion must be evicted deterministically at capacity.");

        _stage = "equal-timestamp-insertion-tie-break";
        var tied = new TelemetryFrameRingBuffer(3);
        var tie1 = CreateFrame(t0, 1);
        var tie2 = CreateFrame(t0, 2);
        var tie3 = CreateFrame(t0, 3);
        tied.Append(tie1);
        tied.Append(tie2);
        tied.Append(tie3);
        var tiedSnapshot = tied.Snapshot();
        Require(ReferenceEquals(tiedSnapshot[0], tie1)
                && ReferenceEquals(tiedSnapshot[1], tie2)
                && ReferenceEquals(tiedSnapshot[2], tie3),
            "Equal timestamps must preserve insertion sequence as deterministic tie-breaker.");

        _stage = "snapshot-detached-from-later-mutations";
        var detached = tied.Snapshot();
        tied.Append(CreateFrame(t0.AddSeconds(1), 4));
        Require(detached.Count == 3
                && ReferenceEquals(detached[0], tie1)
                && ReferenceEquals(detached[1], tie2)
                && ReferenceEquals(detached[2], tie3),
            "A returned snapshot must not change when the buffer mutates later.");

        _stage = "half-open-window";
        var windowed = new TelemetryFrameRingBuffer(4);
        var atStart = CreateFrame(t0, 100);
        var inside = CreateFrame(t0.AddMilliseconds(500), 200);
        var atEnd = CreateFrame(t0.AddSeconds(1), 300);
        windowed.Append(atEnd);
        windowed.Append(inside);
        windowed.Append(atStart);
        var window = windowed.Snapshot(t0, t0.AddSeconds(1));
        Require(window.Count == 2
                && ReferenceEquals(window[0], atStart)
                && ReferenceEquals(window[1], inside),
            "Window snapshots must use [startInclusive, endExclusive) and timestamp ordering.");
        Require(windowed.Snapshot(t0, t0).Count == 0,
            "Equal range boundaries must produce an empty window.");
        RequireThrows<ArgumentOutOfRangeException>(
            () => windowed.Snapshot(t0.AddSeconds(1), t0),
            "Reversed range must be rejected.");

        _stage = "clear-and-null-validation";
        RequireThrows<ArgumentNullException>(() => windowed.Append(null!),
            "Appending a null frame must be rejected.");
        windowed.Clear();
        Require(windowed.Count == 0 && windowed.Snapshot().Count == 0,
            "Clear must remove every retained frame.");

        // This stress section intentionally runs from Program after CLR module
        // initialization has completed. Running Parallel.For from a ModuleInitializer
        // can deadlock when worker threads wait for the same module initialization.
        _stage = "concurrent-bounded-stress";
        const int capacity = 64;
        var concurrent = new TelemetryFrameRingBuffer(capacity);
        Parallel.For(0, 1000, i =>
        {
            concurrent.Append(CreateFrame(t0.AddTicks(i), i));
        });
        Require(concurrent.Count == capacity,
            "Concurrent appends must preserve the configured capacity exactly once saturated.");
        var concurrentSnapshot = concurrent.Snapshot();
        Require(concurrentSnapshot.Count == capacity,
            "Concurrent snapshot count must equal the saturated capacity.");
        for (var i = 1; i < concurrentSnapshot.Count; i++)
        {
            Require(concurrentSnapshot[i - 1].Timestamp <= concurrentSnapshot[i].Timestamp,
                "Concurrent snapshots must remain timestamp sorted.");
        }

        _stage = "complete";
        Console.WriteLine("PASS Track 4 telemetry v2 realtime ring buffer is bounded, deterministic and thread-safe");
    }

    private static TelemetryFrame CreateFrame(DateTimeOffset timestamp, double value)
        => new(timestamp,
        [
            new TelemetryMetricObservation(
                TelemetryStandardMetrics.SystemCpuUtilizationPercent,
                value,
                TelemetryMetricQuality.Measured,
                1d,
                "test",
                TelemetryMetricOrigin.Direct)
        ]);

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
