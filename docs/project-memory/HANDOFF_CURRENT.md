# Current Handoff — 2026-09-08

## Repository

- Repo: `dgsiria71-jpg/FF-PERFOMANCE-ENGINE`
- Branch: `build/initial-product`
- PR: `#1` — open, draft, not merged
- PR base: `main`

### Last verified application-code checkpoint

- Application HEAD: `4f35c66e5ff5bdf899a5f431e1de7433c36c23cd`
- Commit: `feat: register EA App game discovery in shared catalog`
- Windows CI: **#798 — SUCCESS**
- CI run id: `34244307403`

The #798 job passed native configure, C++ build, native tests, managed/WPF build, Core self-tests, `win-x64` publish and artifact upload.

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
└── EA App
```

`AppServices.InitializeAsync()` intentionally does **not** perform game discovery. `DiscoverGamesAsync()` is the explicit authority.

## EA App checkpoint just closed

TDD sequence:

- RED contract: `3fc67a817cb9cc733a67a79afef4b38a3ceb430c` → Windows CI #793 failed only because `EaAppGameDiscoverySource` did not yet exist.
- Production: `6d4278e9b6b9ed7a67ba7d8d0e5e9a00c6b4c823` → Windows CI #796 SUCCESS.
- App composition: `4f35c66e5ff5bdf899a5f431e1de7433c36c23cd` → Windows CI #798 SUCCESS.

EA App rules:

- read each installed game's `__Installer/installerdata.xml` only;
- no EA App/Origin process execution;
- no EA network/API dependency;
- first usable `contentID` is the stable local identity: `ea:<primary-content-id>`;
- preserve the full contentID list as provenance/evidence;
- title is display metadata, not identity;
- bind the install path only to the directory that actually owns the manifest;
- do not fabricate gameplay executable, engine or specialized adapter;
- malformed XML, DTD/external entity payloads and manifests without content IDs are rejected safely;
- discovery is deterministic and cancellation-aware;
- default discovery may use known EA/Origin filesystem roots and read-only registry install-path evidence to locate manifests.

## Exact next action

Continue Track 3 with **Ubisoft Connect**, using a new isolated TDD slice:

1. determine the strongest stable local Ubisoft installed-game identity/metadata source;
2. prefer Ubisoft-native stable identifiers over folder/executable/display-name guesses;
3. write RED first, including malformed metadata, false-positive and cancellation cases;
4. verify intended RED in Windows CI;
5. implement the smallest read-only scanner, with no Ubisoft launcher execution;
6. require full Windows CI GREEN;
7. compose one shared Ubisoft discovery source into `AppServices` without startup scanning;
8. require full Windows CI GREEN before opening the next launcher.

After Ubisoft, continue remaining Track 3 discovery surfaces only through stable, non-fabricated evidence: Xbox/Microsoft Store and then other independent launcher/executable/running-process evidence where architecture justifies it.

## Do not regress

- Do not reimplement Optimize integration; it is already complete beyond the old `20490bd...` handoff.
- Do not weaken Track 2 validated-evidence gates to make game discovery easier.
- Do not scan launchers at application startup.
- Do not use display names, executable names or install directories as cross-launcher identity keys.
- Do not create specialized game adapters until the project can truthfully prove the corresponding capabilities.
