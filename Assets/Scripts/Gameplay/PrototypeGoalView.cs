using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Replaceable primitive presentation/colliders; scoring geometry is in GoalMouth.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class PrototypeGoalView : MonoBehaviour
    {
        [SerializeField] private GoalMouth mouth;
        [SerializeField] private PoolBounds pool;
        [SerializeField] private Transform leftPost, rightPost, crossbar;
        [SerializeField] private Transform backCollider, leftNet, rightNet, netWater;
        [SerializeField] private Transform leftBoundary, rightBoundary;
        [SerializeField] private Transform[] verticalStrands = new Transform[0];
        [SerializeField] private Transform[] horizontalStrands = new Transform[0];
        private Vector4 lastDimensions;

        private void Update()
        {
            if (mouth == null || pool == null)
                return;
            Vector4 dimensions = new Vector4(mouth.Width, mouth.Height, mouth.Depth, pool.Width + mouth.PostThickness);
            if (dimensions != lastDimensions)
                Refresh();
        }

        public void Refresh()
        {
            if (mouth == null || pool == null)
                return;
            float w = mouth.Width, h = mouth.Height, d = mouth.Depth, p = mouth.PostThickness;
            Place(leftPost, new Vector3(-(w + p) * 0.5f, h * 0.5f, p * 0.5f), new Vector3(p, h + p, p));
            Place(rightPost, new Vector3((w + p) * 0.5f, h * 0.5f, p * 0.5f), new Vector3(p, h + p, p));
            Place(crossbar, new Vector3(0f, h + p * 0.5f, p * 0.5f), new Vector3(w + 2f * p, p, p));
            Place(backCollider, new Vector3(0f, h * 0.5f, d + 0.03f), new Vector3(w, h, 0.06f));
            Place(leftNet, new Vector3(-w * 0.5f - 0.03f, h * 0.5f, d * 0.5f), new Vector3(0.06f, h, d));
            Place(rightNet, new Vector3(w * 0.5f + 0.03f, h * 0.5f, d * 0.5f), new Vector3(0.06f, h, d));
            Place(netWater, new Vector3(0f, -0.04f, d * 0.5f), new Vector3(w, 0.06f, d));
            float segment = Mathf.Max(0.01f, (pool.Width - w) * 0.5f - p);
            float center = w * 0.5f + p + segment * 0.5f;
            Place(leftBoundary, new Vector3(-center, 0.08f, 0.25f), new Vector3(segment, 0.5f, 0.5f));
            Place(rightBoundary, new Vector3(center, 0.08f, 0.25f), new Vector3(segment, 0.5f, 0.5f));
            for (int i = 0; i < verticalStrands.Length; i++)
                Place(verticalStrands[i], new Vector3(Mathf.Lerp(-w * 0.5f, w * 0.5f, (float)i / (verticalStrands.Length - 1)), h * 0.5f, d), new Vector3(0.025f, h, 0.025f));
            for (int i = 0; i < horizontalStrands.Length; i++)
                Place(horizontalStrands[i], new Vector3(0f, h * i / (horizontalStrands.Length - 1), d), new Vector3(w, 0.025f, 0.025f));
            lastDimensions = new Vector4(w, h, d, pool.Width + p);
        }

        private static void Place(Transform target, Vector3 position, Vector3 scale)
        {
            if (target == null)
                return;
            target.localPosition = position;
            target.localScale = scale;
        }
    }
}
