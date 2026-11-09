using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Axis-aligned swimming area, independent of decorative geometry.</summary>
    [DisallowMultipleComponent]
    public sealed class PoolBounds : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float width = 20f;
        [SerializeField, Min(1f)] private float length = 30f;
        [Tooltip("World-space height of the swimmer root above this object's Y position.")]
        [SerializeField] private float waterline;

        public float Width => Mathf.Max(1f, width);
        public float Length => Mathf.Max(1f, length);
        public float Waterline => transform.position.y + waterline;

        public Vector3 ClampPosition(Vector3 position, float clearance)
        {
            // Rotation/scale of this component deliberately do not change the world-aligned area.
            float halfWidth = Mathf.Max(0f, Width * 0.5f - Mathf.Max(0f, clearance));
            float halfLength = Mathf.Max(0f, Length * 0.5f - Mathf.Max(0f, clearance));
            Vector3 center = transform.position;
            return new Vector3(
                Mathf.Clamp(position.x, center.x - halfWidth, center.x + halfWidth),
                Waterline,
                Mathf.Clamp(position.z, center.z - halfLength, center.z + halfLength));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(new Vector3(transform.position.x, Waterline, transform.position.z),
                new Vector3(Width, 0.05f, Length));
        }
    }
}
