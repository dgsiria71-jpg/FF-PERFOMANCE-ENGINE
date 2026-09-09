# DG Performance Engine — Implementation Status Ledger

This ledger records verified engineering milestones. Current branch code/tests + fresh exact-commit Windows CI remain authoritative over older handoffs.

## Current verified application checkpoint

- Branch: `build/initial-product`
- Application HEAD: `8dac70fdb2c693533ae481aaadd846ab84fde228`
- Commit: `feat: add capability-honest game adapter tuning dimensions`
- Windows CI: **#1007 — SUCCESS**
- Run: `34416726382`
- Full gate passed: checkout/setup, native configure/build/tests, managed build, Core self-tests, permanent App self-tests, win-x64 publish, artifact upload and cleanup.

Track 5 slice checkpoint:

`docs/project-memory/checkpoints/2026-09-09-track5-game-adapter-tuning-dimensions.complete`

Memory-sync commits after this application SHA are docs-only; the application checkpoint above remains the exact verified code authority until the next implementation slice.

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

Implemented and verified:

- stable `GameIdentity`/catalog;
- generic + specialized FF/FFMAX adapter framework;
- BlueStacks installed package discovery;
- Steam, Epic, Riot, Battle.net, EA App, Ubisoft Connect and Microsoft Store/Xbox GDK discovery;
- separate durable identity and transient evidence planes;
- deterministic evidence binder;
- Windows RunningProcess evidence;
- Windows App Paths KnownExecutable evidence;
- exact workload target resolver consumes only unambiguous bound RunningProcess evidence.

Stable identity comes only from launcher/source-native keys. PID/path/executable observations remain runtime evidence and never manufacture GameId.

Track 5 now extends the adapter layer additively with optional tuning-dimension declarations; this does not change Track 3 resolver identity rules or make generic adapters claim game-specific powers.

## Track 4 — Universal Telemetry / Evidence — GREEN

Canonical design: `docs/superpowers/specs/2026-09-08-universal-telemetry-evidence-design.md`.

Completion checkpoint: `docs/project-memory/checkpoints/2026-09-09-track4-universal-telemetry.complete`.

Closing application commit: `71991379e01518adf2e1c539491a9c0339a56735`, Windows CI #993 / run `34407420906` SUCCESS.

GREEN includes typed schema/quality/coverage/provenance/origin; immutable `TelemetryFrame`; conservative legacy bridge; exact workload target resolution; native CPU + memory direct v2; PresentMon direct v2 + exact accepted-frame count; bounded ring buffer; deterministic aggregation; processor-power clocks; WDDM physical-GPU utilization; typed fail-closed bottleneck analysis; typed diagnostics; Performance A/B typed evidence; typed History compatibility; Guardian-bound controlled benchmark typed evidence; Auto Tuner and Profile Challenge typed benchmark authority; additive universal A/B context; explicit app workload selection; exact selected-workload Performance capture; selected workload precedence over Guardian; no silent fallback for selected unavailable/ambiguous workload; WPF consumes Core/application route authority.

Track 4 is closed GREEN for its canonical scope. Unsupported VRAM/thermal/I/O/network channels remain Unknown until real providers exist.

## Track 5 — Universal Auto Tuner + Profiles — ACTIVE

Canonical scope:

1. generic search-space abstractions;
2. global/system profile dimensions;
3. game-specific candidate dimensions;
4. automatic validated winner promotion;
5. revalidation rules;
6. reuse Track 4 typed evidence authority without weakening it.

### Slice 1 — Universal search-space + system-dimension bridge — GREEN

Checkpoint:

- application SHA: `797c8c7766adea3369948d9cb330bb7ba9a69d52`;
- commit: `feat: add capability-honest universal tuning search space`;
- Windows CI #1000 SUCCESS / run `34411645032`;
- durable record: `docs/project-memory/checkpoints/2026-09-09-track5-universal-search-space.complete`.

Implemented neutral `UniversalTuningDimensionScope`, `UniversalTuningDimension`, `UniversalTuningCandidate`, `UniversalTuningSearchSpacePolicy`, `UniversalTuningSearchSpacePlanner` and `UniversalTuningSystemDimensionFactory`.

Rules: explicit id/authority/values; blank/duplicate declarations fail closed; zero dimensions produce zero candidates; deterministic bounded Cartesian enumeration; no hidden/default axis; no evidence/confidence/recommendation authority; Windows system dimensions reuse Track 2 `WindowsCapabilityCandidatePlan` and only `CanExplore == true` enters the search space.

Existing specialized BlueStacks/FF `TuningCandidate`, `AutoTunerEngine.GenerateCandidates(...)`, runtime/session path and winner/profile flows remained unchanged.

### Slice 2 — Capability-honest game-adapter workload dimensions — GREEN

Checkpoint:

- application SHA: `8dac70fdb2c693533ae481aaadd846ab84fde228`;
- commit: `feat: add capability-honest game adapter tuning dimensions`;
- Windows CI #1007 SUCCESS / run `34416726382`;
- durable record: `docs/project-memory/checkpoints/2026-09-09-track5-game-adapter-tuning-dimensions.complete`.

