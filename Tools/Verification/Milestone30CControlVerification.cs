using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Hub;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

public static class Milestone30CControlVerification
{
    public static async Task<object> Run()
    {
        var match = Object.FindAnyObjectByType<PrototypeMatch>();
        var ball = Object.FindAnyObjectByType<BallController>();
        var selection = Object.FindAnyObjectByType<PlayerSelection>();
        var roster = Object.FindAnyObjectByType<PrototypeRoster>();
        Check(Application.isPlaying && match != null && !HubMatchBridge.Active, "Direct isolated arena required.");
        var intro = Object.FindAnyObjectByType<PreMatchPresentation>();
        if (intro != null && intro.Active) intro.SkipAll();
        await Until(() => match.IsPlaying && !match.AwaitingOpeningPossession, "Opening sprint", 16f);
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        var background = InputSystem.settings.backgroundBehavior;
        var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var camera = match.GameplayView;
        var originalView = camera.View;
        var human = roster.FieldPlayers.First(p => p.Team == selection.HumanTeam);
        var teammate = roster.FieldPlayers.First(p => p.Team == human.Team && p != human);
        var opponent = roster.FieldPlayers.First(p => p.Team != human.Team);
        var brains = Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>()
            .Concat(Object.FindObjectsByType<PrototypeGoalkeeper>()).Where(b => b.enabled).ToArray();
        var actions = new List<string>();
        void Record(SwimmerBallInteraction actor, BallReleaseKind kind) => actions.Add($"{actor.name} {kind} {ball.LastReleaseSource}");
        ball.Released += Record;
        void Keys(params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        void Park()
        {
            foreach (var player in roster.Members)
                player.Motor.ResetPose(new Vector3(player.Team == human.Team ? -8f : 8f, 0f, player.CapNumber - 5f), Quaternion.identity);
        }
        void Hold(SwimmerIdentity player, Vector3 point, Vector3 forward)
        {
            ball.ResetForRestart(point + forward * .9f);
            player.Motor.ResetPose(point, Quaternion.LookRotation(forward));
            Check(player.Interaction.TryCollect(), "Fixture possession");
        }
        try
        {
            foreach (var brain in brains) brain.enabled = false;
            Park();
            selection.Select(human);
            ball.ResetForRestart(Vector3.forward * .94f);
            human.Motor.ResetPose(Vector3.zero, Quaternion.identity);
            await Until(() => ball.Owner == human.Interaction, "Nearby surface auto pickup without collect button");
            await Awaitable.WaitForSecondsAsync(.28f);
            Check(actions.Count == 0 && ball.Owner == human.Interaction, "Selected player held idle without throwing");

            var movement = new List<object>();
            foreach (MatchCameraView view in new[] { MatchCameraView.Lengthwise, MatchCameraView.SidelineBroadcast })
            {
                camera.SetView(view);
                camera.SnapToTarget();
                await Awaitable.NextFrameAsync();
                Vector2 screenForward = camera.ScreenToWorldMovement(Vector2.up);
                Vector3 forward = new Vector3(screenForward.x, 0f, screenForward.y);
                async Task<Vector3> Move(Key key)
                {
                    Hold(human, Vector3.zero, forward);
                    await Awaitable.NextFrameAsync();
                    Keys(key);
                    await Awaitable.WaitForSecondsAsync(.52f);
                    Vector3 speed = human.Motor.Velocity;
                    Keys();
                    await Awaitable.WaitForSecondsAsync(.12f);
                    Check(ball.Owner == human.Interaction && actions.Count == 0, "Movement did not trigger release");
                    return speed;
                }
                Vector3 letter = await Move(Key.W);
                Vector3 arrow = await Move(Key.UpArrow);
                Check(letter.magnitude > 1f && arrow.magnitude > 1f && Vector3.Dot(letter.normalized, arrow.normalized) > .98f &&
                    Mathf.Abs(letter.magnitude - arrow.magnitude) < .25f, $"Keyboard mismatch {view}: {letter}, {arrow}");
                movement.Add(new { view = view.ToString(), w = letter.magnitude, upArrow = arrow.magnitude });
            }
            Vector2 screenRight = camera.ScreenToWorldMovement(Vector2.right);
            Vector3 initialFacing = Vector3.Cross(Vector3.up, new Vector3(screenRight.x, 0f, screenRight.y));
            Hold(human, Vector3.zero, initialFacing);
            Keys(Key.RightArrow);
            await Until(() => Vector3.Dot(human.Motor.Facing, initialFacing) < .85f, "Rotation input turned athlete");
            Keys();
            Check(ball.Owner == human.Interaction && actions.Count == 0, "Rotation did not trigger release");
            match.SetPaused(true);
            Keys(Key.Space, Key.B, Key.L);
            float pausedUntil = Time.realtimeSinceStartup + .22f;
            await Until(() => Time.realtimeSinceStartup >= pausedUntil, "Unscaled wait during pause");
            Keys();
            await Awaitable.NextFrameAsync();
            match.SetPaused(false);
            await Awaitable.WaitForSecondsAsync(.25f);
            Check(ball.Owner == human.Interaction && actions.Count == 0, "Pause UI ignored gameplay keys");

            Hold(human, Vector3.zero, Vector3.forward);
            teammate.Motor.ResetPose(Vector3.forward * 4.5f, Quaternion.LookRotation(Vector3.back));
            await Until(() => !ball.IsGathering, "Gather complete before stale-throw check");
            Check(human.GetComponent<SwimmerPassing>().TryPassTo(teammate) && ball.HasPendingThrow, "Intentional pass queued");
            selection.SelectNext();
            Check(selection.ActivePlayer != human && !ball.HasPendingThrow, "Selection cancels prior athlete's pending pass immediately");
            Check(!human.Interaction.TryShoot(16f) && !human.Interaction.TryShoot(16f, BallActionSource.TacticalAI),
                "Unselected human and human-team AI both rejected at ball authority");
            human.GetComponent<PrototypeSwimmerAI>().enabled = true;
            await Awaitable.WaitForSecondsAsync(.80f);
            Check(actions.Count == 0 && ball.Owner == human.Interaction && selection.ActivePlayer != human,
                "Protected selection left the teammate in possession without throwing");
            await Until(() => selection.ActivePlayer == human, "Human control returned to carrier");

            // Exercise the real input edge for pass and the charge/release edge for shoot.
            Keys(Key.B);
            await Until(() => actions.Count == 1, "Keyboard B released one pass");
            Keys();
            await Until(() => ball.Owner == teammate.Interaction, "Stationary dry pass caught");
            Check(actions[0].EndsWith("Pass HumanInput", StringComparison.Ordinal), "Human pass provenance");
            await Until(() => !ball.IsGathering, "Receiver gather");
            Keys(Key.Space);
            await Awaitable.WaitForSecondsAsync(.23f);
            Keys();
            await Until(() => actions.Count == 2, "Keyboard Space released one shot");
            Check(actions[1].EndsWith("Shot HumanInput", StringComparison.Ordinal), "Human shot provenance");

            // Keep the live field AI active for this shot from an open approach.
            var goal = Object.FindObjectsByType<GoalMouth>().Single(g => g.ScoringTeam == opponent.Team);
            Vector3 forwardToGoal = goal.transform.forward;
            Vector3 lane = goal.transform.position - forwardToGoal * 4.6f;
            lane.y = 0f;
            human.GetComponent<PrototypeSwimmerAI>().enabled = false;
            Park();
            Hold(opponent, lane, forwardToGoal);
            opponent.GetComponent<PrototypeSwimmerAI>().enabled = true;
            await Until(() => actions.Count == 3, "Opposing AI physical release", 8f);
            Check(actions[2].EndsWith("Shot TacticalAI", StringComparison.Ordinal), "Opposing AI shot provenance");
            return new { actions, movement, autoPickupMetres = .94f, cancelledOnSelection = true, pauseInputSafe = true };
        }
        finally
        {
            ball.Released -= Record;
            if (match != null && match.IsPaused) match.SetPaused(false);
            if (camera != null) camera.SetView(originalView);
            InputSystem.RemoveDevice(keyboard);
            InputSystem.settings.backgroundBehavior = background;
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
            foreach (var brain in brains) if (brain != null) brain.enabled = true;
        }
    }

    public static async Task<object> RestartAndDryMiss()
    {
        var match = Object.FindAnyObjectByType<PrototypeMatch>();
        var ball = Object.FindAnyObjectByType<BallController>();
        var selection = Object.FindAnyObjectByType<PlayerSelection>();
        var roster = Object.FindAnyObjectByType<PrototypeRoster>();
        Check(Application.isPlaying && match != null && !HubMatchBridge.Active, "Direct isolated arena required.");
        var intro = Object.FindAnyObjectByType<PreMatchPresentation>();
        if (intro != null && intro.Active) intro.SkipAll();
        else match.RestartMatch();
        await Until(() => match.IsPlaying && !match.AwaitingOpeningPossession, "Opening sprint", 16f);
        var blue = roster.FieldPlayers.First(p => p.Team == selection.HumanTeam);
        var white = roster.FieldPlayers.First(p => p.Team != selection.HumanTeam);
        var whiteOutlet = roster.FieldPlayers.First(p => p.Team == white.Team && p != white);
        var whiteAI = white.GetComponent<PrototypeSwimmerAI>();
        var brains = Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>()
            .Concat(Object.FindObjectsByType<PrototypeGoalkeeper>()).Where(b => b.enabled).ToArray();
        var actions = new List<string>();
        int water = 0;
        void Record(SwimmerBallInteraction actor, BallReleaseKind kind) => actions.Add($"{actor.name} {kind} {ball.LastReleaseSource}");
        void Impact(BallImpactEvent impact) { if (impact.Kind == BallImpactKind.Water) water++; }
        void Park()
        {
            foreach (var player in roster.Members)
                player.Motor.ResetPose(new Vector3(player.Team == blue.Team ? -6f : 6f, 0f, player.CapNumber - 5f), Quaternion.identity);
        }
        void Hold(SwimmerIdentity player, Vector3 point, Vector3 forward)
        {
            ball.ResetForRestart(point + forward * .83f);
            player.Motor.ResetPose(point, Quaternion.LookRotation(forward));
            Check(player.Interaction.TryCollect(), "Fixture possession");
        }
        ball.Released += Record;
        ball.Contact += Impact;
        try
        {
            foreach (var brain in brains) brain.enabled = false;
            Park();
            Hold(blue, Vector3.zero, Vector3.forward);
            white.Motor.ResetPose(Vector3.right * .87f, Quaternion.LookRotation(Vector3.left));
            Check(match.Rules.ReportContact(white, blue, IllegalContact.Push), "Ordinary foul for human team.");
            await Until(() => match.Phase == MatchPhase.FreeThrowReady, "Human free throw ready");
            float clock = match.Clock.Remaining;
            float restartDeadline = match.PresentationTime + 6.25f;
            await Until(() => match.PresentationTime >= restartDeadline, "Existing six-second auto-throw window reached", 8f);
            Check(ball.Owner == blue.Interaction && actions.Count == 0 && match.Clock.Remaining == clock,
                "Human restart waited for command with clock stopped.");
            Check(blue.GetComponent<SwimmerPassing>().TryPass(), "Human ordinary restart pass");
            Check(actions.Count == 1 && actions[0].EndsWith("Pass HumanInput", StringComparison.Ordinal), "Human restart provenance.");
            await Until(() => match.IsPlaying, "Human restart resumes play");

            Park();
            Hold(white, Vector3.zero, Vector3.back);
            blue.Motor.ResetPose(Vector3.left * .87f, Quaternion.LookRotation(Vector3.right));
            Check(match.Rules.ReportContact(blue, white, IllegalContact.Push), "Ordinary foul for opposing team.");
            await Until(() => actions.Count == 2, "Opponent restart delivery", 6f);
            Check(actions[1].EndsWith("Pass RuleRestart", StringComparison.Ordinal), "Opponent restart provenance.");
            await Until(() => match.IsPlaying, "Opponent restart resumes play");

            Park();
            Hold(white, Vector3.forward * 7.3f, Vector3.back);
            whiteOutlet.Motor.ResetPose(Vector3.forward * 1.5f, Quaternion.identity);
            whiteAI.enabled = true;
            await Until(() => actions.Count == 3, "Opponent tactical outlet", 6f);
            Check(actions[2].EndsWith("Pass TacticalAI", StringComparison.Ordinal), "Opponent tactical pass provenance.");
            whiteAI.enabled = false;

            Park();
            Vector3 aim = Vector3.forward;
            Hold(blue, Vector3.back * 3.5f, aim);
            selection.Select(blue);
            water = 0;
            Check(ball.TryPass(blue.Interaction, aim, 7f, null, .08f, 2.5f), "Untargeted dry pass.");
            await Until(() => water == 1 && !ball.IsAirborne && ball.Owner == null, "Missed dry pass physical water impact.");
            await Until(() => ball.Velocity.magnitude < 2.2f, "Ball slowed to pickup range");
            float landingSpeed = ball.Velocity.magnitude;
            Vector3 contact = ball.PhysicsPosition;
            contact.y = 0f;
            blue.Motor.ResetPose(contact - aim * .85f, Quaternion.identity);
            await Until(() => ball.Owner == blue.Interaction, "Dry miss recovered through automatic pickup");
            Check(water == 1 && actions.Count == 4, "One impact and one physical release for the miss.");
            return new { actions, floatingRecoverySpeed = landingSpeed, impactCount = water, waitedPastFormerTimeout = 6.25f };
        }
        finally
        {
            ball.Released -= Record;
            ball.Contact -= Impact;
            foreach (var brain in brains) if (brain != null) brain.enabled = true;
        }
    }

    private static async Task Until(Func<bool> condition, string description, float seconds = 4f)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (!condition())
        {
            Check(Time.realtimeSinceStartup < deadline, description);
            await Awaitable.NextFrameAsync();
        }
    }

    private static void Check(bool pass, string description)
    {
        if (!pass) throw new InvalidOperationException(description);
    }
}
