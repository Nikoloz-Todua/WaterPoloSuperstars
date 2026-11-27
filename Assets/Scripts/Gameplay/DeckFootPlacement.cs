using System;
using UnityEngine;

namespace ToduStudios.WaterPolo
{
    internal static class DeckMotion
    {
        [Serializable]
        private sealed class ContactCycle
        {
            public float stride, support;
            public Vector3[] samples;
            public float[] toeExtension;
        }

        private static readonly ContactCycle cycle = LoadContact();
        internal static readonly AnimationClip Walk = LoadClip("WPSDeckWalk");
        internal static readonly AnimationClip Stand = LoadClip("WPSDeckStand");
        internal static float Stride => cycle != null ? cycle.stride : 1.28f;
        internal static float Support => cycle != null ? cycle.support : .61f;

        internal static (Vector3 pose, float toeExtension) Contact(float phase)
        {
            if (cycle == null) return default;
            int last = cycle.samples.Length - 1;
            float index = Mathf.Repeat(phase, 1f) * last;
            int sample = Mathf.Min(Mathf.FloorToInt(index), last - 1);
            float fraction = index - sample;
            float extension = cycle.toeExtension != null && cycle.toeExtension.Length == cycle.samples.Length
                ? Mathf.Lerp(cycle.toeExtension[sample], cycle.toeExtension[sample + 1], fraction) : 0f;
            return (Vector3.Lerp(cycle.samples[sample], cycle.samples[sample + 1], fraction), extension);
        }

        private static ContactCycle LoadContact()
        {
            var source = Resources.Load<TextAsset>("WPSDeckContacts");
            var data = source != null ? JsonUtility.FromJson<ContactCycle>(source.text) : null;
            return data != null && data.stride > 0f && data.samples != null && data.samples.Length > 1 ? data : null;
        }

        private static AnimationClip LoadClip(string path)
        {
            foreach (var clip in Resources.LoadAll<AnimationClip>(path))
                if (clip.humanMotion && !clip.name.StartsWith("__preview__", StringComparison.Ordinal)) return clip;
            return null;
        }
    }

    /// <summary>Distance-driven deck contacts for the existing athlete and official upper-body poses.</summary>
    internal sealed class DeckFootPlacement
    {
        private Foot left, right;
        private float hipDrop;

        private struct Foot
        {
            public readonly Transform Hip, Knee, Ankle, Toe;
            public readonly Quaternion FootFrame;
            public readonly Quaternion ToeNeutral;
            public readonly Vector3 Heel, Ball;
            public readonly float Side;
            public bool Planted;
            public Vector3 Station, PlantHeading;
            public float PreviousPhase;
            public bool Ready => Hip != null && Knee != null && Ankle != null && Heel.sqrMagnitude > .00001f;

            public Foot(Func<HumanBodyBones, Transform> bone, HumanBodyBones hip, HumanBodyBones knee,
                HumanBodyBones ankle, HumanBodyBones toe, Transform model, SkinnedMeshRenderer[] skins,
                float fallbackDepth, float toeDepth, float side)
            {
                Hip = bone(hip);
                Knee = bone(knee);
                Ankle = bone(ankle);
                Toe = bone(toe);
                Side = side;
                FootFrame = ToeNeutral = Quaternion.identity;
                Heel = Ball = Station = PlantHeading = Vector3.zero;
                Planted = false;
                PreviousPhase = 0f;
                if (Ankle == null || Toe == null) return;
                Vector3 toToe = Toe.position - Ankle.position;
                Quaternion ankleBind = Ankle.rotation;
                ToeNeutral = Toe.localRotation;
                bool hasBind = false;
                foreach (var skin in skins)
                {
                    var bones = skin.bones;
                    int a = Array.IndexOf(bones, Ankle), t = Array.IndexOf(bones, Toe);
                    if (a < 0 || t < 0 || skin.sharedMesh == null) continue;
                    var bind = skin.sharedMesh.bindposes;
                    if (a >= bind.Length || t >= bind.Length) continue;
                    Matrix4x4 anklePose = bind[a].inverse, toePose = bind[t].inverse;
                    toToe = skin.transform.TransformVector(toePose.GetColumn(3) - anklePose.GetColumn(3));
                    ankleBind = skin.transform.rotation * anklePose.rotation;
                    if (Toe.parent == Ankle) ToeNeutral = Quaternion.Inverse(anklePose.rotation) * toePose.rotation;
                    hasBind = true;
                    break;
                }
                Vector3 level = Vector3.ProjectOnPlane(toToe, model.up);
                if (level.sqrMagnitude < .00001f) return;
                Quaternion inverse = Quaternion.Inverse(ankleBind);
                FootFrame = Quaternion.Inverse(Quaternion.LookRotation(level, model.up)) * ankleBind;
                float depth = hasBind ? toeDepth - Vector3.Dot(toToe, model.up) : fallbackDepth;
                Heel = inverse * (-level * .34f - model.up * depth);
                Ball = inverse * (toToe - model.up * toeDepth);
            }
        }

