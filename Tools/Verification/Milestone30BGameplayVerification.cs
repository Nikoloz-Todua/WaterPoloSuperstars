using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

public static class Milestone30BGameplayVerification
{
    private const string Output = "docs/Evidence/Milestone30B";

    public static async Task<object> Run()
    {
        var match = Object.FindAnyObjectByType<PrototypeMatch>();
        var roster = Object.FindAnyObjectByType<PrototypeRoster>();
        var ball = Object.FindAnyObjectByType<BallController>();
        var selection = Object.FindAnyObjectByType<PlayerSelection>();
        var camera = Object.FindAnyObjectByType<ArenaDeveloperCamera>();
        var ceremony = Object.FindAnyObjectByType<PreMatchPresentation>();
        Require(match != null && roster != null && Application.isPlaying, "Play OlympicMatchArena.");
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        var background = InputSystem.settings.backgroundBehavior;
        var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        if (ceremony.Active) ceremony.SkipAll();
        await Until(() => match.IsPlaying && !match.AwaitingOpeningPossession, "opening sprint", 24f);
        Directory.CreateDirectory(Output);
        var source = roster.FieldPlayers.First(p => p.Team == selection.HumanTeam);
        var receiver = roster.FieldPlayers.First(p => p.Team == source.Team && p != source);
        var brains = Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>()
            .Concat(Object.FindObjectsByType<PrototypeGoalkeeper>()).Where(b => b.enabled).ToArray();
        foreach (var brain in brains) brain.enabled = false;
        foreach (var player in roster.Members.Where(p => p != source && p != receiver))
            player.Motor.ResetPose(new Vector3(player.Team == source.Team ? -8f : 8f, 0f, player.CapNumber - 6f),
                Quaternion.identity);
        selection.Select(source);
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var events = new List<BallImpactEvent>();
        ball.Contact += events.Add;
        Vector3 centre = Vector3.zero;
        Vector3 direction = Vector3.forward;
        var lens = camera.GetComponent<Camera>();
        var waypoint = camera.viewpoints[2];
        Vector3 initialEye = waypoint.position;
        Quaternion initialLook = waypoint.rotation;
        float initialFov = lens.fieldOfView;
        int drops = 0;
        void Released(SwimmerBallInteraction player, BallReleaseKind kind)
        {
            if (player == source.Interaction && kind == BallReleaseKind.Drop) drops++;
        }
        ball.Released += Released;
        void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        void Frame(Vector3 centreOfView, Vector3 offset, float fieldOfView = 52f)
        {
            camera.SetView(2);
            waypoint.position = centreOfView + offset;
            waypoint.LookAt(centreOfView);
            lens.fieldOfView = fieldOfView;
        }
        void Possess()
        {
            selection.Select(source);
            source.Motor.ResetPose(centre, Quaternion.LookRotation(direction));
            ball.ResetForRestart(centre + direction * .9f);
            Require(source.Interaction.TryCollect(), "staged live pickup");
        }

        try
        {
            Frame(centre + direction * .35f, Vector3.back * 3f + Vector3.up * 1.5f);
            await Awaitable.EndOfFrameAsync();
            Vector2 screen = match.GameplayView.ScreenToWorldMovement(Vector2.up);
            direction = new Vector3(screen.x, 0f, screen.y).normalized;
            receiver.Motor.ResetPose(centre + direction * 5.2f, Quaternion.LookRotation(-direction));
            Possess();
            await Until(() => !ball.IsGathering, "held-ball gather");
            Keys(Key.DownArrow);
            await Until(() => source.Motor.IsRetreating &&
                Vector3.Dot(source.Motor.Velocity, direction) < -.5f, () => $"camera-relative protected retreat " +
                    $"owner={ball.Owner == source.Interaction}, input={source.GetComponent<SwimmerInput>().enabled}, " +
                    $"key={keyboard.downArrowKey.isPressed}, facing={source.Motor.Facing}, forward={direction}, " +
                    $"velocity={source.Motor.Velocity}, movingBack={source.Motor.IsRetreating}, phase={match.Phase}");
            await Awaitable.WaitForSecondsAsync(.12f);
            float facingDot = Vector3.Dot(source.Motor.Facing, direction);
            Require(facingDot > .965f && ball.Owner == source.Interaction, "retreat kept facing and possession");
            await Screenshot("Control-Retreat-Held");

            Vector3 before = ball.PhysicsPosition;
            Keys(Key.DownArrow, Key.LeftShift);
            await Until(() => drops == 1 && ball.Owner == null, "keyboard sprint retreat releases one ball");
            float originGap = Vector3.Distance(ball.ShotOrigin, before);
            Require(originGap < .23f, "retreat release came from the held ball");
            await Screenshot("Control-Retreat-Release");
            await Until(() => !ball.IsAirborne, "retreat drop settles");
            bool noRecapture = ball.Owner != source.Interaction && !source.Interaction.TryCollect();
            Require(noRecapture && drops == 1, "retreat drop suppressed the previous owner's pickup");
            await Screenshot("Control-Retreat-Loose");
            Keys();

            // A dry release uses the normal receiver handoff before possession changes.
            Possess();
            receiver.Motor.ResetPose(centre + direction * 4.9f, Quaternion.LookRotation(-direction));
            var passer = source.GetComponent<SwimmerPassing>();
            Require(passer.TryPassTo(receiver), "directed pass accepted");
            await Until(() => ball.Owner == null && selection.ActivePlayer == receiver, "pass handoff before catch");
            await Screenshot("Control-Receiver-Selected");
            await Until(() => ball.Owner == receiver.Interaction, "receiver catches physical pass");
            await Screenshot("Control-Receiver-Catch");

            // Select through the existing action during a pass to protect that choice at the catch.
            Possess();
            receiver.Motor.ResetPose(centre + direction * 7.2f, Quaternion.LookRotation(-direction));
            Require(passer.TryPassTo(receiver), "manual override fixture pass");
            await Until(() => selection.ActivePlayer == receiver && ball.Owner == null, "second pass handoff");
            Keys(Key.C);
            await Until(() => selection.ActivePlayer != receiver, "manual C switch");
            Keys();
            var manual = selection.ActivePlayer;
            await Until(() => ball.Owner == receiver.Interaction, "receive after manual switch");
            Require(selection.ActivePlayer == manual, "catch respected the intervening manual selection");
            await Awaitable.WaitForSecondsAsync(1.13f);

            selection.Select(source);
            receiver.Motor.ResetPose(centre + direction * 5.8f, Quaternion.LookRotation(direction));
            ball.ResetForRestart(centre + direction * 7.7f);
            await Until(() => selection.ActivePlayer == receiver && ball.Owner == null, "reachable loose recovery selection");
            await Awaitable.WaitForSecondsAsync(.35f);
            Require(selection.ActivePlayer == receiver, "loose recovery selection stayed stable");

            // Disable control for the impact sample so no one closes the captured ball.
            Vector3 dropPoint = Vector3.left * 2.8f;
            ball.ResetForRestart(dropPoint);
            Frame(dropPoint, Vector3.back * 1.75f + Vector3.right * 1.25f + Vector3.up * .85f);
            events.Clear();
            var beginFlight = typeof(BallController).GetMethod("BeginFlight", BindingFlags.Instance | BindingFlags.NonPublic);
            beginFlight.Invoke(ball, new object[] { ball.PhysicsPosition + Vector3.up * 1.25f, Vector3.down * 2.7f });
            await Until(() => events.Any(e => e.Kind == BallImpactKind.Water), "ordinary impact");
            await Screenshot("Water-Drop-Impact");
            float peak = 0f;
            float settledBy = Time.time + .70f;
            while (Time.time < settledBy)
            {
                peak = Mathf.Max(peak, ball.PhysicsPosition.y - ball.SurfaceHeight);
                await Awaitable.NextFrameAsync();
            }
            Require(events.Count(e => e.Kind == BallImpactKind.Water) == 1 && ball.WaterBounces == 0 && peak < .04f,
                "the drop settled after one impact");
            await Screenshot("Water-Drop-Settled");

            var mouth = Object.FindObjectsByType<GoalMouth>().First(m => m.ScoringTeam == source.Team);
            direction = mouth.transform.forward;
            centre = mouth.transform.position - direction * 8.6f;
            centre.y = match.GameplayView.Target.Position.y;
            Possess();
            Frame(centre + direction * 2.5f, Vector3.Cross(Vector3.up, direction) * 3.1f + Vector3.up * 1.8f);
            events.Clear();
            Require(ball.TryShoot(source.Interaction, 21f, ShotType.Skip), "skip shot accepted");
            await Until(() => events.Any(e => e.Kind == BallImpactKind.Skip), "fast shallow skip");
            var skip = events.First(e => e.Kind == BallImpactKind.Skip);
            Require(skip.Outgoing.y > 0f && skip.Outgoing.magnitude < skip.Incoming.magnitude, "skip gained no energy");
            Frame(skip.Position + direction * 1.0f, Vector3.Cross(Vector3.up, direction) * 3.2f + Vector3.up * 1.22f);
            await Screenshot("Water-Skip-Impact");
            await Awaitable.WaitForSecondsAsync(.085f);
            await Screenshot("Water-Skip-Rise");
            return new
            {
                retreatFacingDot = facingDot, retreatDropOriginGap = originGap, drops, noRecapture,
                passedBeforeCatch = true, manualSelection = manual.PlayerId, looseRecovery = receiver.PlayerId,
                ordinaryImpacts = 1, peakAboveFloat = peak, skipIn = skip.Incoming.magnitude,
                skipOut = skip.Outgoing.magnitude, skipRise = skip.Outgoing.y,
            };
        }
        finally
        {
            ball.Contact -= events.Add;
            ball.Released -= Released;
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = background;
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
            foreach (var brain in brains) if (brain != null) brain.enabled = true;
            lens.fieldOfView = initialFov;
            waypoint.SetPositionAndRotation(initialEye, initialLook);
            camera.SetView(-1);
        }
    }

