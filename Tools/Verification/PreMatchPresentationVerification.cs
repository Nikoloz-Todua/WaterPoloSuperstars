using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Hub;
using System.Reflection;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

public static class PreMatchPresentationVerification
{
    private const string Output = "Logs/PreMatchPresentation/";
    private const string Overlay = "MatchSafeArea/PreMatchPresentation";
    private static T One<T>() where T : Object => Object.FindAnyObjectByType<T>();

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static async Task Await(Func<bool> predicate, string message, float timeout = 15)
    {
        double deadline = EditorApplication.timeSinceStartup + timeout;
        while (!predicate())
        {
            Check(EditorApplication.timeSinceStartup < deadline, message);
            await Task.Delay(14);
        }
    }

    private static async Task Shot(string name)
    {
        ScreenCapture.CaptureScreenshot(Output + name + ".png");
        int frame = Time.frameCount;
        await Await(() => Time.frameCount > frame + 2, "Screenshot did not render");
    }

    private static object Save(string name, object data)
    {
        File.WriteAllText(Output + name + ".json", JsonConvert.SerializeObject(data, Formatting.Indented));
        return data;
    }

    public static object Prepare()
    {
        Check(!Application.isPlaying, "Prepare outside Play Mode");
        Directory.CreateDirectory(Output);
        if (SessionState.GetInt("Prematch.Input", -1) < 0)
        {
            SessionState.SetInt("Prematch.Input", (int)InputSystem.settings.editorInputBehaviorInPlayMode);
            SessionState.SetInt("Prematch.Focus", (int)InputSystem.settings.backgroundBehavior);
        }
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/OlympicMatchArena.unity");
        return new { directEntry = EditorSceneManager.playModeStartScene.name };
    }

    public static object Restore()
    {
        if (SessionState.GetInt("Prematch.Input", -1) >= 0)
        {
            InputSystem.settings.editorInputBehaviorInPlayMode = (InputSettings.EditorInputBehaviorInPlayMode)SessionState.GetInt("Prematch.Input", -1);
            InputSystem.settings.backgroundBehavior = (InputSettings.BackgroundBehavior)SessionState.GetInt("Prematch.Focus", -1);
            SessionState.EraseInt("Prematch.Input");
            SessionState.EraseInt("Prematch.Focus");
        }
        return new { bootEntry = EditorApplication.ExecuteMenuItem("Tools/Water Polo/Recovery/Use Boot as Editor Play Entry") };
    }

    private static void RouteInputToGameView()
    {
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
    }

    private static Vector2 Point(string path)
    {
        var rect = (RectTransform)One<Canvas>().transform.Find(path);
        Check(rect != null && rect.gameObject.activeInHierarchy, "Visible input target " + path);
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
        Check(hits.Any() && hits[0].gameObject.transform.IsChildOf(rect), "Raycast reaches " + path + " (first hit " + (hits.FirstOrDefault().gameObject?.name ?? "none") + ")");
        return point;
    }

