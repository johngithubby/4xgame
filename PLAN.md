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
- Placeholder shot tracers and damage numbers make automatic shooting easier to read.
- Local mission progression unlocks additional minigame layouts.
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
- Current proof: `25/25` Phase 1 EditMode tests passed as part of the full `62/62` EditMode Unity run.
- Current PlayMode proof includes Minigame start movement, lane-button movement, restart, rewards, mission unlocks, and return-to-Base scene flow as part of the full `14/14` PlayMode Unity run.
- Autoreview was run on the Phase 1 polish diff and reported no accepted/actionable findings.

### Remaining Phase 1 Polish Ideas

- Add a simple sound-free hit flash or projectile placeholder. Done with shot tracers.
- Add a second small level definition. Done through mission progression.
- Improve camera framing for different device aspect ratios.
- Expand PlayMode smoke coverage for longer full-run visual/gameplay passes when worthwhile.

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
- Base shows a four-row mission panel and allows direct button selection among unlocked missions.
- Base scene can launch Minigame.
- Base scene can launch the dedicated Heroes screen.
- Minigame win/loss screen can return to Base.
- Minigame wins grant a fixed local coin reward and show it on the completion screen.
- Minigame wins unlock the next local mission when the player clears the highest unlocked mission.
- Base shows a visible feedback message when HQ upgrades complete.
- Editor and development builds expose a local save reset button for prototype iteration.
- Base HUD spacing has been tightened around a phone-sized reference layout.
- Save, wallet, timer, and progression rules have EditMode coverage.
- Base collection/upgrade, mission panel selection, locked mission rejection, completed-upgrade feedback, Base-to-Minigame, Minigame-to-Base, Base-to-Heroes, and Hero screen level/equip actions have PlayMode smoke coverage.

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
16. Add PlayMode smoke coverage for Base -> Minigame navigation and core Base UI actions. Done.
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
- currentMissionLevel
- highestUnlockedMissionLevel
- completedMissionLevels
- unlockedMinigameLevel (legacy compatibility mirror)
- ownedHeroIds
- equippedHeroId
- heroProgress
```

### Acceptance Criteria

- Player can open the Base scene. Done.
- HQ displays level 1 on a fresh save. Done.
- Player can collect coins locally. Done.
- Player can spend coins to start an HQ upgrade. Done.
- Upgrade timer visibly counts down. Done.
- Closing and reopening preserves coins, HQ level, and active timer state. Covered by save/timer implementation and EditMode tests.
- Finished timer upgrades HQ level. Done.
- HQ level affects minigame stats in a visible way. Done.
- Base scene can select previously unlocked missions. Done through direct mission buttons.
- Winning a minigame mission marks it complete, and winning the highest unlocked minigame mission unlocks the next mission. Done through mission 4.
- Winning the minigame grants coins once per run and persists them locally. Done.
- Player gets visible Base feedback when an HQ upgrade completes. Done.
- Development builds can reset local save progress from the Base scene. Done.
- Base scene can load Minigame through the Play button in a PlayMode smoke test. Done.
- No server is required.
- No monetization is added.

### Validation

- Add EditMode tests for resource spending, insufficient funds, timer completion, mission selection/unlocks, minigame win rewards, save reset, and save/load persistence. Done.
- Run Unity EditMode tests with the documented temp-copy batch workflow. Done.
- Run Unity PlayMode smoke tests for Base collect/upgrade persistence, mission panel selection, locked mission rejection, completed-upgrade feedback, Base -> Minigame scene loading, minigame win rewards/unlocks/completion, mission cap behavior, and Minigame end-screen -> Base return. Done.
- Manual Play Mode pass remains useful for visual polish, but the key Base and Minigame UI paths now have automated smoke coverage.

## Phase 3: Heroes

### Goal

Add a simple hero collection and progression layer that modifies minigame stats.

### Current Status

The first Phase 3 hero slices are implemented:

- A local hero catalog exists with one placeholder hero, `Mira Vanguard`.
- A second placeholder hero, `Dax Medic`, unlocks from the HQ level 2 milestone.
- Minigame wins can grant the first hero through gameplay.
- HQ level 2 can grant the second hero through local progression.
- The first hero auto-equips when earned.
- Base HUD shows the equipped hero.
- Base HUD includes a small owned hero panel and manual `EQUIP` button path that cycles owned heroes.
- Equipped hero grants a visible starting squad and damage bonus in the minigame.
- Minigame wins award XP to the equipped hero.
- Hero level increases add a small damage bonus.
- Base HUD shows owned hero level and XP progress.
- Hero ownership, equipped state, and hero level/XP persist in local save data.
- Hero inventory, reward, and progression rules have EditMode coverage.

### Features

- Hero definitions with rarity: Common, Rare, Epic, Legendary.
- Hero levels. First local XP/level slice done.
- Hero inventory. Two local heroes and Base owned-hero panel done.
- Equipped hero selection. First hero auto-equip and Base cycling equip path done.
- Heroes modify minigame stats, such as starting squad size, squad damage, or survivability. Starting bonuses and level-scaled damage done.
- Hero rewards come from gameplay only. Done for first win and HQ level 2 milestone heroes.
- No paid gacha.
- No paid loot boxes.

### Proposed Files

- `Assets/Scripts/Heroes/HeroDefinition.cs`
- `Assets/Scripts/Heroes/HeroCatalog.cs`
- `Assets/Scripts/Heroes/HeroRarity.cs`
- `Assets/Scripts/Heroes/HeroInventory.cs`
- `Assets/Scripts/Heroes/HeroProgression.cs`
- `Assets/Scripts/Heroes/HeroRewardSystem.cs`
- `Assets/Scripts/Heroes/HeroSelectionState.cs`
- `Assets/Scripts/Heroes/HeroStatsApplier.cs`
- `Assets/Scripts/UI/HeroInventoryScreen.cs`
- `Assets/Tests/EditMode/PhaseThreeHeroTests.cs`

### Acceptance Criteria

- Player can earn a hero through gameplay. Done for first win and HQ level 2 milestone heroes.
- Player can view owned heroes. Done through Base panel and dedicated Hero screen.
- Player can select or equip a hero. Done through Base and Hero screen cycling equip buttons.
- Player can manually level an equipped hero with local coins. Done through the Hero screen.
- Equipped hero changes minigame gameplay in a visible way. Done through starting squad and damage bonuses.
- Hero state persists locally. Done, including level/XP.

### Validation

- Add EditMode tests for first hero reward, HQ milestone hero reward, duplicate prevention, manual/cycling equip, hero XP/leveling, manual coin level-up, equipped stat bonuses, save persistence, and invalid equipped hero repair. Done.
- Run Unity EditMode tests with the documented temp-copy batch workflow. Done.
- Run Unity PlayMode smoke tests to keep first-win hero rewards, HQ milestone hero rewards, Base -> Minigame, Base -> Heroes, Hero screen level-up/equip, and return navigation covered. Done.

## Local V2: Mission Progression

### Goal

Add a small local mission progression layer that makes the completed Base, Heroes, and Minigame loops feel like forward progress without adding backend, monetization, PvP, ads, or production art.

### Current Status

The mission select v2 slice is implemented:

- Save data tracks selected mission and highest unlocked mission.
- Save data tracks completed mission levels separately from the highest unlocked mission.
- The legacy `unlockedMinigameLevel` field is retained as a compatibility mirror.
- Older sequential mission progress backfills completed predecessor missions after load.
- Base HUD shows a four-row mission panel with selected, done, ready, and locked states.
- Base HUD direct mission buttons select among unlocked missions and persist immediately.
- Base mission rows show per-mission reward or unlock hints.
- Minigame launches the selected mission from local save data.
- Four local mission layouts exist with distinct gate/zombie pacing.
- Clearing the highest unlocked mission unlocks the next mission up to mission 4 and auto-selects it for the Base return.
- Clearing mission 4 marks it complete without advertising a nonexistent mission 5.
- Win reward text shows mission unlocks alongside coins, hero unlocks, and hero XP.

### Validation

- EditMode tests cover mission defaults, legacy migration, completed mission repair, status/reward labels, locked selection rejection, unlocked selection persistence, frontier mission unlocks, replay behavior, mission cap behavior, save persistence, and level definition selection for missions 3 and 4 as part of the full `62/62` EditMode Unity run.
- PlayMode smoke tests cover Base mission panel display, direct mission selection, locked mission rejection, Minigame win mission unlocks, completed mission persistence, mission 4 cap completion, save persistence, and Base return showing the newly selected/completed mission as part of the full `14/14` PlayMode Unity run.
- iOS smoke export succeeds and produces an Xcode project without adding backend or networking dependencies.
- iOS Simulator SDK export builds, installs, and launches on a booted iPhone 17 simulator through XcodeBuildMCP; the Base mission panel screenshot was captured successfully.

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

- Backend design document exists. Done in `docs/BACKEND_DESIGN.md`.
- No production backend code is added.
- No client networking dependency is required for local gameplay.

### Validation

- Confirm `docs/BACKEND_DESIGN.md` exists. Done.
- Search runtime source and package manifest for client networking, backend, ads, IAP, guild, PvP, gacha, and loot-box dependencies. Done; only plan/design-document references are present.
- Run Unity iOS smoke export after local gameplay validation. Done; the export succeeds without a backend dependency.

## Near-Term Next Step

Local Phases 1 through 3 and the mission select v2 slice now have the requested roadmap slices represented:

1. Dedicated Hero screen. Done.
2. Manual local hero leveling with a coin cost. Done.
3. Mission selection, completed mission tracking, and four local minigame layouts unlocked through mission wins. Done.
4. Shot feedback and end-screen spacing polish. Done.
5. Stability pass with expanded tests, iOS smoke export, and iOS Simulator launch proof. Done and verified with Unity EditMode, PlayMode, iOS export, and Simulator screenshot.
6. Phase 4 backend design doc. Done.
