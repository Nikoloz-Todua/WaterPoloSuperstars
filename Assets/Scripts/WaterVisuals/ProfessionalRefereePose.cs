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
        Transform leftArm, leftElbow, leftHand, rightArm, rightElbow, rightHand, head, chest, neck, leftFoot, rightFoot;
        Quaternion modelFacing, bodyHeading;
        DeckFootPlacement footPlacement;
        float stride, gazeYaw, gazePitch;
        Vector3 headLocalForward;
        RefereeSignal lastSignal;
        float signalAge;
        public Vector3 HeadLookDirection => head != null ? head.TransformDirection(headLocalForward) : transform.forward;

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
            if (head != null) headLocalForward = head.InverseTransformDirection(transform.forward);
            chest = animator.GetBoneTransform(HumanBodyBones.UpperChest) ??
                animator.GetBoneTransform(HumanBodyBones.Chest);
            neck = animator.GetBoneTransform(HumanBodyBones.Neck);
            leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            footPlacement = new DeckFootPlacement(animator.GetBoneTransform, transform,
                .073f * transform.lossyScale.y, .019f * transform.lossyScale.y);
            Stand();
        }

        public void Stand()
        {
            RestoreRest();
            transform.localRotation = modelFacing;
            bodyHeading = transform.rotation;
            RelaxArms();
        }

        void RelaxArms()
        {
            Vector3 down = -transform.up;
            Point(leftArm, leftElbow, down - .11f * transform.right);
            Point(rightArm, rightElbow, down + .11f * transform.right);
            Point(leftElbow, leftHand, down + .14f * transform.forward);
            Point(rightElbow, rightHand, down + .14f * transform.forward);
        }

        public bool FacesTravel(float direction) =>
            Vector3.Dot(bodyHeading * Vector3.forward, Vector3.forward * direction) >= .95f;

        public void AnimateMatchOfficial(float travelled, float travelDirection, float deck,
            Vector3 observation, RefereePoseCue cue)
        {
            RestoreRest();
            Vector3 facing = Mathf.Abs(travelDirection) > .5f ? Vector3.forward * travelDirection :
                Vector3.ProjectOnPlane(observation - transform.position, Vector3.up);
            if (facing.sqrMagnitude > .01f)
                bodyHeading = Quaternion.RotateTowards(bodyHeading, Quaternion.LookRotation(facing), Time.deltaTime * 210f);
            transform.rotation = bodyHeading;

            var feet = new Vector3(transform.position.x, deck, transform.position.z);
            float strideLength = 1.36f * transform.lossyScale.y;
            if (Mathf.Abs(travelled) > .0002f && walking != null)
            {
                SampleWalk(Mathf.Abs(travelled), strideLength, false);
                float speed = Mathf.Abs(travelled) / Mathf.Max(.001f, Time.deltaTime);
                footPlacement.Apply(transform, feet, Vector3.forward * travelDirection, stride,
                    strideLength, Mathf.InverseLerp(.03f, .55f, speed));
            }
            else
            {
                footPlacement.Apply(transform, feet, transform.forward, stride, strideLength, 0f);
                RelaxArms();
            }

            Observe(observation);
            if (cue.Signal != RefereeSignal.None) SignalCue(cue);
        }

        void Observe(Vector3 target)
        {
            if (head == null) return;
            Vector3 look = target - head.position;
            Vector3 flat = Vector3.ProjectOnPlane(look, Vector3.up);
            if (flat.sqrMagnitude < .01f) return;
            float yaw = Vector3.SignedAngle(transform.forward, flat, Vector3.up);
            float pitch = Mathf.Atan2(-look.y, flat.magnitude) * Mathf.Rad2Deg;
            gazeYaw = Mathf.MoveTowards(gazeYaw, Mathf.Clamp(yaw, -96f, 96f), 240f * Time.deltaTime);
            gazePitch = Mathf.MoveTowards(gazePitch, Mathf.Clamp(pitch, -17f, 18f), 150f * Time.deltaTime);
            if (chest != null) chest.rotation = Quaternion.AngleAxis(gazeYaw * .35f, Vector3.up) * chest.rotation;
            if (neck != null) neck.rotation = Quaternion.AngleAxis(gazeYaw * .18f, Vector3.up) * neck.rotation;
            float headYaw = gazeYaw * (1f - (chest != null ? .35f : 0f) - (neck != null ? .18f : 0f));
            head.rotation = Quaternion.AngleAxis(gazePitch, Quaternion.AngleAxis(gazeYaw, Vector3.up) * transform.right) *
                Quaternion.AngleAxis(headYaw, Vector3.up) * head.rotation;
        }

        void SignalCue(RefereePoseCue cue)
        {
            float age = cue.Age;
            float weight = Mathf.SmoothStep(0f, 1f, Mathf.Min(Mathf.InverseLerp(0f, .11f, age),
                Mathf.InverseLerp(0f, .16f, cue.Duration - age)));
            Vector3 rightAtMark = cue.Point - rightArm.position;
            switch (cue.Signal)
            {
                // World Aquatics Part Six Appendix 2, figures A, B, D, H, I and O.
                case RefereeSignal.Start:
                    float lower = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.18f, .66f, age));
                    SignalArm(rightArm, rightElbow, rightHand, Vector3.Slerp(Vector3.up,
                        rightAtMark.normalized, lower), weight);
                    break;
                case RefereeSignal.Goal:
                    SignalArm(rightArm, rightElbow, rightHand, rightAtMark, weight);
                    break;
                case RefereeSignal.Exclusion:
                    Vector3 exit = Vector3.ProjectOnPlane(cue.Boundary - cue.Point, Vector3.up).normalized;
                    float sweep = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.27f, .56f, age));
                    float hold = cue.Contact == IllegalContact.Hold ? Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(.84f, 1.12f, age)) : 0f;
                    SignalArm(rightArm, rightElbow, rightHand, Vector3.Slerp(rightAtMark.normalized,
                        (exit * .62f + Vector3.up).normalized, sweep), weight * (1f - hold));
                    if (hold > 0f)
                    {
                        Vector3 across = chest != null ? chest.position : head.position - transform.up * .28f;
                        Reach(rightArm, rightElbow, rightHand, across + transform.forward * .27f -
                            transform.right * .15f - transform.up * .08f, weight * hold);
                        Reach(leftArm, leftElbow, leftHand, rightHand.position + transform.right * .065f, weight * hold);
                    }
                    break;
                case RefereeSignal.Penalty:
                    SignalArm(leftArm, leftElbow, leftHand, Vector3.up + transform.forward * .08f, weight);
                    break;
                case RefereeSignal.Foul:
                    if (cue.Contact == IllegalContact.Push && age < .43f)
                    {
                        float push = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.04f, .31f, age));
                        Vector3 reach = transform.forward * Mathf.Lerp(.23f, .54f, push) - transform.up * .15f;
                        Reach(leftArm, leftElbow, leftHand, leftArm.position + reach - transform.right * .12f, weight);
                        Reach(rightArm, rightElbow, rightHand, rightArm.position + reach + transform.right * .12f, weight);
                        break;
                    }
                    SignalDirection(cue, weight);
                    break;
                case RefereeSignal.Restart:
                    SignalDirection(cue, weight);
                    break;
            }
        }

        void SignalDirection(RefereePoseCue cue, float weight)
        {
            // Choose the shoulder offering the cleanest path parallel to the sideline.
            Vector3 attack = Vector3.ProjectOnPlane(cue.Direction, Vector3.up);
            if (attack.sqrMagnitude < .01f) return;
            if (Vector3.Dot(rightArm.position - leftArm.position, attack) >= 0f)
            {
                SignalArm(rightArm, rightElbow, rightHand, attack, weight);
                SignalArm(leftArm, leftElbow, leftHand, cue.Point - leftArm.position, weight);
            }
            else
            {
                SignalArm(leftArm, leftElbow, leftHand, attack, weight);
                SignalArm(rightArm, rightElbow, rightHand, cue.Point - rightArm.position, weight);
            }
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
            // Recovery path for the Milestone 29 presentation.
            Quaternion target = Quaternion.LookRotation(Vector3.forward * Mathf.Sign(speed));
            transform.rotation = Quaternion.Slerp(previousFacing, target, 1f - Mathf.Exp(-14f * Time.deltaTime));
            SampleWalk(Mathf.Abs(speed) * Time.deltaTime, 1.36f);
            float deck = transform.parent.position.y;
            float sole = Mathf.Min(leftFoot.position.y, rightFoot.position.y) - .073f * transform.lossyScale.y;
            transform.position += Vector3.up * Mathf.Clamp(deck - sole, -.12f, .12f);
        }

        void SampleWalk(float distance, float strideDistance, bool useFootIK = true)
        {
            if (!graph.IsValid())
            {
                graph = PlayableGraph.Create("Official deck gait");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                gait = AnimationClipPlayable.Create(graph, walking);
                AnimationPlayableOutput.Create(graph, "Deck walking", animator).SetSourcePlayable(gait);
                graph.Play();
            }
            gait.SetApplyFootIK(useFootIK);
            stride = Mathf.Repeat(stride + distance / strideDistance, 1f);
            gait.SetTime(stride * walking.length);
            graph.Evaluate(0f);
        }

        void RestoreRest()
        {
            if (bones == null) return;
            for (int i = 0; i < bones.Length; i++)
                bones[i].SetLocalPositionAndRotation(positions[i], rotations[i]);
        }

        static void SignalArm(Transform arm, Transform elbow, Transform hand, Vector3 direction, float weight = 1f)
        {
            Point(arm, elbow, direction, weight);
            Point(elbow, hand, direction, weight);
        }

        static void Point(Transform joint, Transform next, Vector3 direction, float weight = 1f)
        {
            Vector3 current = next.position - joint.position;
            if (current.sqrMagnitude > .00001f && direction.sqrMagnitude > .00001f)
                joint.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(current, direction), weight) *
                    joint.rotation;
        }

        void Reach(Transform arm, Transform elbow, Transform hand, Vector3 target, float weight = 1f)
        {
            float a = Vector3.Distance(arm.position, elbow.position);
            float b = Vector3.Distance(elbow.position, hand.position);
            Vector3 direction = (target - arm.position).normalized;
            float distance = Mathf.Clamp(Vector3.Distance(target, arm.position), Mathf.Abs(a-b)+.005f, a+b-.005f);
            float along = (a*a - b*b + distance*distance) / (2f*distance);
            Vector3 bendSide = arm == rightArm ? transform.right : -transform.right;
            Vector3 bend = Vector3.ProjectOnPlane(bendSide - transform.up * .35f, direction).normalized;
            Vector3 joint = arm.position + direction * along + bend * Mathf.Sqrt(Mathf.Max(0f, a*a-along*along));
            Point(arm, elbow, joint - arm.position, weight);
            Point(elbow, hand, target - elbow.position, weight);
        }

        void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }
    }
}
