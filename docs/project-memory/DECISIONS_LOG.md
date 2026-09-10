# DG Performance Engine — Closed Decisions Log

This file records decisions that were explicitly closed in project chats/specs and verified implementation slices. Reopen only with a new explicit decision and document why.

## Identity and continuity

- Official product name: **DG Performance Engine**.
- Do not restart/rewrite the project to adopt the new name.
- Preserve existing `FFPerformanceEngine.App/Core/Native` projects until a controlled migration.
- Free Fire / BlueStacks is the first specialized Game Adapter and keeps all validated behavior.
- Branch of active development: `build/initial-product`; PR #1 remains draft until critical architecture is proven.

## Engineering/process

- Native Windows product: C#/.NET 8 + WPF + C++20/Win32.
- Mandatory increment cycle: **docs/memory/context → TDD RED/GREEN → exact application CI → synchronize all relevant memory → exact documentary-HEAD CI → next increment**.
- No production code for a new behavior before its failing test has been observed.
- No completion/GREEN claims without exact-commit CI evidence.
- Repository `AGENTS.md` + `docs/project-memory/*` form the durable continuation protocol.
- Current Git/code/tests + fresh exact Windows CI outrank stale memory text; stale docs are synchronized after a materially GREEN checkpoint.
- No fabricated metrics, defaults, capabilities, paths, sensor values, game executables or causal claims.
- UI presents/requests; Core/application service layers own policies and decisions.
- Global Controlled Benchmark Lease serializes machine-wide controlled measurements/mutations that would contaminate each other.

## Modes

- Equilibrado.
- Desempenho — default.
- Extremo — aggressive but evidence-driven.
- Expert exposes manual advanced controls where the platform actually supports them.
- Safety Envelope is optional in Expert; risk warnings remain explicit.
- Critical instability may force emergency rollback.

## Evidence / profiles

- A/B is based on frozen snapshots, not a moving baseline.
- Aggregates are recomputed from copied evidence points; metadata cannot fabricate FPS/frame-time.
- `Observed != Validated`.
- Five main specialized winner roles remain: Recommended, Maximum FPS, Lowest Latency, Stability, Quality.
- Custom Validated may challenge those incumbents with fresh controlled evidence.
- Environment/fingerprint/freshness drift invalidates reuse where appropriate.
- Automatic Windows recommendation publication accepts only authorized diagnostic or ValidatedEvidence paths; raw ControlledEvidence cannot bypass validation.
- Typed Track 4 telemetry does not weaken any authority gate.

## Windows optimization

- Model changes as formal `Windows Performance Capability` entries, not tweak scripts.
- Every transaction validates dependencies and availability before mutation.
- Snapshot all mutations before first Apply.
- Apply/verify in graph order; rollback reverse order and verify restoration.
- Preserve exact OEM/custom prior state; never assume Balanced.
- Persistent Optimize flow is Analyze → Preview → fresh revalidation → transaction → verify → History → Restore.
- Candidate support space is not recommendation space.

## Game Discovery / adapters

- Discovery is explicit, read-only and not run from `InitializeAsync()`.
- Stable launcher/source-native IDs are identity. Names/exes/folders/PIDs are evidence only.
- Generic adapter must remain capability-honest.
- BlueStacks discovery never starts the emulator merely to scan packages.
- Steam uses stable `steam:<appid>` and does not execute Steam merely to discover.
- Epic uses catalog namespace + catalog item id.
- Riot uses `product.patchline` metadata id.
- Battle.net uses `product_code`; `uid` is evidence.
- No anti-cheat/integrity bypass architecture.

## Track 4 telemetry semantics

- One typed telemetry engine may have many collectors; per-metric provenance/quality/coverage/origin are explicit.
- Missing channels remain absent/Unavailable/Unknown; never synthesize zero or healthy headroom.
- Coverage is producer-local measurement completeness, not probability/confidence.
- `TelemetrySample` remains compatibility state; typed authority is not inferred from free-form `DataQuality` strings.
- PID/path/process/display names are transient runtime evidence, never durable GameId.
- `KnownExecutable` proves only a known executable path and never authorizes a live process capture.
- Additional VRAM/thermal/I/O/network telemetry is capability-driven enrichment only when a real supported provider + consumer exists.

## Track 4 universal Performance capture routing — closed 2026-09-09

