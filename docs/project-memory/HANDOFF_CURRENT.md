# Current Handoff — 2026-09-10

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR #1 remains open/draft to `main`; do not merge/touch `main` while critical architecture is being proven.
- Product: **DG Performance Engine**, evolved incrementally from FF Performance Engine; no rewrite or mass rename.
- Current Git/code/tests + fresh exact Windows CI outrank stale chat or memory text.

## Current exact verified application checkpoint

- Application HEAD: `5fd88d86abb9b00c4fb846486b7bb06026986962`
- Commit: `feat: add Guardian classifier taxonomy projection`
- Windows CI: **#1045 — SUCCESS**
- Run: `34528667164`
- Full gate passed: native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish, artifact upload and cleanup.
- Artifact: `FFPerformanceEngine-win-x64`, id `10172642667`, SHA-256 `c60a2f4f2d21450a3a0dc89593248bd48727e4112b9b15c900ecc9fdc22dcd19`.

Previous verified documentary checkpoint:

- Documentary HEAD: `32c3bdf28dfaedf78b89904e2cfe4a276c0909bd`
- Windows CI: **#1044 — SUCCESS**
- Run: `34527558404`
- It checkpointed Track 6 item 2 Slice 1.

## Track state

- Track 0 — Foundation Hardening: **GREEN**
- Track 1 — Universal Diagnostic Foundation: **GREEN**
- Track 2 — System Optimizer / evidence authority: **GREEN through current branch**
- Track 3 — Game Discovery + Adapter Framework: **GREEN**
- Track 4 — Universal Telemetry / Evidence: **GREEN**
- Track 5 — Universal Auto Tuner + Profiles: **GREEN for current canonical scope**
- Track 6 — Adaptive Guardian 2.0: **ACTIVE; item 1 GREEN; item 2 universal classifiers in progress; Slices 1–2 GREEN**
- Track 7+ — planned per roadmap/canonical context.

## Track 6 architecture authority

Do not redesign or ask the user to re-approve Track 6. Authority remains:

`docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`

Approved implementation order:

1. generic workload state machine — **GREEN**;
2. universal classifiers — **IN PROGRESS**;
3. session optimizer actions;
4. learned action reliability;
5. post-session queue.

Guardian remains an additive expansion of the proven specialized Guardian, not a rewrite. Generic state/classification remains conservative; richer game/lobby/match semantics require specialized-adapter authority.

## Track 6 item 1 — GREEN

### Slice 1 — lifecycle state machine

Core: `src/FFPerformanceEngine.Core/Services/GenericGuardianWorkloadStateMachine.cs`.
Application SHA `725065a90cef2ebd04a9d4d19e703ba46756bcb1`, Windows CI #1039 / run `34522642478` SUCCESS.

Provides conservative `Unresolved / Offline / Desktop / Starting / Ready / Active / Ending`, categorical confidence, exact canonical identity/adapter preservation, fail-closed unknown/ambiguous behavior, non-live `KnownExecutable`, fresh lifecycle on PID replacement and conservative Ending/Desktop/offline transitions.

### Slice 2 — generic observation bridge

Core: `src/FFPerformanceEngine.Core/Services/GenericGuardianWorkloadObservationService.cs`.
Application SHA `6bb501eab866ee1fb17c546a02b5016ef97cad58`, Windows CI #1041 / run `34525442625` SUCCESS. Documentary close `687b802dd187233a4637b7f78ac4c452ab925ef6`, Windows CI #1042 / run `34526017491` SUCCESS.

Provides neutral exact foreground PID probing, exact-target typed frame preservation, exact GameId/PID/path/binding correlation, recent-input attribution only to the exact foreground workload, direct+measured positive accepted-frame render authority, fail-closed unavailable/ambiguous behavior and offline no-probe behavior. No startup discovery or mutation authority was introduced.

## Track 6 item 2 — Universal classifiers — IN PROGRESS

### Slice 1 — typed bottleneck classifier bridge — GREEN

Plan: `docs/superpowers/plans/2026-09-10-track6-universal-classifier-bridge.md`.
Core: `src/FFPerformanceEngine.Core/Services/GenericGuardianBottleneckClassifier.cs`.
Application SHA `c132ec22c1f38fbacaa43ce630098d44674b3565`, Windows CI #1043 / run `34526941137` SUCCESS.
Documentary checkpoint `32c3bdf28dfaedf78b89904e2cfe4a276c0909bd`, Windows CI #1044 / run `34527558404` SUCCESS.

