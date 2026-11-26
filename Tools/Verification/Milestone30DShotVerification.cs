using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Hub;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

public static class Milestone30DShotVerification
{
    public static async Task<string> Run()
    {
        var ball = Object.FindAnyObjectByType<BallController>();
        var selection = Object.FindAnyObjectByType<PlayerSelection>();
        var camera = Object.FindAnyObjectByType<ArenaDeveloperCamera>();
        if (!Application.isPlaying || HubMatchBridge.Active) throw new InvalidOperationException("Use the isolated shooting lab.");
        float deadline = Time.realtimeSinceStartup + 18f;
        while (camera.ViewIndex != 2 || ball.Owner == null || !ball.Match.IsPlaying)
        {
            if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("Shooting lab staging.");
            await Awaitable.NextFrameAsync();
        }
        var player = selection.ActivePlayer;
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        var visual = player.GetComponentInChildren<RiggedSwimmerVisual>();
        var anim = visual.Animator;
        var pelvis = anim.GetBoneTransform(HumanBodyBones.Hips);
        var shoulder = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
        var garment = player.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r => r.name == "CompetitionBriefs");
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var background = InputSystem.settings.backgroundBehavior;
        var editor = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var charge = player.GetComponent<ShotCharge>();
        var initialPosition = player.Motor.Position;
        var initialFacing = player.Motor.Facing;
        float releaseTime = 0f, originError = 0f, releaseHeight = 0f, waist = 0f;
        int releaseCount = 0;
        void Released(SwimmerBallInteraction actor, BallReleaseKind kind)
        {
            if (actor != player.Interaction || kind != BallReleaseKind.Shot) return;
            releaseCount++;
            releaseTime = Time.time;
            originError = Vector3.Distance(ball.ShotOrigin, visual.EvaluatedBallGrip);
            releaseHeight = ball.ShotOrigin.y - ball.SurfaceHeight;
            var mesh = new Mesh();
            garment.BakeMesh(mesh);
            waist = mesh.vertices.Max(vertex => garment.transform.TransformPoint(vertex).y) - ball.SurfaceHeight;
            Object.Destroy(mesh);
        }
        ball.Released += Released;
        try
        {
            async Task<(float rise, string summary)> Shot(string name, float hold)
            {
                Vector3 aim = Quaternion.AngleAxis(-19f, Vector3.up) * initialFacing;
                selection.Select(player);
                player.Motor.ResetPose(initialPosition, Quaternion.LookRotation(aim));
                ball.ResetForRestart(initialPosition + aim * .84f);
                if (!player.Interaction.TryCollect()) throw new InvalidOperationException("Shot fixture failed to collect.");
                var anchor = camera.viewpoints[2];
                Vector3 focus = initialPosition + Vector3.up * .39f;
                anchor.position = focus + Vector3.Cross(Vector3.up, aim) * 2.75f + aim * .65f + Vector3.up * .48f;
                anchor.LookAt(focus);
                camera.GetComponent<Camera>().fieldOfView = 47f;
                camera.SetView(2);
                await Awaitable.WaitForSecondsAsync(.24f);
                await Capture(name + "-01-Low");
                float baseline = pelvis.position.y;
                float baseShoulder = shoulder.position.y;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                float chargeDeadline = Time.realtimeSinceStartup + .85f;
                while (!charge.IsCharging && Time.realtimeSinceStartup < chargeDeadline)
                    await Awaitable.NextFrameAsync();
                await Awaitable.WaitForSecondsAsync(hold);
                if (!charge.IsCharging) throw new InvalidOperationException("Space did not start the real charge.");
                await Capture(name + "-02-Preparation");
                float command = Time.time;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                float end = command + .61f;
                float peak = pelvis.position.y, peakShoulder = shoulder.position.y;
                int before = releaseCount, shotFrame = 0;
                while (Time.time < end)
                {
                    await Awaitable.EndOfFrameAsync();
                    peak = Mathf.Max(peak, pelvis.position.y);
                    peakShoulder = Mathf.Max(peakShoulder, shoulder.position.y);
                    if (releaseCount == before && ball.HasPendingThrow && shotFrame < 2 &&
                        Time.time - command >= (shotFrame == 0 ? .065f : .132f))
                        Save(name + "-" + (++shotFrame + 2).ToString("D2") + "-Rise");
                    if (releaseCount > before && shotFrame < 3)
                    {
                        shotFrame = 3;
                        Save(name + "-05-Release");
                    }
                    await Awaitable.NextFrameAsync();
                }
                await Capture(name + "-06-Recovery");
                if (releaseCount != before + 1 || originError > .045f)
                    throw new InvalidOperationException($"Expected one synchronized shot, {releaseCount-before} releases, origin gap {originError}.");
                return (peak - baseline, $"{name}: peak pelvis rise {peak - baseline:F3} m, pelvis relative to water {peak - ball.SurfaceHeight:F3} m, shoulder rise {peakShoulder - baseShoulder:F3} m, release {1000f*(releaseTime-command):F0} ms, origin gap {originError:F4} m, ball height {releaseHeight:F3} m, top garment at release {waist:F3} m");
            }
            var quick = await Shot("Quick", .065f);
            var power = await Shot("Power", .78f);
            if (quick.rise < .12f || power.rise < .42f || power.rise - quick.rise < .22f)
                throw new InvalidOperationException(quick.summary + " / " + power.summary);
            return quick.summary + " / " + power.summary;
        }
        finally
        {
            ball.Released -= Released;
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = background;
            InputSystem.settings.editorInputBehaviorInPlayMode = editor;
        }
    }

    private static async Task Capture(string name)
    {
        await Awaitable.EndOfFrameAsync();
        Save(name);
    }

    private static void Save(string name)
    {
        const string output = "Logs/Milestone30D/Shooting";
        Directory.CreateDirectory(output);
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        try { File.WriteAllBytes($"{output}/{name}.png", texture.EncodeToPNG()); }
        finally { Object.Destroy(texture); }
    }
}
