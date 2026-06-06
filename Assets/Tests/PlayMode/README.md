# PlayMode Tests

## Files

- `LaneSurvivor.Tests.PlayMode.asmdef`: Declares the PlayMode test assembly so Unity Test Runner can run scene smoke tests.
- `BaseSceneFlowTests.cs`: Loads runtime-built scenes, verifies Base collect/upgrade persistence and completed-upgrade feedback, invokes Base-to-Minigame and Base-to-Heroes navigation, checks Minigame start movement, lane buttons, restart, rewards, and end-screen return to Base, and checks Hero screen level-up/equip actions persist local hero state.

## Compile

Unity compiles and runs these tests through the `LaneSurvivor.Tests.PlayMode` Unity Test Framework assembly.

## Behavior

The tests cover high-level scene wiring and UI button callbacks that need Play Mode, while detailed gameplay and progression rules stay in EditMode tests.
