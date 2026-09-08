# Current Handoff — 2026-09-08

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR: `#1` — open, draft, not merged
- PR base: `main`

### Last verified application-code checkpoint

- Application HEAD: `8392892e7ad658928e7b7aca1719df2b64399125`
- Commit: `feat: resolve telemetry targets from bound game evidence`
- Windows CI: **#888 — SUCCESS**
- CI run id: `34276000513`

The #888 job passed native configure, C++ build, native tests, managed/WPF build, Core self-tests, `win-x64` publish, artifact upload and complete-job finalization.

### Repository-native memory

The continuity system lives in root `AGENTS.md` and `docs/project-memory/`. Git + code/tests + fresh exact-commit Windows CI remain authoritative if this handoff becomes stale.

## Track transition

**Track 3 — Game Discovery + Adapter Framework — GREEN.**

**Track 4 — Universal Telemetry / Evidence — ACTIVE.**

Track 3 original exit criteria are now satisfied and are no longer a blocker for universal telemetry:

- stable launcher-native `GameIdentity` / local catalog ✅
- generic adapter + specialized Free Fire / Free Fire MAX BlueStacks adapters ✅
- BlueStacks installed package discovery ✅
- Steam ✅
- Epic ✅
- Riot ✅
- Battle.net ✅
- EA App ✅
- Ubisoft Connect ✅
- Microsoft Store / Xbox GDK ✅
- explicit two-plane identity/evidence architecture ✅
- running-process evidence ✅
- known-executable Windows App Paths evidence ✅

Additional discovery surfaces remain optional future enrichment only when they add a proven signal. They are not justification to delay Track 4 or to weaken stable identity rules.

## Track 3 identity/evidence architecture — preserved

```text
Identity Sources
      ↓
LocalGameCatalogService
      ↓
stable GameIdentity list
      ↓
GameAdapterResolver

Evidence Sources
      ↓
GameEvidenceCatalogService
      ↓
GameEvidenceBinder
      ├── BoundGameEvidence
      └── UnboundGameEvidence
```

Current evidence plane:

```text
GameEvidenceCatalog
├── Windows running processes       priority 40
└── Windows App Paths executables   priority 30
        ↓
GameEvidenceBinder
├── BoundGameEvidence
└── UnboundGameEvidence
```

Neither evidence source may manufacture a durable identity. Display names, executable names, install folders, PIDs and paths remain evidence rather than cross-launcher identity keys.

`AppServices.InitializeAsync()` still performs no game/package/process/App-Paths discovery. Explicit authority remains `AppServices.DiscoverGamesAsync()`.

## Track 4 foundation now implemented

Canonical design:

- `docs/superpowers/specs/2026-09-08-universal-telemetry-evidence-design.md`
- `docs/superpowers/plans/2026-09-08-universal-telemetry-foundation.md`

New Core namespace:

```text
FFPerformanceEngine.Core.Telemetry
├── TelemetryMetricSchema.cs
├── TelemetryFrame.cs
├── TelemetryLegacyBridge.cs
└── TelemetryWorkloadTargetResolver.cs
```

### Metric schema v2

The additive v2 schema exists alongside legacy `TelemetrySample`.

Each numeric observation carries:

- stable canonical metric descriptor/id;
- finite numeric value;
- typed `Partial` or `Measured` quality;
- normalized `[0,1]` coverage;
- normalized source/provenance id;
- explicit `Direct`, `Derived` or `Legacy` origin.

`Unavailable` is represented by absence / lookup / frame state and cannot be attached to a stored numeric observation.

The first standard catalog exposes the 17 approved metrics across frame, system, thermal and network domains. `TelemetryFrame` defensively copies observations, rejects duplicate metric ids, sorts deterministically, provides normalized lookup and computes summary quality without upgrading any individual metric.

### Conservative legacy bridge

`TelemetryLegacyBridge.FromLegacy(TelemetrySample)` maps all 17 existing nullable fields explicitly. `TelemetrySample` itself remains unchanged and source-compatible.

Authority rules:

- `PresentMon · ...` proves only finite **Frame-domain** metrics as `Measured / presentmon / Direct`;
- `System` proves only CPU utilization + physical memory used/total as `Measured / native-system / Direct`;
- `Frame+System` preserves CPU/memory native authority but does not promote the supplied FPS argument;
- historical exact `Measured` preserves Frame metrics as `Measured / legacy-measured / Legacy` for compatibility;
- unknown labels and unrelated populated fields stay `Partial / legacy-bridge / Legacy`;
- null, NaN and infinity produce no metric and never become zero.

### Universal workload target resolver

