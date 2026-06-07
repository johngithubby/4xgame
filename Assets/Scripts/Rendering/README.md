# Rendering Scripts

## Files

- `PrototypeGeometryFactory.cs`: Creates collider-free placeholder cube meshes and flat road planes for runtime-generated scenes, avoiding Unity's primitive collider path in iOS simulator builds.
- `PrototypeMaterialFactory.cs`: Creates tinted opaque depth-writing placeholder materials for runtime-generated prototype geometry by probing a short list of common 3D Unity shaders and avoiding transparent UI/sprite fallbacks.

## Compile

Unity compiles this directory through the main `LaneSurvivor.Runtime` assembly.

## Behavior

Runtime bootstrap code uses these factories for cubes, flat track surfaces, gates, zombies, shot tracers, and player placeholders. The geometry helper creates meshes directly so gameplay code does not require physics colliders or `GameObject.CreatePrimitive` at runtime, and the track is a surface rather than a solid slab so it cannot cover actors. Prototype materials render in the opaque geometry queue with depth writes enabled when the active shader exposes the relevant controls, preventing the generated road from transparent-sorting over distant gates in iOS Simulator builds.
