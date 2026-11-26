using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Hub;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class Milestone30CAquaticVerification
{
    public static Task<object> Before() => Sample("Before");
    public static Task<object> After() => Sample("After");
    public static async Task<object> Sample(string label)
    {
        var match = Object.FindAnyObjectByType<PrototypeMatch>();
        var ball = Object.FindAnyObjectByType<BallController>();
        var roster = Object.FindAnyObjectByType<PrototypeRoster>();
        var selection = Object.FindAnyObjectByType<PlayerSelection>();
        if (!Application.isPlaying || match == null || HubMatchBridge.Active) throw new InvalidOperationException("Run the direct match.");
        var intro = Object.FindAnyObjectByType<PreMatchPresentation>();
        if (intro != null && intro.Active) intro.SkipAll();
        else match.RestartMatch();
        float deadline = Time.realtimeSinceStartup + 16f;
        while (!match.IsPlaying || match.AwaitingOpeningPossession)
        {
            if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("Opening sprint");
            await Awaitable.NextFrameAsync();
        }
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        var brains = Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>()
            .Concat(Object.FindObjectsByType<PrototypeGoalkeeper>()).Where(b => b.enabled).ToArray();
        foreach (var brain in brains) brain.enabled = false;
        var player = roster.FieldPlayers.First(p => p.Team == selection.HumanTeam);
        selection.Select(player);
        var input = player.GetComponent<SwimmerInput>();
        input.enabled = false;
        var visual = player.GetComponentInChildren<RiggedSwimmerVisual>();
        var anim = visual.Animator;
        var stamina = player.GetComponent<SwimmerStamina>();
        var camera = Object.FindAnyObjectByType<ArenaDeveloperCamera>();
        var lens = camera.GetComponent<Camera>();
        var anchor = camera.viewpoints[2];
        var initialEye = anchor.position;
        var initialLook = anchor.rotation;
        float initialFov = lens.fieldOfView;
        var rest = anim.avatar.humanDescription.skeleton.ToDictionary(b => b.name, b => b.rotation);
        Quaternion RestRotation(Transform bone)
        {
            Quaternion value = Quaternion.identity;
            for (Transform joint = bone; joint != anim.transform && joint != null; joint = joint.parent)
                value = (rest.TryGetValue(joint.name, out var q) ? q : joint.localRotation) * value;
            return value;
        }
        var bones = new[] { HumanBodyBones.Hips, HumanBodyBones.Chest, HumanBodyBones.LeftUpperLeg,
            HumanBodyBones.RightUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg,
            HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot, HumanBodyBones.LeftToes, HumanBodyBones.RightToes }
            .Select(anim.GetBoneTransform).ToArray();
        var anterior = bones.Select(b => Quaternion.Inverse(RestRotation(b)) * Vector3.forward).ToArray();
        var dorsal = bones.Select(b => Quaternion.Inverse(RestRotation(b)) * Vector3.up).ToArray();
        var rows = new List<object>();
        try
        {
            foreach (var other in roster.Members.Where(p => p != player))
                other.Motor.ResetPose(new Vector3(other.Team == player.Team ? -7f : 7f, 0f, other.CapNumber - 5f), Quaternion.identity);
            foreach (var mode in new[] { "Crawl", "Sprint", "Leading", "Eggbeater" })
            {
                player.Motor.ResetPose(Vector3.back * 6f, Quaternion.identity);
                ball.ResetForRestart(mode == "Leading" ? Vector3.back * 5.15f : Vector3.right * 6f + Vector3.forward * 10f);
                if (mode == "Leading" && !player.Interaction.TryCollect()) throw new InvalidOperationException("Lead possession");
                stamina.ResetStamina();
                stamina.RequestSprint(mode == "Sprint");
                player.Motor.SetMoveInput(mode == "Eggbeater" ? Vector2.zero : Vector2.up);
                await Awaitable.WaitForSecondsAsync(.71f);
                if (mode == "Leading" && !ball.IsDribbling) throw new InvalidOperationException("Expected live leading");
                camera.SetView(2);
                lens.fieldOfView = 52f;
                var values = new List<object>();
                for (int i = 0; i < 16; i++)
                {
                    Vector3 focus = (bones[0].position + bones[1].position + bones[6].position + bones[7].position) * .25f;
                    Vector3 offset = i < 7 ? new Vector3(2.35f, -.08f, -.22f) :
                        i < 10 ? new Vector3(.55f, -.28f, -2.18f) : i < 13 ? new Vector3(.36f, 2.82f, -.28f) :
                        new Vector3(1.45f, -.72f, -.88f);
                    anchor.position = focus + offset;
                    anchor.LookAt(focus);
                    await Awaitable.EndOfFrameAsync();
                    Vector3 thigh = bones[4].position - bones[2].position;
                    Vector3 shin = bones[6].position - bones[4].position;
                    values.Add(new
                    {
                        phase = anim.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f, velocity = player.Motor.Velocity.magnitude,
                        frontDown = Enumerable.Range(0, 6).Select(j => Vector3.Dot(bones[j].TransformDirection(anterior[j]), Vector3.down)).ToArray(),
                        soles = Enumerable.Range(6, 2).Select(j => Vector3.Dot(bones[j].TransformDirection(dorsal[j]), Vector3.up)).ToArray(),
                        kneeAngle = Vector3.Angle(thigh, shin), kneeDown = Vector3.Dot(thigh.normalized - shin.normalized, Vector3.down),
                        toeRear = Vector3.Dot((bones[8].position - bones[6].position).normalized, Vector3.back)
                    });
                    if ((mode == "Crawl" && (i == 0 || i == 5 || i == 9 || i == 11 || i == 14)) ||
                        (mode != "Crawl" && (i == 2 || i == 14)))
                    {
                        var texture = ScreenCapture.CaptureScreenshotAsTexture();
                        try { File.WriteAllBytes($"docs/Evidence/Milestone30C/{label}-{mode}-{i:D2}.png", texture.EncodeToPNG()); }
                        finally { Object.Destroy(texture); }
                    }
                    await Awaitable.WaitForSecondsAsync(.085f);
                }
                rows.Add(new { mode, clips = anim.GetCurrentAnimatorClipInfo(0).Select(c => c.clip.name).ToArray(), values });
            }
            return new { mappings = bones.Select(b => new { b.name, rest = rest.ContainsKey(b.name) }).ToArray(), rows };
        }
        finally
        {
            player.Motor.StopMotion();
            selection.RefreshEligibility();
            foreach (var brain in brains) if (brain != null) brain.enabled = true;
            anchor.SetPositionAndRotation(initialEye, initialLook);
            lens.fieldOfView = initialFov;
            camera.SetView(-1);
        }
    }
}

