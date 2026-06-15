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
- HQ, bio-lab, hangar, training, and living-quarters upgrades spend coins and start local persisted timers using the shared 1s, 3s, 10s, 60s, then x5 level-based duration curve.
- Ready upgrades complete from scene load or while the base scene is open.
- Completed HQ, bio-lab, hangar, training, and living-quarters upgrades play building-specific generated local finish sounds.
- HQ level grants a starting squad bonus in the minigame.
- Base shows direct mission buttons and keeps coins, HQ level, and upgrade status inside an expandable Credits control.
- The idle HQ HUD upgrade button remains clickable for missing-credit feedback, and RESET/UPGRADE labels remain readable and clickable inside the HUD canvas.
- Base scene can launch Minigame.
- Base scene can launch the dedicated Heroes screen.
- Minigame win/loss screen can return to Base.
- Minigame wins grant a fixed local coin reward and show it on the completion screen.
- Minigame wins unlock the next local mission when the player clears the highest unlocked mission.
- Base shows a visible feedback message when HQ upgrades complete.
- Editor and development builds expose a local save reset button for prototype iteration.
- Base HUD spacing has been tightened around a phone-sized reference layout.
- Save, wallet, timer, and progression rules have EditMode coverage.
- Base collection/upgrade, shared HQ duration persistence, high-level HQ upgrade/reset clicks, Credits expansion, mission button selection, locked mission rejection, completed-upgrade feedback, living-quarters reference model/upgrade coverage, Base-to-Minigame, Minigame-to-Base, Base-to-Heroes, and Hero screen level/equip actions have PlayMode smoke coverage.

### Smallest Useful Slice

1. Create a `Base` scene. Done.
2. Add an HQ building placeholder. Done.
3. Add a local resource value, initially `coins`. Done.
4. Add a `Collect` button that grants coins. Done.
5. Add an `Upgrade HQ` button. Done.
6. HQ starts at level 1. Done.
7. HQ upgrade spends coins and starts the shared level-based building timer. Done.
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
- bioLabLevel / bioLabUpgradeStartedUtcTicks / bioLabUpgradeDurationSeconds / bioLabUpgradeInProgress
- hangarLevel / hangarUpgradeStartedUtcTicks / hangarUpgradeDurationSeconds / hangarUpgradeInProgress
- trainingFacilityLevel / trainingFacilityUpgradeStartedUtcTicks / trainingFacilityUpgradeDurationSeconds / trainingFacilityUpgradeInProgress
- livingQuartersLevel / livingQuartersUpgradeStartedUtcTicks / livingQuartersUpgradeDurationSeconds / livingQuartersUpgradeInProgress
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
- Run Unity PlayMode smoke tests for Base collect/upgrade persistence, Credits expansion, mission button selection, locked mission rejection, completed-upgrade feedback, Base -> Minigame scene loading, minigame win rewards/unlocks/completion, mission cap behavior, and Minigame end-screen -> Base return. Done.
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
- Base HUD shows direct mission buttons with selected and locked states.
- Base HUD direct mission buttons select among unlocked missions and persist immediately.
- Base reward and unlock hints remain visible through play/reward/status feedback.
- Minigame launches the selected mission from local save data.
- Eight local mission layouts exist with distinct gate/zombie pacing.
- Clearing the highest unlocked mission unlocks the next mission up to mission 8 and auto-selects it for the Base return.
- Clearing mission 8 marks it complete without advertising a nonexistent mission 9.
- Win reward text shows mission unlocks alongside coins, hero unlocks, and hero XP.

### Validation

- EditMode tests cover mission defaults, legacy migration, completed mission repair, status/reward labels, locked selection rejection, unlocked selection persistence, frontier mission unlocks, replay behavior, mission cap behavior, save persistence, and level definition selection for missions 3, 4, and 8 as part of the full `70/70` EditMode Unity run.
- PlayMode smoke tests cover Base Credits expansion, direct mission selection, locked mission rejection, Minigame win mission unlocks, completed mission persistence, mission 8 cap completion, save persistence, world-space combat feedback spawning, and Base return showing the newly selected/completed mission button as part of the PlayMode Unity run.
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
- Base HUD exposes a local claim button for completed daily objective rewards.
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

## Local V6: Base Readability, HQ Growth, And Camera Control

### Goal

Make the Base scene feel like a place the player can inspect and improve before adding deeper strategic systems.

### Current Status

Implemented in this slice:

