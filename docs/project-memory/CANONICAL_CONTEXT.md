# DG Performance Engine — Canonical Context

## 1. Product lineage

The project began as **FF Performance Engine**, a Windows-native adaptive optimizer centered on BlueStacks + Free Fire / Free Fire MAX. It was deliberately universal across compatible Windows PCs rather than hard-coded to one machine.

The product expanded into **DG Performance Engine**. This is not a rewrite. The original system is the first specialized implementation of the larger performance-control architecture.

```text
FF PERFORMANCE ENGINE
Detection + Hardware/System Analysis + Telemetry + A/B + Auto Tuner
+ Profiles + Guardian + History + Snapshot/Rollback + FF/BlueStacks
                         ↓ generalize, do not discard
DG PERFORMANCE ENGINE
Windows + Hardware + Games + Graphics Runtime + Cleaner + Learning
```

Physical namespaces `FFPerformanceEngine.*` remain for now. Rename/migration is gradual and must not invalidate Git history, CI or verified behavior.

## 2. Product philosophy

The engine is not a catalog of gamer tweaks.

```text
MACHINE + WINDOWS + DRIVER + WORKLOAD + MODE + STATE + CONFIG + EVIDENCE
                                  ↓
                        BEST KNOWN DECISION
```

Rules:

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

Current Git/code/tests + fresh exact Windows CI outrank stale chat or stale memory. Do not roll working code backward because an older document says something different; update the document after the checkpoint is proven.

## 4. User-facing operating modes

### Equilibrado

Prioritizes stability, efficiency, thermal behavior and performance gains without aggressively sacrificing parallel/background work.

### Desempenho — default

Primary mode. Pursues measurable performance where it matters without blindly enabling every aggressive setting.

### Extremo

Uses the strongest supported Windows/hardware/game strategies when evidence shows value. It may accept higher power, sustained clocks, fan activity and reduced background work, but “extreme” never means keeping a regression.

Safety Envelope is optional in Expert, warnings remain visible, and critical real instability can trigger emergency rollback. The engine does not bypass firmware/driver protections or remove hardware safety mechanisms not normally exposed.

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

Owns presentation, application orchestration, profiles, history, policies, high-level Auto Tuner, persistence, Windows integration that does not require a hot native path, and adapter logic appropriate for managed code.

### C++20 / Win32

Owns low-latency/native/platform functions where native execution materially helps: precise timing, process/affinity/priority control, selected telemetry/sensor/vendor integrations and low-overhead services.

Interop remains narrow and contractual. UI never directly owns native mutation policy.

## 7. Diagnostic model

The original Hardware/System Scan is generalized, not duplicated.

Relevant domains include CPU topology/clocks/utilization/power/thermal signals; GPU model/VRAM/clocks/utilization/power/thermal/provider controls where available; RAM capacity/pressure; storage/I/O where provable; Windows build/drivers/power/process/services/tasks/startup/scheduling; workload/game context and display state.

The Bottleneck Analyzer may classify CPU/main-thread, GPU, VRAM, RAM, I/O, thermal, power, background contention, frame pacing, renderer/engine limit, network or Unknown. Unsupported channels stay Unknown.

## 8. Windows Performance Capability model

Windows optimization is modeled as capabilities, never loose scripts.

Each capability has identity, current state, candidate space, applicability/dependencies, behavior (`LIVE_SAFE`, session/persistent/restart/reboot), safety/conflicts, read/validate/snapshot/apply/verify/rollback and evidence.

Transaction model:

```text
resolve graph → validate all → snapshot all → durable restore point
→ apply in dependency order → verify → History
failure anywhere → reverse rollback → verify restoration
```

Current real examples include active power policy, CPU boost policy and CPU core parking policy. Recommendation does not mean “maximum setting”; it must be backed by diagnosis/evidence.

## 9. Experimental integrity / evidence ladder

Global controlled measurements share one machine-wide benchmark lease. Guardian is suspended/reconciled around controlled measurements as required.

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

## 10. Profiles and winner frontier

The engine does not search for one universal “best” profile. It maintains objective-specific winners:

- Recommended
- Maximum FPS
- Lowest Latency
- Stability
- Quality
- Custom Validated
- Low-End Recovery as an expanded DG strategy

The same candidate may win more than one role when evidence supports it.

Profiles evolve toward:

```text
Machine → Workload/Game → Objective/Profile → Evidence
```

