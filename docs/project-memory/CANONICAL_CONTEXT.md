# DG Performance Engine — Canonical Context

## 1. Product lineage

The project began as **FF Performance Engine**, a Windows-native adaptive optimizer centered on BlueStacks + Free Fire / Free Fire MAX. It was deliberately universal across compatible Windows PCs rather than hard-coded to one machine.

The product then expanded into **DG Performance Engine**. This is not a rewrite. The original system is the first specialized implementation of a larger performance-control architecture.

```text
FF PERFORMANCE ENGINE
Detection + Hardware/System Analysis + Telemetry + A/B + Auto Tuner
+ Profiles + Guardian + History + Snapshot/Rollback + FF/BlueStacks
                         ↓ generalize, do not discard
DG PERFORMANCE ENGINE
Windows + Hardware + Games + Graphics Runtime + Cleaner + Learning
```

The physical namespaces `FFPerformanceEngine.*` remain for now. Rename/migration is gradual and must not invalidate Git history, CI or verified behavior.

## 2. Product philosophy

The engine is not a catalog of gamer tweaks.

```text
MACHINE + WINDOWS + DRIVER + WORKLOAD + MODE + STATE + CONFIG + EVIDENCE
                                  ↓
                        BEST KNOWN DECISION
```

Rules:

- no preset is good merely because it is popular;
- baseline and prior state must be known before mutation;
- meaningful mutations require rollback;
- contaminated benchmarks are discarded;
- isolated observations do not become causal proof;
- evidence quality/confidence/freshness are first-class;
- the application measures its own overhead;
- missing data remains missing.

## 3. User-facing operating modes

### Equilibrado

Prioritizes stability, efficiency, thermal behavior and performance gains without aggressively sacrificing parallel/background work.

### Desempenho — default

Primary mode. Pursues measurable performance where it matters, without blindly enabling every aggressive setting.

### Extremo

Uses the strongest supported Windows/hardware/game strategies when evidence shows value. It may accept higher power, sustained clocks, fan activity and reduced background work, but “extreme” never means keeping a regression.

Safety Envelope is optional in Expert, risk warnings remain visible, and critical real instability can trigger emergency rollback. The system does not bypass firmware/driver protections or remove hardware safety mechanisms that are not normally exposed.

## 4. High-level architecture

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

## 5. Platform split

### C#/.NET 8 + WPF

Owns presentation, application orchestration, profiles, history, policies, high-level Auto Tuner, persistence, Windows integration that does not require a hot native path, and adapter logic appropriate for managed code.

### C++20 / Win32

Owns low-latency/native/platform functions where native execution materially helps: precise timing, process/affinity/priority control, selected telemetry/sensor/vendor integrations, and low-overhead services.

Interop remains narrow and contractual. UI never directly owns native mutation policy.

## 6. Diagnostic model

The original Hardware/System Scan is generalized, not duplicated.

- CPU: topology, cores/threads, P/E classes when available, CCD/CCX/cache/NUMA/groups where available, clocks/boost, utilization, power, thermals/throttling signals.
- GPU: model/vendor, VRAM, clocks, utilization, power, thermals, supported driver controls.
- RAM: capacity, commit, pressure, working sets.
- Storage/I/O: media/type where provable, free space, latency/queue/activity.
- Windows: build, drivers, power, processes, services, tasks, startup, scheduling state.
- workload/game context and monitor/display state.

The Bottleneck Analyzer can classify CPU/main-thread, GPU, VRAM, RAM, I/O, thermal, power, background contention, frame pacing, renderer/engine limit, network or Unknown.

A diagnostic score may be displayed, but optimization decisions use the underlying measurements and capabilities, not a decorative global score.

## 7. Windows Performance Capability model

Windows optimization is modeled as capabilities, never loose scripts.

Each capability has identity, current state, candidate space, applicability/dependencies, behavior (`LIVE_SAFE`, session/persistent/restart/reboot), safety/conflicts, read/validate/snapshot/apply/verify/rollback and evidence.

The transaction model is:

```text
resolve graph → validate all → snapshot all → durable restore point
→ apply in dependency order → verify → History
failure anywhere → reverse rollback → verify restoration
```

Current real examples include active power policy, CPU boost policy and CPU core parking policy. Recommendation does not equal “maximum setting”; it must be backed by diagnosis/evidence.

## 8. Experimental integrity / evidence ladder

Global controlled measurements share one machine-wide benchmark lease. Guardian is suspended/reconciled around controlled measurements as required.

Evidence progression for Windows capability experiments is intentionally strict:

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

Freshness is bound to machine fingerprint, workload, baseline/candidate tuple and relevant environment state.

## 9. Profiles and winner frontier

The engine does not search for one universal “best” profile. It maintains objective-specific winners:

- Recommended
- Maximum FPS
- Lowest Latency
- Stability
- Quality
- Custom Validated
- Low-End Recovery (expanded DG strategy)

The same candidate may win more than one role when the evidence supports it.

Profiles evolve toward:

```text
Machine → Workload/Game → Objective/Profile → Evidence
```

A Custom Validated profile can challenge incumbents through controlled A/B. Promotion requires compatible fresh evidence; it is never automatic merely because the custom profile exists.

## 10. Guardian

Guardian is an adaptive session supervisor, not an “always tweak” daemon.

Core loop:

```text
detect state → detect real degradation → identify likely cause
→ choose LIVE_SAFE intervention → canary/A-B → keep or rollback
```

Game/session state started with BlueStacks/FF and generalizes toward workload state. During active gameplay, only minimal/live-safe actions are eligible; larger changes wait for lobby/post-session or restart boundaries.

Guardian supports passive learning and may eventually run tightly budgeted live micro-experiments. Recovery performance always has priority over learning speed.

## 11. Adaptive Performance Governor

The Governor is continuous control inside already-known safe policy ranges. It is distinct from Guardian experiments and Auto Tuner exploration.

Strategies:

- Fixed FPS
- Minimum FPS
- Maximum Quality within FPS floor
- Minimum Latency

It uses hysteresis, cooldown, step limits and `LIVE_SAFE` capability classification to avoid oscillation.

## 12. Performance Cost Model / learning

DG learns a cost/response model per machine + workload/game + environment + profile, later scene/load-aware.

Evidence sources have different strength:

1. repeated controlled Auto Tuner A/B — strongest;
2. controlled live micro-experiments — intermediate;
3. repeated passive Guardian observations — contextual support;
4. isolated passive observations — low confidence.

Passive data can reduce confidence and request revalidation after game/driver/environment drift; it cannot silently overwrite strong controlled evidence.

## 13. Graphics / Game Performance Engine

Optimization strength is layered:

1. official game/config/launcher settings;
2. external Windows/driver/API controls;
3. compatible graphics runtime controls;
4. engine/game-specific adapter controls.

The architecture must never assume every game has universal “shadow low” semantics. Generic optimization is limited to what can be proven generically; deep controls require engine/game knowledge.

For compatible workloads, DG may support internal render resolution below the menu minimum, upscaling/sharpening, and an extended graphics range (`DG Low`, `DG Ultra Low`, `Low-End Recovery`). These are candidates to measure, not guarantees.

For anti-cheat/integrity-protected games, DG remains conservative: supported configuration, Windows, driver, hardware and external paths only. No bypass design.

## 14. Hardware Performance Engine

Approved future scope includes:

- CPU Topology & Scheduler Engine;
- CPU Power & Boost Engine;
- GPU/VRAM vendor capability adapters;
- memory/working-set and storage/I/O engines;
- WDDM/display, network/latency and input responsiveness domains.

Hardware tuning is capability-aware and evidence-based. Higher clocks/power are not inherently better.

## 15. Deep Cleaner

Cleaner has Safe, Game/System Deep and Extreme policies, but personal/user-created data is a hard boundary.

Extreme Cleanup may remove healthy *regenerable* caches after clear warning. It never automatically treats saves, mods, screenshots, recordings, presets, personal configs, documents or other user-created content as disposable.

## 16. System Optimizer session vs persistent state

Two distinct scopes:

