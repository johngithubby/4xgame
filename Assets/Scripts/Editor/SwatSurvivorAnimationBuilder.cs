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
        // The optimized FBX owns both authored Humanoid animation subassets.
        private const string ModelPath = "Assets/Resources/Survivor3D/SWAT_Survivor_Mobile.fbx";

        // Resources loading lets editor-built and runtime-bootstrapped scenes use the same controller.
        private const string ControllerPath = "Assets/Resources/Survivor3D/SWAT_Survivor_Controller.controller";

        [MenuItem("Lane Survivor/Rebuild SWAT Locomotion Controller")]
        public static void Rebuild()
        {
            // Custom clip settings become available only after the FBX's initial preprocessing pass.
            EnsureAuthoredClipsLoop();

            // Imported clips include internal preview assets, so match the authored action suffix deterministically.
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();

            AnimationClip idleClip = FindAuthoredClip(clips, "SWAT_Rifle_Idle");
            AnimationClip runClip = FindAuthoredClip(clips, "SWAT_Rifle_Run");

            // Rebuilding from source avoids silently retaining obsolete states or transition parameters.
            if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter(SwatSurvivorLocomotionAnimator.MovingParameterName, AnimatorControllerParameterType.Bool);

            // Idle is the ready-screen default because the gameplay root has not started moving yet.
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idleState = stateMachine.AddState("Rifle Idle");
            idleState.motion = idleClip;
            stateMachine.defaultState = idleState;

            // Run plays in place while PlayerSquad remains authoritative for actual lane and forward movement.
            AnimatorState runState = stateMachine.AddState("Rifle Run");
            runState.motion = runClip;
            runState.speed = 1.08f;

            // Short crossfades remove visible pops without making the character react sluggishly.
            AnimatorStateTransition beginRun = idleState.AddTransition(runState);
            ConfigureTransition(beginRun, true);

            AnimatorStateTransition stopRun = runState.AddTransition(idleState);
            ConfigureTransition(stopRun, false);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Rebuilt SWAT locomotion controller with {idleClip.name} and {runClip.name}.");
        }

        private static void EnsureAuthoredClipsLoop()
        {
            // The model path contract guarantees a ModelImporter unless the licensed FBX was removed.
            ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException($"Missing SWAT model importer at '{ModelPath}'.");
            }

            // Existing custom clips preserve prior settings; otherwise Unity's generated action list is the baseline.
            ModelImporterClipAnimation[] clips = importer.clipAnimations.Length > 0
                ? importer.clipAnimations
                : importer.defaultClipAnimations;
            bool changed = false;
            foreach (ModelImporterClipAnimation clip in clips)
            {
                // Internal FBX takes are not locomotion loops, so only touch the two explicitly authored actions.
                if (!clip.name.Contains("SWAT_Rifle_", StringComparison.Ordinal))
                {
                    continue;
                }

                // Both actions are in-place cycles; PlayerSquad remains authoritative for world translation.
                changed |= !clip.loopTime ||
                           !clip.loopPose ||
                           !clip.keepOriginalPositionXZ ||
                           !clip.keepOriginalPositionY ||
                           !clip.keepOriginalOrientation;
                clip.loopTime = true;
                clip.loopPose = true;
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

        private static AnimationClip FindAuthoredClip(AnimationClip[] clips, string authoredName)
        {
            // Blender FBX action names may receive an armature prefix, so suffix matching is the stable contract.
            AnimationClip clip = clips.FirstOrDefault(candidate => candidate.name.EndsWith(authoredName, StringComparison.Ordinal));
            if (clip == null)
            {
                throw new InvalidOperationException($"Missing authored SWAT animation clip ending in '{authoredName}'.");
            }

            return clip;
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
