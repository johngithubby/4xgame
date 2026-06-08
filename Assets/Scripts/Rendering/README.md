# Rendering Scripts

## Files

- `PrototypeCharacterFactory.cs`: Builds collider-free survivor squad and zombie character hierarchies from generated rounded primitives, including human heads, torsos, connected shoulder/hip/knee/ankle limb chains, high-contrast zombie leg materials, distinct rifle/shotgun/SMG survivor weapon profiles with muzzle anchors, zombie faces, wounds, and armored zombie gear.
- `PrototypeGeometryFactory.cs`: Creates collider-free placeholder cube meshes, rounded sphere meshes, cylinder meshes, and flat road planes for runtime-generated scenes, avoiding Unity's primitive collider path in iOS simulator builds.
- `PrototypeHumanoidAnimationStyle.cs`: Names the procedural animation profiles used by generated survivor and zombie rigs.
- `PrototypeHumanoidAnimator.cs`: Caches generated humanoid joint-chain transforms and applies procedural run/shamble poses with hip swing, connected knee flex, foot pitch, body bob, body roll, head nod, and a small held-weapon pulse.
- `PrototypeMaterialFactory.cs`: Creates tinted opaque depth-writing placeholder materials for runtime-generated prototype geometry, always-visible transparent text materials, and always-visible solid tracer materials for world-space combat feedback by probing a short list of common Unity shaders.

## Compile

Unity compiles this directory through the main `LaneSurvivor.Runtime` assembly.

## Behavior

Runtime bootstrap code uses these factories for flat track surfaces, gate cards, humanoid player and zombie characters, survivor weapon profiles, shot tracers, feedback labels, and remaining simple prototype objects. The geometry helper creates meshes directly so gameplay code does not require physics colliders or `GameObject.CreatePrimitive` at runtime, and the track is a surface rather than a solid slab so it cannot cover actors. The character helper composes shared sphere and cylinder meshes into multi-part bodies so the minigame reads as people fighting zombies rather than blocks colliding with blocks. Survivor weapons are generated from the same collider-free primitive helpers, parented under the right-hand chain, and given direct `Weapon Muzzle` anchors at their visible barrel tips. The humanoid animator layers a sine-wave run cycle over those generated poses: survivors run only while their gameplay root is moving, while zombies shamble in place because level rules keep them stationary until contact. Thighs, knees, shins, and feet now form connected transform chains, so knee flex bends the shin and boot from the joint without opening visual gaps. Zombie leg segments use a pale dirty-gray material so their shamble remains visible against the dark track and through compressed simulator GIFs. Prototype geometry materials render in the opaque geometry queue with depth writes enabled when the active shader exposes the relevant controls, preventing the generated road from transparent-sorting over distant gates in iOS Simulator builds. Combat feedback materials render late in the overlay queue, disable depth writes, and request an always-pass depth test when the shader exposes it so world-space tracers and labels stay readable on iOS without becoming HUD-only effects.
