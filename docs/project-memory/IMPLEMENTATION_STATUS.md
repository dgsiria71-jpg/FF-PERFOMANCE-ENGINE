# DG Performance Engine — Implementation Status Ledger

This ledger records verified engineering milestones. Current branch code/tests + fresh exact-commit Windows CI remain authoritative over older handoffs.

## Current verified application checkpoint

- Branch: `build/initial-product`
- Application HEAD: `f5e57ce4cb61c01132d5ecb5f4d67d2433533fc4`
- Commit: `feat: project validated performance records into universal context`
- Windows CI: **#1018 — SUCCESS**
- Run: `34433407760`
- Full gate passed: checkout/setup, native configure/build/tests, managed build, Core self-tests, permanent App self-tests, win-x64 publish, artifact upload and cleanup.

Track 5 slice checkpoint:

`docs/project-memory/checkpoints/2026-09-10-track5-universal-validated-performance-projection.complete`

Any docs-only memory-sync commit after this application SHA does not replace the application checkpoint above as code authority.

## Product foundation

The repository contains the working native Windows WPF + C++ product foundation: BlueStacks discovery/configuration, PresentMon, Profiles, Guardian, Auto Tuner, History/snapshots, Mini Mode/themes and native interop. **DG Performance Engine is an evolution of this product, not a rewrite.** Physical `FFPerformanceEngine.*` project names remain intentionally preserved until controlled migration.

## Track 0 — Foundation Hardening — GREEN

Checkpoint: `985688276cd7937b74a870d61445fa239ac570ad`, Windows CI #402 SUCCESS.

Verified foundation includes Global Controlled Benchmark Lease, Guardian suspension/reconciliation around controlled work, Auto Tuner/Profile Challenge benchmark exclusivity, cancellation-safe cleanup and preservation of validated FF/BlueStacks behavior.

## Track 1 — Universal Diagnostic Foundation — GREEN

Checkpoint: `f1c932b7ce7af8c61c424c3c619b66784917ee22`, Windows CI #426 SUCCESS.

Verified: MachineContext v2, Hardware Discovery, Windows Performance Capability Registry/Graph, Environment Fingerprint v2 and universal bottleneck-analysis foundation with Unknown instead of invented unavailable state.

## Track 2 — System Optimizer / evidence authority — GREEN through current branch

Representative verified work includes real Windows active-power/CPU-boost/core-parking adapters; runtime capability discovery; adapter-declared support space; atomic session/persistent transactions; exact snapshot/rollback/History; dependency ownership and leases; compare-and-set drift protection; persistent Analyze → Preview → Revalidate → Apply → Verify → History → Restore; controlled Windows A/B; PresentMon evidence-quality hardening; Guardian-bound operational probes; Performance Cost Maps; repeated evidence evaluation; PendingValidation; fresh validation challenges; durable ValidatedEvidence; fresh ValidatedEvidence → persistent recommendation authority.

Authority preserved by later tracks: `Observed != Validated`; exact fingerprint/freshness; durable ValidatedEvidence for automatic persistent recommendation authority; exact rollback/History integrity; Global Controlled Benchmark Lease.

## Track 3 — Game Discovery + Adapter Framework — GREEN

Implemented and verified: stable `GameIdentity`/catalog; generic + specialized FF/FFMAX adapters; BlueStacks package discovery; Steam, Epic, Riot, Battle.net, EA App, Ubisoft Connect and Microsoft Store/Xbox GDK discovery; separate durable identity/transient evidence planes; deterministic binder; RunningProcess/App Paths evidence; exact workload target resolver.

Stable identity comes only from launcher/source-native keys. PID/path/executable observations remain runtime evidence and never manufacture GameId.

## Track 4 — Universal Telemetry / Evidence — GREEN

Canonical design: `docs/superpowers/specs/2026-09-08-universal-telemetry-evidence-design.md`.

Closing application commit: `71991379e01518adf2e1c539491a9c0339a56735`, Windows CI #993 / run `34407420906` SUCCESS.

GREEN includes typed schema/quality/coverage/provenance/origin; immutable `TelemetryFrame`; conservative legacy bridge; exact workload targeting; native CPU + memory direct v2; PresentMon direct v2 + accepted-frame count; bounded aggregation; processor-power clocks; WDDM GPU; typed bottleneck/diagnostics; Performance A/B/History; Guardian-bound controlled benchmark typed evidence; Auto Tuner/Profile Challenge typed benchmark authority; additive universal A/B context; explicit application workload selection and exact selected-workload Performance capture.