- explicit selected stable universal GameId has capture-route precedence over Guardian;
- runtime target comes only from that selected workload's bound `RunningProcess` evidence via `TelemetryWorkloadTargetResolver`;
- exactly one valid PID/path may be captured;
- duplicate evidence for the same PID is equivalent, not ambiguity;
- zero valid running targets blocks capture as unavailable;
- more than one distinct valid target blocks capture as ambiguous;
- `KnownExecutable`/App Paths never turns an unavailable workload into a live target;
- explicit selected unavailable/ambiguous workload must not silently fall back to Guardian/BlueStacks;
- only no universal selection preserves the legacy Guardian/BlueStacks typed route;
- WPF displays route state but does not own identity/fallback policy;
- universal capture uses direct typed telemetry and does not promote legacy sample fallback.

Verification: application SHA `71991379e01518adf2e1c539491a9c0339a56735`; Windows CI #993 / run `34407420906` SUCCESS.

## Track 4 closure — closed 2026-09-09

- Track 4 is GREEN for the current canonical scope after universal Performance capture passed Windows CI #993.
- Future real provider/sensor enrichment does not reopen Track 4 by default.
- Track 5 — Universal Auto Tuner + Profiles — is the active canonical engineering track.

## Track 5 universal search-space authority — closed 2026-09-09

- Universal tuning is additive; the working BlueStacks/FF candidate/runtime path is preserved until separately tested migration.
- Every universal dimension requires explicit identity, explicit authority and explicit candidate values.
- Unsupported/unavailable/missing-current-state/no-candidate dimensions remain absent rather than receiving defaults.
- Zero dimensions produce zero candidates; no implicit default candidate.
- Blank/duplicate dimensions or values fail closed.
- Universal candidate enumeration is deterministic and bounded.
- The planner performs no discovery/mutation/evidence/recommendation work.
- Windows system dimensions enter only through existing Track 2 `WindowsCapabilityCandidatePlan` with `CanExplore == true`.
- Candidate support/search space is exploration only, not recommendation, validation, winner or persistence authority.
- Workload dimensions must come from explicit adapter authority; do not create a generic game-option catalog or assume renderer/quality/resolution/FPS semantics across workloads.
- No discovery/tuning side effect is added to startup.

Verification: application SHA `797c8c7766adea3369948d9cb330bb7ba9a69d52`; Windows CI #1000 / run `34411645032` SUCCESS.

## Track 5 adapter-owned workload tuning authority — closed 2026-09-09

- `IGameAdapter` remains unchanged/source-compatible.
- Optional `GameAdapterTuningDimensionDeclaration` + `IGameTuningDimensionProvider` carry workload search metadata only.
- Workload tuning authority is resolved only by `GameAdapterResolver` for stable selected `GameIdentity`.
- Generic/unregistered/no-provider adapters contribute zero dimensions.
- Provider metadata is not consulted unless resolved adapter proves `ConfigDiscovery + ConfigSnapshot + ConfigMutation + Rollback`.
- `BenchmarkPreparation` is not required merely to declare explorable support; controlled benchmark orchestration remains separate.
- Final id is `workload.<normalized-adapter-id>.<normalized-local-id>` and `AuthorityId` is the resolved adapter id.
- Candidate text/order is preserved exactly.
- Null/blank/empty/duplicate declarations fail closed.
- System + Workload composition reuses `UniversalTuningSearchSpacePlanner`; no second compositor or hidden game axis.
- Current BlueStacks/FF adapter does not receive invented static declarations because its candidate space is machine/instance-dependent.
- Adapter-declared support grants no evidence, confidence, mutation, persistence, winner or recommendation authority.

Verification: application SHA `8dac70fdb2c693533ae481aaadd846ab84fde228`; Windows CI #1007 / run `34416726382` SUCCESS.

## Track 5 dynamic BlueStacks/FF universal candidate authority — closed 2026-09-09

Previous risk:

- the current BlueStacks/FF search space is generated dynamically from machine + instance state; independently rebuilding its CPU/RAM/FPS/resolution/renderer Cartesian space in the universal layer could create combinations the specialized generator never emitted, lose its `Take(12/96)` budget/order, or claim applicability unsupported by the installed BlueStacks build.

Closed decision:

