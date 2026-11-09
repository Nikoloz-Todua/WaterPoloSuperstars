using System;
using System.Linq;
using ToduStudios.WaterPolo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ToduStudios.WaterPolo.Editor
{
    public static class Milestone8PrototypeSetup
    {
        [MenuItem("Tools/Water Polo/Set Up Milestone 8 Prototype")]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))
            { Debug.LogWarning("Exit Play Mode and save open scenes before Milestone 8 setup. Nothing changed."); return; }
            GoalkeeperPrototypeSetup.SetUp();
            var scene=SceneManager.GetActiveScene();
            if(scene.path!=MovementPrototypeBuilder.ScenePath)throw new InvalidOperationException("Expected PrototypePool.");
            var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
            var selection=UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
            var keeper=roster.Members.Single(p=>p.Team==selection.HumanTeam && p.Role==AthleteRole.Goalkeeper);
            var movement=keeper.GetComponent<SwimmerInput>();if(movement==null)movement=keeper.gameObject.AddComponent<SwimmerInput>();
            var source=new SerializedObject(roster.FieldPlayers[0].GetComponent<SwimmerInput>()).FindProperty("inputActions").objectReferenceValue;
            Assign(movement,"inputActions",source);movement.enabled=false;
            var input=keeper.GetComponent<BallInteractionInput>();if(input==null)input=keeper.gameObject.AddComponent<BallInteractionInput>();input.enabled=false;
            var marker=keeper.transform.Find("SelectionRing");
            if(marker==null)
            {
                marker=new GameObject("SelectionRing").transform;marker.SetParent(keeper.transform,false);marker.localPosition=new Vector3(0f,-0.055f,0f);
                var ring=marker.gameObject.AddComponent<LineRenderer>();ring.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Prototype/SelectionRing.mat");
                ring.useWorldSpace=false;ring.loop=true;ring.widthMultiplier=0.055f;ring.positionCount=48;ring.shadowCastingMode=ShadowCastingMode.Off;ring.receiveShadows=false;
                for(int i=0;i<48;i++){float angle=2f*Mathf.PI*i/48;ring.SetPosition(i,new Vector3(Mathf.Cos(angle),0f,Mathf.Sin(angle))*1.05f);}
            }
            Assign(keeper,"selectionIndicator",marker.gameObject);marker.gameObject.SetActive(false);
            foreach(var player in roster.Members.Where(p=>p.Team==selection.HumanTeam))
            {
                var charge=player.GetComponent<ShotCharge>();if(charge==null)charge=player.gameObject.AddComponent<ShotCharge>();
                Assign(charge,"selection",selection);
            }
            bool firstSetup=SetUpPowerBar(selection);
            var camera=UnityEngine.Object.FindAnyObjectByType<GameplayCamera>();
            var team=UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>().Single(t=>t.Team==selection.HumanTeam);
            Assign(camera,"attackingGoal",team.AttackingGoal);
            if(firstSetup)
            {
                var framing=new SerializedObject(camera);framing.FindProperty("distance").floatValue=10f;framing.FindProperty("height").floatValue=11.8f;framing.ApplyModifiedPropertiesWithoutUndo();
                camera.GetComponent<Camera>().fieldOfView=48f;
            }
            SetUpGoalContrast();
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Scene save failed.");
            AssetDatabase.SaveAssets();Debug.Log("Milestone 8 controls configured. Blue keeper control is granted only by actual possession; C remains field-only.");
        }
        private static bool SetUpPowerBar(PlayerSelection selection)
        {
            var canvas=UnityEngine.Object.FindAnyObjectByType<PrototypeScoreboard>().GetComponentInParent<Canvas>();
            if(canvas==null)throw new InvalidOperationException("Existing scoreboard Canvas required.");
            var controller=canvas.GetComponent<ShotPowerBar>();bool created=controller==null;if(created)controller=canvas.gameObject.AddComponent<ShotPowerBar>();
            var panel=canvas.transform.Find("ShotPowerBar") as RectTransform;
            if(panel==null)
            {
                panel=new GameObject("ShotPowerBar",typeof(RectTransform),typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();
                panel.SetParent(canvas.transform,false);panel.anchorMin=panel.anchorMax=new Vector2(0.5f,0f);
                panel.pivot=new Vector2(0.5f,0f);panel.anchoredPosition=new Vector2(0f,70f);panel.sizeDelta=new Vector2(260f,24f);
                var background=panel.GetComponent<UnityEngine.UI.Image>();background.color=new Color(0.02f,0.04f,0.08f,0.85f);background.raycastTarget=false;
                var inset=new GameObject("Inset",typeof(RectTransform)).GetComponent<RectTransform>();inset.SetParent(panel,false);
                inset.anchorMin=Vector2.zero;inset.anchorMax=Vector2.one;inset.offsetMin=new Vector2(3f,3f);inset.offsetMax=new Vector2(-3f,-3f);
                var fill=new GameObject("Fill",typeof(RectTransform),typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();fill.SetParent(inset,false);
                fill.anchorMin=Vector2.zero;fill.anchorMax=Vector2.one;fill.offsetMin=fill.offsetMax=Vector2.zero;
                fill.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            }
            Assign(controller,"selection",selection);Assign(controller,"panel",panel.gameObject);
            Assign(controller,"fill",panel.Find("Inset/Fill").GetComponent<UnityEngine.UI.Image>());panel.gameObject.SetActive(false);
            return created;
        }
        private static void SetUpGoalContrast()
        {
            const string path="Assets/Materials/Prototype/ReadableGoalFrame.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null)
            {
                material=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="ReadableGoalFrame"};
                material.SetColor("_BaseColor",Color.white);AssetDatabase.CreateAsset(material,path);
            }
            foreach(var view in UnityEngine.Object.FindObjectsByType<PrototypeGoalView>())
            {
                var data=new SerializedObject(view);
                foreach(string field in new[]{"leftPost","rightPost","crossbar"})
                    ((Transform)data.FindProperty(field).objectReferenceValue).GetComponent<Renderer>().sharedMaterial=material;
            }
        }
        private static void Assign(UnityEngine.Object target,string field,UnityEngine.Object value){var d=new SerializedObject(target);d.FindProperty(field).objectReferenceValue=value;d.ApplyModifiedPropertiesWithoutUndo();}
    }
}