    public static Task<object> KeeperLeft() => DefensivePresentation(-.76f, true, false);
    public static Task<object> KeeperRight() => DefensivePresentation(.76f, true, false);
    public static Task<object> KeeperHigh() => DefensivePresentation(.02f, true, true);
    public static Task<object> FieldBlock() => DefensivePresentation(0f, false, false);

    public static async Task<object> FullMatch()
    {
        var match = Object.FindAnyObjectByType<PrototypeMatch>();
        var selection = Object.FindAnyObjectByType<PlayerSelection>();
        var ball = Object.FindAnyObjectByType<BallController>();
        Require(Application.isPlaying && match != null && selection != null, "Play the isolated production arena.");
        var tactic = Object.FindObjectsByType<PrototypeTeamTactics>().Single(t => t.Team == selection.HumanTeam);
        Directory.CreateDirectory(Output);
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        float start = Time.realtimeSinceStartup;
        float captureAt = 0f, actionAt = 0f;
        int latestQuarter = -1;
        var movements = Object.FindObjectsByType<SwimmerInput>(FindObjectsInactive.Include).ToDictionary(input => input, input => input.enabled);
        var horns = new List<object>();
        var phases = new List<MatchPhase>();
        MatchPhase lastPhase = (MatchPhase)(-1);
        GoalPresentationPhase previousReplay = GoalPresentationPhase.None;
        bool firstGoalCaptured = false;
        try
        {
            while (Application.isPlaying && match != null && match.Phase != MatchPhase.FullTime)
            {
                if (Time.realtimeSinceStartup - start > 492f) throw new TimeoutException(
                    $"Full-length phase {match.Phase}, Q{match.Clock.Quarter}, remaining {match.Clock.Remaining:F1}.");
                if (match.Phase != lastPhase)
                {
                    lastPhase = match.Phase;
                    phases.Add(lastPhase);
                    captureAt = Time.time;
                }
                if (match.GoalPresentationActive)
                {
                    var replay = match.GoalPresentation;
                    if (replay.Phase != previousReplay)
                    {
                        previousReplay = replay.Phase;
                        if (!firstGoalCaptured)
                            await Screenshot($"Full-Match-Replay-{replay.Phase}");
                        if (replay.Phase == GoalPresentationPhase.BehindGoal) firstGoalCaptured = true;
                        captureAt = Time.time;
                    }
                    if (Time.time - captureAt > 1.15f) replay.Advance();
                }
                else if (match.Phase == MatchPhase.QuarterBreak || match.Phase == MatchPhase.HalfTime)
                {
                    horns.Add(new { quarter = match.Clock.Quarter, match.BlueScore, match.WhiteScore });
                    await Screenshot($"Full-Match-Q{match.Clock.Quarter}-Horn");
                    match.ContinueMatch();
                }
                else if (match.IsCentreRestart && match.CentreTaker != null &&
                    match.CentreTaker.Team == selection.HumanTeam && match.Phase == MatchPhase.CentreRestartReady)
                    match.TryCentrePass(match.CentreTaker);
                else if (match.Phase == MatchPhase.PenaltyReady && match.Rules.Taker != null &&
                    match.Rules.Taker.Team == selection.HumanTeam)
                    match.Rules.Taker.Interaction.TryShoot(16f);
                else if (match.IsPlaying && !match.AwaitingOpeningPossession)
                {
                    if (latestQuarter != match.Clock.Quarter)
                    {
                        latestQuarter = match.Clock.Quarter;
                        await Screenshot($"Full-Match-Q{latestQuarter}-Live");
                    }
                    var controlled = selection.ActivePlayer;
                    if (controlled != null && controlled.EligibleForPlay)
                    {
                        controlled.GetComponent<SwimmerInput>().enabled = false;
                        var move = controlled.Motor;
                        var decision = WaterPoloDecision.Evaluate(controlled, tactic);
                        Vector3 aim = decision.Target - move.Position;
                        aim.y = 0f;
                        bool canThrow = controlled.Interaction.HasBall && !ball.HasPendingThrow && Time.time >= actionAt;
                        if (canThrow && (decision.Behaviour == SwimmerAIBehaviour.Shoot || decision.Behaviour == SwimmerAIBehaviour.Pass))
                        {
                            move.SetFacingDirection(aim);
                            move.SetMoveInput(Vector2.zero);
                            if (Vector3.Dot(move.Facing, aim.normalized) > .995f)
                            {
                                if (decision.Behaviour == SwimmerAIBehaviour.Shoot) controlled.Interaction.TryShoot(17f);
                                else controlled.GetComponent<SwimmerPassing>().TryPassTo(decision.Receiver);
                                actionAt = Time.time + .28f;
                            }
                        }
                        else
                        {
                            move.ClearFacingDirection();
                            Vector3 travel = Vector3.ClampMagnitude(aim / 1.8f, 1f);
                            move.SetMoveInput(new Vector2(travel.x, travel.z));
                        }
                    }
                }
                await Task.Delay(55);
            }
            Require(Application.isPlaying && match != null && match.Phase == MatchPhase.FullTime,
                "The full match reached FullTime.");
            await Screenshot("Full-Match-Final");
            horns.Add(new { quarter = match.Clock.Quarter, match.BlueScore, match.WhiteScore });
            return new { match.Clock.Quarters, match.Clock.RealDuration, realElapsed = Time.realtimeSinceStartup - start,
                match.BlueScore, match.WhiteScore, match.EndSwitchCount, match.SprintOff.Acquisitions,
                horns, phases = phases.Distinct().Select(phase => phase.ToString()).ToArray() };
        }
        finally
        {
            foreach (var input in movements.Keys)
                if (input != null) input.enabled = movements[input];
        }
    }

