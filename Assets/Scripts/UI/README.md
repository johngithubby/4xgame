# UI Scripts

## Files

- `EndScreenController.cs`: Shows the win/loss result panel, optional win reward text, restart button, and base return button.
- `MinigameHudController.cs`: Displays squad count, level progress, current level state, the start button, and lane-change buttons.
- `PlayerSquadScreenMarker.cs`: Renders a compact cyan/magenta overlay marker under a required canvas `RectTransform` that follows the gameplay squad transform through the world camera projection.

## Compile

Unity compiles these files automatically as part of the main runtime assembly and uses the `com.unity.ugui` package for UI components.

## Behavior

The UI is simple and local-only. It reflects gameplay state from `LevelManager` and `PlayerSquad` without storing gameplay data itself. The player marker is drawn in the overlay canvas so road depth, gates, labels, and temporary world effects cannot hide the squad during iOS Simulator runs.
