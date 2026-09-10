# DG Performance Engine — Canonical Context

## 1. Product lineage

The project began as **FF Performance Engine**, a Windows-native adaptive optimizer centered on BlueStacks + Free Fire / Free Fire MAX. It expands into **DG Performance Engine** by generalizing proven components; it is not a rewrite.

```text
FF PERFORMANCE ENGINE
Detection + Hardware/System Analysis + Telemetry + A/B + Auto Tuner
+ Profiles + Guardian + History + Snapshot/Rollback + FF/BlueStacks
                         ↓ generalize, do not discard
DG PERFORMANCE ENGINE
Windows + Hardware + Games + Graphics Runtime + Cleaner + Learning
```

Physical namespaces `FFPerformanceEngine.*` remain until a controlled migration. Rename work must not invalidate Git history, CI or verified behavior.

## 2. Product philosophy

The engine is not a catalog of gamer tweaks.

```text
MACHINE + WINDOWS + DRIVER + WORKLOAD + MODE + STATE + CONFIG + EVIDENCE
                                  ↓
                        BEST KNOWN DECISION
```

Canonical rules:

- no preset is good merely because it is popular;
- baseline/prior state must be known before mutation;
- meaningful mutation requires rollback;
- contaminated benchmarks are discarded;
- isolated observations do not become causal proof;
- evidence quality/confidence/freshness are first-class;
- application overhead is itself a performance concern;
- missing data remains missing/Unknown rather than receiving invented values.

## 3. Engineering continuation protocol

For every meaningful increment:

```text
docs/memory/context
→ TDD RED/GREEN
→ exact application CI
→ synchronize all relevant project memory
→ exact documentary-HEAD CI
→ next increment
```

Current Git/code/tests + fresh exact Windows CI outrank stale chat or stale memory. Do not roll working code backward because an older document says something different; update documentation after the checkpoint is proven.

## 4. User-facing operating modes

### Equilibrado

Prioritizes stability, efficiency and thermal behavior while still pursuing evidence-backed performance gains.

### Desempenho — default

Primary mode. Pursues measurable performance without blindly enabling every aggressive setting.

### Extremo

Uses the strongest supported Windows/hardware/game strategies when evidence shows value. Higher power/clocks are not inherently better and regressions are not retained.

Expert may expose advanced controls where the platform actually supports them. Safety Envelope can be optional in Expert, but warnings remain explicit and critical real instability can force emergency rollback.

## 5. High-level architecture

```text
DG PERFORMANCE ENGINE
│
├── Presentation
│   ├── Main WPF UI
│   ├── Mini Mode / HUD
│   └── Tray
│
├── Core / Performance Control Plane
│   ├── Session coordination
│   ├── Capability ownership / leases
│   ├── Optimization planning
│   ├── Profiles / policy
│   └── History / recovery
│
├── Diagnostic & Capability
│   ├── Environment Discovery
│   ├── Hardware Discovery
│   ├── MachineContext + Fingerprint
│   ├── Capability Registry/Graph
│   └── Bottleneck Analyzer
│
├── Evidence
│   ├── Telemetry Engine
│   ├── PresentMon
│   ├── Benchmark Engine
│   ├── A/B Evidence
│   ├── Data Quality / Confidence
│   └── Validated Evidence
│
├── Optimization Engines
│   ├── System Optimizer
│   ├── Hardware Performance Engine
│   ├── Game Performance Engine
│   ├── Graphics Runtime Engine
│   ├── Deep Cleaner
│   ├── Universal Auto Tuner
│   ├── Adaptive Guardian
│   └── Adaptive Performance Governor
│
├── Workload Layer
│   ├── Game Discovery
│   ├── Local Game Catalog
│   ├── Generic Game Adapter
│   └── Specialized adapters
│       └── Free Fire / BlueStacks
│
└── Safety / Recovery
    ├── Snapshot
    ├── Transaction + rollback
    ├── Last Known Good
    ├── instability response
    └── crash/reboot recovery
```