- HQ upgrades now raise the reference-textured pentagon HQ by a few pixels per level without widening the footprint. Done.
- HQ upgrades no longer darken the HQ body; the generated reference art owns the visible color. Done.
- HQ upgrades no longer add generated side-detail rows or exterior complications. Done.
- The generated HQ label stays hidden while the reference art is available because the art contains the HQ sign. Done.
- Completed HQ upgrades use a pulsating blurred aura made from the exact HQ silhouette. Done.
- Completed HQ upgrades play a unique generated local HQ finish sound. Done.
- Tapping the visible HQ reveals a green/grey flat 2D popup upgrade arrow, and tapping that arrow starts the same persisted HQ upgrade timer as the HUD button when credits allow. Done.
- HQ upgrades reuse the same level-based duration curve as the bio lab, hangar, and training facility: 1s, 3s, 10s, 60s, then x5 each further level. Done.
- Running HQ upgrades show the same circular world-space progress fill treatment as the bio lab. Done.
- The idle HQ HUD upgrade button remains clickable when credits are short, reports the needed credits, and keeps RESET/UPGRADE labels from clipping. Done.
- Building popup arrows close when the player taps another building, empty map space, or HUD action. Done.
- The Base scene supports zooming in and out through on-screen buttons, mouse wheel, keyboard shortcuts, and two-finger pinch math. Done.
- The map that includes the Base supports bounded dragging through mouse and one-finger touch input while ignoring HUD-origin gestures. Done.
- The HQ body keeps its pentagon structure, while the Base floor has no visible border or outline. Done.
- The Base layout includes visible reserved space for future gates, resource drop-off, labs, hangars, and training areas without drawing wall/moat rings. Done.
- The former left-side white status text stack has been replaced with an expandable Credits button that reveals coins, HQ level, and upgrade status on tap. Done.

### Implemented Features

- HQ upgrades increase the HQ height subtly while keeping the same footprint.
- HQ upgrades preserve the reference-textured HQ color instead of darkening the body.
- HQ upgrades avoid generated detail-row complications.
- Completed HQ upgrades show a reference-silhouette glow around the building outline.
- Completed HQ upgrades play a unique generated local HQ finish sound.
- The visible HQ can be tapped to reveal a flat 2D popup upgrade arrow for the next HQ upgrade.
- HQ upgrades use the same level-based duration curve as the other upgradeable buildings.
- Running HQ upgrades show a circular progress fill above the HQ.
- The HQ HUD upgrade button explains missing credits instead of becoming a silent dead button, and compact action/reset labels shrink before clipping.
- Popup upgrade arrows dismiss when the player taps elsewhere in the Base world or uses the HUD.
- The Base scene supports zooming in and out.
- The map that includes the base is draggable.
- The HQ remains pentagon shaped, but the base floor is unoutlined.
- The base layout leaves clear future space for gates, resource drop-off openings, labs, hangars, and training areas without visible perimeter borders.
- The Base HUD uses a Credits button instead of always-visible left-side status text.

### Acceptance Criteria

- HQ level changes are readable without opening a menu, while level 14 remains compact, fixed-width, and reference-colored. Done.
- Completed HQ upgrades show a pulsating outline aura without adding exterior green pieces. Done.
- Completed HQ upgrades play a unique generated HQ finish sound rather than reusing the other building chimes. Done.
- Tapping the visible HQ reveals a flat 2D upgrade arrow, and tapping that arrow starts a saved HQ upgrade when the player has enough credits. Done.
- HQ timers follow the same 1s, 3s, 10s, 60s, then x5 duration curve as the bio lab, hangar, and training facility. Done.
- Running HQ upgrades show a circular progress fill above the HQ. Done.
- Tapping the idle HQ HUD upgrade button always gives feedback, either starting the timer or showing the needed credits. Done.
- RESET and UPGRADE remain readable and clickable inside the HUD canvas. Done.
- Popup upgrade arrows are hidden after tapping another building, empty map space, or HUD action. Done.
- Base zoom works on mobile-friendly input and does not hide core HUD controls. Done.
- The map that includes the base is draggable. Done.
- The base floor has no visible pentagon border or outline. Done.
- The Credits button expands to show coins, HQ level, and upgrade status. Done.
- Existing HQ upgrade, mission launch, hero, and daily objective flows keep working. Done.

### Validation

