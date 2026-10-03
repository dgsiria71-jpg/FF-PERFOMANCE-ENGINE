# Track6 item3 — conservative typed canary comparison policy, scope 1 of comparison work

Date 2026-09-19. Base documentary `4145c6ce5ebf0fea778c1ed9754082c4cdc21de9` CI #1065 SUCCESS. Respect unified 2026-09-06 architecture and canonical handoff; no rewrite.

## Verified absence and bounded mission

`TelemetryFrame` currently contains Timestamp, metrics and quality; `PerformanceCaptureCoordinator` returns typed frame and exact workload target but NO scene/mode/load fingerprint, capture interval envelope, process-lifecycle epoch or controlled benchmark contamination attestation. Existing executor checks exact GameId/PID/path and typed metrics but does not independently prove scene equivalence or capture/mutation ordering. It would be incorrect to interpret FPS gain alone as causal under changing workload.

Create a pure, read-only `GenericGuardianCanaryComparabilityPolicy` with explicit before/after `GenericGuardianCanaryComparisonWindow` input: stable GameId, exact PID/path, same non-empty session epoch; non-empty source/mode/scene/load/environment fingerprints; positive timestamp envelopes containing each frame.Timestamp; before interval completed before transaction begins and after interval begins after transaction completion; explicit controlled benchmark, drift, other mutation statuses all known false in both intervals. Reject unknown or true interference, changed context, missing values, empty/Unavailable frame, invalid or overlapping interval, different lifecycle/target. Do not invent a tolerance or universal scene similarity score. Outcome `InScopeOnSuppliedEvidence` indicates ONLY structural consistency of asserted metadata and is never standalone approval for mutation, `KEEP`, History/profile promotion, or causal performance improvement. `Inconclusive` is default. A caller could fabricate context strings or false contamination flags; proving their source is a *future* trusted adapter/host task, not accomplished here.

## TDD evidence

Verifier `ci/track6-canary-comparability-verify`. RED SHA `fc2e1ba8e01634862fda98a3c648fb37ed4bb4db` run `35428624500`, job `105858954583`: native configure/build/test passed; managed compile failed exactly one missing `GenericGuardianCanaryComparisonWindow` CS0246 with zero warnings; all other steps skipped as expected. Tests were registered in Core SelfTest Program before code.

GREEN verifier SHA `3395a90dc0c6e663842195e7f3dec95bae3675f2`, run `35428706183`, job `105859185411`: native, managed 0 errors/warnings, Core all tests including `PASS Track 6 Guardian comparability policy: exact lifecycle, scene/load/mode, temporal ordering and contamination fail closed`, App selftests and win-x64 publish SUCCESS. Tests exercise matching window, mismatched session/game/PID/path/source/scene/mode/load/environment, null/true contamination, missing identifiers, invalid frame or timestamp, overlap/reversed mutation boundary and missing evidence.

Integrate only one production policy, selftest and Program registration plus this plan into `build/initial-product`; exclude temporary workflow. Verify exact official Windows CI including artifact, then doc checkpoint exact CI.

## Explicit next stage

This policy is NOT currently wired into `GenericGuardianWindowsSessionCanaryExecutor` and no real scene/load/mode evidence source was discovered in generic typed capture. Before enabling host or KEEP, implement provenance-verified adapter-owned scene/load collection and trusted capture/transaction interval stamps, capture benchmark coordination from actual Track0 control rather than caller flags, then enforce mandatory before/after comparison in executor with conservative rollback tests. Only then runtime host with per-lifecycle budget and leases. Do NOT claim fully validated real-world comparability or automatic optimization on this policy alone.