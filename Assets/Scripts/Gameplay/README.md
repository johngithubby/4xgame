# Gameplay Scripts

## Files

- `AutoShooter.cs`: Selects the nearest zombie ahead of the squad, applies automatic damage on a timer, and emits shot events for visual feedback.
- `FloatingFeedback.cs`: Animates short world-space text feedback for gate and zombie outcomes when a future depth-safe feedback path re-enables it.
- `GameplayVisuals.cs`: Defines shared prototype visual heights, compact screen-space player marker sizing, disabled world player mesh rendering, inward gameplay lane spacing, disabled low gate footprints, high gate and zombie card dimensions, perspective chase-camera framing, and aspect-responsive camera FOV rules so the track, squad, gates, and zombies remain readable in runtime-generated scenes.
- `Gate.cs`: Applies a configured gate modifier when the player squad reaches the gate center, owns a per-gate material clone for marker tinting while preserving label font materials, then flashes resolved gate visuals briefly enough that the trailing camera cannot let old gates hide the player.
- `LevelDefinitionFactory.cs`: Creates four small runtime mission definitions from local save progress, including readable finish spacing after the last obstacle and mission-specific gate/zombie pacing.
- `LevelManager.cs`: Owns the Phase 1 level flow, collider-free runtime level spawning, disabled world-shot tracer gating, depth-safe feedback gating, win/loss state, local coin, mission completion/unlock, hero unlock, hero XP win reward claim, scene restart, and base return.
- `LevelState.cs`: Defines the minigame states used by gameplay and UI.
- `LevelStateEvaluator.cs`: Contains small testable rules for win/loss evaluation.
- `PhaseOneRuntimeBootstrap.cs`: Builds the playable Phase 1 prototype from a minimal scene at runtime, applies selected mission and HQ/hero save bonuses, creates the invisible gameplay squad anchor, wires the HUD-layer cyan/magenta player marker, lays out edge HUD text with anchor-matched pivots, keeps state text away from iPhone cutouts, and uses shared placeholder material creation for generated geometry.
- `PlayerSquad.cs`: Moves the squad forward, keeps the gameplay transform locked above the road, tracks squad count, tracks damage, and reports defeat.
- `SimpleCameraFollow.cs`: Keeps the perspective camera locked to the moving squad from an angled chase view and refreshes camera FOV from the active screen aspect so fast lane changes, late gates, tall-phone framing, and finish-line motion stay visible without a top-down board view.
- `SquadLaneInput.cs`: Reads keyboard, pointer, touch, and on-screen button input to move the squad between lanes while ignoring raw pointer gestures over UI buttons.
- `Zombie.cs`: Tracks zombie health, defeat state, and squad loss when the squad reaches an undefeated zombie.

## Compile

Unity compiles these files automatically as part of the main runtime assembly.

## Behavior

The gameplay loop is intentionally small: the squad starts, moves forward, lane input chooses which gates and zombies matter, automatic shooting defeats zombies in the current lane, and the level ends when the squad reaches the finish or reaches zero members. The gameplay squad transform drives gate, zombie, shooting, camera, and finish rules, while a HUD-layer marker follows that transform so the visible player cannot be hidden by road depth, gate cards, or temporary world effects in iOS Simulator recordings. Minigame HUD labels match text pivots to their anchors so top-left status text grows inward from its screen inset instead of clipping at the Game view edge, while the transient state label grows inward from the upper-right inset to avoid iPhone Dynamic Island captures. Gate visuals stay in tight portrait-safe gameplay lanes, skip low footprint decals for now, use high single-piece cards for the readable marker so gates do not shape-shift from slabs into arches or rise from underground during a full run, and consume quickly after contact so a passed gate cannot sit between the chase camera and squad. Runtime world-space text feedback and shot tracers are currently disabled because world-depth effects can hide the squad or read as morphing gates in this camera; the next safe version should be a HUD or overlay toast/tracer. Zombies use the same high-card placeholder rule so side-lane threats are visible before the player reaches them. A wider angled chase camera keeps those side-lane markers in view without reverting to an overhead board view, and extra-tall portrait devices receive a controlled vertical-FOV increase so side lanes keep the same horizontal coverage as the reference phone framing. A win grants one local coin reward for that run, marks the cleared mission complete, can unlock the next mission when clearing the highest unlocked mission, can grant the first hero, awards equipped-hero XP, and saves before the player returns to Base. HQ progression still provides squad bonuses, while mission progression selects among four local layouts.
