# Editor Scripts

## Files

- `LaneSurvivor.Editor.asmdef`: Declares the editor-only assembly and references `LaneSurvivor.Runtime` for scene-building tools.
- `IosSmokeBuild.cs`: Provides a command-line `LaneSurvivor.Editor.IosSmokeBuild.Run` method that builds enabled scenes for iOS into a disposable local folder.
- `PhaseOneSceneBuilder.cs`: Rebuilds the Phase 1 level asset, placeholder materials, Unity scene, player, camera, UI, and build settings.

## Compile

Unity compiles these files into the `LaneSurvivor.Editor` assembly because the assembly definition is limited to the Editor platform.

## Behavior

Run `Lane Survivor/Rebuild Phase 1 Scene` in Unity to regenerate `Assets/Scenes/Minigame.unity` and the first level data from source-controlled scripts. Run the iOS smoke build method from Unity batch mode after local tests pass to validate that enabled scenes compile for the iOS target.