Only `Active` + exact capturable target + typed frame can enter causal classification. Eligible observations delegate directly to Track 4 `UniversalBottleneckAnalyzer`; missing/incomplete evidence remains `Unknown`. The classifier is passive/read-only and grants no action or validation authority.

### Slice 2 — Guardian classifier taxonomy projection — GREEN

Plan: `docs/superpowers/plans/2026-09-10-track6-guardian-classifier-taxonomy.md`.
Permanent Core remains `src/FFPerformanceEngine.Core/Services/GenericGuardianBottleneckClassifier.cs`.

Added the approved `GuardianAnomalyKind` taxonomy and a computed read-only `Family` projection. The projection inspects only the already-proven `BottleneckAnalysisResult.Primary`; it does not inspect raw telemetry or introduce new thresholds.

Evidence-backed mappings:

- `Cpu` → `CpuContention`;
- `Gpu` → `GpuSaturation`;
- `Memory` → `MemoryPressure`;
- `Vram` → `VramPressure`;
- `FramePacing` → `FrameTimeInstability`;
- `Thermal` → `ThermalThrottling`;
- `Network` → `NetworkInstability`.

`Unknown`, `None`, `StorageIo`, `Power` and any other analyzer kind remain Guardian `Unknown`, while raw analyzer output is preserved unchanged. Approved but currently unproven causal families `BackgroundLoad`, `RendererEngineStall`, `SchedulerImbalance` and `InputFrameLatencySpike` exist in the taxonomy but are deliberately not emitted from raw high CPU/latency/missing-render observations.

TDD evidence:

- verifier branch `ci/track6-guardian-classifier-taxonomy-verify`;
- RED SHA `cf03b7ceb13e0bbb9ac5f98d37ea697cf17917ce`, run `34528006765`: native passed; managed failed only for missing `GuardianAnomalyKind` / `Family`, 35 intentional compile errors, 0 warnings;
- GREEN SHA `dfb41c5d0770264de42bc31afd1f265d35835467`, run `34528375851`: native + managed + Core + App + publish SUCCESS;
- selective integration excluded `.github/workflows/track6-guardian-classifier-taxonomy-verify.yml`;
- official application SHA `5fd88d86abb9b00c4fb846486b7bb06026986962`, Windows CI #1045 / run `34528667164` SUCCESS including artifact upload.

## Non-negotiable authority

- `Observed != Validated`.
- Missing telemetry/capability/provenance stays absent/Unknown.
- Stable GameId is separate from transient PID/path/process evidence.
- `KnownExecutable` never authorizes live capture or Guardian action.
- Typed measurement, History validation, ProfileService origin, AutoTuner winner selection and ProfileChallenge promotion retain existing authority.
- Global Controlled Benchmark Lease, Guardian suspension/reconciliation, fingerprint/freshness, rollback and History remain intact.
- Discovery remains explicit/on-demand and is not added to `InitializeAsync()`.
- Guardian does not own deep Auto Tuner exploration.
- Controlled evidence outranks passive Guardian observation.
- UI remains presentation/request only.
- No anti-cheat/integrity bypass.

## Exact next action

Continue **Track 6 item 2 — universal classifiers**. Before adding any more causal heuristic, define the smallest capability-honest support/availability contract for the approved Guardian anomaly families so callers can distinguish evidence-backed families from those intentionally unavailable with current telemetry.

The support contract must be static/read-only and must not imply that `Unknown` means healthy. It should identify the seven currently evidence-backed mappings and explicitly mark `BackgroundLoad`, `RendererEngineStall`, `SchedulerImbalance`, `InputFrameLatencySpike` as unavailable pending dedicated causal evidence. Then decide whether item 2 can close for the current foundation without fabricating unsupported classifiers.

Canonical gate remains:

`docs/memory/context → bounded Slice design → TDD RED → exact intended RED → minimal production → verifier GREEN → selective official integration → exact Windows CI → memory/checkpoint sync → exact documentary-head CI → next Slice`.
