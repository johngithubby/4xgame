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
- The squad auto-starts after a short delay, moves forward, changes lanes with left/right input, applies gates in the matching lane, shoots zombies in the current lane, and reaches a win or loss state.
- Lane input works with the on-screen arrow buttons, keyboard `A/D`, keyboard arrow keys, or tapping/clicking the left or right third of the screen.

## Phase 2

- Open `Assets/Scenes/Base.unity` to try the first base-building slice.
- The base scene shows a placeholder HQ building, local coins, collect, HQ upgrade, and play controls.
- HQ starts at level 1.
- Collect grants local coins.
- Upgrade HQ spends coins and starts a persisted local timer.
- Completed HQ upgrades increase HQ level.
- Base shows feedback when an HQ upgrade completes.
- HQ levels above 1 add a visible starting squad bonus in the minigame.
- Winning the minigame grants a local coin reward and shows the reward on the completion screen.
- Editor and development builds show a local save reset button for quick prototype iteration.
- The minigame end screen has a `BASE` button to return to the base scene.

## Validation

- EditMode tests live in `Assets/Tests/EditMode`.
- Current EditMode test coverage includes Phase 1 gameplay rules and Phase 2 progression/save rules.
- The scene can be regenerated from Unity with `Lane Survivor/Rebuild Phase 1 Scene`.

## Notes

- See `PLAN.md` for the phased prototype roadmap and next implementation slices.
- See `LESSONS_LEARNED.md` for Unity batch-mode, Test Runner, autoreview, and Phase 1 implementation lessons from this prototype.
