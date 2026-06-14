using UnityEngine;
using UnityEngine.EventSystems;

namespace LaneSurvivor.Base
{
    public sealed class BaseCameraController : MonoBehaviour
    {
        public const float MinimumFieldOfView = 36f;

        public const float DefaultFieldOfView = 68f;

        public const float MaximumFieldOfView = 68f;

        public static readonly Vector3 DefaultCameraPosition = new(0f, 7f, -8f);

        private const float ButtonZoomStep = 0.14f;

        private const float ScrollZoomSensitivity = 0.08f;

        private const float PinchZoomSensitivity = 0.004f;

        private const float KeyboardZoomSpeed = 0.75f;

        private const float MinimumCameraX = -2.4f;

        private const float MaximumCameraX = 2.4f;

        private const float MinimumCameraZ = -10.2f;

        private const float MaximumCameraZ = -5.8f;

        [SerializeField]
        private Camera controlledCamera;

        [SerializeField]
        private float zoomNormalized = 0.5f;

        private bool isMouseDraggingMap;

        private bool isTrackingPinch;

        private bool isTouchDraggingMap;

        private int draggingTouchId = -1;

        private Vector2 previousMousePosition;

        private float previousPinchDistance;

        private Vector2 previousTouchPosition;

        public float ZoomNormalized => zoomNormalized;

        public void Configure(Camera cameraToControl)
        {
            // Prefer the supplied camera but fall back to the local component for scene-authored use.
            controlledCamera = cameraToControl != null ? cameraToControl : GetComponent<Camera>();

            // Preserve any camera-authored starting FOV by translating it into the normalized zoom range.
            float startingFieldOfView = controlledCamera != null ? controlledCamera.fieldOfView : DefaultFieldOfView;
            zoomNormalized = Mathf.InverseLerp(MinimumFieldOfView, MaximumFieldOfView, Mathf.Clamp(startingFieldOfView, MinimumFieldOfView, MaximumFieldOfView));

            // Apply once so the camera lands exactly inside the supported zoom range.
            ApplyZoomToCamera();

            // Clamp once on setup so authored camera positions cannot begin outside the draggable map bounds.
            ClampCameraToPanBounds();
        }

        public void ZoomIn()
        {
            // Lower normalized zoom means a tighter field of view and a closer inspection feel.
            SetZoomNormalized(zoomNormalized - ButtonZoomStep);
        }

        public void ZoomOut()
        {
            // Higher normalized zoom means a wider field of view that exposes more of the base floor.
            SetZoomNormalized(zoomNormalized + ButtonZoomStep);
        }

        public void SetZoomNormalized(float value)
        {
            // Clamp once at the public boundary so buttons, pinch, scroll, and tests share the same limits.
            zoomNormalized = Mathf.Clamp01(value);

            // The actual camera mutation stays centralized for predictable tests and scene behavior.
            ApplyZoomToCamera();
        }

        public void ApplyScrollZoom(float scrollDelta)
        {
            // Tiny scroll noise should not cause visible camera jitter in the editor or desktop player.
            if (Mathf.Abs(scrollDelta) < 0.001f)
            {
                return;
            }

            // Positive wheel deltas conventionally zoom in, so they reduce the normalized FOV.
            SetZoomNormalized(zoomNormalized - scrollDelta * ScrollZoomSensitivity);
        }

        public void ApplyPinchZoom(float previousDistance, float currentDistance)
        {
            // Invalid or first-frame touch distances cannot produce a meaningful pinch delta.
            if (previousDistance <= 0f || currentDistance <= 0f)
            {
                return;
            }

            // Spreading fingers increases distance and should zoom inward on the inspectable base.
            float distanceDelta = currentDistance - previousDistance;
            SetZoomNormalized(zoomNormalized - distanceDelta * PinchZoomSensitivity);
        }

