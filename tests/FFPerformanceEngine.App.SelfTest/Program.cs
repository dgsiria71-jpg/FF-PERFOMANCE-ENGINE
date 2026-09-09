using FFPerformanceEngine.App;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;
using FFPerformanceEngine.Core.Workloads;

await using var services = new AppServices();

Require(services.PerformanceWorkloadContext.SelectedGameId is null,
    "AppServices construction must not discover or select a performance workload implicitly.");

var catalog = new ResolvedGameCatalogResult
{
    Games =
    [
        new ResolvedGameCatalogEntry
        {
            Identity = new GameIdentity
            {
                GameId = "steam:730",
                Name = "Counter-Strike",
                Launcher = GameLauncherKind.Steam,
                AdapterId = "untrusted.identity.adapter"
            },
            Adapter = new GenericGameAdapter()
        }
    ]
};

Require(services.SelectPerformanceWorkloadContext(catalog, "  STEAM:730  "),
    "AppServices must accept an explicitly supplied already-resolved catalog + stable GameId.");
Require(services.PerformanceWorkloadContext.SelectedGameId == "steam:730",
    "AppServices must expose the canonical selected stable GameId without inventing process identity.");

var timestamp = new DateTimeOffset(2026, 9, 9, 17, 0, 0, TimeSpan.Zero);
var baseline = services.PerformanceComparison.SetBaseline(
    "A · app universal",
    Interval(timestamp));
Require(baseline.UniversalContext is not null
        && baseline.UniversalContext.GameId == "steam:730"
        && baseline.UniversalContext.AdapterId == "generic"
        && !string.IsNullOrWhiteSpace(baseline.UniversalContext.Machine.Id),
    "Normal AppServices PerformanceComparison captures must consume the explicit universal workload provider.");

services.ClearPerformanceWorkloadContext();
Require(services.PerformanceWorkloadContext.SelectedGameId is null,
    "Explicit application clear must remove the selected workload state.");
var afterClear = services.PerformanceComparison.SetCandidate(
    "B · app no universal",
    Interval(timestamp.AddSeconds(1)));
Require(afterClear.UniversalContext is null,
    "After clear, normal AppServices A/B capture must return to legacy-only/no-context behavior instead of reusing stale GameId.");

Require(!services.SelectPerformanceWorkloadContext(catalog, "steam:not-installed"),
    "AppServices must fail closed when the requested GameId is absent from the supplied resolved catalog.");
Require(services.PerformanceWorkloadContext.SelectedGameId is null,
    "A rejected application workload selection must not preserve stale state.");

Console.WriteLine("PASS Track 4 AppServices explicit workload context composition is on-demand, stable-identity bound and clearable");
return 0;

static PerformanceIntervalSummary Interval(DateTimeOffset timestamp)
{
    var frame = new TelemetryFrame(timestamp,
    [
        new TelemetryMetricObservation(
            TelemetryStandardMetrics.FrameFpsAverage,
            120,
            TelemetryMetricQuality.Measured,
            1,
            "presentmon",
            TelemetryMetricOrigin.Direct),
        new TelemetryMetricObservation(
            TelemetryStandardMetrics.FrameTimeAverageMs,
            8.33,
            TelemetryMetricQuality.Measured,
            1,
            "presentmon",
            TelemetryMetricOrigin.Direct)
    ]);
    return PerformanceIntervalAnalysis.Analyze(
    [
        new PerformanceTimelineEntry
        {
            Timestamp = timestamp,
            Kind = PerformanceTimelineKind.Telemetry,
            Title = "Telemetry",
            Detail = "Measured",
            TypedTelemetry = frame
        }
    ], timestamp, timestamp);
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
