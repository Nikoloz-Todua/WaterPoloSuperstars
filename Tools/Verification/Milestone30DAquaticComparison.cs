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

public static class Milestone30DAquaticComparison
{
    public static Task<object> Before() => Capture("30C");
    public static Task<object> After() => Capture("30D");

    private static async Task<object> Capture(string version)
    {
        var match = Object.FindAnyObjectByType<PrototypeMatch>();
        var ball = Object.FindAnyObjectByType<BallController>();
        var camera = Object.FindAnyObjectByType<ArenaDeveloperCamera>();
        if (!Application.isPlaying || match == null || HubMatchBridge.Active) throw new InvalidOperationException("Use the isolated underwater lab.");
        float deadline = Time.realtimeSinceStartup + 19f;
        while (camera.ViewIndex != 2 || !match.IsPlaying || match.AwaitingOpeningPossession)
        {
            if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("Underwater lab staging.");
            await Awaitable.NextFrameAsync();
        }
        var player = Object.FindAnyObjectByType<PlayerSelection>().ActivePlayer;
        var visual = player.GetComponentInChildren<RiggedSwimmerVisual>();
        var animator = visual.Animator;
        var input = player.GetComponent<SwimmerInput>();
        var updatePose = typeof(RiggedSwimmerVisual).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        var lens = camera.GetComponent<Camera>();
        var world = player.Motor.Position;
        var rotation = Quaternion.LookRotation(player.Motor.Facing);
        var water = Object.FindAnyObjectByType<WaterInteractionEffects>();
        var anchor = camera.viewpoints[2];
        var initialEye = anchor.position;
        var initialLook = anchor.rotation;
        float initialFov = lens.fieldOfView;
        var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        HumanBodyBones[] joints = { HumanBodyBones.Hips, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg,
            HumanBodyBones.LeftFoot, HumanBodyBones.LeftToes, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg,
            HumanBodyBones.RightFoot, HumanBodyBones.RightToes };
        var bones = joints.Select(animator.GetBoneTransform).ToArray();
        var results = new List<object>();
        string output = "Logs/Milestone30D/Swim-" + version;
        Directory.CreateDirectory(output);
        input.enabled = false;
        player.Motor.ResetPose(new Vector3(0f, world.y, 0f), Quaternion.identity);
        ball.ResetForRestart(Vector3.forward * 8.5f);
        await Awaitable.NextFrameAsync();
        visual.enabled = false;
        animator.speed = 0f;
        lens.fieldOfView = 48f;
        int state = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        try
        {
            foreach (var mode in new[] { "Crawl", "Sprint", "Tread" })
            {
                float locomotion = mode == "Crawl" ? 1f : mode == "Sprint" ? 2f : 0f;
                for (int i = 0; i <= 4; i++)
                {
                    float phase = i * .25f;
                    animator.SetFloat("Move", locomotion);
                    animator.Play(state, 0, phase);
                    animator.Update(0f);
                    var humanoid = bones.Select(b => Pack(b.localRotation)).ToArray();
                    updatePose.Invoke(visual, null);
                    var final = bones.Select(b => Pack(b.localRotation)).ToArray();
                    results.Add(new { mode, phase, humanoid, final,
                        joints = bones.Select(b => new[] { b.position.x, b.position.y, b.position.z }).ToArray() });
                    var views = mode == "Crawl" ? new[] { "Under", "Side", "Rear", "Front", "Above" } : new[] { "Under" };
                    foreach (string view in views)
                    {
                        Vector3 focus = hips.position + (mode == "Tread" ? Vector3.up * .06f : Vector3.back * .11f);
                        Vector3 offset = view switch
                        {
                            "Side" => new Vector3(2.30f, -.06f, 0f),
                            "Rear" => new Vector3(.26f, -.16f, -2.15f),
                            "Front" => new Vector3(.31f, -.10f, 2.30f),
                            "Above" => new Vector3(.70f, 2.47f, -.34f),
                            _ => new Vector3(1.55f, -.74f, -1.27f)
                        };
                        anchor.position = focus + offset;
                        anchor.LookAt(focus);
                        await Awaitable.EndOfFrameAsync();
                        var frame = ScreenCapture.CaptureScreenshotAsTexture();
                        try { File.WriteAllBytes($"{output}/{mode}-{view}-{i}.png", frame.EncodeToPNG()); }
                        finally { Object.Destroy(frame); }
                        await Awaitable.NextFrameAsync();
                    }
                }
            }
            File.WriteAllText(output + "/poses.json", Newtonsoft.Json.JsonConvert.SerializeObject(results));
            return new { samples = results.Count, output, phases = "0, 25, 50, 75, 100 percent", water = water != null ? water.Quality.ToString() : null };
        }
        finally
        {
            animator.speed = 1f;
            visual.enabled = true;
            input.enabled = true;
            player.Motor.ResetPose(world, rotation);
            anchor.SetPositionAndRotation(initialEye, initialLook);
            lens.fieldOfView = initialFov;
            camera.SetView(-1);
        }
    }

    private static float[] Pack(Quaternion q) => new[] { q.x, q.y, q.z, q.w };
}
