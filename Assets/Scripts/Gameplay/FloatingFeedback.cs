using UnityEngine;

namespace LaneSurvivor.Gameplay
{
    public sealed class FloatingFeedback : MonoBehaviour
    {
        [SerializeField]
        private TextMesh textMesh;

        [SerializeField]
        private float lifetime = 1.1f;

        [SerializeField]
        private float riseSpeed = 1.4f;

        [SerializeField]
        private bool faceCamera = true;

        private float age;

        private Color startingColor = Color.white;

        private Color startingShadowColor = new(0f, 0f, 0f, 0.75f);

        private TextMesh shadowTextMesh;

        private Transform cameraTransform;

        private Renderer[] feedbackRenderers = new Renderer[0];

        public void Configure(string message, Color color, float duration)
        {
            Configure(message, color, duration, null);
        }

        public void Configure(string message, Color color, float duration, Camera billboardCamera)
        {
            // Store a local text reference so update logic can stay allocation-free.
            textMesh = GetComponent<TextMesh>();

            // A child shadow mesh may be present when runtime feedback asks for readability support.
            shadowTextMesh = transform.Find("Feedback Text Shadow")?.GetComponent<TextMesh>();

            // Runtime feedback owns generated materials, so cache renderers for OnDestroy cleanup.
            feedbackRenderers = GetComponentsInChildren<Renderer>();

            // Cache the camera transform once, but allow a lazy fallback if the camera is created later.
            cameraTransform = billboardCamera != null ? billboardCamera.transform : Camera.main?.transform;

            // Clamp lifetime so a bad caller value cannot destroy the feedback instantly.
            lifetime = Mathf.Max(0.1f, duration);

            // Preserve the original color alpha for the fade calculation.
            startingColor = color;

            // Apply the visible message and color when a TextMesh exists.
            if (textMesh != null)
            {
                textMesh.text = message;
                textMesh.color = startingColor;
            }

            // Mirror the same glyphs into the shadow so the effect still reads over green zombies or bright gates.
            if (shadowTextMesh != null)
            {
                shadowTextMesh.text = message;
                shadowTextMesh.color = startingShadowColor;
            }

            // Align immediately so the first rendered frame is already readable.
            FaceCameraIfNeeded();
        }

        private void Update()
        {
            // Advance age using scaled game time so feedback pauses with gameplay if timeScale changes later.
            age += Time.deltaTime;

            // Move upward a little each frame to make events easy to spot.
            transform.position += Vector3.up * (riseSpeed * Time.deltaTime);

            // Fade text out smoothly over its lifetime.
            if (textMesh != null)
            {
                float visibleFraction = 1f - Mathf.Clamp01(age / lifetime);
                Color fadedColor = startingColor;
                fadedColor.a *= visibleFraction;
                textMesh.color = fadedColor;
            }

            // Fade the shadow with the main glyphs so the label disappears as one event.
            if (shadowTextMesh != null)
            {
                float visibleFraction = 1f - Mathf.Clamp01(age / lifetime);
                Color fadedShadowColor = startingShadowColor;
                fadedShadowColor.a *= visibleFraction;
                shadowTextMesh.color = fadedShadowColor;
            }

            // Keep TextMesh feedback facing the active camera through lane changes and camera follow motion.
            FaceCameraIfNeeded();

            // Remove the temporary object after the fade completes.
            if (age >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void FaceCameraIfNeeded()
        {
            // Static world labels use fixed rotations; floating combat feedback opts into camera-facing text.
            if (!faceCamera)
            {
                return;
            }

            // Camera.main can be unavailable during very early scene bootstrap, so retry lazily.
            if (cameraTransform == null)
            {
                cameraTransform = Camera.main?.transform;
            }

            // Without a camera, keep the last rotation rather than throwing during tests or edit-time creation.
            if (cameraTransform == null)
            {
                return;
            }

            // TextMesh fronts face their local forward direction, so point that forward vector toward the camera ray.
            Vector3 cameraToFeedback = transform.position - cameraTransform.position;
            if (cameraToFeedback.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            // Use the camera's up vector so text does not roll oddly on future camera tilt adjustments.
            transform.rotation = Quaternion.LookRotation(cameraToFeedback.normalized, cameraTransform.up);
        }

        private void OnDestroy()
        {
            // Destroy only generated feedback materials; never touch shared built-in font materials.
            foreach (Renderer feedbackRenderer in feedbackRenderers)
            {
                // A renderer can be destroyed by scene unload before the component cleanup runs.
                if (feedbackRenderer == null)
                {
                    continue;
                }

                // The runtime-created materials are named by PrototypeMaterialFactory.
                DestroyGeneratedFeedbackMaterial(feedbackRenderer.sharedMaterial);
            }
        }

        private static void DestroyGeneratedFeedbackMaterial(Material material)
        {
            // Null materials need no cleanup, and non-generated font materials must remain shared.
            if (material == null || !material.name.StartsWith("LaneSurvivor Generated"))
            {
                return;
            }

            // Play Mode uses delayed destruction, while EditMode tests need immediate object cleanup.
            if (Application.isPlaying)
            {
                Destroy(material);
            }
            else
            {
                DestroyImmediate(material);
            }
        }
    }
}
