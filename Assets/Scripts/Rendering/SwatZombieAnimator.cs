using LaneSurvivor.Data;
using UnityEngine;

namespace LaneSurvivor.Rendering
{
    /// <summary>
    /// Layers an intentionally unstable undead performance over the imported Humanoid walk cycle.
    /// </summary>
    public sealed class SwatZombieAnimator : MonoBehaviour
    {
        // Basic zombies retain enough of the authored walk cadence to keep both feet trading support cleanly.
        public const float BasicAnimatorSpeed = 0.50f;

        // Armored zombies drag their extra mass through a slower version of the same Humanoid cycle.
        public const float ArmoredAnimatorSpeed = 0.39f;

        // The broad off-balance sway completes more slowly than the underlying left/right footsteps.
        private const float BasicStumblePhaseSpeed = 1.82f;

        // Heavy enemies stagger more deliberately while preserving the same irregular shape.
        private const float ArmoredStumblePhaseSpeed = 1.39f;

        // A constant hunch prevents the unarmed tactical model from reading like an alert soldier.
        private const float BaseHunchDegrees = 16f;

        // The visual root can wander sideways without moving the gameplay target out of its authored lane.
        private const float MaximumSideSway = 0.075f;

        // Fore/aft lurching stays small enough that damage and breach positions remain authoritative.
        private const float MaximumLurchDistance = 0.055f;

        // A loose undead stance may toe out, but extreme sideways boots still read as a broken retarget.
        private const float MaximumFootYawDegrees = 16f;

        // Each boot retains a small signed gap from the hip centre so legs can stagger without crossing through each other.
        private const float MinimumFootSideDistance = 0.028f;

        // The model Animator supplies the foot cycle before this component adds asymmetric body motion.
        private Animator modelAnimator;

        // This parent moves visually while the outer Zombie transform remains the gameplay anchor.
        private Transform figureRoot;

        // Humanoid bones are cached once so every rendered frame avoids hierarchy searches.
        private Transform hips;
        private Transform spine;
        private Transform chest;
        private Transform head;
        private Transform jaw;
        private Transform leftUpperArm;
        private Transform rightUpperArm;
        private Transform leftLowerArm;
        private Transform rightLowerArm;
        private Transform leftHand;
        private Transform rightHand;
        private Transform leftUpperLeg;
        private Transform rightUpperLeg;
        private Transform leftLowerLeg;
        private Transform rightLowerLeg;
        private Transform leftFoot;
        private Transform rightFoot;
        private Transform leftToes;
        private Transform rightToes;

        // Rest-pose signs make the anti-crossing rule independent of Character Creator's local-axis conventions.
        private float leftFootSideSign;
        private float rightFootSideSign;

        // The lowest imported boot at configuration time defines the visual road-contact plane in parent space.
        private float supportFootGroundHeight;

        // Named face props follow the animated head while keeping their own pulsing and gravity cues.
        private Transform leftBloodyEye;
        private Transform rightBloodyEye;
        private Transform droolStrand;
        private Transform droolDrop;

        // Rest transforms let the parent wobble remain deterministic instead of accumulating every frame.
        private Vector3 initialFigureLocalPosition;
        private Quaternion initialFigureLocalRotation;
        private Vector3 initialLeftEyeLocalScale;
        private Vector3 initialRightEyeLocalScale;
        private Vector3 initialDroolStrandLocalPosition;
        private Vector3 initialDroolDropLocalPosition;

        // Enemy type selects the lighter or heavier cadence without duplicating the component.
        [SerializeField]
        private ZombieEnemyType enemyType;

        // A deterministic spatial offset prevents every spawned zombie from stumbling in lockstep.
        private float phaseOffset;

        public float AnimationPhase { get; private set; }

        public float CurrentStumbleRollDegrees { get; private set; }

        public float CurrentLeftArmAgitationDegrees { get; private set; }

        public float CurrentRightArmAgitationDegrees { get; private set; }

        // Tests and diagnostics expose residual contact error after the per-frame support-foot correction.
        public float CurrentSupportFootGroundError { get; private set; }

        public bool IsAnimating { get; private set; }

        public bool HasCompleteHumanoidRig { get; private set; }