- PlayMode coverage checks the unoutlined Base floor, reference-textured pentagon HQ geometry, hidden scaffold renderer, compact level-14 height-only HQ growth, fixed footprint, no generated detail rows, reserved future-space objects, camera zoom controls, and map dragging that leave overlay HUD controls fixed. Done.
- PlayMode coverage checks the Credits button expansion and verifies the old left-side Base status labels are not generated. Done.
- Existing PlayMode coverage still checks HQ upgrade persistence and level-based saved duration through the popup arrow and HUD button, visible high-level HQ upgrade/reset clicks through EventSystem pointer handlers, zero-thickness flat 2D arrow mesh generation, running HQ circular progress, popup-arrow dismissal, completed-upgrade feedback, HQ silhouette-aura/sound feedback, mission launch, Hero navigation, and daily objective claiming. Done.
- Latest automated verification: Unity EditMode `84/84`, Unity PlayMode `30/30`, and clean `git diff --check` from `/private/tmp/4xgame-hq-duration`.

## Local V6-1: Bio lab upgrade

### Goal

Upgrade the bio lab for credits over a period of time, while keeping its visual progression readable and compact.

Implemented in this slice:

- The Base scene now creates an upgradeable bio lab on the former future lab pad.
- The visible bio lab uses the generated reference image directly through `Resources/BioLab/BioLabReferenceCutout`.
- The former procedural body, plinth, and dome remain hidden as click/progress fallback scaffolding only.
- Tapping the bio lab reveals a grey or green flat 2D upgrade symbol based on whether the saved wallet can afford the next level.
- Bio-lab popup upgrade symbols close when the player taps another building, empty map space, or HUD action.
- Clicking the green symbol starts a saved bio-lab upgrade timer and spends the upgrade cost.
- A circular progress icon overlays the lab while the saved timer is active.
- Completed bio-lab upgrades increase the saved lab level, clear the timer, pop the lab, play a unique generated bio-lab finish sound, and show a five-second pulsing aura.
- The completion aura uses `Resources/BioLab/BioLabReferenceGlowSilhouette`, a blurred copy of the exact lab silhouette, so the glow follows the building outline.
- Level progression grows the visible lab only slightly taller; it does not add exterior complications or extra outside structures.

### Acceptance Criteria

- When the bio lab is clicked, pop up a clickable flat 2D upgrade symbol; if the user has enough credits, the symbol color is green, else grey. Done.
- When the symbol is clicked and the user has enough credits, start upgrading the bio lab; show this by laying over a circular progress icon that fills progressively with time. Done.
- The time required rises by level: level 1 takes 1 second, level 2 takes 3 seconds, level 3 takes 10 seconds, then 60 seconds, then multiplies by 5 each further level. Done.
- When the bio lab has finished upgrading:
  - Make it re-pop into existence. Done.
  - Grow the building in height by a few pixels, without adding exterior complications or outside structures. Done.
  - Make the exact building silhouette glow for 5 seconds with a pulsating blurred aura. Done.
  - Make a finish sound. Done.

### Validation

- EditMode progression coverage checks bio-lab upgrade costs, timers, completion, and save persistence. Done.
- PlayMode coverage checks grey/green flat 2D upgrade-symbol behavior, zero-thickness arrow mesh generation, saved timer start, circular progress fill, height-only visual leveling, completion pop/glow/sound, reference-textured model loading, hidden occupied lab pad renderer, and no lab-pad label. Done.
- Visual verification captured and reviewed a rendered still plus GIF of the pulsing blurred silhouette aura. Done.
- Latest automated verification: Unity EditMode `84/84`, Unity PlayMode `30/30`, and clean `git diff --check` from `/private/tmp/4xgame-hq-duration`.

## Local V6-2: Hangar and training facility upgrade models

### Goal

Bring the hangar and training facility out of placeholder-pad status by using the generated concept drawings as in-game models and giving both buildings the same popup-arrow upgrade behavior as the bio lab.

Implemented in this slice:

