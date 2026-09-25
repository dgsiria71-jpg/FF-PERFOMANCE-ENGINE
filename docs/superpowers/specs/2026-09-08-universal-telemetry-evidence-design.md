# DG Performance Engine — Universal Telemetry / Evidence Design

**Date:** 2026-09-08  
**Status:** approved by existing canonical architecture and autonomous continuation authority  
**Track:** Track 4 — Universal Telemetry / Evidence  
**Parent spec:** `docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`

## 1. Purpose

Track 4 generalizes the already-working FF/BlueStacks telemetry and performance evidence stack into one universal telemetry contract without discarding or rewriting the validated components that already exist.

The current branch already has:

- `TelemetrySample` as the legacy UI/performance sample model;
- `TelemetryService` for native CPU + physical-memory observations;
- `PresentMonService` for measured frame metrics;
- `PerformanceTimelineBuffer` and interval analysis;
- performance A/B evidence, quality gates and validated-evidence workflows;
- Track 3 stable `GameIdentity` plus bound/unbound runtime evidence.

The problem is not absence of telemetry. The problem is that telemetry semantics are currently implicit:

- metrics are fixed nullable properties on one record;
- `DataQuality` is free-form text;
- quality is mostly sample-wide rather than metric-specific;
- source/provenance is encoded in text such as `PresentMon · 600 frames`;
- performance capture targeting is still BlueStacks-specific;
- adding universal hardware channels would otherwise keep growing the legacy record and duplicate quality logic.

Track 4 introduces a typed additive schema around the existing collectors and consumers.

## 2. Architectural invariant

> One telemetry engine, many collectors, explicit per-metric provenance and quality, no fabricated values.

Consequences:

1. `TelemetrySample` remains source-compatible during migration.
2. New collectors publish schema-v2 frames instead of inventing new UI-specific models.
3. Missing channels are absent/unavailable, never zero-filled.
4. A numerical value without adequate provenance does not become `Measured` merely because it is finite.
5. `GameId` remains the durable workload identity; PID/path are runtime targeting evidence only.
6. PresentMon remains the frame-metric authority already used by Performance/A-B.
7. Performance, Guardian, Auto Tuner and Profiles must converge on shared telemetry contracts rather than independent FPS/hardware calculators.

## 3. Existing contracts preserved

The following remain valid and are not rewritten in the first Track 4 slice:

- `TelemetrySample`
- `TelemetryService.CaptureSystemSample()`
- `PresentMonService.CaptureProcessAsync(...)`
- `PerformanceTimelineBuffer`
- `PerformanceEvidenceSnapshot`
- Global Controlled Benchmark Lease and all Track 2 evidence/freshness gates
- Track 3 `GameIdentity`, `BoundGameEvidence` and `UnboundGameEvidence`

No existing caller is forced to migrate in one commit.

## 4. Metric schema v2

### 4.1 Metric identity

Metrics use stable canonical ids, not localized UI labels.

Initial standard ids:

```text
frame.fps.avg
frame.fps.low1
frame.fps.low01
frame.time.avg_ms
frame.time.p95_ms
frame.time.p99_ms
frame.stutter.percent
frame.latency.avg_ms
system.cpu.utilization.percent
system.memory.used_gb
system.memory.total_gb
system.gpu.utilization.percent
thermal.cpu.celsius
thermal.gpu.celsius
network.ping.ms
network.jitter.ms
network.packet_loss.percent
```

The first implementation exposes these through constants/strong helpers while keeping the underlying id as a normalized string so future vendor/hardware adapters can add namespaced channels without changing a central enum every time.

Rules:

- id is lowercase ASCII-style dotted namespace;
- blank or malformed ids are rejected;
- metric id never embeds machine/game/process identity;
- unit is part of the descriptor, not encoded only in display text.

### 4.2 Metric descriptor

```text
TelemetryMetricDescriptor
- Id
- Unit
- Domain
- Aggregation
```

Initial domains:

```text
Frame
System
Thermal
Network
Other
```

Initial aggregation semantics:

```text
Gauge
Average
Minimum
Maximum
Sum
```

The descriptor describes semantics, not current availability.

### 4.3 Metric observation

```text
TelemetryMetricObservation
- MetricId
- Value
- Quality
- Coverage
- SourceId
- Origin
```