        public ZombieEnemyType EnemyType => enemyType;

        public void Configure(Animator animator, Transform visualRoot, ZombieEnemyType configuredEnemyType)
        {
            // Store the exact imported Animator and visual-only parent constructed by the character factory.
            modelAnimator = animator;
            figureRoot = visualRoot;
            enemyType = configuredEnemyType;

            // Capture the authored lane-facing pose before the first drunk sway is applied.
            CacheFigureBaseline();

            // Humanoid access avoids depending on Character Creator's longer source bone names.
            CacheHumanoidRig();

            // Named props are generated after Animator evaluation, so cache their stable hierarchy names now.
            CacheZombieFaceProps();

            // Position-derived phase is deterministic across reloads while still separating lane encounters.
            Vector3 worldPosition = figureRoot != null ? figureRoot.position : transform.position;
            phaseOffset = Mathf.Repeat(worldPosition.z * 0.47f + worldPosition.x * 0.81f, Mathf.PI * 2f);
            AnimationPhase = phaseOffset;

            ConfigureAnimatorRuntimeSettings();
            IsAnimating = HasCompleteHumanoidRig;
        }

        private void Awake()
        {
            // Scene-deserialized models recover the same dependencies without requiring factory-only references.
            modelAnimator ??= GetComponent<Animator>();
            figureRoot ??= transform.parent;

            CacheFigureBaseline();
            CacheHumanoidRig();
            CacheZombieFaceProps();
            ConfigureAnimatorRuntimeSettings();
        }

        private void LateUpdate()
        {
            // Animator evaluation happens before LateUpdate, making this a non-destructive additive visual layer.
            ApplyZombieOverlay(Time.deltaTime);
        }

        public void ForceEvaluate(float deltaTime)
        {
            // Focused EditMode tests can explicitly evaluate the same authored clip used during gameplay.
            if (deltaTime <= 0f || modelAnimator == null || modelAnimator.runtimeAnimatorController == null)
            {
                return;
            }

            // Re-evaluate the base walk before applying the overlay so repeated test calls cannot accumulate bone twists.
            modelAnimator.Update(deltaTime);
            ApplyZombieOverlay(deltaTime);
        }

        private void ConfigureAnimatorRuntimeSettings()
        {
            if (modelAnimator == null)
            {
                return;
            }

            // Gameplay positions remain fixed lane targets; the Mixamo root is visual only.
            modelAnimator.applyRootMotion = false;

            // Distant zombies must keep walking while just outside the current camera frustum.
            modelAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Basic and armored enemies share one clip but have visibly different weight and urgency.
            modelAnimator.speed = enemyType == ZombieEnemyType.Armored
                ? ArmoredAnimatorSpeed
                : BasicAnimatorSpeed;

            // The reused survivor controller has this transition parameter, while both overridden states use the zombie walk.
            if (modelAnimator.runtimeAnimatorController != null)
            {
                modelAnimator.SetBool(SwatSurvivorLocomotionAnimator.MovingParameterName, true);
            }
        }

        private void CacheFigureBaseline()
        {
            if (figureRoot == null)
            {
                return;
            }

            // Parent motion is always reconstructed from these values instead of layered on the previous frame.
            initialFigureLocalPosition = figureRoot.localPosition;
            initialFigureLocalRotation = figureRoot.localRotation;
        }

