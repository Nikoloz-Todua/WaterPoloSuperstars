using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    public sealed class AthletePreviewCamera : MonoBehaviour
    {
        public RiggedSwimmerVisual athlete;

        [Range(-180f, 180f)] public float angle = 15f;
        [Range(0, 2)] public int framing;
        public bool dry = true;
        [Range(-85f, 85f)] public float elevation;
        public AnimationClip walk;
        public AnimationClip run;
        [Range(0f, 1f)] public float playbackRate = 1f;

        Animator animator;
        PlayableGraph deckGraph;
        AnimationClipPlayable deckPlayable;
        AnimationClip deckClip;
        int poseMode = -1;
        Transform[] joints;
        Quaternion[] rest;
        Transform[] rig;
        Vector3[] restPositions;
        Quaternion[] restRotations;
        static readonly HumanBodyBones[] Bones =
        {
            HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
            HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand
        };

        void Awake()
        {
            animator = athlete.GetComponent<Animator>();
            rig = animator.GetComponentsInChildren<Transform>(true);
            restPositions = new Vector3[rig.Length];
            restRotations = new Quaternion[rig.Length];
            for (int i = 0; i < rig.Length; i++)
            {
                restPositions[i] = rig[i].localPosition;
                restRotations[i] = rig[i].localRotation;
            }
            joints = new Transform[Bones.Length];
            rest = new Quaternion[Bones.Length];
            for (int i = 0; i < Bones.Length; i++)
            {
                joints[i] = animator.GetBoneTransform(Bones[i]);
                rest[i] = joints[i].localRotation;
            }
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.leftArrowKey.isPressed) angle = Mathf.Repeat(angle - 85f * Time.deltaTime + 180f, 360f) - 180f;
            if (keyboard.rightArrowKey.isPressed) angle = Mathf.Repeat(angle + 85f * Time.deltaTime + 180f, 360f) - 180f;
            if (keyboard.tabKey.wasPressedThisFrame) { dry = !dry || deckClip != null; deckClip = null; }
            if (keyboard.f1Key.wasPressedThisFrame) framing = 0;
            if (keyboard.f2Key.wasPressedThisFrame) framing = 1;
            if (keyboard.f3Key.wasPressedThisFrame) framing = 2;
            if (keyboard.f4Key.wasPressedThisFrame) SelectDeckClip(walk);
            if (keyboard.f5Key.wasPressedThisFrame) SelectDeckClip(run);
            if (keyboard.oKey.wasPressedThisFrame) elevation = elevation > 60f ? 0f : 83f;
            if (keyboard.uKey.wasPressedThisFrame) elevation = elevation < -20f ? 0f : -38f;
            if (keyboard.pKey.wasPressedThisFrame) playbackRate = playbackRate >= .99f ? .25f : 1f;
            if (keyboard.rightBracketKey.wasPressedThisFrame && deckGraph.IsValid())
            {
                playbackRate = 0f;
                deckPlayable.SetTime((deckPlayable.GetTime() + 1 / 60d) % deckClip.length);
                deckGraph.Evaluate(0);
            }
        }

        public void SelectDeckClip(AnimationClip clip)
        {
            if (clip == null || clip == deckClip) return;
            DisposeDeckGraph();
            deckClip = clip;
            dry = false;
            poseMode = -1;
        }

        void LateUpdate()
        {
            if (athlete == null || animator == null) return;
            int mode = dry ? 0 : deckClip == null ? 1 : 2;
            if (mode != poseMode)
            {
                poseMode = mode;
                DisposeDeckGraph();
                animator.enabled = mode != 0;
                athlete.enabled = mode == 1;
                athlete.alignmentRoot.localPosition = Vector3.zero;
                for (int i = 0; i < rig.Length; i++)
                    rig[i].SetLocalPositionAndRotation(restPositions[i], restRotations[i]);
                if (mode == 2)
                {
                    deckGraph = PlayableGraph.Create("Athlete deck clip inspection");
                    deckGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    deckPlayable = AnimationClipPlayable.Create(deckGraph, deckClip);
                    deckPlayable.SetApplyFootIK(true);
                    AnimationPlayableOutput.Create(deckGraph, "Athlete pose", animator).SetSourcePlayable(deckPlayable);
                    deckGraph.Play();
                }
            }
            athlete.PreviewPlaybackSpeed = playbackRate;
            if (deckGraph.IsValid()) deckGraph.Evaluate(Time.deltaTime * playbackRate);
            if (dry)
            {
                for (int i = 0; i < joints.Length; i++) joints[i].localRotation = rest[i];
                for (int i = 0; i < joints.Length; i += 3)
                {
                    float side = i == 0 ? -1f : 1f;
                    Point(joints[i], joints[i+1], new Vector3(side * .17f, -1f, 0));
                    Point(joints[i+1], joints[i+2], new Vector3(side * .08f, -1f, .12f));
                }
            }
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Vector3 focus = framing == 1 ? head.position + Vector3.up * .12f :
                mode == 1 ? hips.position + Vector3.up * (framing == 2 ? -.03f : .16f) :
                athlete.alignmentRoot.position + Vector3.up * (framing == 2 ? .91f : .98f);
            float distance = framing == 1 ? .58f : framing == 2 ? .91f : 3.8f;
            float tilt = elevation + (Mathf.Abs(elevation) < 1f && framing == 0 ? 4.3f : 0f);
            transform.position = focus + Quaternion.Euler(-tilt, angle, 0) * (Vector3.forward * distance);
            transform.LookAt(focus);
        }

        void OnDisable() => DisposeDeckGraph();

        void DisposeDeckGraph()
        {
            if (deckGraph.IsValid()) deckGraph.Destroy();
        }

        static void Point(Transform joint, Transform child, Vector3 direction)
        {
            joint.rotation = Quaternion.FromToRotation(child.position - joint.position, direction) * joint.rotation;
        }
    }
}