A Custom Validated profile can challenge incumbents only through compatible fresh controlled evidence; existence alone never authorizes promotion.

## 11. Guardian

Guardian is an adaptive session supervisor, not an always-tweak daemon.

```text
detect state → detect real degradation → identify likely cause
→ choose LIVE_SAFE intervention → canary/A-B → keep or rollback
```

Game/session state started with BlueStacks/FF and generalizes toward workload state. During active gameplay, only minimal/live-safe actions are eligible; larger changes wait for safer boundaries.

## 12. Adaptive Performance Governor

Governor is continuous control inside already-known safe policy ranges and is distinct from Guardian experiments and Auto Tuner exploration.

Strategies include Fixed FPS, Minimum FPS, Maximum Quality within FPS floor and Minimum Latency. Hysteresis, cooldown, step limits and `LIVE_SAFE` classification are required to avoid oscillation.

## 13. Performance Cost Model / learning

Evidence strength order:

1. repeated controlled Auto Tuner A/B;
2. controlled live micro-experiments;
3. repeated passive Guardian observations;
4. isolated passive observations.

Passive evidence can reduce confidence/request revalidation after drift, but cannot silently overwrite stronger controlled evidence.

## 14. Graphics / Game Performance Engine

Optimization strength is layered:

1. official game/config/launcher settings;
2. external Windows/driver/API controls;
3. compatible graphics runtime controls;
4. engine/game-specific adapter controls.

Never assume every game has universal shadow/quality/renderer semantics. Generic optimization is limited to what can be proven generically; deep controls require engine/game knowledge and reversible paths.

For anti-cheat/integrity-protected games, use supported configuration, Windows, driver, hardware and external paths only. No bypass architecture.

## 15. Hardware Performance Engine

Approved future scope includes CPU topology/scheduler, CPU power/boost, GPU/VRAM vendor capabilities, memory/working set, storage/I/O, WDDM/display, network/latency and input responsiveness. Higher clocks/power are not inherently better; tuning remains capability-aware and evidence-based.

## 16. Deep Cleaner

Cleaner may have Safe, Game/System Deep and Extreme policies, but user-created/personal data is a hard boundary. Extreme may remove healthy regenerable caches after warning; never automatically treat saves, mods, screenshots, recordings, presets, personal configs or documents as disposable.

## 17. System Optimizer session vs persistent state

Two scopes remain distinct:

- persistent PC optimization: Analyze → Preview → revalidate → apply/verify → History → Restore;
- temporary workload session optimization: snapshot → apply → monitor → restore when workload ends.

## 18. Recovery and risk

Rollback infrastructure is not optional even when Expert Safety Envelope is disabled. Critical real instability can force emergency rollback. Normal optimization does not include BIOS flashing, firmware modification, arbitrary voltage or forcing unsupported driver/hardware limits.

## 19. UX identity

Primary surfaces: Home, Optimize, Profiles, Guardian, Performance, Expert, History, Settings, Mini Mode.

Visual identity:

- Clean: light-blue/ice/frosted Liquid Glass, no red dominance;
- Dark: smoked graphite/black glass with ruby-red accent;
- Mini Mode: Compact/Mini/Micro, subtle ARGB border in both themes;
- UI quality/overhead adapts so the optimizer does not materially hurt gameplay.

## 20. History / memory inside the product

History records what changed, when, why, by which engine, measured impact, whether kept/reverted and which state is Last Known Good. Snapshots are tuning/system recovery points; backups are internal product data backups. Raw telemetry may compact into bounded aggregates/events/long-term summaries.

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

Stable workload identity remains separate from runtime target evidence. Exact process targeting uses bound `RunningProcess` evidence; duplicate evidence for the same PID is equivalent, zero valid targets is unavailable, more than one distinct target is ambiguous, and `KnownExecutable` never authorizes live capture.

An explicitly selected stable universal workload has Performance capture-route precedence. Unavailable/ambiguous explicit selection blocks capture rather than silently falling back to an unrelated Guardian workload. No universal selection preserves the legacy Guardian/BlueStacks typed compatibility route.

Track 4 closing application SHA: `71991379e01518adf2e1c539491a9c0339a56735`; Windows CI #993 / run `34407420906` SUCCESS.

## 22. Universal Auto Tuner search-space and adapter authority — Track 5 Slices 1–2 GREEN