## 6. Platform split

### C#/.NET 8 + WPF

Owns presentation, application orchestration, profiles, history, policies, high-level Auto Tuner, persistence and managed Windows/adapter logic.

### C++20 / Win32

Owns low-latency/native/platform functions where native execution materially helps: precise timing, process/affinity/priority control and selected telemetry/sensor/vendor integrations.

Interop remains narrow and contractual. UI never directly owns native mutation policy.

## 7. Diagnostic model

Hardware/System Scan generalizes into a universal MachineContext and capability model rather than being duplicated. Relevant domains include CPU, GPU/VRAM where available, RAM, storage/I/O where provable, Windows build/drivers/power/process/services/tasks/startup/scheduling, workload/game context and display state.

Bottleneck analysis may classify CPU/main-thread, GPU, VRAM, RAM, I/O, thermal, power, background contention, frame pacing, renderer/engine limit, network or Unknown. Unsupported channels stay Unknown.

## 8. Windows Performance Capability model

Windows optimization is modeled as formal capabilities, not loose scripts. Each capability defines identity, current state, candidates, applicability/dependencies, behavior, safety/conflicts and read/validate/snapshot/apply/verify/rollback semantics.

```text
resolve graph → validate all → snapshot all → durable restore point
→ apply in dependency order → verify → History
failure anywhere → reverse rollback → verify restoration
```

Current real examples include active power policy, CPU boost policy and CPU core parking policy. Recommendation does not mean maximum setting; it must be evidence-backed.

## 9. Experimental integrity / evidence ladder

Global controlled measurements share one machine-wide benchmark lease. Guardian is suspended/reconciled around controlled work when required.

```text
Supported
→ Candidate
→ Controlled A/B
→ Observed
→ Repeated
→ Evaluated Beneficial
→ Pending Validation
→ Fresh Controlled Challenge
→ Validated Evidence
→ Recommendation Authority
→ Preview
→ Apply/Verify
→ History/Restore
```

`Observed != Validated` and `ControlledEvidence != automatic persistent recommendation`.

Freshness binds evidence to the relevant machine fingerprint, workload, configuration/baseline/candidate tuple and environment state.

## 10. Profiles and winner frontier

The engine maintains objective-specific winners rather than one universal “best” profile:

- Recommended
- Maximum FPS
- Lowest Latency
- Stability
- Quality
- Custom Validated
- Low-End Recovery as an expanded DG strategy

The same candidate may win multiple roles when evidence supports it.

```text
Machine → Workload/Game → Objective/Profile → Evidence
```

A Custom Validated profile can challenge incumbents only through compatible fresh controlled evidence. Existence or correlation metadata alone never authorizes promotion.

## 11. Guardian

Guardian is an adaptive session supervisor:

```text
detect state → detect real degradation → identify likely cause
→ choose LIVE_SAFE intervention → canary/A-B → keep or rollback
```

During active gameplay, only actions proven appropriate for the current workload/state are eligible; larger changes wait for safer boundaries.

## 12. Adaptive Performance Governor

Governor is continuous control inside already-known safe policy ranges and remains distinct from Guardian experiments and Auto Tuner exploration. Hysteresis, cooldown, step limits and `LIVE_SAFE` classification prevent oscillation.

## 13. Performance Cost Model / learning

Evidence strength order:

1. repeated controlled Auto Tuner A/B;
2. controlled live micro-experiments;
3. repeated passive Guardian observations;
4. isolated passive observations.

Passive evidence may reduce confidence or request revalidation after drift but cannot silently overwrite stronger controlled evidence.

## 14. Graphics / Game Performance Engine

Optimization strength is layered:

1. official game/config/launcher settings;
2. external Windows/driver/API controls;
3. compatible graphics runtime controls;
4. engine/game-specific adapter controls.

Do not assume universal renderer/quality/resolution/shadow semantics across games. Deep controls require explicit engine/game knowledge and reversible paths. No anti-cheat/integrity bypass architecture.

