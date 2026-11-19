using UnityEngine;

namespace ToduStudios.WaterPolo
{
    internal readonly struct FieldExit
    {
        internal readonly Vector3 Point;
        internal readonly bool AcrossGoalLine;
        internal FieldExit(Vector3 point, bool acrossGoalLine)
        { Point = point; AcrossGoalLine = acrossGoalLine; }
    }

    /// <summary>Axis-aligned swimming area, independent of decorative geometry.</summary>
    [DisallowMultipleComponent]
    public sealed class PoolBounds : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float width = 20f;
        [SerializeField, Min(1f)] private float length = 30f;
        [Tooltip("World-space height of the swimmer root above this object's Y position.")]
        [SerializeField] private float waterline;
        [SerializeField] private bool officiateBallExits;
        internal bool OfficiatesBallExits => officiateBallExits;

        internal bool TryGetBallExit(Vector3 from, Vector3 to, float radius, out FieldExit exit)
        {
            Vector3 center = transform.position;
            float side = BoundaryTime(from.x - center.x, to.x - center.x, Width * .5f + radius);
            float end = BoundaryTime(from.z - center.z, to.z - center.z, Length * .5f + radius);
            bool acrossGoalLine = end <= side;
            float first = Mathf.Min(side, end);
            exit = default;
            if (float.IsPositiveInfinity(first)) return false;
            Vector3 point = Vector3.LerpUnclamped(from, to, first);
            if (acrossGoalLine) point.z = center.z + Mathf.Sign(point.z - center.z) * Length * .5f;
            else point.x = center.x + Mathf.Sign(point.x - center.x) * Width * .5f;
            exit = new FieldExit(point, acrossGoalLine);
            return true;
        }

        internal bool ContainsPartOfBall(Vector3 point, float radius)
        {
            Vector3 offset = point - transform.position;
            return Mathf.Abs(offset.x) <= Width * .5f + radius && Mathf.Abs(offset.z) <= Length * .5f + radius;
        }

        private static float BoundaryTime(float from, float to, float limit)
        {
            if (to < -limit && from >= -limit) return (-limit - from) / (to - from);
            if (to > limit && from <= limit) return (limit - from) / (to - from);
            return float.PositiveInfinity;
        }

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