- The Base scene now creates a hangar on the former future hangar pad and a training facility on the former future training pad.
- The visible hangar uses `Resources/Hangar/HangarReferenceCutout`; the visible training facility uses `Resources/Training/TrainingFacilityReferenceCutout`.
- The lab and hangar side slots have been pushed farther away from the HQ, and HQ/lab/hangar/training reference materials use separate warm/cool/industrial/tactical tints.
- The occupied hangar and training pads remain logical map slots but no longer draw slabs or separate pad labels.
- Both buildings keep hidden primitive scaffolds for fallback rendering, click/progress sizing, and tests, while the reference images own the visible look.
- Tapping either building reveals a grey or green flat 2D upgrade symbol based on whether the saved wallet can afford the next level.
- Clicking a green symbol starts a saved upgrade timer, spends the upgrade cost, hides the symbol, and shows a circular progress icon above the building.
- Hangar and training upgrades reuse the bio-lab cost scale and duration curve: 1s, 3s, 10s, 60s, then x5 each further level.
- Completed upgrades increase the saved building level, clear the timer, pop the building, play building-specific generated local finish sounds, show five-second pulsing silhouette auras, and grow the visible reference models only slightly taller.
- Tapping another building, empty map space, Credits, Collect, or other HUD actions hides popup arrows for all current buildings.

### Acceptance Criteria

- Hangar and training facility models match their generated concept drawings through reference-textured world quads. Done.
- HQ, lab, hangar, and training palettes are visually separated while preserving the generated silhouettes. Done.
- Both buildings use clickable flat 2D popup upgrade arrows with the same green/grey affordability rule as the bio lab. Done.
- Both buildings start saved timers from the popup arrow, display circular progress while running, and complete from UTC save data after app reopen. Done.
- Both buildings grow only in height by a few pixels after an upgrade; they do not add exterior complications. Done.
- Both buildings use a pulsating blurred aura around the exact reference silhouette when an upgrade completes. Done.
- Both buildings use their own generated local finish sound profiles when an upgrade completes. Done.
- Any outside world or HUD click hides all building popup arrows. Done.

### Validation

- EditMode progression coverage checks hangar and training upgrade costs, timers, completion, malformed timer repair, reset defaults, and save/load persistence. Done.
- PlayMode coverage checks reference-textured hangar/training model loading, hidden occupied pad renderers/labels, zero-thickness flat 2D arrow mesh generation, saved timer start, circular progress fill, height-only visual leveling, popup dismissal across all buildings, and completion pop/glow/sound feedback. Done.
- Latest automated verification: Unity EditMode `84/84`, Unity PlayMode `30/30`, and clean `git diff --check` from `/private/tmp/4xgame-hq-duration`.

## Local V6-3: Living quarters building

### Goal

Add a living-quarters building through the approved concept-art workflow, using the same in-game building style and same upgrade strategy as the other Base facilities while keeping its palette and placement distinct.

### Current Status

- The approved living-quarters concept image has been copied into `Resources/LivingQuarters/LivingQuartersReferenceCutout` and processed into a transparent reference-textured world model. Done.
- A matching blurred silhouette texture exists at `Resources/LivingQuarters/LivingQuartersReferenceGlowSilhouette` for completion auras. Done.
- The Base scene creates living quarters in a separated rear-left residential slot away from HQ, bio lab, hangar, and training. Done.
- Living quarters use a distinct graphite/ivory/warm-window/coral-magenta palette instead of reusing the HQ, lab, hangar, or training color scheme. Done.
- The occupied living-quarters pad remains a logical map slot but no longer draws a slab or separate pad label. Done.
- Tapping living quarters reveals the same flat 2D green/grey popup upgrade arrow rule as the bio lab, hangar, and training facility. Done.
- Clicking the green symbol starts a saved living-quarters upgrade timer, spends the shared upgrade cost, hides the symbol, and shows a circular progress icon above the building. Done.
- Living-quarters upgrades reuse the same cost scale and duration curve: `50 + level * 25`, with durations `1s`, `3s`, `10s`, `60s`, then `x5` each further level. Done.
- Completed living-quarters upgrades increase the saved building level, clear the timer, pop the building, play a unique generated living-quarters finish sound, show a five-second pulsing silhouette aura, and grow the visible reference model only slightly taller. Done.
- Tapping living quarters, another building, empty map space, or HUD action participates in the shared popup-arrow dismissal rule so only one building arrow can remain visible. Done.
- The expandable Credits panel now includes living-quarters level and upgrade status. Done.

### Acceptance Criteria

- Living quarters match the approved concept image through a reference-textured world quad. Done.
- Living quarters are visibly separated from HQ, bio lab, hangar, and training. Done.
- Living quarters use a distinct residential coral/magenta color scheme. Done.
- Living quarters use the same green/grey 2D popup-arrow upgrade affordance as the other buildings. Done.
- Living quarters use the same saved timer, cost, duration, circular progress, height-only growth, and completion glow strategy as the other same-rule buildings, with their own completion-sound profile. Done.
- `PLAN.md`, affected READMEs, EditMode tests, and PlayMode tests are updated for living quarters. Done.

