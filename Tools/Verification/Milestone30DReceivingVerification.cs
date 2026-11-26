using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Hub;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

public static class Milestone30DReceivingVerification
{
    public static async Task<object> Run()
    {
        var ball = Object.FindAnyObjectByType<BallController>();
        var roster = Object.FindAnyObjectByType<PrototypeRoster>();
        var selection = Object.FindAnyObjectByType<PlayerSelection>();
        var camera = Object.FindAnyObjectByType<ArenaDeveloperCamera>();
        if (!Application.isPlaying || HubMatchBridge.Active) throw new InvalidOperationException("Use the isolated long-pass lab.");
        await Until(() => ball.Owner != null && ball.Owner == selection.ActivePlayer.Interaction &&
            ball.Match.IsPlaying && !ball.Match.AwaitingOpeningPossession &&
            roster.FieldPlayers.Any(p => p.Team == selection.HumanTeam && p != selection.ActivePlayer &&
                (p.Motor.Position - selection.ActivePlayer.Motor.Position).sqrMagnitude > 63f &&
                (p.Motor.Position - selection.ActivePlayer.Motor.Position).sqrMagnitude < 72f), 18f, "Test Lab staging");
        var passer = selection.ActivePlayer;
        var target = roster.FieldPlayers.Where(p => p.Team == passer.Team && p != passer)
            .OrderBy(p => Mathf.Abs(Vector3.Distance(p.Motor.Position, passer.Motor.Position) - 8.2f)).First();
        Vector3 incomingFacing = (passer.Motor.Position - target.Motor.Position).normalized;
        var visual = target.GetComponentInChildren<RiggedSwimmerVisual>();
        float initial = Vector3.Dot(target.Motor.Facing, incomingFacing);
        if (initial > -.95f) throw new InvalidOperationException("Fixture receiver should face away.");
        var anchor = camera.viewpoints[2];
        var lens = camera.GetComponent<Camera>();
        camera.SetView(2);
        Vector3 focus = target.Motor.Position + Vector3.up * .27f;
        anchor.position = focus + Vector3.Cross(Vector3.up, incomingFacing) * 2.0f + incomingFacing * 1.1f + Vector3.up * .58f;
        anchor.LookAt(focus);
        lens.fieldOfView = 51f;
        await Capture("Receiver-Facing-Away");
        if (!passer.GetComponent<SwimmerPassing>().TryPass()) throw new InvalidOperationException("Pass rejected.");
        await Until(() => ball.Owner == null && ball.IntendedReceiver == target.Interaction, 2f, "Physical pass toward the selected receiver");
        bool transferred = selection.ActivePlayer == target;
        float duringFlight = Vector3.Dot(target.Motor.Facing, incomingFacing);
        await Until(() => Vector3.Dot(target.Motor.Facing, incomingFacing) > .05f, 1.5f, "Turn before the ball arrives");
        await Capture("Receiver-Turning");
        await Until(() => ball.Owner == target.Interaction, 1.5f, "Physical catch by the receiver");
        float catchDot = Vector3.Dot(target.Motor.Facing, incomingFacing);
        await Awaitable.EndOfFrameAsync();
        float separation = Mathf.Abs(Vector3.Distance(visual.EvaluatedPalm, ball.PhysicsPosition) - ball.Radius);
        float meshGap = PalmVertexGap(visual, ball);
        Save("Receiver-First-Contact");
        await Awaitable.WaitForSecondsAsync(.20f);
        await Capture("Receiver-Control");
        return $"pass 8.20 m; dot initially {initial:F3}, release {duringFlight:F3}, catch {catchDot:F3}; selection in flight {transferred}; palm landmark error {separation:F4} m; nearest weighted palm vertex to collider sphere {meshGap:F4} m";
    }

    private static float PalmVertexGap(RiggedSwimmerVisual visual, BallController ball)
    {
        var hand = visual.Animator.GetBoneTransform(HumanBodyBones.RightHand);
        float gap = float.PositiveInfinity;
        var baked = new Mesh();
        try
        {
            foreach (var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                int joint = Array.IndexOf(skin.bones, hand);
                if (joint < 0) continue;
                var weights = skin.sharedMesh.boneWeights;
                skin.BakeMesh(baked);
                var vertices = baked.vertices;
                for (int i = 0; i < vertices.Length; i++)
                {
                    var weight = weights[i];
                    bool palm = weight.boneIndex0 == joint && weight.weight0 >= .3f ||
                        weight.boneIndex1 == joint && weight.weight1 >= .3f;
                    if (!palm) continue;
                    Vector3 world = skin.transform.TransformPoint(vertices[i]);
                    if ((world - visual.EvaluatedPalm).sqrMagnitude > .007f) continue;
                    gap = Mathf.Min(gap, Mathf.Abs(Vector3.Distance(world, ball.PhysicsPosition) - ball.Radius));
                }
            }
            return gap;
        }
        finally { Object.Destroy(baked); }
    }

