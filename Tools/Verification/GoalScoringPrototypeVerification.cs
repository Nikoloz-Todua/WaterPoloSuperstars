// Run in Play Mode with PrototypePool open alone:
// unity command script run --file Tools/Verification/GoalScoringPrototypeVerification.cs --entry GoalScoringPrototypeVerification.Run
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using ToduStudios.WaterPolo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

public static class GoalScoringPrototypeVerification
{
    public static async Task<object> Run()
    {
        Require(Application.isPlaying && SceneManager.sceneCount == 1 && SceneManager.GetActiveScene().name == "PrototypePool", "Open PrototypePool alone in Play Mode.");
        GoalMouth[] goals = UnityEngine.Object.FindObjectsByType<GoalMouth>();
        Require(goals.Length == 2 && UnityEngine.Object.FindObjectsByType<PrototypeMatch>().Length == 1 && UnityEngine.Object.FindObjectsByType<PrototypeScoreboard>().Length == 1,
            "Exactly two goals, one score authority and one HUD.");
        PrototypeMatch match = UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
        PlayerSelection selection = UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
        if (selection != null) selection.Select(UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>().Members[0]);
        SwimmerMotor motor = selection != null ? selection.ActivePlayer.Motor : UnityEngine.Object.FindAnyObjectByType<SwimmerMotor>();
        SwimmerBallInteraction interaction = motor.GetComponent<SwimmerBallInteraction>();
        BallController ball = UnityEngine.Object.FindAnyObjectByType<BallController>();
        Rigidbody body = ball.GetComponent<Rigidbody>();
        PoolBounds pool = UnityEngine.Object.FindAnyObjectByType<PoolBounds>();
        TMP_Text label = GameObject.Find("PrototypeHUD").transform.Find("ScorePanel/ScoreLabel").GetComponent<TMP_Text>();
        GameObject notice = GameObject.Find("PrototypeHUD").transform.Find("GoalLabel").gameObject;
        GoalMouth blue = goals.Single(g => g.ScoringTeam == PrototypeTeam.Blue);
        GoalMouth white = goals.Single(g => g.ScoringTeam == PrototypeTeam.White);
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
        var passed = new List<string>();
        int initialBlue = match.BlueScore, initialWhite = match.WhiteScore;
        Vector3 anchorLocal = interaction.CarryAnchor.localPosition;
        var enabledAI = UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>().Where(ai => ai.enabled).ToArray();
        foreach (var ai in enabledAI) ai.enabled = false;
        try
        {
            while (match.IsRestarting) await Advance(0.05f);
            var roster = UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
            roster.ResetPositions(); ball.ResetForRestart(new Vector3(0f, ball.SurfaceHeight, 1.65f));
            selection.Select(roster.Members[0]);
            foreach (GoalMouth goal in goals)
            {
                Vector3 Point(float x, float z, float y = 0.15f) => goal.transform.TransformPoint(new Vector3(x, y, z));
                Require(!goal.TryGetCrossing(Point(0f, -1f), Point(0f, ball.Radius - 0.001f), ball.Radius, out _), "Partial ball crossing is not a goal.");
                Require(goal.TryGetCrossing(Point(0f, -1f), Point(0f, 3f), ball.Radius, out _), "Swept whole-ball crossing is detected.");
                Require(!goal.TryGetCrossing(Point(0f, 1f), Point(0f, -1f), ball.Radius, out _), "Wrong-direction geometry rejected.");
                Require(!goal.TryGetCrossing(Point(goal.Width * 0.5f - ball.Radius, -1f), Point(goal.Width * 0.5f - ball.Radius, 1f), ball.Radius, out _), "Post-tangent geometry rejected.");
                Require(!goal.TryGetCrossing(Point(0f, -1f, goal.Height + 0.1f), Point(0f, 1f, goal.Height + 0.1f), ball.Radius, out _), "Crossbar miss rejected.");
            }
            passed.Add("Both directional planes: swept sphere, full radius, posts and crossbar");

            async Task Prepare(GoalMouth goal, float x = 0f, float distance = 4f)
            {
                Press(keyboard);
                Vector3 position = goal.transform.TransformPoint(new Vector3(x, 0f, -distance));
                position.y = pool.Waterline;
                motor.ResetPose(position, goal.transform.rotation);
                ball.ResetForRestart(position + goal.transform.forward * 1.65f);
                await Advance(0.12f);
                await Tap(keyboard, Key.E);
                Require(interaction.HasBall, "E collects the ball for the next shot.");
            }

            async Task ScoreAndRestart(GoalMouth goal, bool fast = false, bool longShot = false)
            {
                int expectedBlue = match.BlueScore + (goal.ScoringTeam == PrototypeTeam.Blue ? 1 : 0);
                int expectedWhite = match.WhiteScore + (goal.ScoringTeam == PrototypeTeam.White ? 1 : 0);
                if (fast) Require(ball.TryShoot(interaction, 80f), "Release a high-speed shot.");
                else await Tap(keyboard, Key.Space);
                if (longShot)
                {
                    await Advance(0.7f);
                    Require(ball.State == BallState.Free && ball.IsScoringShot && !match.IsRestarting, "Long shot remains eligible after release cooldown.");
                }
                await Until(() => match.IsRestarting, 5f);
                Require(match.BlueScore == expectedBlue && match.WhiteScore == expectedWhite, "Correct team scored exactly once.");
                Require(label.text == $"BLUE {expectedBlue} - {expectedWhite} WHITE" && notice.activeSelf, "Scoreboard and GOAL notification update.");
                Require(!motor.enabled && body.isKinematic && !ball.CanCollect, "Restart locks movement and ball interaction.");
                await Advance(0.2f);
                Require(match.BlueScore == expectedBlue && match.WhiteScore == expectedWhite, "Repeated physics steps do not score again.");
                await Until(() => !match.IsRestarting, match.RestartDelay + 2f);
                await Advance(0.05f);
                Require(motor.enabled && motor.GetComponent<SwimmerInput>().enabled && motor.GetComponent<BallInteractionInput>().enabled, "Controls restored after restart.");
                Require(ball.Owner == null && ball.State == BallState.Free && !ball.IsScoringShot && ball.CanCollect, "Restart clears possession and shot eligibility.");
                Require(Vector3.Distance(motor.GetComponent<Rigidbody>().position, Vector3.zero) < 0.02f, "Swimmer reset to saved start.");
                Require(Vector3.Distance(body.position, new Vector3(0f, ball.SurfaceHeight, 1.65f)) < 0.02f, "Ball reset to collection start.");
                Require(!notice.activeSelf && match.BlueScore == expectedBlue && match.WhiteScore == expectedWhite, "Restart hides notice and preserves scores.");
            }

            await Prepare(blue);
            interaction.CarryAnchor.position = blue.transform.TransformPoint(new Vector3(0f, 0.15f, -1f));
            await Advance(0.2f);
            interaction.CarryAnchor.position = blue.transform.TransformPoint(new Vector3(0f, 0.15f, 1.5f));
            await Advance(0.4f);
            Require(ball.State == BallState.Held && match.BlueScore == initialBlue && match.WhiteScore == initialWhite, "Held ball crossing cannot score.");
            interaction.CarryAnchor.localPosition = anchorLocal;
            passed.Add("Held ball physically carried across the line does not score");

            foreach (float x in new[] { blue.Width * 0.5f + 0.8f, blue.Width * 0.5f + blue.PostThickness * 0.5f })
            {
                await Prepare(blue, x);
                await Tap(keyboard, Key.Space);
                await Advance(0.8f);
                Require(match.BlueScore == initialBlue && match.WhiteScore == initialWhite && !match.IsRestarting, "Outside-post or post-centred shot cannot score.");
                Require(blue.transform.InverseTransformPoint(body.position).z < 0f, "Miss remains on the playing side of the end boundary.");
            }
            passed.Add("Actual missed/post-centred shots blocked without scoring");

            await Prepare(blue);
            match.enabled = false;
            Require(ball.TryShoot(interaction, 8f), "Prepare released ball for reverse crossing.");
            body.position = blue.transform.TransformPoint(new Vector3(0f, 0.15f, 0.65f));
            body.linearVelocity = -blue.transform.forward * 8f;
            match.enabled = true; // Begin observing at the physical backside; do not count the test teleport.
            await Advance(0.45f);
            Require(match.BlueScore == initialBlue && match.WhiteScore == initialWhite && !match.IsRestarting, "Actual reverse crossing cannot score.");
            passed.Add("Released ball crossing from net toward pool does not score");

            await Prepare(blue);
            await ScoreAndRestart(blue, fast: true);
            passed.Add("High-speed Blue goal, exact-once score, HUD and restart");
            await Prepare(blue);
            await ScoreAndRestart(blue);
            passed.Add("Second pickup/Space shot and repeat restart work");
            await Prepare(white);
            await ScoreAndRestart(white);
            passed.Add("Opposite goal credits White and restarts correctly");
            await Prepare(blue, distance: 15f);
            await ScoreAndRestart(blue, longShot: true);
            passed.Add("Long shot scores after the Released-to-Free cooldown transition");
            return new { passed, blueAdded = match.BlueScore - initialBlue, whiteAdded = match.WhiteScore - initialWhite, frameCount = Time.frameCount };
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
            interaction.CarryAnchor.localPosition = anchorLocal;
            if (!match.enabled) match.enabled = true;
            if (!match.IsRestarting)
            {
                motor.ResetPose(Vector3.zero, Quaternion.identity);
                ball.ResetForRestart(new Vector3(0f, ball.SurfaceHeight, 1.65f));
                Camera.main.GetComponent<GameplayCamera>().SnapToTarget();
            }
            foreach (var ai in enabledAI) if (ai != null) ai.enabled = true;
        }
    }

    private static void Press(Keyboard keyboard, params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
    private static async Task Tap(Keyboard keyboard, Key key)
    {
        Press(keyboard, key);
        await Advance(0.08f);
        Press(keyboard);
        await Advance(0.02f);
    }
    private static async Task Advance(float seconds)
    {
        float start = Time.fixedTime;
        Stopwatch timer = Stopwatch.StartNew();
        while (Time.fixedTime - start < seconds)
        {
            if (timer.Elapsed.TotalSeconds > 8f) throw new TimeoutException("Physics is not advancing.");
            await Task.Yield();
        }
    }
    private static async Task Until(Func<bool> condition, float timeout)
    {
        Stopwatch timer = Stopwatch.StartNew();
        while (!condition())
        {
            if (timer.Elapsed.TotalSeconds > timeout) throw new TimeoutException("Expected goal/restart did not occur.");
            await Task.Yield();
        }
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
