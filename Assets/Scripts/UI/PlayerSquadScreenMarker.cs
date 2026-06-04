using LaneSurvivor.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace LaneSurvivor.UI
{
    public sealed class PlayerSquadScreenMarker : MonoBehaviour
    {
        [SerializeField]
        private RectTransform canvasRect;

        [SerializeField]
        private RectTransform markerRect;

        [SerializeField]
        private CanvasGroup markerGroup;

        [SerializeField]
        private Transform target;

        [SerializeField]
        private Camera worldCamera;

        public void Configure(RectTransform containingCanvas, RectTransform markerRoot, CanvasGroup visibilityGroup, Transform followTarget, Camera camera)
        {
            // Store the canvas rect so screen coordinates can be converted into overlay coordinates every frame.
            canvasRect = containingCanvas;

            // Store the marker root because child images are just visual pieces.
            markerRect = markerRoot;

            // Store the visibility group so temporary hiding does not disable this component.
            markerGroup = visibilityGroup;

            // Follow the gameplay transform so lane changes and forward progress stay authoritative.
            target = followTarget;

            // Use the gameplay camera projection so the overlay marker sits where the world player would appear.
            worldCamera = camera;

            // Position immediately so the first rendered frame is already aligned.
            LateUpdate();
        }

        public static PlayerSquadScreenMarker Create(RectTransform canvasTransform, Transform followTarget, Camera camera)
        {
            if (canvasTransform == null)
            {
                throw new System.ArgumentNullException(nameof(canvasTransform), "Player marker requires a UI RectTransform parent.");
            }

            // The HUD canvas owns the overlay player marker so it renders after world geometry.
            GameObject markerObject = new("Player Squad Screen Marker");
            markerObject.transform.SetParent(canvasTransform, false);

            // The root rect is moved each frame, while child rects define the placeholder shape.
            RectTransform markerRoot = markerObject.AddComponent<RectTransform>();
            markerRoot.anchorMin = new Vector2(0.5f, 0.5f);
            markerRoot.anchorMax = new Vector2(0.5f, 0.5f);
            markerRoot.pivot = new Vector2(0.5f, 0.5f);
            markerRoot.sizeDelta = new Vector2(GameplayVisuals.ScreenPlayerMarkerWidth, GameplayVisuals.ScreenPlayerMarkerHeight);

            // A cyan base gives the player a stable readable body.
            CreateImage(markerRoot, "Marker Body", new Color(0.12f, 0.88f, 0.98f), Vector2.zero, markerRoot.sizeDelta);

            // A magenta core keeps the player easy to scan in simulator proof frames.
            CreateImage(markerRoot, "Marker Core", new Color(1f, 0.1f, 0.8f), new Vector2(0f, 4f), new Vector2(GameplayVisuals.ScreenPlayerMarkerWidth * 0.68f, GameplayVisuals.ScreenPlayerMarkerHeight * 0.58f));

            // A small magenta mast mirrors the old world marker without making the player oversized.
            CreateImage(markerRoot, "Marker Mast", new Color(1f, 0.1f, 0.8f), new Vector2(0f, GameplayVisuals.ScreenPlayerMarkerHeight * 0.44f), new Vector2(6f, 18f));

            // The component keeps the marker aligned with the gameplay target.
            CanvasGroup markerVisibility = markerObject.AddComponent<CanvasGroup>();
            markerVisibility.blocksRaycasts = false;
            markerVisibility.interactable = false;

            // The component keeps the marker aligned with the gameplay target.
            PlayerSquadScreenMarker marker = markerObject.AddComponent<PlayerSquadScreenMarker>();
            marker.Configure(canvasTransform, markerRoot, markerVisibility, followTarget, camera);
            return marker;
        }

        private void LateUpdate()
        {
            if (canvasRect == null || markerRect == null || target == null || worldCamera == null)
            {
                return;
            }

            // Follow a slightly raised point on the gameplay root rather than the road-contact origin.
            Vector3 targetPoint = target.position + new Vector3(0f, GameplayVisuals.ScreenPlayerMarkerWorldOffsetY, GameplayVisuals.ScreenPlayerMarkerWorldOffsetZ);

            // Convert the target into screen space through the same camera that draws the world.
            Vector3 screenPoint = worldCamera.WorldToScreenPoint(targetPoint);

            if (screenPoint.z <= 0f)
            {
                // If the target is ever behind the camera, hide visually without disabling this updater.
                SetMarkerVisible(false);
                return;
            }

            // Restore the marker after any temporary behind-camera condition.
            SetMarkerVisible(true);

            // Overlay canvases do not need a camera argument for RectTransformUtility conversion.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 localPoint);

            // Clamp the marker inside the canvas so the bottom controls cannot cover it.
            float halfWidth = markerRect.sizeDelta.x * 0.5f;
            float halfHeight = markerRect.sizeDelta.y * 0.5f;
            float minX = canvasRect.rect.xMin + halfWidth;
            float maxX = canvasRect.rect.xMax - halfWidth;
            float minY = canvasRect.rect.yMin + GameplayVisuals.ScreenPlayerMarkerBottomPadding + halfHeight;
            float maxY = canvasRect.rect.yMax - halfHeight;

            // Only clamping after projection preserves visible lane shifts until an edge is actually reached.
            localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);
            localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);

            // Move the marker root; the child images keep their authored offsets.
            markerRect.anchoredPosition = localPoint;
        }

        private void SetMarkerVisible(bool isVisible)
        {
            if (markerGroup == null)
            {
                return;
            }

            // Alpha is enough because the marker has no input behavior.
            markerGroup.alpha = isVisible ? 1f : 0f;
        }

        private static void CreateImage(RectTransform parent, string name, Color color, Vector2 anchoredPosition, Vector2 size)
        {
            // Each piece is an ordinary UI image so it renders in the overlay pass.
            GameObject imageObject = new(name);
            imageObject.transform.SetParent(parent, false);

            // Solid colors are enough for prototype readability and require no imported art.
            Image image = imageObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            // Child rects are centered on the marker root and sized in canvas units.
            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.anchoredPosition = anchoredPosition;
            imageRect.sizeDelta = size;
        }
    }
}
