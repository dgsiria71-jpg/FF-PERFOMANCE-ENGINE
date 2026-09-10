# DG Performance Engine — Implementation Status Ledger

Current branch code/tests + fresh exact-commit Windows CI are authoritative over older handoffs.

## Current verified application checkpoint

- Branch: `build/initial-product`
- Application HEAD: `5fd88d86abb9b00c4fb846486b7bb06026986962`
- Commit: `feat: add Guardian classifier taxonomy projection`
- Windows CI: **#1045 — SUCCESS**
- Run: `34528667164`
- Full official gate passed: native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish, artifact upload and cleanup.
- Artifact `FFPerformanceEngine-win-x64`: id `10172642667`, digest `sha256:c60a2f4f2d21450a3a0dc89593248bd48727e4112b9b15c900ecc9fdc22dcd19`.

Previous verified documentary checkpoint:

- Documentary HEAD `32c3bdf28dfaedf78b89904e2cfe4a276c0909bd`
- Windows CI **#1044 — SUCCESS**
- Run `34527558404`
- It checkpointed Track 6 item 2 Slice 1.

## Product foundation

DG Performance Engine is an evolution of the working FF Performance Engine Windows product, not a rewrite. Physical `FFPerformanceEngine.App/Core/Native` names remain until controlled migration. C#/.NET 8 + WPF owns presentation/orchestration/policies/profiles/persistence; C++20/Win32 owns native/low-overhead platform work where it materially helps.

## Track state

- Track 0 — Foundation Hardening — GREEN
- Track 1 — Universal Diagnostic Foundation — GREEN
- Track 2 — System Optimizer / evidence authority — GREEN through current branch
- Track 3 — Game Discovery + Adapter Framework — GREEN
- Track 4 — Universal Telemetry / Evidence — GREEN
- Track 5 — Universal Auto Tuner + Profiles — GREEN for current canonical scope
- Track 6 — Adaptive Guardian 2.0 — **ACTIVE; item 1 GREEN; item 2 universal classifiers IN PROGRESS; Slices 1–2 GREEN**
- Track 7 — Hardware Performance Engine — PLANNED
- Track 8 — Deep Cleaner — PLANNED
- Track 9 — Auto Optimize — PLANNED
- Track 10 — DG UX Migration — PLANNED

## Track 6 macro architecture — already approved

Authority: `docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`.

Approved sequence:

1. generic workload state machine — GREEN;
2. universal classifiers — IN PROGRESS;
3. session optimizer actions;
4. learned action reliability;
5. post-session queue.

Approved Guardian semantics remain state detection → degradation confirmation → likely-cause classification → workload/state-appropriate `LIVE_SAFE` candidate → micro-snapshot/canary → measured keep or rollback. Inconclusive results roll back. Cooldown and Action Budget prevent thrashing. Controlled evidence remains stronger than passive Guardian observation.

## Track 6 item 1 — Generic workload state machine — GREEN

Slice 1 lifecycle foundation: application SHA `725065a90cef2ebd04a9d4d19e703ba46756bcb1`, Windows CI #1039 / run `34522642478` SUCCESS.

Slice 2 observation bridge: application SHA `6bb501eab866ee1fb17c546a02b5016ef97cad58`, Windows CI #1041 / run `34525442625` SUCCESS; documentary close `687b802dd187233a4637b7f78ac4c452ab925ef6`, Windows CI #1042 / run `34526017491` SUCCESS.

Item 1 provides stable workload identity, exact runtime target, trustworthy generic observation and conservative lifecycle state with no startup discovery or mutation authority.

## Track 6 item 2 — Universal classifiers — IN PROGRESS

### Slice 1 — typed bottleneck classifier bridge — GREEN

