// Step A: run via Unity Pipeline in PrototypePool Play Mode before enabling AI.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class PossessionSelectionVerification
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
        var camera = Camera.main.GetComponent<GameplayCamera>();
        var first = roster.FieldPlayers[0];
        var second = roster.FieldPlayers[1];
        var keyboard = InputSystem.AddDevice<Keyboard>();
        int changes = 0;
        void Changed(SwimmerBallInteraction oldOwner, SwimmerBallInteraction newOwner) => changes++;
        ball.PossessionChanged += Changed;
        var passed = new List<string>();
        var enabledAI = UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>().Where(ai => ai.enabled).ToArray();
        foreach (var ai in enabledAI) ai.enabled = false;
        void Controls(SwimmerIdentity active)
        {
            Require(selection.ActivePlayer == active && camera.Target == active.Motor, "Control and camera follow the actual carrier.");
            foreach (var player in roster.FieldPlayers)
            {
                bool selected = player == active;
                Require(player.GetComponent<SwimmerInput>().enabled == selected && player.GetComponent<BallInteractionInput>().enabled == selected,
                    "Exactly one actor has human input.");
                Require(player.transform.Find("SelectionRing").gameObject.activeSelf == selected, "Ring matches selection.");
            }
        }
        try
        {
            var match = UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
            while (match.IsRestarting) await Advance(0.05f);
            roster.ResetPositions();
            ball.ResetForRestart(new Vector3(0f, ball.SurfaceHeight, 1.65f));
            selection.Select(first);
            await Advance(0.1f);
            int initial = changes;
            await Tap(keyboard, Key.E);
            Require(ball.Owner == first.Interaction && changes == initial + 1, "Real pickup publishes one ownership change.");
            await Tap(keyboard, Key.B, 0.04f);
            Require(ball.Owner == null && selection.ActivePlayer == first, "Pass attempt/loose travel does not select recipient.");
            await Until(() => ball.Owner == second.Interaction, 3f);
            Controls(second);
            Vector3 oldPosition = first.Motor.Position;
            Press(keyboard, Key.D);
            await Advance(0.2f);
            Press(keyboard);
            await Advance(0.3f);
            Require(Vector3.Distance(first.Motor.Position, oldPosition) < 0.01f && second.Motor.Position.x > second.StartPosition.position.x,
                "WASD after reception controls only recipient.");
            second.Motor.ResetPose(second.StartPosition.position, second.StartPosition.rotation);
            await Advance(0.1f);
            await Tap(keyboard, Key.B);
            await Until(() => ball.Owner == first.Interaction, 3f);
            Controls(first);
            passed.Add("A-to-B reception and return pass auto-select only on acquisition, synchronizing ring/camera/input");

            int heldChanges = changes;
            await Tap(keyboard, Key.C);
            Controls(second);
            await Advance(0.3f);
            Require(ball.Owner == first.Interaction && selection.ActivePlayer == second && changes == heldChanges,
                "C preserves possession and is not undone by a per-frame carrier poll.");
            ball.ResetForRestart(first.Motor.Position + Vector3.forward * 1.65f);
            await Advance(0.1f);
            Require(first.Interaction.TryCollect(), "Inactive friendly uses legitimate pickup rules.");
            Controls(first);
            passed.Add("Manual switching remains separate; subsequent inactive-friendly pickup selects once");

            selection.Select(second);
            selection.SetInputLocked(true);
            ball.ResetForRestart(first.Motor.Position + Vector3.forward * 1.65f);
            Require(first.Interaction.TryCollect(), "Exercise an acquisition notification while selection is locked.");
            Require(selection.ActivePlayer == second, "Locked selection ignores ownership notifications.");
            ball.ResetForRestart(new Vector3(0f, ball.SurfaceHeight, 1.65f));
            selection.SetInputLocked(false);
            await Advance(0.2f);
            Require(selection.ActivePlayer == second, "Unlock/reset cannot replay a stale possession event.");
            passed.Add("Stoppage lock and cleared ownership do not queue delayed switching");

            roster.ResetPositions();
            selection.Select(first);
            ball.ResetForRestart(new Vector3(0f, ball.SurfaceHeight, 1.65f));
            await Advance(0.1f);
            await Tap(keyboard, Key.E);
            await Tap(keyboard, Key.B, 0.04f);
            second.Motor.ResetPose(new Vector3(8f, 0f, -6f), Quaternion.identity);
            await Advance(0.8f);
            Require(ball.Owner == null && selection.ActivePlayer == first, "A failed pass never triggers premature switching.");
            passed.Add("Missed pass remains loose with no automatic selection change");
            return new { passed, possessionChanges = changes, frameCount = Time.frameCount };
        }
        finally
        {
            foreach (var keeper in isolatedKeepers) if (keeper != null) keeper.enabled = true;
            ball.PossessionChanged -= Changed;
            InputSystem.RemoveDevice(keyboard);
            selection.SetInputLocked(false);
            roster.ResetPositions();
            ball.ResetForRestart(new Vector3(0f, ball.SurfaceHeight, 1.65f));
            selection.Select(first);
            foreach (var ai in enabledAI) if (ai != null) ai.enabled = true;
        }
    }
    private static void Press(Keyboard keyboard, params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
    private static async Task Tap(Keyboard keyboard, Key key, float duration = 0.08f)
    {
        Press(keyboard, key); await Advance(duration); Press(keyboard); await Advance(0.02f);
    }
    private static async Task Advance(float seconds)
    {
        float start = Time.fixedTime; var timer = Stopwatch.StartNew();
        while (Time.fixedTime - start < seconds)
        {
            if (timer.Elapsed.TotalSeconds > 8f) throw new TimeoutException("Physics stopped advancing.");
            await Task.Yield();
        }
    }
    private static async Task Until(Func<bool> condition, float timeout)
    {
        var timer = Stopwatch.StartNew();
        while (!condition())
        {
            if (timer.Elapsed.TotalSeconds > timeout) throw new TimeoutException("Expected possession did not arrive.");
            await Task.Yield();
        }
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
