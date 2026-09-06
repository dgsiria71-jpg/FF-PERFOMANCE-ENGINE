using FFPerformanceEngine.Core.Services;

internal static class PerformanceTimelineEventRecorderSelfTests
{
    public static void Run()
    {
        var start = new DateTimeOffset(2026, 9, 6, 2, 15, 0, TimeSpan.Zero);
        var timeline = new PerformanceTimelineBuffer(capacity: 8);
        var recorder = new PerformanceTimelineEventRecorder(timeline);

        recorder.RecordGuardianStatus(new GuardianLiveSessionStatus
        {
            Timestamp = start,
            Binding = new GuardianSessionBinding(4321, "Pie64"),
            Message = "Guardian observando a sessão."
        });
        recorder.RecordGuardianStatus(new GuardianLiveSessionStatus
        {
            Timestamp = start.AddSeconds(2),
            Binding = new GuardianSessionBinding(4321, "Pie64"),
            Message = "Guardian observando a sessão."
        });
        recorder.RecordGuardianStatus(new GuardianLiveSessionStatus
        {
            Timestamp = start.AddSeconds(4),
            Binding = new GuardianSessionBinding(4321, "Pie64"),
            Message = "Guardian detectou degradação e está validando a alteração."
        });
        recorder.RecordUserMarker(start.AddSeconds(5), "Percebi uma queda durante o combate.");

        var entries = timeline.Snapshot();
        Require(entries.Count == 3,
            "Repeated identical Guardian statuses must be deduplicated while changed evidence and user markers remain visible.");
        Require(entries.Count(entry => entry.Kind == PerformanceTimelineKind.Guardian) == 2,
            "Guardian timeline integration must retain meaningful status changes without flooding identical loop updates.");
        Require(entries[0].Timestamp == start && entries[1].Timestamp == start.AddSeconds(4),
            "Guardian timeline entries must preserve the source status timestamps.");
        Require(entries[1].Detail == "Guardian detectou degradação e está validando a alteração.",
            "Guardian timeline entries must preserve the exact evidence message.");

        var marker = entries.Single(entry => entry.Kind == PerformanceTimelineKind.UserMarker);
        Require(marker.Timestamp == start.AddSeconds(5) && marker.Detail == "Percebi uma queda durante o combate.",
            "Manual markers must preserve the user-supplied timestamp and detail.");
        Require(marker.Telemetry is null,
            "Manual markers must never fabricate telemetry evidence.");

        Console.WriteLine("PASS Performance Guardian events and manual marker timeline contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
