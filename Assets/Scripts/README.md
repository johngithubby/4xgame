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
- `Rendering/`: Contains shared placeholder/material helpers, collider-free geometry, explicit PBR binding, distinct commander/scout/heavy wardrobe presets, one-third-cycle authored gait staggering for all three licensed SWAT survivors, hidden fallback cards/weapon anchors, and imported zombie character construction.
- `Retention/`: Contains local daily objective rules that count minigame wins and pay claimable coin rewards.
- `Save/`: Contains local JSON save data and persistence helpers.
- `UI/`: Contains HUD and end-screen controllers.

## Compile

Unity compiles runtime scripts through the `LaneSurvivor.Runtime` assembly. Editor-only scripts compile through the `LaneSurvivor.Editor` assembly in `Editor/`.

## Behavior

The runtime assembly owns the local Phase 1 minigame prototype, including the three PBR-textured authored-Animator SWAT survivors, their distinct dark wardrobe/equipment presets and visible barrel anchors, imported zombie rigs, muzzle-attached feedback, the Phase 2 base-building slice, first Phase 3 hero slice, local v2 mission progression, and local daily objective retention slice. Editor tooling references it only to rebuild or configure development scenes.
