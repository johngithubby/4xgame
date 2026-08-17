# PlayMode Tests

## Files

- `LaneSurvivor.Tests.PlayMode.asmdef`: Declares the PlayMode test assembly so Unity Test Runner can run scene smoke tests.
- `BaseSceneFlowTests.cs`: Loads runtime-built scenes and verifies navigation, persistence, layout, interaction, upgrades, and feedback. Minigame coverage includes three enabled skinned SWAT survivors, distinct serialized dark wardrobe and equipment signatures, disabled legacy cards, shared PBR inputs, three authored Animator/controller bridges with staggered phases and visible barrel registries, full-body gait correction, rigid rifle rendering, muzzle-plane tracer origins, live stock-to-muzzle target alignment, tracer-lifetime aim holds, lane motion, combat feedback, and completion flow. Enemy coverage verifies infected SWAT zombies, shared tattered materials/meshes, varied palettes/tears, hidden firearms, and asymmetric stumble motion.

## Compile

Unity compiles and runs these tests through the `LaneSurvivor.Tests.PlayMode` Unity Test Framework assembly.

## Behavior

The tests cover high-level scene wiring and UI button callbacks that need Play Mode, while detailed gameplay and progression rules stay in EditMode tests.
