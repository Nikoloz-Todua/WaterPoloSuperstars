using UnityEngine;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    /// <summary>Applies the prone kick in the sampled pelvis frame on the production skeleton.</summary>
    internal sealed class ProneFlutterPose
    {
        private readonly Transform pelvis;
        private readonly Vector3 pelvisAnterior;
        private readonly Leg left, right;

        internal ProneFlutterPose(Animator animator, Transform model)
        {
            pelvis = animator.GetBoneTransform(HumanBodyBones.Hips);
            pelvisAnterior = pelvis != null ? pelvis.InverseTransformDirection(model.forward) : Vector3.forward;
            left = new Leg(animator, model, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg,
                HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes);
            right = new Leg(animator, model, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg,
                HumanBodyBones.RightFoot, HumanBodyBones.RightToes);
        }

        internal void Apply(Vector3 facing, float movement, float phase)
        {
            float weight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.47f, .92f, movement));
            if (weight <= 0f || pelvis == null) return;
            Vector3 aft = Vector3.ProjectOnPlane(-facing, Vector3.up).normalized;
            Vector3 dorsal = Vector3.ProjectOnPlane(-pelvis.TransformDirection(pelvisAnterior), aft).normalized;
            if (Vector3.Dot(dorsal, Vector3.up) < .48f) return;
            Vector3 rear = (aft - dorsal * .14f).normalized;
            Vector3 across = Vector3.Cross(rear, dorsal).normalized;
            float sprint = Mathf.Clamp01(movement - 1f);
            float beat = phase * (6f * Mathf.PI);
            left.Apply(rear, dorsal, across, beat, -1f, sprint, weight);
            right.Apply(rear, dorsal, across, beat + Mathf.PI, 1f, sprint, weight);
        }

        internal void PressWater(Vector3 forward, float strength)
        {
            Vector3 side = Vector3.Cross(Vector3.up, forward);
            left.Extend(forward, -side, strength);
            right.Extend(forward, side, strength);
        }

        private readonly struct Leg
        {
            private readonly Transform thigh, shin, ankle, toe;
            private readonly Quaternion thighFrame, shinFrame, footFrame, toeRest;

            internal Leg(Animator animator, Transform model, HumanBodyBones hipBone, HumanBodyBones kneeBone,
                HumanBodyBones ankleBone, HumanBodyBones toeBone)
            {
                thigh = animator.GetBoneTransform(hipBone);
                shin = animator.GetBoneTransform(kneeBone);
                ankle = animator.GetBoneTransform(ankleBone);
                toe = animator.GetBoneTransform(toeBone);
                thighFrame = Frame(thigh, shin, model.forward);
                shinFrame = Frame(shin, ankle, model.forward);
                footFrame = Frame(ankle, toe, model.up);
                toeRest = toe != null ? toe.localRotation : Quaternion.identity;
            }

            private static Quaternion Frame(Transform bone, Transform next, Vector3 reference) =>
                bone != null && next != null
                    ? Quaternion.Inverse(bone.rotation) * Quaternion.LookRotation(next.position - bone.position, reference)
                    : Quaternion.identity;

            internal void Apply(Vector3 rear, Vector3 dorsal, Vector3 across, float beat, float side, float sprint, float weight)
            {
                if (thigh == null || shin == null || ankle == null || toe == null) return;
                float hip = Mathf.Sin(beat) * Mathf.Lerp(5.2f, 7f, sprint) * Mathf.Deg2Rad;
                float flex = Mathf.Lerp(6f, Mathf.Lerp(17f, 20f, sprint),
                    .5f + .5f * Mathf.Sin(beat - .84f)) * Mathf.Deg2Rad;
                float plant = Mathf.Lerp(12f, 22f, .5f + .5f * Mathf.Sin(beat - 1.4f)) * Mathf.Deg2Rad;
                Vector3 upper = (rear + dorsal * Mathf.Tan(hip) + across * (side * .034f)).normalized;
                Vector3 kneeDorsal = Vector3.ProjectOnPlane(dorsal, upper).normalized;
                Vector3 lower = upper * Mathf.Cos(flex) + kneeDorsal * Mathf.Sin(flex);
                Vector3 shinDorsal = Quaternion.FromToRotation(upper, lower) * kneeDorsal;
                Vector3 toes = lower * Mathf.Cos(plant) - shinDorsal * Mathf.Sin(plant);
                Quaternion sampledShin = shin.rotation;
                Quaternion sampledFoot = ankle.rotation;
                thigh.rotation = Quaternion.Slerp(thigh.rotation,
                    Quaternion.LookRotation(upper, -kneeDorsal) * Quaternion.Inverse(thighFrame), weight);
                shin.rotation = Quaternion.Slerp(sampledShin,
                    Quaternion.LookRotation(lower, -shinDorsal) * Quaternion.Inverse(shinFrame), weight);
                ankle.rotation = Quaternion.Slerp(sampledFoot,
                    Quaternion.LookRotation(toes, shinDorsal) * Quaternion.Inverse(footFrame), weight);
                toe.localRotation = Quaternion.Slerp(toe.localRotation, toeRest, weight);
            }

            internal void Extend(Vector3 forward, Vector3 outside, float strength)
            {
                if (thigh == null || shin == null || ankle == null) return;
                Vector3 thighDirection = -Vector3.up + forward * .24f + outside * .25f;
                Vector3 shankDirection = -Vector3.up - forward * .19f + outside * .11f;
                thigh.rotation = Quaternion.Slerp(Quaternion.identity,
                    Quaternion.FromToRotation(shin.position - thigh.position, thighDirection), strength) * thigh.rotation;
                shin.rotation = Quaternion.Slerp(Quaternion.identity,
                    Quaternion.FromToRotation(ankle.position - shin.position, shankDirection), strength) * shin.rotation;
            }
        }
    }
}
