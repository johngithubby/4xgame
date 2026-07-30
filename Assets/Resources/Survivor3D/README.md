# SWAT Survivor Technical Trial

## Files

- `SWAT_Survivor_Mobile.fbx`: Mobile-oriented Unity FBX derived from the free Female SWAT Soldier model, with a Unity Humanoid-compatible Character Creator skeleton and authored `SWAT_Rifle_Idle` and `SWAT_Rifle_Run` actions.
- `SWAT_Survivor_Controller.controller`: Unity Animator Controller that crossfades between the two authored in-place actions through the `Moving` parameter.
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

`SwatSurvivorModelImporter.cs` configures the FBX as a Unity Humanoid, imports the two in-place actions, disables blendshapes, applies medium mesh compression, limits skinning to four bone influences, and imports external textures through the correct colour/normal-map paths. `SwatSurvivorAnimationBuilder.cs` applies loop settings and rebuilds the controller. `PrototypeCharacterFactory` instantiates one shared model on the survivor leader, resolves source material-slot names to explicit lit diffuse/normal materials, and switches idle/run state from gameplay-root movement while keeping the two existing wing survivors as a direct A/B comparison.
