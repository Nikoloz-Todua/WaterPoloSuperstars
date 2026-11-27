using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Hub;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

public static class Milestone30FVerification
{
    private const string Output = "Logs/Milestone30F";

    public static async Task<string> Support()
    {
        var ball = Object.FindAnyObjectByType<BallController>();
        var roster = Object.FindAnyObjectByType<PrototypeRoster>();
        var selection = Object.FindAnyObjectByType<PlayerSelection>();
        RequireLab(ball);
        await Until(() => ball.Owner != null && selection.ActivePlayer != null &&
            ball.Owner == selection.ActivePlayer.Interaction &&
            ball.Owner.GetComponent<SwimmerMotor>().Position.x > 1.5f, 20f, "Intelligent Team Support staging");

        var carrier = selection.ActivePlayer;
        var teammates = roster.FieldPlayers.Where(p => p.Team == carrier.Team && p != carrier).ToArray();
        var ai = teammates.Select(p => p.GetComponent<PrototypeSwimmerAI>()).ToArray();
        var marker = teammates.First(p => p.FieldPosition == FieldPositionRole.LeftFlat).GetComponent<PrototypeSwimmerAI>();
        var tactics = ai[0].TacticalTeam;
        float beforeGap = float.PositiveInfinity;
        float afterGap = float.PositiveInfinity;
        int throws = 0;
        void ObserveRelease(SwimmerBallInteraction who, BallReleaseKind kind)
        {
            if (who.GetComponent<SwimmerIdentity>().Team == selection.HumanTeam && kind != BallReleaseKind.Drop) throws++;
        }
        var background = InputSystem.settings.backgroundBehavior;
        var editor = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        ball.Released += ObserveRelease;
        try
        {
            await Awaitable.WaitForSecondsAsync(.18f);
            Vector3 origin = carrier.Motor.Position;
            Vector3 marked = marker.GetComponent<SwimmerMotor>().Position;
            Vector3 firstTarget = marker.Target;
            bool initialLane = tactics.LaneClear(origin, marked);
            bool targetLane = tactics.LaneClear(origin, firstTarget);
            var targets = ai.Select(a => a.Target).ToArray();
            var recording = UnityEngine.Profiling.Recorder.Get("WaterPolo.TeamSupport");
            recording.enabled = true;
            await Awaitable.WaitForSecondsAsync(.95f);
            int watching = 0;
            foreach (var p in teammates)
            {
                var toBall = Vector3.ProjectOnPlane(carrier.Motor.Position - p.Motor.Position, Vector3.up).normalized;
                if (p.Motor.Velocity.magnitude < .5f && Vector3.Dot(p.Motor.Facing, toBall) > .75f) watching++;
                foreach (var other in teammates)
                    if (p != other) beforeGap = Mathf.Min(beforeGap, Vector3.Distance(p.Motor.Position, other.Motor.Position));
            }
            await Capture("Support-Before");
            Vector3 route = -tactics.AttackDirection;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(TravelKey(ball.Match.GameplayView, route)));
            int navSamples = 0;
            long navTicks = 0;
            await Until(() =>
            {
                int blocks = recording.sampleBlockCount;
                if (blocks > 0) { navSamples += blocks; navTicks += recording.elapsedNanoseconds; }
                return Vector3.Distance(origin, carrier.Motor.Position) > 2.8f;
            }, 3f, "Carrier follows manual input");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            await Awaitable.WaitForSecondsAsync(.45f);
            int changed = ai.Where((a, index) => Vector3.Distance(a.Target, targets[index]) > .65f).Count();
            foreach (var p in teammates)
                foreach (var other in teammates)
                    if (p != other) afterGap = Mathf.Min(afterGap, Vector3.Distance(p.Motor.Position, other.Motor.Position));
            await Capture("Support-After");
            bool retained = ball.Owner == carrier.Interaction && selection.ActivePlayer == carrier;
            string report = $"carrier moved {Vector3.Distance(origin, carrier.Motor.Position):F2} m; " +
                $"stationary observers {watching}; marked start lane open {initialLane}, chosen lane open {targetLane}, " +
                $"marked target offset {Vector3.Distance(marked, firstTarget):F2} m; changed targets {changed}/{ai.Length}; " +
                $"minimum teammate gap {beforeGap:F2}/{afterGap:F2} m; human retained {retained}; human throws {throws}; " +
                $"support sample count {navSamples}, mean {(navSamples > 0 ? navTicks / (1000000d * navSamples) : 0):F3} ms";
            File.WriteAllText($"{Output}/Support.txt", report);
            recording.enabled = false;
            if (changed < 2 || watching == 0 || !retained || throws != 0) throw new InvalidOperationException(report);
            return report;
        }
        finally
        {
            ball.Released -= ObserveRelease;
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = background;
            InputSystem.settings.editorInputBehaviorInPlayMode = editor;
        }
    }

    public static Task<string> Receiving() => ReceivingCases();

    public static Task<string> Interception() => ReceivingCases("Defender intercepts");

    private static async Task<string> ReceivingCases(string only = null)
    {
        var ball = Object.FindAnyObjectByType<BallController>();
        var roster = Object.FindAnyObjectByType<PrototypeRoster>();
        var selection = Object.FindAnyObjectByType<PlayerSelection>();
        var camera = Object.FindAnyObjectByType<ArenaDeveloperCamera>();
        RequireLab(ball);
        await Until(() => ball.Owner != null && ball.Match.IsPlaying && !ball.Match.AwaitingOpeningPossession &&
            selection.ActivePlayer != null && ball.Owner == selection.ActivePlayer.Interaction &&
            selection.ActivePlayer.Motor.Position.sqrMagnitude < .10f &&
            roster.FieldPlayers.Count(p => p.Team == selection.HumanTeam &&
                (p.Motor.Position - selection.ActivePlayer.Motor.Position).sqrMagnitude is > 63f and < 72f) > 0,
            20f, "Long Pass to Receiver Facing Away staging");

        var passer = selection.ActivePlayer;
        var receiver = roster.FieldPlayers.First(p => p.Team == passer.Team && p != passer &&
            (p.Motor.Position - passer.Motor.Position).sqrMagnitude is > 63f and < 72f);
        var defender = roster.FieldPlayers.First(p => p.Team != passer.Team);
        var visual = receiver.GetComponentInChildren<RiggedSwimmerVisual>();
        Vector3 forward = passer.Motor.Facing;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 origin = passer.Motor.Position - forward * 4f;
        var report = new List<string>();
        var failures = new List<string>();
        var background = InputSystem.settings.backgroundBehavior;
        var editor = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var recording = UnityEngine.Profiling.Recorder.Get("WaterPolo.PassContact");
        recording.enabled = true;
        int contactSamples = 0;
        long contactNanoseconds = 0;
        try
        {
            await Case("Facing away sequence", async () =>
            {
                var anchor = camera.viewpoints[2];
                var focus = receiver.Motor.Position + Vector3.up * .30f;
                anchor.position = focus + right * 1.85f - forward * 1.42f + Vector3.up * .72f;
                anchor.LookAt(focus);
                camera.GetComponent<Camera>().fieldOfView = 53f;
                camera.SetView(2);
                await Awaitable.WaitForSecondsAsync(.22f);
                await Capture("Receive-01-Away");
                float started = Time.time;
                float initial = Vector3.Angle(receiver.Motor.Facing, -forward);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.B));
                await Until(() => ball.HasPendingThrow, .45f, "Keyboard B queues pass");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                float queued = Time.time;
                await Until(() => Vector3.Angle(receiver.Motor.Facing, -forward) < 120f, .7f, "Body starts turn");
                await Capture("Receive-02-Turn");
                await Until(() => visual.ReceivingHandWeight > .42f, .7f, "Hand is visibly prepared");
                float ready = Time.time;
                await Capture("Receive-03-Prepared");
                await Until(() => ball.Owner == null && ball.IntendedReceiver == receiver.Interaction, .7f, "Locked receiver and physical flight");
                if (selection.ActivePlayer != receiver) throw new InvalidOperationException("Automatic control did not transfer at release.");
                await Until(() =>
                {
                    contactSamples += recording.sampleBlockCount;
                    contactNanoseconds += recording.elapsedNanoseconds;
                    return ball.Owner == receiver.Interaction;
                }, 1.6f, "Facing-away catch");
                float caught = Time.time;
                float receivingAngle = Vector3.Angle(receiver.Motor.Facing, -forward);
                await Awaitable.EndOfFrameAsync();
                float gap = Mathf.Abs(Vector3.Distance(visual.EvaluatedPalm, ball.transform.position) - ball.Radius);
                float skinGap = PalmMeshGap(visual, ball);
                SaveFrame("Receive-04-Contact");
                await Awaitable.WaitForSecondsAsync(.19f);
                await Capture("Receive-05-Recovery");
                if (gap > .045f || skinGap > .065f) throw new InvalidOperationException($"Visible palm gap {gap:F4}, skin gap {skinGap:F4} m.");
                report.Add($"8.2 m, initial angle {initial:F1}°, catch angle {receivingAngle:F1}°, queue delay {queued - started:F3} s, " +
                    $"hand ready {caught - ready:F3} s before caught, first rendered landmark/skin gaps {gap:F4}/{skinGap:F4} m, ball radius {ball.Radius:F3} m.");
                if (contactSamples > 0) report.Add($"Pass contact work {contactNanoseconds / (1000000d * contactSamples):F3} ms/sample, {contactSamples} samples.");
                camera.SetView(-1);
            });

            await Case("Short dry catch and immediate intentional shot", async () =>
            {
                Stage(4.5f, -forward);
                await Pass();
                await Until(() => ball.Owner == receiver.Interaction, 1.6f, "Short catch");
                float caughtAt = Time.time;
                if (!receiver.Interaction.TryShoot(12f)) throw new InvalidOperationException("Immediate intentional shot was rejected.");
                await Until(() => ball.ReleaseKind == BallReleaseKind.Shot && ball.LastReleaser == receiver.Interaction,
                    .9f, "Queued shot physically released");
                report.Add($"Immediate shot retained HumanInput, released {Time.time - caughtAt:F3} s after catch.");
                if (ball.LastReleaseSource != BallActionSource.HumanInput) throw new InvalidOperationException("Shot source changed.");
            });

            foreach (Vector3 direction in new[] { forward, right })
            {
                await Case(direction == forward ? "Swimming away" : "Swimming laterally", async () =>
                {
                    Stage(7.7f, direction);
                    receiver.Motor.SetMoveInput(new Vector2(direction.x, direction.z));
                    await Awaitable.WaitForSecondsAsync(.27f);
                    float startingSpeed = receiver.Motor.Velocity.magnitude;
                    if (startingSpeed < 1f) throw new InvalidOperationException("Fixture started below swimming speed.");
                    await Pass();
                    await Until(() => ball.Owner == receiver.Interaction, 1.6f, "Moving receiver catch");
                    report.Add($"Started at {startingSpeed:F2} m/s.");
                    if (direction == right) await Capture("Receive-Moving");
                });
            }

            await Case("Lob reception", async () =>
            {
                Stage(7.7f, -forward);
                if (!passer.GetComponent<LobPassing>().TryPass()) throw new InvalidOperationException("Lob rejected.");
                await Until(() => ball.IntendedReceiver == receiver.Interaction && ball.IsAirborne, 1f, "Lob released");
                await Until(() => ball.Owner == receiver.Interaction, 2f, "Lob received");
                report.Add($"Lob contact {ball.MotionOrigin.y - ball.SurfaceHeight:F2} m above surface-rest centre.");
            });
            await Case("Manual selection protection", async () =>
            {
                Stage(7.5f, forward);
                await Pass();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.C));
                await Until(() => selection.ActivePlayer != receiver, .7f, "Manual C selection");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                var manual = selection.ActivePlayer;
                await Until(() => ball.Owner == receiver.Interaction, 1.6f, "Original receiver catches after switching");
                if (selection.ActivePlayer != manual) throw new InvalidOperationException("Catch overrode manual selection.");
                await Awaitable.WaitForSecondsAsync(.20f);
                if (ball.Owner != receiver.Interaction) throw new InvalidOperationException("Human teammate threw without a command.");
            });
            await Case("Defender intercepts", async () =>
            {
                Stage(7.8f, -forward);
                defender.Motor.ResetPose(origin + forward * 6.25f + right * .32f, Quaternion.LookRotation(-forward));
                await Pass();
                var defenderVisual = defender.GetComponentInChildren<RiggedSwimmerVisual>();
                var shoulder = defenderVisual.Animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                var elbow = defenderVisual.Animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
                var wrist = defenderVisual.RightHand;
                var reachable = typeof(RiggedSwimmerVisual).GetMethod("CanReachDryCatch",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                float closest = float.PositiveInfinity;
                string trace = null;
                while (ball.IsAirborne && ball.Owner == null && Time.time < ball.ReleaseStartedAt + 1.2f)
                {
                    Vector3 sphere = ball.PhysicsPosition;
                    Vector3 hand = ball.RaisedHand(defender.Interaction, sphere);
                    float along = Vector3.Dot(sphere - hand, forward);
                    if (Mathf.Abs(along) < closest)
                    {
                        closest = Mathf.Abs(along);
                        trace = $"Intercept approach x-offset {Vector3.Dot(sphere - hand, right):F3} m, ball/shoulder y " +
                            $"{sphere.y:F3}/{shoulder.position.y:F3} m, ready weight {defenderVisual.ReceivingHandWeight:F2}, " +
                            $"head clearance {Vector3.Distance(sphere, defenderVisual.HeadPosition):F3} m, " +
                            $"arm {Vector3.Distance(shoulder.position, elbow.position) + Vector3.Distance(elbow.position, wrist.position):F3} m, " +
                            $"rig reach at sample {reachable.Invoke(defenderVisual, new object[] { sphere, ball.Velocity })}.";
                    }
                    await Awaitable.NextFrameAsync();
                }
                if (trace != null) report.Add(trace);
                await Until(() => ball.Owner == defender.Interaction, 1.6f, "Defender intercepts actual flight");
            });
            await Case("Manual movement produces a miss and physical pickup", async () =>
            {
                Stage(8.2f, -forward);
                await Pass();
                Vector3 launch = ball.PhysicsPosition;
                Vector3 initialVelocity = Vector3.ProjectOnPlane(ball.Velocity, Vector3.up).normalized;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(TravelKey(ball.Match.GameplayView, right)));
                await Awaitable.WaitForSecondsAsync(.26f);
                float change = Vector3.Angle(initialVelocity, Vector3.ProjectOnPlane(ball.Velocity, Vector3.up));
                await Until(() => !ball.IsAirborne && ball.Owner == null, 1.7f, "Unreachable pass reaches water");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                if (change > 2f || Vector3.Distance(launch, ball.PhysicsPosition) < 5f)
                    throw new InvalidOperationException("Flight did not follow the released lane.");
                Vector3 landing = ball.PhysicsPosition;
                receiver.Motor.ResetPose(landing - forward * .65f, Quaternion.LookRotation(forward));
                await Until(() => ball.Owner == receiver.Interaction, 1.7f, "Automatic surface pickup");
                report.Add($"Horizontal bearing change {change:F2}° before the miss.");
            });

            string result = string.Join(Environment.NewLine, report);
            Directory.CreateDirectory(Output);
            File.WriteAllText($"{Output}/{(only == null ? "Receiving" : "Interception")}.txt", result);
            if (failures.Count > 0) throw new InvalidOperationException(result);
            return result;
        }
        finally
        {
            recording.enabled = false;
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = background;
            InputSystem.settings.editorInputBehaviorInPlayMode = editor;
        }

        async Task Case(string name, Func<Task> run)
        {
            if (only != null && name != only) return;
            try { await run(); report.Add("PASS " + name); }
            catch (Exception ex) { string failure = $"FAIL {name}: {ex.Message}"; failures.Add(failure); report.Add(failure); }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        }

        void Stage(float distance, Vector3 facing)
        {
            foreach (var p in roster.Members)
            {
                Vector3 park = right * (p.Team == passer.Team ? -7.5f : 7.5f) + forward * ((p.CapNumber - 5f) * 1.45f);
                p.Motor.ResetPose(park, Quaternion.LookRotation(forward));
            }
            selection.Select(passer);
            passer.Motor.ResetPose(origin, Quaternion.LookRotation(forward));
            receiver.Motor.ResetPose(origin + forward * distance, Quaternion.LookRotation(facing));
            ball.ResetForRestart(origin + forward * .85f);
            if (!passer.Interaction.TryCollect()) throw new InvalidOperationException("Could not stage possession.");
        }

        async Task Pass()
        {
            if (!passer.GetComponent<SwimmerPassing>().TryPassTo(receiver)) throw new InvalidOperationException("Intentional pass rejected.");
            await Until(() => ball.IsAirborne && ball.IntendedReceiver == receiver.Interaction, 1.4f, "Committed pass released");
        }
    }

    private static Key TravelKey(GameplayCamera view, Vector3 route)
    {
        Vector2 screenUp = view.ScreenToWorldMovement(Vector2.up);
        Vector2 screenRight = view.ScreenToWorldMovement(Vector2.right);
        Vector2 world = new Vector2(route.x, route.z);
        float vertical = Vector2.Dot(screenUp, world);
        float lateral = Vector2.Dot(screenRight, world);
        return Mathf.Abs(vertical) > Mathf.Abs(lateral) ? vertical > 0f ? Key.W : Key.S : lateral > 0f ? Key.D : Key.A;
    }

    private static float PalmMeshGap(RiggedSwimmerVisual visual, BallController ball)
    {
        float gap = float.PositiveInfinity;
        var mesh = new Mesh();
        try
        {
            foreach (var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                int hand = Array.IndexOf(skin.bones, visual.RightHand);
                if (hand < 0) continue;
                var weights = skin.sharedMesh.boneWeights;
                skin.BakeMesh(mesh);
                var vertices = mesh.vertices;
                for (int i = 0; i < vertices.Length; i++)
                {
                    var weight = weights[i];
                    if (!(weight.boneIndex0 == hand && weight.weight0 >= .3f ||
                        weight.boneIndex1 == hand && weight.weight1 >= .3f)) continue;
                    Vector3 world = skin.transform.TransformPoint(vertices[i]);
                    if ((world - visual.EvaluatedPalm).sqrMagnitude > .007f) continue;
                    gap = Mathf.Min(gap, Mathf.Abs(Vector3.Distance(world, ball.transform.position) - ball.Radius));
                }
            }
            return gap;
        }
        finally { Object.Destroy(mesh); }
    }

    private static void RequireLab(BallController ball)
    {
        if (!Application.isPlaying || HubMatchBridge.Active || ball == null)
            throw new InvalidOperationException("Launch the isolated Test Lab in the production arena.");
    }

    private static async Task Until(Func<bool> condition, float seconds, string name)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (!condition())
        {
            if (!Application.isPlaying || Time.realtimeSinceStartup >= deadline) throw new TimeoutException(name);
            await Awaitable.NextFrameAsync();
        }
    }

    private static async Task Capture(string name)
    {
        await Awaitable.EndOfFrameAsync();
        SaveFrame(name);
    }

    private static void SaveFrame(string name)
    {
        Directory.CreateDirectory(Output);
        var frame = ScreenCapture.CaptureScreenshotAsTexture();
        try { File.WriteAllBytes($"{Output}/{name}.jpg", frame.EncodeToJPG(90)); }
        finally { Object.Destroy(frame); }
    }
}
