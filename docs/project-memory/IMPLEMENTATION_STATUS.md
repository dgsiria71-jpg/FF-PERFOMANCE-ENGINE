# DG Performance Engine — Implementation Status Ledger

Current branch code/tests + fresh exact-commit Windows CI are authoritative over older handoffs.

## Current verified application checkpoint

- Branch: `build/initial-product`
- Application HEAD: `c132ec22c1f38fbacaa43ce630098d44674b3565`
- Commit: `feat: add Track 6 universal classifier bridge`
- Windows CI: **#1043 — SUCCESS**
- Run: `34526941137`
- Full official gate passed: native configure/build/test, managed build, Core self-tests, App self-tests, win-x64 publish, artifact upload and cleanup.
- Artifact `FFPerformanceEngine-win-x64`: id `10172045284`, digest `sha256:950048cca361882a110caa413aa1d7de68f7821c13577675f0bd2971be67d51f`.

Previous verified documentary checkpoint:

- Documentary HEAD `687b802dd187233a4637b7f78ac4c452ab925ef6`
- Windows CI **#1042 — SUCCESS**
- Run `34526017491`
- Track 6 item 1 is closed GREEN there.

## Product foundation

DG Performance Engine is an evolution of the working FF Performance Engine Windows product, not a rewrite. Physical `FFPerformanceEngine.App/Core/Native` names remain until controlled migration. C#/.NET 8 + WPF owns presentation/orchestration/policies/profiles/persistence; C++20/Win32 owns native/low-overhead platform work where it materially helps.

## Track state

- Track 0 — Foundation Hardening — GREEN
- Track 1 — Universal Diagnostic Foundation — GREEN
- Track 2 — System Optimizer / evidence authority — GREEN through current branch
- Track 3 — Game Discovery + Adapter Framework — GREEN
- Track 4 — Universal Telemetry / Evidence — GREEN
- Track 5 — Universal Auto Tuner + Profiles — GREEN for current canonical scope
- Track 6 — Adaptive Guardian 2.0 — **ACTIVE; item 1 GREEN; item 2 universal classifiers IN PROGRESS; Slice 1 GREEN**
- Track 7 — Hardware Performance Engine — PLANNED
- Track 8 — Deep Cleaner — PLANNED
- Track 9 — Auto Optimize — PLANNED
- Track 10 — DG UX Migration — PLANNED

## Track 5 closure authority

Track 5 remains closed GREEN for its approved additive universal foundation scope. Closing application SHA `a0c9a4e30dc48cb1730cee8b6b951f7338680cd1`, Windows CI #1034 / run `34502895182`; closure documentary HEAD `0d7886b6ff19898bcba38585ca6369728bff4145`, Windows CI #1036 / run `34510474469`.

Universal search/correlation/provenance does not manufacture evidence, `Validated`, winner, mutation or persistence authority.

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

### Slice 1 — lifecycle foundation

Plan `docs/superpowers/plans/2026-09-10-track6-generic-workload-state-machine.md`.
Core `src/FFPerformanceEngine.Core/Services/GenericGuardianWorkloadStateMachine.cs`.
Application SHA `725065a90cef2ebd04a9d4d19e703ba46756bcb1`, Windows CI #1039 / run `34522642478` SUCCESS.

### Slice 2 — generic observation bridge

Plan `docs/superpowers/plans/2026-09-10-track6-generic-workload-observation.md`.
Core `src/FFPerformanceEngine.Core/Services/GenericGuardianWorkloadObservationService.cs`.
Application SHA `6bb501eab866ee1fb17c546a02b5016ef97cad58`, Windows CI #1041 / run `34525442625` SUCCESS.
Documentary close `687b802dd187233a4637b7f78ac4c452ab925ef6`, Windows CI #1042 / run `34526017491` SUCCESS.

Item 1 now provides stable workload identity, exact runtime target, trustworthy generic observation and conservative lifecycle state with no startup discovery or mutation authority.

## Track 6 item 2 — Universal classifiers — IN PROGRESS

### Slice 1 — typed bottleneck classifier bridge — GREEN

Plan: `docs/superpowers/plans/2026-09-10-track6-universal-classifier-bridge.md`.

Core: `src/FFPerformanceEngine.Core/Services/GenericGuardianBottleneckClassifier.cs`.

Permanent behavior:

- `GenericGuardianBottleneckClassification` retains the exact source `GenericGuardianWorkloadObservation`, analyzer result and explanatory reason;
- `GenericGuardianBottleneckClassifier` is a Guardian policy boundary over the existing Track 4 `UniversalBottleneckAnalyzer`, not a competing analyzer;
- classification is allowed only for `GuardianWorkloadState.Active`;
- exact capturable `TelemetryWorkloadTarget` and non-null typed frame are mandatory;
- eligible input delegates unchanged to `UniversalBottleneckAnalyzer.Analyze(TelemetryFrame, BottleneckAnalysisContext)`;
- missing GPU evidence cannot be treated as GPU headroom for CPU causality;
- Partial or low-coverage causal telemetry remains `Unknown` under existing analyzer rules;
- analyzer `Unknown` remains `Unknown` with no fallback cause fabricated by Guardian;
- non-Active states, forged Active without exact target and Active without frame fail closed;
- observation/frame/context remain read-only;
- no `Validated`, action, recommendation, canary, Profile, History, Guardian Knowledge or persistence authority is created.

TDD / verification:

- verifier branch `ci/track6-universal-classifier-bridge-verify`;
- RED SHA `7f0c651d55ed33e5708af49526104c6d5c753f80`, run `34526434062`: native passed; managed failed only with 9 intentional `CS0246` for absent classifier contract, 0 warnings;
- GREEN SHA `7acc44f96f8dd8fa3e14340512a6453c518b7c6b`, run `34526631371`: native + managed + Core + App + publish SUCCESS;
- temporary verifier workflow excluded from official branch;
- application SHA `c132ec22c1f38fbacaa43ce630098d44674b3565`;
- Windows CI #1043 / run `34526941137` SUCCESS including artifact upload.

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

Continue Track 6 item 2. Compare the approved Guardian classifier families against the currently proven typed telemetry and `BottleneckKind` coverage. The next bounded Slice must extend classification only where evidence authority is real, remain read-only and preserve explicit `Unknown` for unsupported/unproven families.

Do not jump to item 3 session optimizer actions until item 2 has a complete capability-honest classifier path.
