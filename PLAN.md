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
- World-space shot tracers and damage numbers make automatic shooting easier to read, using depth-safe foreground materials validated on iOS Simulator.
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
- Current proof: Phase 1 EditMode coverage passed as part of the full `70/70` EditMode Unity run.
- Current PlayMode proof includes Minigame start movement, lane-button movement, depth-safe world-space combat feedback, restart, rewards, mission unlocks, and return-to-Base scene flow as part of the full `16/16` PlayMode Unity run.
- Autoreview was run on the Phase 1 polish diff and reported no accepted/actionable findings.

### Remaining Phase 1 Polish Ideas

- Add a simple sound-free hit flash or projectile placeholder. Done with shot tracers.
- Add a second small level definition. Done through mission progression.
- Improve camera framing for different device aspect ratios. Done with aspect-responsive minigame camera FOV.
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
- Base shows an eight-row mission panel and allows direct button selection among unlocked missions.
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
- Winning a minigame mission marks it complete, and winning the highest unlocked minigame mission unlocks the next mission. Done through mission 8.
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
- Base HUD shows an eight-row mission panel with selected, done, ready, and locked states.
- Base HUD direct mission buttons select among unlocked missions and persist immediately.
- Base mission rows show per-mission reward or unlock hints.
- Minigame launches the selected mission from local save data.
- Eight local mission layouts exist with distinct gate/zombie pacing.
- Clearing the highest unlocked mission unlocks the next mission up to mission 8 and auto-selects it for the Base return.
- Clearing mission 8 marks it complete without advertising a nonexistent mission 9.
- Win reward text shows mission unlocks alongside coins, hero unlocks, and hero XP.

### Validation

- EditMode tests cover mission defaults, legacy migration, completed mission repair, status/reward labels, locked selection rejection, unlocked selection persistence, frontier mission unlocks, replay behavior, mission cap behavior, save persistence, and level definition selection for missions 3, 4, and 8 as part of the full `70/70` EditMode Unity run.
- PlayMode smoke tests cover Base mission panel display, direct mission selection, locked mission rejection, Minigame win mission unlocks, completed mission persistence, mission 8 cap completion, save persistence, world-space combat feedback spawning, and Base return showing the newly selected/completed mission as part of the full `16/16` PlayMode Unity run.
- iOS smoke export succeeds and produces an Xcode project without adding backend or networking dependencies.
- iOS Simulator SDK export builds, installs, and launches on a booted iPhone 17 simulator through XcodeBuildMCP; world-space shot tracers, damage labels, and miss labels were captured in simulator video proof.

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

## Local V3: Retention, Content Depth, And Polish

### Goal

Add a local-only next slice that gives players a reason to replay, extends authored mission content, and improves prototype readability without adding backend, monetization, ads, PvP, or production art.

### Current Status

The first Local V3 slice is implemented:

- Local daily objective state is persisted in the save file.
- Minigame wins advance a two-win UTC-day objective.
- Base HUD shows daily objective progress.
- Base HUD includes a local `CLAIM` button for completed daily objective rewards.
- Daily objective claims grant local coins once for the current UTC day.
- Mission progression now supports eight authored local missions.
- Missions 5 through 8 add longer layouts with new gate and zombie pacing.
- A new damage multiplier gate modifies squad damage per member.
- A new armored zombie type reduces incoming shot damage.
- Armored zombies are labeled in-world with placeholder text.
- Shot damage feedback reports the actual damage applied after armor reduction.
- Base mission buttons are array-driven instead of hard-coded to four mission slots.

### Acceptance Criteria

- Player can see a local daily objective on the Base scene. Done.
- Minigame wins advance the daily objective. Done.
- Player can claim the daily reward after the objective is complete. Done.
- The daily reward persists as claimed and cannot be claimed twice on the same UTC day. Done.
- Mission rows and mission buttons cover missions 1 through 8. Done.
- Clearing the highest unlocked mission advances to the next mission through mission 8. Done.
- Clearing mission 8 marks it complete without advertising mission 9. Done.
- At least one new gate behavior exists. Done with damage multiplier gates.
- At least one new enemy behavior exists. Done with armored zombies.
- No server, networking, ads, IAP, gacha, loot boxes, guilds, or PvP are added.

### Validation

- EditMode tests cover daily objective progress, claim, rollover, damage multiplier gates, armored damage reduction, and mission 8 authored content as part of the full `70/70` EditMode Unity run.
- PlayMode tests cover Base daily objective claiming and mission 8 cap completion as part of the full `16/16` PlayMode Unity run.
- Unity EditMode and PlayMode batch validation passed with the documented temp-copy workflow.

## Near-Term Next Step

Local Phases 1 through 3 and the mission select v2 slice now have the requested roadmap slices represented:

1. Dedicated Hero screen. Done.
2. Manual local hero leveling with a coin cost. Done.
3. Mission selection, completed mission tracking, and eight local minigame layouts unlocked through mission wins. Done.
4. Shot feedback and end-screen spacing polish. Done.
5. Stability pass with expanded tests, iOS smoke export, and iOS Simulator launch proof. Done and verified with Unity EditMode, PlayMode, iOS export, Simulator launch, and combat feedback video proof.
6. Phase 4 backend design doc. Done.
7. Device-aspect camera framing polish. Done with responsive camera FOV, upper-right Minigame state text placement, and automated coverage.

## Local V4: Character Visual Pass

### Goal

Make the minigame actors read as people and zombies instead of rectangular prototype blobs while keeping implementation local-only, generated, and asset-license safe.

### Current Status

The first character visual pass is implemented:

