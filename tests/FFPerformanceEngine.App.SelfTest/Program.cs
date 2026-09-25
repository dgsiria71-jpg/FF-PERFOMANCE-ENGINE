using FFPerformanceEngine.App;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;
using FFPerformanceEngine.Core.Workloads;

await using var services = new AppServices();

Require(services.UniversalTuningCandidates is not null,
    "AppServices must compose one shared universal BlueStacks candidate bridge without running it during construction.");
Require(services.UniversalValidatedProfileProvenance is not null,
    "AppServices must compose one shared persisted-Custom universal provenance resolver from existing profile/history authorities.");
var missingProfileProvenance = await services.ResolveCurrentUniversalValidatedProfileProvenanceAsync(Guid.NewGuid());
Require(missingProfileProvenance is null,
    "AppServices on-demand universal profile provenance must fail closed for an unknown persisted profile without inventing identity or triggering unrelated discovery.");
Require(services.UniversalPersistedPromotedProfileProvenance is not null,
    "AppServices must compose one shared persisted promoted-winner provenance resolver from the same profile/history/current-Custom authorities.");
var missingPromotedProfileProvenance = await services.ResolveCurrentUniversalPersistedPromotedProfileProvenanceAsync(Guid.NewGuid());
Require(missingPromotedProfileProvenance is null,
    "AppServices on-demand promoted-winner universal provenance must fail closed for an unknown persisted profile before environment/config work or unrelated discovery.");

var hiddenProvenance = UniversalProfileProvenancePresentation.FromProjection(null);
Require(!hiddenProvenance.IsVisible
        && hiddenProvenance.GameId.Length == 0
        && hiddenProvenance.AdapterId.Length == 0
        && hiddenProvenance.CandidateLines.Count == 0,
    "Profiles presentation must remain completely hidden when AppServices supplies no universal provenance; UI must not fabricate identifiers or candidate values.");

var profileIdentity = LegacyGameIdentityBridge.FromGameKind(GameKind.FreeFire)
    ?? throw new InvalidOperationException("Profiles presentation fixture requires the stable Free Fire identity bridge.");
var profileCandidate = new UniversalTuningCandidate
{
    Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [$"workload.{profileIdentity.AdapterId}.ram-mb"] = "4096",
        [$"workload.{profileIdentity.AdapterId}.cpu-cores"] = "2",
        [$"workload.{profileIdentity.AdapterId}.fps-target"] = "90"
    }
};
var profileProjection = new UniversalValidatedProfileProjection
{
    SpecializedProfile = new PerformanceProfile
    {
        Name = "Custom validado",
        Kind = ProfileKind.Custom,
        Game = GameKind.FreeFire,
        InstanceName = "Pie64",
        Evidence = EvidenceLevel.Validated,
        SourceComparisonId = Guid.NewGuid(),
        EnvironmentFingerprint = "profiles-presentation-fixture"
    },
    SourceValidation = new UniversalValidatedPerformanceProjection
    {
        SpecializedRecord = null!,
        UniversalCandidate = profileCandidate
    },
    UniversalCandidate = profileCandidate,
    Identity = profileIdentity,
    AdapterId = profileIdentity.AdapterId
};
var visibleProvenance = UniversalProfileProvenancePresentation.FromProjection(profileProjection);
var expectedCandidateLines = profileCandidate.Values
    .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
    .ThenBy(pair => pair.Key, StringComparer.Ordinal)
    .Select(pair => $"{pair.Key} = {pair.Value}")
    .ToArray();
Require(visibleProvenance.IsVisible
        && string.Equals(visibleProvenance.GameId, profileIdentity.GameId, StringComparison.Ordinal)
        && string.Equals(visibleProvenance.AdapterId, profileIdentity.AdapterId, StringComparison.Ordinal)
        && visibleProvenance.CandidateLines.SequenceEqual(expectedCandidateLines, StringComparer.Ordinal),
    "Profiles presentation must copy the already-proven GameId, AdapterId and exact universal candidate key/value pairs deterministically without renaming or inferring them.");

