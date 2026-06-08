using System;
using System.Collections.Generic;
using UnityEngine;

namespace LaneSurvivor.Rendering
{
    public sealed class PrototypeHumanoidAnimator : MonoBehaviour
    {
        // Root movement below this speed is treated as jitter instead of an intentional walk.
        private const float MovementSpeedThreshold = 0.04f;

        // Blend speed keeps walk start/stop responsive without snapping generated limbs.
        private const float BlendSpeed = 8f;

        // Cached rigs avoid repeated Transform.Find work every frame.
        private readonly List<HumanoidRig> rigs = new();

        // The style selects both rig discovery and animation timing.
        [SerializeField]
        private PrototypeHumanoidAnimationStyle animationStyle = PrototypeHumanoidAnimationStyle.SurvivorSquad;

        // Cycle speed controls how quickly sine-driven footsteps advance.
        [SerializeField]
        private float cycleSpeed = 6.8f;

        // Leg swing is the primary readable run cue at mobile scale.
        [SerializeField]
        private float legSwingDegrees = 40f;

        // Knee bend makes connected thigh/shin chains read as natural steps instead of straight pendulums.
        [SerializeField]
        private float kneeBendDegrees = 68f;

        // Arm swing counterbalances the legs and keeps the body from looking stiff.
        [SerializeField]
        private float armSwingDegrees = 40f;

        // Body roll gives small procedural figures a sense of weight.
        [SerializeField]
        private float bodyRollDegrees = 2.6f;

        // Head nod keeps faces lively without adding separate facial animation.
        [SerializeField]
        private float headNodDegrees = 3.2f;

        // Vertical bob makes footsteps visible even from the chase camera angle.
        [SerializeField]
        private float bobHeight = 0.035f;

        // Last position is used to detect root motion for survivor walking.
        private Vector3 lastWorldPosition;

        // This flag prevents the first frame from comparing against a default zero vector.
        private bool hasLastWorldPosition;

        // Weight blends the authored rest pose with the procedural walk pose.
        private float motionWeight;

        public int AnimatedRigCount => rigs.Count;

        public float AnimationPhase { get; private set; }

        public bool IsAnimating { get; private set; }

        public PrototypeHumanoidAnimationStyle AnimationStyle => animationStyle;

        public void Configure(PrototypeHumanoidAnimationStyle style)
        {
            // Store the requested style before applying style-specific timing and amplitude defaults.
            animationStyle = style;

            // Style defaults keep survivor walk and zombie shamble visually distinct without imported clips.
            ApplyStyleDefaults(style);

            // Generated characters are assembled before Configure runs, so cache their named body parts now.
            RebuildRigCache();

            // Start motion tracking from the current root position so the first frame does not fake a giant step.
            ResetMotionTracking();

            // Always-animated styles begin fully weighted so zombies shamble immediately after spawning.
            motionWeight = StyleAlwaysAnimates(style) ? 1f : 0f;

            // Reset the public state so tests and scene code see a deterministic initial phase.
            AnimationPhase = 0f;
            IsAnimating = motionWeight > 0f;
        }

        public void ForceEvaluate(float deltaTime, bool rootIsMoving)
        {
            // A non-positive delta cannot advance a procedural cycle safely.
            if (deltaTime <= 0f)
            {
                return;
            }

            // Late-created or manually built test objects may not have called Configure yet.
            if (rigs.Count == 0)
            {
                RebuildRigCache();
            }

            // Zombies shamble even without root motion; survivors need actual movement or a forced moving flag.
            bool shouldAnimate = rootIsMoving || StyleAlwaysAnimates(animationStyle);

            // Blend prevents a harsh snap between rest pose and the walking pose.
            float targetWeight = shouldAnimate ? 1f : 0f;
            motionWeight = Mathf.MoveTowards(motionWeight, targetWeight, BlendSpeed * deltaTime);

            // The cycle phase advances only while the character is meant to be walking or shambling.
            if (shouldAnimate)
            {
                AnimationPhase += deltaTime * cycleSpeed;
            }

            // Public state lets smoke tests verify that runtime actors are actively animated.
            IsAnimating = motionWeight > 0.01f;

            // Every rig receives the same cycle with a per-character offset so formations do not march in lockstep.
            foreach (HumanoidRig rig in rigs)
            {
                ApplyRigPose(rig, AnimationPhase + rig.phaseOffset, motionWeight);
            }
        }

