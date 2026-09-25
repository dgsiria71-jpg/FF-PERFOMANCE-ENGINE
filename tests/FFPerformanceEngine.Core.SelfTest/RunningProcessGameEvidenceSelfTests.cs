using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Workloads;

internal static class RunningProcessGameEvidenceSelfTests
{
    private static string _stage = "not-started";

    [ModuleInitializer]
    internal static void Run()
    {
        try
        {
            RunAsync().WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
        }
        catch (TimeoutException ex)
        {
            throw new InvalidOperationException(
                $"Running process evidence self-test exceeded 30 seconds at stage '{_stage}'.",
                ex);
        }
    }

    private static async Task RunAsync()
    {
        _stage = "create-fixture";
        var observedAt = new DateTimeOffset(2026, 9, 8, 15, 0, 0, TimeSpan.Zero);
        var provider = new FakeRunningProcessProvider(
        [
            new RunningProcessObservation
            {
                ProcessId = 30,
                ExecutablePath = @"C:\Games\Foo\foo.exe",
                DisplayName = " Foo ",
                ObservedAtUtc = observedAt
            },
            new RunningProcessObservation
            {
                ProcessId = 10,
                ExecutablePath = @"C:\Games\Bar\bar.exe",
                DisplayName = "Bar",
                ObservedAtUtc = observedAt
            },
            new RunningProcessObservation
            {
                ProcessId = 0,
                ExecutablePath = @"C:\Invalid\zero.exe",
                ObservedAtUtc = observedAt
            },
            new RunningProcessObservation
            {
                ProcessId = 99,
                ExecutablePath = null,
                DisplayName = "No path",
                ObservedAtUtc = observedAt
            },
            new RunningProcessObservation
            {
                ProcessId = 55,
                ExecutablePath = "relative.exe",
                DisplayName = "Relative",
                ObservedAtUtc = observedAt
            }
        ]);

        _stage = "construct";
        var source = new RunningProcessGameEvidenceSource(provider);
        Require(source.SourceId == "windows-running-process" && source.Priority == 40,
            "Running-process evidence source must expose stable provenance and conservative priority.");
        Require(provider.Calls == 0,
            "Constructing running-process evidence must not enumerate processes.");

        _stage = "observe";
        var observations = await source.ObserveAsync();

        _stage = "assert-observations";
        Require(provider.Calls == 1 && observations.Count == 2,
            "Only positive PIDs with fully-qualified executable paths may become process evidence.");
        Require(observations.Select(item => item.ObservationId)
                .SequenceEqual(["pid:10", "pid:30"]),
            "Running-process observations must be deterministic by PID.");
        Require(observations.All(item => item.Kind == GameEvidenceKind.RunningProcess
                                         && item.GameIdHint is null
                                         && item.Confidence == 0.98),
            "A running process is high-quality runtime evidence but must never self-declare a game identity.");

        var bar = observations.Single(item => item.ObservationId == "pid:10");
        Require(bar.ProcessId == 10
                && bar.ExecutablePath == Path.GetFullPath(@"C:\Games\Bar\bar.exe")
                && bar.DisplayName == "Bar"
                && bar.ObservedAtUtc == observedAt
                && bar.EvidenceText.Contains("PID 10", StringComparison.Ordinal)
                && bar.EvidenceText.Contains(bar.ExecutablePath, StringComparison.OrdinalIgnoreCase),
            "Running-process evidence must preserve PID, normalized full path, timestamp and transparent provenance.");

        var foo = observations.Single(item => item.ObservationId == "pid:30");
        Require(foo.DisplayName == "Foo",
            "Display metadata may be trimmed but remains non-authoritative evidence only.");

        _stage = "assert-cancellation";
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await ExpectThrowsAsync<OperationCanceledException>(
            () => source.ObserveAsync(cancelled.Token),
            "Pre-cancelled process evidence must not invoke the process provider.");
        Require(provider.Calls == 1,
            "Pre-cancellation must leave provider call count unchanged.");

        _stage = "complete";
        Console.WriteLine("PASS Track 3 running-process evidence stays transient and never manufactures GameId");
    }

    private sealed class FakeRunningProcessProvider : IRunningProcessObservationProvider
    {
        private readonly IReadOnlyList<RunningProcessObservation> _observations;

        internal FakeRunningProcessProvider(IReadOnlyList<RunningProcessObservation> observations)
            => _observations = observations;

        internal int Calls { get; private set; }

        public Task<IReadOnlyList<RunningProcessObservation>> ObserveAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return Task.FromResult(_observations);
        }
    }

    private static async Task ExpectThrowsAsync<TException>(Func<Task> action, string message)
        where TException : Exception
    {
        try
        {
            await action();
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
