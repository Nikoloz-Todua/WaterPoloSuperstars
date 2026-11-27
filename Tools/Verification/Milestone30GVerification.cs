using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Hub;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEngine;
using Object = UnityEngine.Object;

public static class Milestone30GVerification
{
    private const string Folder = "Logs/Milestone30G";

    public static Task<string> Before() => Swim("Before");
    public static Task<string> After() => Swim("After");
    public static Task<string> DeckBefore() => Deck("Before");
    public static Task<string> DeckAfter() => Deck("After");

    public static async Task<string> CaptureZero()
    {
        var match = Object.FindAnyObjectByType<PrototypeMatch>();
        var view = Object.FindAnyObjectByType<ArenaDeveloperCamera>();
        var selection = Object.FindAnyObjectByType<PlayerSelection>();
        await Until(() => Application.isPlaying && !HubMatchBridge.Active && view.ViewIndex == 2 &&
            match.IsPlaying && !match.AwaitingOpeningPossession &&
            selection.ActivePlayer.Motor.Position.sqrMagnitude < .08f &&
            selection.ActivePlayer.Motor.Velocity.sqrMagnitude < .03f, "Phase zero centre-pool staging");
        var athlete = selection.ActivePlayer;
        var input = athlete.GetComponent<SwimmerInput>();
        input.enabled = false;
        athlete.Motor.ResetPose(Vector3.zero, Quaternion.identity);
        await Awaitable.WaitForSecondsAsync(.14f);
        var rig = athlete.GetComponentInChildren<RiggedSwimmerVisual>();
        var animator = rig.Animator;
        var ring = athlete.transform.Find("SelectionRing");
        rig.enabled = false;
        animator.keepAnimatorStateOnDisable = true;
        animator.speed = 0f;
        animator.SetFloat("Move", 1f);
        animator.Play(animator.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, 0f);
        animator.Update(0f);
        typeof(RiggedSwimmerVisual).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(rig, null);
        animator.enabled = false;
        if (ring != null) ring.gameObject.SetActive(false);
        try
        {
            var focus = animator.GetBoneTransform(HumanBodyBones.Hips).position + Vector3.back * .22f;
            var anchor = view.viewpoints[2];
            view.GetComponent<Camera>().fieldOfView = 44f;
            anchor.position = focus + new Vector3(2.05f, -.44f, -1.07f);
            anchor.LookAt(focus);
            await Awaitable.NextFrameAsync();
            await CaptureFrame("After-Crawl-Under-0");
            return "Captured the authored phase zero with settled camera.";
        }
        finally
        {
            animator.enabled = true;
            animator.speed = 1f;
            rig.enabled = true;
            input.enabled = true;
            if (ring != null) ring.gameObject.SetActive(true);
        }
    }

    public static async Task<string> Official()
    {
        var view = Object.FindAnyObjectByType<ArenaDeveloperCamera>();
        var ball = Object.FindAnyObjectByType<BallController>();
        if (!Application.isPlaying || ball == null || HubMatchBridge.Active)
            throw new InvalidOperationException("Use the isolated Referee Walking Lab.");
        await Until(() => view.ViewIndex == 2 && ball.Owner != null && ball.Match.IsPlaying, "Referee camera staging");
        var official = Object.FindObjectsByType<PoolsideReferee>().OrderBy(p => p.transform.position.x)
            .First(p => p.GetComponentInChildren<RefereeVisual>()?.UsesProfessional == true);
        var visual = official.GetComponentInChildren<ProfessionalRefereePose>();
        Vector3 start = official.transform.position;
        float direction = start.z <= 0f ? 1f : -1f;
        ball.ResetForRestart(Vector3.forward * direction * 8.4f);
        await Until(() => Vector3.Distance(start, official.transform.position) > .42f && Mathf.Abs(official.Speed) > .4f,
            "Official tracks the real ball location on the deck");
        Directory.CreateDirectory(Folder);
        for (int i = 0; i < 3; i++)
        {
            await CaptureFrame($"Official-Walk-{i}");
            await Awaitable.WaitForSecondsAsync(.17f);
        }
        float travel = Vector3.Distance(start, official.transform.position);
        Vector3 reversal = official.transform.position;
        ball.ResetForRestart(Vector3.forward * -direction * 8.4f);
        await Until(() => official.Speed * direction < -.25f, "Official plants, pivots and resumes in the opposing direction");
        return $"Real side-deck walk travelled {travel:F2} m. Official pivoted and resumed. " +
            $"Return speed {official.Speed:F2} m/s, head facing {visual.HeadLookDirection}, turn travel {Vector3.Distance(reversal, official.transform.position):F2} m.";
    }

