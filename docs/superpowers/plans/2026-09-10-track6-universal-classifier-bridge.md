# Track 6 Universal Classifier Bridge Implementation Plan

> Required workflow: exact current memory/context -> bounded Slice -> TDD RED -> observe exact RED -> minimal production -> verifier GREEN -> selective official integration -> exact Windows CI -> project-memory checkpoint -> documentary CI.

**Goal:** Begin approved Track 6 item 2 without creating a competing bottleneck engine. Add the smallest read-only Guardian classifier seam that consumes the already-exact generic workload observation from Track 6 item 1 and delegates causal bottleneck analysis to the existing typed `UniversalBottleneckAnalyzer`.

**Architecture:** `GenericGuardianBottleneckClassifier` is a Guardian policy boundary, not a new analyzer. It accepts `GenericGuardianWorkloadObservation` + caller-provided `BottleneckAnalysisContext`. Only an `Active` generic workload with one exact capturable target and an exact-target typed frame is eligible for causal analysis. Eligible observations are delegated unchanged to `UniversalBottleneckAnalyzer.Analyze(TelemetryFrame, BottleneckAnalysisContext)`. Ineligible/missing/unproven observations return explicit `BottleneckKind.Unknown` with zero confidence and no fabricated candidate. The result retains the exact observation so stable workload identity/state remains attached without keying authority on PID/path.

**Existing authority reused:**

- Track 6.1 `GenericGuardianWorkloadObservationService` owns no classification but preserves exact-target typed frame + state/signals.
- Track 4 `UniversalBottleneckAnalyzer` already owns typed CPU/GPU/Memory/VRAM/Storage/Thermal/Power/Network/FramePacing causal analysis and its quality/coverage fail-closed rules.
- `BottleneckAnalysisContext` is caller-supplied evidence/context; this Slice does not manufacture target FPS, critical-thread CPU, pressure, thermal or power signals.

## Constraints

- No second bottleneck thresholds/heuristics are implemented in Guardian.
- No conversion from legacy `TelemetrySample` to typed evidence.
- Only `GuardianWorkloadState.Active` may enter causal classification in this first Slice.
- Observation target must still be `ExactRunningProcess` + `CanCaptureProcess`; manually forged `Active` without exact target fails closed.
- `Frame == null` fails closed to Unknown.
- The classifier does not infer or create expected FPS/baseline. Missing target/context remains whatever the existing analyzer can prove; the Guardian seam never fills defaults.
- Existing analyzer result is not upgraded: `Unknown` remains `Unknown`; candidate confidence/order/reasons remain analyzer-owned.
- Classification is passive observation only: it grants no `Validated`, profile, winner, recommendation, mutation, canary, LIVE_SAFE, cooldown, action-budget or persistence authority.
- Do not touch `GuardianEngine`, `GuardianSupervisor`, `GuardianCanaryService`, `GuardianKnowledgeService`, AppServices, startup or WPF in this Slice.
- Existing BlueStacks/FF Guardian remains unchanged.

## Task 1 — TDD RED

Create `tests/FFPerformanceEngine.Core.SelfTest/GenericGuardianBottleneckClassifierSelfTests.cs` and register it in Core self-tests.

Expected new production contracts:

- `GenericGuardianBottleneckClassification` containing exact source observation, `BottleneckAnalysisResult Analysis`, and explanatory `Reason`;
- `GenericGuardianBottleneckClassifier(UniversalBottleneckAnalyzer analyzer)`;
- `Classify(GenericGuardianWorkloadObservation observation, BottleneckAnalysisContext context)`.

RED must fail only because the classifier contracts do not yet exist.

Required tests:

1. Active + exact target + measured typed frame pressure + saturated GPU + explicit critical CPU headroom delegates to existing analyzer and returns `Gpu` with its candidates/signals/confidence semantics.
2. Active + exact target + measured typed frame pressure + saturated critical CPU + measured GPU headroom returns `Cpu` through the existing analyzer.
3. Missing required causal metric remains `Unknown`; Guardian does not treat absence as headroom.
4. Partial or insufficient-coverage typed causal metric remains `Unknown` exactly as Track 4 analyzer requires.
5. Existing analyzer `Unknown` result remains `Unknown`; classifier does not invent a fallback classification.
6. Non-Active states (`Starting`, `Ready`, `Ending`, `Desktop`, `Offline`, `Unresolved`) fail closed to Unknown without causal classification authority.
7. Manually forged `Active` observation with non-exact/uncapturable target fails closed.
8. Active exact target with null frame fails closed.
9. Source `GenericGuardianWorkloadObservation` reference is preserved exactly; classifier does not rewrite stable identity/state/runtime evidence.
10. Constructor/classification are read-only with respect to observation/frame/context.

## Task 2 — Minimal production

Create `src/FFPerformanceEngine.Core/Services/GenericGuardianBottleneckClassifier.cs`.

Implementation:

1. validate arguments;
2. gate on `State.State == Active`;
3. gate on exact `TelemetryWorkloadTarget` still capturable;
4. gate on non-null typed frame;
5. delegate directly to `_analyzer.Analyze(observation.Frame, context)`;
6. return exact observation reference + exact analyzer result and a neutral reason;
7. for any failed gate return a new explicit Unknown/0 result with no candidates/signals and a reason describing availability, never pretending a healthy state.

No mutation/persistence/side effects.

## Task 3 — Verify/integrate/checkpoint

- Require verifier Windows GREEN: native, managed, Core, App, publish.
- Selectively integrate only permanent plan/test/production files; exclude temporary verifier workflow.
- Require exact official Windows CI on the application SHA.
- Then synchronize `HANDOFF_CURRENT`, `IMPLEMENTATION_STATUS`, `ROADMAP` and add a Slice checkpoint. Update `CANONICAL_CONTEXT`/`DECISIONS_LOG` only if this Slice actually changes a closed authority decision; otherwise explicitly preserve them unchanged.
- Require exact documentary-head Windows CI before the next classifier Slice.
