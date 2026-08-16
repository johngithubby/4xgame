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

`SwatSurvivorModelImporter.cs` configures the character FBX as a Unity Humanoid, disables blendshapes, applies medium mesh compression, limits skinning to four bone influences, imports external textures through the correct colour/normal-map paths, and imports animation-only Mixamo FBXs as separate Humanoid sources. `SwatSurvivorAnimationBuilder.cs` locks locomotion roots, applies loop settings, and rebuilds one full-body base layer that crossfades between the downloaded rifle idle and rifle run. `PrototypeCharacterFactory` instantiates the shared model on the survivor leader and visible zombies, resolves source material-slot names to explicit lit diffuse/normal materials, applies infected skin and bone-attached face/wound details to enemies, and leaves the two existing wing survivors as a direct quality and performance comparison. The zombie suit uses a dedicated shared cutout material because the source `Outfit_Burglar2` RGB is nearly black and cannot produce strident colours through ordinary tint multiplication. Per-spawn property blocks provide separate upper/lower/accent colours plus real torso, leg, sleeve, and silhouette holes through which the complete underlying body renderer remains visible. A runtime zombie override maps all controller states to the in-place walk; `SwatZombieAnimator` then adds drunken sway, near-stumbles, unequal leg collapse, agitated arms, head/jaw motion, and drool movement without translating gameplay roots.
