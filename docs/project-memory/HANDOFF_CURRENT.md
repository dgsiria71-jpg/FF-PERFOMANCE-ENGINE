# Current Handoff — 2026-09-10

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR: `#1` — open/draft to `main`; do not merge or touch `main` while the architecture is still being proven.
- Product: **DG Performance Engine**, evolved from the existing FF Performance Engine without rewrite or mass rename.
- Current Git/code/tests + fresh exact Windows CI outrank stale chat or memory text.

## Current exact verified application checkpoint

- Application HEAD: `aaca2c0d08de6e1da2c97a0d543f9f4c30ab627c`
- Commit: `feat: resolve current universal provenance for persisted Custom profiles`
- Windows CI: **#1024 — SUCCESS**
- CI run id: `34438271260`

The exact #1024 job passed checkout/setup, native configure/build/test, managed build, Core self-tests, App self-tests, `win-x64` publish, artifact upload and cleanup.

Checkpoint record:

`docs/project-memory/checkpoints/2026-09-10-track5-universal-validated-profile-provenance-resolution.complete`

Any later docs-only memory-sync commit containing this handoff does not replace the application SHA above as code authority.

## Track state

- Track 0 — Foundation Hardening: **GREEN**
- Track 1 — Universal Diagnostic Foundation: **GREEN**
- Track 2 — System Optimizer / evidence authority: **GREEN through current branch**
- Track 3 — Game Discovery + Adapter Framework: **GREEN**
- Track 4 — Universal Telemetry / Evidence: **GREEN — canonical scope completed**
- Track 5 — Universal Auto Tuner + Profiles: **ACTIVE**
  - Slice 1 — universal search-space + system-dimension bridge: **GREEN**
  - Slice 2 — capability-honest adapter-owned workload dimensions: **GREEN**
  - Slice 3 — dynamic BlueStacks/FF universal candidate bridge: **GREEN**
  - Slice 4 — evidence-backed universal winner/result projection: **GREEN**
  - Slice 5 — read-only universal projection of already-authorized validated History evidence: **GREEN**
  - Slice 6 — read-only universal provenance for a real specialized Custom Validated profile: **GREEN**
  - Slice 7 — read-only universal provenance after already-authorized Profile Challenge promotion: **GREEN**
  - Slice 8 — current reproving of persisted Custom universal provenance for application consumption: **GREEN**
- Track 6+ — planned; follow `ROADMAP.md` and `CANONICAL_CONTEXT.md`.

## Non-negotiable authority inherited from Tracks 2–5

- `Observed != Validated`.
- Missing telemetry/capability remains absent/Unknown; never synthesize zero, headroom, confidence or support.
- Stable workload identity comes only from source-native GameId contracts; PID/path/process/display name are transient evidence.
- `KnownExecutable` never authorizes live process capture.
- Explicit selected workload owns Performance capture routing and fails closed on unavailable/ambiguous targets.
- Global Controlled Benchmark Lease, Guardian suspension/reconciliation, exact fingerprint/freshness, durable validation authority, rollback and History remain intact.
- Candidate/search support is exploration only, never recommendation/validation/winner/persistence authority.
- Universal metadata is additive correlation/provenance only. It can narrow an already-authorized specialized path but can never manufacture `Validated`, a winner, profile origin, recommendation, mutation or persistence permission.
- Game discovery remains explicit/on-demand; never add it to `AppServices.InitializeAsync()`.
- No anti-cheat/integrity bypass.

## Track 5 verified application chain

- Slice 1: `797c8c7766adea3369948d9cb330bb7ba9a69d52` — Windows CI #1000 SUCCESS.
- Slice 2: `8dac70fdb2c693533ae481aaadd846ab84fde228` — Windows CI #1007 SUCCESS.
- Slice 3: `39246089fb28f510287e79639356a4e16d1b6b02` — Windows CI #1014 SUCCESS.
- Slice 4: `f24c8c25612182db3c12351185fa54be227a8252` — Windows CI #1016 SUCCESS.
- Slice 5: `f5e57ce4cb61c01132d5ecb5f4d67d2433533fc4` — Windows CI #1018 / run `34433407760` SUCCESS.
- Slice 6: `4563ef6ab36d5dfdc29375b7156df9b357fa652d` — Windows CI #1020 / run `34435365759` SUCCESS.
- Slice 7: `807bc763db9dd58522f7b3890c5f31ff3f6a2bb0` — Windows CI #1022 / run `34436368817` SUCCESS.
- Slice 8: `aaca2c0d08de6e1da2c97a0d543f9f4c30ab627c` — Windows CI #1024 / run `34438271260` SUCCESS.

## Track 5 Slice 8 — Current persisted-Custom universal provenance resolution — GREEN

### Purpose

Provide an application-policy seam that can re-prove universal provenance for an **already persisted specialized `Custom + Validated` profile** after load/restart, without persisting a second generic profile schema and without inventing the original Auto Tuner mode.

This is intentionally a **current reproving** step. Historical specialized validity comes from `ProfileService` + `HistoryService`; current universal applicability additionally requires the present BlueStacks installation to still expose a candidate-space that reproduces the same candidate.

### Permanent contract

Added `UniversalValidatedProfileProvenanceService.ResolveCurrentAsync(...)`.

