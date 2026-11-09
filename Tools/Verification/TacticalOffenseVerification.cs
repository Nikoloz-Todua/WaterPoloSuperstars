using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using UnityEditor;
using UnityEngine;

public static class TacticalOffenseVerification
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
        var ai = white[0].GetComponent<PrototypeSwimmerAI>(); var team = ai.TacticalTeam;
        var enabledAI = roster.FieldPlayers.Select(p => p.GetComponent<PrototypeSwimmerAI>()).Where(a => a.enabled).ToArray();
        foreach (var controller in enabledAI) controller.enabled = false;
        var passed = new List<string>();
        void Pose(SwimmerIdentity p, float x, float z, float yaw = 0f) => p.Motor.ResetPose(new Vector3(x, 0f, z), Quaternion.Euler(0f, yaw, 0f));
        async Task Fixture(float x, float z)
        {
            ai.enabled = false; roster.ResetPositions(); selection.Select(blue[0]);
            Pose(blue[0], 8f, 12f); Pose(blue[1], 8f, 8f); Pose(white[0], x, z); Pose(white[1], -6f, 10f);
            ball.ResetForRestart(white[0].Motor.Position + Vector3.forward * 1.65f);
            await Advance(0.12f);
            Require(white[0].Interaction.TryCollect(), "White uses legitimate pickup."); team.ResetDecisions();
        }
        try
        {
            await Fixture(0f, -7f);
            int shots = ai.ShotsExecuted, score = match.WhiteScore;
            ai.enabled = true;
            await Until(() => ai.ShotsExecuted > shots, 5f);
            Require(ball.Owner == null && ball.IsScoringShot && selection.ActivePlayer.Team == PrototypeTeam.Blue, "AI chooses and releases a real shot without selecting White.");
            await Until(() => match.IsRestarting, 5f);
            Require(match.WhiteScore == score + 1, "Physical White shot crosses the correct goal and credits White.");
            ai.enabled = false;
            await Until(() => !match.IsRestarting, 4f);
            passed.Add("White pickup, turning, shot selection, physical goal and authoritative White score/restart");

            await Fixture(-4f, 6f); Pose(white[1], -1f, 0f);
            await Advance(0.12f);
            int passes = ai.PassesExecuted; ai.enabled = true;
            Vector3 before = white[0].Motor.Position;
            await Advance(0.3f);
            Require(white[0].Motor.Position.z < before.z - 0.1f, "White carrier advances toward its explicit attacking end.");
            await Until(() => ai.PassesExecuted > passes, 5f);
            Require(ball.Owner == null && ball.IntendedReceiver == white[1].Interaction && ball.GetComponent<Rigidbody>().linearVelocity.magnitude > 6f, "AI makes a physical same-team pass.");
            Require(!white[0].Interaction.TryCollect(), "Passer release protection is preserved.");
            await Until(() => ball.Owner == white[1].Interaction, 4f);
            Require(selection.ActivePlayer.Team == PrototypeTeam.Blue && !white[0].Interaction.HasBall, "White receiver gets exclusive possession without human switch.");
            passed.Add("White carrying advance, better-option pass, moving ball and legitimate teammate reception");

            await Fixture(7f, 7f); Pose(white[1], -8f, 12f);
            var tuning = new SerializedObject(team);
            float hold = tuning.FindProperty("maximumHoldTime").floatValue;
            float threshold = tuning.FindProperty("shotQualityThreshold").floatValue;
            try
            {
                tuning.FindProperty("maximumHoldTime").floatValue = 1.2f;
                tuning.FindProperty("shotQualityThreshold").floatValue = 1f;
                tuning.ApplyModifiedPropertiesWithoutUndo(); shots = ai.ShotsExecuted; ai.enabled = true;
                await Until(() => ai.ShotsExecuted > shots, 5f);
                Require(ball.Owner == null, "Configured maximum hold prevents indefinite possession even with unattractive shots and no better pass.");
            }
            finally
            {
                tuning.Update(); tuning.FindProperty("maximumHoldTime").floatValue = hold;
                tuning.FindProperty("shotQualityThreshold").floatValue = threshold; tuning.ApplyModifiedPropertiesWithoutUndo();
            }
            passed.Add("Configurable anti-stall fallback physically releases ball");
            return new { passed };
        }
        finally
        {
            foreach (var keeper in isolatedKeepers) if (keeper != null) keeper.enabled = true;
            foreach (var controller in enabledAI) controller.enabled = false;
            if (!match.IsRestarting) { roster.ResetPositions(); ball.ResetForRestart(new Vector3(0f, ball.SurfaceHeight, 1.65f)); selection.Select(blue[0]); }
            foreach (var controller in enabledAI) controller.enabled = true;
        }
    }
    private static async Task Advance(float seconds)
    {
        float start = Time.fixedTime; var watch = Stopwatch.StartNew();
        while (Time.fixedTime - start < seconds) { if (watch.Elapsed.TotalSeconds > 8f) throw new TimeoutException("Physics stopped."); await Task.Yield(); }
    }
    private static async Task Until(Func<bool> condition, float seconds)
    {
        var watch = Stopwatch.StartNew(); while (!condition()) { if (watch.Elapsed.TotalSeconds > seconds) throw new TimeoutException("AI action or goal did not occur."); await Task.Yield(); }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
