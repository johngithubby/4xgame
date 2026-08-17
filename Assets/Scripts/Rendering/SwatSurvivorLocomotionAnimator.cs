using LaneSurvivor.Gameplay;
using UnityEngine;

namespace LaneSurvivor.Rendering
{
    public sealed class SwatSurvivorLocomotionAnimator : MonoBehaviour
    {
        // The generated controller uses one stable parameter for its idle/run transition.
        public const string MovingParameterName = "Moving";

        // The base layer owns the complete authored idle/run transition shown by the gameplay camera.
        public const int LocomotionLayerIndex = 0;

        // Full weight prevents the idle pose from visually suppressing the retargeted Mixamo run.
        public const float LocomotionLayerWeight = 1f;

        // All squad members enter this named state with different cycle offsets so their stride does not look cloned.
        public const string RifleRunStateName = "Rifle Run";

        // Match the controller's authored idle-to-run blend while applying each member's normalized phase offset.
        private const float RifleRunTransitionSeconds = 0.12f;

        // Keep both boots visually straight down the lane; two degrees only absorbs floating-point retarget jitter.
        public const float MaximumFootYawFromTravelDirection = 2f;

        // Keeping each boot at least two centimetres on its own side prevents visible leg crossing.
        public const float MinimumFootSideDistance = 0.02f;

        // Motion below this speed is scene jitter rather than deliberate squad travel.
        private const float MovementSpeedThreshold = 0.04f;

        // Automatic shots arrive every 0.35 seconds, so this decay keeps the rifle aimed throughout a volley.
        private const float WeaponAimRecoverySpeed = 2.4f;

        // Keep the weapon locked for one extra rendered frame after Unity schedules the tracer for destruction.
        public const float WeaponAimTracerSafetySeconds = 0.05f;

        // Tests and runtime diagnostics share one strict tolerance for visible barrel-to-target alignment.
        public const float MaximumWeaponAimErrorDegrees = 1.5f;

        // The Animator lives on the imported model root while movement is measured on the gameplay squad root.
        private Animator modelAnimator;

        // PlayerSquad directly translates this root for lane and forward motion.
        private Transform movementRoot;

        // Runtime scenes expose their authoritative movement state after the factory has constructed this component.
        private PlayerSquad playerSquad;

        // Cached Humanoid gait bones allow a small post-retarget correction without name-dependent hierarchy searches.
        private Transform hips;
        private Transform leftUpperLeg;
        private Transform leftFoot;
        private Transform leftToes;
        private Transform rightUpperLeg;
        private Transform rightFoot;
        private Transform rightToes;

        // The fixed gameplay travel heading provides an objective target even when the imported rest feet are misaligned.
        private Vector3 travelDirectionLocal;

        // Rest-pose signs identify which local side of the hips belongs to each foot on this imported skeleton.
        private float leftFootSideSign;
        private float rightFootSideSign;

        // Gait correction remains disabled when an incomplete test Avatar does not expose the required foot chain.
        private bool hasGaitCorrectionBaseline;

        // The imported weapon-body bone directly deforms the visible gun without disturbing Humanoid locomotion bones.
        private Transform weaponAimPivot;

        // This runtime anchor is parented to the imported barrel-end bone and is the real tracer origin.
        private Transform visibleWeaponMuzzle;

        // The imported stock joint and source-muzzle position define the complete physical rifle direction.
        private Transform visibleWeaponStock;

        // The FBX-local weapon rotation is restored smoothly after the current automatic-fire volley ends.
        private Quaternion weaponAimPivotRestLocalRotation;

        // A valid imported pivot/muzzle pair is required before target-facing correction can run.
        private bool hasWeaponAimBaseline;

        // The last selected zombie point remains stable between automatic shots.
        private Vector3 weaponAimTarget;

        // Aim weight holds at one while firing, then blends the visible rifle back to its authored run pose.
        private float weaponAimWeight;

        // This timer prevents the visible rifle from beginning its recovery while an orange tracer still exists.
        private float weaponAimHoldRemaining;

        // Consecutive world positions provide frame-rate-independent movement-state detection.
        private Vector3 previousWorldPosition;

        // The first sample only establishes a baseline and must not start the run clip.
        private bool hasPreviousWorldPosition;