## 15. Hardware Performance Engine

Approved future scope includes CPU topology/scheduler/power/boost, GPU/VRAM vendor capabilities, memory/working-set, storage/I/O, WDDM/display, network/latency and input responsiveness. Higher clocks/power are not inherently better; tuning remains capability-aware and evidence-based.

## 16. Deep Cleaner

Cleaner may support Safe, Game/System Deep and Extreme policies. User-created/personal data is a hard boundary. Extreme may remove healthy regenerable caches only with appropriate policy/warning; saves, mods, screenshots, recordings, presets, personal configs and documents are not automatically disposable.

## 17. System Optimizer session vs persistent state

Two scopes remain distinct:

- persistent PC optimization: Analyze → Preview → revalidate → apply/verify → History → Restore;
- temporary workload session optimization: snapshot → apply → monitor → restore when workload ends.

## 18. Recovery and risk

Rollback infrastructure remains mandatory even when Expert Safety Envelope is disabled. Critical real instability can force emergency rollback. Normal optimization excludes unsupported firmware/BIOS/voltage manipulation.

## 19. UX identity

Primary surfaces: Home, Optimize, Profiles, Guardian, Performance, Expert, History, Settings, Mini Mode.

Visual identity:

- Clean: light-blue/ice/frosted Liquid Glass, no red dominance;
- Dark: smoked graphite/black glass with ruby-red accent;
- Mini Mode: Compact/Mini/Micro, subtle ARGB border in both themes;
- UI overhead adapts so the optimizer does not materially hurt gameplay.

## 20. History / memory inside the product

History records what changed, when, why, by which engine, measured impact, whether kept/reverted and which state is Last Known Good. Snapshots are tuning/system recovery points. Raw telemetry may compact into bounded aggregates/events/long-term summaries.

## 21. Universal telemetry / workload capture authority — Track 4 GREEN

Track 4 is the canonical typed measurement/evidence foundation for later engines.

```text
stable workload identity
        +
bound transient runtime evidence
        ↓
exact capture target or explicit unavailable/ambiguous state
        ↓
direct typed collectors
        ↓
TelemetryFrame(source + quality + coverage + origin)
        ↓
bounded aggregation / typed diagnostics / A-B evidence
        ↓
existing validation, freshness, fingerprint and recommendation authority
```

Stable workload identity remains separate from runtime evidence. Exact process targeting uses bound `RunningProcess` evidence; duplicate evidence for one PID is equivalent, zero valid targets is unavailable, more than one distinct target is ambiguous, and `KnownExecutable` never authorizes live capture.

An explicitly selected stable universal workload has Performance capture-route precedence. Unavailable/ambiguous explicit selection blocks capture rather than silently falling back to another Guardian workload. No universal selection preserves the legacy Guardian/BlueStacks typed compatibility route.

Track 4 closing application SHA: `71991379e01518adf2e1c539491a9c0339a56735`; Windows CI #993 / run `34407420906` SUCCESS.

## 22. Universal Auto Tuner search-space and adapter authority — Track 5 Slices 1–2 GREEN

Track 5 generalizes existing Auto Tuner/Profiles additively. Specialized FF/BlueStacks working code is not replaced merely to obtain universal type names.

### Slice 1 — neutral search-space model

A `UniversalTuningDimension` carries stable id, scope (`System`/`Workload`), explicit `AuthorityId` and exact candidate values. `UniversalTuningCandidate` intentionally carries no evidence/confidence/recommendation/winner/persistence flag.

Blank/duplicate/empty declarations fail closed. Zero dimensions produce zero candidates. Planning is deterministic, bounded and side-effect-free. Windows System dimensions reuse Track 2 capability candidate plans only when `CanExplore == true`.

Slice 1 application SHA `797c8c7766adea3369948d9cb330bb7ba9a69d52`; Windows CI #1000 / run `34411645032` SUCCESS.