Requirements:

- `Value` must be finite;
- stored numeric observations may have `Partial` or `Measured` quality only;
- `Unavailable` is represented by absence from the frame and by typed lookup/frame-summary results, never by pairing an unavailable quality with a fake numeric value;
- `Coverage` is `[0,1]` and represents the producer's valid coverage of its own measurement window/sample, not confidence and not a cross-source score;
- the first compatibility bridge uses `Coverage = 1` for a finite value emitted from one accepted legacy sample; quality/provenance still determine whether that value is `Measured` or `Partial`;
- `SourceId` is nonblank normalized provenance such as `presentmon`, `native-system`, `legacy-bridge`;
- duplicate metric ids inside one frame are rejected in the first slice rather than silently averaged;
- a value does not carry a fake default when unavailable.

## 5. Typed data quality

### 5.1 Quality level

Initial enum:

```text
Unavailable
Partial
Measured
```

Meaning:

- `Unavailable`: no trustworthy numeric observation exists for the requested channel; it is a lookup/frame state, not a stored numeric observation;
- `Partial`: a numeric observation exists but provenance/completeness is insufficient for direct measured authority;
- `Measured`: direct measurement from an accepted collector with sufficient collector-local coverage for that metric.

The initial schema deliberately does not add an `Estimated` level because the current product has no approved estimator contract. If estimators are added later, they receive a separate explicit provenance/quality rule rather than being hidden under `Measured`.

### 5.2 Origin

```text
TelemetryMetricOrigin
- Direct
- Derived
- Legacy
```

`Derived` is not automatically bad, but derivation must be explicit. For example, frame time derived as `1000 / FPS` is not identical provenance to frame times read from PresentMon rows.

### 5.3 Quality is per metric

A frame can legitimately contain:

```text
CPU utilization       Measured / native-system
memory used           Measured / native-system
FPS                    Measured / presentmon
latency                absent → Unavailable on lookup
GPU utilization        absent → Unavailable on lookup
```

No sample-wide string is allowed to upgrade all metrics together.

### 5.4 Frame quality summary

`TelemetryFrame.FrameQuality` is computed only:

- no metrics → `Unavailable`;
- all metrics `Measured` → `Measured`;
- otherwise → `Partial`.

It is presentation/diagnostic summary only and cannot increase a metric's own quality.

## 6. Telemetry frame v2

```text
TelemetryFrame
- Timestamp
- Workload
- Metrics
- FrameQuality
```

Construction rules:

- metric list is copied/immutable from the frame's point of view;
- duplicate normalized metric ids are rejected;
- frame timestamp is explicit and preserved from the producer/legacy sample;
- `TryGetMetric`/lookup returns absence rather than synthesizing zero.

### 6.1 Workload context

```text
TelemetryWorkloadTarget
- GameId: string?
- ProcessId: int?
- ExecutablePath: string?
- BindingQuality
```

Rules:

- `GameId` is stable Track 3 identity when known;
- `ProcessId` and executable path are runtime evidence, never durable identity;
- no process name/display name becomes GameId;
- a frame may be system-only with no workload target.

Initial binding states:

```text
SystemOnly
ExactRunningProcess
AmbiguousRunningProcess
UnavailableRunningProcess
UnknownGame
```

`UnknownGame` means a caller requested a GameId not present in the stable discovered catalog. Such an input is not echoed back as if it were proven stable identity.

## 7. Universal workload-target resolver

Track 4 consumes Track 3 `BoundGameEvidence` instead of inventing another process finder.

For a requested stable `GameId`:

1. trim/lowercase the request and locate exactly one matching stable game in `ResolvedGameCatalogResult`;
2. if the GameId is blank or absent from the stable catalog, return `UnknownGame` with no process/path and no promoted GameId;
3. consider only bound evidence for that stable GameId whose `Observation.Kind == RunningProcess`;
4. require positive PID and fully-qualified executable path;
5. group by distinct PID; duplicate observations of the same PID do not create ambiguity;
6. exactly one valid running PID → `ExactRunningProcess` with stable canonical GameId + PID/path;
7. zero running PIDs → `UnavailableRunningProcess` with stable canonical GameId but no PID/path;
8. more than one distinct running PID → `AmbiguousRunningProcess` with stable canonical GameId but no selected PID/path; do not guess highest PID, newest PID, biggest working set or filename;
9. `KnownExecutable`/App Paths evidence can never supply a live PID or convert `UnavailableRunningProcess` into exact running state.

