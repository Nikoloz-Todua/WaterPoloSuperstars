using UnityEngine;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    /// <summary>Solves the final throwing arm and palm on the in-place aquatic rig.</summary>
    internal sealed class WaterPoloThrowPose
    {
        private static readonly Vector3 Up = Vector3.up;
        private readonly Transform upper, forearm, wrist, chest, head;
        private readonly Vector3 palmLocal, palmNormalLocal, fingerAxisLocal;
        private readonly Quaternion upperFrame, forearmFrame, handFrame, headFrame;

        private readonly struct ArmPose
        {
            public readonly Vector3 Ball, Pole, Fingers, Palm;

            public ArmPose(Vector3 ball, Vector3 pole, Vector3 fingers, Vector3 palm)
            {
                Ball = ball;
                Pole = pole;
                Fingers = fingers;
                Palm = palm;
            }

            public static ArmPose Blend(ArmPose a, ArmPose b, float weight) => new ArmPose(
                Vector3.Lerp(a.Ball, b.Ball, weight), Vector3.Lerp(a.Pole, b.Pole, weight),
                Vector3.Slerp(a.Fingers, b.Fingers, weight), Vector3.Slerp(a.Palm, b.Palm, weight));
        }

        private static readonly ArmPose Low = new ArmPose(
            new Vector3(.28f, -.07f, .24f), new Vector3(.45f, -.25f, -.05f),
            new Vector3(.22f, .10f, 1f), new Vector3(.08f, 1f, .02f));
        private static readonly ArmPose Load = new ArmPose(
            new Vector3(.29f, .33f, -.38f), new Vector3(.43f, .23f, -.30f),
            new Vector3(.20f, 1f, .37f), new Vector3(.20f, .47f, -.79f));
        private static readonly ArmPose CompactLoad = new ArmPose(
            new Vector3(.34f, .24f, -.20f), new Vector3(.45f, .12f, -.16f),
            new Vector3(.07f, 1f, .37f), new Vector3(.16f, .49f, -.69f));
        private static readonly ArmPose Overarm = new ArmPose(
            new Vector3(.14f, .49f, .44f), new Vector3(.25f, .28f, .03f),
            new Vector3(0f, .89f, .38f), new Vector3(.04f, .33f, 1f));
        private static readonly ArmPose PushPass = new ArmPose(
            new Vector3(.25f, .30f, .48f), new Vector3(.36f, .14f, .02f),
            new Vector3(.12f, .86f, .42f), new Vector3(.04f, .24f, 1f));
        private static readonly ArmPose Follow = new ArmPose(
            new Vector3(.03f, .05f, .53f), new Vector3(.29f, -.03f, .08f),
            new Vector3(-.14f, -.32f, 1f), new Vector3(.04f, -.94f, .43f));

        public bool Ready => upper != null && forearm != null && wrist != null && chest != null && head != null;

        public WaterPoloThrowPose(Animator rig, Vector3 facing)
        {
            upper = rig.GetBoneTransform(HumanBodyBones.RightUpperArm);
            forearm = rig.GetBoneTransform(HumanBodyBones.RightLowerArm);
            wrist = rig.GetBoneTransform(HumanBodyBones.RightHand);
            chest = rig.GetBoneTransform(HumanBodyBones.Chest);
            head = rig.GetBoneTransform(HumanBodyBones.Head);
            if (!Ready) return;

            upperFrame = FrameAt(upper, forearm, facing);
            forearmFrame = FrameAt(forearm, wrist, facing);
            headFrame = Quaternion.Inverse(head.rotation) * Quaternion.LookRotation(facing, Up);

            // The shipped rig keeps these landmarks even where the Humanoid Avatar omits finger channels.
            Transform index = wrist.Find("index_01_r");
            Transform pinky = wrist.Find("pinky_01_r");
            Vector3 indexLocal = index != null ? index.localPosition : new Vector3(0f, .12f, .035f);
            Vector3 pinkyLocal = pinky != null ? pinky.localPosition : new Vector3(0f, .10f, -.035f);
            Vector3 knuckles = (indexLocal + pinkyLocal) * .5f;
            fingerAxisLocal = knuckles.normalized;
            palmNormalLocal = Vector3.Cross(fingerAxisLocal, indexLocal - pinkyLocal).normalized;
            handFrame = Quaternion.LookRotation(fingerAxisLocal, palmNormalLocal);
            palmLocal = knuckles * .64f + palmNormalLocal * .013f;
        }

        public Vector3 Grip(float radius) =>
            wrist.TransformPoint(palmLocal) + wrist.TransformDirection(palmNormalLocal) * radius;

        public void Look(Vector3 facing, Vector3 aim, float blend, float maxYaw)
        {
            if (head == null || aim.sqrMagnitude < .001f || blend <= 0f) return;
            Vector3 flat = Vector3.ProjectOnPlane(aim, Up);
            if (flat.sqrMagnitude < .001f) return;
            float yaw = Mathf.Clamp(Vector3.SignedAngle(facing, flat, Up), -maxYaw, maxYaw);
            float pitch = Mathf.Clamp(Mathf.Atan2(aim.y, flat.magnitude) * Mathf.Rad2Deg, -14f, 19f);
            Vector3 glance = Quaternion.AngleAxis(yaw, Up) * facing;
            Quaternion look = Quaternion.LookRotation(glance + Up * Mathf.Tan(pitch * Mathf.Deg2Rad), Up);
            head.rotation = Quaternion.Slerp(head.rotation, look * Quaternion.Inverse(headFrame), blend);
        }

        public void Apply(float radius, Vector3 facing, Vector3 aim, float loadWeight, float gather,
            Vector3 gatherPoint, ThrowMotion motion, float phase, float follow)
        {
            Quaternion facingFrame = Quaternion.LookRotation(facing, Up);
            float yaw = Mathf.Clamp(Vector3.SignedAngle(facing, aim, Up), -45f, 45f);
            bool compact = motion == ThrowMotion.QuickShot || motion == ThrowMotion.CompactPass;
            bool pass = motion == ThrowMotion.CompactPass || motion == ThrowMotion.LongPass;
            ArmPose load = compact ? CompactLoad : Load;
            ArmPose release = pass ? PushPass : Overarm;
            if (motion == ThrowMotion.Lob)
                release = ArmPose.Blend(release, new ArmPose(new Vector3(.16f, .54f, .30f),
                    Overarm.Pole, Overarm.Fingers, Overarm.Palm), .7f);

            ArmPose pose = ArmPose.Blend(Low, Load, loadWeight);
            float shoulderTurn = -16f * loadWeight;
            float shoulderRoll = 0f;
            float weight = 1f;
            if (motion != ThrowMotion.None)
            {
                if (follow > 0f)
                {
                    pose = ArmPose.Blend(release, Follow, Ease(follow));
                    shoulderTurn = Mathf.Lerp(13f, 29f, follow);
                    shoulderRoll = 6f * follow;
                    weight = 1f - Ease(Mathf.InverseLerp(.37f, 1f, follow));
                }
                else
                {
                    float loadEnd = compact ? .48f : .64f;
                    float acceleration = Ease(Mathf.InverseLerp(loadEnd, 1f, phase));
                    pose = phase < loadEnd
                        ? ArmPose.Blend(pose, load, Ease(phase / loadEnd))
                        : ArmPose.Blend(load, release, acceleration);
                    float twist = compact ? 13f : pass ? 18f : 28f;
                    shoulderTurn = Mathf.Lerp(shoulderTurn, -twist, Ease(Mathf.InverseLerp(0f, loadEnd, phase))) *
                        (1f - acceleration) + 13f * acceleration;
                    shoulderRoll = Mathf.Lerp(-4f, 5f, acceleration) * Mathf.Clamp01(phase * 7f);
                }
            }

            chest.rotation = Quaternion.AngleAxis((yaw * .42f + shoulderTurn) * weight, Up) *
                Quaternion.AngleAxis(shoulderRoll * weight, facingFrame * Vector3.right) * chest.rotation;
            Quaternion throwFrame = Quaternion.AngleAxis(yaw * .83f, Up) * facingFrame;
            Vector3 shoulder = upper.position;
            float scale = Mathf.Abs(wrist.lossyScale.y);
            Vector3 target = shoulder + throwFrame * pose.Ball * scale;
            Vector3 pole = shoulder + throwFrame * pose.Pole * scale;
            if (gather < 1f)
            {
                // Reach toward the last controlled surface point before lifting toward the low hold.
                target = Vector3.Lerp(gatherPoint, target, Ease(gather));
                weight *= Mathf.Lerp(.48f, 1f, Ease(gather));
            }

            Vector3 fingers = throwFrame * pose.Fingers;
            Vector3 palm = throwFrame * pose.Palm;
            SolveArm(target, pole, fingers, palm, radius, weight);
            Vector3 gaze = aim;
            gaze.y += Mathf.Lerp(-.09f, .10f, motion != ThrowMotion.None ? .85f : loadWeight);
            Look(facing, gaze, Mathf.Lerp(.45f, .97f, motion != ThrowMotion.None ? weight : loadWeight), 45f);
        }

        private void SolveArm(Vector3 ballTarget, Vector3 pole, Vector3 fingers, Vector3 palm, float radius, float weight)
        {
            fingers.Normalize();
            palm = Vector3.ProjectOnPlane(palm, fingers).normalized;
            Quaternion desiredHand = Quaternion.LookRotation(fingers, palm) * Quaternion.Inverse(handFrame);
            Vector3 wristTarget = ballTarget -
                desiredHand * Vector3.Scale(palmLocal, wrist.lossyScale) - palm * radius;

            Vector3 shoulder = upper.position;
            float upperLength = Vector3.Distance(shoulder, forearm.position);
            float lowerLength = Vector3.Distance(forearm.position, wrist.position);
            Vector3 axis = (wristTarget - shoulder).normalized;
            if (axis == Vector3.zero) return;
            float reach = Mathf.Clamp(Vector3.Distance(wristTarget, shoulder),
                Mathf.Abs(upperLength - lowerLength) + .008f, upperLength + lowerLength - .018f);
            Vector3 bend = Vector3.ProjectOnPlane(pole - shoulder, axis).normalized;
            if (bend == Vector3.zero) bend = Vector3.ProjectOnPlane(forearm.position - shoulder, axis).normalized;
            if (bend == Vector3.zero) return;

            float along = (upperLength * upperLength - lowerLength * lowerLength + reach * reach) / (2f * reach);
            float lateral = Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - along * along));
            Vector3 upperDirection = axis * along + bend * lateral;
            Vector3 lowerDirection = axis * reach - upperDirection;
            Quaternion upperRotation = Quaternion.LookRotation(upperDirection, lowerDirection) * Quaternion.Inverse(upperFrame);
            Quaternion lowerRotation = Quaternion.LookRotation(lowerDirection, -upperDirection) * Quaternion.Inverse(forearmFrame);

            upper.rotation = Quaternion.Slerp(upper.rotation, upperRotation, weight);
            forearm.rotation = Quaternion.Slerp(forearm.rotation, lowerRotation, weight);
            Vector3 actualForearm = (wrist.position - forearm.position).normalized;
            fingers = Vector3.RotateTowards(actualForearm, fingers, 73f * Mathf.Deg2Rad, 0f);
            if (Vector3.Cross(fingers, palm).sqrMagnitude > .001f)
                desiredHand = Quaternion.LookRotation(fingers, palm) * Quaternion.Inverse(handFrame);
            wrist.rotation = Quaternion.Slerp(wrist.rotation, desiredHand, weight);
        }

        private static Quaternion FrameAt(Transform joint, Transform next, Vector3 bend) =>
            Quaternion.LookRotation(joint.InverseTransformDirection(next.position - joint.position),
                joint.InverseTransformDirection(bend));

        private static float Ease(float fraction) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(fraction));
    }
}
