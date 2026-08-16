# PlayMode Tests

## Files

- `LaneSurvivor.Tests.PlayMode.asmdef`: Declares the PlayMode test assembly so Unity Test Runner can run scene smoke tests.
- `BaseSceneFlowTests.cs`: Loads runtime-built scenes and verifies Base, Heroes, and Minigame navigation, persistence, layout, interaction, upgrades, and feedback. Minigame coverage includes the enabled skinned SWAT leader, explicit suit albedo/normal inputs, authored Animator/controller setup, live full-body controller-clock, mapped leg swing, rendered skin deformation, independently bounded left/right boot yaw against lane travel, no hip-centreline crossing, measured foot lift during each forward swing, hidden source weapon skins, pivot-local rigid rifle renderers, the tracer origin on the foremost rendered muzzle plane, live imported stock-to-muzzle target alignment throughout the complete tracer lifetime, tracer removal before rifle recovery, and correctly world-sized tracer/flash parenting beneath the scaled FBX hierarchy. Enemy coverage verifies the female SWAT zombie model, infected face and wound attachments, hidden source firearms, unchanged gameplay target root, and live asymmetric visual-root and arm motion. The suite also covers disabled legacy leader cards and facing switcher, retained wing-survivor decals, lane motion, combat feedback, mission completion, restart, and return to Base.

## Compile

Unity compiles and runs these tests through the `LaneSurvivor.Tests.PlayMode` Unity Test Framework assembly.

## Behavior

The tests cover high-level scene wiring and UI button callbacks that need Play Mode, while detailed gameplay and progression rules stay in EditMode tests.