### Slice 2 — adapter-owned workload dimensions

Optional `GameAdapterTuningDimensionDeclaration` / `IGameTuningDimensionProvider` may expose workload search support only through the exact adapter resolved for stable `GameIdentity` and only with the required reversible configuration lifecycle.

Generic/unregistered/no-provider/incomplete cases expose zero dimensions. Final ids are namespaced as `workload.<normalized-adapter-id>.<normalized-local-id>`. Metadata is exploration only.

The BlueStacks/FF adapter did not receive invented static option lists because its real candidate set is machine/instance-dependent.

Slice 2 application SHA `8dac70fdb2c693533ae481aaadd846ab84fde228`; Windows CI #1007 / run `34416726382` SUCCESS.

## 23. Dynamic BlueStacks/FF universal candidate bridge — Track 5 Slice 3 GREEN

The existing BlueStacks/FF Auto Tuner generates candidates dynamically from machine + instance + mode. Rebuilding independent CPU/RAM/FPS/resolution/renderer option lists in the universal layer would create a second generator and could manufacture combinations absent from the specialized source.

Canonical flow:

```text
GameKind FreeFire / FreeFireMax
        ↓
LegacyGameIdentityBridge
        ↓
GameAdapterResolver
        ↓
exact matching BlueStacksFreeFireGameAdapter
        ↓
AutoTunerEngine.GenerateCandidates(environment, instance, mode)
        ↓
BlueStacksAutoTunerRuntime.BuildCandidatePlan(candidate, instance, capturedSettings)
        ↓
only applicable candidates survive
        ↓
1:1 UniversalTuningCandidate ↔ specialized TuningCandidate binding
```

`AutoTunerEngine.GenerateCandidates(...)` remains the only source of the specialized candidate set. The bridge may filter but never add/reorder/regenerate candidates. Exact bindings are authoritative for runnable/correlated candidates; workload dimension marginals are descriptive and must not be Cartesian-expanded.

Captured allow-listed instance state is required. Renderer is currently correlation-only: if known captured renderer conflicts with a generated non-`Auto` renderer, the candidate is excluded because renderer mutation is not verified.

Exact binding proves only identity/correlation/applicability. It grants no measured evidence, confidence, `Observed`, `Validated`, winner, recommendation, persistence or new mutation authority.

Slice 3 application SHA `39246089fb28f510287e79639356a4e16d1b6b02`; Windows CI #1014 / run `34422254555` SUCCESS; checkpoint `docs/project-memory/checkpoints/2026-09-09-track5-bluestacks-universal-candidate-bridge.complete`.

## 24. Evidence-backed universal winner/result projection — Track 5 Slice 4 GREEN

Slice 4 generalizes the output boundary without replacing the specialized BlueStacks/FF result or creating a second winner/profile authority.

Canonical flow:

```text
existing specialized TuningResult
        +
Slice 3 exact candidate-space bindings
        ↓
workload + adapter consistency gates
        ↓
exact evidence → binding correlation
        ↓
exact winner → source evidence → binding correlation
        ↓
read-only UniversalTuningResultProjection
```

Permanent types:

- `UniversalTuningEvidenceProjection` — exact existing `CandidateEvidence` + exact `UniversalTuningCandidate`;
- `UniversalTuningWinnerProjection` — exact existing `PerformanceProfile` + exact source `CandidateEvidence` + exact universal candidate;
- `UniversalTuningResultProjection` — stable `GameIdentity`, exact adapter id, exact existing `TuningResult`, evidence projections and winner projections;
- `BlueStacksUniversalTuningResultBridge` — pure/read-only correlator.

### Single source of winner/evidence authority

`AutoTunerEngine.SelectWinners(...)` remains the specialized BlueStacks/FF winner authority. The universal bridge performs no measurement, scoring, validation, winner selection, persistence, mutation or discovery.

