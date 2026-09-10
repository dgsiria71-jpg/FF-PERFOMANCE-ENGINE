# Current Handoff — 2026-09-10

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR: `#1` — open/draft to `main`
- Product: **DG Performance Engine**, evolved from the existing FF Performance Engine without rewrite or mass rename.
- Current Git/code/tests + fresh exact Windows CI outrank stale chat or memory text.

## Current exact verified application checkpoint

- Application HEAD: `f5e57ce4cb61c01132d5ecb5f4d67d2433533fc4`
- Commit: `feat: project validated performance records into universal context`
- Windows CI: **#1018 — SUCCESS**
- CI run id: `34433407760`

The exact #1018 job passed checkout/setup, native configure/build/tests, managed build, full Core self-tests, permanent App self-tests, `win-x64` publish, artifact upload and cleanup.

Checkpoint record:

`docs/project-memory/checkpoints/2026-09-10-track5-universal-validated-performance-projection.complete`

Any later memory-sync commit containing this handoff is docs-only and does not replace the application SHA above as code authority.

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
  - Slice 4 — evidence-backed universal winner/result projection: **GREEN**
  - Slice 5 — read-only universal projection of already-authorized validated History evidence: **GREEN**
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

Neutral System/Workload dimensions and candidates are pure, deterministic and bounded. Support/search metadata is exploration only; it is never recommendation/validation/winner/persistence authority.

## Track 5 Slice 2 — Game-adapter workload dimensions — GREEN

Application SHA `8dac70fdb2c693533ae481aaadd846ab84fde228`, Windows CI #1007 / run `34416726382` SUCCESS.

Optional adapter-owned workload tuning metadata is exposed only through the exact resolved adapter and reversible lifecycle. Generic/unregistered/no-provider/incomplete cases expose zero dimensions. No static BlueStacks option catalog was invented.

## Track 5 Slice 3 — Dynamic BlueStacks/FF universal candidate bridge — GREEN

Application SHA `39246089fb28f510287e79639356a4e16d1b6b02`, Windows CI #1014 / run `34422254555` SUCCESS.

`AutoTunerEngine.GenerateCandidates(...)` remains the only dynamic BlueStacks/FF candidate generator. The neutral bridge preserves exact one-to-one `UniversalTuningCandidate` ↔ specialized `TuningCandidate` bindings, source order and installed-build applicability through `BlueStacksAutoTunerRuntime.BuildCandidatePlan(...)`. Descriptive dimension marginals are not independently Cartesian-expanded. Captured renderer drift fails closed because renderer mutation remains unverified.

## Track 5 Slice 4 — Universal winner/result projection — GREEN

Application SHA `f24c8c25612182db3c12351185fa54be227a8252`, Windows CI #1016 / run `34425901211` SUCCESS.

`UniversalTuningResultProjection` and `BlueStacksUniversalTuningResultBridge` add stable workload/adapter/candidate correlation to the exact existing specialized `TuningResult`, `CandidateEvidence` and `PerformanceProfile` objects. They do not measure, score, validate, select, persist or mutate. Cross-workload, adapter mismatch and zero/ambiguous source binding fail closed. Observed evidence remains Observed and cannot invent winners.

## Track 5 Slice 5 — Universal validated performance projection — GREEN

### Purpose

Carry exact universal workload/candidate correlation to the boundary of an **already-authorized specialized validation record** without creating a second validation/profile authority.

### Permanent contracts

Added:

- `UniversalValidatedPerformanceProjection`;
- `BlueStacksUniversalValidatedPerformanceBridge.TryProject(...)`.

The output intentionally contains only:

- the exact original `PerformanceComparisonHistoryRecord` reference;
- the exact Slice 3 `UniversalTuningCandidate` reference.

It creates no new `Validated` state, winner, `PerformanceProfile`, persistence record or mutation authority.

### Exact authority/correlation rules

1. `PerformanceComparisonHistoryRecord.CanOriginateProfile` is evaluated first. If the existing specialized gate does not authorize the record, universal projection is impossible.
2. The bridge rehydrates the record defensively but does not modify it.
3. Candidate and Validation must both remain fully `Measured`.
4. Validation capture must be later than Candidate capture, preserving the independent revalidation condition already enforced by `HistoryService.CompletePerformanceValidationAsync`.
5. Candidate and Validation must retain exact equivalent `PerformanceConfigurationSnapshot` values. The specialized snapshot continues to own game, BlueStacks instance, CPU, RAM, renderer, FPS target, resolution, DPI and structural environment correlation.
6. Candidate and Validation must both carry valid `PerformanceUniversalConfigurationContext` values and those contexts must be exactly equivalent.
7. Universal `GameId` and `AdapterId` must match the stable Slice 3 candidate-space identity and exact resolved adapter authority.
8. Specialized `PerformanceConfigurationSnapshot.Game` must match `candidateSpace.Identity.LegacyGameKind`; cross-workload projection fails closed.
9. The validated specialized candidate configuration must match **exactly one** Slice 3 specialized candidate binding by the candidate-controlled dimensions CPU/RAM/FPS/renderer/resolution. Zero or multiple matches return no projection.
10. Invalid/malformed additive context returns no projection; it does not manufacture fallback identity.

