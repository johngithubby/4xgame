# Scripts

## Files

- `LaneSurvivor.Runtime.asmdef`: Declares the runtime assembly for gameplay, data, and UI scripts so tests and editor tools can reference prototype code explicitly.
- `Base/`: Contains the Phase 2 base scene bootstrap, HUD controller, camera zoom/drag controller, and level-reactive HQ visual component.
- `Data/`: Contains level definitions, gate modifier data, and zombie enemy type data.
- `Economy/`: Contains local resource wallet helpers.
- `Editor/`: Contains editor-only scene building tools.
- `Gameplay/`: Contains minigame controllers, player squad behavior, weapon muzzle registration, gates, zombies, shooting, tracer feedback, and runtime bootstrap code.
- `Heroes/`: Contains local hero definitions, inventory helpers, and gameplay reward rules.
- `Progression/`: Contains HQ upgrade, timer, mission selection, and mission unlock rules that connect base progress to minigame runs.
- `Rendering/`: Contains shared placeholder material, collider-free geometry and prism helpers, generated humanoid character construction, and generated survivor weapon profiles for runtime-generated scenes.
- `Retention/`: Contains local daily objective rules that count minigame wins and pay claimable coin rewards.
- `Save/`: Contains local JSON save data and persistence helpers.
- `UI/`: Contains HUD and end-screen controllers.

## Compile

Unity compiles runtime scripts through the `LaneSurvivor.Runtime` assembly. Editor-only scripts compile through the `LaneSurvivor.Editor` assembly in `Editor/`.

## Behavior

The runtime assembly owns the local Phase 1 minigame prototype, generated survivor weapon/muzzle visuals, Phase 2 base-building slice with V6 Base readability/zoom/drag visuals, first Phase 3 hero slice, local v2 mission progression, and local daily objective retention slice. Editor tooling references it only to rebuild or configure development scenes.
