# DG Performance Engine — Implementation Status Ledger

This ledger records verified engineering milestones. Current branch code/tests + fresh exact-commit Windows CI remain authoritative over older handoffs.

## Current verified application checkpoint

- Branch: `build/initial-product`
- HEAD: `8595e03f7c0dc0f63e9caad42e9b01dcdfa5a9d7`
- Commit: `feat: compose universal context in performance sessions`
- Windows CI: **#989 — SUCCESS**
- Run: `34371201511`
- Full gate: native configure/build/tests, managed build, Core self-tests, win-x64 publish and artifact upload all passed.

## Product foundation

The repository contains the working Windows WPF + C++ product foundation: BlueStacks discovery/configuration, PresentMon, profiles, Guardian, Auto Tuner, History/snapshots, Mini Mode/themes and native interop. DG Performance Engine is an evolution of this product, not a rewrite.

## Track 0 — Foundation Hardening — GREEN

- Checkpoint `985688276cd7937b74a870d61445fa239ac570ad`
- Windows CI #402 SUCCESS
- Global Controlled Benchmark Lease
- Guardian suspend/reconcile
- Auto Tuner/Profile Challenge benchmark exclusivity
- cancellation/cleanup hardening

## Track 1 — Universal Diagnostic Foundation — GREEN

- Checkpoint `f1c932b7ce7af8c61c424c3c619b66784917ee22`
- Windows CI #426 SUCCESS
- MachineContext v2
- Hardware Discovery
- Capability Registry/Graph
- Environment Fingerprint v2
- Universal Bottleneck Analyzer foundation

## Track 2 — System Optimizer / evidence authority — GREEN through current branch

Representative verified work includes real Windows power/boost/core-parking adapters, runtime capability discovery, transaction ownership/dependency hardening, Analyze/Preview/Revalidate/Apply/History/Restore, compare-and-set drift protection, persistent backend, global lease protection, capability candidate planner, controlled Windows A/B, PresentMon evidence-quality hardening, Guardian-bound operational probes, capability Cost Map, Experiment Coordinator, repeated evidence evaluation, PendingValidation, explicit fresh validation challenge and durable ValidatedEvidence.

Authority preserved by all later tracks:

- `Observed != Validated`;
- exact machine/environment fingerprint and freshness;
- durable ValidatedEvidence required for automatic persistent recommendation authority;
- exact rollback / History integrity;
- Global Controlled Benchmark Lease.

## Track 3 — Game Discovery + Adapter Framework — GREEN

Implemented and verified:

- stable GameIdentity/catalog;
- generic + specialized FF/FFMAX adapter framework;
- BlueStacks installed package discovery;
- Steam, Epic, Riot, Battle.net, EA App, Ubisoft Connect and Microsoft Store/Xbox GDK discovery;
- separate durable identity and transient evidence planes;
- deterministic evidence binder;
- Windows RunningProcess evidence;
- Windows App Paths KnownExecutable evidence;
- exact workload target resolver consumes only unambiguous bound RunningProcess evidence.

Stable identity comes only from launcher/source-native keys. PID/path/executable observations remain evidence and never manufacture GameId.

## Track 4 — Universal Telemetry / Evidence — ACTIVE, near closure

Canonical design:

- `docs/superpowers/specs/2026-09-08-universal-telemetry-evidence-design.md`

### Schema / compatibility / workload — GREEN

- typed metric schema v2 with stable descriptors;
- explicit domain/unit/aggregation;
- per-metric quality/coverage/source/origin;
- immutable deterministic `TelemetryFrame`;
- conservative `TelemetrySample -> TelemetryFrame` bridge;
- unavailable values represented by absence, never fake zero;
- universal workload-target resolver from Track 3 bound evidence;
- KnownExecutable/App Paths cannot claim a live PID.

### Direct collectors — GREEN

- native CPU utilization + physical memory direct v2;
- PresentMon direct v2 with explicit accepted-row coverage;
- typed exact accepted-frame count metric `frame.samples.accepted.count`;
- processor-power current/max/limit MHz telemetry using documented Windows power information;
- WDDM physical-GPU utilization with fail-closed PDH instance parsing.

No unsupported GPU/thermal/VRAM/I/O/network value is fabricated.

### Realtime storage / aggregation — GREEN

- bounded thread-safe `TelemetryFrameRingBuffer`;
- deterministic half-open windows;
- quality/coverage-aware generic aggregation;
- exact 1-second aggregation;
- hierarchical 10-second aggregation;
- bounded explicit session aggregate state;
- realtime pipeline composition;
- no raw v2 frame disk persistence.