This resolver is pure Core logic and performs no process enumeration. Discovery already produced the evidence.

## 8. Legacy compatibility bridge

### 8.1 Goal

Existing producers and screens continue using `TelemetrySample` while Track 4 consumers can receive typed v2 frames.

The first bridge direction is:

```text
TelemetrySample → TelemetryFrame
```

It is intentionally conservative.

### 8.2 Legacy quality mapping

The bridge maps known historical labels without widening their authority:

- `PresentMon · <n> frames`:
  - finite frame/FPS metrics → `Measured`, source `presentmon`, origin `Direct`, coverage `1` for that emitted legacy sample;
  - unrelated populated fields not proven by the label remain `Partial`, source `legacy-bridge`, origin `Legacy`.
- `System`:
  - CPU/memory metrics → `Measured`, source `native-system`, origin `Direct`, coverage `1` for that emitted legacy sample;
  - unrelated populated fields remain `Partial`.
- `Frame+System`:
  - system CPU/memory channels may be `Measured` when present;
  - frame values are `Partial`/`Legacy` because the label does not prove which collector supplied the FPS argument.
- exact historical `Measured`:
  - finite frame metrics retain compatibility as `Measured`, source `legacy-measured`, origin `Legacy`, because current A/B logic already accepts that label as direct historical measured evidence;
  - unrelated populated non-frame fields remain `Partial` unless another explicit legacy label proves their source.
- any unknown/free-form label:
  - finite values become `Partial`, source `legacy-bridge`, origin `Legacy`.

The bridge never converts null/NaN/infinity into numeric zero.

### 8.3 Standard legacy field mapping

Every currently defined numeric `TelemetrySample` field has an explicit standard metric id. No reflection-based property-name-to-id convention is used.

### 8.4 Legacy model remains immutable in migration

The first slice does not add v2 properties to `TelemetrySample` and does not change its serialized shape. This avoids accidental History/UI compatibility breakage.

## 9. Current collector migration

After schema/bridge GREEN:

### 9.1 PresentMon

Create a v2 adapter/path that emits direct measured frame observations with explicit source id and coverage derived from accepted frame rows/window quality.

Do not remove `ParseCsv()` legacy output until all current consumers are migrated.

### 9.2 Native system telemetry

CPU and memory native observations become v2 system metrics. The first CPU sample after process start may legitimately be unavailable because utilization requires a prior time baseline.

### 9.3 Future hardware channels

GPU, VRAM, clocks, thermals, per-core CPU, I/O and network are separate collector slices. Each must prove capability/availability and quality independently.

## 10. Realtime storage / aggregation

The canonical pipeline remains:

```text
Collectors
→ realtime ring buffer
→ aggregator
→ 1 s aggregates
→ 10 s aggregates
→ session store
→ long-term summaries
```

The first Track 4 implementation does not persist raw v2 frames to disk.

A later slice introduces bounded ring buffers and typed aggregators. Aggregation must honor metric semantics and quality/coverage; it must not average incompatible metrics or silently blend unavailable samples.

## 11. Performance / A-B compatibility

Existing `PerformanceEvidenceSnapshot` remains authoritative during migration.

Rules:

- existing A/B tests and quality rules must remain green;
- v2 typed quality must not weaken `Measured` requirements;
- legacy PresentMon labels continue to be recognized until the Performance pipeline consumes v2 quality directly;
- `Observed != Validated` remains unchanged;
- recommendation authority still requires the existing Track 2 freshness/fingerprint/validation chain.

Track 4 can later make A/B quality checks consume typed metric quality instead of parsing strings, but that migration receives its own RED/GREEN slice.

## 12. Universal configuration snapshot

The Track 4 end state extends A/B context beyond BlueStacks-specific configuration using an additive universal workload/configuration envelope:

```text
Machine fingerprint
Windows state
GameId/workload
Adapter id/version
Relevant system capability values
Game/emulator configuration when known
Display/driver context when material
```

This does not replace the existing exact BlueStacks `PerformanceConfigurationSnapshot` until compatibility tests prove the migration.

