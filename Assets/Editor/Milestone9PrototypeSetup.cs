using System;
using System.Linq;
using ToduStudios.WaterPolo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ToduStudios.WaterPolo.Editor
{
    public static class Milestone9PrototypeSetup
    {
        [MenuItem("Tools/Water Polo/Set Up Milestone 9 Match")]
        public static void SetUp()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode || Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))
            { Debug.LogWarning("Stop Play Mode and save open scenes before match setup. Nothing changed."); return; }
            Milestone8PrototypeSetup.SetUp();
            var scene=SceneManager.GetActiveScene();
            if(scene.path!=MovementPrototypeBuilder.ScenePath)throw new InvalidOperationException("Expected PrototypePool.");
            var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
            var selection=UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
            bool expansion=roster.FieldPlayers.Count<12;
            var actors=roster.Members[0].transform.parent;
            foreach(PrototypeTeam team in Enum.GetValues(typeof(PrototypeTeam)))
            {
                var fields=actors.GetComponentsInChildren<SwimmerIdentity>(true).Where(p=>p.IsFieldPlayer && p.Team==team).OrderBy(p=>p.PlayerId,StringComparer.Ordinal).ToList();
                if(fields.Count>6)throw new InvalidOperationException("Full-team prototype expects six fields per team; unexpected actors were preserved.");
                while(fields.Count<6)
                {
                    var player=UnityEngine.Object.Instantiate(fields[0].gameObject,actors).GetComponent<SwimmerIdentity>();
                    player.name=team+"Field"+(fields.Count+1);
                    var data=new SerializedObject(player);data.FindProperty("playerId").stringValue="prototype-"+team.ToString().ToLowerInvariant()+"-"+(fields.Count+1);
                    int cap=1;while(fields.Any(p=>p.CapNumber==cap))cap++;
                    data.FindProperty("capNumber").intValue=cap;data.ApplyModifiedPropertiesWithoutUndo();
                    var start=new GameObject(player.name+"Start").transform;start.SetParent(fields[0].StartPosition.parent,false);
                    Assign(player,"startPosition",start);fields.Add(player);
                }
                if(expansion)
                {
                    for(int i=0;i<fields.Count;i++)
                    {
                        var roles=new[]{FieldPositionRole.CentreForward,FieldPositionRole.LeftWing,FieldPositionRole.RightWing,FieldPositionRole.CentreBack,FieldPositionRole.LeftFlat,FieldPositionRole.RightFlat};
                        var player=fields[i];var data=new SerializedObject(player);data.FindProperty("fieldPosition").enumValueIndex=(int)roles[i];data.ApplyModifiedPropertiesWithoutUndo();
                        var context=UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>().Single(t=>t.Team==team);
                        var position=context.Formation(player);
                        var rotation=Quaternion.LookRotation(context.AttackDirection);
                        player.transform.SetPositionAndRotation(position,rotation);player.StartPosition.SetPositionAndRotation(position,rotation);
                    }
                }
            }
            var all=actors.GetComponentsInChildren<SwimmerIdentity>(true).OrderBy(p=>p.IsFieldPlayer?0:1).ThenBy(p=>p.Team).ThenBy(p=>p.PlayerId,StringComparer.Ordinal).ToArray();
            var members=new SerializedObject(roster);var array=members.FindProperty("members");array.arraySize=all.Length;
            for(int i=0;i<all.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=all[i];members.ApplyModifiedPropertiesWithoutUndo();
            foreach(var player in all)
            {
                if(player.IsFieldPlayer)
                {
                    var team=UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>().Single(t=>t.Team==player.Team);
                    BasicAIPrototypeSetup.ConfigureAI(player,roster,selection,player.Interaction.Ball,UnityEngine.Object.FindAnyObjectByType<PoolBounds>(),UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>());
                    Assign(player.GetComponent<PrototypeSwimmerAI>(),"tacticalTeam",team);
                }
                bool human=player==all[0];player.SetSelected(human);
                var movement=player.GetComponent<SwimmerInput>();if(movement!=null)movement.enabled=human;
                var input=player.GetComponent<BallInteractionInput>();if(input!=null)input.enabled=human;
            }
            var strategy=selection.GetComponent<TeamStrategyInput>();if(strategy==null)strategy=selection.gameObject.AddComponent<TeamStrategyInput>();
            Assign(strategy,"selection",selection);Assign(strategy,"team",UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>().Single(t=>t.Team==selection.HumanTeam));
            foreach(var team in UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>())
            {var data=new SerializedObject(team);data.FindProperty("automaticDefense").boolValue=team.Team!=selection.HumanTeam;data.ApplyModifiedPropertiesWithoutUndo();}
            var match=UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();var ball=UnityEngine.Object.FindAnyObjectByType<BallController>();
            var shotClock=match.GetComponent<PossessionShotClock>();if(shotClock==null)shotClock=match.gameObject.AddComponent<PossessionShotClock>();
            Assign(shotClock,"match",match);Assign(shotClock,"ball",ball);Assign(match,"shotClock",shotClock);Assign(ball,"match",match);
            Assign(roster,"poolBounds",UnityEngine.Object.FindAnyObjectByType<PoolBounds>());
            var matchData=new SerializedObject(match);
            var contexts=UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>().OrderBy(t=>t.Team).ToArray();
            var teamArray=matchData.FindProperty("teams");teamArray.arraySize=contexts.Length;
            for(int i=0;i<contexts.Length;i++)teamArray.GetArrayElementAtIndex(i).objectReferenceValue=contexts[i];
            var keepers=all.Where(p=>!p.IsFieldPlayer).Select(p=>p.GetComponent<PrototypeGoalkeeper>()).ToArray();
            var keeperArray=matchData.FindProperty("keepers");keeperArray.arraySize=keepers.Length;
            for(int i=0;i<keepers.Length;i++)keeperArray.GetArrayElementAtIndex(i).objectReferenceValue=keepers[i];
            if(expansion)
            {
                var neutral=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="BallStart");
                var pool=UnityEngine.Object.FindAnyObjectByType<PoolBounds>();neutral.position=pool.transform.position+Vector3.up*(ball.SurfaceHeight-pool.transform.position.y);ball.transform.position=neutral.position;
                matchData.FindProperty("clock.quarters").intValue=4;matchData.FindProperty("clock.realSecondsPerQuarter").floatValue=80f;matchData.FindProperty("clock.displayedSecondsPerQuarter").floatValue=480f;
                var possession=new SerializedObject(shotClock);possession.FindProperty("possessionSeconds").floatValue=28f;possession.FindProperty("clockRate").floatValue=1f;possession.ApplyModifiedPropertiesWithoutUndo();
            }
            matchData.ApplyModifiedPropertiesWithoutUndo();
            foreach(var player in all)
            {
                var stamina=player.GetComponent<SwimmerStamina>();bool created=stamina==null;if(created)stamina=player.gameObject.AddComponent<SwimmerStamina>();Assign(stamina,"match",match);
                if(created){var data=new SerializedObject(stamina);data.FindProperty("sprintAllowed").boolValue=player.IsFieldPlayer;data.ApplyModifiedPropertiesWithoutUndo();}
                var lob=player.GetComponent<LobPassing>();if(lob==null)lob=player.gameObject.AddComponent<LobPassing>();Assign(lob,"roster",roster);
            }
            MatchHUDSetup.Configure(match,selection,UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>().Single(t=>t.Team==selection.HumanTeam));
            var cue=ball.GetComponent<BallHeightCue>();bool firstPresentation=cue==null;if(cue==null)cue=ball.gameObject.AddComponent<BallHeightCue>();
            var environment=scene.GetRootGameObjects().Single(g=>g.name=="Environment").transform;
            var marker=environment.Find("BallShadow");
            if(marker==null)
            {
                var disk=GameObject.CreatePrimitive(PrimitiveType.Cylinder);disk.name="BallShadow";marker=disk.transform;marker.SetParent(environment,false);UnityEngine.Object.DestroyImmediate(disk.GetComponent<Collider>());
                const string path="Assets/Materials/Prototype/BallShadow.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="BallShadow"};material.SetColor("_BaseColor",new Color(0.025f,0.09f,0.13f));AssetDatabase.CreateAsset(material,path);}
                disk.GetComponent<Renderer>().sharedMaterial=material;disk.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            Assign(cue,"ball",ball);Assign(cue,"groundMarker",marker);marker.gameObject.SetActive(false);
            if(firstPresentation || expansion)
            {
                var camera=UnityEngine.Object.FindAnyObjectByType<GameplayCamera>();var data=new SerializedObject(camera);
                data.FindProperty("distance").floatValue=13f;data.FindProperty("height").floatValue=15f;data.FindProperty("goalLookAhead").floatValue=1.8f;
                data.FindProperty("keeperDistance").floatValue=10.5f;data.FindProperty("keeperHeight").floatValue=12.3f;
                data.FindProperty("ballFramingRange").floatValue=14f;data.FindProperty("maxBallOffset").floatValue=3f;data.FindProperty("ballInfluence").floatValue=0.25f;data.ApplyModifiedPropertiesWithoutUndo();camera.GetComponent<Camera>().fieldOfView=52f;
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("Milestone 9 match configured. Existing identities and tuning preserved.");
        }
        internal static void Assign(UnityEngine.Object target,string field,UnityEngine.Object value)
        {var data=new SerializedObject(target);data.FindProperty(field).objectReferenceValue=value;data.ApplyModifiedPropertiesWithoutUndo();}
    }
}
