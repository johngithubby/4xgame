# Mixamo Rifle Animations

## Files

- `Mixamo_Rifle_Lowered_Idle.fbx`: Mixamo `Rifle Idle` (`Two Hand Lowered Gun Rifle Idle`), exported as FBX for Unity at 30 FPS without skin. Its stable two-hand pose avoids the rejected aiming idle's open grip and steep weapon pitch on this rig.
- `Mixamo_Rifle_Run.fbx`: Mixamo in-place rifle run, exported as FBX for Unity at 30 FPS without skin. It matches the survivor's 4.2 m/s gameplay travel better than a slow walk.
- `Mixamo_Rifle_Walk.fbx`: Mixamo `Walk With Rifle` (`Walking With Rifle At Waist Level 3 Cycle`), exported in place as FBX for Unity at 30 FPS without skin. Zombies reuse its calmer grounded foot exchange before a late-frame component adds unstable undead motion.

## Unity behavior

`SwatSurvivorModelImporter.cs` imports the files as independent Humanoid animation sources without materials or meshes. `SwatSurvivorAnimationBuilder.cs` locks their root transforms, enables looping, and assigns the complete lowered-idle and rifle-run poses to one fully weighted base layer in the SWAT locomotion controller. This keeps the survivor's legs, hips, torso, and rifle hold in the same authored gait while `PlayerSquad` remains authoritative for world movement. Runtime zombies override every state with `Mixamo_Rifle_Walk`, slow its cadence, hide the weapon meshes, and keep enemy gameplay roots stationary while `SwatZombieAnimator` adds asymmetric stumbling.

## Licence

The animations were downloaded from Mixamo under Adobe's Mixamo terms for use in this game. Do not redistribute the raw FBX files as a standalone animation pack.
