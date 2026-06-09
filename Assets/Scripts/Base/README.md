# Base Scripts

## Files

- `BaseHudController.cs`: Displays local base state, an eight-row mission panel with selected/done/locked status and reward hints, local daily objective progress, owned hero level/XP panel, equipped hero state, and exposes collect, upgrade, daily objective claim, direct mission selection, play, heroes, equip hero, and development reset button actions.
- `BaseCameraController.cs`: Controls Base scene zoom through on-screen buttons, mouse wheel, keyboard shortcuts, and two-finger pinch, and pans the Base map through bounded mouse/one-finger dragging while ignoring HUD-origin gestures.
- `BaseSceneBootstrap.cs`: Builds the Phase 2/V6 base scene at runtime, loads local progress, rolls over the local daily objective, completes ready HQ upgrades, grants HQ milestone hero rewards, creates the pentagon base footprint, future wall/moat/opening/lab/hangar/training reserved spaces, lays out a phone-sized HUD with anchor-matched text pivots and zoom buttons, saves direct mission button selection and daily objective claim changes, shows base feedback messages, and navigates to the minigame or Hero scene.
- `HQBuilding.cs`: Mirrors saved HQ level onto the pentagon HQ body and world-space label, grows the HQ with each level, darkens its body color from white toward black, switches label contrast for readability, and adds more generated side-detail rows as levels increase.

## Compile

Unity compiles these files as part of the `LaneSurvivor.Runtime` assembly.

## Behavior

The base scene is local-only. It lets the player collect coins, spend coins on a timed HQ upgrade, claim a completed daily objective, persist progress locally, select among unlocked missions with numbered buttons, see all authored missions with `SELECTED`, `DONE`, or `LOCKED` status plus reward/unlock hints, inspect a pentagon-shaped base/HQ, zoom and drag the world camera without moving overlay HUD controls, see upgrade-complete feedback alongside any milestone hero reward, see the minigame coin/hero/mission reward hint, view owned hero levels and XP, cycle equipped heroes, open the dedicated Hero screen, reset local progress in editor/development builds, and launch the minigame with the selected mission plus HQ-derived and hero-derived bonuses. The HUD uses a narrow mobile reference resolution so the placeholder controls fit better on iPhone-like screens, matches text pivots to their anchors so top-left labels use true screen insets, and generated placeholder geometry uses shared material creation so iOS player builds do not depend on one specific shader name.
