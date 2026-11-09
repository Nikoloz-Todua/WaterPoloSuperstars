using System;
using System.IO;
using ToduStudios.WaterPolo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ToduStudios.WaterPolo.Editor
{
    /// <summary>Explicit, repeat-safe setup. Never rebuilds or overwrites an existing prototype.</summary>
    public static class MovementPrototypeBuilder
    {
        public const string ScenePath = "Assets/Scenes/PrototypePool.unity";
        private const string MaterialFolder = "Assets/Materials/Prototype";

        [MenuItem("Tools/Water Polo/Create Movement Prototype")]
        public static void CreateMovementPrototype()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before opening or creating the movement prototype.");
                return;
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                {
                    Debug.LogWarning("Save your open scene changes before running Create Movement Prototype. Nothing was changed.");
                    return;
                }
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene loaded = SceneManager.GetSceneAt(i);
                if (loaded.path == ScenePath)
                {
                    if (SceneManager.sceneCount > 1)
                        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                    else
                        SceneManager.SetActiveScene(loaded);
                    Debug.Log("PrototypePool is already open; existing objects and edits were preserved.");
                    return;
                }
            }

            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                Debug.Log("Opened the existing PrototypePool without rebuilding it.");
                return;
            }

            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (actions == null || actions.FindAction("Player/Move") == null || lit == null || unlit == null)
                throw new InvalidOperationException("Prototype setup requires URP shaders and the existing Player/Move action.");

            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory(MaterialFolder);
            AssetDatabase.Refresh();
            Material water = Material("Water", unlit, new Color(0.025f, 0.43f, 0.64f));
            Material floor = Material("Floor", lit, new Color(0.12f, 0.27f, 0.34f));
            Material edge = Material("PoolEdge", lit, new Color(0.86f, 0.9f, 0.88f));
            Material coral = Material("Swimmer", lit, new Color(0.95f, 0.19f, 0.12f));
            Material cap = Material("Cap", lit, new Color(1f, 0.82f, 0.12f));
            Material skin = Material("Skin", lit, new Color(0.86f, 0.55f, 0.35f));
            Material line = Material("Markings", unlit, new Color(0.62f, 0.87f, 0.94f));

            // Only saved scenes can reach this point; their files remain untouched.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            Transform environment = Root("Environment", scene);
            Transform actors = Root("Actors", scene);
            Transform gameplay = Root("Gameplay", scene);
            Transform cameraRig = Root("CameraRig", scene);
            Transform lighting = Root("Lighting", scene);

            PoolBounds bounds = Child("PoolBounds", gameplay).AddComponent<PoolBounds>();
            float width = bounds.Width;
            float length = bounds.Length;
            Primitive("PoolFloor", PrimitiveType.Cube, environment, new Vector3(0f, -1.6f, 0f), new Vector3(width, 0.3f, length), floor);
            Primitive("WaterSurface", PrimitiveType.Cube, environment, new Vector3(0f, -0.12f, 0f), new Vector3(width, 0.08f, length), water);
            Transform walls = Child("PoolBoundaries", environment).transform;
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Primitive(sign < 0 ? "LeftEdge" : "RightEdge", PrimitiveType.Cube, walls,
                    new Vector3(sign * (width * 0.5f + 0.25f), 0f, 0f), new Vector3(0.5f, 0.5f, length + 1f), edge);
                Primitive(sign < 0 ? "NearEdge" : "FarEdge", PrimitiveType.Cube, walls,
                    new Vector3(0f, 0f, sign * (length * 0.5f + 0.25f)), new Vector3(width, 0.5f, 0.5f), edge);
            }

            Transform markings = Child("PoolMarkings", environment).transform;
            foreach (float z in new[] { -10f, 0f, 10f })
                Primitive("Line_" + z, PrimitiveType.Cube, markings, new Vector3(0f, -0.07f, z), new Vector3(width, 0.015f, 0.06f), line);

            GameObject swimmer = Child("Swimmer", actors);
            Rigidbody body = swimmer.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            CapsuleCollider collider = swimmer.AddComponent<CapsuleCollider>();
            collider.direction = 2;
            collider.radius = 0.4f;
            collider.height = 2.2f;
            SwimmerMotor motor = swimmer.AddComponent<SwimmerMotor>();
            SerializedObject motorData = new SerializedObject(motor);
            motorData.FindProperty("poolBounds").objectReferenceValue = bounds;
            motorData.ApplyModifiedPropertiesWithoutUndo();
            SwimmerInput input = swimmer.AddComponent<SwimmerInput>();
            SerializedObject inputData = new SerializedObject(input);
            inputData.FindProperty("inputActions").objectReferenceValue = actions;
            inputData.ApplyModifiedPropertiesWithoutUndo();

            Transform visual = Child("Visual", swimmer.transform).transform;
            GameObject torso = Primitive("Body", PrimitiveType.Capsule, visual, new Vector3(0f, 0.1f, -0.15f), new Vector3(0.65f, 0.7f, 0.55f), coral);
            torso.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Primitive("Head", PrimitiveType.Sphere, visual, new Vector3(0f, 0.18f, 0.64f), new Vector3(0.5f, 0.5f, 0.5f), skin);
            Primitive("Cap", PrimitiveType.Sphere, visual, new Vector3(0f, 0.32f, 0.6f), new Vector3(0.52f, 0.32f, 0.5f), cap);
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Primitive(sign < 0 ? "LeftArm" : "RightArm", PrimitiveType.Cube, visual,
                    new Vector3(sign * 0.48f, 0.04f, 0.15f), new Vector3(0.22f, 0.2f, 0.8f), skin);
            }

            GameObject cameraObject = Child("Main Camera", cameraRig);
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            camera.transform.position = new Vector3(0f, 32f, -24f);
            camera.transform.rotation = Quaternion.LookRotation(-camera.transform.position, Vector3.up);
            camera.fieldOfView = 55f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 120f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.075f, 0.11f);

            Light sun = Child("Directional Light", lighting).AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.61f, 0.68f);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not save " + ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = swimmer;
            Debug.Log("Created PrototypePool. WASD/arrows or gamepad left stick move the swimmer.");
        }

        private static Transform Root(string name, Scene scene)
        {
            GameObject go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            return go.transform;
        }

        private static GameObject Child(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static Material Material(string name, Shader shader, Color color)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;
            material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.25f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
