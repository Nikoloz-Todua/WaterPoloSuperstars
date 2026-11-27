using Unity.Profiling;
using UnityEngine;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    /// <summary>Samples the authored flutter on the production rig after Humanoid evaluation.</summary>
    internal sealed class ProneFlutterPose
    {
        [System.Serializable]
        private sealed class Cycle
        {
            public int frames;
            public Quaternion[] rotations;
            public bool Valid => frames > 0 && rotations != null && rotations.Length == (frames + 1) * JointCount;
        }

        [System.Serializable]
        private sealed class Motion
        {
            public Cycle crawl, sprint;
        }

        private static readonly ProfilerMarker Sampling = new ProfilerMarker("WaterPolo.AuthoredFlutter");
        private static readonly HumanBodyBones[] Joints =
        {
            HumanBodyBones.Hips,
            HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes,
            HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, HumanBodyBones.RightToes
        };
        private static readonly int[] Parents = { -1, 0, 1, 2, 3, 0, 5, 6, 7 };
        private const int JointCount = 9;
        private static Motion motion;
        private static bool loaded;

        private readonly Transform[] joints = new Transform[JointCount];
        private readonly Quaternion[] bind = new Quaternion[JointCount];
        private readonly Quaternion[] parentBind = new Quaternion[JointCount];
        private readonly Quaternion[] deltas = new Quaternion[JointCount];
        private readonly Transform spine;
        private readonly bool ready;

        internal ProneFlutterPose(Animator animator, Transform model)
        {
            if (!loaded)
            {
                loaded = true;
                var asset = Resources.Load<TextAsset>("WPSFlutterMotion");
                if (asset != null) motion = JsonUtility.FromJson<Motion>(asset.text);
            }
            Quaternion inverse = Quaternion.Inverse(model.rotation);
            for (int i = 0; i < JointCount; i++)
            {
                Transform bone = animator.GetBoneTransform(Joints[i]);
                joints[i] = bone;
                if (bone == null) continue;
                bind[i] = inverse * bone.rotation;
                parentBind[i] = inverse * bone.parent.rotation;
            }
            spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            ready = joints[0] != null && joints[1] != null && joints[2] != null && joints[3] != null &&
                joints[5] != null && joints[6] != null && joints[7] != null &&
                motion?.crawl?.Valid == true && motion.sprint?.Valid == true;
        }

        internal void Apply(Vector3 facing, float movement, float phase)
        {
            float weight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.48f, .95f, movement));
            if (!ready || weight <= 0f) return;

            using var measurement = Sampling.Auto();
            float sprint = Mathf.Clamp01(movement - 1f);
            phase = Mathf.Repeat(phase, 1f);
            float normalFrame = phase * motion.crawl.frames;
            float sprintFrame = phase * motion.sprint.frames;
            int normal = Mathf.Min(Mathf.FloorToInt(normalFrame), motion.crawl.frames - 1);
            int fast = Mathf.Min(Mathf.FloorToInt(sprintFrame), motion.sprint.frames - 1);
            for (int i = 0; i < joints.Length; i++)
            {
                var a = Sample(motion.crawl, normal, normalFrame - normal, i);
                deltas[i] = sprint > 0f
                    ? Quaternion.Slerp(a, Sample(motion.sprint, fast, sprintFrame - fast, i), sprint)
                    : a;
            }

            Quaternion upperBody = spine != null ? spine.rotation : Quaternion.identity;
            var root = joints[0];
            Quaternion desiredRoot = Quaternion.LookRotation(facing, Vector3.up) * deltas[0] * bind[0];
            root.rotation = Quaternion.Slerp(root.rotation, desiredRoot, weight);
            if (spine != null) spine.rotation = upperBody;
            for (int i = 1; i < joints.Length; i++)
            {
                var bone = joints[i];
                if (bone == null) continue;
                // The bind frames are the production bones, including their FBX roll.
                // Work in parent space so the transition blends both ends of the chain coherently.
                Quaternion authoredParent = deltas[Parents[i]] * parentBind[i];
                Quaternion desired = Quaternion.Inverse(authoredParent) * deltas[i] * bind[i];
                bone.localRotation = Quaternion.Slerp(bone.localRotation, desired, weight);
            }
        }

        private static Quaternion Sample(Cycle cycle, int frame, float blend, int joint)
        {
            int index = frame * JointCount + joint;
            return Quaternion.Slerp(cycle.rotations[index], cycle.rotations[index + JointCount], blend);
        }

        internal void PressWater(Vector3 forward, float strength)
        {
            Vector3 side = Vector3.Cross(Vector3.up, forward);
            Extend(joints[1], joints[2], joints[3], forward, -side, strength);
            Extend(joints[5], joints[6], joints[7], forward, side, strength);
        }

        private static void Extend(Transform thigh, Transform shin, Transform ankle, Vector3 forward, Vector3 outside, float strength)
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
