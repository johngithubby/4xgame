# Base Scripts

## Files

- `BaseHudController.cs`: Displays local base state, selected mission, owned hero level/XP panel, equipped hero state, and exposes collect, upgrade, mission selection, play, heroes, equip hero, and development reset button actions.
- `BaseSceneBootstrap.cs`: Builds the Phase 2 base scene at runtime, loads local progress, completes ready HQ upgrades, grants HQ milestone hero rewards, lays out a phone-sized HUD, saves mission selection changes, shows base feedback messages, and navigates to the minigame or Hero scene.
- `HQBuilding.cs`: Mirrors saved HQ level onto the placeholder HQ cube and world-space label.

## Compile

Unity compiles these files as part of the `LaneSurvivor.Runtime` assembly.

## Behavior

The base scene is local-only. It lets the player collect coins, spend coins on a timed HQ upgrade, persist progress locally, select among unlocked missions, see upgrade-complete feedback alongside any milestone hero reward, see the minigame coin/hero/mission reward hint, view owned hero levels and XP, cycle equipped heroes, open the dedicated Hero screen, reset local progress in editor/development builds, and launch the minigame with the selected mission plus HQ-derived and hero-derived bonuses. The HUD uses a narrow mobile reference resolution so the placeholder controls fit better on iPhone-like screens, and generated placeholder geometry uses shared material creation so iOS player builds do not depend on one specific shader name.