### Validation

- EditMode progression coverage checks living-quarters upgrade cost, duration, start, progress, completion, malformed timer repair, reset defaults, and save/load persistence. Done.
- PlayMode coverage checks living-quarters reference-textured model loading, separated placement, hidden occupied pad renderer/label, distinct tint, zero-thickness flat 2D arrow mesh generation, saved timer start, circular progress fill, height-only visual leveling, shared popup dismissal, unique sound profile, and completion pop/glow/sound feedback. Done.
- Latest automated verification: Unity EditMode `85/85`, Unity PlayMode `30/30`, focused living-quarters visual capture `1/1`, and clean `git diff --check` from `/private/tmp/4xgame-living-quarters`. Done.
- Visual proof captured at `/private/tmp/4xgame-living-quarters/Logs/living-quarters-glow.gif`, with still frames confirming the living-quarters building is separated and the coral completion aura pulses around the building outline. Done.

## Future Local V7: Human Population, Gate Intake, And Role Training

### Goal

Turn rescued humans into the foundation of base growth and future staffing requirements.

### Proposed Features

- Saved humans increase the number of humans living in the base.
- Rescued humans can arrive at the base gates after missions.
- The player can let rescued humans into the base so they become members.
- Base members can be trained into roles such as soldiers, engineers, workers, and specialists.
- Soldiers can feed squad strength, while engineers and workers become prerequisites for construction, research, vehicles, and advanced weapons.

### Acceptance Criteria

- The save file persists total humans plus trained role counts.
- A mission reward path can add rescued humans without requiring chemical systems yet.
- Training spends or reserves humans in a clear local-only way.

## Future Local V8: Truck Expeditions, Terrain Discovery, And Resource Harvesting

### Goal

Add the first world-facing economy loop that discovers terrain and gathers materials needed by later systems.

### Proposed Features

- Resource harvesting is done with trucks sent from the base.
- Trucks are specialized by chosen resource type, such as metal ore or uranium.
- Trucks discover terrain along their travel path.
- When a truck finds its chosen resource type, extraction starts automatically.
- A working truck returns to the base, unloads through a special base opening, and returns to the resource site.
- The automatic harvest, return, unload, and repeat loop continues until the resource is exhausted.

### Acceptance Criteria

- The player only needs to choose and send a truck type.
- Terrain discovery, resource site state, truck travel state, and stored resources persist locally.
- Metals become available as a prerequisite for base defenses, drones, vehicles, and heavier weapons.

## Future Local V9: Base Defense, Walls, Moats, And Horde Pressure

### Goal

Make the base vulnerable so construction, staffing, and resource gathering have a defensive purpose.

### Proposed Features

- Enemy hordes can periodically attack the base.
- The player can construct walls, moats, and other physical obstructions.
- Defenses require harvested resources and trained workers or engineers.
- Horde attacks can damage or breach defenses.
- Base defense results affect resources, population safety, and future risk.

### Acceptance Criteria

- A basic horde attack can resolve locally without backend systems.
- Constructed defenses visibly change the base.
- Existing base progression remains usable even if a defense event is pending.

## Future Local V10: Squad Groups And Tactical Commands

### Goal

Give combat more tactical control before adding many richer enemy and strike systems.

### Proposed Features

- A squad can be divided into groups.
- Tapping members of a group selects that group.
- Selected groups can focus fire on tapped enemies.
- Groups can be assigned roles or positions such as rear guard and attack.

### Acceptance Criteria

- Group selection and focus fire are readable on mobile.
- Group commands do not break the existing automatic shooting fallback.
- Role assignments produce visible combat behavior differences.

## Future Local V11: Expanded Zombie Types And Physical Counterplay

### Goal

Broaden enemy behavior after the player has more tactical tools and base defenses.

### Proposed Features

- Add irrational foot-soldier zombies that are easy to kill.
- Expand armored zombies beyond the current first prototype type.
- Add rock-throwing zombies.
- Add larger, smarter hero zombies with rudimentary armor.
- Zombies can pile up to overcome walls and threaten helicopters or drones.
- Zombies can use large boulders overhead as drone defense.

### Acceptance Criteria

- Each zombie category has a distinct visual read and gameplay role.
- Zombie pile and boulder counterplay interact with base defenses, drones, or aviation only after those systems exist.
- New enemy types are data-driven enough to support authored missions and horde attacks.

