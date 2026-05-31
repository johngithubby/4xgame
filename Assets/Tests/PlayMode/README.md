# PlayMode Tests

## Files

- `LaneSurvivor.Tests.PlayMode.asmdef`: Declares the PlayMode test assembly so Unity Test Runner can run scene smoke tests.
- `BaseSceneFlowTests.cs`: Loads the Base scene, verifies its runtime-built objects, invokes the Play button, and confirms the Minigame scene loads.

## Compile

Unity compiles and runs these tests through the `LaneSurvivor.Tests.PlayMode` Unity Test Framework assembly.

## Behavior

The tests cover high-level scene wiring that needs Play Mode, while detailed gameplay and progression rules stay in EditMode tests.
