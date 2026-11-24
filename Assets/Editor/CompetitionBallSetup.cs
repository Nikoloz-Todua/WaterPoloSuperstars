using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ToduStudios.WaterPolo.Editor
{
    public static class CompetitionBallSetup
    {
        const string Arena = "Assets/Scenes/OlympicMatchArena.unity";
        const string Root = "Assets/Equipment/CompetitionBall/";
        const string Menu = "Tools/Water Polo/Arena/";

        [MenuItem(Menu + "Apply Competition Ball and Dribbling")]
        public static void Apply()
        {
            var (ball, view) = Open();
            var mesh = ReadMesh();
            var material = ReadMaterial();
            ApplyVisual(ball, view, mesh, material, true);
        }

        [MenuItem(Menu + "Restore Previous Ball and Carry")]
        public static void Restore()
        {
            var (ball, view) = Open();
            var mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            var material = AssetDatabase.LoadAssetAtPath<Material>(
                AssetDatabase.GUIDToAssetPath("07fcfedc08f880b4aa52c88d2f653787"));
            ApplyVisual(ball, view, mesh, material, false);
        }

        static (BallController Ball, MeshFilter View) Open()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before changing the saved ball presentation.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save existing scene changes first.");
            EditorSceneManager.OpenScene(Arena);
            var ball = Object.FindObjectsByType<BallController>().Single();
            return (ball, ball.transform.Find("Visual").GetComponent<MeshFilter>());
        }

        static void ApplyVisual(BallController ball, MeshFilter view, Mesh mesh, Material material, bool enabled)
        {
            if (mesh == null || material == null) throw new InvalidOperationException("Missing ball visual asset.");
            view.sharedMesh = mesh;
            view.GetComponent<MeshRenderer>().sharedMaterial = material;
            var state = new SerializedObject(ball);
            state.FindProperty("automaticPickup").boolValue = enabled;
            state.FindProperty("swimDribbling").boolValue = enabled;
            var reach = state.FindProperty("automaticPickupDistance");
            if (enabled && Mathf.Approximately(reach.floatValue, 1.32f)) reach.floatValue = 1.20f;
            var lead = state.FindProperty("dribbleHeadGap");
            if (enabled && Mathf.Approximately(lead.floatValue, .32f)) lead.floatValue = .36f;
            state.FindProperty("rollingVisual").objectReferenceValue = enabled ? view.transform : null;
            state.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(ball.gameObject.scene);
            if (!EditorSceneManager.SaveScene(ball.gameObject.scene))
                throw new InvalidOperationException("Could not save the arena ball binding.");
            AssetDatabase.SaveAssets();
        }

        static Mesh ReadMesh()
        {
            string path = Root + "CompetitionBall.fbx";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = false;
            importer.importNormals = ModelImporterNormals.Calculate;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.SaveAndReimport();
            var mesh = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().Single();
            if (Mathf.Abs(mesh.bounds.size.x - 1f) > .015f || mesh.bounds.center.magnitude > .015f)
                throw new InvalidOperationException("Competition ball import must have a centered unit diameter.");
            return mesh;
        }

        static Material ReadMaterial()
        {
            string path = Root + "Competition Ball Rubber.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetTexture("_BaseMap", ReadTexture("BallColor", false, true, TextureImporterFormat.ASTC_6x6));
            material.SetTexture("_BumpMap", ReadTexture("BallNormal", true, false, TextureImporterFormat.ASTC_6x6));
            material.SetTexture("_MetallicGlossMap", ReadTexture("BallSmoothness", false, false, TextureImporterFormat.ASTC_8x8));
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 1f);
            material.SetFloat("_BumpScale", .55f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Texture2D ReadTexture(string name, bool normal, bool srgb, TextureImporterFormat format)
        {
            string path = Root + name + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = srgb;
            importer.mipmapEnabled = true;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings {
                name = "Android", overridden = true, maxTextureSize = 1024, format = format
            });
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