        private void Awake()
        {
            // Scene-deserialized actors may not be configured by the factory, so cache a best-effort rig on Awake.
            if (rigs.Count == 0)
            {
                Configure(animationStyle);
            }
        }

        private void OnEnable()
        {
            // Re-enabling an actor should not treat time spent inactive as a huge root-motion step.
            ResetMotionTracking();
        }

        private void LateUpdate()
        {
            // LateUpdate observes movement after PlayerSquad.Update has advanced the gameplay root.
            float deltaTime = Time.deltaTime;

            // Unity can report zero delta during editor transitions, so skip those frames cleanly.
            if (deltaTime <= 0f)
            {
                return;
            }

            // Root motion decides whether survivor rigs should walk this frame.
            bool rootIsMoving = MeasureRootMotion(deltaTime);

            // Apply the procedural walk or shamble pose after movement is known.
            ForceEvaluate(deltaTime, rootIsMoving);
        }

        private void ApplyStyleDefaults(PrototypeHumanoidAnimationStyle style)
        {
            switch (style)
            {
                case PrototypeHumanoidAnimationStyle.SurvivorSquad:
                    // Survivors use an exaggerated mobile-scale run cycle so limb motion reads from the chase camera.
                    cycleSpeed = 9.4f;
                    legSwingDegrees = 40f;
                    kneeBendDegrees = 68f;
                    armSwingDegrees = 40f;
                    bodyRollDegrees = 5.0f;
                    headNodDegrees = 5.2f;
                    bobHeight = 0.065f;
                    break;
                case PrototypeHumanoidAnimationStyle.ZombieShamble:
                    // Basic zombies move slower, with connected dragging legs and a heavier body sway.
                    cycleSpeed = 3.0f;
                    legSwingDegrees = 18f;
                    kneeBendDegrees = 32f;
                    armSwingDegrees = 13f;
                    bodyRollDegrees = 5.0f;
                    headNodDegrees = 6.0f;
                    bobHeight = 0.032f;
                    break;
                case PrototypeHumanoidAnimationStyle.ArmoredZombieShamble:
                    // Armored zombies feel heavier by taking shorter, slower steps.
                    cycleSpeed = 2.2f;
                    legSwingDegrees = 13f;
                    kneeBendDegrees = 26f;
                    armSwingDegrees = 9f;
                    bodyRollDegrees = 3.8f;
                    headNodDegrees = 4.2f;
                    bobHeight = 0.022f;
                    break;
                default:
                    // Failing loudly keeps new styles from silently using survivor tuning.
                    throw new ArgumentOutOfRangeException(nameof(style), style, "Unsupported humanoid animation style.");
            }
        }

