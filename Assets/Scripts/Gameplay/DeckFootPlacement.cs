using System;
using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Keeps the low foot near its contact point in the existing deck-walk clips.</summary>
    internal sealed class DeckFootPlacement
    {
        private struct Foot
        {
            public Transform Hip, Knee, Ankle, Toe;
            public Vector3 Contact;
            public Quaternion Rotation;
            public bool Planted;

            public float Sole(float ankleDepth, float toeDepth)
            {
                if (Ankle == null) return float.PositiveInfinity;
                float heel = Ankle.position.y - ankleDepth;
                return Toe != null ? Mathf.Min(heel, Toe.position.y - toeDepth) : heel;
            }
        }

        private Foot left, right;
        private readonly float ankleDepth, toeDepth;
        private Vector3 lastDeckPoint;
        private bool hasSample;

        public DeckFootPlacement(Func<HumanBodyBones, Transform> bone, float ankleDepth, float toeDepth)
        {
            left.Hip = bone(HumanBodyBones.LeftUpperLeg);
            left.Knee = bone(HumanBodyBones.LeftLowerLeg);
            left.Ankle = bone(HumanBodyBones.LeftFoot);
            left.Toe = bone(HumanBodyBones.LeftToes);
            right.Hip = bone(HumanBodyBones.RightUpperLeg);
            right.Knee = bone(HumanBodyBones.RightLowerLeg);
            right.Ankle = bone(HumanBodyBones.RightFoot);
            right.Toe = bone(HumanBodyBones.RightToes);
            this.ankleDepth = ankleDepth;
            this.toeDepth = toeDepth;
        }

        public void Reset() => hasSample = left.Planted = right.Planted = false;

        public void Apply(Transform root, Vector3 deckPoint)
        {
            if (!hasSample || (deckPoint - lastDeckPoint).sqrMagnitude > .09f)
                Reset();
            lastDeckPoint = deckPoint;
            hasSample = true;

            LimitFootPitch(left, root.forward);
            LimitFootPitch(right, root.forward);
            AlignHeight(root, deckPoint.y);
            Pin(ref left, deckPoint.y, root.forward);
            Pin(ref right, deckPoint.y, root.forward);
        }

        private static void LimitFootPitch(Foot foot, Vector3 travel)
        {
            if (foot.Ankle == null || foot.Toe == null) return;
            Vector3 axis = (foot.Toe.position - foot.Ankle.position).normalized;
            Vector3 forward = Vector3.ProjectOnPlane(axis, Vector3.up).normalized;
            if (forward.sqrMagnitude < .01f) forward = travel;
            float pitch = Mathf.Atan2(axis.y, Vector3.ProjectOnPlane(axis, Vector3.up).magnitude) * Mathf.Rad2Deg;
            float limited = Mathf.Clamp(pitch, -48f, 16f);
            if (Mathf.Abs(limited - pitch) < .5f) return;
            Vector3 target = forward * Mathf.Cos(limited * Mathf.Deg2Rad) + Vector3.up * Mathf.Sin(limited * Mathf.Deg2Rad);
            foot.Ankle.rotation = Quaternion.FromToRotation(axis, target) * foot.Ankle.rotation;
        }

        public void AlignHeight(Transform root, float deckHeight)
        {
            float sole = Mathf.Min(left.Sole(ankleDepth, toeDepth), right.Sole(ankleDepth, toeDepth));
            if (!float.IsPositiveInfinity(sole))
                root.position += Vector3.up * Mathf.Clamp(deckHeight - sole, -.13f, .13f);
        }

        private void Pin(ref Foot foot, float deckHeight, Vector3 forward)
        {
            if (foot.Ankle == null || foot.Knee == null || foot.Hip == null) return;
            Vector3 ankle = foot.Ankle.position;
            float clearance = foot.Sole(ankleDepth, toeDepth) - deckHeight;
            if (!foot.Planted)
            {
                if (clearance > .015f) return;
                foot.Contact = ankle;
                foot.Rotation = foot.Ankle.rotation;
                foot.Planted = true;
            }

            Vector3 slip = Vector3.ProjectOnPlane(foot.Contact - ankle, Vector3.up);
            float error = slip.magnitude;
            if (clearance > .05f || error > .11f)
            {
                foot.Planted = false;
                return;
            }

            float weight = (1f - Mathf.InverseLerp(.015f, .05f, clearance)) *
                (1f - Mathf.InverseLerp(.065f, .11f, error));
            Vector3 hip = foot.Hip.position, knee = foot.Knee.position;
            Vector3 target = ankle + weight * slip;
            float thigh = Vector3.Distance(hip, knee), calf = Vector3.Distance(knee, ankle);
            Vector3 reach = (target - hip).normalized;
            Vector3 bend = Vector3.ProjectOnPlane(knee - hip, reach);
            Vector3 expected = Vector3.ProjectOnPlane(forward, reach);
            if (Vector3.Dot(bend, expected) < 0f) bend = expected;
            if (bend.sqrMagnitude < .00001f || thigh < .01f || calf < .01f) return;
            float length = Mathf.Clamp(Vector3.Distance(target, hip), Mathf.Abs(thigh - calf) + .001f, thigh + calf - .001f);
            float along = (thigh * thigh - calf * calf + length * length) / (2f * length);
            Vector3 kneeTarget = hip + reach * along +
                bend.normalized * Mathf.Sqrt(Mathf.Max(0f, thigh * thigh - along * along));
            Quaternion ankleRotation = Quaternion.Slerp(foot.Ankle.rotation, foot.Rotation, weight);
            foot.Hip.rotation = Quaternion.FromToRotation(knee - hip, kneeTarget - hip) * foot.Hip.rotation;
            foot.Knee.rotation = Quaternion.FromToRotation(foot.Ankle.position - foot.Knee.position,
                hip + reach * length - foot.Knee.position) * foot.Knee.rotation;
            foot.Ankle.rotation = ankleRotation;
        }
    }
}
