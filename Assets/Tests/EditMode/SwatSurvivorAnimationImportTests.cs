using System;
using System.Linq;
using LaneSurvivor.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LaneSurvivor.Tests.EditMode
{
    public sealed class SwatSurvivorAnimationImportTests
    {
        // Stable source paths make the import contract testable without loading a gameplay scene.
        private const string IdleAnimationPath = "Assets/Resources/Survivor3D/Animations/Mixamo_Rifle_Lowered_Idle.fbx";
        private const string RunAnimationPath = "Assets/Resources/Survivor3D/Animations/Mixamo_Rifle_Run.fbx";
        private const string ControllerPath = "Assets/Resources/Survivor3D/SWAT_Survivor_Controller.controller";

        [Test]
        public void MixamoRifleClips_ImportAsLoopedHumanoidMotions()
        {
            // Both animation-only FBXs must remain independently retargetable Humanoid sources.
            AssertMixamoImportContract(IdleAnimationPath, true);
            AssertMixamoImportContract(RunAnimationPath, false);
        }

        [Test]
        public void SwatController_UsesMixamoIdleAndWalkAssets()
        {
            // The source-controlled controller should exist after the deterministic editor rebuild step.
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Assert.IsNotNull(controller);

            // State names form the readable controller contract used during visual debugging.
            AnimatorState[] baseStates = controller.layers[0].stateMachine.states
                .Select(childState => childState.state)
                .ToArray();
            AnimatorState idleState = baseStates.Single(state => state.name == "Rifle Idle");
            AnimatorState runState = baseStates.Single(state => state.name == "Rifle Run");

            // Motion asset paths prove the temporary Blender actions are no longer driving locomotion.
            Assert.AreEqual(IdleAnimationPath, AssetDatabase.GetAssetPath(idleState.motion));
            Assert.AreEqual(RunAnimationPath, AssetDatabase.GetAssetPath(runState.motion));

            // A single full-body layer prevents a static idle layer from suppressing visible Mixamo locomotion.
            Assert.AreEqual(1, controller.layers.Length);
            Assert.IsNull(controller.layers[0].avatarMask);

            // Runtime movement continues to switch the same bool parameter used by the existing bridge component.
            AnimatorControllerParameter movingParameter = controller.parameters.Single(parameter => parameter.name == "Moving");
            Assert.AreEqual(AnimatorControllerParameterType.Bool, movingParameter.type);
        }

        private static void AssertMixamoImportContract(string animationPath, bool expectedLoopPose)
        {
            // The model importer owns both Humanoid mapping and deterministic clip-loop settings.
            ModelImporter importer = AssetImporter.GetAtPath(animationPath) as ModelImporter;
            Assert.IsNotNull(importer, $"Missing Mixamo importer at '{animationPath}'.");
            Assert.AreEqual(ModelImporterAnimationType.Human, importer.animationType);
            Assert.AreEqual(ModelImporterAvatarSetup.CreateFromThisModel, importer.avatarSetup);
            Assert.AreEqual(ModelImporterMaterialImportMode.None, importer.materialImportMode);
            Assert.IsTrue(importer.importAnimation);
            Assert.AreEqual(ModelImporterAnimationCompression.Off, importer.animationCompression);

            // The rebuild step persists one looping, fully root-locked locomotion take per downloaded FBX.
            ModelImporterClipAnimation clip = importer.clipAnimations.Single();
            Assert.IsTrue(clip.loopTime);
            Assert.AreEqual(expectedLoopPose, clip.loopPose);
            Assert.IsTrue(clip.lockRootRotation);
            Assert.IsTrue(clip.lockRootHeightY);
            Assert.IsTrue(clip.lockRootPositionXZ);

            // Exactly one non-preview motion keeps controller generation deterministic across reimports.
            AnimationClip[] motions = AssetDatabase.LoadAllAssetsAtPath(animationPath)
                .OfType<AnimationClip>()
                .Where(candidate => !candidate.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            Assert.AreEqual(1, motions.Length);
        }
    }
}
