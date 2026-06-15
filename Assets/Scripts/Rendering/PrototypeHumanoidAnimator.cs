using System;
using System.Collections.Generic;
using LaneSurvivor.Gameplay;
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

        // Shot recoil recovers quickly so every AutoShooter volley can visibly pulse the soldier card.
        private const float ShotRecoilRecoverySpeed = 9f;

        // Recoil nudges the reference card backward in local Z, away from the visible rifle direction.
        private const float ShotRecoilBackOffset = 0.045f;

        // A tiny lift keeps the recoil readable on a flat cutout without desynchronizing the hidden muzzle.
        private const float ShotRecoilLift = 0.025f;

        // A small pitch gives the flat card a firing kick while keeping the stance practical.
        private const float ShotRecoilPitchDegrees = -3.2f;

        // Aim pose fades slower than recoil so rapid auto-fire keeps soldiers visibly oriented at their target.
        private const float ShotAimRecoverySpeed = 2.8f;

        // Target-aware yaw is intentionally modest because the card must still read from the chase camera.
        private const float ShotAimYawLimitDegrees = 14f;

        // Target-aware pitch gives high and low targets a subtle shoulder/weapon adjustment.
        private const float ShotAimPitchLimitDegrees = 5f;

        // The card sways side to side so the rendered soldier visibly steps, not only the hidden rig.
        private const float SoldierWalkSway = 0.035f;

        // A small extra lift makes the PNG boots pulse with the same cadence as the generated leg cycle.
        private const float SoldierWalkLift = 0.030f;

        // Forward/back card motion creates a stride impression on the lane depth axis.
        private const float SoldierWalkStrideDepth = 0.028f;

        // Roll is the strongest card-level walk cue at phone scale.
        private const float SoldierWalkRollDegrees = 4.8f;

        // Pitch is subtle so walking does not look like the soldier is falling.
        private const float SoldierWalkPitchDegrees = 1.8f;

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

                // Recoil decays after it has affected this evaluation so a just-fired shot gets one visible kick.
                rig.shotRecoil = Mathf.MoveTowards(rig.shotRecoil, 0f, ShotRecoilRecoverySpeed * deltaTime);

                // Aim weight fades after the pose so each shot gives at least one target-facing evaluation.
                rig.shotAimWeight = Mathf.MoveTowards(rig.shotAimWeight, 0f, ShotAimRecoverySpeed * deltaTime);
            }
        }

        public void PlaySurvivorShot(Vector3 shotOrigin)
        {
            // Legacy callers still trigger recoil and aim along the down-lane forward direction.
            PlaySurvivorShot(shotOrigin, shotOrigin + Vector3.forward);
        }

        public void PlaySurvivorShot(Vector3 shotOrigin, Vector3 targetPoint)
        {
            // Rebuild lazily so tests that create hierarchies after Awake can still trigger a card recoil.
            if (rigs.Count == 0)
            {
                RebuildRigCache();
            }

            // Only survivor squads have soldier reference cards and visible weapon recoil.
            if (animationStyle != PrototypeHumanoidAnimationStyle.SurvivorSquad)
            {
                return;
            }

            // Find the rig whose weapon anchor is closest to the muzzle origin chosen by AutoShooter.
            HumanoidRig closestRig = null;
            float closestDistance = float.MaxValue;
            foreach (HumanoidRig rig in rigs)
            {
                // Prefer the muzzle anchor because it is the exact point AutoShooter used for this shot.
                Transform matchTransform = rig.weaponMuzzle.IsValid ? rig.weaponMuzzle.transform : null;

                // Fallback to the weapon root if a hand-built rig forgot to include a muzzle anchor.
                if (matchTransform == null && rig.weapon.IsValid)
                {
                    matchTransform = rig.weapon.transform;
                }

                // Last fallback is the survivor root, which keeps legacy tests from failing hard.
                if (matchTransform == null)
                {
                    matchTransform = rig.root;
                }

                // Null roots should never happen for captured rigs, but this keeps hand-built tests safe.
                if (matchTransform == null)
                {
                    continue;
                }

                // Squared distance avoids a square root while preserving nearest-rig ordering.
                float distance = (matchTransform.position - shotOrigin).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestRig = rig;
                }
            }

            // If no specific rig could be matched, skip rather than kicking every soldier card at once.
            if (closestRig == null)
            {
                return;
            }

            // A value of one lets ApplyRigPose create the authored recoil offset on the next animation evaluation.
            closestRig.shotRecoil = 1f;

            // A value of one lets ApplyRigPose turn the visible card toward the target for this shot.
            closestRig.shotAimWeight = 1f;

            // Store the target-facing angles on the rig so recoil recovery does not erase aiming immediately.
            SetShotAimAngles(closestRig, shotOrigin, targetPoint);
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

            if (animationStyle == PrototypeHumanoidAnimationStyle.SurvivorSquad)
            {
                // Survivors keep both hands locked to their authored firing pose instead of swinging weapons while shooting.
                ApplyPartPose(rig.leftArm, Quaternion.identity, Vector3.zero);
                ApplyPartPose(rig.rightArm, Quaternion.identity, Vector3.zero);

                // Stable hand transforms keep eye-level and hip-fire muzzle anchors from bobbing around the survivor body.
                ApplyPartPose(rig.leftHand, Quaternion.identity, Vector3.zero);
                ApplyPartPose(rig.rightHand, Quaternion.identity, Vector3.zero);
            }
            else
            {
                // Zombie arms swing opposite their neighboring legs while preserving their authored reaching pose underneath.
                ApplyPartPose(rig.leftArm, Quaternion.Euler(counterStride * weightedArmSwing, 0f, 0f), Vector3.zero);
                ApplyPartPose(rig.rightArm, Quaternion.Euler(stride * weightedArmSwing, 0f, 0f), Vector3.zero);

                // Zombie hands inherit the arm motion with a smaller wrist-like pulse.
                ApplyPartPose(rig.leftHand, Quaternion.Euler(counterStride * weightedArmSwing * 0.35f, 0f, 0f), Vector3.zero);
                ApplyPartPose(rig.rightHand, Quaternion.Euler(stride * weightedArmSwing * 0.35f, 0f, 0f), Vector3.zero);
            }

            // The head gets both a nod and a tiny vertical offset so faces read as alive.
            ApplyPartPose(rig.head, Quaternion.Euler(headNod, 0f, -bodyRoll * 0.2f), Vector3.up * bounce * 0.28f);

            // Held weapons should inherit only the stable authored hand pose so tracers remain visually aligned.
            ApplyPartPose(rig.weapon, Quaternion.identity, Vector3.zero);

            // The reference soldier card replaces the visible primitive body and receives walk, aim, and shot motion.
            ApplySoldierReferencePose(rig, phase, weight, stride);
        }

        private static void ApplySoldierReferencePose(HumanoidRig rig, float phase, float weight, float stride)
        {
            // Older fallback rigs may not have a soldier card, so the visual layer stays optional.
            if (!rig.soldierVisual.IsValid)
            {
                return;
            }

            // Lateral sway makes the full-body PNG read as walking even though its internal legs are baked into the art.
            float walkSway = Mathf.Sin(phase) * SoldierWalkSway * weight;

            // Twice-per-cycle lift matches the root bob and gives every footfall a visible pulse.
            float walkLift = Mathf.Abs(Mathf.Cos(phase)) * SoldierWalkLift * weight;

            // Depth stride keeps the card's lower body from looking like it slides rigidly down the lane.
            float walkStrideDepth = Mathf.Cos(phase) * SoldierWalkStrideDepth * weight;

            // Roll follows the stride direction, giving the soldier weight over the planted side.
            float walkRoll = -stride * SoldierWalkRollDegrees * weight;

            // Pitch oscillates gently across the stride so walking and shooting remain distinguishable.
            float walkPitch = Mathf.Sin(phase + Mathf.PI * 0.5f) * SoldierWalkPitchDegrees * weight;

            // Shot recoil moves the flat art just enough to read without drifting away from the hidden rig.
            Vector3 recoilOffset = new(0f, rig.shotRecoil * ShotRecoilLift, -rig.shotRecoil * ShotRecoilBackOffset);

            // Walking and recoil are additive because they describe independent parts of the visible performance.
            Vector3 additiveOffset = new Vector3(walkSway, walkLift, walkStrideDepth) + recoilOffset;

            // Aim angles fade with their own weight so the soldier keeps facing the zombie between fast shots.
            float aimPitch = rig.shotAimPitchDegrees * rig.shotAimWeight;

            // Positive local yaw turns the soldier card toward right-side targets while still facing down-lane.
            float aimYaw = rig.shotAimYawDegrees * rig.shotAimWeight;

            // A small pitch sells weapon kick on the full-body card while the hidden muzzle remains stable.
            float recoilPitch = rig.shotRecoil * ShotRecoilPitchDegrees;

            // Card rotation combines walk balance, target aim, and recoil into one readable visible pose.
            Quaternion additiveRotation = Quaternion.Euler(walkPitch + aimPitch + recoilPitch, aimYaw, walkRoll);

            // Card pose is additive over the generated rest position set by the character factory.
            ApplyPartPose(rig.soldierVisual, additiveRotation, additiveOffset);
        }

        private static void SetShotAimAngles(HumanoidRig rig, Vector3 shotOrigin, Vector3 targetPoint)
        {
            // A missing root leaves no stable local frame for target-facing math.
            if (rig.root == null)
            {
                return;
            }

            // Convert the world shot line into survivor-local space so +Z remains "toward the zombies."
            Vector3 localShot = rig.root.InverseTransformDirection(targetPoint - shotOrigin);

            // A near-zero line cannot define useful aim angles.
            if (localShot.sqrMagnitude < 0.0001f)
            {
                rig.shotAimYawDegrees = 0f;
                rig.shotAimPitchDegrees = 0f;
                return;
            }

            // Normalize after the zero guard so angle math is independent of target distance.
            localShot.Normalize();

            // Yaw comes from the horizontal direction to the zombie, clamped to keep the texture readable.
            rig.shotAimYawDegrees = Mathf.Clamp(Mathf.Atan2(localShot.x, Mathf.Max(0.001f, localShot.z)) * Mathf.Rad2Deg, -ShotAimYawLimitDegrees, ShotAimYawLimitDegrees);

            // Pitch tilts toward vertical target differences without overwhelming the recoil kick.
            rig.shotAimPitchDegrees = Mathf.Clamp(-Mathf.Asin(Mathf.Clamp(localShot.y, -1f, 1f)) * Mathf.Rad2Deg, -ShotAimPitchLimitDegrees, ShotAimPitchLimitDegrees);
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
                weapon = CaptureSurvivorWeapon(survivorRoot),
                weaponMuzzle = CapturePart(survivorRoot, PlayerSquad.WeaponMuzzleAnchorName),
                soldierVisual = CapturePart(survivorRoot, PrototypeCharacterFactory.SoldierReferenceVisualName),
                phaseOffset = phaseOffset
            };
        }

        private static AnimatedPart CaptureSurvivorWeapon(Transform survivorRoot)
        {
            // Weapon roots have profile-specific names, so test each known generated profile in deterministic order.
            Transform weapon = FindDescendant(survivorRoot, PrototypeCharacterFactory.LeaderRifleName) ??
                               FindDescendant(survivorRoot, PrototypeCharacterFactory.LeftWingShotgunName) ??
                               FindDescendant(survivorRoot, PrototypeCharacterFactory.RightWingSmgName);

            return new AnimatedPart(weapon);
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
                weaponMuzzle = default,
                soldierVisual = default,
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

            public AnimatedPart weaponMuzzle;

            public AnimatedPart soldierVisual;

            public float phaseOffset;

            public float shotRecoil;

            public float shotAimWeight;

            public float shotAimYawDegrees;

            public float shotAimPitchDegrees;
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
