# DG Performance Engine — Implementation Status Ledger

This is a curated ledger of important verified milestones. Git history remains the complete commit ledger and current code/tests + fresh exact-commit Windows CI are authoritative.

## Baseline application foundation

The repository contains a functional Windows WPF + C++ product foundation: navigation/UI, BlueStacks discovery/configuration, PresentMon, profiles, Guardian, Auto Tuner, History/snapshots, Mini Mode themes and native interop.

## Performance A/B and profile evidence

Representative verified milestones:

- `8062c318...` — real Performance A/B presentation/service transformation.
- `c259c723c443a819bfae540017d0f76aecbceeef` — aggregates recomputed from frozen points; Windows CI run 268 / `34015937217` SUCCESS.
- `6e2e6a9667dc04f5a8f654a5cb0696fed6100b4f` — exact configuration/fingerprint attached to evidence and Validated profile origin; CI #316 SUCCESS.
- `225f2776d8709ebf51ab3e77b91550c4d89003d7` — challenge/freshness/drift block; CI #342 SUCCESS.
- later hardening added incumbent freshness, challenge progress and physical A/B rounds before DG expansion.

## Track 0 — Experimental Integrity — GREEN

- `985688276cd7937b74a870d61445fa239ac570ad`
- Global Controlled Benchmark Lease + Guardian suspension/reconciliation + concurrency hardening.
- Windows CI #402 SUCCESS.

## Track 1 — Universal Diagnostic Foundation — GREEN

- `f1c932b7ce7af8c61c424c3c619b66784917ee22`
- MachineContext v2, Hardware Discovery, Capability Registry/Graph, fingerprint v2, Universal Bottleneck Analyzer.
- Windows CI #426 / run `34075848480` SUCCESS.

## Track 2 — System Optimizer / evidence authority — GREEN through current branch

Representative checkpoints:

- `b21330602f2eeba1d04336da8df32a3a73daf6fa` — hardened transaction engine, CI #456.
- `1b6cc31c5c28ae2fe846d2a5862c3ce7c68be83a` — active power policy adapter, CI #462.
- runtime capability discovery — CI #468.
- shared AppServices composition — CI #470.
- `24aae2fa4ba69a1829ec1379f2c436993f55d360` — CPU boost via PowrProf, CI #478.
- `d91380378d4689197d43519f83ffd44fe8b6c56c` — boost adapter composition, CI #480.
- Core Parking adapter + AppServices — CI #490/#492.
- dependency closure availability — CI #496.
- evidence-gated Persistent PC planner — CI #504.
- Analyze/Preview/Revalidate/Apply/History/Restore — CI #510.
- compare-and-set drift protection — CI #520.
- persistent backend — CI #522.
- Global lease protection for Apply/Restore — CI #554.
- shared application benchmark lease — CI #556.
- WPF Optimize surface — CI #560.
- atomic recommendation publication — CI #568.
- Candidate Planner — CI #586.
- controlled Windows capability A/B — CI #592.
- PresentMon evidence-quality hardening — CI #598.
- Guardian-bound operational probe — CI #604.
- capability Cost Map — CI #612.
- stale PID/Guardian hardening — CI #616.
- Experiment Coordinator — CI #622.
- AppServices experiment stack — CI #624.
- Evidence Evaluation — CI #630.
- PendingValidation — CI #636.
- coordinator cost/evaluation/validation — CI #640.
- shared evaluator/gate — CI #642.
- fresh validation challenge → ValidatedEvidence — CI #648.
- raw ControlledEvidence bypass deliberately RED at CI #658 and then blocked in production.
- Optimize presentation authority hardened through CI #686; branch later remained GREEN through Track 3 entry.

Track 2 authority remains unchanged by later tracks: `Observed != Validated`, exact machine fingerprint/freshness, durable ValidatedEvidence and existing recommendation gates remain required for automatic persistent recommendations.

## Track 3 — Game Discovery + Adapter Framework — GREEN

### Stable identity / adapters

- `4087f9449347aeca6c3cb2f1d8c41771b25c5910` — BlueStacks installed FF/FFMAX package discovery, CI #718.
- `f309aeda70b9ae3756eeab08d34313cd1206a440` — neutral Game Adapter framework, CI #722.
- `1820f99e731ac3b5945a18a67587c825c015f420` — GameDiscoveryCoordinator.
- `b7bb91164140ef3c6f9e8b5f75b596983662ecad` — AppServices composition, CI #728.

### Launcher/package sources

- Steam — RED #730; production + fixture hardening GREEN #738; AppServices #740.
- Epic — RED `cfcaccb1...` #742; production `12e9dda5...` #744; composition #746.
- Riot — RED `ca8b90c4...` #748; production `ea15de4b...` #750; composition `23cafd31...` #752.
- Battle.net — RED `a1529cf4...` #754; `product.db` source `7bd3e2a2...` #756; composition `1a362277...` #758.
- EA App — RED `3fc67a81...` #793; `installerdata.xml` source `6d4278e9...` #796; composition `4f35c66e...` #798.
- Ubisoft Connect — RED `25efac79...` #801; registry source `956667cd...` #803; composition `8603a2ba...` #805.
- Microsoft Store / Xbox GDK — RED `3aee884c...` #810; Core PFN/GDK source + safety fixes #812/#814; Windows provider #818; composition `de38db04...` #820.

Stable identity comes only from source-native keys. Display names, executable names, install folders and runtime paths are never cross-launcher identity authorities.

### Identity/evidence two-plane architecture

