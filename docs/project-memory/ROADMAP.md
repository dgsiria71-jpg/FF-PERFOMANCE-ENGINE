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

Foundation and current collector migration are GREEN through application head `edfbba0845d60447e6fdd158b75eee6def39a60f`, Windows CI #900 SUCCESS:

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

Current sequence:

1. **bounded realtime v2 `TelemetryFrame` ring buffer** ← NEXT;
2. quality/coverage-aware deterministic 1 s aggregates;
3. 10 s aggregate layer;
4. session aggregation/store (no raw-frame disk persistence before approved design);
5. add real GPU/VRAM/clocks/thermals/I/O/network channels one proven provider at a time;
6. migrate Performance/A-B away from free-form `DataQuality` parsing only after v2 collectors/storage are stable;
7. extend universal A/B configuration/workload context without weakening existing validation/freshness authority.

Ring-buffer / aggregation invariants:

- bounded memory only;
- deterministic capacity eviction and snapshots;
- aggregate by stable metric id/descriptor semantics;
- absent metrics are ignored, never zero-filled;
- incompatible descriptors never blend;
- aggregated quality cannot exceed the weakest contributor;
- coverage remains explicit and conservative;
- mixed provenance cannot masquerade as one direct collector;
- legacy `PerformanceTimelineBuffer` remains separate and unchanged.

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