var promotedProfile = new PerformanceProfile
{
    Name = "Recomendado promovido",
    Kind = ProfileKind.Recommended,
    Game = GameKind.FreeFire,
    InstanceName = "Pie64",
    Evidence = EvidenceLevel.Validated,
    SourceComparisonId = Guid.NewGuid(),
    EnvironmentFingerprint = "promoted-profiles-presentation-fixture"
};
var promotedProjection = new UniversalPersistedPromotedProfileProjection
{
    PromotedProfile = promotedProfile,
    PromotionReceipt = null!,
    ChallengerProfile = profileProjection,
    RevalidationRound = null!,
    UniversalCandidate = profileCandidate,
    Identity = profileIdentity,
    AdapterId = profileIdentity.AdapterId
};
var hiddenPromotedProvenance = UniversalPromotedProfileProvenancePresentation.FromProjection(null);
Require(!hiddenPromotedProvenance.IsVisible
        && hiddenPromotedProvenance.ProfileId == Guid.Empty
        && hiddenPromotedProvenance.ProfileName.Length == 0
        && hiddenPromotedProvenance.GameId.Length == 0
        && hiddenPromotedProvenance.AdapterId.Length == 0
        && hiddenPromotedProvenance.CandidateLines.Count == 0,
    "Promoted-winner presentation must remain hidden and empty when AppServices supplies no durable current universal provenance.");
var visiblePromotedProvenance = UniversalPromotedProfileProvenancePresentation.FromProjection(promotedProjection);
Require(visiblePromotedProvenance.IsVisible
        && visiblePromotedProvenance.ProfileId == promotedProfile.Id
        && string.Equals(visiblePromotedProvenance.ProfileName, promotedProfile.Name, StringComparison.Ordinal)
        && visiblePromotedProvenance.ProfileKind == promotedProfile.Kind
        && string.Equals(visiblePromotedProvenance.GameId, profileIdentity.GameId, StringComparison.Ordinal)
        && string.Equals(visiblePromotedProvenance.AdapterId, profileIdentity.AdapterId, StringComparison.Ordinal)
        && visiblePromotedProvenance.CandidateLines.SequenceEqual(expectedCandidateLines, StringComparer.Ordinal),
    "Promoted-winner presentation must copy only the already-proven winner identity, stable GameId, AdapterId and exact universal candidate values without reconstructing receipt/revalidation/challenge authority.");

Require(services.PerformanceWorkloadContext.SelectedGameId is null,
    "AppServices construction must not discover or select a performance workload implicitly.");
var initialTarget = services.ResolveSelectedPerformanceCaptureTarget();
Require(initialTarget.BindingQuality == TelemetryWorkloadBindingQuality.UnknownGame
        && !initialTarget.CanCaptureProcess,
    "AppServices must expose an unavailable universal capture target until a stable workload is explicitly selected.");
var initialRoute = services.ResolvePerformanceCaptureRoute();
Require(!initialRoute.UsesSelectedWorkload
        && initialRoute.SelectedGameId is null
        && initialRoute.WorkloadTarget is null
        && !initialRoute.CanCapture,
    "Without explicit universal selection, Performance routing must remain on the legacy Guardian path and fail closed when Guardian has no exact binding.");
var initialRoutePresentation = PerformanceCaptureRoutePresentation.FromRoute(initialRoute);
Require(!initialRoutePresentation.CanMeasure
        && initialRoutePresentation.TargetText.Contains("Guardian", StringComparison.Ordinal)
        && initialRoutePresentation.TargetDetail.Contains("BlueStacks", StringComparison.Ordinal)
        && initialRoutePresentation.CaptureDetail.Contains("Guardian", StringComparison.Ordinal),
    "Legacy Guardian routing must have one deterministic UI presentation without pretending a universal workload is selected.");
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
        Bound("steam:730", GameEvidenceKind.RunningProcess, 7730, executablePath)
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
var exactRoute = services.ResolvePerformanceCaptureRoute();
Require(exactRoute.UsesSelectedWorkload
        && exactRoute.SelectedGameId == "steam:730"
        && ReferenceEquals(exactRoute.WorkloadTarget, exactTarget) == false
        && exactRoute.WorkloadTarget?.BindingQuality == TelemetryWorkloadBindingQuality.ExactRunningProcess
        && exactRoute.WorkloadTarget.ProcessId == 7730
        && exactRoute.GuardianTarget is null
        && exactRoute.CanCapture,
    "An explicit stable workload with one exact RunningProcess must take routing authority over Guardian and expose only its exact universal target.");
var exactRoutePresentation = PerformanceCaptureRoutePresentation.FromRoute(exactRoute);
Require(exactRoutePresentation.CanMeasure
        && exactRoutePresentation.TargetText.Contains("steam:730", StringComparison.Ordinal)
        && exactRoutePresentation.TargetText.Contains("7730", StringComparison.Ordinal)
        && exactRoutePresentation.TargetDetail.Contains("RunningProcess", StringComparison.Ordinal)
        && !exactRoutePresentation.TargetText.Contains("Instância", StringComparison.Ordinal),
    "Universal exact routing UI must expose stable GameId + exact PID without relabeling a native game as a BlueStacks instance.");

