// Run via Unity Pipeline in Play Mode, with PrototypePool open alone:
// unity command script run --file Tools/Verification/TeamPlayPrototypeVerification.cs --entry TeamPlayPrototypeVerification.Run
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

public static class TeamPlayPrototypeVerification
{
    public static async Task<object> Run()
    {
        Require(Application.isPlaying && SceneManager.sceneCount == 1 && SceneManager.GetActiveScene().name == "PrototypePool", "Open PrototypePool alone in Play Mode.");
        PrototypeRoster roster = UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
        PlayerSelection selection = UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
        GameplayCamera camera = Camera.main.GetComponent<GameplayCamera>();
        PrototypeMatch match = UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
        BallController ball = UnityEngine.Object.FindAnyObjectByType<BallController>();
        Rigidbody ballBody = ball.GetComponent<Rigidbody>();
        Require(roster.Members.Count >= 2 && UnityEngine.Object.FindObjectsByType<SwimmerMotor>().Length == roster.Members.Count &&
            UnityEngine.Object.FindObjectsByType<PlayerSelection>().Length == 1 && UnityEngine.Object.FindObjectsByType<BallController>().Length == 1, "Two swimmers, one selector and one ball.");
        SwimmerIdentity first = roster.Members[0], second = roster.Members[1];
        Require(first.PlayerId != second.PlayerId && first.Team == second.Team, "Distinct same-team identities.");
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
        var passed = new List<string>();
        int blueStart = match.BlueScore, whiteStart = match.WhiteScore;
        // Isolate original movement/ball fixtures; Friendly/Opponent AI suites exercise live AI separately.
        var enabledAI = UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>().Where(ai => ai.enabled).ToArray();
        foreach (var ai in enabledAI) ai.enabled = false;

        void SingleControl(SwimmerIdentity expected)
        {
            Require(selection.ActivePlayer == expected && camera.Target == expected.Motor, "Selection and camera agree.");
            Require(roster.Members.Count(p => p.GetComponent<SwimmerInput>().enabled) == 1 &&
                roster.Members.Count(p => p.GetComponent<BallInteractionInput>().enabled) == 1, "Only one pair of human input adapters is enabled.");
            Require(roster.Members.Count(p => p.transform.Find("SelectionRing").gameObject.activeSelf) == 1 &&
                expected.transform.Find("SelectionRing").gameObject.activeSelf, "Exactly the selected swimmer is marked.");
        }
        async Task ResetFixture()
        {
            Press(keyboard);
            await Advance(0.04f);
            roster.ResetPositions();
            ball.ResetForRestart(new Vector3(0f, ball.SurfaceHeight, 1.65f));
            selection.Select(first);
            await Advance(0.12f);
            camera.SnapToTarget();
        }

        try
        {
            await ResetFixture();
            SingleControl(first);
            Vector3 secondBefore = second.Motor.Position;
            Press(keyboard, Key.W);
            await Advance(0.5f);
            Require(first.Motor.Position.z > 0.5f && Vector3.Distance(second.Motor.Position, secondBefore) < 0.005f, "Only first swimmer responds to W.");
            Press(keyboard);
            await Advance(0.4f);
            Vector3 firstBefore = first.Motor.Position;
            Vector3 focusBefore = camera.Focus;
            await Tap(keyboard, Key.C);
            SingleControl(second);
            Require(camera.Focus.z > focusBefore.z && camera.Focus.z < second.Motor.Position.z - 0.1f, "Camera follows selection smoothly without snapping.");
            Press(keyboard, Key.D);
            await Advance(0.5f);
            Require(second.Motor.Position.x > secondBefore.x + 0.5f && Vector3.Distance(first.Motor.Position, firstBefore) < 0.005f, "Only second swimmer responds to D; inactive swimmer holds position.");
            Press(keyboard);
            await Advance(0.4f);
            passed.Add("WASD isolation, C switching, one selection ring and smooth camera target change");

            await ResetFixture();
            foreach (SwimmerIdentity player in roster.Members)
            {
                Vector3 view = Camera.main.WorldToViewportPoint(player.Motor.Position);
                Require(view.z > 0f && view.x > 0f && view.x < 1f && view.y > 0f && view.y < 1f, "Wider initial camera includes both swimmers.");
            }
            await Tap(keyboard, Key.E);
            Require(ball.Owner == first.Interaction, "First swimmer picks up.");
            await Tap(keyboard, Key.C);
            SingleControl(second);
            Require(ball.Owner == first.Interaction && ball.State == BallState.Held, "Switching does not transfer or drop possession.");
            await Tap(keyboard, Key.B);
            await Tap(keyboard, Key.Space);
            Require(ball.Owner == first.Interaction && ball.State == BallState.Held, "Selected non-owner cannot pass or shoot another swimmer's ball.");
            passed.Add("Selection preserves exclusive ownership and rejects non-owner B/Space");

            await ResetFixture();
            await Tap(keyboard, Key.E);
            await Tap(keyboard, Key.B, 0.04f);
            Require(ball.Owner == null && ball.State == BallState.Released && ball.IntendedReceiver == second.Interaction,
                "B launches a real assisted pass toward the aligned teammate.");
            Vector3 launchDirection = ballBody.linearVelocity.normalized;
            Require(ballBody.linearVelocity.magnitude > 6f && launchDirection.x > 0f && Vector3.Dot(launchDirection, first.transform.forward) > 0.97f,
                "Pass has configurable speed and modest directional assistance.");
            Require(!first.Interaction.TryCollect(), "Passer cannot immediately recover the release.");
            await Advance(0.1f);
            Require(ball.Owner == null && Vector3.Distance(ballBody.position, ball.ShotOrigin) > 0.4f, "Pass visibly travels before reception.");
            await Until(() => ball.Owner == second.Interaction, 3f);
            Require(selection.ActivePlayer == second && second.GetComponent<SwimmerInput>().enabled, "Successful reception automatically selects the friendly carrier.");
            Require(!first.Interaction.HasBall && second.Interaction.HasBall && !first.Interaction.TryCollect(), "Received possession is exclusive.");
            SingleControl(second);
            Require(ball.Owner == second.Interaction, "Selecting the receiver preserves possession.");
            await Tap(keyboard, Key.B);
            await Until(() => ball.Owner == first.Interaction, 3f);
            Require(selection.ActivePlayer == first && first.Interaction.HasBall && !second.Interaction.HasBall, "Return pass automatically selects the first swimmer upon acquisition.");
            passed.Add("B pass travel, capped assistance, release protection, automatic receiving and return pass");

            await ResetFixture();
            await Tap(keyboard, Key.E);
            await Tap(keyboard, Key.B, 0.04f);
            Vector3 missedDirection = ballBody.linearVelocity.normalized;
            second.Motor.ResetPose(new Vector3(8f, 0f, -6f), Quaternion.identity);
            await Advance(0.7f);
            Require(ball.Owner == null && ball.State == BallState.Free && Vector3.Dot(ballBody.linearVelocity.normalized, missedDirection) > 0.999f,
                "Moving the receiver away leaves an ordinary loose ball with no homing.");
            await Until(() => ball.IntendedReceiver == null, 3f);
            Require(ball.CanCollect, "Missed reception expires and the ball remains collectible.");
            passed.Add("Missed pass keeps normal ball physics and expires reception intent");

            await ResetFixture();
            first.Motor.ResetPose(new Vector3(-4f, 0f, -4f), Quaternion.identity);
            second.Motor.ResetPose(new Vector3(0f, 0f, 11f), Quaternion.identity);
            selection.Select(second);
            ball.ResetForRestart(new Vector3(0f, ball.SurfaceHeight, 12.65f));
            await Advance(0.12f);
            await Tap(keyboard, Key.E);
            Require(ball.Owner == second.Interaction, "Second swimmer independently collects.");
            await Tap(keyboard, Key.Space);
            await Until(() => match.IsRestarting, 4f);
            Require(match.BlueScore == blueStart + 1 && match.WhiteScore == whiteStart, "Second swimmer's shot scores exactly once.");
            await Tap(keyboard, Key.C);
            Press(keyboard, Key.W);
            await Advance(0.1f);
            Press(keyboard);
            Require(selection.InputLocked && selection.ActivePlayer == second && roster.Members.All(p => !p.Motor.enabled), "Restart locks both swimmers and ignores C.");
            await Until(() => !match.IsRestarting, match.RestartDelay + 2f);
            await Advance(0.08f);
            SingleControl(second);
            foreach (SwimmerIdentity player in roster.Members)
                Require(Vector3.Distance(player.Motor.Position, player.StartPosition.position) < 0.02f, "Both swimmers reset to their own starts.");
            Require(ball.State == BallState.Free && ball.Owner == null && ball.IntendedReceiver == null && ball.CanCollect, "Restart clears possession and receiving intent.");
            passed.Add("Second swimmer shooting, exact-once goal, control lock and two-swimmer restart");

            await Tap(keyboard, Key.C);
            await Tap(keyboard, Key.E);
            await Tap(keyboard, Key.B);
            await Until(() => ball.Owner == second.Interaction, 3f);
            second.Motor.ResetPose(new Vector3(0f, 0f, 11f), Quaternion.identity);
            await Advance(0.15f);
            await Tap(keyboard, Key.Space);
            await Until(() => match.IsRestarting, 4f);
            await Until(() => !match.IsRestarting, match.RestartDelay + 2f);
            Require(match.BlueScore == blueStart + 2 && match.WhiteScore == whiteStart, "Pickup, pass and shot still work after restart.");
            SingleControl(second);
            passed.Add("Post-restart pickup/pass/shot and repeated restart");
            return new { passed, blueAdded = match.BlueScore - blueStart, whiteAdded = match.WhiteScore - whiteStart, players = roster.Members.Count, frameCount = Time.frameCount };
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
            if (!match.IsRestarting)
            {
                roster.ResetPositions();
                ball.ResetForRestart(new Vector3(0f, ball.SurfaceHeight, 1.65f));
                selection.Select(first);
                camera.SnapToTarget();
            }
            foreach (var ai in enabledAI) if (ai != null) ai.enabled = true;
        }
    }

    private static void Press(Keyboard keyboard, params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
    private static async Task Tap(Keyboard keyboard, Key key, float duration = 0.08f)
    {
        Press(keyboard, key);
        await Advance(duration);
        Press(keyboard);
        await Advance(0.02f);
    }
    private static async Task Advance(float seconds)
    {
        float start = Time.fixedTime;
        Stopwatch timer = Stopwatch.StartNew();
        while (Time.fixedTime - start < seconds)
        {
            if (timer.Elapsed.TotalSeconds > 8f) throw new TimeoutException("Physics stopped advancing.");
            await Task.Yield();
        }
    }
    private static async Task Until(Func<bool> condition, float seconds)
    {
        Stopwatch timer = Stopwatch.StartNew();
        while (!condition())
        {
            if (timer.Elapsed.TotalSeconds > seconds) throw new TimeoutException("Expected pass/restart event did not occur.");
            await Task.Yield();
        }
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
