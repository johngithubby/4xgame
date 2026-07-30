# UI Scripts

## Files

- `EndScreenController.cs`: Shows the win/loss result panel, optional win reward text, restart button, and base return button.
- `MinigameHudController.cs`: Displays squad count, level progress, current level state, the start button, and lane-change buttons.
- `PlayerSquadScreenMarker.cs`: Provides an optional compact cyan/magenta overlay marker under a required canvas `RectTransform` that follows the gameplay squad transform through the world camera projection when the fallback marker flag is enabled.

## Compile

Unity compiles these files automatically as part of the main runtime assembly and uses the `com.unity.ugui` package for UI components.

## Behavior

The UI is simple and local-only. It reflects gameplay state from `LevelManager` and `PlayerSquad` without storing gameplay data itself. The player marker code remains available as a simulator-visibility fallback, but the current minigame uses visible world-space 3D soldier rigs as the primary player representation.