### Typed consumers / benchmark authority — GREEN

- typed fail-closed UniversalBottleneckAnalyzer v2;
- typed UniversalDiagnosticService;
- Performance capture/A-B typed metric evidence with legacy History fallback;
- typed A/B evidence persistence preserves `Observed/PendingValidation/Validated`;
- Guardian-bound Windows controlled benchmark typed evidence;
- Auto Tuner typed PresentMon benchmark authority;
- physical Profile Challenge typed PresentMon benchmark authority;
- legacy textual benchmark authority no longer drives those production benchmark decisions.

### Universal A/B configuration/workload context — GREEN in Core

Official checkpoints:

- `4a9b12412d38a7ff0d355a74c890744290322b5a` — Windows CI #988 SUCCESS
  - adds `PerformanceUniversalConfigurationContext` schema v1;
  - stable GameId comes only from exactly one Track 3 catalog identity;
  - resolved adapter is authoritative for AdapterId;
  - machine fingerprint v2 is preserved;
  - capability values are included only when explicitly requested + uniquely resolved + Available + current value present;
  - PID/path are not persisted;
  - unknown adapter version/workload/display/driver details remain absent;
  - `PerformanceEvidenceSnapshot.UniversalContext` persists through History without upgrading old records.

- `8595e03f7c0dc0f63e9caad42e9b01dcdfa5a9d7` — Windows CI #989 SUCCESS
  - adds combined legacy + universal snapshot capture;
  - `PerformanceComparisonSession` accepts an optional universal context provider additively;
  - normal SetBaseline/SetCandidate can preserve both contexts together;
  - existing one-provider legacy constructor remains source-compatible;
  - universal-only/no-context sessions fail closed;
  - universal-only evidence still cannot originate BlueStacks profiles.

### Recent exact checkpoints

- `32e46b71d48ffcdb0550351896c6c46e1a54e42e` — integrated typed diagnostics/benchmark pipeline — Windows CI #983 SUCCESS
- `eb6855a38a0a838af9c5f529831f520803750a2f` — exact accepted-frame count — Windows CI #984 SUCCESS
- `1d4cb81c514dd8848754526a6b8c5a51b081a637` — Auto Tuner typed authority — Windows CI #985 SUCCESS
- `db39145d35bd83370b2d39ad3ffe239d4e9ffdf6` — Profile Challenge typed authority — Windows CI #986 SUCCESS
- `4a9b12412d38a7ff0d355a74c890744290322b5a` — universal A/B context — Windows CI #988 SUCCESS
- `8595e03f7c0dc0f63e9caad42e9b01dcdfa5a9d7` — session context composition — Windows CI #989 SUCCESS

### Legacy benchmark-authority audit — GREEN

Earlier temporary verifier audit proved:

- zero production `PresentMonFrameCount` references;
- Auto Tuner and Profile Challenge active code no longer call legacy `CaptureBenchmarkAsync`;
- remaining production `CaptureBenchmarkAsync` members are compatibility surfaces only;
- `PresentMon · N frames` remains legacy output/compatibility disclosure, not typed benchmark authority;
- old History rehydration remains supported;
- no validation/freshness gate was weakened.

## Current Track 4 gap

The Core contract and `PerformanceComparisonSession` are ready for universal context, but the real application composition still uses:

`PerformanceComparison = new PerformanceComparisonSession(CapturePerformanceConfiguration);`

`CapturePerformanceConfiguration()` remains intentionally Free Fire/BlueStacks + Guardian specific. The WPF application does not yet own an explicit selected generic stable GameId/catalog result that can feed the new universal provider.

This is now the remaining proven integration boundary. It must not be solved by startup discovery or by guessing GameId from PID/path/process/display names.

Required invariants for the next slice:

1. current BlueStacks provider and `PerformanceConfigurationSnapshot` semantics remain intact;
2. generic workload identity must be an explicitly proven Track 3 GameId;
3. any required game discovery remains explicit/on-demand;
4. no proven universal context => provider returns null and legacy-only behavior remains;
5. proven universal context may be attached additively, including alongside BlueStacks legacy context;
6. History/profile authority remains unchanged.

## Exact next action

TDD slice: **Explicit application workload-context composition**.

RED first for the smallest app-facing state/provider seam, verify the intended failure, implement only the additive composition, run an isolated verifier, integrate selectively, and require a fresh full Windows CI on the exact official SHA before calling GREEN.
