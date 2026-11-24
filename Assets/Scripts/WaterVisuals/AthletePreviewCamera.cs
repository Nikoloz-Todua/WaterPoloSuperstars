using UnityEngine;
using UnityEngine.InputSystem;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    public sealed class AthletePreviewCamera : MonoBehaviour
    {
        public RiggedSwimmerVisual athlete;

        [Range(-180f, 180f)] public float angle = 15f;
        [Range(0, 2)] public int framing;
        public bool dry = true;

        Animator animator;
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
            if (keyboard.tabKey.wasPressedThisFrame) dry = !dry;
            if (keyboard.f1Key.wasPressedThisFrame) framing = 0;
            if (keyboard.f2Key.wasPressedThisFrame) framing = 1;
            if (keyboard.f3Key.wasPressedThisFrame) framing = 2;
        }

        void LateUpdate()
        {
            if (athlete == null || animator == null) return;
            if (dry == animator.enabled)
            {
                animator.enabled = !dry;
                athlete.enabled = !dry;
                athlete.alignmentRoot.localPosition = Vector3.zero;
                if (dry)
                    for (int i = 0; i < rig.Length; i++)
                        rig[i].SetLocalPositionAndRotation(restPositions[i], restRotations[i]);
            }
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
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            Vector3 focus = framing == 1 ? head.position + Vector3.up * .12f :
                athlete.alignmentRoot.position + Vector3.up * (framing == 2 ? .91f : .98f);
            float distance = framing == 1 ? .58f : framing == 2 ? .91f : 3.8f;
            transform.position = focus + Quaternion.Euler(0, angle, 0) * new Vector3(0, framing == 0 ? .29f : .027f, distance);
            transform.LookAt(focus);
        }

        static void Point(Transform joint, Transform child, Vector3 direction)
        {
            joint.rotation = Quaternion.FromToRotation(child.position - joint.position, direction) * joint.rotation;
        }
    }
}

