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

### Smallest Useful Slice

1. Create a `Base` scene.
2. Add an HQ building placeholder.
3. Add a local resource value, initially `coins`.
4. Add a `Collect` button that grants coins.
5. Add an `Upgrade HQ` button.
6. HQ starts at level 1.
7. HQ upgrade spends coins and starts a short timer.
8. Timer persists locally across app close/reopen.
9. Completed HQ upgrade increases HQ level.
10. HQ level grants a visible minigame bonus, such as increased starting squad count.
11. Add navigation from Base to Minigame.
12. Add navigation from Minigame win/loss screen back to Base.

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

- Player can open the Base scene.
- HQ displays level 1 on a fresh save.
- Player can collect coins locally.
- Player can spend coins to start an HQ upgrade.
- Upgrade timer visibly counts down.
- Closing and reopening preserves coins, HQ level, and active timer state.
- Finished timer upgrades HQ level.
- HQ level affects minigame content or stats in a visible way.
- No server is required.
- No monetization is added.

### Validation

- Add EditMode tests for resource spending, insufficient funds, timer completion, and save/load persistence.
- Run Unity EditMode tests with the documented temp-copy batch workflow.
- Manually press Play through Base -> Minigame -> win/loss -> Base.

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

Start Phase 2 with the smallest base-building slice:

1. Add local save data and save manager.
2. Add base scene bootstrap and simple UI.
3. Add HQ model with collect and upgrade timer.
4. Connect HQ level to minigame starting squad bonus.
5. Add tests for save, resources, and timers.
6. Run EditMode tests on a temporary project copy.
