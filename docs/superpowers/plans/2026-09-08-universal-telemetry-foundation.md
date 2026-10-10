# Universal Telemetry Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the first Track 4 typed telemetry foundation—metric schema v2, per-metric quality/provenance, conservative legacy conversion, and safe workload PID resolution from Track 3 evidence—without replacing existing telemetry or weakening A/B evidence.

**Architecture:** Add a focused `FFPerformanceEngine.Core.Telemetry` namespace beside existing legacy `Models/TelemetrySample.cs`. The v2 schema is additive and immutable-by-construction; `TelemetryLegacyBridge` explicitly maps every legacy field; `TelemetryWorkloadTargetResolver` consumes already-bound Track 3 running-process evidence and performs no platform I/O. Existing `TelemetryService`, `PresentMonService`, Performance Timeline and A/B contracts remain unchanged in this plan.

**Tech Stack:** C#/.NET 8 Core library, existing console self-test project, Windows CI (`windows-2022`), WPF solution regression build.

**Spec:** `docs/superpowers/specs/2026-09-08-universal-telemetry-evidence-design.md`

## Global Constraints

- Evolution without restart: preserve existing `TelemetrySample`, `TelemetryService`, `PresentMonService`, `PerformanceTimelineBuffer` and A/B evidence APIs.
- No fabricated values: null/NaN/infinity do not become zero or a metric.
- Stored numeric observations use `Partial` or `Measured`; `Unavailable` is absence/lookup/frame state.
- Every stored metric carries explicit source, quality, coverage and origin.
- `GameId` remains stable Track 3 identity; PID/path are runtime evidence only.
- `KnownExecutable` evidence can never claim a live PID.
- Ambiguous process evidence stays ambiguous; never pick highest/newest PID or guess by filename.
- `AppServices.InitializeAsync()` remains free of game/process discovery.
- Existing Track 2 `Observed != Validated`, freshness, fingerprint, benchmark lease and recommendation authority are unchanged.
- Every code task uses RED → intended failure → minimal GREEN → fresh full Windows CI.

---

## File Structure

New Core folder:

```text
src/FFPerformanceEngine.Core/Telemetry/
├── TelemetryMetricSchema.cs
├── TelemetryFrame.cs
├── TelemetryLegacyBridge.cs
└── TelemetryWorkloadTargetResolver.cs
```

New self-tests:

```text
tests/FFPerformanceEngine.Core.SelfTest/
├── TelemetryMetricSchemaV2SelfTests.cs
├── TelemetryLegacyBridgeSelfTests.cs
└── TelemetryWorkloadTargetResolverSelfTests.cs
```

No existing runtime file is modified until a later collector-composition plan.

---

### Task 1: Metric schema v2 and immutable telemetry frame

**Files:**
- Test: `tests/FFPerformanceEngine.Core.SelfTest/TelemetryMetricSchemaV2SelfTests.cs`
- Create: `src/FFPerformanceEngine.Core/Telemetry/TelemetryMetricSchema.cs`
- Create: `src/FFPerformanceEngine.Core/Telemetry/TelemetryFrame.cs`

**Interfaces:**

Produces:

