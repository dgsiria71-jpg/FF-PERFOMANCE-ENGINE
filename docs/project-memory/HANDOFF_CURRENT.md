# Current Handoff — 2026-09-09

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR: `#1` — open/draft to `main`
- Product direction: DG Performance Engine, evolved from the existing FF Performance Engine without rewrite or mass rename.

## Current exact verified application checkpoint

- Application HEAD: `8595e03f7c0dc0f63e9caad42e9b01dcdfa5a9d7`
- Commit: `feat: compose universal context in performance sessions`
- Windows CI: **#989 — SUCCESS**
- CI run id: `34371201511`

The exact #989 job passed native configure/build/tests, managed build, all Core self-tests, `win-x64` publish, artifact upload and post-job cleanup.

Repository-native continuity remains authoritative: current branch code/tests + fresh exact-commit Windows CI outrank stale handoffs or chat reconstruction.

## Track state

- Track 0 — Foundation Hardening: GREEN
- Track 1 — Universal Diagnostic Foundation: GREEN
- Track 2 — System Optimizer / evidence authority: GREEN through current branch
- Track 3 — Game Discovery + Adapter Framework: GREEN
- Track 4 — Universal Telemetry / Evidence: ACTIVE, near closure of the current canonical scope
- Track 5+ — planned; do not skip the remaining Track 4 application-composition boundary.

## Track 4 verified state

Current code includes:

- typed metric schema v2 with explicit unit/domain/aggregation/quality/coverage/source/origin;
- conservative legacy bridge; absence represents unavailable values;
- unambiguous workload target resolver from Track 3 bound RunningProcess evidence;
- direct native CPU + physical-memory v2 telemetry;
- direct PresentMon v2 telemetry plus exact accepted-frame count;
- bounded realtime ring buffer and deterministic 1-second/10-second aggregation;
- processor-power CPU clock/current/max/limit telemetry;
- WDDM physical-GPU utilization with fail-closed parsing;
- typed fail-closed `UniversalBottleneckAnalyzer` v2;
- typed `UniversalDiagnosticService`;
- Performance capture/A-B typed metric evidence with History compatibility;
- Guardian-bound Windows controlled benchmark typed evidence;
- Auto Tuner and physical Profile Challenge typed PresentMon benchmark authority;
- additive `PerformanceUniversalConfigurationContext` bound to stable Track 3 GameId + resolved adapter + machine fingerprint + explicitly proven capability values;
- `PerformanceEvidenceSnapshot.UniversalContext` History round-trip without upgrading old records;
- `PerformanceComparisonSession` can now compose legacy BlueStacks configuration and universal workload context together, legacy-only, universal-only, or neither.

## Most recent verified checkpoints

- `32e46b71d48ffcdb0550351896c6c46e1a54e42e` — integrated typed diagnostics/benchmark pipeline — Windows CI #983 SUCCESS.
- `eb6855a38a0a838af9c5f529831f520803750a2f` — exact accepted-frame count — Windows CI #984 SUCCESS.
- `1d4cb81c514dd8848754526a6b8c5a51b081a637` — Auto Tuner typed authority — Windows CI #985 SUCCESS.
- `db39145d35bd83370b2d39ad3ffe239d4e9ffdf6` — Profile Challenge typed authority — Windows CI #986 SUCCESS.
- `4a9b12412d38a7ff0d355a74c890744290322b5a` — additive universal A/B workload/configuration context — Windows CI #988 SUCCESS.
- `8595e03f7c0dc0f63e9caad42e9b01dcdfa5a9d7` — universal+legacy context composition in `PerformanceComparisonSession` — Windows CI #989 SUCCESS.

## Universal configuration invariants now proven

- Stable workload identity comes from Track 3 `GameId`, never PID/path/display name.
- PID and executable path remain transient runtime evidence and are absent from the persisted universal context contract.
- `AdapterId` comes from the resolved catalog adapter, not from guessed metadata.
- Adapter version, workload configuration and display/driver context stay absent until directly proven.
- Capability values are included only when explicitly requested, uniquely resolved, `Available`, and carrying a current value.
- Existing `PerformanceConfigurationSnapshot` remains unchanged as the exact BlueStacks profile/freshness authority.
- Old History without `UniversalContext` rehydrates without artificial upgrade.
- Universal-only measured evidence does not bypass `Observed/PendingValidation/Validated` or `CanOriginateProfile` gates.
- Combined legacy + universal context survives snapshot rehydration.
- Existing one-provider `PerformanceComparisonSession(() => legacy)` remains source-compatible.

## Benchmark-authority audit

The earlier Track 4 verifier audit remains valid:

- zero production references to `PresentMonFrameCount`;
- active Auto Tuner and Profile Challenge code no longer calls legacy `CaptureBenchmarkAsync`;
- remaining production `CaptureBenchmarkAsync` methods are compatibility surfaces only;
- `PresentMon · N frames` strings are legacy output/compatibility disclosure, not typed benchmark authority;
- legacy History rehydration remains intentionally supported;
- Observed/PendingValidation/Validated gates remain unchanged.

Temporary verifier workflows/branches are never integration authority and must not be merged into `build/initial-product`.

## Authority invariants

- `Observed != Validated` remains unchanged.
- Current exact machine/environment fingerprint and freshness gates remain required.
- Global Controlled Benchmark Lease semantics remain unchanged.
- Missing telemetry is Unknown/absent, never implicit headroom or zero.
- Coverage is completeness, never probability/confidence.
- `TelemetrySample` remains a compatibility model, not new typed authority.
- No startup game/process discovery is added.
- No raw v2 frame disk persistence is introduced by Track 4 migration.
- No anti-cheat/integrity bypass.

## Current remaining Track 4 boundary

The Core can now represent and compose universal A/B context, but the real WPF application still constructs:

`PerformanceComparison = new PerformanceComparisonSession(CapturePerformanceConfiguration);`

`CapturePerformanceConfiguration()` is intentionally BlueStacks/Guardian-specific. Therefore normal `PerformancePage` baseline/candidate captures still receive only the legacy BlueStacks provider even though the Core session can accept a universal provider.

There is not yet an approved application-level owner for a selected stable generic `GameId` + corresponding `ResolvedGameCatalogResult`. Track 3 discovery is explicit/on-demand, so this must not be solved by silently running discovery at startup or by deriving GameId from PID/path.

## Exact next action

Continue Track 4 with an isolated TDD slice for **explicit application workload-context composition**:

1. RED: define the smallest application-facing context provider/state seam that can supply a proven stable GameId/catalog result to `PerformanceComparisonSession`.
2. Preserve `CapturePerformanceConfiguration()` and current BlueStacks behavior unchanged.
3. Do not trigger game discovery implicitly at startup or on ordinary telemetry ticks.
4. Do not infer GameId from PID/path/process/display name.
5. When no explicit proven universal workload context exists, return `null` and preserve legacy-only captures.
6. When a proven context exists, attach universal context additively; BlueStacks may carry both contexts.
7. Re-prove History and profile authority remain unchanged.
8. Run clean Core/app verifier as appropriate, integrate selectively, and require fresh full Windows CI on the exact official SHA.

After this boundary is GREEN, reassess Track 4 closure versus additional proven hardware channels (VRAM/thermals/I/O/network) one real provider at a time; do not invent sensors merely to satisfy roadmap labels.
