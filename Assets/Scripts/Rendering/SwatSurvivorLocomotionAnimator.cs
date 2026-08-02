using LaneSurvivor.Gameplay;
using UnityEngine;

namespace LaneSurvivor.Rendering
{
    public sealed class SwatSurvivorLocomotionAnimator : MonoBehaviour
    {
        // The generated controller uses one stable parameter for its idle/walk transition.
        public const string MovingParameterName = "Moving";

        // The base layer owns the complete authored idle/walk transition shown by the gameplay camera.
        public const int LocomotionLayerIndex = 0;

        // Full weight prevents the idle pose from visually suppressing the retargeted Mixamo walk.
        public const float LocomotionLayerWeight = 1f;

        // Motion below this speed is scene jitter rather than deliberate squad travel.
        private const float MovementSpeedThreshold = 0.04f;

        // The Animator lives on the imported model root while movement is measured on the gameplay squad root.
        private Animator modelAnimator;

        // PlayerSquad directly translates this root for lane and forward motion.
        private Transform movementRoot;

        // Runtime scenes expose their authoritative movement state after the factory has constructed this component.
        private PlayerSquad playerSquad;

        // Consecutive world positions provide frame-rate-independent movement-state detection.
        private Vector3 previousWorldPosition;

        // The first sample only establishes a baseline and must not start the run clip.
        private bool hasPreviousWorldPosition;

        public bool IsMoving { get; private set; }

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

            if (modelAnimator != null && modelAnimator.runtimeAnimatorController != null)
            {
                // Damp-free bool switching lets the controller's authored transition duration control blending.
                modelAnimator.SetBool(MovingParameterName, isMoving);
            }
        }
    }
}