- persistent PC optimization (“Otimizar este PC”): startup/system policies/storage/etc. with explicit preview/history/restore;
- temporary game-session optimization: snapshot → apply → monitor → restore when the workload ends.

In Extreme mode, nonessential background processes/tasks/services may be contained temporarily when dependencies prove that doing so is safe for the workload and Windows.

## 17. Recovery and risk

Safety Envelope is optional in Expert, but rollback infrastructure is not optional.

- Critical real instability can force emergency rollback.
- High-risk events are policy configurable.
- Moderate/low events are logged/alerted.
- No BIOS flashing, firmware modification, arbitrary voltage, bypass of thermal protections or forcing unsupported driver/hardware limits as normal DG optimization.

## 18. UX identity

Primary surfaces:

- Home
- Optimize
- Profiles
- Guardian
- Performance
- Expert
- History
- Settings
- Mini Mode

Visual identity:

- Clean: light-blue/ice/frosted Liquid Glass, no red theme dominance.
- Dark: smoked graphite/black glass with ruby-red accent.
- Mini Mode: separate native HUD, Compact/Mini/Micro, subtle ARGB border in both themes.
- Liquid Glass quality must adapt to machine performance; the optimizer must not materially hurt gameplay because its UI is expensive.

## 19. History / memory inside the product

History is not just logs. It answers what changed, when, why, by whom/which engine, measured impact, whether kept/reverted, and which state is Last Known Good.

Snapshots are system/tuning recovery points. Backups are internal DG data backups. They are separate concepts.

Raw telemetry can be compacted into session aggregates/events/long-term summaries to avoid unbounded storage.

## 20. Universal telemetry / workload capture authority — Track 4 closed 2026-09-09

Track 4 is the canonical typed measurement/evidence foundation for every later engine. Its completed model is:

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

### Stable identity versus runtime targeting

Stable workload identity remains a Track 3 concern and comes from source-native GameId contracts. Runtime process targeting is separate transient evidence.

For process-specific universal Performance capture:

1. an application workflow explicitly selects a stable GameId from a resolved Track 3 catalog;
2. only bound evidence for that selected GameId is retained for capture targeting;
3. only `RunningProcess` observations with positive PID and fully qualified path are candidates;
4. duplicate evidence for the same PID is equivalent, not ambiguity;
5. exactly one distinct valid PID resolves `ExactRunningProcess`;
6. zero valid running PIDs resolves unavailable with the proven GameId but no PID/path;
7. more than one distinct valid PID resolves ambiguous with no guessed PID/path;
8. `KnownExecutable`/App Paths never authorizes a live process capture;
9. blank/unknown GameId is never promoted;
10. PID/path/process/display name never manufactures stable GameId.

### Application capture-route precedence

When a universal stable GameId is explicitly selected, that selection owns Performance capture routing.

- exact target → direct typed process capture is allowed;
- unavailable/ambiguous target → capture is blocked;
- while that selection exists, the app must **not** silently fall back to an unrelated Guardian/BlueStacks workload;
- when no universal selection exists, the legacy Guardian/BlueStacks typed route remains the compatibility path.

### Track 4 completion checkpoint

Closing application SHA: `71991379e01518adf2e1c539491a9c0339a56735`.

Windows CI #993 / run `34407420906` — SUCCESS.

Track 4 is **GREEN for its current canonical scope**.

## 21. Universal Auto Tuner search-space authority — Track 5 Slice 1 GREEN 2026-09-09

Track 5 generalizes the existing Auto Tuner and Profiles additively. The specialized FF/BlueStacks tuner remains working code and is not replaced merely to obtain universal type names.

The first universal tuning layer deliberately separates three concepts:

```text
PROVEN SUPPORT / DECLARED OPTION
              ↓
      UNIVERSAL SEARCH SPACE
              ↓
     CONTROLLED MEASUREMENT
              ↓
        TYPED EVIDENCE
              ↓
 REPEATABILITY / FRESHNESS / VALIDATION
              ↓
 WINNER / RECOMMENDATION AUTHORITY
```

