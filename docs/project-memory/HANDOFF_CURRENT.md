# Current Handoff — 2026-09-08

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR: `#1` — open, draft, not merged
- PR base: `main`

### Last verified application-code checkpoint

- Application HEAD: `6832557bb7ce0e62fad894d09077ae5d7593f397`
- Commit: `feat: compose App Paths known executable evidence`
- Windows CI: **#864 — SUCCESS**
- CI run id: `34271999758`

The #864 job passed native configure, C++ build, native tests, managed/WPF build, Core self-tests, `win-x64` publish, artifact upload and complete-job finalization.

### Repository-native memory bootstrap

The project continuity system lives in root `AGENTS.md` and `docs/project-memory/`. Git + code/tests + fresh exact-commit Windows CI remain authoritative if this handoff becomes stale.

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

## Game Identity + Evidence Discovery — GREEN and composed

The approved two-plane design is implemented:

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

Both evidence sources are deliberately non-authoritative. They may enrich or bind to an already-proven `GameIdentity`, but they cannot manufacture a new durable game identity.

## Evidence foundation provenance

### Contracts + deterministic catalog

- RED `4a7dd620d0a132d0f25d1dc3319cc01eb96434fd` → CI #832 / run `34265469006`.
- GREEN `3b836e13e79f3eb8d6a0b1a7dae420103df48f50` → CI #836 / run `34265674843` SUCCESS.

### Deterministic binder

- RED `7bef375c4d5a366171468fd82e5c874357e313d8` → CI #838 / run `34265859816`.
- GREEN `b564d3a35e05c32236a38e9cdb594835fe5c6b47` → CI #840 / run `34266012506` SUCCESS.

### Two-plane coordinator

- RED `3570b94dbdb23ec95baaaf9fd86e72a598b163df` → CI #842 / run `34266191172`.
- GREEN `11ae5104938924035b4cd1e13658b619205236a6` → CI #844 / run `34266428959` SUCCESS.

### Running-process evidence

- RED `a92e045c63fdaae6a741e3b477d8768625b995b5` → CI #846 / run `34266610116`.
- Core GREEN `94a47907b7384680dcf324af39bdabeb91ab319e` → CI #848 / run `34266765471` SUCCESS.
- Windows provider first build `a638765c4a968ea4088e6ac69719f8a2b27e2a67` → CI #850 exposed only missing `System.IO` import.
- Provider GREEN `bf1a35a86440f9b5ff704aca6dc9b2514da411bb` → CI #852 / run `34267108921` SUCCESS.
- Composition `1f99581daa9d09ac4cd12473d43f630062c0fd46` → CI #854 / run `34270208022` SUCCESS.

### Windows App Paths / KnownExecutable evidence

Microsoft documents `HKCU/HKLM\Software\Microsoft\Windows\CurrentVersion\App Paths` as the preferred registration surface mapping an executable name to a fully-qualified application path. DG uses this only as static executable evidence.

- RED `5e51412d61bcfb8c53f068492ba772cd08a4ca98` → CI #858 / run `34271363808`; native remained GREEN and managed failed only because `KnownExecutableObservation`, `IKnownExecutableObservationProvider` and `KnownExecutableGameEvidenceSource` did not yet exist.
- Core GREEN `d420ef1613eb495ad994da2d0211ae8e642a4cfb` → CI #860 / run `34271553807` SUCCESS.
- Windows App Paths provider `4039f044f242dbb714bb282d8b90b5f737921a26` → CI #862 / run `34271718079` SUCCESS.
- AppServices composition `6832557bb7ce0e62fad894d09077ae5d7593f397` → CI #864 / run `34271999758` SUCCESS.

App Paths rules:

- read-only HKCU + HKLM;
- Registry64 + Registry32 on 64-bit Windows, Registry32 on 32-bit Windows;
- only the documented default value is consumed as executable-path evidence;
- path must be fully qualified and the file must currently exist;
- stale, malformed, protected or inaccessible registrations are isolated/skipped;
- the App Paths `Path` value is not interpreted as a game executable;
- command lines are not parsed from registry strings;
- key name/filename is metadata only;
- source emits `GameEvidenceKind.KnownExecutable`, confidence `0.92`, priority `30`, `GameIdHint = null` and no PID/runtime claim;
- duplicate executable paths collapse deterministically by canonical case-insensitive path;
- the resulting path is still transient evidence and is never persisted into stable identity automatically.

## Current binding rules

The binder remains deterministic and fail-safe:

1. exact normalized `GameIdHint` binds only to an existing catalog identity;
2. a non-empty unmatched hint returns `NoMatchingIdentity` and does not fall through to path matching;
3. without a hint, a fully-qualified executable path may bind only by safe install-root containment to exactly one existing `GameId`;
4. lexical prefix collisions such as `C:\Games\Foo` versus `C:\Games\Foobar` do not bind;
5. evidence contained by more than one game stays `AmbiguousInstallPath`;
6. filename-only or invalid paths do not bind;
7. evidence never mutates `GameId`, name, launcher, engine, adapter, stable executable lists or stable install paths;
8. PID/path/timestamp and App Paths registration remain evidence rather than identity.

## Startup / authority invariant

`AppServices.InitializeAsync()` intentionally does **not** perform identity discovery or evidence discovery. It still performs only Windows capability refresh, settings load and Guardian reconciliation.

The explicit authority remains:

```text
AppServices.DiscoverGamesAsync()
        ↓
GameDiscoveryCoordinator
        ├── identity plane
        └── evidence plane
```

No launcher, Microsoft Store package, process enumeration or App Paths registry enumeration runs merely because `AppServices` is constructed or initialized.

## Exact next action

Before widening the binder, reconcile Track 3 exit criteria with Track 4 readiness. Additional installed-app / independent-launcher surfaces are allowed only when they add a truthful signal that can consume the existing evidence plane without inventing durable identity.

If installed-app registry evidence is pursued, do **not** stuff `InstallLocation` into `ExecutablePath`. Model install-root evidence explicitly and add a new deterministic binder rule only after a RED proves the need. Do not use `DisplayName`, publisher, uninstall command, folder name or fuzzy matching as identity.

If no higher-value Track 3 identity/evidence gap remains, close Track 3 and begin the approved Track 4 Universal Telemetry / Evidence work rather than expanding discovery merely for count.

## Do not regress

- Do not reimplement Optimize integration; Track 2 already owns validated recommendation authority and rollback.
- Do not weaken `ValidatedEvidence` / freshness / fingerprint gates to make game discovery easier.
- Do not scan launchers, Windows packages, running processes or App Paths at application startup.
- Do not use display names, executable names or install directories as durable cross-launcher identity keys.
- Do not let evidence create specialized adapter capabilities.
- Do not persist running-process/App-Paths paths into stable launcher-manifest executable fields automatically.
- Do not treat Windows package identity, Store product identity and physical executable paths as interchangeable concepts.
- Do not broaden discovery using heuristic rules such as “large EXE = game”.
