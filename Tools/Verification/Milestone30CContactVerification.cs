using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Hub;
using ToduStudios.WaterPolo.WaterVisuals;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class Milestone30CContactVerification
{
    private const string Output = "docs/Evidence/Milestone30C";

    public static async Task<object> Run()
    {
        var match = Object.FindAnyObjectByType<PrototypeMatch>();
        var roster = Object.FindAnyObjectByType<PrototypeRoster>();
        var selection = Object.FindAnyObjectByType<PlayerSelection>();
        var ball = Object.FindAnyObjectByType<BallController>();
        Check(Application.isPlaying && match != null && !HubMatchBridge.Active, "Direct isolated arena required.");
        Check(new SerializedObject(roster).FindProperty("resolvePlayerContact").boolValue, "Saved contact option must be on.");
        var intro = Object.FindAnyObjectByType<PreMatchPresentation>();
        if (intro != null && intro.Active) intro.SkipAll();
        float timeout = Time.realtimeSinceStartup + 16f;
        while (!match.IsPlaying || match.AwaitingOpeningPossession)
        {
            Check(Time.realtimeSinceStartup < timeout, "Opening sprint");
            await Awaitable.NextFrameAsync();
        }

        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        Directory.CreateDirectory(Output);
        var view = Object.FindAnyObjectByType<ArenaDeveloperCamera>();
        var lens = view.GetComponent<Camera>();
        var waypoint = view.viewpoints[2];
        Vector3 eye = waypoint.position;
        Quaternion look = waypoint.rotation;
        float fieldOfView = lens.fieldOfView;
        var brains = Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>()
            .Concat(Object.FindObjectsByType<PrototypeGoalkeeper>()).Where(b => b.enabled).ToArray();
        var input = roster.Members.Select(p => p.GetComponent<SwimmerInput>()).Where(p => p != null).ToArray();
        var centre = roster.FieldPlayers.First(p => p.Team == selection.HumanTeam && p.FieldPosition == FieldPositionRole.CentreForward);
        var teammate = roster.FieldPlayers.First(p => p.Team == centre.Team && p != centre);
        var defender = roster.FieldPlayers.First(p => p.Team != centre.Team && p.FieldPosition == FieldPositionRole.CentreBack);
        var cases = new List<object>();
        void Frame(Vector3 target, Vector3 offset)
        {
            waypoint.position = target + offset;
            waypoint.LookAt(target);
            lens.fieldOfView = 46f;
            view.SetView(2);
        }
        void Stage(SwimmerIdentity other, Vector3 a, Vector3 b, Vector3 faceA, Vector3 faceB)
        {
            ball.ResetForRestart(new Vector3(6f, 0f, 9f));
            foreach (var player in roster.Members)
                player.Motor.ResetPose(new Vector3(player.Team == centre.Team ? -5f : 5f, 0f, player.CapNumber - 5f), Quaternion.identity);
            selection.Select(centre);
            foreach (var controls in input) controls.enabled = false;
            centre.Motor.ResetPose(a, Quaternion.LookRotation(faceA));
            other.Motor.ResetPose(b, Quaternion.LookRotation(faceB));
        }
        async Task<Vector3> Drive(SwimmerIdentity actor, SwimmerIdentity other, Vector3 a, Vector3 b, int steps,
            string evidence = null)
        {
            float min = float.PositiveInfinity, peakSpeed = 0f, minOrder = float.PositiveInfinity;
            for (int i = 0; i < steps; i++)
            {
                actor.Motor.SetMoveInput(new Vector2(a.x, a.z));
                other.Motor.SetMoveInput(new Vector2(b.x, b.z));
                await Awaitable.FixedUpdateAsync();
                Vector3 gap = Vector3.ProjectOnPlane(other.Motor.Position - actor.Motor.Position, Vector3.up);
                min = Mathf.Min(min, gap.magnitude);
                minOrder = Mathf.Min(minOrder, Vector3.Dot(gap, a));
                peakSpeed = Mathf.Max(peakSpeed, Mathf.Max(actor.Motor.Velocity.magnitude, other.Motor.Velocity.magnitude));
                if (evidence != null && (i == 15 || i == 29 || i == 45))
                    await Screenshot(evidence + "-" + (i + 1));
            }
            return new Vector3(min, peakSpeed, minOrder);
        }
        try
        {
            foreach (var brain in brains) brain.enabled = false;
            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "WaterPolo.SwimmerContact", 120);

            foreach (var opponent in new[] { teammate, defender })
            {
                Stage(opponent, Vector3.back * 1.32f, Vector3.forward * 1.32f, Vector3.forward, Vector3.back);
                bool isOpponent = opponent == defender;
                Frame(Vector3.zero, isOpponent ? new Vector3(2.9f, .88f, 0f) : new Vector3(.01f, 5.2f, -1.35f));
                var result = await Drive(centre, opponent, Vector3.forward, Vector3.back, 51,
                    isOpponent ? "Contact-Centre-Side" : "Contact-Teammates-Above");
                Check(result.z > .70f && result.x < 1.12f && result.y < 6.4f, $"Head-on separation/order {result}");
                cases.Add(new { scenario = isOpponent ? "Centre forward vs centre back" : "Teammate head-on",
                    gap = result.x, peakSpeed = result.y, order = result.z });
            }

            Stage(defender, new Vector3(.50f, 0f, -1.6f), Vector3.zero, Vector3.forward, Vector3.right);
            var slide = await Drive(centre, defender, Vector3.forward, Vector3.zero, 55);
            Check(slide.x > .51f && centre.Motor.Position.z > .30f && slide.y < 6.4f,
                $"Offset shoulder contact should slide. gap {slide.x} position {centre.Motor.Position}");
            cases.Add(new { scenario = "Shoulder slide", gap = slide.x, exitZ = centre.Motor.Position.z, peakSpeed = slide.y });
            Stage(defender, Vector3.back * 1.8f, Vector3.left * 1.8f, Vector3.forward, Vector3.right);
            var cross = await Drive(centre, defender, Vector3.forward, Vector3.right, 60);
            Check(cross.x > .45f && centre.Motor.Position.z > 0f && defender.Motor.Position.x > 0f,
                $"Crossing swimmers cleared one another, gap {cross.x} positions {centre.Motor.Position} / {defender.Motor.Position}");
            cases.Add(new { scenario = "Crossing paths", gap = cross.x, peakSpeed = cross.y });

            Vector3 wall = Vector3.right * 8.9f;
            Stage(defender, wall + Vector3.left * 2.25f, wall, Vector3.right, Vector3.left);
            var stamina = centre.GetComponent<SwimmerStamina>();
            stamina.ResetStamina();
            stamina.RequestSprint(true);
            var sprint = await Drive(centre, defender, Vector3.right, Vector3.zero, 62);
            Check(sprint.z > .63f && sprint.x < .88f && sprint.y > 4.1f && sprint.y < 6.15f && defender.Motor.Position.x <= 8.91f,
                $"Sprint held its bounds and did not cross: {sprint}, defender {defender.Motor.Position}");
            cases.Add(new { scenario = "Sprint into stationary defender at side limit", gap = sprint.x,
                peakSpeed = sprint.y, defenderX = defender.Motor.Position.x });
            centre.Motor.StopMotion();
            defender.Motor.StopMotion();

            Stage(defender, Vector3.back * 1.74f, Vector3.forward * 1.74f, Vector3.forward, Vector3.back);
            ball.ResetForRestart(Vector3.zero);
            var contest = await Drive(centre, defender, Vector3.forward, Vector3.back, 30);
            Check(ball.Owner == centre.Interaction || ball.Owner == defender.Interaction, "One contacting swimmer recovered the physical ball.");
            cases.Add(new { scenario = "Contested loose ball", gap = contest.x, owner = ball.Owner.name });
            var values = recorder.Valid ? recorder.ToArray() : Array.Empty<ProfilerRecorderSample>();
            return new { cases, marker = "WaterPolo.SwimmerContact", samples = values.Length,
                averageMicroseconds = values.Length > 0 ? values.Sum(s => (double)s.Value) / Math.Max(1d, values.Sum(s => s.Count)) / 1000d : (double?)null,
                peakFrameMicroseconds = values.Length > 0 ? values.Max(s => s.Value) / 1000d : (double?)null };
        }
        finally
        {
            foreach (var brain in brains) if (brain != null) brain.enabled = true;
            if (selection != null) selection.RefreshEligibility();
            waypoint.SetPositionAndRotation(eye, look);
            lens.fieldOfView = fieldOfView;
            view.SetView(-1);
        }
    }

    private static async Task Screenshot(string name)
    {
        await Awaitable.EndOfFrameAsync();
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        try { File.WriteAllBytes(Path.Combine(Output, name + ".png"), texture.EncodeToPNG()); }
        finally { Object.Destroy(texture); }
    }

    private static void Check(bool condition, string issue)
    {
        if (!condition) throw new InvalidOperationException(issue);
    }
}
