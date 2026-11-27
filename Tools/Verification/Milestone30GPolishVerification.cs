using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Hub;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEngine;
using Object = UnityEngine.Object;

public static class Milestone30GPolishVerification
{
    private const string Output = "Logs/Milestone30GPolish";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    public static Task<string> Before() => Swim("Before", false);
    public static Task<string> After() => Swim("After", true);
    public static Task<string> SustainedLeading() => Swim("Leading", true, false);

    private static async Task<string> Swim(string label, bool advancing, bool samplePoses = true)
    {
        var match = Object.FindAnyObjectByType<PrototypeMatch>();
        var view = Object.FindAnyObjectByType<ArenaDeveloperCamera>();
        var selector = Object.FindAnyObjectByType<PlayerSelection>();
        var ball = Object.FindAnyObjectByType<BallController>();
        if (!Application.isPlaying || match == null || HubMatchBridge.Active)
            throw new InvalidOperationException("Start a short isolated swimming Test Lab.");
        await Until(() => view.ViewIndex == 2 && match.IsPlaying && !match.AwaitingOpeningPossession &&
            selector.ActivePlayer.Motor.Position.sqrMagnitude < .12f, "Centre-pool staging");
        var athlete = selector.ActivePlayer;
        var rig = athlete.GetComponentInChildren<RiggedSwimmerVisual>();
        var animator = rig.Animator;
        var input = athlete.GetComponent<SwimmerInput>();
        var stamina = athlete.GetComponent<SwimmerStamina>();
        var ring = athlete.transform.Find("SelectionRing");
        var flutter = typeof(RiggedSwimmerVisual).GetField("flutter", Private).GetValue(rig);
        var bind = (Quaternion[])flutter.GetType().GetField("bind", Private).GetValue(flutter);
        var joints = (Transform[])flutter.GetType().GetField("joints", Private).GetValue(flutter);
        var late = typeof(RiggedSwimmerVisual).GetMethod("LateUpdate", Private);
        var records = new List<string>();
        input.enabled = false;
        var motor = athlete.Motor;
        Vector3 facing = motor.Facing;
        Vector2 direction = new Vector2(facing.x, facing.z);
        try
        {
            if (advancing)
            {
                motor.SetMoveInput(direction);
                await Until(() => rig.Locomotion >= .91f, "Live crawl");
                stamina.RequestSprint(true);
                await Until(() => rig.Locomotion > 1.2f, "Live sprint");
                stamina.RequestSprint(false);
                motor.SetMoveInput(Vector2.zero);
                await Until(() => rig.Locomotion < .08f, "Return to eggbeater");
                motor.ResetPose(Vector3.zero, Quaternion.LookRotation(facing));
                ball.ResetForRestart(facing * .84f);
                if (!athlete.Interaction.TryCollect()) throw new InvalidOperationException("Staged carrier could not collect.");
                motor.SetMoveInput(direction, true);
                await Until(() => ball.IsDribbling && animator.GetFloat("Move") > .95f, "Sustained ball-leading crawl");
                stamina.RequestSprint(true);
                await Until(() => ball.IsDribbling && animator.GetFloat("Move") > 1.62f, "Sustained ball-leading sprint");
                await Record("Live-Ball-Leading-Sprint", joints[0].position + Vector3.forward * .10f,
                    new Vector3(2.34f, -.32f, -.33f));
                stamina.RequestSprint(false);
                motor.SetMoveInput(Vector2.zero);
                await Until(() => ball.Owner == athlete.Interaction && !ball.IsDribbling && rig.Locomotion < .08f,
                    "Stationary protected gather");
                records.Add("Live crawl, sprint, eggbeater, ball-leading crawl/sprint and stationary gather passed.");
            }
            if (!samplePoses) return string.Join(Environment.NewLine, records);
            motor.ResetPose(Vector3.zero, Quaternion.identity);
            ball.ResetForRestart(Vector3.forward * 8f);
            await Awaitable.WaitForSecondsAsync(.20f);
            rig.enabled = false;
            if (ring != null) ring.gameObject.SetActive(false);
            animator.keepAnimatorStateOnDisable = true;
            animator.speed = 0f;
            int state = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
            foreach (var sample in new (string Name, float Move, float Phase)[] { ("Crawl", 1f, .25f), ("Sprint", 2f, .125f) })
            {
                animator.enabled = true;
                animator.SetFloat("Move", sample.Move);
                animator.Play(state, 0, sample.Phase);
                animator.Update(0f);
                float rawLeft = FootAlignment(2, 3), rawRight = FootAlignment(6, 7);
                late.Invoke(rig, null);
                animator.enabled = false;
                float left = FootAlignment(2, 3), right = FootAlignment(6, 7);
                records.Add($"{sample.Name} phase {sample.Phase}: Humanoid L/R {rawLeft:F3}/{rawRight:F3}, final L/R {left:F3}/{right:F3}");
                if (advancing && (left < .85f || right < .85f))
                    throw new InvalidOperationException(records.Last());
                var focus = joints[0].position + Vector3.back * .39f - Vector3.up * .03f;
                await Record($"{label}-{sample.Name}-Side", focus, new Vector3(1.92f, -.33f, -.46f));
                await Record($"{label}-{sample.Name}-Rear", focus, new Vector3(.68f, -.30f, -1.70f));
            }
            File.WriteAllLines($"{Output}/{label}-Swim.txt", records);
            return string.Join(Environment.NewLine, records);
        }
        finally
        {
            animator.enabled = true;
            animator.speed = 1f;
            rig.enabled = true;
            input.enabled = true;
            stamina.RequestSprint(false);
            motor.SetMoveInput(Vector2.zero);
            if (ring != null) ring.gameObject.SetActive(true);
        }

        float FootAlignment(int shin, int ankle)
        {
            Vector3 towardToe = (joints[ankle + 1].position - joints[ankle].position).normalized;
            Vector3 front = joints[shin].rotation * (Quaternion.Inverse(bind[shin]) * Vector3.forward);
            Vector3 top = joints[ankle].rotation * (Quaternion.Inverse(bind[ankle]) * Vector3.up);
            return Vector3.Dot(Vector3.ProjectOnPlane(front, towardToe).normalized,
                Vector3.ProjectOnPlane(top, towardToe).normalized);
        }
        async Task Record(string name, Vector3 focus, Vector3 offset)
        {
            view.GetComponent<Camera>().fieldOfView = 41f;
            var anchor = view.viewpoints[2];
            anchor.position = focus + offset;
            anchor.LookAt(focus);
            await Awaitable.NextFrameAsync();
            await Awaitable.EndOfFrameAsync();
            Directory.CreateDirectory(Output);
            var frame = ScreenCapture.CaptureScreenshotAsTexture();
            try { File.WriteAllBytes($"{Output}/{name}.jpg", frame.EncodeToJPG(91)); }
            finally { Object.Destroy(frame); }
        }
    }

    private static async Task Until(Func<bool> check, string description)
    {
        float deadline = Time.realtimeSinceStartup + 11f;
        while (!check())
        {
            if (Time.realtimeSinceStartup >= deadline) throw new TimeoutException(description);
            await Awaitable.NextFrameAsync();
        }
    }
}
