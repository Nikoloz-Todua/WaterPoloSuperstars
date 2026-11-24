using System;
using System.Linq;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ToduStudios.WaterPolo.Editor
{
    public static class AuthenticAthleteEquipmentSetup
    {
        const string Root = ProductionAthleteSetup.Root + "/AuthenticEquipment";
        const string Headgear = Root + "/PremiumHeadgear.fbx";

        [MenuItem("Tools/Water Polo/Athletes/Configure Premium Equipment Preview")]
        public static void ConfigurePreview()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before configuring the preview.");
            ProductionAthleteSetup.OpenPreview();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var importer = (ModelImporter)AssetImporter.GetAtPath(Headgear);
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = false;
            importer.preserveHierarchy = true;
            importer.SaveAndReimport();

            var capTextures = ImportFabric("Cap", 1024);
            var briefsTextures = ImportFabric("Briefs", 512);
            var blueFabric = Fabric("Blue Cap", new Color(.075f, .29f, .95f), capTextures);
            var whiteFabric = Fabric("White Cap", new Color(.96f, .96f, .98f), capTextures);
            var redFabric = Fabric("Red Keeper Cap", new Color(.97f, .048f, .06f), capTextures);
            var blueTrim = Fabric("Blue Guards", new Color(.085f, .24f, .69f), capTextures);
            var whiteTrim = Fabric("White Guards", new Color(.77f, .81f, .89f), capTextures);
            var redTrim = Fabric("Red Keeper Guards", new Color(.81f, .045f, .057f), capTextures);
            var blueBriefs = Fabric("Blue Briefs", new Color(.035f, .11f, .42f), briefsTextures);
            var whiteBriefs = Fabric("White Briefs", new Color(.79f, .84f, .93f), briefsTextures);
            var blue = Kit("Blue Premium Kit", new[] { blueFabric, blueTrim },
                new[] { redFabric, redTrim }, blueBriefs, "Chalk Number");
            var white = Kit("White Premium Kit", new[] { whiteFabric, whiteTrim },
                new[] { redFabric, redTrim }, whiteBriefs, "Ink Number");

            var preview = UnityEngine.Object.FindAnyObjectByType<AthletePreviewCamera>();
            Bind(preview.athlete, blue, white);
            EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);
            EditorSceneManager.SaveScene(preview.gameObject.scene);
            AssetDatabase.SaveAssets();
        }

        public static void Bind(RiggedSwimmerVisual rig) => Bind(rig,
            AssetDatabase.LoadAssetAtPath<AthleteKitStyle>(Root + "/Blue Premium Kit.asset"),
            AssetDatabase.LoadAssetAtPath<AthleteKitStyle>(Root + "/White Premium Kit.asset"));

        public static void RestorePrevious(RiggedSwimmerVisual rig)
        {
            string previous = ProductionAthleteSetup.Root;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(previous + "/Models/Athlete001.fbx");
            var equipment = new SerializedObject(rig.GetComponent<AthleteEquipment>());
            ReplaceMesh(equipment, "cap", source, "WaterPoloCap", rig.transform);
            ReplaceMesh(equipment, "number", source, "CapNumber", rig.transform);
            equipment.FindProperty("blue").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AthleteKitStyle>(previous + "/Materials/Blue Team Kit.asset");
            equipment.FindProperty("white").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AthleteKitStyle>(previous + "/Materials/White Team Kit.asset");
            equipment.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Bind(RiggedSwimmerVisual rig, AthleteKitStyle blue, AthleteKitStyle white)
        {
            if (blue == null || white == null)
                throw new InvalidOperationException("Configure both preview kits before binding the equipment.");
            var equipment = new SerializedObject(rig.GetComponent<AthleteEquipment>());
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Headgear);
            ReplaceMesh(equipment, "cap", source, "PremiumCapGameplay", rig.transform);
            ReplaceMesh(equipment, "number", source, "PremiumNumberGameplay", rig.transform);
            equipment.FindProperty("blue").objectReferenceValue = blue;
            equipment.FindProperty("white").objectReferenceValue = white;
            equipment.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ReplaceMesh(SerializedObject equipment, string slot, GameObject source, string part, Transform root)
        {
            var renderers = source.GetComponentsInChildren<SkinnedMeshRenderer>();
            var mesh = renderers.Single(renderer => renderer.name == part);
            var destination = (SkinnedMeshRenderer)equipment.FindProperty(slot).objectReferenceValue;
            Transform Map(Transform bone)
            {
                var path = AnimationUtility.CalculateTransformPath(bone, source.transform);
                var mapped = root.Find(path);
                if (mapped == null) throw new InvalidOperationException($"Headgear bone path missing from the shipped rig: {path}");
                Matrix4x4 a = source.transform.worldToLocalMatrix * bone.localToWorldMatrix;
                Matrix4x4 b = root.worldToLocalMatrix * mapped.localToWorldMatrix;
                for (int i = 0; i < 16; i++)
                    if (Mathf.Abs(a[i] - b[i]) > .001f)
                        throw new InvalidOperationException($"Headgear rest bind mismatch on {path}.");
                return mapped;
            }

            var bones = mesh.bones.Select(Map).ToArray();
            var rootBone = Map(mesh.rootBone);
            var transform = source.transform.worldToLocalMatrix * mesh.transform.localToWorldMatrix;
            destination.transform.SetLocalPositionAndRotation(transform.GetPosition(), transform.rotation);
            destination.transform.localScale = transform.lossyScale;
            destination.sharedMesh = mesh.sharedMesh;
            destination.rootBone = rootBone;
            destination.bones = bones;
            EditorUtility.SetDirty(destination.transform);
            EditorUtility.SetDirty(destination);
        }

        static Texture2D[] ImportFabric(string name, int androidSize) => new[]
        {
            ImportTexture(name + "Neutral", true, false, androidSize, TextureImporterFormat.ASTC_6x6),
            ImportTexture(name + "Normal", false, true, androidSize, TextureImporterFormat.ASTC_6x6),
            ImportTexture(name + "Smoothness", false, false, androidSize, TextureImporterFormat.ASTC_8x8),
        };

        static Texture2D ImportTexture(string name, bool color, bool normal, int size, TextureImporterFormat format)
        {
            string path = Root + "/Textures/" + name + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = color;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android", overridden = true, maxTextureSize = size,
                format = format, textureCompression = TextureImporterCompression.CompressedHQ
            });
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material Fabric(string name, Color color, Texture2D[] maps)
        {
            string path = Root + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            material.SetTexture("_BaseMap", maps[0]);
            material.SetTexture("_BumpMap", maps[1]);
            material.SetTexture("_MetallicGlossMap", maps[2]);
            material.SetFloat("_BumpScale", .55f);
            material.SetFloat("_Metallic", 0);
            material.SetFloat("_Smoothness", 1);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static AthleteKitStyle Kit(string name, Material[] field, Material[] keeper, Material briefs, string number)
        {
            string path = Root + "/" + name + ".asset";
            var kit = AssetDatabase.LoadAssetAtPath<AthleteKitStyle>(path);
            if (kit != null) return kit;
            kit = ScriptableObject.CreateInstance<AthleteKitStyle>();
            var state = new SerializedObject(kit);
            void SetCaps(string property, Material[] materials)
            {
                var cap = state.FindProperty(property);
                cap.arraySize = materials.Length;
                for (int i = 0; i < materials.Length; i++) cap.GetArrayElementAtIndex(i).objectReferenceValue = materials[i];
            }
            Material Ink(string ink) => AssetDatabase.LoadAssetAtPath<Material>(
                ProductionAthleteSetup.Root + "/Materials/" + ink + ".mat");
            SetCaps("cap", field);
            SetCaps("goalkeeperCap", keeper);
            state.FindProperty("briefs").objectReferenceValue = briefs;
            state.FindProperty("fieldNumber").objectReferenceValue = Ink(number);
            state.FindProperty("goalkeeperNumber").objectReferenceValue = Ink("Chalk Number");
            state.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(kit, path);
            return kit;
        }
    }
}