`TelemetryWorkloadTargetResolver` consumes only the already-resolved Track 3 `ResolvedGameCatalogResult` and bound evidence. It performs no platform I/O.

Rules:

1. blank/unknown/non-unique requested identity → `UnknownGame`, no promoted `GameId`, PID or path;
2. exactly one stable catalog identity is required before runtime evidence is considered;
3. only `BoundGameEvidence` with `GameEvidenceKind.RunningProcess` is eligible;
4. PID must be positive and executable path fully-qualified/normalizable;
5. zero valid PID groups → `UnavailableRunningProcess` while preserving the proven stable `GameId`;
6. multiple PIDs → `AmbiguousRunningProcess`, no chosen PID/path;
7. one PID associated with conflicting paths → ambiguous;
8. duplicate same-PID/same-path evidence, including case variants, remains exact;
9. exactly one PID with one normalized path → `ExactRunningProcess`;
10. `KnownExecutable` / App Paths evidence can never yield a live PID.

No `Process.GetProcesses`, `File.Exists`, Guardian dependency, working-set ranking, process-name guessing or newest/highest-PID heuristic exists in the resolver.

## Track 4 TDD provenance

### Task 1 — metric schema v2 + immutable frame

- RED `86953dca06fd278383d35f5a9202371dfe1a0410` → Windows CI #878 / run `34273684420`; native remained GREEN and managed failed only because `FFPerformanceEngine.Core.Telemetry` contracts did not yet exist.
- GREEN `4ced969a17b41e4cad56c9e413c626d9cd2326d6` → Windows CI #880 / run `34275125229` SUCCESS.

### Task 2 — conservative legacy bridge

- RED `9a48b5c7719d4f131819fb0e7b0aecb0add37be9` → Windows CI #882 / run `34275316122`; 0 warnings and failures only from missing `TelemetryLegacyBridge`.
- GREEN `476df79441e0c8770f23f260a2824908595d461a` → Windows CI #884 / run `34275483157` SUCCESS.

### Task 3 — universal workload target resolver

- RED `27271db72d5d7a99ad5b9b35ac203fd89ef4a6cd` → Windows CI #886 / run `34275838695`; native remained GREEN and managed failed only on missing target/resolver contracts.
- GREEN `8392892e7ad658928e7b7aca1719df2b64399125` → Windows CI #888 / run `34276000513` SUCCESS.

## Invariants that are now explicit

- legacy `TelemetrySample` is preserved;
- v2 quality/provenance is per metric, not a free-form sample-wide authority;
- unavailable metrics are absent, not fabricated zero observations;
- a finite value alone is insufficient to become `Measured`;
- unknown GameId input is never echoed as proven identity;
- only unambiguous bound RunningProcess evidence yields a process target;
- App Paths / KnownExecutable never yields a live PID;
- runtime PID/path never replaces durable `GameId`;
- existing Performance/A-B `Observed != Validated`, freshness, fingerprint and recommendation-authority gates are untouched;
- Global Controlled Benchmark Lease semantics are unchanged;
- no game/process discovery was added to application startup.

## Exact next action

Continue **Track 4**, not Track 3 discovery expansion.

Next implementation plan/slice should migrate the **existing collectors** additively into v2 while preserving old APIs:

1. native system telemetry adapter/path:
   - existing CPU observation → `system.cpu.utilization.percent`;
   - existing physical memory used/total → their standard v2 metrics;
   - first CPU sample may be unavailable because a delta baseline is required;
   - no GPU/thermal values are invented.
2. PresentMon v2 adapter/path:
   - direct measured frame metrics with source `presentmon`;
   - explicit coverage based on accepted frame rows/window quality;
   - retain `ParseCsv()` / legacy `TelemetrySample` output until current consumers migrate.
3. after collector adapters are GREEN, implement bounded realtime v2 ring buffer + quality-aware aggregation.
4. only then add new hardware channels one provider at a time and later migrate A/B away from free-form `DataQuality` parsing.

## Do not regress

- Do not replace or mass-edit `TelemetrySample` yet.
- Do not create a second FPS or hardware telemetry engine.
- Do not infer GPU/temperature/clock data when a real collector has not proven it.
- Do not let `FrameQuality` override per-metric quality.
- Do not use weak evidence to manufacture `GameId`.
- Do not pick a PID from an ambiguous process set.
- Do not reimplement Optimize authority or weaken ValidatedEvidence/freshness/fingerprint gates.
- Do not scan launchers, Windows packages, processes or App Paths during `InitializeAsync()`.
- Do not expand discovery merely to increase launcher count.