        private void CacheHumanoidRig()
        {
            if (modelAnimator == null || !modelAnimator.isHuman)
            {
                HasCompleteHumanoidRig = false;
                return;
            }

            // Core body bones carry the irregular centre-of-mass and head agitation.
            hips = modelAnimator.GetBoneTransform(HumanBodyBones.Hips);
            spine = modelAnimator.GetBoneTransform(HumanBodyBones.Spine);
            chest = modelAnimator.GetBoneTransform(HumanBodyBones.Chest);
            head = modelAnimator.GetBoneTransform(HumanBodyBones.Head);
            jaw = modelAnimator.GetBoneTransform(HumanBodyBones.Jaw);

            // Both complete arm chains are required for the visibly independent flailing requested for the enemy.
            leftUpperArm = modelAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            rightUpperArm = modelAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            leftLowerArm = modelAnimator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            rightLowerArm = modelAnimator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            leftHand = modelAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
            rightHand = modelAnimator.GetBoneTransform(HumanBodyBones.RightHand);

            // The full leg chains keep the Mixamo foot exchange while allowing uneven knee collapse and boot yaw.
            leftUpperLeg = modelAnimator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            rightUpperLeg = modelAnimator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            leftLowerLeg = modelAnimator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            rightLowerLeg = modelAnimator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            leftFoot = modelAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFoot = modelAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
            leftToes = modelAnimator.GetBoneTransform(HumanBodyBones.LeftToes);
            rightToes = modelAnimator.GetBoneTransform(HumanBodyBones.RightToes);

            HasCompleteHumanoidRig = hips != null && spine != null && chest != null && head != null && jaw != null &&
                                       leftUpperArm != null && rightUpperArm != null &&
                                       leftLowerArm != null && rightLowerArm != null &&
                                       leftHand != null && rightHand != null &&
                                       leftUpperLeg != null && rightUpperLeg != null &&
                                       leftLowerLeg != null && rightLowerLeg != null &&
                                       leftFoot != null && rightFoot != null &&
                                       leftToes != null && rightToes != null;
            if (!HasCompleteHumanoidRig)
            {
                return;
            }

            // The first evaluated Humanoid pose defines which side of the hips belongs to each imported boot.
            float hipsLocalX = transform.InverseTransformPoint(hips.position).x;
            leftFootSideSign = Mathf.Sign(transform.InverseTransformPoint(leftFoot.position).x - hipsLocalX);
            rightFootSideSign = Mathf.Sign(transform.InverseTransformPoint(rightFoot.position).x - hipsLocalX);
            HasCompleteHumanoidRig = !Mathf.Approximately(leftFootSideSign, 0f) &&
                                       !Mathf.Approximately(rightFootSideSign, 0f);
            if (HasCompleteHumanoidRig)
            {
                // Parent-space height remains stable even while the child model yaws, rolls, and changes pose.
                supportFootGroundHeight = GetLowestFootHeightInFigureParent();
            }
        }

        private void CacheZombieFaceProps()
        {
            // Recursive lookup is required because each prop is attached to its nearest animated facial bone.
            leftBloodyEye = FindDescendant(transform, PrototypeCharacterFactory.SwatZombieBloodyEyeLeftName);
            rightBloodyEye = FindDescendant(transform, PrototypeCharacterFactory.SwatZombieBloodyEyeRightName);
            droolStrand = FindDescendant(transform, PrototypeCharacterFactory.SwatZombieDroolStrandName);
            droolDrop = FindDescendant(transform, PrototypeCharacterFactory.SwatZombieDroolDropName);

            // Local values preserve factory-authored sizes and anchor offsets under the scaled FBX hierarchy.
            initialLeftEyeLocalScale = leftBloodyEye != null ? leftBloodyEye.localScale : Vector3.one;
            initialRightEyeLocalScale = rightBloodyEye != null ? rightBloodyEye.localScale : Vector3.one;
            initialDroolStrandLocalPosition = droolStrand != null ? droolStrand.localPosition : Vector3.zero;
            initialDroolDropLocalPosition = droolDrop != null ? droolDrop.localPosition : Vector3.zero;
        }

        private void ApplyZombieOverlay(float deltaTime)
        {
            // Invalid frames or incomplete imported rigs should preserve the readable Mixamo fallback pose.
            if (deltaTime <= 0f || !HasCompleteHumanoidRig || figureRoot == null)
            {
                return;
            }

            // The broad body phase is deliberately slower than the Animator's footstep cadence.
            float phaseSpeed = enemyType == ZombieEnemyType.Armored
                ? ArmoredStumblePhaseSpeed
                : BasicStumblePhaseSpeed;
            AnimationPhase += deltaTime * phaseSpeed;
            float phase = AnimationPhase;

            // Remove the previous frame's parent overlay before evaluating this frame's support-foot displacement.
            figureRoot.localPosition = initialFigureLocalPosition;
            figureRoot.localRotation = initialFigureLocalRotation;

            // Two incommensurate waves keep the side balance from reading as a clean repeated sine walk.
            float sideSway = Mathf.Sin(phase) * MaximumSideSway +
                             Mathf.Sin(phase * 0.43f + 1.2f) * MaximumSideSway * 0.38f;

            // A narrow catch pulse creates a repeated near-fall and recovery instead of a clean sinusoidal march.
            float catchPulse = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(phase * 0.73f + 1.6f)), 4f);