    public static async Task<string> DeckPreview()
    {
        var preview = Object.FindAnyObjectByType<AthletePreviewCamera>();
        if (!Application.isPlaying || preview == null) throw new InvalidOperationException("Use Natural Athlete Walk.");
        Directory.CreateDirectory(Folder);
        preview.playbackRate = .5f;
        preview.ShowProfessionalDeck(false);
        await Awaitable.WaitForSecondsAsync(.25f);
        await CaptureFrame("Preview-AuthoredWalk");
        preview.ShowProfessionalDeck(true);
        await Awaitable.WaitForSecondsAsync(.20f);
        await CaptureFrame("Preview-AuthoredStand");
        preview.SelectDeckClip(preview.walk);
        await Awaitable.WaitForSecondsAsync(.27f);
        await CaptureFrame("Preview-LegacyHumanoid");
        return "Advanced three preview clips on the production skinned rig.";
    }

    public static async Task<string> ReceiveCompatibility()
    {
        var selection = Object.FindAnyObjectByType<PlayerSelection>();
        var ball = Object.FindAnyObjectByType<BallController>();
        var roster = Object.FindAnyObjectByType<PrototypeRoster>();
        await Until(() => Application.isPlaying && !HubMatchBridge.Active && ball.Match.IsPlaying &&
            !ball.Match.AwaitingOpeningPossession &&
            selection.ActivePlayer != null && ball.Owner == selection.ActivePlayer.Interaction &&
            selection.ActivePlayer.Motor.Position.sqrMagnitude < .10f, "Facing-away receive staging");
        var passer = selection.ActivePlayer;
        var receiver = roster.FieldPlayers.First(p => p.Team == passer.Team && p != passer);
        await Until(() => (receiver.Motor.Position - passer.Motor.Position).sqrMagnitude is > 63f and < 72f,
            "The lab's designated 8.2 m receiver");
        if (!passer.GetComponent<SwimmerPassing>().TryPass())
            throw new InvalidOperationException("Authorized pass was rejected.");
        await Until(() => ball.Owner == receiver.Interaction && selection.ActivePlayer == receiver,
            "Production receive and automatic selection");
        await Awaitable.EndOfFrameAsync();
        var rig = receiver.GetComponentInChildren<RiggedSwimmerVisual>();
        float gap = Mathf.Abs(Vector3.Distance(rig.EvaluatedPalm, ball.transform.position) - ball.Radius);
        return $"Facing-away reception and selection passed. First rendered palm landmark gap {gap:F4} m.";
    }

    public static async Task<string> SwimCost()
    {
        var ball = Object.FindAnyObjectByType<BallController>();
        var view = Object.FindAnyObjectByType<ArenaDeveloperCamera>();
        var selection = Object.FindAnyObjectByType<PlayerSelection>();
        await Until(() => Application.isPlaying && !HubMatchBridge.Active && view.ViewIndex == 2 &&
            ball.Match.IsPlaying && !ball.Match.AwaitingOpeningPossession, "Side-view swim staging");
        var athlete = selection.ActivePlayer;
        var rig = athlete.GetComponentInChildren<RiggedSwimmerVisual>();
        var input = athlete.GetComponent<SwimmerInput>();
        input.enabled = false;
        Vector3 heading = athlete.Motor.Facing;
        athlete.Motor.SetMoveInput(new Vector2(heading.x, heading.z));
        var recorder = UnityEngine.Profiling.Recorder.Get("WaterPolo.AuthoredFlutter");
        recorder.enabled = true;
        long nanos = 0;
        int count = 0;
        try
        {
            await Until(() => rig.Locomotion > .85f, "Side-view live crawl");
            float deadline = Time.time + .85f;
            while (Time.time < deadline)
            {
                nanos += recorder.elapsedNanoseconds;
                count += recorder.sampleBlockCount;
                await Awaitable.NextFrameAsync();
            }
            return $"Authored flutter marker {nanos / (1000000d * Mathf.Max(1, count)):F4} ms/sample across {count} samples.";
        }
        finally
        {
            athlete.Motor.SetMoveInput(Vector2.zero);
            input.enabled = true;
            recorder.enabled = false;
        }
    }

