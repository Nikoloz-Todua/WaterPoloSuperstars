using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class Milestone30BEquipmentVerification
{
    private const string Arena = "Assets/Scenes/OlympicMatchArena.unity";
    private const string Garment = "Assets/ProductionAthletes/Candidate/AtlasCompetitionBriefs.fbx";
    private const string Output = "docs/Evidence/Milestone30B";

    public static object Inventory()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != Arena) throw new InvalidOperationException("Open OlympicMatchArena first.");
        var roster = Object.FindAnyObjectByType<PrototypeRoster>();
        var camera = Object.FindAnyObjectByType<ArenaDeveloperCamera>()?.GetComponent<Camera>();
        var athletes = roster.Members.Select(player =>
        {
            var briefs = Briefs(player);
            var mesh = briefs.sharedMesh;
            var material = briefs.sharedMaterial;
            var bones = briefs.bones;
            var layer = briefs.gameObject.layer;
            return new
            {
                player.PlayerId, player.CapNumber, team = player.Team.ToString(), goalkeeper = !player.IsFieldPlayer,
                renderer = briefs.name, briefs.enabled, active = briefs.gameObject.activeInHierarchy, briefs.forceRenderingOff,
                mesh = AssetDatabase.GetAssetPath(mesh), triangles = mesh.GetIndexCount(0) / 3u, mesh.vertexCount,
                material = AssetDatabase.GetAssetPath(material), shader = material.shader.name, material.shader.isSupported,
                material.renderQueue, rgba = material.color.ToString(), surface = material.GetFloat("_Surface"),
                cull = material.GetFloat("_Cull"), zWrite = material.GetFloat("_ZWrite"), layer,
                cameraIncludesLayer = camera != null && (camera.cullingMask & (1 << layer)) != 0,
                lod = briefs.GetComponentInParent<LODGroup>()?.name,
                bounds = briefs.localBounds.ToString(), root = briefs.rootBone?.name,
                boneSlots = bones.Length, presentBones = bones.Count(bone => bone != null),
                bindPoses = mesh.bindposeCount,
            };
        }).ToArray();
        if (athletes.Length != 14 || athletes.Any(value => value.mesh != Garment || value.triangles != 2688))
            throw new InvalidOperationException("Expected the rebuilt briefs on all fourteen players.");
        return new
        {
            scene = scene.path, scene.isDirty, playing = EditorApplication.isPlaying,
            boot = AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene), athletes,
        };
    }

    public static async Task<string> Live()
    {
        var match = Object.FindAnyObjectByType<PrototypeMatch>();
        var ball = Object.FindAnyObjectByType<BallController>();
        var roster = Object.FindAnyObjectByType<PrototypeRoster>();
        var intro = Object.FindAnyObjectByType<PreMatchPresentation>();
        var view = Object.FindAnyObjectByType<ArenaDeveloperCamera>();
        if (!Application.isPlaying || SceneManager.GetActiveScene().path != Arena || match == null || view == null)
            throw new InvalidOperationException("Play OlympicMatchArena in a visible Game view.");
        Directory.CreateDirectory(Output);

        if (intro.Active)
        {
            await Until(() => intro.Stage == PreMatchStage.TeamWalkout, "walkout");
            await Capture("Equipment-Walkout-A");
            await Task.Delay(350);
            await Capture("Equipment-Walkout-B");
            await Until(() => intro.Stage == PreMatchStage.FormalLineup && intro.StageElapsed > .4f, "formal lineup");
            await Capture("Equipment-Lineup");
            intro.SkipAll();
        }
        await Until(() => match.IsPlaying && !match.AwaitingOpeningPossession, "live play");

        var lens = view.GetComponent<Camera>();
        var waypoint = view.viewpoints[2];
        Vector3 waypointPosition = waypoint.position;
        Quaternion waypointRotation = waypoint.rotation;
        float fov = lens.fieldOfView;
        var contestants = roster.Members.GroupBy(player => (player.Team, player.IsFieldPlayer))
            .Select(group => group.First()).ToArray();
        try
        {
            view.SetView(2);
            lens.fieldOfView = 41f;
            foreach (var athlete in contestants)
            {
                var briefs = Briefs(athlete);
                if (!briefs.enabled || briefs.forceRenderingOff || !briefs.gameObject.activeInHierarchy)
                    throw new InvalidOperationException($"The live kit is hidden for {athlete.PlayerId}.");
                string name = $"Equipment-{athlete.Team}-{(athlete.IsFieldPlayer ? "Field" : "Keeper")}";
                async Task Photograph(string angle, float side, float height, float forward)
                {
                    await Awaitable.NextFrameAsync();
                    Vector3 focus = Vector3.Lerp(briefs.bounds.center, athlete.transform.position, .36f);
                    waypoint.position = focus + athlete.transform.right * side +
                        athlete.transform.forward * forward + Vector3.up * height;
                    waypoint.LookAt(focus);
                    await Capture(name + "-" + angle);
                }
                await Photograph("Front", .38f, .85f, 2.05f);
                await Photograph("Side", 2.05f, .64f, 0f);
                await Photograph("Rear", -.38f, .76f, -2.05f);
                await Photograph("Underwater", 1.65f, -.30f, .80f);
            }
            return $"Captured live production athlete kits with ball radius {ball.Radius:F2} m.";
        }
        finally
        {
            lens.fieldOfView = fov;
            waypoint.SetPositionAndRotation(waypointPosition, waypointRotation);
            view.SetView(-1);
        }
    }

    public static async Task<string> Readability()
    {
        var match = Object.FindAnyObjectByType<PrototypeMatch>();
        var ball = Object.FindAnyObjectByType<BallController>();
        var roster = Object.FindAnyObjectByType<PrototypeRoster>();
        var view = Object.FindAnyObjectByType<ArenaDeveloperCamera>();
        var water = Object.FindAnyObjectByType<PoolWaterVisuals>();
        var intro = Object.FindAnyObjectByType<PreMatchPresentation>();
        if (!Application.isPlaying || match == null || water == null || view == null)
            throw new InvalidOperationException("Play OlympicMatchArena in the Game view.");
        if (intro.Active) intro.SkipAll();
        await Until(() => match.IsPlaying && !match.AwaitingOpeningPossession, "live play");
        Directory.CreateDirectory(Output);

        var tile = (Renderer)new SerializedObject(water).FindProperty("waterSurface").objectReferenceValue;
        var material = tile.sharedMaterial;
        string[] colorKeys = { "_Color_Deep", "_Color_Shallow", "_Specular_Color" };
        Color[] oldColors = { new Color(.013f, .24f, .40f, .71f), new Color(.064f, .46f, .59f, .205f),
            new Color(1.12f, 1.18f, 1.20f, 1f) };
        Color[] installedColors = colorKeys.Select(material.GetColor).ToArray();
        float installedDepth = material.GetFloat("_Water_Depth");
        float installedRefraction = material.GetFloat("_Refraction_Strength");
        var camera = view.GetComponent<Camera>();
        float fov = camera.fieldOfView;
        var waypoint = view.viewpoints[2];
        Vector3 priorEye = waypoint.position;
        Quaternion priorLook = waypoint.rotation;
        float clock = Time.timeScale;
        var brains = Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>()
            .Concat(Object.FindObjectsByType<PrototypeGoalkeeper>()).Where(brain => brain.enabled).ToArray();

        void Tint(bool baseline)
        {
            for (int i = 0; i < colorKeys.Length; i++)
                material.SetColor(colorKeys[i], baseline ? oldColors[i] : installedColors[i]);
            material.SetFloat("_Water_Depth", baseline ? 1.72f : installedDepth);
            material.SetFloat("_Refraction_Strength", baseline ? (installedRefraction > .02f ? .034f : .025f) : installedRefraction);
        }

        try
        {
            foreach (var brain in brains) brain.enabled = false;
            foreach (var player in roster.Members)
                player.Motor.ResetPose(new Vector3(player.Team == PrototypeTeam.Blue ? -8f : 8f,
                    0f, -6f + player.CapNumber), Quaternion.identity);
            ball.ResetForRestart(Vector3.back * 8f);
            camera.fieldOfView = 40f;
            view.SetView(2);
            waypoint.position = new Vector3(1.22f, .89f, 2.30f);
            waypoint.LookAt(new Vector3(0f, -.18f, 0f));
            foreach (var athlete in roster.FieldPlayers.GroupBy(p => p.Team).Select(team => team.First()))
            {
                athlete.Motor.ResetPose(Vector3.zero, Quaternion.identity);
                await Awaitable.WaitForSecondsAsync(.24f);
                Time.timeScale = 0f;
                var briefs = Briefs(athlete);
                var installedKit = briefs.sharedMaterial;
                Material oldWhite = null;
                try
                {
                    if (athlete.Team == PrototypeTeam.White)
                    {
                        oldWhite = new Material(installedKit);
                        oldWhite.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(
                            "Assets/ProductionAthletes/AuthenticEquipment/Textures/BriefsNeutral.png"));
                        oldWhite.SetColor("_BaseColor", new Color(.79f, .84f, .93f, 1f));
                        oldWhite.SetFloat("_Smoothness", 1f);
                        oldWhite.DisableKeyword("_EMISSION");
                        briefs.sharedMaterial = oldWhite;
                    }
                    Tint(true);
                    await Capture($"Equipment-{athlete.Team}-M30-Water");
                    Tint(false);
                    briefs.sharedMaterial = installedKit;
                    await Capture($"Equipment-{athlete.Team}-M30B-Water");
                }
                finally
                {
                    briefs.sharedMaterial = installedKit;
                    if (oldWhite != null) Object.Destroy(oldWhite);
                }
                Time.timeScale = clock;
                athlete.Motor.ResetPose(new Vector3(8f, 0f, -5f), Quaternion.identity);
            }
            return "Both team cloths captured at the same pose under the two pool finishes.";
        }
        finally
        {
            Tint(false);
            Time.timeScale = clock;
            camera.fieldOfView = fov;
            waypoint.SetPositionAndRotation(priorEye, priorLook);
            foreach (var brain in brains) if (brain != null) brain.enabled = true;
            view.SetView(-1);
        }
    }

    private static SkinnedMeshRenderer Briefs(SwimmerIdentity athlete)
    {
        var equipment = athlete.GetComponentInChildren<AthleteEquipment>();
        return (SkinnedMeshRenderer)new SerializedObject(equipment).FindProperty("briefs").objectReferenceValue;
    }

    private static async Task Capture(string name)
    {
        await Awaitable.EndOfFrameAsync();
        var frame = ScreenCapture.CaptureScreenshotAsTexture();
        try { File.WriteAllBytes(Path.Combine(Output, name + ".png"), frame.EncodeToPNG()); }
        finally { Object.Destroy(frame); }
    }

    private static async Task Until(Func<bool> condition, string label)
    {
        float deadline = Time.realtimeSinceStartup + 23f;
        while (!condition())
        {
            if (!Application.isPlaying || Time.realtimeSinceStartup > deadline) throw new TimeoutException(label);
            await Awaitable.NextFrameAsync();
        }
    }

}
