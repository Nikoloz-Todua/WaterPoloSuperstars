using System;
using System.Linq;
using System.Threading.Tasks;
using ToduStudios.WaterPolo.Hub;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace ToduStudios.WaterPolo.Editor
{
    [InitializeOnLoad]
    public static class ExclusionTestMenu
    {
        private const string Menu="Tools/Water Polo/Verification/Exclusion Test/";
        private const string Pending="WaterPolo.ExclusionDemo.Pending";
        private const string PriorEntry="WaterPolo.ExclusionDemo.PriorEntry";
        private const string OwnsEditorPlay="WaterPolo.ExclusionDemo.OwnsEditorPlay";
        private enum Scenario { Blue, White, Early, Replacement, Halftime }
        private static bool running;

        static ExclusionTestMenu() => EditorApplication.playModeStateChanged+=PlayModeChanged;

        [MenuItem(Menu+"Blue - Full Countdown",false,410)] private static void Blue() => Launch(Scenario.Blue);
        [MenuItem(Menu+"White - Full Countdown",false,411)] private static void White() => Launch(Scenario.White);
        [MenuItem(Menu+"Blue - Early Return",false,412)] private static void Early() => Launch(Scenario.Early);
        [MenuItem(Menu+"White - Third Personal and Replacement",false,413)] private static void Replacement() => Launch(Scenario.Replacement);
        [MenuItem(Menu+"Blue - After Halftime",false,414)] private static void Halftime() => Launch(Scenario.Halftime);

        private static void Launch(Scenario scenario)
        {
            if (running || EditorApplication.isCompiling) return;
            if (EditorApplication.isPlaying)
            {
                if (SceneManager.GetActiveScene().path != OlympicArenaSetup.ScenePath || HubApplication.Instance != null)
                {
                    Debug.LogWarning("Exclusion Test uses isolated OlympicMatchArena play. Stop Play Mode and choose the command again.");
                    return;
                }
                _=RunLogged(scenario);
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var arena=AssetDatabase.LoadAssetAtPath<SceneAsset>(OlympicArenaSetup.ScenePath);
            if (arena == null) { Debug.LogError("OlympicMatchArena scene is missing."); return; }
            SessionState.SetString(PriorEntry,AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            SessionState.SetBool(OwnsEditorPlay,true);
            SessionState.SetInt(Pending,(int)scenario);
            EditorSceneManager.playModeStartScene=arena;
            EditorApplication.isPlaying=true;
        }

        private static void PlayModeChanged(PlayModeStateChange state)
        {
            if (state==PlayModeStateChange.EnteredPlayMode)
            {
                int scenario=SessionState.GetInt(Pending,-1);
                SessionState.EraseInt(Pending);
                if (scenario>=0) _=RunLogged((Scenario)scenario);
            }
            if (state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(OwnsEditorPlay,false))
            {
                string path=SessionState.GetString(PriorEntry,"");
                EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(path)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                SessionState.EraseBool(OwnsEditorPlay);
                SessionState.EraseString(PriorEntry);
                SessionState.EraseInt(Pending);
            }
        }

        private static async Task RunLogged(Scenario scenario)
        {
            running=true;
            try { await Run(scenario); }
            catch (OperationCanceledException) { }
            catch (Exception exception) { if(EditorApplication.isPlaying) Debug.LogException(exception); }
            finally { running=false; }
        }

        private static async Task WaitFor(PrototypeMatch match, Func<bool> predicate, string action)
        {
            float deadline=match.PresentationTime+34f;
            while (true)
            {
                if (!EditorApplication.isPlaying || match==null) throw new OperationCanceledException();
                if (predicate()) return;
                if (match.PresentationTime>=deadline) throw new InvalidOperationException("Exclusion test timed out waiting for "+action+".");
                await Task.Delay(20);
            }
        }

        private static async Task Run(Scenario scenario)
        {
            if (HubApplication.Instance != null) throw new InvalidOperationException("Exclusion Test cannot run in a hub session.");
            var match=Object.FindAnyObjectByType<PrototypeMatch>();
            var roster=Object.FindAnyObjectByType<PrototypeRoster>();
            var ball=Object.FindAnyObjectByType<BallController>();
            if (match==null || roster==null || ball==null) throw new InvalidOperationException("Open the production arena.");
            EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
            match.RestartMatch();
            await WaitFor(match,()=>match.IsPlaying&&!match.AwaitingOpeningPossession,"opening sprint possession");

            if (scenario==Scenario.Halftime)
            {
                for(int quarter=1;quarter<=2;quarter++)
                {
                    match.ShotClock.ResetPossession();
                    match.AdvanceLiveTime(match.Clock.Remaining+.01f);
                    match.ContinueMatch();
                    await WaitFor(match,()=>match.IsPlaying&&!match.AwaitingOpeningPossession,"next sprint possession");
                }
                if (!match.EndsSwitched) throw new InvalidOperationException("The halftime reversal did not occur.");
            }

            var actors=Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>()
                .Concat(Object.FindObjectsByType<PrototypeGoalkeeper>()).ToArray();
            var initial=actors.Select(actor=>actor.enabled).ToArray();
            PrototypeTeam team=scenario==Scenario.White || scenario==Scenario.Replacement ? PrototypeTeam.White : PrototypeTeam.Blue;
            var offender=roster.FieldPlayers.First(p=>p.Team==team);
            var opposition=roster.FieldPlayers.Where(p=>p.Team!=team).Take(2).ToArray();
            var defended=Object.FindObjectsByType<PrototypeTeamTactics>().Single(t=>t.Team==team).DefendedGoal;
            try
            {
                // The scripted demonstration holds tactical AI while the actual clocks, entry path and UI advance.
                foreach (var actor in actors) actor.enabled=false;
                int initialCap=offender.CapNumber;
                int incidents=scenario==Scenario.Replacement?3:1;
                for(int incident=1;incident<=incidents;incident++)
                {
                    await WaitFor(match,()=>match.IsPlaying,"live play before the next incident");
                    var edge=defended.transform.position-defended.transform.forward*2.5f+Vector3.right*7f;
                    offender.Motor.ResetPose(edge,Quaternion.identity);
                    opposition[1].Motor.ResetPose(edge+Vector3.left*.6f,Quaternion.identity);
                    var ballLane=defended.transform.position-defended.transform.forward*5.9f+Vector3.right*6.8f;
                    opposition[0].Motor.ResetPose(ballLane,Quaternion.LookRotation(defended.transform.forward));
                    ball.ResetForRestart(opposition[0].Motor.Position);
                    if (!opposition[0].Interaction.TryCollect())
                        throw new InvalidOperationException("Could not establish possession for incident "+incident+".");
                    if (!match.Rules.ReportContact(offender,opposition[1],IllegalContact.Hold))
                        throw new InvalidOperationException("The off-ball exclusion was not accepted for incident "+incident+".");
                    if (offender.ExclusionRemaining != 18f || offender.EligibleForPlay || match.Rules.ActiveFields(team)!=5)
                        throw new InvalidOperationException("Expected an 18-second exclusion and five active field players.");
                    await WaitFor(match,()=>!match.IsPaused&&(match.IsPlaying||match.Phase==MatchPhase.FreeThrowReady),"legal free throw");
                    if (!match.IsPlaying && !match.Rules.Taker.GetComponent<SwimmerPassing>().TryPass())
                        throw new InvalidOperationException("The free throw could not release.");
                    await WaitFor(match,()=>!offender.AwaitingExit&&match.IsPlaying,"arrival at the re-entry area");
                    if (scenario==Scenario.Early)
                    {
                        var teammate=roster.FieldPlayers.First(p=>p.Team==team&&p!=offender&&p.EligibleForPlay);
                        ball.ResetForRestart(teammate.Motor.Position);
                        if (!teammate.Interaction.TryCollect())
                            throw new InvalidOperationException("The excluded player's team could not regain possession.");
                    }
                    else if (incident<incidents) match.AdvanceLiveTime(offender.ExclusionRemaining+.01f);

                    await WaitFor(match,()=>offender.EligibleForPlay,"legal entry");
                    bool replacement=scenario==Scenario.Replacement&&incident==incidents;
                    if (match.Rules.ActiveFields(team)!=6 || (offender.CapNumber!=initialCap)!=replacement)
                        throw new InvalidOperationException("The returned roster does not match the scenario.");
                }
                Debug.Log($"Exclusion Test passed. {team} returned to six field players. Scenario {scenario}.");
            }
            finally
            {
                for(int i=0;i<actors.Length;i++) if(actors[i]!=null) actors[i].enabled=initial[i];
            }
        }
    }
}