The exact source `CandidateEvidence`, `PerformanceProfile` and `TuningResult` objects remain attached rather than being reconstructed into stronger neutral objects.

### Fail-closed identity and provenance

- `TuningResult.Game` must match `candidateSpace.Identity.LegacyGameKind`;
- `candidateSpace.AdapterId` must be nonblank and match stable `candidateSpace.Identity.AdapterId`;
- every evidence item must match exactly one Slice 3 binding;
- every winner must match exactly one existing source evidence configuration and then exactly one Slice 3 binding;
- zero or multiple matches fail closed;
- PID/path/process/display-name fuzzy correlation is not used.

### `Observed != Validated`

The projection preserves `EvidenceLevel` exactly. Universal candidate existence/binding cannot upgrade `Observed` to `Validated`.

An Observed-only specialized result remains winnerless after projection. The universal layer cannot invent a winner simply because an exact candidate binding exists.

The existing five specialized winner roles and order are preserved: Maximum FPS, Lowest Latency, Stability, Quality, Recommended.

### Compatibility preserved

Slice 4 does not change:

- `AutoTunerEngine` scoring/selection;
- `AutoTunerRunCoordinator`;
- `AutoTunerSessionService`;
- specialized runtime execution;
- persisted `PerformanceProfile` schema;
- Profile Challenge / Custom Validated promotion / incumbent freshness;
- direct typed PresentMon benchmark authority;
- Global Controlled Benchmark Lease;
- rollback / History;
- startup behavior.

### TDD / verification checkpoint

Temporary verifier: `ci/track5-universal-winner-profile-projection-verify`.

- initial RED: verifier #1 / run `34423384376`, SHA `726ba12f6e185a5a333ea66d7e86edb098577614` — bridge absent;
- Task 1 GREEN: verifier #2 / run `34423487642`, SHA `f9250eab9c18e998b9586a937e865fde7a494067`;
- cross-workload RED: verifier #3 / run `34423652214`, SHA `644dec7a627f3309b33e3b41a6d9796c96647fc1`;
- cross-workload GREEN: verifier #4 / run `34423775408`, SHA `2f19ba53d74651c1326aa3fbc9b299994d365f8b`;
- adapter-authority RED: verifier #5 / run `34425347150`, SHA `07c28539e0c3a1ed1d0b555c524bb1c804ca5940`;
- adapter-authority GREEN: verifier #6 / run `34425467591`, SHA `383f12477c83910de17dac2e260140abc98543ac`;
- final regression GREEN: verifier #7 / run `34425669336`, SHA `53ee4ee732d044c92960434368631e912b386390`;
- selective integration excluded the verifier workflow;
- official application SHA `f24c8c25612182db3c12351185fa54be227a8252`;
- Windows CI #1016 / run `34425901211` SUCCESS;
- checkpoint `docs/project-memory/checkpoints/2026-09-09-track5-universal-winner-profile-projection.complete`.

Track 5 remains ACTIVE. The next boundary is revalidation/validated promotion: carry exact universal provenance through the existing authority chain without letting correlation metadata satisfy measurement, freshness or validation gates.

## 25. Canonical track state

- Track 0 Foundation Hardening — GREEN
- Track 1 Universal Diagnostic Foundation — GREEN
- Track 2 System Optimizer — GREEN through current branch
- Track 3 Game Discovery + Adapter Framework — GREEN
- Track 4 Universal Telemetry / Evidence — GREEN for current canonical scope
- Track 5 Universal Auto Tuner + Profiles — ACTIVE; Slices 1–4 GREEN
- Track 6 Adaptive Guardian 2.0 — planned
- Track 7 Hardware Performance Engine — planned
- Track 8 Deep Cleaner — planned
- Track 9 Auto Optimize — planned
- Track 10 DG UX Migration — planned

The historical larger architecture extends beyond Track 10, but exact raw Track 11–19 numbering is not currently authoritative and must not be invented. Preserve approved future domains recorded in `ROADMAP.md` until the original source is recovered.
