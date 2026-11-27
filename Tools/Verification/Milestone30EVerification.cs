using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Hub;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

public static class Milestone30EVerification
{
    private static readonly (Vector2 screen, Key[] keys)[] Directions =
    {
        (Vector2.up, new[] { Key.UpArrow }), (Vector2.left, new[] { Key.LeftArrow }),
        (Vector2.down, new[] { Key.DownArrow }), (Vector2.right, new[] { Key.RightArrow }),
        (new Vector2(1f, 1f), new[] { Key.RightArrow, Key.UpArrow })
    };

    public static Task<string> Carry() => Run(true);
    public static Task<string> Passing() => Run(false);

    private static async Task<string> Run(bool carry)
    {
        var ball = Object.FindAnyObjectByType<BallController>();
        var roster = Object.FindAnyObjectByType<PrototypeRoster>();
        var selection = Object.FindAnyObjectByType<PlayerSelection>();
        var inspection = Object.FindAnyObjectByType<ArenaDeveloperCamera>();
        if (!Application.isPlaying || HubMatchBridge.Active) throw new InvalidOperationException("Launch the isolated Test Lab first.");
        await Until(() => ball.Match.IsPlaying && !ball.Match.AwaitingOpeningPossession &&
            ball.Owner == selection.ActivePlayer.Interaction, 24f, "Live staged possession");

        UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        await Awaitable.NextFrameAsync();
        var background = InputSystem.settings.backgroundBehavior;
        var editor = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var mouse = carry ? null : InputSystem.AddDevice<Mouse>();
        var teamField = typeof(PlayerSelection).GetField("humanTeam", BindingFlags.Instance | BindingFlags.NonPublic);
        var originalTeam = selection.HumanTeam;
        var output = new List<string>();
        var view = ball.Match.GameplayView;
        var player = selection.ActivePlayer;
        Vector3 World(Vector2 input)
        {
            Vector2 planar = view.ScreenToWorldMovement(input.normalized);
            return new Vector3(planar.x, 0f, planar.y).normalized;
        }
        void Keys(params Key[] keys)
        {
            if (!keyboard.enabled) InputSystem.EnableDevice(keyboard);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        }
        async Task Stage(Vector3 heading)
        {
            Keys();
            await Until(() => !keyboard.anyKey.isPressed, .6f, "Previous synthetic input cleared");
            await Awaitable.NextFrameAsync();
            foreach (var athlete in roster.Members)
            {
                if (athlete.TryGetComponent<PrototypeSwimmerAI>(out var ai)) ai.enabled = false;
                if (athlete.TryGetComponent<PrototypeGoalkeeper>(out var keeper)) keeper.enabled = false;
                if (athlete != player)
                    athlete.Motor.ResetPose(new Vector3(athlete.Team == PrototypeTeam.Blue ? -8f : 8f,
                        0f, (athlete.CapNumber - 4) * 1.65f), Quaternion.identity);
            }
            selection.Select(player);
            player.Motor.ResetPose(Vector3.zero, Quaternion.LookRotation(heading));
            player.GetComponent<SwimmerStamina>().ResetStamina();
            ball.ResetForRestart(heading * .84f);
            Check(player.Interaction.TryCollect(), "Stage possession");
            await Until(() => !ball.IsGathering, 2f, "Finish gathering");
            await Awaitable.NextFrameAsync();
        }
        float worstChevron = 0f;
        void CheckChevron()
        {
            var marker = GameObject.Find("Possession Facing Chevron")?.GetComponent<LineRenderer>();
            Check(marker != null && marker.enabled && marker.positionCount == 3, "Shaftless chevron");
            var wings = (marker.GetPosition(0) + marker.GetPosition(2)) * .5f;
            worstChevron = Mathf.Max(worstChevron, Vector3.Angle(marker.transform.TransformDirection(marker.GetPosition(1) - wings), player.transform.forward));
            Check(worstChevron < .3f, $"Chevron error {worstChevron:F3} degrees");
        }
        async Task Aim(Vector2 screen, Key[] keys)
        {
            Keys(new[] { Key.LeftAlt }.Concat(keys).ToArray());
            await Until(() => Vector3.Angle(player.transform.forward, World(screen)) < 4f, 1.2f, "Settle body yaw");
            await Awaitable.EndOfFrameAsync();
            CheckChevron();
        }

        try
        {
            if (carry)
            {
                Check(inspection.ViewIndex == 2, "Use Directional Aim and Held Rotation");
                var motor = player.Motor;
                var visual = player.Interaction.GetComponentInChildren<RiggedSwimmerVisual>();
                Vector3 up = World(Vector2.up);
                float worstGrip = 0f, nearestHead = float.MaxValue;
                int releases = 0;
                void Count(SwimmerBallInteraction source, BallReleaseKind kind) { if (source == player.Interaction) releases++; }
                ball.Released += Count;
                bool Raised() => ball.PhysicsPosition.y > ball.SurfaceHeight + .16f &&
                    Vector3.Dot(ball.PhysicsPosition - visual.HeadPosition, player.transform.forward) < -.13f;
                void HeldSample()
                {
                    Check(ball.Owner == player.Interaction && !ball.IsDribbling,
                        $"Protected ownership. Owner={ball.Owner}, leading={ball.IsDribbling}, last={ball.ReleaseKind}, " +
                        $"retreat={motor.IsRetreating}, facing={motor.Facing}, aim={motor.ControlAim}, velocity={motor.Velocity}");
                    worstGrip = Mathf.Max(worstGrip, Vector3.Distance(ball.PhysicsPosition, visual.EvaluatedBallGrip));
                    nearestHead = Mathf.Min(nearestHead, Vector3.Distance(ball.PhysicsPosition, visual.HeadPosition));
                    CheckChevron();
                }
                async Task Sample(float seconds, Action sample)
                {
                    float end = Time.time + seconds;
                    do { await Awaitable.EndOfFrameAsync(); sample(); } while (Time.time < end);
                }
                try
                {
                    await Stage(up);
                    HeldSample();
                    Check(Mathf.Abs(ball.PhysicsPosition.y - ball.SurfaceHeight) < .05f &&
                        visual.EvaluatedPalmNormal.y < -.80f, "Stationary palm over floating ball");
                    await Capture("01-Stationary");
                    foreach (var choice in new[] { Directions[0], Directions[3] })
                    {
                        await Stage(up);
                        Vector3 start = motor.Position;
                        Keys(choice.keys);
                        await Until(() => keyboard[choice.keys[0]].isPressed, .65f,
                            $"Short movement key delivered ({choice.screen}, deviceEnabled={keyboard.enabled})");
                        await Sample(.28f, () =>
                        {
                            HeldSample();
                            if (Vector3.Distance(start, motor.Position) > .035f)
                                Check(ball.PhysicsPosition.y > ball.SurfaceHeight + .07f, "Translation after lift");
                        });
                        Check(Raised() && motor.Velocity.magnitude > .35f && motor.Velocity.magnitude <= motor.MaxSpeed * .59f,
                            $"Brief movement {choice.screen} rear={Raised()}, speed={motor.Velocity.magnitude:F2}, " +
                            $"lift={ball.PhysicsPosition.y - ball.SurfaceHeight:F2}, angle={Vector3.Angle(motor.Facing, World(choice.screen)):F1}");
                        output.Add($"Short {choice.screen} speed={motor.Velocity.magnitude:F2} m/s, lift={ball.PhysicsPosition.y - ball.SurfaceHeight:F2} m");
                        await Capture(choice.screen.x == 0f ? "02-Short-forward" : "03-Short-lateral");
                        Keys();
                        await Until(() => motor.Velocity.sqrMagnitude < .01f &&
                            Mathf.Abs(ball.PhysicsPosition.y - ball.SurfaceHeight) < .05f, 1.3f, "Return to stationary");
                    }

                    await Stage(up);
                    Vector3 mark = motor.Position;
                    foreach (int i in new[] { 1, 2, 3, 0, 2, 0, 3 })
                    {
                        var choice = Directions[i];
                        var turn = Aim(choice.screen, choice.keys);
                        while (!turn.IsCompleted)
                        {
                            await Awaitable.EndOfFrameAsync();
                            HeldSample();
                        }
                        await turn;
                        await Sample(.06f, HeldSample);
                        Check(Raised(), $"Rear load while facing {choice.screen}");
                        if (i == 1) await Capture("04-Turn-left");
                        if (i == 2) await Capture("05-Turn-back");
                        if (i == 3) await Capture("06-Turn-right");
                    }
                    Check(Vector3.Distance(mark, motor.Position) < .035f && releases == 0 && worstGrip < .005f,
                        $"No translation or release during turns. Maximum settled grip error {worstGrip:F4} m");
                    output.Add($"Left/right/180/full circle, grip error {worstGrip:F4} m, nearest head joint {nearestHead:F3} m");

                    await Stage(up);
                    Keys(Key.DownArrow);
                    await Until(() => keyboard.downArrowKey.isPressed, .65f, "Retreat key delivered");
                    await Sample(.34f, HeldSample);
                    Check(Raised() && motor.IsRetreating && Vector3.Dot(motor.Velocity, up) < -.8f &&
                        Vector3.Angle(motor.Facing, up) < 4f, "Protected retreat");
                    await Capture("07-Retreat");
                    Keys(Key.DownArrow, Key.Numpad4);
                    await Until(() => Vector3.Angle(player.transform.forward, World(Vector2.left)) < 4f,
                        1.2f, "Independent retreat aim");
                    Check(Raised() && motor.IsRetreating && Vector3.Dot(motor.Velocity, up) < -.8f,
                        "Maintain retreat while turning");
                    await Capture("08-Retreat-aim");
                    output.Add($"Retreat with lateral aim: {motor.Velocity.magnitude:F2} m/s, ball stays behind the turned head");
                    Keys(Key.LeftArrow);
                    await Until(() => !motor.IsRetreating && Vector3.Dot(motor.Velocity, World(Vector2.left)) > .7f,
                        .9f, "Changing travel input exits retreat after the turn");
                    Check(releases == 0, "No turn released the ball");

                    foreach (bool sprint in new[] { false, true })
                    {
                        await Stage(up);
                        float start = Time.time;
                        Keys(sprint ? new[] { Key.UpArrow, Key.LeftShift } : new[] { Key.UpArrow });
                        await Until(() => ball.IsDribbling, 1.2f, "Forward leading");
                        float entry = Time.time - start;
                        Check(ball.Owner == player.Interaction, "Dribble retains ownership");
                        await Sample(.52f, () => Check(ball.IsDribbling && ball.Owner == player.Interaction, "Sustained leading"));
                        Check(motor.Velocity.magnitude > (sprint ? 4.1f : 2.8f), "Freestyle propulsion");
                        output.Add($"{(sprint ? "Sprint" : "Normal")} dribble after {entry:F2} s, speed {motor.Velocity.magnitude:F2} m/s");
                        await Capture(sprint ? "10-Sprint" : "09-Dribble");
                        Keys();
                        await Until(() => !ball.IsDribbling && !ball.IsGathering && motor.Velocity.sqrMagnitude < .01f &&
                            Mathf.Abs(ball.PhysicsPosition.y - ball.SurfaceHeight) < .05f, 1.6f, "Gather and stop");
                    }

                    await Stage(up);
                    Keys(Key.DownArrow);
                    await Until(() => Raised() && motor.IsRetreating, 1.2f, "Raised before deliberate drop");
                    Keys(Key.DownArrow, Key.LeftShift);
                    await Until(() => ball.Owner == null && ball.ReleaseKind == BallReleaseKind.Drop, .6f, "Backward sprint drops");
                    Check(ball.LastReleaseSource == BallActionSource.VoluntaryRelease && releases == 1,
                        "One voluntary drop");
                    Keys();
                    await Until(() => !ball.IsAirborne, .70f, "Voluntary drop lands");
                    Vector3 drop = ball.PhysicsPosition;
                    motor.ResetPose(drop - up * .84f, Quaternion.LookRotation(up));
                    Check(!player.Interaction.TryCollect(), "Previous owner cannot reclaim the nearby settled drop");
                    output.Add("Backward sprint released once, immediate recapture rejected");
                }
                finally { ball.Released -= Count; }

                var charge = player.GetComponent<ShotCharge>();
                var shoulder = visual.Animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                foreach (bool full in new[] { false, true })
                {
                    await Stage(up);
                    float baseHeight = shoulder.position.y;
                    Keys(Key.Space);
                    await Until(() => charge.IsCharging, .7f, "Real Space charge starts");
                    await Awaitable.WaitForSecondsAsync(full ? .77f : .045f);
                    int shots = 0;
                    float originError = float.PositiveInfinity, rise = 0f;
                    void Shot(SwimmerBallInteraction source, BallReleaseKind kind)
                    {
                        if (source != player.Interaction || kind != BallReleaseKind.Shot) return;
                        shots++;
                        rise = shoulder.position.y - baseHeight;
                        originError = Vector3.Distance(ball.ShotOrigin, visual.EvaluatedBallGrip);
                    }
                    ball.Released += Shot;
                    try
                    {
                        Keys();
                        await Until(() => shots > 0, 1.2f, "Physical charged-shot release");
                        Check(shots == 1 && ball.LastReleaseSource == BallActionSource.HumanInput &&
                            originError < .025f && rise > (full ? .42f : .15f), "Shooting elevation and grip release");
                        output.Add($"{(full ? "Full" : "Quick")} shot release shoulder rise {rise:F3} m, origin {originError:F4} m");
                    }
                    finally { ball.Released -= Shot; }
                }
            }
            else
            {
                inspection.SetView(-1);
                foreach (var team in new[] { PrototypeTeam.Blue, PrototypeTeam.White })
                foreach (var mode in new[] { MatchCameraView.Lengthwise, MatchCameraView.SidelineBroadcast })
                {
                    teamField.SetValue(selection, team);
                    player = roster.FieldPlayers.First(candidate => candidate.Team == team);
                    selection.Select(player);
                    view.SetView(mode);
                    view.SnapToTarget();
                    await Awaitable.WaitForSecondsAsync(.5f);
                    Vector3 up = World(Vector2.up);
                    await Stage(up);
                    foreach (var choice in Directions)
                        await Aim(choice.screen, choice.keys);
                    Keys();

                    foreach (int i in new[] { 1, 3 })
                    {
                        await Stage(up);
                        bool pointer = team == PrototypeTeam.Blue && mode == MatchCameraView.SidelineBroadcast && i == 3;
                        bool capture = team == PrototypeTeam.Blue && (mode == MatchCameraView.Lengthwise || pointer);
                        var choice = Directions[i];
                        Vector3 right = World(choice.screen);
                        var outlets = roster.FieldPlayers.Where(candidate => candidate.Team == team && candidate != player).Take(2).ToArray();
                        outlets[0].Motor.ResetPose(right * 4.6f, Quaternion.LookRotation(-right));
                        outlets[1].Motor.ResetPose(-right * 4.4f, Quaternion.LookRotation(right));
                        var visual = player.GetComponentInChildren<RiggedSwimmerVisual>();
                        int releases = 0;
                        float originError = 0f, flightError = 0f, facingError = 0f, releasedAt = 0f;
                        void Release(SwimmerBallInteraction source, BallReleaseKind kind)
                        {
                            if (source != player.Interaction || kind != BallReleaseKind.Pass) return;
                            releases++;
                            facingError = Vector3.Angle(player.transform.forward, right);
                            originError = Vector3.Distance(ball.PhysicsPosition, visual.EvaluatedBallGrip);
                            flightError = Vector3.Angle(right, Vector3.ProjectOnPlane(ball.Velocity, Vector3.up));
                            releasedAt = Time.time;
                        }
                        ball.Released += Release;
                        try
                        {
                            float press;
                            if (pointer)
                            {
                                var button = Object.FindObjectsByType<GameplayActionButton>().Single(b => b.Action == MobileMatchAction.Pass);
                                var rect = button.GetComponent<RectTransform>();
                                Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
                                Vector2 drag = point + choice.screen * Mathf.Max(85f, Screen.height * .12f);
                                void Pointer(Vector2 pos, bool down) => InputSystem.QueueStateEvent(mouse,
                                    new MouseState { position = pos, buttons = (ushort)(down ? 1 : 0) });
                                Keys(Key.DownArrow);
                                await Awaitable.WaitForSecondsAsync(.23f);
                                Pointer(point, false);
                                await Awaitable.NextFrameAsync();
                                Pointer(point, true);
                                await Awaitable.WaitForSecondsAsync(.07f);
                                Pointer(drag, true);
                                await Until(() => Vector3.Angle(player.Motor.ControlAim, right) < 5f, .45f,
                                    "PASS drag accepts the pointer bearing");
                                await Until(() => Vector3.Angle(player.transform.forward, right) < 8f, .75f,
                                    "Visible body finishes the pointer turn");
                                Check(player.Motor.IsRetreating, "PASS drag turns during independent retreat");
                                await Capture("13-Pointer-aim");
                                press = Time.time;
                                Pointer(drag, false);
                            }
                            else
                            {
                                if (capture) await Capture($"11-Pass-before-{i}");
                                press = Time.time;
                                Keys(Key.LeftAlt, choice.keys[0], Key.B);
                                if (capture)
                                {
                                    await Until(() => ball.HasPendingThrow &&
                                        Vector3.Angle(player.transform.forward, right) < 22f, 1.1f, "Pass winds up facing the lane");
                                    await Awaitable.WaitForSecondsAsync(.05f);
                                    await Capture($"11-Pass-loaded-{i}");
                                }
                            }
                            await Until(() => releases > 0, 1.2f, "One explicit physical pass");
                            await Awaitable.EndOfFrameAsync();
                            Check(releases == 1 && ball.LastReleaseSource == BallActionSource.HumanInput &&
                                ball.Delivery == PassDelivery.Dry && ball.IntendedReceiver == outlets[0].Interaction &&
                                facingError < 10f && flightError < (pointer ? 32f : 13f) && originError < .025f,
                                $"{team}/{mode}/{i}: facing={facingError:F2}, flight={flightError:F2}, grip={originError:F4}, receiver={ball.IntendedReceiver}");
                            if (capture)
                            {
                                await Awaitable.WaitForSecondsAsync(.13f);
                                await Capture(pointer ? "14-Pointer-release" : $"12-Pass-release-{i}");
                            }
                            output.Add($"{team} {mode} {(i == 1 ? "left" : "right")} {(pointer ? "pointer" : "keyboard")} " +
                                $"release {releasedAt - press:F2} s, facing {facingError:F1} degrees, flight {flightError:F1} degrees, origin {originError:F4} m");
                        }
                        finally { ball.Released -= Release; Keys(); }
                    }
                }
                await Stage(World(Vector2.up));
                foreach (var athlete in roster.FieldPlayers.Where(candidate => candidate.Team == player.Team && candidate != player))
                    athlete.Motor.ResetPose(-World(Vector2.up) * 7f + World(Vector2.right) * (athlete.CapNumber - 4),
                        Quaternion.identity);
                Vector3 open = World(Vector2.up);
                Keys(Key.B);
                await Until(() => ball.Owner == null && ball.ReleaseKind == BallReleaseKind.Pass, 1.2f, "Open-water pass");
                float openError = Vector3.Angle(open, Vector3.ProjectOnPlane(ball.Velocity, Vector3.up));
                Check(ball.IntendedReceiver == null && openError < 2f, "No off-cone teammate selected");
                output.Add($"Open-water flight error {openError:F2} degrees, maximum chevron error {worstChevron:F3} degrees");
            }
            return string.Join("\n", output);
        }
        finally
        {
            Directory.CreateDirectory("Logs/Milestone30E");
            File.WriteAllLines($"Logs/Milestone30E/{(carry ? "Carry" : "Passing")}.txt", output);
            teamField.SetValue(selection, originalTeam);
            InputSystem.RemoveDevice(keyboard);
            if (mouse != null) InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior = background;
            InputSystem.settings.editorInputBehaviorInPlayMode = editor;
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static async Task Until(Func<bool> condition, float seconds, string message)
    {
        float end = Time.realtimeSinceStartup + seconds;
        while (!condition())
        {
            if (!Application.isPlaying || Time.realtimeSinceStartup > end) throw new TimeoutException(message);
            await Awaitable.NextFrameAsync();
        }
    }

    private static async Task Capture(string name)
    {
        await Awaitable.EndOfFrameAsync();
        Directory.CreateDirectory("Logs/Milestone30E");
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        try { File.WriteAllBytes($"Logs/Milestone30E/{name}.png", texture.EncodeToPNG()); }
        finally { Object.Destroy(texture); }
    }
}

