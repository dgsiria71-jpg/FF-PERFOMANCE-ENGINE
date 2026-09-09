# Current Handoff — 2026-09-09

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR: `#1` — open/draft to `main`
- Product: **DG Performance Engine**, evolved from the existing FF Performance Engine without rewrite or mass rename.
- Current Git/code/tests + fresh exact Windows CI outrank stale chat or memory text.

## Current exact verified application checkpoint

- Application HEAD: `797c8c7766adea3369948d9cb330bb7ba9a69d52`
- Commit: `feat: add capability-honest universal tuning search space`
- Windows CI: **#1000 — SUCCESS**
- CI run id: `34411645032`

The exact #1000 job passed checkout/setup, native configure/build/tests, managed build, full Core self-tests, permanent App self-tests, `win-x64` publish, artifact upload and cleanup.

Checkpoint record:

`docs/project-memory/checkpoints/2026-09-09-track5-universal-search-space.complete`

Memory-sync commits after the application SHA are docs-only and do not replace the application checkpoint above as code authority.

## Track state

- Track 0 — Foundation Hardening: **GREEN**
- Track 1 — Universal Diagnostic Foundation: **GREEN**
- Track 2 — System Optimizer / evidence authority: **GREEN through current branch**
- Track 3 — Game Discovery + Adapter Framework: **GREEN**
- Track 4 — Universal Telemetry / Evidence: **GREEN — canonical scope completed**
- Track 5 — Universal Auto Tuner + Profiles: **ACTIVE**
  - Slice 1 — universal search-space + system-dimension bridge: **GREEN**
- Track 6+ — planned; follow `ROADMAP.md` and the unified architecture.

## Track 4 authority preserved

Track 4 closed at application SHA:

`71991379e01518adf2e1c539491a9c0339a56735`

Windows CI #993 / run `34407420906` SUCCESS.

Core invariants that Track 5 must preserve:

- immutable typed `TelemetryFrame` with explicit source/quality/coverage/origin;
- missing telemetry remains absent/Unknown, never synthetic zero or implicit headroom;
- coverage is producer-local completeness, never confidence;
- stable workload identity comes only from source-native GameId contracts;
- PID/path/process/display name are transient evidence only;
- `KnownExecutable` never authorizes live process capture;
- explicit universal workload selection owns Performance capture routing;
- selected unavailable/ambiguous workload blocks capture instead of silently falling back to another Guardian workload;
- no universal selection preserves the legacy Guardian/BlueStacks typed route;
- `Observed != Validated`;
- Global Controlled Benchmark Lease, exact fingerprint/freshness and durable validation/recommendation authority remain unchanged;
- no startup workload discovery;
- no anti-cheat/integrity bypass.

## Track 5 Slice 1 — Universal tuning search space — GREEN

### Purpose

Create the smallest additive universal candidate/search-space abstraction without rewriting the existing BlueStacks/FF Auto Tuner.

The existing legacy path remains intact:

- `TuningCandidate` still contains BlueStacks/FF-specific CPU cores, RAM, renderer, FPS target and resolution;
- `AutoTunerEngine.GenerateCandidates(...)` remains source-compatible;
- `AutoTunerSessionService` remains the specialized BlueStacks/FF runtime/session path;
- profile persistence and winner authority remain unchanged.

### Neutral contracts

Added:

- `UniversalTuningDimensionScope { System, Workload }`;
- `UniversalTuningDimension`;
- `UniversalTuningCandidate`;
- `UniversalTuningSearchSpacePolicy`;
- `UniversalTuningSearchSpacePlanner`;
- `UniversalTuningSystemDimensionFactory`.

### Search-space invariants

A universal dimension requires:

- explicit nonblank `Id`;
- explicit nonblank `AuthorityId`;
- one or more explicit candidate values.

Fail closed:

- blank dimension id;
- blank authority id;
- empty candidate-value list;
- blank candidate value;
- duplicate dimension id case-insensitively;
- duplicate candidate value.

Zero declared dimensions produce **zero candidates**, not a fabricated empty/default candidate.

The planner:

