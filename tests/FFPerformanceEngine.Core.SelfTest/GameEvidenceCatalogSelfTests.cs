using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Workloads;

internal static class GameEvidenceCatalogSelfTests
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
                $"Game evidence catalog self-test exceeded 30 seconds at stage '{_stage}'.",
                ex);
        }
    }

    private static async Task RunAsync()
    {
        _stage = "create-fixture";
        var observedAt = new DateTimeOffset(2026, 9, 8, 15, 0, 0, TimeSpan.Zero);

        var high = new FakeEvidenceSource(
            "  RUNNING  ",
            80,
            _ => Task.FromResult<IReadOnlyList<GameEvidenceObservation>>(
            [
                new()
                {
                    ObservationId = " PID:42 ",
                    Kind = GameEvidenceKind.RunningProcess,
                    Confidence = 0.70,
                    ObservedAtUtc = observedAt,
                    ExecutablePath = @"C:\Games\Foo\foo.exe",
                    ProcessId = 42,
                    EvidenceText = "first"
                },
                new()
                {
                    ObservationId = "pid:42",
                    Kind = GameEvidenceKind.RunningProcess,
                    Confidence = 0.95,
                    ObservedAtUtc = observedAt.AddSeconds(1),
                    ExecutablePath = @"C:\Games\Foo\foo.exe",
                    ProcessId = 42,
                    EvidenceText = "stronger duplicate"
                },
                new()
                {
                    ObservationId = " ",
                    Kind = GameEvidenceKind.Other,
                    Confidence = 1,
                    ObservedAtUtc = observedAt,
                    EvidenceText = "invalid id"
                }
            ]));

        var broken = new FakeEvidenceSource(
            "broken",
            70,
            _ => throw new InvalidOperationException("boom"));

        var low = new FakeEvidenceSource(
            "installed",
            20,
            _ => Task.FromResult<IReadOnlyList<GameEvidenceObservation>>(
            [
                new()
                {
                    ObservationId = "app:1",
                    Kind = GameEvidenceKind.InstalledApplication,
                    Confidence = double.PositiveInfinity,
                    ObservedAtUtc = observedAt,
                    DisplayName = "Example",
                    EvidenceText = "installed app"
                }
            ]));

        _stage = "discover";
        var catalog = new GameEvidenceCatalogService([low, broken, high]);
        var result = await catalog.DiscoverAsync();

        _stage = "assert-discovery";
        Require(high.Calls == 1 && broken.Calls == 1 && low.Calls == 1,
            "Each evidence source must execute once per explicit pass.");
        Require(result.Observations.Count == 2,
            "Blank ids and lower-confidence duplicates must be removed.");
        Require(result.Observations[0].SourceId == "running"
                && result.Observations[0].Priority == 80
                && result.Observations[0].Observation.ObservationId == "pid:42"
                && result.Observations[0].Observation.Confidence == 0.95
                && result.Observations[0].Observation.EvidenceText == "stronger duplicate",
            "Highest-confidence duplicate must win after canonical source/observation normalization.");
        Require(result.Observations[1].SourceId == "installed"
                && result.Observations[1].Priority == 20
                && result.Observations[1].Observation.Confidence == 0,
            "Non-finite confidence must normalize to zero.");
        Require(result.Warnings.Count == 1
                && result.Warnings[0].SourceId == "broken"
                && result.Warnings[0].Message.Contains("boom", StringComparison.Ordinal),
            "One evidence-source failure must become one warning without suppressing other observations.");

        _stage = "assert-cancellation";
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await ExpectThrowsAsync<OperationCanceledException>(
            () => catalog.DiscoverAsync(cancelled.Token),
            "Pre-cancelled evidence discovery must not invoke platform sources.");
        Require(high.Calls == 1 && broken.Calls == 1 && low.Calls == 1,
            "A cancelled second pass must not touch any evidence source.");

        _stage = "complete";
        Console.WriteLine("PASS Track 3 evidence catalog isolates weak observations from stable game identity authority");
    }

    private sealed class FakeEvidenceSource : IGameEvidenceSource
    {
        private readonly Func<CancellationToken, Task<IReadOnlyList<GameEvidenceObservation>>> _observe;

        internal FakeEvidenceSource(
            string sourceId,
            int priority,
            Func<CancellationToken, Task<IReadOnlyList<GameEvidenceObservation>>> observe)
        {
            SourceId = sourceId;
            Priority = priority;
            _observe = observe;
        }

        internal int Calls { get; private set; }
        public string SourceId { get; }
        public int Priority { get; }

        public Task<IReadOnlyList<GameEvidenceObservation>> ObserveAsync(
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return _observe(cancellationToken);
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
