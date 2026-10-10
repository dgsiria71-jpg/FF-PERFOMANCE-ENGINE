# Track 6 item 3 — Exact Guardian action-to-Windows-mutation authority

Date: 2026-09-19. Base documentary SHA `0bdf7e0d3bf4325bb7713e89e5de012d08030dc8`, Windows CI #1063 SUCCESS. Follow the 2026-09-06 unified architecture, recovered master, HANDOFF_CURRENT and Slice4 checkpoint.

## Problem and bounded decision

The pre-Slice4 regression checks actual Windows capability availability/scope/LiveSafe safety, but the executor still accepted caller-selected capability and target independent from the eligible `Action.Id`. A different *also LiveSafe* capability reached typed capture. Do not infer that declaring an action LiveSafe authorizes arbitrary Windows changes.

Introduce an explicit, immutable in-memory `GenericGuardianSessionMutationCatalog` keyed by stable GameId + exact anomaly family + Action.Id. One entry binds exactly one `WindowsMutationRequest` including capability, target value and optional expected-current-state precondition. Reject duplicate identity entries, missing identity/capability/target, unknown keys, cross-game/family/action substitutions and any changed mutation field. Only capability IDs use case-insensitive identity; target and expected-state compare ordinal exactly. `TryBind` may create a binding for a known candidate, but execution independently calls `IsAuthorized` before capture. The executor's constructor requires the catalog; there is no permissive legacy constructor or automatic mapping. It also independently retains exact eligibility and actual capability LiveSafe checks. Snapshot/apply/verify/rollback remain Track2 responsibility.

Configuration provenance must be supplied and audited by the *future trusted owning host*, never inferred from a caller's metadata or auto-generated exploration list. This slice registers **zero production mappings** and does not enable automatic generic Guardian execution. The catalog is a narrow authorization equality contract, not proof that a registered policy is beneficial or that evidence is comparable.

## Verification and integration

RED: verifier branch `ci/track6-action-mutation-authority-verify`, SHA `e413ebc54c55e97aba54c8e0ba8f2a206bfd5df4`, Windows run `35427880650`, job `105856904400`. Native tests and .NET compilation passed with zero warnings/errors. Core test failed at `GenericGuardianLiveSafeCapabilitySelfTests.cs:81`: unrelated LiveSafe `test.other-live` reached typed capture (`captures=1`). No actual Windows mutation was performed in this fake-capture regression.

GREEN verifier: `692e9cec1dba5dec4ccfa542638a3f28cbaa24d0`, run `35428050561`, job `105857390668`; native configure/build/test, managed build, Core tests, App tests, Windows publish all SUCCESS. Existing reversible-canary regression coverage preserved under explicit fixture mapping. New adversarial checks reject other LiveSafe/unsafe/unknown capability, changed target and altered expected-state precondition before capture.

Official application: `4a3d27eb68f20220f304dba1ba675ffac76deae7`, Windows CI #1064 / run `35428164066`, job `105857672380` SUCCESS for all steps including artifact upload. Artifact `FFPerformanceEngine-win-x64`, ID `10579378430`, digest `sha256:a2e0baa766a4bb50e1709222302e025353cdc6a07b374956e3fddb5a8b5b8a8b`. Integrated exactly new catalog, changed executor and the two affected selftest files; temporary verifier workflow excluded. `main` unchanged.

## Remaining boundaries

Next independently prove comparable uncontaminated typed before/after intervals (time ordering, exact workload/scene/load context, benchmark suspension and drift). Current same PID/GameId and typed FPS quality do not prove scene equivalence or causal gain. Only after that: trusted adapter-owned production mapping and owner-managed host composition of lifecycle epoch, Action Budget, transaction lease cleanup, global controlled benchmark suspension/reconciliation and no startup discovery. Do not call item 3 or Guardian host complete. `Observed != Validated`, no profile or recommendation promotion.
