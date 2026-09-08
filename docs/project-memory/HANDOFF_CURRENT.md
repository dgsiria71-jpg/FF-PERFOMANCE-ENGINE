# Current Handoff — 2026-09-08

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR: `#1` — open, draft, not merged
- PR base: `main`

### Last verified application-code checkpoint

- Application HEAD: `de38db0487af48eef9441b6869c18550b4785d58`
- Commit: `feat: register Microsoft Store GDK discovery in shared catalog`
- Windows CI: **#820 — SUCCESS**
- CI run id: `34249611622`

The #820 job passed native configure, C++ build, native tests, managed/WPF build, Core self-tests, `win-x64` publish and artifact upload.

### Repository-native memory bootstrap

The project continuity system lives in root `AGENTS.md` and `docs/project-memory/`. The original bootstrap was verified at `6b15e87a491de32aa7997147d2074f719978d23d` by Windows CI #788, and the final bookkeeping head `5bbc07966c314e50d5ae2124c339c6ff51d68bb6` by Windows CI #791.

Current Git + fresh CI remain authoritative if this handoff ever becomes stale.

## Active Track

**Track 3 — Game Discovery + Adapter Framework**.

### GREEN, composed in the application

- BlueStacks package discovery for Free Fire / Free Fire MAX
- neutral GameIdentity / LocalGameCatalog
- Generic Game Adapter + specialized BlueStacks FF/FF MAX adapters
- GameDiscoveryCoordinator
- Steam manifest discovery
- Epic manifest discovery
- Riot product metadata discovery
- Battle.net `product.db` discovery
- EA App `__Installer/installerdata.xml` discovery
- Ubisoft Connect registry install discovery
- Microsoft Store / Xbox GDK package discovery

Current shared catalog composition:

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

`AppServices.InitializeAsync()` intentionally does **not** perform game discovery. `DiscoverGamesAsync()` remains the explicit authority.

## Microsoft Store / Xbox GDK checkpoint just closed

TDD / verification sequence:

- RED contract: `3aee884c950f1eda64dfecbc4a9d7fea4bff7b80` → Windows CI #810 failed only because the Microsoft Store observation/provider/source contracts did not yet exist.
- Core production: `dd36f0bfcb51982972bf0f8e3d59b5b7f311c847` → CI #812 exposed one nullable-flow compile error (`CS8602`).
- Core null-safety fix: `d8294b9bcee3e522e6acbc1bc17acbf3a610c1f0` → Windows CI #814 SUCCESS.
- Windows WinRT provider: `9c7662217af4bb9f0638e795a6b06c2e91f163a8` → CI #816 proved WinRT projection compatibility and exposed only a missing `System.IO` import.
- Provider import fix: `d3d2bce47685f11452c089d03c804fedb204d42d` → Windows CI #818 SUCCESS.
- App composition: `de38db0487af48eef9441b6869c18550b4785d58` → Windows CI #820 SUCCESS.

Microsoft Store / Xbox GDK rules:

- the neutral Core never references WinRT; `IMicrosoftStoreGamePackageProvider` is the platform boundary;
- the Windows WPF layer uses `PackageManager.FindPackagesForUser("")` only on explicit discovery;
- normalized Package Family Name is the stable local identity: `xbox:<pfn>`;
- versioned PackageFullName, StoreId, TitleId, package name and executable declarations are provenance/evidence, not identity keys;
- a valid `MicrosoftGame.config` is required as positive GDK game evidence;
- framework, resource, bundle and optional packages are rejected as main playable titles;
- DLC semantics carrying `AllowedProducts` are rejected from the base-game catalog;
- malformed XML and DTD/external-entity payloads are rejected safely;
- unresolved `ms-resource:` display names fall back to config `Identity.Name` rather than path/title guessing;
- the Core does not fabricate directly accessible gameplay executables from config declarations;
- the Windows provider uses supported `Package.EffectiveLocation` / `InstalledLocation` + Storage APIs instead of traversing protected `WindowsApps` manually;
- `MicrosoftGame.config` size is bounded before text read, and inaccessible/stale individual packages are isolated rather than aborting the entire source;
- construction is side-effect free; `AppServices.InitializeAsync()` still does not enumerate games.

## Exact next action

Before writing another Track 3 scanner, **re-read `docs/project-memory/ROADMAP.md` and the canonical unified architecture spec** and reconcile them with the now-completed launcher/package discovery surface.

Then choose the next already-approved Track 3 boundary. Do not automatically create a broad standalone/executable/process scanner merely to increase game count. Any next discovery source must preserve the established rules:

1. stable, truthful local identity before catalog admission;
2. positive evidence instead of filename/folder/display-name guessing;
3. read-only discovery and side-effect-free construction;
4. no startup scanning;
5. no fabricated engine, executable or specialized adapter capabilities;
6. RED first, intended Windows CI failure, minimum production, full GREEN, then composition and another full GREEN.

If the roadmap confirms generic standalone/executable/running-process evidence as next, design the identity model first so those lower-confidence observations **enrich** already-known games or remain explicitly provisional instead of accidentally becoming cross-launcher identity authorities.

## Do not regress

- Do not reimplement Optimize integration; it is already complete beyond the old `20490bd...` handoff.
- Do not weaken Track 2 validated-evidence gates to make game discovery easier.
- Do not scan launchers or Windows packages at application startup.
- Do not use display names, executable names or install directories as cross-launcher identity keys.
- Do not create specialized game adapters until the project can truthfully prove the corresponding capabilities.
- Do not treat Windows package identity, Microsoft Store product identity and a physical executable path as interchangeable concepts.
- Do not broaden Microsoft Store recall using "large EXE = game" or similar heuristics; the current source is intentionally precision-first GDK discovery.
