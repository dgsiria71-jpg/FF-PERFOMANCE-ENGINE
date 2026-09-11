using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class PerformanceEvidenceQualitySelfTests
{
    internal static void Run()
    {
        var start = new DateTimeOffset(2026, 9, 7, 14, 30, 0, TimeSpan.Zero);
        var interval = PerformanceIntervalAnalysis.Analyze(
        [
            Entry(start, 120, 8.33, "PresentMon · 600 frames"),
            Entry(start.AddSeconds(1), 121, 8.27, "PresentMon · 605 frames")
        ],
        start,
        start.AddSeconds(1));

        var snapshot = PerformanceEvidenceSnapshot.Capture(
            "PresentMon measured evidence",
            interval,
            start.AddSeconds(2));

        Require(snapshot.Quality == PerformanceEvidenceQuality.Measured,
            "PresentMon frame telemetry is direct measured evidence and must not be downgraded to Partial only because DataQuality carries frame-count detail.");
        Require(snapshot.FpsEvidenceSamples == 2 && snapshot.FrameTimeEvidenceSamples == 2,
            "Measured PresentMon evidence must still be derived from the finite copied points, not from the quality label alone.");

        var partial = PerformanceEvidenceSnapshot.Capture(
            "Partial evidence",
            PerformanceIntervalAnalysis.Analyze(
            [
                Entry(start, 120, 8.33, "PresentMon · 600 frames"),
                Entry(start.AddSeconds(1), 121, null, "Partial")
            ],
            start,
            start.AddSeconds(1)),
            start.AddSeconds(2));
        Require(partial.Quality == PerformanceEvidenceQuality.Partial,
            "Recognizing PresentMon as measured must not promote a mixed or incomplete interval to fully measured evidence.");

        Console.WriteLine("PASS PresentMon detailed DataQuality is recognized as direct measured A/B evidence without weakening completeness checks");
    }

    private static PerformanceTimelineEntry Entry(
        DateTimeOffset timestamp,
        double? fps,
        double? frameTimeMs,
        string dataQuality)
        => new()
        {
            Timestamp = timestamp,
            Kind = PerformanceTimelineKind.Telemetry,
            Title = "Telemetry",
            Detail = dataQuality,
            Telemetry = new TelemetrySample
            {
                Timestamp = timestamp,
                Fps = fps,
                FrameTimeMs = frameTimeMs,
                LatencyMs = 5,
                DataQuality = dataQuality
            }
        };

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
