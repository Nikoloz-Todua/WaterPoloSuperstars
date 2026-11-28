using UnityEngine;

namespace ToduStudios.WaterPolo
{
    public enum PrototypeTeam { Blue, White }

    /// <summary>Unit-scale local +Z points from the playing area into the goal.</summary>
    [DisallowMultipleComponent]
    public sealed class GoalMouth : MonoBehaviour
    {
        [SerializeField] private PrototypeTeam scoringTeam;
        [SerializeField, Min(0.5f)] private float width = 3f;
        [SerializeField, Min(0.3f)] private float height = 0.9f;
        [SerializeField, Min(0.5f)] private float depth = 1f;
        [SerializeField, Min(0.02f)] private float postThickness = 0.1f;

        public PrototypeTeam ScoringTeam => scoringTeam;
        public void SetScoringTeam(PrototypeTeam team) => scoringTeam=team;
        public float Width => Mathf.Max(0.5f, width);
        public float Height => Mathf.Max(0.3f, height);
        public float Depth => Mathf.Max(0.5f, depth);
        public float PostThickness => Mathf.Max(0.02f, postThickness);

        private bool FitsOpening(Vector3 local, float radius)
        {
            // Strict post/crossbar clearance: tangent contact alone must not count.
            return Mathf.Abs(local.x) + radius < Width * 0.5f - 0.001f &&
                local.y >= 0f && local.y + radius < Height - 0.001f; // A floating ball may be partially submerged.
        }

        public bool TryGetCrossing(Vector3 from, Vector3 to, float radius, out Vector3 crossing)
        {
            Vector3 a = transform.InverseTransformPoint(from);
            Vector3 b = transform.InverseTransformPoint(to);
            crossing = default;
            // Score only once the trailing edge of the whole sphere is past the plane.
            if (a.z >= radius || b.z < radius || b.z <= a.z)
                return false;
            Vector3 atLine = Vector3.LerpUnclamped(a, b, (radius - a.z) / (b.z - a.z));
            if (!FitsOpening(atLine, radius))
                return false;
            crossing = transform.TransformPoint(atLine);
            return true;
        }

        public bool AllowsPocket(Vector3 position, float radius)
        {
            Vector3 local = transform.InverseTransformPoint(position);
            return local.z >= -radius - 0.001f && FitsOpening(local, radius);
        }

        internal Vector3 ClampSwimmer(Vector3 from, Vector3 to, float radius)
        {
            Vector3 a = transform.InverseTransformPoint(from);
            Vector3 b = transform.InverseTransformPoint(to);
            if (Mathf.Max(a.z, b.z) < -radius || Mathf.Min(a.z, b.z) > Depth + .06f + radius ||
                Mathf.Min(a.x, b.x) > Width * .5f + PostThickness + radius ||
                Mathf.Max(a.x, b.x) < -Width * .5f - PostThickness - radius) return to;

            float inner = Mathf.Max(0f, Width * .5f - radius);
            float outer = Width * .5f + Mathf.Max(PostThickness, .06f) + radius;
            // Solve transverse travel first so a blocked forward stroke can still slide along a post.
            if (a.z > -radius && a.z < Depth + .06f + radius)
            {
                b.x = StopAtBand(a.x, b.x, inner, outer);
                b.x = StopAtBand(a.x, b.x, -outer, -inner);
            }
            if (Mathf.Abs(b.x) > inner && Mathf.Abs(b.x) < outer)
                b.z = StopAtBand(a.z, b.z, -radius, Depth + .06f + radius);
            else if (Mathf.Abs(b.x) <= inner)
                // The stroke briefly extends farther than the upright capsule.
                b.z = StopAtBand(a.z, b.z, Mathf.Max(0f, Depth - radius - .10f), Depth + .06f + radius);
            return transform.TransformPoint(b);
        }

        private static float StopAtBand(float from, float to, float low, float high)
        {
            if (from <= low && to > low) return low;
            if (from >= high && to < high) return high;
            if (from > low && from < high)
                return from - low < high - from ? Mathf.Min(from, to) : Mathf.Max(from, to);
            return to;
        }

        public Vector3 ClampPocket(Vector3 position, float radius)
        {
            Vector3 local = transform.InverseTransformPoint(position);
            local.z = Mathf.Min(local.z, Mathf.Max(radius, Depth - radius));
            return transform.TransformPoint(local);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(new Vector3(0f, Height * 0.5f, 0f), new Vector3(Width, Height, 0.01f));
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * Depth);
        }
    }
}