        public void ApplyMapDrag(Vector2 previousScreenPosition, Vector2 currentScreenPosition)
        {
            // A configured camera is required to translate screen drag into movement across the base plane.
            if (controlledCamera == null)
            {
                return;
            }

            // Convert the previous pointer location into a point on the map plane.
            if (!TryGetMapPlanePoint(previousScreenPosition, out Vector3 previousWorldPoint))
            {
                return;
            }

            // Convert the current pointer location into a point on the same plane.
            if (!TryGetMapPlanePoint(currentScreenPosition, out Vector3 currentWorldPoint))
            {
                return;
            }

            // Move opposite the screen-space pointer delta so the map appears to follow the drag gesture.
            Vector3 dragWorldDelta = previousWorldPoint - currentWorldPoint;
            transform.position += new Vector3(dragWorldDelta.x, 0f, dragWorldDelta.z);

            // Keep the base inside an inspectable range instead of letting the player fling it offscreen.
            ClampCameraToPanBounds();
        }

        private void Awake()
        {
            // Scene-created controllers are configured by the bootstrap, while authored scenes can self-configure.
            if (controlledCamera == null)
            {
                Configure(GetComponent<Camera>());
            }
        }

        private void Update()
        {
            // Desktop scroll gives quick editor verification while keeping the same zoom bounds as touch.
            ApplyScrollZoom(Input.mouseScrollDelta.y);

            // Keyboard zoom is a lightweight editor/development affordance for screenshots and smoke tests.
            HandleKeyboardZoom();

            // Mouse drag supports editor and desktop inspection of the same draggable base map.
            HandleMouseMapDrag();

            // Two-finger pinch is the mobile-friendly path requested for Base inspection.
            HandleTouchPinch();

            // One-finger drag is the mobile-friendly path for moving the base map around the camera view.
            HandleTouchMapDrag();
        }

        private void HandleMouseMapDrag()
        {
            // Starting on UI should click the HUD instead of dragging the world map underneath it.
            if (Input.GetMouseButtonDown(0))
            {
                isMouseDraggingMap = !IsPointerOverUi();
                previousMousePosition = Input.mousePosition;
                return;
            }

            // Releasing the button ends the map drag immediately.
            if (Input.GetMouseButtonUp(0))
            {
                isMouseDraggingMap = false;
                return;
            }

            // Only apply pan while the active press began away from HUD controls.
            if (!isMouseDraggingMap || !Input.GetMouseButton(0))
            {
                return;
            }

            // Translate this frame's mouse movement into bounded camera pan.
            Vector2 currentMousePosition = Input.mousePosition;
            ApplyMapDrag(previousMousePosition, currentMousePosition);
            previousMousePosition = currentMousePosition;
        }

        private void HandleKeyboardZoom()
        {
            // Equals shares the plus key on many keyboards, while KeypadPlus covers extended layouts.
            bool zoomInHeld = Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus);

            // Minus and KeypadMinus cover the matching zoom-out paths.
            bool zoomOutHeld = Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus);

            if (zoomInHeld)
            {
                // Unscaled time keeps camera inspection responsive even if later Base systems pause gameplay time.
                SetZoomNormalized(zoomNormalized - KeyboardZoomSpeed * Time.unscaledDeltaTime);
            }

