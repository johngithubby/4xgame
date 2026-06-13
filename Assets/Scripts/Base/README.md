# Base Scripts

## Files

- `BaseHudController.cs`: Displays local credits in an expandable button, shows coins/HQ/upgrade status inside the expanded detail panel, keeps owned hero level/XP and equipped hero state visible, and exposes collect, upgrade, daily objective claim, direct mission selection, play, heroes, equip hero, zoom, and development reset button actions.
- `BaseCameraController.cs`: Controls Base scene zoom through on-screen buttons, mouse wheel, keyboard shortcuts, and two-finger pinch, and pans the Base map through bounded mouse/one-finger dragging while ignoring HUD-origin gestures.
- `BaseSceneBootstrap.cs`: Builds the Phase 2/V6 base scene at runtime, loads local progress, rolls over the local daily objective, completes ready HQ upgrades, grants HQ milestone hero rewards, creates an oversized unoutlined base floor, future gate/resource/lab/hangar/training reserved spaces with the training pad offset away from the HQ for visibility, lays out a phone-sized HUD with an expandable credits control, direct mission buttons, and zoom buttons, saves direct mission button selection and daily objective claim changes, shows base feedback messages, and navigates to the minigame or Hero scene.
- `HQBuilding.cs`: Mirrors saved HQ level onto the restored pentagon HQ body and world-space label, grows the HQ by small per-upgrade steps, darkens its body color slowly from white toward black, switches label contrast for readability, and adds generated side-detail rows as levels increase.

## Compile

Unity compiles these files as part of the `LaneSurvivor.Runtime` assembly.

## Behavior

The base scene is local-only. It lets the player collect coins, spend coins on a timed HQ upgrade, expand the Credits button to inspect coins, HQ level, and upgrade status, claim a completed daily objective, persist progress locally, select among unlocked missions with numbered buttons, inspect the restored pentagon HQ on an unoutlined base floor, zoom and drag the world camera without moving overlay HUD controls, see upgrade-complete feedback alongside any milestone hero reward, see the minigame coin/hero/mission reward hint, view owned hero levels and XP, cycle equipped heroes, open the dedicated Hero screen, reset local progress in editor/development builds, and launch the minigame with the selected mission plus HQ-derived and hero-derived bonuses. The HUD uses a narrow mobile reference resolution so the placeholder controls fit better on iPhone-like screens, keeps the former left-side white status stack collapsed behind the Credits button, and generated placeholder geometry uses shared material creation so iOS player builds do not depend on one specific shader name.
