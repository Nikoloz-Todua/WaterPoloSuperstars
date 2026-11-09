using System;
using System.Linq;
using TMPro;
using ToduStudios.WaterPolo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ToduStudios.WaterPolo.Editor
{
    public static class GoalScoringPrototypeSetup
    {
        [MenuItem("Tools/Water Polo/Set Up Goals and Scoring Prototype")]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            {
                Debug.LogWarning("Exit Play Mode and save scene changes before goal setup. Nothing was changed.");
                return;
            }
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                TMP_PackageResourceImporter.ImportResources(true, false, false);
                Debug.Log("Importing TMP Essential Resources without examples. Run goal setup again after import completes.");
                return;
            }
            CameraBallPrototypeSetup.SetUp();
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != MovementPrototypeBuilder.ScenePath)
                throw new InvalidOperationException("Expected PrototypePool.");
            Transform environment = Root(scene, "Environment");
            Transform gameplay = Root(scene, "Gameplay");
            Transform actors = Root(scene, "Actors");
            PoolBounds pool = gameplay.Find("PoolBounds").GetComponent<PoolBounds>();
            BallController ball = actors.Find("Ball").GetComponent<BallController>();
            SwimmerMotor swimmer = actors.Find("Swimmer").GetComponent<SwimmerMotor>();
            GoalMouth[] existing = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<GoalMouth>(true)).ToArray();
            if (existing.Length > 2)
                throw new InvalidOperationException("Unexpected extra goals; refusing to duplicate them.");

            Material frame = Material("GoalFrame", new Color(0.97f, 0.98f, 1f));
            Material net = Material("GoalNet", new Color(0.4f, 0.63f, 0.68f));
            Material water = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Prototype/Water.mat");
            Transform goalRoot = Child(environment, "Goals");
            GoalMouth blue = Goal(goalRoot, "BlueScoringGoal", PrototypeTeam.Blue, 1f, pool, frame, net, water);
            GoalMouth white = Goal(goalRoot, "WhiteScoringGoal", PrototypeTeam.White, -1f, pool, frame, net, water);
            AssignArray(ball, "goalMouths", new UnityEngine.Object[] { blue, white });

            // Retain original objects, but replace the uninterrupted end slabs with split sections.
            foreach (string end in new[] { "NearEdge", "FarEdge" })
            {
                Transform slab = environment.Find("PoolBoundaries/" + end);
                if (slab == null) continue;
                Renderer renderer = slab.GetComponent<Renderer>();
                if (renderer != null) renderer.enabled = false;
                Collider collider = slab.GetComponent<Collider>();
                if (collider != null) collider.enabled = false;
            }

            Transform starts = Child(gameplay, "RestartPositions");
            Transform swimmerStart = starts.Find("SwimmerStart");
            if (swimmerStart == null)
            {
                swimmerStart = Child(starts, "SwimmerStart");
                swimmerStart.SetPositionAndRotation(swimmer.transform.position, swimmer.transform.rotation);
            }
            Transform ballStart = starts.Find("BallStart");
            if (ballStart == null)
            {
                ballStart = Child(starts, "BallStart");
                ballStart.position = ball.transform.position;
            }
            PrototypeMatch match = GetOrAdd<PrototypeMatch>(Child(gameplay, "PrototypeMatch").gameObject);
            Assign(match, "ball", ball);
            Assign(match, "swimmer", swimmer);
            Assign(match, "swimmerStart", swimmerStart);
            Assign(match, "ballStart", ballStart);
            Assign(match, "gameplayCamera", Root(scene, "CameraRig").Find("Main Camera").GetComponent<GameplayCamera>());
            AssignArray(match, "goals", new UnityEngine.Object[] { blue, white });
            BuildHud(scene, match);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save PrototypePool.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = match.gameObject;
            Debug.Log("Goals and scoring ready. BLUE attacks +Z; WHITE attacks -Z. Score once per released shot, then restart with E/Space controls restored.");
        }

        private static GoalMouth Goal(Transform parent, string name, PrototypeTeam team, float sign, PoolBounds pool, Material frame, Material net, Material water)
        {
            Transform root = parent.Find(name);
            bool created = root == null;
            if (created)
            {
                root = Child(parent, name);
                root.position = new Vector3(pool.transform.position.x, pool.Waterline - 0.08f, pool.transform.position.z + sign * pool.Length * 0.5f);
                root.rotation = Quaternion.Euler(0f, sign > 0f ? 0f : 180f, 0f);
            }
            GoalMouth mouth = GetOrAdd<GoalMouth>(root.gameObject);
            if (created)
            {
                SerializedObject data = new SerializedObject(mouth);
                data.FindProperty("scoringTeam").enumValueIndex = (int)team;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            Child(root, "ScoringPlane");
            Transform model = Child(root, "PlaceholderModel");
            PrototypeGoalView view = GetOrAdd<PrototypeGoalView>(root.gameObject);
            Assign(view, "mouth", mouth);
            Assign(view, "pool", pool);
            Assign(view, "leftPost", Box(model, "LeftPost", frame, true));
            Assign(view, "rightPost", Box(model, "RightPost", frame, true));
            Assign(view, "crossbar", Box(model, "Crossbar", frame, true));
            Transform back = Child(model, "BackNetCollider");
            GetOrAdd<BoxCollider>(back.gameObject);
            Assign(view, "backCollider", back);
            Assign(view, "leftNet", Box(model, "LeftNet", net, true));
            Assign(view, "rightNet", Box(model, "RightNet", net, true));
            Assign(view, "netWater", Box(model, "PocketWater", water, false));
            Assign(view, "leftBoundary", Box(model, "EndBoundaryLeft", frame, true));
            Assign(view, "rightBoundary", Box(model, "EndBoundaryRight", frame, true));
            Transform strands = Child(model, "BackNetStrands");
            AssignArray(view, "verticalStrands", Enumerable.Range(0, 9).Select(i => (UnityEngine.Object)Box(strands, "Vertical" + i, net, false)).ToArray());
            AssignArray(view, "horizontalStrands", Enumerable.Range(0, 4).Select(i => (UnityEngine.Object)Box(strands, "Horizontal" + i, net, false)).ToArray());
            view.Refresh();
            return mouth;
        }

        private static void BuildHud(Scene scene, PrototypeMatch match)
        {
            Transform hud = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "PrototypeHUD")?.transform;
            if (hud == null)
            {
                GameObject root = new GameObject("PrototypeHUD", typeof(RectTransform));
                SceneManager.MoveGameObjectToScene(root, scene);
                hud = root.transform;
            }
            Canvas canvas = GetOrAdd<Canvas>(hud.gameObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            UnityEngine.UI.CanvasScaler scaler = GetOrAdd<UnityEngine.UI.CanvasScaler>(hud.gameObject);
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            GetOrAdd<UnityEngine.UI.GraphicRaycaster>(hud.gameObject);
            RectTransform panel = RectChild(hud, "ScorePanel");
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 1f);
            panel.pivot = new Vector2(0.5f, 1f);
            panel.anchoredPosition = new Vector2(0f, -24f);
            panel.sizeDelta = new Vector2(620f, 86f);
            UnityEngine.UI.Image image = GetOrAdd<UnityEngine.UI.Image>(panel.gameObject);
            image.color = new Color(0.025f, 0.06f, 0.11f, 0.94f);
            image.raycastTarget = false;
            RectTransform scoreRect = RectChild(panel, "ScoreLabel");
            scoreRect.anchorMin = Vector2.zero;
            scoreRect.anchorMax = Vector2.one;
            scoreRect.offsetMin = new Vector2(12f, 4f);
            scoreRect.offsetMax = new Vector2(-12f, -4f);
            TextMeshProUGUI score = Label(scoreRect, "BLUE 0 - 0 WHITE", 44f, Color.white);
            RectTransform goalRect = RectChild(hud, "GoalLabel");
            goalRect.anchorMin = goalRect.anchorMax = goalRect.pivot = new Vector2(0.5f, 0.5f);
            goalRect.anchoredPosition = new Vector2(0f, 180f);
            goalRect.sizeDelta = new Vector2(700f, 130f);
            TextMeshProUGUI goal = Label(goalRect, "GOAL!", 88f, new Color(1f, 0.85f, 0.16f));
            goal.gameObject.SetActive(false);
            PrototypeScoreboard scoreboard = GetOrAdd<PrototypeScoreboard>(hud.gameObject);
            Assign(scoreboard, "match", match);
            Assign(scoreboard, "scoreLabel", score);
            Assign(scoreboard, "goalLabel", goal);
        }

        private static TextMeshProUGUI Label(RectTransform rect, string text, float size, Color color)
        {
            TextMeshProUGUI label = GetOrAdd<TextMeshProUGUI>(rect.gameObject);
            label.font = TMP_Settings.defaultFontAsset;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        private static Transform Root(Scene scene, string name) => scene.GetRootGameObjects().Single(go => go.name == name).transform;
        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            // Use Unity's null comparison, including Editor missing-component wrappers.
            T component = go.GetComponent<T>();
            if (component == null) component = go.AddComponent<T>();
            return component;
        }

        private static Transform Child(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) return child;
            child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static RectTransform RectChild(Transform parent, string name)
        {
            Transform found = parent.Find(name);
            if (found != null) return found.GetComponent<RectTransform>();
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static Transform Box(Transform parent, string name, Material material, bool physical)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing;
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.GetComponent<Renderer>().sharedMaterial = material;
            if (!physical) UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
            return box.transform;
        }

        private static Material Material(string name, Color color)
        {
            string path = "Assets/Materials/Prototype/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.25f);
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