            // A slower fore/aft lurch creates near-falls without changing gameplay collision or targeting.
            float lurch = Mathf.Sin(phase * 0.57f + 2.1f) * MaximumLurchDistance + catchPulse * 0.035f;

            // Irregular roll is the main drunk-stumble cue and is intentionally stronger than a normal walk sway.
            CurrentStumbleRollDegrees = Mathf.Sin(phase) * 9.5f +
                                        Mathf.Sin(phase * 0.41f + 0.7f) * 4.5f;

            // Occasional extra pitch makes the character appear to catch herself before falling forward.
            float stumblePitch = BaseHunchDegrees + catchPulse * 12f;

            // Wandering yaw makes the zombie search drunkenly instead of marching on a perfectly fixed heading.
            float wanderingYaw = Mathf.Sin(phase * 0.61f + 0.4f) * 7f;

            // Rebuild the entire visual-root pose from its baseline so wobble never accumulates over time.
            figureRoot.localPosition = initialFigureLocalPosition + new Vector3(sideSway, 0f, lurch);
            figureRoot.localRotation = initialFigureLocalRotation *
                                       Quaternion.Euler(catchPulse * 2.5f, wanderingYaw, CurrentStumbleRollDegrees);

            // Axes follow the current lane-facing model, keeping overlay intent independent of FBX bone-local conventions.
            Vector3 modelRight = transform.right;
            Vector3 modelUp = transform.up;
            Vector3 modelForward = transform.forward;

            // The spine carries the strong hunch without rotating both feet upward around the visual-root pivot.
            RotateAroundWorldAxis(spine, modelRight, stumblePitch + Mathf.Sin(phase * 0.82f) * 3f);
            RotateAroundWorldAxis(chest, modelForward, -CurrentStumbleRollDegrees * 0.46f);
            RotateAroundWorldAxis(chest, modelUp, Mathf.Sin(phase * 0.66f + 1f) * 5f);
            RotateAroundWorldAxis(hips, modelForward, CurrentStumbleRollDegrees * 0.24f);

            // Head motion trails the torso and the jaw repeatedly hangs open as the model stumbles.
            RotateAroundWorldAxis(head, modelRight, -4f + Mathf.Sin(phase * 1.47f + 0.8f) * 6f);
            RotateAroundWorldAxis(head, modelForward, -CurrentStumbleRollDegrees * 0.35f);
            RotateAroundWorldAxis(jaw, modelRight, 9f + Mathf.Abs(Mathf.Sin(phase * 2.35f)) * 7f);

            // Independent mixed-frequency waves make neither arm mirror the other like a normal walking gait.
            CurrentLeftArmAgitationDegrees = Mathf.Sin(phase * 2.38f + 0.35f) * 22f +
                                             Mathf.Sin(phase * 4.91f) * 7f;
            CurrentRightArmAgitationDegrees = Mathf.Sin(phase * 2.11f + 2.2f) * 25f +
                                              Mathf.Sin(phase * 4.37f + 0.6f) * 8f;

            // Derive each character side from the live shoulders so reaching works despite FBX-specific local axes.
            Vector3 leftOutward = Vector3.ProjectOnPlane(leftUpperArm.position - chest.position, modelUp).normalized;
            Vector3 rightOutward = Vector3.ProjectOnPlane(rightUpperArm.position - chest.position, modelUp).normalized;

            // Aim complete arm chains away from the source rifle hold and toward unequal, agitated reaching directions.
            Vector3 leftUpperDirection = modelForward * (0.64f + Mathf.Sin(phase * 1.31f) * 0.15f) +
                                         modelUp * (0.12f + CurrentLeftArmAgitationDegrees / 95f) +
                                         leftOutward * (0.42f + Mathf.Sin(phase * 2.02f) * 0.14f);
            Vector3 rightUpperDirection = modelForward * (0.56f + Mathf.Sin(phase * 1.09f + 1.8f) * 0.18f) +
                                          modelUp * (0.18f + CurrentRightArmAgitationDegrees / 88f) +
                                          rightOutward * (0.48f + Mathf.Sin(phase * 1.73f + 0.9f) * 0.17f);
            AimBoneToward(leftUpperArm, leftLowerArm, leftUpperDirection);
            AimBoneToward(rightUpperArm, rightLowerArm, rightUpperDirection);

