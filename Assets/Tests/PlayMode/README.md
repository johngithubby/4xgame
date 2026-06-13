# PlayMode Tests

## Files

- `LaneSurvivor.Tests.PlayMode.asmdef`: Declares the PlayMode test assembly so Unity Test Runner can run scene smoke tests.
- `BaseSceneFlowTests.cs`: Loads runtime-built scenes, verifies Base collect/upgrade persistence, expandable Credits button details, local daily objective claim persistence, mission button selection persistence, locked mission rejection, completed-upgrade feedback, checks the removed left-side Base status labels stay absent, checks the V6 unoutlined Base floor and restored pentagon HQ geometry, HQ level size/color/detail readability, verifies the V6-1 reference-textured bio lab with visible `Resources/BioLab/BioLabReferenceCutout` model, matching `Resources/BioLab/BioLabReferenceGlowSilhouette` blurred completion aura, hidden procedural scaffold renderers, hidden occupied lab pad renderer, no lab-pad label, grey/green upgrade symbol affordance, saved bio-lab timer start, circular progress fill, height-only bio-lab visual leveling without exterior add-ons, pulsating completion silhouette aura/pop/sound trigger, Base future-space placeholders, Base zoom controls, and Base map dragging, invokes Base-to-Minigame and Base-to-Heroes navigation, checks Minigame HUD edge labels stay inside the canvas with state text anchored upper-right, checks Minigame start movement, lane buttons, generated survivor/zombie runtime body parts with connected knee/shin hierarchy, runtime survivor rifle/shotgun/SMG muzzle anchors, runtime eye-level and hip-fire weapon heights, runtime humanoid animator rig setup, world-space depth-safe muzzle-aligned combat feedback spawning, restart, rewards, mission unlocks, mission completion at the authored cap, and end-screen return to Base, checks Hero screen header/list layout stays inside the canvas without overlap, and checks Hero screen level-up/equip actions persist local hero state.

## Compile

Unity compiles and runs these tests through the `LaneSurvivor.Tests.PlayMode` Unity Test Framework assembly.

## Behavior

The tests cover high-level scene wiring and UI button callbacks that need Play Mode, while detailed gameplay and progression rules stay in EditMode tests.
