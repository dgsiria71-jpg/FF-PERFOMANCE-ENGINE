# DG Performance Engine — Implementation Status Ledger

This ledger records verified engineering milestones. Current branch code/tests + fresh exact-commit Windows CI remain authoritative over older handoffs.

## Current verified application checkpoint

- Branch: `build/initial-product`
- Application HEAD: `797c8c7766adea3369948d9cb330bb7ba9a69d52`
- Commit: `feat: add capability-honest universal tuning search space`
- Windows CI: **#1000 — SUCCESS**
- Run: `34411645032`
- Full gate passed: checkout/setup, native configure/build/tests, managed build, Core self-tests, permanent App self-tests, win-x64 publish, artifact upload and cleanup.

Track 5 slice checkpoint:

`docs/project-memory/checkpoints/2026-09-09-track5-universal-search-space.complete`

Memory-sync commits after this application SHA are docs-only; the application checkpoint above remains the exact verified code authority until the next implementation slice.

## Product foundation

The repository contains the working native Windows WPF + C++ product foundation: BlueStacks discovery/configuration, PresentMon, Profiles, Guardian, Auto Tuner, History/snapshots, Mini Mode/themes and native interop. **DG Performance Engine is an evolution of this product, not a rewrite.** Physical `FFPerformanceEngine.*` project names remain intentionally preserved until controlled migration.

## Track 0 — Foundation Hardening — GREEN

Checkpoint: `985688276cd7937b74a870d61445fa239ac570ad`, Windows CI #402 SUCCESS.

Verified foundation includes:

- Global Controlled Benchmark Lease;
- Guardian suspension/reconciliation around controlled work;
- Auto Tuner/Profile Challenge benchmark exclusivity;
- cancellation-safe cleanup;
- preservation of validated FF/BlueStacks behavior.

## Track 1 — Universal Diagnostic Foundation — GREEN

Checkpoint: `f1c932b7ce7af8c61c424c3c619b66784917ee22`, Windows CI #426 SUCCESS.

Verified:

- MachineContext v2;
- Hardware Discovery;
- Windows Performance Capability Registry/Graph;
- Environment Fingerprint v2;
- universal bottleneck-analysis foundation;
- Unknown instead of invented unavailable state.

## Track 2 — System Optimizer / evidence authority — GREEN through current branch

Representative verified work includes:

- real Windows active-power, CPU boost and core-parking adapters;
- runtime capability discovery;
- adapter-declared support space and candidate planning;
- atomic session/persistent transactions;
- exact snapshot/rollback/History;
- dependency-closure ownership and leases;
- compare-and-set drift protection;
- persistent PC Analyze → Preview → Revalidate → Apply → Verify → History → Restore;
- controlled Windows A/B;
- PresentMon evidence-quality hardening;
- Guardian-bound operational probes;
- measured Performance Cost Map;
- Experiment Coordinator;
- repeated evidence evaluation;
- PendingValidation;
- explicit fresh validation challenge;
- durable ValidatedEvidence;
- fresh ValidatedEvidence → persistent recommendation authority.

Authority preserved by every later track:

- `Observed != Validated`;
- exact machine/environment fingerprint and freshness;
- durable ValidatedEvidence for automatic persistent recommendation authority;
- exact rollback and History integrity;
- Global Controlled Benchmark Lease.

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

## Track 4 — Universal Telemetry / Evidence — GREEN

Canonical design:

- `docs/superpowers/specs/2026-09-08-universal-telemetry-evidence-design.md`
- completion checkpoint: `docs/project-memory/checkpoints/2026-09-09-track4-universal-telemetry.complete`
- closing application commit: `71991379e01518adf2e1c539491a9c0339a56735`
- Windows CI #993 SUCCESS / run `34407420906`

### Schema / compatibility / workload — GREEN

- typed metric schema v2 with stable descriptors;
- explicit domain/unit/aggregation;
- per-metric quality/coverage/source/origin;
- immutable deterministic `TelemetryFrame`;
- conservative `TelemetrySample -> TelemetryFrame` bridge;
- unavailable values represented by absence/typed Unavailable, never fake zero;
- stable workload target resolver from Track 3 bound evidence;
- KnownExecutable/App Paths cannot claim a live PID;
- unknown/blank GameId does not become promoted stable identity;
- multiple distinct running PIDs remain ambiguous and no PID is guessed.

### Direct collectors / aggregation — GREEN

- native CPU utilization + physical memory direct v2;
- PresentMon direct v2 with explicit accepted-row coverage;
- exact accepted-frame count metric `frame.samples.accepted.count`;
- processor-power current/max/limit MHz telemetry;
- WDDM physical-GPU utilization with fail-closed instance parsing;
- bounded thread-safe realtime ring buffer;
- deterministic quality-aware 1-second and hierarchical 10-second aggregation;
- bounded explicit session aggregate state;
- no raw v2 frame disk persistence.

No unsupported GPU/thermal/VRAM/I/O/network value is fabricated. Additional providers remain future capability-driven enrichment rather than Track 4 blockers.

### Typed diagnostics / benchmark authority — GREEN

- typed fail-closed `UniversalBottleneckAnalyzer`;
- typed `UniversalDiagnosticService`;
- Performance capture/A-B consumes typed metric evidence with explicit per-metric provenance;
- legacy History fallback remains readable without gaining authority;
- typed A/B History persistence preserves `Observed/PendingValidation/Validated`;
- Guardian-bound Windows controlled benchmark uses typed evidence;
- Auto Tuner uses direct typed PresentMon validation/repeatability authority;
- physical Profile Challenge uses direct typed PresentMon authority;
- legacy textual `DataQuality` is compatibility disclosure, not active benchmark authority.

