using System;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using ToduStudios.WaterPolo;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class Milestone9Verification
{
    public static object Setup()
    {
        Check(!Application.isPlaying,"Setup requires Edit Mode.");
        ToduStudios.WaterPolo.Editor.Milestone9PrototypeSetup.SetUp();
        int Count()=>UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Transform>(true).Length);
        int before=Count();var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();var ids=roster.Members.Select(p=>p.PlayerId).ToArray();
        var camera=UnityEngine.Object.FindAnyObjectByType<GameplayCamera>();float distance=Read(camera,"distance");var stamina=roster.FieldPlayers[0].GetComponent<SwimmerStamina>();float drain=Read(stamina,"drainPerSecond");
        Write(camera,"distance",distance+0.37f);Write(stamina,"drainPerSecond",drain+0.02f);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(camera.gameObject.scene);
        ToduStudios.WaterPolo.Editor.Milestone9PrototypeSetup.SetUp();
        bool preserved=Mathf.Approximately(Read(camera,"distance"),distance+0.37f) && Mathf.Approximately(Read(stamina,"drainPerSecond"),drain+0.02f);
        Write(camera,"distance",distance);Write(stamina,"drainPerSecond",drain);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(camera.gameObject.scene);
        Check(preserved,"Repeated setup preserves camera and stamina Inspector tuning.");
        Check(Count()==before && ids.SequenceEqual(roster.Members.Select(p=>p.PlayerId)),"Repeated setup preserves objects/identities.");
        Check(roster.Members.Count==8 && roster.FieldPlayers.Count==6,"Eight athletes, six fields.");
        foreach(var team in new[]{PrototypeTeam.Blue,PrototypeTeam.White})
        {
            var members=roster.Members.Where(p=>p.Team==team).ToArray();
            Check(members.Count(p=>p.IsFieldPlayer)==3 && members.Count(p=>!p.IsFieldPlayer)==1,"3+1 per team.");
            Check(members.Where(p=>p.IsFieldPlayer).Select(p=>p.FieldPosition).Distinct().Count()==3,"Three distinct tactical roles.");
        }
        Check(roster.Members.Select(p=>p.PlayerId).Distinct().Count()==8 && roster.Members.Select(p=>p.StartPosition).Distinct().Count()==8,"Unique identity and start markers.");
        Check(UnityEngine.Object.FindObjectsByType<PrototypeMatch>().Length==1 && UnityEngine.Object.FindObjectsByType<BallController>().Length==1 && UnityEngine.Object.FindObjectsByType<PossessionShotClock>().Length==1,"One match phase, ball authority and shot clock.");
        var module=UnityEngine.Object.FindAnyObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();Check(module!=null && module.actionsAsset!=null,"Serialized new-Input-System UI module configured.");
        var hud=UnityEngine.Object.FindAnyObjectByType<MatchHUD>();
        Check(hud.transform.Find("MatchPanel/Continue").GetComponent<UnityEngine.UI.Button>().onClick.GetPersistentEventCount()==1 && hud.transform.Find("MatchPanel/Restart").GetComponent<UnityEngine.UI.Button>().onClick.GetPersistentEventCount()==1,"No duplicate UI listeners.");
        return new {passed=true,objects=before,athletes=8};
    }
    public static async Task<object> Expansion()
    {
        Check(Application.isPlaying,"Runtime check requires Play Mode.");
        var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();var selection=UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
        var ball=UnityEngine.Object.FindAnyObjectByType<BallController>();var match=UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
        while(match.IsRestarting)await Advance(0.05f);
        var seen=new System.Collections.Generic.HashSet<SwimmerIdentity>();
        for(int i=0;i<3;i++){seen.Add(selection.ActivePlayer);selection.SelectNext();}
        Check(seen.Count==3 && seen.All(p=>p.Team==PrototypeTeam.Blue && p.IsFieldPlayer),"C cycles three Blue fields.");
        Check(roster.Members.Where(p=>p.Team==PrototypeTeam.White).All(p=>!selection.Select(p)),"White never eligible.");
        await Advance(0.5f);
        Check(roster.Members.Count(p=>p.GetComponent<SwimmerInput>()!=null && p.GetComponent<SwimmerInput>().enabled)==1,"One human movement input.");
        Check(roster.Members.All(p=>Mathf.Abs(p.Motor.Position.x)<9f && Mathf.Abs(p.Motor.Position.z)<15f && Mathf.Abs(p.Motor.Position.y)<0.001f),"All eight bounded at waterline.");
        return new {passed=true,selectedFields=seen.Count,athletes=roster.Members.Count};
    }
    public static async Task<object> Tactics()
    {
        Check(Application.isPlaying,"Tactics requires Play Mode.");
        var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();var ball=UnityEngine.Object.FindAnyObjectByType<BallController>();
        var selection=UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();var match=UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();while(match.IsRestarting)await Advance(0.05f);
        var ai=UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>();foreach(var a in ai)a.enabled=false;
        var team=UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>().Single(t=>t.Team==PrototypeTeam.Blue);
        var blue=roster.FieldPlayers.Where(p=>p.Team==PrototypeTeam.Blue).ToArray();var white=roster.FieldPlayers.Where(p=>p.Team==PrototypeTeam.White).ToArray();
        var previous=team.DefenseMode;float shotSpeed=Read(team,"aiShotSpeed");
        try
        {
            roster.ResetPositions();ball.ResetForRestart(white[0].Motor.Position+Vector3.back*1.65f);Check(white[0].Interaction.TryCollect(),"Opponent acquires through existing API.");
            team.SetDefense(DefenseStrategy.Press);team.ResetDecisions();
            Check(blue.Count(p=>WaterPoloDecision.Evaluate(p,team).Behaviour==SwimmerAIBehaviour.Press)==1,"Exactly one presser.");
            var pressTargets=blue.Select(p=>team.Defense(p,white[0])).ToArray();team.SetDefense(DefenseStrategy.Zone);
            var zoneTargets=blue.Select(p=>team.Defense(p,white[0])).ToArray();
            Check(pressTargets.Where((p,i)=>Vector3.Distance(p,zoneTargets[i])>1f).Any(),"Defense modes produce different positioning.");
            Check(zoneTargets.Select(p=>Mathf.RoundToInt(p.x)).Distinct().Count()==3,"Zone protects three distinct lanes.");
            ball.ResetForRestart(blue[0].Motor.Position+Vector3.forward*1.65f);Check(blue[0].Interaction.TryCollect(),"Friendly acquisition.");
            var supports=blue.Skip(1).Select(p=>team.Support(p,blue[0])).ToArray();
            Check(Vector3.Distance(supports[0],supports[1])>4f && supports.All(p=>Vector3.Distance(p,blue[0].Motor.Position)>3f),"Role-based supporting lanes remain spaced.");
            Vector3 from=new Vector3(0f,0f,0f),to=new Vector3(0f,0f,8f);
            white[1].Motor.ResetPose(new Vector3(-8f,0f,-8f),Quaternion.identity);white[2].Motor.ResetPose(new Vector3(8f,0f,-8f),Quaternion.identity);
            white[0].Motor.ResetPose(new Vector3(0f,0f,4f),Quaternion.identity);await Advance(0.1f);
            Check(!team.LaneClear(from,to) && team.LaneClear(from,new Vector3(6f,0f,8f)),"Defender obstruction changes pass-lane risk.");
            var first=WaterPoloDecision.Evaluate(blue[1],team);var second=WaterPoloDecision.Evaluate(blue[1],team);
            Check(first.Target==second.Target && first.Behaviour==second.Behaviour,"Tactics deterministic under stable context.");
            Vector3 shootingPoint=team.AttackingGoal.transform.TransformPoint(new Vector3(0f,0f,-4f));
            float quality=team.ShotQuality(shootingPoint);Write(team,"aiShotSpeed",2f);
            Check(quality>0f && team.ShotQuality(shootingPoint)==0f,"Low shot power cannot produce an optimistic out-of-range quality estimate.");Write(team,"aiShotSpeed",shotSpeed);
            match.ShotClock.Tick(match.ShotClock.Remaining-3f);
            Check(WaterPoloDecision.Evaluate(blue[0],team).Behaviour==SwimmerAIBehaviour.Shoot,"Low possession time produces shooting urgency.");
            return new {passed=true,roles=true,pressZone=true,lanes=true,deterministic=true,shotPowerBudget=true,clockUrgency=true};
        }
        finally{Write(team,"aiShotSpeed",shotSpeed);team.SetDefense(previous);ball.ResetForRestart(new Vector3(0f,ball.SurfaceHeight,1.65f));roster.ResetPositions();selection.Select(blue[0]);foreach(var a in ai)a.enabled=true;}
    }
    public static async Task<object> Clocks()
    {
        Check(Application.isPlaying,"Clock check requires Play Mode.");
        var match=UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
        var selection=UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();var ball=UnityEngine.Object.FindAnyObjectByType<BallController>();var shot=match.ShotClock;
        var ai=UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>();foreach(var a in ai)a.enabled=false;
        var keepers=UnityEngine.Object.FindObjectsByType<PrototypeGoalkeeper>();foreach(var k in keepers)k.enabled=false;
        var blue=roster.FieldPlayers.Where(p=>p.Team==PrototypeTeam.Blue).ToArray();var white=roster.FieldPlayers.First(p=>p.Team==PrototypeTeam.White);
        float duration=Read(match,"clock.realSecondsPerQuarter");int quarters=new SerializedObject(match).FindProperty("clock.quarters").intValue;
        try
        {
            match.RestartMatch();blue[0].Motor.ResetPose(Vector3.zero,Quaternion.identity);blue[1].Motor.ResetPose(new Vector3(0f,0f,5f),Quaternion.identity);
            ball.ResetForRestart(Vector3.forward*1.65f);Check(blue[0].Interaction.TryCollect(),"Clock starts on legitimate acquisition.");int changes=shot.TeamChanges;
            await Advance(0.2f);float before=shot.Remaining;
            Check(before<30f && before>29f,"Shot clock counts real live time.");
            Check(blue[0].GetComponent<SwimmerPassing>().TryPassTo(blue[1]),"Physical same-team pass.");await Until(()=>ball.Owner==blue[1].Interaction,4f);
            Check(shot.TeamChanges==changes && shot.Remaining<before,"Same-team reception does not reset clock.");
            var blueKeeper=keepers.Single(k=>k.Identity.Team==PrototypeTeam.Blue);ball.ResetForRestart(blueKeeper.HandPosition);Check(blueKeeper.Identity.Interaction.TryCollect(),"Keeper legitimate acquisition.");
            Check(shot.TeamChanges==changes,"Same-team keeper hold does not reset possession.");selection.SelectNext();Check(shot.TeamChanges==changes,"C does not reset possession clock.");
            ball.ResetForRestart(white.Motor.Position+white.transform.forward*1.5f);Check(white.Interaction.TryCollect(),"Opposition acquisition.");
            Check(shot.TeamChanges==changes+1 && shot.Remaining>29.9f,"Opposing acquisition resets exactly once.");
            ball.ResetForRestart(blue[0].Motor.Position+blue[0].transform.forward*1.5f);Check(blue[0].Interaction.TryCollect(),"Blue turnover fixture.");
            Check(blue[0].GetComponent<ShotCharge>().BeginCharge(),"Charge before possession timeout.");shot.Tick(31f);
            Check(match.Phase==MatchPhase.TurnoverRestart && selection.InputLocked && ball.Owner==null && !blue[0].GetComponent<ShotCharge>().IsCharging,"Turnover cancels charge/owner and locks input.");
            float time=match.Clock.Remaining;await Advance(0.2f);Check(match.Clock.Remaining==time,"Quarter clock pauses during turnover.");
            await Until(()=>match.IsPlaying,4f);Check(ball.Owner==white.Interaction && roster.Members.Count(p=>p.Interaction.HasBall)==1 && !selection.InputLocked,"Opponent awarded a legal exclusive restart possession.");

            Write(match,"clock.realSecondsPerQuarter",1.2f);var data=new SerializedObject(match);data.FindProperty("clock.quarters").intValue=2;data.ApplyModifiedPropertiesWithoutUndo();match.RestartMatch();
            Check(Mathf.Abs(match.Clock.DisplayRemaining-480f)<0.1f,"Compressed quarter starts at displayed eight minutes.");
            await Until(()=>match.Phase==MatchPhase.QuarterBreak,4f);
            Check(selection.InputLocked && ball.Owner==null && roster.Members.All(p=>!p.Motor.enabled),"Quarter end stops all eight and clears ownership.");
            Check(!blue[0].Interaction.TryShoot() && !blue[0].Interaction.TryCollect(),"Direct gameplay APIs reject quarter break.");
            await Advance(0.1f);Check(match.Clock.Remaining==0f,"Quarter cannot run during break.");
            ScreenCapture.CaptureScreenshot("Logs/Milestone9QuarterBreak.png");await Advance(0.1f);
            match.ContinueMatch();Check(match.IsPlaying && match.Clock.Quarter==2 && roster.Members.All(p=>Vector3.Distance(p.Motor.Position,p.StartPosition.position)<0.01f),"Continue starts next quarter with all eight reset.");
            await Until(()=>match.Phase==MatchPhase.FullTime,4f);Check(match.Result=="DRAW" && selection.InputLocked,"Final quarter produces locked full time result.");
            var hud=UnityEngine.Object.FindAnyObjectByType<MatchHUD>();await Advance(0.05f);Check(hud.transform.Find("MatchPanel").gameObject.activeSelf,"Full time panel displayed.");
            ScreenCapture.CaptureScreenshot("Logs/Milestone9FullTime.png");await Advance(0.1f);
            match.ReturnToPrototype();Check(match.Phase==MatchPhase.PreMatch && match.BlueScore==0 && match.WhiteScore==0,"Return to prototype returns to ready state.");
            match.ContinueMatch();Check(match.IsPlaying && match.Clock.Quarter==1,"Start/restart initializes fresh match.");
            return new {passed=true,teamClock=true,turnover=true,quarters=true,fullTime=true,resetAthletes=8};
        }
        finally
        {
            Write(match,"clock.realSecondsPerQuarter",duration);var data=new SerializedObject(match);data.FindProperty("clock.quarters").intValue=quarters;data.ApplyModifiedPropertiesWithoutUndo();match.RestartMatch();
            foreach(var k in keepers)k.enabled=true;foreach(var a in ai)a.enabled=true;
        }
    }
    private static float Read(UnityEngine.Object target,string field)=>new SerializedObject(target).FindProperty(field).floatValue;
    public static async Task<object> Aerial()
    {
        Check(Application.isPlaying,"Aerial requires Play Mode.");var match=UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();match.RestartMatch();
        var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();var ball=UnityEngine.Object.FindAnyObjectByType<BallController>();var selection=UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
        var ai=UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>();foreach(var a in ai)a.enabled=false;
        var keepers=UnityEngine.Object.FindObjectsByType<PrototypeGoalkeeper>();foreach(var k in keepers)k.enabled=false;
        var blue=roster.FieldPlayers.Where(p=>p.Team==PrototypeTeam.Blue).ToArray();var keyboard=InputSystem.AddDevice<Keyboard>();
        async Task Prepare()
        {
            match.RestartMatch();foreach(var player in roster.FieldPlayers)player.Motor.ResetPose(new Vector3(player.Team==PrototypeTeam.Blue?-8f:8f,0f,-8f),Quaternion.identity);
            blue[0].Motor.ResetPose(Vector3.zero,Quaternion.identity);blue[1].Motor.ResetPose(new Vector3(0f,0f,8f),Quaternion.identity);selection.Select(blue[0]);
            ball.ResetForRestart(Vector3.forward*1.65f);Check(blue[0].Interaction.TryCollect(),"Lob fixture range pickup.");await Advance(0.1f);
        }
        try
        {
            await Prepare();InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.L));await Advance(0.08f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());
            Check(ball.IsAirborne && ball.Owner==null && ball.ReleaseKind==BallReleaseKind.Lob,"L releases physical aerial pass.");
            await Advance(0.5f);Check(ball.PhysicsPosition.y>ball.SurfaceHeight+1f && ball.GetComponent<Rigidbody>().useGravity,"Ball really rises in physics, with gravity.");
            blue[2].Motor.ResetPose(new Vector3(ball.PhysicsPosition.x,0f,ball.PhysicsPosition.z),Quaternion.identity);
            Check(!blue[2].Interaction.TryCollect() && ball.Owner==null,"A nearby field player cannot claim a high ball.");
            blue[2].Motor.ResetPose(new Vector3(-8f,0f,-8f),Quaternion.identity);
            await Until(()=>ball.Owner==blue[1].Interaction,4f);
            Check(!ball.IsAirborne && !ball.GetComponent<Rigidbody>().useGravity && selection.ActivePlayer==blue[1] && roster.Members.Count(p=>p.Interaction.HasBall)==1,"Descending receiver acquires exclusively and restores surface physics/selection.");
            Check(blue[1].Interaction.TryShoot(),"Flat shot works immediately after aerial reception.");

            await Prepare();Check(ball.TryLob(blue[0].Interaction,Vector3.right,6f,1.3f,null),"Unaimed lob uses the shared authority.");
            await Until(()=>!ball.IsAirborne,4f);Check(ball.Owner==null && Mathf.Abs(ball.PhysicsPosition.y-ball.SurfaceHeight)<0.01f && ball.CanCollect,$"Missed lob lands into normal collectible waterline state: owner={ball.Owner}, state={ball.State}, y={ball.PhysicsPosition.y}, surface={ball.SurfaceHeight}, collect={ball.CanCollect}, phase={match.Phase}.");
            var goal=UnityEngine.Object.FindObjectsByType<GoalMouth>().First();
            Check(!goal.TryGetCrossing(goal.transform.TransformPoint(new Vector3(0f,2f,-1f)),goal.transform.TransformPoint(new Vector3(0f,2f,1f)),ball.Radius,out _),"Above-crossbar crossing remains invalid.");
            await Prepare();Check(blue[0].GetComponent<LobPassing>().TryPass(),"Second lob starts.");
            match.RequestTurnover(PrototypeTeam.White);Check(!ball.IsAirborne && ball.Owner==null && !ball.GetComponent<Rigidbody>().useGravity,"Stoppage clears aerial state safely.");
            await Until(()=>match.IsPlaying,4f);Check(ball.Owner!=null && ball.Owner.GetComponent<SwimmerIdentity>().Team==PrototypeTeam.White,"Post-lob turnover restores legal opposing possession.");
            return new {passed=true,realHeight=true,highPickupRejected=true,physicalReception=true,landing=true,aboveBarRejected=true,transitionCleanup=true};
        }
        finally{InputSystem.RemoveDevice(keyboard);match.RestartMatch();foreach(var k in keepers)k.enabled=true;foreach(var a in ai)a.enabled=true;}
    }
    public static async Task<object> Integration()
    {
        Check(Application.isPlaying,"Integration requires Play Mode.");
        var match=UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();var ball=UnityEngine.Object.FindAnyObjectByType<BallController>();
        var selection=UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();var ai=UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>();var keepers=UnityEngine.Object.FindObjectsByType<PrototypeGoalkeeper>();
        var blue=roster.FieldPlayers.Where(p=>p.Team==PrototypeTeam.Blue).ToArray();var white=roster.FieldPlayers.First(p=>p.Team==PrototypeTeam.White);
        var blueKeeper=keepers.Single(k=>k.Identity.Team==PrototypeTeam.Blue);int resets=0;bool invalidReset=false;
        void ResetObserved()
        {
            if(!match.IsPlaying)return;resets++;
            if(roster.Members.Any(p=>!p.Motor.enabled || Vector3.Distance(p.Motor.Position,p.StartPosition.position)>0.01f || (p.GetComponent<ShotCharge>()!=null && p.GetComponent<ShotCharge>().IsCharging)) || ball.IsAirborne || ball.IntendedReceiver!=null || selection.InputLocked)invalidReset=true;
        }
        match.Changed+=ResetObserved;
        try
        {
            foreach(var a in ai)a.enabled=false;foreach(var k in keepers)k.enabled=false;match.RestartMatch();
            var goal=UnityEngine.Object.FindObjectsByType<GoalMouth>().Single(g=>g.ScoringTeam==PrototypeTeam.Blue);
            Vector3 position=goal.transform.TransformPoint(new Vector3(0f,0f,-2.3f));position.y=0f;
            blue[0].Motor.ResetPose(position,goal.transform.rotation);ball.ResetForRestart(position+goal.transform.forward*1.65f);
            Check(blue[0].Interaction.TryCollect(),"Final-seconds real-shot pickup.");match.Clock.Tick(match.Clock.Remaining-0.12f);
            Check(ball.TryShoot(blue[0].Interaction,80f),"Launch physical shot before quarter deadline.");await Until(()=>match.Phase==MatchPhase.GoalRestart,4f);
            Check(match.BlueScore==1 && match.Clock.Remaining>0f,$"Goal counts before quarter ends and freezes remaining time: Blue={match.BlueScore}, White={match.WhiteScore}, time={match.Clock.Remaining}, phase={match.Phase}.");
            await Until(()=>match.Phase==MatchPhase.QuarterBreak,5f);Check(match.BlueScore==1 && !invalidReset,"Goal restart followed by quarter break retains score/reset invariants.");
            match.ContinueMatch();Check(match.Clock.Quarter==2,"Quarter continuation.");

            blueKeeper.enabled=true;ball.ResetForRestart(blue[0].Motor.Position+Vector3.forward*1.65f);Check(blue[0].Interaction.TryCollect(),"Possession countdown fixture.");
            match.ShotClock.Tick(5f);float before=match.ShotClock.Remaining;ball.ResetForRestart(blueKeeper.HandPosition);
            await Until(()=>ball.Owner==blueKeeper.Identity.Interaction,4f);
            Check(selection.ActivePlayer==blueKeeper.Identity && match.ShotClock.Remaining<=before && match.ShotClock.Remaining>before-1f,"Keeper catch integrates same-team clock and human selection.");
            Check(blueKeeper.GetComponent<LobPassing>().TryPass(),"Controlled keeper physically distributes a lob.");
            Check(selection.ActivePlayer.IsFieldPlayer && ball.IsAirborne,"Keeper release restores field control during real flight.");
            await Until(()=>ball.Owner!=null,4f);Check(ball.Owner.GetComponent<SwimmerIdentity>().Team==PrototypeTeam.Blue && selection.ActivePlayer.Interaction==ball.Owner,"Keeper lob received through authority and auto-selection.");

            match.RequestTurnover(PrototypeTeam.White);await Until(()=>match.IsPlaying,4f);
            Check(ball.Owner==white.Interaction && !invalidReset && !blueKeeper.HumanControlled,"Turnover clears keeper control, flight and stale actions.");
            white.Motor.ResetPose(new Vector3(0f,0f,-8f),Quaternion.Euler(0f,180f,0f));int catches=blueKeeper.Catches;
            Check(ball.TryLob(white.Interaction,Vector3.back,5f,1.3f,null),"Opponent physical lob near defended area.");await Advance(0.75f);
            Check(ball.IsAirborne && ball.PhysicsPosition.y>1f && blueKeeper.Catches==catches,"Keeper cannot catch an aerial ball above real reach.");
            match.Clock.Tick(match.Clock.Remaining-0.01f);await Until(()=>match.Phase==MatchPhase.QuarterBreak,3f);
            Check(ball.Owner==null && !ball.IsAirborne && ball.IntendedReceiver==null,"Quarter break cancels pending vertical flight.");
            match.ContinueMatch();Check(roster.Members.All(p=>Vector3.Distance(p.Motor.Position,p.StartPosition.position)<0.01f),"All eight restart after quarter movement cleanup.");

            match.RestartMatch();foreach(var k in keepers)k.enabled=true;foreach(var a in ai)a.enabled=true;
            var keyboard=InputSystem.AddDevice<Keyboard>();
            var humanTeam=UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>().Single(t=>t.Team==selection.HumanTeam);var mode=humanTeam.DefenseMode;
            try{InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Z));await Advance(0.06f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Advance(0.04f);}
            finally{InputSystem.RemoveDevice(keyboard);}
            Check(humanTeam.DefenseMode!=mode,"Z actually toggles human defense strategy.");
            int shots=ai.Sum(a=>a.ShotsExecuted);float start=Time.fixedTime;
            while(Time.fixedTime-start<16f)
            {
                await Advance(0.1f);
                Check(roster.Members.Count(p=>p.Interaction.HasBall)<=1,"Exclusive ownership in integrated play.");
                Check(selection.ActivePlayer.Team==PrototypeTeam.Blue && (selection.ActivePlayer.IsFieldPlayer || ball.Owner==selection.ActivePlayer.Interaction || !match.IsPlaying),"Valid Blue field/possessing keeper control.");
                Check(roster.Members.All(p=>Mathf.Abs(p.Motor.Position.x)<=9f && Mathf.Abs(p.Motor.Position.z)<15f && p.GetComponent<SwimmerStamina>().Value>=0f && p.GetComponent<SwimmerStamina>().Value<=1f),"All eight remain bounded with legal fatigue.");
                Check(Camera.main.GetComponent<GameplayCamera>().Target==selection.ActivePlayer.Motor,"Camera remains synchronized.");
            }
            Check(ai.Sum(a=>a.ShotsExecuted)>shots,"Live White attack executes physical shots.");
            ScreenCapture.CaptureScreenshot("Logs/Milestone9Match.png");await Advance(0.1f);
            return new {passed=true,lastSecondGoal=true,keeperClockAndLob=true,airReach=true,eightAthleteResets=resets,liveSeconds=16,liveShots=ai.Sum(a=>a.ShotsExecuted)-shots};
        }
        finally{match.Changed-=ResetObserved;match.RestartMatch();foreach(var k in keepers)k.enabled=true;foreach(var a in ai)a.enabled=true;}
    }
    public static async Task<object> Stamina()
    {
        Check(Application.isPlaying,"Stamina requires Play Mode.");var match=UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();match.RestartMatch();
        var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();var selection=UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();var ball=UnityEngine.Object.FindAnyObjectByType<BallController>();
        var ai=UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>();foreach(var a in ai)a.enabled=false;
        var keepers=UnityEngine.Object.FindObjectsByType<PrototypeGoalkeeper>();foreach(var k in keepers)k.enabled=false;
        var player=roster.FieldPlayers[0];var stamina=player.GetComponent<SwimmerStamina>();var keyboard=InputSystem.AddDevice<Keyboard>();
        try
        {
            selection.Select(player);player.Motor.ResetPose(Vector3.zero,Quaternion.identity);ball.ResetForRestart(new Vector3(-8f,ball.SurfaceHeight,-8f));await Advance(0.1f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.LeftShift));await Advance(1f);
            Check(stamina.IsSprinting && player.Motor.Velocity.magnitude>5.5f && stamina.Value<0.9f,"Actual Shift+W increases speed and consumes stamina.");
            var next=roster.FieldPlayers[1];selection.Select(next);await Advance(0.25f);
            Check(next.GetComponent<SwimmerStamina>().IsSprinting && !stamina.IsSprinting,"Held Shift transfers cleanly to the newly controlled swimmer.");selection.Select(player);await Advance(0.15f);
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));await Advance(0.5f);
            Check(!stamina.IsSprinting && player.Motor.Velocity.magnitude<4.1f,"Releasing sprint restores normal tuned speed.");
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Advance(0.2f);float before=stamina.Value;await Advance(0.3f);Check(stamina.Value>before,"Rest recovers stamina.");
            stamina.RequestSprint(true);stamina.Simulate(true,10f);Check(stamina.Value==0f && !stamina.IsSprinting,"Depletion clamps zero and ends sprint.");
            stamina.Simulate(true,0.1f);Check(!stamina.IsSprinting,"Exhaustion threshold prevents rapid sprint oscillation.");stamina.Simulate(false,3f);stamina.Simulate(true,0.1f);Check(stamina.IsSprinting,"Sufficient recovery permits sprint again.");stamina.StopSprint();
            float carried=stamina.Value;match.RequestTurnover(PrototypeTeam.White);await Until(()=>match.IsPlaying,4f);Check(stamina.Value>=carried && stamina.Value<0.8f,"Stamina persists through restart instead of resetting full.");
            match.RestartMatch();Check(roster.Members.All(p=>p.GetComponent<SwimmerStamina>().Value==1f),"New match resets all fatigue.");
            var enemy=roster.FieldPlayers.First(p=>p.Team==PrototypeTeam.White);selection.Select(player);
            foreach(var p in roster.FieldPlayers)p.Motor.ResetPose(new Vector3(p.Team==PrototypeTeam.Blue?-8f:8f,0f,-8f),Quaternion.identity);
            enemy.Motor.ResetPose(Vector3.zero,Quaternion.identity);ball.ResetForRestart(new Vector3(0f,ball.SurfaceHeight,9f));
            var team=enemy.GetComponent<PrototypeSwimmerAI>().TacticalTeam;team.ResetDecisions();enemy.GetComponent<PrototypeSwimmerAI>().enabled=true;await Advance(0.6f);
            Check(enemy.GetComponent<SwimmerStamina>().IsSprinting && enemy.GetComponent<SwimmerStamina>().Value<1f,"AI pursuit uses the same sprint drain.");
            Check(keepers.All(k=>!k.GetComponent<SwimmerStamina>().SprintAllowed),"Keepers have explicit non-sprint profile.");
            return new {passed=true,humanSprint=true,AIUsesFatigue=true,exhaustionRecovery=true,restartPersistence=true};
        }
        finally{InputSystem.RemoveDevice(keyboard);match.RestartMatch();foreach(var k in keepers)k.enabled=true;foreach(var a in ai)a.enabled=true;}
    }
    private static void Write(UnityEngine.Object target,string field,float value){var d=new SerializedObject(target);d.FindProperty(field).floatValue=value;d.ApplyModifiedPropertiesWithoutUndo();}
    private static async Task Until(Func<bool> condition,float timeout){var watch=Stopwatch.StartNew();while(!condition()){if(watch.Elapsed.TotalSeconds>timeout)throw new TimeoutException("Expected match event did not occur.");await Task.Yield();}}
    private static async Task Advance(float duration){float start=Time.fixedTime;var watch=Stopwatch.StartNew();while(Time.fixedTime-start<duration){if(watch.Elapsed.TotalSeconds>10)throw new TimeoutException("Physics not advancing.");await Task.Yield();}}
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
}
