using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEngine;
using Object = UnityEngine.Object;

public static class RefereeMilestone30Verification
{
    const string Output = "docs/Evidence/Milestone30";

    public static async Task<string> Run()
    {
        var match = Object.FindAnyObjectByType<PrototypeMatch>();
        var ball = Object.FindAnyObjectByType<BallController>();
        var roster = Object.FindAnyObjectByType<PrototypeRoster>();
        var intro = Object.FindAnyObjectByType<PreMatchPresentation>();
        var selection = Object.FindAnyObjectByType<PlayerSelection>();
        var referees = Object.FindObjectsByType<PoolsideReferee>();
        Check(referees.Length == 2 && referees.All(r => r.GetComponentsInChildren<Collider>().Length == 0),
            "Two presentation-only deck officials");
        var observer = referees.OrderByDescending(r => r.transform.position.x).First();
        var pose = observer.GetComponentInChildren<ProfessionalRefereePose>();
        var animator = pose.GetComponent<Animator>();
        Check(animator.isHuman && animator.avatar.isValid && !animator.applyRootMotion, "In-place Humanoid model");
        if (intro.Active) intro.SkipAll();
        await Until(() => match.IsPlaying && !match.AwaitingOpeningPossession, "Physical opening acquisition");
        Check(match.Clock.Remaining > 8f, "Run before the final eight live seconds of the period");
        match.ShotClock.ResetPossession();

        var movement = Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>()
            .Concat(Object.FindObjectsByType<PrototypeGoalkeeper>()).ToArray();
        var prior = movement.Select(b => b.enabled).ToArray();
        var view = match.GameplayView;
        var lens = view.GetComponent<Camera>();
        Vector3 oldPosition = lens.transform.position;
        Quaternion oldRotation = lens.transform.rotation;
        float oldFov = lens.fieldOfView;
        Directory.CreateDirectory(Output);
        foreach (var motor in movement) motor.enabled = false;
        view.enabled = false;

        try
        {
            var blue = roster.FieldPlayers.Where(p => p.Team == PrototypeTeam.Blue).ToArray();
            var white = roster.FieldPlayers.Where(p => p.Team == PrototypeTeam.White).ToArray();
            Vector3 attack = Object.FindObjectsByType<PrototypeTeamTactics>()
                .Single(t => t.Team == PrototypeTeam.Blue).AttackingGoal.transform.forward;
            foreach (var player in roster.Members)
                player.Motor.ResetPose(new Vector3(player.Team == PrototypeTeam.Blue ? -8f : 8f, 0f,
                    (player.CapNumber % 7 - 3) * 2f), Quaternion.identity);

            ball.ResetForRestart(new Vector3(0f, ball.SurfaceHeight, attack.z * 11f));
            await Until(() => Mathf.Abs(observer.Speed) >= 1.05f, "Deck route movement");
            await WaitFrames(8);
            float gaze = Vector3.Dot(Planar(pose.HeadLookDirection).normalized, Planar(observer.ObservationPoint - pose.transform.position).normalized);
            float facing = Vector3.Dot(Planar(pose.transform.forward).normalized, Vector3.forward * Mathf.Sign(observer.Speed));
            Check(gaze > .63f && facing > .85f, $"Travel and observation are independent (gaze {gaze:F3}, travel {facing:F3})");
            await Capture(lens, pose, "Official-Walk-A");
            var leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            float stepWidth = float.PositiveInfinity;
            float stanceHeight = float.PositiveInfinity;
            float sampleUntil = Time.time + .48f;
            while (Time.time < sampleUntil)
            {
                stepWidth = Mathf.Min(stepWidth, Vector3.Dot(rightFoot.position - leftFoot.position, pose.transform.right));
                stanceHeight = Mathf.Min(stanceHeight, Mathf.Min(leftFoot.position.y, rightFoot.position.y) - .073f);
                await WaitFrames(1);
            }
            Check(stepWidth > .10f && stanceHeight > .67f && stanceHeight < .74f,
                $"Foot lane separation and stance height ({stepWidth:F3} m, Y={stanceHeight:F3} m)");
            await Capture(lens, pose, "Official-Walk-B");

            Claim(blue[0], Vector3.zero, Vector3.right);
            white[0].Motor.ResetPose(Vector3.left * .8f, Quaternion.identity);
            Check(match.Rules.ReportContact(white[0], blue[0], IllegalContact.Push), "Blue gets the ordinary free throw");
            await Seconds(.59f);
            float bluePoint = ArmDot(animator, attack);
            Check(referees.All(r => r.Signal == RefereeSignal.Foul && Vector3.Dot(r.SignalledAttackDirection, attack) > .99f) &&
                bluePoint > .91f, $"Blue's signed attack gesture ({bluePoint:F3})");
            await Capture(lens, pose, "Official-Free-Throw");
            match.EndRuleRestart();

            Claim(white[0], Vector3.zero, Vector3.left);
            blue[0].Motor.ResetPose(Vector3.right * .8f, Quaternion.identity);
            Check(match.Rules.ReportContact(blue[0], white[0], IllegalContact.Push), "White gets the ordinary free throw");
            await Seconds(.61f);
            float whitePoint = ArmDot(animator, -attack);
            Check(referees.All(r => r.Signal == RefereeSignal.Foul && Vector3.Dot(r.SignalledAttackDirection, -attack) > .99f) &&
                whitePoint > .91f, $"White's opposing attack gesture ({whitePoint:F3})");
            match.EndRuleRestart();

            Claim(blue[0], Vector3.zero, Vector3.right);
            Vector3 offence = new(.7f, ball.SurfaceHeight, 2.3f);
            blue[1].Motor.ResetPose(new Vector3(0f, offence.y, offence.z), Quaternion.identity);
            white[1].Motor.ResetPose(offence, Quaternion.identity);
            Check(match.Rules.ReportContact(white[1], blue[1], IllegalContact.Hold), "Actual off-ball holding exclusion");
            await Seconds(.17f);
            Check(referees.All(r => r.Signal == RefereeSignal.Exclusion && r.SignalledCapNumber == white[1].CapNumber) &&
                Vector3.Distance(observer.ObservationPoint, offence) < .1f && !white[1].EligibleForPlay,
                "Captured exclusion point, roster cap number and authoritative removal");
            float pointingWrist = animator.GetBoneTransform(HumanBodyBones.RightHand).position.y;
            await Capture(lens, pose, "Official-Exclusion-Point");
            await Seconds(.39f);
            float sweptWrist = animator.GetBoneTransform(HumanBodyBones.RightHand).position.y;
            Check(sweptWrist - pointingWrist > .30f, "Arm sweeps up toward the field boundary");
            await Capture(lens, pose, "Official-Exclusion-Sweep");
            match.EndRuleRestart();

            var goal = Object.FindObjectsByType<PrototypeTeamTactics>().Single(t => t.Team == PrototypeTeam.Blue).AttackingGoal;
            Vector3 fiveMetres = goal.transform.position - attack * 5f;
            Claim(blue[0], fiveMetres, attack);
            white[0].Motor.ResetPose(fiveMetres - attack * .8f, Quaternion.LookRotation(attack));
            Check(blue[0].GetComponent<ShotCharge>().BeginCharge(), "Genuine shooting intent for the penalty fixture");
            Check(match.Rules.ReportContact(white[0], blue[0], IllegalContact.Push), "Actual qualifying shooting foul");
            await Until(() => match.Phase == MatchPhase.PenaltySetup, "Advantage resolves to five-metre penalty");
            await Seconds(.22f);
            int decisions = match.Rules.DecisionCount;
            var head = animator.GetBoneTransform(HumanBodyBones.Head);
            Check(referees.All(r => r.Signal == RefereeSignal.Penalty && r.SignalledCapNumber == white[0].CapNumber) &&
                animator.GetBoneTransform(HumanBodyBones.LeftHand).position.y > head.position.y + .15f, "Penalty award arm raised");
            await Capture(lens, pose, "Official-Penalty-Award");
            await Until(() => match.Phase == MatchPhase.PenaltyReady, "Existing ready phase");
            await Seconds(.14f);
            var right = animator.GetBoneTransform(HumanBodyBones.RightHand);
            float raised = right.position.y;
            Check(referees.All(r => r.Signal == RefereeSignal.Start) && raised > head.position.y + .13f,
                "Penalty readiness raises the start hand");
            await Seconds(.46f);
            Check(right.position.y < raised - .3f && match.Phase == MatchPhase.PenaltyReady &&
                match.Rules.DecisionCount == decisions && ball.Owner == blue[0].Interaction,
                "Arm lowering leaves the ready phase and possession intact");
            await Capture(lens, pose, "Official-Penalty-Start");
            return $"PASS. Walk gaze dot {gaze:F3}, travel facing dot {facing:F3}, minimum ankle separation {stepWidth:F3} m, " +
                $"estimated stance under the lower ankle at world Y={stanceHeight:F3} m, opposing award arm dots {bluePoint:F3}/{whitePoint:F3}, " +
                $"exclusion lift {sweptWrist - pointingWrist:F2} m. Both officials used live events and the ready cue left the penalty unplayed.";
        }
        finally
        {
            for (int i = 0; i < movement.Length; i++) movement[i].enabled = prior[i];
            lens.transform.SetPositionAndRotation(oldPosition, oldRotation);
            lens.fieldOfView = oldFov;
            view.enabled = true;
        }

        void Claim(SwimmerIdentity player, Vector3 point, Vector3 facing)
        {
            match.ShotClock.ResetPossession();
            player.Motor.ResetPose(point, Quaternion.LookRotation(facing));
            ball.ResetForRestart(player.Motor.Position);
            Check(player.Interaction.TryCollect(), $"Legal isolated possession setup for {player.Team} #{player.CapNumber} " +
                $"({match.Phase}, collect={ball.CanCollect}, owner={ball.Owner}, ball={ball.PhysicsPosition}, motor={player.Motor.Position})");
            if (player.Team == selection.HumanTeam) selection.Select(player);
        }
    }

