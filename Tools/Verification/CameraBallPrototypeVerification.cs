// Run via Unity Pipeline while PrototypePool is the only scene in Play Mode:
// unity command script run --file Tools/Verification/CameraBallPrototypeVerification.cs --entry CameraBallPrototypeVerification.Run
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

public static class CameraBallPrototypeVerification
{
    public static async Task<object> Run()
    {
        // Original field-player fixture: keeper behavior is exercised by the dedicated keeper suites.
        var isolatedKeepers = UnityEngine.Object.FindObjectsByType<PrototypeGoalkeeper>().Where(k => k.enabled).ToArray();
        foreach (var keeper in isolatedKeepers) keeper.enabled = false;
        var runningMatch = UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
        while (runningMatch != null && runningMatch.IsRestarting) await Advance(0.05f);
        Require(Application.isPlaying && SceneManager.sceneCount == 1 && SceneManager.GetActiveScene().name == "PrototypePool", "Open PrototypePool alone in Play Mode.");
        Require(UnityEngine.Object.FindObjectsByType<BallController>().Length == 1, "Exactly one ball.");
        PlayerSelection selection = UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
        PrototypeRoster roster = UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
        Require(UnityEngine.Object.FindObjectsByType<SwimmerMotor>().Length == (roster != null ? roster.Members.Count : 1), "Expected roster-sized swimmer count.");
        if (selection != null) selection.Select(roster.FieldPlayers[0]);
        SwimmerMotor motor = selection != null ? selection.ActivePlayer.Motor : UnityEngine.Object.FindAnyObjectByType<SwimmerMotor>();
        SwimmerBallInteraction interaction = motor.GetComponent<SwimmerBallInteraction>();
        BallController ball = UnityEngine.Object.FindAnyObjectByType<BallController>();
        PoolBounds bounds = UnityEngine.Object.FindAnyObjectByType<PoolBounds>();
        GameplayCamera camera = Camera.main.GetComponent<GameplayCamera>();
        Rigidbody swimmerBody = motor.GetComponent<Rigidbody>();
        Rigidbody ballBody = ball.GetComponent<Rigidbody>();
        SphereCollider ballCollider = ball.GetComponent<SphereCollider>();
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
        GameObject competitor = null;
        var passed = new List<string>();
        Vector3 start = swimmerBody.position;
        Quaternion rotation = swimmerBody.rotation;
        Vector3 ballStart = ballBody.position;
        var enabledAI = UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>().Where(ai => ai.enabled).ToArray();
        foreach (var ai in enabledAI) ai.enabled = false;
        try
        {
            var match = UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
            while (match != null && match.IsRestarting) await Advance(0.05f);
            roster.ResetPositions(); ball.ResetForRestart(new Vector3(0f, ball.SurfaceHeight, 1.65f));
            selection.Select(roster.FieldPlayers[0]); await Advance(0.12f); camera.SnapToTarget();
            start = swimmerBody.position; rotation = swimmerBody.rotation; ballStart = ballBody.position;
            ResetBall(ball, ballBody, motor.transform.position + Vector3.forward * 5f);
            await Advance(0.1f);
            await Tap(keyboard, Key.Space);
            Require(ball.State == BallState.Free && ballBody.linearVelocity.magnitude < 0.01f, "Space without possession does nothing.");
            await Tap(keyboard, Key.E);
            Require(ball.State == BallState.Free && ball.Owner == null, "Out-of-range pickup is rejected.");
            passed.Add("Empty shooting and out-of-range E rejected");

            ResetBall(ball, ballBody, motor.transform.position + Vector3.forward * 1.65f);
            await Advance(0.1f);
            await Tap(keyboard, Key.E);
            await Advance(0.2f);
            Require(ball.State == BallState.Held && ball.Owner == interaction && interaction.HasBall, "E collects a nearby ball.");
            Require(ballBody.isKinematic && !ballCollider.enabled, "Held ball cannot collide with owner.");
            Require(Vector3.Distance(ball.transform.position, interaction.CarryAnchor.position) < 0.06f, "Held ball reaches the hand anchor.");
            competitor = new GameObject("TemporaryOwnershipClaimant");
            SwimmerBallInteraction challenger = competitor.AddComponent<SwimmerBallInteraction>();
            SerializedObject challengerData = new SerializedObject(challenger);
            challengerData.FindProperty("carryAnchor").objectReferenceValue = competitor.transform;
            challengerData.ApplyModifiedPropertiesWithoutUndo();
            competitor.transform.position = motor.transform.position;
            Require(!ball.TryCollect(challenger) && ball.Owner == interaction, "A second valid claimant cannot take the held ball.");
            passed.Add("Nearby E pickup, hand attachment and exclusive ownership");

            Vector3 beforeMove = motor.transform.position;
            Vector3 cameraBefore = camera.transform.position;
            Press(keyboard, Key.W);
            await Advance(0.9f);
            Require(motor.transform.position.z > beforeMove.z + 1f, "Movement still works while carrying.");
            Require(Vector3.Distance(ball.transform.position, interaction.CarryAnchor.position) < 0.3f, "Ball follows while swimming.");
            Require(Vector3.Distance(camera.transform.position, cameraBefore) > 0.5f, "Camera follows movement.");
            Require(camera.Focus.z > motor.transform.position.z + 0.1f, "Forward camera lead is visible during travel.");
            Press(keyboard, Key.D);
            await Advance(0.7f);
            Press(keyboard);
            await Advance(0.5f);
            Vector3 facing = motor.transform.forward;
            await Tap(keyboard, Key.Space, 0.04f);
            Require(ball.State == BallState.Released && ball.Owner == null && !interaction.HasBall, "Space releases ownership.");
            Require(!ballBody.isKinematic && ballCollider.enabled, "Released ball uses 3D physics.");
            Require(ballBody.linearVelocity.magnitude > 9f && Vector3.Dot(ballBody.linearVelocity.normalized, facing) > 0.99f, "Shot follows swimmer facing.");
            Require(!interaction.TryCollect(), "Immediate re-collection is blocked.");
            Require(Mathf.Abs(ballBody.position.y - ball.SurfaceHeight) < 0.0001f, "Shot stays at the waterline.");
            passed.Add("Carry movement, forward camera lead and facing-direction Space shot");

            await Advance(0.7f);
            Require(ball.State == BallState.Free && ball.CanCollect, "Released ball becomes collectible after cooldown.");
            ballBody.linearVelocity = Vector3.zero;
            ballBody.position = motor.transform.position + motor.transform.forward * 1.65f;
            await Advance(0.1f);
            await Tap(keyboard, Key.E);
            Require(interaction.HasBall, "E can collect the released ball again.");
            interaction.enabled = false;
            Require(ball.Owner == null && ball.State == BallState.Released, "Disabling the carrier drops ownership.");
            interaction.enabled = true;
            await Advance(0.7f);
            passed.Add("Cooldown, re-collection and carrier-disable cleanup");

            foreach (Vector3 outward in new[] { Vector3.left, Vector3.right, Vector3.back, Vector3.forward })
            {
                Vector3 edge = bounds.ClampPosition(bounds.transform.position + outward * 10000f, ball.Radius);
                if (outward.z != 0f) edge.x = 4f; // Exercise the closed end wall outside the Milestone 3 goal mouth.
                ResetBall(ball, ballBody, edge - outward * 0.1f);
                ballBody.linearVelocity = outward * 80f;
                await Advance(0.2f);
                Vector3 clamped = bounds.ClampPosition(ballBody.position, ball.Radius);
                Require(Mathf.Abs(ballBody.position.x - clamped.x) < 0.001f && Mathf.Abs(ballBody.position.z - clamped.z) < 0.001f,
                    "Fast ball stays inside pool bounds.");
                Require(Mathf.Abs(ballBody.position.y - ball.SurfaceHeight) < 0.0001f, "Free ball does not sink.");
            }
            passed.Add("High-speed ball containment and floating");

            // Follow-only settling is measured with the ball parked outside its framing influence.
            // The earlier containment checks leave it moving, which legitimately biases the camera.
            ResetBall(ball, ballBody, new Vector3(-8f, ball.SurfaceHeight, -12f));
            Press(keyboard);
            motor.enabled = false;
            swimmerBody.position = Vector3.zero;
            motor.enabled = true;
            await Advance(0.4f);
            camera.SnapToTarget();
            Vector3 initialFocus = camera.Focus;
            swimmerBody.position = Vector3.forward * 3f;
            await Advance(0.04f);
            float partialFollow = camera.Focus.z - initialFocus.z;
            Require(partialFollow > 0f && partialFollow < 2.9f, "Camera follows smoothly rather than snapping.");
            await Advance(0.9f);
            Require(Mathf.Abs(camera.Focus.z - 3f) < 0.1f, "Camera settles onto the swimmer.");
            foreach (float x in new[] { -bounds.Width, bounds.Width })
            foreach (float z in new[] { -bounds.Length, bounds.Length })
            {
                swimmerBody.position = bounds.ClampPosition(new Vector3(x, 0f, z), 1.1f);
                await Advance(0.8f);
                Vector3 viewport = Camera.main.WorldToViewportPoint(motor.transform.position);
                Require(viewport.z > 0f && viewport.x > 0.03f && viewport.x < 0.97f && viewport.y > 0.03f && viewport.y < 0.97f,
                    "Bounded camera keeps swimmer visible at each pool corner.");
            }
            passed.Add("Smooth camera settling and swimmer visibility at all corners");
            return new { passed, partialFollow, cameraDistance = Vector3.Distance(camera.transform.position, motor.transform.position), frameCount = Time.frameCount };
        }
        finally
        {
            foreach (var keeper in isolatedKeepers) if (keeper != null) keeper.enabled = true;
            InputSystem.RemoveDevice(keyboard);
            if (competitor != null)
                UnityEngine.Object.DestroyImmediate(competitor);
            motor.enabled = false;
            swimmerBody.position = start;
            swimmerBody.rotation = rotation;
            motor.enabled = true;
            ResetBall(ball, ballBody, ballStart);
            camera.SnapToTarget();
            foreach (var ai in enabledAI) if (ai != null) ai.enabled = true;
        }
    }

    private static void ResetBall(BallController ball, Rigidbody body, Vector3 position)
    {
        ball.enabled = false;
        body.position = new Vector3(position.x, ball.SurfaceHeight, position.z);
        ball.enabled = true;
    }

    private static void Press(Keyboard keyboard, params Key[] keys) => InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));

    private static async Task Tap(Keyboard keyboard, Key key, float duration = 0.1f)
    {
        Press(keyboard, key);
        await Advance(duration);
        Press(keyboard);
        await Advance(0.02f);
    }

    private static async Task Advance(float seconds)
    {
        float start = Time.fixedTime;
        Stopwatch watchdog = Stopwatch.StartNew();
        while (Time.fixedTime - start < seconds)
        {
            if (watchdog.Elapsed.TotalSeconds > 8)
                throw new TimeoutException("Physics stopped advancing during verification.");
            await Task.Yield();
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
