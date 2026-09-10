# Current Handoff — 2026-09-09

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR: `#1` — open/draft to `main`
- Product: **DG Performance Engine**, evolved from the existing FF Performance Engine without rewrite or mass rename.
- Current Git/code/tests + fresh exact Windows CI outrank stale chat or memory text.

## Current exact verified application checkpoint

- Application HEAD: `39246089fb28f510287e79639356a4e16d1b6b02`
- Commit: `feat: bridge dynamic BlueStacks candidates into universal tuning space`
- Windows CI: **#1014 — SUCCESS**
- CI run id: `34422254555`

The exact #1014 job passed checkout/setup, native configure/build/tests, managed build, full Core self-tests, permanent App self-tests, `win-x64` publish, artifact upload and cleanup.

Checkpoint record:

`docs/project-memory/checkpoints/2026-09-09-track5-bluestacks-universal-candidate-bridge.complete`

Memory-sync commits after the application SHA are docs-only and do not replace the application checkpoint above as code authority.

## Track state

- Track 0 — Foundation Hardening: **GREEN**
- Track 1 — Universal Diagnostic Foundation: **GREEN**
- Track 2 — System Optimizer / evidence authority: **GREEN through current branch**
- Track 3 — Game Discovery + Adapter Framework: **GREEN**
- Track 4 — Universal Telemetry / Evidence: **GREEN — canonical scope completed**
- Track 5 — Universal Auto Tuner + Profiles: **ACTIVE**
  - Slice 1 — universal search-space + system-dimension bridge: **GREEN**
  - Slice 2 — capability-honest game-adapter workload dimensions: **GREEN**
  - Slice 3 — dynamic BlueStacks/FF universal candidate bridge: **GREEN**
- Track 6+ — planned; follow `ROADMAP.md` and the unified architecture.

## Authority inherited from Tracks 2–4

Track 5 must continue preserving:

- immutable typed `TelemetryFrame` with explicit source/quality/coverage/origin;
- missing telemetry remains absent/Unknown, never synthetic zero or implicit headroom;
- coverage is producer-local completeness, never confidence;
- stable workload identity comes only from source-native GameId contracts;
- PID/path/process/display name are transient evidence only;
- `KnownExecutable` never authorizes live process capture;
- explicit universal workload selection owns Performance capture routing;
- selected unavailable/ambiguous workload blocks capture instead of silently falling back to another Guardian workload;
- `Observed != Validated`;
- Global Controlled Benchmark Lease, exact fingerprint/freshness, durable validation/recommendation authority and rollback/History remain unchanged;
- no startup workload discovery;
- no anti-cheat/integrity bypass.

## Track 5 Slice 1 — Universal tuning search space — GREEN

Application SHA `797c8c7766adea3369948d9cb330bb7ba9a69d52`, Windows CI #1000 / run `34411645032` SUCCESS.

Added neutral `UniversalTuningDimensionScope`, `UniversalTuningDimension`, `UniversalTuningCandidate`, `UniversalTuningSearchSpacePolicy`, `UniversalTuningSearchSpacePlanner` and `UniversalTuningSystemDimensionFactory`.

The planner is pure, deterministic and bounded. Explicit support/search space is exploration only; it is not recommendation/validation/winner/persistence authority.

## Track 5 Slice 2 — Game-adapter workload dimensions — GREEN

Application SHA `8dac70fdb2c693533ae481aaadd846ab84fde228`, Windows CI #1007 / run `34416726382` SUCCESS.

Added optional `GameAdapterTuningDimensionDeclaration` / `IGameTuningDimensionProvider` and pure `UniversalTuningWorkloadDimensionFactory` without changing `IGameAdapter`. Workload dimension authority comes only from the adapter resolved for stable `GameIdentity`; generic/unregistered/no-provider/incomplete reversible-lifecycle cases produce zero dimensions.

The current BlueStacks/FF adapter deliberately did not receive invented static options because its candidate space is machine/instance-dependent.

## Track 5 Slice 3 — Dynamic BlueStacks/FF universal candidate bridge — GREEN

### Purpose

Connect the already-working machine/instance-dependent BlueStacks/FF candidate generator to the neutral Track 5 model without duplicating it, rebuilding a second option catalog, changing the runtime/session path or granting new evidence/recommendation authority.

### Permanent contracts

Added:

- `BlueStacksUniversalTuningCandidateBinding`;
- `BlueStacksUniversalTuningCandidateSpace`;
- `BlueStacksUniversalTuningCandidateBridge`.

The exact specialized `TuningCandidate` remains attached to each neutral `UniversalTuningCandidate`; the binding set, not a recomputed Cartesian product of dimension marginals, is the authoritative set of runnable specialized candidates for this bridge.

### Source-of-truth rules