An item appearing in the search space proves only that an explicit authority exposed it as an explorable candidate. It does **not** prove that the setting is beneficial, recommended, validated, safe to persist, or entitled to a profile winner role.

### Neutral dimension model

A universal tuning dimension has:

- `Id`: stable dimension identity;
- `Scope`: `System` or `Workload`;
- `AuthorityId`: the authority that explicitly supplied the candidate space;
- `CandidateValues`: exact declared options.

The neutral `UniversalTuningCandidate` contains only a mapping of dimension id → selected value. It intentionally has no confidence, evidence level, recommendation, winner role or persistence flag.

### Fail-closed declaration rules

Universal tuning rejects rather than repairs/invents:

- blank dimension id;
- blank authority id;
- an empty candidate list;
- blank candidate values;
- duplicate dimension ids case-insensitively;
- duplicate candidate values.

Zero dimensions produce zero candidates. There is no implicit “default candidate”.

### Deterministic bounded exploration

`UniversalTuningSearchSpacePlanner`:

1. validates all declarations before returning candidates;
2. orders dimensions deterministically by id;
3. preserves each authority's declared candidate-value order exactly;
4. forms the Cartesian product;
5. varies the last sorted dimension fastest;
6. stops at `MaxCandidates` as a deterministic prefix;
7. never randomizes candidate order;
8. never creates hidden axes or default values;
9. performs no machine mutation and grants no evidence/recommendation authority.

This bounded product is a planning primitive, not an instruction to benchmark every possible combination blindly. Later Track 5 search strategy can prune/adapt exploration while preserving the declared-authority boundary.

### System dimensions reuse Track 2 authority

Windows/system tuning dimensions do not recreate capability discovery.

The bridge consumes the existing `WindowsCapabilityCandidatePlan` produced by the Track 2 capability candidate planner.

Only `CanExplore == true` enters the universal search space.

Therefore:

- `Unavailable` stays absent;
- `MissingCurrentState` stays absent;
- `NoCandidateSpace` stays absent;
- `Ready` with zero candidates stays absent.

For accepted plans:

- normalized CapabilityId is both dimension identity and authority identity;
- TargetValue strings remain exact;
- ExplorationRank determines target order;
- duplicate target values fail closed.

The bridge does not inspect the registry, infer availability, generate schema points, mutate Windows, consult recommendation confidence/value or publish a recommendation.

### Specialized compatibility

The existing FF/BlueStacks path remains unchanged and source-compatible:

- `TuningCandidate`;
- `AutoTunerEngine.GenerateCandidates(...)`;
- `AutoTunerSessionService`;
- `BlueStacksAutoTunerRuntime`;
- existing five winner roles;
- Custom Validated challenge/promotion mechanisms.

Future migration of that specialized search space into the neutral universal model receives its own TDD slice. Do not force a destructive conversion merely because universal contracts now exist.

### Workload/game authority requirement

The next universal dimensions must come from existing or deliberately extended Game Adapter authority.

Do not assume these names/semantics are universal:

- renderer;
- graphics quality;
- resolution;
- FPS target;
- internal render scale;
- game engine toggles.

A generic adapter may expose only what it can prove. A specialized adapter may expose deeper dimensions only when it understands the workload/configuration semantics and can support reversible application/verification as later orchestration requires.

Do not create a second independent catalog of game options inside Auto Tuner.

### Verification checkpoint

Application SHA:

`797c8c7766adea3369948d9cb330bb7ba9a69d52`

Windows CI:

`#1000` / run `34411645032` — SUCCESS.

Durable checkpoint:

`docs/project-memory/checkpoints/2026-09-09-track5-universal-search-space.complete`

Track 5 remains **ACTIVE**. Slice 1 is GREEN; next is capability-honest workload/game-adapter dimensions.

## 22. Adapter-owned workload tuning authority — Track 5 Slice 2 GREEN 2026-09-09

Track 5 now has an additive seam for game/workload-specific candidate dimensions without turning any game-specific setting into a universal semantic.

The authoritative flow is:

