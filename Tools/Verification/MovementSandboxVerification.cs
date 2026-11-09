// Run through Unity Pipeline in Play Mode, with PrototypePool open alone:
// unity command script run --file Tools/Verification/MovementSandboxVerification.cs --entry MovementSandboxVerification.Run
// Kept outside Assets so this verification tool is never compiled into a player build.
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

public static class MovementSandboxVerification
{
    public static async Task<object> Run()
    {
        // Original field-player fixture: keeper behavior is exercised by the dedicated keeper suites.
        var isolatedKeepers = UnityEngine.Object.FindObjectsByType<PrototypeGoalkeeper>().Where(k => k.enabled).ToArray();
        foreach (var keeper in isolatedKeepers) keeper.enabled = false;
        var runningMatch = UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
        while (runningMatch != null && runningMatch.IsRestarting) await Advance(0.05f);
        Require(Application.isPlaying && SceneManager.sceneCount == 1 &&
            SceneManager.GetActiveScene().name == "PrototypePool", "Open PrototypePool alone in Play Mode.");
        PlayerSelection selection = UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
        if (selection != null) selection.Select(UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>().Members[0]);
        SwimmerMotor motor = selection != null ? selection.ActivePlayer.Motor : UnityEngine.Object.FindAnyObjectByType<SwimmerMotor>();
        SwimmerInput input = motor.GetComponent<SwimmerInput>();
        Rigidbody body = motor.GetComponent<Rigidbody>();
        PoolBounds bounds = UnityEngine.Object.FindAnyObjectByType<PoolBounds>();
        Vector3 originalPosition = body.position;
        Quaternion originalRotation = body.rotation;
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
        Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
        var passed = new List<string>();
        float straightSpeed = 0f;
        float diagonalSpeed = 0f;
        var enabledAI = UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>().Where(ai => ai.enabled).ToArray();
        foreach (var ai in enabledAI) ai.enabled = false;

        try
        {
            var match = UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
            while (match != null && match.IsRestarting) await Advance(0.05f);
            var roster = UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
            roster.ResetPositions();
            var ball = UnityEngine.Object.FindAnyObjectByType<BallController>();
            ball.ResetForRestart(new Vector3(0f, ball.SurfaceHeight, 1.65f));
            selection.Select(roster.FieldPlayers[0]); await Advance(0.12f);
            originalPosition = body.position; originalRotation = body.rotation;
            int frame = Time.frameCount;
            await Advance(0.1f);
            Require(Time.frameCount > frame, "Player loop advances.");
            Require(body.isKinematic && !body.useGravity, "Kinematic swimmer configuration.");
            Press(keyboard, Key.W);
            await Advance(0.08f);
            Require(motor.Velocity.magnitude > 0f && motor.Velocity.magnitude < motor.MaxSpeed,
                $"Movement accelerates rather than snapping to full speed. Velocity={motor.Velocity}, max={motor.MaxSpeed}, input={input.enabled}, selected={selection.ActivePlayer.PlayerId}, locked={selection.InputLocked}");
            await Advance(0.8f);
            straightSpeed = motor.Velocity.magnitude;
            Require(Mathf.Abs(straightSpeed - motor.MaxSpeed) < 0.03f && body.position.z > originalPosition.z,
                "W produces forward movement at configured maximum speed.");
            passed.Add("WASD input and acceleration");

            Press(keyboard, Key.W, Key.D);
            await Advance(0.8f);
            diagonalSpeed = motor.Velocity.magnitude;
            Require(Mathf.Abs(diagonalSpeed - straightSpeed) < 0.03f && motor.Velocity.x > 0f && motor.Velocity.z > 0f,
                "Diagonal movement has the same maximum speed as straight movement.");
            Require(Vector3.Dot(body.rotation * Vector3.forward, motor.Velocity.normalized) > 0.995f,
                "Swimmer faces travel direction.");
            passed.Add("Normalized diagonal speed and facing");

            Press(keyboard);
            await Advance(0.04f);
            Require(motor.Velocity.magnitude > 0f && motor.Velocity.magnitude < straightSpeed,
                "Release decelerates smoothly.");
            await Advance(0.5f);
            Require(motor.Velocity.magnitude < 0.001f, "Movement stops.");
            Vector3 stopped = body.position;
            await Advance(0.1f);
            Require(Vector3.Distance(body.position, stopped) < 0.001f, "No drift after stopping.");
            passed.Add("Smooth deceleration and no drift");

            Press(keyboard, Key.LeftArrow);
            await Advance(0.6f);
            Require(motor.Velocity.x < -motor.MaxSpeed + 0.03f, "Arrow keys move the swimmer.");
            passed.Add("Arrow-key input");
            Press(keyboard);
            InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(0.5f, 0f) });
            await Advance(0.6f);
            Require(motor.Velocity.x > 0.1f && motor.Velocity.magnitude < motor.MaxSpeed - 0.1f,
                "Analogue stick strength is preserved.");
            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            await Advance(0.5f);
            passed.Add("Analogue gamepad input");

            // Start near each edge to exercise integration without long travel waits.
            foreach (Key key in new[] { Key.A, Key.D, Key.S, Key.W })
            {
                Vector3 outward = key == Key.A ? Vector3.left : key == Key.D ? Vector3.right :
                    key == Key.S ? Vector3.back : Vector3.forward;
                Vector3 edge = bounds.ClampPosition(bounds.transform.position + outward * 10000f, 1.1f);
                motor.enabled = false;
                body.position = edge - outward * 0.2f;
                motor.enabled = true;
                Press(keyboard, key);
                await Advance(0.6f);
                Require(Vector3.Distance(body.position, edge) < 0.01f, "Stops at " + key + " boundary.");
                await Advance(0.2f);
                Require(Vector3.Distance(body.position, edge) < 0.01f, "Held input cannot cross " + key + " boundary.");
            }
            passed.Add("All four boundaries under sustained input");

            Press(keyboard);
            body.position += Vector3.up * 3f;
            await Advance(0.1f);
            Require(Mathf.Abs(body.position.y - bounds.Waterline) < 0.0001f, "Waterline is enforced.");
            passed.Add("Fixed waterline");

            Camera camera = Camera.main;
            await Advance(0.7f);
            Vector3 viewport = camera.WorldToViewportPoint(body.position);
            Require(viewport.z > 0f && viewport.x > 0f && viewport.x < 1f && viewport.y > 0f && viewport.y < 1f,
                "Camera contains the swimmer at the current aspect ratio.");
            passed.Add("Swimmer framing at current Game view aspect (Milestone 2 follow camera)");
            return new { passed, straightSpeed, diagonalSpeed, frameCount = Time.frameCount };
        }
        finally
        {
            foreach (var keeper in isolatedKeepers) if (keeper != null) keeper.enabled = true;
            InputSystem.RemoveDevice(keyboard);
            InputSystem.RemoveDevice(gamepad);
            input.enabled = false;
            motor.enabled = false;
            body.position = originalPosition;
            body.rotation = originalRotation;
            motor.enabled = true;
            input.enabled = true;
            foreach (var ai in enabledAI) if (ai != null) ai.enabled = true;
        }
    }

    private static void Press(Keyboard keyboard, params Key[] keys)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
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
