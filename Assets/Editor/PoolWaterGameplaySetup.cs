using System;
using System.Linq;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ToduStudios.WaterPolo.Editor
{
    public static class PoolWaterGameplaySetup
    {
        public const string RootName = "Verified Pool Water Presentation";
        [MenuItem("Tools/Water Polo/Water Visuals/Apply Verified Water to PrototypePool")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save intended edits before applying pool presentation.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(WaterVisualTestSetup.ScenePath) == null)
                throw new InvalidOperationException("Set up and verify WaterVisualTest first.");
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/PrototypePool.unity", OpenSceneMode.Single);
            var bounds = Object.FindAnyObjectByType<PoolBounds>();
            if (bounds == null || Object.FindAnyObjectByType<PrototypeRoster>().Members.Count != 14) throw new InvalidOperationException("Existing full-team pool required.");
            var environment = GameObject.Find("Environment").transform;
            var existing = environment.Find(RootName);
            if (existing != null)
            {
                var configured = existing.GetComponent<PoolWaterVisuals>();
                var baked = existing.GetComponentInChildren<ReflectionProbe>();
                if (configured == null || configured.waterSurface == null || existing.GetComponent<PoolWaterPreferenceBridge>() == null
                    || baked == null || baked.texture == null)
                    throw new InvalidOperationException("Existing pool presentation is incomplete; inspect its references before repair. No authored objects replaced.");
                return; // Never rebuild tuned visuals or duplicate observers.
            }
            var material = AssetDatabase.LoadAssetAtPath<Material>(WaterVisualTestSetup.Root + "/Materials/Poolceramic.mat");
            if (material == null) throw new InvalidOperationException("Verified pool tile material is missing.");
            var root = new GameObject(RootName).transform; root.SetParent(environment, false);
            root.position = new Vector3(bounds.transform.position.x, bounds.Waterline, bounds.transform.position.z);
            foreach (string name in new[] { "WaterSurface", "PoolFloor" }) environment.Find(name).GetComponent<Renderer>().enabled = false;
            var marks = environment.Find("PoolMarkings");
            foreach (var renderer in marks.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
            float w = bounds.Width, l = bounds.Length, depth = 2.05f;
            var floor = WaterVisualTestSetup.Surface("Match Tiled Floor", root, Vector3.down * depth, Vector3.right * w, Vector3.forward * l, material);
            WaterVisualTestSetup.Surface("Match Far Tiled Wall", root, new Vector3(0f,-depth/2,l/2), Vector3.right*w, Vector3.up*depth, material);
            WaterVisualTestSetup.Surface("Match Near Tiled Wall", root, new Vector3(0f,-depth/2,-l/2), Vector3.left*w, Vector3.up*depth, material);
            WaterVisualTestSetup.Surface("Match Left Tiled Wall", root, new Vector3(-w/2,-depth/2,0f), Vector3.forward*l, Vector3.up*depth, material);
            WaterVisualTestSetup.Surface("Match Right Tiled Wall", root, new Vector3(w/2,-depth/2,0f), Vector3.back*l, Vector3.up*depth, material);
            // Preserve regulation cross-pool lines on the real floor instead of floating opaque strips just below the surface.
            foreach (Transform mark in marks)
            {
                var copy = new GameObject("Floor " + mark.name, typeof(MeshFilter), typeof(MeshRenderer)); copy.transform.SetParent(root, false);
                copy.transform.localPosition = new Vector3(mark.localPosition.x, -depth + 0.012f, mark.localPosition.z);
                copy.transform.localRotation = mark.localRotation; copy.transform.localScale = mark.localScale;
                copy.GetComponent<MeshFilter>().sharedMesh = mark.GetComponent<MeshFilter>().sharedMesh;
                copy.GetComponent<Renderer>().sharedMaterial = mark.GetComponent<Renderer>().sharedMaterial;
            }
            var surface = WaterVisualTestSetup.Surface("Match Transparent Water", root, Vector3.zero, Vector3.right*w, Vector3.forward*l,
                AssetDatabase.LoadAssetAtPath<Material>(WaterVisualTestSetup.Root + "/Materials/CompetitionPool_Balanced.mat"));
            surface.gameObject.layer = 4; surface.shadowCastingMode = ShadowCastingMode.Off; surface.receiveShadows = false;
            var water = root.gameObject.AddComponent<PoolWaterVisuals>(); water.waterSurface = surface;
            water.viewCamera = Object.FindAnyObjectByType<GameplayCamera>().GetComponent<Camera>();
            water.useIsolatedTestPipeline = false; water.keyboardShortcuts = false; // Never intercept gameplay keys or replace its pipeline.
            water.profiles = new[] { "Low", "Balanced", "High" }.Select(n => AssetDatabase.LoadAssetAtPath<PoolWaterQualityProfile>(WaterVisualTestSetup.Root + "/Settings/WaterQuality_"+n+".asset")).ToArray();
            var bridge = root.gameObject.AddComponent<PoolWaterPreferenceBridge>(); bridge.water = water; bridge.preferences = Object.FindAnyObjectByType<GamePreferences>();
            var probe = new GameObject("Baked Match Pool Reflection", typeof(ReflectionProbe)).GetComponent<ReflectionProbe>(); probe.transform.SetParent(root, false);
            probe.transform.localPosition = Vector3.up * 0.5f; probe.size = new Vector3(w+16f,18f,l+16f); probe.boxProjection = true;
            probe.resolution = 128; probe.cullingMask = ~(1 << 4); probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
            probe.backgroundColor = new Color(.035f,.075f,.11f); probe.mode = ReflectionProbeMode.Baked;
            // Baked probes see static geometry. Set flags only for the bake and restore every original object's flags.
            var staticObjects = Object.FindObjectsByType<Renderer>().Where(r => r.GetComponentInParent<SwimmerIdentity>() == null
                && r.GetComponentInParent<BallController>() == null).Select(r => r.gameObject).Distinct().ToArray();
            var flags = staticObjects.Select(GameObjectUtility.GetStaticEditorFlags).ToArray();
            try
            {
                foreach (var item in staticObjects) GameObjectUtility.SetStaticEditorFlags(item, StaticEditorFlags.ReflectionProbeStatic);
                WaterVisualTestSetup.BakePoolProbe(probe, root, root, WaterVisualTestSetup.Root + "/Textures/MatchPoolReflection.exr");
            }
            finally { for (int i = 0; i < staticObjects.Length; i++) GameObjectUtility.SetStaticEditorFlags(staticObjects[i], flags[i]); }
            if (root.GetComponentsInChildren<Collider>().Length != 0) throw new InvalidOperationException("Presentation must not add colliders.");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("Verified transparent pool presentation applied. Original gameplay, physics, camera and local preference authority retained; no planar reflection camera in match.");
        }
    }
}