```text
stable GameIdentity
       ↓
GameAdapterResolver
       ↓
resolved specialized adapter
       ↓
reversible config lifecycle capability gate
       ↓
optional IGameTuningDimensionProvider
       ↓
explicit workload dimensions
       ↓
UniversalTuningSearchSpacePlanner
```

### Optional provider, unchanged adapter base contract

`IGameAdapter` remains unchanged and source-compatible.

A specialized adapter may opt in through:

- `GameAdapterTuningDimensionDeclaration`;
- `IGameTuningDimensionProvider`.

Generic adapters and existing adapters that do not opt in continue working normally and contribute zero game-specific tuning dimensions.

The current `BlueStacksFreeFireGameAdapter` deliberately does not expose static declarations yet. Its existing candidate space depends on real machine + BlueStacks instance state and remains owned by the existing specialized generator until a dedicated bridge slice proves the mapping.

### Resolved authority, never process-derived authority

`UniversalTuningWorkloadDimensionFactory` accepts a stable `GameIdentity` and resolves the adapter through `GameAdapterResolver`.

Authority comes from the **resolved adapter**. It never comes from:

- PID;
- executable path;
- process name;
- display name;
- an unregistered raw `GameIdentity.AdapterId` string.

If the requested specialization is not registered, the resolver falls back to Generic and the result is zero workload dimensions.

### Reversible configuration capability gate

Before provider metadata is consulted, the resolved adapter must prove all of:

- `ConfigDiscovery`;
- `ConfigSnapshot`;
- `ConfigMutation`;
- `Rollback`.

If any one is absent, the provider is not invoked and the adapter contributes zero workload dimensions.

`BenchmarkPreparation` is intentionally not required merely to **declare** an explorable dimension. Actual controlled benchmark execution remains separate orchestration and must continue to use the existing benchmark lease/evidence authority.

### Namespace and declaration integrity

Accepted dimensions are projected as:

```text
Id          = workload.<normalized-adapter-id>.<normalized-local-id>
Scope       = Workload
AuthorityId = <normalized resolved adapter id>
Values      = exact provider-declared candidate strings/order
```

Normalization applies only to identity (`Trim().ToLowerInvariant()`). Candidate value text is preserved exactly.

The factory validates the complete provider declaration set and fails closed for:

- null provider result;
- null declaration;
- blank local id;
- empty candidate-value list;
- blank candidate value;
- duplicate local ids case-insensitively;
- duplicate exact candidate values.

Returned workload dimensions are deterministically ordered by final dimension id.

### Composition remains single-source

No new Cartesian/composition engine was added.

The existing `UniversalTuningSearchSpacePlanner` composes explicit System + Workload dimensions directly. This preserves one deterministic search-space authority and prevents a parallel game-specific planner from manufacturing hidden/default axes.

### Critical authority separation

**Adapter-declared dimension support proves only that the resolved adapter exposes an explorable configuration space.**

It does not grant:

- measured evidence;
- confidence;
- `Observed`;
- `Validated`;
- permission to mutate;
- permission to persist;
- profile winner status;
- recommendation authority.

The required downstream chain remains:

```text
explicit support/search space
→ controlled measurement
→ typed evidence
→ repeatability/evaluation
→ fingerprint/freshness
→ validation challenge where applicable
→ ValidatedEvidence
→ winner/recommendation authority
```

### Verification checkpoint

Application SHA:

`8dac70fdb2c693533ae481aaadd846ab84fde228`

Windows CI:

`#1007` / run `34416726382` — SUCCESS.

TDD verifier evidence:

- RED `34412460197` → optional provider contracts absent;
- GREEN `34412615403` → provider contract/source compatibility passed;
- RED `34412791339` → workload dimension factory absent;
- GREEN `34412911108` → factory + fail-closed rules + System/Workload composition passed.

Durable checkpoint:

`docs/project-memory/checkpoints/2026-09-09-track5-game-adapter-tuning-dimensions.complete`

Track 5 remains **ACTIVE**. The next slice must bridge the existing dynamic BlueStacks/FF candidate generator into the neutral universal abstraction without creating a duplicate static option catalog or breaking the specialized runtime/profile path.
