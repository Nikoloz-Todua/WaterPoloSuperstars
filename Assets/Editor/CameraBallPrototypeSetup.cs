using System;
using System.Linq;
using ToduStudios.WaterPolo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ToduStudios.WaterPolo.Editor
{
    public static class CameraBallPrototypeSetup
    {
        [MenuItem("Tools/Water Polo/Set Up Camera and Ball Prototype")]
        public static void SetUp()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            {
                Debug.LogWarning("Exit Play Mode and save scene changes before setting up the camera and ball. Nothing was changed.");
                return;
            }

            MovementPrototypeBuilder.CreateMovementPrototype();
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != MovementPrototypeBuilder.ScenePath)
                throw new InvalidOperationException("The movement prototype must be open.");
            Transform actors = scene.GetRootGameObjects().Single(go => go.name == "Actors").transform;
            SwimmerMotor swimmer = actors.Find("Swimmer").GetComponent<SwimmerMotor>();
            PoolBounds bounds = scene.GetRootGameObjects().Single(go => go.name == "Gameplay")
                .transform.Find("PoolBounds").GetComponent<PoolBounds>();
            Camera camera = scene.GetRootGameObjects().Single(go => go.name == "CameraRig")
                .transform.Find("Main Camera").GetComponent<Camera>();
            BallController[] balls = scene.GetRootGameObjects().SelectMany(go => go.GetComponentsInChildren<BallController>(true)).ToArray();
            if (balls.Length > 1 || (actors.Find("Ball") != null && actors.Find("Ball").GetComponent<BallController>() == null))
                throw new InvalidOperationException("Unexpected ball objects; refusing to duplicate or replace them.");

            BallController ball = balls.SingleOrDefault();
            if (ball == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    throw new InvalidOperationException("URP Lit shader is unavailable.");
                const string materialPath = "Assets/Materials/Prototype/Ball.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(shader) { name = "Ball" };
                    material.SetColor("_BaseColor", new Color(1f, 0.83f, 0.04f));
                    material.SetFloat("_Smoothness", 0.35f);
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                GameObject root = new GameObject("Ball");
                root.transform.SetParent(actors, false);
                Vector3 start = bounds.ClampPosition(swimmer.transform.position + swimmer.transform.forward * 1.65f, 0.14f);
                start.y = bounds.Waterline + 0.07f;
                root.transform.position = start;
                Rigidbody body = root.AddComponent<Rigidbody>();
                body.mass = 0.45f;
                body.useGravity = false;
                body.constraints = RigidbodyConstraints.FreezePositionY;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                root.AddComponent<SphereCollider>().radius = 0.14f;
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visual.name = "Visual";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localScale = Vector3.one * 0.28f;
                UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
                visual.GetComponent<Renderer>().sharedMaterial = material;
                ball = root.AddComponent<BallController>();
            }
            Assign(ball, "poolBounds", bounds);

            Transform anchor = swimmer.transform.Find("BallCarryAnchor");
            if (anchor == null)
            {
                anchor = new GameObject("BallCarryAnchor").transform;
                anchor.SetParent(swimmer.transform, false);
                anchor.localPosition = new Vector3(0.45f, 0.07f, 0.85f);
            }
            SwimmerBallInteraction interaction = swimmer.GetComponent<SwimmerBallInteraction>();
            if (interaction == null)
                interaction = swimmer.gameObject.AddComponent<SwimmerBallInteraction>();
            Assign(interaction, "ball", ball);
            Assign(interaction, "carryAnchor", anchor);
            if (swimmer.GetComponent<BallInteractionInput>() == null)
                swimmer.gameObject.AddComponent<BallInteractionInput>();

            GameplayCamera gameplayCamera = camera.GetComponent<GameplayCamera>();
            if (gameplayCamera == null)
            {
                gameplayCamera = camera.gameObject.AddComponent<GameplayCamera>();
                camera.fieldOfView = 50f;
            }
            Assign(gameplayCamera, "target", swimmer);
            Assign(gameplayCamera, "poolBounds", bounds);
            gameplayCamera.SnapToTarget();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not save the upgraded prototype scene.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = swimmer.gameObject;
            Debug.Log("Camera and ball prototype ready: E collects a nearby ball; Space shoots. Existing movement and scene objects were preserved.");
        }

        private static void Assign(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            SerializedObject data = new SerializedObject(target);
            data.FindProperty(field).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