- `AutoTunerEngine.GenerateCandidates(EnvironmentSnapshot, BlueStacksInstance?, AutoTunerMode)` remains the **single source of truth** for existing BlueStacks/FF candidate generation;
- the neutral layer adds `BlueStacksUniversalTuningCandidateBinding`, `BlueStacksUniversalTuningCandidateSpace` and a pure/read-only `BlueStacksUniversalTuningCandidateBridge`;
- stable identity comes from `LegacyGameIdentityBridge`; authorization requires `GameAdapterResolver` to resolve the exact matching `BlueStacksFreeFireGameAdapter` for `FreeFire` or `FreeFireMax`;
- the bridge must preserve source candidate order and existing Adaptive/Deep bounds; it may filter unsupported candidates but may never add/reorder/regenerate them;
- installed-build applicability remains owned by `BlueStacksAutoTunerRuntime.BuildCandidatePlan(...)` evaluated against captured allow-listed instance settings;
- universal workload dimensions are **descriptive marginals of the surviving exact bindings** and must not be Cartesian-expanded to manufacture runnable candidates;
- each `UniversalTuningCandidate` remains bound one-to-one to its exact specialized `TuningCandidate`; that binding set is the authoritative runnable/correlated set for the bridge;
- candidate mapping carries no evidence/confidence/Observed/Validated/winner/recommendation/persistence authority;
- caller-provided captured instance config is required; missing/mismatched instance snapshot state fails closed;
- renderer has a stricter correlation rule: captured `graphics_renderer` / `graphics_engine` may prove installed state, but current runtime has no verified renderer mutation. A generated non-`Auto` candidate whose renderer conflicts with captured installed renderer is therefore excluded;
- **no renderer mutation** is authorized by this slice;
- runtime/session/five winner roles/Custom Validated/persistence/typed evidence/lease/rollback/History remain unchanged.

Reason:

- preserve the tested specialized generator as the only candidate authority while introducing neutral correlation;
- prevent dimension marginals from expanding into fabricated candidate combinations;
- keep applicability tied to the actual installed BlueStacks allow-list and reversible runtime path;
- prevent evidence from being attributed to a renderer the runtime cannot actually apply;
- preserve strict separation between exploration metadata and validated winner/recommendation authority.

TDD / verification:

- initial RED: verifier #1 / run `34417641306` — bridge/binding contracts absent;
- Task 1 GREEN: verifier #4 / run `34417996142` on `e434f8e2a83f466408d91ab6e90a980b9de3d7e7`;
- Task 2 RED: verifier #5 / run `34418189031` on `888cd58d59fe845304e428a79653989499d39c64` — renderer drift admitted;
- final verifier GREEN: #6 / run `34418407183` on `07180bd58fc5ff0b01ef3b9038056fe2693d30bb`;
- official application SHA `39246089fb28f510287e79639356a4e16d1b6b02`;
- Windows CI #1014 / run `34422254555` SUCCESS;
- checkpoint `docs/project-memory/checkpoints/2026-09-09-track5-bluestacks-universal-candidate-bridge.complete`.

## Graphics/runtime

- Optimization layers: official config → external driver/API → compatible runtime → engine/game adapter.
- Internal render resolution may be lower than output resolution only when compatible semantics are known and reversible.
- DG Extended Graphics Range may go below menu “Low” where engine semantics are known and reversible.
- Do not reduce everything blindly; measure performance benefit vs visual/memory/thermal cost.
- Low-End Recovery is an official objective for turning unplayable workloads into playable ones when feasible.

## Guardian / Governor / learning

- Guardian: detect → confirm → intervene → measure → keep/rollback.
- Gameplay changes are restricted to capabilities classified `LIVE_SAFE` for that workload.
- Governor is continuous control within validated ranges; Guardian micro-experiments test new hypotheses; Auto Tuner performs deep controlled exploration.
- These systems require capability ownership/leases so they do not fight each other.
- Controlled evidence outranks passive observations.
- Passive evidence can request revalidation after drift, not silently rewrite validated truth.

## Cleaner

- Safe/Deep/Extreme cleanup supported conceptually.
- Extreme may remove healthy regenerable caches after warning.
- Never automatically delete saves, mods, screenshots, recordings, presets, personal configs, documents or user-created content.

## UX

- Main surfaces: Home, Optimize, Profiles, Guardian, Performance, Expert, History, Settings, Mini Mode.
- Clean theme: light/ice-blue Liquid Glass; no red theme.
- Dark theme: smoked/graphite Liquid Glass with ruby-red accent.
- Mini Mode: Compact/Mini/Micro, ARGB border on both themes, low overhead, click-through/always-on-top configurable.
- Main UI, Mini and Tray share Core state; Mini does not run heavy logic.
- History is audit/recovery memory, not just logs.
