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
│   ├── Environment / Hardware Discovery
│   ├── MachineContext + Fingerprint
│   ├── Capability Registry/Graph
│   └── Bottleneck Analyzer
│
├── Evidence
│   ├── Telemetry Engine / PresentMon
│   ├── Benchmark + A/B
│   ├── Data Quality / Confidence
│   └── Validated Evidence
│
├── Optimization Engines
│   ├── System Optimizer
│   ├── Hardware Performance Engine
│   ├── Game / Graphics Runtime Engine
│   ├── Deep Cleaner
│   ├── Universal Auto Tuner
│   ├── Adaptive Guardian
│   └── Adaptive Performance Governor
│
├── Workload Layer
│   ├── Game Discovery / Local Catalog
│   ├── Generic Game Adapter
│   └── Specialized adapters
│       └── Free Fire / BlueStacks
│
└── Safety / Recovery
    ├── Snapshot / Transaction / rollback
    ├── Last Known Good
    └── crash/reboot/instability recovery
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

Stable workload identity remains separate from runtime evidence. Exact process targeting uses bound `RunningProcess` evidence; `KnownExecutable` never authorizes live capture. Explicit selected-workload routing blocks on unavailable/ambiguous selection rather than silently choosing another workload. No universal selection preserves the legacy Guardian/BlueStacks compatibility route.

Track 4 closing application SHA: `71991379e01518adf2e1c539491a9c0339a56735`; Windows CI #993 / run `34407420906` SUCCESS.

## 22. Universal Auto Tuner search-space and adapter authority — Track 5 Slices 1–2 GREEN

Track 5 generalizes existing Auto Tuner/Profiles additively. Specialized FF/BlueStacks working code is not replaced merely to obtain universal type names.

A `UniversalTuningDimension` carries stable id, scope (`System`/`Workload`), explicit authority and exact candidate values. `UniversalTuningCandidate` intentionally carries no evidence/confidence/recommendation/winner/persistence flag. Planning is deterministic, bounded and side-effect-free. Windows System dimensions reuse Track 2 candidate plans only when exploration is allowed.

Optional adapter-owned workload tuning declarations are exposed only through the exact adapter resolved for stable `GameIdentity` and required reversible lifecycle. Generic/unregistered/no-provider/incomplete cases expose zero dimensions. The BlueStacks/FF adapter does not invent static option lists because its candidate set is machine/instance-dependent.

Slice 1: `797c8c7766adea3369948d9cb330bb7ba9a69d52`, Windows CI #1000 / run `34411645032` SUCCESS.

Slice 2: `8dac70fdb2c693533ae481aaadd846ab84fde228`, Windows CI #1007 / run `34416726382` SUCCESS.

## 23. Dynamic BlueStacks/FF universal candidate bridge — Track 5 Slice 3 GREEN

The existing BlueStacks/FF Auto Tuner remains the only dynamic specialized candidate generator.

```text
GameKind FF/FFMAX
→ stable legacy GameIdentity + exact adapter
→ AutoTunerEngine.GenerateCandidates(machine, instance, mode)
→ BlueStacksAutoTunerRuntime.BuildCandidatePlan(candidate, capturedSettings)
→ only applicable candidates survive
→ exact 1:1 UniversalTuningCandidate ↔ specialized TuningCandidate binding
```

The universal bridge may filter but never add/reorder/regenerate specialized candidates. Captured allow-listed instance state is required. Renderer remains correlation-only until a verified reversible renderer mutation path exists.

Exact binding proves only identity/correlation/applicability; it grants no evidence, validation, winner, recommendation or persistence authority.

Slice 3: `39246089fb28f510287e79639356a4e16d1b6b02`; Windows CI #1014 / run `34422254555` SUCCESS.

## 24. Evidence-backed universal winner/result projection — Track 5 Slice 4 GREEN

Slice 4 adds a pure/read-only projection over the existing specialized `TuningResult`.

Permanent contracts:

- `UniversalTuningEvidenceProjection` — exact existing `CandidateEvidence` + exact universal candidate;
- `UniversalTuningWinnerProjection` — exact existing `PerformanceProfile` + exact source evidence + exact universal candidate;
- `UniversalTuningResultProjection` — stable identity/adapter + exact existing specialized result;
- `BlueStacksUniversalTuningResultBridge` — correlation only.

`AutoTunerEngine.SelectWinners(...)` remains the winner authority. The bridge performs no measurement, scoring, validation, winner selection, persistence, mutation or discovery.

Cross-workload, blank/tampered adapter authority, missing/ambiguous evidence binding and missing/ambiguous winner provenance fail closed. Evidence level is preserved exactly. An Observed-only specialized result remains winnerless; universal candidate existence cannot invent or validate a winner.

Slice 4: `f24c8c25612182db3c12351185fa54be227a8252`; Windows CI #1016 / run `34425901211` SUCCESS.

## 25. Universal projection of already-authorized validated History evidence — Track 5 Slice 5 GREEN

Slice 5 extends universal correlation to the revalidation boundary while keeping all validation/profile authority specialized.

Canonical flow:

```text
existing PerformanceComparisonHistoryRecord
        ↓ FIRST GATE
CanOriginateProfile == true
        ↓
Candidate + Validation fully Measured
        ↓
Validation later than Candidate
        ↓
exact equivalent PerformanceConfigurationSnapshot
        ↓
valid equivalent Candidate/Validation UniversalContext
        ↓
exact GameId + AdapterId + legacy workload match
        ↓
exactly one Slice 3 specialized candidate binding
        ↓
read-only UniversalValidatedPerformanceProjection
```

