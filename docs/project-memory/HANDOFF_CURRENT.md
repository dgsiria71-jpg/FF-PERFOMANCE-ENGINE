# Current Handoff — 2026-09-09

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR: `#1` — open/draft to `main`
- Product: **DG Performance Engine**, evolved from the existing FF Performance Engine without rewrite or mass rename.
- Current Git/code/tests + fresh exact Windows CI outrank stale chat or memory text.

## Current exact verified application checkpoint

- Application HEAD: `8dac70fdb2c693533ae481aaadd846ab84fde228`
- Commit: `feat: add capability-honest game adapter tuning dimensions`
- Windows CI: **#1007 — SUCCESS**
- CI run id: `34416726382`

The exact #1007 job passed checkout/setup, native configure/build/tests, managed build, full Core self-tests, permanent App self-tests, `win-x64` publish, artifact upload and cleanup.

Checkpoint record:

`docs/project-memory/checkpoints/2026-09-09-track5-game-adapter-tuning-dimensions.complete`

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
- Track 6+ — planned; follow `ROADMAP.md` and the unified architecture.

## Track 4 authority preserved

Track 4 closed at application SHA `71991379e01518adf2e1c539491a9c0339a56735`, Windows CI #993 / run `34407420906` SUCCESS.

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

Application SHA `797c8c7766adea3369948d9cb330bb7ba9a69d52`, Windows CI #1000 / run `34411645032` SUCCESS.

Added neutral contracts:

- `UniversalTuningDimensionScope { System, Workload }`;
- `UniversalTuningDimension`;
- `UniversalTuningCandidate`;
- `UniversalTuningSearchSpacePolicy`;
- `UniversalTuningSearchSpacePlanner`;
- `UniversalTuningSystemDimensionFactory`.

The planner is pure, deterministic and bounded. It accepts only explicit dimension identity, authority and values; invalid/duplicate declarations fail closed; zero dimensions produce zero candidates; support/search space is exploration only, never recommendation/validation/winner/persistence authority.

The system bridge consumes existing Track 2 `WindowsCapabilityCandidatePlan` and accepts only `CanExplore == true`; it does not rediscover, mutate or infer Windows state and does not consult recommendation confidence/value as search authority.

## Track 5 Slice 2 — Game-adapter workload dimensions — GREEN

### Purpose

Allow specialized Game Adapters to contribute workload-specific search-space dimensions without making any game option universal and without rewriting the existing BlueStacks/FF tuner.

### Optional adapter contract

Added to the Track 3 adapter layer without changing `IGameAdapter`:

- `GameAdapterTuningDimensionDeclaration`;
- optional `IGameTuningDimensionProvider`.

`GenericGameAdapter` and the current `BlueStacksFreeFireGameAdapter` deliberately do **not** implement this provider in this slice. Existing adapter implementations therefore remain source-compatible.

### Resolved adapter authority

Added `UniversalTuningWorkloadDimensionFactory`.

Rules:

1. take the caller-selected stable `GameIdentity`;
2. resolve authority only through `GameAdapterResolver`;
3. Generic/unregistered specialization => zero workload dimensions;
4. adapter without `IGameTuningDimensionProvider` => zero dimensions;
5. require `ConfigDiscovery + ConfigSnapshot + ConfigMutation + Rollback` before the provider is invoked;
6. provider receives the exact stable `GameIdentity` supplied by the caller;
7. local id is normalized only for identity and namespaced as `workload.<adapter-id>.<local-id>`;
8. `AuthorityId` is the normalized id of the **resolved** adapter;
9. candidate values preserve exact provider text and order;
10. dimensions are returned deterministically by final id.

Fail closed:

- null provider result;
- null declaration;
- blank local id;
- empty value list;
- blank value;
- duplicate local ids case-insensitively;
- duplicate exact candidate values.

No PID/path/process/display name can create tuning authority.

### Composition

No second search-space compositor was needed. The already-GREEN `UniversalTuningSearchSpacePlanner` directly composes explicit System + Workload dimensions into the deterministic Cartesian space. No hidden/default game axis is added.

### Authority boundary

**Adapter-declared workload support is exploration only.** It does not grant measured evidence, confidence, recommendation, winner role, permission to persist, or permission to mutate.

The existing authority chain remains:

```text
explicit support/search space
→ controlled measurement
→ typed evidence
→ repeatability/evaluation
→ freshness/fingerprint
→ validation challenge where applicable
→ ValidatedEvidence
→ winner/recommendation authority
```

### TDD provenance

Temporary proving branch: `ci/track5-game-adapter-dimensions-verify`.

- Task 1 RED — verifier #1 / run `34412460197`: compile failed only because `IGameTuningDimensionProvider` and `GameAdapterTuningDimensionDeclaration` did not exist;
- Task 1 GREEN — verifier #3 / run `34412615403` on `997fc7d3c9c73923a15ea3a9d4975d82b8e1b4fa`: Core + App.SelfTest + WPF SUCCESS;
- Task 2 RED — verifier #4 / run `34412791339`: compile failed only because `UniversalTuningWorkloadDimensionFactory` did not exist;
- Task 2/3 GREEN — verifier #5 / run `34412911108` on `56bfd6c0ef05c70e6148ad5a411313c627be7dbd`: Core + App.SelfTest + WPF SUCCESS, including deterministic System + Workload composition;
- selective atomic official integration created `8dac70fdb2c693533ae481aaadd846ab84fde228` and excluded the temporary verifier workflow;
- official Windows CI #1007 / run `34416726382` passed the exact integrated SHA.

Permanent tests are called explicitly from Core self-test `Program.cs`; no new `ModuleInitializer` was introduced.

## Non-negotiable invariants still active

- `Observed != Validated`.
- Candidate support/search space is not recommendation space.
- Unsupported/unproven tuning dimensions are absent, never guessed.
- Game option semantics are adapter-owned; renderer/quality/resolution/FPS/etc. are not assumed universal.
- Workload tuning authority comes from the adapter resolved for stable `GameIdentity`, never from PID/path/process/display name.
- A provider without reversible config lifecycle cannot contribute dimensions.
- Current exact machine/environment fingerprint and freshness remain mandatory where authority requires them.
- Global Controlled Benchmark Lease semantics remain unchanged.
- Game discovery remains explicit/on-demand; never add it to `AppServices.InitializeAsync()`.
- No hidden tuning side effect is introduced by search-space construction.
- Existing BlueStacks/FF Auto Tuner remains the first specialized implementation and stays source-compatible until a separately tested migration explicitly changes it.

## Exact next action

Continue **Track 5** with the next additive slice: connect the existing specialized BlueStacks/FF candidate space to the universal abstraction **without duplicating or replacing its environment/instance-specific candidate generator**.

Required sequence:

1. read the current `AutoTunerEngine.GenerateCandidates(...)`, `TuningCandidate`, BlueStacks runtime/session and config snapshot/mutation contracts;
2. identify the exact machine + BlueStacks-instance inputs that make its candidate space dynamic;
3. design an additive bridge/provider seam that reuses the existing generator as authority instead of inventing static BlueStacks options;
4. preserve the legacy candidate/runtime/session path and five winner roles unchanged until later winner/profile migration;
5. prove mapping/correlation between legacy specialized candidates and neutral universal dimensions/candidates without losing reversibility or identity;
6. RED first, isolated verifier, then GREEN;
7. selective official integration → exact Windows CI → memory synchronization.
