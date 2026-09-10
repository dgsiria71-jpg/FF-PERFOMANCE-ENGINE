# DG Performance Engine — Implementation Status Ledger

Current branch code/tests + fresh exact-commit Windows CI are authoritative over older handoffs.

## Current verified application checkpoint

- Branch: `build/initial-product`
- Application HEAD: `725065a90cef2ebd04a9d4d19e703ba46756bcb1`
- Commit: `feat: add Track 6 generic workload state machine`
- Windows CI: **#1039 — SUCCESS**
- Run: `34522642478`
- Full official gate passed: native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish and artifact upload.
- Track 6.1 Slice 1 checkpoint: `2026-09-10-track6-generic-workload-state-machine.complete`.

Previous verified documentary checkpoint:

- Documentary HEAD: `7a54d1e19f5abb13cf40c7b967895400f3bea170`
- Windows CI: **#1038 — SUCCESS**
- Run: `34520178481`
- It recovered and made operational the already-approved 2026-09-06 Track 6 macro architecture.

## Product foundation

DG Performance Engine is an evolution of the working FF Performance Engine Windows product, not a rewrite. Physical `FFPerformanceEngine.App/Core/Native` names remain until controlled migration. C#/.NET 8 + WPF owns presentation/orchestration/policies/profiles/persistence; C++20/Win32 owns native/low-overhead platform work where it materially helps.

## Track state

- Track 0 — Foundation Hardening — GREEN
- Track 1 — Universal Diagnostic Foundation — GREEN
- Track 2 — System Optimizer / evidence authority — GREEN through current branch
- Track 3 — Game Discovery + Adapter Framework — GREEN
- Track 4 — Universal Telemetry / Evidence — GREEN
- Track 5 — Universal Auto Tuner + Profiles — GREEN for current canonical scope
- Track 6 — Adaptive Guardian 2.0 — **ACTIVE; approved macro architecture; item 1 generic workload state machine in progress; Slice 1 GREEN**
- Track 7 — Hardware Performance Engine — PLANNED
- Track 8 — Deep Cleaner — PLANNED
- Track 9 — Auto Optimize — PLANNED
- Track 10 — DG UX Migration — PLANNED

## Track 5 closure authority

Track 5 remains closed GREEN for its approved additive universal foundation scope. Closing application SHA is `a0c9a4e30dc48cb1730cee8b6b951f7338680cd1`, Windows CI #1034 / run `34502895182`. Final Track 5 closure docs HEAD `0d7886b6ff19898bcba38585ca6369728bff4145` passed Windows CI #1036 / run `34510474469`.

Track 5 does not claim that every discovered game already has a physical tuning runtime. Universal search/correlation/provenance layers do not create evidence, `Validated`, winners, mutation or persistence authority.

## Track 6 macro architecture — already approved

Authority:

`docs/superpowers/specs/2026-09-06-dg-performance-engine-unified-architecture-design.md`

Do not reopen this architecture unless a real conflict is discovered. Approved implementation sequence:

1. generic workload state machine;
2. universal classifiers;
3. session optimizer actions;
4. learned action reliability;
5. post-session queue.

Approved Guardian semantics remain: state detection → real degradation confirmation → likely-cause classification → workload/state-appropriate `LIVE_SAFE` candidate → micro-snapshot/canary → measured keep or rollback. Inconclusive results roll back. Cooldown and Action Budget prevent thrashing. Quick Boost uses already-validated compatible actions only. Controlled evidence remains stronger than passive Guardian observation.

## Track 6.1 Slice 1 — Generic workload state machine — GREEN

Implementation plan:

`docs/superpowers/plans/2026-09-10-track6-generic-workload-state-machine.md`

Permanent Core production:

`src/FFPerformanceEngine.Core/Services/GenericGuardianWorkloadStateMachine.cs`

Added contracts:

- `GuardianWorkloadState`: `Unresolved`, `Offline`, `Desktop`, `Starting`, `Ready`, `Active`, `Ending`;
- `GuardianWorkloadStateConfidence`: categorical `Unknown`, `Low`, `Medium`, `High`;
- `GenericGuardianWorkloadSignals`: explicit system-online, foreground, render-activity and recent-input inputs;
- `GuardianWorkloadStateSnapshot`: state/confidence + already-resolved `GameIdentity`, exact adapter and `TelemetryWorkloadTarget`;
- `GenericGuardianWorkloadStateMachine.Observe(...)` + `Reset()`.

Behavior and authority:

- reuses `TelemetryWorkloadTargetResolver`; it does not create another PID/path resolver;
- requires exact stable selected `GameId` context before exposing identity/adapter;
- unknown/duplicate identity => fail closed `Unresolved`;
- ambiguous RunningProcess => `Unresolved`, never actionable;
- `KnownExecutable`-only => never live;
- new exact workload/PID => `Starting`;
- same exact process without sufficient activity => `Ready`;
- same exact process with render activity plus foreground or recent input => `Active`;
- process loss after live lifecycle => one `Ending`, then `Desktop`;
- explicit offline => `Offline` and no active-process authorization;
- Reset clears transition memory;
- catalog/evidence remain read-only;
- exact canonical identity/adapter references are preserved when proven.

This Slice intentionally contains no discovery, process enumeration, telemetry capture, universal classifiers, dynamic baseline, mutation, canary, Profile/History/Knowledge persistence, AppServices composition or WPF integration. Existing BlueStacks/FF Guardian implementation is unchanged.

## Track 6.1 Slice 1 TDD / verification

- verifier branch: `ci/track6-generic-workload-state-machine-verify`;
- RED SHA `5a8ca66ab2adf1bf9962bc922f2eeeda274b6679`, verifier run `34521933778`: native passed; managed build failed only on absent new production contracts (`CS0246` / `CS0103`);
- GREEN SHA `0db8de367e4424b375ecfa9d4eb97cfdf1ede575`, verifier run `34522187740`: native + managed + Core + App + publish SUCCESS;
- temporary verifier workflow excluded from official branch;
- official application SHA `725065a90cef2ebd04a9d4d19e703ba46756bcb1`;
- Windows CI #1039 / run `34522642478` SUCCESS including artifact upload.

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

Continue **Track 6 item 1**, not universal classifiers yet. Inspect current exact foreground/window, render/process telemetry and recent-input sources and build the next bounded Slice around an explicit/on-demand generic Guardian observation bridge feeding the already-GREEN state machine for an already-selected stable workload.

The next bridge must reuse existing typed/Track 3–4 authority, fail closed on unavailable/ambiguous targets, perform no discovery at startup and grant no mutation/baseline/profile authority.

Then repeat the canonical TDD/CI/memory cycle before another Slice.
