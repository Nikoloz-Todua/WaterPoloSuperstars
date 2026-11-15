using System;
using System.IO;
using System.Linq;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ToduStudios.WaterPolo.Editor
{
    [InitializeOnLoad]
    public static class WaterVisualTestSetup
    {
        public const string ScenePath = "Assets/Scenes/WaterVisualTest.unity";
        public const string Root = "Assets/WaterVisuals";
        public const string Vendor = "Assets/Shaders/Uber Stylized Water";
        private const string EntryKey = "WaterVisualTest.PreviousEntry";
        private const string RestoreKey = "WaterVisualTest.RestoreEntry";

        static WaterVisualTestSetup()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(RestoreKey, false)) return;
                string old = SessionState.GetString(EntryKey, "");
                EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(old) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(old);
                SessionState.SetBool(RestoreKey, false);
            };
        }

        [MenuItem("Tools/Water Polo/Water Visuals/Set Up WaterVisualTest")]
        public static void SetUp()
        {
            RequireSavedEditMode();
            EnsureFolder(Root); EnsureFolder(Root + "/Materials"); EnsureFolder(Root + "/Textures");
            EnsureFolder(Root + "/Settings"); EnsureFolder(Root + "/Meshes");
            if (Shader.Find("UberStylizedWater") == null) throw new InvalidOperationException("Import the licensed prebuilt UberStylizedWater shader first; see docs/WATER_VISUALS.md.");
            CreateAssets();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) BuildScene();
            else EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            Debug.Log("WaterVisualTest ready. Tools > Water Polo > Water Visuals > Play WaterVisualTest. Keys 1/2/3 quality, V viewpoint. PrototypePool unchanged.");
        }

        [MenuItem("Tools/Water Polo/Water Visuals/Play WaterVisualTest")]
        public static void PlayTest()
        {
            RequireSavedEditMode();
            SetUp();
            SessionState.SetString(EntryKey, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            SessionState.SetBool(RestoreKey, true);
            EditorSceneManager.playModeStartScene = null;
            EditorApplication.isPlaying = true;
        }

        private static void RequireSavedEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save intended scene changes first; setup discards nothing.");
        }

        private static void CreateAssets()
        {
            CreateTiles();
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Root + "/Settings/PoolVisual_Renderer.asset");
            if (renderer == null)
            {
                AssetDatabase.CopyAsset("Assets/Settings/Mobile_Renderer.asset", Root + "/Settings/PoolVisual_Renderer.asset");
                renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Root + "/Settings/PoolVisual_Renderer.asset");
            }
            string[] names = { "Low", "Balanced", "High" };
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i], path = Root + "/Settings/PoolVisual_" + name + ".asset";
                var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (pipeline == null)
                {
                    pipeline = Object.Instantiate(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset"));
                    pipeline.name = "PoolVisual_" + name;
                    pipeline.supportsCameraDepthTexture = true;
                    pipeline.supportsCameraOpaqueTexture = i != 0;
                    pipeline.renderScale = i == 0 ? 0.85f : 1f;
                    pipeline.msaaSampleCount = i == 2 ? 4 : 2;
                    var data = new SerializedObject(pipeline);
                    data.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                    data.FindProperty("m_OpaqueDownsampling").intValue = i == 2 ? 0 : 1;
                    data.ApplyModifiedPropertiesWithoutUndo();
                    AssetDatabase.CreateAsset(pipeline, path);
                }
                var material = WaterMaterial(name, i);
                string profilePath = Root + "/Settings/WaterQuality_" + name + ".asset";
                if (AssetDatabase.LoadAssetAtPath<PoolWaterQualityProfile>(profilePath) != null) continue;
                var profile = ScriptableObject.CreateInstance<PoolWaterQualityProfile>();
                profile.waterMaterial = material; profile.testPipeline = pipeline;
                profile.planarReflections = i == 2; profile.reflectionScale = 0.4f;
                profile.costNotes = i == 0 ? "Mobile candidate: 0.85 render scale, 2x MSAA, depth, ripple normals and baked reflections. No opaque copy, refraction, caustics or second camera. Profile on hardware."
                    : i == 1 ? "Mobile candidate: 1.0 scale, 2x MSAA, half-resolution opaque copy, refraction, caustics and baked reflections. No second camera. Profile on hardware."
                    : "Editor visual reference: full-resolution opaque copy, 4x MSAA, refraction, caustics and 0.4-scale planar reflection (second scene render every frame). Not a phone performance claim.";
                AssetDatabase.CreateAsset(profile, profilePath);
            }
        }

        private static Material WaterMaterial(string name, int quality)
        {
            string path = Root + "/Materials/CompetitionPool_" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material; // Preserve useful Inspector tuning.
            material = new Material(Shader.Find("UberStylizedWater")) { name = "CompetitionPool_" + name, renderQueue = 3000, enableInstancing = true };
            material.SetColor("_Color_Shallow", new Color(0.18f, 0.69f, 0.76f, 0.12f));
            material.SetColor("_Color_Deep", new Color(0.025f, 0.38f, 0.48f, 0.58f));
            material.SetFloat("_Water_Depth", 2.5f); material.SetFloat("_WorldSpaceDepth", 1f);
            material.SetFloat("_ShoreFade", 1f); material.SetFloat("_ShoreFade_Smoothness", 0.045f);
            material.SetFloat("_Enable_SurfaceFoam", 0f); material.SetFloat("_Enable_Intersection", 0f);
            material.SetFloat("_DistanceMask_Start", 12f); material.SetFloat("_DistanceMask_Fade", 35f);
            Toggle(material, "_ENABLESHORELINE", false); Toggle(material, "_ENABLE_UNDERWATERLAYER", false);
            Toggle(material, "_ENABLEWAVE", false); Toggle(material, "_ENABLENORMAL", true);
            Toggle(material, "_ENABLEREFRACTION", quality != 0); Toggle(material, "_ENABLECAUSTICS", quality != 0);
            Toggle(material, "_ENABLEPLANERREFLECTION", quality == 2);
            material.SetTexture("_Normal_Map", AssetDatabase.LoadAssetAtPath<Texture2D>(Vendor + "/Textures/Normal 1.png"));
            material.SetFloat("_Normal_Strength", 0.48f); material.SetFloat("_Normal_DistanceStrength", 0.18f);
            material.SetFloat("_Normal_Pan", 0.055f); material.SetFloat("_Normal_Scale", 1.8f);
            material.SetFloat("_Reflection_Strength", 0.62f); material.SetFloat("_Reflection_Fresnel", 3f);
            material.SetFloat("_Reflection_Distortion", 0.6f);
            material.SetFloat("_Refraction_Strength", 0.045f); material.SetFloat("_Refraction_Distance_Strength", 0.008f);
            material.SetFloat("_Refraction_Distance_Fade", 22f);
            material.SetTexture("_Caustics_Map", AssetDatabase.LoadAssetAtPath<Texture2D>(Vendor + "/Textures/Caustic 1.png"));
            material.SetTexture("_Caustics_Distortion_Map", AssetDatabase.LoadAssetAtPath<Texture2D>(Vendor + "/Textures/Noise 1.png"));
            material.SetFloat("_Caustics_Depth", -2.05f); material.SetFloat("_Caustics_Pan", 0.075f);
            material.SetFloat("_Caustics_Scale", 1.25f); material.SetFloat("_Caustics_Strength", 5.5f);
            material.SetFloat("_Caustics_Distortion_Strength", 0.14f); material.SetFloat("_Caustics_Distortion_Scale", 1.4f);
            material.SetFloat("_Caustics_Start", 0f); material.SetFloat("_Caustics_Fade", 55f);
            material.SetColor("_Specular_Color", new Color(1.8f, 1.8f, 1.7f, 1f));
            material.SetFloat("_Specular_Size", 0.7f); material.SetFloat("_Specular_Spread", 0.12f);
            material.SetFloat("_Specular_Hardness", 0f);
            material.SetColor("_ShadowColor", new Color(0.05f, 0.17f, 0.2f, 0.08f));
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void Toggle(Material material, string key, bool on)
        {
            material.SetFloat(key, on ? 1f : 0f);
            if (on) material.EnableKeyword(key); else material.DisableKeyword(key);
        }

        private static void CreateTiles()
        {
            string path = Root + "/Textures/PoolTiles.png";
            if (File.Exists(path)) return;
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
            var colors = new Color[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                bool grout = x % 64 < 2 || y % 64 < 2;
                float variation = Mathf.Sin((x / 64) * 2.2f + (y / 64) * 5.1f) * 0.025f;
                float grain = Mathf.Sin(x * 1.75f + y * 2.37f) * 0.004f;
                colors[y * size + x] = grout ? new Color(0.47f, 0.62f, 0.65f) : new Color(0.81f + variation + grain, 0.9f + variation, 0.91f + variation);
            }
            texture.SetPixels(colors); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Repeat; importer.mipmapEnabled = true; importer.anisoLevel = 4;
            importer.maxTextureSize = 512; importer.SaveAndReimport();
        }

        private static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Water Visual Test — presentation only").transform;
            var shell = Child("Tiled Competition Pool", root);
            var tile = Lit("Pool ceramic", new Color(0.9f, 0.97f, 1f), 0.32f);
            tile.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/PoolTiles.png")); EditorUtility.SetDirty(tile);
            var dark = Lit("Lane navy", new Color(0.025f, 0.075f, 0.11f), 0.25f);
            var deck = Lit("Warm stone deck", new Color(0.64f, 0.66f, 0.63f), 0.15f);
            var white = Lit("White enamel", new Color(0.86f, 0.89f, 0.9f), 0.4f);
            var steel = Lit("Brushed steel", new Color(0.28f, 0.35f, 0.38f), 0.7f, 0.8f);
            var accent = Lit("Competition blue", new Color(0.015f, 0.17f, 0.3f), 0.35f);
            Surface("Tiled Floor", shell, new Vector3(0f, -2.05f, 0f), Vector3.right * 14f, Vector3.forward * 25f, tile);
            Surface("Far tiled wall", shell, new Vector3(0f, -1.025f, 12.5f), Vector3.right * 14f, Vector3.up * 2.05f, tile);
            Surface("Near tiled wall", shell, new Vector3(0f, -1.025f, -12.5f), Vector3.left * 14f, Vector3.up * 2.05f, tile);
            Surface("Left tiled wall", shell, new Vector3(-7f, -1.025f, 0f), Vector3.forward * 25f, Vector3.up * 2.05f, tile);
            Surface("Right tiled wall", shell, new Vector3(7f, -1.025f, 0f), Vector3.back * 25f, Vector3.up * 2.05f, tile);
            for (int lane = -2; lane <= 2; lane++)
            {
                Box("Floor lane " + (lane + 3), shell, new Vector3(lane * 2.4f, -2.042f, 0f), new Vector3(0.16f, 0.012f, 21f), dark);
                for (int end = -1; end <= 1; end += 2)
                    Box("Lane end T", shell, new Vector3(lane * 2.4f, -2.04f, end * 10.5f), new Vector3(1.2f, 0.015f, 0.16f), dark);
            }
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Box("Side deck", shell, new Vector3(sign * 9f, 0.03f, 0f), new Vector3(4f, 0.14f, 31f), deck);
                Box("End deck", shell, new Vector3(0f, 0.03f, sign * 14f), new Vector3(14f, 0.14f, 3f), deck);
                Box("Side coping", shell, new Vector3(sign * 7.1f, 0.06f, 0f), new Vector3(0.25f, 0.15f, 25.5f), white);
                Box("End coping", shell, new Vector3(0f, 0.06f, sign * 12.6f), new Vector3(14f, 0.15f, 0.25f), white);
                Box("Waterline band", shell, new Vector3(sign * 6.997f, -0.15f, 0f), new Vector3(0.012f, 0.16f, 25f), accent);
            }
            var architecture = Child("Pool Hall and Reflection Landmarks", root);
            var glass = Lit("Daylight panels", new Color(0.66f, 0.84f, 0.94f), 0.8f);
            glass.EnableKeyword("_EMISSION"); glass.SetColor("_EmissionColor", new Color(0.35f, 0.48f, 0.56f)); EditorUtility.SetDirty(glass);
            Box("Far hall wall", architecture, new Vector3(0f, 3f, 16f), new Vector3(24f, 6f, 0.3f), white);
            Box("Far blue fascia", architecture, new Vector3(0f, 1f, 15.8f), new Vector3(24f, 1.6f, 0.1f), accent);
            for (int x = -2; x <= 2; x++)
            {
                Box("High daylight window", architecture, new Vector3(x * 4.4f, 4f, 15.78f), new Vector3(3.9f, 2.5f, 0.08f), glass);
                Box("Window mullion", architecture, new Vector3(x * 4.4f, 4f, 15.7f), new Vector3(0.06f, 2.5f, 0.12f), steel);
            }
            for (int z = -1; z <= 2; z++)
            {
                Box("Ceiling daylight strip", architecture, new Vector3(0f, 7.5f, z * 7f), new Vector3(19f, 0.08f, 1.3f), glass);
                Box("Roof beam", architecture, new Vector3(0f, 7.3f, z * 7f + 1f), new Vector3(21f, 0.18f, 0.2f), steel);
            }
            // Sparse, bounded deck details provide recognizable reflection silhouettes.
            for (int i = 0; i < 5; i++)
            {
                float x = -4.8f + i * 2.4f;
                Box("Starting block support", architecture, new Vector3(x, 0.35f, 13.5f), new Vector3(0.22f, 0.6f, 0.35f), steel);
                Box("Starting block", architecture, new Vector3(x, 0.68f, 13.5f), new Vector3(0.65f, 0.12f, 0.6f), accent);
            }
            var skin = Lit("Test skin", new Color(0.67f, 0.35f, 0.2f), 0.25f);
            var skinLight = Lit("Test skin light", new Color(0.88f, 0.59f, 0.37f), 0.24f);
            var red = Lit("Test red suit", new Color(0.68f, 0.035f, 0.025f), 0.3f);
            var athletes = Child("Opaque Submerged Test Characters", root);
            BuildSwimmer(athletes, "Surface swimmer", new Vector3(-1.5f, -0.22f, -3.4f), skinLight, accent, white, false);
            BuildSwimmer(athletes, "Upright waterline swimmer", new Vector3(1.1f, -0.45f, -2.5f), skin, red, white, true);
            BuildSwimmer(athletes, "Fully submerged swimmer", new Vector3(0.2f, -1.15f, 2f), skinLight, red, white, false);
            var water = Surface("Competition Water Surface", root, Vector3.zero, Vector3.right * 14f, Vector3.forward * 25f,
                AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/CompetitionPool_High.mat"));
            water.gameObject.layer = 4; water.shadowCastingMode = ShadowCastingMode.Off; water.receiveShadows = false;
            var camera = new GameObject("Water Test Camera", typeof(Camera), typeof(AudioListener), typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
            camera.tag = "MainCamera"; camera.transform.SetParent(root); camera.fieldOfView = 49f;
            camera.nearClipPlane = 0.1f; camera.farClipPlane = 90f; camera.allowHDR = true;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.56f, 0.7f, 0.79f);
            var cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.requiresDepthOption = CameraOverrideOption.On; cameraData.requiresColorOption = CameraOverrideOption.On;
            var views = Child("Viewpoints — V cycles", root);
            var overview = View("Overview", views, new Vector3(11f, 9f, -18f), new Vector3(0f, -0.4f, 0f));
            var detail = View("Submerged anatomy detail", views, new Vector3(5.3f, 4.3f, -8.8f), new Vector3(-0.3f, -0.45f, -2f));
            var grazing = View("Reflection angle", views, new Vector3(4f, 2.1f, -11f), new Vector3(0f, 0f, 3f));
            camera.transform.SetPositionAndRotation(detail.position, detail.rotation);
            var cameraController = camera.gameObject.AddComponent<PoolWaterTestCamera>();
            cameraController.viewpoints = new[] { detail, overview, grazing };
            var sun = new GameObject("Daylight", typeof(Light)).GetComponent<Light>(); sun.transform.SetParent(root);
            sun.type = LightType.Directional; sun.intensity = 1.6f; sun.color = new Color(1f, 0.96f, 0.89f);
            sun.shadows = LightShadows.Soft; sun.shadowStrength = 0.6f; sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            RenderSettings.sun = sun; RenderSettings.skybox = null; RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.63f, 0.72f, 0.79f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.53f, 0.59f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.31f, 0.35f);
            RenderSettings.reflectionIntensity = 0.8f;
            var manager = new GameObject("Optional High Quality Planar Reflections", typeof(PlanarReflectionManager)).GetComponent<PlanarReflectionManager>();
            manager.transform.SetParent(root); manager.runOnEditMode = false; manager.enabled = false;
            var volume = manager.gameObject.AddComponent<PlanarReflectionVolume>(); volume.enabled = false;
            volume.isGlobal = false; volume.volumeSize = new Vector3(45f, 30f, 60f); volume.blendDistance = 4f;
            volume.reflectionTargets.Add(water.gameObject); volume.reflectionPlaneOffset = 0f; volume.reflectSkybox = false;
            volume.renderScale = 0.4f; volume.hideReflectionCamera = true;
            var controller = root.gameObject.AddComponent<PoolWaterVisuals>(); controller.waterSurface = water; controller.viewCamera = camera;
            controller.reflectionManager = manager; controller.reflectionVolume = volume;
            controller.profiles = new[] { "Low", "Balanced", "High" }.Select(n => AssetDatabase.LoadAssetAtPath<PoolWaterQualityProfile>(Root + "/Settings/WaterQuality_" + n + ".asset")).ToArray();
            // Bake one small static environment cubemap for the two no-planar presets.
            var probe = new GameObject("Baked Pool Hall Reflection", typeof(ReflectionProbe)).GetComponent<ReflectionProbe>();
            probe.transform.SetParent(root); probe.transform.position = new Vector3(0f, 0.5f, 0f);
            probe.size = new Vector3(30f, 18f, 45f); probe.center = new Vector3(0f, 2f, 0f); probe.resolution = 128;
            probe.boxProjection = true; probe.cullingMask = ~(1 << 4); probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
            probe.backgroundColor = camera.backgroundColor; probe.mode = ReflectionProbeMode.Baked;
            EditorSceneManager.SaveScene(scene, ScenePath);
            string cubemapPath = Root + "/Textures/PoolHallReflection.exr";
            BakePoolProbe(probe, shell, architecture, cubemapPath);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = controller.gameObject;
        }

        public static void BakePoolProbe(ReflectionProbe probe, Transform shell, Transform architecture, string path)
        {
            foreach (var group in new[] { shell, architecture })
                foreach (var item in group.GetComponentsInChildren<Renderer>())
                    GameObjectUtility.SetStaticEditorFlags(item.gameObject, StaticEditorFlags.ReflectionProbeStatic);
            if (!Lightmapping.BakeReflectionProbe(probe, path)) throw new IOException("Could not bake pool hall reflection.");
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureShape = TextureImporterShape.TextureCube; importer.generateCubemap = TextureImporterGenerateCubemap.AutoCubemap;
            importer.SaveAndReimport(); // BakeReflectionProbe configures specular cubemap convolution.
            probe.mode = ReflectionProbeMode.Custom; probe.customBakedTexture = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            if (probe.customBakedTexture == null) throw new IOException("Baked cubemap did not import.");
        }

        private static void BuildSwimmer(Transform parent, string name, Vector3 position, Material skin, Material suit, Material cap, bool upright)
        {
            var rig = Child(name, parent); rig.localPosition = position;
            if (!upright) rig.localRotation = Quaternion.Euler(78f, 0f, 0f);
            Sphere("Torso", rig, new Vector3(0f, 0f, 0f), new Vector3(0.52f, 0.75f, 0.3f), skin);
            Sphere("Suit", rig, new Vector3(0f, -0.4f, 0f), new Vector3(0.46f, 0.3f, 0.31f), suit);
            Sphere("Head", rig, new Vector3(0f, 0.61f, 0f), new Vector3(0.33f, 0.42f, 0.34f), skin);
            Sphere("Cap", rig, new Vector3(0f, 0.73f, -0.025f), new Vector3(0.35f, 0.23f, 0.34f), cap);
            var la = Limb("Left arm", rig, new Vector3(-0.25f, 0.24f, 0f), new Vector3(-0.78f, -0.13f, 0.17f), 0.09f, skin);
            var ra = Limb("Right arm", rig, new Vector3(0.25f, 0.24f, 0f), new Vector3(0.78f, -0.13f, 0.17f), 0.09f, skin);
            var ll = Limb("Left leg", rig, new Vector3(-0.13f, -0.45f, 0f), new Vector3(-0.22f, -1.24f, 0.1f), 0.12f, skin);
            var rl = Limb("Right leg", rig, new Vector3(0.13f, -0.45f, 0f), new Vector3(0.22f, -1.24f, -0.05f), 0.12f, skin);
            var animation = rig.gameObject.AddComponent<PoolWaterTestSwimmer>(); animation.leftArm = la; animation.rightArm = ra;
            animation.leftLeg = ll; animation.rightLeg = rl;
        }
        private static Transform Limb(string name, Transform parent, Vector3 start, Vector3 end, float radius, Material material)
        {
            var pivot = Child(name, parent); pivot.localPosition = start;
            pivot.localRotation = Quaternion.FromToRotation(Vector3.up, end - start);
            var part = GameObject.CreatePrimitive(PrimitiveType.Capsule); part.name = name + " opaque mesh"; part.transform.SetParent(pivot, false);
            part.transform.localPosition = Vector3.up * (Vector3.Distance(start, end) * 0.5f);
            part.transform.localScale = new Vector3(radius * 2f, Vector3.Distance(start, end) * 0.5f, radius * 2f);
            part.GetComponent<Renderer>().sharedMaterial = material; Object.DestroyImmediate(part.GetComponent<Collider>());
            return pivot;
        }
        private static void Sphere(string name, Transform parent, Vector3 p, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = p; go.transform.localScale = size; go.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }
        private static Transform View(string name, Transform parent, Vector3 p, Vector3 target)
        {
            var go = Child(name, parent); go.position = p; go.rotation = Quaternion.LookRotation(target - p, Vector3.up); return go;
        }
        private static Transform Child(string name, Transform parent)
        {
            var go = new GameObject(name).transform; go.SetParent(parent, false); return go;
        }
        private static void Box(string name, Transform parent, Vector3 p, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = p; go.transform.localScale = size; go.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }
        internal static MeshRenderer Surface(string name, Transform parent, Vector3 p, Vector3 right, Vector3 forward, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false); go.transform.localPosition = p;
            string path = Root + "/Meshes/" + name.Replace(" ", "") + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh { name = name };
                mesh.vertices = new[] { (-right - forward) * 0.5f, (right - forward) * 0.5f, (-right + forward) * 0.5f, (right + forward) * 0.5f };
                mesh.uv = new[] { Vector2.zero, Vector2.right * right.magnitude, Vector2.up * forward.magnitude, new Vector2(right.magnitude, forward.magnitude) };
                mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 }; mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, path);
            }
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material; return renderer;
        }
        private static Material Lit(string name, Color color, float smoothness, float metallic = 0f)
        {
            string path = Root + "/Materials/" + name.Replace(" ", "") + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path); if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, enableInstancing = true };
            material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", smoothness); material.SetFloat("_Metallic", metallic);
            AssetDatabase.CreateAsset(material, path); return material;
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/')); EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }
    }
}
