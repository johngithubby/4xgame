# Mixamo Rifle Animations

## Files

- `Mixamo_Rifle_Lowered_Idle.fbx`: Mixamo `Rifle Idle` (`Two Hand Lowered Gun Rifle Idle`), exported as FBX for Unity at 30 FPS without skin. Its stable two-hand pose avoids the rejected aiming idle's open grip and steep weapon pitch on this rig.
- `Mixamo_Rifle_Walk.fbx`: Mixamo `Walk With Rifle` (`Walking With Rifle At Waist Level 3 Cycle`), exported in place as FBX for Unity at 30 FPS without skin. Its calmer repeated stride replaces both the rejected aimed run and high-knee quick-walk candidate.

## Unity behavior

`SwatSurvivorModelImporter.cs` imports both files as independent Humanoid animation sources without materials or meshes. `SwatSurvivorAnimationBuilder.cs` locks their root transforms, enables looping, and assigns both complete poses to one fully weighted base layer in the SWAT locomotion controller. This keeps the legs, hips, torso, and rifle hold in the same authored gait while `PlayerSquad` remains authoritative for world movement.

## Licence

The animations were downloaded from Mixamo under Adobe's Mixamo terms for use in this game. Do not redistribute the raw FBX files as a standalone animation pack.