        // The normalized phase is serialized so scene-built squads retain their deliberately staggered strides.
        [SerializeField, Range(0f, 1f)]
        private float locomotionPhaseOffset;

        public bool IsMoving { get; private set; }

        // Tests and diagnostics can confirm that the three visible survivors do not share one synchronized gait.
        public float LocomotionPhaseOffset => locomotionPhaseOffset;

        // These values expose the final post-retarget pose used by rendering and regression tests.
        public float LeftFootYawFromTravelDirection { get; private set; }
        public float RightFootYawFromTravelDirection { get; private set; }
        public float LeftFootCentrelineCrossing { get; private set; }
        public float RightFootCentrelineCrossing { get; private set; }

        // The final error is measured from the visible muzzle forward axis to the current zombie point.
        public float WeaponAimErrorDegrees { get; private set; }

        public void SetGameplayMoving(bool isMoving)
        {
            // PlayerSquad owns the authoritative run state and pushes changes without relying on component update order.
            SetMoving(isMoving);
        }

        public void Configure(Animator animator, Transform authoritativeMovementRoot, float normalizedPhaseOffset)
        {
            // Store exact dependencies so the component never searches the whole scene at runtime.
            modelAnimator = animator;
            movementRoot = authoritativeMovementRoot;

            // Repeat out-of-range input safely while preserving deliberate thirds of the authored run cycle.
            locomotionPhaseOffset = Mathf.Repeat(normalizedPhaseOffset, 1f);

            // PlayerSquad is added immediately after factory construction, so LateUpdate retries this lookup when needed.
            playerSquad = movementRoot != null ? movementRoot.GetComponent<PlayerSquad>() : null;

            // Apply runtime-only Animator settings whenever a factory assigns or replaces its controller.
            ConfigureAnimatorRuntimeSettings();

            // Cache the imported rest geometry before live animation starts rewriting Humanoid bone transforms.
            CacheGaitCorrectionBaseline();

            // Cache the actual imported rifle pivot and visible barrel anchor used by combat feedback.
            CacheWeaponAimBaseline();

            ResetMotionSample();
            SetMoving(false);
        }

        private void Awake()
        {
            // Scene-built characters deserialize this component without the runtime-only references assigned by Configure.
            modelAnimator ??= GetComponent<Animator>();
            playerSquad ??= GetComponentInParent<PlayerSquad>();

            // Prefer the component that owns movement; factory-only objects fall back to the known two-level hierarchy.
            if (movementRoot == null)
            {
                movementRoot = playerSquad != null
                    ? playerSquad.transform
                    : transform.parent != null
                        ? transform.parent.parent
                        : null;
            }

            // Scene-deserialized Animators must still receive the same deterministic runtime settings as factory instances.
            ConfigureAnimatorRuntimeSettings();

            // Cache the imported rest geometry before the first rendered locomotion cycle.
            CacheGaitCorrectionBaseline();

            // Scene-deserialized characters must rediscover the imported rifle hierarchy as well.
            CacheWeaponAimBaseline();

            ResetMotionSample();
            SetMoving(playerSquad != null && playerSquad.IsMoving);
        }

        private void ConfigureAnimatorRuntimeSettings()
        {
            if (modelAnimator == null)
            {
                return;
            }

            // Gameplay owns translation; both Mixamo actions remain in place.
            modelAnimator.applyRootMotion = false;

            // The gameplay camera must keep evaluating the character even if imported renderer bounds lag one stride.
            modelAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Explicitly restore full locomotion influence for scene-built Animator instances.
            if (modelAnimator.runtimeAnimatorController != null && modelAnimator.layerCount > LocomotionLayerIndex)
            {
                modelAnimator.SetLayerWeight(LocomotionLayerIndex, LocomotionLayerWeight);
            }
        }

        private void OnEnable()
        {
            // Re-enabling after a scene transition should not compare against a stale position.
            ResetMotionSample();
        }

