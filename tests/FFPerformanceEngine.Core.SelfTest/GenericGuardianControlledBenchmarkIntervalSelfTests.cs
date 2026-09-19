using FFPerformanceEngine.Core.Services;

internal static class GenericGuardianControlledBenchmarkIntervalSelfTests
{
    internal static async Task RunAsync()
    {
        var authority = new ControlledBenchmarkLeaseManager();
        var secondManager = new ControlledBenchmarkLeaseManager();
        var monitor = new GenericGuardianControlledBenchmarkIntervalCapture(authority);

        var clean = await monitor.CaptureAsync(_ => Task.FromResult(7));
        Require(clean.Attempted && clean.UninterruptedIdle && clean.Value == 7,
            "Only an idle, uninterrupted authority-owned capture may be reported clean.");

        var executions = 0;
        await using (var active = await secondManager.AcquireAsync("interval-test-already-active"))
        {
            var blocked = await monitor.CaptureAsync(_ =>
            {
                executions++;
                return Task.FromResult(1);
            });
            Require(!blocked.Attempted && !blocked.UninterruptedIdle && executions == 0,
                "An active Track 0 lease must reject capture before invoking its delegate.");
        }

        var before = await monitor.CaptureAsync(_ => Task.FromResult(10));
        await using (var between = await secondManager.AcquireAsync("interval-test-between-windows")) { }
        var after = await monitor.CaptureAsync(_ => Task.FromResult(11));
        Require(before.UninterruptedIdle && after.UninterruptedIdle
                && !ControlledBenchmarkActivitySnapshot.ProvesUninterruptedIdle(before.Before, after.After),
            "A complete benchmark BETWEEN individually clean windows must invalidate the overall before/after interval.");

        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = monitor.CaptureAsync(async cancellationToken =>
        {
            entered.TrySetResult();
            await finish.Task.WaitAsync(cancellationToken);
            return 42;
        });
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await using (var overlap = await secondManager.AcquireAsync("interval-test-during-capture")) { }
        finish.TrySetResult();
        var contaminated = await pending.WaitAsync(TimeSpan.FromSeconds(5));
        Require(contaminated.Attempted && !contaminated.UninterruptedIdle && contaminated.Value == 42
                && contaminated.Before.State == ControlledBenchmarkActivityState.Idle
                && contaminated.After.State == ControlledBenchmarkActivityState.Idle
                && contaminated.Before.Generation != contaminated.After.Generation,
            "A benchmark that starts and FINISHES DURING one capture cannot be mistaken for clean idle snapshots.");

        var failure = new InvalidOperationException("capture failed");
        try
        {
            await monitor.CaptureAsync<int>(_ => Task.FromException<int>(failure));
            throw new InvalidOperationException("Capture failure must propagate.");
        }
        catch (InvalidOperationException exception) when (ReferenceEquals(exception, failure)) { }

        Console.WriteLine("PASS Track 6 authority-bracketed benchmark capture: busy denied, transient and inter-window interference detected, failures preserved");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
