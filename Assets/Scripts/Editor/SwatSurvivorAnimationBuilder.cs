using System;
using System.Linq;
using LaneSurvivor.Rendering;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LaneSurvivor.Editor
{
    public static class SwatSurvivorAnimationBuilder
    {
        // Animation-only Mixamo FBXs remain separate from the licensed mesh so the character is not duplicated.
        private const string IdleAnimationPath = "Assets/Resources/Survivor3D/Animations/Mixamo_Rifle_Lowered_Idle.fbx";
        private const string RunAnimationPath = "Assets/Resources/Survivor3D/Animations/Mixamo_Rifle_Run.fbx";

        // Resources loading lets editor-built and runtime-bootstrapped scenes use the same controller.
        private const string ControllerPath = "Assets/Resources/Survivor3D/SWAT_Survivor_Controller.controller";

        // Rebuilds remove the obsolete partial lower-body layer asset left by the first locomotion experiment.
        private const string LowerBodyMaskPath = "Assets/Resources/Survivor3D/SWAT_Lower_Body.mask";

        [MenuItem("Lane Survivor/Rebuild SWAT Locomotion Controller")]
        public static void Rebuild()
        {
            // Custom loop and root-lock settings become available after each FBX's initial preprocessing pass.
            EnsureMixamoClipLoops(IdleAnimationPath, true);
            EnsureMixamoClipLoops(RunAnimationPath, false);

            // Each animation-only FBX contributes exactly one non-preview Humanoid motion to the controller.
            AnimationClip idleClip = LoadMixamoClip(IdleAnimationPath);
            AnimationClip runClip = LoadMixamoClip(RunAnimationPath);

            // Rebuilding from source avoids silently retaining obsolete states, layers, masks, or transitions.
            if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            // The full-body rifle walk no longer needs the old leg-only mask.
            if (AssetDatabase.LoadAssetAtPath<AvatarMask>(LowerBodyMaskPath) != null)
            {
                AssetDatabase.DeleteAsset(LowerBodyMaskPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter(SwatSurvivorLocomotionAnimator.MovingParameterName, AnimatorControllerParameterType.Bool);

            // The base layer owns both complete rifle poses so the displayed mesh cannot remain pinned to idle.
            AnimatorStateMachine baseStateMachine = controller.layers[0].stateMachine;
            AnimatorState idleState = baseStateMachine.AddState("Rifle Idle");
            idleState.motion = idleClip;
            baseStateMachine.defaultState = idleState;

            // The 4.2 m/s gameplay pace is a run, so this faster cycle prevents slow-walk foot dragging against the road.
            AnimatorState runState = baseStateMachine.AddState("Rifle Run");
            runState.motion = runClip;
            runState.speed = 1.15f;

            // Short crossfades remove visible pops without making the character react sluggishly.
            AnimatorStateTransition beginRun = idleState.AddTransition(runState);
            ConfigureTransition(beginRun, true);

            AnimatorStateTransition stopRun = runState.AddTransition(idleState);
            ConfigureTransition(stopRun, false);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Rebuilt SWAT locomotion controller with Mixamo clips '{idleClip.name}' and '{runClip.name}'.");
        }

        private static void EnsureMixamoClipLoops(string animationPath, bool blendLoopPose)
        {
            // Every configured path must resolve to an imported animation FBX before the controller is rebuilt.
            ModelImporter importer = AssetImporter.GetAtPath(animationPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Missing Mixamo animation importer at '{animationPath}'.");
            }

            // Existing custom clips preserve prior settings; otherwise Unity's generated action list is the baseline.
            ModelImporterClipAnimation[] clips = importer.clipAnimations.Length > 0
                ? importer.clipAnimations
                : importer.defaultClipAnimations;
            bool changed = false;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                // Both downloaded actions are in-place cycles; PlayerSquad remains authoritative for world translation.
                changed |= !clip.loopTime ||
                           clip.loopPose != blendLoopPose ||
                           !clip.lockRootRotation ||
                           !clip.lockRootHeightY ||
                           !clip.lockRootPositionXZ ||
                           !clip.keepOriginalPositionXZ ||
                           !clip.keepOriginalPositionY ||
                           !clip.keepOriginalOrientation;
                clip.loopTime = true;
                // Mixamo's run already closes cleanly; preserving it avoids redistributing the left-foot lift across the loop.
                clip.loopPose = blendLoopPose;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalPositionXZ = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalOrientation = true;
            }

            if (!changed && importer.clipAnimations.Length > 0)
            {
                return;
            }

            // Saving custom clip settings triggers one deterministic reimport before the controller loads its motions.
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimationClip LoadMixamoClip(string animationPath)
        {
            // Preview clips are editor-only helpers; the remaining clip is the actual downloaded Mixamo motion.
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(animationPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
            if (clips.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one Mixamo animation clip at '{animationPath}', but found {clips.Length}.");
            }

            return clips[0];
        }

        private static void ConfigureTransition(AnimatorStateTransition transition, bool movingCondition)
        {
            // Conditions respond immediately; normalized exit time would delay state changes by almost a full loop.
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.12f;
            transition.AddCondition(
                movingCondition ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                0f,
                SwatSurvivorLocomotionAnimator.MovingParameterName);
        }
    }
}
