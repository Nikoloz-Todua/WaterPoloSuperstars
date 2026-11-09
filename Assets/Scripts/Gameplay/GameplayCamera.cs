using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>A fixed-angle sports camera. Only the bounded focus point follows the swimmer.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(200)]
    public sealed class GameplayCamera : MonoBehaviour
    {
        [SerializeField] private SwimmerMotor target;
        [SerializeField] private PoolBounds poolBounds;
        [SerializeField, Min(1f)] private float distance = 6.5f;
        [SerializeField, Min(1f)] private float height = 7.5f;
        [SerializeField, Range(20f, 80f)] private float pitch = 50f;
        [SerializeField, Range(-180f, 180f)] private float yaw;
        [SerializeField, Min(0.01f)] private float smoothTime = 0.22f;
        [SerializeField, Min(0f)] private float lookAhead = 1.8f;
        [Tooltip("Keep the follow focus this far inside the pool; the deck may remain visible at edges.")]
        [SerializeField, Min(0f)] private float boundsInset = 1.5f;
        [Tooltip("Allowed deck beyond the pool when bounding the inner 70% of the camera view.")]
        [SerializeField, Min(0f)] private float outsideMargin = 2f;

        private Vector3 focus;
        private Vector3 focusVelocity;
        private bool initialized;

        public Vector3 Focus => focus;

        private Vector3 DesiredFocus()
        {
            // Velocity-based lead fades away at rest and doesn't jump when facing reverses.
            Vector3 lead = target.Velocity / Mathf.Max(0.1f, target.MaxSpeed) * lookAhead;
            return ClampFocus(target.transform.position + lead);
        }

        private Vector3 ClampFocus(Vector3 desired)
        {
            desired = poolBounds.ClampPosition(desired, boundsInset);
            Camera lens = GetComponent<Camera>();
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 offset = Quaternion.Euler(0f, yaw, 0f) * Vector3.back * distance + Vector3.up * height;
            float spread = Mathf.Tan(lens.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.7f;
            Vector2 min = Vector2.positiveInfinity;
            Vector2 max = Vector2.negativeInfinity;
            // Approximate the useful ground footprint, allowing some deck at the outer screen edges.
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            {
                Vector3 ray = rotation * new Vector3(x * spread * lens.aspect, y * spread, 1f);
                if (ray.y >= -0.01f)
                    return desired; // Near-horizon Inspector settings cannot define a finite footprint.
                Vector3 ground = offset + ray * (-height / ray.y);
                Vector2 point = new Vector2(ground.x, ground.z);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            Vector3 center = poolBounds.transform.position;
            float minX = center.x - poolBounds.Width * 0.5f - outsideMargin - min.x;
            float maxX = center.x + poolBounds.Width * 0.5f + outsideMargin - max.x;
            float minZ = center.z - poolBounds.Length * 0.5f - outsideMargin - min.y;
            float maxZ = center.z + poolBounds.Length * 0.5f + outsideMargin - max.y;
            desired.x = minX <= maxX ? Mathf.Clamp(desired.x, minX, maxX) : (minX + maxX) * 0.5f;
            desired.z = minZ <= maxZ ? Mathf.Clamp(desired.z, minZ, maxZ) : (minZ + maxZ) * 0.5f;
            return desired;
        }

        public void SnapToTarget()
        {
            if (target == null || poolBounds == null)
                return;
            focus = DesiredFocus();
            focusVelocity = Vector3.zero;
            initialized = true;
            ApplyPose();
        }

        private void OnEnable()
        {
            initialized = false;
        }

        private void LateUpdate()
        {
            if (target == null || poolBounds == null)
                return;
            if (!initialized)
                SnapToTarget();
            focus = Vector3.SmoothDamp(focus, DesiredFocus(), ref focusVelocity,
                Mathf.Max(0.01f, smoothTime), Mathf.Infinity, Time.deltaTime);
            focus = ClampFocus(focus);
            ApplyPose();
        }

        private void ApplyPose()
        {
            Vector3 offset = Quaternion.Euler(0f, yaw, 0f) * Vector3.back * distance;
            transform.SetPositionAndRotation(focus + offset + Vector3.up * height,
                Quaternion.Euler(pitch, yaw, 0f));
        }
    }
}
