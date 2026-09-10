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
- Existing already-correct fail-closed behavior may be locked by regression tests without manufacturing an artificial production change.
- No completion/GREEN claims without exact-commit fresh CI evidence.
- Repository `AGENTS.md` + `docs/project-memory/*` form the durable continuation protocol.
- Current Git/code/tests + fresh exact Windows CI outrank stale memory text; stale docs are synchronized only after a materially GREEN application checkpoint.
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
- Universal search, candidate binding and result projection metadata cannot upgrade `Observed` to `Validated` or manufacture a winner.

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

- `AutoTunerEngine.GenerateCandidates(EnvironmentSnapshot, BlueStacksInstance?, AutoTunerMode)` remains the single source of truth for existing BlueStacks/FF candidate generation.
- `BlueStacksUniversalTuningCandidateBridge` is pure/read-only and preserves an exact one-to-one universal↔specialized binding set.
- Stable identity comes from `LegacyGameIdentityBridge`; authorization requires `GameAdapterResolver` to resolve the exact matching BlueStacks FF/FF MAX adapter.
- The bridge preserves source candidate order and existing Adaptive/Deep bounds; it may filter unsupported candidates but may never add/reorder/regenerate them.
- Installed-build applicability remains owned by `BlueStacksAutoTunerRuntime.BuildCandidatePlan(...)` against captured allow-listed instance settings.
- Universal dimension values are descriptive marginals of exact surviving bindings and must not be Cartesian-expanded to manufacture runnable candidates.
- Caller-provided captured instance state is required; missing/mismatched state fails closed.
- Captured renderer state is correlation evidence only. Since renderer mutation is not verified, a non-`Auto` candidate conflicting with known captured renderer is excluded.
- No renderer mutation is authorized by this slice.
- Candidate mapping carries no evidence/confidence/Observed/Validated/winner/recommendation/persistence authority.

Verification: application SHA `39246089fb28f510287e79639356a4e16d1b6b02`; Windows CI #1014 / run `34422254555` SUCCESS; checkpoint `docs/project-memory/checkpoints/2026-09-09-track5-bluestacks-universal-candidate-bridge.complete`.

## Track 5 universal winner/result projection authority — closed 2026-09-09

Previous risk:

- once exact universal candidate bindings existed, a new neutral output layer could accidentally become a second winner engine, infer `Validated` from candidate metadata, correlate a result to the wrong workload/adapter, or silently match evidence/winners approximately.

Closed decision:

- `AutoTunerEngine.SelectWinners(...)` remains the existing specialized winner authority for the BlueStacks/FF compatibility path;
- the neutral output seam is `BlueStacksUniversalTuningResultBridge.Project(...)` and is **correlation-only**;
- `UniversalTuningResultProjection` retains the exact source `TuningResult` rather than copying/recomputing its authority;
- `UniversalTuningEvidenceProjection` retains the exact source `CandidateEvidence` and exact Slice 3 `UniversalTuningCandidate` binding;
- `UniversalTuningWinnerProjection` retains the exact source `PerformanceProfile`, exact source evidence and exact universal candidate binding;
- `TuningResult.Game` must equal `candidateSpace.Identity.LegacyGameKind`; cross-workload projection fails closed;
- `candidateSpace.AdapterId` must be present and match `candidateSpace.Identity.AdapterId`; blank/tampered adapter authority fails closed;
- every evidence item requires **exactly one** specialized↔universal Slice 3 binding; zero or multiple matches fail closed;
- every winner requires **exactly one** existing source evidence configuration and then exactly one Slice 3 binding; zero/multiple provenance fails closed;
- projection preserves specialized evidence objects and `EvidenceLevel` exactly; it cannot upgrade `Observed` to `Validated`;
- projection preserves existing specialized winner objects, five roles and order; it cannot create a winner absent from `TuningResult.Winners`;
- an Observed-only `TuningResult` remains winnerless even when an exact universal candidate binding exists;
- no fuzzy PID/path/process/display-name matching is allowed in this seam;
- no generic persisted profile schema is introduced by Slice 4;
- no change is made to Auto Tuner session/runtime, Profile Challenge/Custom Validated authority, direct typed PresentMon authority, Global Controlled Benchmark Lease, rollback or History.

