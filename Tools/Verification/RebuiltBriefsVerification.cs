using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo.Editor;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEngine;
using Object = UnityEngine.Object;

public static class RebuiltBriefsVerification
{
    const string Output = "Logs/Milestone30/Briefs/Unity";

    public static async Task<string> Run()
    {
        var preview = Object.FindAnyObjectByType<AthletePreviewCamera>();
        if (preview == null || !Application.isPlaying)
            throw new InvalidOperationException("Start Play Athlete Preview first.");
        var rig = preview.athlete;
        preview.framing = 2;
        preview.dry = true;
        preview.angle = 24;
        Directory.CreateDirectory(Output);
        await PauseForFrames(.12f);
        await Capture("Original-Standing");
        RebuiltBriefsPreview.Toggle();
        var suit = rig.transform.Find("Atlas Competition Briefs Preview").GetComponent<SkinnedMeshRenderer>();
        var skin = rig.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r => r.name == "AthleteBody");
        var suitBake = new Mesh();
        var skinBake = new Mesh();
        var lines = new List<string>();
        try
        {
            var body = WorldVertices(skin, skinBake);
            var garment = WorldVertices(suit, suitBake);
            var seam = new List<(int garment, int body)>();
            for (int i = 0; i < garment.Length; i++)
                for (int j = 0; j < body.Length; j++)
                    if ((garment[i] - body[j]).sqrMagnitude < .00011f * .00011f)
                    {
                        seam.Add((i, j));
                        break;
                    }
            if (seam.Count < 200)
                throw new InvalidOperationException($"Imported fabric expected at least 200 registered vertices. Found {seam.Count}.");
            lines.Add($"Imported triangles: {suit.sharedMesh.GetIndexCount(0) / 3}, vertices: {suit.sharedMesh.vertexCount}, matched seam vertices: {seam.Count}.");
            float Measure()
            {
                body = WorldVertices(skin, skinBake);
                garment = WorldVertices(suit, suitBake);
                return seam.Max(pair => Vector3.Distance(garment[pair.garment], body[pair.body]));
            }

            lines.Add($"Standing: {Measure() * 1000:F3} mm.");
            await Capture("Candidate-Standing-Front");
            preview.angle = -162;
            await Capture("Candidate-Standing-Rear");
            preview.angle = 36;
            preview.playbackRate = .55f;
            async Task Sample(string label)
            {
                float max = 0;
                for (int i = 0; i < 9; i++)
                {
                    await PauseForFrames(.090f);
                    max = Mathf.Max(max, Measure());
                    if (i == 2 || i == 6)
                        await Capture($"Candidate-{label}-{i:D2}");
                }
                lines.Add($"{label}: {max * 1000:F3} mm across 9 advancing samples.");
                if (max > .0015f)
                    throw new InvalidOperationException($"{label} exceeds the seam tolerance: {max * 1000:F3} mm.");
            }
            preview.dry = false;
            string[] modes = { "Eggbeater", "Freestyle", "Sprint", "Hold", "Pass", "Shot", "Block", "Save" };
            for (int i = 0; i < modes.Length; i++)
            {
                rig.previewState = i;
                await Sample(modes[i]);
            }
            preview.SelectDeckClip(preview.walk);
            await Sample("Walk");
            preview.SelectDeckClip(preview.run);
            await Sample("Run");
            return string.Join(Environment.NewLine, lines);
        }
        finally
        {
            File.WriteAllLines($"{Output}/Deformation.txt", lines);
            Object.Destroy(suitBake);
            Object.Destroy(skinBake);
            RebuiltBriefsPreview.Toggle();
        }
    }

    static Vector3[] WorldVertices(SkinnedMeshRenderer renderer, Mesh snapshot)
    {
        renderer.BakeMesh(snapshot);
        var vertices = snapshot.vertices;
        var transform = renderer.localToWorldMatrix;
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] = transform.MultiplyPoint3x4(vertices[i]);
        return vertices;
    }

    static async Task Capture(string label)
    {
        string path = Path.GetFullPath($"{Output}/{label}.png");
        ScreenCapture.CaptureScreenshot(path);
        await PauseForFrames(.065f);
    }

    static async Task PauseForFrames(float seconds)
    {
        float before = Time.realtimeSinceStartup;
        int frames = Time.frameCount;
        var deadline = DateTime.UtcNow.AddSeconds(8);
        while (Time.realtimeSinceStartup - before < seconds || Time.frameCount < frames + 2)
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException("The preview Game view did not advance.");
            await Task.Delay(12);
        }
    }
}