Permanent contracts:

- `UniversalValidatedPerformanceProjection` — exact original specialized History record reference + exact Slice 3 `UniversalTuningCandidate` reference;
- `BlueStacksUniversalValidatedPerformanceBridge.TryProject(...)` — additive fail-closed correlator.

### Authority boundary

`PerformanceComparisonHistoryRecord.CanOriginateProfile` remains the first gate and is unchanged. Universal metadata can only narrow an already-authorized specialized record; it can never make that gate true.

`HistoryService.CompletePerformanceValidationAsync` continues to own `PendingValidation → later independent Measured validation → exact specialized configuration equivalence → Validated`. Slice 5 does not create a new validation path.

The projection rechecks Candidate + Validation measurement quality and later-capture ordering because these are required semantics for universal validated correlation, including adversarial manually-constructed records. It does so without editing the legacy property or History service.

Candidate and Validation must both have valid `PerformanceUniversalConfigurationContext` values and those contexts must be exactly equivalent. The universal context must match stable candidate-space `GameId` and exact adapter authority. Specialized configuration Game must match the candidate-space legacy workload.

The specialized validated candidate must match exactly one Slice 3 binding by candidate-controlled CPU/RAM/FPS/renderer/resolution. Full specialized Candidate↔Validation equivalence still owns BlueStacks instance, DPI and structural environment correlation.

### Legacy rule

A legacy validated History record with no `UniversalContext` may remain fully valid through the existing specialized BlueStacks profile-origin path. `TryProject(...)` returns `null`; it never fabricates historical universal context and never invalidates the specialized record.

### No new authority

Slice 5 creates no new `Validated`, winner, profile, recommendation, persistence or mutation state. It does not change `PerformanceProfile`, `ProfileService`, `HistoryService`, Profile Challenge, typed measurement, lease, rollback or startup behavior.

### Verification

Temporary verifier: `ci/track5-universal-validation-projection-verify`.

- RED SHA `37774d0685403104bf4ace7e12e903217ee1098a`, verifier #3 / run `34433104755`: Core failed only because `BlueStacksUniversalValidatedPerformanceBridge` did not exist;
- GREEN SHA `2edf4e18acce9dcc3847d0367922f3ba64afcf1c`, verifier #4 / run `34433257697`: Core + App self-tests + WPF build SUCCESS;
- selective official integration excluded the temporary verifier workflow;
- application SHA `f5e57ce4cb61c01132d5ecb5f4d67d2433533fc4`;
- Windows CI #1018 / run `34433407760` SUCCESS;
- checkpoint `docs/project-memory/checkpoints/2026-09-10-track5-universal-validated-performance-projection.complete`.

Track 5 later slices extend the same authority model through real Custom profile origin, promotion, restart-safe current provenance, AppServices composition and Profiles presentation. Exact evidence for Slices 6–13 is recorded in their checkpoints and the Track 5 closure checkpoint.

## 26. Canonical track state

- Track 0 Foundation Hardening — GREEN
- Track 1 Universal Diagnostic Foundation — GREEN
- Track 2 System Optimizer — GREEN through current branch
- Track 3 Game Discovery + Adapter Framework — GREEN
- Track 4 Universal Telemetry / Evidence — GREEN for current canonical scope
- Track 5 Universal Auto Tuner + Profiles — **GREEN for current canonical scope**; Slices 1–13 verified through application SHA `a0c9a4e30dc48cb1730cee8b6b951f7338680cd1`, Windows CI #1034 / run `34502895182` SUCCESS
- Track 6 Adaptive Guardian 2.0 — planned; next design boundary
- Track 7 Hardware Performance Engine — planned
- Track 8 Deep Cleaner — planned
- Track 9 Auto Optimize — planned
- Track 10 DG UX Migration — planned

The historical larger architecture extends beyond Track 10, but exact raw Track 11–19 numbering is not currently authoritative and must not be invented. Preserve approved future domains recorded in `ROADMAP.md` until the original source is recovered.

## 27. Track 5 closure boundary — 2026-09-10

Track 5 is closed GREEN for the approved **additive universal foundation** scope. It does not claim a generic physical tuning runtime for every discovered game. Unsupported adapters remain capability-honest and receive no fabricated search dimensions, mutation authority, evidence, validation or profile authority.

At closure the permanent chain covers universal search-space contracts, system and adapter-owned workload dimensions, exact BlueStacks/FF candidate correlation, result/winner correlation, validated-History correlation, real Custom Validated profile provenance, already-authorized promotion provenance, restart-safe persisted Custom and promoted-winner reproving, explicit AppServices seams and presentation-only Profiles UI.

Universal layers can only narrow/correlate already-authorized specialized state. `AutoTunerEngine`, typed measurement, `HistoryService`, `ProfileService`, `ProfileChallengeService`, freshness/fingerprint gates, Global Controlled Benchmark Lease, Guardian coordination, rollback and History retain their existing authority.

Deferred non-blocking expansion includes new game-specific physical tuning adapters/runtimes, renderer/graphics mutation only after verified reversible lifecycle support, future non-BlueStacks persisted provenance once such adapters own real measurement/mutation/profile authority, and optional provenance UI batching/polish. These items do not reopen Track 5 by default.

Closure checkpoint: `docs/project-memory/checkpoints/2026-09-10-track5-universal-auto-tuner-profiles.complete`.