Plan: `docs/superpowers/plans/2026-09-10-track6-universal-classifier-bridge.md`.
Core: `src/FFPerformanceEngine.Core/Services/GenericGuardianBottleneckClassifier.cs`.
Application SHA `c132ec22c1f38fbacaa43ce630098d44674b3565`, Windows CI #1043 / run `34526941137` SUCCESS.
Documentary checkpoint `32c3bdf28dfaedf78b89904e2cfe4a276c0909bd`, Windows CI #1044 / run `34527558404` SUCCESS.

The classifier is a read-only Guardian policy seam over existing Track 4 `UniversalBottleneckAnalyzer`. Only `Active` + exact capturable target + typed frame can classify. Missing or incomplete evidence remains `Unknown`. No action/validation authority is created.

### Slice 2 — Guardian classifier taxonomy projection — GREEN

Plan: `docs/superpowers/plans/2026-09-10-track6-guardian-classifier-taxonomy.md`.
Core remains: `src/FFPerformanceEngine.Core/Services/GenericGuardianBottleneckClassifier.cs`.

Added:

- `GuardianAnomalyKind`: `Unknown`, `CpuContention`, `GpuSaturation`, `MemoryPressure`, `VramPressure`, `FrameTimeInstability`, `BackgroundLoad`, `ThermalThrottling`, `NetworkInstability`, `RendererEngineStall`, `SchedulerImbalance`, `InputFrameLatencySpike`;
- computed read-only `GenericGuardianBottleneckClassification.Family`.

The projection maps only already-proven analyzer causes:

- CPU → CpuContention;
- GPU → GpuSaturation;
- Memory → MemoryPressure;
- VRAM → VramPressure;
- FramePacing → FrameTimeInstability;
- Thermal → ThermalThrottling;
- Network → NetworkInstability.

Analyzer `Unknown`, `None`, `StorageIo`, `Power` and any unmapped result remain Guardian `Unknown`, without erasing raw analyzer output. Raw high frame latency does not manufacture `InputFrameLatencySpike`; high system CPU does not manufacture `BackgroundLoad` or `SchedulerImbalance`; missing render activity does not manufacture `RendererEngineStall`.

TDD / verification:

- verifier branch `ci/track6-guardian-classifier-taxonomy-verify`;
- RED SHA `cf03b7ceb13e0bbb9ac5f98d37ea697cf17917ce`, run `34528006765`: native passed; managed failed only because `GuardianAnomalyKind` / `Family` were absent, 35 intentional compile errors, 0 warnings;
- GREEN SHA `dfb41c5d0770264de42bc31afd1f265d35835467`, run `34528375851`: native + managed + Core + App + publish SUCCESS;
- verifier workflow excluded from official branch;
- official application SHA `5fd88d86abb9b00c4fb846486b7bb06026986962`;
- Windows CI #1045 / run `34528667164` SUCCESS including artifact upload.

## Non-negotiable authority

- `Observed != Validated`.
- Missing telemetry/capability/provenance remains absent/Unknown.
- Stable `GameId` remains separate from transient PID/path/process evidence.
- `KnownExecutable` never grants live capture/action.
- Global Controlled Benchmark Lease and Guardian suspension/reconciliation remain intact.
- Existing measurement, History validation, ProfileService origin, AutoTuner winner selection and ProfileChallenge promotion retain their authorities.
- Guardian does not own deep Auto Tuner exploration.
- Gameplay mutation requires explicit workload/state-appropriate `LIVE_SAFE` authority plus measurable rollback-capable execution.
- Discovery stays explicit/on-demand and does not move into `InitializeAsync()`.
- WPF remains presentation/request only.
- No anti-cheat/integrity bypass.

## Exact next engineering action

Continue Track 6 item 2 with a bounded **classifier support/availability contract**. It must make explicit which approved Guardian anomaly families are currently evidence-backed by the typed analyzer and which are intentionally unavailable pending dedicated causal evidence.

Do not add heuristic classifiers merely to fill the taxonomy. After that capability-honest support Slice, evaluate whether item 2 is complete for the current foundation and, if so, close it before beginning item 3 session optimizer actions.
