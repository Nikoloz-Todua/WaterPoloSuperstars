using System;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ToduStudios.WaterPolo.Editor
{
    public static class InteractiveWaterSetup
    {
        private const string Arena = "Assets/Scenes/OlympicMatchArena.unity";
        private const string ContactMaterial = "Assets/WaterVisuals/Materials/PoolContactWater.mat";
        private const string Menu = "Tools/Water Polo/Arena/";

        [MenuItem(Menu + "Apply Interactive Water")]
        public static void Apply() => SetProduction(true);

        [MenuItem(Menu + "Restore Previous Water Presentation")]
        public static void Restore() => SetProduction(false);

        private static void SetProduction(bool enabled)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before changing saved water presentation.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save intended scene edits before applying water presentation.");

            var scene = SceneManager.GetActiveScene();
            if (scene.path != Arena) scene = EditorSceneManager.OpenScene(Arena, OpenSceneMode.Single);
            var effects = UnityEngine.Object.FindAnyObjectByType<WaterInteractionEffects>();
            var water = UnityEngine.Object.FindAnyObjectByType<PoolWaterVisuals>();
            if (effects == null || water == null || effects.gameObject.scene != scene || water.gameObject.scene != scene)
                throw new InvalidOperationException("The production arena water presentation is incomplete.");

            var serialized = new SerializedObject(effects);
            serialized.FindProperty("useContactSampling").boolValue = enabled;
            if (enabled)
            {
                var materialSlot = serialized.FindProperty("interactiveWaterMaterial");
                if (materialSlot.objectReferenceValue == null)
                {
                    var material = AssetDatabase.LoadAssetAtPath<Material>(ContactMaterial);
                    if (material == null)
                    {
                        var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/PoolContactWater.shader");
                        if (shader == null || ShaderUtil.ShaderHasError(shader))
                            throw new InvalidOperationException("Pool contact shader failed to import.");
                        material = new Material(shader) { name = "Pool Contact Water" };
                        AssetDatabase.CreateAsset(material, ContactMaterial);
                    }
                    materialSlot.objectReferenceValue = material;
                }
            }

            bool changed = serialized.ApplyModifiedProperties();
            if (water.interactiveWaterFinish != enabled)
            {
                Undo.RecordObject(water, "Change pool water finish");
                water.interactiveWaterFinish = enabled;
                changed = true;
            }
            if (changed)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("The arena did not save.");
            }
        }

        [MenuItem(Menu + "Water Preview/Off")]
        private static void Off() => Preview(WaterEffectsQuality.Off);
        [MenuItem(Menu + "Water Preview/Low")]
        private static void Low() => Preview(WaterEffectsQuality.Low);
        [MenuItem(Menu + "Water Preview/Medium")]
        private static void Medium() => Preview(WaterEffectsQuality.Standard);
        [MenuItem(Menu + "Water Preview/High")]
        private static void High() => Preview(WaterEffectsQuality.High);

        private static void Preview(WaterEffectsQuality quality)
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play the arena first.");
            var water = UnityEngine.Object.FindAnyObjectByType<WaterInteractionEffects>();
            if (water != null) water.Quality = quality;
        }
    }
}