### Universal A/B context and Performance capture — GREEN

Key checkpoints:

- `4a9b12412d38a7ff0d355a74c890744290322b5a` — universal A/B context — Windows CI #988 SUCCESS;
- `8595e03f7c0dc0f63e9caad42e9b01dcdfa5a9d7` — session context composition — Windows CI #989 SUCCESS;
- `21eb0d9ed7cd5c181fc609fca02f89f37883d59c` — explicit App workload context — Windows CI #991 SUCCESS;
- `71991379e01518adf2e1c539491a9c0339a56735` — universal Performance capture + WPF route — Windows CI #993 SUCCESS.

Final Track 4 capture rules:

- selected workload retains only its own bound runtime evidence;
- selected stable GameId resolves through `TelemetryWorkloadTargetResolver`;
- exactly one valid RunningProcess PID/path permits process capture;
- zero valid running processes => unavailable;
- multiple distinct PIDs => ambiguous;
- KnownExecutable never creates a live capture target;
- explicit universal selection has precedence over Guardian;
- selected unavailable/ambiguous workload blocks instead of silently measuring another Guardian workload;
- no universal selection preserves legacy Guardian/BlueStacks typed compatibility;
- WPF consumes App/Core route authority rather than deciding identity itself.

Track 4 is closed GREEN for its canonical scope.

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

Implemented neutral contracts:

- `UniversalTuningDimensionScope.System`;
- `UniversalTuningDimensionScope.Workload`;
- `UniversalTuningDimension`;
- `UniversalTuningCandidate`;
- `UniversalTuningSearchSpacePolicy`;
- `UniversalTuningSearchSpacePlanner`;
- `UniversalTuningSystemDimensionFactory`.

#### Capability-honest search-space rules

- dimension id is explicit and nonblank;
- authority id is explicit and nonblank;
- candidate values are explicit and nonblank;
- duplicate dimension ids case-insensitively fail closed;
- duplicate candidate values fail closed;
- zero dimensions produce zero candidates, never a fabricated default candidate;
- dimensions are deterministically ordered by id;
- each authority's candidate-value order is preserved;
- Cartesian enumeration is deterministic and the last sorted dimension varies fastest;
- `MaxCandidates` truncates a deterministic prefix only;
- no randomization, hidden axis, confidence, evidence or recommendation is attached by the planner.

#### Windows system-dimension bridge

`UniversalTuningSystemDimensionFactory.FromWindowsCandidatePlan(...)` reuses the already-proven Track 2 `WindowsCapabilityCandidatePlan` rather than rebuilding capability discovery or candidate generation.

Only `CanExplore == true` is projected.

The bridge keeps unavailable/missing/no-space plans absent, preserves exact TargetValue text in ExplorationRank order, rejects duplicate target values, and never:

- inspects or mutates the Windows registry/capability state;
- generates new schema candidate points;
- infers availability;
- reads recommendation confidence/value as search authority;
- publishes a recommendation.

#### Compatibility

The existing specialized path remains source-compatible and unchanged:

- `TuningCandidate`;
- `AutoTunerEngine.GenerateCandidates(...)`;
- `AutoTunerSessionService`;
- BlueStacks Auto Tuner runtime;
- five profile winner roles and Custom Validated flows.

No startup discovery or tuning side effect was added.

#### Authority boundary

**Support/search space is exploration only. It is not recommendation space.**

Candidate existence does not grant:

- `Observed` status;
- `Validated` status;
- confidence;
- winner role;
- persistent recommendation;
- permission to apply a mutation.

Existing controlled measurement, typed evidence, repeatability, freshness/fingerprint, validation challenge, ValidatedEvidence and promotion gates remain authoritative.

#### TDD provenance

Temporary verifier branch: `ci/track5-universal-search-space-verify`.

- Task 1 RED: run `34410887507` failed because universal search-space contracts did not exist;
- Task 1 GREEN: verifier #3 on `6feb03b019008092868602c6400e9272ab968200` passed Core + App.SelfTest + WPF;
- Task 2 RED: run `34411302456` failed only because `UniversalTuningSystemDimensionFactory` did not exist;
- Task 2 GREEN: verifier #5 / run `34411435761` on `5c50b2a268246feefcfc2ba190176f9231d52d83` passed Core + App.SelfTest + WPF;
- selective official integration excluded the temporary verifier workflow;
- official Windows CI #1000 passed the exact integrated application SHA.

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

## Exact next engineering slice

Continue Track 5 with **capability-honest workload/game candidate dimensions**.

Required sequence:

1. locate/read existing Track 3 generic + specialized Game Adapter contracts;
2. reuse their declared capabilities instead of creating a second game-option catalog;
3. define a workload tuning-dimension seam only for adapter-declared identity/authority/values;
4. do not assume renderer/quality/resolution/FPS semantics are universal;
5. unsupported/ambiguous/duplicate declarations fail closed;
6. compose adapter dimensions with the already-GREEN universal planner deterministically;
7. preserve BlueStacks/FF specialized candidate generation until its own later migration slice receives RED/GREEN proof;
8. TDD RED → isolated verifier GREEN → selective official integration → fresh Windows CI → memory synchronization.
