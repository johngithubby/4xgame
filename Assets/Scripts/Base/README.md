# Base Scripts

## Files

- `BaseHudController.cs`: Displays local base state and exposes collect, upgrade, play, and development reset button actions.
- `BaseSceneBootstrap.cs`: Builds the Phase 2 base scene at runtime, loads local progress, completes ready HQ upgrades, shows base feedback messages, and navigates to the minigame.
- `HQBuilding.cs`: Mirrors saved HQ level onto the placeholder HQ cube and world-space label.

## Compile

Unity compiles these files as part of the `LaneSurvivor.Runtime` assembly.

## Behavior

The base scene is local-only. It lets the player collect coins, spend coins on a timed HQ upgrade, persist progress locally, see the minigame win reward hint, reset local progress in editor/development builds, and launch the minigame with an HQ-derived starting squad bonus.
