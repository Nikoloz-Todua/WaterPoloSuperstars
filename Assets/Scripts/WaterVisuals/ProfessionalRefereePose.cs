using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    /// <summary>In-place deck gait and signals for the existing event-driven official.</summary>
    [RequireComponent(typeof(Animator))]
    public sealed class ProfessionalRefereePose : MonoBehaviour
    {
        [SerializeField] AnimationClip walking;
        Animator animator;
        PlayableGraph graph;
        AnimationClipPlayable gait;
        Transform[] bones;
        Vector3[] positions;
        Quaternion[] rotations;
        Transform leftArm, leftElbow, leftHand, rightArm, rightElbow, rightHand, head, leftFoot, rightFoot;
        Quaternion modelFacing;
        float stride;
        RefereeSignal lastSignal;
        float signalAge;

        void Awake()
        {
            animator = GetComponent<Animator>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            modelFacing = transform.localRotation;
            bones = transform.GetComponentsInChildren<Transform>(true);
            positions = new Vector3[bones.Length];
            rotations = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                positions[i] = bones[i].localPosition;
                rotations[i] = bones[i].localRotation;
            }
            leftArm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            leftElbow = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            rightArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            rightElbow = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            head = animator.GetBoneTransform(HumanBodyBones.Head);
            leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            Stand();
        }

        public void Stand()
        {
            RestoreRest();
            transform.localRotation = modelFacing;
            Vector3 down = -transform.up;
            Point(leftArm, leftElbow, down - .11f * transform.right);
            Point(rightArm, rightElbow, down + .11f * transform.right);
            Point(leftElbow, leftHand, down + .14f * transform.forward);
            Point(rightElbow, rightHand, down + .14f * transform.forward);
        }

        public void Animate(float speed, RefereeSignal signal, Vector3 point, Vector3 attack, bool restartSweep)
        {
            if (signal != lastSignal)
            {
                signalAge = 0f;
                lastSignal = signal;
            }
            else signalAge += Time.deltaTime;

            if (signal == RefereeSignal.None && Mathf.Abs(speed) > .16f)
            {
                Walk(speed);
                return;
            }

            Stand();
            if (signal == RefereeSignal.None) return;
            if (signalAge < .29f)
            {
                Reach(rightArm, rightElbow, rightHand, head.position + transform.forward * .085f -
                    transform.right * .015f + transform.up * .027f);
                return;
            }

            Vector3 toward = (point - rightArm.position).normalized;
            switch (signal)
            {
                case RefereeSignal.Goal:
                    SignalArm(rightArm, rightElbow, rightHand, toward);
                    break;
                case RefereeSignal.Exclusion:
                    Vector3 outside = Vector3.ProjectOnPlane(transform.position - point, Vector3.up).normalized;
                    Vector3 sweep = Vector3.Slerp(toward, outside + transform.up * .17f, Mathf.InverseLerp(.47f, .85f, signalAge));
                    SignalArm(rightArm, rightElbow, rightHand, sweep);
                    break;
                case RefereeSignal.Penalty:
                    SignalArm(rightArm, rightElbow, rightHand, transform.up + transform.forward * .25f);
                    break;
                case RefereeSignal.Restart when restartSweep:
                    SignalArm(rightArm, rightElbow, rightHand, Vector3.Slerp(transform.up, toward, Mathf.InverseLerp(.55f, 1.08f, signalAge)));
                    break;
                case RefereeSignal.Foul:
                case RefereeSignal.Restart:
                    if (attack.sqrMagnitude > .01f) SignalArm(leftArm, leftElbow, leftHand, attack);
                    SignalArm(rightArm, rightElbow, rightHand, toward);
                    break;
            }
        }

        void Walk(float speed)
        {
            Quaternion previousFacing = transform.rotation;
            RestoreRest();
            if (!graph.IsValid())
            {
                graph = PlayableGraph.Create("Official deck gait");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                gait = AnimationClipPlayable.Create(graph, walking);
                gait.SetApplyFootIK(true);
                AnimationPlayableOutput.Create(graph, "Deck walking", animator).SetSourcePlayable(gait);
                graph.Play();
            }
            // A visual facing override follows deck travel while the authoritative root observes the pool.
            Quaternion target = Quaternion.LookRotation(Vector3.forward * Mathf.Sign(speed));
            transform.rotation = Quaternion.Slerp(previousFacing, target, 1f - Mathf.Exp(-14f * Time.deltaTime));
            stride = Mathf.Repeat(stride + Mathf.Abs(speed) * Time.deltaTime / 1.36f, 1f);
            gait.SetTime(stride * walking.length);
            graph.Evaluate(0f);
            float deck = transform.parent.position.y;
            float sole = Mathf.Min(leftFoot.position.y, rightFoot.position.y) - .073f * transform.lossyScale.y;
            transform.position += Vector3.up * Mathf.Clamp(deck - sole, -.12f, .12f);
        }

        void RestoreRest()
        {
            if (bones == null) return;
            for (int i = 0; i < bones.Length; i++)
                bones[i].SetLocalPositionAndRotation(positions[i], rotations[i]);
        }

        static void SignalArm(Transform arm, Transform elbow, Transform hand, Vector3 direction)
        {
            Point(arm, elbow, direction);
            Point(elbow, hand, direction);
        }

        static void Point(Transform joint, Transform next, Vector3 direction)
        {
            Vector3 current = next.position - joint.position;
            if (current.sqrMagnitude > .00001f && direction.sqrMagnitude > .00001f)
                joint.rotation = Quaternion.FromToRotation(current, direction) * joint.rotation;
        }

        void Reach(Transform arm, Transform elbow, Transform hand, Vector3 target)
        {
            float a = Vector3.Distance(arm.position, elbow.position);
            float b = Vector3.Distance(elbow.position, hand.position);
            Vector3 direction = (target - arm.position).normalized;
            float distance = Mathf.Clamp(Vector3.Distance(target, arm.position), Mathf.Abs(a-b)+.005f, a+b-.005f);
            float along = (a*a - b*b + distance*distance) / (2f*distance);
            Vector3 bend = Vector3.ProjectOnPlane(transform.right - transform.up * .35f, direction).normalized;
            Vector3 joint = arm.position + direction * along + bend * Mathf.Sqrt(Mathf.Max(0f, a*a-along*along));
            Point(arm, elbow, joint - arm.position);
            Point(elbow, hand, target - elbow.position);
        }

        void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }
    }
}
