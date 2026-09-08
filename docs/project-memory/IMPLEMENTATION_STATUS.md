# DG Performance Engine — Implementation Status Ledger

This is a curated ledger of important verified milestones. Git history remains the complete commit ledger.

## Baseline application foundation

The repository already contains a functional Windows WPF + C++ product foundation: navigation/UI, BlueStacks discovery/configuration, PresentMon, profiles, Guardian, Auto Tuner, History/snapshots, Mini Mode themes and native interop.

## Performance A/B and profile evidence

- `8062c318...` — real Performance A/B presentation/service transformation.
- `c259c723c443a819bfae540017d0f76aecbceeef` — adversarial hardening: recompute aggregates from actual frozen points; Windows CI run 268 / `34015937217` SUCCESS.
- `6e2e6a9667dc04f5a8f654a5cb0696fed6100b4f` — exact configuration/fingerprint attached to evidence and Validated profile origin; Windows CI #316 SUCCESS.
- `225f2776d8709ebf51ab3e77b91550c4d89003d7` — profile challenge/freshness/drift block GREEN, Windows CI #342 SUCCESS.
- later hardening added incumbent freshness, challenge progress and physical A/B rounds before DG expansion.

## Track 0 — Experimental Integrity

- `985688276cd7937b74a870d61445fa239ac570ad` — Global Controlled Benchmark Lease + Guardian suspension/reconciliation + concurrency hardening.
- Windows CI #402 SUCCESS.

## Track 1 — Universal Diagnostic Foundation

- `f1c932b7ce7af8c61c424c3c619b66784917ee22`
- Windows CI #426 / run `34075848480` SUCCESS.
- MachineContext v2, Hardware Discovery, Capability Registry/Graph, fingerprint v2, Universal Bottleneck Analyzer.

## Track 2 — System Optimizer and evidence authority

Representative checkpoints:

- `b21330602f2eeba1d04336da8df32a3a73daf6fa` — hardened SystemOptimizationTransactionEngine, CI #456 SUCCESS.
- `1b6cc31c5c28ae2fe846d2a5862c3ce7c68be83a` — real active power policy adapter, CI #462 SUCCESS.
- runtime capability discovery — CI #468 SUCCESS.
- shared AppServices composition — CI #470 SUCCESS.
- `24aae2fa4ba69a1829ec1379f2c436993f55d360` — CPU boost via PowrProf, CI #478 SUCCESS.
- `d91380378d4689197d43519f83ffd44fe8b6c56c` — boost adapter composed, CI #480 SUCCESS.
- Core Parking refactor/adapter — CI #490 SUCCESS.
- `0af02c25f2a8e8a8a2f48880fae5483316ef2cf4` — Core Parking AppServices, CI #492 SUCCESS.
- `58b92eb...` — dependency closure availability hardening, CI #496 SUCCESS.
- `1224cc3...` — evidence-gated Persistent PC planner, CI #504 SUCCESS.
- `75fb715...` — Analyze/Preview/Revalidate/Apply/History/Restore, CI #510 SUCCESS.
- `22d4494...` — compare-and-set drift protection, CI #520 SUCCESS.
- `798d4ed...` — persistent PC backend in AppServices, CI #522 SUCCESS.
- Global lease protection for persistent Apply/Restore — CI #554 SUCCESS.
- shared application-level benchmark lease — CI #556 SUCCESS.
- WPF “Otimizar este PC” surface — CI #560 SUCCESS.
- atomic recommendation batch publication — CI #568 SUCCESS.
- Candidate Planner `766091...` — CI #586 SUCCESS.
- controlled Windows capability A/B `771aed...` — CI #592 SUCCESS.
- PresentMon evidence-quality hardening `77e344...` — CI #598 SUCCESS.
- Guardian-bound operational benchmark probe — CI #604 SUCCESS.
- Windows capability Cost Map — CI #612 SUCCESS.
- stale PID/Guardian suspension hardening — CI #616 SUCCESS.
- Experiment Coordinator — CI #622 SUCCESS.
- AppServices experiment stack — CI #624 SUCCESS.
- Evidence Evaluation — CI #630 SUCCESS.
- PendingValidation gate — CI #636 SUCCESS.
- coordinator returns cost/evaluation/validation decision — CI #640 SUCCESS.
- shared evaluator/gate in AppServices — CI #642 SUCCESS.
- fresh validation challenge → ValidatedEvidence — CI #648 SUCCESS.
- ControlledEvidence bypass test RED at CI #658; production was hardened so automatic evidence recommendations require `ValidatedEvidence` (or authorized Diagnostic provenance).
- `20490bd8643c724019674a3c896c1cf952958f34` — Windows capability experiment presentation gate later confirmed GREEN at CI #686.
- branch subsequently advanced through additional Optimize integration/hardening; actual repository state was trusted over the old handoff, and HEAD was confirmed GREEN at CI #710 before Track 3 continued.

## Track 3 — Game Discovery + Adapter Framework

### BlueStacks / Game identity foundation

