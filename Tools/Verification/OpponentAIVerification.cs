using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class OpponentAIVerification
{
    public static async Task<object> Run()
    {
        var roster = UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
        var selection = UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
        var ball = UnityEngine.Object.FindAnyObjectByType<BallController>();
        var match = UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
        var first = roster.Members[0]; var second = roster.Members[1]; var opponent = roster.Members[2];
        var ai = opponent.GetComponent<PrototypeSwimmerAI>();
        var keyboard = InputSystem.AddDevice<Keyboard>(); var passed = new List<string>();
        try
        {
            Require(roster.Members.Count == 3 && opponent.Team != selection.HumanTeam, "One separately identified opponent.");
            roster.ResetPositions(); selection.Select(first);
            ball.ResetForRestart(first.Motor.Position + Vector3.forward * 1.65f);
            Require(first.Interaction.TryCollect(), "Existing pickup.");
            Vector3 before = opponent.Motor.Position;
            await Advance(0.7f);
            Require(ai.Behaviour == SwimmerAIBehaviour.Defend && Vector3.Distance(before, opponent.Motor.Position) > 0.3f, "Opponent approaches defensive goal-side position.");
            Require(ai.Target.z > first.Motor.Position.z && ai.Target.z < 15f, "Opponent positions between carrier and its defended goal.");
            for (int i = 0; i < 5; i++)
            {
                selection.SelectNext();
                Require(selection.ActivePlayer == first || selection.ActivePlayer == second, "C never selects enemy.");
            }
            Require(!selection.Select(opponent) && ball.Owner == first.Interaction, "Direct enemy selection rejected without changing possession.");
            Require(!opponent.GetComponent<SwimmerInput>().enabled && !opponent.GetComponent<BallInteractionInput>().enabled && !opponent.transform.Find("SelectionRing").gameObject.activeSelf, "Opponent never has human input or selection marker.");
            passed.Add("Opponent defensive positioning, C filtering and ownership-independent selection");

            roster.ResetPositions(); selection.Select(first);
            first.Motor.ResetPose(new Vector3(-7f, 0f, -10f), Quaternion.identity);
            second.Motor.ResetPose(new Vector3(-7f, 0f, -6f), Quaternion.identity);
            ball.ResetForRestart(opponent.Motor.Position + Vector3.left * 3.5f);
            await Advance(0.2f);
            Require(ball.Owner == null && ai.Behaviour == SwimmerAIBehaviour.PursueLooseBall, "Opponent must approach before legitimate collection.");
            await Until(() => ball.Owner == opponent.Interaction, 5f);
            Require(selection.ActivePlayer == first && !first.Interaction.HasBall && !second.Interaction.HasBall, "Enemy possession is exclusive and never takes human selection.");
            await Advance(0.4f); // Allow the existing motor's normal deceleration after the pickup.
            Vector3 heldAt = opponent.Motor.Position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D)); await Advance(0.4f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); await Advance(0.3f);
            Require(ai.Behaviour == SwimmerAIBehaviour.HoldBall && Vector3.Distance(heldAt, opponent.Motor.Position) < 0.1f, "Human keys cannot move opponent.");
            passed.Add("Opponent loose-ball pursuit uses existing collection rules; enemy ownership never selects enemy");

            ball.ResetForRestart(new Vector3(0f, ball.SurfaceHeight, 12.65f));
            first.Motor.ResetPose(new Vector3(0f, 0f, 11f), Quaternion.identity);
            second.Motor.ResetPose(new Vector3(-5f, 0f, -4f), Quaternion.identity);
            opponent.Motor.ResetPose(new Vector3(6f, 0f, 7f), Quaternion.identity);
            selection.Select(first); await Advance(0.08f);
            Require(first.Interaction.TryCollect() && first.Interaction.TryShoot(), "Shoot after opponent ownership reset.");
            int blue = match.BlueScore;
            await Until(() => match.IsRestarting, 4f);
            Require(match.BlueScore == blue + 1 && selection.InputLocked && roster.Members.All(p => !p.Motor.enabled), "Goal freezes all three and scores once.");
            await Advance(0.2f);
            Require(match.BlueScore == blue + 1, "Restart cannot score twice.");
            await Until(() => !match.IsRestarting, 4f);
            foreach (var player in roster.Members)
                Require(Vector3.Distance(player.Motor.Position, player.StartPosition.position) < 0.1f && player.Motor.enabled, "All swimmers reset and resume.");
            Require(ball.Owner == null && ball.IntendedReceiver == null && ball.CanCollect && selection.ActivePlayer.Team == selection.HumanTeam, "Restart clears ownership/pass intent and restores friendly control.");
            await Advance(0.12f); // Let interpolated actor transforms catch up to their reset physics poses.
            Require(first.Interaction.TryCollect() && first.GetComponent<SwimmerPassing>().TryPass(), "Next pickup and pass work after restart.");
            await Until(() => ball.Owner == second.Interaction, 4f);
            Require(selection.ActivePlayer == second, "Post-restart pass still automatically selects receiver with full AI enabled.");
            Vector3 returnDirection = first.Motor.Position - second.Motor.Position;
            returnDirection.y = 0f;
            second.Motor.ResetPose(second.Motor.Position, Quaternion.LookRotation(returnDirection));
            await Advance(0.12f);
            Require(second.GetComponent<SwimmerPassing>().TryPass(), "Receiver can aim and pass back with live AI.");
            Require(ball.Owner == null && selection.ActivePlayer == second, "Return attempt cannot switch before acquisition.");
            await Until(() => ball.Owner == first.Interaction, 4f);
            Require(selection.ActivePlayer == first && Camera.main.GetComponent<GameplayCamera>().Target == first.Motor, "Return reception retargets human input and camera with all AI enabled.");
            passed.Add("All-three goal lock/reset, exact-once scoring and post-restart pickup/pass/automatic selection");

            roster.ResetPositions(); selection.Select(first);
            ball.ResetForRestart(new Vector3(0f, ball.SurfaceHeight, 1.65f));
            Require(first.Interaction.TryCollect(), "Boundary fixture pickup.");
            first.Motor.ResetPose(new Vector3(8.9f, 0f, 13.9f), Quaternion.identity);
            await Advance(1.5f);
            foreach (var player in roster.Members)
                Require(Mathf.Abs(player.Motor.Position.x) <= 8.91f && Mathf.Abs(player.Motor.Position.z) <= 13.91f && Mathf.Abs(player.Motor.Position.y) < 0.001f, "AI respects pool bounds and waterline.");
            passed.Add("AI targets and motors remain inside pool bounds at edge pressure");
            return new { passed, players = roster.Members.Count };
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
            if (!match.IsRestarting) { roster.ResetPositions(); ball.ResetForRestart(new Vector3(0f, ball.SurfaceHeight, 1.65f)); selection.Select(first); }
        }
    }
    private static async Task Advance(float seconds)
    {
        float start = Time.fixedTime; var timer = Stopwatch.StartNew();
        while (Time.fixedTime - start < seconds) { if (timer.Elapsed.TotalSeconds > 8f) throw new TimeoutException("Physics stopped advancing."); await Task.Yield(); }
    }
    private static async Task Until(Func<bool> condition, float seconds)
    {
        var timer = Stopwatch.StartNew();
        while (!condition()) { if (timer.Elapsed.TotalSeconds > seconds) throw new TimeoutException("Expected possession/restart event did not occur."); await Task.Yield(); }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
