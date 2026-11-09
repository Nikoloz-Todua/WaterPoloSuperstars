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
    public static class TeamPlayPrototypeSetup
    {
        [MenuItem("Tools/Water Polo/Set Up Two Swimmer Passing Prototype")]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            {
                Debug.LogWarning("Exit Play Mode and save open scenes before two-swimmer setup. Nothing was changed.");
                return;
            }
            GoalScoringPrototypeSetup.SetUp();
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != MovementPrototypeBuilder.ScenePath) throw new InvalidOperationException("Expected PrototypePool.");
            Transform Root(string name) => scene.GetRootGameObjects().Single(go => go.name == name).transform;
            Transform actors = Root("Actors");
            Transform gameplay = Root("Gameplay");
            SwimmerMotor first = actors.Find("Swimmer").GetComponent<SwimmerMotor>();
            PoolBounds bounds = gameplay.Find("PoolBounds").GetComponent<PoolBounds>();
            Transform starts = gameplay.Find("RestartPositions");
            Camera camera = Root("CameraRig").Find("Main Camera").GetComponent<Camera>();
            GameplayCamera follow = camera.GetComponent<GameplayCamera>();
            SwimmerIdentity opponent = actors.Find("Opponent")?.GetComponent<SwimmerIdentity>();
            if (opponent != null && (opponent.PlayerId != "prototype-white-1" || opponent.Team != PrototypeTeam.White))
                throw new InvalidOperationException("Unexpected opponent identity; refusing to replace it.");
            if (actors.GetComponentsInChildren<SwimmerMotor>(true).Any(m => m != first && m.name != "Swimmer2" &&
                (m.GetComponent<SwimmerIdentity>() == null || m.GetComponent<SwimmerIdentity>().Team != PrototypeTeam.White)))
                throw new InvalidOperationException("Unexpected extra swimmers; refusing to duplicate them.");

            Transform secondTransform = actors.Find("Swimmer2");
            bool createdSecond = secondTransform == null;
            if (createdSecond)
            {
                // Clone only the existing swimmer, retaining its working body and mapped child references.
                GameObject secondObject = UnityEngine.Object.Instantiate(first.gameObject, actors);
                secondObject.name = "Swimmer2";
                secondTransform = secondObject.transform;
                secondTransform.SetPositionAndRotation(bounds.ClampPosition(bounds.transform.position + new Vector3(2f, 0f, 5f), 1.1f), Quaternion.Euler(0f, 180f, 0f));
                secondTransform.Find("Visual/Cap").GetComponent<Renderer>().sharedMaterial =
                    Material("TeammateCap", "Universal Render Pipeline/Lit", new Color(0.95f, 0.98f, 1f));
            }
            SwimmerMotor second = secondTransform.GetComponent<SwimmerMotor>();
            if (second == null) throw new InvalidOperationException("Swimmer2 needs the existing swimmer components.");
            Transform secondStart = starts.Find("Swimmer2Start");
            if (secondStart == null)
            {
                secondStart = Child(starts, "Swimmer2Start");
                secondStart.SetPositionAndRotation(secondTransform.position, secondTransform.rotation);
            }

            Material selected = Material("SelectionRing", "Universal Render Pipeline/Unlit", new Color(0.35f, 1f, 0.25f));
            SwimmerIdentity firstIdentity = Identity(first, "prototype-blue-1", 1, starts.Find("SwimmerStart"), selected);
            SwimmerIdentity secondIdentity = Identity(second, "prototype-blue-2", 2, secondStart, selected, createdSecond);
            PrototypeRoster roster = GetOrAdd<PrototypeRoster>(Child(gameplay, "PrototypeRoster").gameObject);
            SwimmerIdentity[] participants = new[] { firstIdentity, secondIdentity }.Concat(
                actors.GetComponentsInChildren<SwimmerIdentity>(true).Where(p => p != firstIdentity && p != secondIdentity)).ToArray();
            AssignArray(roster, "members", participants);
            Transform controlRoot = gameplay.Find("PlayerSelection");
            bool firstUpgrade = controlRoot == null || controlRoot.GetComponent<PlayerSelection>() == null;
            if (controlRoot == null) controlRoot = Child(gameplay, "PlayerSelection");
            PlayerSelection selection = GetOrAdd<PlayerSelection>(controlRoot.gameObject);
            Assign(selection, "roster", roster);
            Assign(selection, "gameplayCamera", follow);
            GetOrAdd<PlayerSwitchInput>(controlRoot.gameObject);
            foreach (SwimmerIdentity player in participants)
            {
                SwimmerPassing passing = GetOrAdd<SwimmerPassing>(player.gameObject);
                Assign(passing, "roster", roster);
                player.GetComponent<SwimmerInput>().enabled = player == firstIdentity;
                player.GetComponent<BallInteractionInput>().enabled = player == firstIdentity;
                player.Motor.enabled = true;
                player.SetSelected(player == firstIdentity);
            }
            PrototypeMatch match = gameplay.Find("PrototypeMatch").GetComponent<PrototypeMatch>();
            Assign(match, "roster", roster);
            Assign(match, "playerSelection", selection);
            if (firstUpgrade)
            {
                SerializedObject settings = new SerializedObject(follow);
                settings.FindProperty("distance").floatValue = 9.5f;
                settings.FindProperty("height").floatValue = 11.5f;
                settings.FindProperty("smoothTime").floatValue = 0.3f;
                settings.ApplyModifiedPropertiesWithoutUndo();
                camera.fieldOfView = 55f;
            }
            follow.SetTarget(first);
            follow.SnapToTarget();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save PrototypePool.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = controlRoot.gameObject;
            Debug.Log("Two-swimmer prototype ready. E pickup, Space shoot, B directional pass, C switch. Yellow cap starts selected; white cap is the teammate. Repeat setup preserves geometry and camera tuning.");
        }

        private static SwimmerIdentity Identity(SwimmerMotor motor, string id, int cap, Transform start, Material selected, bool cloned = false)
        {
            SwimmerIdentity identity = GetOrAdd<SwimmerIdentity>(motor.gameObject);
            SerializedObject data = new SerializedObject(identity);
            if (cloned || string.IsNullOrEmpty(data.FindProperty("playerId").stringValue)) data.FindProperty("playerId").stringValue = id;
            data.FindProperty("team").enumValueIndex = (int)PrototypeTeam.Blue;
            data.FindProperty("capNumber").intValue = cap;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assign(identity, "startPosition", start);
            Transform marker = Child(motor.transform, "SelectionRing");
            marker.localPosition = new Vector3(0f, -0.055f, 0f);
            LineRenderer ring = GetOrAdd<LineRenderer>(marker.gameObject);
            ring.sharedMaterial = selected;
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.widthMultiplier = 0.055f;
            ring.positionCount = 48;
            ring.shadowCastingMode = ShadowCastingMode.Off;
            ring.receiveShadows = false;
            for (int i = 0; i < ring.positionCount; i++)
            {
                float angle = 2f * Mathf.PI * i / ring.positionCount;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 1.2f);
            }
            Assign(identity, "selectionIndicator", marker.gameObject);
            return identity;
        }

        private static Transform Child(Transform parent, string name)
        {
            Transform result = parent.Find(name);
            if (result != null) return result;
            result = new GameObject(name).transform;
            result.SetParent(parent, false);
            return result;
        }
        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component == null) component = go.AddComponent<T>();
            return component;
        }
        private static Material Material(string name, string shader, Color color)
        {
            string path = "Assets/Materials/Prototype/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find(shader)) { name = name };
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
        private static void Assign(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            SerializedObject data = new SerializedObject(target);
            data.FindProperty(field).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void AssignArray(UnityEngine.Object target, string field, UnityEngine.Object[] values)
        {
            SerializedObject data = new SerializedObject(target);
            SerializedProperty array = data.FindProperty(field);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
