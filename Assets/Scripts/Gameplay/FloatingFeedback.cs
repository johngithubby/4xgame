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

        private float age;

        private Color startingColor = Color.white;

        public void Configure(string message, Color color, float duration)
        {
            // Store a local text reference so update logic can stay allocation-free.
            textMesh = GetComponent<TextMesh>();

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

            // Remove the temporary object after the fade completes.
            if (age >= lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
