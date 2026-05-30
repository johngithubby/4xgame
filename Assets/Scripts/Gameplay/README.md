# Gameplay Scripts

## Files

- `AutoShooter.cs`: Selects the nearest zombie ahead of the squad and applies automatic damage on a timer.
- `FloatingFeedback.cs`: Animates short world-space text feedback for gate and zombie outcomes.
- `Gate.cs`: Applies a configured gate modifier once when the player squad reaches the gate position.
- `LevelManager.cs`: Owns the Phase 1 level flow, runtime level spawning, win/loss state, and scene restart.
- `LevelState.cs`: Defines the minigame states used by gameplay and UI.
- `LevelStateEvaluator.cs`: Contains small testable rules for win/loss evaluation.
- `PhaseOneRuntimeBootstrap.cs`: Builds the playable Phase 1 prototype from a minimal scene at runtime.
- `PlayerSquad.cs`: Moves the squad forward, tracks squad count, tracks damage, and reports defeat.
- `SimpleCameraFollow.cs`: Keeps the camera following the moving squad with a fixed offset.
- `SquadLaneInput.cs`: Reads keyboard, pointer, touch, and on-screen button input to move the squad between lanes while ignoring raw pointer gestures over UI buttons.
- `Zombie.cs`: Tracks zombie health, defeat state, and squad loss when the squad reaches an undefeated zombie.

## Compile

Unity compiles these files automatically as part of the main runtime assembly.

## Behavior

The gameplay loop is intentionally small: the squad starts, moves forward, lane input chooses which gates and zombies matter, automatic shooting defeats zombies in the current lane, visible feedback explains outcomes, and the level ends when the squad reaches the finish or reaches zero members.