    private static async Task<object> DefensivePresentation(float lateral, bool keeperTest, bool high)
    {
        var match = Object.FindAnyObjectByType<PrototypeMatch>();
        var roster = Object.FindAnyObjectByType<PrototypeRoster>();
        var ball = Object.FindAnyObjectByType<BallController>();
        var selection = Object.FindAnyObjectByType<PlayerSelection>();
        var camera = Object.FindAnyObjectByType<ArenaDeveloperCamera>();
        var ceremony = Object.FindAnyObjectByType<PreMatchPresentation>();
        Require(Application.isPlaying && match != null && roster != null, "Play OlympicMatchArena.");
        if (ceremony.Active) ceremony.SkipAll();
        await Until(() => match.IsPlaying && !match.AwaitingOpeningPossession, "opening sprint", 24f);
        Directory.CreateDirectory(Output);
        var source = roster.FieldPlayers.First(p => p.Team == selection.HumanTeam &&
            p.FieldPosition == FieldPositionRole.CentreForward);
        var keeper = Object.FindObjectsByType<PrototypeGoalkeeper>().Single(p => p.Identity.Team != source.Team);
        var marker = roster.FieldPlayers.First(p => p.Team != source.Team && p.FieldPosition == FieldPositionRole.CentreBack);
        var goal = keeper.DefendedGoal;
        var actor = keeperTest ? keeper.Identity : marker;
        var visual = actor.GetComponentInChildren<RiggedSwimmerVisual>();
        var markerTactics = marker.GetComponent<PrototypeSwimmerAI>().TacticalTeam;
        var brains = Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>()
            .Concat(Object.FindObjectsByType<PrototypeGoalkeeper>()).Where(b => b.enabled && (!keeperTest || b != keeper)).ToArray();
        foreach (var brain in brains) brain.enabled = false;
        foreach (var player in roster.Members.Where(p => p != keeper.Identity))
            player.Motor.ResetPose(new Vector3(player.Team == source.Team ? -8f : 8f, 0f, player.CapNumber - 6f),
                Quaternion.identity);
        var forward = goal.transform.forward;
        var right = Vector3.Cross(Vector3.up, forward);
        var origin = goal.transform.position - forward * (keeperTest ? 6.8f : 7.5f);
        origin.y = 0f;
        var keeperPosition = goal.transform.position - forward * 1.4f;
        keeperPosition.y = origin.y;
        keeper.Identity.Motor.ResetPose(keeperPosition, Quaternion.LookRotation(-forward));
        var aimPoint = goal.transform.position + right * lateral;
        var aim = Vector3.ProjectOnPlane(aimPoint - origin, Vector3.up).normalized;
        source.Motor.ResetPose(origin, Quaternion.LookRotation(aim));
        marker.Motor.ResetPose(keeperTest ? origin + right * 7f : origin + forward * 1.58f + right * .16f,
            Quaternion.LookRotation(-forward));
        selection.Select(source);
        ball.ResetForRestart(origin + forward * .65f);
        Require(source.Interaction.TryCollect(), "shot fixture claimed the ball");
        await Until(() => !ball.IsGathering, "shot fixture initial grip");
        var lens = camera.GetComponent<Camera>();
        var view = camera.viewpoints[2];
        Vector3 priorEye = view.position;
        Quaternion priorLook = view.rotation;
        float originalFov = lens.fieldOfView;
        camera.SetView(2);
        lens.fieldOfView = keeperTest ? 51f : 59f;
        Vector3 focus = actor.Motor.Position + Vector3.up * .23f;
        view.position = focus + right * (keeperTest ? 1.9f : 2.6f) - forward * 2.45f + Vector3.up * .46f;
        view.LookAt(focus);
        await Awaitable.EndOfFrameAsync();
        Vector3? physicalContact = null;
        void Contact(BallImpactEvent impact)
        {
            if (impact.Kind == BallImpactKind.Save) physicalContact = impact.Position;
        }
        ball.Contact += Contact;
        string name = keeperTest ? high ? "Keeper-High" : lateral < 0f ? "Keeper-Left" : "Keeper-Right" : "Defense-Block";
        try
        {
            bool centerAssigned;
            if (!keeperTest)
            {
                bool press;
                markerTactics.SetDefense(DefenseStrategy.Press);
                press = markerTactics.MarkFor(marker) == source;
                markerTactics.SetDefense(DefenseStrategy.Zone);
                centerAssigned = press && markerTactics.MarkFor(marker) == source;
                Require(centerAssigned, "centre back retains opposing centre in Press and Zone");
            }
            else centerAssigned = false;
            var originalHead = visual.HeadPosition.y;
            var shoulder = visual.Animator.GetBoneTransform(HumanBodyBones.Chest);
            await Screenshot(name + "-Ready");
            Require(ball.TryShoot(source.Interaction, keeperTest ? 13.5f : 18f,
                high ? ShotType.High : ShotType.Normal), "shot commitment");
            float bestLift = 0f, bestReach = 0f, largestOther = 0f;
            float start = Time.time;
            int image = 0;
            while (Time.time - start < .67f)
            {
                await Awaitable.EndOfFrameAsync();
                bestLift = Mathf.Max(bestLift, visual.HeadPosition.y - originalHead);
                float left = visual.LeftHand.position.y - shoulder.position.y;
                float rightHeight = visual.RightHand.position.y - shoulder.position.y;
                if (Mathf.Max(left, rightHeight) > bestReach)
                {
                    bestReach = Mathf.Max(left, rightHeight);
                    largestOther = Mathf.Min(left, rightHeight);
                }
                if (image < 5 && Time.time >= start + .17f + image * .095f)
                    await Screenshot(name + $"-{image++:00}");
            }
            Require(bestLift > (keeperTest ? .08f : .037f), "directional pose produced torso rise");
            if (!keeperTest)
            {
                Require(bestReach > .22f && largestOther < .19f, "field block uses one raised arm");
            }
            return new { name, bestLift, bestReach, otherHeightAtPeak = largestOther, centerAssigned,
                attempts = keeper.SaveAttempts, keeper.Catches, keeper.Deflections, savedContact = physicalContact.HasValue,
                physicalContact = physicalContact?.ToString(), match.BlueScore, match.WhiteScore };
        }
        finally
        {
            ball.Contact -= Contact;
            lens.fieldOfView = originalFov;
            view.SetPositionAndRotation(priorEye, priorLook);
            camera.SetView(-1);
            foreach (var brain in brains) if (brain != null) brain.enabled = true;
        }
    }

    private static async Task Screenshot(string name)
    {
        await Awaitable.EndOfFrameAsync();
        var frame = ScreenCapture.CaptureScreenshotAsTexture();
        try { File.WriteAllBytes(Path.Combine(Output, name + ".png"), frame.EncodeToPNG()); }
        finally { Object.Destroy(frame); }
    }

    private static Task Until(Func<bool> predicate, string name, float seconds = 4f) => Until(predicate, () => name, seconds);

    private static async Task Until(Func<bool> predicate, Func<string> description, float seconds = 4f)
    {
        float limit = Time.realtimeSinceStartup + seconds;
        while (!predicate())
        {
            if (!Application.isPlaying || Time.realtimeSinceStartup > limit) throw new TimeoutException(description());
            await Awaitable.NextFrameAsync();
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