## Future Local V12: Recon, Herd Tracking, And Threat Forecasting

### Goal

Let the player understand threats before battles by finding and tracking enemy herds outside the base.

### Proposed Features

- Recon systems can scout enemy herds before they reach the base.
- Discovered herds appear as world threats with size, distance, and time-to-arrival information.
- Herd tracking can warn the player about upcoming battles or base-defense events.
- Herd records can become targets for later drone, aviation, missile, or bomb systems.

### Acceptance Criteria

- Herd discovery and herd status persist locally.
- Scouted herds change the player's warning time or preparation options in a visible way.
- The player can ignore recon and still play, but with higher risk.

## Future Local V13: Drones And Drone Operators

### Goal

Add the first reusable remote-force system after population, resources, and recon targets exist.

### Proposed Features

- Bases can train drone operators.
- Drones require metals, engineers, workers, fuel, and trained operators.
- Drones can support recon and attack roles.
- Drones can soften up discovered herds before battles or base-defense events.
- Drone herd culling can reduce the risk of the base being overrun.
- Drone losses or repairs create ongoing resource pressure.
- Zombie anti-drone counterplay can include boulder shielding once drone combat exists.

### Acceptance Criteria

- Drone construction and operation use the existing population-role and resource systems.
- Drone missions can target discovered herds or map locations.
- Drone attacks change the later battle or base-defense encounter in a visible way.
- Drone feedback clearly shows success, damage, loss, or return state.

## Future Local V14: Aviation, Hangars, And Air Missions

### Goal

Expand remote-force play from drones into heavier aircraft once fuel, staffing, and recon loops are proven.

### Proposed Features

- Aviation can include fighters, bombers, recon aircraft, and helicopters.
- Helicopters can support attack and recon.
- Recon aviation can scout enemy herds before they reach the base.
- Attack aircraft can soften herds before battles or base-defense events.
- Aircraft require appropriate base structures, staff, fuel, and materials.

### Acceptance Criteria

- Aircraft roles are distinct from drone roles.
- Air missions target discovered world threats or unexplored areas.
- Horde and zombie counterplay can threaten aircraft without making them useless.

## Future Local V15: Satellites And Strategic Intelligence

### Goal

Add a late intelligence layer that expands world discovery once the map, resources, enemy facilities, and herd systems exist.

### Proposed Features

- Satellites can discover world resources.
- Satellites can discover enemy facilities.
- Satellites can discover enemy herds.
- Satellite intelligence can improve truck expedition choices, recon planning, and strike targeting.

### Acceptance Criteria

- Satellites augment discovery but do not replace truck expeditions or recon aviation.
- Satellite results create actionable map information.
- Enemy facilities become visible as future targets or threat sources.

## Future Local V16: Chemical Research And Dezombification

### Goal

Add the high-consequence rescue system after population, combat targeting, drones, and strike delivery are already understandable.

### Proposed Features

- A base can build chemical warfare research labs.
- Research labs let the player design chemical bombs or chemical drones.
- Chemical weapons can slowly dezombify zombies.
- Chemical potency is directly proportional to the time required for a zombie to become human again.
- Restored humans stop in shock, look at their hands and feet, then panic when they see zombies.
- Zombies attack restored humans.
- If the squad kills restored humans, the player loses coins.
- Saved restored humans grant more credits and can become base members.

### Acceptance Criteria

- Dezombified humans have a distinct state from zombies and squad members.
- Combat targeting prevents accidental rules from feeling unfair, while still making rescued humans vulnerable.
- The rescue, credit, penalty, and base-population outcomes are visible at the end of a round.

## Future Local V17: Heavy Weapons, Strategic Weapons, And Endgame Escalation

### Goal

Reserve the most destructive and resource-intensive systems for late game, after the economy, intelligence, targeting, and enemy-threat loops are established.

### Proposed Features

- Weapons can progress from small arms to howitzers, rocket artillery, tanks, and other heavy systems.
- Late-game superweapons can include earthquake triggers and weather weaponization.
- Atomic bombs require harvested uranium, engineers, and launch vehicles.
- Nuclear launch vehicles can include submarines, airplane bombers, or ground-launched ballistic missiles.
- Strategic weapons should have major costs, target requirements, and consequences.

### Acceptance Criteria

- Heavy weapons require resources, staffing, and discovered targets.
- Strategic weapons do not trivialize base defense, herd management, or rescue systems.
- Nuclear and superweapon systems remain optional late-game escalation rather than core early progression.
