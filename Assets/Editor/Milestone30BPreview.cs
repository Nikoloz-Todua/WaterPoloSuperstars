using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ToduStudios.WaterPolo.Hub;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace ToduStudios.WaterPolo.Editor
{
    [InitializeOnLoad]
    public static class Milestone30BPreview
    {
        private const string Lab = "Tools/Water Polo/Test Lab/";
        private const string Pending = "WaterPolo.M30B.Preview";
        private const string DeckPreview = "WaterPolo.DeckPreview";
        private enum Scenario
        {
            Carry, Passing, Underwater, Drop, Skip, Defense, KeeperLeft, KeeperRight, KeeperHigh,
            Match, FreeSwim, FreeSwimUnderwater, LongPassAway, PowerShot, QuickShot, OrdinaryFoul, DirectionalPassing,
            HeadOn, Shoulder, SprintContact, ContestedPickup, IntelligentSupport,
            MovingReceiver, LateralReceiving, HighReceiving, MissedPass,
            FreestyleSide, FreestyleRear, Eggbeater, RefereeWalk, KeeperNeutral, KeeperCatch, KeeperMiss,
            SidelineFreedom, MouthFreedom, KeeperBehind, PartialLine, WideMiss, HighMiss, KeeperExit,
            PostRebound, GoalFlow, LateGoal, KeeperConceding, SidelineExit, DefenderExit
        }

        static Milestone30BPreview() => EditorApplication.playModeStateChanged += PlayChanged;

        [MenuItem("Tools/Water Polo/Play/Play Full Game", false, 1)]
        private static void PlayFullGame() => Milestone12HubSetup.PlayHub();
        [MenuItem("Tools/Water Polo/Play/Play Match", false, 2)]
        private static void PlayMatch() => Launch(Scenario.Match);
        [MenuItem(Lab + "Ceremony and Walking/Arena Walkout and Pennants", false, 410)]
        private static void Ceremony() => Launch(Scenario.Match);
        [MenuItem(Lab + "Ceremony and Walking/Natural Athlete Walk", false, 411)]
        private static void NaturalWalk() => LaunchDeckPreview(false);
        [MenuItem(Lab + "Ceremony and Walking/Standing and Foot Placement", false, 412)]
        private static void NaturalStand() => LaunchDeckPreview(true);
        [MenuItem(Lab + "Ceremony and Walking/Referee Walking", false, 413)]
        private static void RefereeWalk() => Launch(Scenario.RefereeWalk);
        [MenuItem(Lab + "Athlete and Equipment/Play Athlete Preview", false, 411)]
        private static void Equipment() => ProductionAthleteSetup.PlayPreview();
        [MenuItem(Lab + "Athlete and Equipment/Competition Swimwear Inspection", false, 412)]
        private static void CompetitionBriefs() => ProductionAthleteSetup.PlayPreview();
        [MenuItem(Lab + "Athlete and Equipment/Underwater Swimwear Inspection", false, 413)]
        private static void UnderwaterBriefs() => Launch(Scenario.FreestyleSide);
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
        [MenuItem(Lab + "Team Tactics/Intelligent Team Support", false, 410)]
        private static void IntelligentSupport() => Launch(Scenario.IntelligentSupport);
        [MenuItem(Lab + "Shooting and Passing/Stationary Dry Pass", false, 417)]
        private static void StationaryDryPass() => Launch(Scenario.Passing);
        [MenuItem(Lab + "Shooting and Passing/Moving Receiver", false, 418)]
        private static void MovingReceiver() => Launch(Scenario.MovingReceiver);
        [MenuItem(Lab + "Shooting and Passing/Lateral Receiving", false, 419)]
        private static void LateralReceiving() => Launch(Scenario.LateralReceiving);
        [MenuItem(Lab + "Shooting and Passing/High Receiving", false, 420)]
        private static void HighReceiving() => Launch(Scenario.HighReceiving);
        [MenuItem(Lab + "Shooting and Passing/Missed Pass and Recovery", false, 421)]
        private static void MissedPass() => Launch(Scenario.MissedPass);
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
        [MenuItem(Lab + "Swimming and Floating/Freestyle Side View", false, 414)]
        private static void FreestyleSide() => Launch(Scenario.FreestyleSide);
        [MenuItem(Lab + "Swimming and Floating/Freestyle Rear View", false, 415)]
        private static void FreestyleRear() => Launch(Scenario.FreestyleRear);
        [MenuItem(Lab + "Swimming and Floating/Eggbeater Comparison", false, 416)]
        private static void Eggbeater() => Launch(Scenario.Eggbeater);
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
        [MenuItem(Lab + "Goalkeeper and Penalties/Goalkeeper Neutral and Ball Tracking", false, 419)]
        private static void KeeperNeutral() => Launch(Scenario.KeeperNeutral);
        [MenuItem(Lab + "Goalkeeper and Penalties/Ordinary Goalkeeper Catch", false, 420)]
        private static void KeeperCatch() => Launch(Scenario.KeeperCatch);
        [MenuItem(Lab + "Goalkeeper and Penalties/Goalkeeper Miss and Recovery", false, 421)]
        private static void KeeperMiss() => Launch(Scenario.KeeperMiss);
        [MenuItem(Lab + "Boundaries and Ball Physics/Sideline Movement Freedom")]
        private static void SidelineFreedom() => Launch(Scenario.SidelineFreedom);
        [MenuItem(Lab + "Boundaries and Ball Physics/Goal-Line and Goal-Mouth Movement")]
        private static void MouthFreedom() => Launch(Scenario.MouthFreedom);
        [MenuItem(Lab + "Boundaries and Ball Physics/Goalkeeper Loose Ball Behind Keeper")]
        private static void KeeperBehind() => Launch(Scenario.KeeperBehind);
        [MenuItem(Lab + "Boundaries and Ball Physics/Ball Partially Across Goal Line")]
        private static void PartialLine() => Launch(Scenario.PartialLine);
        [MenuItem(Lab + "Boundaries and Ball Physics/Wide Shot - Visible Trajectory")]
        private static void WideMiss() => Launch(Scenario.WideMiss);
        [MenuItem(Lab + "Boundaries and Ball Physics/High Miss - Visible Trajectory")]
        private static void HighMiss() => Launch(Scenario.HighMiss);
        [MenuItem(Lab + "Boundaries and Ball Physics/Goalkeeper Deflection Out of Play")]
        private static void KeeperExit() => Launch(Scenario.KeeperExit);
        [MenuItem(Lab + "Boundaries and Ball Physics/Post Rebound - Live Ball")]
        private static void PostRebound() => Launch(Scenario.PostRebound);
        [MenuItem(Lab + "Boundaries and Ball Physics/Sideline Exit - Throw In")]
        private static void SidelineExit() => Launch(Scenario.SidelineExit);
        [MenuItem(Lab + "Boundaries and Ball Physics/Defender Deflection Out of Play")]
        private static void DefenderExit() => Launch(Scenario.DefenderExit);
        [MenuItem(Lab + "Match Continuity/Ordinary Foul - Legal Repositioning")]
        private static void FoulContinuity() => Launch(Scenario.OrdinaryFoul);
        [MenuItem(Lab + "Match Continuity/Goal Reactions and Centre Restart")]
        private static void GoalFlow() => Launch(Scenario.GoalFlow);
        [MenuItem(Lab + "Match Continuity/Goal Celebration Preview")]
        private static void LateGoal() => Launch(Scenario.LateGoal);
        [MenuItem(Lab + "Match Continuity/Goalkeeper Conceding Reaction")]
        private static void KeeperConceding() => Launch(Scenario.KeeperConceding);

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
            int deckPreview = SessionState.GetInt(DeckPreview, -1);
            if (deckPreview >= 0 && state == PlayModeStateChange.EnteredPlayMode)
            {
                Object.FindAnyObjectByType<AthletePreviewCamera>()?.ShowProfessionalDeck(deckPreview == 1);
                EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
                SessionState.EraseInt(DeckPreview);
            }
            int scenario = SessionState.GetInt(Pending, -1);
            if (scenario < 0) return;
            if (state == PlayModeStateChange.EnteredPlayMode) _ = SetUpLogged((Scenario)scenario);
            if (state != PlayModeStateChange.EnteredEditMode) return;
            SessionState.EraseInt(Pending);
            Milestone12HubSetup.SetBootEntry();
        }

        private static void LaunchDeckPreview(bool standing)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            SessionState.SetInt(DeckPreview, standing ? 1 : 0);
            ProductionAthleteSetup.PlayPreview();
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
                scenario == Scenario.KeeperHigh || scenario == Scenario.KeeperNeutral ||
                scenario == Scenario.KeeperCatch || scenario == Scenario.KeeperMiss ||
                scenario == Scenario.KeeperBehind || scenario == Scenario.KeeperConceding;
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
            if (keeperScenario)
            {
                float depth = new SerializedObject(keeper).FindProperty("stationDepth").floatValue;
                keeper.Identity.Motor.ResetPose(new Vector3(goal.x, origin.y, goal.z) - forward * depth,
                    Quaternion.LookRotation(-forward));
            }
            selection.Select(shooter);
            if (scenario >= Scenario.SidelineFreedom)
            {
                await StageContinuity(scenario, match, ball, shooter, receiver, keeper, camera);
                return;
            }

            if (scenario == Scenario.IntelligentSupport)
            {
                StageTeamSupport(roster, ball, shooter, origin, forward, right);
                camera.SetView(-1);
                match.GameplayView.SnapToTarget();
                return;
            }
            if (scenario >= Scenario.HeadOn && scenario <= Scenario.ContestedPickup)
            {
                StageContact(scenario, roster, ball, selection, camera, origin, forward, right);
                return;
            }
            bool swimView = scenario >= Scenario.FreestyleSide && scenario <= Scenario.Eggbeater;
            if (scenario == Scenario.FreeSwim || scenario == Scenario.FreeSwimUnderwater || swimView)
            {
                ball.ResetForRestart(forward * 10f);
                Frame(camera, origin - forward * .34f, forward, right, scenario == Scenario.FreeSwimUnderwater || swimView);
                if (swimView)
                {
                    var anchor = camera.viewpoints[2];
                    Vector3 focus = origin - forward * .48f + Vector3.down * (scenario == Scenario.Eggbeater ? .37f : .21f);
                    anchor.position = focus + (scenario == Scenario.FreestyleRear
                        ? -forward * 2.0f + right * .34f : right * 2.22f - forward * .17f) - Vector3.up * .28f;
                    anchor.LookAt(focus);
                    camera.GetComponent<Camera>().fieldOfView = 48f;
                }
                FollowInspection(camera, () => shooter != null ? shooter.Motor.Position : (Vector3?)null);
                return;
            }
            if (scenario == Scenario.RefereeWalk)
            {
                ball.ResetForRestart(origin + forward * .85f);
                if (!shooter.Interaction.TryCollect())
                    throw new InvalidOperationException("The referee fixture could not establish possession.");
                var official = Object.FindObjectsByType<PoolsideReferee>().OrderBy(p => p.transform.position.x)
                    .First(p => p.GetComponentInChildren<RefereeVisual>()?.UsesProfessional == true);
                Vector3 focus = official.transform.position + Vector3.up * .83f;
                var anchor = camera.viewpoints[2];
                anchor.position = focus + Vector3.right * Mathf.Sign(-official.transform.position.x) * 2.60f +
                    Vector3.forward * .88f + Vector3.up * .36f;
                anchor.LookAt(focus);
                camera.SetView(2);
                camera.GetComponent<Camera>().fieldOfView = 48f;
                FollowInspection(camera, () => official != null ? official.transform.position : (Vector3?)null);
                return;
            }
            if (scenario == Scenario.Passing)
                receiver.Motor.ResetPose(origin + forward * 4.6f + right * .43f, Quaternion.LookRotation(-forward));
            if (scenario == Scenario.LongPassAway)
                receiver.Motor.ResetPose(origin + forward * 8.2f + right * .25f, facing);
            if (scenario >= Scenario.MovingReceiver && scenario <= Scenario.MissedPass)
            {
                Vector3 receiverFacing = scenario == Scenario.MovingReceiver ? forward :
                    scenario == Scenario.LateralReceiving ? right : -forward;
                Vector3 receiverPoint = origin + forward * 7.7f + (scenario == Scenario.MissedPass ? right * 6f : Vector3.zero);
                receiver.Motor.ResetPose(receiverPoint, Quaternion.LookRotation(receiverFacing));
            }

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
                receiver.Motor.ResetPose(origin - right * 3.2f + forward * .4f, facing);
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
            else if (scenario == Scenario.Passing || scenario == Scenario.LongPassAway ||
                scenario >= Scenario.MovingReceiver && scenario <= Scenario.MissedPass)
            {
                Frame(camera, receiver.Motor.Position + Vector3.up * .21f, -forward, right, false);
                camera.SetView(-1);
            }
            else if (scenario == Scenario.Defense)
                camera.SetView(-1);
            else if (keeperScenario)
            {
                Vector3 focus = keeper.Identity.Motor.Position + Vector3.up * .38f;
                var anchor = camera.viewpoints[2];
                anchor.position = focus - forward * 2.7f + right * 1.35f + Vector3.up * .53f;
                anchor.LookAt(focus);
                camera.GetComponent<Camera>().fieldOfView = 49f;
                camera.SetView(2);
            }
            else Frame(camera, origin + forward * .42f, forward, right, scenario == Scenario.Underwater);
            if (scenario == Scenario.Underwater || scenario == Scenario.Carry ||
                scenario == Scenario.PowerShot || scenario == Scenario.QuickShot)
                FollowInspection(camera, () => shooter != null ? shooter.Motor.Position : (Vector3?)null);
            if (scenario == Scenario.MovingReceiver || scenario == Scenario.LateralReceiving)
                _ = RunMovingPassOnT(ball, shooter, receiver, scenario == Scenario.MovingReceiver ? forward : right);

            if (scenario == Scenario.KeeperNeutral) return;
            if (keeperScenario)
            {
                await Awaitable.WaitForSecondsAsync(.55f);
                float side = scenario == Scenario.KeeperMiss ? keeper.DefendedGoal.Width * .5f - ball.Radius - .08f :
                    scenario == Scenario.KeeperCatch ? -.31f : scenario == Scenario.KeeperLeft ? -.71f :
                    scenario == Scenario.KeeperRight ? .63f : .025f;
                Vector3 start = goal - forward * (scenario == Scenario.KeeperMiss ? 4.5f : 6f) + right * side;
                start.y = ball.SurfaceHeight + (scenario == Scenario.KeeperHigh ? .93f : .43f);
                if (scenario == Scenario.KeeperCatch)
                {
                    Vector3 end = start + forward * 4.35f;
                    typeof(BallController).GetMethod("LaunchPass", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(ball, new object[] { shooter.Interaction, start, end, .50f, null, PassDelivery.Dry,
                            .08f, .8f, BallActionSource.HumanInput });
                }
                else
                {
                    // Contract fixture: prescribe the initial trajectory only, before the first physics step.
                    bool elevated = scenario == Scenario.KeeperHigh || scenario == Scenario.KeeperMiss;
                    float speed = scenario == Scenario.KeeperMiss ? 23f : scenario == Scenario.KeeperHigh ? 11.5f : 12f;
                    float height = elevated ? keeper.DefendedGoal.Height - ball.Radius - .16f : .43f;
                    Vector3 end = goal + right * side + Vector3.up * height;
                    float flight = Vector3.Dot(end - start, forward) / speed;
                    typeof(BallController).GetMethod("LaunchShot", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(ball, new object[] { shooter.Interaction, forward, speed,
                            elevated ? ShotType.High : ShotType.Normal, start, BallActionSource.HumanInput });
                    typeof(BallController).GetMethod("BeginFlight", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(ball, new object[] { start, BallTrajectory.Velocity(start, end, flight) });
                }
                return;
            }

            if (scenario != Scenario.Skip) return;
            await Awaitable.WaitForSecondsAsync(.55f);
            if (!ball.TryShoot(shooter.Interaction, 20f, ShotType.Skip))
                throw new InvalidOperationException("The preview shot was rejected.");
        }

        private static async Task StageContinuity(Scenario scenario, PrototypeMatch match, BallController ball,
            SwimmerIdentity shooter, SwimmerIdentity receiver, PrototypeGoalkeeper keeper, ArenaDeveloperCamera camera)
        {
            var mouth = keeper.DefendedGoal;
            var goal = mouth.transform.position;
            var forward = mouth.transform.forward;
            var right = mouth.transform.right;
            var pool = Object.FindAnyObjectByType<PoolBounds>();
            var body = ball.GetComponent<Rigidbody>();
            ball.ResetForRestart(pool.transform.position - forward * 4f);

            if (scenario is Scenario.SidelineFreedom or Scenario.MouthFreedom)
            {
                Vector3 point = scenario == Scenario.MouthFreedom ? goal - forward * 2.15f :
                    pool.transform.position + right * (pool.Width * .5f - 1.85f) + forward * 1.3f;
                shooter.Motor.ResetPose(point, Quaternion.LookRotation(forward));
                if (scenario == Scenario.MouthFreedom)
                    keeper.Identity.Motor.ResetPose(goal - forward * 2.35f + right * 3.3f, Quaternion.LookRotation(-forward));
                Frame(camera, point, forward, right, false);
                var movementView = camera.viewpoints[2];
                if (scenario == Scenario.MouthFreedom)
                    StageMouthView(movementView, goal, forward, right);
                else
                {
                    movementView.position = point - right * 3.6f - forward * 1.65f + Vector3.up * 2.1f;
                    movementView.LookAt(point + right * .65f + Vector3.up * .2f);
                    FollowInspection(camera, () => shooter != null ? shooter.Motor.Position : (Vector3?)null);
                }
                return;
            }
            if (scenario == Scenario.KeeperBehind)
            {
                Frame(camera, goal - forward * .62f, forward, right, false);
                StageMouthView(camera.viewpoints[2], goal, forward, right);
                await Awaitable.WaitForSecondsAsync(.44f);
                Vector3 point = goal + forward * .06f + right * .34f;
                point.y = ball.SurfaceHeight;
                ball.ResetForRestart(point);
                body.position = point;
                return;
            }

            bool goalScene = scenario is Scenario.GoalFlow or Scenario.LateGoal or Scenario.KeeperConceding;
            shooter.Motor.ResetPose(goal - forward * 4.65f, Quaternion.LookRotation(forward));
            if (goalScene && scenario != Scenario.KeeperConceding)
                receiver.Motor.ResetPose(goal - forward * 5.55f + right * 2.1f, Quaternion.LookRotation(forward));
            if (scenario == Scenario.LateGoal)
            {
                while (match.Clock.Quarter < match.Clock.Quarters) match.Clock.NextQuarter();
                match.Clock.Tick(match.Clock.RealDuration - 9f);
            }
            Frame(camera, goal - forward * .65f, forward, right, false);
            var anchor = camera.viewpoints[2];
            anchor.position = goal - forward * 3.7f + right * 6.0f + Vector3.up * 3.05f;
            anchor.LookAt(goal + forward * .60f + Vector3.up * .56f);
            camera.GetComponent<Camera>().fieldOfView = 53f;
            if (scenario == Scenario.PartialLine)
            {
                keeper.Identity.Motor.ResetPose(goal - forward * 2.15f - right * 3.25f, Quaternion.LookRotation(-forward));
                StageMouthView(anchor, goal, forward, right);
            }
            await Awaitable.WaitForSecondsAsync(.58f);

            float side = scenario switch
            {
                Scenario.WideMiss => 2.63f,
                Scenario.PostRebound => (mouth.Width + mouth.PostThickness) * .5f,
                Scenario.KeeperConceding => mouth.Width * .5f - ball.Radius - .08f,
                _ => 0f
            };
            Vector3 crossing = goal + right * side;
            crossing.y += scenario == Scenario.HighMiss ? mouth.Height + .66f : .45f;
            Vector3 start = goal - forward * 4.5f + right * side;
            start.y += scenario == Scenario.HighMiss ? 1.15f : .48f;
            float speed = scenario == Scenario.KeeperConceding ? 23f : 12f;
            if (scenario == Scenario.PartialLine)
            {
                start = goal + forward * .045f;
                start.y = ball.SurfaceHeight + .005f;
                LaunchFixtureShot(ball, shooter, forward, start, Vector3.zero, 1f, ShotType.Normal);
                while (EditorApplication.isPlaying && match.IsPlaying && body != null)
                {
                    if (Keyboard.current?.tKey.wasPressedThisFrame == true)
                    {
                        body.linearVelocity = forward * 3f;
                        break;
                    }
                    await Awaitable.NextFrameAsync();
                }
                return;
            }
            if (scenario == Scenario.KeeperExit)
            {
                start = goal - forward * 1.05f + right * .95f + Vector3.up * .58f;
                LaunchFixtureShot(ball, shooter, forward, start, forward * speed, speed, ShotType.Normal);
                if (!ball.TryDeflectContact(keeper.Identity.Interaction, start, forward * 9.5f + right * 14f + Vector3.up * .25f))
                    throw new InvalidOperationException("Could not stage the goalkeeper's physical deflection.");
                return;
            }
            if (scenario == Scenario.DefenderExit)
            {
                start = goal - forward * 1.08f + right * 2.65f + Vector3.up * .66f;
                LaunchFixtureShot(ball, shooter, forward, start, forward * speed, speed, ShotType.Normal);
                var defender = Object.FindAnyObjectByType<PrototypeRoster>().FieldPlayers.First(p => p.Team != shooter.Team);
                typeof(BallController).GetMethod("NotePlayerContact", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(ball, new object[] { defender, false });
                return;
            }
            if (scenario == Scenario.SidelineExit)
            {
                start = pool.transform.position + right * (pool.Width * .5f - 1.8f) + Vector3.up * .80f;
                LaunchFixtureShot(ball, shooter, right, start, right * 10f + Vector3.up * .5f, 10f, ShotType.Normal);
                Frame(camera, start, forward, -right, false);
                return;
            }
            LaunchFixtureShot(ball, shooter, forward, start, BallTrajectory.Velocity(start, crossing, 4.5f / speed),
                speed, scenario == Scenario.HighMiss ? ShotType.High : ShotType.Normal);
        }

        private static void StageMouthView(Transform anchor, Vector3 goal, Vector3 forward, Vector3 right)
        {
            anchor.position = goal - forward * 2.5f + right * 2.45f + Vector3.up * 1.85f;
            anchor.LookAt(goal - forward * .30f + Vector3.up * .28f);
        }

        private static void LaunchFixtureShot(BallController ball, SwimmerIdentity shooter, Vector3 forward,
            Vector3 start, Vector3 velocity, float speed, ShotType shot)
        {
            const BindingFlags methods = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(BallController).GetMethod("LaunchShot", methods)
                .Invoke(ball, new object[] { shooter.Interaction, forward, speed, shot, start, BallActionSource.Verification });
            typeof(BallController).GetMethod("BeginFlight", methods).Invoke(ball, new object[] { start, velocity });
        }

        private static async Task RunMovingPassOnT(BallController ball, SwimmerIdentity passer,
            SwimmerIdentity receiver, Vector3 direction)
        {
            while (EditorApplication.isPlaying && ball != null && ball.Owner == passer.Interaction)
            {
                if (Keyboard.current?.tKey.wasPressedThisFrame == true)
                {
                    receiver.Motor.SetMoveInput(new Vector2(direction.x, direction.z));
                    await Awaitable.WaitForSecondsAsync(.27f);
                    if (EditorApplication.isPlaying && ball != null && ball.Owner == passer.Interaction)
                    {
                        receiver.Motor.SetMoveInput(Vector2.zero);
                        passer.GetComponent<SwimmerPassing>().TryPass();
                    }
                    return;
                }
                await Awaitable.NextFrameAsync();
            }
        }

        private static void StageTeamSupport(PrototypeRoster roster, BallController ball, SwimmerIdentity carrier,
            Vector3 origin, Vector3 forward, Vector3 right)
        {
            origin += right * 2.3f + forward * 1.8f;
            carrier.Motor.ResetPose(origin, Quaternion.LookRotation(forward));
            foreach (var player in roster.FieldPlayers)
            {
                var ai = player.GetComponent<PrototypeSwimmerAI>();
                ai.enabled = true;
                if (player == carrier) continue;
                bool friendly = player.Team == carrier.Team;
                Vector3 point = friendly ? ai.TacticalTeam.Support(player, carrier) :
                    origin + forward * (6f + player.CapNumber % 3) - right * (2f + player.CapNumber % 6);
                if (!friendly && player.FieldPosition == FieldPositionRole.LeftFlat)
                    point = origin + forward * 2.7f - right * 3.1f;
                if (!friendly && player.FieldPosition == FieldPositionRole.LeftWing)
                    point = origin + forward * 4.9f - right * 5.4f;
                if (friendly && player.FieldPosition == FieldPositionRole.LeftFlat)
                    point = origin + forward * 5.5f - right * 6.2f;
                player.Motor.ResetPose(point, Quaternion.LookRotation(friendly ? forward : -forward));
            }
            ball.ResetForRestart(origin + forward * .85f);
            if (!carrier.Interaction.TryCollect())
                throw new InvalidOperationException("The team support fixture could not establish possession.");
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

        private static void FollowInspection(ArenaDeveloperCamera camera, Func<Vector3?> followPoint)
        {
            var anchor = camera.viewpoints[2];
            Vector3 prior = followPoint() ?? anchor.position;
            void Follow()
            {
                Vector3? point = EditorApplication.isPlaying ? followPoint() : null;
                if (!point.HasValue || camera == null || camera.ViewIndex != 2)
                {
                    EditorApplication.update -= Follow;
                    return;
                }
                Vector3 position = point.Value;
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