Unsupported VRAM/thermal/I/O/network channels remain Unknown until real providers exist.

## Track 5 — Universal Auto Tuner + Profiles — ACTIVE

Canonical scope:

1. generic search-space abstractions;
2. global/system profile dimensions;
3. game-specific candidate dimensions;
4. evidence-backed universal output correlation;
5. automatic **validated** winner promotion/revalidation without weakening existing authority;
6. reuse Track 4 typed evidence authority.

### Slice 1 — Universal search-space + system-dimension bridge — GREEN

- application SHA: `797c8c7766adea3369948d9cb330bb7ba9a69d52`;
- Windows CI #1000 / run `34411645032` SUCCESS;
- checkpoint: `docs/project-memory/checkpoints/2026-09-09-track5-universal-search-space.complete`.

Implemented neutral System/Workload dimensions, neutral candidates, deterministic bounded planning and Windows system composition from Track 2 `CanExplore == true` capability plans. Search metadata is exploration only.

### Slice 2 — Capability-honest game-adapter workload dimensions — GREEN

- application SHA: `8dac70fdb2c693533ae481aaadd846ab84fde228`;
- Windows CI #1007 / run `34416726382` SUCCESS;
- checkpoint: `docs/project-memory/checkpoints/2026-09-09-track5-game-adapter-tuning-dimensions.complete`.

Implemented optional adapter-owned workload tuning declarations with exact resolved-adapter and reversible lifecycle authority. Generic/unregistered/no-provider/incomplete cases expose zero dimensions. No static BlueStacks catalog was invented.

### Slice 3 — Dynamic BlueStacks/FF universal candidate bridge — GREEN

- application SHA: `39246089fb28f510287e79639356a4e16d1b6b02`;
- Windows CI #1014 / run `34422254555` SUCCESS;
- checkpoint: `docs/project-memory/checkpoints/2026-09-09-track5-bluestacks-universal-candidate-bridge.complete`.

Implemented exact one-to-one universal↔specialized BlueStacks candidate bindings while preserving the existing specialized generator, source order/bounds, installed-build applicability and fail-closed captured renderer correlation. Binding/dimension metadata grants no evidence/winner/recommendation authority.

### Slice 4 — Evidence-backed universal winner/result projection — GREEN

- application SHA: `f24c8c25612182db3c12351185fa54be227a8252`;
- Windows CI #1016 / run `34425901211` SUCCESS;
- checkpoint: `docs/project-memory/checkpoints/2026-09-09-track5-universal-winner-profile-projection.complete`.

Implemented `UniversalTuningEvidenceProjection`, `UniversalTuningWinnerProjection`, `UniversalTuningResultProjection` and pure/read-only `BlueStacksUniversalTuningResultBridge`. It retains exact specialized result/evidence/profile objects and exact Slice 3 candidate bindings. Cross-workload, adapter mismatch, missing/ambiguous evidence/winner provenance fail closed. Existing evidence level and five specialized winner roles remain authoritative; Observed-only input cannot invent a winner. No profile schema, scoring, validation, persistence or mutation authority changed.

### Slice 5 — Read-only universal projection of validated History evidence — GREEN

Checkpoint:

- application SHA: `f5e57ce4cb61c01132d5ecb5f4d67d2433533fc4`;
- commit: `feat: project validated performance records into universal context`;
- Windows CI #1018 SUCCESS / run `34433407760`;
- durable record: `docs/project-memory/checkpoints/2026-09-10-track5-universal-validated-performance-projection.complete`.

Implemented:

- `UniversalValidatedPerformanceProjection` ✅
- pure/read-only `BlueStacksUniversalValidatedPerformanceBridge.TryProject(...)` ✅
- `PerformanceComparisonHistoryRecord.CanOriginateProfile` remains the first gate and is not modified ✅
- exact original specialized validated History record is retained as the output authority reference ✅
- exact Slice 3 `UniversalTuningCandidate` object is retained ✅
- Candidate + Validation must both be `Measured` ✅
- Validation must be later than Candidate ✅
- exact Candidate/Validation `PerformanceConfigurationSnapshot` equivalence remains mandatory ✅
- exact universal Candidate/Validation context equivalence is required ✅
- universal context must match stable candidate-space `GameId` and exact `AdapterId` ✅
- specialized game must match `candidateSpace.Identity.LegacyGameKind` ✅
- validated specialized candidate must map to exactly one Slice 3 binding ✅
- missing/malformed/unproven universal context returns no projection rather than inventing identity ✅
- legacy validated History without `UniversalContext` remains specialized-valid; it simply receives no universal projection ✅
- Observed/PendingValidation cannot project ✅
- adversarial non-Measured Candidate is blocked even when the old specialized property remains true ✅
- cross-machine/context, GameId/workload, AdapterId and missing/ambiguous binding mismatches fail closed ✅
- no new `Validated`, profile, winner, recommendation, persistence or mutation authority ✅
- no changes to `HistoryService`, `ProfileService`, `PerformanceProfile`, Profile Challenge, typed PresentMon authority, Global Controlled Benchmark Lease, rollback or startup ✅

#### TDD provenance

Temporary verifier branch: `ci/track5-universal-validation-projection-verify`.

- verifier workflow: `46fc5271ddf5db4c31b00c10319a6ed804f8fa4d`;
- test contract: `ce558a357d9b0dfcfb2274f56581f9d450b090a0` (superseded/cancelled before completing because runner registration followed);
- exact RED SHA: `37774d0685403104bf4ace7e12e903217ee1098a`;
- verifier #3 / run `34433104755`: failed in Core only with `CS0103` for absent `BlueStacksUniversalValidatedPerformanceBridge`;
- minimal production SHA: `2edf4e18acce9dcc3847d0367922f3ba64afcf1c`;
- verifier #4 / run `34433257697`: Core + App self-tests + WPF build SUCCESS;
- selective atomic integration excluded `.github/workflows/core-track5-universal-validation-projection-verifier.yml`;
- exact integrated SHA `f5e57ce4cb61c01132d5ecb5f4d67d2433533fc4` passed Windows CI #1018 / run `34433407760` including native, managed, both self-test suites, publish and artifact.

### Track 5 authority boundary after Slice 5

**Search declarations, candidate bindings, result/winner projections and validated-History universal projections are correlation/provenance metadata only.** They do not grant measured evidence, confidence, validation, mutation permission, persistence permission, winner role or recommendation authority.

The universal validated projection is stricter than the additive legacy surface where appropriate, but it never edits legacy authority: `CanOriginateProfile` still decides specialized eligibility, while universal projection may only narrow an already-eligible record when valid universal context is present.

Legacy History without universal context remains valid in the specialized path and receives no fabricated universal metadata.

## Recent exact checkpoints

- `71991379e01518adf2e1c539491a9c0339a56735` — universal Performance capture + WPF route — Windows CI #993 SUCCESS
- `797c8c7766adea3369948d9cb330bb7ba9a69d52` — universal tuning search-space foundation — Windows CI #1000 SUCCESS
- `8dac70fdb2c693533ae481aaadd846ab84fde228` — game-adapter workload tuning dimensions — Windows CI #1007 SUCCESS
- `39246089fb28f510287e79639356a4e16d1b6b02` — dynamic BlueStacks/FF universal candidate bridge — Windows CI #1014 SUCCESS
- `f24c8c25612182db3c12351185fa54be227a8252` — universal winner/result projection — Windows CI #1016 SUCCESS
- `f5e57ce4cb61c01132d5ecb5f4d67d2433533fc4` — universal validated History projection — Windows CI #1018 SUCCESS

## Exact next engineering slice

Continue Track 5 at the **validated profile origin/challenge provenance boundary**.

Required sequence:

1. inspect `ProfileService.CreateCustomFromValidatedComparisonAsync`, `ProfileChallengeService`, `ProfileChallengeRoundService`, challenge progress/freshness and winner replacement alongside the new universal validated projection;
2. find the smallest additive/read-only seam that can retain exact stable workload + universal candidate provenance when the specialized chain explicitly originates or challenges a validated profile;
3. first prove whether Profile Challenge round capture actually has a valid `PerformanceUniversalConfigurationContext`; absence must remain absence — do not fabricate context just to make universal projection succeed;
4. do not introduce a generic persisted profile schema unless a failing test proves it is necessary;
5. preserve direct typed measurement, later independent validation, exact specialized configuration, machine fingerprint/freshness and all existing winner/Custom authority;
6. TDD RED first on an isolated verifier;
7. verifier GREEN → selective official integration → fresh exact Windows CI → relevant memory sync → documentary HEAD CI before the next increment.
