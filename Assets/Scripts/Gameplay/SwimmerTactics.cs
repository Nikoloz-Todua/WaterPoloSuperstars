using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Pool-plane target calculations, independent of input, possession mutations and physics.</summary>
    public static class SwimmerTactics
    {
        public static Vector3 Support(Vector3 carrier, Vector3 facing, int rank, float forwardDistance, float width)
        {
            facing = Vector3.ProjectOnPlane(facing, Vector3.up).normalized;
            if (facing.sqrMagnitude < 0.001f) facing = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, facing);
            int row = rank / 2;
            float side = rank % 2 == 0 ? 1f : -1f;
            return carrier + facing * (forwardDistance + row * 1.5f) + right * side * (width + row * 0.75f);
        }

        public static Vector3 Defend(Vector3 carrier, Vector3 defendedGoal, float pressureDistance)
        {
            Vector3 goalSide = Vector3.ProjectOnPlane(defendedGoal - carrier, Vector3.up).normalized;
            return carrier + goalSide * pressureDistance;
        }
    }
}