        private void RebuildRigCache()
        {
            // Rebuilding clears stale transform references after regenerated scene objects or tests create new children.
            rigs.Clear();

            if (animationStyle == PrototypeHumanoidAnimationStyle.SurvivorSquad)
            {
                // Survivor squad children are direct roots named by the character factory.
                int survivorIndex = 0;
                foreach (Transform child in transform)
                {
                    // Only survivor roots should be animated by this style.
                    if (!child.name.StartsWith("Survivor", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    // Each survivor gets a small phase offset so the group looks organic.
                    rigs.Add(CaptureSurvivorRig(child, survivorIndex));
                    survivorIndex++;
                }

                return;
            }

            // Zombie actors keep their posed body under a single "Zombie Figure" child.
            Transform zombieFigure = transform.Find("Zombie Figure");
            if (zombieFigure != null)
            {
                rigs.Add(CaptureZombieRig(zombieFigure));
            }
        }

        private bool MeasureRootMotion(float deltaTime)
        {
            // First measurement establishes a baseline and should not trigger a walk pose.
            if (!hasLastWorldPosition)
            {
                ResetMotionTracking();
                return false;
            }

            // Root motion should be measured in the gameplay plane only.
            Vector3 delta = transform.position - lastWorldPosition;
            delta.y = 0f;

            // Store the current position for the next LateUpdate comparison.
            lastWorldPosition = transform.position;

            // Convert distance to speed so frame rate does not change the movement threshold.
            float speed = delta.magnitude / Mathf.Max(deltaTime, 0.0001f);

            // Tiny camera or floating-point shifts should not start a walk cycle.
            return speed > MovementSpeedThreshold;
        }

        private void ResetMotionTracking()
        {
            // The current transform position becomes the next root-motion comparison baseline.
            lastWorldPosition = transform.position;
            hasLastWorldPosition = true;
        }

        private void ApplyRigPose(HumanoidRig rig, float phase, float weight)
        {
            // A zero weight restores the authored rest pose while preserving the same code path as animation.
            float weightedLegSwing = legSwingDegrees * weight;

            // Knee flex is separate from thigh swing so legs visibly bend through each stride.
            float weightedKneeBend = kneeBendDegrees * weight;

            // Arm swing mirrors the opposite leg, matching a readable human walk pattern.
            float weightedArmSwing = armSwingDegrees * weight;

            // A sine wave drives forward/back limb motion.
            float stride = Mathf.Sin(phase);

            // The opposite side uses a half-cycle offset.
            float counterStride = Mathf.Sin(phase + Mathf.PI);

            // Hip swing is signed so positive and negative stride phases move the connected legs opposite directions.
            float leftHipSwing = -stride * weightedLegSwing;

            // The opposite hip uses a half-cycle offset for a normal alternating gait.
            float rightHipSwing = -counterStride * weightedLegSwing;

            // Knees bend most during recovery, with a small baseline flex so legs do not lock straight.
            float leftKneeBend = CalculateKneeBend(phase) * weightedKneeBend;

            // The right knee uses the opposite half of the stride cycle.
            float rightKneeBend = CalculateKneeBend(phase + Mathf.PI) * weightedKneeBend;

            // The bounce peaks twice per step cycle, matching a footfall cadence.
            float bounce = Mathf.Abs(Mathf.Cos(phase)) * bobHeight * weight;

            // Body sway is subtle for survivors and heavier for zombie styles.
            float bodyRoll = Mathf.Sin(phase) * bodyRollDegrees * weight;

            // The head nod trails the body roll so the pose feels less mechanical.
            float headNod = Mathf.Sin(phase + Mathf.PI * 0.5f) * headNodDegrees * weight;

            // Move and roll the rig root as a single body mass.
            rig.root.localPosition = rig.initialRootLocalPosition + Vector3.up * bounce;
            rig.root.localRotation = rig.initialRootLocalRotation * Quaternion.Euler(0f, 0f, bodyRoll);

            // Torso and pelvis counter-rotate slightly so the body does not look like one stiff piece.
            ApplyPartPose(rig.torso, Quaternion.Euler(0f, 0f, bodyRoll * 0.35f), Vector3.zero);
            ApplyPartPose(rig.pelvis, Quaternion.Euler(0f, 0f, -bodyRoll * 0.45f), Vector3.zero);

            // Connected hip pivots swing the whole leg chain so knees and ankles remain attached.
            ApplyPartPose(rig.leftLeg, Quaternion.Euler(leftHipSwing, 0f, 0f), Vector3.zero);
            ApplyPartPose(rig.rightLeg, Quaternion.Euler(rightHipSwing, 0f, 0f), Vector3.zero);

            // Connected knee pivots fold the shin and foot chain without moving the knee away from the thigh.
            ApplyPartPose(rig.leftKnee, Quaternion.Euler(leftKneeBend, 0f, 0f), Vector3.zero);
            ApplyPartPose(rig.rightKnee, Quaternion.Euler(rightKneeBend, 0f, 0f), Vector3.zero);

            // The shin pivot adds a tiny extra roll at the same joint, exaggerating the bend while preserving contact.
            ApplyPartPose(rig.leftLowerLeg, Quaternion.Euler(leftKneeBend * 0.10f, 0f, 0f), Vector3.zero);
            ApplyPartPose(rig.rightLowerLeg, Quaternion.Euler(rightKneeBend * 0.10f, 0f, 0f), Vector3.zero);

            // Feet pitch against knee bend and hip swing, but stay attached to the shin chain.
            ApplyPartPose(rig.leftFoot, Quaternion.Euler(-leftHipSwing * 0.22f - leftKneeBend * 0.30f, 0f, 0f), Vector3.zero);
            ApplyPartPose(rig.rightFoot, Quaternion.Euler(-rightHipSwing * 0.22f - rightKneeBend * 0.30f, 0f, 0f), Vector3.zero);

            // Arms swing opposite their neighboring legs; zombie arms keep their authored reaching pose underneath.
            ApplyPartPose(rig.leftArm, Quaternion.Euler(counterStride * weightedArmSwing, 0f, 0f), Vector3.zero);
            ApplyPartPose(rig.rightArm, Quaternion.Euler(stride * weightedArmSwing, 0f, 0f), Vector3.zero);

            // Hands inherit arm movement, with a small pulse to make the silhouette lively.
            ApplyPartPose(rig.leftHand, Quaternion.Euler(counterStride * weightedArmSwing * 0.35f, 0f, 0f), Vector3.zero);
            ApplyPartPose(rig.rightHand, Quaternion.Euler(stride * weightedArmSwing * 0.35f, 0f, 0f), Vector3.zero);

            // The head gets both a nod and a tiny vertical offset so faces read as alive.
            ApplyPartPose(rig.head, Quaternion.Euler(headNod, 0f, -bodyRoll * 0.2f), Vector3.up * bounce * 0.28f);

            // Rifles should track the survivor hand rhythm without swinging as far as arms.
            ApplyPartPose(rig.weapon, Quaternion.Euler(counterStride * weightedArmSwing * 0.22f, 0f, 0f), Vector3.zero);
        }

        private static void ApplyPartPose(AnimatedPart part, Quaternion additiveRotation, Vector3 additivePosition)
        {
            if (!part.IsValid)
            {
                return;
            }

            // Position offsets are local to the part's original generated pose.
            part.transform.localPosition = part.initialLocalPosition + additivePosition;

            // Rotation offsets layer the walk cycle on top of the authored static pose.
            part.transform.localRotation = part.initialLocalRotation * additiveRotation;
        }

        private static float CalculateKneeBend(float phase)
        {
            // Recovery flex peaks once per stride, while baseline flex prevents stick-straight planted legs.
            return 0.18f + Mathf.Clamp01((1f - Mathf.Cos(phase)) * 0.5f) * 0.82f;
        }

        private static bool StyleAlwaysAnimates(PrototypeHumanoidAnimationStyle style)
        {
            // Zombies do not move down the lane, so their walk cycle loops in place.
            return style == PrototypeHumanoidAnimationStyle.ZombieShamble ||
                   style == PrototypeHumanoidAnimationStyle.ArmoredZombieShamble;
        }

        private static HumanoidRig CaptureSurvivorRig(Transform survivorRoot, int survivorIndex)
        {
            // Phase offsets break up the three-person formation without random runtime state.
            float phaseOffset = survivorIndex * 1.35f;

            return new HumanoidRig
            {
                root = survivorRoot,
                initialRootLocalPosition = survivorRoot.localPosition,
                initialRootLocalRotation = survivorRoot.localRotation,
                torso = CapturePart(survivorRoot, "Human Torso"),
                pelvis = CapturePart(survivorRoot, "Human Pelvis"),
                head = CapturePart(survivorRoot, "Human Head"),
                leftArm = CapturePart(survivorRoot, "Human Arm Left"),
                rightArm = CapturePart(survivorRoot, "Human Arm Right"),
                leftHand = CapturePart(survivorRoot, "Human Hand Left"),
                rightHand = CapturePart(survivorRoot, "Human Hand Right"),
                leftLeg = CapturePart(survivorRoot, "Human Leg Left"),
                rightLeg = CapturePart(survivorRoot, "Human Leg Right"),
                leftKnee = CapturePart(survivorRoot, "Human Knee Left"),
                rightKnee = CapturePart(survivorRoot, "Human Knee Right"),
                leftLowerLeg = CapturePart(survivorRoot, "Human Shin Left"),
                rightLowerLeg = CapturePart(survivorRoot, "Human Shin Right"),
                leftFoot = CapturePart(survivorRoot, "Human Boot Left"),
                rightFoot = CapturePart(survivorRoot, "Human Boot Right"),
                weapon = CapturePart(survivorRoot, "Human Rifle"),
                phaseOffset = phaseOffset
            };
        }

        private static HumanoidRig CaptureZombieRig(Transform zombieRoot)
        {
            return new HumanoidRig
            {
                root = zombieRoot,
                initialRootLocalPosition = zombieRoot.localPosition,
                initialRootLocalRotation = zombieRoot.localRotation,
                torso = CapturePart(zombieRoot, "Zombie Torso"),
                pelvis = CapturePart(zombieRoot, "Zombie Pelvis"),
                head = CapturePart(zombieRoot, "Zombie Head"),
                leftArm = CapturePart(zombieRoot, "Zombie Arm Left"),
                rightArm = CapturePart(zombieRoot, "Zombie Arm Right"),
                leftHand = CapturePart(zombieRoot, "Zombie Hand Left"),
                rightHand = CapturePart(zombieRoot, "Zombie Hand Right"),
                leftLeg = CapturePart(zombieRoot, "Zombie Leg Left"),
                rightLeg = CapturePart(zombieRoot, "Zombie Leg Right"),
                leftKnee = CapturePart(zombieRoot, "Zombie Knee Left"),
                rightKnee = CapturePart(zombieRoot, "Zombie Knee Right"),
                leftLowerLeg = CapturePart(zombieRoot, "Zombie Shin Left"),
                rightLowerLeg = CapturePart(zombieRoot, "Zombie Shin Right"),
                leftFoot = CapturePart(zombieRoot, "Zombie Foot Left"),
                rightFoot = CapturePart(zombieRoot, "Zombie Foot Right"),
                weapon = default,
                phaseOffset = 0f
            };
        }

        private static AnimatedPart CapturePart(Transform root, string childName)
        {
            // Missing optional parts become inert records so one animator can handle survivor and zombie rigs.
            Transform child = FindDescendant(root, childName);
            return new AnimatedPart(child);
        }

        private static Transform FindDescendant(Transform root, string childName)
        {
            // A missing root cannot contain an optional generated body part.
            if (root == null)
            {
                return null;
            }

            // Direct children cover torso/head parts and keep the common case cheap.
            Transform directChild = root.Find(childName);
            if (directChild != null)
            {
                return directChild;
            }

            // Nested joint chains need a recursive lookup because knees, shins, hands, and feet are now connected.
            foreach (Transform child in root)
            {
                // Search each branch depth-first so the first matching generated part is returned deterministically.
                Transform nestedChild = FindDescendant(child, childName);
                if (nestedChild != null)
                {
                    return nestedChild;
                }
            }

            // Returning null lets AnimatedPart represent optional geometry without special cases.
            return null;
        }

        private sealed class HumanoidRig
        {
            public Transform root;

            public Vector3 initialRootLocalPosition;

            public Quaternion initialRootLocalRotation;

            public AnimatedPart torso;

            public AnimatedPart pelvis;

            public AnimatedPart head;

            public AnimatedPart leftArm;

            public AnimatedPart rightArm;

            public AnimatedPart leftHand;

            public AnimatedPart rightHand;

            public AnimatedPart leftLeg;

            public AnimatedPart rightLeg;

            public AnimatedPart leftKnee;

            public AnimatedPart rightKnee;

            public AnimatedPart leftLowerLeg;

            public AnimatedPart rightLowerLeg;

            public AnimatedPart leftFoot;

            public AnimatedPart rightFoot;

            public AnimatedPart weapon;

            public float phaseOffset;
        }

        private readonly struct AnimatedPart
        {
            public readonly Transform transform;

            public readonly Vector3 initialLocalPosition;

            public readonly Quaternion initialLocalRotation;

            public bool IsValid => transform != null;

            public AnimatedPart(Transform capturedTransform)
            {
                // Null transforms are allowed for optional body parts.
                transform = capturedTransform;

                // Valid parts store their generated rest position for lossless pose restoration.
                initialLocalPosition = capturedTransform != null ? capturedTransform.localPosition : Vector3.zero;

                // Valid parts store their generated rest rotation for additive animation.
                initialLocalRotation = capturedTransform != null ? capturedTransform.localRotation : Quaternion.identity;
            }
        }
    }
}