```csharp
namespace FFPerformanceEngine.Core.Telemetry;

public enum TelemetryMetricDomain { Frame, System, Thermal, Network, Other }
public enum TelemetryAggregationKind { Gauge, Average, Minimum, Maximum, Sum }
public enum TelemetryUnit { FramesPerSecond, Milliseconds, Percent, Gibibytes, Celsius, Count }
public enum TelemetryMetricQuality { Unavailable, Partial, Measured }
public enum TelemetryMetricOrigin { Direct, Derived, Legacy }

public sealed record TelemetryMetricDescriptor
{
    public string Id { get; }
    public TelemetryUnit Unit { get; }
    public TelemetryMetricDomain Domain { get; }
    public TelemetryAggregationKind Aggregation { get; }

    public TelemetryMetricDescriptor(
        string id,
        TelemetryUnit unit,
        TelemetryMetricDomain domain,
        TelemetryAggregationKind aggregation);
}

public static class TelemetryStandardMetrics
{
    public static readonly TelemetryMetricDescriptor FrameFpsAverage;
    public static readonly TelemetryMetricDescriptor FrameFpsLow1;
    public static readonly TelemetryMetricDescriptor FrameFpsLow01;
    public static readonly TelemetryMetricDescriptor FrameTimeAverageMs;
    public static readonly TelemetryMetricDescriptor FrameTimeP95Ms;
    public static readonly TelemetryMetricDescriptor FrameTimeP99Ms;
    public static readonly TelemetryMetricDescriptor FrameStutterPercent;
    public static readonly TelemetryMetricDescriptor FrameLatencyAverageMs;
    public static readonly TelemetryMetricDescriptor SystemCpuUtilizationPercent;
    public static readonly TelemetryMetricDescriptor SystemMemoryUsedGb;
    public static readonly TelemetryMetricDescriptor SystemMemoryTotalGb;
    public static readonly TelemetryMetricDescriptor SystemGpuUtilizationPercent;
    public static readonly TelemetryMetricDescriptor CpuTemperatureCelsius;
    public static readonly TelemetryMetricDescriptor GpuTemperatureCelsius;
    public static readonly TelemetryMetricDescriptor NetworkPingMs;
    public static readonly TelemetryMetricDescriptor NetworkJitterMs;
    public static readonly TelemetryMetricDescriptor NetworkPacketLossPercent;
    public static IReadOnlyList<TelemetryMetricDescriptor> All { get; }
}

public sealed record TelemetryMetricObservation
{
    public TelemetryMetricDescriptor Metric { get; }
    public double Value { get; }
    public TelemetryMetricQuality Quality { get; }
    public double Coverage { get; }
    public string SourceId { get; }
    public TelemetryMetricOrigin Origin { get; }

    public TelemetryMetricObservation(
        TelemetryMetricDescriptor metric,
        double value,
        TelemetryMetricQuality quality,
        double coverage,
        string sourceId,
        TelemetryMetricOrigin origin);
}

public sealed record TelemetryFrame
{
    public DateTimeOffset Timestamp { get; }
    public IReadOnlyList<TelemetryMetricObservation> Metrics { get; }
    public TelemetryMetricQuality FrameQuality { get; }

    public TelemetryFrame(
        DateTimeOffset timestamp,
        IEnumerable<TelemetryMetricObservation> metrics);

    public bool TryGetMetric(string metricId, out TelemetryMetricObservation? observation);
}
```

Validation rules:

```csharp
Metric id pattern: ^[a-z][a-z0-9]*(?:[._][a-z0-9]+)*$
Source id pattern: ^[a-z0-9][a-z0-9._-]*$
```

`TelemetryMetricDescriptor` normalizes `id.Trim().ToLowerInvariant()` and rejects blank/malformed ids.

`TelemetryMetricObservation`:

```csharp
ArgumentNullException.ThrowIfNull(metric);
if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
if (quality == TelemetryMetricQuality.Unavailable)
    throw new ArgumentException("Numeric telemetry cannot be Unavailable.", nameof(quality));
if (!double.IsFinite(coverage) || coverage < 0 || coverage > 1)
    throw new ArgumentOutOfRangeException(nameof(coverage));
sourceId = sourceId.Trim().ToLowerInvariant();
if (!SourcePattern.IsMatch(sourceId)) throw new ArgumentException(...);
```

`TelemetryFrame` copies observations, rejects duplicate normalized `Metric.Id`, sorts by `Metric.Id` ordinal, computes quality:

```text
0 metrics -> Unavailable
all Measured -> Measured
otherwise -> Partial
```

- [ ] **Step 1: Write RED self-test**

Create a synchronous ModuleInitializer test with 30-second watchdog. Required assertions:

```csharp
Require(TelemetryStandardMetrics.FrameFpsAverage.Id == "frame.fps.avg");
Require(TelemetryStandardMetrics.SystemCpuUtilizationPercent.Id == "system.cpu.utilization.percent");
Require(TelemetryStandardMetrics.All.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count()
        == TelemetryStandardMetrics.All.Count);

Expect<ArgumentException>(() => new TelemetryMetricDescriptor("Frame FPS", ...));
Expect<ArgumentOutOfRangeException>(() => Observation(double.NaN, ...));
Expect<ArgumentOutOfRangeException>(() => Observation(1, coverage: 1.01, ...));
Expect<ArgumentException>(() => Observation(1, quality: TelemetryMetricQuality.Unavailable, ...));
Expect<ArgumentException>(() => Observation(1, sourceId: "Present Mon!", ...));

var measured = Observation(120, Measured, FrameFpsAverage);
var partial = Observation(8.3, Partial, FrameTimeAverageMs);
Require(new TelemetryFrame(now, Array.Empty<TelemetryMetricObservation>()).FrameQuality == Unavailable);
Require(new TelemetryFrame(now, [measured]).FrameQuality == Measured);
Require(new TelemetryFrame(now, [measured, partial]).FrameQuality == Partial);
Expect<ArgumentException>(() => new TelemetryFrame(now, [measured, measured]));
Require(frame.TryGetMetric(" FRAME.FPS.AVG ", out var found) && ReferenceEquals(found, measured));
```

