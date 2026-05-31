# Project Plan

## Guardrails

- Build an original mobile strategy/action prototype inspired by the broad survival strategy genre, not a copy of any specific commercial game.
- Do not use protected names, logos, art, UI layouts, characters, sounds, or proprietary mechanics from existing games.
- Use Unity as the engine unless a future technical constraint creates a strong reason to switch.
- Target iOS first.
- Use placeholder art only until the game loop is proven.
- Keep implementation local-only until Phases 1 through 3 are stable.
- Do not add ads, real-money purchases, paid gacha, dark-pattern monetization, guilds, real PvP, or production backend work during early phases.
- Keep systems modular, small, readable, and data-driven where useful.

## Phase 1: Advertised Minigame Prototype

### Goal

Create a simple zombie lane shooter prototype that proves the core advertised action loop.

### Current Status

Phase 1 is implemented and pushed on `develop`.

### Implemented Features

- Player squad moves forward automatically.
- Squad count is displayed in the HUD.
- Squad can change lanes with on-screen buttons, keyboard `A/D`, arrow keys, or screen-third pointer/touch input.
- Gates modify squad count or damage.
- Gates resolve only when the squad is in the matching lane.
- Zombies appear in lanes.
- Squad automatically shoots zombies in the current lane.
- Zombies can be defeated.
- Zombie breach damage only applies when the squad is in the same lane.
- Basic level start, win, and lose flow.
- Win/loss screen with restart.
- Placeholder primitives, colored gates, lane markers, finish marker, and floating feedback text.
- Local-only scene with no server and no monetization.

### Important Files

- `Assets/Scenes/Minigame.unity`
- `Assets/Scripts/Data/LevelDefinition.cs`
- `Assets/Scripts/Gameplay/PlayerSquad.cs`
- `Assets/Scripts/Gameplay/SquadLaneInput.cs`
- `Assets/Scripts/Gameplay/Gate.cs`
- `Assets/Scripts/Gameplay/Zombie.cs`
- `Assets/Scripts/Gameplay/AutoShooter.cs`
- `Assets/Scripts/Gameplay/LevelManager.cs`
- `Assets/Scripts/Gameplay/FloatingFeedback.cs`
- `Assets/Scripts/UI/MinigameHudController.cs`
- `Assets/Scripts/UI/EndScreenController.cs`
- `Assets/Tests/EditMode/PhaseOneGameplayTests.cs`

### Validation

- EditMode tests pass with the temp-copy Unity batch workflow documented in `LESSONS_LEARNED.md`.
- Current proof: `7/7` EditMode tests passed.
- Autoreview was run on the Phase 1 polish diff and reported no accepted/actionable findings.

### Remaining Phase 1 Polish Ideas

- Add a simple sound-free hit flash or projectile placeholder.
- Add a second small level definition.
- Improve camera framing for different device aspect ratios.
- Add a PlayMode smoke test once PlayMode test setup is worthwhile.

## Phase 2: Light Base-Building

### Goal

Add a simple home/base screen connected to the minigame. Prove the loop where local base progress affects minigame stats or unlocks.

### Current Status

The first tiny Phase 2 slice is implemented:

- `Base` scene exists.
- Placeholder HQ building displays saved HQ level.
- Local coins can be collected.
- HQ upgrade spends coins and starts a local persisted timer.
- Ready upgrades complete from scene load or while the base scene is open.
- HQ level grants a starting squad bonus in the minigame.
- Base scene can launch Minigame.
- Minigame win/loss screen can return to Base.
- Minigame wins grant a fixed local coin reward and show it on the completion screen.
- Base shows a visible feedback message when HQ upgrades complete.
- Editor and development builds expose a local save reset button for prototype iteration.
- Base HUD spacing has been tightened around a phone-sized reference layout.
- Save, wallet, timer, and progression rules have EditMode coverage.
- Base-to-Minigame scene navigation has PlayMode smoke coverage.

### Smallest Useful Slice

1. Create a `Base` scene. Done.
2. Add an HQ building placeholder. Done.
3. Add a local resource value, initially `coins`. Done.
4. Add a `Collect` button that grants coins. Done.
5. Add an `Upgrade HQ` button. Done.
6. HQ starts at level 1. Done.
7. HQ upgrade spends coins and starts a short timer. Done.
8. Timer persists locally across app close/reopen. Done.
9. Completed HQ upgrade increases HQ level. Done.
10. HQ level grants a visible minigame bonus, such as increased starting squad count. Done.
11. Add navigation from Base to Minigame. Done.
12. Add navigation from Minigame win/loss screen back to Base. Done.
13. Add visible minigame win coin rewards. Done.
14. Add upgrade-complete Base feedback. Done.
15. Add a development-only local save reset button. Done.
16. Add PlayMode smoke coverage for Base -> Minigame navigation. Done.
17. Tighten Base HUD spacing for mobile-like screens. Done.