            if (zoomOutHeld)
            {
                // The same speed in the opposite direction keeps keyboard zoom symmetric.
                SetZoomNormalized(zoomNormalized + KeyboardZoomSpeed * Time.unscaledDeltaTime);
            }
        }

        private void HandleTouchPinch()
        {
            // Fewer than two touches means the pinch gesture has ended or not begun.
            if (Input.touchCount < 2)
            {
                isTrackingPinch = false;
                previousPinchDistance = 0f;
                return;
            }

            // The first two touches define the pinch span for this simple inspect camera.
            Touch firstTouch = Input.GetTouch(0);
            Touch secondTouch = Input.GetTouch(1);
            float currentPinchDistance = Vector2.Distance(firstTouch.position, secondTouch.position);

            if (!isTrackingPinch)
            {
                // Store the first observed span so the next frame can compute a real delta.
                previousPinchDistance = currentPinchDistance;
                isTrackingPinch = true;
                return;
            }

            // Apply the frame-to-frame pinch delta using the same clamped zoom range as other inputs.
            ApplyPinchZoom(previousPinchDistance, currentPinchDistance);
            previousPinchDistance = currentPinchDistance;
        }

        private void HandleTouchMapDrag()
        {
            // A two-finger gesture belongs to pinch zoom, not map drag.
            if (Input.touchCount != 1)
            {
                isTouchDraggingMap = false;
                draggingTouchId = -1;
                return;
            }

            // The only active touch controls the map pan for this small prototype.
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                // Starting on UI should press the HUD rather than drag the Base world.
                isTouchDraggingMap = !IsPointerOverUi(touch.fingerId);
                draggingTouchId = touch.fingerId;
                previousTouchPosition = touch.position;
                return;
            }

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                // Ended and canceled touches both clear drag state.
                isTouchDraggingMap = false;
                draggingTouchId = -1;
                return;
            }

            // Ignore unrelated touches or touches that began over UI.
            if (!isTouchDraggingMap || draggingTouchId != touch.fingerId)
            {
                return;
            }

            // Translate the touch move into bounded pan on the Base map plane.
            ApplyMapDrag(previousTouchPosition, touch.position);
            previousTouchPosition = touch.position;
        }

        private void ApplyZoomToCamera()
        {
            // A missing camera should not break tests that instantiate the component in isolation.
            if (controlledCamera == null)
            {
                return;
            }

            // Field-of-view zoom leaves the Screen Space Overlay HUD untouched while changing world inspection scale.
            controlledCamera.fieldOfView = Mathf.Lerp(MinimumFieldOfView, MaximumFieldOfView, zoomNormalized);
        }

        private bool TryGetMapPlanePoint(Vector2 screenPosition, out Vector3 worldPoint)
        {
            // Start with a safe default so failed raycasts never leak uninitialized data.
            worldPoint = Vector3.zero;

            // The base map lives on the X/Z plane at world Y zero.
            Plane mapPlane = new(Vector3.up, Vector3.zero);

            // Camera rays let zoom level and aspect ratio naturally affect how much drag moves the map.
            Ray screenRay = controlledCamera.ScreenPointToRay(screenPosition);

            // A ray parallel to the plane cannot produce a useful drag position.
            if (!mapPlane.Raycast(screenRay, out float enter))
            {
                return false;
            }

            // Return the world-space point under the pointer on the Base map plane.
            worldPoint = screenRay.GetPoint(enter);
            return true;
        }

        private void ClampCameraToPanBounds()
        {
            // A missing camera transform should not happen, but this keeps isolated tests safe.
            if (controlledCamera == null)
            {
                return;
            }

            // Clamp only horizontal map axes; the authored camera height and pitch remain unchanged.
            Vector3 cameraPosition = transform.position;
            float clampedX = Mathf.Clamp(cameraPosition.x, MinimumCameraX, MaximumCameraX);
            float clampedZ = Mathf.Clamp(cameraPosition.z, MinimumCameraZ, MaximumCameraZ);
            transform.position = new Vector3(clampedX, cameraPosition.y, clampedZ);
        }

        private static bool IsPointerOverUi()
        {
            // Without an EventSystem, no UI can be consuming the pointer.
            if (EventSystem.current == null)
            {
                return false;
            }

            // The parameterless overload is the mouse/pointer path used in editor and desktop players.
            return EventSystem.current.IsPointerOverGameObject();
        }

        private static bool IsPointerOverUi(int pointerId)
        {
            // Without an EventSystem, no UI can be consuming the touch.
            if (EventSystem.current == null)
            {
                return false;
            }

            // Touch input needs the finger id so Unity checks the correct pointer against UI.
            return EventSystem.current.IsPointerOverGameObject(pointerId);
        }
    }
}