Inputs are:

- persisted profile id;
- current `EnvironmentSnapshot`;
- exact current `BlueStacksInstance`;
- current captured allow-listed BlueStacks settings.

Output is the existing `UniversalValidatedProfileProjection` or `null`. No new authority-bearing projection type is introduced.

### Exact fail-closed rules

1. Exactly one persisted profile must match the requested id.
2. It must remain `ProfileKind.Custom + EvidenceLevel.Validated`, FF/FFMAX, with non-null `SourceComparisonId` and exact current instance-name binding.
3. Exactly one persisted History comparison must match that source id.
4. Candidate configuration is defensively rehydrated and its stored environment must remain structurally compatible with the current environment.
5. The current environment must contain exactly one instance matching the supplied current instance.
6. The service rebuilds the real current candidate-space for every existing `AutoTunerMode` using `BlueStacksUniversalTuningCandidateBridge` and the supplied current allow-list.
7. Each successful path is re-proved through `BlueStacksUniversalValidatedPerformanceBridge` and `BlueStacksUniversalValidatedProfileBridge`; no caller metadata is trusted as authority.
8. Zero successful projections returns `null`.
9. `Adaptive` and `Deep` overlap is not treated as false ambiguity: multiple modes may resolve only when every successful projection has the same specialized profile id, source record id, stable GameId, legacy game kind, AdapterId and semantically identical universal candidate values.
10. Any disagreement among successful mode projections returns `null`; no mode is chosen arbitrarily.
11. Missing current allow-list capability, wrong instance, renderer/candidate drift, wrong source id, Observed profile or generated winner profile returns `null`.

### Important provenance distinction

The persisted `PerformanceConfigurationSnapshot` preserves the specialized tuning configuration and environment fingerprint, while the current `PerformanceUniversalConfigurationContext` does **not** persist the BlueStacks allow-list (`WorkloadConfiguration` is currently empty). Therefore:

- this service does not claim to reconstruct the exact historical candidate-space;
- it re-proves that the historical specialized origin can still be represented by the current candidate-space;
- loss of current allow-listed capability means **no current universal projection**, not invalidation of the specialized validated profile;
- the service never fabricates missing capability or an original `AutoTunerMode`.

### Explicit scope boundary

Generated/promoted winner profiles are intentionally outside this resolver. Slice 7 can project a promotion when the exact specialized `ProfileChallengeResult` is present, but that exact result is not persisted as an object today. Slice 8 does not reconstruct it from History JSON or widen challenge authority.

### TDD provenance

Temporary verifier branch: `ci/track5-universal-profile-provenance-resolution-verify`.

- verifier workflow commit: `23933be645374156a1ffc71cc46124c149c63df2`;
- test contract commit: `569036d81cfca214820324bb01c1cd59ce053522`;
- runner registration / clean RED SHA: `73bc232531b22e541664eb89db4bc568ffa869a3`;
- clean RED verifier #3 / run `34438037547`: Core failed only with `CS0246` because `UniversalValidatedProfileProvenanceService` did not exist;
- minimal production GREEN SHA: `e510ed20ed802c9f5584f727339a50e697a7e135`;
- GREEN verifier #4 / run `34438142785`: Core self-tests + App self-tests + WPF build SUCCESS;
- selective official integration excluded the temporary verifier workflow;
- official application SHA `aaca2c0d08de6e1da2c97a0d543f9f4c30ab627c` passed Windows CI #1024 / run `34438271260` completely.

Official integration diff from the Slice 7 documentary head contains exactly three permanent files:

- `src/FFPerformanceEngine.Core/Services/UniversalValidatedProfileProvenanceService.cs` added;
- `tests/FFPerformanceEngine.Core.SelfTest/UniversalValidatedProfileProvenanceServiceSelfTests.cs` added;
- `tests/FFPerformanceEngine.Core.SelfTest/Program.cs` +1 runner registration.

## Canonical documents not changed by Slice 8

`CANONICAL_CONTEXT.md` and `DECISIONS_LOG.md` remain authoritative and unchanged because Slice 8 implements the already-approved read-only application/provenance boundary and introduces no new architecture, mutation authority or persistence authority.

## Exact next action

Continue **Track 5** with a bounded **AppServices composition seam** before touching WPF presentation.

Required sequence:

1. compose one shared `BlueStacksUniversalTuningCandidateBridge` from existing `AutoTuner + GameAdapters` and one shared `UniversalValidatedProfileProvenanceService` from existing `Profiles + History + candidate bridge`;
2. expose an AppServices-owned on-demand method that, for a requested persisted Custom profile, captures the current environment, resolves exactly one bound BlueStacks instance, captures that instance's current allow-listed settings and delegates to Slice 8;
3. construction/`InitializeAsync()` must not perform profile provenance resolution, candidate generation or extra game discovery implicitly;
4. missing/wrong profile, missing/ambiguous instance or missing current allow-list must fail closed to no universal provenance;
5. AppServices must not infer original AutoTuner mode, validation, winner status or persistence permission;
6. TDD RED first on an isolated verifier branch; GREEN verifier → selective official integration → exact Windows CI → memory sync → documentary HEAD CI;
7. only after the application composition seam is GREEN should `ProfilesPage` receive presentation-only GameId/AdapterId/candidate provenance.