- `4087f9449347aeca6c3cb2f1d8c41771b25c5910` — BlueStacks installed FF/FFMAX package discovery, CI #718 SUCCESS.
- `f309aeda70b9ae3756eeab08d34313cd1206a440` — neutral Game Adapter framework, CI #722 SUCCESS.
- `1820f99e731ac3b5945a18a67587c825c015f420` — GameDiscoveryCoordinator.
- `b7bb91164140ef3c6f9e8b5f75b596983662ecad` — AppServices composition, CI #728 SUCCESS.

### Steam

- RED: CI #730.
- `e300de2d8696a006367aa6d25315fdffe289ef7a` — platform annotation fix after CA1416.
- original #734 run was externally cancelled after a long self-test hang; root cause was async fixture I/O inside ModuleInitializer, not Steam scanner behavior.
- `0a58a44d...` — fixture made synchronous; CI #738 SUCCESS.
- `42f83e53...` — Steam composed in AppServices; CI #740 SUCCESS.

### Epic

- RED: `cfcaccb1...`, CI #742.
- `12e9dda5...` — Epic `.item` manifest discovery GREEN, CI #744 SUCCESS.
- `3080f1d8...` — Epic composed, CI #746 SUCCESS.

### Riot

- RED: `ca8b90c4...`, CI #748.
- `ea15de4b...` — Riot product metadata discovery GREEN, CI #750 SUCCESS.
- `23cafd31fba0740c1762881c3063d6ce6ddca9f2` — Riot composed, CI #752 SUCCESS.

### Battle.net

- RED contract: `a1529cf41214fedd646864f747995e6b757c09be`, CI #754 failed only on missing source type.
- `7bd3e2a2a48b8c0cf058e609fb26ea92550b31e3` — read-only `product.db` discovery, CI #756 SUCCESS.
- `1a3622770a7b8f4c7afe7099af6e42990189d80d` — Battle.net composed in shared catalog, CI #758 SUCCESS.

### EA App

- RED contract: `3fc67a817cb9cc733a67a79afef4b38a3ceb430c`, CI #793 failed only because `EaAppGameDiscoverySource` did not yet exist.
- `6d4278e9b6b9ed7a67ba7d8d0e5e9a00c6b4c823` — read-only `__Installer/installerdata.xml` discovery using stable primary `contentID`, full alias provenance, DTD/malformed XML rejection and no executable fabrication; CI #796 SUCCESS.
- `4f35c66e5ff5bdf899a5f431e1de7433c36c23cd` — EA App composed in shared `GameCatalog`; CI #798 SUCCESS.

### Ubisoft Connect

- RED contract: `25efac795392fc424d6004d6e478d40604987da7`, CI #801 failed only because `UbisoftInstallRegistration` / `UbisoftGameDiscoverySource` did not yet exist.
- `956667cd798198a703575b34f2710eb13fbfdff7` — read-only HKLM Ubisoft launcher install discovery, numeric local install identity normalization, 32/64-bit view deduplication, stale registration rejection, optional uninstall DisplayName and no executable/title guessing; CI #803 SUCCESS.
- `8603a2ba946c481051fd85f0944df03ea0810abd` — Ubisoft composed in the shared `GameCatalog`; CI #805 SUCCESS.

### Microsoft Store / Xbox GDK

- RED contract: `3aee884c950f1eda64dfecbc4a9d7fea4bff7b80`, CI #810 failed only because the neutral Microsoft Store observation/provider/source contracts did not yet exist.
- `dd36f0bfcb51982972bf0f8e3d59b5b7f311c847` — neutral Core PFN-based GDK discovery + safe `MicrosoftGame.config` parser; CI #812 exposed one nullable-flow compile error.
- `d8294b9bcee3e522e6acbc1bc17acbf3a610c1f0` — explicit PFN null-safety; CI #814 SUCCESS.
- `9c7662217af4bb9f0638e795a6b06c2e91f163a8` — Windows PackageManager/Storage provider; CI #816 proved WinRT projection compatibility and exposed only a missing `System.IO` import.
- `d3d2bce47685f11452c089d03c804fedb204d42d` — provider import fix; CI #818 SUCCESS.
- `de38db0487af48eef9441b6869c18550b4785d58` — Microsoft Store/Xbox GDK composed in shared `GameCatalog`; CI #820 SUCCESS.

Identity is normalized Package Family Name (`xbox:<pfn>`). The current source is precision-first: valid `MicrosoftGame.config` is required; PackageFullName, StoreId, TitleId and configured executable declarations remain evidence; framework/resource/bundle/optional/DLC packages are excluded; the Windows provider uses PackageManager + supported Storage APIs and does not enumerate at startup.

## Current verified application head

```text
de38db0487af48eef9441b6869c18550b4785d58
Windows CI #820 — SUCCESS
```

Next Track 3 action: re-read `docs/project-memory/ROADMAP.md` and the canonical unified architecture spec, reconcile them with the completed discovery sources, and choose the next already-approved discovery boundary before writing code.
