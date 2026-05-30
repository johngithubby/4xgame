# Scripts

## Files

- `LaneSurvivor.Runtime.asmdef`: Declares the runtime assembly for gameplay, data, and UI scripts so tests and editor tools can reference prototype code explicitly.
- `Base/`: Contains the Phase 2 base scene bootstrap, HUD controller, and HQ placeholder component.
- `Data/`: Contains level definitions and gate modifier data.
- `Economy/`: Contains local resource wallet helpers.
- `Editor/`: Contains editor-only scene building tools.
- `Gameplay/`: Contains minigame controllers, player squad behavior, gates, zombies, shooting, and runtime bootstrap code.
- `Progression/`: Contains HQ upgrade and timer rules that connect base progress to minigame bonuses.
- `Save/`: Contains local JSON save data and persistence helpers.
- `UI/`: Contains HUD and end-screen controllers.

## Compile

Unity compiles runtime scripts through the `LaneSurvivor.Runtime` assembly. Editor-only scripts compile through the `LaneSurvivor.Editor` assembly in `Editor/`.

## Behavior

The runtime assembly owns the local Phase 1 minigame prototype and Phase 2 base-building slice. Editor tooling references it only to rebuild or configure development scenes.
