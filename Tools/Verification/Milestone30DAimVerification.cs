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

public static class Milestone30DAimVerification
{
    public static async Task<object> Run()
    {
        var ball = Object.FindAnyObjectByType<BallController>();
        var selection = Object.FindAnyObjectByType<PlayerSelection>();
        var roster = Object.FindAnyObjectByType<PrototypeRoster>();
        var inspection = Object.FindAnyObjectByType<ArenaDeveloperCamera>();
        if (!Application.isPlaying || HubMatchBridge.Active) throw new InvalidOperationException("Use the isolated possession lab.");
        await Until(() => inspection.ViewIndex == 2 && ball.Owner == selection.ActivePlayer.Interaction, 18f, "Possession lab setup");
        var player = selection.ActivePlayer;
        var visual = player.GetComponentInChildren<RiggedSwimmerVisual>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = InputSystem.AddDevice<Mouse>();
        var background = InputSystem.settings.backgroundBehavior;
        var editor = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var camera = ball.Match.GameplayView;
        inspection.SetView(-1);
        float largestGripError = 0f, worstAngle = 0f, worstTravel = 0f;
        void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        Vector3 World(Vector2 screen)
        {
            Vector2 xy = camera.ScreenToWorldMovement(screen.normalized);
            return new Vector3(xy.x, 0, xy.y).normalized;
        }
        void Stage(Vector3 aim)
        {
            foreach (var other in roster.Members.Where(p => p != player))
                other.Motor.ResetPose(-aim * 7f + Vector3.Cross(Vector3.up, aim) * (other.CapNumber - 5f) * 1.35f,
                    Quaternion.LookRotation(aim));
            selection.Select(player);
            player.Motor.ResetPose(Vector3.zero, Quaternion.LookRotation(aim));
            ball.ResetForRestart(aim * .84f);
            if (!player.Interaction.TryCollect()) throw new InvalidOperationException("Stage possession.");
        }
        var directions = new[] { (Vector2.right, new[] { Key.RightArrow }), (Vector2.up, new[] { Key.UpArrow }),
            (Vector2.left, new[] { Key.LeftArrow }), (new Vector2(-1, -1), new[] { Key.LeftArrow, Key.DownArrow }),
            (Vector2.down, new[] { Key.DownArrow }) };
        try
        {
            foreach (MatchCameraView mode in new[] { MatchCameraView.Lengthwise, MatchCameraView.SidelineBroadcast })
            {
                camera.SetView(mode);
                camera.SnapToTarget();
                await Awaitable.NextFrameAsync();
                Stage(World(Vector2.up));
                await Until(() => !ball.IsGathering, 2f, "Grip gathered");
                var mark = player.Motor.Position;
                foreach (var direction in directions)
                {
                    Keys(new[] { Key.LeftAlt }.Concat(direction.Item2).ToArray());
                    await Awaitable.WaitForSecondsAsync(.22f);
                    await Awaitable.EndOfFrameAsync();
                    var arrow = GameObject.Find("Possession Aim Arrow")?.GetComponent<LineRenderer>();
                    if (arrow == null || !arrow.enabled || ball.Owner != player.Interaction || ball.IsDribbling)
                        throw new InvalidOperationException("Held rotation and visible aim");
                    worstAngle = Mathf.Max(worstAngle, Vector3.Angle((arrow.GetPosition(1) - arrow.GetPosition(0)), World(direction.Item1)));
                    largestGripError = Mathf.Max(largestGripError, Mathf.Abs(Vector3.Distance(visual.EvaluatedPalm, ball.PhysicsPosition) - ball.Radius));
                    worstTravel = Mathf.Max(worstTravel, Vector3.Distance(mark, player.Motor.Position));
                }
                Keys();
                await Awaitable.NextFrameAsync();
                await Capture("Aim-" + mode);
            }
            Stage(World(Vector2.up));
            var desired = World(Vector2.left);
            foreach (var other in roster.Members.Where(p => p != player))
                other.Motor.ResetPose(-desired * 8f + Vector3.Cross(Vector3.up, desired) * (other.CapNumber - 5f) * .7f, Quaternion.identity);
            await Until(() => !ball.IsGathering, 2f, "Pointer grip");
            var button = Object.FindObjectsByType<GameplayActionButton>().Single(b => b.Action == MobileMatchAction.Pass);
            var rect = button.GetComponent<RectTransform>();
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            void Pointer(Vector2 pos, bool down) => InputSystem.QueueStateEvent(mouse, new MouseState { position = pos, buttons = (ushort)(down ? 1 : 0) });
            Pointer(point, false);
            await Awaitable.WaitForSecondsAsync(.10f);
            Pointer(point, true);
            await Awaitable.WaitForSecondsAsync(.09f);
            Pointer(point + Vector2.left * 94f, true);
            await Awaitable.WaitForSecondsAsync(.18f);
            float pointerAim = Vector3.Angle(player.Motor.ControlAim, desired);
            Pointer(point + Vector2.left * 94f, false);
            await Until(() => ball.Owner == null && ball.ReleaseKind == BallReleaseKind.Pass, 2f, "Real UI drag released the pass");
            float passAngle = Vector3.Angle(Vector3.ProjectOnPlane(ball.Velocity, Vector3.up), desired);
            if (ball.Delivery != PassDelivery.Dry || ball.IntendedReceiver != null || passAngle > 2f || pointerAim > 2f ||
                worstAngle > 2f || worstTravel > .03f || largestGripError > .015f)
                throw new InvalidOperationException($"Aim errors {worstAngle:F3} keyboard, {pointerAim:F3} drag, {passAngle:F3} release. Grip {largestGripError:F4}, travel {worstTravel:F3}.");
            return $"both cameras, arrow error {worstAngle:F3} deg, pointer aim {pointerAim:F3} deg, open-water pass {passAngle:F3} deg, held error {largestGripError:F4} m, travel {worstTravel:F3} m";
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
            InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior = background;
            InputSystem.settings.editorInputBehaviorInPlayMode = editor;
        }
    }

    private static async Task Until(Func<bool> condition, float seconds, string label)
    {
        float end = Time.realtimeSinceStartup + seconds;
        while (!condition())
        {
            if (Time.realtimeSinceStartup > end) throw new TimeoutException(label);
            await Awaitable.NextFrameAsync();
        }
    }

    private static async Task Capture(string name)
    {
        await Awaitable.EndOfFrameAsync();
        Directory.CreateDirectory("Logs/Milestone30D/Aim");
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        try { File.WriteAllBytes($"Logs/Milestone30D/Aim/{name}.png", texture.EncodeToPNG()); }
        finally { Object.Destroy(texture); }
    }
}
