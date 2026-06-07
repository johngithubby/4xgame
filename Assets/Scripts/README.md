# Scripts

## Files

- `LaneSurvivor.Runtime.asmdef`: Declares the runtime assembly for gameplay, data, and UI scripts so tests and editor tools can reference prototype code explicitly.
- `Base/`: Contains the Phase 2 base scene bootstrap, HUD controller, and HQ placeholder component.
- `Data/`: Contains level definitions and gate modifier data.
- `Economy/`: Contains local resource wallet helpers.
- `Editor/`: Contains editor-only scene building tools.
- `Gameplay/`: Contains minigame controllers, player squad behavior, gates, zombies, shooting, and runtime bootstrap code.
- `Heroes/`: Contains local hero definitions, inventory helpers, and gameplay reward rules.
- `Progression/`: Contains HQ upgrade, timer, mission selection, and mission unlock rules that connect base progress to minigame runs.
- `Rendering/`: Contains shared placeholder material and collider-free geometry helpers for runtime-generated scenes.
- `Save/`: Contains local JSON save data and persistence helpers.
- `UI/`: Contains HUD and end-screen controllers.

## Compile

Unity compiles runtime scripts through the `LaneSurvivor.Runtime` assembly. Editor-only scripts compile through the `LaneSurvivor.Editor` assembly in `Editor/`.

## Behavior

The runtime assembly owns the local Phase 1 minigame prototype, Phase 2 base-building slice, first Phase 3 hero slice, and local v2 mission progression. Editor tooling references it only to rebuild or configure development scenes.
