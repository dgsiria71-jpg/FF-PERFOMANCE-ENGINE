# DG Performance Engine — Implementation Status Ledger

Current branch code/tests + fresh exact-commit Windows CI are authoritative over older handoffs.

## Current verified application checkpoint

- Branch: `build/initial-product`
- Application HEAD: `6bb501eab866ee1fb17c546a02b5016ef97cad58`
- Commit: `feat: add Track 6 generic workload observation bridge`
- Windows CI: **#1041 — SUCCESS**
- Run: `34525442625`
- Full official gate passed: native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish and artifact upload.
- Track 6.1 Slice 2 checkpoint: `2026-09-10-track6-generic-workload-observation.complete`.

Previous verified documentary checkpoint:

- Documentary HEAD: `7af89b838bf2300819e33b30fa9c5ab7879f32a1`
- Windows CI: **#1040 — SUCCESS**
- Run: `34523528901`
- It synchronized Track 6.1 Slice 1 and made the already-approved Track 6 architecture operational in project memory.

## Product foundation

DG Performance Engine is an evolution of the working FF Performance Engine Windows product, not a rewrite. Physical `FFPerformanceEngine.App/Core/Native` names remain until controlled migration. C#/.NET 8 + WPF owns presentation/orchestration/policies/profiles/persistence; C++20/Win32 owns native/low-overhead platform work where it materially helps.

## Track state

- Track 0 — Foundation Hardening — GREEN
- Track 1 — Universal Diagnostic Foundation — GREEN
- Track 2 — System Optimizer / evidence authority — GREEN through current branch
- Track 3 — Game Discovery + Adapter Framework — GREEN
- Track 4 — Universal Telemetry / Evidence — GREEN
- Track 5 — Universal Auto Tuner + Profiles — GREEN for current canonical scope
- Track 6 — Adaptive Guardian 2.0 — **ACTIVE; item 1 generic workload state machine GREEN; item 2 universal classifiers NEXT**
- Track 7 — Hardware Performance Engine — PLANNED
- Track 8 — Deep Cleaner — PLANNED
- Track 9 — Auto Optimize — PLANNED
- Track 10 — DG UX Migration — PLANNED

## Track 5 closure authority

Track 5 remains closed GREEN for its approved additive universal foundation scope. Closing application SHA `a0c9a4e30dc48cb1730cee8b6b951f7338680cd1`, Windows CI #1034 / run `34502895182`; closure documentary HEAD `0d7886b6ff19898bcba38585ca6369728bff4145`, Windows CI #1036 / run `34510474469`.

Track 5 does not claim that every discovered game has a physical tuning runtime. Universal search/correlation/provenance does not manufacture evidence, `Validated`, winner, mutation or persistence authority.

## Track 6 macro architecture — already approved

Authority:

`docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`

Do not reopen this architecture unless a real contradiction is discovered. Approved implementation sequence:

1. generic workload state machine — GREEN;
2. universal classifiers — NEXT;
3. session optimizer actions;
4. learned action reliability;
5. post-session queue.

Approved Guardian semantics remain: state detection → real degradation confirmation → likely-cause classification → workload/state-appropriate `LIVE_SAFE` candidate → micro-snapshot/canary → measured keep or rollback. Inconclusive results roll back. Cooldown and Action Budget prevent thrashing. Controlled evidence remains stronger than passive Guardian observation.

## Track 6 item 1 — Generic workload state machine — GREEN

### Slice 1 — Generic lifecycle foundation

Plan: `docs/superpowers/plans/2026-09-10-track6-generic-workload-state-machine.md`.

Core: `src/FFPerformanceEngine.Core/Services/GenericGuardianWorkloadStateMachine.cs`.

Provides stable generic states `Unresolved / Offline / Desktop / Starting / Ready / Active / Ending`, categorical confidence, exact canonical identity/adapter preservation, fail-closed unknown/ambiguous behavior, `KnownExecutable` non-authority, PID restart semantics, conservative Ending/Desktop and explicit offline behavior.

Verification: application SHA `725065a90cef2ebd04a9d4d19e703ba46756bcb1`, Windows CI #1039 / run `34522642478` SUCCESS.

### Slice 2 — Generic workload observation bridge

Plan: `docs/superpowers/plans/2026-09-10-track6-generic-workload-observation.md`.

Core: `src/FFPerformanceEngine.Core/Services/GenericGuardianWorkloadObservationService.cs`.

Permanent contracts/behavior:

- `IForegroundProcessProbe.GetForegroundProcessId()` and `WindowsForegroundProcessProbe` provide neutral foreground PID only;
- `GenericGuardianWorkloadObservation` carries state snapshot, explicit generic signals and the exact-target typed frame when available;
- `GenericGuardianWorkloadObservationService` reuses `TelemetryWorkloadTargetResolver` and accepts the existing exact typed workload capture seam;
- unknown/ambiguous/unavailable targets perform no foreground/input/telemetry probe;
- foreground is exact PID equality, never process/window-name identity;
- global recent input is consulted only for the exact foreground workload PID;
- capture results with mismatched GameId/PID/path/binding are rejected;
- render activity requires direct + measured + positive `frame.samples.accepted.count`;
- exact-target typed frames remain available to later classifiers even if that render-authority test fails;
- explicit offline performs no external probes and yields the state-machine Offline result;
- constructor is side-effect free.

This Slice contains no discovery/startup, baseline, universal classification, mutation, canary, Profile/History/Knowledge persistence, AppServices or WPF changes. Existing specialized BlueStacks/FF Guardian behavior remains untouched.

## Track 6.1 Slice 2 TDD / verification

- verifier branch: `ci/track6-generic-workload-observation-verify`;
- RED SHA `da16472d69ba12169a7bd6a3d519cba49087ab2f`, verifier run `34524859953`: native passed; managed build failed only on the intentionally absent observation contracts, exactly 3 `CS0246`, 0 warnings;
- GREEN SHA `7d3c3cb1e10f7bc1b40fe6ee5e6ad9d5f135530c`, verifier run `34525158496`: native + managed + Core + App + publish SUCCESS;
- temporary verifier workflow excluded from official branch;
- official application SHA `6bb501eab866ee1fb17c546a02b5016ef97cad58`;
- Windows CI #1041 / run `34525442625` SUCCESS including artifact upload.

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

Begin Track 6 item 2 — **universal classifiers** — using the already-preserved exact-target typed `TelemetryFrame` and generic workload state.

Inspect the current typed diagnostic/bottleneck analyzer contracts and current Guardian decision seams first. The first bounded classifier Slice must be pure/read-only, capability-honest and fail to explicit `Unknown` when required metrics are absent or untrusted. It must not create action, validation, profile, winner or persistence authority and must not promote the existing global action-id Guardian Knowledge into universal evidence.

Repeat the canonical TDD/CI/memory cycle before another Slice.
