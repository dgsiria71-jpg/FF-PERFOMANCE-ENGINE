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

```text
HEAD 797c8c7766adea3369948d9cb330bb7ba9a69d52
feat: add capability-honest universal tuning search space
Windows CI #1000 / run 34411645032 — SUCCESS
```

Checkpoint: `docs/project-memory/checkpoints/2026-09-09-track5-universal-search-space.complete`.

Implemented neutral System/Workload dimensions, neutral candidates, deterministic bounded Cartesian planning, fail-closed declaration validation and a Windows system bridge that reuses Track 2 `WindowsCapabilityCandidatePlan` and accepts only `CanExplore == true`.

Closed boundary: **search/support space is exploration only; it is not recommendation or validated winner authority.**

### Slice 2 — Capability-honest game-adapter workload dimensions — GREEN

```text
HEAD 8dac70fdb2c693533ae481aaadd846ab84fde228
feat: add capability-honest game adapter tuning dimensions
Windows CI #1007 / run 34416726382 — SUCCESS
```

Checkpoint: `docs/project-memory/checkpoints/2026-09-09-track5-game-adapter-tuning-dimensions.complete`.

Implemented optional adapter-owned workload tuning declarations, resolved-adapter authority, reversible lifecycle gating, deterministic namespaced dimensions and direct System + Workload composition through the existing universal planner. Generic/unregistered/no-provider cases remain empty and no static BlueStacks option catalog was invented.

Closed boundary: **adapter declaration proves only explorable support. It grants no evidence, confidence, mutation permission, persistence permission, winner role or recommendation authority.**

### Slice 3 — Dynamic BlueStacks/FF universal candidate bridge — GREEN

Verified application checkpoint:

```text
HEAD 39246089fb28f510287e79639356a4e16d1b6b02
feat: bridge dynamic BlueStacks candidates into universal tuning space
Windows CI #1014 / run 34422254555 — SUCCESS
```

Checkpoint: `docs/project-memory/checkpoints/2026-09-09-track5-bluestacks-universal-candidate-bridge.complete`.

Implemented:

- exact one-to-one `UniversalTuningCandidate` ↔ specialized `TuningCandidate` binding ✅
- stable workload identity via `LegacyGameIdentityBridge` + exact resolved BlueStacks FF/FF MAX adapter ✅
- existing `AutoTunerEngine.GenerateCandidates(...)` remains the only dynamic generator ✅
- existing Adaptive/Deep bounds and surviving order preserved ✅
- existing `BlueStacksAutoTunerRuntime.BuildCandidatePlan(...)` remains applicability authority ✅
- captured allow-listed installed instance state is required ✅
- generated-but-unrepresentable candidates filtered, never repaired/invented ✅
- five workload dimension marginals projected under resolved adapter authority ✅
- dimension marginals are descriptive only; they must not be Cartesian-expanded to manufacture candidates absent from the specialized generator ✅
- captured renderer drift fails closed when current runtime cannot mutate renderer safely ✅
- no renderer mutation added ✅
- existing runtime/session/five winner roles/Profile Challenge path unchanged ✅
- no new evidence/recommendation/persistence authority ✅

TDD evidence:

- initial RED: run `34417641306`;
- Task 1 GREEN: verifier #4 / run `34417996142` on `e434f8e2a83f466408d91ab6e90a980b9de3d7e7`;
- Task 2 RED: verifier #5 / run `34418189031` on `888cd58d59fe845304e428a79653989499d39c64`;
- final GREEN: verifier #6 / run `34418407183` on `07180bd58fc5ff0b01ef3b9038056fe2693d30bb`;
- official integration: `39246089fb28f510287e79639356a4e16d1b6b02`, Windows CI #1014 SUCCESS.

Closed boundary: **exact candidate binding proves correlation/applicability only. It does not prove that the candidate is beneficial, measured, validated, a winner or safe to persist.**

### Immediate sequence

1. Generic search-space/candidate contract ✅
2. Compose system/universal dimensions from proven Windows capability candidate plans ✅
3. Capability-honest workload/game-adapter declaration + composition ✅
4. Dynamic BlueStacks/FF specialized candidate → exact universal binding bridge ✅
5. **Generalize evidence-backed winner/profile outputs while preserving existing BlueStacks/FF five-role and Custom Validated authority** ← NEXT
   - inspect `CandidateEvidence`, `TuningResult`, `PerformanceProfile`, `AutoTunerEngine.SelectWinners(...)` and session persistence;
   - preserve direct typed PresentMon/repeatability authority;
   - add stable workload identity + exact universal candidate/config correlation only through an additive neutral seam;
   - do not promote search/binding metadata to evidence;
   - preserve Recommended, Maximum FPS, Lowest Latency, Stability and Quality exactly for the specialized compatibility path;
   - preserve Custom Validated challenge, incumbent freshness and validation gates.
6. Revalidation and automatic promotion must retain typed measured evidence, repeatability, freshness/fingerprint and existing validation gates.
7. Integrate with UI only after Core/application policy is proven.

Every independent slice remains:

```text
docs/memory/context
→ TDD RED/GREEN
→ exact application CI
→ synchronize all relevant memory
→ exact documentary-HEAD CI
→ next increment
```

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

The historical larger master architecture extends beyond the canonical 0–10 sequence, but its exact raw Track 11–19 numbering is not currently authoritative here and must not be invented. Preserve these approved future domains until the original source is recovered:

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
