# Current Handoff — 2026-09-08

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR: `#1` — open, draft, not merged
- PR base: `main`

## Last verified application-code checkpoint

- Application HEAD: `f5265286480662ffcfd3f89fbb03a1cd31a09e59`
- Commit: `feat: add quality aware telemetry v2 aggregation`
- Windows CI: **#928 — SUCCESS**
- CI run id: `34279297336`

The exact #928 job passed native configure, C++ build, native tests, managed/WPF build, Core self-tests, `win-x64` publish, artifact upload and final job completion.

Repository-native continuity remains in root `AGENTS.md` and `docs/project-memory/`. Current code/tests + fresh exact-commit CI outrank stale documentation.

## Track state

**Track 3 — Game Discovery + Adapter Framework — GREEN.**

**Track 4 — Universal Telemetry / Evidence — ACTIVE.**

Track 3 exit criteria remain satisfied: stable launcher-native GameIdentity/catalog, generic + specialized adapters, BlueStacks/Steam/Epic/Riot/Battle.net/EA/Ubisoft/Microsoft Store-Xbox discovery, separate identity/evidence planes, deterministic binder, running-process evidence and Windows App Paths KnownExecutable evidence. Additional discovery is optional enrichment and is not a Track 4 blocker.

`AppServices.InitializeAsync()` still performs no game/package/process/App-Paths discovery. Explicit game/evidence discovery authority remains `AppServices.DiscoverGamesAsync()`.

## Track 4 foundation — GREEN

Canonical design and plans:

- `docs/superpowers/specs/2026-09-08-universal-telemetry-evidence-design.md`
- `docs/superpowers/plans/2026-09-08-universal-telemetry-foundation.md`
- `docs/superpowers/plans/2026-09-08-telemetry-v2-current-collectors.md`
- `docs/superpowers/plans/2026-09-08-telemetry-v2-realtime-buffer-aggregation.md`

### Schema v2

- RED `86953dca06fd278383d35f5a9202371dfe1a0410` → CI #878 / run `34273684420`.
- GREEN `4ced969a17b41e4cad56c9e413c626d9cd2326d6` → CI #880 / run `34275125229` SUCCESS.

Typed telemetry has stable metric descriptors/ids, domain/unit/aggregation, per-metric quality, coverage, source provenance and origin. Stored numeric observations must be finite; `Unavailable` is represented by absence rather than a fake number. `TelemetryFrame` copies/sorts observations, rejects duplicate metric ids and computes frame summary quality without upgrading individual metrics.

### Legacy compatibility bridge

- RED `9a48b5c7719d4f131819fb0e7b0aecb0add37be9` → CI #882 / run `34275316122`.
- GREEN `476df79441e0c8770f23f260a2824908595d461a` → CI #884 / run `34275483157` SUCCESS.

`TelemetryLegacyBridge` maps all 17 current `TelemetrySample` numeric fields explicitly and conservatively. `TelemetrySample` itself remains unchanged. PresentMon text proves only frame-domain values; `System`/`Frame+System` prove only the current native CPU + physical-memory channels; unknown labels and unrelated populated fields stay Partial; null/NaN/infinity are omitted.

### Universal workload target resolver

- RED `27271db72d5d7a99ad5b9b35ac203fd89ef4a6cd` → CI #886 / run `34275838695`.
- GREEN `8392892e7ad658928e7b7aca1719df2b64399125` → CI #888 / run `34276000513` SUCCESS.

Only exactly one stable catalog GameId plus exactly one unambiguous bound `RunningProcess` PID/path becomes process-capture eligible. Zero running evidence is unavailable; multiple PIDs or conflicting paths are ambiguous; `KnownExecutable`/App Paths can never supply a live PID. The resolver performs no process enumeration or filesystem probing.

## Current collector migration — GREEN

### Native system telemetry v2

- RED `e3c766e58b6c3c0ef5086c927bacf74745658e68` → CI #894 / run `34276841999`; native remained GREEN and managed failed with 0 warnings only on the missing v2 system contracts.
- GREEN `b9b1338828aa890380a8d689f8bb53ff737b517d` → CI #896 / run `34277078378` SUCCESS.

`TelemetryService` shares one native snapshot for legacy and v2 projections. `CaptureSystemSample()` remains source-compatible. `CaptureSystemFrame()` publishes only finite CPU utilization + physical-memory used/total values as `Measured / native-system / Direct / coverage 1`. No GPU, thermal, clock, I/O or network data is invented.

### PresentMon direct v2