## 13. Failure isolation

- one collector failure does not fabricate unavailable channels;
- malformed metric observations are rejected at the schema boundary;
- cancellation propagates;
- unknown metric/source ids are not accepted if blank/malformed;
- duplicate metric observations in one frame are explicit errors in the first slice rather than hidden averaging;
- ambiguous workload process targeting prevents process-specific capture but still permits system-only telemetry;
- a stale PID is not converted into a different process by guessing.

## 14. Implementation sequence

### Slice A — Schema v2 + typed quality + legacy bridge

RED first for:

1. standard metric ids/descriptors are stable;
2. invalid/malformed metric ids and source ids are rejected;
3. non-finite metric values are rejected;
4. coverage outside `[0,1]` is rejected;
5. numeric observations with `Unavailable` quality are rejected;
6. duplicate metric ids in one frame are rejected;
7. frame-quality summary is computed, never caller-promoted;
8. PresentMon legacy frame metrics map to measured while unrelated fields do not;
9. `System` maps CPU/memory only to measured;
10. `Frame+System` does not promote frame metrics to measured;
11. unknown labels map populated fields to Partial;
12. null/nonfinite legacy values produce no metric;
13. `TelemetrySample` source compatibility remains intact.

### Slice B — Universal workload target resolver

RED first for:

1. one bound RunningProcess PID resolves exactly;
2. duplicate evidence for one PID does not create ambiguity;
3. KnownExecutable evidence never supplies a PID;
4. zero running evidence returns `UnavailableRunningProcess` with the proven GameId;
5. multiple distinct PIDs are ambiguous and no PID/path is selected;
6. evidence for another GameId is ignored;
7. invalid PID/path evidence is ignored;
8. unknown/blank requested GameId returns `UnknownGame` and is not promoted;
9. stable canonical GameId remains unchanged.

### Slice C — Current collector adapters

- native system sample → v2 metrics;
- PresentMon sample → v2 measured frame metrics with provenance;
- preserve old APIs;
- full Windows CI after each adapter.

### Slice D — Ring buffer + aggregation

- bounded realtime v2 buffer;
- deterministic 1-second aggregate;
- later 10-second/session aggregation;
- quality/coverage-aware semantics.

### Slice E — Universal hardware channels

Add proven channels one provider at a time, prioritizing values that materially improve the Bottleneck Analyzer and Auto Tuner. No sensor is claimed merely because a vendor commonly exposes it.

### Slice F — A/B typed-quality migration + universal configuration context

Only after v2 collectors are stable, migrate performance evidence away from free-form `DataQuality` parsing and extend configuration context. Preserve old history through explicit rehydration compatibility.

## 15. Track 3 closure

Track 3 original exit criteria are satisfied before Track 4 begins:

- stable `GameIdentity` ✅
- local catalog ✅
- generic + specialized adapter framework ✅
- major launcher/package scanners ✅
- FF/BlueStacks encapsulated as specialized adapter ✅
- two-plane non-authoritative runtime evidence ✅
- running-process evidence ✅
- known-executable App Paths evidence ✅

Additional discovery sources remain allowed later when they add a proven signal, but they are no longer a blocker for Universal Telemetry.

## 16. Non-goals of the first Track 4 milestone

- no replacement of `TelemetrySample`;
- no mass UI rewrite;
- no GPU/temperature sensor fabrication;
- no new disk telemetry database yet;
- no automatic process priority/affinity changes;
- no fuzzy process-to-game matching;
- no change to validated recommendation authority;
- no modification to controlled benchmark lease semantics;
- no startup game/process discovery.

## 17. Success criteria

The first Track 4 milestone is complete when:

1. a typed v2 metric frame exists alongside legacy telemetry;
2. every stored numeric metric carries explicit source, quality, coverage and origin;
3. unavailable metrics are represented by absence/typed lookup state rather than fake numeric values;
4. legacy telemetry can be conservatively transformed without fabricating measurements;
5. a stable game can resolve an exact runtime PID only through unambiguous Track 3 bound RunningProcess evidence;
6. unknown GameId input cannot be promoted to stable workload identity;
7. current Performance/A-B/Guardian/AutoTuner tests remain green;
8. no discovery is added to application startup;
9. exact commits receive fresh full Windows CI before GREEN claims.
