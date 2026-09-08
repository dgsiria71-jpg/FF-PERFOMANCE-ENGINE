# Current Handoff — 2026-09-08

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR: `#1` — open, draft, not merged
- PR base: `main`

### Last verified application-code checkpoint

- Application HEAD: `1f99581daa9d09ac4cd12473d43f630062c0fd46`
- Commit: `feat: compose running process evidence discovery`
- Windows CI: **#854 — SUCCESS**
- CI run id: `34270208022`

The #854 job passed native configure, C++ build, native tests, managed/WPF build, Core self-tests, `win-x64` publish, artifact upload and complete-job finalization.

### Repository-native memory bootstrap

The project continuity system lives in root `AGENTS.md` and `docs/project-memory/`. The original bootstrap was verified at `6b15e87a491de32aa7997147d2074f719978d23d` by Windows CI #788, and the final bookkeeping head `5bbc07966c314e50d5ae2124c339c6ff51d68bb6` by Windows CI #791.

Current Git + fresh CI remain authoritative if this handoff ever becomes stale.

## Active Track

**Track 3 — Game Discovery + Adapter Framework**.

### Stable identity plane — GREEN and composed

- BlueStacks package discovery for Free Fire / Free Fire MAX
- neutral `GameIdentity` / `LocalGameCatalogService`
- Generic Game Adapter + specialized BlueStacks FF/FF MAX adapters
- Steam manifest discovery
- Epic manifest discovery
- Riot product metadata discovery
- Battle.net `product.db` discovery
- EA App `__Installer/installerdata.xml` discovery
- Ubisoft Connect registry install discovery
- Microsoft Store / Xbox GDK package discovery

Current identity catalog composition:

```text
GameCatalog
├── BlueStacks
│   ├── Free Fire
│   └── Free Fire MAX
├── Steam
├── Epic Games
├── Riot
├── Battle.net
├── EA App
├── Ubisoft Connect
└── Microsoft Store / Xbox GDK
```

Stable identity still comes only from source-native keys. Display names, executable names and install paths are not cross-launcher identity authorities.

## Game Identity + Evidence Discovery foundation — GREEN

The approved two-plane design is now implemented and composed:

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

The first real evidence source is Windows running-process observation. It is intentionally non-authoritative: PID/path/runtime state can enrich or bind to an already-proven `GameIdentity`, but cannot manufacture a new durable game identity.

### TDD / verification provenance

#### Task 1 — evidence contracts + deterministic catalog

- RED: `4a7dd620d0a132d0f25d1dc3319cc01eb96434fd` → Windows CI #832 / run `34265469006`; native remained GREEN and managed failed only on missing evidence contracts.
- GREEN: `3b836e13e79f3eb8d6a0b1a7dae420103df48f50` → Windows CI #836 / run `34265674843` SUCCESS.

#### Task 2 — deterministic evidence binder

- RED: `7bef375c4d5a366171468fd82e5c874357e313d8` → Windows CI #838 / run `34265859816`; failed only because `GameEvidenceBinder` did not exist.
- GREEN: `b564d3a35e05c32236a38e9cdb594835fe5c6b47` → Windows CI #840 / run `34266012506` SUCCESS.

#### Task 3 — coordinator composition + backward compatibility

- RED: `3570b94dbdb23ec95baaaf9fd86e72a598b163df` → Windows CI #842 / run `34266191172`; failed only on the new four-argument coordinator contract and additive `BoundEvidence` / `UnboundEvidence` fields.
- GREEN: `11ae5104938924035b4cd1e13658b619205236a6` → Windows CI #844 / run `34266428959` SUCCESS.

#### Task 4 — neutral running-process evidence source

- RED: `a92e045c63fdaae6a741e3b477d8768625b995b5` → Windows CI #846 / run `34266610116`; failed only because the running-process observation/provider contracts did not exist.
- GREEN: `94a47907b7384680dcf324af39bdabeb91ab319e` → Windows CI #848 / run `34266765471` SUCCESS.

#### Task 5 — Windows running-process provider

- Provider: `a638765c4a968ea4088e6ac69719f8a2b27e2a67` → Windows CI #850 / run `34266941632`; managed build exposed one compile-only issue: missing `System.IO` import for `Path`.
- Root-cause fix: `bf1a35a86440f9b5ff704aca6dc9b2514da411bb` → Windows CI #852 / run `34267108921` SUCCESS.

#### Task 6 — AppServices composition

- `1f99581daa9d09ac4cd12473d43f630062c0fd46` → Windows CI #854 / run `34270208022` SUCCESS.
- Diff against `bf1a35a8...`: only `src/FFPerformanceEngine.App/AppServices.cs`, with the three shared evidence properties, running-process source/catalog/binder construction and four-argument `GameDiscoveryCoordinator` wiring.
- `InitializeAsync()` was re-read on the exact GREEN SHA and still performs only Windows capability refresh, settings load and Guardian reconciliation.

## Current binding rules

The binder is deterministic and fail-safe:

1. exact normalized `GameIdHint` binds only to an existing catalog identity;
2. a non-empty unmatched hint returns `NoMatchingIdentity` and does not fall through to path matching;
3. without a hint, a fully-qualified executable path may bind only by safe install-root containment to exactly one existing `GameId`;
4. lexical prefix collisions such as `C:\Games\Foo` versus `C:\Games\Foobar` do not bind;
5. evidence contained by more than one game stays `AmbiguousInstallPath`;
6. filename-only or invalid paths do not bind;
7. evidence never mutates `GameId`, name, launcher, engine, adapter, stable executable lists or stable install paths;
8. running PID/path/timestamp remains transient evidence.

## Startup / authority invariant

`AppServices.InitializeAsync()` intentionally does **not** perform identity discovery or evidence discovery.

The explicit application authority remains:

```text
AppServices.DiscoverGamesAsync()
        ↓
GameDiscoveryCoordinator
        ├── identity plane
        └── evidence plane
```

No launcher, Microsoft Store package or process enumeration runs merely because `AppServices` is constructed or initialized.

## Exact next action

The foundation plan `docs/superpowers/plans/2026-09-08-game-evidence-discovery-foundation.md` is complete through Task 6. After this memory checkpoint itself receives fresh Windows CI GREEN, start the **next separate Track 3 plan** for installed-app / independent-launcher evidence.

The next plan must consume the existing evidence plane rather than inventing provisional durable identities. Add one evidence source at a time with RED → intended failure → minimum production → full Windows CI GREEN → composition → another full GREEN.

Do not create a generic `standalone:<hash(path)>`, executable-name identity, folder-name identity or fuzzy display-name identity.

## Do not regress

- Do not reimplement Optimize integration; Track 2 already owns validated recommendation authority and rollback.
- Do not weaken `ValidatedEvidence` / freshness / fingerprint gates to make game discovery easier.
- Do not scan launchers, Windows packages or running processes at application startup.
- Do not use display names, executable names or install directories as durable cross-launcher identity keys.
- Do not let evidence create specialized adapter capabilities.
- Do not persist running-process paths into stable launcher-manifest executable fields automatically.
- Do not treat Windows package identity, Store product identity and a physical executable path as interchangeable concepts.
- Do not broaden discovery using heuristic rules such as “large EXE = game”.