- RED `053ec156bfa2a260e6a53ec14169b818a646f786` → CI #898 / run `34277312296`; native remained GREEN and managed failed with 0 warnings and only six missing API errors for `ParseCsvFrame` / `CaptureProcessFrameAsync`.
- GREEN `edfbba0845d60447e6fdd158b75eee6def39a60f` → CI #900 / run `34277780294` SUCCESS.

`ParseCsv()` and `ParseCsvFrame()` use the same internal PresentMon statistics object, preventing formula drift. Legacy FPS, 1% low, 0.1% low, frame-time average/P95/P99, stutter, latency and `DataQuality = "PresentMon · <n> frames"` behavior remain compatible. V2 emits only the eight proven frame/latency metrics, source `presentmon`, origin `Direct`, quality `Measured`, with frame coverage derived from accepted frame rows and latency coverage from accepted latency rows. Missing latency stays absent. One process capture request still invokes PresentMon only once because legacy/v2 share the same CSV-capture routine.

## Realtime storage / 1-second aggregation — GREEN

### Bounded `TelemetryFrame` ring buffer

- RED `24d09a17c60378b65c62314c697a1624d0f4e8f4` → CI #922 / run `34278694616`; native remained GREEN, managed failed with 0 warnings and exactly six errors, all because `TelemetryFrameRingBuffer` did not yet exist.
- GREEN `dddb6d2a28559115412629637c40187a64dc1e4f` → CI #924 / run `34278856955` SUCCESS.

`TelemetryFrameRingBuffer` is a pure in-memory bounded Core buffer. Capacity is validated, appends are lock-protected, retention eviction is FIFO by insertion sequence, `Count` never exceeds capacity, snapshots are detached arrays sorted by `(Timestamp, insertion sequence)`, equal timestamps are deterministic, time windows use `[startInclusive, endExclusive)`, and concurrent append/read behavior is bounded. `PerformanceTimelineBuffer` is unchanged and remains a separate legacy/event timeline. No disk persistence or timer was added.

### Typed 1-second aggregation

- RED `5ef8ad730d02399b60733a066d87e6ce059c47bc` → CI #926 / run `34279119026`; native remained GREEN, managed failed with 0 warnings and exactly fourteen errors, all because `TelemetryFrameAggregator` did not yet exist.
- GREEN `f5265286480662ffcfd3f89fbb03a1cd31a09e59` → CI #928 / run `34279297336` SUCCESS.

`TelemetryFrameAggregator` is pure Core logic. It filters windows as `[start,end)`, emits a result timestamped exactly at `end`, returns an empty/Unavailable frame for an empty window, groups by stable metric id and rejects incompatible `Unit`, `Domain` or `Aggregation` semantics before blending. `Gauge`/`Average` use arithmetic mean, `Minimum`/`Maximum`/`Sum` use their declared operations. Missing channels are ignored rather than zero-filled. Aggregate quality is the weakest contributing quality, coverage is the minimum contributing coverage, output origin is always `Derived`, homogeneous source ids are preserved, and mixed source provenance becomes `aggregate-mixed`. Contributor ordering is canonicalized before floating-point arithmetic so input enumeration order cannot change results.

## Authority invariants

- legacy `TelemetrySample` remains source-compatible;
- per-metric typed quality/provenance is authoritative in v2;
- unavailable values are absent, never zero-filled;
- finite numbers do not become Measured without accepted provenance;
- aggregation cannot upgrade quality or completeness;
- mixed provenance cannot masquerade as one direct source;
- incompatible descriptor semantics never blend;
- `GameId` remains durable identity; PID/path remain runtime evidence;
- only unambiguous bound RunningProcess evidence yields a live PID;
- App Paths/KnownExecutable never yields a live PID;
- `Observed != Validated` remains unchanged;
- Track 2 freshness/fingerprint/ValidatedEvidence/recommendation authority remains unchanged;
- Global Controlled Benchmark Lease remains unchanged;
- no startup discovery was added;
- no raw v2 telemetry persistence to disk has been introduced.

## Exact next action

Continue **Track 4** with the next isolated design/TDD boundary:

1. add the deterministic **10-second aggregation layer** on top of the already-GREEN generic aggregator, without reimplementing metric math;
2. define a bounded session aggregate/store contract and retention semantics before any disk persistence;
3. decide and test the application-level realtime pipeline composition (`collectors → v2 ring buffer → 1s → 10s/session`) without changing legacy UI/A-B consumers yet;
4. preserve `PerformanceTimelineBuffer` as a separate compatibility/event surface during migration;
5. only after realtime/session storage is GREEN, add new hardware channels one real provider at a time;
6. migrate Performance/A-B away from free-form `DataQuality` parsing only in a later isolated RED/GREEN slice, preserving every Track 2 validation/freshness gate.
