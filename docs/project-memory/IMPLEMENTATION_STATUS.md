# DG Performance Engine — Implementation Status Ledger

Current branch code/tests + fresh exact-commit Windows CI are authoritative over older handoffs. The recovered full architecture is preserved in `RECOVERED_MASTER_ARCHITECTURE_2026-09-11.md`.

## Current verified application checkpoint

- Branch: `build/initial-product`
- Application HEAD: `cef217f4d4f053109ee6bed34483d773f02605bf`
- Commit: `feat: add reversible Guardian Windows session canary`
- Track 6 item 3 Slice 2: **GREEN**
- Windows CI: **#1053 — SUCCESS**
- Run: `34546467152`
- Artifact `FFPerformanceEngine-win-x64`: id `10179230537`, digest `sha256:b8d3821173725bc2859ce82eef3d5fc76f146a79eb815d6757f01abd867780b0`.

Previous verified documentary HEAD: `8dcc6042ec72fe1b38fa5d1cd1faee8a9f1e012d`, Windows CI #1054 / run `34564823407` SUCCESS.

## Track state

- Track 0 — Foundation Hardening — GREEN
- Track 1 — Universal Diagnostic Foundation — GREEN
- Track 2 — System Optimizer / evidence authority — GREEN through current branch
- Track 3 — Game Discovery + Adapter Framework — GREEN
- Track 4 — Universal Telemetry / Evidence — GREEN
- Track 5 — Universal Auto Tuner + Profiles — GREEN for current canonical scope
- Track 6 — Adaptive Guardian 2.0 — **ACTIVE; items 1–2 GREEN; item 3 IN PROGRESS; Slices 1–2 GREEN**
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

Production `GenericGuardianWindowsSessionCanaryExecutor` reuses Track 2 transaction authority and Track 4 typed capture rather than duplicating them. It accepts one explicit eligible binding, revalidates it, captures typed before evidence, starts exactly one explicit session transaction, captures typed after evidence, delegates family semantics to `IGenericGuardianSessionCanaryOutcomeEvaluator`, keeps only `Improved`, and restores exact prior state on regression/inconclusive/unavailable-after/failure/cancellation. Kept state is owned by a reversible async-disposable lease.

TDD evidence:

- RED `d8c167f9dd5f174c56b1a49ac77fa77488dee9ec`, verifier run `34545776498`: native passed; managed failed with exactly 6 intended missing-contract `CS0246` errors and 0 warnings;
- production precursor `26af2d51667596f9f0a356022196d172bf3c2569`;
- GREEN `380169a047415e17b6fcfbd85a471f88fdb743b9`, verifier run `34546198986`: native/managed/Core/App/publish SUCCESS;
- official application `cef217f4d4f053109ee6bed34483d773f02605bf`, Windows CI #1053 / run `34546467152` SUCCESS;
- official artifact id `10179230537`, digest `sha256:b8d3821173725bc2859ce82eef3d5fc76f146a79eb815d6757f01abd867780b0`.

Authority boundary: Slice 2 proves reversible orchestration only. No family-specific threshold policy, cooldown/Action Budget, runtime host wiring, learned ranking/reliability, post-session queue, recommendation/profile/winner promotion or persistent optimization is introduced.

## Non-negotiable authority

- `Observed != Validated`; live Guardian canary evidence is not controlled validation.
- Missing telemetry/capability/provenance remains absent/Unknown.
- Stable `GameId` and exact running-process evidence remain distinct.
- Existing Track 0 lease, Track 2 transaction/rollback, Track 4 typed telemetry, Track 5 profile/winner provenance and specialized FF/BlueStacks behavior remain authoritative.
- Discovery stays explicit/on-demand; no startup discovery. WPF remains presentation/request only. No integrity/anti-cheat bypass.

## Exact next engineering action

After the documentary checkpoint is exact-CI GREEN, remain inside Track 6 item 3. Build the smallest family-specific canary outcome-policy layer that uses only supported typed evidence and returns `Inconclusive` when the required causal metric is unavailable. Do not add learned reliability yet. Cooldown/Action Budget and runtime host wiring remain subsequent item-3 slices.
