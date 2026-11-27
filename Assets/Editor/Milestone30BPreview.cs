using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ToduStudios.WaterPolo.Hub;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ToduStudios.WaterPolo.Editor
{
    [InitializeOnLoad]
    public static class Milestone30BPreview
    {
        private const string Lab = "Tools/Water Polo/Test Lab/";
        private const string Pending = "WaterPolo.M30B.Preview";
        private enum Scenario
        {
            Carry, Passing, Underwater, Drop, Skip, Defense, KeeperLeft, KeeperRight, KeeperHigh,
            Match, FreeSwim, FreeSwimUnderwater, LongPassAway, PowerShot, QuickShot, OrdinaryFoul, DirectionalPassing, HeadOn, Shoulder, SprintContact, ContestedPickup
        }

        static Milestone30BPreview() => EditorApplication.playModeStateChanged += PlayChanged;

        [MenuItem("Tools/Water Polo/Play/Play Full Game", false, 1)]
        private static void PlayFullGame() => Milestone12HubSetup.PlayHub();
        [MenuItem("Tools/Water Polo/Play/Play Match", false, 2)]
        private static void PlayMatch() => Launch(Scenario.Match);
        [MenuItem(Lab + "Ceremony and Walking/Arena Walkout and Pennants", false, 410)]
        private static void Ceremony() => Launch(Scenario.Match);
        [MenuItem(Lab + "Athlete and Equipment/Play Athlete Preview", false, 411)]
        private static void Equipment() => ProductionAthleteSetup.PlayPreview();
        [MenuItem(Lab + "Ball Control and Player Switching/Possession and Throwing", false, 410)]
        private static void Carry() => Launch(Scenario.Carry);
        [MenuItem(Lab + "Ball Control and Player Switching/Directional Aim and Held Rotation", false, 411)]
        private static void AimAndRotate() => Launch(Scenario.Carry);
        [MenuItem(Lab + "Shooting and Passing/Directional Passing", false, 410)]
        private static void DirectionalPassing() => Launch(Scenario.DirectionalPassing);
        [MenuItem(Lab + "Shooting and Passing/Passing and Switching", false, 411)]
        private static void Passing() => Launch(Scenario.Passing);
        [MenuItem(Lab + "Shooting and Passing/Long Pass to Receiver Facing Away", false, 412)]
        private static void LongPassAway() => Launch(Scenario.LongPassAway);
        [MenuItem(Lab + "Shooting and Passing/Full Power Shooting Elevation", false, 415)]
        private static void PowerShot() => Launch(Scenario.PowerShot);
        [MenuItem(Lab + "Shooting and Passing/Quick Shooting Elevation", false, 416)]
        private static void QuickShot() => Launch(Scenario.QuickShot);
        [MenuItem(Lab + "Referees and Match Rules/Ordinary Foul Repositioning", false, 410)]
        private static void OrdinaryFoul() => Launch(Scenario.OrdinaryFoul);
        [MenuItem(Lab + "Swimming and Floating/Underwater Ball Leading", false, 412)]
        private static void Underwater() => Launch(Scenario.Underwater);
        [MenuItem(Lab + "Swimming and Floating/Freestyle and Floating", false, 410)]
        private static void FreeSwim() => Launch(Scenario.FreeSwim);
        [MenuItem(Lab + "Swimming and Floating/Underwater Freestyle", false, 411)]
        private static void FreeSwimUnderwater() => Launch(Scenario.FreeSwimUnderwater);
        [MenuItem(Lab + "Swimming and Floating/Underwater Sprint Freestyle", false, 413)]
        private static void SprintFreestyle() => Launch(Scenario.FreeSwimUnderwater);
        [MenuItem(Lab + "Arena and Water/Ordinary Ball Drop", false, 413)]
        private static void Drop() => Launch(Scenario.Drop);
        [MenuItem(Lab + "Shooting and Passing/Skip Shot", false, 414)]
        private static void Skip() => Launch(Scenario.Skip);
        [MenuItem(Lab + "Player Contact and Defense/Centre Defense and Shot Block", false, 415)]
        private static void Defense() => Launch(Scenario.Defense);
        [MenuItem(Lab + "Player Contact and Defense/Head-On Centre Players", false, 410)]
        private static void HeadOn() => Launch(Scenario.HeadOn);
        [MenuItem(Lab + "Player Contact and Defense/Shoulder Slide", false, 411)]
        private static void Shoulder() => Launch(Scenario.Shoulder);
        [MenuItem(Lab + "Player Contact and Defense/Sprint into Stationary Defender", false, 412)]
        private static void SprintContact() => Launch(Scenario.SprintContact);
        [MenuItem(Lab + "Player Contact and Defense/Contested Loose Ball", false, 413)]
        private static void ContestedPickup() => Launch(Scenario.ContestedPickup);
        [MenuItem(Lab + "Goalkeeper and Penalties/Keeper Save Left", false, 416)]
        private static void KeeperLeft() => Launch(Scenario.KeeperLeft);
        [MenuItem(Lab + "Goalkeeper and Penalties/Keeper Save Right", false, 417)]
        private static void KeeperRight() => Launch(Scenario.KeeperRight);
        [MenuItem(Lab + "Goalkeeper and Penalties/Keeper Save High", false, 418)]
        private static void KeeperHigh() => Launch(Scenario.KeeperHigh);

        private static void Launch(Scenario scenario)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            {
                Debug.LogWarning("Stop Play Mode before starting a Water Polo Test Lab preview.");
                return;
            }
            OlympicArenaSetup.Open();
            SessionState.SetInt(Pending, (int)scenario);
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(OlympicArenaSetup.ScenePath);
            EditorApplication.isPlaying = true;
        }

        private static void PlayChanged(PlayModeStateChange state)
        {
            int scenario = SessionState.GetInt(Pending, -1);
            if (scenario < 0) return;
            if (state == PlayModeStateChange.EnteredPlayMode) _ = SetUpLogged((Scenario)scenario);
            if (state != PlayModeStateChange.EnteredEditMode) return;
            SessionState.EraseInt(Pending);
            Milestone12HubSetup.SetBootEntry();
        }

        private static async Task SetUpLogged(Scenario scenario)
        {
            try { await SetUp(scenario); }
            catch (OperationCanceledException) { }
            catch (Exception exception) { if (EditorApplication.isPlaying) Debug.LogException(exception); }
        }

        private static async Task SetUp(Scenario scenario)
        {
            if (scenario == Scenario.Match) return;
            var match = Object.FindAnyObjectByType<PrototypeMatch>();
            var roster = Object.FindAnyObjectByType<PrototypeRoster>();
            var ball = Object.FindAnyObjectByType<BallController>();
            var selection = Object.FindAnyObjectByType<PlayerSelection>();
            var camera = Object.FindAnyObjectByType<ArenaDeveloperCamera>();
            var intro = Object.FindAnyObjectByType<PreMatchPresentation>();
            if (match == null || roster == null || ball == null || selection == null || camera == null || HubMatchBridge.Active)
                throw new InvalidOperationException("The preview requires the saved production arena.");
            EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
            await Awaitable.NextFrameAsync();
            if (intro != null && intro.Active) intro.SkipAll();
            await WaitFor(() => match != null && match.IsPlaying && !match.AwaitingOpeningPossession, 25f);

            var shooter = roster.FieldPlayers.First(player => player.Team == selection.HumanTeam);
            var receiver = roster.FieldPlayers.First(player => player.Team == shooter.Team && player != shooter);
            var keeper = Object.FindObjectsByType<PrototypeGoalkeeper>()
                .First(player => player.DefendedGoal.ScoringTeam == shooter.Team);
            bool keeperScenario = scenario == Scenario.KeeperLeft || scenario == Scenario.KeeperRight ||
                scenario == Scenario.KeeperHigh;
            Vector3 goal = keeper.DefendedGoal.transform.position;
            Vector3 forward = keeper.DefendedGoal.transform.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 origin = Vector3.zero;
            if (scenario == Scenario.Skip || scenario == Scenario.Defense || keeperScenario)
                origin = goal - forward * (scenario == Scenario.Skip ? 8f : 6.3f);
            origin.y = shooter.Motor.Position.y;
            Quaternion facing = Quaternion.LookRotation(forward);

            foreach (var athlete in roster.Members)
            {
                if (athlete != shooter && athlete.TryGetComponent<PrototypeSwimmerAI>(out var ai))
                    ai.enabled = scenario == Scenario.OrdinaryFoul || scenario == Scenario.Defense && athlete.Team != shooter.Team;
                if (athlete.TryGetComponent<PrototypeGoalkeeper>(out var controller))
                    controller.enabled = keeperScenario && controller == keeper;
                if (athlete != shooter && athlete != keeper.Identity)
                    athlete.Motor.ResetPose(new Vector3(athlete.Team == shooter.Team ? -7.5f : 7.5f,
                        origin.y, -4f + athlete.CapNumber), Quaternion.identity);
            }
            if (scenario == Scenario.Defense)
            {
                var marker = roster.FieldPlayers.First(player => player.Team != shooter.Team &&
                    player.FieldPosition == FieldPositionRole.CentreBack);
                marker.Motor.ResetPose(origin + forward * 1.25f + right * .22f, Quaternion.LookRotation(-forward));
            }
            shooter.Motor.ResetPose(origin, facing);
            selection.Select(shooter);

            if (scenario >= Scenario.HeadOn)
            {
                StageContact(scenario, roster, ball, selection, camera, origin, forward, right);
                return;
            }
            if (scenario == Scenario.FreeSwim || scenario == Scenario.FreeSwimUnderwater)
            {
                ball.ResetForRestart(forward * 10f);
                Frame(camera, origin - forward * .34f, forward, right, scenario == Scenario.FreeSwimUnderwater);
                FollowAthlete(camera, shooter.Motor);
                return;
            }
            if (scenario == Scenario.Passing)
                receiver.Motor.ResetPose(origin + forward * 4.6f + right * .43f, Quaternion.LookRotation(-forward));
            if (scenario == Scenario.LongPassAway)
                receiver.Motor.ResetPose(origin + forward * 8.2f + right * .25f, facing);

            if (scenario == Scenario.Drop)
            {
                ball.ResetForRestart(origin + right * 2f);
                Vector3 point = ball.PhysicsPosition + Vector3.up;
                Frame(camera, ball.PhysicsPosition, -forward, right, false);
                await Awaitable.WaitForSecondsAsync(.65f);
                // Use the same authoritative fixture entry as the earlier ball physics checks.
                typeof(BallController).GetMethod("BeginFlight", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(ball, new object[] { point, Vector3.down * 1.6f });
                return;
            }

            ball.ResetForRestart(origin + forward * .90f);
            if (!shooter.Interaction.TryCollect())
                throw new InvalidOperationException("The preview carrier could not collect the staged ball.");
            if (scenario == Scenario.OrdinaryFoul)
            {
                var offender = roster.FieldPlayers.First(player => player.Team != shooter.Team);
                offender.Motor.ResetPose(origin + right * .9f, Quaternion.LookRotation(-right));
                if (!match.Rules.ReportContact(offender, shooter, IllegalContact.Push))
                    throw new InvalidOperationException("The ordinary foul fixture could not award the restart.");
                camera.SetView(-1);
                return;
            }
            if (scenario == Scenario.DirectionalPassing)
            {
                camera.SetView(-1);
                var view = match.GameplayView;
                view.SnapToTarget();
                Vector2 screenUp = view.ScreenToWorldMovement(Vector2.up);
                Vector2 screenRight = view.ScreenToWorldMovement(Vector2.right);
                Vector3 up = new Vector3(screenUp.x, 0f, screenUp.y).normalized;
                Vector3 across = new Vector3(screenRight.x, 0f, screenRight.y).normalized;
                shooter.Motor.ResetPose(origin, Quaternion.LookRotation(up));
                var outlets = roster.FieldPlayers.Where(player => player.Team == shooter.Team && player != shooter).Take(2).ToArray();
                outlets[0].Motor.ResetPose(origin - across * 4.6f, Quaternion.LookRotation(across));
                outlets[1].Motor.ResetPose(origin + across * 4.6f, Quaternion.LookRotation(-across));
            }
            else if (scenario == Scenario.Passing || scenario == Scenario.LongPassAway || scenario == Scenario.Defense)
                camera.SetView(-1);
            else if (keeperScenario)
                Frame(camera, keeper.Identity.Motor.Position + Vector3.up * .35f, forward, right, false);
            else Frame(camera, origin + forward * .42f, forward, right, scenario == Scenario.Underwater);
            if (scenario == Scenario.Underwater || scenario == Scenario.Carry ||
                scenario == Scenario.PowerShot || scenario == Scenario.QuickShot)
                FollowAthlete(camera, shooter.Motor);

            if (!keeperScenario && scenario != Scenario.Skip) return;
            await Awaitable.WaitForSecondsAsync(.55f);
            Vector3 aim = forward;
            if (keeperScenario)
            {
                float side = scenario == Scenario.KeeperLeft ? -.92f : scenario == Scenario.KeeperRight ? .92f : .04f;
                aim = Vector3.ProjectOnPlane(goal + right * side - shooter.Motor.Position, Vector3.up).normalized;
                shooter.Motor.ResetPose(shooter.Motor.Position, Quaternion.LookRotation(aim));
                await Awaitable.FixedUpdateAsync();
            }
            var shot = scenario == Scenario.Skip ? ShotType.Skip :
                scenario == Scenario.KeeperHigh ? ShotType.High : ShotType.Normal;
            if (!ball.TryShoot(shooter.Interaction, scenario == Scenario.Skip ? 20f : 12f, shot))
                throw new InvalidOperationException("The preview shot was rejected.");
        }

        private static void StageContact(Scenario scenario, PrototypeRoster roster, BallController ball,
            PlayerSelection selection, ArenaDeveloperCamera camera, Vector3 origin, Vector3 forward, Vector3 right)
        {
            var blue = roster.FieldPlayers.First(p => p.Team == selection.HumanTeam && p.FieldPosition == FieldPositionRole.CentreForward);
            var white = roster.FieldPlayers.First(p => p.Team != selection.HumanTeam && p.FieldPosition == FieldPositionRole.CentreBack);
            float offset = scenario == Scenario.Shoulder ? .52f : 0f;
            bool approach = scenario == Scenario.HeadOn || scenario == Scenario.ContestedPickup;
            ball.ResetForRestart(scenario == Scenario.ContestedPickup ? origin : origin + forward * 10f);
            blue.Motor.ResetPose(origin - forward * 1.85f + right * offset, Quaternion.LookRotation(forward));
            white.Motor.ResetPose(origin + forward * (approach ? 1.85f : 0f), Quaternion.LookRotation(-forward));
            selection.Select(blue);

            var anchor = camera.viewpoints[2];
            // Screen Up should follow the exact contact lane. A lateral camera offset
            // steers the selected swimmer into a glancing approach.
            anchor.position = origin - forward * 3.2f + Vector3.up * 4.4f;
            anchor.LookAt(origin + Vector3.up * .05f);
            camera.GetComponent<Camera>().fieldOfView = 42f;
            camera.SetView(2);
            if (!approach) return;
            var counter = new Vector2(-forward.x, -forward.z);
            bool started = false;
            void DriveOpponent()
            {
                if (!EditorApplication.isPlaying || white == null || blue == null || ball == null || !ball.Match.IsPlaying)
                {
                    EditorApplication.update -= DriveOpponent;
                    return;
                }
                started |= Vector3.Dot(blue.Motor.Velocity, forward) > .2f;
                white.Motor.SetMoveInput(started && ball.Owner == null ? counter : Vector2.zero);
            }
            EditorApplication.update += DriveOpponent;
        }

        private static void Frame(ArenaDeveloperCamera camera, Vector3 focus, Vector3 forward, Vector3 right, bool underwater)
        {
            Transform point = camera.viewpoints[2];
            point.position = focus + right * 2.15f + forward * .70f +
                Vector3.up * (underwater ? -.36f : .78f);
            point.LookAt(focus + Vector3.up * (underwater ? -.19f : .09f));
            camera.GetComponent<Camera>().fieldOfView = 48f;
            camera.SetView(2);
        }

        private static void FollowAthlete(ArenaDeveloperCamera camera, SwimmerMotor motor)
        {
            var anchor = camera.viewpoints[2];
            Vector3 prior = motor.Position;
            void Follow()
            {
                if (!EditorApplication.isPlaying || camera == null || motor == null || camera.ViewIndex != 2)
                {
                    EditorApplication.update -= Follow;
                    return;
                }
                Vector3 position = motor.Position;
                anchor.position += position - prior;
                prior = position;
            }
            EditorApplication.update += Follow;
        }

        private static async Task WaitFor(Func<bool> predicate, float seconds)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!predicate())
            {
                if (!EditorApplication.isPlaying) throw new OperationCanceledException();
                if (Time.realtimeSinceStartup > deadline)
                    throw new TimeoutException("The opening sprint did not produce live play.");
                await Awaitable.NextFrameAsync();
            }
        }
    }
}
