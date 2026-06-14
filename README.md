# 4xgame

This repository contains a small Unity iOS-first mobile game prototype. Phase 1 is an original, local-only zombie lane shooter prototype using placeholder primitives and simple Unity UI.

## Engine

- Unity `6000.4.9f1`
- iOS Build Support is expected for later device/Xcode builds.
- No paid assets, ads, monetization, backend, or copyrighted game assets are used.

## Phase 1

- Open the project root in Unity.
- Open `Assets/Scenes/Minigame.unity`.
- Press Play.
- The squad auto-starts after a short delay, moves forward, changes lanes with left/right input, applies gates in the matching lane, shoots humanoid zombies in the current lane, and reaches a win or loss state.
- Lane input works with the on-screen arrow buttons, keyboard `A/D`, keyboard arrow keys, or tapping/clicking the left or right third of the screen.
- Local mission progression unlocks additional minigame layouts with tougher lane pacing.
- Automatic shots now show placeholder tracers and damage text.
- The minigame now renders the player as a three-survivor formation with generated heads, bodies, limbs, helmets, and gear instead of rectangular blobs.
- The three survivors now hold distinct generated weapons: leader rifle, left-wing shotgun, and right-wing SMG.
- Rifle and shotgun survivors keep their weapons near eye level while the SMG survivor fires from the hip, with firing arms stabilized so muzzle positions do not bob during running.
- Automatic shot tracers now start from registered weapon muzzle anchors when generated weapons are present, with the old root-derived fallback kept for hand-built test scenes.
- Zombies now render as generated humanoid bodies with faces, reaching arms, legs, feet, wounds, and extra armor pieces for armored enemies.
- Survivors now use a procedural bent-knee walking cycle while moving, and zombies use a slower in-place shamble.
- The minigame chase camera widens its vertical FOV on extra-tall portrait devices so side lanes stay readable across iOS aspect ratios.
- The minigame state label is anchored upper-right so simulator captures do not hide it under iPhone Dynamic Island overlays.

## Phase 2

- Open `Assets/Scenes/Base.unity` to try the first base-building slice.
- The base scene shows a draggable unoutlined base floor with a generated reference-matched pentagon HQ, a generated reference-matched upgradeable bio lab, an expandable Credits button for coins/HQ/bio-lab upgrade status, collect, HQ upgrade, direct mission buttons, a daily objective claim button, world zoom controls, and play controls.
- The Base HUD uses a narrow mobile reference layout for the placeholder controls.
- HQ starts at level 1.
- Collect grants local coins.
- Upgrade HQ spends coins and starts a persisted local timer from either the HUD button or the flat 2D popup arrow revealed by tapping the HQ building, then shows the same circular world progress fill as the bio lab.
- Completed HQ upgrades increase HQ level.
- HQ upgrades subtly raise the reference-textured HQ by a few pixels without widening, darkening, or adding generated detail rows.
- Base shows feedback when an HQ upgrade completes, including a pulsating blurred aura made from the same HQ silhouette.
- The bio lab model uses the generated concept image directly as a reference-textured world model, with the old primitive tower kept hidden only as upgrade/click fallback scaffolding, no visible lab-pad slab, and no separate lab-pad label.
- Tapping the bio lab reveals a green or grey flat 2D upgrade symbol based on local credits, starts a saved timer when affordable, shows a circular progress fill, raises the lab height by a few pixels on completion without exterior add-ons, and plays local pop/sound feedback with a pulsating blurred aura made from the same building silhouette.
- Tapping another building, empty map space, or HUD action dismisses any visible HQ or bio-lab upgrade arrow.
- The Base layout reserves visible future space for gates, resource drop-off, labs, hangars, and training areas without drawing a base border.
- The Base map can be dragged for inspection while overlay HUD controls stay fixed.
- HQ levels above 1 add a visible starting squad bonus in the minigame.
- Winning a mission marks it complete, and winning the highest unlocked mission unlocks the next local mission up to mission 8.
- Winning the minigame grants a local coin reward and shows the reward on the completion screen.
- Winning the minigame also advances a local daily objective; completing two wins in the current UTC day unlocks a claimable local coin reward on the Base screen.
- Editor and development builds show a local save reset button for quick prototype iteration.
- The minigame end screen has a `BASE` button to return to the base scene.

## Phase 3

- Winning the minigame can grant the first local hero, `Mira Vanguard`.
- Reaching HQ level 2 can grant the second local hero, `Dax Medic`.
- The first hero is auto-equipped and shown in the Base hero panel.
- The Base hero panel lists owned heroes and includes an `EQUIP` button path to cycle equipped heroes.
- The dedicated `Assets/Scenes/Heroes.unity` screen lists owned heroes, cycles equipment, and levels the equipped hero with local coins.
- Minigame wins award XP to the equipped hero, and the Base panel shows hero level/XP.
- Equipped heroes modify minigame starting squad size and damage.
- Hero ownership and equipment persist in the local save.
- Hero rewards are gameplay-only; no paid gacha or loot boxes are used.

## Validation

- EditMode tests live in `Assets/Tests/EditMode`.
- PlayMode scene smoke tests live in `Assets/Tests/PlayMode`.
- Current EditMode test coverage includes Phase 1 gameplay rules, generated survivor weapon/muzzle rules, muzzle-origin shot events, Phase 2 progression/save/mission completion rules, bio-lab upgrade timer rules, the local daily objective, advanced mission content, and Phase 3 hero inventory rules.
- Current EditMode visual coverage includes compact placeholder geometry, disabled depth-unsafe world effects, and responsive minigame camera FOV rules for tall portrait devices.
- Current PlayMode test coverage includes Base collect/upgrade persistence, Credits button expansion, daily objective claiming, mission button selection, locked mission rejection, completed-upgrade feedback, V6 unoutlined Base floor and reference-textured pentagon HQ visuals, HQ height-only leveling and blurred silhouette-aura completion feedback, V6-1 reference-textured bio-lab model loading plus symbol/progress/blurred silhouette-aura completion feedback, Base zoom and drag controls, Base-to-Minigame flow, Minigame HUD edge-label placement, generated runtime weapon muzzle anchors, muzzle-aligned tracer spawning, Minigame start movement, lane-button movement, restart, rewards, mission unlocks, mission cap completion, end-screen return to Base, Base-to-Heroes flow, and Hero screen level-up/equip persistence.
- The scene can be regenerated from Unity with `Lane Survivor/Rebuild Phase 1 Scene`.
- A batch-mode iOS smoke build entry point exists at `LaneSurvivor.Editor.IosSmokeBuild.Run`, including optional simulator SDK export flags for local Simulator launch checks.

## Notes

- See `PLAN.md` for the phased prototype roadmap and next implementation slices.
- See `LESSONS_LEARNED.md` for Unity batch-mode, Test Runner, autoreview, and Phase 1 implementation lessons from this prototype.
- See `docs/BACKEND_DESIGN.md` for the Phase 4 online design draft.
