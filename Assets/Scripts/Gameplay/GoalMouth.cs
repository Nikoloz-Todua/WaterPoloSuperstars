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
        public float Width => Mathf.Max(0.5f, width);
        public float Height => Mathf.Max(0.3f, height);
        public float Depth => Mathf.Max(0.5f, depth);
        public float PostThickness => Mathf.Max(0.02f, postThickness);

        private bool FitsOpening(Vector3 local, float radius)
        {
            // Strict post/crossbar clearance: tangent contact alone must not count.
            return Mathf.Abs(local.x) + radius < Width * 0.5f - 0.001f &&
                local.y - radius >= 0f && local.y + radius < Height - 0.001f;
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
