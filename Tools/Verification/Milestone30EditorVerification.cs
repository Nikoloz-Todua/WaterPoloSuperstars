using System;
using System.IO;
using System.Linq;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Editor;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Milestone30EditorVerification
{
    private const string Arena = "Assets/Scenes/OlympicMatchArena.unity";

    public static object SaveDiagnosticCopy()
    {
        var scene = SceneManager.GetActiveScene();
        const string path = "Logs/Milestone30/Recovery/EditorSceneDiagnostic.unity";
        return new { scene.path, scene.isDirty, copied = EditorSceneManager.SaveScene(scene, path, true), diagnosticPath = path };
    }

    public static object VerifyWaterRecovery()
    {
        RequireReady();
        InteractiveWaterSetup.Apply();
        var effects = UnityEngine.Object.FindAnyObjectByType<WaterInteractionEffects>();
        if (effects == null || !effects.UsesContactSampling) throw new InvalidOperationException("Interactive effects are not assigned.");
        var before = File.ReadAllBytes(Arena);
        InteractiveWaterSetup.Restore();
        if (effects.UsesContactSampling) throw new InvalidOperationException("Previous water did not restore.");
        InteractiveWaterSetup.Apply();
        var reapplied = File.ReadAllBytes(Arena);
        InteractiveWaterSetup.Apply();
        var repeat = File.ReadAllBytes(Arena);
        return new { guid = AssetDatabase.AssetPathToGUID(Arena), byteIdenticalReapply = before.SequenceEqual(reapplied),
            byteIdenticalRepeat = reapplied.SequenceEqual(repeat), clean = !SceneManager.GetActiveScene().isDirty };
    }

    public static object SaveUnchangedActiveScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != Arena)
            throw new InvalidOperationException("Open the production arena in Edit Mode first.");
        string path = "Logs/Milestone30/Recovery/EditorSceneDiagnostic.unity";
        if (!EditorSceneManager.SaveScene(scene, path, true)) throw new IOException("Could not write diagnostic scene copy.");
        if (!File.ReadAllBytes(path).SequenceEqual(File.ReadAllBytes(Arena)))
            throw new InvalidOperationException("The in-memory scene differs from disk. Inspect the diagnostic copy before saving.");
        return new { saved = EditorSceneManager.SaveScene(scene), scene.isDirty };
    }

    public static object ApplyAndVerifyBriefs()
    {
        RequireReady();
        void CheckBriefMeshes(int triangles)
        {
            var roster = UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
            if (roster == null || roster.Members.Count != 14) throw new InvalidOperationException("Expected fourteen saved athletes.");
            foreach (var member in roster.Members)
            {
                var kit = member.GetComponentInChildren<AthleteEquipment>();
                var serialized = new SerializedObject(kit);
                var briefs = (SkinnedMeshRenderer)serialized.FindProperty("briefs").objectReferenceValue;
                if (briefs.sharedMesh.GetIndexCount(0) / 3u != triangles || briefs.sharedMaterial == null || briefs.rootBone == null)
                    throw new InvalidOperationException($"Kit {member.PlayerId} did not retain its mesh, bone and material references.");
            }
        }

        RebuiltBriefsPreview.Apply();
        CheckBriefMeshes(2688);
        var applied = File.ReadAllBytes(Arena);
        RebuiltBriefsPreview.Restore();
        CheckBriefMeshes(820);
        RebuiltBriefsPreview.Apply();
        CheckBriefMeshes(2688);
        bool sameReapply = applied.SequenceEqual(File.ReadAllBytes(Arena));
        RebuiltBriefsPreview.Apply();
        bool sameRepeat = applied.SequenceEqual(File.ReadAllBytes(Arena));
        return new { athletes = 14, garmentTriangles = 2688, equipmentRecovered = true, sameReapply, sameRepeat };
    }

    public static object FinalState()
    {
        var scene = SceneManager.GetActiveScene();
        var ball = UnityEngine.Object.FindAnyObjectByType<BallController>();
        var effects = UnityEngine.Object.FindAnyObjectByType<WaterInteractionEffects>();
        var defaults = new SerializedObject(ball);
        return new { scene = scene.path, dirty = scene.isDirty, playing = EditorApplication.isPlaying,
            boot = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene), guid = AssetDatabase.AssetPathToGUID(Arena),
            usesInteractiveWater = effects.UsesContactSampling,
            usesProfessionalThrows = defaults.FindProperty("professionalThrows").boolValue };
    }

    public static object VerifyArenaRecovery()
    {
        RequireReady();
        ProfessionalThrowSetup.Apply();
        ProfessionalRefereeSetup.ApplyDeckPresentation();
        var initial = File.ReadAllBytes(Arena);
        ProfessionalThrowSetup.Restore();
        ProfessionalRefereeSetup.RestoreDeckPresentation();
        bool BoolOn(MonoBehaviour target, string name) => new SerializedObject(target).FindProperty(name).boolValue;
        var restoredBall = UnityEngine.Object.FindAnyObjectByType<BallController>();
        var restoredIntro = UnityEngine.Object.FindAnyObjectByType<PreMatchPresentation>();
        if (BoolOn(restoredBall, "professionalThrows") || BoolOn(restoredIntro, "useRefinedDeckWalking"))
            throw new InvalidOperationException("The previous throwing or walkout settings were not restored.");
        ProfessionalThrowSetup.Apply();
        ProfessionalRefereeSetup.ApplyDeckPresentation();
        if (!BoolOn(UnityEngine.Object.FindAnyObjectByType<BallController>(), "professionalThrows") ||
            !UnityEngine.Object.FindObjectsByType<PoolsideReferee>().All(value => BoolOn(value, "useMatchTrackingPresentation")))
            throw new InvalidOperationException("The new throwing or officials were not applied.");
        var reapplied = File.ReadAllBytes(Arena);
        ProfessionalThrowSetup.Apply();
        ProfessionalRefereeSetup.ApplyDeckPresentation();
        return new { byteIdenticalReapply = initial.SequenceEqual(reapplied), byteIdenticalRepeat = reapplied.SequenceEqual(File.ReadAllBytes(Arena)) };
    }

    private static void RequireReady()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path != Arena || SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Open the saved production arena in Edit Mode first.");
    }
}
