using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Pure geometry. Goal local +Z points into its net, so the keeper stays at negative local Z.</summary>
    public static class GoalkeeperPositioning
    {
        public static float TrackingX(GoalMouth goal, Vector3 position, Vector3 velocity, float stationDepth, float horizon)
        {
            Vector3 local = goal.transform.InverseTransformPoint(position);
            Vector3 motion = goal.transform.InverseTransformDirection(velocity);
            if (motion.z > 0.1f)
            {
                float time = (-stationDepth - local.z) / motion.z;
                if (time >= 0f && time <= horizon) return local.x + motion.x * time;
            }
            return local.x * 0.5f;
        }
        public static bool SweptContact(Vector3 ballFrom, Vector3 ballTo, Vector3 handFrom, Vector3 handTo, float radius, out float fraction)
        {
            Vector3 from = ballFrom - handFrom, travel = (ballTo - handTo) - from;
            float c = from.sqrMagnitude - radius * radius;
            fraction = 0f;
            if (c <= 0f) return true;
            float a = travel.sqrMagnitude, b = Vector3.Dot(from, travel);
            float discriminant = b * b - a * c;
            if (a < 0.000001f || discriminant < 0f) return false;
            fraction = (-b - Mathf.Sqrt(discriminant)) / a;
            return fraction >= 0f && fraction <= 1f;
        }
    }
}