Also assert frame metrics are returned in canonical id order and that mutating the original input list after construction cannot alter `frame.Metrics`.

- [ ] **Step 2: Commit RED and verify Windows CI**

Target commit: `test: define telemetry metric schema v2 contract`.

Expected intended failure: managed build fails only because `FFPerformanceEngine.Core.Telemetry` contracts do not exist. Native configure/build/test must remain GREEN.

- [ ] **Step 3: Implement minimal schema/frame**

Use `System.Text.RegularExpressions.GeneratedRegex` only if it keeps the code simpler; otherwise use two compiled static `Regex` instances. Do not add packages.

Standard descriptors use these ids/units/domains:

```text
frame.fps.avg                FramesPerSecond / Frame
frame.fps.low1               FramesPerSecond / Frame
frame.fps.low01              FramesPerSecond / Frame
frame.time.avg_ms            Milliseconds / Frame
frame.time.p95_ms            Milliseconds / Frame
frame.time.p99_ms            Milliseconds / Frame
frame.stutter.percent        Percent / Frame
frame.latency.avg_ms         Milliseconds / Frame
system.cpu.utilization.percent Percent / System
system.memory.used_gb        Gibibytes / System
system.memory.total_gb       Gibibytes / System
system.gpu.utilization.percent Percent / System
thermal.cpu.celsius          Celsius / Thermal
thermal.gpu.celsius          Celsius / Thermal
network.ping.ms              Milliseconds / Network
network.jitter.ms            Milliseconds / Network
network.packet_loss.percent  Percent / Network
```

Use `Gauge` for pre-aggregated/window statistics and system/thermal/network observations in this first milestone; aggregation semantics can be specialized in the later aggregation plan without changing metric ids.

- [ ] **Step 4: Require full Windows CI GREEN**

Required steps: native configure/build/test, managed build, Core self-tests, publish, artifact, Complete job.

- [ ] **Step 5: Checkpoint**

Target: `feat: add typed telemetry metric schema v2`.

---

### Task 2: Conservative legacy TelemetrySample bridge

**Files:**
- Test: `tests/FFPerformanceEngine.Core.SelfTest/TelemetryLegacyBridgeSelfTests.cs`
- Create: `src/FFPerformanceEngine.Core/Telemetry/TelemetryLegacyBridge.cs`
- Read-only compatibility reference: `src/FFPerformanceEngine.Core/Models/TelemetrySample.cs`

**Consumes:** Task 1 metric/frame contracts and existing `FFPerformanceEngine.Core.Models.TelemetrySample`.

**Produces:**

```csharp
public static class TelemetryLegacyBridge
{
    public static TelemetryFrame FromLegacy(TelemetrySample sample);
}
```

Explicit field mapping, no reflection:

```text
Fps                  -> FrameFpsAverage
OnePercentLow        -> FrameFpsLow1
PointOnePercentLow   -> FrameFpsLow01
FrameTimeMs          -> FrameTimeAverageMs
FrameTimeP95Ms       -> FrameTimeP95Ms
FrameTimeP99Ms       -> FrameTimeP99Ms
StutterPercent       -> FrameStutterPercent
LatencyMs            -> FrameLatencyAverageMs
CpuPercent           -> SystemCpuUtilizationPercent
GpuPercent           -> SystemGpuUtilizationPercent
MemoryUsedGb         -> SystemMemoryUsedGb
MemoryTotalGb        -> SystemMemoryTotalGb
CpuTemperatureC      -> CpuTemperatureCelsius
GpuTemperatureC      -> GpuTemperatureCelsius
PingMs               -> NetworkPingMs
JitterMs             -> NetworkJitterMs
PacketLossPercent    -> NetworkPacketLossPercent
```

Finite legacy values only. Every emitted observation has coverage `1`.

Quality/provenance function accepts descriptor + normalized `DataQuality`:

