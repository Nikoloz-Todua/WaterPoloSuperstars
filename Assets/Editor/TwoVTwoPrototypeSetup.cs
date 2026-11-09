using System;
using System.Linq;
using ToduStudios.WaterPolo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ToduStudios.WaterPolo.Editor
{
    public static class TwoVTwoPrototypeSetup
    {
        [MenuItem("Tools/Water Polo/Set Up 2v2 Prototype")]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            { Debug.LogWarning("Exit Play Mode and save open scenes before 2v2 setup. Nothing was changed."); return; }
            BasicAIPrototypeSetup.SetUp();
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != MovementPrototypeBuilder.ScenePath) throw new InvalidOperationException("Expected PrototypePool.");
            var roster = UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
            var selection = UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
            var ball = UnityEngine.Object.FindAnyObjectByType<BallController>();
            var pool = UnityEngine.Object.FindAnyObjectByType<PoolBounds>();
            var match = UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
            Transform actors = roster.Members[0].transform.parent;
            Transform fourth = actors.Find("Opponent2");
            if (fourth == null)
            {
                fourth = UnityEngine.Object.Instantiate(roster.Members.First(p => p.Team == PrototypeTeam.White).gameObject, actors).transform;
                fourth.name = "Opponent2";
                fourth.SetPositionAndRotation(pool.ClampPosition(new Vector3(-4f, pool.Waterline, 8f), 1.1f), Quaternion.Euler(0f, 180f, 0f));
                var identity = fourth.GetComponent<SwimmerIdentity>();
                var data = new SerializedObject(identity);
                data.FindProperty("playerId").stringValue = "prototype-white-2";
                data.FindProperty("capNumber").intValue = 4;
                data.ApplyModifiedPropertiesWithoutUndo();
                Transform start = new GameObject("Opponent2Start").transform;
                start.SetParent(roster.Members[0].StartPosition.parent, false);
                start.SetPositionAndRotation(fourth.position, fourth.rotation);
                Assign(identity, "startPosition", start);
            }
            SwimmerIdentity[] players = actors.GetComponentsInChildren<SwimmerIdentity>(true)
                .OrderBy(p => p.Team).ThenBy(p => p.PlayerId, StringComparer.Ordinal).ToArray();
            if (players.Length != 4 || players.Count(p => p.Team == PrototypeTeam.Blue) != 2 || players.Count(p => p.Team == PrototypeTeam.White) != 2 || players.Select(p => p.PlayerId).Distinct().Count() != players.Length)
                throw new InvalidOperationException("2v2 setup requires two distinct Blue and two distinct White identities; existing unexpected actors were preserved.");
            var rosterData = new SerializedObject(roster);
            var members = rosterData.FindProperty("members"); members.arraySize = players.Length;
            for (int i = 0; i < players.Length; i++) members.GetArrayElementAtIndex(i).objectReferenceValue = players[i];
            rosterData.ApplyModifiedPropertiesWithoutUndo();
            foreach (PrototypeTeam side in Enum.GetValues(typeof(PrototypeTeam)))
            {
                Transform root = roster.transform.parent.Find(side + "Team");
                if (root == null) { root = new GameObject(side + "Team").transform; root.SetParent(roster.transform.parent, false); }
                var context = root.GetComponent<PrototypeTeamTactics>();
                if (context == null) context = root.gameObject.AddComponent<PrototypeTeamTactics>();
                var data = new SerializedObject(context); data.FindProperty("team").enumValueIndex = (int)side; data.ApplyModifiedPropertiesWithoutUndo();
                Assign(context, "roster", roster); Assign(context, "ball", ball); Assign(context, "pool", pool);
                Assign(context, "match", match);
                var goals = UnityEngine.Object.FindObjectsByType<GoalMouth>();
                Assign(context, "attackingGoal", goals.Single(g => g.ScoringTeam == side));
                Assign(context, "defendedGoal", goals.Single(g => g.ScoringTeam != side));
                foreach (var player in players.Where(p => p.Team == side)) Assign(player.GetComponent<PrototypeSwimmerAI>(), "tacticalTeam", context);
            }
            foreach (var player in players)
            {
                var stealing = player.GetComponent<SwimmerSteal>();
                if (stealing == null) stealing = player.gameObject.AddComponent<SwimmerSteal>();
                Assign(stealing, "roster", roster); Assign(stealing, "match", match); Assign(stealing, "selection", selection);
                bool human = player == players[0];
                player.GetComponent<SwimmerInput>().enabled = human;
                player.GetComponent<BallInteractionInput>().enabled = human;
                player.SetSelected(human);
                if (player.Team == PrototypeTeam.Blue)
                {
                    var renderer = player.transform.Find("Visual/Body").GetComponent<Renderer>();
                    if (renderer.sharedMaterial == null || renderer.sharedMaterial == AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Prototype/Swimmer.mat"))
                        renderer.sharedMaterial = BlueMaterial();
                }
            }
            Assign(selection, "ball", ball);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save PrototypePool.");
            AssetDatabase.SaveAssets();
            Debug.Log("2v2 prototype configured: two Blue human-eligible swimmers and two White AI swimmers; all restart positions registered.");
        }

        private static Material BlueMaterial()
        {
            const string path = "Assets/Materials/Prototype/BlueTeamBody.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "BlueTeamBody" };
            material.SetColor("_BaseColor", new Color(0.06f, 0.18f, 0.75f));
            AssetDatabase.CreateAsset(material, path); return material;
        }
        private static void Assign(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var data = new SerializedObject(target); data.FindProperty(field).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
