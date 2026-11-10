using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using ToduStudios.WaterPolo;

/// <summary>Editor-driven checks outside the player build. Run Setup in Edit Mode; other entries in advancing Play Mode.</summary>
public static class Milestone11Verification
{
    public static object SourceContracts()=>new {compiled=true,matchOwnsPause=typeof(PrototypeMatch).GetProperty("IsPaused")!=null,uiNavigation=typeof(MatchMenuController).FullName};
    public static object Setup()
    {
        Check(!Application.isPlaying,"Setup requires Edit Mode.");
        var roster=Find<PrototypeRoster>();var ids=roster.Members.Select(p=>p.PlayerId).ToArray();
        ToduStudios.WaterPolo.Editor.Milestone11UISetup.SetUp();int count=Count();
        ToduStudios.WaterPolo.Editor.Milestone11UISetup.SetUp();
        Check(count==Count() && ids.SequenceEqual(roster.Members.Select(p=>p.PlayerId)) && ids.Length==14,"Repeated setup preserves objects and fourteen identities.");
        Check(UnityEngine.Object.FindObjectsByType<Canvas>().Length==1 && UnityEngine.Object.FindObjectsByType<EventSystem>().Length==1,"One Canvas and EventSystem.");
        Check(UnityEngine.Object.FindObjectsByType<MatchMenuController>().Length==1 && UnityEngine.Object.FindObjectsByType<GamePreferences>().Length==1,"One navigation and preferences component.");
        Check(Find<EventSystem>().GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>()!=null,"New Input System pointer/touch UI.");
        Check(Find<Canvas>().GetComponentsInChildren<RoundedPanelGraphic>(true).All(g=>g.GetComponent<CanvasRenderer>()!=null),"Every panel/icon has its required CanvasRenderer before Play Mode.");
        return new {passed=true,repeatSafe=true,athletes=14,objects=count};
    }
    public static async Task<object> PauseAndRules()
    {
        using(var f=new Fixture())
        {
            var keyboard=InputSystem.AddDevice<Keyboard>();
            try
            {
                f.Reset();f.Claim(f.Blue[0]);await Delay(0.1f);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));await Delay(0.15f);
                Check(f.Blue[0].GetComponent<ShotCharge>().IsCharging,"Injected Space starts a charge.");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space,Key.Escape));await Delay(0.06f);
                Check(f.Match.IsPaused && f.Menus.Page==MatchMenuPage.Pause && Time.timeScale==0f && AudioListener.pause,"Escape opens authoritative suspension.");
                Check(!f.Blue[0].GetComponent<ShotCharge>().IsCharging,"Pause cancels charge.");
                var owner=f.Ball.Owner;var positions=f.Roster.Members.Select(p=>p.Motor.Position).ToArray();var velocities=f.Roster.Members.Select(p=>p.Motor.Velocity).ToArray();
                var stamina=f.Roster.Members.Select(p=>p.GetComponent<SwimmerStamina>().Value).ToArray();
                var refs=UnityEngine.Object.FindObjectsByType<PoolsideReferee>();var refPos=refs.Select(r=>r.transform.position).ToArray();
                float clock=f.Match.Clock.Remaining,shot=f.Match.ShotClock.Remaining,deadline=f.Match.PresentationTime;
                f.Selection.SelectNext();f.Match.AdvanceLiveTime(100f);f.Match.ShotClock.Tick(100f);
                Check(!owner.TryShoot() && !f.Match.Rules.TryHold(f.White[0]),"Direct competitive APIs reject paused commands.");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Delay(1.1f);
                Near(f.Match.Clock.Remaining,clock,"Period frozen");Near(f.Match.ShotClock.Remaining,shot,"Possession frozen");Near(f.Match.PresentationTime,deadline,"Unscaled rule timeline frozen");
                Check(f.Ball.Owner==owner && f.Match.BlueScore==0 && f.Match.WhiteScore==0,"Ownership and score preserved.");
                for(int i=0;i<positions.Length;i++){Near(Vector3.Distance(positions[i],f.Roster.Members[i].Motor.Position),0f,"Athlete pose frozen");Near(Vector3.Distance(velocities[i],f.Roster.Members[i].Motor.Velocity),0f,"Velocity preserved");Near(stamina[i],f.Roster.Members[i].GetComponent<SwimmerStamina>().Value,"Stamina frozen");}
                Check(refs.Select((r,i)=>Vector3.Distance(r.transform.position,refPos[i])<0.0001f).All(x=>x),"Referees frozen.");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));await Delay(0.05f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Delay(0.05f);
                Check(!f.Match.IsPaused && Time.timeScale==1f && !AudioListener.pause && f.Ball.Owner==owner,"Resume retains possession without stale shot.");
                // Preserve a real moving aerial ball, including its Y velocity.
                f.Blue[0].Motor.ResetPose(Vector3.zero,Quaternion.LookRotation(Vector3.right));Check(owner.TryShoot(18f,ShotType.High),"High release before suspension.");
                f.Menus.OpenPause();Vector3 ballPos=f.Ball.PhysicsPosition,ballVelocity=f.Ball.Velocity;await Delay(0.7f);
                Near(Vector3.Distance(ballPos,f.Ball.PhysicsPosition),0f,"Airborne pose frozen");Near(Vector3.Distance(ballVelocity,f.Ball.Velocity),0f,"Airborne velocity preserved");
                f.Menus.Resume();await Delay(0.15f);Check(Vector3.Distance(ballPos,f.Ball.PhysicsPosition)>0.2f,"Physical flight resumes.");
                // Real off-ball hold causes exclusion and protected setup; pause must not consume either deadline.
                f.Reset();f.Park();f.Blue[0].Motor.ResetPose(Vector3.zero,Quaternion.identity);f.Blue[1].Motor.ResetPose(new Vector3(3f,0f,0f),Quaternion.identity);
                f.White[0].Motor.ResetPose(new Vector3(3f,0f,-0.9f),Quaternion.identity);f.Claim(f.Blue[0]);
                Check(f.Match.Rules.ReportContact(f.White[0],f.Blue[1],IllegalContact.Hold),"Real exclusion trigger.");
                f.Menus.OpenPause();float excluded=f.White[0].ExclusionRemaining;await Delay(1.1f);
                Check(f.Match.Phase==MatchPhase.FreeThrowSetup,"Setup does not advance while paused.");Near(f.White[0].ExclusionRemaining,excluded,"Exclusion live time frozen");
                Check(!f.Ball.TryAwardRestart(f.Blue[0].Interaction),"Paused restart cannot reacquire ownership.");
                f.Menus.Resume();await Until(()=>f.Match.Phase==MatchPhase.FreeThrowReady);
                f.Menus.OpenPause();await Delay(0.8f);Check(!f.Blue[0].GetComponent<SwimmerPassing>().TryPass(),"Protected free throw cannot execute from paused API.");
                f.Menus.Resume();Check(f.Blue[0].GetComponent<SwimmerPassing>().TryPass(),"Free throw resumes through shared pass API.");
                // A pending turnover restart also retains its unscaled delay.
                f.Match.RequestTurnover(PrototypeTeam.White);f.Menus.OpenPause();await Delay(1.3f);
                Check(f.Match.Phase==MatchPhase.TurnoverRestart,"Paused turnover cannot restart in background.");f.Menus.Resume();await Until(()=>f.Match.IsPlaying);
                // Qualifying penalty setup and physical flight use the same suspension timeline.
                f.Reset();f.Park();var goal=f.Team(PrototypeTeam.Blue).AttackingGoal;Vector3 spot=goal.transform.position-goal.transform.forward*5f;spot.y=0f;
                f.Blue[0].Motor.ResetPose(spot,Quaternion.LookRotation(goal.transform.forward));f.White[0].Motor.ResetPose(spot-goal.transform.forward*0.9f,Quaternion.LookRotation(goal.transform.forward));f.Claim(f.Blue[0]);
                Check(f.Blue[0].GetComponent<ShotCharge>().BeginCharge(),"Penalty shooting intent.");Check(f.Match.Rules.ReportContact(f.White[0],f.Blue[0],IllegalContact.Push) && f.Match.Phase==MatchPhase.PenaltySetup,"Qualifying foul awards penalty.");
                f.Menus.OpenPause();await Delay(1.1f);Check(f.Match.Phase==MatchPhase.PenaltySetup,"Penalty setup frozen.");f.Menus.Resume();await Until(()=>f.Match.Phase==MatchPhase.PenaltyReady);
                Check(f.Blue[0].Interaction.TryShoot(10f),"Penalty physical release.");f.Menus.OpenPause();ballPos=f.Ball.PhysicsPosition;await Delay(3.2f);
                Check(f.Match.Phase==MatchPhase.PenaltyFlight && f.Match.BlueScore==0,"Penalty timeout/scoring frozen.");Near(Vector3.Distance(ballPos,f.Ball.PhysicsPosition),0f,"Penalty ball frozen");
                f.Menus.Resume();await Until(()=>f.Match.Phase==MatchPhase.GoalRestart);Check(f.Match.BlueScore==1,"Resumed physical penalty scores once.");
                f.Menus.OpenPause();await Delay(1.8f);Check(f.Match.Phase==MatchPhase.GoalRestart && f.Match.BlueScore==1,"Goal restart and score stable during pause.");
                f.Menus.Resume();await Until(()=>f.Match.IsPlaying);Check(f.Ball.LastReleaser?.GetComponent<SwimmerIdentity>().Team==PrototypeTeam.White && f.Ball.ReleaseKind==BallReleaseKind.Pass,"Conceding team puts the physical centre pass into play once.");
                return new {passed=true,escape=true,chargeCancelled=true,posesAndVelocity=true,clocks=true,exclusion=true,freeThrow=true,penalty=true,goalRestart=true};
            }
            finally{InputSystem.RemoveDevice(keyboard);}
        }
    }
    public static async Task<object> MenusSettingsAndTransitions()
    {
        using(var f=new Fixture())
        {
            var prefs=Find<GamePreferences>();bool had=PlayerPrefs.HasKey(GamePreferences.StorageKey);string saved=PlayerPrefs.GetString(GamePreferences.StorageKey,"");
            try
            {
                f.Reset();await Delay(0.1f);await Click(f,"MatchSafeArea/PauseButton");Check(f.Match.IsPaused,"Pointer Pause button works.");
                await Click(f,"MenuOverlay/MenuSafeArea/PausePanel/Settings");Check(f.Menus.Page==MatchMenuPage.Settings,"Settings button navigation.");
                var settings=f.Canvas.transform.Find("MenuOverlay/MenuSafeArea/SettingsPanel");
                settings.Find("masterRow/Slider").GetComponent<Slider>().value=0.63f;settings.Find("sfxRow/Slider").GetComponent<Slider>().value=0.42f;
                settings.Find("zoomRow/Slider").GetComponent<Slider>().value=1.17f;settings.Find("smoothingRow/Slider").GetComponent<Slider>().value=1.3f;
                await Click(f,"MenuOverlay/MenuSafeArea/SettingsPanel/Low");Check(Find<WaterInteractionEffects>().Quality==WaterEffectsQuality.Off,"Low applies effects preset.");
                Near(AudioListener.volume,0.63f,"Master functional");Near(Find<MatchAtmosphere>().SfxVolume,0.42f,"SFX functional");Near(Find<GameplayCamera>().UserZoom,1.17f,"Camera distance functional");Near(Find<GameplayCamera>().UserSmoothing,1.3f,"Camera smoothing functional");
                Check(!settings.Find("crowdRow").gameObject.activeSelf || prefs.SupportsCrowd,"Unsupported crowd slider hidden.");
                await Click(f,"MenuOverlay/MenuSafeArea/SettingsPanel/Back");Check(f.Menus.Page==MatchMenuPage.Pause && f.Match.IsPaused,"Settings return keeps match paused.");
                prefs.Reload();Near(prefs.Current.master,0.63f,"Preferences persisted and reloaded");Check(prefs.Current.graphics==MatchGraphicsQuality.Low,"Graphics persistence.");
                await Click(f,"MenuOverlay/MenuSafeArea/PausePanel/Restart");Check(f.Menus.Page==MatchMenuPage.RestartConfirmation,"Restart confirmation opens.");
                await Click(f,"MenuOverlay/MenuSafeArea/ConfirmationPanel/Cancel");Check(f.Menus.Page==MatchMenuPage.Pause && f.Match.IsPaused,"Restart can cancel.");
                f.Menus.AskExit();f.Menus.Back();Check(f.Menus.Page==MatchMenuPage.Pause && f.Match.IsPaused,"Exit can cancel through Back.");
                f.Menus.AskRestart();await Click(f,"MenuOverlay/MenuSafeArea/ConfirmationPanel/Confirm");
                Check(f.Match.Clock.Quarter==1 && f.Match.BlueScore==0 && f.Match.WhiteScore==0 && !f.Match.IsPaused && !f.Match.EndsSwitched && f.Match.IsPlaying,"Confirmed restart restores Q1 and original ends.");Near(prefs.Current.zoom,1.17f,"Restart keeps settings");
                // Existing period APIs and UI Continue perform the real end swap exactly once.
                f.Match.ShotClock.ResetPossession();f.Ball.ResetForRestart(Vector3.zero);f.Match.AdvanceLiveTime(80f);
                Check(f.Menus.Page==MatchMenuPage.Transition,"Quarter transition card.");f.Menus.OpenSettings();f.Menus.Cancel();f.Menus.Continue();
                f.Match.AdvanceLiveTime(80f);Check(f.Match.Phase==MatchPhase.HalfTime,"Halftime state.");f.Menus.Continue();Check(f.Match.EndsSwitched && f.Match.EndSwitchCount==1 && f.Match.Clock.Quarter==3,"Continue swaps ends once.");
                f.Match.AdvanceLiveTime(80f);f.Menus.Continue();f.Match.AdvanceLiveTime(80f);
                Check(f.Match.Phase==MatchPhase.FullTime && f.Menus.Page==MatchMenuPage.Transition && Read<TMP_Text>(f.Menus,"transitionTitle").text=="DRAW","Full-time draw presented.");
                f.Menus.AskRestart();f.Menus.Confirm();Check(!f.Match.EndsSwitched && f.Match.Clock.Quarter==1,"Post-halftime restart restores original ends.");
                f.Menus.OpenPause();f.Menus.AskExit();f.Menus.Confirm();Check(f.Match.Phase==MatchPhase.PreMatch && f.Menus.Page==MatchMenuPage.Landing && Time.timeScale==1f && !AudioListener.pause,"Exit safely returns to landing.");
                float clock=f.Match.Clock.Remaining;await Delay(0.3f);Near(f.Match.Clock.Remaining,clock,"Landing is stopped");
                await Click(f,"MenuOverlay/MenuSafeArea/LandingPanel/Start");Check(f.Match.IsPlaying && f.Menus.Page==MatchMenuPage.None,"Landing Start returns to match.");
                // Restart must also clear actual exclusion, stamina and permanent-removal history.
                f.Park();f.Blue[0].Motor.ResetPose(Vector3.zero,Quaternion.identity);f.Blue[1].Motor.ResetPose(Vector3.right*3f,Quaternion.identity);f.White[0].Motor.ResetPose(new Vector3(3f,0f,-0.9f),Quaternion.identity);f.Claim(f.Blue[0]);
                Check(f.Match.Rules.ReportContact(f.White[0],f.Blue[1],IllegalContact.Hold),"Restart history fixture.");
                f.Menus.OpenPause();f.Menus.AskRestart();f.Menus.Confirm();Check(f.Roster.Members.All(p=>p.PersonalFouls==0 && p.ExclusionRemaining==0f && p.EligibleForPlay && p.GetComponent<Collider>().enabled && p.GetComponent<SwimmerStamina>().Value==1f),"Restart restores fouls, eligibility, colliders and stamina.");
                return new {passed=true,pointerButtons=true,confirmCancel=true,restart=true,exitLanding=true,settingsPersistence=true,halftime=true,fullTime=true};
            }
            finally{if(had)PlayerPrefs.SetString(GamePreferences.StorageKey,saved);else PlayerPrefs.DeleteKey(GamePreferences.StorageKey);PlayerPrefs.Save();prefs.Reload();}
        }
    }
    public static async Task<object> LiveAIPause()
    {
        Check(Application.isPlaying,"Runtime required.");var match=Find<PrototypeMatch>();var roster=Find<PrototypeRoster>();var ball=Find<BallController>();var menus=Find<MatchMenuController>();
        try
        {
            match.RestartMatch();await Delay(1.1f);var ai=UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>();
            Check(ai.All(a=>a.enabled),"Integration uses enabled AI.");menus.OpenPause();
            var states=ai.Select(a=>a.Behaviour).ToArray();var decisions=ai.Select(a=>Field<float>(a,"nextDecision")).ToArray();var poses=roster.Members.Select(p=>p.Motor.Position).ToArray();var ballPos=ball.PhysicsPosition;
            await Delay(0.7f);Check(states.SequenceEqual(ai.Select(a=>a.Behaviour)) && decisions.SequenceEqual(ai.Select(a=>Field<float>(a,"nextDecision"))),"AI decisions preserved rather than reset by suspension.");
            Check(poses.Select((p,i)=>Vector3.Distance(p,roster.Members[i].Motor.Position)<0.0001f).All(x=>x) && Vector3.Distance(ballPos,ball.PhysicsPosition)<0.0001f,$"Live AI and ball poses preserved. ballDelta={Vector3.Distance(ballPos,ball.PhysicsPosition)}, maxSwimmerDelta={poses.Select((p,i)=>Vector3.Distance(p,roster.Members[i].Motor.Position)).Max()}, owner={ball.Owner}, phase={match.Phase}");
            menus.Resume();float remaining=match.Clock.Remaining;await Delay(0.5f);Check(match.Clock.Remaining<remaining,"Live clock resumes with AI.");
            return new {passed=true,liveAI=true,decisionsPreserved=true,resume=true};
        }
        finally{match.RestartMatch();}
    }
    public static async Task<object> MobileButtons()
    {
        using(var f=new Fixture())
        {
            var keyboard=InputSystem.AddDevice<Keyboard>();
            try
            {
                f.Reset();f.Park();var p=f.Blue[0];p.Motor.ResetPose(Vector3.zero,Quaternion.LookRotation(Vector3.right));f.Blue[1].Motor.ResetPose(Vector3.right*6f,Quaternion.identity);f.Claim(p);await Delay(0.1f);
                var controls=Find<MobileMatchControls>();var stick=Find<VirtualSwimJoystick>();var r=stick.GetComponent<RectTransform>();
                var data=new PointerEventData(EventSystem.current){pointerId=21,button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(new Vector3(r.rect.width*0.32f,0f,0f)))};
                ExecuteEvents.Execute(stick.gameObject,data,ExecuteEvents.pointerDownHandler);await Delay(0.3f);Check(stick.Value.x>0.9f && p.Motor.Velocity.x>1f,"Continuous joystick enters existing swimmer input.");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));await Delay(0.7f);Check(p.Motor.Velocity.z>1f && Mathf.Abs(p.Motor.Velocity.x)<0.2f,$"Keyboard takes priority over held joystick after physical turning. velocity={p.Motor.Velocity}, input={Field<InputAction>(p.GetComponent<SwimmerInput>(),"moveAction").ReadValue<Vector2>()}");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                var sprint=Action(f,MobileMatchAction.Sprint);Down(sprint,22);await Delay(0.3f);Check(p.GetComponent<SwimmerStamina>().IsSprinting && controls.SprintHeld && p.GetComponent<SwimmerStamina>().Value<1f,"Sprint pointer hold consumes shared stamina.");
                Up(sprint,22);ExecuteEvents.Execute(stick.gameObject,data,ExecuteEvents.pointerUpHandler);await Delay(0.35f);Check(!controls.SprintHeld && stick.Value==Vector2.zero && p.Motor.Velocity.magnitude<0.1f,"Release stops sprint and recentres joystick.");
                p.Motor.ResetPose(Vector3.zero,Quaternion.LookRotation(Vector3.right));f.Claim(p);await Delay(0.1f);
                var shoot=Action(f,MobileMatchAction.Shoot);Down(shoot,23);await Delay(0.3f);Check(p.GetComponent<ShotCharge>().IsCharging,"Pointer down starts real charge.");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));await Delay(0.1f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Delay(0.1f);Check(p.GetComponent<ShotCharge>().IsCharging,"Keyboard release cannot steal pointer-owned charge.");
                Up(shoot,23);Check(f.Ball.Owner==null && f.Ball.ReleaseKind==BallReleaseKind.Shot && f.Ball.Velocity.magnitude>14f,"Pointer up executes charged physical shot.");
                f.Reset();f.Park();p.Motor.ResetPose(Vector3.zero,Quaternion.LookRotation(Vector3.right));f.Blue[1].Motor.ResetPose(Vector3.right*6f,Quaternion.identity);f.Claim(p);await Delay(0.1f);
                Down(Action(f,MobileMatchAction.Pass),24);Check(f.Ball.Owner==null && f.Ball.ReleaseKind==BallReleaseKind.Pass,"PASS enters physical passing API.");Up(Action(f,MobileMatchAction.Pass),24);await Until(()=>f.Ball.Owner!=null);Check(f.Ball.Owner==f.Blue[1].Interaction && f.Selection.ActivePlayer==f.Blue[1],"Physical friendly reception transfers human control.");
                f.Reset();f.Park();p.Motor.ResetPose(Vector3.zero,Quaternion.LookRotation(Vector3.right));f.Blue[1].Motor.ResetPose(Vector3.right*6f,Quaternion.identity);f.Claim(p);await Delay(0.1f);
                Down(Action(f,MobileMatchAction.Lob),25);Check(f.Ball.IsAirborne && f.Ball.ReleaseKind==BallReleaseKind.Lob,"LOB uses real aerial API.");
                f.Reset();f.Claim(p);await Delay(0.1f);var owner=f.Ball.Owner;Down(Action(f,MobileMatchAction.Switch),26);await Delay(0.05f);Check(f.Selection.ActivePlayer!=p && f.Selection.ActivePlayer.Team==PrototypeTeam.Blue && f.Ball.Owner==owner,"SWITCH changes control without possession.");
                f.Reset();f.Park();p.Motor.ResetPose(Vector3.zero,Quaternion.identity);f.White[0].Motor.ResetPose(Vector3.forward*0.9f,Quaternion.LookRotation(Vector3.back));f.Claim(f.White[0]);f.Selection.Select(p);await Delay(1.1f);
                Down(Action(f,MobileMatchAction.Steal),27);Check(p.GetComponent<SwimmerSteal>().LastResult==StealResult.Success && f.Ball.Owner==p.Interaction,"STEAL uses range/facing/cooldown/ownership checks.");
                var defense=f.Team(PrototypeTeam.Blue).DefenseMode;await Delay(0.05f);Down(Action(f,MobileMatchAction.Strategy),28);Check(f.Team(PrototypeTeam.Blue).DefenseMode!=defense,"On-screen defense uses shared strategy command.");
                f.Reset();f.Park();p.Motor.ResetPose(Vector3.zero,Quaternion.LookRotation(Vector3.right));f.Claim(p);await Delay(0.1f);
                var mode=Action(f,MobileMatchAction.ShotMode);for(int i=0;i<3 && controls.ShotMode!=ShotType.Skip;i++){Down(mode,29);Up(mode,29);}Down(Action(f,MobileMatchAction.Shoot),30);await Delay(0.2f);Up(Action(f,MobileMatchAction.Shoot),30);Check(f.Ball.CurrentShot==ShotType.Skip,"Compact mode control selects real skip configuration.");
                f.Reset();f.Claim(p);await Delay(0.1f);Down(Action(f,MobileMatchAction.Shoot),31);f.Menus.OpenPause();await Delay(0.1f);Up(shoot,31);Check(f.Ball.Owner==p.Interaction && !p.GetComponent<ShotCharge>().IsCharging && !controls.Begin(MobileMatchAction.Pass),"Pause cancels holds and rejects UI gameplay commands.");f.Menus.Resume();await Delay(0.1f);Check(f.Ball.Owner==p.Interaction,"Pointer release around resume cannot replay a shot.");
                var keeper=f.Roster.Members.Single(k=>k.Team==PrototypeTeam.Blue && !k.IsFieldPlayer);f.Claim(keeper);await Delay(0.05f);Check(f.Selection.ActivePlayer==keeper && controls.CanMove && controls.CanUse(MobileMatchAction.Pass) && !controls.CanUse(MobileMatchAction.Sprint),"Legitimate Blue keeper possession retains human control and distribution, without field sprint.");
                return new {passed=true,joystick=true,keyboardPriority=true,sprintHold=true,shootHoldRelease=true,physicalPassReception=true,lob=true,switchWithoutPossession=true,steal=true,strategy=true,skipMode=true,pausedCommandsRejected=true};
            }
            finally{InputSystem.RemoveDevice(keyboard);}
        }
    }
    public static async Task<object> CentreRestarts()
    {
        using(var f=new Fixture())
        {
            var keyboard=InputSystem.AddDevice<Keyboard>();int signals=0;void Signal(SwimmerIdentity p){signals++;}f.Match.CentreRestartSignalled+=Signal;
            try
            {
                foreach(bool reversed in new[]{false,true})
                {
                    f.Reset();if(reversed)Half(f);f.Park();signals=0;
                    await Score(f,f.Blue[0]);float stopped=f.Match.Clock.Remaining;
                    await Until(()=>f.Match.Phase==MatchPhase.CentreRestartSetup);
                    Check(f.Match.CentreTaker.Team==PrototypeTeam.White && f.Ball.Owner==f.Match.CentreTaker.Interaction,"White conceding team owns centre setup.");
                    var taker=f.Match.CentreTaker;var receiver=f.Match.CentreReceiver;
                    Check(Vector3.ProjectOnPlane(taker.Motor.Position,Vector3.up).magnitude<0.1f,"Designated taker is at pool centre.");
                    Check(f.Roster.FieldPlayers.Where(p=>p!=taker && p.EligibleForPlay).All(p=>Vector3.Dot(p.Motor.Position,f.Team(p.Team).AttackDirection)<=0.01f),"Fields return to their own half using current attacking directions.");
                    await Until(()=>f.Match.Phase==MatchPhase.CentreRestartReady);var positions=f.Roster.Members.Select(p=>p.Motor.Position).ToArray();await Delay(0.12f);
                    Check(positions.Select((p,i)=>Vector3.Distance(p,f.Roster.Members[i].Motor.Position)<0.01f).All(x=>x),"No premature movement in ready formation.");Near(f.Match.Clock.Remaining,stopped,"Clock stays stopped through preparation");
                    await Until(()=>f.Match.IsPlaying);Check(signals==1 && f.Match.CentreSignals==1 && f.Ball.LastReleaser==taker.Interaction && f.Ball.Owner==null && f.Ball.ReleaseKind==BallReleaseKind.Pass && f.Ball.Velocity.magnitude>1f,"White AI makes one genuine physical centre pass after one signal.");
                    await Until(()=>f.Ball.Owner!=null);Check(f.Ball.Owner==receiver.Interaction,"AI centre ball is acquired only at physical reception.");Check(f.Match.BlueScore==1 && f.Match.WhiteScore==0,"Goal counted once through centre restart.");
                    f.Reset();if(reversed)Half(f);f.Park();signals=0;await Score(f,f.White[0]);stopped=f.Match.Clock.Remaining;
                    await Until(()=>f.Match.Phase==MatchPhase.CentreRestartReady);await Delay(0.08f);taker=f.Match.CentreTaker;receiver=f.Match.CentreReceiver;
                    Check(taker.Team==PrototypeTeam.Blue && f.Selection.ActivePlayer==taker && f.Ball.Owner==taker.Interaction,"Blue conceding team automatically controls centre taker.");
                    var controls=Find<MobileMatchControls>();Check(Action(f,MobileMatchAction.Pass).gameObject.activeInHierarchy && controls.CanUse(MobileMatchAction.Pass) && !controls.CanUse(MobileMatchAction.Shoot) && !controls.CanMove && !controls.CanUse(MobileMatchAction.Switch),"Protected restart exposes PASS and blocks shooting/movement/switching.");
                    positions=f.Roster.Members.Select(p=>p.Motor.Position).ToArray();var active=f.Selection.ActivePlayer;f.Selection.SelectNext();Check(f.Selection.ActivePlayer==active,"Keyboard selection cannot bypass designated taker.");
                    foreach(var ai in UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>())ai.enabled=true;
                    await Delay(6.3f);Check(f.Match.Phase==MatchPhase.CentreRestartReady && f.Ball.Owner==taker.Interaction && signals==1,"Human restart never auto-passes, even beyond the free-throw fallback deadline.");
                    ScreenCapture.CaptureScreenshot($"Logs/UIFix-Centre-Q{f.Match.Clock.Quarter}.png");await Delay(0.12f);
                    Check(positions.Select((p,i)=>Vector3.Distance(p,f.Roster.Members[i].Motor.Position)<0.01f).All(x=>x),"Enabled AI remains in restart formation.");Near(f.Match.Clock.Remaining,stopped,"Period waits for manual pass");Near(f.Match.ShotClock.Remaining,28f,"Shot clock reset but not consumed");
                    f.Menus.OpenPause();await Delay(0.3f);Check(!controls.Begin(MobileMatchAction.Pass) && f.Match.Phase==MatchPhase.CentreRestartReady,"Pause remains functional during centre restart.");f.Menus.Resume();await Delay(0.05f);
                    if(reversed){Down(Action(f,MobileMatchAction.Pass),71);Up(Action(f,MobileMatchAction.Pass),71);}
                    else{InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.B));await Delay(0.05f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());}
                    Check(f.Match.IsPlaying && f.Ball.LastReleaser==taker.Interaction && f.Ball.ReleaseKind==BallReleaseKind.Pass && f.Match.WhiteScore==1,"Manual B/UI pass legally releases competition after either end orientation.");
                    await Delay(0.1f);Check(f.Match.Clock.Remaining<stopped && f.Match.ShotClock.Remaining<28f && f.Match.ShotClock.PossessionTeam==PrototypeTeam.Blue,"Both clocks begin only after the legal release.");
                    foreach(var ai in UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>())ai.enabled=false;
                }
                return new {passed=true,humanBlueManual=true,whiteAIPhysical=true,signalsExactlyOnce=true,ownHalfFormations=true,quarterOneAndThree=true,halftimeDirections=true,protectedControls=true,clocks=true,exclusivePhysicalReception=true};
            }
            finally{f.Match.CentreRestartSignalled-=Signal;InputSystem.RemoveDevice(keyboard);}
        }
    }
    public static async Task<object> MousePause()
    {
        using(var f=new Fixture())
        {
            var mouse=InputSystem.AddDevice<Mouse>();
            try
            {
                f.Reset();await Delay(0.12f);var target=f.Canvas.transform.Find("MatchSafeArea/PauseButton").GetComponent<RectTransform>();
                Check(target.gameObject.activeInHierarchy && target.rect.width>=96f,"Visible mobile-size Pause control.");
                Vector2 point=RectTransformUtility.WorldToScreenPoint(null,target.TransformPoint(target.rect.center));
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point});await Delay(0.12f);
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point}.WithButton(MouseButton.Left));await Delay(0.12f);
                InputSystem.QueueStateEvent(mouse,new MouseState{position=point});await Delay(0.12f);
                Check(f.Match.IsPaused && f.Menus.Page==MatchMenuPage.Pause,"Injected mouse reaches Pause through the actual InputSystemUIInputModule.");
                await Click(f,"MenuOverlay/MenuSafeArea/PausePanel/Resume");Check(f.Match.IsPlaying,"Resume pointer path.");
                return new {passed=true,actualInputSystemMouse=true,pauseVisible=true,pauseAndResume=true};
            }
            finally{InputSystem.RemoveDevice(mouse);}
        }
    }
    private static void Half(Fixture f)
    {f.Match.ShotClock.ResetPossession();f.Ball.ResetForRestart(Vector3.zero);f.Match.AdvanceLiveTime(80f);f.Match.ContinueMatch();f.Match.AdvanceLiveTime(80f);f.Match.ContinueMatch();Check(f.Match.Clock.Quarter==3 && f.Match.EndsSwitched,"Q3 end switch fixture.");}
    private static async Task Score(Fixture f,SwimmerIdentity scorer)
    {
        var goal=f.Team(scorer.Team).AttackingGoal;var point=goal.transform.position-goal.transform.forward*5f;point.y=0f;
        scorer.Motor.ResetPose(point,Quaternion.LookRotation(goal.transform.forward));f.Claim(scorer);Check(scorer.Interaction.TryShoot(16f,ShotType.Normal),"Score fixture releases real shot.");await Until(()=>f.Match.Phase==MatchPhase.GoalRestart);
    }
    private static GameplayActionButton Action(Fixture f,MobileMatchAction action)=>f.Canvas.GetComponentsInChildren<GameplayActionButton>(true).Single(b=>b.Action==action);
    private static void Down(GameplayActionButton b,int id)
    {
        Check(b.gameObject.activeInHierarchy && b.Available,"Action visible/available: "+b.Action);
        var rect=b.GetComponent<RectTransform>();var data=new PointerEventData(EventSystem.current){pointerId=id,button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);Check(hits.Any(h=>h.gameObject==b.gameObject),"Action raycast reaches "+b.Action);
        ExecuteEvents.Execute(b.gameObject,data,ExecuteEvents.pointerDownHandler);
    }
    private static void Up(GameplayActionButton b,int id)=>ExecuteEvents.Execute(b.gameObject,new PointerEventData(EventSystem.current){pointerId=id,button=PointerEventData.InputButton.Left},ExecuteEvents.pointerUpHandler);
    public static async Task<object> ActualGameViewLayouts()
    {
        using(var f=new Fixture())
        {
            const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static;
            var asm=typeof(UnityEditor.Editor).Assembly;var type=asm.GetType("UnityEditor.GameView");var view=EditorWindow.GetWindow(type);
            var indexProperty=type.GetProperty("selectedSizeIndex",flags);int original=(int)indexProperty.GetValue(view);
            var sizesType=asm.GetType("UnityEditor.GameViewSizes");var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes=singleton.GetProperty("instance",flags).GetValue(null);var group=sizesType.GetMethod("GetGroup",flags).Invoke(sizes,new[]{type.GetProperty("currentSizeGroupType",flags).GetValue(view)});var groupType=group.GetType();
            int count=(int)groupType.GetMethod("GetTotalCount").Invoke(group,null);var added=new List<int>();var results=new List<object>();
            var safes=f.Canvas.GetComponentsInChildren<SafeAreaLayout>(true);var safeStates=safes.Select(s=>s.enabled).ToArray();
            try
            {
                foreach(int width in new[]{1920,2340,2400})
                {
                    var size=Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),width,1080,"M11 verification"});
                    groupType.GetMethod("AddCustomSize").Invoke(group,new[]{size});int selected=(int)groupType.GetMethod("GetTotalCount").Invoke(group,null)-1;added.Add(selected);
                    indexProperty.SetValue(view,selected);view.Repaint();await Delay(0.4f);
                    Check(Screen.width==width && Screen.height==1080,$"Actual Game view render size is {width}x1080, observed {Screen.width}x{Screen.height}.");
                    foreach(var safe in safes){safe.enabled=false;safe.Apply(new Rect(70f,24f,width-140f,1032f),new Vector2(width,1080f));}
                    foreach(string page in new[]{"HUD","Pause","Settings","Confirmation","Landing","Transition"})
                    {
                        f.Reset();f.Park();f.Claim(f.Blue[0]);
                        if(page!="HUD")f.Menus.OpenPause();
                        if(page=="Settings")f.Menus.OpenSettings();
                        else if(page=="Confirmation")f.Menus.AskRestart();
                        else if(page=="Landing"){f.Menus.AskExit();f.Menus.Confirm();}
                        else if(page=="Transition"){f.Menus.Resume();f.Ball.ResetForRestart(Vector3.zero);f.Match.ShotClock.ResetPossession();f.Match.AdvanceLiveTime(80f);}
                        await Delay(0.16f);Canvas.ForceUpdateCanvases();
                        var corners=new Vector3[4];var targets=new List<RectTransform>();
                        targets.AddRange(f.Canvas.GetComponentsInChildren<Button>().Select(b=>b.GetComponent<RectTransform>()));
                        targets.AddRange(f.Canvas.GetComponentsInChildren<GameplayActionButton>().Select(b=>b.GetComponent<RectTransform>()));
                        targets.AddRange(f.Canvas.GetComponentsInChildren<VirtualSwimJoystick>().Select(b=>b.GetComponent<RectTransform>()));
                        foreach(var r in targets)
                        {
                            if(!r.gameObject.activeInHierarchy)continue;r.GetWorldCorners(corners);
                            foreach(var p in corners){var point=RectTransformUtility.WorldToScreenPoint(null,p);Check(point.x>=69f && point.x<=width-69f && point.y>=23f && point.y<=1057f,$"{width} {page}: {r.name} stays in safe area.");}
                            Check(r.rect.height>=64f || r.name=="PauseButton",$"Usable interaction size: {r.name}");
                        }
                        var overflow=new List<string>();foreach(var t in f.Canvas.GetComponentsInChildren<TMP_Text>())
                        {t.ForceMeshUpdate();if(t.isTextOverflowing)overflow.Add(t.name+":"+t.text);}
                        Check(overflow.Count==0,$"{width} {page}: text fits: "+string.Join("; ",overflow));
                        foreach(var graphic in f.Canvas.GetComponentsInChildren<RoundedPanelGraphic>())
                        {var mesh=graphic.canvasRenderer.GetMesh();Check(mesh!=null && mesh.vertexCount>0,$"Rendered panel/icon mesh: {graphic.name}");}
                        ScreenCapture.CaptureScreenshot($"Logs/UIFix-{width}-{page}.png");await Delay(0.12f);
                        results.Add(new {width,height=1080,page,actualGameView=true,safeInsets=true,textFits=true,renderedPanelMeshes=true});
                    }
                }
                return new {passed=true,layouts=results};
            }
            finally
            {
                indexProperty.SetValue(view,original);
                for(int i=added.Count-1;i>=0;i--)groupType.GetMethod("RemoveCustomSize").Invoke(group,new object[]{added[i]});
                Check((int)groupType.GetMethod("GetTotalCount").Invoke(group,null)==count,"Temporary Game view sizes removed.");
                for(int i=0;i<safes.Length;i++){safes[i].Apply(Screen.safeArea,new Vector2(Screen.width,Screen.height));safes[i].enabled=safeStates[i];}view.Repaint();
            }
        }
    }
    public static async Task<object> LandscapeLayouts()
    {
        using(var f=new Fixture())
        {
            var canvas=f.Canvas;var rect=canvas.GetComponent<RectTransform>();var scaler=canvas.GetComponent<CanvasScaler>();
            var renderMode=canvas.renderMode;var worldCamera=canvas.worldCamera;var position=rect.position;var rotation=rect.rotation;var scale=rect.localScale;var size=rect.sizeDelta;
            float factor=canvas.scaleFactor;bool scalerEnabled=scaler.enabled;
            var safes=canvas.GetComponentsInChildren<SafeAreaLayout>(true);var safeStates=safes.Select(s=>s.enabled).ToArray();
            var cameraObject=new GameObject("M11LayoutVerificationCamera");var lens=cameraObject.AddComponent<Camera>();
            lens.orthographic=true;lens.orthographicSize=540f;lens.clearFlags=CameraClearFlags.SolidColor;lens.backgroundColor=new Color(0.04f,0.1f,0.16f);lens.nearClipPlane=0.1f;lens.farClipPlane=1100f;
            var reports=new List<object>();
            try
            {
                f.Reset();f.Menus.OpenPause();scaler.enabled=false;foreach(var s in safes)s.enabled=false;
                canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=lens;rect.position=new Vector3(0f,1000f,0f);rect.rotation=Quaternion.identity;rect.localScale=Vector3.one;
                lens.transform.position=new Vector3(0f,1000f,-1000f);
                foreach(int width in new[]{1920,2340,2400})
                {
                    rect.sizeDelta=new Vector2(width,1080f);lens.aspect=(float)width/1080f;
                    // Both edges inset: simulates notches/system cutouts without claiming physical-device testing.
                    var safe=new Rect(70f,24f,width-140f,1032f);foreach(var s in safes)s.Apply(safe,new Vector2(width,1080f));
                    var rt=new RenderTexture(width,1080,24);lens.targetTexture=rt;
                    try
                    {
                        foreach(string page in new[]{"Pause","Settings","Confirmation","Landing","Transition","HUD"})
                        {
                            f.Menus.Resume();f.Match.RestartMatch();f.Menus.OpenPause();
                            if(page=="Settings")f.Menus.OpenSettings();
                            else if(page=="Confirmation")f.Menus.AskExit();
                            else if(page=="Landing"){f.Menus.AskExit();f.Menus.Confirm();}
                            else if(page=="Transition"){f.Menus.Resume();f.Match.AdvanceLiveTime(80f);}
                            else if(page=="HUD")f.Menus.Resume();
                            await Delay(0.14f);Canvas.ForceUpdateCanvases();
                            var corners=new Vector3[4];int buttons=0;
                            foreach(var b in canvas.GetComponentsInChildren<Button>())
                            {
                                if(!b.gameObject.activeInHierarchy)continue;buttons++;var r=b.GetComponent<RectTransform>();r.GetWorldCorners(corners);
                                foreach(var p in corners){Vector3 local=rect.InverseTransformPoint(p);Check(local.x>=-width*0.5f+69f && local.x<=width*0.5f-69f && local.y>=-517f && local.y<=517f,$"{width} {page}: button inside safe area: {b.name}");}
                                Check(r.rect.height>=64f,$"{page}: usable button height: {b.name}");
                            }
                            var overflow=new List<string>();
                            foreach(var t in canvas.GetComponentsInChildren<TMP_Text>())
                            {if(!t.gameObject.activeInHierarchy)continue;t.ForceMeshUpdate();if(t.isTextOverflowing)overflow.Add(t.name+":"+t.text);}
                            Check(overflow.Count==0,$"{width} {page}: text must fit: "+string.Join("; ",overflow));
                            // Capture actual uGUI meshes through a temporary orthographic camera. Restored below; no scene asset is saved.
                            lens.Render();var prior=RenderTexture.active;RenderTexture.active=rt;
                            var image=new Texture2D(width,1080,TextureFormat.RGB24,false);
                            try{image.ReadPixels(new Rect(0f,0f,width,1080f),0,0);image.Apply();System.IO.File.WriteAllBytes($"Logs/M11-{width}-{page}.png",image.EncodeToPNG());}
                            finally{RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(image);}
                            reports.Add(new {width,height=1080,page,buttons,safeArea=true,textFits=true});
                        }
                    }
                    finally{lens.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
                }
                return new {passed=true,layouts=reports,rendered=true,physicalDevice=false};
            }
            finally
            {
                canvas.renderMode=renderMode;canvas.worldCamera=worldCamera;rect.position=position;rect.rotation=rotation;rect.localScale=scale;rect.sizeDelta=size;canvas.scaleFactor=factor;scaler.enabled=scalerEnabled;
                for(int i=0;i<safes.Length;i++){safes[i].Apply(Screen.safeArea,new Vector2(Screen.width,Screen.height));safes[i].enabled=safeStates[i];}
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }
    }
    private sealed class Fixture:IDisposable
    {
        public PrototypeMatch Match=Find<PrototypeMatch>();public BallController Ball=Find<BallController>();public PrototypeRoster Roster=Find<PrototypeRoster>();public PlayerSelection Selection=Find<PlayerSelection>();public MatchMenuController Menus=Find<MatchMenuController>();public Canvas Canvas=Find<MatchHUD>().GetComponent<Canvas>();
        private Behaviour[] ai;private bool[] enabled;public SwimmerIdentity[] Blue,White;
        public Fixture(){Check(Application.isPlaying,"Runtime check requires Play Mode.");ai=UnityEngine.Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>().Concat(UnityEngine.Object.FindObjectsByType<PrototypeGoalkeeper>()).ToArray();enabled=ai.Select(a=>a.enabled).ToArray();foreach(var a in ai)a.enabled=false;Blue=Roster.FieldPlayers.Where(p=>p.Team==PrototypeTeam.Blue).ToArray();White=Roster.FieldPlayers.Where(p=>p.Team==PrototypeTeam.White).ToArray();}
        public void Reset()=>Match.RestartMatch();
        public void Park(){int b=0,w=0;foreach(var p in Roster.FieldPlayers)p.Motor.ResetPose(new Vector3(p.Team==PrototypeTeam.Blue?-8f:8f,0f,-11f+4f*(p.Team==PrototypeTeam.Blue?b++:w++)),Quaternion.identity);}
        public void Claim(SwimmerIdentity player){Ball.ResetForRestart(player.Motor.Position);Check(player.Interaction.TryCollect(),"Range-checked fixture pickup.");}
        public PrototypeTeamTactics Team(PrototypeTeam team)=>UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>().Single(t=>t.Team==team);
        public void Dispose(){Match.RestartMatch();for(int i=0;i<ai.Length;i++)ai[i].enabled=enabled[i];}
    }
    private static async Task Click(Fixture f,string path)
    {
        await Delay(0.03f);var target=f.Canvas.transform.Find(path);Check(target!=null && target.gameObject.activeInHierarchy,"Visible button: "+path);
        var rect=target.GetComponent<RectTransform>();var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
        var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
        var button=hits.Select(h=>h.gameObject.GetComponentInParent<Button>()).FirstOrDefault(b=>b!=null);
        Check(button==target.GetComponent<Button>(),"Raycast reaches visible button: "+path);
        ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerDownHandler);ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(button.gameObject,data,ExecuteEvents.pointerClickHandler);
        await Delay(0.03f);
    }
    private static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindAnyObjectByType<T>();
    private static T Read<T>(UnityEngine.Object o,string name) where T:UnityEngine.Object=>(T)new SerializedObject(o).FindProperty(name).objectReferenceValue;
    private static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(o);
    private static int Count()=>UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Transform>(true).Length);
    private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Near(float value,float expected,string message)=>Check(Mathf.Abs(value-expected)<0.0001f,$"{message}: {value} / {expected}");
    private static async Task Delay(float seconds){float until=Time.realtimeSinceStartup+seconds;while(Time.realtimeSinceStartup<until)await Task.Yield();}
    private static async Task Until(Func<bool> condition){var watch=Stopwatch.StartNew();while(!condition()){if(watch.Elapsed.TotalSeconds>12)throw new TimeoutException("Expected match transition.");await Task.Yield();}}
}