    public static string ImportDeck()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before importing deck clips.");
        var avatar = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
            "Assets/SwimmerAnimationPrototype/Models/WaterPoloAthlete.fbx").OfType<Avatar>().Single(a => a.isHuman && a.isValid);
        var result = new List<string>();
        foreach (string name in new[] { "WPSDeckWalk", "WPSDeckStand" })
        {
            string path = $"Assets/Resources/{name}.fbx";
            var importer = (UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(path);
            if (importer == null) throw new FileNotFoundException(path);
            importer.animationType = UnityEditor.ModelImporterAnimationType.Human;
            importer.avatarSetup = UnityEditor.ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = avatar;
            importer.preserveHierarchy = true;
            importer.materialImportMode = UnityEditor.ModelImporterMaterialImportMode.None;
            importer.animationCompression = UnityEditor.ModelImporterAnimationCompression.Optimal;
            importer.animationRotationError = .25f;
            var clip = importer.defaultClipAnimations.Single();
            clip.name = name;
            clip.loopTime = true;
            clip.loopPose = false;
            clip.lockRootPositionXZ = clip.lockRootHeightY = clip.lockRootRotation = true;
            clip.keepOriginalPositionXZ = clip.keepOriginalPositionY = clip.keepOriginalOrientation = true;
            importer.clipAnimations = new[] { clip };
            importer.SaveAndReimport();
            var imported = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c => c.name == name);
            if (!imported.humanMotion) throw new InvalidOperationException($"{name} did not import as Humanoid.");
            result.Add($"{name} {imported.length:F3}s");
        }
        return string.Join(", ", result);
    }

    private static async Task<string> Deck(string version)
    {
        var intro = Object.FindAnyObjectByType<PreMatchPresentation>();
        var lens = Object.FindAnyObjectByType<GameplayCamera>().GetComponent<Camera>();
        if (!Application.isPlaying || intro == null || !intro.Active || HubMatchBridge.Active)
            throw new InvalidOperationException("Use the isolated Arena Walkout and Pennants Lab.");
        UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        var cast = GameObject.Find("Prematch visual cast");
        await Until(() => intro.Stage == PreMatchStage.TeamWalkout, "Walkout stage");
        cast = GameObject.Find("Prematch visual cast");
        var walker = cast.transform.GetChild(2);
        var captain = cast.transform.GetChild(6);
        var peer = cast.transform.GetChild(13);
        Vector3 CameraFocus() => intro.Stage == PreMatchStage.CaptainExchange
            ? (captain.position + peer.position) * .5f + Vector3.up * .73f
            : walker.position + Vector3.up * .76f;
        void PlaceCamera(UnityEngine.Rendering.ScriptableRenderContext _, Camera camera)
        {
            if (camera != lens || !intro.Active) return;
            Vector3 focus = CameraFocus();
            Vector3 offset = intro.Stage == PreMatchStage.CaptainExchange ? new Vector3(.22f, .37f, 3.25f)
                : walker.right * 2.91f + walker.forward * .63f + Vector3.up * .28f;
            lens.transform.position = focus + offset;
            lens.transform.LookAt(focus);
            lens.fieldOfView = 43f;
        }
        UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += PlaceCamera;
        var stageTimes = new List<string>();
        Directory.CreateDirectory(Folder);
        try
        {
            foreach (float time in new[] { .95f, 1.13f, 1.32f, 1.51f, 1.69f })
            {
                await Until(() => intro.StageElapsed >= time, "Advancing walk stride");
                await CaptureFrame($"{version}-DeckWalk-{stageTimes.Count}");
                stageTimes.Add($"walk {intro.StageElapsed:F3}");
            }
            await Until(() => intro.Stage == PreMatchStage.FormalLineup && intro.StageElapsed >= .25f, "Standing lineup");
            await CaptureFrame($"{version}-DeckStand");
            await Until(() => intro.Stage == PreMatchStage.CaptainExchange && intro.StageElapsed >= .30f, "Captain approach");
            await CaptureFrame($"{version}-Captain-Approach");
            await Until(() => intro.StageElapsed >= 1.05f, "Captain exchange hold");
            await CaptureFrame($"{version}-Captain-Hold");
            if (version == "After")
            {
                await Until(() => intro.StageElapsed >= 2.30f, "Captain return turn");
                await CaptureFrame($"{version}-Captain-Return");
            }
            if (intro.CastCount != 14) throw new InvalidOperationException("Unexpected ceremony cast.");
            return $"Captured five advancing walk poses, lineup and captain approach/hold. {string.Join(", ", stageTimes)}.";
        }
        finally { UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= PlaceCamera; }
    }

    private static async Task CaptureFrame(string name)
    {
        await Awaitable.EndOfFrameAsync();
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        try { File.WriteAllBytes($"{Folder}/{name}.jpg", texture.EncodeToJPG(90)); }
        finally { Object.Destroy(texture); }
    }

    private static async Task<string> Swim(string variant)
    {
        var match = Object.FindAnyObjectByType<PrototypeMatch>();
        var view = Object.FindAnyObjectByType<ArenaDeveloperCamera>();
        var ball = Object.FindAnyObjectByType<BallController>();
        var selection = Object.FindAnyObjectByType<PlayerSelection>();
        if (!Application.isPlaying || match == null || HubMatchBridge.Active)
            throw new InvalidOperationException("Run the isolated Underwater Freestyle Test Lab.");
        await Until(() => match.IsPlaying && !match.AwaitingOpeningPossession && view.ViewIndex == 2, "Lab staging");
        var player = selection.ActivePlayer;
        var rig = player.GetComponentInChildren<RiggedSwimmerVisual>();
        var animator = rig.Animator;
        var motorInput = player.GetComponent<SwimmerInput>();
        var lens = view.GetComponent<Camera>();
        var anchor = view.viewpoints[2];
        var ring = player.transform.Find("SelectionRing");
        var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        var knee = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        var foot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        var toe = animator.GetBoneTransform(HumanBodyBones.LeftToes);
        var late = typeof(RiggedSwimmerVisual).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        Directory.CreateDirectory(Folder);
        Vector3 heading = player.Motor.Facing;
        motorInput.enabled = false;
        ball.ResetForRestart(-heading * 7f);
        player.Motor.SetMoveInput(new Vector2(heading.x, heading.z));
        await Until(() => rig.Locomotion > .85f && !ball.IsDribbling, "Advancing crawl");
        float first = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        await Until(() => animator.GetCurrentAnimatorStateInfo(0).normalizedTime - first > 1f, "Advancing complete crawl cycle");
        var stamina = player.GetComponent<SwimmerStamina>();
        stamina.RequestSprint(true);
        await Until(() => rig.Locomotion > 1.2f, "Advancing sprint");
        float sprintStart = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        await Until(() => animator.GetCurrentAnimatorStateInfo(0).normalizedTime - sprintStart > .9f, "Advancing sprint cycle");
        stamina.RequestSprint(false);
        player.Motor.SetMoveInput(Vector2.zero);
        await Until(() => rig.Locomotion < .08f, "Return to upright tread");
        if (variant == "After")
        {
            player.Motor.ResetPose(Vector3.zero, Quaternion.LookRotation(heading));
            ball.ResetForRestart(heading * .84f);
            if (!player.Interaction.TryCollect()) throw new InvalidOperationException("Ball-leading setup could not acquire the ball.");
            player.Motor.SetMoveInput(new Vector2(heading.x, heading.z), true);
            await Until(() => ball.IsDribbling && rig.Locomotion > .85f, "Production ball-leading crawl");
            stamina.RequestSprint(true);
            await Until(() => ball.IsDribbling && rig.Locomotion > 1.2f, "Production ball-leading sprint");
            stamina.RequestSprint(false);
            player.Motor.SetMoveInput(Vector2.zero);
            await Until(() => ball.Owner == player.Interaction && !ball.IsDribbling && rig.Locomotion < .10f,
                "Possession gathers back to tread");
            ball.ResetForRestart(-heading * 7f);
        }

        player.Motor.ResetPose(Vector3.zero, Quaternion.identity);
        await Awaitable.NextFrameAsync();
        rig.enabled = false;
        if (ring != null) ring.gameObject.SetActive(false);
        animator.speed = 0f;
        animator.keepAnimatorStateOnDisable = true;
        int state = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        var readings = new List<object>();
        try
        {
            lens.fieldOfView = 44f;
            foreach (var (label, move, count) in new[] { ("Crawl", 1f, 9), ("Sprint", 2f, 3), ("Tread", 0f, 1) })
            {
                for (int i = 0; i < count; i++)
                {
                    float phase = i / 8f;
                    animator.enabled = true;
                    animator.SetFloat("Move", move);
                    animator.Play(state, 0, phase);
                    animator.Update(0f);
                    float beforeHip = Vector3.Angle(hips.up, Vector3.up);
                    var jointsBefore = new[] { hips.localRotation.eulerAngles, knee.localRotation.eulerAngles, foot.localRotation.eulerAngles };
                    late.Invoke(rig, null);
                    animator.enabled = false;
                    readings.Add(new { label, phase, beforeHip, afterHip = Vector3.Angle(hips.up, Vector3.up),
                        jointsBefore, jointsAfter = new[] { hips.localRotation.eulerAngles, knee.localRotation.eulerAngles, foot.localRotation.eulerAngles },
                        toeFromHip = Vector3.Dot(toe.position - hips.position, Vector3.up) });
                    Vector3 focus = hips.position + (move > 0f ? Vector3.back * .22f : Vector3.zero);
                    var under = new Vector3(2.05f, -.44f, -1.07f);
                    await Record($"{variant}-{label}-Under-{i}", focus, under);
                    if (label != "Crawl" || i != 2) continue;
                    await Record($"{variant}-Crawl-Side", focus, new Vector3(2.42f, -.08f, 0f));
                    await Record($"{variant}-Crawl-Rear", focus, new Vector3(.15f, -.18f, -2.22f));
                    await Record($"{variant}-Crawl-Above", focus, new Vector3(.56f, 2.6f, -.21f));
                }
            }
            File.WriteAllText($"{Folder}/{variant}-swim.json",
                Newtonsoft.Json.JsonConvert.SerializeObject(readings, Newtonsoft.Json.Formatting.Indented,
                    new Newtonsoft.Json.JsonSerializerSettings { ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore }));
            return $"Advancing crawl, sprint and return to tread, then {readings.Count} matched pose samples." +
                (variant == "After" ? " Live normal and sprint leading gathered back to stationary possession." : "");
        }
        finally
        {
            animator.enabled = true;
            animator.speed = 1f;
            rig.enabled = true;
            motorInput.enabled = true;
            if (ring != null) ring.gameObject.SetActive(true);
        }

        async Task Record(string name, Vector3 focus, Vector3 offset)
        {
            anchor.position = focus + offset;
            anchor.LookAt(focus);
            await Awaitable.NextFrameAsync();
            await Awaitable.EndOfFrameAsync();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            try { File.WriteAllBytes($"{Folder}/{name}.jpg", texture.EncodeToJPG(88)); }
            finally { Object.Destroy(texture); }
            await Awaitable.NextFrameAsync();
        }
    }

    private static async Task Until(Func<bool> condition, string name)
    {
        float deadline = Time.realtimeSinceStartup + 12f;
        while (!condition())
        {
            if (Time.realtimeSinceStartup > deadline) throw new TimeoutException(name);
            await Awaitable.NextFrameAsync();
        }
    }
}