        public DeckFootPlacement(Func<HumanBodyBones, Transform> bone, Transform model, float ankleDepth, float toeDepth)
        {
            var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            left = new Foot(bone, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg,
                HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes, model, skins, ankleDepth, toeDepth, -1f);
            right = new Foot(bone, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg,
                HumanBodyBones.RightFoot, HumanBodyBones.RightToes, model, skins, ankleDepth, toeDepth, 1f);
        }

        public void Apply(Transform root, Vector3 deck, Vector3 travel, float phase, float strideLength, float movement)
        {
            if (!left.Ready || !right.Ready) return;
            travel = Vector3.ProjectOnPlane(travel, Vector3.up).normalized;
            if (travel.sqrMagnitude < .5f) travel = root.forward;
            Vector3 forward = Vector3.ProjectOnPlane(root.forward, Vector3.up).normalized;
            Vector3 across = Vector3.Cross(Vector3.up, forward);
            var leftTarget = Contact(ref left, deck, forward, across, travel, phase, strideLength, movement);
            var rightTarget = Contact(ref right, deck, forward, across, travel, phase + .5f, strideLength, movement);
            float drop = Mathf.Max(ReachDrop(left, leftTarget.position), ReachDrop(right, rightTarget.position));
            float limit = .14f * Mathf.Clamp(strideLength / DeckMotion.Stride, .72f, 1.30f);
            drop = Mathf.Min(limit, drop);
            // Make enough room for a planted leg immediately, then yield upward as the load leaves it.
            hipDrop = Mathf.Max(drop, Mathf.MoveTowards(hipDrop, drop, .39f * Time.deltaTime));
            root.position -= Vector3.up * hipDrop;
            Bend(left, leftTarget.position, leftTarget.rotation, leftTarget.heading, leftTarget.toeExtension);
            Bend(right, rightTarget.position, rightTarget.rotation, rightTarget.heading, rightTarget.toeExtension);
        }

        private static (Vector3 position, Quaternion rotation, Vector3 heading, float toeExtension) Contact(
            ref Foot foot, Vector3 deck, Vector3 forward,
            Vector3 across, Vector3 travel, float phase, float stride, float movement)
        {
            phase = Mathf.Repeat(phase, 1f);
            float scale = stride / DeckMotion.Stride;
            var contact = DeckMotion.Contact(phase);
            Vector3 sample = contact.pose * movement;
            Vector3 heading = Quaternion.AngleAxis(foot.Side * 3.5f, Vector3.up) * forward;
            Vector3 point = deck + across * (foot.Side * .108f * Mathf.Clamp(scale, .80f, 1.3f)) + travel * (sample.x * scale);
            // Retain a support foot through braking. It releases as it nears the next swing.
            bool support = phase < DeckMotion.Support && movement > (foot.Planted ? .08f : .23f);
            if (support)
            {
                if (!foot.Planted || phase < foot.PreviousPhase)
                {
                    foot.Station = point;
                    foot.PlantHeading = heading;
                }
                Vector3 correction = Vector3.ProjectOnPlane(foot.Station - point, Vector3.up);
                float settle = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.08f, .35f, movement));
                float release = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(DeckMotion.Support - .065f, DeckMotion.Support, phase));
                float lockWeight = settle * (1f - release);
                point += Vector3.ClampMagnitude(correction, .095f * Mathf.Max(.70f, scale)) * lockWeight;
                float yaw = Vector3.SignedAngle(heading, foot.PlantHeading, Vector3.up);
                heading = Quaternion.AngleAxis(Mathf.Clamp(yaw, -32f, 32f) * lockWeight, Vector3.up) * heading;
            }
            foot.Planted = support;
            foot.PreviousPhase = phase;
            Quaternion neutral = Quaternion.LookRotation(heading, Vector3.up) * foot.FootFrame;
            Vector3 hinge = Vector3.Cross(Vector3.up, heading);
            Quaternion rotation = Quaternion.AngleAxis(-sample.z, hinge) * neutral;
            Vector3 pivot = sample.z >= 0f ? foot.Heel : foot.Ball;
            point += Vector3.ProjectOnPlane(neutral * pivot - rotation * pivot, Vector3.up);
            point.y += -Mathf.Min((rotation * foot.Heel).y, (rotation * foot.Ball).y) + sample.y * scale;
            return (point, rotation, heading, contact.toeExtension * movement);
        }

        private static float ReachDrop(Foot foot, Vector3 point)
        {
            float range = Vector3.Distance(foot.Hip.position, foot.Knee.position) +
                Vector3.Distance(foot.Knee.position, foot.Ankle.position) - .018f;
            Vector3 delta = foot.Hip.position - point;
            float horizontalSquared = delta.x * delta.x + delta.z * delta.z;
            return Mathf.Max(0f, delta.y - Mathf.Sqrt(Mathf.Max(.001f, range * range - horizontalSquared)));
        }

        private static void Bend(Foot foot, Vector3 point, Quaternion rotation, Vector3 forward, float toeExtension)
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
            if (foot.Toe != null)
            {
                foot.Toe.localRotation = foot.ToeNeutral;
                if (toeExtension > .01f)
                    foot.Toe.rotation = Quaternion.AngleAxis(-toeExtension, Vector3.Cross(Vector3.up, forward)) * foot.Toe.rotation;
            }
        }
    }
}
