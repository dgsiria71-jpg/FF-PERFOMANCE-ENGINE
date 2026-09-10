# DG Performance Engine — Roadmap

Current branch code/tests + fresh exact-commit Windows CI are authoritative. This roadmap records the canonical sequence without inventing missing historical numbering.

## Track 0 — Foundation Hardening — GREEN

Global controlled benchmark coordination, Guardian suspend/reconcile, Auto Tuner/Profile Challenge exclusivity, cancellation/cleanup hardening and preserved validated regressions.

## Track 1 — Universal Diagnostic Foundation — GREEN

Universal MachineContext, Hardware Discovery, Capability Registry/Graph, Environment Fingerprint v2 and universal bottleneck-analysis foundation.

## Track 2 — System Optimizer — GREEN through current branch

Real Windows capability adapters, runtime discovery, atomic transactions, exact restore/History, controlled A/B, Cost Maps, pending validation, fresh validation and durable validated evidence. Observed and Validated remain distinct.

## Track 3 — Game Discovery + Adapter Framework — GREEN

Stable GameIdentity/catalog, generic + specialized FF/FFMAX adapters, major launcher discovery, durable identity vs transient evidence, deterministic binding and exact workload target resolution.

## Track 4 — Universal Telemetry / Evidence — GREEN

Closing application SHA `71991379e01518adf2e1c539491a9c0339a56735`, Windows CI #993 / run `34407420906` SUCCESS. Typed telemetry/evidence, direct PresentMon, CPU/memory/power/WDDM GPU, bounded aggregation, diagnostics and explicit selected-workload capture routing remain proven. Unsupported channels stay Unknown.

## Track 5 — Universal Auto Tuner + Profiles — GREEN for current canonical scope

Closing application SHA `a0c9a4e30dc48cb1730cee8b6b951f7338680cd1`, Windows CI #1034 / run `34502895182` SUCCESS. Final closure documentary HEAD `0d7886b6ff19898bcba38585ca6369728bff4145`, Windows CI #1036 / run `34510474469` SUCCESS.

## Track 6 — Adaptive Guardian 2.0 — ACTIVE

Macro architecture authority:

`docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`

Approved implementation order:

1. **generic workload state machine** — GREEN;
2. **universal classifiers** — IN PROGRESS;
3. **session optimizer actions** — pending item 2;
4. **learned action reliability** — pending item 3;
5. **post-session queue** — pending item 4.

Approved Guardian classifier families remain CPU contention, GPU saturation, memory pressure, VRAM pressure, frame-time instability, background load, thermal throttling, network instability, renderer/engine stall, scheduler imbalance, input/frame-latency spike and legitimate `Unknown`.

Approved intervention model remains detect degradation → confirm anomaly → select state/workload-appropriate LIVE_SAFE candidate → micro-snapshot → canary → measure → KEEP/ROLLBACK. Inconclusive rolls back; cooldown and Action Budget prevent thrashing.

### Track 6 item 1 — Generic workload state machine — GREEN

#### Slice 1 — Core lifecycle foundation — GREEN

- application SHA `725065a90cef2ebd04a9d4d19e703ba46756bcb1`;
- Windows CI #1039 / run `34522642478` SUCCESS;
- plan `docs/superpowers/plans/2026-09-10-track6-generic-workload-state-machine.md`;
- checkpoint `docs/project-memory/checkpoints/2026-09-10-track6-generic-workload-state-machine.complete`.

#### Slice 2 — Generic workload observation bridge — GREEN

- application SHA `6bb501eab866ee1fb17c546a02b5016ef97cad58`;
- Windows CI #1041 / run `34525442625` SUCCESS;
- documentary close `687b802dd187233a4637b7f78ac4c452ab925ef6`, Windows CI #1042 / run `34526017491` SUCCESS;
- plan `docs/superpowers/plans/2026-09-10-track6-generic-workload-observation.md`;
- checkpoint `docs/project-memory/checkpoints/2026-09-10-track6-generic-workload-observation.complete`.

Item 1 is complete for the current Core foundation: stable workload identity + exact runtime target + trustworthy generic observation + conservative lifecycle state.

### Track 6 item 2 — Universal classifiers — IN PROGRESS

#### Slice 1 — Typed bottleneck classifier bridge — GREEN

- application SHA `c132ec22c1f38fbacaa43ce630098d44674b3565`;
- Windows CI #1043 / run `34526941137` SUCCESS;
- documentary checkpoint `32c3bdf28dfaedf78b89904e2cfe4a276c0909bd`, Windows CI #1044 / run `34527558404` SUCCESS;
- plan `docs/superpowers/plans/2026-09-10-track6-universal-classifier-bridge.md`.

