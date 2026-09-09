# DG Performance Engine — Roadmap

Current branch code/tests + fresh exact-commit Windows CI are authoritative. This roadmap records the canonical track sequence without inventing missing Track 11–19 numbering.

## Track 0 — Foundation Hardening — GREEN

- Global Controlled Benchmark Lease
- Guardian suspend/reconcile
- Auto Tuner/Profile Challenge exclusivity
- cancellation/cleanup hardening
- preserve validated regressions

Checkpoint: `985688276...`, Windows CI #402 SUCCESS.

## Track 1 — Universal Diagnostic Foundation — GREEN

- universal MachineContext
- Hardware Discovery
- Capability Registry/Graph
- Environment Fingerprint v2
- Universal Bottleneck Analyzer foundation

Checkpoint: `f1c932b7...`, Windows CI #426 SUCCESS.

## Track 2 — System Optimizer — GREEN through current branch

Implemented beyond the original scope: real Windows power/boost/core-parking capability adapters, runtime capability discovery, atomic transactions, exact restore/History, Optimize workflow, recommendation authority, candidate planning, controlled A/B, Cost Maps, evidence evaluation, PendingValidation, fresh validation challenges and durable ValidatedEvidence.

Authority remains strict: `Observed != Validated`, current fingerprint/freshness, durable validated evidence for persistent automatic recommendations, exact rollback and Global Controlled Benchmark Lease.

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

Additional discovery is optional enrichment, not a later-track blocker.

## Track 4 — Universal Telemetry / Evidence — GREEN

Closing verified application checkpoint:

```text
HEAD 71991379e01518adf2e1c539491a9c0339a56735
feat: route Performance capture through selected workloads
Windows CI #993 / run 34407420906 — SUCCESS
```

Completion record:

`docs/project-memory/checkpoints/2026-09-09-track4-universal-telemetry.complete`

GREEN includes:

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
- repository audit: no remaining production `PresentMonFrameCount` recommendation/benchmark authority ✅
- additive universal A/B workload/configuration context ✅
- universal context History round-trip without old-record upgrade ✅
- combined BlueStacks legacy + universal context capture ✅
- `PerformanceComparisonSession` legacy-only, universal-only, combined and context-free modes ✅
- universal-only evidence cannot acquire BlueStacks profile-origin authority ✅
- explicit application workload context selection/clear ✅
- selected workload retains only its own bound evidence ✅
- exact universal Performance capture from one bound RunningProcess PID ✅
- KnownExecutable cannot authorize process capture ✅
- multiple valid PIDs remain ambiguous and no PID is guessed ✅
- universal capture uses direct typed provider only ✅
- selected workload has capture-route precedence over Guardian ✅
- unavailable/ambiguous selected workload blocks instead of silently falling back to another workload ✅
- no universal selection preserves legacy Guardian/BlueStacks compatibility route ✅
- Performance WPF renders/uses application route authority instead of owning identity policy ✅
- exact official Windows CI for final integrated slice ✅

Recent Track 4 checkpoints:

- `32e46b71d48ffcdb0550351896c6c46e1a54e42e` — typed diagnostics/benchmark pipeline — CI #983 SUCCESS
- `eb6855a38a0a838af9c5f529831f520803750a2f` — exact accepted-frame count — CI #984 SUCCESS
- `1d4cb81c514dd8848754526a6b8c5a51b081a637` — Auto Tuner typed authority — CI #985 SUCCESS
- `db39145d35bd83370b2d39ad3ffe239d4e9ffdf6` — Profile Challenge typed authority — CI #986 SUCCESS
- `4a9b12412d38a7ff0d355a74c890744290322b5a` — universal A/B context — CI #988 SUCCESS
- `8595e03f7c0dc0f63e9caad42e9b01dcdfa5a9d7` — session context composition — CI #989 SUCCESS
- `21eb0d9ed7cd5c181fc609fca02f89f37883d59c` — explicit application workload context — CI #991 SUCCESS
- `71991379e01518adf2e1c539491a9c0339a56735` — universal Performance capture route/WPF — CI #993 SUCCESS

Track 4 closure does **not** claim nonexistent sensors. VRAM/thermals/I/O/network remain Unknown when unsupported and may be added later only with real provider + concrete consumer. They are future capability enrichment, especially relevant to Track 7, not fabricated Track 4 debt.

## Track 5 — Universal Auto Tuner + Profiles — NEXT

Canonical scope:

- generic search-space abstractions;
- global/system profile dimensions;
- game-specific candidate dimensions;
- automatic **validated** winner promotion;
- revalidation rules;
- reuse Track 4 typed evidence authority.

### Immediate sequence

1. **Generic search-space/candidate contract** ← NEXT
   - audit current `AutoTunerEngine`, candidate/profile models and specialized BlueStacks runtime;
   - introduce the smallest additive generic abstraction;
   - capability-honest: unsupported/unproven dimensions are absent, never guessed;
   - preserve the current BlueStacks/FF search space as the first specialized implementation;
   - no startup discovery/tuning side effects.
2. Compose system/universal candidate dimensions from proven capabilities.
3. Add game-adapter candidate dimensions without assuming universal semantics for renderer/quality/etc.
4. Generalize evidence-backed winner/profile outputs while preserving the existing five roles and Custom Validated authority.
5. Revalidation and promotion must keep typed measured evidence, repeatability, freshness/fingerprint and existing validation gates.
6. Integrate with UI only after Core/application policy is proven.

Every independent slice remains TDD RED → GREEN → exact Windows CI → memory synchronization.

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
- additional proven telemetry where needed (for example VRAM/thermals/I/O/network when supported)
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

The historical 89-page master architecture reportedly extends the roadmap to Track 0–19. The exact raw file is not currently mounted, so exact Track 11–19 numbering must not be invented. Preserve these approved domains until that source is recovered:

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
