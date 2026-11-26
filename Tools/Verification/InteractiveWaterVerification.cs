using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.WaterVisuals;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class InteractiveWaterVerification
{
    private const string Output = "Logs/Milestone30/Water";
    private static T One<T>() where T : Object => Object.FindAnyObjectByType<T>();

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static async Task Wait(Func<bool> predicate, string label, float seconds = 8f)
    {
        var deadline = System.Diagnostics.Stopwatch.StartNew();
        while (!predicate())
        {
            if (deadline.Elapsed.TotalSeconds > seconds) throw new TimeoutException(label);
            await Task.Delay(9);
        }
    }

    private static Task Frame()
    {
        int current = Time.frameCount;
        return Wait(() => Time.frameCount > current, "The Game view failed to advance.");
    }

    private static async Task Image(string name)
    {
        ScreenCapture.CaptureScreenshot(Path.GetFullPath($"{Output}/{name}.png"));
        await Frame();
        await Frame();
    }

    public static async Task<object> Run()
    {
        Directory.CreateDirectory(Output);
        var match = One<PrototypeMatch>();
        var effects = One<WaterInteractionEffects>();
        var ball = One<BallController>();
        var roster = One<PrototypeRoster>();
        Require(Application.isPlaying && effects.UsesContactSampling, "Enter the saved interactive arena.");
        var introduction = One<PreMatchPresentation>();
        if (introduction.Active) introduction.SkipAll();
        await Wait(() => match.IsPlaying && !match.AwaitingOpeningPossession, "Opening sprint did not resolve.", 18f);
        var oldQuality = effects.Quality;
        var camera = One<ArenaDeveloperCamera>();
        var lensAnchor = camera.viewpoints[2];
        var priorAnchorPosition = lensAnchor.position;
        var priorAnchorRotation = lensAnchor.rotation;
        var selection = One<PlayerSelection>();
        bool selectionWasEnabled = selection.enabled;
        var brains = Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>().Concat(Object.FindObjectsByType<PrototypeGoalkeeper>())
            .Concat(Object.FindObjectsByType<SwimmerInput>()).Where(item => item.enabled).ToArray();
        var players = roster.Members.ToArray();
        Require(players.Length == 14, "The production roster should have fourteen athletes.");
        var active = players.First(p => p.Team == PrototypeTeam.Blue && p.IsFieldPlayer);
        try
        {
            // Scripted routes own motor commands while this comparison runs.
            selection.enabled = false;
            foreach (var brain in brains) brain.enabled = false;
            camera.SetView(2);
            void Look(Vector3 focus, Vector3 offset)
            {
                lensAnchor.position = focus + offset;
                lensAnchor.LookAt(focus);
            }

            var results = new List<object>();
            var timing = new FrameTiming[1];
            using var thread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
            using var sampling = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "WPS.Water.SampleContacts", 1);
            using var upload = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "WPS.Water.BuildContactMesh", 1);
            using var alloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
            foreach (var quality in new[] { WaterEffectsQuality.Off, WaterEffectsQuality.Low, WaterEffectsQuality.Standard, WaterEffectsQuality.High })
            {
                effects.Quality = quality;
                Look(Vector3.zero, new Vector3(13f, 11f, -18f));
                for (int i = 0; i < players.Length; i++)
                {
                    var player = players[i];
                    bool fromBlue = i < 7;
                    var direction = fromBlue ? Vector3.forward : Vector3.back;
                    player.Motor.ResetPose(new Vector3(-7.7f + i % 7 * 2.55f, 0, fromBlue ? -8f : 8f), Quaternion.LookRotation(direction));
                    player.GetComponent<SwimmerStamina>().RequestSprint(false);
                }
                ball.ResetForRestart(new Vector3(0f, 0f, 0f));
                for (int i = 0; i < players.Length; i++)
                {
                    players[i].Motor.SetMoveInput(i < 7 ? Vector2.up : Vector2.down);
                    players[i].GetComponent<SwimmerStamina>().RequestSprint((i & 1) == 0);
                }
                await Task.Delay(230);
                const int sampleCount = 36;
                int draws = 0, triangles = 0, maxEffects = 0, maxParticles = 0, gpuSamples = 0;
                int moving = 0;
                long main = 0, sample = 0, mesh = 0, allocated = 0;
                double gpu = 0;
                for (int i = 0; i < sampleCount; i++)
                {
                    FrameTimingManager.CaptureFrameTimings();
                    await Frame();
                    moving = Mathf.Max(moving, players.Count(player => player.Motor.Velocity.magnitude >= .5f));
                    draws += UnityStats.drawCalls;
                    triangles += UnityStats.triangles;
                    maxEffects = Mathf.Max(maxEffects, effects.ActiveEffects);
                    maxParticles = Mathf.Max(maxParticles, effects.BubbleCount);
                    if (thread.Valid) main += thread.LastValue;
                    if (sampling.Valid && quality != WaterEffectsQuality.Off) sample += sampling.LastValue;
                    if (upload.Valid && quality != WaterEffectsQuality.Off) mesh += upload.LastValue;
                    if (alloc.Valid) allocated += alloc.LastValue;
                    if (FrameTimingManager.GetLatestTimings(1, timing) > 0 && timing[0].gpuFrameTime > 0d)
                    { gpuSamples++; gpu += timing[0].gpuFrameTime; }
                }
                results.Add(new
                {
                    quality, sampleCount, moving, draws = draws / (float)sampleCount, triangles = triangles / (float)sampleCount,
                    maxEffects, maxParticles, mainMs = thread.Valid ? main / (1e6 * sampleCount) : (double?)null,
                    samplingMs = sampling.Valid ? sample / (1e6 * sampleCount) : (double?)null,
                    contactMeshMs = upload.Valid ? mesh / (1e6 * sampleCount) : (double?)null,
                    gpuMs = gpuSamples > 0 ? gpu / gpuSamples : (double?)null,
                    allocationsPerFrameWithFixture = alloc.Valid ? allocated / (float)sampleCount : (float?)null
                });
                if (quality == WaterEffectsQuality.High) await Image("Full-roster-high");
            }

            foreach (var player in players)
            {
                player.Motor.StopMotion();
                player.GetComponent<SwimmerStamina>().RequestSprint(false);
                player.Motor.ResetPose(new Vector3(player.Team == PrototypeTeam.Blue ? -8f : 8f, 0f, -10f + player.CapNumber * 2f), Quaternion.identity);
            }
            float line = One<PoolBounds>().Waterline;
            Vector3 impact = new Vector3(0f, line + .45f, 1f);
            Look(new Vector3(0f, line, 1.55f), new Vector3(2.45f, 1.0f, -1.5f));
            await Frame();
            var beginFlight = typeof(BallController).GetMethod("BeginFlight", BindingFlags.Instance | BindingFlags.NonPublic);
            async Task<float> Splash(string name, Vector3 velocity)
            {
                int before = effects.WaterImpacts;
                ball.ResetForRestart(impact);
                beginFlight.Invoke(ball, new object[] { impact, velocity });
                await Wait(() => effects.WaterImpacts == before + 1, "A physical water crossing was not observed.");
                Require(Mathf.Abs(effects.LastWaterImpactPoint.y - line) < .002f, "Impact must project onto the actual water plane.");
                await Task.Delay(88);
                await Image(name);
                await Task.Delay(165);
                Require(effects.WaterImpacts == before + 1, "Settling triggered additional ball impacts.");
                return effects.LastWaterImpactStrength;
            }
            float gentle = await Splash("Soft-ball-impact", new Vector3(.22f, -.3f, .35f));
            await Task.Delay(850);
            float fast = await Splash("Power-ball-impact", new Vector3(1.8f, -10f, 8f));
            Require(fast > gentle + .25f, "Fast ball impacts must be visibly stronger.");

            active.Motor.ResetPose(new Vector3(0f, 0f, -6f), Quaternion.identity);
            active.Motor.SetMoveInput(Vector2.up);
            int left = effects.LeftHandEntries, right = effects.RightHandEntries;
            for (int i = 0; i < 4; i++)
            {
                var target = active.Motor.Position + Vector3.up * .01f + Vector3.forward * .16f;
                Look(target, new Vector3(2.3f, .65f, 1.03f));
                await Frame();
                await Image($"Crawl-stroke-{i + 1}");
                await Task.Delay(240);
            }
            await Wait(() => effects.LeftHandEntries > left && effects.RightHandEntries > right, "A stroke entry was not observed from each arm.", 3f);
            for (int i = 0; i < 3; i++)
            {
                var target = active.Motor.Position - Vector3.forward * .35f - Vector3.up * .25f;
                Look(target, new Vector3(2.35f, -.47f, -.38f));
                await Frame();
                await Image($"Underwater-crawl-{i + 1}");
                await Task.Delay(87);
            }
            active.GetComponent<SwimmerStamina>().RequestSprint(true);
            for (int i = 0; i < 3; i++)
            {
                var target = active.Motor.Position + Vector3.up * .02f - Vector3.forward * .4f;
                Look(target, new Vector3(2.7f, .74f, .83f));
                await Frame();
                await Image($"Sprint-wake-{i + 1}");
                await Task.Delay(130);
            }
            active.Motor.SetMoveInput(Vector2.down);
            await Task.Delay(96);
            Look(active.Motor.Position, new Vector3(2.3f, .60f, .94f));
            await Image("Sprint-reversal-turbulence");
            active.Motor.StopMotion();
            active.GetComponent<SwimmerStamina>().RequestSprint(false);
            Look(active.Motor.Position, new Vector3(1.0f, .57f, 1.8f));
            await Task.Delay(400);
            await Image("Eggbeater-tread");
            int surfaceBeforePause = effects.ActiveEffects, dropletsBeforePause = effects.BubbleCount;
            match.SetPaused(true);
            await Task.Delay(250);
            Require(effects.ActiveEffects == surfaceBeforePause && effects.BubbleCount == dropletsBeforePause, "Pause changed the existing visual effects.");
            match.SetPaused(false);
            effects.Quality = WaterEffectsQuality.Off;
            await Frame();
            Require(effects.ActiveEffects == 0 && effects.BubbleCount == 0, "Off did not clear the visual batch.");
            return new
            {
                Screen.width, Screen.height, api = SystemInfo.graphicsDeviceType.ToString(), results,
                leftEntries = effects.LeftHandEntries, rightEntries = effects.RightHandEntries, effects.WakeEmissions, effects.TreadEmissions,
                gentle, fast, pausePreservesEffects = true, offClearsEffects = true
            };
        }
        finally
        {
            if (match.IsPaused) match.SetPaused(false);
            effects.Quality = oldQuality;
            lensAnchor.SetPositionAndRotation(priorAnchorPosition, priorAnchorRotation);
            camera.SetView(-1);
            foreach (var player in players)
            {
                player.Motor.StopMotion();
                player.GetComponent<SwimmerStamina>().RequestSprint(false);
            }
            foreach (var brain in brains) if (brain != null) brain.enabled = true;
            selection.enabled = selectionWasEnabled;
        }
    }

    public static async Task<object> Replay()
    {
        Directory.CreateDirectory(Output);
        var match = One<PrototypeMatch>();
        var effects = One<WaterInteractionEffects>();
        var ball = One<BallController>();
        var replay = One<GoalReplayDirector>();
        var roster = One<PrototypeRoster>();
        var preferences = One<GamePreferences>();
        var values = (GamePreferences.Values)typeof(GamePreferences).GetField("values", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(preferences);
        var previousReplays = values.automaticGoalReplays;
        var previousQuality = effects.Quality;
        var introduction = One<PreMatchPresentation>();
        var brains = Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>().Concat(Object.FindObjectsByType<PrototypeGoalkeeper>())
            .Concat(Object.FindObjectsByType<SwimmerInput>()).Where(item => item.enabled).ToArray();
        if (introduction.Active) introduction.SkipAll();
        await Wait(() => match.IsPlaying && !match.AwaitingOpeningPossession, "Opening sprint did not resolve.", 18f);
        try
        {
            values.automaticGoalReplays = true;
            effects.Quality = WaterEffectsQuality.High;
            foreach (var brain in brains) brain.enabled = false;
            foreach (var athlete in roster.Members)
                athlete.Motor.ResetPose(new Vector3(athlete.Team == PrototypeTeam.Blue ? -8f : 8f, 0f, -10f + athlete.CapNumber * 2f), Quaternion.identity);
            var shooter = roster.FieldPlayers.First(p => p.Team == PrototypeTeam.Blue);
            var goal = shooter.GetComponent<PrototypeSwimmerAI>().TacticalTeam.AttackingGoal;
            Vector3 origin = goal.transform.position - goal.transform.forward * 5f + goal.transform.right * .55f;
            float line = One<PoolBounds>().Waterline;
            shooter.Motor.ResetPose(origin, Quaternion.LookRotation(goal.transform.forward));
            Vector3 drop = origin - goal.transform.right * 1.2f + goal.transform.forward * .8f;
            drop.y = line + .6f;
            ball.ResetForRestart(drop);
            int contacts = effects.WaterImpacts;
            var beginFlight = typeof(BallController).GetMethod("BeginFlight", BindingFlags.Instance | BindingFlags.NonPublic);
            beginFlight.Invoke(ball, new object[] { drop, Vector3.down * 2.6f });
            await Wait(() => effects.WaterImpacts > contacts, "Replay fixture did not produce a real physics contact.");
            Vector3 contact = effects.LastWaterImpactPoint;
            ball.ResetForRestart(origin);
            Require(shooter.Interaction.TryCollect(), "Could not claim the ball for the physical goal.");
            await Task.Delay(520);
            Require(shooter.Interaction.TryShoot(22f, ShotType.Normal), "Could not submit the physical shot.");
            await Wait(() => replay.Phase == GoalPresentationPhase.Celebration, "The physical shot did not score.");
            int score = match.BlueScore;
            float clock = match.Clock.Remaining;

            int sideline = 0, endline = 0;
            await Wait(() => replay.Phase == GoalPresentationPhase.Broadcast && effects.PlayingRecordedContacts, "Sideline replay did not begin.");
            while (replay.Phase == GoalPresentationPhase.Broadcast)
            {
                if (effects.VisibleBallImpacts > 0 && sideline++ == 0) await Image("Replay-sideline-water");
                await Frame();
            }
            await Wait(() => replay.Phase == GoalPresentationPhase.BehindGoal && effects.PlayingRecordedContacts, "Goal-line replay did not begin.");
            while (replay.Phase == GoalPresentationPhase.BehindGoal && endline == 0)
            {
                if (effects.VisibleBallImpacts > 0) { endline++; await Image("Replay-goal-line-water"); }
                else await Frame();
            }
            Require(sideline > 0 && endline > 0, "The recorded water impact was not drawn from both replay cameras.");
            Require(match.BlueScore == score && match.Clock.Remaining == clock, "Replay changed live score or time.");
            Require(effects.WaterImpacts == contacts + 1 && effects.LastWaterImpactPoint == contact, "Replay generated a new live physical impact.");
            replay.SkipAll();
            await Frame();
            Require(!effects.PlayingRecordedContacts && effects.VisibleBallImpacts == 0, "Replay skip left stale water effects.");
            return new { renderedSidelineFrames = sideline, renderedGoalLineFrames = endline, contact, score, skipCleared = true };
        }
        finally
        {
            values.automaticGoalReplays = previousReplays;
            effects.Quality = previousQuality;
            foreach (var brain in brains) if (brain != null) brain.enabled = true;
        }
    }
}