Only `Active` + exact target + typed frame can enter causal analysis, which delegates to the existing Track 4 analyzer. Missing/incomplete evidence remains `Unknown`.

#### Slice 2 — Guardian classifier taxonomy projection — GREEN

- application SHA `5fd88d86abb9b00c4fb846486b7bb06026986962`;
- Windows CI **#1045 / run `34528667164` SUCCESS**;
- artifact `FFPerformanceEngine-win-x64`, id `10172642667`, digest `sha256:c60a2f4f2d21450a3a0dc89593248bd48727e4112b9b15c900ecc9fdc22dcd19`;
- plan `docs/superpowers/plans/2026-09-10-track6-guardian-classifier-taxonomy.md`.

Permanent taxonomy:

`Unknown / CpuContention / GpuSaturation / MemoryPressure / VramPressure / FrameTimeInstability / BackgroundLoad / ThermalThrottling / NetworkInstability / RendererEngineStall / SchedulerImbalance / InputFrameLatencySpike`.

Evidence-backed projection is deliberately narrow:

- `Cpu` → `CpuContention`;
- `Gpu` → `GpuSaturation`;
- `Memory` → `MemoryPressure`;
- `Vram` → `VramPressure`;
- `FramePacing` → `FrameTimeInstability`;
- `Thermal` → `ThermalThrottling`;
- `Network` → `NetworkInstability`.

`Unknown`, `None`, `StorageIo`, `Power` or future unmapped analyzer kinds project to Guardian `Unknown` while preserving raw analyzer details. Raw high latency does not produce `InputFrameLatencySpike`; high total CPU does not produce `BackgroundLoad`/`SchedulerImbalance`; absent render activity does not produce `RendererEngineStall`.

TDD verifier:

- RED `cf03b7ceb13e0bbb9ac5f98d37ea697cf17917ce`, run `34528006765`: native SUCCESS, managed expected failure solely for absent taxonomy/Family, 35 errors, 0 warnings;
- GREEN `dfb41c5d0770264de42bc31afd1f265d35835467`, run `34528375851`: native/managed/Core/App/publish SUCCESS;
- temporary verifier workflow excluded from official integration.

#### Next Slice inside item 2

Add a static/read-only **classifier support/availability contract** for every approved Guardian anomaly family. It must distinguish families currently backed by existing typed analyzer authority from families unavailable pending dedicated causal evidence. It must not convert “unsupported” into “healthy” and must not add new telemetry heuristics.

After that Slice, evaluate whether item 2 can close for the current capability-honest foundation. Do not begin session optimizer actions before the item-2 closure gate.

### Track 6 non-negotiable constraints

- Guardian does not own deep Auto Tuner exploration;
- gameplay interventions require workload-appropriate `LIVE_SAFE` authority;
- controlled evidence outranks passive observation;
- Global Controlled Benchmark Lease prevents Guardian contamination of controlled work;
- missing telemetry/capability remains Unknown/absent;
- stable workload identity remains distinct from transient process evidence;
- `KnownExecutable` never authorizes live process binding/capture;
- BlueStacks/FF Guardian behavior stays preserved during incremental universalization;
- UI presents/requests; policy remains outside WPF.

Every independent Slice remains:

```text
docs/memory/context
→ bounded design
→ TDD RED
→ exact intended RED
→ minimal production
→ verifier GREEN
→ selective official integration
→ exact Windows CI
→ memory/checkpoint sync
→ exact documentary-head CI
→ next Slice
```

## Track 7 — Hardware Performance Engine — PLANNED

Vendor capability adapters, CPU/GPU controls, additional proven telemetry, Expert/Auto Tuner integration and instability detection.

## Track 8 — Deep Cleaner — PLANNED

Analyzer/classifier, Safe/Deep/Extreme policies, personal-data protection, quarantine/history and UI.

## Track 9 — Auto Optimize — PLANNED

Environment/change detection, recommendation engine, local learning, confidence decay and auto-apply policy.

## Track 10 — DG UX Migration — PLANNED

Analyze, Games, Cleaner and System Optimize surfaces plus approved Home/Profiles/Guardian/Performance/Expert/History/Settings/Mini architecture.

## Expanded master architecture

A larger historical Track 0–19 master architecture was reported, but its raw source is not currently mounted. Do not fabricate exact Track 11–19 numbering. Preserve recovered approved domains in canonical project-memory and the unified architecture spec until the original source is recovered.
