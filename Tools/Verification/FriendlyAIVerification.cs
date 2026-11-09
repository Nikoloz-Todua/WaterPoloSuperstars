using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class FriendlyAIVerification
{
    public static async Task<object> Run()
    {
        var roster = UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
        var selection = UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
        var ball = UnityEngine.Object.FindAnyObjectByType<BallController>();
        var first = roster.Members[0]; var second = roster.Members[1];
        var ai = second.GetComponent<PrototypeSwimmerAI>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var passed = new List<string>();
        var opponents = roster.Members.Where(p => p.Team != selection.HumanTeam).Select(p => p.GetComponent<PrototypeSwimmerAI>()).Where(ai => ai != null && ai.enabled).ToArray();
        foreach (var opponent in opponents) opponent.enabled = false;
        try
        {
            roster.ResetPositions(); selection.Select(first);
            second.Motor.ResetPose(new Vector3(-4f, 0f, -2f), Quaternion.identity);
            ball.ResetForRestart(first.Motor.Position + Vector3.forward * 1.65f);
            Require(first.Interaction.TryCollect(), "Fixture pickup within existing range.");
            var before = second.Motor.Position; var humanBefore = first.Motor.Position;
            await Advance(0.7f);
            Require(ai.Behaviour == SwimmerAIBehaviour.Support && Vector3.Distance(before, second.Motor.Position) > 0.3f, "Inactive teammate swims to support.");
            Require(Vector3.Distance(first.Motor.Position, humanBefore) < 0.01f, "AI never writes to selected actor.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            await Advance(0.3f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            await Advance(0.2f);
            Require(first.Motor.Position.z > humanBefore.z + 0.2f, "Human input remains responsive with AI enabled.");
            selection.Select(second); await Advance(0.3f);
            Require(ball.Owner == first.Interaction && selection.ActivePlayer == second && first.GetComponent<PrototypeSwimmerAI>().Behaviour == SwimmerAIBehaviour.HoldBall, "Manual selection does not transfer possession or trigger AI-human conflict.");
            passed.Add("Supporting movement, human input ownership and manual switch while teammate holds ball");

            roster.ResetPositions(); selection.Select(first);
            second.Motor.ResetPose(new Vector3(3f, 0f, 4f), Quaternion.identity);
            ball.ResetForRestart(new Vector3(6f, ball.SurfaceHeight, 4f));
            await Advance(0.15f);
            Require(ball.Owner == null && ai.Behaviour == SwimmerAIBehaviour.PursueLooseBall, "Pickup cannot bypass range.");
            await Until(() => ball.Owner == second.Interaction, 5f);
            Require(selection.ActivePlayer == second && Camera.main.GetComponent<GameplayCamera>().Target == second.Motor, "AI's legitimate loose-ball pickup automatically selects friendly carrier.");
            passed.Add("Nearest inactive friendly approaches loose ball and acquires via existing range/cooldown authority");

            roster.ResetPositions(); selection.Select(first);
            ball.ResetForRestart(new Vector3(0f, ball.SurfaceHeight, 1.65f));
            before = second.Motor.Position;
            await Advance(0.5f);
            Require(ball.Owner == null && ai.Behaviour == SwimmerAIBehaviour.Idle && Vector3.Distance(before, second.Motor.Position) < 0.1f, $"Human closer to ball prevents redundant teammate chase. Owner={ball.Owner}, behaviour={ai.Behaviour}, drift={Vector3.Distance(before, second.Motor.Position)}, human={first.Motor.Position}, teammate={second.Motor.Position}, ball={ball.PhysicsPosition}");
            passed.Add("Nearest-per-team arbitration includes controlled human");
            return new { passed };
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard); roster.ResetPositions();
            ball.ResetForRestart(new Vector3(0f, ball.SurfaceHeight, 1.65f)); selection.Select(first);
            foreach (var opponent in opponents) if (opponent != null) opponent.enabled = true;
        }
    }
    private static async Task Advance(float seconds)
    {
        float start = Time.fixedTime; var timer = Stopwatch.StartNew();
        while (Time.fixedTime - start < seconds)
        { if (timer.Elapsed.TotalSeconds > 8f) throw new TimeoutException("Physics stopped advancing."); await Task.Yield(); }
    }
    private static async Task Until(Func<bool> condition, float seconds)
    {
        var timer = Stopwatch.StartNew();
        while (!condition()) { if (timer.Elapsed.TotalSeconds > seconds) throw new TimeoutException("Expected pickup did not occur."); await Task.Yield(); }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
