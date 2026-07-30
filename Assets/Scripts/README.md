# Scripts

## Files

- `LaneSurvivor.Runtime.asmdef`: Declares the runtime assembly for gameplay, data, UI, imported Animator support, and generated audio feedback scripts so tests and editor tools can reference prototype code explicitly.
- `Base/`: Contains the Phase 2 base scene bootstrap, HUD controller, camera zoom/drag controller, level-reactive HQ visual component, and V6-1 reference-textured bio-lab upgrade component.
- `Data/`: Contains level definitions, gate modifier data, and zombie enemy type data.
- `Economy/`: Contains local resource wallet helpers.
- `Editor/`: Contains editor-only scene building tools.
- `Gameplay/`: Contains minigame controllers, player squad behavior, weapon muzzle registration, gates, zombies, shooting, muzzle-flash and tracer feedback, and runtime bootstrap code.
- `Heroes/`: Contains local hero definitions, inventory helpers, and gameplay reward rules.
- `Progression/`: Contains HQ upgrade, bio-lab upgrade, timer, mission selection, and mission unlock rules that connect base progress to minigame runs.
- `Rendering/`: Contains shared placeholder and textured-cutout material helpers, collider-free geometry/prism/card helpers, explicit PBR material binding and authored Animator switching for the licensed SWAT leader, generated wing-survivor animation rigs and camera-facing decals, hidden survivor weapon anchors, and generated zombie character construction for runtime-generated scenes.
- `Retention/`: Contains local daily objective rules that count minigame wins and pay claimable coin rewards.
- `Save/`: Contains local JSON save data and persistence helpers.
- `UI/`: Contains HUD and end-screen controllers.

## Compile

Unity compiles runtime scripts through the `LaneSurvivor.Runtime` assembly. Editor-only scripts compile through the `LaneSurvivor.Editor` assembly in `Editor/`.

## Behavior

The runtime assembly owns the local Phase 1 minigame prototype, including the PBR-textured authored-Animator SWAT leader, generated wing survivors, hidden weapon/muzzle anchors, generated zombie rigs, muzzle-attached flash/tracer feedback, Phase 2 base-building slice with V6 Base readability/zoom/drag visuals and the V6-1 reference-textured bio-lab upgrade, first Phase 3 hero slice, local v2 mission progression, and local daily objective retention slice. Editor tooling references it only to rebuild or configure development scenes.
