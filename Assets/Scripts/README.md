# Scripts

## Files

- `LaneSurvivor.Runtime.asmdef`: Declares the runtime assembly for gameplay, data, and UI scripts so tests and editor tools can reference prototype code explicitly.
- `Data/`: Contains level definitions and gate modifier data.
- `Editor/`: Contains editor-only scene building tools.
- `Gameplay/`: Contains minigame controllers, player squad behavior, gates, zombies, shooting, and runtime bootstrap code.
- `UI/`: Contains HUD and end-screen controllers.

## Compile

Unity compiles runtime scripts through the `LaneSurvivor.Runtime` assembly. Editor-only scripts compile through the `LaneSurvivor.Editor` assembly in `Editor/`.

## Behavior

The runtime assembly owns the local Phase 1 minigame prototype. Editor tooling references it only to rebuild or configure the development scene.