Track 5 generalizes existing Auto Tuner/Profiles additively. Specialized FF/BlueStacks working code is not replaced merely to obtain universal type names.

### Slice 1 — neutral search-space model

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

A universal dimension contains stable `Id`, `Scope` (`System` or `Workload`), explicit `AuthorityId` and exact `CandidateValues`. `UniversalTuningCandidate` is only dimension id → selected value and intentionally carries no evidence/confidence/recommendation/winner/persistence flag.

Blank/duplicate/empty declarations fail closed. Zero dimensions produce zero candidates. `UniversalTuningSearchSpacePlanner` validates, deterministically orders dimensions, preserves producer value order, builds a bounded Cartesian prefix and performs no discovery/mutation/evidence/recommendation work.

Windows system dimensions reuse Track 2 `WindowsCapabilityCandidatePlan`; only `CanExplore == true` enters the universal search space.

Slice 1 application SHA `797c8c7766adea3369948d9cb330bb7ba9a69d52`; Windows CI #1000 / run `34411645032` SUCCESS.

### Slice 2 — adapter-owned workload dimensions

`IGameAdapter` remains unchanged. Optional `GameAdapterTuningDimensionDeclaration` / `IGameTuningDimensionProvider` may expose workload search support only when resolved adapter proves reversible configuration capability (`ConfigDiscovery + ConfigSnapshot + ConfigMutation + Rollback`).

Authority comes from the adapter resolved for stable selected `GameIdentity`, never PID/path/process/display name or an unregistered requested adapter string. Generic/unregistered/no-provider/incomplete reversible-lifecycle cases contribute zero workload dimensions.

Accepted workload identities are namespaced as:

```text
workload.<normalized-adapter-id>.<normalized-local-id>
```

Candidate value text/order remains exact. Null/blank/empty/duplicate declarations fail closed. System + Workload dimensions reuse the same universal planner.

The current BlueStacks/FF adapter deliberately did not receive static declarations in Slice 2 because its real candidate set is machine/instance-dependent.

Slice 2 application SHA `8dac70fdb2c693533ae481aaadd846ab84fde228`; Windows CI #1007 / run `34416726382` SUCCESS.

## 23. Dynamic BlueStacks/FF universal candidate bridge — Track 5 Slice 3 GREEN 2026-09-09

### Problem closed

The existing BlueStacks/FF Auto Tuner already generates candidates dynamically from `EnvironmentSnapshot`, `BlueStacksInstance` and tuning mode. It also applies an existing bounded source order (`Adaptive <= 12`, `Deep <= 96`) and separately checks installed BlueStacks allow-listed config before mutation.

A universal layer that recreated independent CPU/RAM/FPS/resolution/renderer option lists and Cartesian-expanded them would be a second candidate generator and could manufacture combinations that the specialized engine never emitted.

### Canonical bridge

Slice 3 adds:

- `BlueStacksUniversalTuningCandidateBinding`;
- `BlueStacksUniversalTuningCandidateSpace`;
- `BlueStacksUniversalTuningCandidateBridge`.

The canonical flow is:

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
for each generated candidate in source order
        ↓
BlueStacksAutoTunerRuntime.BuildCandidatePlan(candidate, instance, capturedSettings)
        ↓
only CanApply candidates survive
        ↓
