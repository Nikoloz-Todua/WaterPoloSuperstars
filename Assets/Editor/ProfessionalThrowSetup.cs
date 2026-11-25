using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ToduStudios.WaterPolo.Editor
{
    public static class ProfessionalThrowSetup
    {
        private const string Menu = "Tools/Water Polo/Arena/";
        private const string Arena = "Assets/Scenes/OlympicMatchArena.unity";

        [MenuItem(Menu + "Apply Professional Throwing")]
        public static void Apply() => SetEnabled(true);

        [MenuItem(Menu + "Restore Previous Throwing")]
        public static void Restore() => SetEnabled(false);

        private static void SetEnabled(bool enable)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before changing production throwing.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save existing scene edits before changing production throwing.");

            Scene arena = SceneManager.GetActiveScene();
            if (arena.path != Arena) arena = EditorSceneManager.OpenScene(Arena);
            var ball = UnityEngine.Object.FindAnyObjectByType<BallController>();
            if (ball == null || ball.gameObject.scene != arena)
                throw new InvalidOperationException("The production arena ball is missing.");

            var serialized = new SerializedObject(ball);
            var option = serialized.FindProperty("professionalThrows");
            if (option.boolValue == enable) return;
            option.boolValue = enable;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(arena);
            if (!EditorSceneManager.SaveScene(arena))
                throw new InvalidOperationException("Could not save production throwing.");
        }
    }
}
