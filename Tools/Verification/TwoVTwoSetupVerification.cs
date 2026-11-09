using System;
using System.Linq;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TwoVTwoSetupVerification
{
    public static object Run()
    {
        Require(!Application.isPlaying, "Run setup verification outside Play Mode.");
        TwoVTwoPrototypeSetup.SetUp();
        var scene = SceneManager.GetActiveScene();
        int Count() => scene.GetRootGameObjects().Sum(go => go.GetComponentsInChildren<Transform>(true).Length);
        int before = Count();
        var camera = UnityEngine.Object.FindAnyObjectByType<GameplayCamera>();
        var settings = new SerializedObject(camera); float distance = settings.FindProperty("distance").floatValue;
        try
        {
            settings.FindProperty("distance").floatValue = distance + 0.2f; settings.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            TwoVTwoPrototypeSetup.SetUp();
            settings.Update(); Require(Mathf.Approximately(settings.FindProperty("distance").floatValue, distance + 0.2f), "Repeat setup preserves camera tuning.");
            var roster = UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
            Require(Count() == before && roster.Members.Count == 4, "No duplicate scene objects; four members.");
            Require(roster.Members.Count(p => p.Team == PrototypeTeam.Blue) == 2 && roster.Members.Count(p => p.Team == PrototypeTeam.White) == 2, "Two distinct members per side.");
            Require(roster.Members.Select(p => p.PlayerId).Distinct().Count() == 4 && roster.Members.Select(p => p.StartPosition).Distinct().Count() == 4, "Unique identities and restart markers.");
            Require(UnityEngine.Object.FindObjectsByType<BallController>().Length == 1 && UnityEngine.Object.FindObjectsByType<PrototypeMatch>().Length == 1 && UnityEngine.Object.FindObjectsByType<PlayerSelection>().Length == 1, "Single gameplay authorities.");
            var teams = UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>();
            Require(teams.Length == 2, "Two shared team contexts.");
            foreach (var team in teams) Require(team.AttackingGoal.ScoringTeam == team.Team && team.DefendedGoal.ScoringTeam != team.Team && team.Roster == roster, "Explicit correct goals and shared roster.");
            foreach (var p in roster.Members) Require(p.Interaction.Ball != null && p.GetComponents<SwimmerMotor>().Length == 1 && p.GetComponents<PrototypeSwimmerAI>().Length == 1, "One motor/AI and valid shared ball.");
            return new { objectsBefore = before, objectsAfter = Count(), blue = 2, white = 2, preservedTuning = true };
        }
        finally
        {
            settings.Update(); settings.FindProperty("distance").floatValue = distance; settings.ApplyModifiedPropertiesWithoutUndo(); camera.SnapToTarget();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
