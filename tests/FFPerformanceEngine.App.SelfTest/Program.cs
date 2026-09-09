using FFPerformanceEngine.App;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;
using FFPerformanceEngine.Core.Workloads;

await using var services = new AppServices();

Require(services.PerformanceWorkloadContext.SelectedGameId is null,
    "AppServices construction must not discover or select a performance workload implicitly.");
var initialTarget = services.ResolveSelectedPerformanceCaptureTarget();
Require(initialTarget.BindingQuality == TelemetryWorkloadBindingQuality.UnknownGame
        && !initialTarget.CanCaptureProcess,
    "AppServices must expose an unavailable universal capture target until a stable workload is explicitly selected.");
var initialCapture = await services.CaptureSelectedPerformanceTelemetryAsync(TimeSpan.FromMilliseconds(1));
Require(!initialCapture.Captured
        && initialCapture.Frame is null
        && initialCapture.Target.BindingQuality == TelemetryWorkloadBindingQuality.UnknownGame,
    "AppServices selected-workload capture must fail closed before PresentMon when no explicit stable workload exists.");

var executablePath = Path.GetFullPath(
    Path.Combine(Path.GetTempPath(), "DG-App-Universal-Capture", "cs2.exe"));
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
    ],
    BoundEvidence =
    [
        new BoundGameEvidence
        {
            GameId = "steam:730",
            BindingReason = GameEvidenceBindingReason.ExactGameIdHint,
            SourceId = "running-process",
            Priority = 40,
            Observation = new GameEvidenceObservation
            {
                ObservationId = "running-process:steam:730:7730",
                Kind = GameEvidenceKind.RunningProcess,
                Confidence = 1,
                ObservedAtUtc = new DateTimeOffset(2026, 9, 9, 17, 30, 0, TimeSpan.Zero),
                GameIdHint = "steam:730",
                ProcessId = 7730,
                ExecutablePath = executablePath,
                EvidenceText = "app universal capture fixture"
            }
        }
    ]
};

Require(services.SelectPerformanceWorkloadContext(catalog, "  STEAM:730  "),
    "AppServices must accept an explicitly supplied already-resolved catalog + stable GameId.");
Require(services.PerformanceWorkloadContext.SelectedGameId == "steam:730",
    "AppServices must expose the canonical selected stable GameId without inventing process identity.");
var exactTarget = services.ResolveSelectedPerformanceCaptureTarget();
Require(exactTarget.BindingQuality == TelemetryWorkloadBindingQuality.ExactRunningProcess
        && exactTarget.GameId == "steam:730"
        && exactTarget.ProcessId == 7730
        && string.Equals(exactTarget.ExecutablePath, executablePath, StringComparison.OrdinalIgnoreCase)
        && exactTarget.CanCaptureProcess,
    "AppServices must resolve the selected workload to exactly the bound RunningProcess evidence without consulting Guardian state.");

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
var clearedTarget = services.ResolveSelectedPerformanceCaptureTarget();
Require(clearedTarget.BindingQuality == TelemetryWorkloadBindingQuality.UnknownGame
        && clearedTarget.ProcessId is null
        && !clearedTarget.CanCaptureProcess,
    "Clearing the selected workload must remove stale runtime PID/path evidence from the application capture authority.");
var afterClear = services.PerformanceComparison.SetCandidate(
    "B · app no universal",
    Interval(timestamp.AddSeconds(1)));
Require(afterClear.UniversalContext is null,
    "After clear, normal AppServices A/B capture must return to legacy-only/no-context behavior instead of reusing stale GameId.");

Require(!services.SelectPerformanceWorkloadContext(catalog, "steam:not-installed"),
    "AppServices must fail closed when the requested GameId is absent from the supplied resolved catalog.");
Require(services.PerformanceWorkloadContext.SelectedGameId is null,
    "A rejected application workload selection must not preserve stale state.");
var rejectedCapture = await services.CaptureSelectedPerformanceTelemetryAsync(TimeSpan.FromMilliseconds(1));
Require(!rejectedCapture.Captured
        && rejectedCapture.Target.BindingQuality == TelemetryWorkloadBindingQuality.UnknownGame,
    "A rejected workload selection must never fall through to an unrelated Guardian/BlueStacks capture target.");

Console.WriteLine("PASS Track 4 AppServices explicit workload context and universal capture composition are on-demand, stable-identity bound and fail-closed");
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