```text
PresentMon · ... + Frame domain
  -> Measured / presentmon / Direct
System + CPU or Memory metric ids only
  -> Measured / native-system / Direct
Frame+System + CPU or Memory metric ids only
  -> Measured / native-system / Direct
Measured + Frame domain
  -> Measured / legacy-measured / Legacy
anything else
  -> Partial / legacy-bridge / Legacy
```

Important: `System`/`Frame+System` do not upgrade GPU/thermal/network metrics because the current native `TelemetryService` proves only CPU + physical memory. PresentMon labels do not upgrade CPU/GPU/memory/network values.

- [ ] **Step 1: Write RED self-test**

Cases:

1. PresentMon sample with FPS/frame-time/latency + an injected CPU value:
   - frame metrics Measured/presentmon/Direct;
   - CPU Partial/legacy-bridge/Legacy.
2. `System` sample with CPU/memory + injected GPU/temp:
   - CPU/memory Measured/native-system/Direct;
   - GPU/temp Partial.
3. `Frame+System` with FPS + CPU:
   - CPU Measured/native-system;
   - FPS Partial/legacy-bridge/Legacy.
4. exact `Measured` with FPS + CPU:
   - FPS Measured/legacy-measured/Legacy;
   - CPU Partial.
5. unknown label with finite values:
   - all Partial/legacy-bridge/Legacy.
6. null/NaN/infinity fields:
   - no corresponding metrics.
7. timestamp exactly preserved.
8. Verify `TelemetrySample` still compiles/constructs with its current properties and no v2 field is required.

- [ ] **Step 2: Commit RED and verify intended failure**

Target: `test: define legacy telemetry v2 bridge contract`.

Expected: missing `TelemetryLegacyBridge` only; Task 1 schema remains GREEN.

- [ ] **Step 3: Implement bridge**

Use an explicit `AddIfFinite` helper:

```csharp
private static void AddIfFinite(
    ICollection<TelemetryMetricObservation> output,
    TelemetryMetricDescriptor descriptor,
    double? value,
    string dataQuality)
{
    if (value is not double number || !double.IsFinite(number)) return;
    var policy = ResolveLegacyPolicy(descriptor, dataQuality);
    output.Add(new TelemetryMetricObservation(
        descriptor,
        number,
        policy.Quality,
        1d,
        policy.SourceId,
        policy.Origin));
}
```

No writes back to the source sample.

- [ ] **Step 4: Require full Windows CI GREEN**

- [ ] **Step 5: Checkpoint**

Target: `feat: bridge legacy telemetry into schema v2`.

---

### Task 3: Universal workload target resolver from bound evidence

**Files:**
- Test: `tests/FFPerformanceEngine.Core.SelfTest/TelemetryWorkloadTargetResolverSelfTests.cs`
- Create: `src/FFPerformanceEngine.Core/Telemetry/TelemetryWorkloadTargetResolver.cs`
- Read: `src/FFPerformanceEngine.Core/Workloads/GameDiscoveryCoordinator.cs`
- Read: `src/FFPerformanceEngine.Core/Workloads/GameEvidenceModels.cs`

**Consumes:** `ResolvedGameCatalogResult`, `GameEvidenceKind.RunningProcess`, stable `GameIdentity.GameId`.

**Produces:**

```csharp
public enum TelemetryWorkloadBindingQuality
{
    SystemOnly,
    ExactRunningProcess,
    AmbiguousRunningProcess,
    UnavailableRunningProcess,
    UnknownGame
}

public sealed record TelemetryWorkloadTarget
{
    public string? GameId { get; init; }
    public int? ProcessId { get; init; }
    public string? ExecutablePath { get; init; }
    public TelemetryWorkloadBindingQuality BindingQuality { get; init; }

    public bool CanCaptureProcess
        => BindingQuality == TelemetryWorkloadBindingQuality.ExactRunningProcess
           && ProcessId is > 0
           && !string.IsNullOrWhiteSpace(ExecutablePath);

    public static TelemetryWorkloadTarget SystemOnly { get; }
}

public sealed class TelemetryWorkloadTargetResolver
{
    public TelemetryWorkloadTarget Resolve(
        ResolvedGameCatalogResult catalog,
        string? requestedGameId);
}
```

Path validation uses `Path.IsPathFullyQualified` + `Path.GetFullPath`, rejecting invalid paths. Windows comparison is case-insensitive.

Canonical algorithm:

