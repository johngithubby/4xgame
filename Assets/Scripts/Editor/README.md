# Editor Scripts

## Files

- `LaneSurvivor.Editor.asmdef`: Declares the editor-only assembly and references `LaneSurvivor.Runtime` for scene-building tools.
- `IosSmokeBuild.cs`: Provides a command-line `LaneSurvivor.Editor.IosSmokeBuild.Run` method that builds enabled scenes for iOS into a disposable local folder, with optional simulator SDK selection through `LANE_SURVIVOR_IOS_SIMULATOR=1`, optional simulator architecture selection through `LANE_SURVIVOR_IOS_SIMULATOR_ARCH`, and optional verification start-scene ordering through `LANE_SURVIVOR_IOS_START_SCENE`.
- `PhaseOneSceneBuilder.cs`: Rebuilds the Phase 1 level asset, placeholder materials, Unity scene, runtime-matched visible reference-textured woman rifle survivor squad with obsolete flat cards suppressed, optional disabled HUD-layer player marker, perspective angled chase camera, anchor-matched HUD text layout with upper-right state text, UI/end-screen reward layout, and build settings.
- `SwatSurvivorAnimationBuilder.cs`: Applies looping/in-place settings to the authored SWAT rifle actions and rebuilds the Resources Animator Controller with short idle/run crossfades.
- `SwatSurvivorModelImporter.cs`: Applies path-specific mobile import settings to the optimized Female SWAT Soldier FBX, validates its Character Creator skeleton as Unity Humanoid, imports authored actions, preserves accessible bones, configures external diffuse/normal channels, and omits unused blendshapes.

## Compile

Unity compiles these files into the `LaneSurvivor.Editor` assembly because the assembly definition is limited to the Editor platform.

## Behavior

Run `Lane Survivor/Rebuild SWAT Locomotion Controller` after replacing the optimized FBX, then run `Lane Survivor/Rebuild Phase 1 Scene` to regenerate `Assets/Scenes/Minigame.unity` and the first level data from source-controlled scripts. Regenerated Minigame HUD text uses the same anchor-matched pivots as the runtime bootstrap so upper-left labels stay inside the Game view and upper-right state text avoids iPhone Dynamic Island captures. Run the iOS smoke build method from Unity batch mode after local tests pass to validate that enabled scenes compile for the iOS target, set `LANE_SURVIVOR_IOS_SIMULATOR=1` when exporting a project for Simulator deployment, set `LANE_SURVIVOR_IOS_SIMULATOR_ARCH=ARM64` for modern Apple Silicon iOS simulators that reject x86_64 apps, and set `LANE_SURVIVOR_IOS_START_SCENE=Minigame` only for automated visual verification runs that need to skip Base UI tapping.
