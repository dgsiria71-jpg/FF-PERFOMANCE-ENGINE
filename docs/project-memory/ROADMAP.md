# DG Performance Engine — Roadmap

Current branch code/tests + fresh exact-commit Windows CI are authoritative. This roadmap records the canonical track sequence without inventing missing Track 11–19 numbering.

## Track 0 — Foundation Hardening — GREEN

- Global Controlled Benchmark Lease
- Guardian suspend/reconcile
- Auto Tuner/Profile Challenge exclusivity
- preserve regressions

Checkpoint: `985688276...`, Windows CI #402 SUCCESS.

## Track 1 — Universal Diagnostic Foundation — GREEN

- universal MachineContext
- Hardware Discovery
- Capability Registry/Graph
- Environment Fingerprint v2
- Universal Bottleneck Analyzer

Checkpoint: `f1c932b7...`, Windows CI #426 SUCCESS.

## Track 2 — System Optimizer — GREEN through current branch

Implemented beyond the original scope: real Windows power/boost/core-parking capability adapters, runtime capability discovery, atomic transactions, exact restore/History, Optimize workflow, recommendation authority, candidate planning, controlled A/B, Cost Maps, evidence evaluation, PendingValidation, fresh validation challenges and durable ValidatedEvidence.

## Track 3 — Game Discovery + Adapter Framework — GREEN

- GameIdentity/catalog ✅
- generic + FF/FFMAX specialized adapters ✅
- BlueStacks packages ✅
- Steam ✅
- Epic ✅
- Riot ✅
- Battle.net ✅
- EA App ✅
- Ubisoft Connect ✅
- Microsoft Store/Xbox GDK ✅
- durable identity vs transient evidence planes ✅
- deterministic evidence binder ✅
- RunningProcess evidence ✅
- App Paths KnownExecutable evidence ✅
- exact workload target resolver ✅

Additional discovery is optional enrichment, not a Track 4 blocker.

## Track 4 — Universal Telemetry / Evidence — ACTIVE, NEAR CLOSURE OF CURRENT CANONICAL SCOPE

Current verified official checkpoint:

```text
HEAD db39145d35bd83370b2d39ad3ffe239d4e9ffdf6
Windows CI #986 / run 34320863316 — SUCCESS
```

GREEN now includes:

- metric schema v2 ✅
- typed quality / coverage / provenance / origin ✅
- immutable `TelemetryFrame` ✅
- conservative legacy bridge ✅
- universal workload target resolver ✅
- native CPU + physical memory direct v2 ✅
- PresentMon direct v2 ✅
- exact accepted-frame count metric ✅
- bounded realtime ring buffer ✅
- deterministic 1-second aggregation ✅
- hierarchical 10-second aggregation ✅
- bounded explicit session aggregate state ✅
- realtime pipeline composition ✅
- processor-power CPU clock/current/max/limit telemetry ✅
- WDDM physical-GPU utilization ✅
- typed fail-closed bottleneck analyzer ✅
- typed UniversalDiagnosticService ✅
- Performance capture/A-B typed evidence ✅
- History typed evidence compatibility ✅
- Guardian-bound Windows benchmark typed evidence ✅
- Auto Tuner typed benchmark authority ✅
- physical Profile Challenge typed benchmark authority ✅
- repository audit confirms no remaining production `PresentMonFrameCount` authority ✅

### Current Track 4 sequence

1. **Additive universal A/B configuration/workload context** ← NEXT
   - stable Track 3 GameId/workload;
   - machine/Windows context;
   - adapter id/version when proven;
   - relevant capability values when proven;
   - game/emulator config when known;
   - display/driver context only when material and actually observed;
   - preserve the existing BlueStacks-specific `PerformanceConfigurationSnapshot` unchanged for legacy profile/freshness authority.
2. History compatibility for new universal context; old records remain valid but are never silently upgraded.
3. Re-run exact validation/freshness/Observed/Pending/Validated tests on the additive model.
4. Reassess Track 4 closure.
5. Add further hardware channels only when there is a real, supported provider and a concrete diagnostic/tuning consumer. Candidate domains include VRAM, thermals, I/O and network; missing sensors remain Unknown rather than fabricated.

Track 4 invariants:

- missing values remain absent/Unknown;
- coverage is completeness, never probability;
- no fake sensor values;
- stable identity never comes from PID/path/display name;
- legacy History remains readable;
- `Observed != Validated`;
- exact fingerprint/freshness authority remains intact;
- no startup discovery side effects;
- no raw v2 telemetry disk database yet;
- no anti-cheat/integrity bypass.

## Track 5 — Universal Auto Tuner + Profiles — PLANNED

- generic search-space abstractions
- universal system/game candidate dimensions
- validated winner promotion
- revalidation rules
- reuse the typed evidence authority already migrated in Track 4

## Track 6 — Adaptive Guardian 2.0 — PLANNED

- generic workload state machine
- universal classifiers
- session optimizer actions
- learned action reliability
- post-session queue

## Track 7 — Hardware Performance Engine — PLANNED

- vendor capability adapters
- CPU controls
- GPU controls
- Expert integration
- Auto Tuner integration
- instability detection

## Track 8 — Deep Cleaner — PLANNED

- analyzer/classifier
- Safe/Deep/Extreme policies
- personal-data protection
- quarantine/history
- UI

## Track 9 — Auto Optimize — PLANNED

- environment/change detection
- recommendation engine
- local learning
- confidence decay
- auto-apply policy

## Track 10 — DG UX Migration — PLANNED

- Analyze
- Games
- Cleaner
- System Optimize

## Expanded master architecture — approved future domains

The historical 89-page master architecture reportedly extends the roadmap to Track 0–19. The exact raw file is not currently mounted, so exact Track 11–19 numbering must not be invented. Preserve these approved domains until the source is recovered:

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
- hardware-in-the-loop testing