        public bool PlayWeaponShot(Transform firingMuzzle, Vector3 targetPoint)
        {
            // Only the anchor created on this imported rifle may steer its visible MPX weapon hierarchy.
            if (!hasWeaponAimBaseline || firingMuzzle == null || firingMuzzle != visibleWeaponMuzzle)
            {
                return false;
            }

            // A zero-length shot cannot define a stable look direction and should keep the authored weapon pose.
            if ((targetPoint - visibleWeaponMuzzle.position).sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            // Store the same target point used by damage and tracer feedback so every visual agrees.
            weaponAimTarget = targetPoint;
            weaponAimWeight = 1f;

            // Refresh the hold on every automatic shot so the gun stays aligned throughout a continuous volley.
            weaponAimHoldRemaining = GameplayVisuals.ShotTracerLifetimeSeconds + WeaponAimTracerSafetySeconds;

            // Apply immediately because AutoShooter reads the muzzle position and spawns effects in this same frame.
            ApplyVisibleWeaponAim();
            return true;
        }

        private void LateUpdate()
        {
            // A missing Animator/controller or movement root leaves no safe state to evaluate.
            if (modelAnimator == null || modelAnimator.runtimeAnimatorController == null || movementRoot == null)
            {
                return;
            }

            // Prefer gameplay's explicit state; it remains stable even when camera-relative presentation keeps actors centred.
            playerSquad ??= movementRoot.GetComponent<PlayerSquad>();
            if (playerSquad != null)
            {
                SetMoving(playerSquad.IsMoving);
                previousWorldPosition = movementRoot.position;
                hasPreviousWorldPosition = true;

                // Animator evaluation has completed before LateUpdate, so correct the exact pose that will be rendered.
                StabilizeRetargetedGait();

                // Re-apply target aim after every Animator evaluation so the run clip cannot pull the gun off target.
                UpdateVisibleWeaponAim();
                return;
            }

            // Factory-only tests without a PlayerSquad component fall back to transform displacement detection.
            if (!hasPreviousWorldPosition)
            {
                ResetMotionSample();
                return;
            }

            // Lane and forward travel occur in the ground plane, so vertical animation does not affect the state.
            Vector3 movement = movementRoot.position - previousWorldPosition;
            movement.y = 0f;
            previousWorldPosition = movementRoot.position;

            // Converting displacement to speed keeps the threshold stable across frame rates.
            float speed = movement.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            SetMoving(speed > MovementSpeedThreshold);

            // Factory-only previews receive the same rendered-pose correction as the production scene.
            StabilizeRetargetedGait();

            // Isolated previews retain the same imported-weapon aiming and recovery behavior as gameplay.
            UpdateVisibleWeaponAim();
        }

        private void CacheWeaponAimBaseline()
        {
            // Exact imported names are stable asset contracts documented by the factory and covered by tests.
            weaponAimPivot = FindDescendant(transform, PrototypeCharacterFactory.SwatWeaponAimPivotName);
            visibleWeaponMuzzle = FindDescendant(transform, PlayerSquad.WeaponMuzzleAnchorName);
            visibleWeaponStock = FindDescendant(transform, PrototypeCharacterFactory.SwatWeaponStockBoneName);
            hasWeaponAimBaseline = weaponAimPivot != null &&
                                    visibleWeaponMuzzle != null &&
                                    visibleWeaponMuzzle.IsChildOf(weaponAimPivot) &&
                                    visibleWeaponStock != null &&
                                    visibleWeaponStock.IsChildOf(weaponAimPivot);
            if (!hasWeaponAimBaseline)
            {
                WeaponAimErrorDegrees = 0f;
                return;
            }

            // Mixamo clips do not animate the weapon-specific body bone, so its imported local rotation is authoritative.
            weaponAimPivotRestLocalRotation = weaponAimPivot.localRotation;

            weaponAimWeight = 0f;
            weaponAimHoldRemaining = 0f;
            WeaponAimErrorDegrees = 0f;
        }

        private void UpdateVisibleWeaponAim()
        {
            if (!hasWeaponAimBaseline)
            {
                return;
            }

            if (weaponAimHoldRemaining > 0f)
            {
                // A live tracer must never outlast the target-facing pose that visually launched it.
                weaponAimWeight = 1f;
                ApplyVisibleWeaponAim();

                // Count down only after the fully aimed pose has been applied for this rendered frame.
                weaponAimHoldRemaining = Mathf.Max(
                    0f,
                    weaponAimHoldRemaining - Mathf.Max(Time.deltaTime, 0f));
                return;
            }

            if (weaponAimWeight > 0.0001f)
            {
                // Continuous correction follows the same fixed zombie point while the player advances during the flash.
                ApplyVisibleWeaponAim();
                weaponAimWeight = Mathf.MoveTowards(
                    weaponAimWeight,
                    0f,
                    WeaponAimRecoverySpeed * Mathf.Max(Time.deltaTime, 0f));
                return;
            }

            // Restore the imported weapon-body pose after firing so idle/run authoring remains unchanged between encounters.
            weaponAimPivot.localRotation = weaponAimPivotRestLocalRotation;
            WeaponAimErrorDegrees = 0f;
        }

        private void ApplyVisibleWeaponAim()
        {
            // Start from the current target line after the animated hand has positioned the complete weapon hierarchy.
            Vector3 targetDirection = weaponAimTarget - visibleWeaponMuzzle.position;
            if (targetDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            // Aim from the rendered muzzle-to-stock axis, because imported helper-bone axes do not match the visible MPX.
            Vector3 renderedWeaponDirection = GetVisibleWeaponDirection();
            if (renderedWeaponDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            // Rotating the skinned weapon-body bone moves every visible gun part while leaving the running body untouched.
            Quaternion exactWorldAim = Quaternion.FromToRotation(
                renderedWeaponDirection.normalized,
                targetDirection.normalized) * weaponAimPivot.rotation;

            // Convert to the animated hand's current local frame before blending from the imported rest rotation.
            Quaternion exactLocalAim = weaponAimPivot.parent != null
                ? Quaternion.Inverse(weaponAimPivot.parent.rotation) * exactWorldAim
                : exactWorldAim;
            weaponAimPivot.localRotation = Quaternion.Slerp(
                weaponAimPivotRestLocalRotation,
                exactLocalAim,
                weaponAimWeight);

            // The true barrel tip sits farther from the hand pivot, so converge after each rotation moves that origin.
            if (weaponAimWeight > 0.999f)
            {
                for (int refinementIndex = 0; refinementIndex < 3; refinementIndex++)
                {
                    // Recalculate both vectors because rotating around the hand changes the barrel tip's world position.
                    Vector3 refinedDirection = weaponAimTarget - visibleWeaponMuzzle.position;
                    Vector3 refinedRenderedWeaponDirection = GetVisibleWeaponDirection();
                    if (refinedDirection.sqrMagnitude <= 0.0001f || refinedRenderedWeaponDirection.sqrMagnitude <= 0.0001f)
                    {
                        break;
                    }

                    // Each correction removes the remaining angular error introduced by the previous origin movement.
                    weaponAimPivot.rotation = Quaternion.FromToRotation(
                        refinedRenderedWeaponDirection.normalized,
                        refinedDirection.normalized) * weaponAimPivot.rotation;
                }
            }

            // Keep the effect anchor's +Z aligned with the same target after the visible mesh has finished rotating.
            Vector3 finalDirection = weaponAimTarget - visibleWeaponMuzzle.position;
            if (finalDirection.sqrMagnitude > 0.0001f)
            {
                visibleWeaponMuzzle.rotation = Quaternion.LookRotation(finalDirection.normalized, transform.up);
            }

            // Measure the exact rendered rifle after correction so invisible helper axes cannot hide visual regressions.
            Vector3 finalRenderedWeaponDirection = GetVisibleWeaponDirection();
            WeaponAimErrorDegrees = finalDirection.sqrMagnitude > 0.0001f
                ? Vector3.Angle(finalRenderedWeaponDirection, finalDirection.normalized)
                : 0f;
        }

        private Vector3 GetVisibleWeaponDirection()
        {
            // The live imported joint positions match the visible stock and muzzle bounds while remaining allocation-free.
            return visibleWeaponMuzzle.position - visibleWeaponStock.position;
        }

        private static Transform FindDescendant(Transform root, string descendantName)
        {
            // A missing branch cannot contain the requested imported weapon joint.
            if (root == null)
            {
                return null;
            }

            // Check the current node so callers can search from either the model or a nested imported branch.
            if (root.name == descendantName)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                // Depth-first search follows deterministic FBX hierarchy order and stops at the first exact name.
                Transform match = FindDescendant(child, descendantName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private void CacheGaitCorrectionBaseline()
        {
            // A missing or invalid Humanoid Avatar cannot provide stable cross-rig bone mappings.
            if (modelAnimator == null || modelAnimator.avatar == null || !modelAnimator.isHuman || !modelAnimator.avatar.isValid)
            {
                hasGaitCorrectionBaseline = false;
                return;
            }

            // Resolve every required joint from Unity's Humanoid mapping rather than Character Creator bone names.
            hips = modelAnimator.GetBoneTransform(HumanBodyBones.Hips);
            leftUpperLeg = modelAnimator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            leftFoot = modelAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
            leftToes = modelAnimator.GetBoneTransform(HumanBodyBones.LeftToes);
            rightUpperLeg = modelAnimator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            rightFoot = modelAnimator.GetBoneTransform(HumanBodyBones.RightFoot);
            rightToes = modelAnimator.GetBoneTransform(HumanBodyBones.RightToes);

            // Any missing joint disables the complete correction instead of creating asymmetric partial posing.
            hasGaitCorrectionBaseline = hips != null &&
                                        leftUpperLeg != null &&
                                        leftFoot != null &&
                                        leftToes != null &&
                                        rightUpperLeg != null &&
                                        rightFoot != null &&
                                        rightToes != null;
            if (!hasGaitCorrectionBaseline)
            {
                return;
            }

            // PlayerSquad advances along world +Z, so both boots must be judged against that travel direction.
            travelDirectionLocal = transform.InverseTransformDirection(Vector3.forward);
            travelDirectionLocal = Vector3.ProjectOnPlane(travelDirectionLocal, Vector3.up).normalized;

            // Side signs make the centreline rule independent of left/right axis conventions in the imported FBX.
            float hipsLocalX = transform.InverseTransformPoint(hips.position).x;
            leftFootSideSign = Mathf.Sign(transform.InverseTransformPoint(leftFoot.position).x - hipsLocalX);
            rightFootSideSign = Mathf.Sign(transform.InverseTransformPoint(rightFoot.position).x - hipsLocalX);

            // Degenerate overlapping rest feet cannot define a meaningful side and therefore disable correction safely.
            hasGaitCorrectionBaseline = travelDirectionLocal.sqrMagnitude > 0.0001f &&
                                        GetPlanarToeDirectionLocal(leftFoot, leftToes).sqrMagnitude > 0.0001f &&
                                        GetPlanarToeDirectionLocal(rightFoot, rightToes).sqrMagnitude > 0.0001f &&
                                        !Mathf.Approximately(leftFootSideSign, 0f) &&
                                        !Mathf.Approximately(rightFootSideSign, 0f);
        }

        private void StabilizeRetargetedGait()
        {
            // Idle preserves the downloaded pose exactly; only an active retargeted walk needs correction.
            if (!IsMoving || !hasGaitCorrectionBaseline)
            {
                return;
            }

            // Correct centreline crossing at the hip so the whole thigh, shin, ankle, and boot remain connected.
            KeepFootOnRestSide(leftUpperLeg, leftFoot, leftFootSideSign);
            KeepFootOnRestSide(rightUpperLeg, rightFoot, rightFootSideSign);

            // Apply yaw last so the lateral hip correction cannot rotate a boot back toward the opposite lane.
            ClampFootYaw(leftFoot, leftToes);
            ClampFootYaw(rightFoot, rightToes);

            // Measure the corrected bones rather than reporting the pre-correction Animator output.
            UpdateGaitMeasurements();
        }

        private void KeepFootOnRestSide(Transform upperLeg, Transform foot, float sideSign)
        {
            // Signed distance is positive on the foot's valid rest side and negative after crossing the hips.
            float hipsLocalX = transform.InverseTransformPoint(hips.position).x;
            float footLocalX = transform.InverseTransformPoint(foot.position).x;
            float signedSideDistance = (footLocalX - hipsLocalX) * sideSign;
            if (signedSideDistance >= MinimumFootSideDistance)
            {
                return;
            }

            // Convert the missing lateral distance into a restrained anatomical abduction at the upper leg.
            float legLength = Mathf.Max(Vector3.Distance(upperLeg.position, foot.position), 0.01f);
            float requiredDistance = MinimumFootSideDistance - signedSideDistance;
            float correctionDegrees = Mathf.Atan2(requiredDistance, legLength) * Mathf.Rad2Deg;

            // Rotating around model-forward moves a downward leg laterally without altering stride flexion.
            upperLeg.rotation = Quaternion.AngleAxis(sideSign * correctionDegrees, transform.forward) * upperLeg.rotation;
        }

        private void ClampFootYaw(Transform foot, Transform toes)
        {
            // Compare the current boot heading with the lane's actual travel heading in the gameplay ground plane.
            Vector3 currentToeDirectionLocal = GetPlanarToeDirectionLocal(foot, toes);
            if (currentToeDirectionLocal.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float currentYaw = Vector3.SignedAngle(travelDirectionLocal, currentToeDirectionLocal, Vector3.up);
            float allowedYaw = Mathf.Clamp(currentYaw, -MaximumFootYawFromTravelDirection, MaximumFootYawFromTravelDirection);
            float excessYaw = currentYaw - allowedYaw;

            // Removing only the excess preserves natural toe-out while eliminating the sideways boot defect.
            foot.rotation = Quaternion.AngleAxis(-excessYaw, transform.up) * foot.rotation;
        }

        private Vector3 GetPlanarToeDirectionLocal(Transform foot, Transform toes)
        {
            // Convert both joints into model space before projecting away vertical ankle pitch.
            Vector3 footLocalPosition = transform.InverseTransformPoint(foot.position);
            Vector3 toesLocalPosition = transform.InverseTransformPoint(toes.position);
            Vector3 toeDirection = Vector3.ProjectOnPlane(toesLocalPosition - footLocalPosition, Vector3.up);
            return toeDirection.normalized;
        }

        private void UpdateGaitMeasurements()
        {
            // Final boot yaw is measured against the same objective lane heading used by the correction.
            LeftFootYawFromTravelDirection = Mathf.Abs(Vector3.SignedAngle(
                travelDirectionLocal,
                GetPlanarToeDirectionLocal(leftFoot, leftToes),
                Vector3.up));
            RightFootYawFromTravelDirection = Mathf.Abs(Vector3.SignedAngle(
                travelDirectionLocal,
                GetPlanarToeDirectionLocal(rightFoot, rightToes),
                Vector3.up));

            // Positive values report only actual opposite-side penetration; valid stance width reads as zero.
            float hipsLocalX = transform.InverseTransformPoint(hips.position).x;
            float leftSignedDistance = (transform.InverseTransformPoint(leftFoot.position).x - hipsLocalX) * leftFootSideSign;
            float rightSignedDistance = (transform.InverseTransformPoint(rightFoot.position).x - hipsLocalX) * rightFootSideSign;
            LeftFootCentrelineCrossing = Mathf.Max(0f, -leftSignedDistance);
            RightFootCentrelineCrossing = Mathf.Max(0f, -rightSignedDistance);
        }

        private void ResetMotionSample()
        {
            // Hand-built tests can configure dependencies after OnEnable, so null roots remain safe.
            if (movementRoot == null)
            {
                hasPreviousWorldPosition = false;
                return;
            }

            previousWorldPosition = movementRoot.position;
            hasPreviousWorldPosition = true;
        }

        private void SetMoving(bool isMoving)
        {
            // Only a false-to-true edge should seek into the run clip; LateUpdate repeats the current state every frame.
            bool startedMoving = isMoving && !IsMoving;

            // Public state lets tests verify clip switching without depending on Animator internals.
            IsMoving = isMoving;

            // Idle has no gait defect, and clearing stale values makes state transitions deterministic for diagnostics.
            if (!isMoving)
            {
                LeftFootYawFromTravelDirection = 0f;
                RightFootYawFromTravelDirection = 0f;
                LeftFootCentrelineCrossing = 0f;
                RightFootCentrelineCrossing = 0f;
            }

            if (modelAnimator != null && modelAnimator.runtimeAnimatorController != null)
            {
                // Damp-free bool switching lets the controller's authored transition duration control blending.
                modelAnimator.SetBool(MovingParameterName, isMoving);

                if (startedMoving)
                {
                    // Offset only time, not playback speed, so every survivor keeps the same foot-to-road velocity.
                    modelAnimator.CrossFadeInFixedTime(
                        RifleRunStateName,
                        RifleRunTransitionSeconds,
                        LocomotionLayerIndex,
                        locomotionPhaseOffset);
                }
            }
        }
    }
}
