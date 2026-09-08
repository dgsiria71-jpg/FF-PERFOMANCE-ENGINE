# Current Handoff — 2026-09-08

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR: `#1` — open, draft, not merged
- PR base: `main`

### Last verified application-code checkpoint

- Application HEAD: `8603a2ba946c481051fd85f0944df03ea0810abd`
- Commit: `feat: register Ubisoft game discovery in shared catalog`
- Windows CI: **#805 — SUCCESS**
- CI run id: `34245559188`

The #805 job passed native configure, C++ build, native tests, managed/WPF build, Core self-tests, `win-x64` publish and artifact upload.

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
└── Ubisoft Connect
```

`AppServices.InitializeAsync()` intentionally does **not** perform game discovery. `DiscoverGamesAsync()` is the explicit authority.

## Ubisoft Connect checkpoint just closed

TDD sequence:

- RED contract: `25efac795392fc424d6004d6e478d40604987da7` → Windows CI #801 failed only because `UbisoftInstallRegistration` / `UbisoftGameDiscoverySource` did not yet exist.
- Production: `956667cd798198a703575b34f2710eb13fbfdff7` → Windows CI #803 SUCCESS.
- App composition: `8603a2ba946c481051fd85f0944df03ea0810abd` → Windows CI #805 SUCCESS.

Ubisoft rules:

- read HKLM Ubisoft Launcher install registrations through both 64-bit and 32-bit registry views;
- use the normalized positive numeric `Ubisoft\Launcher\Installs\<id>` key as a stable **local installation identity**: `ubisoft:<id>`;
- do **not** claim that registry install ID is necessarily the same identifier used by `uplay://launch/...`;
- `InstallDir` must be fully qualified and exist, otherwise the registration is stale and rejected;
- optional uninstall `DisplayName` may supply display metadata only;
- when `DisplayName` is absent, use the transparent label `Ubisoft game <id>` rather than guessing from the folder name;
- duplicate 32/64-bit views are collapsed deterministically by normalized numeric ID;
- do not fabricate gameplay executable, engine, auxiliary processes or specialized adapter;
- no Ubisoft Connect process execution;
- discovery is read-only, side-effect free at construction and cancellation-aware.

## Exact next action

Continue Track 3 with **Xbox / Microsoft Store / Gaming Services** as a new isolated research + TDD slice.

This surface must be designed more carefully than ordinary launcher registries because Windows package identity, Microsoft Store product identity, Gaming Services metadata and physical install location are distinct concepts. Before writing a scanner:

1. identify the strongest supported local APIs/data sources for installed game packages;
2. distinguish stable package identity from Store/Product IDs instead of conflating them;
3. exclude system/framework packages and non-game apps using positive evidence rather than name guesses;
4. preserve package family/full names and install-location evidence without assuming an executable is directly accessible;
5. avoid unauthorized traversal of protected `WindowsApps` content; prefer supported package APIs/metadata;
6. write RED first for deterministic identity, false-positive filtering, inaccessible package metadata and cancellation;
7. verify intended RED in Windows CI;
8. implement the minimum read-only source;
9. require full Windows CI GREEN;
10. compose it into the shared catalog without startup scanning and require another full GREEN.

After Xbox/Microsoft Store, reassess Track 3 against the canonical roadmap before adding generic standalone/executable/running-process discovery. Do not create a broad heuristic scanner merely to increase game count; stable identity and truthful evidence remain more important than recall.

## Do not regress

- Do not reimplement Optimize integration; it is already complete beyond the old `20490bd...` handoff.
- Do not weaken Track 2 validated-evidence gates to make game discovery easier.
- Do not scan launchers at application startup.
- Do not use display names, executable names or install directories as cross-launcher identity keys.
- Do not create specialized game adapters until the project can truthfully prove the corresponding capabilities.
- Do not treat Windows package identity, Microsoft Store product identity and a physical executable path as interchangeable concepts.
