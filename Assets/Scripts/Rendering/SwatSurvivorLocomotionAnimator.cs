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

        // Keep both boots visually straight down the lane; two degrees only absorbs floating-point retarget jitter.
        public const float MaximumFootYawFromTravelDirection = 2f;

        // Keeping each boot at least two centimetres on its own side prevents visible leg crossing.
        public const float MinimumFootSideDistance = 0.02f;

        // Motion below this speed is scene jitter rather than deliberate squad travel.
        private const float MovementSpeedThreshold = 0.04f;

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

        // Consecutive world positions provide frame-rate-independent movement-state detection.
        private Vector3 previousWorldPosition;

        // The first sample only establishes a baseline and must not start the run clip.
        private bool hasPreviousWorldPosition;

        public bool IsMoving { get; private set; }

        // These values expose the final post-retarget pose used by rendering and regression tests.
        public float LeftFootYawFromTravelDirection { get; private set; }
        public float RightFootYawFromTravelDirection { get; private set; }
        public float LeftFootCentrelineCrossing { get; private set; }
        public float RightFootCentrelineCrossing { get; private set; }

        public void SetGameplayMoving(bool isMoving)
        {
            // PlayerSquad owns the authoritative run state and pushes changes without relying on component update order.
            SetMoving(isMoving);
        }

        public void Configure(Animator animator, Transform authoritativeMovementRoot)
        {
            // Store exact dependencies so the component never searches the whole scene at runtime.
            modelAnimator = animator;
            movementRoot = authoritativeMovementRoot;

            // PlayerSquad is added immediately after factory construction, so LateUpdate retries this lookup when needed.
            playerSquad = movementRoot != null ? movementRoot.GetComponent<PlayerSquad>() : null;

            // Apply runtime-only Animator settings whenever a factory assigns or replaces its controller.
            ConfigureAnimatorRuntimeSettings();

            // Cache the imported rest geometry before live animation starts rewriting Humanoid bone transforms.
            CacheGaitCorrectionBaseline();

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
            }
        }
    }
}
