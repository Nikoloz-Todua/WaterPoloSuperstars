using System;
using System.Linq;
using ToduStudios.WaterPolo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ToduStudios.WaterPolo.Editor
{
    public static class BasicAIPrototypeSetup
    {
        [MenuItem("Tools/Water Polo/Set Up Automatic Switching and AI Prototype")]
        public static void SetUp()
        {
            if (!CanSetUp()) return;
            SetUpFriendly();
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != MovementPrototypeBuilder.ScenePath) throw new InvalidOperationException("Expected PrototypePool.");
            PrototypeRoster roster = UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
            PlayerSelection selection = UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
            BallController ball = UnityEngine.Object.FindAnyObjectByType<BallController>();
            PoolBounds pool = UnityEngine.Object.FindAnyObjectByType<PoolBounds>();
            PrototypeMatch match = UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
            Transform actors = roster.Members[0].transform.parent;
            Transform opponent = actors.Find("Opponent");
            bool created = opponent == null;
            if (created)
            {
                opponent = UnityEngine.Object.Instantiate(roster.Members[0].gameObject, actors).transform;
                opponent.name = "Opponent";
                opponent.SetPositionAndRotation(pool.ClampPosition(new Vector3(4f, pool.Waterline, 8f), 1.1f), Quaternion.Euler(0f, 180f, 0f));
                opponent.Find("Visual/Cap").GetComponent<Renderer>().sharedMaterial = Material("OpponentCap", new Color(0.9f, 0.05f, 0.12f));
                opponent.Find("Visual/Body").GetComponent<Renderer>().sharedMaterial = Material("OpponentBody", new Color(0.9f, 0.94f, 1f));
            }
            SwimmerIdentity identity = opponent.GetComponent<SwimmerIdentity>();
            Transform starts = roster.Members[0].StartPosition.parent;
            Transform start = starts.Find("OpponentStart");
            if (start == null)
            {
                start = new GameObject("OpponentStart").transform;
                start.SetParent(starts, false);
                start.SetPositionAndRotation(opponent.position, opponent.rotation);
            }
            SerializedObject data = new SerializedObject(identity);
            data.FindProperty("playerId").stringValue = "prototype-white-1";
            data.FindProperty("team").enumValueIndex = (int)PrototypeTeam.White;
            data.FindProperty("capNumber").intValue = 3;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assign(identity, "startPosition", start);
            opponent.GetComponent<SwimmerInput>().enabled = false;
            opponent.GetComponent<BallInteractionInput>().enabled = false;
            identity.SetSelected(false);
            data = new SerializedObject(roster);
            SerializedProperty members = data.FindProperty("members");
            SwimmerIdentity[] participants = new[] { actors.Find("Swimmer").GetComponent<SwimmerIdentity>(), actors.Find("Swimmer2").GetComponent<SwimmerIdentity>(), identity }
                .Concat(actors.GetComponentsInChildren<SwimmerIdentity>(true).Where(p => p.IsFieldPlayer && p.Team == PrototypeTeam.Blue && p != actors.Find("Swimmer").GetComponent<SwimmerIdentity>() && p != actors.Find("Swimmer2").GetComponent<SwimmerIdentity>()))
                .Concat(actors.GetComponentsInChildren<SwimmerIdentity>(true).Where(p => p.IsFieldPlayer && p.Team == PrototypeTeam.White && p != identity))
                .Concat(actors.GetComponentsInChildren<SwimmerIdentity>(true).Where(p => !p.IsFieldPlayer)).ToArray();
            members.arraySize = participants.Length;
            for (int i = 0; i < participants.Length; i++) members.GetArrayElementAtIndex(i).objectReferenceValue = participants[i];
            data.ApplyModifiedPropertiesWithoutUndo();
            ConfigureAI(identity, roster, selection, ball, pool, match);
            GameplayCamera camera = UnityEngine.Object.FindAnyObjectByType<GameplayCamera>();
            data = new SerializedObject(camera);
            bool firstCameraUpgrade = data.FindProperty("ball").objectReferenceValue == null;
            data.FindProperty("ball").objectReferenceValue = ball;
            if (firstCameraUpgrade)
            {
                data.FindProperty("distance").floatValue = 11f;
                data.FindProperty("height").floatValue = 13f;
                data.FindProperty("smoothTime").floatValue = 0.3f;
                camera.GetComponent<Camera>().fieldOfView = 55f;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            camera.SnapToTarget();
            Save(scene);
            Debug.Log("Automatic switching and AI references configured. Existing additional roster members and tactical-team references were preserved; use Set Up 2v2 Prototype for the complete current setup.");
        }

        [MenuItem("Tools/Water Polo/Set Up Friendly AI Prototype")]
        public static void SetUpFriendly()
        {
            if (!CanSetUp()) return;
            TeamPlayPrototypeSetup.SetUp();
            Scene scene = SceneManager.GetActiveScene();
            PrototypeRoster roster = UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
            PlayerSelection selection = UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
            BallController ball = UnityEngine.Object.FindAnyObjectByType<BallController>();
            PoolBounds pool = UnityEngine.Object.FindAnyObjectByType<PoolBounds>();
            PrototypeMatch match = UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
            Assign(selection, "ball", ball);
            foreach (SwimmerIdentity player in roster.Members) if (player.IsFieldPlayer) ConfigureAI(player, roster, selection, ball, pool, match);
            Save(scene);
            Debug.Log("Friendly support and legitimate loose-ball pursuit configured; possession changes select the actual friendly carrier.");
        }

        private static bool CanSetUp()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty)) return true;
            Debug.LogWarning("Exit Play Mode and save open scenes before AI setup. Nothing was changed.");
            return false;
        }

        private static void Save(Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save PrototypePool.");
            AssetDatabase.SaveAssets();
        }

        private static Material Material(string name, Color color)
        {
            string path = "Assets/Materials/Prototype/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        internal static void ConfigureAI(SwimmerIdentity player, PrototypeRoster roster, PlayerSelection selection, BallController ball, PoolBounds pool, PrototypeMatch match)
        {
            PrototypeSwimmerAI ai = player.GetComponent<PrototypeSwimmerAI>();
            if (ai == null) ai = player.gameObject.AddComponent<PrototypeSwimmerAI>();
            Assign(ai, "roster", roster); Assign(ai, "selection", selection); Assign(ai, "ball", ball);
            Assign(ai, "pool", pool); Assign(ai, "match", match);
            PrototypeTeamTactics context = UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>().FirstOrDefault(t => t.Team == player.Team);
            if (context != null) Assign(ai, "tacticalTeam", context);
        }

        private static void Assign(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            SerializedObject data = new SerializedObject(target);
            data.FindProperty(name).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
