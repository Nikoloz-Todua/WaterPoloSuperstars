using System;
using System.IO;
using System.Linq;
using ToduStudios.WaterPolo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ArenaInspection
{
    public static object CollectEvidence()
    {
        Directory.CreateDirectory("Logs/Arena");
        foreach(string name in new[]{"First-Broadcast.png","First-Overview.png"})
        {
            string source="Assets/Logs/Arena/"+name;
            if(File.Exists(source))File.Copy(source,"Logs/Arena/"+name,true);
            if(AssetDatabase.LoadMainAssetAtPath(source)!=null)AssetDatabase.DeleteAsset(source);
        }
        // Only the two generated captures were created here. Remove their now-empty import folders.
        if(Directory.Exists("Assets/Logs/Arena") && Directory.GetFiles("Assets/Logs/Arena").Length==0)AssetDatabase.DeleteAsset("Assets/Logs/Arena");
        if(Directory.Exists("Assets/Logs") && Directory.GetFileSystemEntries("Assets/Logs").Length==0)AssetDatabase.DeleteAsset("Assets/Logs");
        return new { evidenceOutsideAssets=true };
    }
    public static object Inspect()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Inspect in saved Edit Mode.");
        for (int i=0;i<SceneManager.sceneCount;i++) if(SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save intended edits first.");
        Directory.CreateDirectory("Logs/Arena");
        Directory.CreateDirectory("Temp/ArenaRecovery");
        foreach(var path in new[]{"Assets/Scenes/PrototypePool.unity","Assets/Scenes/WaterVisualTest.unity","ProjectSettings/EditorBuildSettings.asset"})
            File.Copy(path,"Temp/ArenaRecovery/"+Path.GetFileName(path),false);
        var setup=EditorSceneManager.GetSceneManagerSetup();
        var reports=new System.Collections.Generic.List<object>();
        try
        {
            foreach(var path in new[]{"Assets/Scenes/WaterVisualTest.unity","Assets/Scenes/PrototypePool.unity"})
            {
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                var report=new {
                    scene=path, ambient=RenderSettings.ambientMode.ToString(),sky=RenderSettings.ambientSkyColor.ToString(),equator=RenderSettings.ambientEquatorColor.ToString(),ground=RenderSettings.ambientGroundColor.ToString(),
                    nodes=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Select(t=>new { path=PathOf(t),pos=t.position.ToString("F3"),scale=t.localScale.ToString("F3"),components=t.GetComponents<Component>().Select(c=>c==null?"MISSING":c.GetType().Name).ToArray() }).ToArray(),
                    cameras=Object.FindObjectsByType<Camera>().Select(c=>new {c.name,c.fieldOfView,c.farClipPlane,position=c.transform.position.ToString(),rotation=c.transform.eulerAngles.ToString(),background=c.backgroundColor.ToString()}).ToArray(),
                    lights=Object.FindObjectsByType<Light>().Select(l=>new{l.name,l.intensity,color=l.color.ToString(),rotation=l.transform.eulerAngles.ToString(),shadows=l.shadows.ToString()}).ToArray(),
                    renderers=Object.FindObjectsByType<Renderer>().Select(r=>new {path=PathOf(r.transform),r.enabled,bounds=r.bounds.ToString(),materials=r.sharedMaterials.Select(m=>m==null?"MISSING":AssetDatabase.GetAssetPath(m)).ToArray()}).ToArray(),
                    scripts=Object.FindObjectsByType<MonoBehaviour>().Select(m=>new {path=PathOf(m.transform),type=m.GetType().Name,data=EditorJsonUtility.ToJson(m)}).ToArray()
                };
                reports.Add(report);
                File.WriteAllText("Logs/Arena/Inspect-"+scene.name+".json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
            }
        }
        finally {EditorSceneManager.RestoreSceneManagerSetup(setup);}
        return new {scenes=reports.Count,evidence="Logs/Arena/Inspect-*.json",recovery="Temp/ArenaRecovery"};
    }
    static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
}
