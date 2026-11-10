using System;
using System.Linq;
using ToduStudios.WaterPolo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ToduStudios.WaterPolo.Editor
{
    public static class Milestone10PrototypeSetup
    {
        [MenuItem("Tools/Water Polo/Set Up Milestone 10 Match")]
        public static void SetUp()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path!=MovementPrototypeBuilder.ScenePath)
            {Debug.LogWarning("Open PrototypePool, stop Play Mode and save intended edits before M10 setup. Nothing changed.");return;}
            var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
            if(roster==null || roster.Members.Count!=14)throw new InvalidOperationException("M10 extends the existing fourteen-athlete M9 match. Run M9 setup first.");
            var match=UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
            var ball=UnityEngine.Object.FindAnyObjectByType<BallController>();
            var selection=UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
            var teams=UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>();
            var rules=match.GetComponent<MatchRules>();if(rules==null)rules=match.gameObject.AddComponent<MatchRules>();
            Assign(rules,"match",match);Assign(rules,"roster",roster);Assign(rules,"ball",ball);Assign(rules,"selection",selection);Assign(rules,"pool",UnityEngine.Object.FindAnyObjectByType<PoolBounds>());
            var data=new SerializedObject(rules);var array=data.FindProperty("teams");array.arraySize=teams.Length;
            for(int i=0;i<teams.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=teams[i];data.ApplyModifiedPropertiesWithoutUndo();
            Assign(match,"rules",rules);Assign(selection,"tacticalTeam",teams.Single(t=>t.Team==selection.HumanTeam));
            var canvas=UnityEngine.Object.FindAnyObjectByType<MatchHUD>().GetComponentInParent<Canvas>();
            var atmosphere=canvas.GetComponent<MatchAtmosphere>();if(atmosphere==null)atmosphere=canvas.gameObject.AddComponent<MatchAtmosphere>();
            Assign(atmosphere,"match",match);Assign(atmosphere,"rules",rules);Assign(atmosphere,"ball",ball);Assign(atmosphere,"selection",selection);
            Assign(atmosphere,"roster",roster);
            Assign(atmosphere,"eventLabel",MatchHUDSetup.Label(canvas.transform,"RefereeEvents",new Vector2(0.5f,1f),new Vector2(0f,-230f),new Vector2(1000f,95f),26f));
            Assign(atmosphere,"rulesLabel",MatchHUDSetup.Label(canvas.transform,"RuleStatus",new Vector2(0.5f,0f),new Vector2(0f,130f),new Vector2(1500f,48f),22f));
            var source=atmosphere.GetComponent<AudioSource>();source.playOnAwake=false;source.loop=false;source.spatialBlend=0f;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("Milestone 10 configured in place: contextual selection, shared tactics, referee and event audio.");
        }
        private static void Assign(UnityEngine.Object target,string field,UnityEngine.Object value)=>Milestone9PrototypeSetup.Assign(target,field,value);
    }
}
