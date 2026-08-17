# SWAT Survivor Technical Trial

## Files

- `SWAT_Survivor_Mobile.fbx`: Mobile-oriented Unity FBX derived from the free Female SWAT Soldier model, with a Unity Humanoid-compatible Character Creator skeleton.
- `Animations/`: Animation-only Mixamo Unity FBXs for a lowered rifle idle, an in-place rifle run, and an in-place rifle walk, downloaded at 30 FPS without skin. The survivor uses idle/run; zombies reuse the walk beneath their procedural stumble.
- `SWAT_Survivor_Controller.controller`: Unity Animator Controller that retargets and crossfades between the two Mixamo Humanoid clips through the `Moving` parameter.
- `SWAT_Zombie_Tattered_Clothing.shader`: Resources-loaded Built-in Standard surface shader that keeps the outfit alpha/normal maps, replaces its almost-black albedo with three vivid per-zombie colours, and alpha-clips five deterministic ragged holes with matching cut-out shadows.
- `Textures/`: External 1024-pixel-capped diffuse, normal, bump, opacity, and specular source channels used to construct deterministic runtime PBR materials.

## Source and licence

- Source: [Female SWAT Soldier on CGTrader](https://www.cgtrader.com/free-3d-models/military/military-character/female-swat-soldier)
- Creator: `pathumtharaka1998`
- CGTrader model ID: `6020849`
- Listing licence at download time: Royalty Free License, with the listing marked “no AI.”

The raw `.blend` source and downloaded texture archive are intentionally kept outside the repository. Do not redistribute this FBX as a standalone model or asset pack; it is included only as part of the game project under the source listing's licence.

## Optimization

The source listed 344,456 triangles. Blender 3.6 removed hidden mouth/tear geometry, removed unused facial blendshapes, reduced dense clothing, helmet, belt, and weapon geometry, and exported visible texture channels at a maximum of 1024 pixels. The resulting gameplay trial contains 59,288 triangles and retains the original 150-bone Character Creator rig.

## Unity behavior

`SwatSurvivorModelImporter.cs` configures the character FBX as a Unity Humanoid, disables blendshapes, applies medium mesh compression, limits skinning to four bone influences, imports external textures through the correct colour/normal-map paths, and imports animation-only Mixamo FBXs as separate Humanoid sources. `SwatSurvivorAnimationBuilder.cs` locks locomotion roots, applies loop settings, and rebuilds one full-body base layer that crossfades between the downloaded rifle idle and rifle run. `PrototypeCharacterFactory` instantiates the shared model on all three survivors and visible zombies. Survivor property blocks replace the flat near-black suit RGB with distinct dark navy, charcoal/burgundy, and olive palettes, while removable gear creates commander, scout, and heavy silhouettes without duplicating meshes or materials. The zombie suit retains its dedicated shared cutout material, vivid per-spawn colours, real torso/leg/sleeve holes, infected skin, and late-frame drunken stumble overlay.
