# PlayMode Tests

## Files

- `LaneSurvivor.Tests.PlayMode.asmdef`: Declares the PlayMode test assembly so Unity Test Runner can run scene smoke tests.
- `BaseSceneFlowTests.cs`: Loads runtime-built scenes and verifies Base, Heroes, and Minigame navigation, persistence, layout, interaction, upgrades, and feedback. Minigame coverage includes the enabled skinned SWAT leader, explicit suit albedo/normal inputs, authored Animator/controller setup, live full-body controller-clock, mapped leg swing, rendered skin deformation, independently bounded left/right boot yaw against lane travel, no hip-centreline crossing, and measured foot lift during each forward swing so neither boot can drag along the road, plus disabled legacy leader cards and facing switcher, retained wing-survivor decals, hidden generated muzzle carriers, lane motion, combat feedback, mission completion, restart, and return to Base.

## Compile

Unity compiles and runs these tests through the `LaneSurvivor.Tests.PlayMode` Unity Test Framework assembly.

## Behavior

The tests cover high-level scene wiring and UI button callbacks that need Play Mode, while detailed gameplay and progression rules stay in EditMode tests.