    static float ArmDot(Animator animator, Vector3 direction)
    {
        float Dot(HumanBodyBones shoulder, HumanBodyBones wrist) => Vector3.Dot(direction,
            (animator.GetBoneTransform(wrist).position - animator.GetBoneTransform(shoulder).position).normalized);
        return Mathf.Max(Dot(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftHand),
            Dot(HumanBodyBones.RightUpperArm, HumanBodyBones.RightHand));
    }

    static Vector3 Planar(Vector3 point) => Vector3.ProjectOnPlane(point, Vector3.up);

    static async Task Capture(Camera lens, ProfessionalRefereePose pose, string file)
    {
        Vector3 target = pose.transform.position + Vector3.up * 1.15f;
        Vector3 poolward = Planar(pose.HeadLookDirection).normalized;
        Vector3 across = Vector3.Cross(Vector3.up, poolward);
        Vector3 eye = target + poolward * 3.5f + across * 1.15f + Vector3.up * .18f;
        Vector3 frame = file.EndsWith("Award") || file.EndsWith("Sweep") ? target + across * 1.18f : target;
        lens.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(frame - eye));
        lens.fieldOfView = 43f;
        await WaitFrames(2);
        ScreenCapture.CaptureScreenshot(Path.GetFullPath($"{Output}/{file}.png"));
        await WaitFrames(2);
    }

    static Task WaitFrames(int frames)
    {
        int target = Time.frameCount + frames;
        return Until(() => Time.frameCount >= target, "Advancing Game View");
    }

    static Task Seconds(float duration)
    {
        float target = Time.time + duration;
        return Until(() => Time.time >= target, "Advancing Play Mode");
    }

    static async Task Until(Func<bool> condition, string action)
    {
        float deadline = Time.realtimeSinceStartup + 10f;
        while (!condition())
        {
            Check(Time.realtimeSinceStartup < deadline, $"{action} timed out");
            await Task.Delay(12);
        }
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
