using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

public static class CleanFieldExtensions
{
    public static object Apply()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/Scenes/OlympicMatchArena.unity" || scene.isDirty) throw new InvalidOperationException("Open saved production arena.");
        var root = GameObject.Find("Continuous Competition Ropes and Reentry Rectangles");
        var edits = new Dictionary<MeshFilter, Mesh>();
        int removed = 0;
        foreach (var f in root.GetComponentsInChildren<MeshFilter>())
        {
            var mesh = f.sharedMesh;
            var vertices = mesh.vertices;
            var indices = mesh.triangles;
            var keep = new List<int>();
            for (int i = 0; i < indices.Length; i += 3)
            {
                var a = vertices[indices[i]];
                var b = vertices[indices[i + 1]];
                var c = vertices[indices[i + 2]];
                bool strip = Mathf.Abs(a.y + .025f) < .0001f && a.y == b.y && a.y == c.y &&
                    Mathf.Abs(Mathf.Max(a.x,b.x,c.x)-Mathf.Min(a.x,b.x,c.x)-2.55f)<.001f &&
                    Mathf.Abs(a.z)<11f;
                if (strip) { removed++; continue; }
                keep.Add(indices[i]); keep.Add(indices[i+1]); keep.Add(indices[i+2]);
            }
            if (keep.Count == indices.Length) continue;
            var copy = UnityEngine.Object.Instantiate(mesh);
            copy.triangles = keep.ToArray();
            edits.Add(f,copy);
        }
        if (removed == 0) return "Already clean";
        if (removed != 28 || edits.Count != 3) throw new Exception("Unexpected geometry: "+removed);
        foreach (var entry in edits)
        {
            var original = AssetDatabase.GetAssetPath(entry.Key.sharedMesh);
            var path = original.Replace(".asset"," M22.asset");
            AssetDatabase.CreateAsset(entry.Value,path);
            entry.Key.sharedMesh = entry.Value;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed");
        AssetDatabase.SaveAssets();
        return removed;
    }
}