            // Forearms use different vertical beats so one reaches while the other repeatedly flails and drops.
            Vector3 leftLowerDirection = modelForward * 0.82f +
                                         modelUp * (-0.18f + Mathf.Sin(phase * 3.33f + 1.1f) * 0.24f) +
                                         leftOutward * (0.16f + Mathf.Sin(phase * 2.51f) * 0.12f);
            Vector3 rightLowerDirection = modelForward * 0.76f +
                                          modelUp * (-0.10f + Mathf.Sin(phase * 3.07f + 2.6f) * 0.29f) +
                                          rightOutward * (0.20f + Mathf.Sin(phase * 2.27f + 0.8f) * 0.14f);
            AimBoneToward(leftLowerArm, leftHand, leftLowerDirection);
            AimBoneToward(rightLowerArm, rightHand, rightLowerDirection);

            // Hands twitch around the newly authored reach instead of preserving a visible rifle grip.
            RotateAroundWorldAxis(leftHand, modelUp, Mathf.Sin(phase * 5.2f) * 16f);
            RotateAroundWorldAxis(rightHand, modelUp, Mathf.Sin(phase * 4.8f + 1.8f) * 18f);

            // Asymmetric leg overlays let one knee soften while the other foot catches the next authored step.
            float leftCollapse = Mathf.Max(0f, Mathf.Sin(phase * 0.94f + 0.5f));
            float rightCollapse = Mathf.Max(0f, Mathf.Sin(phase * 0.83f + 3.0f));
            float leftDrag = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(phase * 0.79f + 2.4f)), 3f);
            RotateAroundWorldAxis(leftUpperLeg, modelRight, leftCollapse * 6f - rightCollapse * 2f - leftDrag * 5f);
            RotateAroundWorldAxis(rightUpperLeg, modelRight, rightCollapse * 8f - leftCollapse * 2f + catchPulse * 4f);
            RotateAroundWorldAxis(leftLowerLeg, modelRight, leftCollapse * 7f - leftDrag * 4f);
            RotateAroundWorldAxis(rightLowerLeg, modelRight, rightCollapse * 10f + catchPulse * 5f);

            // Correct only anatomical impossibilities after the uneven collapse, retaining a loose zombie toe-out.
            KeepFootOnOwnSide(leftUpperLeg, leftFoot, leftFootSideSign);
            KeepFootOnOwnSide(rightUpperLeg, rightFoot, rightFootSideSign);
            ClampFootYaw(leftFoot, leftToes);
            ClampFootYaw(rightFoot, rightToes);

            // Counter the root roll and leg collapse so at least one boot remains planted on the cached road plane.
            GroundSupportFoot();

            // A small asynchronous pulse keeps the oversized infected eyes visibly alive at gameplay distance.
            float eyePulse = 1f + Mathf.Sin(phase * 3.7f) * 0.07f;
            if (leftBloodyEye != null)
            {
                leftBloodyEye.localScale = initialLeftEyeLocalScale * eyePulse;
            }

            if (rightBloodyEye != null)
            {
                rightBloodyEye.localScale = initialRightEyeLocalScale * (2f - eyePulse);
            }

            // Drool remains gravity-biased while its anchor follows the animated jaw and head.
            float droolSwingDegrees = Mathf.Sin(phase * 1.9f + 0.4f) * 10f;
            if (droolStrand != null)
            {
                droolStrand.localPosition = initialDroolStrandLocalPosition;
                droolStrand.rotation = Quaternion.AngleAxis(droolSwingDegrees, modelForward);
            }

            if (droolDrop != null)
            {
                droolDrop.localPosition = initialDroolDropLocalPosition;
            }

            IsAnimating = true;
        }

        private static void RotateAroundWorldAxis(Transform bone, Vector3 axis, float degrees)
        {
            if (bone == null || axis.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            // Pre-multiplication applies an additive world-axis correction over the freshly evaluated Humanoid clip.
            bone.rotation = Quaternion.AngleAxis(degrees, axis.normalized) * bone.rotation;
        }

        private static void AimBoneToward(Transform bone, Transform child, Vector3 desiredWorldDirection)
        {
            if (bone == null || child == null || desiredWorldDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            // Rotating the current child direction onto the requested vector preserves the imported bone's twist basis.
            Vector3 currentDirection = child.position - bone.position;
            if (currentDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            bone.rotation = Quaternion.FromToRotation(
                currentDirection.normalized,
                desiredWorldDirection.normalized) * bone.rotation;
        }

        private void KeepFootOnOwnSide(Transform upperLeg, Transform foot, float sideSign)
        {
            // Signed distance becomes negative only when an animated boot crosses through the opposite hip side.
            float hipsLocalX = transform.InverseTransformPoint(hips.position).x;
            float footLocalX = transform.InverseTransformPoint(foot.position).x;
            float signedSideDistance = (footLocalX - hipsLocalX) * sideSign;
            if (signedSideDistance >= MinimumFootSideDistance)
            {
                return;
            }

            // A restrained upper-leg abduction moves the connected thigh, shin, and boot as one anatomical chain.
            float legLength = Mathf.Max(Vector3.Distance(upperLeg.position, foot.position), 0.01f);
            float requiredDistance = MinimumFootSideDistance - signedSideDistance;
            float correctionDegrees = Mathf.Atan2(requiredDistance, legLength) * Mathf.Rad2Deg;
            upperLeg.rotation = Quaternion.AngleAxis(sideSign * correctionDegrees, transform.forward) * upperLeg.rotation;
        }

        private void ClampFootYaw(Transform foot, Transform toes)
        {
            // Toe-to-foot direction describes the visible boot heading without relying on imported transform axes.
            Vector3 actorUp = transform.up;
            Vector3 actorForward = Vector3.ProjectOnPlane(transform.forward, actorUp).normalized;
            Vector3 toeDirection = Vector3.ProjectOnPlane(toes.position - foot.position, actorUp).normalized;
            if (actorForward.sqrMagnitude <= 0.0001f || toeDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            // Preserve some drunken toe-out while removing the sideways-foot defect seen in earlier character work.
            float currentYaw = Vector3.SignedAngle(actorForward, toeDirection, actorUp);
            float allowedYaw = Mathf.Clamp(currentYaw, -MaximumFootYawDegrees, MaximumFootYawDegrees);
            foot.rotation = Quaternion.AngleAxis(-(currentYaw - allowedYaw), actorUp) * foot.rotation;
        }

        private float GetLowestFootHeightInFigureParent()
        {
            // Figure-local motion is expressed in its parent's coordinates, so measure both boots in that same space.
            Transform figureParent = figureRoot != null ? figureRoot.parent : null;
            float leftHeight = figureParent != null
                ? figureParent.InverseTransformPoint(leftFoot.position).y
                : leftFoot.position.y;
            float rightHeight = figureParent != null
                ? figureParent.InverseTransformPoint(rightFoot.position).y
                : rightFoot.position.y;
            return Mathf.Min(leftHeight, rightHeight);
        }

        private void GroundSupportFoot()
        {
            // The lower boot is the current support foot even when the walk clip trades support mid-cycle.
            float currentLowestFootHeight = GetLowestFootHeightInFigureParent();
            float heightCorrection = supportFootGroundHeight - currentLowestFootHeight;

            // Applying the correction in parent-local Y preserves lateral sway, lurch, yaw, and roll unchanged.
            figureRoot.localPosition += Vector3.up * heightCorrection;

            // Re-measure after the transform update so diagnostics describe the visible corrected pose.
            CurrentSupportFootGroundError = GetLowestFootHeightInFigureParent() - supportFootGroundHeight;
        }

        private static Transform FindDescendant(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            // Exact self matching makes the helper work for both model roots and recursive child calls.
            if (root.name == childName)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                // Depth-first search follows the deterministic imported hierarchy and generated attachment order.
                Transform match = FindDescendant(child, childName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }
    }
}
