// Run in Edit Mode. Repeats setup, verifies references/counts, and restores temporary camera tuning.
using System;
using System.Linq;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AIPrototypeSetupVerification
{
    public static object Run()
    {
        Require(!Application.isPlaying && !SceneManager.GetActiveScene().isDirty, "Run on a saved Edit Mode scene.");
        BasicAIPrototypeSetup.SetUp();
        var scene = SceneManager.GetActiveScene();
        int Objects() => scene.GetRootGameObjects().Sum(go => go.GetComponentsInChildren<Transform>(true).Length);
        int count = Objects();
        var roster = UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
        var camera = UnityEngine.Object.FindAnyObjectByType<GameplayCamera>();
        var data = new SerializedObject(camera);
        float distance = data.FindProperty("distance").floatValue;
        try
        {
            data.FindProperty("distance").floatValue = distance + 0.2f;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            BasicAIPrototypeSetup.SetUp();
            data.Update();
            Require(Mathf.Approximately(data.FindProperty("distance").floatValue, distance + 0.2f), "Repeat setup preserves camera tuning.");
            Require(Objects() == count, "Repeat setup cannot duplicate objects.");
            Require(roster.Members.Count == 3 && roster.Members.Select(p => p.PlayerId).Distinct().Count() == 3, "Exactly three unique roster identities.");
            Require(UnityEngine.Object.FindObjectsByType<SwimmerMotor>().Length == 3 && UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>().Length == 3, "One motor and one shared AI adapter per swimmer.");
            Require(UnityEngine.Object.FindObjectsByType<BallController>().Length == 1 && UnityEngine.Object.FindObjectsByType<PlayerSelection>().Length == 1 && UnityEngine.Object.FindObjectsByType<PrototypeMatch>().Length == 1 && UnityEngine.Object.FindObjectsByType<GoalMouth>().Length == 2, "No duplicate ball/control/score/goal authority.");
            foreach (var player in roster.Members)
            {
                Require(player.StartPosition != null && player.Interaction.Ball != null && player.transform.Find("SelectionRing") != null, "Actor start, ball and marker references.");
                var ai = new SerializedObject(player.GetComponent<PrototypeSwimmerAI>());
                foreach (string field in new[] { "roster", "selection", "ball", "pool", "match", "defendedGoal" })
                    Require(ai.FindProperty(field).objectReferenceValue != null, "AI reference: " + field);
                Require(player.GetComponents<PrototypeSwimmerAI>().Length == 1 && player.GetComponents<SwimmerPassing>().Length == 1, "No duplicate AI or passing components.");
            }
            return new { objectsBefore = count, objectsAfter = Objects(), players = roster.Members.Count, preservedCameraTuning = true, allReferencesAssigned = true };
        }
        finally
        {
            data.Update(); data.FindProperty("distance").floatValue = distance;
            data.ApplyModifiedPropertiesWithoutUndo(); camera.SnapToTarget();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
