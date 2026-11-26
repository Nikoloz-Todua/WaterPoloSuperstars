using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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
        private const string Menu = "Tools/Water Polo/Verification/Milestone 30B/";
        private const string Pending = "WaterPolo.M30B.Preview";
        private enum Scenario { Carry, Passing, Underwater, Drop, Skip, Defense, KeeperLeft, KeeperRight, KeeperHigh }

        static Milestone30BPreview() => EditorApplication.playModeStateChanged += PlayChanged;

        [MenuItem(Menu + "Possession and Throwing", false, 410)]
        private static void Carry() => Launch(Scenario.Carry);
        [MenuItem(Menu + "Passing and Switching", false, 411)]
        private static void Passing() => Launch(Scenario.Passing);
        [MenuItem(Menu + "Underwater Freestyle", false, 412)]
        private static void Underwater() => Launch(Scenario.Underwater);
        [MenuItem(Menu + "Ordinary Ball Drop", false, 413)]
        private static void Drop() => Launch(Scenario.Drop);
        [MenuItem(Menu + "Skip Shot", false, 414)]
        private static void Skip() => Launch(Scenario.Skip);
        [MenuItem(Menu + "Center Defense and Shot Block", false, 415)]
        private static void Defense() => Launch(Scenario.Defense);
        [MenuItem(Menu + "Keeper Save Left", false, 416)]
        private static void KeeperLeft() => Launch(Scenario.KeeperLeft);
        [MenuItem(Menu + "Keeper Save Right", false, 417)]
        private static void KeeperRight() => Launch(Scenario.KeeperRight);
        [MenuItem(Menu + "Keeper Save High", false, 418)]
        private static void KeeperHigh() => Launch(Scenario.KeeperHigh);

        private static void Launch(Scenario scenario)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            {
                Debug.LogWarning("Stop Play Mode before starting a Milestone 30B preview.");
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
            var match = Object.FindAnyObjectByType<PrototypeMatch>();
            var roster = Object.FindAnyObjectByType<PrototypeRoster>();
            var ball = Object.FindAnyObjectByType<BallController>();
            var selection = Object.FindAnyObjectByType<PlayerSelection>();
            var camera = Object.FindAnyObjectByType<ArenaDeveloperCamera>();
            var intro = Object.FindAnyObjectByType<PreMatchPresentation>();
            if (match == null || roster == null || ball == null || selection == null || camera == null)
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
                    ai.enabled = scenario == Scenario.Defense && athlete.Team != shooter.Team;
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
            if (scenario == Scenario.Passing)
                receiver.Motor.ResetPose(origin + forward * 4.6f + right * .43f, Quaternion.LookRotation(-forward));

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
            if (scenario == Scenario.Passing || scenario == Scenario.Defense)
                camera.SetView(-1);
            else if (keeperScenario)
                Frame(camera, keeper.Identity.Motor.Position + Vector3.up * .35f, forward, right, false);
            else Frame(camera, origin + forward * .42f, forward, right, scenario == Scenario.Underwater);
            if (scenario == Scenario.Underwater || scenario == Scenario.Carry)
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
            Vector3 prior = motor.transform.position;
            void Follow()
            {
                if (!EditorApplication.isPlaying || camera == null || motor == null || camera.ViewIndex != 2)
                {
                    EditorApplication.update -= Follow;
                    return;
                }
                Vector3 position = motor.transform.position;
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
