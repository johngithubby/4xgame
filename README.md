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

## Validation

- EditMode tests live in `Assets/Tests/EditMode`.
- The scene can be regenerated from Unity with `Lane Survivor/Rebuild Phase 1 Scene`.

## Notes

- See `PLAN.md` for the phased prototype roadmap and next implementation slices.
- See `LESSONS_LEARNED.md` for Unity batch-mode, Test Runner, autoreview, and Phase 1 implementation lessons from this prototype.