    private static async Task Click(Mouse mouse, string path)
    {
        int frame = Time.frameCount;
        await Await(() => Time.frameCount >= frame + 3, "Layout for " + path);
        var point = Point(path);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
        await Task.Delay(55);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point, buttons = 1 });
        await Task.Delay(65);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
        await Task.Delay(70);
    }

    private static async Task WaitForSprint(PrototypeMatch match, PreMatchPresentation intro, MatchCameraView view)
    {
        await Await(() => match.IsPlaying && !match.AwaitingOpeningPossession, "Opening sprint did not deliver ordinary possession");
        var roster = One<PrototypeRoster>();
        Check(!intro.Active && intro.CastCount == 0 && GameObject.Find("Prematch visual cast") == null, "Ceremony visual actors released");
        Check(match.SprintOff.Starts == 1 && match.SprintOff.Acquisitions == 1, "Exactly one opening sprint and pickup");
        Check(match.GameplayView.View == view && !match.GameplayView.CinematicOverride, "Requested gameplay camera restored");
        Check(roster.Members.Count == 14 && roster.Members.Select(p => p.PlayerId).Distinct().Count() == 14, "Exactly fourteen unique live athletes");
        Check(!One<PlayerSelection>().InputLocked, "Live selection unlocked");
        Check(One<BallController>().Owner != null, "Physical ball has a valid opening owner");
    }

    public static async Task<object> Full()
    {
        RouteInputToGameView();
        var match = One<PrototypeMatch>();
        var intro = One<PreMatchPresentation>();
        match.GameplayView.SetView(MatchCameraView.Lengthwise);
        var view = match.GameplayView.View;
        var mouse = InputSystem.AddDevice<Mouse>();
        try
        {
            var durations = new[] { .12f, 2.7f, 1.1f, 1.4f, 1f, 1.2f, 1.9f, .75f };
            var roster = One<PrototypeRoster>();
            var poses = roster.Members.Select(p => p.Motor.Position).ToArray();
            float remaining = match.Clock.Remaining;
            var seen = new List<string>();
            for (var stage = PreMatchStage.ArenaIntro; stage < PreMatchStage.Complete; stage++)
            {
                await Await(() => intro.Stage == stage && intro.StageElapsed >= durations[(int)stage], "Missed fresh stage " + stage);
                Check(match.IsPreMatchPresentation && match.Clock.Remaining == remaining, "Ceremony holds match clock");
                Check(One<PlayerSelection>().InputLocked && One<BallController>().Owner == null, "No gameplay selection or possession");
                Check(roster.Members.Select(p => p.Motor.Position).SequenceEqual(poses), "Live athlete positions untouched");
                var cast = match.gameObject.scene.GetRootGameObjects().SingleOrDefault(root => root.name == "Prematch visual cast");
                Check(intro.CastCount == 14 && cast != null && cast.GetComponentsInChildren<Collider>(true).Length == 0, "Fourteen isolated visual actors");
                await Shot($"{(int)stage + 1:00}-{stage}");
                seen.Add(stage.ToString());
                if (stage != PreMatchStage.TeamWalkout) continue;

                await Click(mouse, Overlay + "/IntroPause");
                Check(match.IsPaused && One<MatchMenuController>().Page == MatchMenuPage.Pause, "Intro pause opens the actual match menu");
                float pausedAt = intro.StageElapsed;
                var camera = match.GameplayView.CaptureCameraPose();
                await Task.Delay(460);
                Check(intro.StageElapsed == pausedAt && match.GameplayView.transform.position == camera.position, "Pause holds both timeline and camera");
                await Shot("09-IntroPaused");
                await Click(mouse, "MenuOverlay/MenuSafeArea/PausePanel/Resume");
                Check(!match.IsPaused && intro.Stage == stage, "Resume continues the same ceremony stage");
            }
            await Await(() => match.IsPeriodSprint, "Natural transition to opening sprint");
            await Shot("10-OpeningSprint");
            await WaitForSprint(match, intro, view);
            await Shot("11-LiveMatch");
            return Save("Full-PASS", new { stages = seen, pauseThroughPointer = true, camera = view.ToString(), sprintStarts = match.SprintOff.Starts, pickups = match.SprintOff.Acquisitions, actors = roster.Members.Count, width = Screen.width, height = Screen.height });
        }
        finally { InputSystem.RemoveDevice(mouse); }
    }

    public static async Task<object> Controls()
    {
        RouteInputToGameView();
        var match = One<PrototypeMatch>();
        var intro = One<PreMatchPresentation>();
        match.GameplayView.SetView(MatchCameraView.SidelineBroadcast);
        var touch = InputSystem.AddDevice<Touchscreen>();
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var pad = InputSystem.AddDevice<Gamepad>();
        var mouse = InputSystem.AddDevice<Mouse>();
        try
        {
            await Await(() => intro.Active && intro.StageElapsed > .25f, "Fresh presentation for input test");
            var previous = intro.Stage;
            var point = Point(Overlay);
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = 7, phase = UnityEngine.InputSystem.TouchPhase.Began, position = point });
            await Task.Delay(85);
            InputSystem.QueueStateEvent(touch, new TouchState { touchId = 7, phase = UnityEngine.InputSystem.TouchPhase.Ended, position = point });
            await Await(() => intro.Stage == previous + 1, "One simulated touch advances exactly one stage", 1);
            await Task.Delay(280);
            previous = intro.Stage;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
            await Await(() => intro.Stage == previous + 1, "Keyboard Enter advances one stage", 1);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            await Task.Delay(280);
            previous = intro.Stage;
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.South));
            await Await(() => intro.Stage == previous + 1, "Gamepad South advances one stage", 1);
            await Task.Delay(330);
            Check(intro.Stage == previous + 1, "Held confirm does not advance a second stage");
            InputSystem.QueueStateEvent(pad, new GamepadState());
            await Click(mouse, Overlay + "/SkipIntro");
            Check(!intro.Active && match.IsPeriodSprint, "Skip button enters Q1 from the middle of the ceremony");
            await WaitForSprint(match, intro, MatchCameraView.SidelineBroadcast);
            return Save("Controls-PASS", new { touch = true, enter = true, gamepad = true, heldConfirm = true, skipButton = true, camera = match.GameplayView.View.ToString() });
        }
        finally
        {
            InputSystem.RemoveDevice(mouse);
            InputSystem.RemoveDevice(pad);
            InputSystem.RemoveDevice(keyboard);
            InputSystem.RemoveDevice(touch);
        }
    }

    public static async Task<object> GamepadSkip()
    {
        RouteInputToGameView();
        var match = One<PrototypeMatch>();
        var intro = One<PreMatchPresentation>();
        var view = match.GameplayView.View;
        var keyboard = InputSystem.AddDevice<Keyboard>();
        var pad = InputSystem.AddDevice<Gamepad>();
        try
        {
            await Await(() => intro.Active && intro.StageElapsed > .25f, "Fresh intro from the direct arena menu");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F6, Key.F8));
            await Task.Delay(220);
            Check(intro.Active && match.GameplayView.View == view && match.GameplayView.CinematicOverride, "Gameplay and inspection toggles do not steal the ceremony camera");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.East));
            await Await(() => match.IsPeriodSprint && !intro.Active, "Gamepad East immediately skips to the sprint", 2);
            InputSystem.QueueStateEvent(pad, new GamepadState());
            await WaitForSprint(match, intro, view);
            return Save("GamepadSkip-PASS", new { directEditorEntry = true, fullGamepadSkip = true, f6f8Protected = true, cameraRestored = true });
        }
        finally
        {
            InputSystem.RemoveDevice(pad);
            InputSystem.RemoveDevice(keyboard);
        }
    }

    public static object StopDuringIntro()
    {
        var intro = One<PreMatchPresentation>();
        Check(intro != null && intro.Active && intro.CastCount == 14, "Stop exercise needs an active presentation");
        EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
        return Save("StopPlay-PASS", new { stage = intro.Stage.ToString(), scheduled = true });
    }
    private sealed class MemoryProfile : IProfileRepository
    {
        private string json;
        public LocalProfile Load() => json == null ? null : LocalProfileCodec.FromJson(json);
        public void Save(LocalProfile profile) => json = JsonUtility.ToJson(profile);
    }

    private static string ProfileHash()
    {
        string path = HubApplication.SavePath;
        if (!File.Exists(path)) return "absent";
        using var sha = System.Security.Cryptography.SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
    }

    public static object Boot()
    {
        Directory.CreateDirectory(Output);
        File.WriteAllText(Output + "ProfileBefore.txt", ProfileHash());
        return new { boot = EditorApplication.ExecuteMenuItem("Tools/Water Polo/Play Hub Entry") };
    }

    private static string ButtonPath(Transform target)
    {
        var path = new Stack<string>();
        for (var node = target; node != null && node.GetComponent<Canvas>() == null; node = node.parent)
            path.Push(node.name);
        return string.Join("/", path);
    }

    private static Button Named(string name) =>
        Object.FindObjectsByType<Button>().Single(button => button.name == name);

    private static async Task OpenOfficialMatch(Mouse mouse)
    {
        await Task.Delay(300);
        await Click(mouse, ButtonPath(One<HubView>().home.play.transform));
        await Click(mouse, ButtonPath(Named("Mode_division_1").transform));
        await Click(mouse, ButtonPath(Named("LaunchMode").transform));
        await Await(() => One<PreMatchPresentation>() != null && One<PreMatchPresentation>().Active, "Official division launches the production ceremony");
        var match = One<PrototypeMatch>();
        Check(match.gameObject.scene.name == "OlympicMatchArena" && One<HubMatchBridge>() != null, "Canonical hub destination");
        var roster = One<PrototypeRoster>();
        var catalog = HubApplication.Instance.Progression.Catalog;
        var list = One<Canvas>().transform.Find(Overlay + "/StartingLineups/BlueLineup/Athletes").GetComponent<TMPro.TMP_Text>().text;
        Check(roster.Members.Where(p => p.Team == PrototypeTeam.Blue).All(p => catalog.Player(p.CollectionDefinitionId) is { } data && list.Contains(data.name)), "Roster graphic contains assigned collection athlete names");
    }

    private static async Task GoalReplay(PrototypeMatch match, Mouse mouse)
    {
        var roster = One<PrototypeRoster>();
        var computers = Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>().Concat(Object.FindObjectsByType<PrototypeGoalkeeper>()).ToArray();
        var active = computers.Select(brain => brain.enabled).ToArray();
        var preferences = One<GamePreferences>();
        var field = typeof(GamePreferences).GetField("values", BindingFlags.Instance | BindingFlags.NonPublic);
        var values = (GamePreferences.Values)field.GetValue(preferences);
        bool originalReplays = values.automaticGoalReplays;
        values.automaticGoalReplays = true;
        try
        {
            foreach (var brain in computers) brain.enabled = false;
            foreach (var player in roster.Members)
                player.Motor.ResetPose(new Vector3(player.Team == PrototypeTeam.Blue ? -8f : 8f, 0, -8 + player.CapNumber), Quaternion.identity);
            var shooter = roster.FieldPlayers.First(p => p.Team == PrototypeTeam.Blue);
            var goal = shooter.GetComponent<PrototypeSwimmerAI>().TacticalTeam.AttackingGoal;
            shooter.Motor.ResetPose(goal.transform.position - goal.transform.forward * 5f + goal.transform.right * .7f, Quaternion.LookRotation(goal.transform.forward));
            var ball = One<BallController>();
            ball.ResetForRestart(shooter.Motor.Position);
            Check(shooter.Interaction.TryCollect(), "Fixture obtains physical shot possession");
            await Task.Delay(1900);
            Check(shooter.Interaction.TryShoot(22f, ShotType.Normal), "Ball released against the actual goal mouth");
            var replay = match.GoalPresentation;
            await Await(() => replay.Phase == GoalPresentationPhase.Celebration, "Celebration follows one official goal");
            float clock = match.Clock.Remaining;
            var view = match.GameplayView.View;
            string scorerId = string.IsNullOrEmpty(shooter.CollectionDefinitionId) ? shooter.PlayerId : shooter.CollectionDefinitionId;
            Check(replay.CurrentGoal.ScorerId == scorerId && match.BlueScore == 1, "Goal registered once with the physical shooter");
            await Await(() => replay.Phase == GoalPresentationPhase.Broadcast, "Existing first replay angle");
            await Shot("15-RecordedGoalRegression");
            await Await(() => replay.Phase == GoalPresentationPhase.BehindGoal, "Existing second replay angle", 10);
            Check(match.Clock.Remaining == clock && match.BlueScore == 1, "Replay does not move match clock or duplicate goals");
            await Click(mouse, "MatchSafeArea/GoalPresentation/SkipAll");
            await Await(() => match.IsPlaying && match.CentreSignals == 1, "Recorded goal resumes through the actual centre pass");
            Check(match.GameplayView.View == view && !match.GameplayView.CinematicOverride, "Restored camera after existing replay");
        }
        finally
        {
            values.automaticGoalReplays = originalReplays;
            for (int i = 0; i < computers.Length; i++) if (computers[i] != null) computers[i].enabled = active[i];
        }
    }

    public static async Task<object> Hub()
    {
        RouteInputToGameView();
        await Await(() => One<HubView>() != null && One<HubView>().Service != null, "Boot initializes MainLobby");
        var app = HubApplication.Instance;
        var original = app.Progression;
        var profile = new HubProgression(original.Catalog, new MemoryProfile());
        var mouse = InputSystem.AddDevice<Mouse>();
        app.UseVerificationProfile(profile);
        try
        {
            const string menu = "MenuOverlay/MenuSafeArea/";
            const string lobby = "Lobby";
            var originalView = One<HubView>();
            app.OpenLobby();
            await Await(() => One<HubView>() != null && One<HubView>() != originalView, "Load isolated profile into the lobby");
            int coins = profile.Snapshot.coins;

            await OpenOfficialMatch(mouse);
            var first = One<PrototypeMatch>();
            var intro = One<PreMatchPresentation>();
            await Click(mouse, Overlay + "/IntroPause");
            await Click(mouse, menu + "PausePanel/Restart");
            await Click(mouse, menu + "ConfirmationPanel/Cancel");
            Check(intro.Active && first.IsPaused, "Cancel returns to paused intro");
            string firstSession = profile.Snapshot.activeMatch.id;
            await Click(mouse, menu + "PausePanel/Restart");
            await Click(mouse, menu + "ConfirmationPanel/Confirm");
            Check(first.IsPeriodSprint && !intro.Active && intro.CastCount == 0 && !first.GameplayView.CinematicOverride, "Confirmed restart cleans the ceremony and jumps to Q1");
            Check(profile.Snapshot.activeMatch.id != firstSession && profile.Snapshot.played == 0, "No reward on restart");
            await Shot("12-RestartFromIntro");
            await WaitForSprint(first, intro, first.GameplayView.View);
            await Click(mouse, "MatchSafeArea/PauseButton");
            await Click(mouse, menu + "PausePanel/Exit");
            await Click(mouse, menu + "ConfirmationPanel/Confirm");
            await Await(() => One<HubView>() != null && One<HubView>().Page == lobby, "Return to lobby");

            await OpenOfficialMatch(mouse);
            await Await(() => One<PreMatchPresentation>().Stage == PreMatchStage.TeamWalkout, "Exit during active walkout");
            await Click(mouse, Overlay + "/IntroPause");
            await Click(mouse, menu + "PausePanel/Exit");
            await Click(mouse, menu + "ConfirmationPanel/Confirm");
            await Await(() => One<HubView>() != null && One<HubView>().Page == lobby, "Exit ceremony returns directly to lobby");
            Check(One<PreMatchPresentation>() == null && GameObject.Find("Prematch visual cast") == null && Time.timeScale == 1f && !AudioListener.pause, "Exit releases all presentation state");
            Check(profile.Snapshot.activeMatch == null && profile.Snapshot.coins == coins && profile.Snapshot.played == 0, "Abandon has no reward");
            await Shot("13-LobbyAfterIntroExit");

            await OpenOfficialMatch(mouse);
            var match = One<PrototypeMatch>();
            intro = One<PreMatchPresentation>();
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Backspace));
                await Await(() => match.IsPeriodSprint, "Backspace skips the ceremony to Q1", 2);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            }
            finally { InputSystem.RemoveDevice(keyboard); }
            await WaitForSprint(match, intro, match.GameplayView.View);
            await GoalReplay(match, mouse);

            for (int quarter = 1; quarter <= 4; quarter++)
            {
                await Await(() => match.IsPlaying && !match.AwaitingOpeningPossession, "Quarter opening acquisition");
                Check(match.SprintOff.Starts == quarter && match.SprintOff.Acquisitions == quarter && !intro.Active, "One sprint per quarter and no later intros");
                Check(match.EndsSwitched == (quarter >= 3), "Halftime reverses ends");
                var ball = One<BallController>();
                ball.ResetForRestart(Vector3.zero);
                match.ShotClock.ResetPossession();
                match.AdvanceLiveTime(match.Clock.Remaining);
                if (quarter < 4) One<MatchMenuController>().Continue();
            }

            Check(match.Phase == MatchPhase.FullTime && profile.Snapshot.wins == 1 && profile.Snapshot.played == 1, "One official four-period win");
            int balance = profile.Snapshot.coins;
            var bridge = One<HubMatchBridge>();
            Check(bridge.TrySaveResult() && profile.Snapshot.coins == balance, "Result retry cannot duplicate the reward");
            One<MatchMenuController>().Continue();
            await Await(() => One<HubView>() != null && One<HubView>().Page == "Result", "Official result page");
            await Shot("16-OfficialResult");
            await Click(mouse, ButtonPath(Named("Continue").transform));
            await Await(() => One<HubView>().Page == lobby, "Return to official lobby");
            Check(ProfileHash() == File.ReadAllText(Output + "ProfileBefore.txt"), "Real progression profile hash unchanged");
            return Save("Hub-PASS", new { officialLaunch = true, rosterNames = true, restart = true, exitDuringWalkout = true, keyboardSkip = true, goalBothAngles = true, centreRestart = true, fourPhysicalSprintAcquisitions = true, proofUsesAcceleratedClocks = true, rewardDeduplication = true, profileHashUnchanged = true });
        }
        finally
        {
            InputSystem.RemoveDevice(mouse);
            if (One<PrototypeMatch>() != null)
            {
                One<PrototypeMatch>().SetPaused(false);
                app.ReturnToLobby(true);
                await Await(() => One<HubView>() != null, "Release verification match");
            }
            app.UseVerificationProfile(original);
            app.OpenLobby();
        }
    }

}





