# Current Handoff — 2026-09-09

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR: `#1` — open/draft to `main`
- Product direction: DG Performance Engine, evolved from the existing FF Performance Engine without rewrite or mass rename.

## Current exact verified application checkpoint

- Application HEAD: `21eb0d9ed7cd5c181fc609fca02f89f37883d59c`
- Commit: `feat: wire explicit universal workload context through AppServices`
- Windows CI: **#991 — SUCCESS**
- CI run id: `34380228966`

The exact #991 job passed native configure/build/tests, managed build, the full Core self-test suite, the new permanent WPF `App.SelfTest`, `win-x64` publish, artifact upload and post-job cleanup.

Repository-native continuity remains authoritative: current branch code/tests + fresh exact-commit Windows CI outrank stale handoffs or chat reconstruction.

## Track state

- Track 0 — Foundation Hardening: GREEN
- Track 1 — Universal Diagnostic Foundation: GREEN
- Track 2 — System Optimizer / evidence authority: GREEN through current branch
- Track 3 — Game Discovery + Adapter Framework: GREEN
- Track 4 — Universal Telemetry / Evidence: ACTIVE, near closure of the current canonical scope
- Track 5+ — planned; do not skip the remaining proven Track 4 universal capture-target boundary.

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
- typed fail-closed `UniversalBottleneckAnalyzer` v2 and typed `UniversalDiagnosticService`;
- Performance capture/A-B typed metric evidence with History compatibility;
- Guardian-bound Windows controlled benchmark typed evidence;
- Auto Tuner and physical Profile Challenge typed PresentMon benchmark authority;
- additive `PerformanceUniversalConfigurationContext` bound to stable Track 3 GameId + resolved adapter + machine fingerprint + explicitly proven capability values;
- `PerformanceEvidenceSnapshot.UniversalContext` History round-trip without upgrading old records;
- `PerformanceComparisonSession` composes legacy-only, universal-only, combined or context-free snapshots;
- explicit `PerformanceWorkloadContextSelection` owned by the application boundary;
- `AppServices` exposes explicit select/clear operations and supplies the universal provider to normal `PerformanceComparison` captures;
- permanent `FFPerformanceEngine.App.SelfTest` is executed by Windows CI.

## Most recent verified checkpoints

- `db39145d35bd83370b2d39ad3ffe239d4e9ffdf6` — Profile Challenge typed authority — Windows CI #986 SUCCESS.
- `4a9b12412d38a7ff0d355a74c890744290322b5a` — additive universal A/B workload/configuration context — Windows CI #988 SUCCESS.
- `8595e03f7c0dc0f63e9caad42e9b01dcdfa5a9d7` — universal+legacy context composition in `PerformanceComparisonSession` — Windows CI #989 SUCCESS.
- `bd22ae0a8ca93b57289d0786532f1b6eb5d5ffb0` — memory checkpoint after universal session composition — Windows CI #990 SUCCESS.
- `21eb0d9ed7cd5c181fc609fca02f89f37883d59c` — explicit application workload context + cross-workload guard + permanent App self-test — Windows CI #991 SUCCESS.

## Application workload-context invariants now proven

- App construction starts with no selected universal workload; no game discovery/selection is performed implicitly.
- Selection accepts only an already-resolved `ResolvedGameCatalogResult` plus an explicitly requested stable Track 3 `GameId`.
- Requested GameId is normalized and must resolve to exactly one catalog entry.
- Adapter authority comes from the resolved adapter, never untrusted `GameIdentity.AdapterId` metadata.
- Unknown or ambiguous selection fails closed and clears prior selection so stale identity cannot leak into later A/B evidence.
- Capability values remain opt-in and must still satisfy the universal-context availability/current-value gates.
- `CapturePerformanceConfiguration()` remains the unchanged BlueStacks/Guardian legacy provider.
- `PerformanceComparison` receives universal context only from the explicit application selection provider.
- A frozen evidence snapshot cannot represent two different workloads: legacy FF/FFMAX configuration plus a universal context for another GameId causes the incompatible universal context to be omitted fail-closed.
- Matching BlueStacks legacy + matching universal GameId may coexist additively.
- Universal-only context still does not bypass `Observed/PendingValidation/Validated` or `CanOriginateProfile`.

## TDD evidence for the application-composition slice

Temporary verifier branch `ci/track4-remaining-verify` was used only as an isolated proving ground and was not merged wholesale.

- verifier #14: intended RED — `PerformanceWorkloadContextSelection` absent (`CS0246`).
- verifier #15: first GREEN attempt exposed nullable-boundary compile error (`CS8601`); fixed rather than suppressed.
- verifier #16: second intended RED reached semantic cross-workload contamination guard (FFMAX legacy + Steam universal).
- verifier #17: Core + WPF build GREEN after selector + central cross-workload guard.
- verifier #18: app-level intended RED — `AppServices` lacked selection/wiring APIs.
- verifier #19: Core + real `App.SelfTest` + WPF build GREEN.
- selective official integration then passed Windows CI #991 on the exact application SHA.

Temporary verifier workflows/branches remain non-authoritative and must not be merged as infrastructure.

## Authority invariants

- `Observed != Validated` remains unchanged.
- Current exact machine/environment fingerprint and freshness gates remain required.
- Global Controlled Benchmark Lease semantics remain unchanged.
- Missing telemetry is Unknown/absent, never implicit headroom or zero.
- Coverage is completeness, never probability/confidence.
- Stable workload identity never comes from PID/path/process/display name.
- `TelemetrySample` remains a compatibility model, not new typed authority.
- No startup game/process discovery is added.
- No raw v2 frame disk persistence is introduced by Track 4 migration.
- No anti-cheat/integrity bypass.

## Current remaining Track 4 boundary

The application can now persist a proven universal workload context, but **Performance capture targeting remains BlueStacks-only**.

`PerformanceCaptureCoordinator.CaptureTypedAsync(...)` still accepts `GuardianLiveSessionStatus`, calls `PerformanceCaptureTargetPolicy.FromGuardianStatus(...)`, and refuses capture without an exact Guardian-bound BlueStacks PID. Meanwhile Track 4 already has `TelemetryWorkloadTargetResolver`, which can fail-closed resolve an exact running PID from a proven stable GameId plus bound `RunningProcess` evidence.

The explicit application selector currently stores only the stable selected catalog entry needed for persistence; it does not yet expose the bound runtime evidence required for universal process capture.

## Exact next action

Continue Track 4 with an isolated TDD slice for **Universal Performance Capture Targeting**:

1. RED: an explicitly selected stable GameId with exactly one valid bound RunningProcess must resolve to an exact process capture target using the existing `TelemetryWorkloadTargetResolver` semantics.
2. Preserve the existing Guardian/BlueStacks `CaptureTypedAsync` overload and behavior unchanged/source-compatible.
3. Add no fuzzy process matching and no GameId inference from PID/path/name.
4. KnownExecutable/App Paths evidence must never create a live capture target.
5. Unknown, unavailable or ambiguous selected workload must fail closed without invoking PresentMon.
6. Exact target may invoke the existing typed PresentMon-by-PID capture and append typed timeline evidence.
7. Keep discovery explicit/on-demand; no startup discovery or polling side effect.
8. Add application wiring only after Core target/capture behavior is GREEN.
9. Run isolated verifier, integrate selectively, then require fresh full Windows CI on the exact official SHA.

After this boundary is GREEN, reassess Track 4 closure before choosing any additional hardware sensor slice. VRAM/thermals/I/O/network are candidates only when a real supported provider and a concrete consumer are both proven.