### Compatibility / legacy behavior

A legacy History record with no `UniversalContext` can remain `CanOriginateProfile == true` through the existing specialized BlueStacks path. `TryProject(...)` simply returns `null`. No universal context is retroactively invented and the specialized record is not invalidated.

An adversarial record can also remain specialized-eligible while carrying a non-Measured Candidate because the old `CanOriginateProfile` property does not inspect Candidate quality. The new additive projection deliberately narrows that case by requiring Candidate + Validation to be Measured, without modifying the old property or `HistoryService`.

No change was made to:

- `PerformanceComparisonHistoryRecord.CanOriginateProfile`;
- `HistoryService`;
- `PerformanceProfile`;
- `ProfileService`;
- `ProfileChallengeService` / `ProfileChallengeRoundService`;
- incumbent freshness / winner replacement;
- typed PresentMon measurement authority;
- Global Controlled Benchmark Lease;
- rollback/History persistence behavior;
- startup behavior.

### TDD provenance

Temporary proving branch: `ci/track5-universal-validation-projection-verify`.

- verifier workflow commit: `46fc5271ddf5db4c31b00c10319a6ed804f8fa4d`;
- test contract commit: `ce558a357d9b0dfcfb2274f56581f9d450b090a0` (its run was superseded/cancelled by the runner-registration push);
- exact RED SHA: `37774d0685403104bf4ace7e12e903217ee1098a`;
- verifier #3 / run `34433104755`: Core failed only with `CS0103` because `BlueStacksUniversalValidatedPerformanceBridge` did not exist;
- minimal production GREEN SHA: `2edf4e18acce9dcc3847d0367922f3ba64afcf1c`;
- verifier #4 / run `34433257697`: Core self-tests + App self-tests + WPF build all SUCCESS;
- selective atomic official integration copied only the permanent production/test/runner blobs and excluded the temporary verifier workflow;
- official application SHA `f5e57ce4cb61c01132d5ecb5f4d67d2433533fc4` passed Windows CI #1018 / run `34433407760` completely.

New regression coverage proves:

- correct already-authorized universal validation projection;
- exact original History-record and universal-candidate references are retained;
- Observed and PendingValidation are blocked;
- a non-Measured Candidate is blocked even when the legacy property remains true;
- legacy validated History without universal context remains specialized-valid but receives no universal projection;
- candidate/validation universal-context mismatch is blocked;
- cross-machine universal context is blocked;
- GameId/cross-workload mismatch is blocked;
- AdapterId mismatch is blocked;
- zero and ambiguous candidate bindings are blocked.

## Non-negotiable invariants still active

- `Observed != Validated`.
- Candidate support/search space is not recommendation space.
- Exact universal↔specialized candidate binding proves correspondence/applicability only, not benefit.
- Universal result projection proves correlation/provenance only, not new evidence or profile authority.
- Universal validated performance projection exists only after specialized validation authority and grants no new authority itself.
- Legacy specialized History remains valid without universal metadata; missing metadata is absence, not inference.
- Unsupported/unproven tuning dimensions/candidates are absent, never guessed.
- Game option semantics are adapter-owned; renderer/quality/resolution/FPS/etc. are not assumed universal.
- Workload tuning authority comes from stable identity + resolved adapter, never PID/path/process/display name.
- Renderer remains read/correlation-only until a separate verified reversible mutation path exists.
- Current exact machine/environment fingerprint and freshness remain mandatory where authority requires them.
- Global Controlled Benchmark Lease semantics remain unchanged.
- Game discovery remains explicit/on-demand; never add it to `AppServices.InitializeAsync()`.
- Existing BlueStacks/FF Auto Tuner remains the first specialized implementation and stays source-compatible until a separately tested migration explicitly changes it.

## Exact next action

Continue **Track 5** at the next bounded validated-promotion seam. Do not add a generic profile schema or make universal correlation a validation source.

Required sequence:

1. inspect `ProfileService.CreateCustomFromValidatedComparisonAsync`, `ProfileChallengeService`, `ProfileChallengeRoundService`, challenge progress/freshness and winner replacement together with the new `BlueStacksUniversalValidatedPerformanceBridge`;
2. determine the smallest read-only/additive way to retain the exact universal candidate/workload provenance when the existing specialized chain explicitly originates or challenges a validated profile;
3. first prove whether the current challenge-round capture path actually possesses a valid `PerformanceUniversalConfigurationContext`; if it does not, do **not** fabricate one and do not touch `ProfileChallengeRoundService` merely to satisfy a type;
4. preserve typed direct measurement, independent later validation, exact `PerformanceConfigurationSnapshot`, machine fingerprint/freshness and current `CanOriginateProfile`/winner replacement authority;
5. preserve all five specialized BlueStacks/FF winner roles and Custom Validated semantics;
6. TDD RED first on a new isolated verifier branch;
7. GREEN verifier → selective official integration → exact Windows CI → synchronize relevant memory → documentary HEAD CI before the following increment.
