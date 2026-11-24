using System;
using System.Linq;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ToduStudios.WaterPolo.Editor
{
    public static class ProfessionalRefereeSetup
    {
        const string Root = "Assets/Equipment/ProfessionalReferee/";
        const string Arena = "Assets/Scenes/OlympicMatchArena.unity";
        const string RigName = "Professional Official";
        const string Menu = "Tools/Water Polo/Arena/";

        [MenuItem(Menu + "Apply Professional Officials")]
        public static void Apply()
        {
            var scene = OpenArena();
            var prefab = PrepareOfficial();
            var originals = Object.FindObjectsByType<RefereeVisual>();
            if (originals.Length != 2) throw new InvalidOperationException("Expected the two existing referee roots.");
            foreach (var original in originals) Bind(original, prefab);
            Save(scene);
        }

        public static GameObject PrepareCandidate() => PrepareOfficial();

        public static void Bind(RefereeVisual original, GameObject prefab)
        {
            var child = original.transform.Find(RigName);
            if (child == null)
            {
                var instance = Application.isPlaying ? Object.Instantiate(prefab, original.transform) :
                    (GameObject)PrefabUtility.InstantiatePrefab(prefab, original.transform);
                instance.name = RigName;
                child = instance.transform;
            }
            var state = new SerializedObject(original);
            state.FindProperty("professional").objectReferenceValue = child.GetComponent<ProfessionalRefereePose>();
            state.ApplyModifiedPropertiesWithoutUndo();
            foreach (Transform model in original.transform) model.gameObject.SetActive(model == child);
        }

        [MenuItem(Menu + "Restore Previous Officials")]
        public static void Restore()
        {
            var scene = OpenArena();
            foreach (var original in Object.FindObjectsByType<RefereeVisual>())
                foreach (Transform model in original.transform)
                    model.gameObject.SetActive(model.name != RigName);
            Save(scene);
        }

        static Scene OpenArena()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("Open a saved scene in Edit Mode before configuring officials.");
            return EditorSceneManager.OpenScene(Arena);
        }

        static void Save(Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Failed to save the officials.");
        }

        static GameObject PrepareOfficial()
        {
            string prefabPath = Root + "Professional Official.prefab";
            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (saved != null) return saved;
            string modelPath = Root + "PoolsideOfficial.fbx";
            var avatar = AssetDatabase.LoadAllAssetsAtPath(AnimatedAthleteImport.Root + "/Models/WaterPoloAthlete.fbx")
                .OfType<Avatar>().Single(a => a.isHuman && a.isValid);
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = avatar;
            importer.preserveHierarchy = true;
            importer.importAnimation = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var walk = AssetDatabase.LoadAllAssetsAtPath(AuthenticAthleteMotionSetup.Root + "/MeshyDeckSource.fbx")
                .OfType<AnimationClip>().Single(clip => clip.name == "Walking" && clip.humanMotion);
            var instance = Object.Instantiate(model);
            try
            {
                instance.name = RigName;
                instance.transform.localScale = Vector3.one * (1.8f / 1.9f);
                var animator = instance.GetComponent<Animator>();
                if (animator == null) animator = instance.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.applyRootMotion = false;
                var renderer = instance.GetComponentsInChildren<SkinnedMeshRenderer>().Single();
                renderer.sharedMaterial = MaterialAsset();
                renderer.localBounds = new Bounds(Vector3.up * .9f, new Vector3(2f, 2.45f, 2f));
                var pose = new SerializedObject(instance.AddComponent<ProfessionalRefereePose>());
                pose.FindProperty("walking").objectReferenceValue = walk;
                pose.ApplyModifiedPropertiesWithoutUndo();
                return PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            }
            finally { Object.DestroyImmediate(instance); }
        }

        static Material MaterialAsset()
        {
            string path = Root + "Official Uniform and Skin.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetTexture("_BaseMap", ReadTexture("OfficialColor", true, false));
            material.SetTexture("_BumpMap", ReadTexture("OfficialNormal", false, true));
            material.SetTexture("_MetallicGlossMap", ReadTexture("OfficialSmoothness", false, false));
            material.SetFloat("_BumpScale", .5f);
            material.SetFloat("_Smoothness", 1f);
            material.SetFloat("_Metallic", 0f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Texture2D ReadTexture(string name, bool srgb, bool normal)
        {
            string path = Root + name + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = srgb;
            importer.mipmapEnabled = true;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings {
                name = "Android", overridden = true, maxTextureSize = 1024,
                format = name == "OfficialSmoothness" ? TextureImporterFormat.ASTC_8x8 : TextureImporterFormat.ASTC_6x6
            });
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