1. Stable workload identity comes from `LegacyGameIdentityBridge.FromGameKind(...)`.
2. Authority must resolve through `GameAdapterResolver` to the matching `BlueStacksFreeFireGameAdapter` for `FreeFire` or `FreeFireMax`.
3. The bridge requires the executable candidate lifecycle capabilities used by the specialized path.
4. `AutoTunerEngine.GenerateCandidates(environment, instance, mode)` remains the only generator.
5. Generator order and existing Adaptive/Deep budgets remain authoritative.
6. Each generated candidate is checked through `BlueStacksAutoTunerRuntime.BuildCandidatePlan(candidate, instance, capturedSettings)`.
7. Filtering may remove unrepresentable candidates but may never add, reorder or regenerate candidates.
8. Descriptive workload dimensions are projected from the surviving exact bindings under `workload.<resolved-adapter-id>.*`.
9. Those dimension marginals must never be independently Cartesian-expanded to manufacture combinations that the specialized generator did not emit.
10. No evidence/confidence/winner/recommendation/persistence authority is attached by the bridge.

### Installed snapshot / renderer correlation

The caller supplies captured allow-listed BlueStacks instance settings. Missing/mismatched instance snapshot state fails closed.

A TDD-discovered correlation bug was closed: `graphics_renderer` / `graphics_engine` is capturable but renderer mutation is not currently verified. Therefore, if captured installed renderer is known and differs from a generated non-`Auto` renderer, that candidate is rejected instead of attributing measured evidence to a renderer the runtime cannot actually apply.

No renderer mutation was added.

### Compatibility preserved

No changes were made to:

- `AutoTunerRunCoordinator`;
- `AutoTunerSessionService`;
- `BlueStacksAutoTunerRuntime` execution semantics;
- PresentMon typed benchmark authority;
- existing five winner roles;
- Custom Validated challenge/promotion;
- profile persistence;
- Global Controlled Benchmark Lease;
- rollback/History.

### TDD provenance

Temporary proving branch: `ci/track5-bluestacks-universal-candidate-bridge-verify`.

- initial RED — verifier #1 / run `34417641306`: missing bridge/binding contracts only;
- intermediate test correction: a record-equality assertion accidentally compared `GameIdentity.Sources` arrays by reference; it was narrowed to the stable identity fields without production change;
- Task 1 GREEN — verifier #4 / run `34417996142`, SHA `e434f8e2a83f466408d91ab6e90a980b9de3d7e7`: Core + App.SelfTest + WPF SUCCESS;
- Task 2 RED — verifier #5 / run `34418189031`, SHA `888cd58d59fe845304e428a79653989499d39c64`: captured renderer drift was incorrectly admitted;
- final GREEN — verifier #6 / run `34418407183`, SHA `07180bd58fc5ff0b01ef3b9038056fe2693d30bb`: Core + App.SelfTest + WPF SUCCESS;
- selective atomic official integration copied only permanent plan/Core/test blobs and excluded the verifier workflow;
- official application SHA `39246089fb28f510287e79639356a4e16d1b6b02` passed Windows CI #1014 / run `34422254555`.

Permanent tests are called explicitly from Core self-test `Program.cs`; no new `ModuleInitializer` was introduced.

## Non-negotiable invariants still active

- `Observed != Validated`.
- Candidate support/search space is not recommendation space.
- Exact universal↔specialized binding proves correspondence/applicability only, not benefit.
- Unsupported/unproven tuning dimensions/candidates are absent, never guessed.
- Game option semantics are adapter-owned; renderer/quality/resolution/FPS/etc. are not assumed universal.
- Workload tuning authority comes from stable identity + resolved adapter, never PID/path/process/display name.
- Renderer remains read/correlation-only until a separate verified reversible mutation path exists.
- Current exact machine/environment fingerprint and freshness remain mandatory where authority requires them.
- Global Controlled Benchmark Lease semantics remain unchanged.
- Game discovery remains explicit/on-demand; never add it to `AppServices.InitializeAsync()`.
- Existing BlueStacks/FF Auto Tuner remains the first specialized implementation and stays source-compatible until a separately tested migration explicitly changes it.

## Exact next action

Continue **Track 5** with the next additive slice from the existing roadmap: generalize the evidence-backed winner/profile output boundary while preserving the current five BlueStacks/FF winner roles and Custom Validated authority.

Required sequence:

1. read current `CandidateEvidence`, `TuningResult`, `PerformanceProfile`, `AutoTunerEngine.SelectWinners(...)`, `AutoTunerSessionService` persistence and Profile Challenge promotion/freshness contracts;
2. identify the smallest neutral result/evidence/profile seam that can carry stable workload identity + universal candidate/config context without weakening the existing specialized result;
3. preserve direct typed PresentMon evidence, repeatability, exact configuration correlation, fingerprint/freshness and `Observed != Validated`;
4. do not promote a neutral candidate merely because it exists in Slice 1–3 search metadata;
5. preserve the existing five winner roles exactly for the BlueStacks/FF compatibility path;
6. preserve Custom Validated challenge/promotion authority and incumbent freshness rules;
7. TDD RED first on an isolated verifier branch;
8. GREEN verifier → selective official integration → exact Windows CI → synchronize all relevant memory → validate the final documentary HEAD before the following increment.
