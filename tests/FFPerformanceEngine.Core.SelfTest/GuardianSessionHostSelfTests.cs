using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class GuardianSessionHostSelfTests
{
    public static async Task RunAsync()
    {
        await PublishesLiveStatusAndAvoidsDuplicateLoopForSameInstance();
        await SwitchingInstanceCancelsOldLoopAndStartsNewOne();
        await StopCancelsTheLiveLoop();
        Console.WriteLine("PASS Guardian application-lifetime host lifecycle");
    }

    private static async Task PublishesLiveStatusAndAvoidsDuplicateLoopForSameInstance()
    {
        var runner = new FakeLiveRunner();
        await using var host = new GuardianSessionHost(runner);
        var published = 0;
        host.StatusChanged += (_, status) =>
        {
            published++;
            Require(status.Binding?.ProcessId == 4100, "Host must forward the exact live Guardian status without fabricating another binding.");
        };

        await host.StartAsync("Pie64", TimeSpan.FromMilliseconds(25));
        await runner.WaitForStartsAsync(1);
        await host.StartAsync("Pie64", TimeSpan.FromMilliseconds(25));
        await Task.Delay(25);

        Require(runner.StartCount == 1, "Starting the same Guardian instance twice must not create duplicate monitoring loops.");
        Require(host.IsRunning && host.InstanceName == "Pie64", "Host must expose the application-lifetime instance while its loop is active.");
        Require(host.CurrentStatus?.Binding?.ProcessId == 4100 && published == 1, "Host must retain and publish the latest real live-session status.");
    }

    private static async Task SwitchingInstanceCancelsOldLoopAndStartsNewOne()
    {
        var runner = new FakeLiveRunner();
        await using var host = new GuardianSessionHost(runner);

        await host.StartAsync("Pie64", TimeSpan.FromMilliseconds(25));
        await runner.WaitForStartsAsync(1);
        await host.StartAsync("Android11", TimeSpan.FromMilliseconds(25));
        await runner.WaitForStartsAsync(2);

        Require(runner.CancelledInstances.Contains("Pie64"), "Switching instance must cancel the previous monitoring loop before binding the new target.");
        Require(host.InstanceName == "Android11" && host.IsRunning, "Host must expose only the newly selected BlueStacks instance after a switch.");
    }

    private static async Task StopCancelsTheLiveLoop()
    {
        var runner = new FakeLiveRunner();
        await using var host = new GuardianSessionHost(runner);

        await host.StartAsync("Pie64", TimeSpan.FromMilliseconds(25));
        await runner.WaitForStartsAsync(1);
        await host.StopAsync();

        Require(runner.CancelledInstances.Contains("Pie64"), "Stopping the application Guardian host must cancel the live monitoring loop.");
        Require(!host.IsRunning && host.InstanceName is null, "Stopped host must no longer report an active Guardian instance.");
        Require(runner.ResetCount == 1, "Stopping the host must reset the live binding/cooldown runner state exactly once.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class FakeLiveRunner : IGuardianLiveSessionRunner
    {
        private readonly object _sync = new();
        private readonly List<TaskCompletionSource> _startWaiters = [];

        public int StartCount { get; private set; }
        public int ResetCount { get; private set; }
        public List<string> CancelledInstances { get; } = [];

        public async Task RunAsync(
            string instanceName,
            TimeSpan interval,
            Action<GuardianLiveSessionStatus> publish,
            CancellationToken cancellationToken = default)
        {
            lock (_sync)
            {
                StartCount++;
                foreach (var waiter in _startWaiters) waiter.TrySetResult();
                _startWaiters.Clear();
            }

            publish(new GuardianLiveSessionStatus
            {
                Binding = new GuardianSessionBinding(4100, instanceName),
                Instance = new BlueStacksInstance { Name = instanceName },
                Message = "live"
            });

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                CancelledInstances.Add(instanceName);
            }
        }

        public Task ResetAsync(CancellationToken cancellationToken = default)
        {
            ResetCount++;
            return Task.CompletedTask;
        }

        public async Task WaitForStartsAsync(int count)
        {
            Task waiter;
            lock (_sync)
            {
                if (StartCount >= count) return;
                var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _startWaiters.Add(source);
                waiter = source.Task;
            }
            await waiter.WaitAsync(TimeSpan.FromSeconds(2));
        }
    }
}
