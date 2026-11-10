using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class StealDefenseVerification
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
        var blue = roster.FieldPlayers.Where(p => p.Team == PrototypeTeam.Blue).ToArray();
        var white = roster.FieldPlayers.Where(p => p.Team == PrototypeTeam.White).ToArray();
        var allAI = roster.FieldPlayers.Select(p => p.GetComponent<PrototypeSwimmerAI>()).ToArray();
        var originallyEnabled = allAI.Where(a => a.enabled).ToArray();
        foreach (var ai in allAI) ai.enabled = false;
        var steal = blue[0].GetComponent<SwimmerSteal>();
        var keyboard = InputSystem.AddDevice<Keyboard>(); var passed = new List<string>();
        // This fixture verifies Press; automatic White strategy selection is covered by integrated M9 play.
        var defenderTeam=white[0].GetComponent<PrototypeSwimmerAI>().TacticalTeam;
        var defenseTuning=new UnityEditor.SerializedObject(defenderTeam);bool automatic=defenseTuning.FindProperty("automaticDefense").boolValue;var defenseMode=defenderTeam.DefenseMode;
        defenseTuning.FindProperty("automaticDefense").boolValue=false;defenseTuning.ApplyModifiedPropertiesWithoutUndo();defenderTeam.SetDefense(DefenseStrategy.Press);
        void Pose(SwimmerIdentity p, float x, float z, float yaw) => p.Motor.ResetPose(new Vector3(x, 0f, z), Quaternion.Euler(0f, yaw, 0f));
        async Task Claim(SwimmerIdentity carrier)
        {
            ball.ResetForRestart(carrier.Motor.Position + carrier.transform.forward * 1.65f);
            Require(carrier.Interaction.TryCollect(), "Fixture claim uses range-checked pickup."); await Advance(0.05f);
        }
        async Task WaitReady(SwimmerSteal command) { await Advance(Mathf.Max(0f, command.NextAttemptAt - Time.time) + 0.08f); }
        try
        {
            roster.ResetPositions(); selection.Select(blue[0]);
            Pose(white[0], 0f, 0f, 0f); Pose(white[1], -7f, -7f, 0f);
            Pose(blue[0], 0f, 1.5f, 180f); Pose(blue[1], 7f, 7f, 0f); await Advance(0.12f);
            await Claim(white[0]);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F)); await Advance(0.08f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); await Advance(0.04f);
            Require(steal.LastResult == StealResult.Protected && ball.Owner == white[0].Interaction, "F cannot bypass new-carrier protection.");
            Require(steal.TrySteal() == StealResult.Cooldown && ball.Owner == white[0].Interaction, "Failed attempt consumes cooldown and preserves possession.");
            await WaitReady(steal); Pose(blue[0], 0f, 4f, 180f); await Advance(0.12f);
            Require(steal.TrySteal() == StealResult.OutOfRange && ball.Owner == white[0].Interaction, "Distant steal rejected.");
            await WaitReady(steal); Pose(blue[0], 0f, 1.5f, 0f); await Advance(0.12f);
            Require(steal.TrySteal() == StealResult.WrongFacing, "Facing away is rejected.");
            await WaitReady(steal); Pose(blue[0], 0f, -1.5f, 0f); await Advance(0.12f);
            Require(steal.TrySteal() == StealResult.Shielded, "Rear reach cannot ignore the carrier's shielded side.");
            await WaitReady(steal); Pose(blue[0], 0f, 1.5f, 180f); await Advance(0.12f);
            selection.Select(blue[1]);
            Require(steal.TrySteal() == StealResult.Success, "Close exposed facing attempt succeeds through shared ball authority.");
            Require(ball.Owner == blue[0].Interaction && selection.ActivePlayer == blue[0] && Camera.main.GetComponent<GameplayCamera>().Target == blue[0].Motor, "Friendly steal auto-selects actual new carrier and camera.");
            Require(roster.FieldPlayers.Count(p => p.Interaction.HasBall) == 1, "Exactly one owner after steal.");
            passed.Add("F input, protection, distance, facing, shielding, failed-attempt cooldown and friendly steal/selection");

            var teammateSteal = blue[1].GetComponent<SwimmerSteal>();
            Require(teammateSteal.TrySteal() == StealResult.NoOpponent && ball.Owner == blue[0].Interaction, "Same-team steals cannot transfer ownership.");
            selection.SetInputLocked(true);
            Require(white[0].GetComponent<SwimmerSteal>().TrySteal() == StealResult.Unavailable, "Stoppage lock blocks direct and AI commands.");
            selection.SetInputLocked(false); passed.Add("Friendly ownership rejection and input-lock protection");

            // One presser, one separate goal-side defender; both White AI enabled while Blue is human-controlled.
            Pose(blue[0], 0f, 0f, 0f); Pose(blue[1], -4f, -2f, 0f);
            Pose(white[0], 0f, 2.7f, 180f); Pose(white[1], 5f, 5f, 180f); await Advance(0.12f);
            await Claim(blue[0]);
            var whiteTeam = white[0].GetComponent<PrototypeSwimmerAI>().TacticalTeam; whiteTeam.ResetDecisions();
            var presser = whiteTeam.Presser;
            Require(presser == white[0], "Nearest defender assigned pressure.");
            var firstDecision = WaterPoloDecision.Evaluate(white[0], whiteTeam);
            var secondDecision = WaterPoloDecision.Evaluate(white[1], whiteTeam);
            Require(firstDecision.Behaviour == SwimmerAIBehaviour.Press && secondDecision.Behaviour == SwimmerAIBehaviour.Defend && Vector3.Distance(firstDecision.Target, secondDecision.Target) > 2f, "Distinct press and defensive lane targets.");
            Pose(white[1], 0.5f, 2.4f, 180f); await Advance(0.6f);
            Require(whiteTeam.Presser == presser, "Small distance change cannot oscillate pressure assignment.");
            Pose(white[1], 6f, 6f, 180f);
            white[0].GetComponent<PrototypeSwimmerAI>().enabled = true;
            white[1].GetComponent<PrototypeSwimmerAI>().enabled = true;
            await Until(() => ball.Owner != blue[0].Interaction, 6f);
            Require(ball.Owner != null && ball.Owner.GetComponent<SwimmerIdentity>().Team == PrototypeTeam.White && selection.ActivePlayer.Team == PrototypeTeam.Blue, "AI closes, aims and uses the same steal command without taking human control.");
            passed.Add("Coordinated defense, assignment hysteresis, AI pressure/steal and valid Blue control");
            return new { passed };
        }
        finally
        {
            defenseTuning.Update();defenseTuning.FindProperty("automaticDefense").boolValue=automatic;defenseTuning.ApplyModifiedPropertiesWithoutUndo();defenderTeam.SetDefense(defenseMode);
            foreach (var keeper in isolatedKeepers) if (keeper != null) keeper.enabled = true;
            InputSystem.RemoveDevice(keyboard); foreach (var ai in allAI) ai.enabled = false;
            selection.SetInputLocked(false); roster.ResetPositions(); ball.ResetForRestart(new Vector3(0f, ball.SurfaceHeight, 1.65f)); selection.Select(blue[0]);
            foreach (var ai in originallyEnabled) ai.enabled = true;
        }
    }
    private static async Task Advance(float seconds)
    {
        float start = Time.fixedTime; var watch = Stopwatch.StartNew(); while (Time.fixedTime - start < seconds) { if (watch.Elapsed.TotalSeconds > 8f) throw new TimeoutException("Physics stopped."); await Task.Yield(); }
    }
    private static async Task Until(Func<bool> condition, float seconds)
    {
        var watch = Stopwatch.StartNew(); while (!condition()) { if (watch.Elapsed.TotalSeconds > seconds) throw new TimeoutException("Expected AI steal did not occur."); await Task.Yield(); }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