    public static async Task<string> Variants()
    {
        var ball = Object.FindAnyObjectByType<BallController>();
        var roster = Object.FindAnyObjectByType<PrototypeRoster>();
        var selection = Object.FindAnyObjectByType<PlayerSelection>();
        if (!Application.isPlaying || HubMatchBridge.Active) throw new InvalidOperationException("Use the isolated long-pass lab.");
        await Until(() => ball.Owner != null && ball.Match.IsPlaying && !ball.Match.AwaitingOpeningPossession, 18f, "Opening possession");
        // Run after the lab has staged the field and parked live AI.
        await Awaitable.WaitForSecondsAsync(.35f);
        var passer = roster.FieldPlayers.First(p => p.Team == selection.HumanTeam);
        var receiver = roster.FieldPlayers.First(p => p.Team == passer.Team && p != passer);
        var defender = roster.FieldPlayers.First(p => p.Team != passer.Team);
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var background = InputSystem.settings.backgroundBehavior;
        var editor = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        Vector3 origin = Vector3.back * 4f;
        Vector3 forward = Vector3.forward, right = Vector3.right;
        void Stage(float distance, Vector3 facing)
        {
            foreach (var athlete in roster.Members)
                athlete.Motor.ResetPose(new Vector3(athlete.Team == passer.Team ? -7.5f : 7.5f, 0f, (athlete.CapNumber - 5f) * 1.45f), Quaternion.identity);
            selection.Select(passer);
            passer.Motor.ResetPose(origin, Quaternion.identity);
            receiver.Motor.ResetPose(origin + forward * distance, Quaternion.LookRotation(facing));
            ball.ResetForRestart(origin + forward * .85f);
            if (!passer.Interaction.TryCollect()) throw new InvalidOperationException("Controlled claim.");
        }
        async Task Pass()
        {
            if (!passer.GetComponent<SwimmerPassing>().TryPassTo(receiver)) throw new InvalidOperationException("Directed pass command.");
            await Until(() => ball.Owner == null && ball.IntendedReceiver == receiver.Interaction, 2f, "Directed flight began");
        }
        try
        {
            Stage(4.5f, Vector3.back);
            await Pass();
            await Until(() => ball.Owner == receiver.Interaction, 1.6f, "Short catch");
            foreach (var movement in new[] { Vector2.up, Vector2.right })
            {
                Stage(7.7f, new Vector3(movement.x, 0f, movement.y));
                receiver.Motor.SetMoveInput(movement);
                await Awaitable.WaitForSecondsAsync(.27f);
                if (receiver.Motor.Velocity.magnitude < 1f) throw new InvalidOperationException("Fixture receiver was moving before the pass.");
                await Pass();
                await Until(() => ball.Owner == receiver.Interaction, 1.6f,
                    movement == Vector2.up ? "Swimming-away receiver turns and catches" : "Sideways receiver turns and catches");
            }
            Stage(7.5f, Vector3.forward);
            await Pass();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.C));
            await Until(() => selection.ActivePlayer != receiver, .7f, "Manual switch");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            var manual = selection.ActivePlayer;
            await Until(() => ball.Owner == receiver.Interaction, 1.6f, "Protected manual selection and catch");
            if (selection.ActivePlayer != manual) throw new InvalidOperationException("Catch overrode manual selection.");

            Stage(7.8f, Vector3.back);
            defender.Motor.ResetPose(origin + forward * 6.25f + right * .32f, Quaternion.LookRotation(Vector3.back));
            await Pass();
            await Until(() => ball.Owner == defender.Interaction, 1.6f, "Defender interception");
            Stage(7.8f, Vector3.back);
            await Pass();
            receiver.Motor.ResetPose(origin + forward * 7.8f + right * 3f, Quaternion.LookRotation(Vector3.right));
            // A deliberate route away overrides the contextual receive adjustment.
            receiver.Motor.SetMoveInput(Vector2.right, true);
            receiver.GetComponent<SwimmerInput>().enabled = false;
            await Until(() => !ball.IsAirborne && ball.Owner == null, 1.6f, "Unreachable pass lands loose");
            return "short catch, receiver swimming away, side-moving receiver, manual protection, a physical interception and an unreachable miss all passed";
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = background;
            InputSystem.settings.editorInputBehaviorInPlayMode = editor;
            selection.RefreshEligibility();
        }
    }

    private static async Task Until(Func<bool> condition, float seconds, string name)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (!condition())
        {
            if (Time.realtimeSinceStartup > deadline) throw new TimeoutException(name);
            await Awaitable.NextFrameAsync();
        }
    }

    private static async Task Capture(string name)
    {
        await Awaitable.EndOfFrameAsync();
        Save(name);
    }

    private static void Save(string name)
    {
        const string output = "Logs/Milestone30D/Receiving";
        Directory.CreateDirectory(output);
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        try { File.WriteAllBytes($"{output}/{name}.png", texture.EncodeToPNG()); }
        finally { Object.Destroy(texture); }
    }
}
