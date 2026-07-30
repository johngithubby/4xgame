# Rendering Scripts

## Files

- `PrototypeCharacterFactory.cs`: Builds collider-free survivor and zombie character hierarchies, instantiates the optimized licensed 3D SWAT model on the leader, resolves its external diffuse/normal/specular channels into lit PBR materials, assigns its authored locomotion controller, retains two reference-decal wing survivors, keeps hidden generated muzzle carriers for gameplay, and builds connected zombie bodies and armored gear.
- `PrototypeGeometryFactory.cs`: Creates collider-free placeholder cube meshes, rounded sphere meshes, cylinder meshes, regular prism meshes, flat road planes, and upright texture-card planes for runtime-generated scenes, avoiding Unity's primitive collider path in iOS simulator builds.
- `PrototypeHumanoidAnimationStyle.cs`: Names the procedural animation profiles used by generated survivor and zombie rigs.
- `PrototypeHumanoidAnimator.cs`: Drives generated wing-survivor and zombie joint chains plus survivor gameplay roots and hidden muzzle carriers; it deliberately leaves imported SWAT body bones to the authored Animator so procedural LateUpdate posing cannot fight the clips.
- `PrototypeMaterialFactory.cs`: Creates tinted opaque depth-writing placeholder materials for runtime-generated prototype geometry, alpha-blended textured materials for reference cutout cards, always-visible transparent text materials, and always-visible solid tracer materials for world-space combat feedback by probing a short list of common Unity shaders.
- `ProceduralSoldierRearTexture.cs`: Generates the rear-facing woman soldier cutout used by chase-camera survivor views from the approved readable front cutout's alpha silhouette and paint detail, then layers rear armor, helmet, ponytail, backpack, a single compact raised shoulder rifle with supporting forearms, teal suit panels, gray plates, and cyan lights without reusing the front-facing PNG backward.
- `ReferenceModelFacingVisibility.cs`: Shows the reference-derived rear soldier decal in the minigame chase camera and switches to the approved front soldier decal only when a camera crosses to the zombie-facing side of the survivor root.
- `SwatSurvivorLocomotionAnimator.cs`: Measures authoritative survivor-root movement and switches the imported controller's `Moving` bool without applying root motion.

## Compile

Unity compiles this directory through the main `LaneSurvivor.Runtime` assembly.

## Behavior

Runtime bootstrap code uses these factories for flat track surfaces, pentagon base/HQ prisms, gate cards, the 3D SWAT leader beside two reference-decal wing survivors, hidden humanoid skeleton rigs, humanoid zombie characters, muzzle flares, shot tracers, feedback labels, and remaining simple prototype objects. The optimized SWAT leader shares a 59,288-triangle FBX and explicit 1024-pixel-capped PBR texture channels. Its Unity Humanoid-compatible bones play a two-handed rifle idle and an alternating contact/pass run cycle, with short controller crossfades and gameplay-owned world movement. Built-in Standard materials receive deterministic albedo and normal-map assignments, source specular maps where provided, and restrained smoothness/metallic weapon response, replacing the old colour-only fallbacks. Both legacy leader cards and their camera-facing switcher remain disabled after the FBX loads, while the two wing members retain the previous decals as an immediate in-game quality and performance comparison. Gameplay continues to use hidden generated rifles and muzzle anchors, so firing rules and effects remain independent of the visual model.
