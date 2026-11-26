using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Hub;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class Milestone30CDeckVerification
{
    public static async Task<object> Run()
    {
        var intro = Object.FindAnyObjectByType<PreMatchPresentation>();
        var match = Object.FindAnyObjectByType<PrototypeMatch>();
        Check(Application.isPlaying && intro != null && intro.Active && !HubMatchBridge.Active,
            "Enter a fresh direct OlympicMatchArena and run immediately.");
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        var seen = new HashSet<PreMatchStage>();
        seen.Add(intro.Stage);
        var windows = new Dictionary<PreMatchStage, float[]>
        {
            [PreMatchStage.TeamWalkout] = new[] { 1.48f, 1.64f, 1.79f },
            [PreMatchStage.FormalLineup] = new[] { 1f },
            [PreMatchStage.CaptainExchange] = new[] { .38f, .52f, 1.22f }
        };
        var capture = new HashSet<string>();
        var walkPitches = new List<float>();
        var standingPitches = new List<float>();
        const string Output = "docs/Evidence/Milestone30C";
        Directory.CreateDirectory(Output);
        float timeEnd = Time.realtimeSinceStartup + 32f;
        float initialClock = match.Clock.Remaining;
        while (intro.Active)
        {
            await Awaitable.EndOfFrameAsync();
            Check(Time.realtimeSinceStartup < timeEnd, "Ceremony deadline.");
            if (!intro.Active) break;
            var stage = intro.Stage;
            seen.Add(stage);
            Check(match.Clock.Remaining == initialClock && intro.CastCount == 14, "Ceremony is visual and clock stays held.");
            if (!windows.TryGetValue(stage, out var frames)) continue;
            var cast = GameObject.Find("Prematch visual cast");
            if (cast == null) continue;
            var pitchData = stage == PreMatchStage.FormalLineup ? standingPitches : walkPitches;
            foreach (var animator in cast.GetComponentsInChildren<Animator>())
            {
                foreach (var side in new[] { "l", "r" })
                {
                    Transform ankle = Find(animator.transform, "foot_" + side);
                    Transform toe = Find(animator.transform, "ball_" + side);
                    if (ankle == null || toe == null) continue;
                    Vector3 direction = toe.position - ankle.position;
                    pitchData.Add(Mathf.Atan2(direction.y, Vector3.ProjectOnPlane(direction, Vector3.up).magnitude) * Mathf.Rad2Deg);
                }
            }
            for (int i = 0; i < frames.Length; i++)
            {
                string name = "Deck-" + stage + "-" + (i + 1);
                if (intro.StageElapsed < frames[i] || !capture.Add(name)) continue;
                var texture = ScreenCapture.CaptureScreenshotAsTexture();
                try { File.WriteAllBytes(Path.Combine(Output, name + ".png"), texture.EncodeToPNG()); }
                finally { Object.Destroy(texture); }
            }
        }
        Check(seen.Count == 8, $"All eight stages expected, observed {string.Join(", ", seen)}");
        Check(capture.Count == 7, "Expected timed walkout, lineup and captain exchange capture frames.");
        Check(walkPitches.Count > 0 && standingPitches.Count > 0, "Expected live rendered athlete foot joints.");
        Check(walkPitches.Max() < 17f && standingPitches.Max() < 0f,
            $"Unnatural toe rise in deck sample: moving {walkPitches.Max():F2}, standing {standingPitches.Max():F2}");
        return new { stages = seen.Select(s => s.ToString()), frames = capture, walking = new { min = walkPitches.Min(), max = walkPitches.Max() },
            standing = new { min = standingPitches.Min(), max = standingPitches.Max() } };
    }

    private static Transform Find(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = Find(parent.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
