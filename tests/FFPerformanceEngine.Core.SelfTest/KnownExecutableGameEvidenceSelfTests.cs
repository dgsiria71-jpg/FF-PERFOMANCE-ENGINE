using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Workloads;

internal static class KnownExecutableGameEvidenceSelfTests
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
                $"Known-executable evidence self-test exceeded 30 seconds at stage '{_stage}'.",
                ex);
        }
    }

    private static async Task RunAsync()
    {
        _stage = "create-fixture";
        var observedAt = new DateTimeOffset(2026, 9, 8, 19, 30, 0, TimeSpan.Zero);
        var provider = new FakeKnownExecutableProvider(
        [
            new KnownExecutableObservation
            {
                ExecutablePath = @"C:\Games\Foo\foo.exe",
                RegistrationName = " foo.exe ",
                ObservedAtUtc = observedAt
            },
            new KnownExecutableObservation
            {
                ExecutablePath = @"c:\games\foo\FOO.exe",
                RegistrationName = "FOO.EXE",
                ObservedAtUtc = observedAt.AddSeconds(1)
            },
            new KnownExecutableObservation
            {
                ExecutablePath = @"C:\Games\Bar\bar.exe",
                RegistrationName = "bar.exe",
                ObservedAtUtc = observedAt
            },
            new KnownExecutableObservation
            {
                ExecutablePath = "relative.exe",
                RegistrationName = "relative.exe",
                ObservedAtUtc = observedAt
            },
            new KnownExecutableObservation
            {
                ExecutablePath = null,
                RegistrationName = "missing.exe",
                ObservedAtUtc = observedAt
            }
        ]);

        _stage = "construct";
        var source = new KnownExecutableGameEvidenceSource(provider);
        Require(source.SourceId == "windows-app-paths" && source.Priority == 30,
            "Known-executable evidence source must expose stable provenance below running-process priority.");
        Require(provider.Calls == 0,
            "Constructing known-executable evidence must not inspect Windows registrations.");

        _stage = "observe";
        var observations = await source.ObserveAsync();

        _stage = "assert-observations";
        Require(provider.Calls == 1 && observations.Count == 2,
            "Only fully-qualified executable paths may become known-executable evidence and duplicate paths must collapse deterministically.");
        Require(observations.All(item => item.Kind == GameEvidenceKind.KnownExecutable
                                         && item.GameIdHint is null
                                         && item.ProcessId is null
                                         && item.Confidence == 0.92),
            "Registered executables are strong static evidence but never self-declare game identity or runtime state.");

        var foo = observations.Single(item =>
            string.Equals(item.ExecutablePath, Path.GetFullPath(@"C:\Games\Foo\foo.exe"), StringComparison.OrdinalIgnoreCase));
        Require(foo.ObservationId.StartsWith("path:", StringComparison.Ordinal)
                && foo.DisplayName == "foo.exe"
                && foo.ObservedAtUtc == observedAt
                && foo.EvidenceText.Contains("App Paths", StringComparison.Ordinal)
                && foo.EvidenceText.Contains(foo.ExecutablePath!, StringComparison.OrdinalIgnoreCase),
            "Known-executable evidence must retain canonical path, earliest duplicate observation and transparent registration provenance.");

        var orderedPaths = observations.Select(item => item.ExecutablePath).ToArray();
        Require(orderedPaths.SequenceEqual(
            orderedPaths.OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(item => item, StringComparer.Ordinal)),
            "Known-executable evidence output must be deterministic by canonical path.");

        _stage = "assert-cancellation";
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await ExpectThrowsAsync<OperationCanceledException>(
            () => source.ObserveAsync(cancelled.Token),
            "Pre-cancelled known-executable discovery must not invoke the provider.");
        Require(provider.Calls == 1,
            "Pre-cancellation must leave provider call count unchanged.");

        _stage = "complete";
        Console.WriteLine("PASS Track 3 App Paths evidence stays non-authoritative and deterministic");
    }

    private sealed class FakeKnownExecutableProvider : IKnownExecutableObservationProvider
    {
        private readonly IReadOnlyList<KnownExecutableObservation> _observations;

        internal FakeKnownExecutableProvider(IReadOnlyList<KnownExecutableObservation> observations)
            => _observations = observations;

        internal int Calls { get; private set; }

        public Task<IReadOnlyList<KnownExecutableObservation>> ObserveAsync(
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
