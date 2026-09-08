# DG Performance Engine — Roadmap

## Canonical Tracks from the committed 2026-09-06 spec

### Track 0 — Foundation Hardening — GREEN

- Global Controlled Benchmark Lease
- Guardian suspend/reconcile
- real Auto Tuner/Profile Challenge exclusivity
- preserve tests

Checkpoint: `985688276...`, Windows CI #402 SUCCESS.

### Track 1 — Universal Diagnostic Foundation — GREEN

- MachineContext universal
- Hardware Discovery
- Capability Model / Registry / Graph
- Environment Fingerprint v2 compatibility
- Universal Bottleneck Analyzer

Checkpoint: `f1c932b7...`, Windows CI #426 SUCCESS.

### Track 2 — System Optimizer — SUBSTANTIALLY IMPLEMENTED / GREEN THROUGH CURRENT HEAD

Scope from spec:

- mutation contracts
- persistent/session transactions
- restore + History
- “Otimizar este PC”

The implementation has advanced beyond the original Track-2 definition: real power/boost/core-parking adapters, runtime capability discovery, transaction ownership/dependency hardening, persistent preview/apply/restore, recommendation authority, candidate planning, controlled A/B, cost maps, evidence evaluation, PendingValidation, fresh validation challenge, durable ValidatedEvidence, validated recommendation bridge and Optimize WPF integration are already present in current branch history.

### Track 3 — Game Discovery + Adapter Framework — GREEN

Original scope:

1. GameIdentity
2. catalog
3. generic adapter
4. launcher scanners
5. encapsulate FF/BlueStacks

Status:

- GameIdentity/catalog ✅
- generic + FF/FFMAX specialized adapter framework ✅
- BlueStacks installed package discovery ✅
- Steam ✅
- Epic ✅
- Riot ✅
- Battle.net ✅
- EA App ✅
- Ubisoft Connect ✅
- Microsoft Store / Xbox GDK ✅
- separate identity/evidence planes ✅
- deterministic GameEvidence binder ✅
- Windows running-process evidence ✅
- Windows App Paths KnownExecutable evidence ✅

Track 3 original exit criteria are satisfied. Additional discovery/evidence surfaces are optional future enrichment only when they add a proven truthful signal without weakening launcher-native stable identity. They are not a blocker for Track 4.

### Track 4 — Universal Telemetry / Evidence — ACTIVE

GREEN through application head `f5265286480662ffcfd3f89fbb03a1cd31a09e59`, Windows CI #928 SUCCESS:

- metric schema v2 ✅
- 17 canonical initial metric descriptors ✅
- typed per-metric quality / coverage / provenance / origin ✅
- immutable deterministic `TelemetryFrame` ✅
- conservative legacy `TelemetrySample` bridge ✅
- universal workload target resolver from bound Track 3 evidence ✅
- KnownExecutable/App Paths prevented from claiming a live PID ✅
- existing legacy telemetry and A/B APIs preserved ✅
- native CPU + physical-memory collector direct v2 path ✅ (`b9b13388...`, CI #896)
- PresentMon direct v2 frame path ✅ (`edfbba08...`, CI #900)
- shared legacy/v2 PresentMon statistics; no formula fork ✅
- explicit accepted-row coverage for PresentMon frame/latency metrics ✅
- bounded thread-safe realtime `TelemetryFrame` ring buffer ✅ (`dddb6d2a...`, CI #924)
- deterministic half-open window snapshots + FIFO capacity eviction ✅
- quality/coverage-aware generic aggregation ✅ (`f5265286...`, CI #928)
- deterministic 1-second aggregate helper ✅
- incompatible metric descriptors blocked from blending ✅
- absent metrics remain absent, never zero-filled ✅
- mixed provenance explicitly becomes `aggregate-mixed` ✅

Current sequence:

1. **10-second aggregation layer reusing the generic aggregator** ← NEXT;
2. bounded session aggregate/store + retention semantics;
3. application-level realtime pipeline composition (`collectors → ring buffer → 1s → 10s/session`);
4. keep legacy `PerformanceTimelineBuffer` / UI / A-B consumers intact while migration is additive;
5. add real GPU/VRAM/clocks/thermals/I/O/network channels one proven provider at a time;
6. migrate Performance/A-B away from free-form `DataQuality` parsing only after v2 realtime/session storage is stable;
7. extend universal A/B configuration/workload context without weakening existing validation/freshness authority.

Realtime aggregation invariants:

- bounded memory only for raw realtime frames;
- no raw v2 frame disk persistence yet;
- deterministic capacity eviction and snapshots;
- aggregate by stable metric id/descriptor semantics;
- absent metrics are ignored, never zero-filled;
- incompatible descriptors never blend;
- aggregated quality cannot exceed the weakest contributor;
- coverage remains explicit and conservative;
- aggregate output is `Derived`;
- mixed provenance cannot masquerade as one direct collector;
- legacy `PerformanceTimelineBuffer` remains separate and unchanged;
- no Track 2 validation/recommendation authority changes.

### Track 5 — Universal Auto Tuner + Profiles — PLANNED

- generic search-space abstractions
- system profile dimensions
- game candidate dimensions
- validated winner promotion
- revalidation rules

### Track 6 — Adaptive Guardian 2.0 — PLANNED

- generic workload state machine
- universal classifiers
- session optimizer actions
- learned action reliability
- post-session queue

### Track 7 — Hardware Performance Engine — PLANNED

- vendor capability adapters
- CPU controls
- GPU controls
- Expert integration
- Auto Tuner integration
- instability detection

### Track 8 — Deep Cleaner — PLANNED

- analyzer/classifier
- Safe/Deep/Extreme policies
- personal-data protection
- quarantine/history
- UI

### Track 9 — Auto Optimize — PLANNED

- environment/change detection
- recommendation engine
- local learning
- confidence decay
- auto-apply policy

### Track 10 — DG UX Migration — PLANNED

- Analyze
- Games
- Cleaner
- System Optimize

## Expanded master architecture (historically approved, source reconstruction)

A later 2026-09-06 master architecture was reported as extending the roadmap to **Track 0–19** and explicitly covering additional domains. The raw 89-page master file is not currently mounted in this runtime, so the exact Track 11–19 titles are **not reconstructed as if they were exact**. The following approved capabilities are preserved as future roadmap domains and must be mapped back to exact Track numbers if/when the raw master is recovered:

- Memory & Working Set Engine
- Storage / I/O Engine
- GPU / VRAM Engine
- WDDM / Display Engine
- Network / Latency Engine
- Input Responsiveness Engine
- Process / Services / Tasks Director
- Resource Director
- Privileged Broker
- crash/reboot recovery
- Capability Registry/Graph + ownership/leases
- DG Graphics Runtime Engine
- Scene Complexity / Load Classifier
- native low-overhead HUD
- Low-End Recovery
- Adaptive Performance Governor
- Scene-Aware Performance Cost Model
- controlled + passive + live micro-A/B learning
- Data Architecture v2
- update/adapter lifecycle
- hardware-in-the-loop (HIL) testing

Do **not** invent exact Track 11–19 numbering until the original master Markdown/DOCX is recovered. The concepts themselves are approved and should remain in planning.
