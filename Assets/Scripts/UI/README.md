# UI Scripts

## Files

- `EndScreenController.cs`: Shows the win/loss result panel and wires the restart button.
- `MinigameHudController.cs`: Displays squad count, level progress, current level state, the start button, and lane-change buttons.

## Compile

Unity compiles these files automatically as part of the main runtime assembly and uses the `com.unity.ugui` package for UI components.

## Behavior

The UI is simple and local-only. It reflects gameplay state from `LevelManager` and `PlayerSquad` without storing gameplay data itself.