```text
blank requested id -> UnknownGame, no GameId/PID/path
find normalized requested id in catalog.Games.Identity.GameId
0 or >1 matches -> UnknownGame, no promoted GameId
canonical GameId = matched Identity.GameId
filter BoundEvidence by canonical GameId + RunningProcess
filter PID > 0 + fully-qualified normalizable path
group by PID
for each PID, normalize distinct paths case-insensitively
if any one PID has >1 distinct path -> AmbiguousRunningProcess
0 valid PID groups -> UnavailableRunningProcess + canonical GameId
>1 valid PID groups -> AmbiguousRunningProcess + canonical GameId
1 PID group with 1 path -> ExactRunningProcess + canonical GameId + PID + path
```

`KnownExecutable` evidence is ignored even if it has an executable path.

- [ ] **Step 1: Write RED self-test**

Build stable catalog entries for `steam:100` and `epic:200` using generic adapters. Cases:

```text
one RunningProcess PID 77 for steam:100 -> exact PID/path
same PID 77 duplicated with same path/case variant -> still exact
PID 77 with two conflicting paths -> ambiguous, no PID/path
PID 77 + PID 88 -> ambiguous, no PID/path
KnownExecutable path only -> unavailable running process
RunningProcess evidence for epic:200 only -> steam:100 unavailable
invalid PID/path -> ignored -> unavailable
blank/unknown requested id -> UnknownGame and GameId null
```

Assert the resolver never modifies catalog games/evidence.

- [ ] **Step 2: Commit RED and verify intended failure**

Target: `test: define universal telemetry workload target contract`.

Expected: missing resolver/target contracts only.

- [ ] **Step 3: Implement pure Core resolver**

No `Process.GetProcesses`, no filesystem existence checks, no Guardian dependency, no side effects.

- [ ] **Step 4: Require full Windows CI GREEN**

- [ ] **Step 5: Checkpoint**

Target: `feat: resolve telemetry targets from bound game evidence`.

---

### Task 4: Track transition + canonical memory checkpoint

**Files:**
- Modify atomically: `docs/project-memory/HANDOFF_CURRENT.md`
- Modify atomically: `docs/project-memory/IMPLEMENTATION_STATUS.md`
- Modify: `docs/project-memory/ROADMAP.md`

**Requirements:**

Update roadmap state:

```text
Track 3 — Game Discovery + Adapter Framework — GREEN
Track 4 — Universal Telemetry / Evidence — ACTIVE
```

Do not delete Track 3 details; record that additional discovery surfaces are optional future enrichment rather than a Track 4 blocker.

Record exact RED/GREEN SHAs and CI numbers for Tasks 1–3, plus invariants:

- legacy `TelemetrySample` preserved;
- per-metric typed quality/provenance exists;
- unavailable metrics are absent, not zero;
- unknown GameId is not promoted;
- only unambiguous bound RunningProcess evidence yields a PID;
- App Paths/KnownExecutable never yields a live PID;
- existing A/B validated-evidence authority unchanged;
- no startup discovery added.

- [ ] **Step 1: Fetch exact current blobs/tree and prepare all three documents**

Use Git Data blobs + `create_tree` + `create_commit` so memory + roadmap transition lands in one atomic commit.

- [ ] **Step 2: Inspect compare against application head**

Expected files only:

```text
docs/project-memory/HANDOFF_CURRENT.md
docs/project-memory/IMPLEMENTATION_STATUS.md
docs/project-memory/ROADMAP.md
```

- [ ] **Step 3: Require Windows CI GREEN on exact documentation HEAD**

Only then claim the first Track 4 foundation plan complete.

Target commit: `docs: activate Track 4 telemetry foundation`.

---

## Self-Review Result

- Spec coverage for the first milestone: schema v2, typed quality, no fake unavailable values, explicit legacy compatibility, universal target binding, Track 3 closure and Track 4 activation all have tasks.
- Deliberately deferred to later plans: direct collector v2 APIs, ring buffers/aggregation, new hardware sensors/channels, A/B typed-quality migration and universal configuration snapshot. These require the foundation here but can be reviewed/validated independently.
- Placeholder scan: no TBD/TODO/unnamed implementation hook.
- Type consistency: Tasks 2 and 3 consume only Task 1 or already-GREEN Track 3 types. No nullability-only overloads or undefined comparers.
- Scope protection: no UI rewrite, no startup discovery, no new sensor claims, no change to recommendation authority.
