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
    public static class RebuiltBriefsPreview
    {
        const string Model = "Assets/ProductionAthletes/Candidate/AtlasCompetitionBriefs.fbx";
        const string OriginalModel = "Assets/ProductionAthletes/Models/Athlete001.fbx";
        const string Arena = "Assets/Scenes/OlympicMatchArena.unity";
        const string PreviewRenderer = "Atlas Competition Briefs Preview";
        const string Menu = "Tools/Water Polo/Athletes/";
        const string ToggleMenu = Menu + "Toggle Rebuilt Briefs in Preview";

        [MenuItem(ToggleMenu, true)]
        static bool CanToggle() => FindPreview() != null;

        [MenuItem(ToggleMenu)]
        public static void Toggle()
        {
            var preview = FindPreview();
            if (preview == null)
                throw new InvalidOperationException("Play the isolated Athlete Preview before toggling the briefs.");
            var rig = preview.athlete;
            var prior = Briefs(rig);
            var existing = rig.transform.Find(PreviewRenderer);
            if (existing != null)
            {
                prior.gameObject.SetActive(true);
                Object.Destroy(existing.gameObject);
                return;
            }

            var garment = LoadBriefs(true);
            var (root, bones) = MapBones(garment, rig.transform);
            var relative = Relative(garment);
            var comparison = new GameObject(PreviewRenderer).AddComponent<SkinnedMeshRenderer>();
            comparison.gameObject.layer = prior.gameObject.layer;
            comparison.transform.SetParent(rig.transform, false);
            comparison.transform.SetLocalPositionAndRotation(relative.GetPosition(), relative.rotation);
            comparison.transform.localScale = relative.lossyScale;
            comparison.sharedMesh = garment.sharedMesh;
            comparison.rootBone = root;
            comparison.bones = bones;
            comparison.sharedMaterial = prior.sharedMaterial;
            comparison.localBounds = prior.localBounds;
            comparison.quality = prior.quality;
            comparison.shadowCastingMode = prior.shadowCastingMode;
            comparison.receiveShadows = prior.receiveShadows;
            prior.gameObject.SetActive(false);
        }

        [MenuItem(Menu + "Apply Rebuilt Competition Briefs")]
        public static void Apply() => SetArenaBriefs(true);

        [MenuItem(Menu + "Restore Previous Competition Briefs")]
        public static void Restore() => SetArenaBriefs(false);

        static void SetArenaBriefs(bool rebuilt)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before applying competition briefs.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save the open scenes before applying competition briefs.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != Arena) scene = EditorSceneManager.OpenScene(Arena);
            var garment = LoadBriefs(rebuilt);
            if (!SameTransform(Relative(LoadBriefs(false)), Relative(garment)))
                throw new InvalidOperationException("The rebuilt briefs require the original renderer transform.");
            var rigs = Object.FindObjectsByType<RiggedSwimmerVisual>(FindObjectsInactive.Include)
                .Where(rig => rig.gameObject.scene == scene && rig.identity != null).ToArray();
            if (rigs.Length != 14)
                throw new InvalidOperationException($"Expected fourteen arena kit bindings. Found {rigs.Length}.");

            // Resolve all existing kit slots and all mapped bones before changing the arena.
            var bindings = rigs.Select(rig =>
            {
                var (root, bones) = MapBones(garment, rig.transform);
                return (renderer: Briefs(rig), root, bones);
            }).ToArray();
            if (bindings.Select(binding => binding.renderer).Distinct().Count() != 14)
                throw new InvalidOperationException("The arena should have fourteen separate brief renderers.");

            var changed = bindings.Where(binding => binding.renderer.sharedMesh != garment.sharedMesh ||
                binding.renderer.rootBone != binding.root || !binding.renderer.bones.SequenceEqual(binding.bones)).ToArray();
            if (changed.Length == 0) return;

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(rebuilt ? "Apply rebuilt competition briefs" : "Restore previous competition briefs");
            Undo.RecordObjects(changed.Select(binding => (Object)binding.renderer).ToArray(), Undo.GetCurrentGroupName());
            foreach (var (renderer, root, bones) in changed)
            {
                renderer.sharedMesh = garment.sharedMesh;
                renderer.rootBone = root;
                renderer.bones = bones;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                EditorUtility.SetDirty(renderer);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw new InvalidOperationException("The arena did not save the competition briefs.");
            }
        }

        static SkinnedMeshRenderer Briefs(RiggedSwimmerVisual rig)
        {
            var kit = rig.GetComponent<AthleteEquipment>();
            if (kit != null && new SerializedObject(kit).FindProperty("briefs").objectReferenceValue
                is SkinnedMeshRenderer renderer)
                return renderer;
            throw new InvalidOperationException($"The athlete kit has no briefs slot on {rig.name}.");
        }

        static SkinnedMeshRenderer LoadBriefs(bool rebuilt)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(rebuilt ? Model : OriginalModel);
            string part = rebuilt ? "Atlas Competition Briefs" : "CompetitionBriefs";
            var garment = asset == null ? null : asset.GetComponentsInChildren<SkinnedMeshRenderer>()
                .SingleOrDefault(renderer => rebuilt || renderer.name == part);
            if (garment == null || garment.sharedMesh == null || garment.sharedMesh.subMeshCount != 1 ||
                garment.sharedMesh.GetIndexCount(0) != (rebuilt ? 8064u : 2460u))
                throw new InvalidOperationException($"Imported {part} does not match the audited geometry.");
            return garment;
        }

        static (Transform root, Transform[] bones) MapBones(SkinnedMeshRenderer garment, Transform rig)
        {
            Transform Map(Transform bone)
            {
                string path = AnimationUtility.CalculateTransformPath(bone, garment.transform.root);
                return rig.Find(path)
                    ?? throw new InvalidOperationException($"Atlas bone path missing on {rig.name}: {path}");
            }
            return (Map(garment.rootBone), garment.bones.Select(Map).ToArray());
        }

        static Matrix4x4 Relative(SkinnedMeshRenderer renderer) =>
            renderer.transform.root.worldToLocalMatrix * renderer.transform.localToWorldMatrix;

        static bool SameTransform(Matrix4x4 a, Matrix4x4 b)
        {
            for (int i = 0; i < 16; i++)
                if (Mathf.Abs(a[i] - b[i]) > .001f)
                    return false;
            return true;
        }

        static AthletePreviewCamera FindPreview()
        {
            if (!EditorApplication.isPlaying) return null;
            var preview = Object.FindAnyObjectByType<AthletePreviewCamera>();
            return preview != null && preview.gameObject.scene.path == ProductionAthleteSetup.Preview
                && preview.athlete != null ? preview : null;
        }
    }
}
