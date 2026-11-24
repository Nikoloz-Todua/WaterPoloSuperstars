using System;
using System.IO;
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
    [InitializeOnLoad]
    public static class ProductionAthleteSetup
    {
        public const string Root = "Assets/ProductionAthletes";
        public const string Preview = Root + "/AthletePreview.unity";
        const string Arena = "Assets/Scenes/OlympicMatchArena.unity";
        const string Recovery = "Assets/Scenes/Recovery/OlympicMatchArena_PreProductionAthletes.unity";
        const string Model = Root + "/Models/Athlete001.fbx";
        const string Menu = "Tools/Water Polo/Athletes/";

        [MenuItem(Menu + "Open Athlete Preview")]
        public static void OpenPreview()
        {
            RequireSaved();
            if (!File.Exists(Preview)) BuildPreview();
            else EditorSceneManager.OpenScene(Preview);
        }

        [MenuItem(Menu + "Play Athlete Preview")]
        public static void PlayPreview()
        {
            OpenPreview();
            SessionState.SetBool("ProductionAthlete.RestoreEntry", true);
            SessionState.SetString("ProductionAthlete.PreviousEntry", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(Preview);
            EditorApplication.EnterPlaymode();
        }

        static ProductionAthleteSetup()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                if (change != PlayModeStateChange.EnteredEditMode ||
                    !SessionState.GetBool("ProductionAthlete.RestoreEntry", false)) return;
                string previous = SessionState.GetString("ProductionAthlete.PreviousEntry", "");
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
                SessionState.SetBool("ProductionAthlete.RestoreEntry", false);
            };
        }

        [MenuItem(Menu + "Apply Production Athlete Visuals")]
        public static void ApplyToArena()
        {
            RequireSaved();
            var scene = EditorSceneManager.OpenScene(Arena);
            var rigged = ArenaRigs();
            if (rigged.All(visual => visual.TryGetComponent<AthleteEquipment>(out _))) return;
            if (!File.Exists(Recovery) && !AssetDatabase.CopyAsset(Arena, Recovery))
                throw new IOException("Could not create a pre-athlete arena recovery.");

            try
            {
                foreach (var visual in rigged) Attach(visual);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the athlete visual bindings.");
            }
            catch
            {
                EditorSceneManager.OpenScene(Arena);
                throw;
            }
        }

        [MenuItem(Menu + "Restore Previous Athlete Visuals")]
        public static void RestoreArena()
        {
            RequireSaved();
            var scene = EditorSceneManager.OpenScene(Arena);
            bool changed = false;
            foreach (var visual in ArenaRigs())
            {
                if (!visual.TryGetComponent<AthleteEquipment>(out var kit)) continue;
                var state = new SerializedObject(kit);
                var fallback = state.FindProperty("fallback");
                for (int i = 0; i < fallback.arraySize; i++)
                    if (fallback.GetArrayElementAtIndex(i).objectReferenceValue is Renderer renderer)
                    {
                        renderer.enabled = true;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                    }
                foreach (string property in new[] { "body", "briefs", "cap", "number" })
                    if (state.FindProperty(property).objectReferenceValue is Renderer part) Object.DestroyImmediate(part.gameObject);
                Object.DestroyImmediate(kit);
                changed = true;
            }
            if (changed && !EditorSceneManager.SaveScene(scene))
                throw new IOException("Could not save the restored athlete visuals.");
        }

        static RiggedSwimmerVisual[] ArenaRigs()
        {
            var players = Object.FindObjectsByType<SwimmerIdentity>();
            foreach (var team in new[] { PrototypeTeam.Blue, PrototypeTeam.White })
                if (players.Count(p => p.Team == team && p.IsFieldPlayer) != 6 ||
                    players.Count(p => p.Team == team && !p.IsFieldPlayer) != 1)
                    throw new InvalidOperationException("The production arena requires six fields and one keeper per team.");
            return players.Select(player =>
            {
                var rig = player.GetComponentInChildren<RiggedSwimmerVisual>();
                if (rig == null || rig.identity != player || !rig.GetComponent<Animator>().avatar.isValid)
                    throw new InvalidOperationException($"Invalid existing rig on {player.name}.");
                return rig;
            }).ToArray();
        }

        static void RequireSaved()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save the open scene edits first.");
        }

        public static void PrepareImport()
        {
            RequireSaved();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var model = (ModelImporter)AssetImporter.GetAtPath(Model);
            model.animationType = ModelImporterAnimationType.Generic;
            model.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
            model.importAnimation = false;
            model.preserveHierarchy = true;
            model.optimizeGameObjects = false;
            model.optimizeBones = true;
            model.materialImportMode = ModelImporterMaterialImportMode.None;
            model.importBlendShapes = false;
            model.importCameras = false;
            model.importLights = false;
            model.SaveAndReimport();

            ImportTexture("SkinAlbedo", TextureImporterType.Default, true, 1024, TextureImporterFormat.ASTC_6x6);
            ImportTexture("SkinNormal", TextureImporterType.NormalMap, false, 1024, TextureImporterFormat.ASTC_6x6);
            ImportTexture("SkinMetallicSmoothness", TextureImporterType.Default, false, 1024, TextureImporterFormat.ASTC_8x8);
            ImportTexture("CapNumbers", TextureImporterType.Default, true, 512, TextureImporterFormat.ASTC_4x4);
        }

        static Texture2D Texture(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/" + name + ".png");

        static void ImportTexture(string name, TextureImporterType type, bool srgb, int androidSize, TextureImporterFormat format)
        {
            var texture = (TextureImporter)AssetImporter.GetAtPath(Root + "/Textures/" + name + ".png");
            texture.textureType = type;
            texture.sRGBTexture = srgb;
            texture.alphaIsTransparency = name == "CapNumbers";
            texture.alphaSource = type == TextureImporterType.NormalMap ? TextureImporterAlphaSource.None : TextureImporterAlphaSource.FromInput;
            texture.maxTextureSize = name == "CapNumbers" ? 512 : 2048;
            texture.mipmapEnabled = true;
            texture.isReadable = false;
            texture.textureCompression = TextureImporterCompression.CompressedHQ;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Trilinear;
            texture.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android", overridden = true, maxTextureSize = androidSize,
                format = format, compressionQuality = 100
            });
            texture.SaveAndReimport();
        }

        static Material MaterialAsset(string name, Color color, float smoothness = .27f, Action<Material> configure = null)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
            material.SetFloat("_Smoothness", smoothness);
            configure?.Invoke(material);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Material NumberMaterial(string name, Color color) => MaterialAsset(name, color, .18f, material =>
        {
            material.SetTexture("_BaseMap", Texture("CapNumbers"));
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", .38f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.renderQueue = (int)RenderQueue.AlphaTest;
            material.SetShaderPassEnabled("ShadowCaster", false);
        });

        static AthleteKitStyle Style(string name, Material[] cap, Material[] keeper, Material suit, Material number, Material keeperNumber)
        {
            string path = Root + "/Materials/" + name + ".asset";
            var style = AssetDatabase.LoadAssetAtPath<AthleteKitStyle>(path);
            if (style != null) return style;
            style = ScriptableObject.CreateInstance<AthleteKitStyle>();
            var serialized = new SerializedObject(style);
            SetMaterials(serialized.FindProperty("cap"), cap);
            SetMaterials(serialized.FindProperty("goalkeeperCap"), keeper);
            serialized.FindProperty("briefs").objectReferenceValue = suit;
            serialized.FindProperty("fieldNumber").objectReferenceValue = number;
            serialized.FindProperty("goalkeeperNumber").objectReferenceValue = keeperNumber;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(style, path);
            return style;
        }

        static void SetMaterials(SerializedProperty array, Object[] materials)
        {
            array.arraySize = materials.Length;
            for (int i = 0; i < materials.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = materials[i];
        }

        static (Material skin, AthleteKitStyle blue, AthleteKitStyle white) Materials()
        {
            if (!AssetDatabase.IsValidFolder(Root + "/Materials")) AssetDatabase.CreateFolder(Root, "Materials");
            var skin = MaterialAsset("Atlas Skin", Color.white, .82f, material =>
            {
                material.SetTexture("_BaseMap", Texture("SkinAlbedo"));
                material.SetTexture("_BumpMap", Texture("SkinNormal"));
                material.SetTexture("_MetallicGlossMap", Texture("SkinMetallicSmoothness"));
                material.SetFloat("_BumpScale", .65f);
                material.EnableKeyword("_NORMALMAP");
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
            });
            var whiteInk = NumberMaterial("Chalk Number", new Color(.97f, .98f, 1f));
            var darkInk = NumberMaterial("Ink Number", new Color(.022f, .05f, .11f));
            var red = MaterialAsset("Keeper Red Fabric", new Color(.63f, .027f, .042f));
            var blueCloth = MaterialAsset("Blue Cap Fabric", new Color(.025f, .145f, .55f));
            var whiteCloth = MaterialAsset("White Cap Fabric", new Color(.88f, .91f, .97f));
            var blueTrim = MaterialAsset("Blue Binding and Guards", new Color(.03f, .095f, .30f));
            var whiteTrim = MaterialAsset("White Binding and Guards", new Color(.67f, .73f, .85f));
            var blueSuit = MaterialAsset("Blue Competition Briefs", new Color(.025f, .077f, .30f), .23f);
            var whiteSuit = MaterialAsset("White Competition Briefs", new Color(.73f, .80f, .91f), .23f);
            return (skin,
                Style("Blue Team Kit", new[] { blueCloth, blueTrim }, new[] { red, blueTrim }, blueSuit, whiteInk, whiteInk),
                Style("White Team Kit", new[] { whiteCloth, whiteTrim }, new[] { red, whiteTrim }, whiteSuit, darkInk, whiteInk));
        }

        public static AthleteEquipment Attach(RiggedSwimmerVisual visual)
        {
            if (visual.TryGetComponent<AthleteEquipment>(out var existing)) return existing;
            var shell = AssetDatabase.LoadAssetAtPath<GameObject>(Model);
            var parts = shell.GetComponentsInChildren<SkinnedMeshRenderer>();
            string[] names = { "AthleteBody", "CompetitionBriefs", "WaterPoloCap", "CapNumber" };
            var required = names.Select(name => parts.Single(part => part.name == name)).ToArray();
            var fallback = visual.GetComponentsInChildren<Renderer>();
            var root = visual.transform;
            Transform Map(Transform source)
            {
                string path = AnimationUtility.CalculateTransformPath(source, shell.transform);
                var destination = path.Length == 0 ? root : root.Find(path);
                if (destination == null) throw new InvalidOperationException($"Missing existing rig transform {path}");
                Matrix4x4 a = shell.transform.worldToLocalMatrix * source.localToWorldMatrix;
                Matrix4x4 b = root.worldToLocalMatrix * destination.localToWorldMatrix;
                for (int i = 0; i < 16; i++)
                    if (Mathf.Abs(a[i] - b[i]) > .001f)
                        throw new InvalidOperationException($"Rest transform mismatch at {path}, matrix index {i}: {a[i]} and {b[i]}");
                return destination;
            }

            // Resolve every reference before changing the visible actor.
            var bindings = required.Select(part => (root: Map(part.rootBone), bones: part.bones.Select(Map).ToArray())).ToArray();
            var (skin, blue, white) = Materials();
            var kit = visual.gameObject.AddComponent<AthleteEquipment>();
            var serialized = new SerializedObject(kit);
            serialized.FindProperty("identity").objectReferenceValue = visual.identity;
            serialized.FindProperty("skin").objectReferenceValue = skin;
            serialized.FindProperty("blue").objectReferenceValue = blue;
            serialized.FindProperty("white").objectReferenceValue = white;
            SetMaterials(serialized.FindProperty("fallback"), fallback);
            string[] properties = { "body", "briefs", "cap", "number" };
            var style = visual.identity != null && visual.identity.Team == PrototypeTeam.White ? white : blue;
            bool goalkeeper = visual.identity != null && !visual.identity.IsFieldPlayer;
            Material[][] materials = { new[] { skin }, new[] { style.Briefs }, style.Cap(goalkeeper), new[] { style.Number(goalkeeper) } };

            for (int i = 0; i < required.Length; i++)
            {
                var source = required[i];
                var part = new GameObject(source.name, typeof(SkinnedMeshRenderer)).GetComponent<SkinnedMeshRenderer>();
                // Interchange renderers may carry an axis or FBX unit transform at their parent.
                Matrix4x4 matrix = shell.transform.worldToLocalMatrix * source.transform.localToWorldMatrix;
                part.transform.SetParent(visual.transform, false);
                part.transform.SetLocalPositionAndRotation(matrix.GetPosition(), matrix.rotation);
                part.transform.localScale = matrix.lossyScale;
                part.gameObject.layer = visual.gameObject.layer;
                part.sharedMesh = source.sharedMesh;
                part.rootBone = bindings[i].root;
                part.bones = bindings[i].bones;
                part.sharedMaterials = materials[i];
                part.quality = SkinQuality.Bone4;
                part.localBounds = new Bounds(new Vector3(0, .95f, 0), new Vector3(2.7f, 2.8f, 2.7f));
                part.shadowCastingMode = i == 3 ? ShadowCastingMode.Off : ShadowCastingMode.On;
                part.receiveShadows = i != 3;
                serialized.FindProperty(properties[i]).objectReferenceValue = part;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            // Keep fallback renderers available on the original rig.
            foreach (var renderer in fallback)
            {
                renderer.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }

            return kit;
        }

        static void BuildPreview()
        {
            PrepareImport();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AnimatedAthleteSetup.Prefab);
            var athlete = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            athlete.transform.localPosition = Vector3.zero;
            var visual = athlete.GetComponentInChildren<RiggedSwimmerVisual>();
            Attach(visual);
            var camera = new GameObject("Athlete Preview Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            camera.nearClipPlane = .025f;
            camera.fieldOfView = 34f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.085f, .12f, .18f);
            camera.transform.SetPositionAndRotation(new Vector3(.4f, 1.23f, 3.9f), Quaternion.Euler(4f, 186f, 0));
            var preview = camera.gameObject.AddComponent<AthletePreviewCamera>();
            preview.athlete = visual;

            var key = new GameObject("Key", typeof(Light)).GetComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.4f;
            key.transform.rotation = Quaternion.Euler(42, 154, 0);
            var fill = new GameObject("Fill", typeof(Light)).GetComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(.74f, .83f, 1f);
            fill.intensity = .55f;
            fill.transform.rotation = Quaternion.Euler(32, -44, 0);
            RenderSettings.ambientLight = new Color(.43f, .47f, .54f);
            EditorSceneManager.SaveScene(scene, Preview);
        }
    }
}

