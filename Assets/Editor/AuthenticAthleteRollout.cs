using System;
using System.IO;
using System.Linq;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ToduStudios.WaterPolo.Editor
{
    public static class AuthenticAthleteRollout
    {
        const string Arena = "Assets/Scenes/OlympicMatchArena.unity";
        const string Recovery = "Assets/Scenes/Recovery/OlympicMatchArena_PreAuthenticAthletes.unity";
        const string Menu = "Tools/Water Polo/Athletes/";

        [MenuItem(Menu + "Apply Authentic Athlete Motion and Equipment")]
        public static void Apply() => ChangeArena(true);

        [MenuItem(Menu + "Restore Milestone 27 Athlete Motion and Equipment")]
        public static void Restore() => ChangeArena(false);

        static void ChangeArena(bool install)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before changing the saved arena.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save existing scene changes before changing athlete bindings.");

            var scene = EditorSceneManager.OpenScene(Arena);
            var athletes = Object.FindObjectsByType<RiggedSwimmerVisual>().Where(v => v.identity != null).ToArray();
            if (athletes.Length != 14 || athletes.Any(v => !v.TryGetComponent<AthleteEquipment>(out _)))
                throw new InvalidOperationException("Apply the Milestone 27 visual bindings to the fourteen arena athletes first.");
            if (install && !File.Exists(Recovery) && !AssetDatabase.CopyAsset(Arena, Recovery))
                throw new IOException("Could not make an arena recovery scene.");

            try
            {
                var prototype = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    AnimatedAthleteImport.Root + "/AnimatedSwimmer.controller");
                foreach (var athlete in athletes)
                {
                    if (install)
                    {
                        AuthenticAthleteEquipmentSetup.Bind(athlete);
                        AuthenticAthleteMotionSetup.Bind(athlete);
                    }
                    else
                    {
                        AuthenticAthleteEquipmentSetup.RestorePrevious(athlete);
                        athlete.deckWalk = null;
                        athlete.useAuthoredWaterline = false;
                        var animator = athlete.GetComponent<Animator>();
                        animator.runtimeAnimatorController = prototype;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(athlete);
                    }
                }
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the arena athlete bindings.");
            }
            catch
            {
                EditorSceneManager.OpenScene(Arena);
                throw;
            }
        }
    }
}
