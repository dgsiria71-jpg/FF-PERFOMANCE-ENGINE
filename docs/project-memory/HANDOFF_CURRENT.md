# Current Handoff — 2026-09-08

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR: `#1` — open, draft, not merged
- PR base: `main`

## Last verified application-code checkpoint

- Application HEAD: `edfbba0845d60447e6fdd158b75eee6def39a60f`
- Commit: `feat: expose direct PresentMon telemetry v2`
- Windows CI: **#900 — SUCCESS**
- CI run id: `34277780294`

The exact #900 job passed native configure, C++ build, native tests, managed/WPF build, Core self-tests, `win-x64` publish, artifact upload and final job completion.

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

### Schema v2

- RED `86953dca06fd278383d35f5a9202371dfe1a0410` → CI #878 / run `34273684420`.
- GREEN `4ced969a17b41e4cad56c9e413c626d9cd2326d6` → CI #880 / run `34275125229` SUCCESS.

Typed telemetry now has stable metric descriptors/ids, domain/unit/aggregation, per-metric quality, coverage, source provenance and origin. Stored numeric observations must be finite; `Unavailable` is represented by absence rather than a fake number. `TelemetryFrame` copies/sorts observations, rejects duplicate metric ids and computes frame summary quality without upgrading individual metrics.

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

`TelemetryService` now shares one native snapshot for both legacy and v2 projections. `CaptureSystemSample()` remains source-compatible. `CaptureSystemFrame()` publishes only finite CPU utilization + physical-memory used/total values as `Measured / native-system / Direct / coverage 1`. No GPU, thermal, clock, I/O or network data is invented.

### PresentMon direct v2

- RED `053ec156bfa2a260e6a53ec14169b818a646f786` → CI #898 / run `34277312296`; native remained GREEN and managed failed with 0 warnings and only six missing API errors for `ParseCsvFrame` / `CaptureProcessFrameAsync`.
- GREEN `edfbba0845d60447e6fdd158b75eee6def39a60f` → CI #900 / run `34277780294` SUCCESS.

`ParseCsv()` and `ParseCsvFrame()` now use the same internal PresentMon statistics object, preventing formula drift. Legacy FPS, 1% low, 0.1% low, frame-time average/P95/P99, stutter, latency and `DataQuality = "PresentMon · <n> frames"` behavior remain compatible. V2 emits only the eight proven frame/latency metrics, source `presentmon`, origin `Direct`, quality `Measured`, with frame coverage derived from accepted frame rows and latency coverage from accepted latency rows. Missing latency stays absent. One process capture request still invokes PresentMon only once because legacy/v2 share the same CSV-capture routine.

## Authority invariants

- legacy `TelemetrySample` remains source-compatible;
- per-metric typed quality/provenance is authoritative in v2;
- unavailable values are absent, never zero-filled;
- finite numbers do not become Measured without accepted provenance;
- `GameId` remains durable identity; PID/path remain runtime evidence;
- only unambiguous bound RunningProcess evidence yields a live PID;
- App Paths/KnownExecutable never yields a live PID;
- `Observed != Validated` remains unchanged;
- Track 2 freshness/fingerprint/ValidatedEvidence/recommendation authority remains unchanged;
- Global Controlled Benchmark Lease remains unchanged;
- no startup discovery was added.

## Exact next action

Continue **Track 4 Slice D — realtime storage / aggregation** using TDD.

Required first milestone:

1. bounded in-memory `TelemetryFrame` ring buffer;
2. deterministic snapshots and capacity eviction;
3. no disk persistence yet;
4. deterministic 1-second aggregation by stable metric id;
5. never average incompatible descriptors;
6. absent metrics do not become zero samples;
7. aggregation quality cannot exceed the weakest contributing observation;
8. aggregation coverage is explicit and cannot exceed contributing coverage;
9. homogeneous source/origin can be preserved; mixed provenance must be represented conservatively instead of pretending one direct source;
10. keep existing `PerformanceTimelineBuffer` unchanged and separate.

After the 1-second layer is GREEN, add the 10-second/session aggregation layer, then real hardware channels one provider at a time. A/B typed-quality migration remains a later isolated RED/GREEN slice and must not weaken current validation/freshness authority.
