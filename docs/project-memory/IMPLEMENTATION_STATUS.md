# DG Performance Engine — Implementation Status Ledger

This ledger records verified engineering milestones. Current branch code/tests + fresh exact-commit Windows CI remain authoritative over older handoffs.

## Current verified application checkpoint

- Branch: `build/initial-product`
- Application HEAD: `71991379e01518adf2e1c539491a9c0339a56735`
- Commit: `feat: route Performance capture through selected workloads`
- Windows CI: **#993 — SUCCESS**
- Run: `34407420906`
- Full gate passed: checkout/setup, native configure/build/tests, managed build, Core self-tests, permanent App self-tests, win-x64 publish, artifact upload and cleanup.

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

### Direct collectors — GREEN

- native CPU utilization + physical memory direct v2;
- PresentMon direct v2 with explicit accepted-row coverage;
- exact accepted-frame count metric `frame.samples.accepted.count`;
- processor-power current/max/limit MHz telemetry using documented Windows information;
- WDDM physical-GPU utilization with fail-closed PDH instance parsing.

No unsupported GPU/thermal/VRAM/I/O/network value is fabricated. Additional providers remain future capability-driven enrichment rather than Track 4 blockers.

### Realtime storage / aggregation — GREEN

- bounded thread-safe `TelemetryFrameRingBuffer`;
- deterministic half-open windows;
- quality/coverage-aware generic aggregation;
- exact 1-second aggregation;
- hierarchical 10-second aggregation;
- bounded explicit session aggregate state;
- realtime pipeline composition;
- no raw v2 frame disk persistence.

### Typed diagnostics / benchmark authority — GREEN

- typed fail-closed `UniversalBottleneckAnalyzer`;
- typed `UniversalDiagnosticService`;
- Performance capture/A-B consumes typed metric evidence with explicit per-metric provenance;
- legacy History fallback remains readable without gaining authority;
- typed A/B evidence History persistence preserves `Observed/PendingValidation/Validated`;
- Guardian-bound Windows controlled benchmark uses typed evidence;
- Auto Tuner uses direct typed PresentMon validation/repeatability authority;
- physical Profile Challenge uses direct typed PresentMon authority;
- legacy textual `DataQuality` is compatibility disclosure, not the active benchmark decision authority.

### Universal A/B configuration/workload context — GREEN

Verified checkpoints preserved:

- `4a9b12412d38a7ff0d355a74c890744290322b5a` — Windows CI #988 SUCCESS
  - additive `PerformanceUniversalConfigurationContext` schema v1;
  - stable GameId from exactly one Track 3 catalog identity;
  - resolved adapter authoritative for AdapterId;
  - machine fingerprint v2;
  - capability values only when explicitly requested + uniquely resolved + Available + current value present;
  - PID/path excluded from durable context;
  - universal context History round-trip without old-record upgrade.

- `8595e03f7c0dc0f63e9caad42e9b01dcdfa5a9d7` — Windows CI #989 SUCCESS
  - combined legacy + universal snapshot capture;
  - `PerformanceComparisonSession` optional universal context provider;
  - legacy-only, universal-only, combined and context-free capture behavior;
  - existing legacy constructor source-compatible;
  - universal-only evidence cannot originate BlueStacks profiles.

- `21eb0d9ed7cd5c181fc609fca02f89f37883d59c` — Windows CI #991 SUCCESS
  - explicit application-owned `PerformanceWorkloadContextSelection`;
  - AppServices explicit select/clear only;
  - resolved adapter authority instead of untrusted identity metadata;
  - unknown/ambiguous selection clears stale state;
  - cross-workload legacy/universal contamination blocked;
  - permanent App self-test added to Windows CI.

### Universal Performance capture targeting — GREEN

Closing checkpoint: `71991379e01518adf2e1c539491a9c0339a56735`, Windows CI #993 SUCCESS.

Implemented:

- selected workload retains only its own bound runtime evidence;
- selected stable GameId resolves through existing `TelemetryWorkloadTargetResolver` semantics;
- exact one valid RunningProcess PID/path permits process capture;
- duplicate same-PID evidence is not ambiguity;
- no valid running process => `UnavailableRunningProcess`;
- more than one distinct valid PID => `AmbiguousRunningProcess`;
- KnownExecutable never creates a live capture target;
- process path/name/PID never manufactures GameId;
- `PerformanceCaptureCoordinator.CaptureWorkloadTypedAsync(...)` uses direct typed provider only and appends typed timeline evidence;
- existing Guardian `CaptureTypedAsync(...)` remains source-compatible;
- no legacy `TelemetrySample` fallback is used to promote universal capture authority;
- `PerformancePresentation` supports the universal typed result without calling a native game a BlueStacks instance;
- `AppServices` provides one capture route authority;
- explicit universal selection has precedence over Guardian;
- selected but unavailable/ambiguous workload is blocked instead of silently measuring another Guardian workload;
- when no universal selection exists, legacy Guardian/BlueStacks typed capture remains the compatibility path;
- `PerformancePage` consumes the route/presentation service instead of deciding identity itself.

### TDD provenance for the closing slice

Temporary verifier branch `ci/track4-universal-capture-verify` was not merged wholesale. Important RED/GREEN sequence is preserved:

- RED: selected application workload lacked a runtime capture-target seam;
- GREEN: selector delegates to existing exact resolver;
- RED: universal typed coordinator entry absent;
- compatibility analysis rejected a same-name overload that would make `CaptureTypedAsync(null, ...)` ambiguous; new API uses `CaptureWorkloadTypedAsync(...)`;
- verifier #7 GREEN: direct typed universal coordinator;
- presentation RED → verifier #9 GREEN;
- AppServices composition RED → verifier #11 GREEN;
- verifier #12 RED: route precedence APIs absent;
- verifier #13 GREEN: precedence and no-fallback behavior;
- verifier #14 RED: route presentation absent;
- verifier #15 GREEN: presentation contract;
- verifier #16 / run `34407129430` GREEN: Core + App self-test + WPF build after real page wiring;
- selective official integration deliberately excluded the temporary verifier workflow;
- full official Windows CI #993 then passed the exact integrated application SHA.

### Track 4 closure audit

The canonical architecture lists Track 4 as:

1. metric schema v2;
2. hardware channels;
3. universal data quality;
4. universal A/B configuration snapshot;
5. compatibility migration.

The detailed Track 4 spec additionally defines exact workload resolver semantics and success criteria requiring current consumers to remain green and exact commits to pass Windows CI. Those requirements are now satisfied in the current application checkpoint, so Track 4 is closed **GREEN for its canonical scope**.

Future VRAM/thermal/I/O/network sources may be added when supported by real providers and consumers, especially under the planned Hardware Performance Engine. They do not justify fabricating values or keeping Track 4 indefinitely open.

## Recent exact checkpoints

- `32e46b71d48ffcdb0550351896c6c46e1a54e42e` — integrated typed diagnostics/benchmark pipeline — Windows CI #983 SUCCESS
- `eb6855a38a0a838af9c5f529831f520803750a2f` — exact accepted-frame count — Windows CI #984 SUCCESS
- `1d4cb81c514dd8848754526a6b8c5a51b081a637` — Auto Tuner typed authority — Windows CI #985 SUCCESS
- `db39145d35bd83370b2d39ad3ffe239d4e9ffdf6` — Profile Challenge typed authority — Windows CI #986 SUCCESS
- `4a9b12412d38a7ff0d355a74c890744290322b5a` — universal A/B context — Windows CI #988 SUCCESS
- `8595e03f7c0dc0f63e9caad42e9b01dcdfa5a9d7` — session context composition — Windows CI #989 SUCCESS
- `21eb0d9ed7cd5c181fc609fca02f89f37883d59c` — explicit App workload context — Windows CI #991 SUCCESS
- `71991379e01518adf2e1c539491a9c0339a56735` — universal Performance capture + WPF route — Windows CI #993 SUCCESS

## Next engineering track — Track 5 Universal Auto Tuner + Profiles

Do not rewrite the working FF/BlueStacks Auto Tuner. Generalize it additively.

First implementation boundary:

1. inspect canonical Track 5 requirements and current Auto Tuner/Profile candidate contracts;
2. define a generic capability-honest search-space/candidate abstraction;
3. keep BlueStacks/FF candidate generation as a specialized implementation;
4. unsupported/unproven candidate dimensions remain unavailable and cannot be invented;
5. existing typed evidence, repeatability, freshness/fingerprint and winner-validation gates remain the only promotion authority;
6. RED first, isolated verifier, selective integration, full Windows CI;
7. update project memory again at the resulting GREEN checkpoint.
