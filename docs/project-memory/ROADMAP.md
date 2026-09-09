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
- additive universal A/B workload/configuration context ✅
- universal context History round-trip without old-record upgrade ✅
- explicit application workload context selection/clear ✅
- exact universal Performance capture from one bound RunningProcess PID ✅
- selected workload has capture-route precedence over Guardian ✅
- unavailable/ambiguous selected workload blocks instead of silently falling back ✅
- no universal selection preserves legacy Guardian/BlueStacks compatibility ✅
- WPF consumes Core/application route authority ✅
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

Track 4 closure does **not** claim nonexistent sensors. VRAM/thermals/I/O/network remain Unknown when unsupported and may be added later only with real provider + concrete consumer.

## Track 5 — Universal Auto Tuner + Profiles — ACTIVE

Canonical scope:

- generic search-space abstractions;
- global/system profile dimensions;
- game-specific candidate dimensions;
- automatic **validated** winner promotion;
- revalidation rules;
- reuse Track 4 typed evidence authority.

### Slice 1 — Generic search-space + Windows system dimension bridge — GREEN

Verified application checkpoint:

```text
HEAD 797c8c7766adea3369948d9cb330bb7ba9a69d52
feat: add capability-honest universal tuning search space
Windows CI #1000 / run 34411645032 — SUCCESS
```

Checkpoint record:

`docs/project-memory/checkpoints/2026-09-09-track5-universal-search-space.complete`

Implemented:

- neutral `UniversalTuningDimensionScope` with `System` and `Workload` ✅
- explicit `UniversalTuningDimension` identity + authority + candidate values ✅
- neutral `UniversalTuningCandidate` ✅
- deterministic bounded `UniversalTuningSearchSpacePlanner` ✅
- fail-closed invalid/blank/duplicate dimension declarations ✅
- zero dimensions produce zero candidates, never a fabricated default candidate ✅
- deterministic Cartesian enumeration with exact bounded prefix ✅
- no hidden/default axes, randomness, confidence, evidence or recommendation authority ✅
- `UniversalTuningSystemDimensionFactory` reuses Track 2 `WindowsCapabilityCandidatePlan` ✅
- only `CanExplore == true` plans become system dimensions ✅
- Unavailable/MissingCurrentState/NoCandidateSpace remain absent ✅
- producer TargetValue preserved exactly in ExplorationRank order ✅
- duplicate target values fail closed ✅
- existing BlueStacks/FF `TuningCandidate`, `AutoTunerEngine` and runtime/session path remain source-compatible ✅
- no startup discovery/tuning side effect ✅

Closed boundary:

**search/support space is exploration only; it is not recommendation or validated winner authority.** Existing typed evidence, repeatability, freshness/fingerprint, validation challenge, ValidatedEvidence and Global Controlled Benchmark Lease semantics remain unchanged.

TDD evidence:

- Task 1 RED: run `34410887507`;
- Task 1 GREEN: verifier #3 on `6feb03b019008092868602c6400e9272ab968200`;
- Task 2 RED: run `34411302456`;
- Task 2 GREEN: verifier #5 / run `34411435761` on `5c50b2a268246feefcfc2ba190176f9231d52d83`;
- official integration: `797c8c7766adea3369948d9cb330bb7ba9a69d52`, CI #1000 SUCCESS.

### Immediate sequence

1. Generic search-space/candidate contract ✅
2. Compose system/universal dimensions from proven Windows capability candidate plans ✅
3. **Add capability-honest workload/game-adapter dimensions** ← NEXT
   - locate and reuse existing Track 3 generic/specialized adapter contracts;
   - adapter must explicitly declare dimension identity, authority and values;
   - do not invent a second game-option catalog;
   - do not assume renderer, quality, resolution, FPS target or similar semantics are universal across games;
   - unsupported/ambiguous/duplicate declarations fail closed;
   - compose only explicit workload dimensions with the already-GREEN neutral planner.
4. Connect the specialized BlueStacks/FF candidate space to the universal abstraction in its own TDD slice without breaking the legacy path.
5. Generalize evidence-backed winner/profile outputs while preserving the existing five roles and Custom Validated authority.
6. Revalidation and automatic promotion must retain typed measured evidence, repeatability, freshness/fingerprint and existing validation gates.
7. Integrate with UI only after Core/application policy is proven.

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
- additional proven telemetry where needed
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
