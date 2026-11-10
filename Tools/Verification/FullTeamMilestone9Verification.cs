using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using ToduStudios.WaterPolo;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class FullTeamMilestone9Verification
{
    public static object Setup()
    {
        Check(!Application.isPlaying,"Setup requires Edit Mode.");
        var original=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>().Members.Select(p=>p.PlayerId).ToArray();
        ToduStudios.WaterPolo.Editor.Milestone9PrototypeSetup.SetUp();
        var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();var scene=roster.gameObject.scene;
        int Count()=>scene.GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Transform>(true).Length);
        int count=Count();var ids=roster.Members.Select(p=>p.PlayerId).ToArray();
        Check(original.All(id=>ids.Contains(id)),"Existing athlete identities preserved.");
        Check(roster.Members.Count==14 && roster.FieldPlayers.Count==12 && ids.Distinct().Count()==14,"Fourteen distinct athletes, twelve fields.");
        foreach(var team in new[]{PrototypeTeam.Blue,PrototypeTeam.White})
        {
            var players=roster.Members.Where(p=>p.Team==team).ToArray();
            Check(players.Count(p=>p.IsFieldPlayer)==6 && players.Count(p=>!p.IsFieldPlayer)==1,"Six fields plus one keeper per team.");
            Check(players.Where(p=>p.IsFieldPlayer).Select(p=>p.FieldPosition).Distinct().Count()==6,"Six stable tactical roles.");
            Check(players.Where(p=>p.IsFieldPlayer).All(p=>team==PrototypeTeam.Blue?p.StartPosition.position.z<0f:p.StartPosition.position.z>0f),"Starts occupy own defensive halves.");
            for(int i=0;i<players.Length;i++)for(int j=i+1;j<players.Length;j++)Check(Vector3.Distance(players[i].StartPosition.position,players[j].StartPosition.position)>2.5f,"Separated starts including keeper.");
        }
        var match=UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();var clock=match.Clock;
        Check(clock.Quarters==4 && clock.RealDuration==80f && clock.DisplayRatio==6f,"Saved exact four-period 80/480 timing.");
        var shot=new SerializedObject(match.ShotClock);Check(shot.FindProperty("possessionSeconds").floatValue==28f && shot.FindProperty("clockRate").floatValue==1f,"Independent 28-second possession clock.");
        var camera=UnityEngine.Object.FindAnyObjectByType<GameplayCamera>();var data=new SerializedObject(camera);float distance=data.FindProperty("distance").floatValue;
        data.FindProperty("distance").floatValue=distance+0.25f;data.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        try
        {
            ToduStudios.WaterPolo.Editor.Milestone9PrototypeSetup.SetUp();
            Check(Count()==count && ids.SequenceEqual(roster.Members.Select(p=>p.PlayerId)),"Repeated setup has no duplicates or replaced identities.");
            Check(Mathf.Approximately(new SerializedObject(camera).FindProperty("distance").floatValue,distance+0.25f),"Repeat setup preserves camera tuning.");
            return new {passed=true,athletes=14,fieldsPerTeam=6,objects=count,periods=4,liveSeconds=80,displaySeconds=480,shotClock=28};
        }
        finally{data.Update();data.FindProperty("distance").floatValue=distance;data.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
    }
    private static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    public static async Task<object> Gameplay()
    {
        Check(Application.isPlaying,"Gameplay requires Play Mode.");
        var match=UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();var ball=UnityEngine.Object.FindAnyObjectByType<BallController>();var selection=UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
        var ai=UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>();var keepers=UnityEngine.Object.FindObjectsByType<PrototypeGoalkeeper>();var blue=roster.FieldPlayers.Where(p=>p.Team==PrototypeTeam.Blue).ToArray();var white=roster.FieldPlayers.First(p=>p.Team==PrototypeTeam.White);
        var teams=UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>();var keyboard=InputSystem.AddDevice<Keyboard>();var passed=new List<string>();
        void EnableAI(bool value){foreach(var a in ai)a.enabled=value;foreach(var k in keepers)k.enabled=value;}
        void Park()
        {foreach(var side in teams){int i=0;foreach(var p in roster.FieldPlayers.Where(p=>p.Team==side.Team))p.Motor.ResetPose(new Vector3(side.Team==PrototypeTeam.Blue?8.3f:-8.3f,0f,-11f+4f*i++),Quaternion.LookRotation(side.AttackDirection));}}
        async Task Claim(SwimmerIdentity player)
        {ball.ResetForRestart(player.Motor.Position+player.Motor.Facing*1.4f);Check(player.Interaction.TryCollect(),"Fixture uses legitimate ownership acquisition.");await Advance(0.08f);}
        void Boundary(){match.Clock.Tick(Mathf.Max(0f,match.Clock.Remaining-0.001f));match.AdvanceLiveTime(0.02f);}
        async Task AIShoot()
        {
            EnableAI(false);Park();var context=teams.Single(t=>t.Team==PrototypeTeam.White);var goal=context.AttackingGoal;
            Vector3 point=goal.transform.TransformPoint(new Vector3(0f,0f,-5f));point.y=0f;white.Motor.ResetPose(point,Quaternion.LookRotation(-goal.transform.forward));
            await Claim(white);var controller=white.GetComponent<PrototypeSwimmerAI>();int shots=controller.ShotsExecuted,score=match.WhiteScore;controller.enabled=true;
            await Until(()=>controller.ShotsExecuted>shots);await Until(()=>match.Phase==MatchPhase.GoalRestart);
            Check(match.WhiteScore==score+1,"White AI turns, attacks and scores physically at its current attacking end.");controller.enabled=false;await Until(()=>match.IsPlaying);
        }
        async Task Live(float seconds)
        {
            EnableAI(true);float start=Time.fixedTime;
            while(Time.fixedTime-start<seconds)
            {
                await Advance(0.1f);Check(roster.Members.Count(p=>p.Interaction.HasBall)<=1,"Exclusive possession with fourteen active athletes.");
                Check(selection.ActivePlayer.Team==PrototypeTeam.Blue && (selection.ActivePlayer.IsFieldPlayer || ball.Owner==selection.ActivePlayer.Interaction || !match.IsPlaying),"Valid Blue field/keeper control in live full-team play.");
                Check(roster.Members.All(p=>Mathf.Abs(p.Motor.Position.x)<=8.91f && Mathf.Abs(p.Motor.Position.z)<=14.6f && Mathf.Abs(p.Motor.Position.y)<0.001f && p.GetComponent<SwimmerStamina>().Value>=0f && p.GetComponent<SwimmerStamina>().Value<=1f),"Pool/waterline and stamina bounds for every athlete.");
                Check(Camera.main.GetComponent<GameplayCamera>().Target==selection.ActivePlayer.Motor,"Camera remains synchronized with active control.");
            }
        }
        try
        {
            EnableAI(false);match.RestartMatch();Park();selection.Select(blue[0]);blue[0].Motor.ResetPose(Vector3.zero,Quaternion.identity);ball.ResetForRestart(new Vector3(7f,ball.SurfaceHeight,12f));
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.LeftShift));await Advance(0.7f);
            Check(blue[0].Motor.Velocity.magnitude>5.5f && blue[0].GetComponent<SwimmerStamina>().Value<0.95f,"W/Shift movement and shared sprint drain remain functional.");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Advance(0.25f);passed.Add("Human movement/sprint/stamina");

            Park();blue[0].Motor.ResetPose(new Vector3(0f,0f,-2f),Quaternion.identity);blue[1].Motor.ResetPose(new Vector3(0f,0f,3f),Quaternion.identity);await Claim(blue[0]);
            int changes=match.ShotClock.TeamChanges;float before=match.ShotClock.Remaining;await Tap(keyboard,Key.B);
            Check(ball.Owner==null && ball.ReleaseKind==BallReleaseKind.Pass,"B launches a physical normal pass.");await Until(()=>ball.Owner==blue[1].Interaction);
            Check(selection.ActivePlayer==blue[1] && match.ShotClock.TeamChanges==changes && match.ShotClock.Remaining<before,"Normal reception auto-selects without resetting same-team time.");
            blue[1].Motor.ResetPose(Vector3.zero,Quaternion.Euler(0f,90f,0f));int sequence=ball.ShotSequence;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));await Advance(0.8f);
            Check(ball.Owner==blue[1].Interaction && blue[1].GetComponent<ShotCharge>().NormalizedCharge==1f,"Hold charges without early release.");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Advance(0.04f);Check(ball.Owner==null && ball.ShotSequence==sequence+1 && ball.Velocity.x>20f,"Release fires exactly one powered facing shot.");passed.Add("Physical B reception, unchanged possession clock and charged shooting");

            Park();blue[0].Motor.ResetPose(new Vector3(0f,0f,-3f),Quaternion.identity);blue[1].Motor.ResetPose(new Vector3(0f,0f,5f),Quaternion.identity);await Claim(blue[0]);await Tap(keyboard,Key.L);await Advance(0.5f);
            Check(ball.IsAirborne && ball.PhysicsPosition.y>ball.SurfaceHeight+1f,"L passes with real vertical flight.");
            blue[2].Motor.ResetPose(new Vector3(ball.PhysicsPosition.x,0f,ball.PhysicsPosition.z),Quaternion.identity);Check(!blue[2].Interaction.TryCollect(),"High airborne ball cannot be stolen by proximity.");
            blue[2].Motor.ResetPose(new Vector3(8f,0f,-3f),Quaternion.identity);await Until(()=>ball.Owner==blue[1].Interaction);
            Check(!ball.IsAirborne && selection.ActivePlayer==blue[1],"Aerial reception restores water physics and human control.");passed.Add("Lob height, high-pickup rejection and physical reception");

            Park();white.Motor.ResetPose(Vector3.zero,Quaternion.identity);blue[0].Motor.ResetPose(new Vector3(0f,0f,1.5f),Quaternion.Euler(0f,180f,0f));await Claim(white);selection.Select(blue[0]);await Tap(keyboard,Key.F);
            var stealing=blue[0].GetComponent<SwimmerSteal>();Check(stealing.LastResult==StealResult.Protected && ball.Owner==white.Interaction,"Protected failed F attempt preserves ownership.");
            await Advance(Mathf.Max(0f,stealing.NextAttemptAt-Time.time)+0.06f);await Tap(keyboard,Key.F);
            Check(stealing.LastResult==StealResult.Success && ball.Owner==blue[0].Interaction && selection.ActivePlayer==blue[0],"Legal F steal transfers one ownership/control.");passed.Add("F protection/cooldown and successful steal");

            await AIShoot();passed.Add("White AI physical goal before halftime");match.RestartMatch();await Live(10f);
            EnableAI(false);Boundary();match.ContinueMatch();Boundary();Check(match.Phase==MatchPhase.HalfTime,"Live full-team match reaches halftime correctly.");
            await Advance(0.1f);ScreenCapture.CaptureScreenshot("Logs/Milestone9Full-Halftime.png");await Advance(0.1f);match.ContinueMatch();await AIShoot();passed.Add("White AI physical goal after halftime");
            await Live(10f);Check(match.EndSwitchCount==1 && match.EndsSwitched,"Full-team second-half play retains correct ends.");
            ScreenCapture.CaptureScreenshot("Logs/Milestone9Full-Gameplay.png");await Advance(0.1f);
            passed.Add("Twenty simulated seconds of live full-team play across both halves");
            return new {passed,athletes=14,liveSeconds=20,AIPhysicalGoals=2};
        }
        finally{InputSystem.RemoveDevice(keyboard);match.RestartMatch();EnableAI(true);}
    }
    public static async Task<object> RolesAndPossession()
    {
        Check(Application.isPlaying,"Role/possession check requires Play Mode.");
        var match=UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();var ball=UnityEngine.Object.FindAnyObjectByType<BallController>();var selection=UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
        var ai=UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>();var keepers=UnityEngine.Object.FindObjectsByType<PrototypeGoalkeeper>();
        foreach(var a in ai)a.enabled=false;foreach(var k in keepers)k.enabled=false;match.RestartMatch();
        var teams=UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>().OrderBy(t=>t.Team).ToArray();var blue=roster.FieldPlayers.Where(p=>p.Team==PrototypeTeam.Blue).ToArray();var white=roster.FieldPlayers.First(p=>p.Team==PrototypeTeam.White);
        var keyboard=InputSystem.AddDevice<Keyboard>();
        try
        {
            var seen=new HashSet<SwimmerIdentity>();for(int i=0;i<6;i++){seen.Add(selection.ActivePlayer);await Tap(keyboard,Key.C);}
            Check(seen.Count==6 && seen.All(p=>p.IsFieldPlayer && p.Team==PrototypeTeam.Blue),"C input cycles all six Blue fields, excludes keepers/opponents.");
            foreach(var player in blue)
            {
                ball.ResetForRestart(player.Motor.Position+player.Motor.Facing*1.4f);Check(player.Interaction.TryCollect(),"Each Blue field acquires legitimately.");
                Check(selection.ActivePlayer==player && Camera.main.GetComponent<GameplayCamera>().Target==player.Motor,"Actual acquisition switches control/camera for every field identity.");
                Check(roster.Members.Count(p=>p.GetComponent<SwimmerInput>()!=null && p.GetComponent<SwimmerInput>().enabled)==1,"Exactly one human movement adapter.");
            }
            ball.ResetForRestart(blue[0].Motor.Position+blue[0].Motor.Facing*1.4f);Check(blue[0].Interaction.TryCollect(),"Supporting shape fixture.");
            var support=blue.Skip(1).Select(p=>teams[0].Support(p,blue[0])).ToArray();Separate(support,2.4f,"Five off-ball support targets stay separated.");
            Check(support.All(p=>Vector3.Distance(p,blue[0].Motor.Position)>3f),"Support does not pile onto the carrier.");
            teams[1].SetDefense(DefenseStrategy.Press);var modeData=new SerializedObject(teams[1]);bool automatic=modeData.FindProperty("automaticDefense").boolValue;
            modeData.FindProperty("automaticDefense").boolValue=false;modeData.ApplyModifiedPropertiesWithoutUndo();
            try
            {
                teams[1].ResetDecisions();var defenders=roster.FieldPlayers.Where(p=>p.Team==PrototypeTeam.White).ToArray();
                Check(defenders.Count(p=>WaterPoloDecision.Evaluate(p,teams[1]).Behaviour==SwimmerAIBehaviour.Press)==1,"Exactly one coordinated presser.");
                teams[1].SetDefense(DefenseStrategy.Zone);Separate(defenders.Select(p=>teams[1].Defense(p,blue[0])).ToArray(),2.4f,"Six distinct zone targets.");
                var guard=keepers.Single(k=>k.Identity.Team==PrototypeTeam.Blue);ball.ResetForRestart(guard.HandPosition);Check(guard.Identity.Interaction.TryCollect(),"Same-team keeper clock fixture.");
                Check(teams[1].Presser==null,"Keeper possession does not create a bogus field presser/duplicate marking rank.");
            }
            finally{modeData.Update();modeData.FindProperty("automaticDefense").boolValue=automatic;modeData.ApplyModifiedPropertiesWithoutUndo();}
            match.RestartMatch();ball.ResetForRestart(blue[0].Motor.Position+blue[0].Motor.Facing*1.4f);Check(blue[0].Interaction.TryCollect(),"Independent shot-clock acquisition.");
            int changes=match.ShotClock.TeamChanges;match.AdvanceLiveTime(10f);
            Near(match.Clock.DisplayRemaining,420f,"Match clock compresses ten seconds.");Near(match.ShotClock.Remaining,18f,"Possession clock consumes ten actual seconds, not sixty.");
            selection.SelectNext();Check(match.ShotClock.TeamChanges==changes,"Manual switch does not reset shot time.");
            var keeper=keepers.Single(k=>k.Identity.Team==PrototypeTeam.Blue);ball.ResetForRestart(keeper.HandPosition);Check(keeper.Identity.Interaction.TryCollect(),"Keeper acquires through range-checked shared API.");
            Check(match.ShotClock.TeamChanges==changes,"Same-team keeper acquisition preserves countdown.");
            ball.ResetForRestart(white.Motor.Position+white.Motor.Facing*1.4f);Check(white.Interaction.TryCollect(),"Genuine opposing acquisition.");
            Check(match.ShotClock.TeamChanges==changes+1 && match.ShotClock.Remaining==28f,"Opposing team receives exactly one 28-second reset.");
            match.AdvanceLiveTime(28f);Check(match.Phase==MatchPhase.TurnoverRestart && ball.Owner==null && selection.InputLocked,"Shot-clock expiration pauses and clears ownership.");
            float paused=match.Clock.Remaining;match.AdvanceLiveTime(10f);Near(match.Clock.Remaining,paused,"Turnover does not consume quarter time.");await Until(()=>match.IsPlaying);
            Check(ball.Owner!=null && ball.Owner.GetComponent<SwimmerIdentity>().Team==PrototypeTeam.Blue && match.ShotClock.Remaining>27.8f,"Opponent is legally awarded the next possession.");
            return new {passed=true,selectedFields=6,coordinatedSupport=true,pressZone=true,independentClock=true,turnover=true};
        }
        finally{InputSystem.RemoveDevice(keyboard);match.RestartMatch();foreach(var k in keepers)k.enabled=true;foreach(var a in ai)a.enabled=true;}
    }
    private static void Separate(Vector3[] points,float minimum,string message)
    {for(int i=0;i<points.Length;i++)for(int j=i+1;j<points.Length;j++)Check(Vector3.Distance(points[i],points[j])>minimum,message);}
    private static async Task Tap(Keyboard keyboard,Key key,float duration=0.06f)
    {InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));await Advance(duration);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Advance(0.04f);}
    public static async Task<object> GoalsAndKeepers()
    {
        Check(Application.isPlaying,"Goal/keeper check requires Play Mode.");
        var match=UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();var ball=UnityEngine.Object.FindAnyObjectByType<BallController>();var selection=UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
        var ai=UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>();var keepers=UnityEngine.Object.FindObjectsByType<PrototypeGoalkeeper>();var teams=UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>();
        var tuning=keepers.Select(k=>new {keeper=k,save=Read(k,"saveChance"),catchChance=Read(k,"catchChance"),reaction=Read(k,"reactionDelay")}).ToArray();
        foreach(var a in ai)a.enabled=false;foreach(var k in keepers)k.enabled=false;match.RestartMatch();
        PrototypeTeamTactics Team(PrototypeTeam team)=>teams.Single(t=>t.Team==team);
        void Park()
        {
            foreach(var side in teams){int rank=0;foreach(var p in roster.FieldPlayers.Where(p=>p.Team==side.Team))p.Motor.ResetPose(new Vector3(side.Team==PrototypeTeam.Blue?8.3f:-8.3f,0f,-11f+4f*rank++),Quaternion.LookRotation(side.AttackDirection));}
        }
        async Task Goal(PrototypeTeam credited,PrototypeTeam shooterTeam)
        {
            foreach(var k in keepers)k.enabled=false;Park();var goal=Team(credited).AttackingGoal;
            var shooter=roster.FieldPlayers.First(p=>p.Team==shooterTeam);Vector3 point=goal.transform.TransformPoint(new Vector3(0f,0f,-4f));point.y=0f;
            shooter.Motor.ResetPose(point,goal.transform.rotation);ball.ResetForRestart(point+goal.transform.forward*1.65f);
            Check(shooter.Interaction.TryCollect() && ball.TryShoot(shooter.Interaction,80f),"Real scoring fixture uses pickup/physical release.");
            int blue=match.BlueScore,white=match.WhiteScore;await Until(()=>match.Phase==MatchPhase.GoalRestart);
            Check(match.BlueScore==blue+(credited==PrototypeTeam.Blue?1:0) && match.WhiteScore==white+(credited==PrototypeTeam.White?1:0),"Current goal mapping credits correct team, including own goals.");
            await Advance(0.15f);Check(match.BlueScore+match.WhiteScore==blue+white+1,"No duplicate score during restart.");await Until(()=>match.IsPlaying);
            var conceded=credited==PrototypeTeam.Blue?PrototypeTeam.White:PrototypeTeam.Blue;
            Check(ball.Owner!=null && ball.Owner.GetComponent<SwimmerIdentity>().Team==conceded && !ball.IsScoringShot && roster.Members.Count(p=>p.Interaction.HasBall)==1,"Conceding team receives one legal restart ownership.");
            Check(roster.Members.All(p=>Vector3.Distance(p.Motor.Position,p.StartPosition.position)<0.02f),"All fourteen reset to current-half starts.");
        }
        void ExpirePeriod(){match.Clock.Tick(Mathf.Max(0f,match.Clock.Remaining-0.001f));match.AdvanceLiveTime(0.02f);}
        try
        {
            await Goal(PrototypeTeam.Blue,PrototypeTeam.Blue);await Goal(PrototypeTeam.White,PrototypeTeam.White);
            ExpirePeriod();match.ContinueMatch();Check(match.Clock.Quarter==2 && !match.EndsSwitched,"Q2 retains initial ends.");
            var blueKeeper=keepers.Single(k=>k.Identity.Team==PrototypeTeam.Blue);blueKeeper.enabled=true;
            ball.ResetForRestart(blueKeeper.HandPosition);await Until(()=>ball.Owner==blueKeeper.Identity.Interaction);
            Check(selection.ActivePlayer==blueKeeper.Identity && blueKeeper.GetComponent<ShotCharge>().BeginCharge(),"Keeper human possession/charge before halftime.");
            ExpirePeriod();Check(match.Phase==MatchPhase.HalfTime && ball.Owner==null && !blueKeeper.GetComponent<ShotCharge>().IsCharging && keepers.All(k=>k.GetComponent<GoalkeeperDistribution>().IntendedOutlet==null),"Halftime cancels keeper possession, charge and queued distribution.");
            match.ContinueMatch();Check(match.BlueScore==1 && match.WhiteScore==1 && match.EndsSwitched && selection.ActivePlayer.IsFieldPlayer,"Halftime preserves score and restores valid field control.");
            foreach(var guard in keepers)
            {
                foreach(var k in keepers)k.enabled=k==guard;Park();Write(guard,"saveChance",1f);Write(guard,"catchChance",1f);Write(guard,"reactionDelay",0f);
                guard.Identity.Motor.ResetPose(guard.Identity.StartPosition.position,guard.Identity.StartPosition.rotation);
                var shooter=roster.FieldPlayers.First(p=>p.Team!=guard.Identity.Team);var goal=guard.DefendedGoal;
                Vector3 point=goal.transform.TransformPoint(new Vector3(0f,0f,-6f));point.y=0f;shooter.Motor.ResetPose(point,goal.transform.rotation);
                var receiver=roster.FieldPlayers.Single(p=>p.Team==guard.Identity.Team && p.FieldPosition==FieldPositionRole.CentreBack);
                Vector3 outlet=goal.transform.TransformPoint(new Vector3(0f,0f,-6f));outlet.y=0f;
                // Move the opposing shooter slightly farther from this outlet while retaining an unobstructed central shot.
                point=goal.transform.TransformPoint(new Vector3(0f,0f,-9f));point.y=0f;shooter.Motor.ResetPose(point,goal.transform.rotation);
                await Advance(0.2f);ball.ResetForRestart(point+goal.transform.forward*1.65f);Check(shooter.Interaction.TryCollect(),"Opponent acquires before keeper save fixture.");
                int saves=guard.Catches,total=match.BlueScore+match.WhiteScore;Check(ball.TryShoot(shooter.Interaction,80f),"Actual high-speed shot toward newly defended goal.");
                await Until(()=>ball.Owner==guard.Identity.Interaction);
                Check(guard.Catches==saves+1 && match.BlueScore+match.WhiteScore==total && guard.AtStation,"Keeper physically saves at new goal, not old goal.");
                Check(selection.ActivePlayer.Team==PrototypeTeam.Blue && (guard.Identity.Team!=PrototypeTeam.Blue || selection.ActivePlayer==guard.Identity),"Blue catch controls keeper, White catch never selects White.");
                receiver.Motor.ResetPose(outlet,Quaternion.LookRotation(Team(guard.Identity.Team).AttackDirection));await Advance(0.12f);
                if(guard.Identity.Team==PrototypeTeam.Blue)
                    Check(guard.GetComponent<SwimmerPassing>().TryPass(),"Human keeper uses shared physical outlet pass after halftime.");
                else
                {int distributions=guard.GetComponent<GoalkeeperDistribution>().Distributions;await Until(()=>guard.GetComponent<GoalkeeperDistribution>().Distributions>distributions);}
                Check(ball.Owner==null && ball.ReleaseKind==BallReleaseKind.Pass && ball.Velocity.sqrMagnitude>20f,"Keeper distribution travels physically.");
                await Until(()=>ball.Owner==receiver.Interaction);
                Check(roster.Members.Count(p=>p.Interaction.HasBall)==1 && (receiver.Team!=PrototypeTeam.Blue || selection.ActivePlayer==receiver),"Outlet reception preserves exclusive ownership and human auto-selection.");
            }
            await Goal(PrototypeTeam.Blue,PrototypeTeam.Blue);await Goal(PrototypeTeam.White,PrototypeTeam.White);await Goal(PrototypeTeam.White,PrototypeTeam.Blue);
            Check(match.BlueScore==2 && match.WhiteScore==3,"Both teams and a Blue own goal attributed correctly after halftime.");
            return new {passed=true,goals=5,afterHalftimeOwnGoal=true,newEndCatches=2,physicalDistributions=2,concedingRestarts=true,allReset=14};
        }
        finally
        {
            foreach(var t in tuning){Write(t.keeper,"saveChance",t.save);Write(t.keeper,"catchChance",t.catchChance);Write(t.keeper,"reactionDelay",t.reaction);}
            match.RestartMatch();foreach(var k in keepers)k.enabled=true;foreach(var a in ai)a.enabled=true;
        }
    }
    private static float Read(UnityEngine.Object target,string field)=>new SerializedObject(target).FindProperty(field).floatValue;
    private static void Write(UnityEngine.Object target,string field,float value){var data=new SerializedObject(target);data.FindProperty(field).floatValue=value;data.ApplyModifiedPropertiesWithoutUndo();}
    public static async Task<object> TimingAndEnds()
    {
        Check(Application.isPlaying,"Timing requires Play Mode.");
        var match=UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();var ball=UnityEngine.Object.FindAnyObjectByType<BallController>();
        var selection=UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();var camera=UnityEngine.Object.FindAnyObjectByType<GameplayCamera>();
        var ai=UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>();var keepers=UnityEngine.Object.FindObjectsByType<PrototypeGoalkeeper>();
        foreach(var a in ai)a.enabled=false;foreach(var k in keepers)k.enabled=false;
        var teams=UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>().OrderBy(t=>t.Team).ToArray();
        match.RestartMatch();var attack=teams.Select(t=>t.AttackingGoal).ToArray();var defend=teams.Select(t=>t.DefendedGoal).ToArray();
        var starts=roster.Members.Select(p=>p.StartPosition.position).ToArray();
        try
        {
            var numeric=new MatchClock();numeric.Reset();numeric.Tick(10d);Near(numeric.DisplayRemaining,420f,"10 seconds displays 7:00.");numeric.Tick(30d);Near(numeric.DisplayRemaining,240f,"40 seconds displays 4:00.");
            Check(numeric.Tick(40d) && numeric.Remaining==0f && !numeric.Tick(1d),"80 seconds expires once, never negative.");
            numeric.Reset();for(int i=0;i<4000;i++)numeric.Tick((double)0.02f);Check(numeric.Remaining==0f,"4000 physics steps end exactly at the period boundary.");
            match.AdvanceLiveTime(10f);Near(match.Clock.DisplayRemaining,420f,"Authoritative match uses the same 6x conversion.");
            match.AdvanceLiveTime(30f);Near(match.Clock.DisplayRemaining,240f,"Authoritative 40 live seconds.");match.AdvanceLiveTime(40f);
            Check(match.Phase==MatchPhase.QuarterBreak && !match.EndsSwitched && match.Clock.Quarter==1,"Q1 ends without halftime swap.");
            match.AdvanceLiveTime(20f);Check(match.Clock.Remaining==0f && match.ShotClock.Remaining==28f,"Stoppage consumes neither clock.");
            match.ContinueMatch();Check(match.Clock.Quarter==2 && match.Clock.DisplayRemaining==480f,"Q2 starts at 8:00.");
            match.AdvanceLiveTime(80f);Check(match.Phase==MatchPhase.HalfTime && !match.EndsSwitched && selection.InputLocked,"Halftime begins only after Q2, before ends change.");
            await Advance(0.05f);var panel=UnityEngine.Object.FindAnyObjectByType<MatchHUD>().transform.Find("MatchPanel");
            Check(panel.gameObject.activeSelf && panel.Find("Title").GetComponent<TMPro.TMP_Text>().text.Contains("HALFTIME"),"HALFTIME panel is visible.");
            Vector3 cameraBefore=camera.transform.position;match.ContinueMatch();match.ContinueMatch();
            Check(match.Clock.Quarter==3 && match.Clock.DisplayRemaining==480f && match.EndsSwitched && match.EndSwitchCount==1,"Continue twice changes ends exactly once and starts Q3.");
            Check(Vector3.Distance(cameraBefore,camera.transform.position)<0.001f,"Halftime does not snap the camera pose.");
            var pool=UnityEngine.Object.FindAnyObjectByType<PoolBounds>();var half=Quaternion.Euler(0f,180f,0f);
            for(int i=0;i<teams.Length;i++)
            {
                Check(teams[i].AttackingGoal==defend[i] && teams[i].DefendedGoal==attack[i] && teams[i].AttackingGoal.ScoringTeam==teams[i].Team,"Team goal references and scoring attribution reverse together.");
                var keeper=keepers.Single(k=>k.Identity.Team==teams[i].Team);Check(keeper.DefendedGoal==teams[i].DefendedGoal && keeper.AtStation,"Keeper relocated to its new defended goal.");
                var fields=roster.FieldPlayers.Where(p=>p.Team==teams[i].Team).ToArray();Check(fields.All(p=>Vector3.Dot(p.Motor.Position-pool.transform.position,teams[i].AttackDirection)<0f),"All fields restart in their new defensive half.");
            }
            for(int i=0;i<roster.Members.Count;i++)
                Check(Vector3.Distance(roster.Members[i].Motor.Position,pool.transform.position+half*(starts[i]-pool.transform.position))<0.01f,"Every athlete physically changed ends.");
            Check(ball.Owner==null && !ball.IsAirborne && ball.IntendedReceiver==null && Vector3.Distance(ball.PhysicsPosition,new Vector3(pool.transform.position.x,ball.SurfaceHeight,pool.transform.position.z))<0.01f,"Neutral centre ball and cleared actions at period start.");
            match.AdvanceLiveTime(80f);Check(match.Phase==MatchPhase.QuarterBreak && match.Clock.Quarter==3,"Q3 ends in ordinary break.");
            match.ContinueMatch();Check(match.Clock.Quarter==4 && match.EndSwitchCount==1 && teams[0].AttackingGoal==defend[0],"Q4 retains Q3 ends.");
            match.AdvanceLiveTime(80f);Check(match.Phase==MatchPhase.FullTime && match.Result=="DRAW","Full time after Q4.");
            match.AdvanceLiveTime(100f);Check(match.Clock.Remaining==0f && match.EndSwitchCount==1,"Full time cannot continue clocks/swap.");
            match.RestartMatch();Check(!match.EndsSwitched && match.EndSwitchCount==0 && match.Clock.Quarter==1 && match.Clock.DisplayRemaining==480f,"New match restores initial quarter/ends.");
            for(int i=0;i<teams.Length;i++)Check(teams[i].AttackingGoal==attack[i] && teams[i].DefendedGoal==defend[i],"Original goal mapping restored.");
            for(int i=0;i<roster.Members.Count;i++)Check(Vector3.Distance(roster.Members[i].Motor.Position,starts[i])<0.01f,"Original starts restored.");
            return new {passed=true,realSeconds=80,ratio=6,periods=4,swapsPerMatch=1,physicalRelocations=14,newMatchRestores=true};
        }
        finally{match.RestartMatch();foreach(var k in keepers)k.enabled=true;foreach(var a in ai)a.enabled=true;}
    }
    private static void Near(float value,float expected,string message)=>Check(Mathf.Abs(value-expected)<0.002f,message+$" ({value} vs {expected})");
    private static async Task Advance(float seconds){var watch=Stopwatch.StartNew();float start=Time.fixedTime;while(Time.fixedTime-start<seconds){if(watch.Elapsed.TotalSeconds>10)throw new TimeoutException("Physics not advancing.");await Task.Yield();}}
    private static async Task Until(Func<bool> condition,float seconds=5f){var watch=Stopwatch.StartNew();while(!condition()){if(watch.Elapsed.TotalSeconds>seconds)throw new TimeoutException("Expected full-team match transition did not occur.");await Task.Yield();}}
}
