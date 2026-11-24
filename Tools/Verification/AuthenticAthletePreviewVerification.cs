using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Editor;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

public static class AuthenticAthletePreviewVerification
{
    const string Folder = "Logs/AuthenticWaterPoloAnimations";
    const string Arena = "Assets/Scenes/OlympicMatchArena.unity";
    const string Fixture = "Assets/ProductionAthletes/AuthenticMotion/IsolatedAthleteArena.unity";
    static readonly int Locomotion = Animator.StringToHash("Base Layer.Locomotion");
    static readonly HumanBodyBones[] Joints =
    {
        HumanBodyBones.Hips, HumanBodyBones.Head, HumanBodyBones.LeftHand, HumanBodyBones.RightHand,
        HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot,
        HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot,
    };

    public static string PrepareIsolatedArena()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Stop Play Mode before creating the arena fixture.");
        Check(!EditorSceneManager.GetActiveScene().isDirty, "Save the current scene before creating the fixture.");
        var scene = EditorSceneManager.OpenScene(Arena);
        Check(AssetDatabase.LoadAssetAtPath<SceneAsset>(Fixture) == null, "Remove the previous isolated fixture first.");
        Check(EditorSceneManager.SaveScene(scene, Fixture, true), "Could not save the isolated arena copy.");
        scene = EditorSceneManager.OpenScene(Fixture);
        var visual = Object.FindObjectsByType<RiggedSwimmerVisual>()
            .Single(v => v.identity.Team == PrototypeTeam.Blue && v.identity.CapNumber == 1);
        AuthenticAthleteEquipmentSetup.Bind(visual);
        AuthenticAthleteMotionSetup.Bind(visual);
        EditorSceneManager.MarkSceneDirty(scene);
        Check(EditorSceneManager.SaveScene(scene), "Could not save the one-athlete configuration.");
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(Fixture);
        return Fixture;
    }

    public static string RemoveIsolatedArena()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Stop Play Mode before removing the arena fixture.");
        Check(!EditorSceneManager.GetActiveScene().isDirty, "Save the current scene before changing the Play entry.");
        EditorSceneManager.OpenScene(Arena);
        Milestone12HubSetup.SetBootEntry();
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Fixture) != null) AssetDatabase.DeleteAsset(Fixture);
        return AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene);
    }

    public static async Task<string> SingleArena()
    {
        Check(EditorApplication.isPlaying && EditorSceneManager.GetActiveScene().path == Fixture, "Play the single-athlete fixture.");
        var match = One<PrototypeMatch>();
        var intro = One<PreMatchPresentation>();
        var roster = One<PrototypeRoster>();
        var visual = Object.FindObjectsByType<RiggedSwimmerVisual>().Single(v => v.useAuthoredWaterline);
        var player = visual.identity;
        Check(roster.Members.Count == 14 && visual.Animator.isHuman && !visual.Animator.applyRootMotion,
            "The original roster and Avatar must be present.");

        var messages = new List<string>();
        foreach (var stage in Enum.GetValues(typeof(PreMatchStage)).Cast<PreMatchStage>().Where(s => s != PreMatchStage.Complete))
        {
            await Until(() => intro.Stage == stage, $"Ceremony reached {stage}.", 8);
            float sample = stage == PreMatchStage.TeamWalkout ? 1.5f : .25f;
            await Until(() => intro.StageElapsed >= sample, $"Ceremony advancing in {stage}.");
            if (stage is > PreMatchStage.ArenaIntro and < PreMatchStage.MatchStartTransition)
            {
                var cast = GameObject.Find("Prematch visual cast");
                var equipment = cast.GetComponentsInChildren<SkinnedMeshRenderer>()
                    .Where(r => r.enabled && r.sharedMesh != null).ToArray();
                Check(equipment.Count(r => r.sharedMesh.name == "AthleteBody") == 14 &&
                    equipment.Count(r => r.sharedMesh.name == "PremiumCapGameplay") == 1, "Ceremony retained all fourteen realistic bodies.");
                if (stage == PreMatchStage.TeamWalkout)
                {
                    var deck = cast.GetComponentsInChildren<Animator>().Single();
                    Check(deck.isHuman && !deck.applyRootMotion, "One ceremony actor is sampling the retargeted in-place walk.");
                    var left = deck.GetBoneTransform(HumanBodyBones.LeftFoot);
                    var right = deck.GetBoneTransform(HumanBodyBones.RightFoot);
                    float first = left.position.y - right.position.y;
                    await Until(() => Mathf.Abs(left.position.y - right.position.y - first) > .045f,
                        "Walking changed the relative foot height.");
                    await Shot("Match-Walkout-Step-1");
                    await Task.Delay(160);
                    await Shot("Match-Walkout-Step-2");
                }
            }
            await Shot($"Match-Ceremony-{(int)stage:00}-{stage}");
            messages.Add($"Ceremony {stage} advanced.");
        }
        await Until(() => match.IsPlaying && !match.AwaitingOpeningPossession, "Real sprint-off acquisition.", 16);

        var ball = One<BallController>();
        var selection = One<PlayerSelection>();
        var computers = Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>()
            .Concat(Object.FindObjectsByType<PrototypeGoalkeeper>()).ToArray();
        bool[] active = computers.Select(brain => brain.enabled).ToArray();
        var preferences = One<GamePreferences>();
        var preferenceField = typeof(GamePreferences).GetField("values", BindingFlags.Instance | BindingFlags.NonPublic);
        var values = (GamePreferences.Values)preferenceField.GetValue(preferences);
        bool originalReplays = values.automaticGoalReplays;
        values.automaticGoalReplays = true;
        var input = InputSystem.settings;
        var originalBackground = input.backgroundBehavior;
        var originalRouting = input.editorInputBehaviorInPlayMode;
        input.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        input.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try
        {
            foreach (var brain in computers) brain.enabled = false;
            foreach (var p in roster.Members)
                p.Motor.ResetPose(new Vector3(p.Team == PrototypeTeam.Blue ? -8f : 8f, 0, -8 + p.CapNumber), Quaternion.identity);
            ball.ResetForRestart(new Vector3(8, 0, 4));
            player.Motor.ResetPose(new Vector3(0, 0, -3), Quaternion.identity);
            selection.Select(player);
            await Until(() => visual.Locomotion < .05f, "Natural stop enters the eggbeater state.");
            float surface = One<PoolBounds>().Waterline;
            Transform head = visual.Animator.GetBoneTransform(HumanBodyBones.Head);
            Transform hips = visual.Animator.GetBoneTransform(HumanBodyBones.Hips);
            Check(head.position.y > surface && hips.position.y < surface, "Eggbeater places hips below and head above water.");
            messages.Add($"Tread waterline {surface:F3}, head {head.position.y-surface:+.000;-.000} m, hips {hips.position.y-surface:+.000;-.000} m.");
            Close(visual);
            await Shot("Match-Tread");
            One<ArenaDeveloperCamera>().SetView(-1);

            Vector3 start = player.Motor.Position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
            await Until(() => visual.Locomotion > .79f && Vector3.Distance(start, player.Motor.Position) > .9f,
                "Keyboard and real motor drive the crawl.");
            await Task.Delay(340);
            Check(hips.position.y < surface, "Crawl pelvis stays submerged.");
            messages.Add($"Crawl head {head.position.y-surface:+.000;-.000} m, hips {hips.position.y-surface:+.000;-.000} m.");
            await Shot("Match-Broadcast-Crawl");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift));
            var stamina = player.GetComponent<SwimmerStamina>();
            await Until(() => stamina.IsSprinting && visual.Locomotion > 1.35f, "Stamina-controlled sprint cycle.");
            Close(visual);
            await Shot("Match-Sprint");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            await Until(() => player.Motor.Velocity.magnitude < .3f, "Stopped after releasing input.");
            One<ArenaDeveloperCamera>().SetView(-1);

            var receiver = roster.FieldPlayers.First(p => p != player && p.Team == player.Team);
            player.Motor.ResetPose(new Vector3(0, 0, -3), Quaternion.identity);
            receiver.Motor.ResetPose(new Vector3(0, 0, 2), Quaternion.identity);
            ball.ResetForRestart(player.Motor.Position);
            Check(player.Interaction.TryCollect(), "Physical collection.");
            await Until(() => visual.Animator.GetBool("Hold") && visual.Animator.GetLayerWeight(1) > .9f, "Hold layer and IK blended in.");
            float wrist = Vector3.Distance(ball.transform.position, visual.RightHand.position);
            Check(wrist < .17f, $"Held ball too far from the driven wrist ({wrist:F3} m).");
            messages.Add($"Possession wrist gap {wrist:F3} m.");
            Close(visual);
            await Shot("Match-Possession");
            int releases = visual.ReleaseAnimations;
            Check(player.GetComponent<SwimmerPassing>().TryPassTo(receiver), "Authoritative dry pass released.");
            await Until(() => ball.Owner == receiver.Interaction, "Physical dry pass caught.");
            Check(visual.ReleaseAnimations == releases + 1, "Pass produced one presentation event.");
            messages.Add("Physical dry pass and catch passed.");

            var goal = Object.FindObjectsByType<PrototypeTeamTactics>().Single(t => t.Team == player.Team).AttackingGoal;
            player.Motor.ResetPose(goal.transform.position-goal.transform.forward*5f, Quaternion.LookRotation(goal.transform.forward));
            selection.Select(player);
            ball.ResetForRestart(player.Motor.Position);
            Check(player.Interaction.TryCollect() && player.Interaction.TryShoot(22, ShotType.Normal), "Physical shot released.");
            await Until(() => match.BlueScore == 1, "Physical Blue goal scored.");
            Check(visual.ReleaseAnimations == releases + 2, "Shot produced one presentation event.");
            One<ArenaDeveloperCamera>().SetView(-1);
            var replay = match.GoalPresentation;
            await Until(() => replay.Phase == GoalPresentationPhase.Broadcast, "Recorded broadcast replay.");
            await Shot("Match-Replay-Broadcast");
            await Until(() => replay.Phase == GoalPresentationPhase.BehindGoal, "Recorded goal-line replay.");
            await Shot("Match-Replay-Goal-Line");
            Check(match.BlueScore == 1, "Replay preserved the exact score.");
            messages.Add("One Blue goal and both recorded replay angles passed.");
            File.WriteAllLines(Folder + "/SingleArena.txt", messages);
            return string.Join("\n", messages);
        }
        finally
        {
            values.automaticGoalReplays = originalReplays;
            for (int i = 0; i < computers.Length; i++)
                if (computers[i] != null) computers[i].enabled = active[i];
            InputSystem.RemoveDevice(keyboard);
            input.backgroundBehavior = originalBackground;
            input.editorInputBehaviorInPlayMode = originalRouting;
        }
    }

    static void Close(RiggedSwimmerVisual visual)
    {
        var dev = One<ArenaDeveloperCamera>();
        Transform view = dev.viewpoints[2];
        Vector3 focus = visual.motor.Position + Vector3.up * .04f;
        view.position = focus + visual.transform.right * 1.75f + visual.transform.forward * 2.9f + Vector3.up * .92f;
        view.LookAt(focus);
        dev.SetView(2);
    }

    static T One<T>() where T : Object => Object.FindAnyObjectByType<T>();

    static void Check(bool pass, string message)
    {
        if (!pass) throw new InvalidOperationException(message);
    }

    static async Task Until(Func<bool> predicate, string message, int seconds = 7)
    {
        double deadline = EditorApplication.timeSinceStartup + seconds;
        while (!predicate())
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException(message);
            await Task.Delay(15);
        }
    }

    public static async Task<string> ArenaBenchmark()
    {
        Check(EditorApplication.isPlaying && EditorSceneManager.GetActiveScene().path == Arena, "Play the production arena first.");
        var rigs = Object.FindObjectsByType<RiggedSwimmerVisual>();
        Check(rigs.Length == 14 && rigs.All(v => v.useAuthoredWaterline), "The benchmark needs the fourteen-athlete rollout.");
        var match = One<PrototypeMatch>();
        var intro = One<PreMatchPresentation>();
        if (intro.Active) intro.SkipAll();
        await Until(() => match.IsPlaying && !match.AwaitingOpeningPossession, "Natural sprint possession starts the match.", 18);
        var atlas = new MaterialPropertyBlock();
        int atlasTransform = Shader.PropertyToID("_BaseMap_ST");
        foreach (var rig in rigs)
        {
            var kit = rig.GetComponent<AthleteEquipment>();
            kit.NumberRenderer.GetPropertyBlock(atlas);
            Vector4 uv = atlas.GetVector(atlasTransform);
            int printed = Mathf.RoundToInt(uv.z * 8) + 8 * (7 - Mathf.RoundToInt(uv.w * 8));
            Check(printed == rig.identity.CapNumber, "Printed number follows the roster.");
            var cap = rig.transform.Find("WaterPoloCap").GetComponent<SkinnedMeshRenderer>();
            string role = rig.identity.IsFieldPlayer ? rig.identity.Team + " Cap" : "Red Keeper Cap";
            Check(cap.enabled && cap.sharedMesh.name == "PremiumCapGameplay" && cap.sharedMaterial.name == role,
                $"Premium headgear and role material for {rig.identity.Team} #{printed}.");
        }
        match.SetPaused(true);
        var temporary = new List<SkinnedMeshRenderer>();
        var disabled = new List<SkinnedMeshRenderer>();
        var briefs = new Dictionary<SkinnedMeshRenderer, Material>();
        try
        {
            var report = new List<string>
            {
                $"{Screen.width} x {Screen.height}, {SystemInfo.graphicsDeviceType}, {SystemInfo.graphicsDeviceName}, {SystemInfo.processorType}",
                "14 role colors and roster atlas addresses verified. Thirty frames per case, same parked camera and exact frozen character poses."
            };
            await Shot("Benchmark-M28");
            report.Add(await SampleArena(rigs, "M28 equipment"));
            // Use separately initialized renderers for the previous kit. Swapping GPU
            // skinning vertex layouts on active renderers can invalidate their cache.
            const string path = ProductionAthleteSetup.Root;
            var oldModel = AssetDatabase.LoadAssetAtPath<GameObject>(path + "/Models/Athlete001.fbx");
            var originals = oldModel.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var rig in rigs)
            {
                var style = AssetDatabase.LoadAssetAtPath<AthleteKitStyle>(
                    path + "/Materials/" + rig.identity.Team + " Team Kit.asset");
                bool keeper = !rig.identity.IsFieldPlayer;
                foreach (string part in new[] { "WaterPoloCap", "CapNumber" })
                {
                    var current = rig.transform.Find(part).GetComponent<SkinnedMeshRenderer>();
                    var source = originals.Single(renderer => renderer.name == part);
                    var renderer = BenchmarkRenderer(oldModel, source, current, rig);
                    temporary.Add(renderer);
                    renderer.sharedMaterials = part == "CapNumber" ? new[] { style.Number(keeper) } : style.Cap(keeper);
                    if (current.HasPropertyBlock())
                    {
                        current.GetPropertyBlock(atlas);
                        renderer.SetPropertyBlock(atlas);
                    }
                    disabled.Add(current);
                    current.enabled = false;
                    renderer.enabled = true;
                }
                var garment = rig.transform.Find("CompetitionBriefs").GetComponent<SkinnedMeshRenderer>();
                briefs.Add(garment, garment.sharedMaterial);
                garment.sharedMaterial = style.Briefs;
            }
            await Frames(8);
            await Shot("Benchmark-M27");
            report.Add(await SampleArena(rigs, "M27 equipment"));
            File.WriteAllLines(Folder + "/ArenaBenchmark.txt", report);
            return string.Join("\n", report);
        }
        finally
        {
            foreach (var renderer in temporary)
            {
                renderer.enabled = false;
                Object.Destroy(renderer.gameObject);
            }
            foreach (var renderer in disabled) renderer.enabled = true;
            foreach (var item in briefs) item.Key.sharedMaterial = item.Value;
            match.SetPaused(false);
        }
    }

    static SkinnedMeshRenderer BenchmarkRenderer(GameObject model, SkinnedMeshRenderer source,
        SkinnedMeshRenderer current, RiggedSwimmerVisual rig)
    {
        Transform Map(Transform bone) => rig.transform.Find(AnimationUtility.CalculateTransformPath(bone, model.transform));
        var part = new GameObject(source.name + " (benchmark)");
        part.layer = current.gameObject.layer;
        part.transform.SetParent(rig.transform, false);
        var matrix = model.transform.worldToLocalMatrix * source.transform.localToWorldMatrix;
        part.transform.SetLocalPositionAndRotation(matrix.GetPosition(), matrix.rotation);
        part.transform.localScale = matrix.lossyScale;
        var renderer = part.AddComponent<SkinnedMeshRenderer>();
        renderer.enabled = false;
        renderer.sharedMesh = source.sharedMesh;
        renderer.bones = source.bones.Select(Map).ToArray();
        renderer.rootBone = Map(source.rootBone);
        renderer.localBounds = current.localBounds;
        renderer.quality = current.quality;
        renderer.shadowCastingMode = current.shadowCastingMode;
        renderer.receiveShadows = current.receiveShadows;
        return renderer;
    }

    static async Task<string> SampleArena(RiggedSwimmerVisual[] rigs, string label)
    {
        var meshes = rigs.SelectMany(rig => rig.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r => r.enabled)).ToArray();
        int raw = meshes.Sum(r => Enumerable.Range(0, r.sharedMesh.subMeshCount).Sum(i => (int)r.sharedMesh.GetIndexCount(i) / 3));
        var textures = meshes.SelectMany(r => r.sharedMaterials).Distinct().Where(m => m != null)
            .SelectMany(m => m.GetTexturePropertyNames().Select(m.GetTexture)).Where(t => t != null).Distinct().ToArray();
        double size = textures.Sum(Profiler.GetRuntimeMemorySizeLong) / 1048576d;
        var fields = new[] { "drawCalls", "batches", "triangles", "mainThreadFrameTime", "renderThreadFrameTime" };
        var values = new List<double>[fields.Length];
        var stats = fields.Select(field => typeof(UnityStats).GetProperty(field, BindingFlags.Public | BindingFlags.Static)).ToArray();
        for (int j = 0; j < values.Length; j++) values[j] = new List<double>();
        for (int i = 0; i < 30; i++)
        {
            await Frames(1);
            for (int j = 0; j < values.Length; j++)
                if (stats[j] != null) values[j].Add(Convert.ToDouble(stats[j].GetValue(null)));
        }
        return $"{label}: {raw} roster mesh triangles, {meshes.Length} enabled stable renderers, " +
            $"{textures.Length} textures, {size:F2} MiB Editor texture object memory; " +
            string.Join(", ", fields.Zip(values, (field, samples) => samples.Count == 0 ? $"{field} unavailable" : $"{field}={samples.Average():F3}"));
    }

    public static async Task<string> Capture()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play the athlete preview before capturing.");
        Directory.CreateDirectory(Folder);
        var preview = Object.FindAnyObjectByType<AthletePreviewCamera>();
        var animator = preview.athlete.Animator;
        if (!animator.isHuman || !animator.avatar.isValid || animator.applyRootMotion)
            throw new InvalidOperationException("Preview requires a valid in-place Humanoid.");
        var clips = animator.runtimeAnimatorController.animationClips;
        var rows = new List<string>();
        foreach (var clip in clips.Concat(new[] { preview.walk, preview.run }))
            rows.Add($"clip {clip.name} {clip.frameRate:F0} Hz {clip.length:F3} s humanoid={clip.humanMotion}");

        var equipment = new SerializedObject(preview.athlete.GetComponent<AthleteEquipment>());
        var enabled = preview.athlete.GetComponentsInChildren<SkinnedMeshRenderer>()
            .Where(r => r.enabled).ToArray();
        int triangles = 0;
        foreach (var renderer in enabled)
        {
            var mesh = renderer.sharedMesh;
            int count = Enumerable.Range(0, mesh.subMeshCount).Sum(i => (int)mesh.GetIndexCount(i) / 3);
            rows.Add($"renderer {renderer.name} {mesh.name} {count} triangles");
            triangles += count;
        }
        rows.Add($"total {triangles} triangles, {enabled.Length} enabled renderers");

        preview.dry = true;
        preview.framing = 0;
        Aim(preview, 5, 0);
        await Shot("Standing-Front");
        Aim(preview, 90, 0);
        await Shot("Standing-Side");
        preview.framing = 1;
        Aim(preview, 78, 4);
        await Shot("Blue-Cap-Right");
        Aim(preview, -78, 4);
        await Shot("Blue-Cap-Left");
        SetKit(equipment, true, false);
        await Shot("White-Cap");
        SetKit(equipment, false, true);
        await Shot("Keeper-Cap");
        SetKit(equipment, false, false);
        preview.framing = 2;
        Aim(preview, 17, 2);
        await Shot("Briefs-Front");
        Aim(preview, 163, 2);
        await Shot("Briefs-Back");

        preview.dry = false;
        preview.framing = 0;
        preview.playbackRate = 1;
        foreach (var spec in new[] {
            (Name:"Freestyle-Side", State:1, Angle:90f, Elevation:4f, Samples:8),
            (Name:"Freestyle-Top", State:1, Angle:150f, Elevation:83f, Samples:8),
            (Name:"Freestyle-Under", State:1, Angle:130f, Elevation:-37f, Samples:8),
            (Name:"Freestyle-Front", State:1, Angle:0f, Elevation:14f, Samples:4),
            (Name:"Sprint-Side", State:2, Angle:90f, Elevation:4f, Samples:6),
            (Name:"Eggbeater-Under", State:0, Angle:20f, Elevation:-36f, Samples:8),
            (Name:"Eggbeater-Side", State:0, Angle:90f, Elevation:3f, Samples:6)
        })
        {
            preview.athlete.previewState = spec.State;
            preview.playbackRate = 1;
            Aim(preview, spec.Angle, spec.Elevation);
            await Task.Delay(650);
            preview.playbackRate = 0f;
            animator.SetFloat("Move", spec.State);
            await Frames(2);
            for (int step = 0; step <= spec.Samples; step++)
            {
                animator.Play(Locomotion, 0, step / (float)spec.Samples);
                animator.Update(0);
                await Frames(2);
                Vector3 origin = preview.athlete.transform.parent.position;
                rows.Add($"{spec.Name} {step}/{spec.Samples} " + string.Join(" ", Joints.Select(j =>
                {
                    Vector3 p = animator.GetBoneTransform(j).position - origin;
                    return $"{j}={p.x:F3},{p.y:F3},{p.z:F3}";
                })));
                if (step < spec.Samples) await Shot($"{spec.Name}-{step:00}");
            }
        }

        preview.playbackRate = 1f;
        Aim(preview, 55, 6);
        foreach (var action in new[] { (3, "Ball-Tread"), (4, "Pass"), (5, "Shot"), (6, "Block"), (7, "Save") })
        {
            preview.athlete.previewState = action.Item1;
            await Task.Delay(action.Item1 == 3 || action.Item1 == 6 ? 420 : 140);
            await Shot(action.Item2);
        }
        preview.playbackRate = .25f;
        Aim(preview, 85, 4);
        foreach (var deck in new[] { preview.walk, preview.run })
        {
            preview.SelectDeckClip(deck);
            for (int step = 0; step < 8; step++)
            {
                await Task.Delay(500);
                await Shot($"{deck.name}-{step:00}");
            }
        }
        File.WriteAllLines(Folder + "/PreviewSamples.txt", rows);
        return $"{rows.Count} recorded samples in {Folder}; current frame {Time.frameCount}";
    }

    public static async Task<string> PreviewKeys()
    {
        var preview = One<AthletePreviewCamera>();
        Check(preview != null && EditorApplication.isPlaying && preview.dry, "Start a fresh athlete preview.");
        var settings = InputSystem.settings;
        var routing = settings.editorInputBehaviorInPlayMode;
        var background = settings.backgroundBehavior;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        async Task KeyPress(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            await Frames(3);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            await Frames(3);
        }
        try
        {
            await KeyPress(Key.Tab);
            await KeyPress(Key.Digit2);
            Check(!preview.dry && preview.athlete.enabled && preview.athlete.previewState == 1, "Tab and 2 select crawl.");
            await KeyPress(Key.O);
            Check(preview.elevation > 80f, "O requests overhead.");
            await KeyPress(Key.U);
            Check(preview.elevation < -30f, "U requests the low angle.");
            await KeyPress(Key.F2);
            Check(preview.framing == 1, "F2 frames the cap.");
            await KeyPress(Key.F4);
            await Until(() => !preview.athlete.enabled && preview.athlete.Animator.enabled, "Walking graph activated.");
            await KeyPress(Key.RightBracket);
            Check(preview.playbackRate == 0f, "Right Bracket steps and holds the walking preview.");
            await KeyPress(Key.P);
            Check(preview.playbackRate == 1f, "P resumes playback.");
            await KeyPress(Key.F5);
            Check(preview.athlete.Animator.enabled && !preview.athlete.enabled, "F5 retains the deck graph.");
            await KeyPress(Key.Tab);
            Check(preview.dry && !preview.athlete.Animator.enabled, "Tab restores the standing preview.");
            preview.angle = 15f;
            preview.elevation = 0f;
            preview.framing = 0;
            return "Tab, 2, O, U, F2, F4, Right Bracket, P, F5 and standing return passed via queued Input System keyboard events.";
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
            settings.editorInputBehaviorInPlayMode = routing;
            settings.backgroundBehavior = background;
        }
    }

    static void SetKit(SerializedObject equipment, bool white, bool keeper)
    {
        equipment.FindProperty("previewTeam").enumValueIndex = white ? 1 : 0;
        equipment.FindProperty("previewGoalkeeper").boolValue = keeper;
        equipment.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Aim(AthletePreviewCamera preview, float angle, float elevation)
    {
        preview.angle = angle;
        preview.elevation = elevation;
    }

    static async Task Frames(int count)
    {
        int first = Time.frameCount;
        double until = EditorApplication.timeSinceStartup + 4;
        while (Time.frameCount - first < count)
        {
            if (EditorApplication.timeSinceStartup > until)
                throw new TimeoutException("The preview is not advancing.");
            await Task.Delay(14);
        }
    }

    static async Task Shot(string name)
    {
        await Frames(2);
        ScreenCapture.CaptureScreenshot(Path.GetFullPath(Folder + "/" + name + ".png"));
        await Frames(2);
    }
}