1. validates the complete input;
2. sorts dimensions deterministically by id;
3. preserves each authority's declared candidate-value order exactly;
4. builds the Cartesian product with the last sorted dimension varying fastest;
5. truncates only at deterministic `MaxCandidates`;
6. never randomizes the explored prefix;
7. never attaches hidden/default axes;
8. never attaches evidence, confidence or recommendation authority.

### Windows system-dimension bridge

`UniversalTuningSystemDimensionFactory.FromWindowsCandidatePlan(...)` consumes the already-existing Track 2 `WindowsCapabilityCandidatePlan`.

Only `plan.CanExplore == true` becomes a universal System dimension.

Therefore these remain absent:

- `Unavailable`;
- `MissingCurrentState`;
- `NoCandidateSpace`;
- nominal `Ready` with zero candidates.

For a valid plan:

- `CapabilityId` becomes both dimension identity and authority identity;
- targets remain exact producer `TargetValue` strings;
- ordering follows `ExplorationRank`;
- duplicate targets fail closed.

The bridge does **not**:

- inspect the Windows capability registry;
- create new schema points;
- infer availability;
- mutate Windows;
- consult recommended values or recommendation confidence;
- publish recommendation authority.

### Closed authority boundary

**Support/search space is exploration only. It is not recommendation, validation, winner status or persistence authority.**

The existing evidence chain remains authoritative:

```text
candidate/support space
→ controlled measurement
→ typed evidence
→ repeatability/evaluation
→ PendingValidation where applicable
→ fresh validation challenge
→ ValidatedEvidence
→ winner/recommendation authority
```

Track 5 is not allowed to shortcut that chain.

## TDD provenance for Slice 1

Temporary proving branch:

`ci/track5-universal-search-space-verify`

The temporary verifier workflow was deliberately excluded from the official branch.

Observed RED/GREEN sequence:

- Task 1 RED — run `34410887507`: compile failed because `UniversalTuningDimensionScope`, `UniversalTuningDimension` and `UniversalTuningCandidate` did not exist;
- Task 1 GREEN — verifier #3 on `6feb03b019008092868602c6400e9272ab968200`: Core + App.SelfTest + WPF SUCCESS;
- Task 2 RED — run `34411302456`: compile failed only because `UniversalTuningSystemDimensionFactory` did not exist;
- Task 2 GREEN — verifier #5 / run `34411435761` on `5c50b2a268246feefcfc2ba190176f9231d52d83`: Core + App.SelfTest + WPF SUCCESS;
- selective atomic official integration created `797c8c7766adea3369948d9cb330bb7ba9a69d52` without the temporary verifier workflow;
- official Windows CI #1000 then passed the exact integrated SHA.

Tests are registered explicitly from the permanent Core self-test `Program.cs`; no `ModuleInitializer` was introduced.

## Non-negotiable invariants still active

- `Observed != Validated`.
- Candidate support/search space is not recommendation space.
- Unsupported/unproven tuning dimensions are absent, never guessed.
- Current exact machine/environment fingerprint and freshness remain mandatory where authority requires them.
- Global Controlled Benchmark Lease semantics remain unchanged.
- Stable workload identity never comes from PID/path/process/display name.
- Game discovery remains explicit/on-demand; never add it to `AppServices.InitializeAsync()`.
- No hidden tuning side effect is introduced by search-space construction.
- Existing BlueStacks/FF Auto Tuner remains the first specialized implementation and stays source-compatible until a separately tested migration explicitly changes it.

## Exact next action

Continue **Track 5** with the next additive slice: capability-honest workload/game dimensions.

Before implementation:

1. locate and read the existing Track 3 generic/specialized Game Adapter contracts in the current repository;
2. identify what those adapters already prove about supported workload configuration/capabilities;
3. do **not** create a second game-option catalog;
4. define a workload-dimension seam only where an adapter can explicitly declare dimension identity, authority and candidate values;
5. do not assume renderer, graphics quality, resolution, FPS target or other semantics are universal across games;
6. RED first for unsupported/ambiguous/duplicate declarations and deterministic composition with the already-GREEN universal search-space planner;
7. preserve the existing BlueStacks/FF tuner path until a later specialized adapter composition slice has its own RED/GREEN proof;
8. isolated verifier → selective official integration → exact Windows CI → memory synchronization.