Reason:

- carry stable workload + exact candidate provenance forward without duplicating evidence or winner logic;
- preserve `Observed != Validated` and keep universal support/correlation metadata strictly below validation authority;
- force workload/adapter/provenance mismatches to fail closed instead of creating plausible but false associations;
- keep the tested specialized BlueStacks/FF winner path as the compatibility authority while Track 5 generalizes incrementally.

TDD / verification:

- initial RED: verifier #1 / run `34423384376` on `726ba12f6e185a5a333ea66d7e86edb098577614` — bridge absent;
- Task 1 GREEN: verifier #2 / run `34423487642` on `f9250eab9c18e998b9586a937e865fde7a494067`;
- cross-workload RED: verifier #3 / run `34423652214` on `644dec7a627f3309b33e3b41a6d9796c96647fc1`;
- cross-workload GREEN: verifier #4 / run `34423775408` on `2f19ba53d74651c1326aa3fbc9b299994d365f8b`;
- adapter-authority RED: verifier #5 / run `34425347150` on `07c28539e0c3a1ed1d0b555c524bb1c804ca5940`;
- adapter-authority GREEN: verifier #6 / run `34425467591` on `383f12477c83910de17dac2e260140abc98543ac`;
- final regression GREEN: verifier #7 / run `34425669336` on `53ee4ee732d044c92960434368631e912b386390`;
- selective integration excluded the temporary verifier workflow;
- official application SHA `f24c8c25612182db3c12351185fa54be227a8252`;
- Windows CI #1016 / run `34425901211` SUCCESS;
- checkpoint `docs/project-memory/checkpoints/2026-09-09-track5-universal-winner-profile-projection.complete`.

## Track 5 closure — closed 2026-09-10

- Track 5 — Universal Auto Tuner + Profiles — is GREEN for the approved current canonical scope.
- The approved scope is an additive universal foundation over the proven specialized BlueStacks/Free Fire Auto Tuner/Profile system; it does not claim a generic physical tuning runtime for every discovered game.
- Universal candidate/search-space metadata never became evidence, validation, recommendation, winner, mutation or persistence authority.
- `AutoTunerEngine`, typed measurement, `HistoryService`, `ProfileService` and `ProfileChallengeService` retain specialized authority.
- Persisted Custom provenance can be re-proven after restart only against current capability and exact source/configuration/fingerprint/metrics; failure remains null without invalidating specialized history.
- Persisted promoted-winner provenance requires the durable specialized promotion receipt, exact current winner, preserved Custom, measured revalidation and current Custom reproving; it never reconstructs `ProfileChallengeResult`.
- AppServices composition is explicit/on-demand and adds no Track 5 discovery/provenance work to `InitializeAsync()`.
- Profiles UI consumes only application/Core provenance results and remains hidden when current provenance is absent.
- Future game-specific physical tuning adapters/runtimes, reversible renderer/graphics mutation, future non-BlueStacks persisted provenance and UI batching/polish are non-blocking expansions and do not reopen Track 5 by default.
- Closing application authority: `a0c9a4e30dc48cb1730cee8b6b951f7338680cd1`; Windows CI #1034 / run `34502895182` SUCCESS.
- Slice 13 documentary precursor: `6944e0221bce44159bfa894c270e75b095f8e575`; Windows CI #1035 / run `34503363082` SUCCESS.
- Closure checkpoint: `docs/project-memory/checkpoints/2026-09-10-track5-universal-auto-tuner-profiles.complete`.
- Track 6 — Adaptive Guardian 2.0 — becomes the next design boundary, but requires a separately approved bounded design before implementation.

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
