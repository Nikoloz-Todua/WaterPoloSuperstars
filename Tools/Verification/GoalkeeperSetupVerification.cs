using System;
using System.Linq;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GoalkeeperSetupVerification
{
    public static object Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Run keeper setup verification in Edit Mode.");
        GoalkeeperPrototypeSetup.SetUp(); var scene=SceneManager.GetActiveScene();
        int Count()=>scene.GetRootGameObjects().Sum(go=>go.GetComponentsInChildren<Transform>(true).Length);
        int before=Count(); var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
        var keepers=UnityEngine.Object.FindObjectsByType<PrototypeGoalkeeper>();
        var camera=UnityEngine.Object.FindAnyObjectByType<GameplayCamera>();
        float cameraDistance=new SerializedObject(camera).FindProperty("distance").floatValue;
        var data=new SerializedObject(keepers[0]); float reach=data.FindProperty("saveReach").floatValue;
        try
        {
            data.FindProperty("saveReach").floatValue=reach+0.02f; data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            GoalkeeperPrototypeSetup.SetUp(); data.Update();
            Require(Mathf.Approximately(data.FindProperty("saveReach").floatValue,reach+0.02f),"Repeat setup preserves keeper tuning.");
            Require(Count()==before && roster.Members.Count==6 && roster.FieldPlayers.Count==4 && keepers.Length==2,"Repeat setup retains exactly six athletes with four field roles.");
            Require(keepers.Select(k=>k.Identity.Team).Distinct().Count()==2 && roster.Members.Select(p=>p.PlayerId).Distinct().Count()==6 && roster.Members.Select(p=>p.StartPosition).Distinct().Count()==6,"One keeper per side, six unique identities and start markers.");
            Require(UnityEngine.Object.FindObjectsByType<GoalkeeperInterception>().Length==1 && UnityEngine.Object.FindObjectsByType<GoalkeeperDistribution>().Length==2 && UnityEngine.Object.FindObjectsByType<BallController>().Length==1,"Single interception/ball authority and one distributor per keeper.");
            Require(Mathf.Approximately(new SerializedObject(camera).FindProperty("distance").floatValue,cameraDistance),"Camera tuning unchanged.");
            foreach(var k in keepers)
            {
                Require(k.DefendedGoal.ScoringTeam!=k.Identity.Team && k.Identity.Role==AthleteRole.Goalkeeper && k.Identity.Interaction.Ball==k.Ball,"Correct team, goal, role and shared ball.");
                bool manualKeeper = k.Identity.Team == PrototypeTeam.Blue && k.GetComponent<ShotCharge>() != null;
                Require(k.GetComponent<PrototypeSwimmerAI>()==null && k.GetComponent<CapsuleCollider>().isTrigger,"Keeper has no field AI or hidden solid blocking wall.");
                Require(manualKeeper ? k.GetComponent<SwimmerInput>()!=null && k.GetComponent<BallInteractionInput>()!=null : k.GetComponent<SwimmerInput>()==null && k.GetComponent<BallInteractionInput>()==null,"Only the configured Blue possession keeper has human adapters.");
                Require(k.GetComponents<PrototypeGoalkeeper>().Length==1 && k.GetComponents<GoalkeeperDistribution>().Length==1 && k.GetComponent<SwimmerPassing>()!=null,"No duplicate keeper/distribution components; shared pass API present.");
                var distribution=new SerializedObject(k.GetComponent<GoalkeeperDistribution>());
                Require(distribution.FindProperty("tacticalTeam").objectReferenceValue!=null && distribution.FindProperty("match").objectReferenceValue!=null,"Distribution references assigned.");
            }
            return new {objectsBefore=before,objectsAfter=Count(),athletes=6,fieldPlayers=4,keepers=2,preservedTuning=true};
        }
        finally
        {
            data.Update();data.FindProperty("saveReach").floatValue=reach;data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
    }
    private static void Require(bool value,string message) {if(!value)throw new InvalidOperationException(message);}
}
