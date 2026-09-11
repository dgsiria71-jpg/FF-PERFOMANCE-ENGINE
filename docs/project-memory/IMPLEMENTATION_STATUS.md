# DG Performance Engine — Implementation Status Ledger

Current branch code/tests + fresh exact-commit Windows CI are authoritative over older handoffs. The recovered full architecture is preserved in `RECOVERED_MASTER_ARCHITECTURE_2026-09-11.md`.

## Current verified application checkpoint

- Branch: `build/initial-product`
- Application HEAD: `37e4744abcea1c791d6e19ea93b517f461fdbdb1`
- Commit: `feat: add typed Guardian canary outcome policy`
- Track 6 item 3 Slice 3: **GREEN**
- Windows CI: **#1058 — SUCCESS**
- Run: `34587241098`
- Artifact `FFPerformanceEngine-win-x64`: id `10194147656`, digest `sha256:3759a9e218797f4cb2233ee7cba5c5bd773838c04bf245394c1acbbcd3a82d66`.

Previous verified documentary HEAD: `94dc8a4681e044221479677edc0d4345e1f49989`, Windows CI #1055 / run `34567424200` SUCCESS.

## Track state

- Track 0 — Foundation Hardening — GREEN
- Track 1 — Universal Diagnostic Foundation — GREEN
- Track 2 — System Optimizer / evidence authority — GREEN through current branch
- Track 3 — Game Discovery + Adapter Framework — GREEN
- Track 4 — Universal Telemetry / Evidence — GREEN
- Track 5 — Universal Auto Tuner + Profiles — GREEN for current canonical scope
- Track 6 — Adaptive Guardian 2.0 — **ACTIVE; items 1–2 GREEN; item 3 IN PROGRESS; Slices 1–3 GREEN**
- Track 7 — Hardware Performance Engine — PLANNED
- Track 8 — Deep Cleaner — PLANNED
- Track 9 — Auto Optimize — PLANNED
- Track 10 — DG UX Migration — PLANNED

## Track 6 macro architecture

Authority: `docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`.

Approved sequence: generic workload state machine → universal classifiers → session optimizer actions → learned action reliability → post-session queue. Guardian remains state-aware, evidence-driven, rollback-first and capability-honest.

### Item 1 — Generic workload state machine — GREEN

Application `725065a90cef2ebd04a9d4d19e703ba46756bcb1` / CI #1039; observation bridge `6bb501eab866ee1fb17c546a02b5016ef97cad58` / CI #1041; documentary close `687b802dd187233a4637b7f78ac4c452ab925ef6` / CI #1042.

### Item 2 — Universal classifiers — GREEN

Typed bottleneck bridge `c132ec22c1f38fbacaa43ce630098d44674b3565` / CI #1043; taxonomy `5fd88d86abb9b00c4fb846486b7bb06026986962` / CI #1045; capability-honest support final application `26b9a0dbad71a742a612426af6120f9b074fe092` / CI #1049; documentary close `b0feaa8147a1bec8b1f3199b888cbd34a90d2ef9` / CI #1050.

Evidence-backed families: `CpuContention`, `GpuSaturation`, `MemoryPressure`, `VramPressure`, `FrameTimeInstability`, `ThermalThrottling`, `NetworkInstability`. `Unknown` is fallback/not healthy. `BackgroundLoad`, `RendererEngineStall`, `SchedulerImbalance`, `InputFrameLatencySpike` remain unavailable pending dedicated causal evidence.

### Item 3 — Session optimizer actions — IN PROGRESS

#### Slice 1 — Eligibility — GREEN

Application `7df0a6d5712a01aaf0ef58c0e47b7da4e0b937bd`, CI #1051 / run `34544846607`. Eligibility is read-only and requires exact stable workload, `Active`+`High`, evidence-backed family and explicit `LiveSafe` candidate; it grants no execution/ranking/learning authority.

#### Slice 2 — Reversible session canary — GREEN

Plan `docs/superpowers/plans/2026-09-10-track6-session-canary-execution.md`.

