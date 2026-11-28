using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.WaterVisuals;
using ToduStudios.WaterPolo.Hub;
using Object = UnityEngine.Object;

public static class Milestone30HVerification
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static string GarmentInventory()
    {
        var roster = Object.FindAnyObjectByType<PrototypeRoster>();
        if (roster == null) throw new InvalidOperationException("Open the saved Olympic arena.");
        return string.Join(Environment.NewLine, roster.Members.Select(athlete =>
        {
            var equipment = athlete.GetComponentInChildren<AthleteEquipment>(true);
            var bindings = new SerializedObject(equipment);
            var suit = (SkinnedMeshRenderer)bindings.FindProperty("briefs").objectReferenceValue;
            var body = (SkinnedMeshRenderer)bindings.FindProperty("body").objectReferenceValue;
            var kit = (AthleteKitStyle)bindings.FindProperty(athlete.Team == PrototypeTeam.Blue ? "blue" : "white").objectReferenceValue;
            var fallbacks = bindings.FindProperty("fallback");
            int visible = 0;
            for (int j = 0; j < fallbacks.arraySize; j++)
                if (fallbacks.GetArrayElementAtIndex(j).objectReferenceValue is Renderer r && r.enabled) visible++;
            var material = suit.sharedMaterial;
            return $"{athlete.Team} #{athlete.CapNumber} {athlete.Role}: {suit.name}, {suit.sharedMesh.GetIndexCount(0) / 3} triangles, {suit.bones.Length} bones, {suit.sharedMesh.bindposeCount} bind poses, " +
                $"{(suit.enabled ? "enabled" : "disabled")}, {visible} enabled fallback(s), {AssetDatabase.GetAssetPath(suit.sharedMesh)}. " +
                $"Material {material.name} ({material.shader.name}, {material.GetTexture("_BaseMap")?.name}), kit match {material == kit.Briefs}. " +
                $"Body {body.sharedMesh.name}, shared root {suit.rootBone == body.rootBone}, same ordered bones {suit.bones.SequenceEqual(body.bones)}.";
        }));
    }

    public static async Task<string> GarmentPreview()
    {
        var rig = Object.FindAnyObjectByType<AthletePreviewCamera>();
        if (!Application.isPlaying || rig == null) throw new InvalidOperationException("Launch Competition Swimwear Inspection.");
        rig.framing = 2;
        rig.angle = 25f;
        rig.dry = true;
        await Awaitable.WaitForSecondsAsync(.24f);
        await Capture("Briefs-Standing");
        rig.elevation = -24f;
        rig.angle = 55f;
        rig.dry = false;
        rig.athlete.previewState = 2;
        await Awaitable.WaitForSecondsAsync(.45f);
        await Capture("Briefs-Sprint");
        return "Captured standing and advancing sprint on the athlete preview.";
    }

    public static async Task<string> UnderwaterBriefs()
    {
        var selection = Object.FindAnyObjectByType<PlayerSelection>();
        var view = Object.FindAnyObjectByType<ArenaDeveloperCamera>();
        await Until(() => view != null && view.ViewIndex == 2 && selection?.ActivePlayer != null);
        var player = selection.ActivePlayer;
        var input = player.GetComponent<SwimmerInput>();
        input.enabled = false;
        try
        {
            player.Motor.SetMoveInput(Vector2.up);
            await Awaitable.WaitForSecondsAsync(.82f);
            await Capture("Briefs-Production-Underwater");
            return $"Rendered {player.Team} #{player.CapNumber} in {player.gameObject.scene.name} at {player.Motor.Velocity.magnitude:F2} m/s.";
        }
        finally
        {
            player.Motor.SetMoveInput(Vector2.zero);
            input.enabled = true;
        }
    }

    public static async Task<string> Keeper(string label, bool capture = true)
    {
        var ball = Object.FindAnyObjectByType<BallController>();
        var view = Object.FindAnyObjectByType<ArenaDeveloperCamera>();
        if (ball == null || HubMatchBridge.Active) throw new InvalidOperationException("Use a keeper Test Lab.");
        await Until(() => view != null && view.ViewIndex == 2 && ball.Match.IsPlaying);
        var keeper = Object.FindObjectsByType<PrototypeGoalkeeper>().Single(k => k.enabled);
        var rig = keeper.Identity.GetComponentInChildren<RiggedSwimmerVisual>();
        var weight = typeof(RiggedSwimmerVisual).GetField("saveWeight", Private);
        var rise = typeof(RiggedSwimmerVisual).GetField("keeperRise", Private);
        var left = rig.Animator.GetBoneTransform(HumanBodyBones.LeftHand);
        var right = rig.Animator.GetBoneTransform(HumanBodyBones.RightHand);
        var shoulders = rig.Animator.GetBoneTransform(HumanBodyBones.Chest);
        if (label == "Neutral")
        {
            await Awaitable.WaitForSecondsAsync(.35f);
            float neutralLeft = left.position.y - shoulders.position.y;
            float neutralRight = right.position.y - shoulders.position.y;
            if (capture) await Capture("Keeper-Neutral");
            Vector3 start = keeper.Identity.Motor.Position;
            var carrier = ball.Owner.GetComponent<SwimmerIdentity>();
            Vector3 along = keeper.DefendedGoal.transform.right;
            carrier.Motor.ResetPose(carrier.Motor.Position + along * 2.5f, carrier.transform.rotation);
            await Awaitable.WaitForSecondsAsync(.75f);
            if (capture) await Capture("Keeper-Tracking");
            return $"Neutral wrist/chest L {neutralLeft:F2} m, R {neutralRight:F2} m. " +
                $"Tracking moved {Vector3.Dot(keeper.Identity.Motor.Position - start, along):F2} m toward the carrier. Attempts {keeper.SaveAttempts}.";
        }
        await Until(() => ball.ReleaseKind is BallReleaseKind.Shot or BallReleaseKind.Pass);
        float deadline = Time.time + 1.12f;
        float peakWeight = 0f, peakRise = 0f;
        float peakLeft = float.MinValue, peakRight = float.MinValue;
        bool pictured = false;
        while (Time.time < deadline)
        {
            await Awaitable.NextFrameAsync();
            peakWeight = Mathf.Max(peakWeight, (float)weight.GetValue(rig));
            peakRise = Mathf.Max(peakRise, (float)rise.GetValue(rig));
            peakLeft = Mathf.Max(peakLeft, left.position.y - shoulders.position.y);
            peakRight = Mathf.Max(peakRight, right.position.y - shoulders.position.y);
            if (capture && !pictured && peakWeight > (label == "Miss" ? .24f : .76f))
            {
                pictured = true;
                await Capture("Keeper-" + label);
            }
        }
        if (capture && label == "Miss") await Capture("Keeper-Recovered");
        return $"{label}: attempts {keeper.SaveAttempts}, catches {keeper.Catches}, parries {keeper.Deflections}, " +
            $"phase {ball.Match.Phase}, score Blue {ball.Match.BlueScore}–{ball.Match.WhiteScore} White, " +
            $"save blend peak/final {peakWeight:F2}/{(float)weight.GetValue(rig):F2}, " +
            $"peak lift {peakRise:F2} m, wrist/chest peak L {peakLeft:F2} m, R {peakRight:F2} m, owner {ball.Owner?.name ?? "none"}.";
    }

    private static async Task Until(Func<bool> ready)
    {
        float deadline = Time.realtimeSinceStartup + 7f;
        while (!ready())
        {
            if (!Application.isPlaying || Time.realtimeSinceStartup > deadline)
                throw new TimeoutException("The isolated fixture did not reach its staged state.");
            await Awaitable.NextFrameAsync();
        }
    }

    private static async Task Capture(string name)
    {
        await Awaitable.EndOfFrameAsync();
        Directory.CreateDirectory("Logs/Milestone30H");
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        try { File.WriteAllBytes($"Logs/Milestone30H/{name}.jpg", texture.EncodeToJPG(90)); }
        finally { Object.Destroy(texture); }
    }
}

