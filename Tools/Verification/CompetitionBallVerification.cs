using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEngine;
using Object = UnityEngine.Object;

public static class CompetitionBallVerification
{
    const string Output = "Logs/Milestone29/Ball";
    static T One<T>() where T : Object => Object.FindAnyObjectByType<T>();
    static void Check(bool passed, string message)
    {
        if (!passed) throw new InvalidOperationException(message);
    }
    static async Task Await(Func<bool> predicate, string message, float timeout = 7f)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (!predicate())
        {
            if (Time.realtimeSinceStartup > deadline) throw new TimeoutException(message);
            await Task.Delay(12);
        }
    }
    static async Task Capture(string name, Vector3 focus, Vector3 offset)
    {
        var dev = One<ArenaDeveloperCamera>();
        var view = dev.viewpoints[2];
        view.position = focus + offset;
        view.LookAt(focus);
        dev.SetView(2);
        int frame = Time.frameCount;
        await Await(() => Time.frameCount >= frame + 2, "Game view advancing.");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath(Output + "/" + name + ".png"));
        await Await(() => Time.frameCount >= frame + 5, "Game view captured.");
        dev.SetView(-1);
    }
    static Vector3 Planar(Vector3 value) => Vector3.ProjectOnPlane(value, Vector3.up);

    public static async Task<object> Presentation()
    {
        var match = One<PrototypeMatch>();
        var intro = One<PreMatchPresentation>();
        if (intro.Active) intro.SkipAll();
        await Await(() => match.IsPlaying && !match.AwaitingOpeningPossession, "Protected sprint-off completed.", 18f);
        var roster = One<PrototypeRoster>();
        var ball = One<BallController>();
        var player = One<PlayerSelection>().ActivePlayer;
        var visual = player.GetComponentInChildren<RiggedSwimmerVisual>();
        var stamina = player.GetComponent<SwimmerStamina>();
        var input = player.GetComponent<SwimmerInput>();
        var brains = Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>()
            .Concat(Object.FindObjectsByType<PrototypeGoalkeeper>()).Where(behaviour => behaviour.enabled).ToArray();
        MatchCameraView prior = match.GameplayView.View;
        Directory.CreateDirectory(Output);
        try
        {
            foreach (var brain in brains) brain.enabled = false;
            input.enabled = false;
            foreach (var member in roster.Members.Where(member => member != player))
                member.Motor.ResetPose(new Vector3(member.Team == PrototypeTeam.Blue ? -7.5f : 7.5f, 0,
                    -9f + member.CapNumber), Quaternion.identity);
            var cameraSamples = new System.Collections.Generic.List<object>();
            foreach (MatchCameraView view in new[] { MatchCameraView.Lengthwise, MatchCameraView.SidelineBroadcast })
            {
                match.GameplayView.SetView(view);
                await Task.Delay(350);
                int draws = 0, triangles = 0;
                for (int i = 0; i < 30; i++)
                {
                    int frame = Time.frameCount;
                    await Await(() => Time.frameCount > frame, "Camera performance frames.");
                    draws += UnityEditor.UnityStats.drawCalls;
                    triangles += UnityEditor.UnityStats.triangles;
                }
                cameraSamples.Add(new { view, drawCalls = draws / 30f, renderedTriangles = triangles / 30f });
            }

            var athleteRenderers = roster.Members.SelectMany(member => member.GetComponentsInChildren<SkinnedMeshRenderer>())
                .Where(renderer => renderer.enabled).ToArray();
            var officialRenderers = Object.FindObjectsByType<ProfessionalRefereePose>()
                .SelectMany(pose => pose.GetComponentsInChildren<SkinnedMeshRenderer>()).ToArray();
            var materials = athleteRenderers.Cast<Renderer>().Concat(officialRenderers)
                .Append(ball.transform.Find("Visual").GetComponent<Renderer>())
                .SelectMany(renderer => renderer.sharedMaterials).Where(material => material != null).Distinct().ToArray();
            var textures = materials.SelectMany(material => material.GetTexturePropertyNames().Select(material.GetTexture))
                .Where(texture => texture != null).Distinct().ToArray();
            long textureBytes = textures.Sum(UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong);

            var strokes = new System.Collections.Generic.List<object>();
            foreach (bool sprint in new[] { false, true })
            {
                var point = new Vector3(0, 0, -8);
                player.Motor.ResetPose(point, Quaternion.identity);
                ball.ResetForRestart(point + Vector3.forward * .48f);
                await Await(() => ball.Owner == player.Interaction, "Automatic acquisition for the crawl sample.");
                input.enabled = false;
                player.Motor.SetMoveInput(Vector2.up);
                stamina.RequestSprint(sprint);
                await Await(() => ball.IsDribbling && (!sprint || stamina.IsSprinting), "Sustained dribbling for pose capture.");
                await Task.Delay(210);
                var hips = visual.Animator.GetBoneTransform(HumanBodyBones.Hips);
                var hands = new[] { visual.Animator.GetBoneTransform(HumanBodyBones.LeftHand), visual.Animator.GetBoneTransform(HumanBodyBones.RightHand) };
                var elbows = new[] { visual.Animator.GetBoneTransform(HumanBodyBones.LeftLowerArm), visual.Animator.GetBoneTransform(HumanBodyBones.RightLowerArm) };
                float leastLead = float.MaxValue, nearestForearm = float.MaxValue, lowestHip = float.MaxValue, highestHip = float.MinValue;
                float waterline = visual.pool.Waterline;
                var dev = One<ArenaDeveloperCamera>();
                var camera = dev.viewpoints[2];
                dev.SetView(2);
                float began = Time.time;
                float duration = sprint ? 1.1f : 1.4f;
                int sample = 0;
                while (Time.time - began < duration)
                {
                    Vector3 centre = ball.transform.position;
                    float hipY = hips.position.y - waterline;
                    lowestHip = Mathf.Min(lowestHip, hipY);
                    highestHip = Mathf.Max(highestHip, hipY);
                    leastLead = Mathf.Min(leastLead, Vector3.Dot(centre - visual.HeadPosition, Vector3.forward));
                    for (int side = 0; side < hands.Length; side++)
                    {
                        Vector3 elbow = elbows[side].position;
                        Vector3 forearm = hands[side].position - elbow;
                        float t = Mathf.Clamp01(Vector3.Dot(centre - elbow, forearm) / Mathf.Max(forearm.sqrMagnitude, .0001f));
                        nearestForearm = Mathf.Min(nearestForearm, Vector3.Distance(centre, elbow + forearm * t));
                    }
                    Vector3 focus = visual.HeadPosition - Vector3.forward * .22f;
                    camera.position = focus + new Vector3(2.45f, .60f, .92f);
                    camera.LookAt(focus);
                    if (sample < 4 && Time.time - began >= sample * duration / 4)
                    {
                        ScreenCapture.CaptureScreenshot(Path.GetFullPath($"{Output}/{(sprint ? "Sprint" : "Crawl")}-Stroke-{++sample}.png"));
                    }
                    await Task.Delay(12);
                }
                strokes.Add(new { sprint, leastLead, nearestForearm, lowestHip, highestHip, radius = ball.Radius, framesCaptured = sample });
                dev.SetView(-1);
                player.Motor.StopMotion();
            }
            int Triangles(SkinnedMeshRenderer renderer) => Enumerable.Range(0, renderer.sharedMesh.subMeshCount)
                .Sum(index => (int)renderer.sharedMesh.GetIndexCount(index) / 3);
            return new
            {
                Screen.width, Screen.height, api = SystemInfo.graphicsDeviceType,
                athleteRenderers = athleteRenderers.Length, athleteTriangles = athleteRenderers.Sum(Triangles),
                officialRenderers = officialRenderers.Length, officialTriangles = officialRenderers.Sum(Triangles),
                materials = materials.Length, textures = textures.Length, textureMiB = textureBytes / (1024f * 1024f),
                cameraSamples, strokes
            };
        }
        finally
        {
            player.Motor.StopMotion();
            stamina.RequestSprint(false);
            input.enabled = true;
            One<ArenaDeveloperCamera>().SetView(-1);
            match.GameplayView.SetView(prior);
            foreach (var brain in brains) brain.enabled = true;
        }
    }

    public static async Task<string> Run()
    {
        Check(Application.isPlaying, "Play the production arena before running the fixture.");
        Directory.CreateDirectory(Output);
        var match = One<PrototypeMatch>();
        var ball = One<BallController>();
        var flags = new UnityEditor.SerializedObject(ball);
        Check(flags.FindProperty("automaticPickup").boolValue && flags.FindProperty("swimDribbling").boolValue,
            "Enable the competition pickup and dribbling switches for the fixture.");
        var roster = One<PrototypeRoster>();
        var selection = One<PlayerSelection>();
        var visual = ball.transform.Find("Visual");
        var mesh = visual.GetComponent<MeshFilter>().sharedMesh;
        Check(mesh.GetIndexCount(0) == 3014u * 3 && Mathf.Abs(ball.Radius - .14f) < .0001f &&
            Mathf.Abs(ball.GetComponent<Rigidbody>().mass - .45f) < .0001f, "Original physical ball and 3,014-triangle shell.");
        var intro = One<PreMatchPresentation>();
        if (intro.Active) intro.SkipAll();
        await Await(() => match.IsPlaying && !match.AwaitingOpeningPossession, "Protected sprint-off completed.", 18f);

        var brains = Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>()
            .Concat(Object.FindObjectsByType<PrototypeGoalkeeper>()).ToArray();
        var previousBrains = brains.Select(b => b.enabled).ToArray();
        var preferences = One<GamePreferences>();
        var values = (GamePreferences.Values)typeof(GamePreferences).GetField("values",
            BindingFlags.NonPublic | BindingFlags.Instance).GetValue(preferences);
        bool previousReplay = values.automaticGoalReplays;
        values.automaticGoalReplays = true;
        SwimmerInput input = null;
        try
        {
            foreach (var brain in brains) brain.enabled = false;
            int blue = 0, white = 0;
            foreach (var player in roster.Members)
                player.Motor.ResetPose(new Vector3(player.Team == PrototypeTeam.Blue ? -7.5f : 7.5f, 0,
                    -9f + 3 * (player.Team == PrototypeTeam.Blue ? blue++ : white++)), Quaternion.identity);
            var human = roster.FieldPlayers.First(p => p.Team == PrototypeTeam.Blue);
            var rival = roster.FieldPlayers.First(p => p.Team == PrototypeTeam.White);
            var humanView = human.GetComponentInChildren<RiggedSwimmerVisual>();
            var receiver = roster.FieldPlayers.First(p => p.Team == human.Team && p != human);
            selection.Select(human);
            input = human.GetComponent<SwimmerInput>();
            input.enabled = false;
            Vector2 forward = Vector2.up;
            Vector3 direction = Vector3.forward;
            human.Motor.ResetPose(-direction * 3f, Quaternion.LookRotation(direction));
            ball.ResetForRestart(human.Motor.Position + direction * 1.8f);
            await Capture("Branded-Ball", ball.PhysicsPosition, new Vector3(.36f, .31f, -.49f));
            human.Motor.SetMoveInput(forward);
            float closest = float.PositiveInfinity;
            try
            {
                await Await(() =>
                {
                    closest = Mathf.Min(closest, Planar(ball.PhysicsPosition-human.Motor.Position).magnitude);
                    return ball.Owner == human.Interaction;
                }, "Human collected automatically with movement alone.", 3f);
            }
            catch (TimeoutException)
            {
                var state = new UnityEditor.SerializedObject(ball);
                throw new InvalidOperationException($"Pickup probe: closest {closest:F3}, swimmer {human.Motor.Position} v {human.Motor.Velocity}, " +
                    $"ball {ball.PhysicsPosition} v {ball.Velocity}, {ball.State}, can collect {ball.CanCollect}, " +
                    $"owner {ball.Owner}, phase {match.Phase}, automatic {state.FindProperty("automaticPickup").boolValue}, " +
                    $"motor enabled {human.Motor.enabled}, selected {selection.ActivePlayer == human}.");
            }
            human.Motor.SetMoveInput(Vector2.zero);
            await Await(() => !ball.IsDribbling && human.Motor.Velocity.sqrMagnitude < .01f &&
                Vector3.Distance(ball.transform.position, human.Interaction.HeldBallPoint) < .075f, "Stationary one-handed hold.");
            await Capture("One-Hand-Hold", humanView.HeadPosition - Vector3.up * .08f, direction * 2f + Vector3.right * .9f + Vector3.up * .65f);

            human.Motor.SetMoveInput(forward);
            await Await(() => ball.IsDribbling && Vector3.Dot(ball.transform.position-humanView.HeadPosition,direction) > .16f &&
                !humanView.Animator.GetBool("Hold"), "Crawl ball ahead of head with free arms.");
            await Task.Delay(140);
            float lead = Vector3.Dot(ball.transform.position-humanView.HeadPosition,direction);
            await Capture("Crawl-Dribble", humanView.HeadPosition, Vector3.Cross(Vector3.up,direction) * 2.7f + direction * 1.4f + Vector3.up * .9f);
            var stamina = human.GetComponent<SwimmerStamina>();
            stamina.RequestSprint(true);
            await Await(() => stamina.IsSprinting && ball.IsDribbling && humanView.Locomotion > 1.3f &&
                Mathf.Abs(ball.transform.position.y-ball.SurfaceHeight)<.04f, "Sprint dribbled at the waterline.");
            await Capture("Sprint-Dribble", humanView.HeadPosition, Vector3.Cross(Vector3.up,direction) * 2.8f + direction * 1.4f + Vector3.up);
            stamina.RequestSprint(false);
            human.Motor.SetMoveInput(Vector2.right);
            await Await(() => ball.IsDribbling && human.Motor.Velocity.x > 2.6f &&
                Vector3.Dot(ball.transform.position-humanView.HeadPosition,Vector3.right) > .13f, "Turned with the ball ahead.");
            human.Motor.SetMoveInput(Vector2.zero);
            await Await(() => !ball.IsDribbling && ball.Owner == human.Interaction &&
                Vector3.Distance(ball.transform.position, human.Interaction.HeldBallPoint) < .075f, "Stopping recovered the ball to the hand.");

            human.Motor.SetMoveInput(forward);
            await Await(() => ball.IsDribbling && Time.time - ball.OwnershipStartedAt > 1.04f, "Dribbling after acquisition protection.");
            Vector3 approach = human.Motor.Facing;
            rival.Motor.ResetPose(human.Motor.Position + approach * 1.52f, Quaternion.LookRotation(-approach));
            await Task.Delay(25);
            Check(rival.GetComponent<SwimmerSteal>().TrySteal() == StealResult.Success &&
                ball.Owner == rival.Interaction && !ball.IsDribbling, "A legal challenge takes ownership and clears the old dribble.");
            human.Motor.StopMotion();

            rival.Motor.ResetPose(Vector3.right * .65f, Quaternion.LookRotation(Vector3.left));
            human.Motor.ResetPose(Vector3.left * .88f, Quaternion.LookRotation(Vector3.right));
            int acquisitions = 0;
            void Acquired(SwimmerBallInteraction a, SwimmerBallInteraction b) { if (b != null) acquisitions++; }
            ball.PossessionChanged += Acquired;
            try
            {
                ball.ResetForRestart(Vector3.zero);
                await Await(() => ball.Owner == rival.Interaction, "The nearest of two contestants acquired the ball.");
                await Task.Delay(100);
                Check(acquisitions == 1 && !human.Interaction.HasBall, "Exactly one automatic owner.");
            }
            finally { ball.PossessionChanged -= Acquired; }
            human.Motor.ResetPose(Vector3.left * 6f, Quaternion.identity);
            rival.Motor.ResetPose(Vector3.right * 2.5f, Quaternion.LookRotation(Vector3.left));
            ball.ResetForRestart(Vector3.zero);
            var ai = rival.GetComponent<PrototypeSwimmerAI>();
            ai.enabled = true;
            await Await(() => ball.Owner == rival.Interaction && rival.Motor.Position.x < 1.34f, "AI pursued and acquired the loose ball.");
            ai.enabled = false;
            rival.Motor.ResetPose(Vector3.right * 7.5f + Vector3.forward * 9f, Quaternion.identity);

            // The same movement command consumed by the keyboard/AI drives two isolated releases.
            try
            {
                Vector3 point = -direction * 4f;
                human.Motor.ResetPose(point, Quaternion.LookRotation(direction));
                receiver.Motor.ResetPose(point + direction * 5f, Quaternion.LookRotation(-direction));
                ball.ResetForRestart(point + direction * .48f);
                await Await(() => ball.Owner == human.Interaction, "Automatic setup for the pass.");
                human.Motor.SetMoveInput(forward);
                await Await(() => ball.IsDribbling, "Pass started in dribbling state.");
                int releases = humanView.ReleaseAnimations;
                Check(human.GetComponent<SwimmerPassing>().TryPassTo(receiver), "Authoritative pass released directly from dribbling.");
                human.Motor.StopMotion();
                await Capture("Pass-From-Dribble", humanView.HeadPosition + direction * 1.1f,
                    Vector3.right * 3.1f + Vector3.up * .85f + direction * .5f);
                await Await(() => ball.Owner == receiver.Interaction, "Receiver caught the physical pass.");
                Check(humanView.ReleaseAnimations == releases + 1, "Exactly one pass animation event.");

                var goal = human.GetComponent<PrototypeSwimmerAI>().TacticalTeam.AttackingGoal;
                Vector3 facing = goal.transform.forward;
                point = goal.transform.position - facing * 6.8f;
                human.Motor.ResetPose(point, Quaternion.LookRotation(facing));
                ball.ResetForRestart(point + facing * .48f);
                await Await(() => ball.Owner == human.Interaction, "Automatic setup for the shot.");
                input.enabled = false; // The possession-driven selection change enables this component.
                human.Motor.SetMoveInput(new Vector2(facing.x, facing.z));
                await Await(() => ball.IsDribbling, "Shot started in dribbling state.");
                int score = match.BlueScore;
                Check(human.Interaction.TryShoot(22f, ShotType.Normal), "Authoritative shot released directly from dribbling.");
                human.Motor.StopMotion();
                await Await(() => match.BlueScore == score + 1, "Physical Blue goal registered.");
                var replay = match.GoalPresentation;
                await Await(() => replay.Phase == GoalPresentationPhase.Broadcast, "Recorded goal replay reached broadcast.");
                Check(Object.FindObjectsByType<MeshFilter>().Count(f => f.sharedMesh == mesh && f.gameObject.activeInHierarchy) >= 2,
                    "Recorded replay uses the branded ball mesh.");
                await Await(() => replay.Phase == GoalPresentationPhase.BehindGoal, "Recorded goal-line replay.");
                await Task.Delay(170);
                ScreenCapture.CaptureScreenshot(Path.GetFullPath(Output + "/Goal-Line-Replay.png"));
                await Task.Delay(45);
                replay.SkipAll();
                await Await(() => match.IsPlaying && !replay.Active, "White's real centre restart returned to live play.", 12f);
                return $"PASS. Human pickup, AI pursuit/pickup, exclusive contest, one-hand recovery, crawl lead {lead:F3} m, sprint, turn, legal steal, pass/catch, shot/goal, both replay angles and centre restart.";
            }
            finally { human.Motor.SetMoveInput(Vector2.zero); }
        }
        finally
        {
            foreach (var (brain, enabled) in brains.Zip(previousBrains, (brain, enabled) => (brain, enabled))) brain.enabled = enabled;
            values.automaticGoalReplays = previousReplay;
            if (input != null) input.enabled = true;
            One<ArenaDeveloperCamera>().SetView(-1);
        }
    }
}