var timestamp = new DateTimeOffset(2026, 9, 9, 17, 0, 0, TimeSpan.Zero);
var baseline = services.PerformanceComparison.SetBaseline(
    "A · app universal",
    Interval(timestamp));
Require(baseline.UniversalContext is not null
        && baseline.UniversalContext.GameId == "steam:730"
        && baseline.UniversalContext.AdapterId == "generic"
        && !string.IsNullOrWhiteSpace(baseline.UniversalContext.Machine.Id),
    "Normal AppServices PerformanceComparison captures must consume the explicit universal workload provider.");

var knownOnlyCatalog = new ResolvedGameCatalogResult
{
    Games = catalog.Games,
    BoundEvidence =
    [
        Bound("steam:730", GameEvidenceKind.KnownExecutable, null, executablePath)
    ]
};
Require(services.SelectPerformanceWorkloadContext(knownOnlyCatalog, "steam:730"),
    "A stable workload remains selectable even when its runtime process evidence is unavailable.");
var blockedRoute = services.ResolvePerformanceCaptureRoute();
Require(blockedRoute.UsesSelectedWorkload
        && blockedRoute.SelectedGameId == "steam:730"
        && blockedRoute.WorkloadTarget?.BindingQuality == TelemetryWorkloadBindingQuality.UnavailableRunningProcess
        && blockedRoute.GuardianTarget is null
        && !blockedRoute.CanCapture,
    "An explicitly selected workload with only KnownExecutable evidence must remain authoritative but blocked; routing must not silently fall back to Guardian.");
var blockedRoutePresentation = PerformanceCaptureRoutePresentation.FromRoute(blockedRoute);
Require(!blockedRoutePresentation.CanMeasure
        && blockedRoutePresentation.TargetText.Contains("steam:730", StringComparison.Ordinal)
        && !blockedRoutePresentation.TargetText.Contains("PID", StringComparison.Ordinal)
        && blockedRoutePresentation.TargetDetail.Contains("permanece selecionado", StringComparison.OrdinalIgnoreCase)
        && blockedRoutePresentation.TargetDetail.Contains("Guardian", StringComparison.Ordinal),
    "Blocked universal routing UI must keep the selected GameId visible and explicitly disclose that Guardian fallback is forbidden.");
var blockedPresentation = await services.CaptureCurrentPerformanceTelemetryAsync(TimeSpan.FromMilliseconds(1));
Require(!blockedPresentation.HasMeasurement
        && blockedPresentation.Instance == "—"
        && blockedPresentation.ProcessId == "—"
        && blockedPresentation.Detail.Contains("steam:730", StringComparison.Ordinal),
    "Blocked selected-workload capture must fail closed before PresentMon and disclose the selected stable GameId without inventing a PID or BlueStacks instance.");

services.ClearPerformanceWorkloadContext();
Require(services.PerformanceWorkloadContext.SelectedGameId is null,
    "Explicit application clear must remove the selected workload state.");
var clearedTarget = services.ResolveSelectedPerformanceCaptureTarget();
Require(clearedTarget.BindingQuality == TelemetryWorkloadBindingQuality.UnknownGame
        && clearedTarget.ProcessId is null
        && !clearedTarget.CanCaptureProcess,
    "Clearing the selected workload must remove stale runtime PID/path evidence from the application capture authority.");
var clearedRoute = services.ResolvePerformanceCaptureRoute();
Require(!clearedRoute.UsesSelectedWorkload
        && clearedRoute.SelectedGameId is null
        && clearedRoute.WorkloadTarget is null,
    "Clearing universal selection must restore the legacy Guardian route instead of preserving universal routing state.");
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

Console.WriteLine("PASS Track 4 AppServices explicit workload context, capture routing, route presentation and universal targeting are on-demand, stable-identity bound and fail-closed");
return 0;

static BoundGameEvidence Bound(
    string gameId,
    GameEvidenceKind kind,
    int? processId,
    string? executablePath)
    => new()
    {
        GameId = gameId,
        BindingReason = GameEvidenceBindingReason.ExactGameIdHint,
        SourceId = kind == GameEvidenceKind.RunningProcess ? "running-process" : "app-paths",
        Priority = kind == GameEvidenceKind.RunningProcess ? 40 : 30,
        Observation = new GameEvidenceObservation
        {
            ObservationId = $"{kind}:{gameId}:{processId}",
            Kind = kind,
            Confidence = 1,
            ObservedAtUtc = new DateTimeOffset(2026, 9, 9, 17, 30, 0, TimeSpan.Zero),
            GameIdHint = gameId,
            ProcessId = processId,
            ExecutablePath = executablePath,
            EvidenceText = "app universal capture fixture"
        }
    };

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
