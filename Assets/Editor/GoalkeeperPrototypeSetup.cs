using System;
using System.Linq;
using ToduStudios.WaterPolo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ToduStudios.WaterPolo.Editor
{
    public static class GoalkeeperPrototypeSetup
    {
        [MenuItem("Tools/Water Polo/Set Up Goalkeeper Prototype")]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            { Debug.LogWarning("Exit Play Mode and save open scenes before goalkeeper setup. Nothing was changed."); return; }
            TwoVTwoPrototypeSetup.SetUp();
            var scene = SceneManager.GetActiveScene();
            if (scene.path != MovementPrototypeBuilder.ScenePath) throw new InvalidOperationException("Expected PrototypePool.");
            var roster = UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
            var selection = UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
            var match = UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
            var ball = UnityEngine.Object.FindAnyObjectByType<BallController>();
            var pool = UnityEngine.Object.FindAnyObjectByType<PoolBounds>();
            var teams = UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>();
            var actors = roster.Members[0].transform.parent;
            foreach (var team in teams)
            {
                var existing = actors.GetComponentsInChildren<SwimmerIdentity>(true).Where(p => p.Role == AthleteRole.Goalkeeper && p.Team == team.Team).ToArray();
                if (existing.Length > 1) throw new InvalidOperationException("Duplicate goalkeeper identities; nothing will be deleted.");
                var identity = existing.SingleOrDefault(); bool created = identity == null;
                if (created)
                {
                    var template = roster.FieldPlayers.First(p => p.Team == team.Team);
                    var go = UnityEngine.Object.Instantiate(template.gameObject, actors); go.name = team.Team + "Goalkeeper";
                    identity = go.GetComponent<SwimmerIdentity>();
                    UnityEngine.Object.DestroyImmediate(go.GetComponent<PrototypeSwimmerAI>());
                    UnityEngine.Object.DestroyImmediate(go.GetComponent<SwimmerSteal>());
                    UnityEngine.Object.DestroyImmediate(go.GetComponent<SwimmerInput>());
                    UnityEngine.Object.DestroyImmediate(go.GetComponent<BallInteractionInput>());
                    var ring = go.transform.Find("SelectionRing"); if (ring != null) UnityEngine.Object.DestroyImmediate(ring.gameObject);
                    var data = new SerializedObject(identity);
                    data.FindProperty("playerId").stringValue = "prototype-" + team.Team.ToString().ToLowerInvariant() + "-goalkeeper";
                    data.FindProperty("role").enumValueIndex = (int)AthleteRole.Goalkeeper;
                    data.FindProperty("capNumber").intValue = 13; data.FindProperty("selectionIndicator").objectReferenceValue = null;
                    data.ApplyModifiedPropertiesWithoutUndo();
                    Vector3 position = team.DefendedGoal.transform.TransformPoint(new Vector3(0f, 0f, -1.4f)); position.y = pool.Waterline;
                    go.transform.SetPositionAndRotation(position, Quaternion.LookRotation(-team.DefendedGoal.transform.forward));
                    var start = new GameObject(team.Team + "GoalkeeperStart").transform;
                    start.SetParent(roster.Members[0].StartPosition.parent, false); start.SetPositionAndRotation(position, go.transform.rotation);
                    Assign(identity, "startPosition", start);
                    var collider = go.GetComponent<CapsuleCollider>(); collider.direction = 1; collider.center = new Vector3(0f, 0.3f, 0f); collider.height = 1.6f; collider.radius = 0.4f; collider.isTrigger = true;
                    data = new SerializedObject(identity.Motor); data.FindProperty("maxSpeed").floatValue = 2.5f;
                    data.FindProperty("acceleration").floatValue = 8f; data.FindProperty("deceleration").floatValue = 10f;
                    data.FindProperty("boundaryClearance").floatValue = 0.45f; data.ApplyModifiedPropertiesWithoutUndo();
                    var visual = go.transform.Find("Visual");
                    var body = visual.Find("Body"); body.localRotation = Quaternion.identity; body.localPosition = new Vector3(0f, 0.25f, 0f); body.localScale = new Vector3(0.7f, 0.6f, 0.55f);
                    body.GetComponent<Renderer>().sharedMaterial = Material("GoalkeeperBody", new Color(1f, 0.5f, 0.05f));
                    visual.Find("Head").localPosition = new Vector3(0f, 0.8f, 0f);
                    visual.Find("Cap").localPosition = new Vector3(0f, 0.94f, 0f);
                    visual.Find("Cap").GetComponent<Renderer>().sharedMaterial = Material(team.Team + "GoalkeeperCap", team.Team == PrototypeTeam.Blue ? new Color(0.05f, 0.15f, 0.95f) : Color.white);
                    foreach (string arm in new[] { "LeftArm", "RightArm" }) { var part = visual.Find(arm); part.localPosition = new Vector3(arm == "LeftArm" ? -0.55f : 0.55f, 0.3f, 0f); part.localScale = new Vector3(0.3f, 0.2f, 0.5f); }
                    identity.Interaction.CarryAnchor.localPosition = new Vector3(0f, 0.07f, 0.55f);
                    data = new SerializedObject(identity.Interaction); data.FindProperty("pickupDistance").floatValue = 0.8f; data.FindProperty("releaseDistance").floatValue = 1.1f; data.ApplyModifiedPropertiesWithoutUndo();
                }
                var keeper = identity.GetComponent<PrototypeGoalkeeper>(); if (keeper == null) keeper = identity.gameObject.AddComponent<PrototypeGoalkeeper>();
                Assign(keeper, "defendedGoal", team.DefendedGoal); Assign(keeper, "ball", ball); Assign(keeper, "match", match); Assign(keeper, "selection", selection);
                var distribution = identity.GetComponent<GoalkeeperDistribution>(); bool newDistribution = distribution == null;
                if (newDistribution) distribution = identity.gameObject.AddComponent<GoalkeeperDistribution>();
                Assign(distribution, "tacticalTeam", team); Assign(distribution, "match", match);
                if (newDistribution)
                {
                    var passingData = new SerializedObject(identity.GetComponent<SwimmerPassing>());
                    passingData.FindProperty("passSpeed").floatValue = 11f; passingData.FindProperty("maximumTargetDistance").floatValue = 22f;
                    passingData.FindProperty("receivingWindow").floatValue = 5f; passingData.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            var players = roster.FieldPlayers.ToArray();
            var keepers = actors.GetComponentsInChildren<SwimmerIdentity>(true).Where(p => p.Role == AthleteRole.Goalkeeper).OrderBy(p => p.Team).ToArray();
            var members = players.Concat(keepers).ToArray(); var rosterData = new SerializedObject(roster);
            var array = rosterData.FindProperty("members"); array.arraySize = members.Length;
            for (int i = 0; i < members.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = members[i];
            rosterData.ApplyModifiedPropertiesWithoutUndo();
            var interception = roster.transform.parent.GetComponent<GoalkeeperInterception>();
            if (interception == null) interception = roster.transform.parent.gameObject.AddComponent<GoalkeeperInterception>();
            Assign(interception, "ball", ball); Assign(interception, "match", match);
            var observerData = new SerializedObject(interception); var keeperArray = observerData.FindProperty("keepers"); keeperArray.arraySize = keepers.Length;
            for (int i = 0; i < keepers.Length; i++) keeperArray.GetArrayElementAtIndex(i).objectReferenceValue = keepers[i].GetComponent<PrototypeGoalkeeper>();
            observerData.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save goalkeeper prototype.");
            AssetDatabase.SaveAssets();
            Debug.Log("Goalkeeper prototype configured: four field players and two role-specific goalkeepers. Existing pool, goals, camera and HUD preserved.");
        }
        private static Material Material(string name, Color color)
        {
            string path = "Assets/Materials/Prototype/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path); if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name }; material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path); return material;
        }
        private static void Assign(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var data = new SerializedObject(target); data.FindProperty(field).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
