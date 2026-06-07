# Rendering Scripts

## Files

- `PrototypeGeometryFactory.cs`: Creates collider-free placeholder cube meshes and flat road planes for runtime-generated scenes, avoiding Unity's primitive collider path in iOS simulator builds.
- `PrototypeMaterialFactory.cs`: Creates tinted opaque depth-writing placeholder materials for runtime-generated prototype geometry, always-visible transparent text materials, and always-visible solid tracer materials for world-space combat feedback by probing a short list of common Unity shaders.

## Compile

Unity compiles this directory through the main `LaneSurvivor.Runtime` assembly.

## Behavior

Runtime bootstrap code uses these factories for cubes, flat track surfaces, gates, zombies, shot tracers, feedback labels, and player placeholders. The geometry helper creates meshes directly so gameplay code does not require physics colliders or `GameObject.CreatePrimitive` at runtime, and the track is a surface rather than a solid slab so it cannot cover actors. Prototype geometry materials render in the opaque geometry queue with depth writes enabled when the active shader exposes the relevant controls, preventing the generated road from transparent-sorting over distant gates in iOS Simulator builds. Combat feedback materials render late in the overlay queue, disable depth writes, and request an always-pass depth test when the shader exposes it so world-space tracers and labels stay readable on iOS without becoming HUD-only effects.