### Proposed Files

- `Assets/Scenes/Base.unity`
- `Assets/Scripts/Base/HQBuilding.cs`
- `Assets/Scripts/Base/BaseSceneBootstrap.cs`
- `Assets/Scripts/Base/BaseHudController.cs`
- `Assets/Scripts/Economy/ResourceWallet.cs`
- `Assets/Scripts/Progression/UpgradeTimer.cs`
- `Assets/Scripts/Save/SaveGameData.cs`
- `Assets/Scripts/Save/SaveGameManager.cs`
- `Assets/Scripts/Progression/PlayerProgression.cs`
- `Assets/Tests/EditMode/PhaseTwoProgressionTests.cs`

### Data Model Draft

```text
SaveGameData
- coins
- hqLevel
- hqUpgradeStartedUtcTicks
- hqUpgradeDurationSeconds
- hqUpgradeInProgress
- unlockedMinigameLevel
```

### Acceptance Criteria

- Player can open the Base scene. Done.
- HQ displays level 1 on a fresh save. Done.
- Player can collect coins locally. Done.
- Player can spend coins to start an HQ upgrade. Done.
- Upgrade timer visibly counts down. Done.
- Closing and reopening preserves coins, HQ level, and active timer state. Covered by save/timer implementation and EditMode tests.
- Finished timer upgrades HQ level. Done.
- HQ level affects minigame content or stats in a visible way. Done.
- Winning the minigame grants coins once per run and persists them locally. Done.
- Player gets visible Base feedback when an HQ upgrade completes. Done.
- Development builds can reset local save progress from the Base scene. Done.
- Base scene can load Minigame through the Play button in a PlayMode smoke test. Done.
- No server is required.
- No monetization is added.

### Validation

- Add EditMode tests for resource spending, insufficient funds, timer completion, minigame win rewards, save reset, and save/load persistence. Done.
- Run Unity EditMode tests with the documented temp-copy batch workflow. Done.
- Run Unity PlayMode smoke tests for Base -> Minigame scene loading. Done.
- Manual Play Mode pass remains useful for visual polish, but the scene-loading path now has automated smoke coverage.

## Phase 3: Heroes

### Goal

Add a simple hero collection and progression layer that modifies minigame stats.

### Features

- Hero definitions with rarity: Common, Rare, Epic, Legendary.
- Hero levels.
- Hero inventory.
- Equipped hero selection.
- Heroes modify minigame stats, such as starting squad size, squad damage, or survivability.
- Hero rewards come from gameplay only.
- No paid gacha.
- No paid loot boxes.

### Proposed Files

- `Assets/Scripts/Heroes/HeroDefinition.cs`
- `Assets/Scripts/Heroes/HeroRarity.cs`
- `Assets/Scripts/Heroes/HeroInventory.cs`
- `Assets/Scripts/Heroes/HeroRewardSystem.cs`
- `Assets/Scripts/Heroes/HeroSelectionState.cs`
- `Assets/Scripts/Heroes/HeroStatsApplier.cs`
- `Assets/Scripts/UI/HeroInventoryScreen.cs`
- `Assets/Tests/EditMode/HeroProgressionTests.cs`

### Acceptance Criteria

- Player can earn a hero through gameplay.
- Player can view owned heroes.
- Player can select or equip a hero.
- Equipped hero changes minigame gameplay in a visible way.
- Hero state persists locally.

## Phase 4: Online Design Only

### Goal

Design future online systems after local Phases 1 through 3 are stable. Do not implement production networking yet.

### Design Document Topics

- Accounts and login.
- Cloud save.
- Leaderboards.
- Guilds or alliances.
- PvP simulation, not real-time PvP.
- Server-authoritative timers and resources.
- App Store purchase validation only if monetization is later added.
- Basic anti-cheat validation.
- Data ownership and migration plan.

### Proposed File

- `docs/BACKEND_DESIGN.md`

### Acceptance Criteria

- Backend design document exists.
- No production backend code is added.
- No client networking dependency is required for local gameplay.

## Near-Term Next Step

Phase 2 local loop is stable enough to move into Phase 3 after any desired manual visual pass:

1. Start Phase 3 heroes with a tiny hero data/inventory slice.
2. Later, add a second small level definition unlocked by HQ level 2.
3. Later, improve full mobile safe-area handling once the UI stops being placeholder-only.