1:1 UniversalTuningCandidate ↔ specialized TuningCandidate binding
```

### Single source of truth

`AutoTunerEngine.GenerateCandidates(...)` remains the only source of the specialized BlueStacks candidate set. The universal bridge:

- may filter generated candidates;
- must preserve surviving order;
- must preserve the generator's existing candidate budget;
- must never independently regenerate or expand the specialized candidate set.

`BlueStacksAutoTunerRuntime.BuildCandidatePlan(...)` remains installed-build applicability authority for the captured allow-listed configuration surface.

### Exact bindings vs descriptive dimensions

Each surviving specialized candidate is mapped losslessly to a neutral candidate carrying exactly the five current specialized fields:

- CPU cores;
- RAM MB;
- renderer;
- FPS target;
- resolution.

Ids are under `workload.<resolved-adapter-id>.*` and `AuthorityId` is the resolved specialized adapter id.

The per-axis dimension values are **descriptive marginals of the surviving binding set**. They are useful for neutral introspection/search metadata but are not the runnable candidate authority. The exact binding list is authoritative for this specialized bridge.

Therefore the marginals must not be passed through a Cartesian expansion to invent candidate combinations that were not emitted by `AutoTunerEngine.GenerateCandidates(...)`.

### Stable identity / adapter authority

Stable identity comes only from `LegacyGameIdentityBridge.FromGameKind(...)`. The bridge requires the resolver to return the exact matching `BlueStacksFreeFireGameAdapter` for the same `GameKind`; wrong/generic/unavailable specialization fails closed.

No PID, executable path, process name or display name creates workload tuning authority.

### Installed-config fail-closed behavior

The caller supplies captured allow-listed settings for the named BlueStacks instance. If the snapshot does not contain that instance namespace, the specialized universal candidate space is empty.

Generated candidates that `BuildCandidatePlan(...)` cannot represent are absent.

A TDD-discovered special case is renderer correlation:

- BlueStacks currently captures `graphics_renderer` / `graphics_engine` as installed-state evidence;
- current runtime intentionally does not automatically mutate renderer because mutation semantics are version-dependent/unverified;
- therefore a generated non-`Auto` candidate whose renderer conflicts with the known captured renderer is excluded from the universal binding set;
- this prevents benchmark evidence from being attributed to a renderer that the runtime did not actually apply;
- Slice 3 does **not** add renderer mutation.

### Authority boundary

An exact universal↔specialized binding proves only:

- stable workload/adapter correlation;
- the specialized generator emitted that candidate;
- the current installed-build planner can represent it under the captured snapshot.

It does **not** grant:

- measured evidence;
- confidence;
- `Observed`;
- `Validated`;
- winner status;
- recommendation authority;
- persistence permission;
- any new mutation authority.

Existing direct typed PresentMon evidence, repeatability, exact configuration correlation, machine fingerprint/freshness, validation challenges, Custom Validated promotion, Global Controlled Benchmark Lease, rollback and History remain authoritative.

### Compatibility preserved

Slice 3 intentionally does not change:

- `AutoTunerRunCoordinator`;
- `AutoTunerSessionService`;
- specialized runtime execution semantics;
- existing five winner roles;
- Profile persistence;
- Profile Challenge / incumbent freshness;
- Track 4 typed benchmark authority.

### TDD / verification checkpoint

Temporary verifier branch: `ci/track5-bluestacks-universal-candidate-bridge-verify`.

- initial RED: run `34417641306` — bridge/binding contracts absent;
- Task 1 GREEN: verifier #4 / run `34417996142`, SHA `e434f8e2a83f466408d91ab6e90a980b9de3d7e7`;
- Task 2 RED: verifier #5 / run `34418189031`, SHA `888cd58d59fe845304e428a79653989499d39c64` — renderer drift incorrectly survived;
- final verifier GREEN: #6 / run `34418407183`, SHA `07180bd58fc5ff0b01ef3b9038056fe2693d30bb`;
- selective integration excluded the temporary verifier workflow;
- official application SHA: `39246089fb28f510287e79639356a4e16d1b6b02`;
- Windows CI #1014 / run `34422254555` — SUCCESS;
- durable checkpoint: `docs/project-memory/checkpoints/2026-09-09-track5-bluestacks-universal-candidate-bridge.complete`.

Track 5 remains **ACTIVE**. The next additive slice is the evidence-backed winner/profile output boundary: carry stable workload + exact universal candidate/config correlation without weakening the existing five BlueStacks/FF winner roles, Custom Validated promotion or typed evidence/freshness/validation authority.

## 24. Canonical track state

- Track 0 Foundation Hardening — GREEN
- Track 1 Universal Diagnostic Foundation — GREEN
- Track 2 System Optimizer — GREEN through current branch
- Track 3 Game Discovery + Adapter Framework — GREEN
- Track 4 Universal Telemetry / Evidence — GREEN for current canonical scope
- Track 5 Universal Auto Tuner + Profiles — ACTIVE; Slices 1–3 GREEN
- Track 6 Adaptive Guardian 2.0 — planned
- Track 7 Hardware Performance Engine — planned
- Track 8 Deep Cleaner — planned
- Track 9 Auto Optimize — planned
- Track 10 DG UX Migration — planned

The historical larger architecture reportedly extends beyond Track 10, but the exact raw Track 11–19 numbering is not currently authoritative and must not be invented. Preserve the approved future domains recorded in `ROADMAP.md` until the original source is recovered.
