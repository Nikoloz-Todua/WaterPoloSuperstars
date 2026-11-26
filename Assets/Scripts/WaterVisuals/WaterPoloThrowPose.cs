using UnityEngine;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    /// <summary>Solves the final throwing arm and palm on the in-place aquatic rig.</summary>
    internal sealed class WaterPoloThrowPose
    {
        private static readonly Vector3 Up = Vector3.up;
        private readonly Transform upper, forearm, wrist, chest, head;
        private readonly Transform leftUpper, leftForearm, leftWrist;
        private readonly Vector3 palmLocal, palmNormalLocal, fingerAxisLocal;
        private readonly Quaternion upperFrame, forearmFrame, handFrame, headFrame;
        private readonly Quaternion leftHandFrame;
        private readonly FingerJoint[] fingers;
        private bool posedFingers;

        private readonly struct FingerJoint
        {
            public readonly Transform Bone;
            public readonly Quaternion Rest;
            public readonly Vector3 CurlAxis, SpreadAxis;
            public readonly float Curl, Spread;

            public FingerJoint(Transform bone, Vector3 curlAxis, Vector3 spreadAxis, float curl, float spread)
            {
                Bone = bone;
                Rest = bone.localRotation;
                CurlAxis = bone.parent.InverseTransformDirection(curlAxis);
                SpreadAxis = bone.parent.InverseTransformDirection(spreadAxis);
                Curl = curl;
                Spread = spread;
            }
        }

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
            new Vector3(.27f, -.14f, .36f), new Vector3(.39f, -.28f, .06f),
            new Vector3(.14f, -.10f, 1f), new Vector3(.02f, -1f, -.11f));
        private static readonly ArmPose ReadyCarry = new ArmPose(
            new Vector3(.34f, .06f, .18f), new Vector3(.40f, -.16f, -.04f),
            new Vector3(.16f, .62f, .87f), new Vector3(.08f, .34f, -.88f));
        private static readonly ArmPose Load = new ArmPose(
            new Vector3(.32f, .38f, -.43f), new Vector3(.46f, .16f, -.30f),
            new Vector3(.14f, 1f, .24f), new Vector3(.14f, .30f, -.91f));
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
            leftUpper = rig.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            leftForearm = rig.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            leftWrist = rig.GetBoneTransform(HumanBodyBones.LeftHand);
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
            leftHandFrame = PalmFrame(leftWrist, "l");
            palmLocal = knuckles * .64f + palmNormalLocal * .013f;
            var joints = new System.Collections.Generic.List<FingerJoint>(12);
            string[] digits = { "index", "middle", "ring", "pinky" };
            float[] spread = { -8f, -2f, 4f, 12f };
            Vector3 normal = wrist.TransformDirection(palmNormalLocal);
            Vector3 fan = wrist.TransformDirection(fingerAxisLocal);
            Vector3 curl = Vector3.Cross(fan, normal).normalized;
            for (int digit = 0; digit < digits.Length; digit++)
            {
                Transform bone = wrist.Find(digits[digit] + "_01_r");
                for (int joint = 0; bone != null && joint < 3; joint++)
                {
                    joints.Add(new FingerJoint(bone, curl, normal, joint == 0 ? 7f : 13f,
                        joint == 0 ? spread[digit] : 0f));
                    bone = joint < 2 ? bone.Find(digits[digit] + (joint == 0 ? "_02_r" : "_03_r")) : null;
                }
            }
            fingers = joints.ToArray();
        }

        public Vector3 PalmPosition => wrist.TransformPoint(palmLocal);
        public Vector3 PalmDirection => wrist.TransformDirection(palmNormalLocal);
        public Vector3 Grip(float radius) =>
            PalmPosition + PalmDirection * radius;

        public void ResetFingers()
        {
            if (!posedFingers) return;
            foreach (var joint in fingers) joint.Bone.localRotation = joint.Rest;
            posedFingers = false;
        }

        private void PoseFingers(float curl, float spread)
        {
            foreach (var joint in fingers)
                joint.Bone.localRotation = Quaternion.AngleAxis(joint.Spread * spread, joint.SpreadAxis) *
                    Quaternion.AngleAxis(joint.Curl * curl, joint.CurlAxis) * joint.Rest;
            posedFingers = true;
        }

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

        public void Apply(float radius, float surfaceHeight, Vector3 facing, Vector3 aim, float loadWeight, float gather,
            Vector3 gatherPoint, ThrowMotion motion, float phase, float follow, float readyCarry = 0f,
            Vector3 gatherArrival = default)
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

            ArmPose pose = ArmPose.Blend(ArmPose.Blend(Low, ReadyCarry, readyCarry), Load, loadWeight);
            float departure = Mathf.Lerp(readyCarry, 1f, loadWeight);
            float shoulderTurn = -16f * loadWeight;
            float shoulderRoll = 0f;
            float weight = 1f;
            if (motion != ThrowMotion.None)
            {
                if (follow > 0f)
                {
                    pose = ArmPose.Blend(release, Follow, Ease(follow));
                    departure = 1f;
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
                    departure = Mathf.Lerp(departure, 1f, Ease(Mathf.InverseLerp(0f, loadEnd, phase)));
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
            target.y = Mathf.Lerp(surfaceHeight, target.y, departure);
            Vector3 pole = shoulder + throwFrame * pose.Pole * scale;
            if (gather < 1f)
            {
                // Reach toward the last controlled surface point before lifting toward the low hold.
                target = Vector3.Lerp(gatherPoint, target, Ease(gather));
                if (gatherArrival.sqrMagnitude < .01f) weight *= Mathf.Lerp(.48f, 1f, Ease(gather));
            }

            Vector3 fingers = throwFrame * pose.Fingers;
            Vector3 palm = throwFrame * pose.Palm;
            if (gather < 1f && gatherArrival.sqrMagnitude > .01f)
            {
                float absorb = Ease(gather);
                Vector3 facingArrival = Vector3.ProjectOnPlane(-gatherArrival, Up).normalized;
                fingers = Vector3.Slerp(Up, fingers, absorb);
                palm = Vector3.Slerp((facingArrival + Up * .12f).normalized, palm, absorb);
            }
            SolveArm(target, pole, fingers, palm, radius, weight);
            PoseFingers(follow > 0f ? 1f - weight : .70f, weight);
            Vector3 gaze = aim;
            gaze.y += Mathf.Lerp(-.09f, .10f, motion != ThrowMotion.None ? .85f : loadWeight);
            Look(facing, gaze, Mathf.Lerp(.45f, .97f, motion != ThrowMotion.None ? weight : loadWeight), 45f);
        }

        public void Receive(float radius, Vector3 facing, Vector3 incoming, Vector3 ballTarget, float reach)
        {
            Vector3 towardBall = Vector3.ProjectOnPlane(-incoming, Up).normalized;
            Vector3 palm = (towardBall + Up * .12f).normalized;
            Vector3 right = Vector3.Cross(Up, facing);
            Vector3 pole = upper.position + right * .36f - Up * .20f + towardBall * .13f;
            SolveArm(ballTarget, pole, Up + right * .10f, palm, radius, reach);
            PoseFingers(.12f, reach);
            Look(facing, ballTarget - head.position, reach, 65f);
        }

        public void Guard(Vector3 facing, Vector3 target, float amount)
        {
            Vector3 right = Vector3.Cross(Up, facing);
            Vector3 lane = Vector3.ProjectOnPlane(target - chest.position, Up).normalized;
            bool left = Vector3.Dot(lane, right) < -.12f && leftUpper != null && leftForearm != null && leftWrist != null;
            Vector3 side = left ? -right : right;
            Transform shoulder = left ? leftUpper : upper;
            Vector3 end = shoulder.position + Up * .53f + lane * .25f + side * .15f;
            Vector3 pole = shoulder.position + side * .28f + lane * .18f - Up * .12f;
            Transform elbow = left ? leftForearm : forearm;
            Transform hand = left ? leftWrist : wrist;
            Reach(shoulder, elbow, hand, end, pole, amount);
            OpenHand(hand, elbow, left ? leftHandFrame : handFrame, lane, amount);
            Look(facing, target - head.position, amount, 58f);
        }

        public void Keeper(Vector3 facing, Vector3 tracking, Vector3 contact, float dive, bool twoHands, float radius)
        {
            if (leftUpper == null || leftForearm == null || leftWrist == null) return;
            Vector3 right = Vector3.Cross(Up, facing);
            float lateral = Vector3.Dot(contact - chest.position, right);
            float height = contact.y - chest.position.y;
            bool central = twoHands && Mathf.Abs(lateral) < .26f && height > .23f;
            bool rightSide = lateral >= 0f;
            chest.rotation = Quaternion.AngleAxis(-Mathf.Clamp(lateral * 35f, -22f, 22f) * dive, facing) *
                Quaternion.AngleAxis(7f * dive, right) * chest.rotation;
            Vector3 interception = contact - facing * (radius * .65f);
            Vector3 rightReady = upper.position + right * .33f + facing * .33f - Up * .03f;
            Vector3 leftReady = leftUpper.position - right * .33f + facing * .33f - Up * .03f;
            Vector3 rightTarget = central ? interception + right * .11f : rightSide ? interception : rightReady - Up * .14f;
            Vector3 leftTarget = central ? interception - right * .11f : !rightSide ? interception : leftReady - Up * .14f;
            Reach(upper, forearm, wrist, Vector3.Lerp(rightReady, rightTarget, dive),
                upper.position + right * .35f - Up * .18f, Mathf.Lerp(.57f, 1f, dive));
            Reach(leftUpper, leftForearm, leftWrist, Vector3.Lerp(leftReady, leftTarget, dive),
                leftUpper.position - right * .35f - Up * .18f, Mathf.Lerp(.57f, 1f, dive));
            OpenHand(wrist, forearm, handFrame, facing, Mathf.Lerp(.52f, .90f, dive));
            OpenHand(leftWrist, leftForearm, leftHandFrame, facing, Mathf.Lerp(.52f, .90f, dive));
            Look(facing, tracking - head.position, .75f, 68f);
        }

        private static Quaternion PalmFrame(Transform hand, string side)
        {
            var index = hand != null ? hand.Find("index_01_" + side) : null;
            var pinky = hand != null ? hand.Find("pinky_01_" + side) : null;
            if (index == null || pinky == null) return Quaternion.identity;
            Vector3 fingers = (index.localPosition + pinky.localPosition).normalized;
            Vector3 palm = Vector3.Cross(index.localPosition - pinky.localPosition, fingers);
            return Quaternion.LookRotation(fingers, palm);
        }

        private static void OpenHand(Transform hand, Transform elbow, Quaternion frame, Vector3 forward, float weight)
        {
            Vector3 fingers = (hand.position - elbow.position).normalized;
            Vector3 palm = Vector3.ProjectOnPlane(forward, fingers);
            if (palm.sqrMagnitude < .02f || fingers.sqrMagnitude < .01f) return;
            Quaternion open = Quaternion.LookRotation(fingers, palm) * Quaternion.Inverse(frame);
            hand.rotation = Quaternion.Slerp(hand.rotation, open, weight);
        }

        private static void Reach(Transform shoulder, Transform elbow, Transform hand, Vector3 target, Vector3 pole, float weight)
        {
            Vector3 first = elbow.position - shoulder.position;
            Vector3 second = hand.position - elbow.position;
            if (!JointDirections(shoulder.position, target, pole, first, second, out var upperAim, out var lowerAim))
                return;
            shoulder.rotation = Quaternion.Slerp(shoulder.rotation,
                Quaternion.FromToRotation(first, upperAim) * shoulder.rotation, weight);
            elbow.rotation = Quaternion.Slerp(elbow.rotation,
                Quaternion.FromToRotation(hand.position - elbow.position, lowerAim) * elbow.rotation, weight);
        }

        private void SolveArm(Vector3 ballTarget, Vector3 pole, Vector3 fingers, Vector3 palm, float radius, float weight)
        {
            fingers.Normalize();
            palm = Vector3.ProjectOnPlane(palm, fingers).normalized;
            Quaternion desiredHand = Quaternion.LookRotation(fingers, palm) * Quaternion.Inverse(handFrame);
            Vector3 wristTarget = ballTarget -
                desiredHand * Vector3.Scale(palmLocal, wrist.lossyScale) - palm * radius;

            if (!JointDirections(upper.position, wristTarget, pole, forearm.position - upper.position,
                wrist.position - forearm.position, out var upperDirection, out var lowerDirection)) return;
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

        private static bool JointDirections(Vector3 shoulder, Vector3 target, Vector3 pole, Vector3 first,
            Vector3 second, out Vector3 upperDirection, out Vector3 lowerDirection)
        {
            upperDirection = lowerDirection = Vector3.zero;
            Vector3 axis = (target - shoulder).normalized;
            if (axis == Vector3.zero) return false;
            float upperLength = first.magnitude;
            float lowerLength = second.magnitude;
            float reach = Mathf.Clamp(Vector3.Distance(target, shoulder),
                Mathf.Abs(upperLength - lowerLength) + .008f, upperLength + lowerLength - .018f);
            Vector3 bend = Vector3.ProjectOnPlane(pole - shoulder, axis).normalized;
            if (bend == Vector3.zero) bend = Vector3.ProjectOnPlane(first, axis).normalized;
            if (bend == Vector3.zero) return false;
            float along = (upperLength * upperLength - lowerLength * lowerLength + reach * reach) / (2f * reach);
            float lateral = Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - along * along));
            upperDirection = axis * along + bend * lateral;
            lowerDirection = axis * reach - upperDirection;
            return true;
        }
        private static Quaternion FrameAt(Transform joint, Transform next, Vector3 bend) =>
            Quaternion.LookRotation(joint.InverseTransformDirection(next.position - joint.position),
                joint.InverseTransformDirection(bend));

        private static float Ease(float fraction) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(fraction));
    }
}
