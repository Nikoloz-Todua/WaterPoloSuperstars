using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class TwoVTwoMatchVerification
{
    public static async Task<object> Run()
    {
        // Original field-player fixture: keeper behavior is exercised by the dedicated keeper suites.
        var isolatedKeepers = UnityEngine.Object.FindObjectsByType<PrototypeGoalkeeper>().Where(k => k.enabled).ToArray();
        foreach (var keeper in isolatedKeepers) keeper.enabled = false;
        var runningMatch = UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
        while (runningMatch != null && runningMatch.IsRestarting) await Advance(0.05f);
        var roster = UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
        var selection = UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
        var ball = UnityEngine.Object.FindAnyObjectByType<BallController>();
        var match = UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
        var blue = roster.FieldPlayers.Where(p => p.Team == PrototypeTeam.Blue).ToArray();
        var white = roster.FieldPlayers.Where(p => p.Team == PrototypeTeam.White).ToArray();
        var controllers = roster.FieldPlayers.Select(p => p.GetComponent<PrototypeSwimmerAI>()).ToArray();
        var originallyEnabled = controllers.Where(a => a.enabled).ToArray();
        var keyboard = InputSystem.AddDevice<Keyboard>(); var passed = new List<string>();
        int blueBefore = match.BlueScore, whiteBefore = match.WhiteScore, validRestarts = 0;
        bool invalidRestart = false;
        void Restarted()
        {
            if (match.IsRestarting) return;
            bool valid = ball.Owner == null && ball.IntendedReceiver == null && !ball.IsScoringShot && ball.CanCollect &&
                !selection.InputLocked && selection.ActivePlayer.Team == PrototypeTeam.Blue &&
                roster.FieldPlayers.All(p => p.Motor.enabled && Vector3.Distance(p.Motor.Position, p.StartPosition.position) < 0.01f);
            if (valid) validRestarts++; else invalidRestart = true;
        }
        match.Changed += Restarted;
        void EnableAI(bool value) { foreach (var ai in controllers) ai.enabled = value; }
        void Pose(SwimmerIdentity p, float x, float z, float yaw = 0f) => p.Motor.ResetPose(new Vector3(x, 0f, z), Quaternion.Euler(0f, yaw, 0f));
        async Task Clear()
        {
            EnableAI(false); roster.ResetPositions(); selection.Select(blue[0]);
            ball.ResetForRestart(new Vector3(0f, ball.SurfaceHeight, 1.65f));
            foreach (var context in UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>()) context.ResetDecisions();
            await Advance(0.12f);
        }
        async Task CheckGoal(bool whiteGoal)
        {
            int expectedBlue = match.BlueScore + (whiteGoal ? 0 : 1), expectedWhite = match.WhiteScore + (whiteGoal ? 1 : 0);
            await Until(() => match.IsRestarting, 7f);
            Require(match.BlueScore == expectedBlue && match.WhiteScore == expectedWhite && selection.InputLocked && roster.FieldPlayers.All(p => !p.Motor.enabled), "Correct exact-once goal freezes all four actors.");
            await Advance(0.2f);
            Require(match.BlueScore == expectedBlue && match.WhiteScore == expectedWhite, "No repeated score during restart.");
            await Until(() => !match.IsRestarting, 4f);
            Require(!invalidRestart, "Every restart clears owner/pass/shot and resets all four before AI resumes.");
        }
        try
        {
            Require(blue.Length == 2 && white.Length == 2 && controllers.All(a => a.TacticalTeam != null), "Complete 2v2 configuration.");
            await Clear(); Pose(white[0], 8f, 13f, 180f); Pose(white[1], -8f, 13f, 180f);
            EnableAI(true);
            await Tap(keyboard, Key.E); Require(ball.Owner == blue[0].Interaction, "Human E pickup.");
            await Tap(keyboard, Key.B, 0.04f);
            Require(ball.Owner == null && selection.ActivePlayer == blue[0], "Pass attempt does not switch early.");
            await Until(() => ball.Owner == blue[1].Interaction, 4f);
            Require(selection.ActivePlayer == blue[1] && Camera.main.GetComponent<GameplayCamera>().Target == blue[1].Motor, "Live-AI friendly reception switches input/ring/camera.");
            Vector3 direction = PrototypeTeamTactics.Planar(blue[0].Motor.Position - blue[1].Motor.Position);
            blue[1].Motor.ResetPose(blue[1].Motor.Position, Quaternion.LookRotation(direction)); await Advance(0.12f);
            await Tap(keyboard, Key.B, 0.04f);
            await Until(() => ball.Owner == blue[0].Interaction, 4f);
            Require(selection.ActivePlayer == blue[0], "Return reception selects original player with all AI enabled.");
            await Tap(keyboard, Key.C);
            Require(selection.ActivePlayer == blue[1] && ball.Owner == blue[0].Interaction, "C keeps ownership independent while old human becomes AI.");
            passed.Add("Full-AI human pickup/pass/return, automatic switches, camera/ring and ownership-independent C");

            for (int cycle = 0; cycle < 2; cycle++)
            {
                await Clear(); Pose(blue[0], 8f, 10f); Pose(blue[1], -8f, 10f);
                Pose(white[0], cycle == 0 ? 0f : 1f, -10f); Pose(white[1], 6f, -3f, 180f);
                await Advance(0.12f); ball.ResetForRestart(white[0].Motor.Position + Vector3.forward * 1.65f);
                Require(white[0].Interaction.TryCollect(), "New White possession via pickup."); EnableAI(true);
                await CheckGoal(true);
                await Clear(); Pose(blue[0], 0f, 11f); Pose(blue[1], 6f, 4f);
                Pose(white[0], 7f, -8f, 180f); Pose(white[1], -7f, -8f, 180f);
                ball.ResetForRestart(new Vector3(0f, ball.SurfaceHeight, 12.65f)); await Advance(0.12f);
                EnableAI(true); await Tap(keyboard, Key.E); await Tap(keyboard, Key.Space, 0.04f);
                await CheckGoal(false);
            }
            Require(validRestarts == 4, "Two White AI and two Blue human goals restart all four repeatedly.");
            passed.Add("Two White AI goals and two Blue human goals, all four resets, cleared decisions and persistent scores");

            await Clear(); EnableAI(true);
            // Observe ordinary live play with no fixture intervention for 20 simulated seconds.
            float start = Time.fixedTime;
            int shots = controllers.Sum(a => a.ShotsExecuted);
            while (Time.fixedTime - start < 20f)
            {
                await Advance(0.1f);
                Require(roster.FieldPlayers.Count(p => p.Interaction.HasBall) <= 1 && selection.ActivePlayer.Team == PrototypeTeam.Blue, "Live play preserves ownership and valid human team.");
                foreach (var p in roster.FieldPlayers)
                    Require(Mathf.Abs(p.Motor.Position.x) <= 8.91f && Mathf.Abs(p.Motor.Position.z) <= 13.91f && Mathf.Abs(p.Motor.Position.y) < 0.001f, "All actors remain bounded at waterline.");
                foreach (var p in white)
                    Require(!p.GetComponent<SwimmerInput>().enabled && !p.GetComponent<BallInteractionInput>().enabled, "White never receives human input.");
                if (ball.Owner != null && ball.Owner.GetComponent<SwimmerIdentity>().Team == PrototypeTeam.White)
                    Require(Time.time - ball.OwnershipStartedAt < 10.5f, "White possession never stalls past timeout plus aim allowance.");
            }
            Require(controllers.Sum(a => a.ShotsExecuted) > shots, "Ordinary uninterrupted play produces an autonomous attacking shot.");
            passed.Add("20 seconds of ordinary live 2v2: autonomous attack, no stalled White carrier, exclusive ownership and bounded actors");
            return new { passed, validRestarts, blueAdded = match.BlueScore - blueBefore, whiteAdded = match.WhiteScore - whiteBefore };
        }
        finally
        {
            foreach (var keeper in isolatedKeepers) if (keeper != null) keeper.enabled = true;
            match.Changed -= Restarted; InputSystem.RemoveDevice(keyboard); EnableAI(false);
            if (!match.IsRestarting) { roster.ResetPositions(); ball.ResetForRestart(new Vector3(0f, ball.SurfaceHeight, 1.65f)); selection.Select(blue[0]); }
            foreach (var ai in originallyEnabled) ai.enabled = true;
        }
    }
    private static async Task Tap(Keyboard keyboard, Key key, float seconds = 0.08f)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); await Advance(seconds);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); await Advance(0.02f);
    }
    private static async Task Advance(float seconds)
    {
        float start = Time.fixedTime; var watch = Stopwatch.StartNew(); while (Time.fixedTime - start < seconds) { if (watch.Elapsed.TotalSeconds > 8f) throw new TimeoutException("Physics stopped."); await Task.Yield(); }
    }
    private static async Task Until(Func<bool> condition, float seconds)
    {
        var watch = Stopwatch.StartNew(); while (!condition()) { if (watch.Elapsed.TotalSeconds > seconds) throw new TimeoutException("Expected live possession/goal event did not occur."); await Task.Yield(); }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
