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
        private float followSharpness = 8f;

        public void Initialize(Transform followTarget, Vector3 followOffset)
        {
            target = followTarget;
            offset = followOffset;
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = target.position + offset;
            float blend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, blend);
            transform.LookAt(target.position + Vector3.forward * 4f);
        }

        private void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            transform.position = target.position + offset;
            transform.LookAt(target.position + Vector3.forward * 4f);
        }
    }
}
