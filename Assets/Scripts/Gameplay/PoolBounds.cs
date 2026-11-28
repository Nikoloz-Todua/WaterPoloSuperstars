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
        [SerializeField] private WaterVisuals.ArenaDimensions physicalBasin;
        private GoalMouth[] swimGoals = System.Array.Empty<GoalMouth>();
        internal bool OfficiatesBallExits => officiateBallExits;
        internal bool HasPhysicalBasin => physicalBasin != null;
        internal float DeckHeight => Waterline + (physicalBasin != null ? physicalBasin.deckElevation : 0f);
        internal bool AboveBasin(Vector3 point) => !HasPhysicalBasin ||
            Mathf.Abs(point.x - transform.position.x) < physicalBasin.PoolWidth * .5f &&
            Mathf.Abs(point.z - transform.position.z) < physicalBasin.PoolLength * .5f;
        internal void SetSwimGoals(GoalMouth[] goals) => swimGoals = goals;
        private float EndRopeOffset => HasPhysicalBasin ? .30f : 0f;

        internal bool TryGetBallExit(Vector3 from, Vector3 to, float radius, out FieldExit exit)
        {
            Vector3 center = transform.position;
            float side = BoundaryTime(from.x - center.x, to.x - center.x, Width * .5f + radius);
            float end = BoundaryTime(from.z - center.z, to.z - center.z, Length * .5f + EndRopeOffset + radius);
            bool acrossGoalLine = end <= side;
            float first = Mathf.Min(side, end);
            exit = default;
            if (float.IsPositiveInfinity(first)) return false;
            Vector3 point = Vector3.LerpUnclamped(from, to, first);
            if (acrossGoalLine) point.z = center.z + Mathf.Sign(point.z - center.z) * (Length * .5f + EndRopeOffset);
            else point.x = center.x + Mathf.Sign(point.x - center.x) * Width * .5f;
            exit = new FieldExit(point, acrossGoalLine);
            return true;
        }

        internal bool ContainsPartOfBall(Vector3 point, float radius)
        {
            Vector3 offset = point - transform.position;
            return Mathf.Abs(offset.x) <= Width * .5f + radius &&
                Mathf.Abs(offset.z) <= Length * .5f + EndRopeOffset + radius;
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
            => ClampRectangle(position, Width, Length, clearance);

        internal Vector3 ClampBasin(Vector3 position, float radius) => HasPhysicalBasin
            ? ClampRectangle(position, physicalBasin.PoolWidth, physicalBasin.PoolLength, radius)
            : ClampPosition(position, radius);

        internal Vector3 ClampSwimmer(Vector3 previous, Vector3 proposed, float radius)
        {
            proposed = ClampBasin(proposed, radius);
            foreach (var goal in swimGoals)
                if (goal != null && goal.isActiveAndEnabled)
                    proposed = goal.ClampSwimmer(previous, proposed, radius);
            return proposed;
        }

        private Vector3 ClampRectangle(Vector3 position, float width, float length, float clearance)
        {
            // Rotation/scale of this component deliberately do not change the world-aligned area.
            float halfWidth = Mathf.Max(0f, width * 0.5f - Mathf.Max(0f, clearance));
            float halfLength = Mathf.Max(0f, length * 0.5f - Mathf.Max(0f, clearance));
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
