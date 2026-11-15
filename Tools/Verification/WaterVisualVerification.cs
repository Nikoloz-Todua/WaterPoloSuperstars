using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class WaterVisualVerification
{
    public static object Setup()
    {
        Check(!Application.isPlaying, "Requires Edit Mode.");
        const string root = ToduStudios.WaterPolo.Editor.WaterVisualTestSetup.Root;
        string guid = AssetDatabase.AssetPathToGUID(ToduStudios.WaterPolo.Editor.WaterVisualTestSetup.ScenePath);
        var profile = AssetDatabase.LoadAssetAtPath<PoolWaterQualityProfile>(root + "/Settings/WaterQuality_High.asset");
        float scale = profile.reflectionScale;
        string hash = Hash("Assets/Scenes/PrototypePool.unity");
        int count = Object.FindObjectsByType<Transform>().Length;
        ToduStudios.WaterPolo.Editor.WaterVisualTestSetup.SetUp();
        Check(AssetDatabase.AssetPathToGUID(ToduStudios.WaterPolo.Editor.WaterVisualTestSetup.ScenePath) == guid, "Scene identity preserved.");
        Check(profile.reflectionScale == scale && Object.FindObjectsByType<Transform>().Length == count, "Repeat-safe scene and profile tuning.");
        Check(Hash("Assets/Scenes/PrototypePool.unity") == hash, "PrototypePool remains byte-identical.");
        Check(Object.FindObjectsByType<PoolWaterTestSwimmer>().Length == 3, "Three test swimmers.");
        Check(Object.FindObjectsByType<Collider>().Length == 0, "No gameplay colliders in visual fixture.");
        Check(Object.FindAnyObjectByType<ReflectionProbe>().texture != null, "Baked environment reflection assigned.");
        return new { passed = true, sceneGuid = guid, objects = count, swimmers = 3, prototypeUnchanged = true };
    }

    public static async Task<object> Rendering()
    {
        Check(Application.isPlaying && !EditorApplication.isPaused, "Requires advancing Play Mode.");
        Directory.CreateDirectory("Logs/WaterVisuals");
        var visual = Object.FindAnyObjectByType<PoolWaterVisuals>();
        var camera = Object.FindAnyObjectByType<PoolWaterTestCamera>();
        var swimmers = Object.FindObjectsByType<PoolWaterTestSwimmer>();
        var renders = swimmers.SelectMany(s => s.GetComponentsInChildren<Renderer>()).ToArray();
        var cast = renders.Select(r => r.shadowCastingMode).ToArray();
        var animations = swimmers.Select(s => s.animate).ToArray();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        float oldTimeScale = Time.timeScale; int oldQuality = visual.qualityIndex, oldView = camera.viewIndex;
        int first = Time.frameCount;
        var results = new List<object>();
        try
        {
            await Frames(5); Check(Time.frameCount > first, "Player loop advances.");
            Time.timeScale = 0f;
            foreach (var swimmer in swimmers) swimmer.animate = false;
            for (int i = 0; i < 3; i++)
            {
                await KeyPress(keyboard, i == 0 ? Key.Digit1 : i == 1 ? Key.Digit2 : Key.Digit3);
                Check(visual.qualityIndex == i && QualitySettings.renderPipeline == visual.profiles[i].testPipeline, "Injected quality key updates test pipeline.");
                Check(visual.reflectionManager.enabled == (i == 2), "Only High renders planar reflections.");
                Check(visual.profiles[i].testPipeline.supportsCameraDepthTexture, "Depth retained at every quality.");
                Check(visual.RuntimeMaterial.IsKeywordEnabled("_ENABLEREFRACTION") == (i != 0), "Refraction matches quality budget.");
                await Shot("Quality-" + i);
                var messages = ShaderUtil.GetShaderMessages(visual.RuntimeMaterial.shader);
                Check(visual.RuntimeMaterial.shader.isSupported && !messages.Any(m => m.severity.ToString() == "Error"), "Rendered water variant compiles.");
                results.Add(new { quality = i, compiled = true, actualGameView = true });
            }
            for (int i = 0; i < 3; i++)
            {
                int previous = camera.viewIndex; await KeyPress(keyboard, Key.V);
                Check(camera.viewIndex == (previous + 1) % 3, "Injected V changes viewpoint.");
                await Shot("View-" + camera.viewIndex);
            }
            camera.SetView(0);
            // Remove shadow contributions before occlusion tests so a hidden limb cannot pass by casting a shadow.
            foreach (var renderer in renders) renderer.shadowCastingMode = ShadowCastingMode.Off;
            var baseline = await Shot("Transparency-Baseline");
            visual.waterSurface.enabled = false; var dry = await Shot("Transparency-Dry"); visual.waterSurface.enabled = true;
            int waterPixels = Changed(baseline, dry); Check(waterPixels > 3000, "Visible water changes rendered image.");
            var submerged = new List<object>();
            foreach (var swimmer in swimmers)
            {
                var parts = swimmer.GetComponentsInChildren<Renderer>().Where(r => r.bounds.max.y < -0.025f).ToArray();
                Check(parts.Length >= 4, "Fixture includes fully submerged limbs.");
                foreach (var part in parts) part.enabled = false;
                var without = await Shot("Hidden-" + swimmer.name.Replace(" ", ""));
                foreach (var part in parts) part.enabled = true;
                int pixels = Changed(baseline, without); Check(pixels > 80, "Underwater opaque geometry contributes visible pixels: " + swimmer.name);
                submerged.Add(new { swimmer = swimmer.name, whollySubmergedMeshes = parts.Length, changedPixels = pixels, shadowsExcluded = true });
            }
            var mat = visual.RuntimeMaterial;
            float refract = mat.GetFloat("_Refraction_Strength"), distant = mat.GetFloat("_Refraction_Distance_Strength");
            mat.SetFloat("_Refraction_Strength", 0f); mat.SetFloat("_Refraction_Distance_Strength", 0f);
            var noRefraction = await Shot("Refraction-Off");
            mat.SetFloat("_Refraction_Strength", refract); mat.SetFloat("_Refraction_Distance_Strength", distant);
            int refractedPixels = Changed(baseline, noRefraction); Check(refractedPixels > 80, "Ripple refraction visibly distorts submerged image.");
            mat.DisableKeyword("_ENABLECAUSTICS"); var noCaustics = await Shot("Caustics-Off"); mat.EnableKeyword("_ENABLECAUSTICS");
            int causticPixels = Changed(baseline, noCaustics); Check(causticPixels > 300, "Underwater caustics contribute visible light patterns.");
            float reflection = mat.GetFloat("_Reflection_Strength"); mat.SetFloat("_Reflection_Strength", 0f);
            var noReflection = await Shot("Reflections-Off"); mat.SetFloat("_Reflection_Strength", reflection);
            int reflectionPixels = Changed(baseline, noReflection); Check(reflectionPixels > 100, "Pool hall reflections contribute visible pixels.");
            foreach (var swimmer in swimmers) swimmer.animate = false;
            Time.timeScale = 1f;
            var animationA = await Shot("Animation-A");
            await Task.Delay(650);
            var animationB = await Shot("Animation-B");
            int animatedPixels = Changed(animationA, animationB); Check(animatedPixels > 100, "Ripples/caustics animate while test character poses are frozen.");
            return new { passed = true, firstFrame = first, lastFrame = Time.frameCount, qualities = results,
                submerged, waterPixels, refractedPixels, causticPixels, reflectionPixels, animatedPixels,
                shader = mat.shader.name, platform = SystemInfo.graphicsDeviceType.ToString(), hardwarePerformanceClaim = false };
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard); Time.timeScale = oldTimeScale;
            for (int i = 0; i < swimmers.Length; i++) swimmers[i].animate = animations[i];
            for (int i = 0; i < renders.Length; i++) { renders[i].enabled = true; renders[i].shadowCastingMode = cast[i]; }
            visual.waterSurface.enabled = true; visual.ApplyQuality(oldQuality); camera.SetView(oldView);
        }
    }

    public static async Task<object> CaptureFinal()
    {
        var camera = Object.FindAnyObjectByType<PoolWaterTestCamera>();
        int old = camera.viewIndex;
        try { for (int i = 0; i < 3; i++) { camera.SetView(i); await Shot("Final-" + i); } }
        finally { camera.SetView(old); }
        return new { captured = 3, actualGameView = true, folder = "Logs/WaterVisuals" };
    }

    public static object GameplaySetup()
    {
        Check(!Application.isPlaying, "Requires PrototypePool Edit Mode.");
        Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "PrototypePool", "Existing match scene active.");
        string before = Hash("Assets/Scenes/PrototypePool.unity");
        int objects = Object.FindObjectsByType<Transform>().Length;
        ToduStudios.WaterPolo.Editor.PoolWaterGameplaySetup.Apply();
        Check(Hash("Assets/Scenes/PrototypePool.unity") == before && objects == Object.FindObjectsByType<Transform>().Length, "Repeated gameplay setup leaves saved scene byte-identical.");
        var water = Object.FindAnyObjectByType<PoolWaterVisuals>();
        Check(water != null && !water.useIsolatedTestPipeline && !water.keyboardShortcuts, "Presentation does not own match pipeline or keyboard input.");
        Check(water.GetComponentsInChildren<Collider>().Length == 0 && Object.FindObjectsByType<PlanarReflectionManager>().Length == 0, "No added colliders or planar camera manager.");
        Check(Object.FindAnyObjectByType<ToduStudios.WaterPolo.PrototypeRoster>().Members.Count == 14, "Fourteen athletes retained.");
        return new { passed = true, objects, athletes = 14, repeatSafe = true, noAddedPhysics = true };
    }

    public static async Task<object> GameplayPresentation()
    {
        Check(Application.isPlaying, "Direct PrototypePool Play Mode required.");
        var water = Object.FindAnyObjectByType<PoolWaterVisuals>();
        var match = Object.FindAnyObjectByType<ToduStudios.WaterPolo.PrototypeMatch>();
        var roster = Object.FindAnyObjectByType<ToduStudios.WaterPolo.PrototypeRoster>();
        var preferences = Object.FindAnyObjectByType<ToduStudios.WaterPolo.GamePreferences>();
        Check(water != null && !water.useIsolatedTestPipeline && !water.keyboardShortcuts, "Match uses presentation only, without a pipeline/key override.");
        Check(water.reflectionManager == null && water.reflectionVolume == null && Object.FindObjectsByType<Camera>().Length == 1, "No second camera or planar manager in match.");
        Check(roster.Members.Count == 14 && Object.FindObjectsByType<ToduStudios.WaterPolo.BallController>().Length == 1, "Fourteen athletes and one authoritative ball.");
        Check(water.GetComponentsInChildren<Collider>().Length == 0, "Water presentation adds no physics colliders.");
        var pipeline = GraphicsSettings.currentRenderPipeline;
        int first = Time.frameCount, originalQuality = (int)preferences.Current.graphics;
        bool hadPreference = PlayerPrefs.HasKey(ToduStudios.WaterPolo.GamePreferences.StorageKey);
        string preference = PlayerPrefs.GetString(ToduStudios.WaterPolo.GamePreferences.StorageKey, "");
        var result = new List<object>();
        try
        {
            for (int i = 0; i < 3; i++)
            {
                preferences.SetGraphics(i); await Frames(5);
                Check(water.qualityIndex == i && GraphicsSettings.currentRenderPipeline == pipeline, "Existing graphics preference updates water without changing gameplay pipeline.");
                Check(!water.RuntimeMaterial.IsKeywordEnabled("_ENABLEPLANERREFLECTION"), "Match high quality still uses baked reflections.");
                await Shot("Match-Quality-" + i);
                Check(!ShaderUtil.GetShaderMessages(water.RuntimeMaterial.shader).Any(m => m.severity.ToString() == "Error"), "Match water variant compiles.");
                result.Add(new { quality = i, pipeline = pipeline.name, singleCamera = true, rendered = true });
            }
            preferences.SetGraphics(originalQuality);
            match.RestartMatch();
            var selected = Object.FindAnyObjectByType<ToduStudios.WaterPolo.PlayerSelection>().ActivePlayer;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                Vector3 before = selected.Motor.Position;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                await Seconds(.6f); InputSystem.QueueStateEvent(keyboard, new KeyboardState()); await Frames(3);
                Check(Vector3.Distance(before, selected.Motor.Position) > .2f, "Existing keyboard movement remains physical.");
            }
            finally { InputSystem.RemoveDevice(keyboard); }
            var ball = Object.FindAnyObjectByType<ToduStudios.WaterPolo.BallController>();
            ball.ResetForRestart(selected.Motor.Position + selected.Motor.Facing * .3f);
            Check(selected.Interaction.TryCollect(), "Existing authoritative collection succeeds.");
            Check(selected.Interaction.TryShoot(12f), "Existing physical shot API succeeds.");
            Check(ball.Owner == null && ball.IsAirborne, "Water rendering does not replace ball flight.");
            await Seconds(.25f); await Shot("Match-Final");
            return new { passed = true, firstFrame = first, lastFrame = Time.frameCount, athletes = 14, qualities = result,
                movement = true, collection = true, physicalShot = true, noAddedPhysics = true, originalPipeline = pipeline.name };
        }
        finally
        {
            preferences.SetGraphics(originalQuality); preferences.Save();
            if (hadPreference) PlayerPrefs.SetString(ToduStudios.WaterPolo.GamePreferences.StorageKey, preference);
            else PlayerPrefs.DeleteKey(ToduStudios.WaterPolo.GamePreferences.StorageKey);
            PlayerPrefs.Save();
        }
    }

    private static async Task Seconds(float seconds)
    {
        float until = Time.time + seconds; var watch = Stopwatch.StartNew();
        while (Time.time < until) { Check(watch.Elapsed.TotalSeconds < 15, "Simulation time advances."); await Task.Yield(); }
    }

    private static async Task<Key> KeyPress(Keyboard keyboard, Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); await Frames(2);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); await Frames(2); return key;
    }
    private static async Task<Color32[]> Shot(string name)
    {
        await Frames(3); var watch = Stopwatch.StartNew();
        while (ShaderUtil.anythingCompiling) { Check(watch.Elapsed.TotalSeconds < 30, "Shader variant compilation completes."); await Task.Delay(100); }
        await Frames(3);
        string path = "Logs/WaterVisuals/" + name + ".png";
        ScreenCapture.CaptureScreenshot(path); await Frames(4);
        var image = new Texture2D(2, 2, TextureFormat.RGB24, false);
        try { Check(image.LoadImage(File.ReadAllBytes(path)), "Game view screenshot readable."); return image.GetPixels32(); }
        finally { Object.Destroy(image); }
    }
    private static int Changed(Color32[] a, Color32[] b)
    {
        Check(a.Length == b.Length, "Capture dimensions stable."); int count = 0;
        for (int i = 0; i < a.Length; i++) if (Math.Abs(a[i].r - b[i].r) + Math.Abs(a[i].g - b[i].g) + Math.Abs(a[i].b - b[i].b) > 18) count++;
        return count;
    }
    private static async Task Frames(int count)
    {
        int target = Time.frameCount + count; var watch = Stopwatch.StartNew();
        while (Time.frameCount < target) { Check(watch.Elapsed.TotalSeconds < 15, "Player loop must advance."); await Task.Yield(); }
    }
    private static string Hash(string path)
    {
        using (var sha = System.Security.Cryptography.SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));
    }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}
