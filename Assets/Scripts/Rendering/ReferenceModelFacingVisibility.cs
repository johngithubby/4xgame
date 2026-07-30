using UnityEngine;

namespace LaneSurvivor.Rendering
{
    public sealed class ReferenceModelFacingVisibility : MonoBehaviour
    {
        // A small positive dot identifies cameras that have crossed to the zombie side of the soldier.
        private const float MinimumZombieSideDot = 0.10f;

        // The renderer owns the approved front texture for cameras looking from the zombie side.
        [SerializeField]
        private Renderer frontRenderer;

        // The rear renderer is the correct chase-camera view when soldiers run toward zombies.
        [SerializeField]
        private Renderer rearRenderer;

        // The facing root supplies local +Z, which is the survivor's zombie-facing direction.
        [SerializeField]
        private Transform facingRoot;

        public void Configure(Renderer rendererToControl, Renderer rearRendererToControl, Transform rootThatFacesZombies)
        {
            // Store the front renderer so runtime camera changes can toggle it without hierarchy searches.
            frontRenderer = rendererToControl;

            // Store the rear renderer so chase cameras see the side that actually faces the player.
            rearRenderer = rearRendererToControl;

            // Store the survivor root because its forward direction is the authoritative aim/run direction.
            facingRoot = rootThatFacesZombies;

            // Evaluate immediately for scenes where the camera already exists before this component enables.
            UpdateVisibility();
        }

        private void OnEnable()
        {
            // Scene activation can happen before Configure in tests, so this path is intentionally null-safe.
            UpdateVisibility();
        }

        private void LateUpdate()
        {
            // LateUpdate follows camera movement and survivor animation before deciding which side should render.
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            // Keep the texture visible in edit-mode factory tests or unusual scenes without a configured renderer.
            if (frontRenderer == null || facingRoot == null)
            {
                return;
            }

            // A missing camera means there is no side classification, so keep the approved front model visible.
            Camera camera = Camera.main;
            if (camera == null)
            {
                frontRenderer.enabled = true;
                SetRearVisible(false);
                return;
            }

            // Compare only in the gameplay plane so camera height does not change front/back classification.
            Vector3 toCamera = camera.transform.position - facingRoot.position;
            toCamera.y = 0f;

            // If the camera is exactly on the root, avoid flicker by leaving the existing visibility untouched.
            if (toCamera.sqrMagnitude < 0.0001f)
            {
                return;
            }

            // Cameras ahead of the survivor are on the zombie side and should see the approved front model.
            float zombieSideDot = Vector3.Dot(facingRoot.forward, toCamera.normalized);

            // The chase camera is behind the survivor, so it must show the rear view to avoid backward soldiers.
            bool cameraHasCrossedToZombieSide = zombieSideDot > MinimumZombieSideDot;
            frontRenderer.enabled = cameraHasCrossedToZombieSide;
            SetRearVisible(!cameraHasCrossedToZombieSide);
        }

        private void SetRearVisible(bool isVisible)
        {
            // Older tests may configure only a front renderer, so rear visibility remains optional.
            if (rearRenderer != null)
            {
                rearRenderer.enabled = isVisible;
            }
        }
    }
}
