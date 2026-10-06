using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class GenericGuardianPostSessionQueueSelfTests
{
    internal static async Task RunAsync()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "dg-guardian-postsession-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var history = new HistoryService(Path.Combine(root, "history.json"));
            var queue = new GenericGuardianPostSessionQueueService(
                Path.Combine(root, "guardian-postsession.json"),
                history);
            var a = new GenericGuardianCanarySessionKey(
                Guid.NewGuid(),
                "game.a",
                100,
                @"C:\Games\a.exe");
            var b = new GenericGuardianCanarySessionKey(
                Guid.NewGuid(),
                "game.a",
                101,
                @"C:\Games\a.exe");

            await queue.ObserveUnresolvedAsync(
                a,
                GuardianAnomalyKind.GpuSaturation,
                "No LiveSafe GPU action is approved.");
            await queue.ObserveUnresolvedAsync(
                a,
                GuardianAnomalyKind.GpuSaturation,
                "Still unresolved on a later cycle.");
            await queue.ObserveUnresolvedAsync(
                b,
                GuardianAnomalyKind.GpuSaturation,
                "Independent later session.");

            Require(
                (await queue.LoadReadyAsync()).Count == 0,
                "Unresolved anomalies must remain pending until their exact session ends.");

            await queue.CompleteSessionAsync(a);
            var ready = await queue.LoadReadyAsync();
            Require(
                ready.Count == 1
                && ready[0].SessionEpoch == a.SessionEpoch
                && ready[0].GameId == "game.a"
                && ready[0].Family == GuardianAnomalyKind.GpuSaturation
                && ready[0].ObservationCount == 2
                && ready[0].ReadyForReview
                && ready[0].SessionEndedAt is not null,
                "Post-session queue must deduplicate one session/family and expose it only after that session completes.");

            var historyEvents = await history.LoadAsync();
            Require(
                historyEvents.Any(item =>
                    item.Kind == HistoryEventKind.Guardian
                    && item.Summary.Contains("GpuSaturation", StringComparison.Ordinal)),
                "Publishing a post-session review item must be auditable in History.");

            Require(
                await queue.AcknowledgeAsync(ready[0].Id),
                "Ready post-session item must support explicit acknowledgement.");
            Require(
                (await queue.LoadReadyAsync()).Count == 0,
                "Acknowledged post-session items must leave the active review queue.");

            await queue.CompleteSessionAsync(b);
            ready = await queue.LoadReadyAsync();
            Require(
                ready.Count == 1
                && ready[0].SessionEpoch == b.SessionEpoch
                && ready[0].ObservationCount == 1,
                "Same anomaly in another physical session must remain an independent review item.");

            Console.WriteLine("PASS Track 6 Guardian post-session queue is session-scoped, deduplicated, audited and review-only");
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