Implemented:

- optional `GameAdapterTuningDimensionDeclaration`;
- optional `IGameTuningDimensionProvider` while leaving `IGameAdapter` unchanged/source-compatible;
- pure `UniversalTuningWorkloadDimensionFactory`;
- exact resolved-adapter authority through `GameAdapterResolver`;
- Generic/unregistered/no-provider adapters expose zero workload dimensions;
- provider invocation requires `ConfigDiscovery + ConfigSnapshot + ConfigMutation + Rollback`;
- exact stable `GameIdentity` is passed to provider;
- workload IDs are namespaced as `workload.<normalized-adapter-id>.<normalized-local-id>`;
- `AuthorityId` comes from the resolved adapter;
- candidate values preserve provider text and order exactly;
- null/blank/empty/duplicate declarations fail closed;
- returned workload dimensions are deterministically ordered;
- existing `UniversalTuningSearchSpacePlanner` directly composes System + Workload dimensions, so no second compositor or hidden game axis was added.

Compatibility preserved:

- Generic adapter does not claim tuning dimensions;
- current `BlueStacksFreeFireGameAdapter` deliberately remains without a static provider in this slice;
- existing dynamic BlueStacks/FF candidate generation remains authoritative and unchanged;
- no startup discovery/tuning side effect;
- no mutation/benchmark/recommendation logic added to the factory.

Authority boundary:

**Adapter-declared dimension support is exploration only.** Candidate existence grants no measured evidence, confidence, winner role, recommendation, persistence permission or mutation permission. Existing typed evidence, repeatability, fingerprint/freshness, validation, ValidatedEvidence, Global Controlled Benchmark Lease, rollback and History remain authoritative.

#### TDD provenance

Temporary verifier branch: `ci/track5-game-adapter-dimensions-verify`.

- Task 1 RED: verifier #1 / run `34412460197` failed only for missing `IGameTuningDimensionProvider` and `GameAdapterTuningDimensionDeclaration`;
- Task 1 GREEN: verifier #3 / run `34412615403` on `997fc7d3c9c73923a15ea3a9d4975d82b8e1b4fa` passed Core + App.SelfTest + WPF;
- Task 2 RED: verifier #4 / run `34412791339` failed only for missing `UniversalTuningWorkloadDimensionFactory`;
- Task 2/3 GREEN: verifier #5 / run `34412911108` on `56bfd6c0ef05c70e6148ad5a411313c627be7dbd` passed Core + App.SelfTest + WPF including System + Workload composition;
- selective official integration excluded the temporary verifier workflow;
- official Windows CI #1007 passed the exact integrated application SHA.

## Recent exact checkpoints

- `32e46b71d48ffcdb0550351896c6c46e1a54e42e` — integrated typed diagnostics/benchmark pipeline — Windows CI #983 SUCCESS
- `eb6855a38a0a838af9c5f529831f520803750a2f` — exact accepted-frame count — Windows CI #984 SUCCESS
- `1d4cb81c514dd8848754526a6b8c5a51b081a637` — Auto Tuner typed authority — Windows CI #985 SUCCESS
- `db39145d35bd83370b2d39ad3ffe239d4e9ffdf6` — Profile Challenge typed authority — Windows CI #986 SUCCESS
- `4a9b12412d38a7ff0d355a74c890744290322b5a` — universal A/B context — Windows CI #988 SUCCESS
- `8595e03f7c0dc0f63e9caad42e9b01dcdfa5a9d7` — session context composition — Windows CI #989 SUCCESS
- `21eb0d9ed7cd5c181fc609fca02f89f37883d59c` — explicit App workload context — Windows CI #991 SUCCESS
- `71991379e01518adf2e1c539491a9c0339a56735` — universal Performance capture + WPF route — Windows CI #993 SUCCESS
- `797c8c7766adea3369948d9cb330bb7ba9a69d52` — universal tuning search-space foundation — Windows CI #1000 SUCCESS
- `8dac70fdb2c693533ae481aaadd846ab84fde228` — game-adapter workload tuning dimensions — Windows CI #1007 SUCCESS

## Exact next engineering slice

Continue Track 5 with the **specialized BlueStacks/FF candidate-space bridge into the universal abstraction**.

Required sequence:

1. read current `TuningCandidate`, `AutoTunerEngine.GenerateCandidates(...)`, BlueStacks environment/instance inputs, runtime/session and config mutation/snapshot paths;
2. identify why the current BlueStacks candidate space is machine/instance-dependent rather than static adapter metadata;
3. reuse the existing generator as the source of truth; do not build a duplicate renderer/FPS/resolution/CPU/RAM option catalog;
4. design the smallest additive mapping/provider seam that can expose neutral dimensions/candidates while preserving correlation to the legacy specialized candidate;
5. preserve the existing BlueStacks/FF runtime/session and five winner roles until later migration slices explicitly replace them;
6. prove reversibility/identity and deterministic mapping fail closed;
7. TDD RED → isolated verifier GREEN → selective official integration → fresh Windows CI → memory synchronization.
