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

GREEN includes metric schema v2; typed quality/coverage/provenance/origin; immutable `TelemetryFrame`; conservative legacy bridge; universal workload target resolver; native CPU/memory direct v2; PresentMon direct v2 + accepted-frame count; bounded ring buffer; deterministic aggregation; processor-power clocks; WDDM physical-GPU utilization; typed bottleneck diagnostics; typed Performance A/B/History; Guardian-bound Windows benchmark typed evidence; Auto Tuner/Profile Challenge typed authority; universal A/B workload/configuration context; explicit application workload selection; exact selected-workload Performance capture; selected workload precedence over Guardian; fail-closed unavailable/ambiguous routing; WPF consumption of Core/application policy; final exact Windows CI.

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

Checkpoint: `docs/project-memory/checkpoints/2026-09-09-track5-universal-search-space.complete`.

Implemented neutral System/Workload dimensions, neutral candidates, deterministic bounded Cartesian planning, fail-closed declaration validation and a Windows system bridge that reuses Track 2 `WindowsCapabilityCandidatePlan` and accepts only `CanExplore == true`.

Closed boundary: **search/support space is exploration only; it is not recommendation or validated winner authority.**

### Slice 2 — Capability-honest game-adapter workload dimensions — GREEN

Verified application checkpoint:

```text
HEAD 8dac70fdb2c693533ae481aaadd846ab84fde228
feat: add capability-honest game adapter tuning dimensions
Windows CI #1007 / run 34416726382 — SUCCESS
```

Checkpoint: `docs/project-memory/checkpoints/2026-09-09-track5-game-adapter-tuning-dimensions.complete`.

Implemented:

- optional `GameAdapterTuningDimensionDeclaration` ✅
- optional `IGameTuningDimensionProvider` without changing `IGameAdapter` ✅
- pure `UniversalTuningWorkloadDimensionFactory` ✅
- resolved-adapter authority through `GameAdapterResolver` ✅
- Generic/unregistered/no-provider => zero workload dimensions ✅
- required declaration capability gate `ConfigDiscovery + ConfigSnapshot + ConfigMutation + Rollback` ✅
- provider not invoked when the reversible lifecycle is incomplete ✅
- stable `GameIdentity` passed unchanged to provider ✅
- namespaced `workload.<adapter-id>.<local-id>` identities ✅
- `AuthorityId` from resolved adapter ✅
- exact provider value text/order preserved ✅
- null/blank/empty/duplicate declarations fail closed ✅
- deterministic workload dimension order ✅
- System + Workload composition reuses the existing universal planner ✅
- no second game-option catalog or hidden game axis ✅
- current Generic and BlueStacks/FF adapters do not gain invented static options ✅
- existing dynamic BlueStacks/FF generator/runtime/session remains unchanged ✅

TDD evidence:

- Task 1 RED: run `34412460197`;
- Task 1 GREEN: verifier #3 / run `34412615403` on `997fc7d3c9c73923a15ea3a9d4975d82b8e1b4fa`;
- Task 2 RED: run `34412791339`;
- Task 2/3 GREEN: verifier #5 / run `34412911108` on `56bfd6c0ef05c70e6148ad5a411313c627be7dbd`;
- official integration: `8dac70fdb2c693533ae481aaadd846ab84fde228`, Windows CI #1007 SUCCESS.

Closed boundary: **adapter declaration proves only explorable support. It grants no evidence, confidence, mutation permission, persistence permission, winner role or recommendation authority.**

### Immediate sequence

1. Generic search-space/candidate contract ✅
2. Compose system/universal dimensions from proven Windows capability candidate plans ✅
3. Capability-honest workload/game-adapter declaration + composition ✅
4. **Connect the existing dynamic BlueStacks/FF candidate space to the universal abstraction** ← NEXT
   - inspect `TuningCandidate` and `AutoTunerEngine.GenerateCandidates(...)`;
   - preserve its machine + BlueStacks-instance-dependent generation;
   - reuse that generator rather than duplicating renderer/FPS/resolution/CPU/RAM option catalogs;
   - map/correlate specialized candidates to neutral universal dimensions/candidates additively;
   - preserve the existing BlueStacks/FF runtime/session and winner/profile path until later migration.
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