`GenericGuardianWindowsSessionCanaryExecutor` reuses Track 2 transaction authority and Track 4 typed capture. It accepts one explicit eligible binding, revalidates it, captures typed before evidence, starts one explicit session transaction, captures typed after evidence, delegates family semantics through `IGenericGuardianSessionCanaryOutcomeEvaluator`, keeps only `Improved`, restores exact prior state on all non-improved/failure/cancellation paths, and returns kept state only through a reversible async-disposable lease.

TDD evidence: RED `d8c167f9dd5f174c56b1a49ac77fa77488dee9ec` / run `34545776498`; GREEN `380169a047415e17b6fcfbd85a471f88fdb743b9` / run `34546198986`; official application `cef217f4d4f053109ee6bed34483d773f02605bf`, Windows CI #1053 / run `34546467152` SUCCESS.

#### Slice 3 — Typed canary outcome policy — GREEN

Core `src/FFPerformanceEngine.Core/Services/GenericGuardianTypedCanaryOutcomeEvaluator.cs`.
Test `tests/FFPerformanceEngine.Core.SelfTest/GenericGuardianTypedCanaryOutcomeEvaluatorSelfTests.cs`.

Permanent contract:

- policy plugs into the existing outcome-evaluator seam; it does not alter transaction/capture/executor authority;
- `CpuContention` and `GpuSaturation` are the only generic families currently allowed to produce `Improved`/`Regressive`;
- required before/after metrics are measured finite `FrameFpsAverage` and `FrameTimeAverageMs` at coverage >= 0.75;
- >=2% relative FPS gain plus non-worsening average frame time => `Improved`;
- >=2% relative FPS loss => `Regressive`;
- sub-threshold/noisy, incomplete, partial, low-coverage or invalid evidence => `Inconclusive`;
- Memory/VRAM/frame-pacing/thermal/network families remain `Inconclusive` until dedicated typed outcome evidence is available; all fallback/unavailable families also fail closed;
- no new score, persistence, learning, host wiring, startup mutation or presentation behavior.

TDD / verification:

- RED `83cde58ba9d44135b6c02d3b03b5bca3e4ca6ba3`, verifier run `34586771695`: native passed; managed failed only for missing `GenericGuardianTypedCanaryOutcomeEvaluator`, exactly 1 `CS0246`, 0 warnings;
- minimal implementation precursor `5aaddec30413ef65690e7c01dcc36ffc7981b4b2`;
- GREEN `e2df32f109fc0dc1cb8e2bd91fec1df8d1d299d4`, verifier run `34587029659`: native/managed/Core/App/publish SUCCESS;
- official application `37e4744abcea1c791d6e19ea93b517f461fdbdb1`, Windows CI #1058 / run `34587241098` SUCCESS;
- official artifact id `10194147656`, digest `sha256:3759a9e218797f4cb2233ee7cba5c5bd773838c04bf245394c1acbbcd3a82d66`.

Authority boundary: Slice 3 closes capability-honest family outcome semantics only for CPU/GPU. It adds no cooldown/Action Budget, runtime host wiring, learned ranking/reliability, post-session queue, recommendation/profile/winner promotion or persistent optimization.

## Non-negotiable authority

- `Observed != Validated`; live Guardian canary evidence is not controlled validation.
- Missing telemetry/capability/provenance remains absent/Unknown.
- Stable `GameId` and exact running-process evidence remain distinct.
- Existing Track 0 lease, Track 2 transaction/rollback, Track 4 typed telemetry, Track 5 profile/winner provenance and specialized FF/BlueStacks behavior remain authoritative.
- Discovery stays explicit/on-demand; no startup discovery. WPF remains presentation/request only. No integrity/anti-cheat bypass.

## Exact next engineering action

After the documentary checkpoint is exact-CI GREEN, remain inside Track 6 item 3 and implement the smallest **cooldown + Action Budget** boundary that prevents repeated live-session interventions from thrashing. It must compose with the existing eligibility → reversible session canary → typed outcome path without adding learned reliability. Runtime host wiring remains the final subsequent item-3 slice.
