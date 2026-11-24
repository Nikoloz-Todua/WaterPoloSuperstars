using System;
using System.IO;
using System.Linq;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ToduStudios.WaterPolo.Editor
{
    public static class AuthenticAthleteMotionSetup
    {
        public const string Root = ProductionAthleteSetup.Root + "/AuthenticMotion";
        public const string Controller = Root + "/Authentic Athlete Preview.overrideController";
        const string Deck = Root + "/MeshyDeckSource.fbx";
        static readonly string[] Baseline = { "HeadUpSwim", "Sprint", "TreadWater", "Possession" };
        static readonly string[] Authored =
        {
            "WaterPoloFreestyle", "WaterPoloSprint", "WaterPoloEggbeater", "WaterPoloBallTread"
        };

        [MenuItem("Tools/Water Polo/Athletes/Configure Authentic Motion Preview")]
        public static void ConfigurePreview()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before configuring the preview.");
            ProductionAthleteSetup.OpenPreview();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var prototype = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                AnimatedAthleteImport.Root + "/AnimatedSwimmer.controller");
            var avatar = AssetDatabase.LoadAllAssetsAtPath(AnimatedAthleteImport.Root + "/Models/WaterPoloAthlete.fbx")
                .OfType<Avatar>().Single(a => a.isHuman && a.isValid);

            for (int i = 0; i < Authored.Length; i++)
                ImportHumanoid(Root + "/" + Authored[i] + ".fbx", avatar, Authored[i]);
            ImportHumanoid(Deck, null, null);
            var deckAvatar = AssetDatabase.LoadAllAssetsAtPath(Deck).OfType<Avatar>().Single();
            if (!deckAvatar.isValid || !deckAvatar.isHuman)
                throw new InvalidOperationException("The source deck motion must provide a valid Humanoid Avatar.");

            var replacement = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(Controller);
            if (replacement == null)
            {
                replacement = new AnimatorOverrideController(prototype);
                AssetDatabase.CreateAsset(replacement, Controller);
            }
            for (int i = 0; i < Authored.Length; i++)
                replacement[Baseline[i]] = Clips(Root + "/" + Authored[i] + ".fbx").Single();
            EditorUtility.SetDirty(replacement);
            var preview = UnityEngine.Object.FindAnyObjectByType<AthletePreviewCamera>();
            var animator = preview.athlete.GetComponent<Animator>();
            animator.runtimeAnimatorController = replacement;
            preview.athlete.useAuthoredWaterline = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(preview.athlete);
            var deck = Clips(Deck);
            preview.walk = deck.Single(clip => clip.name.Contains("Walking", StringComparison.OrdinalIgnoreCase));
            preview.run = deck.Single(clip => clip.name.Contains("Running", StringComparison.OrdinalIgnoreCase));
            EditorUtility.SetDirty(preview);
            EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);
            EditorSceneManager.SaveScene(preview.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Authentic athlete preview. Deck takes: " +
                string.Join(", ", deck.Select(clip => $"{clip.name} ({clip.frameRate} Hz, {clip.length:F3} s)")));
        }

        static AnimationClip[] Clips(string path) => AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>().Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
            .ToArray();

        public static void Bind(RiggedSwimmerVisual visual)
        {
            var animator = visual.GetComponent<Animator>();
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(Controller);
            if (controller == null) throw new InvalidOperationException("Configure the motion preview first.");
            visual.deckWalk = Clips(Deck).Single(clip => clip.name == "Walking");
            visual.useAuthoredWaterline = true;
            animator.runtimeAnimatorController = controller;
            PrefabUtility.RecordPrefabInstancePropertyModifications(visual);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
        }

        static void ImportHumanoid(string path, Avatar sourceAvatar, string authoredName)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new FileNotFoundException("Missing animation interchange file", path);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = sourceAvatar == null
                ? ModelImporterAvatarSetup.CreateFromThisModel : ModelImporterAvatarSetup.CopyFromOther;
            if (sourceAvatar != null) importer.sourceAvatar = sourceAvatar;
            importer.preserveHierarchy = true;
            importer.importAnimation = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
            importer.animationRotationError = .25f;
            var takes = importer.defaultClipAnimations;
            if (takes.Length == 0) throw new InvalidOperationException($"No FBX takes in {path}.");
            foreach (var take in takes)
            {
                if (authoredName != null) take.name = authoredName;
                else
                {
                    var parts = take.takeName.Split('|');
                    if (parts.Length >= 3) take.name = parts[1];
                }
                take.loopTime = true;
                take.loopPose = false;
                take.lockRootPositionXZ = true;
                take.lockRootHeightY = true;
                take.lockRootRotation = true;
                take.keepOriginalPositionXZ = true;
                take.keepOriginalPositionY = true;
                take.keepOriginalOrientation = true;
            }
            importer.clipAnimations = takes;
            importer.SaveAndReimport();
        }
    }
}
