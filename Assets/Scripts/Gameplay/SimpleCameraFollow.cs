using UnityEngine;

namespace LaneSurvivor.Gameplay
{
    public sealed class SimpleCameraFollow : MonoBehaviour
    {
        [SerializeField]
        private Transform target;

        [SerializeField]
        private Vector3 offset = new(0f, 8f, -8f);

        [SerializeField]
        private Vector3 lookAtOffset = new(0f, 1f, 0f);

        [SerializeField]
        private bool followTargetX;

        [SerializeField]
        private bool useResponsiveFieldOfView = true;

        private Camera followCamera;

        private float lastAppliedAspect = -1f;

        public void Initialize(Transform followTarget, Vector3 followOffset)
        {
            Initialize(followTarget, followOffset, lookAtOffset, followTargetX);
        }

        public void Initialize(Transform followTarget, Vector3 followOffset, Vector3 followLookAtOffset)
        {
            Initialize(followTarget, followOffset, followLookAtOffset, followTargetX);
        }

        public void Initialize(Transform followTarget, Vector3 followOffset, Vector3 followLookAtOffset, bool trackTargetX)
        {
            target = followTarget;
            offset = followOffset;
            lookAtOffset = followLookAtOffset;
            followTargetX = trackTargetX;
            followCamera = GetComponent<Camera>();
            ApplyResponsiveFieldOfView();
            SnapToTarget();
        }

        private void LateUpdate()
        {
            ApplyResponsiveFieldOfView();

            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = target.position + offset;
            desiredPosition.x = GetCameraCenterX();

            // This prototype uses a locked chase camera so fast lane/finish motion cannot leave the squad off-screen.
            transform.position = desiredPosition;
            transform.LookAt(GetLookAtPoint());
        }

        private void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            transform.position = target.position + offset;
            transform.position = new Vector3(GetCameraCenterX(), transform.position.y, transform.position.z);
            transform.LookAt(GetLookAtPoint());
        }

        private Vector3 GetLookAtPoint()
        {
            // Matching the camera center keeps side-lane movement visible without creating a sideways look angle.
            Vector3 lookAtPoint = target.position + lookAtOffset;
            lookAtPoint.x = GetCameraCenterX();
            return lookAtPoint;
        }

        private float GetCameraCenterX()
        {
            // Side-lane play needs optional X tracking so the squad cannot drift out of the narrow portrait frame.
            return followTargetX && target != null ? target.position.x + offset.x : offset.x;
        }

        private void ApplyResponsiveFieldOfView()
        {
            if (!useResponsiveFieldOfView)
            {
                return;
            }

            if (followCamera == null)
            {
                // Cache the camera lazily because tests and generated scenes can add components in either order.
                followCamera = GetComponent<Camera>();
            }

            if (followCamera == null || followCamera.orthographic)
            {
                return;
            }

            // Unity reports the active render target aspect, which changes with device and Game view shape.
            float currentAspect = followCamera.aspect;

            // Avoid rewriting the Camera every frame when no aspect or FOV change is needed.
            if (Mathf.Approximately(currentAspect, lastAppliedAspect))
            {
                return;
            }

            // The shared visual rule keeps side lanes visible on narrow portrait devices.
            followCamera.fieldOfView = GameplayVisuals.GetCameraFieldOfViewForAspect(currentAspect);
            lastAppliedAspect = currentAspect;
        }
    }
}
