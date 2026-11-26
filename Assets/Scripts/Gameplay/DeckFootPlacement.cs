using System;
using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Distance-driven deck contacts for the existing athlete and official upper-body poses.</summary>
    internal sealed class DeckFootPlacement
    {
        private const float SupportFraction = .60f;
        private readonly Foot left, right;
        private readonly float ankleDepth, toeDepth;

        private readonly struct Foot
        {
            public readonly Transform Hip, Knee, Ankle;
            public readonly Quaternion FootFrame;
            public readonly Vector3 LocalToe;
            public readonly float Side;
            public bool Ready => Hip != null && Knee != null && Ankle != null && LocalToe.sqrMagnitude > .00001f;

            public Foot(Func<HumanBodyBones, Transform> bone, HumanBodyBones hip, HumanBodyBones knee,
                HumanBodyBones ankle, HumanBodyBones toe, Transform model, float side)
            {
                Hip = bone(hip);
                Knee = bone(knee);
                Ankle = bone(ankle);
                var tip = bone(toe);
                LocalToe = tip != null && Ankle != null
                    ? Ankle.InverseTransformDirection(tip.position - Ankle.position) : Vector3.zero;
                FootFrame = LocalToe.sqrMagnitude > .00001f
                    ? Quaternion.LookRotation(LocalToe, Ankle.InverseTransformDirection(model.up))
                    : Quaternion.identity;
                Side = side;
            }
        }

        public DeckFootPlacement(Func<HumanBodyBones, Transform> bone, Transform model, float ankleDepth, float toeDepth)
        {
            left = new Foot(bone, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg,
                HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes, model, -1f);
            right = new Foot(bone, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg,
                HumanBodyBones.RightFoot, HumanBodyBones.RightToes, model, 1f);
            this.ankleDepth = ankleDepth;
            this.toeDepth = toeDepth;
        }

        public void Apply(Transform root, Vector3 deck, Vector3 travel, float phase, float strideLength, float movement)
        {
            if (!left.Ready || !right.Ready) return;
            travel = Vector3.ProjectOnPlane(travel, Vector3.up).normalized;
            if (travel.sqrMagnitude < .5f) travel = root.forward;
            Vector3 forward = Vector3.ProjectOnPlane(root.forward, Vector3.up).normalized;
            Vector3 across = Vector3.Cross(Vector3.up, forward);
            var leftTarget = Contact(left, deck, forward, across, travel, phase, strideLength, movement);
            var rightTarget = Contact(right, deck, forward, across, travel, phase + .5f, strideLength, movement);
            float drop = Mathf.Max(ReachDrop(left, leftTarget.position), ReachDrop(right, rightTarget.position));
            root.position -= Vector3.up * Mathf.Min(.16f * strideLength / 1.36f, drop);
            Bend(left, leftTarget.position, leftTarget.rotation, forward - across * .08f);
            Bend(right, rightTarget.position, rightTarget.rotation, forward + across * .08f);
        }

        private (Vector3 position, Quaternion rotation) Contact(Foot foot, Vector3 deck, Vector3 forward,
            Vector3 across, Vector3 travel, float phase, float stride, float movement)
        {
            phase = Mathf.Repeat(phase, 1f);
            float scale = stride / 1.36f;
            float front = stride * SupportFraction * .5f;
            float along, lift, pitch;
            if (phase < SupportFraction)
            {
                float support = phase / SupportFraction;
                along = front - stride * phase;
                lift = 0f;
                pitch = 14f * (1f - Ease(support / .25f)) - 27f * Ease((support - .72f) / .28f);
            }
            else
            {
                float swing = (phase - SupportFraction) / (1f - SupportFraction);
                along = Mathf.Lerp(-front, front, Ease(swing));
                lift = .135f * scale * Mathf.Pow(Mathf.Sin(Mathf.PI * swing), 1.35f);
                pitch = Mathf.Lerp(-27f, 14f, Ease(swing));
            }

            // Foot and toe joints sit at different heights when the sole is flat.
            float neutral = Mathf.Asin(Mathf.Clamp((toeDepth - ankleDepth) / foot.LocalToe.magnitude, -.55f, .35f));
            float radians = neutral + pitch * movement * Mathf.Deg2Rad;
            Vector3 footLine = forward * Mathf.Cos(radians) + Vector3.up * Mathf.Sin(radians);
            Quaternion rotation = Quaternion.LookRotation(footLine, Vector3.up) * Quaternion.Inverse(foot.FootFrame);
            float contactHeight = Mathf.Max(ankleDepth, toeDepth - (rotation * foot.LocalToe).y);
            Vector3 point = deck + across * (foot.Side * .112f * scale) + travel * (along * movement) +
                Vector3.up * (contactHeight + lift * movement);
            return (point, rotation);
        }

        private static float Ease(float value) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(value));

        private static float ReachDrop(Foot foot, Vector3 point)
        {
            float range = Vector3.Distance(foot.Hip.position, foot.Knee.position) +
                Vector3.Distance(foot.Knee.position, foot.Ankle.position) - .018f;
            Vector3 delta = foot.Hip.position - point;
            float horizontalSquared = delta.x * delta.x + delta.z * delta.z;
            return Mathf.Max(0f, delta.y - Mathf.Sqrt(Mathf.Max(.001f, range * range - horizontalSquared)));
        }

        private static void Bend(Foot foot, Vector3 point, Quaternion rotation, Vector3 forward)
        {
            Vector3 hip = foot.Hip.position;
            Vector3 upper = foot.Knee.position - hip;
            float thigh = upper.magnitude;
            float calf = Vector3.Distance(foot.Knee.position, foot.Ankle.position);
            Vector3 axis = (point - hip).normalized;
            float distance = Mathf.Clamp(Vector3.Distance(point, hip), Mathf.Abs(thigh - calf) + .005f, thigh + calf - .004f);
            float along = (thigh * thigh - calf * calf + distance * distance) / (2f * distance);
            Vector3 bend = Vector3.ProjectOnPlane(forward, axis).normalized;
            Vector3 knee = hip + axis * along + bend * Mathf.Sqrt(Mathf.Max(0f, thigh * thigh - along * along));
            foot.Hip.rotation = Quaternion.FromToRotation(upper, knee - hip) * foot.Hip.rotation;
            foot.Knee.rotation = Quaternion.FromToRotation(foot.Ankle.position - foot.Knee.position,
                hip + axis * distance - foot.Knee.position) * foot.Knee.rotation;
            foot.Ankle.rotation = rotation;
        }
    }
}
