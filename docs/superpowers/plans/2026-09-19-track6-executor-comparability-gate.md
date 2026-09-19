# Track 6 item 3 — Mandatory executor comparability gate

Date: 2026-09-19. Classification: bounded increment extending the already verified generic Guardian canary and structural comparability policy, not a new host or real scene sensor. Original application base `9f19d09a7bd200ab543b92749f02d6c6294d80d1` CI #1067. User requested continuation without restarting and no automatic generic Guardian activation.

## Intent and limits

Close the proven gap: existing executor's `Improved` could retain a reversible Windows session mutation based on before/after FPS without enforcing the newly built comparator. No Game Adapter currently authenticates scene/mode/load and TelemetryFrame does not carry them. Do not register fake scene identity, automatic action catalog, invented benchmark false flags, or modify startup; safe default must be no mutation.

## Implementation

1. TDD tests on isolated `ci/track6-executor-comparability-gate-verify`: no comparison provider ⇒ no typed capture or transaction; null/unknown/contaminated BEFORE ⇒ no mutation; changed scene, mode, load, source, environment, real session epoch, benchmark/drift/other mutation, frame/timestamp mismatch or missing AFTER ⇒ Track2 rollback and no evaluator/KEEP; genuinely comparable test-only positive path retains reversible lease and original rollback tests pass.
2. Introduce `IGenericGuardianCanaryEvidenceSource` interface strictly as a host-vetted seam. It receives actual exact target, frame and executor-owned capture timestamps. A source implementing it is not automatically authentic; test double is tests-only.
3. Extend executor preflight with nonnull provider and exact real session epoch/target. AFTER timing envelopes are measured around calls to the existing typed coordinator and Track2 BeginSessionAsync. A comparison window must reference the exact captured frame object, match target and epoch, reproduce exact capture interval and pass `GenericGuardianCanaryComparabilityPolicy.IsValidWindow`. BEFORE invalid is rejected before any mutation; AFTER invalid or `Evaluate != InScopeOnSuppliedEvidence` triggers `RestoreAsync(CancellationToken.None)` before returning Inconclusive, no kept lease or outcome evaluation.
4. Preserve existing verified apply/verify/rollback and exception aggregate semantics. An Improved outcome may remain active only as an explicitly owned reversible lease after structural comparison. Keep no generic host composition or automatic activation in this increment.
5. Update legacy mapping regression to verify exact catalog + real LiveSafe capability independently, but require no capture without context; never weaken executor to preserve outdated expectations. Run full Windows CI on verifier SHA and selective official branch SHA. Exclude the verifier-only workflow.

## Evidence and outcome

RED verifier `f86ae42d633a33aa239349bae10d22c9506d6c0d` run `35430191463`: expected missing-interface CS0246, 0 warnings; native PASS. Initial implementation `97c876bc0cf75d4bae02aa7cd63de5053431a8ad` run `35430314996`: native/managed PASS, new selftest PASS, legacy mapping test FAILED due expected capture without source. Final verifier `921b47f4d161116074031fbe8e806a15d6c6b391` run `35430452533`: full native/managed/Core/App/publish SUCCESS. Official `853ab298965d587abad71dc0d3af258f4ff9fc75` Windows CI #1068 run `35430572705`, job `105864269265` SUCCESS including artifact `FFPerformanceEngine-win-x64` ID `10580437246` digest sha256 `3de5362763133de86ad9d6c39231926273f3e0cccb0bc98acd63e3b126adb0e6`.

## Unfinished gates

This is **mandatory structural enforcement** only. Real adapter scene/mode/load/environment provenance over full windows remains absent. Track0 global lease active/overlap signal must be wired from owning authority, not caller-supplied false. No generic runtime host with admission budget, session lifecycle, retained lease cleanup or benchmark reconciliation exists yet; default executor declines and app unchanged. Next: real benchmark interference signal, genuine supported-adapter interval evidence, then owner-managed host under explicit enable and Windows/HIL verification. Unsupported/unknown remains Inconclusive; no causal FPS improvement or validated winner claimed.