- Player squad now renders as a three-survivor formation made from generated rounded meshes.
- Survivor visuals include heads, helmets, torsos, vests, arms, hands, legs, boots, and generated weapons.
- Basic zombies now render as humanoid undead bodies with green heads, eyes, mouths, reaching arms, legs, feet, and wound details.
- Armored zombies add generated helmet, chest armor, shoulder armor, strap, and belt pieces.
- Survivors now use a stronger procedural bent-knee run cycle while the gameplay root moves.
- Basic and armored zombies now use slower procedural bent-knee in-place shamble animation.
- Survivor and zombie legs now use connected hip, knee, shin, and foot transform chains so knee bends do not create detached joint gaps.
- Zombie leg segments now use a pale dirty-gray material so their shamble remains readable against the dark road and in compressed simulator GIFs.
- Runtime minigame and editor scene rebuild paths both use the same generated character factory.
- The old HUD-only player marker is disabled by default, with visible world-space survivor meshes now serving as the primary player representation.
- No imported art, copyrighted assets, backend dependencies, ads, monetization, gacha, guilds, or PvP were added.

### Validation

- EditMode coverage checks collider-free sphere/cylinder primitive generation and survivor/zombie body hierarchies.
- EditMode coverage checks connected knee/shin pivots, procedural survivor running, zombie shambling, and high-contrast zombie leg material visibility.
- PlayMode coverage checks the runtime Minigame scene spawns humanoid player and zombie parts with animator rigs instead of single-card actor blobs.
- Latest verification: Unity EditMode `73/73`, Unity PlayMode `17/17`, clean `git diff --check`, and fresh iOS Simulator GIF captures for survivor running and zombie shamble visibility.

## Local V5: Survivor Weapon Variety and Tracer Alignment

### Goal

Give the three survivor figures distinct generated weapons held in firing position, and make every world-space shot tracer originate from the visible weapon muzzle instead of an approximate squad-root offset.

### Current Status

Implemented in this slice:

- Kept this slice visual-only; no squad damage, fire rate, target selection, mission balance, hero bonus, economy, or progression tuning changed.
- Replaced the single shared `Human Rifle` prop with three procedural weapon profiles: leader rifle, left-wing shotgun, and right-wing SMG.
- Attached each weapon to the survivor right-hand chain in a firing pose so it points down-lane toward zombie targets.
- Added two firing-pose types: rifle/shotgun survivors hold weapons near eye level, while the SMG survivor fires from the hip.
- Stabilized survivor firing arms during the run cycle so weapons no longer swing up and down with walking arm motion.
- Added a named `Weapon Muzzle` anchor under each weapon, positioned at the barrel tip and following survivor animation.
- Added a small runtime weapon/muzzle registry on `PlayerSquad` so combat feedback can select a real muzzle transform.
- Updated automatic shot feedback so `ShotFired` events use a selected muzzle world position, rotating through available survivor muzzles.
- Preserved the old root-derived forward/lane fallback offset only for scenes or tests that have no weapon muzzle anchors.
- Bypassed the fake forward and lane offsets for weapon-based shots so tracers line up with the visible held weapon.
- Preserved the current always-visible, depth-safe tracer material and flat strip mesh behavior on iOS Simulator.
- Kept all weapons generated from existing collider-free primitive mesh helpers; no weapon art or external assets were imported.

### Acceptance Criteria

- The leader visibly holds a rifle, the left survivor visibly holds a shotgun, and the right survivor visibly holds an SMG. Done.
- Weapons are in firing position in the generated Minigame runtime scene and the editor-rebuilt scene. Done through the shared character factory, including eye-level and hip-fire variants.
- Each weapon has a muzzle anchor at the visible barrel tip. Done.
- Shot tracers start at the selected weapon muzzle in world space and point to the zombie target point. Done for weapon-origin shot events.
- Tracers no longer start from behind the squad, from the lane stripe, or from a hard-coded approximate offset when muzzle anchors exist. Done.
- If a future scene omits weapon anchors, shooting still works with the existing safe fallback origin. Done through the no-muzzle event path.
- No gameplay balance changes are introduced in this phase. Done.

### Validation

- EditMode coverage checks that the generated survivor squad has three distinct weapon profiles, each with a named muzzle anchor. Done.
- EditMode coverage checks muzzle anchors are forward of their survivor bodies and parented under the corresponding weapon transforms. Done.
- EditMode coverage checks rifle/shotgun eye-level holds, SMG hip-fire hold height, and stable survivor weapon arms during running. Done.
- EditMode coverage checks shot events use a muzzle transform position when one is available. Done.
- PlayMode coverage checks tracer spawning uses the supplied muzzle transform position without fake offsets. Done.
- PlayMode coverage checks the runtime Minigame scene spawns all three weapons and muzzle anchors. Done.
- Latest automated verification: Unity EditMode `74/74`, Unity PlayMode `17/17`, clean `git diff --check`, and iOS Simulator export/build/install/launch proof.
- iOS Simulator verification captured `/private/tmp/4xgame-v5-weapons-20260608.mov`, `/private/tmp/4xgame-v5-weapons.gif`, `/private/tmp/4xgame-v5-combat-contact.jpg`, plus the follow-up firing-pose proof at `/private/tmp/4xgame-pose-20260608.mov`, `/private/tmp/4xgame-pose-20260608.gif`, and `/private/tmp/4xgame-pose-contact-20260608.jpg`; survivors visibly hold distinct generated weapons in firing pose, rifle/shotgun holds stay high, the SMG stays lower, and firing arms stay stable while legs run. Done.
