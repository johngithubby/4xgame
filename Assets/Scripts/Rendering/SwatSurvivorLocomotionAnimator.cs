using UnityEngine;

namespace LaneSurvivor.Rendering
{
    public sealed class SwatSurvivorLocomotionAnimator : MonoBehaviour
    {
        // The generated controller uses one stable parameter for its idle/run transition.
        public const string MovingParameterName = "Moving";

        // Motion below this speed is scene jitter rather than deliberate squad travel.
        private const float MovementSpeedThreshold = 0.04f;

        // The Animator lives on the imported model root while movement is measured on the gameplay survivor root.
        private Animator modelAnimator;

        // The survivor root inherits authoritative PlayerSquad lane and forward motion.
        private Transform movementRoot;

        // Consecutive world positions provide frame-rate-independent movement-state detection.
        private Vector3 previousWorldPosition;

        // The first sample only establishes a baseline and must not start the run clip.
        private bool hasPreviousWorldPosition;

        public bool IsMoving { get; private set; }

        public void Configure(Animator animator, Transform authoritativeMovementRoot)
        {
            // Store exact dependencies so the component never searches the whole scene at runtime.
            modelAnimator = animator;
            movementRoot = authoritativeMovementRoot;

            // Imported locomotion is in-place; gameplay code remains the only system moving the squad.
            if (modelAnimator != null)
            {
                modelAnimator.applyRootMotion = false;
            }

            ResetMotionSample();
            SetMoving(false);
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

            // The first live frame establishes the comparison position.
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