- Evidence contracts/catalog RED `4a7dd620...` #832 → GREEN `3b836e13...` #836.
- Deterministic evidence binder RED `7bef375c...` #838 → GREEN `b564d3a3...` #840.
- Two-plane coordinator RED `3570b94d...` #842 → GREEN `11ae5104...` #844.
- Running-process evidence RED `a92e045c...` #846 → Core GREEN `94a47907...` #848 → Windows provider GREEN `bf1a35a8...` #852 → composition `1f99581d...` #854.
- Windows App Paths KnownExecutable evidence RED `5e51412d...` #858 → Core GREEN `d420ef16...` #860 → provider `4039f044...` #862 → composition `6832557b...` #864.

Evidence cannot create durable identity. Runtime PID/path and App Paths registration remain evidence. `AppServices.InitializeAsync()` still performs no game/process/App-Paths discovery; explicit authority remains `DiscoverGamesAsync()`.

Track 3 original exit criteria are satisfied. Additional discovery sources are optional enrichment, not a blocker for Track 4.

## Track 4 — Universal Telemetry / Evidence — ACTIVE

Canonical docs:

- `docs/superpowers/specs/2026-09-08-universal-telemetry-evidence-design.md`
- `docs/superpowers/plans/2026-09-08-universal-telemetry-foundation.md`
- `docs/superpowers/plans/2026-09-08-telemetry-v2-current-collectors.md`

### Metric schema v2 + immutable frame

- RED `86953dca06fd278383d35f5a9202371dfe1a0410` — CI #878 / run `34273684420`; native GREEN, managed failed only on missing telemetry-v2 contracts.
- GREEN `4ced969a17b41e4cad56c9e413c626d9cd2326d6` — CI #880 / run `34275125229` SUCCESS.

Implemented:
- 17 stable standard metric descriptors;
- typed domain/unit/aggregation;
- per-metric `Partial` / `Measured` quality;
- `[0,1]` coverage;
- normalized source provenance;
- `Direct` / `Derived` / `Legacy` origin;
- finite numeric enforcement;
- absence instead of fake `Unavailable` numeric values;
- immutable/deterministic `TelemetryFrame` with duplicate rejection and typed lookup.

### Conservative legacy bridge

- RED `9a48b5c7719d4f131819fb0e7b0aecb0add37be9` — CI #882 / run `34275316122`.
- GREEN `476df79441e0c8770f23f260a2824908595d461a` — CI #884 / run `34275483157` SUCCESS.

All 17 legacy nullable numeric fields are mapped explicitly. PresentMon labels prove only Frame-domain metrics; `System` / `Frame+System` prove only CPU + physical memory; unrelated or unknown-label values remain Partial. Null/NaN/infinity emit no observation. `TelemetrySample` remains unchanged.

### Universal workload target resolver

- RED `27271db72d5d7a99ad5b9b35ac203fd89ef4a6cd` — CI #886 / run `34275838695`.
- GREEN `8392892e7ad658928e7b7aca1719df2b64399125` — CI #888 / run `34276000513` SUCCESS.

Only exactly one stable catalog identity and one unambiguous bound RunningProcess PID/path becomes process-capture eligible. Zero running evidence stays unavailable; multiple PIDs/conflicting paths remain ambiguous; KnownExecutable/App Paths never yields a live PID. Resolver is pure Core logic with no process/file probing.

### Native system collector v2

- RED `e3c766e58b6c3c0ef5086c927bacf74745658e68` — CI #894 / run `34276841999`; 0 warnings, failures only on missing system-v2 contracts.
- GREEN `b9b1338828aa890380a8d689f8bb53ff737b517d` — CI #896 / run `34277078378` SUCCESS.

`TelemetryService` now shares one internal native snapshot between legacy and v2 outputs. `CaptureSystemFrame()` emits only finite CPU utilization and physical memory used/total as `Measured / native-system / Direct / coverage 1`. Legacy `CaptureSystemSample()` remains callable and compatible. No GPU/thermal/clock/I/O/network value is invented.

### PresentMon direct v2

- RED `053ec156bfa2a260e6a53ec14169b818a646f786` — CI #898 / run `34277312296`; native GREEN, 0 warnings and exactly six missing-method errors for the new v2 APIs.
- GREEN `edfbba0845d60447e6fdd158b75eee6def39a60f` — CI #900 / run `34277780294` SUCCESS.

`PresentMonService.ParseCsv()` and `ParseCsvFrame()` share one internal statistics object, so legacy and v2 FPS/lows/frame-time percentiles/stutter/latency formulas cannot drift. Legacy `DataQuality = "PresentMon · <accepted frames> frames"` remains unchanged. V2 emits only proven frame/latency metrics as `Measured / presentmon / Direct`; frame coverage uses accepted frame rows / data rows and latency coverage uses accepted latency rows / data rows. Missing latency remains absent. `CaptureProcessAsync()` and `CaptureProcessFrameAsync()` share the same one-shot CSV capture path.

### Current verified application head

```text
edfbba0845d60447e6fdd158b75eee6def39a60f
Windows CI #900 / run 34277780294 — SUCCESS
```

### Next Track 4 boundary

Implement **bounded realtime v2 storage + typed aggregation** with TDD:

1. bounded `TelemetryFrame` ring buffer;
2. deterministic capacity eviction / window snapshot;
3. no raw-frame disk persistence yet;
4. deterministic 1-second aggregate by stable metric id;
5. absent channels are ignored, never zero-filled;
6. descriptor incompatibility must not be blended;
7. aggregate quality cannot exceed the weakest contributor;
8. coverage must remain explicit and conservative;
9. homogeneous provenance may be preserved, mixed provenance must not masquerade as one direct source;
10. existing legacy `PerformanceTimelineBuffer` remains separate and unchanged.

After 1-second aggregation is GREEN, extend to 10-second/session aggregation, then add real hardware channels one provider at a time. Performance/A-B migration away from free-form `DataQuality` parsing is later and must preserve all Track 2 validation/freshness authority.
