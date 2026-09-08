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

### Track 3 — Game Discovery + Adapter Framework — ACTIVE

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
- next: EA App → Ubisoft Connect → Xbox/Microsoft Store → other stable discovery surfaces

### Track 4 — Universal Telemetry / Evidence — PLANNED

- metric schema v2
- universal hardware channels
- data quality
- universal A/B configuration snapshot
- compatibility migration

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
