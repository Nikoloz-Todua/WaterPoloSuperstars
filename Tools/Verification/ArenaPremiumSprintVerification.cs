using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using TMPro;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Editor;
using ToduStudios.WaterPolo.Hub;
using ToduStudios.WaterPolo.WaterVisuals;
using Object=UnityEngine.Object;

public static class PremiumSprintChecks
{
 const string Out="Logs/ArenaPremium/Sprint/",Scene="Assets/Scenes/OlympicMatchArena.unity",Recovery="Assets/Scenes/Recovery/OlympicMatchArena_PreAuthenticField.unity";
 static T One<T>() where T:Object=>Object.FindAnyObjectByType<T>();
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static object Save(string name,object value){Directory.CreateDirectory(Out);File.WriteAllText(Out+name+".json",JsonConvert.SerializeObject(value,Formatting.Indented));return value;}
 static string Hash(string path)=>Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(path)));
 static T Field<T>(object o,string n)=>(T)o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
 static string Id(Object o)=>GlobalObjectId.GetGlobalObjectIdSlow(o).ToString();
 static async Task Until(Func<bool> p,float seconds=12){double end=EditorApplication.timeSinceStartup+seconds;while(!p()){Check(EditorApplication.timeSinceStartup<end,"Timed out waiting for state");await Task.Delay(15);}}
 static async Task Frames(int n){int end=Time.frameCount+n;await Until(()=>Time.frameCount>=end);}
 static async Task Seconds(float seconds){double end=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<end)await Task.Delay(15);}
 static async Task Shot(string name){await Seconds(.25f);await Frames(4);ScreenCapture.CaptureScreenshot(Out+name+".png");await Frames(5);}
 static void Resize(int width,bool restore=false)
 {
  var flags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;var asm=typeof(UnityEditor.Editor).Assembly;var type=asm.GetType("UnityEditor.GameView");var win=EditorWindow.GetWindow(type);var selected=type.GetProperty("selectedSizeIndex",flags);var sizes=typeof(ScriptableSingleton<>).MakeGenericType(asm.GetType("UnityEditor.GameViewSizes")).GetProperty("instance",flags).GetValue(null);var group=sizes.GetType().GetMethod("GetGroup",flags).Invoke(sizes,new[]{type.GetProperty("currentSizeGroupType",flags).GetValue(win)});int added=SessionState.GetInt("FieldSprint.Added",-1),old=SessionState.GetInt("FieldSprint.Size",-1);
  if(added>=0){if(old>=0)selected.SetValue(win,old);group.GetType().GetMethod("RemoveCustomSize").Invoke(group,new object[]{added});SessionState.SetInt("FieldSprint.Added",-1);}
  if(restore){SessionState.SetInt("FieldSprint.Size",-1);return;}
  if(old<0)SessionState.SetInt("FieldSprint.Size",(int)selected.GetValue(win));var size=Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),width,1080,"M17 temporary verification"});group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});added=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1;SessionState.SetInt("FieldSprint.Added",added);selected.SetValue(win,added);win.Focus();
 }
 public static object Direct(){Resize(1920);EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(Scene);return new{entry=Scene};}
 public static async Task<object> Preview()
 {
  var m=One<PrototypeMatch>();m.RestartMatch();await Shot("First-Preparing");await Until(()=>m.IsSprintRace);await Shot("First-Race");await Until(()=>m.IsPlaying);await Shot("First-Playing");
  return new{phase=m.Phase.ToString(),starts=m.SprintOff.Starts,acquired=m.SprintOff.Acquisitions,owner=One<BallController>().Owner?.name,frame=Time.frameCount};
 }
 static void VerificationInput()
 {
  if(SessionState.GetInt("FieldSprint.Background",-1)<0){SessionState.SetInt("FieldSprint.Background",(int)InputSystem.settings.backgroundBehavior);SessionState.SetInt("FieldSprint.EditorInput",(int)InputSystem.settings.editorInputBehaviorInPlayMode);}
  InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
 }
 static async Task Opening(PrototypeMatch m){await Until(()=>m.IsPlaying,10);}
 static async Task Click(Mouse mouse,Transform target)
 {
  await Frames(2);Check(target!=null&&target.gameObject.activeInHierarchy,"Visible pointer target");var r=(RectTransform)target;Vector2 p=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));var data=new PointerEventData(EventSystem.current){position=p};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);Check(hits.Any()&&hits[0].gameObject.transform.IsChildOf(target),"Pointer reaches "+target.name);
  InputSystem.QueueStateEvent(mouse,new MouseState{position=p});await Seconds(.04f);InputSystem.QueueStateEvent(mouse,new MouseState{position=p,buttons=1});await Seconds(.05f);InputSystem.QueueStateEvent(mouse,new MouseState{position=p});await Seconds(.07f);
 }
 public static async Task<object> SprintCompetition()
 {
  VerificationInput();var m=One<PrototypeMatch>();var ball=One<BallController>();var roster=One<PrototypeRoster>();var selection=One<PlayerSelection>();var sprint=m.SprintOff;var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();int signals=0,claims=0,waters=0;void Signal(){signals++;}void Claim(SwimmerBallInteraction a,SwimmerBallInteraction b){if(b!=null)claims++;}void Water(BallImpactEvent e){if(e.Kind==BallImpactKind.Water)waters++;}m.SprintStarted+=Signal;ball.PossessionChanged+=Claim;ball.Contact+=Water;
  try
  {
   m.RestartMatch();Check(m.Phase==MatchPhase.SprintSetup&&selection.ActivePlayer==sprint.HumanSprinter,"Fresh Q1 selects designated sprinter");
   Check(Vector3.Distance(ball.transform.position,m.PeriodCenter+Vector3.up*.45f)<.001f,"Visible ball sits in cradle immediately, including before paused physics");
   var teams=Object.FindObjectsByType<PrototypeTeamTactics>();var positions=roster.Members.Select(p=>p.Motor.Position).ToArray();
   foreach(var team in teams)
   {
    var lineup=roster.Members.Where(p=>p.Team==team.Team).ToArray();Check(lineup.All(p=>Mathf.Abs(p.Motor.Position.z-team.DefendedGoal.transform.position.z)<.001f),"Every athlete starts on own actual goal line");
    for(int i=0;i<lineup.Length;i++)for(int j=i+1;j<lineup.Length;j++)Check(Vector3.Distance(lineup[i].Motor.Position,lineup[j].Motor.Position)>=.99f,"Legal starting spacing");
    Check(lineup.Count(p=>Mathf.Abs(p.Motor.Position.x-team.DefendedGoal.transform.position.x)<1.5f)<=2,"At most two within posts");
   }
   int accepted=0;for(int i=0;i<100;i++)if(sprint.Stroke(sprint.HumanSprinter))accepted++;Check(accepted==0&&sprint.HumanStrokePower==0,"No pre-whistle tap bank");Check(!ball.CanCollect&&roster.Members.All(p=>!p.Interaction.TryCollect()),"No collection from cradle");
   var menus=One<MatchMenuController>();menus.OpenPause();await Seconds(.35f);Check(m.Phase==MatchPhase.SprintSetup&&signals==0&&ball.PeriodSupported,"Pause holds release deadline");Check(positions.Select((p,i)=>Vector3.Distance(p,roster.Members[i].Motor.Position)<.001f).All(x=>x),"No advance before whistle");menus.Resume();await Until(()=>m.IsSprintRace);Check(signals==1&&sprint.Starts==1&&ball.PeriodDropping&&!ball.CanCollect,"Whistle releases one uncollectible falling ball");
   float period=m.Clock.Remaining,shot=m.ShotClock.Remaining;Check(period==80&&shot==28&&ball.Owner==null,"Opening clocks not independently advanced");
   // Reject high input frequency; legitimate repeated Shift remains active competition.
   accepted=0;for(int i=0;i<100;i++)if(sprint.Stroke(sprint.HumanSprinter))accepted++;Check(accepted==1&&sprint.HumanStrokePower<=.29f,"One accepted stroke per cadence window");
   float peak=0;InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Frames(2);
   for(int i=0;i<12&&m.IsSprintRace;i++)
   {
    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftShift));await Seconds(.10f);peak=Mathf.Max(peak,sprint.HumanSprinter.Motor.Velocity.magnitude);
    InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Seconds(.12f);
   }
   await Opening(m);Check(ball.Owner!=null&&signals==1&&claims==1&&waters==1&&sprint.Acquisitions==1,"One real water contact and possession ends the sprint");
   Check(ball.Owner.GetComponent<SwimmerIdentity>().Team==PrototypeTeam.Blue,"Timely human Shift strokes can win");Check(peak<=6.01f&&sprint.HumanSprinter.GetComponent<SwimmerStamina>().Value<1&&sprint.HumanStrokePower==0,"Bounded speed, stamina and no retained boost");
   Check(m.Clock.Remaining<period&&m.ShotClock.PossessionTeam==PrototypeTeam.Blue,"Normal clocks begin on acquisition");var loser=sprint.Sprinter(PrototypeTeam.White);Vector3 lostAt=loser.Motor.Position;await Seconds(.2f);Check(loser.GetComponent<PrototypeSwimmerAI>().Behaviour!=SwimmerAIBehaviour.Stopped&&Vector3.Distance(lostAt,loser.Motor.Position)>.01f,"Losing AI immediately remains live");await Shot("Human-Sprint-Winner");
   // With no human stroke/movement, the opposing athlete must really swim and acquire.
   m.RestartMatch();signals=claims=waters=0;await Until(()=>m.IsSprintRace);Vector3 start=sprint.Sprinter(PrototypeTeam.White).Motor.Position;await Seconds(.6f);Check(Vector3.Distance(start,sprint.Sprinter(PrototypeTeam.White).Motor.Position)>.2f,"Opponent physically accelerates");await Opening(m);Check(ball.Owner.GetComponent<SwimmerIdentity>().Team==PrototypeTeam.White&&claims==1&&signals==1,"AI can win without scripted ownership");await Shot("AI-Sprint-Winner");
   // Real on-screen SPRINT touches use the existing input module and hold fallback.
   m.RestartMatch();await Until(()=>m.IsSprintRace);var button=Object.FindObjectsByType<GameplayActionButton>().Single(b=>b.Action==MobileMatchAction.Sprint);Vector2 point=RectTransformUtility.WorldToScreenPoint(null,((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center));
   InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=1});await Seconds(.6f);Check(One<MobileMatchControls>().SprintHeld&&sprint.HumanSprinter.Motor.Velocity.magnitude>2,"Actual on-screen Sprint hold moves designated swimmer");InputSystem.QueueStateEvent(mouse,new MouseState{position=point});await Frames(3);Check(!One<MobileMatchControls>().SprintHeld,"Pointer release clears hold");
   menus.OpenPause();int count=sprint.Starts;Vector3 bp=ball.PhysicsPosition;float deadline=m.PresentationTime;await Seconds(.3f);Check(sprint.Starts==count&&m.PresentationTime==deadline&&ball.PhysicsPosition==bp,"Race pause stable");menus.Resume();await Frames(3);Check(sprint.Starts==count,"Resume never starts another sprint");await Opening(m);
   return Save("Sprint-PASS",new{all14OnOwnGoalLines=true,spacing=true,preWhistleGates=true,oneWhistle=true,actualDropWaterAndSinglePickup=true,cappedShiftStrokes=true,humanCanWin=true,aiCanWin=true,loserImmediatelyLive=true,actualPointerSprintHoldRelease=true,staminaCost=true,pauseAndResume=true});
  }
  finally{m.SprintStarted-=Signal;ball.PossessionChanged-=Claim;ball.Contact-=Water;InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);m.SetPaused(false);}
 }
 static PrototypeTeamTactics Team(PrototypeTeam t)=>Object.FindObjectsByType<PrototypeTeamTactics>().Single(x=>x.Team==t);
 static void Park(){int b=0,w=0;foreach(var p in One<PrototypeRoster>().FieldPlayers)p.Motor.ResetPose(new Vector3(p.Team==PrototypeTeam.Blue?-8:8,0,-10+4*(p.Team==PrototypeTeam.Blue?b++:w++)),Quaternion.identity);}
 static void Claim(SwimmerIdentity p){var ball=One<BallController>();ball.ResetForRestart(p.Motor.Position);Check(p.Interaction.TryCollect(),"Legitimate nearby fixture pickup");}
 static async Task Score(PrototypeTeam t)
 {
  var goal=Team(t).AttackingGoal;var p=One<PrototypeRoster>().FieldPlayers.First(x=>x.Team==t);Park();p.Motor.ResetPose(goal.transform.position-goal.transform.forward*5+goal.transform.right*.7f,Quaternion.LookRotation(goal.transform.forward));One<PlayerSelection>().Select(p);Claim(p);Check(p.Interaction.TryShoot(22,ShotType.Normal),"Physical shot release");await Until(()=>One<PrototypeMatch>().Phase==MatchPhase.GoalRestart,5);
 }
 public static async Task<object> PeriodsAndRestarts()
 {
  var m=One<PrototypeMatch>();var ball=One<BallController>();var ai=Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>().Concat(Object.FindObjectsByType<PrototypeGoalkeeper>()).ToArray();var enabled=ai.Select(a=>a.enabled).ToArray();var records=new List<object>();
  try
  {
   m.RestartMatch();
   for(int q=1;q<=4;q++)
   {
    Check(m.Phase==MatchPhase.SprintSetup&&m.Clock.Quarter==q,"Each period stages sprint");
    foreach(var team in Object.FindObjectsByType<PrototypeTeamTactics>())Check(One<PrototypeRoster>().Members.Where(p=>p.Team==team.Team).All(p=>Mathf.Abs(p.Motor.Position.z-team.DefendedGoal.transform.position.z)<.001f),"Current defended end at Q"+q);
    await Opening(m);Check(m.SprintOff.Starts==q&&m.SprintOff.Acquisitions==q,"One sprint and pickup per quarter");
    if(q==1||q==3)
    {
     foreach(var a in ai)a.enabled=false;await Score(q==1?PrototypeTeam.Blue:PrototypeTeam.White);int starts=m.SprintOff.Starts;
     Check(!GameObject.Find(AuthenticFieldSetup.Name).transform.Find("Retracting Period Ball Release/Cradle").gameObject.activeInHierarchy,"No cradle after goal");await Shot("Q"+q+"-Goal");
     await Until(()=>m.IsCentreRestart);Check(m.CentreTaker.Team==(q==1?PrototypeTeam.White:PrototypeTeam.Blue),"Conceding team centre restart");
     if(q==3){await Until(()=>m.Phase==MatchPhase.CentreRestartReady);await Frames(2);Check(One<MobileMatchControls>().Begin(MobileMatchAction.Pass),"Human Blue centre pass");}
     await Until(()=>m.IsPlaying);Check(m.SprintOff.Starts==starts&&ball.ReleaseKind==BallReleaseKind.Pass,"Goal keeps ordinary physical centre pass, never sprint");
     for(int i=0;i<ai.Length;i++)ai[i].enabled=enabled[i];
    }
    Check(m.BlueScore==(q>=1?1:0)&&m.WhiteScore==(q>=3?1:0),"Later period preserves score");
    records.Add(new{quarter=q,starts=m.SprintOff.Starts,blue=m.BlueScore,white=m.WhiteScore,swapped=m.EndsSwitched});
    ball.ResetForRestart(Vector3.zero);m.ShotClock.ResetPossession();m.AdvanceLiveTime(m.Clock.Remaining);Check(m.Phase==(q==4?MatchPhase.FullTime:q==2?MatchPhase.HalfTime:MatchPhase.QuarterBreak),"Correct period end");
    if(q<4){One<MatchMenuController>().Continue();if(q==2)Check(m.EndSwitchCount==1&&m.EndsSwitched,"One halftime end switch before Q3 sprint");}
   }
   Check(m.Result=="DRAW"&&One<MatchMenuController>().Page==MatchMenuPage.Transition,"Four-period match completes");await Shot("Four-Period-FullTime");
   One<MatchMenuController>().AskRestart();One<MatchMenuController>().Confirm();Check(m.Phase==MatchPhase.SprintSetup&&m.SprintOff.Starts==0&&m.SprintOff.Acquisitions==0&&m.BlueScore==0&&!m.EndsSwitched,"Confirmed restart clears sprint state and restores Q1");
   return Save("Periods-PASS",new{quarters=records,allFourSprint=true,goalsUseCentreNotSprint=true,physicalGoalsBothEnds=true,halftime=true,scoreRetained=true,fullTime=true,restartClean=true,acceleratedPeriodTransitions=true});
  }
  finally{for(int i=0;i<ai.Length;i++)if(ai[i]!=null)ai[i].enabled=enabled[i];m.SetPaused(false);}
 }
 public static async Task<object> GoalGeometryAndContacts()
 {
  var m=One<PrototypeMatch>();m.RestartMatch();await Opening(m);var ball=One<BallController>();var ai=Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>().Concat(Object.FindObjectsByType<PrototypeGoalkeeper>()).ToArray();var enabled=ai.Select(a=>a.enabled).ToArray();foreach(var a in ai)a.enabled=false;
  int frames=0;void Frame(BallImpactEvent e){if(e.Kind==BallImpactKind.Frame)frames++;}ball.Contact+=Frame;var results=new List<object>();
  try
  {
   foreach(var goal in Object.FindObjectsByType<GoalMouth>())
   {
    Check(goal.TryGetCrossing(goal.transform.TransformPoint(new Vector3(0,.45f,-1)),goal.transform.TransformPoint(new Vector3(0,.45f,1)),ball.Radius,out _),"Clear inner opening crossing");
    Check(!goal.TryGetCrossing(goal.transform.TransformPoint(new Vector3(1.5f,.45f,-1)),goal.transform.TransformPoint(new Vector3(1.5f,.45f,1)),ball.Radius,out _),"Outside/tangent post cannot score");
    Check(!goal.TryGetCrossing(goal.transform.TransformPoint(new Vector3(0,.94f,-1)),goal.transform.TransformPoint(new Vector3(0,.94f,1)),ball.Radius,out _),"Crossbar height cannot score");
    var hits=Physics.SphereCastAll(goal.transform.TransformPoint(new Vector3(1.54f,.45f,-1)),ball.Radius,goal.transform.forward,2);
    Check(hits.Any(h=>h.collider.name=="RightPost"),"Real post collider swept contact query");
    foreach(bool crossbar in new[]{false,true})
    {
     Park();var p=One<PrototypeRoster>().FieldPlayers.First(x=>x.Team==goal.ScoringTeam);p.Motor.ResetPose(goal.transform.position-goal.transform.forward*5,Quaternion.LookRotation(goal.transform.forward));One<PlayerSelection>().Select(p);Claim(p);Check(p.Interaction.TryShoot(12),"Frame probe physical release");
     var body=ball.GetComponent<Rigidbody>();body.position=goal.transform.TransformPoint(new Vector3(crossbar?0:1.54f,crossbar?.94f:.45f,-.6f));body.linearVelocity=goal.transform.forward*6;int before=frames,score=m.BlueScore+m.WhiteScore;await Seconds(.3f);
     Check(m.BlueScore+m.WhiteScore==score,"Frame probe cannot score");Check(frames>before,"Actual ball frame collision: "+goal.name+(crossbar?" crossbar":" post"));results.Add(new{goal=goal.name,crossbar,actualContact=true});
    }
   }
   return Save("GoalContacts-PASS",new{officialOpening=true,strictOutsideAndCrossbarRejected=true,actualFrameContacts=results});
  }
  finally{ball.Contact-=Frame;ball.ResetForRestart(Vector3.zero);for(int i=0;i<ai.Length;i++)ai[i].enabled=enabled[i];}
 }
 static float Float(Object o,string name)=>new SerializedObject(o).FindProperty(name).floatValue;
 static void Float(Object o,string name,float value){var s=new SerializedObject(o);s.FindProperty(name).floatValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
 public static async Task<object> TouchAndGameplay()
 {
  VerificationInput();var m=One<PrototypeMatch>();var ball=One<BallController>();var select=One<PlayerSelection>();var touch=InputSystem.AddDevice<Touchscreen>();var keyboard=InputSystem.AddDevice<Keyboard>();var ai=Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>().Concat(Object.FindObjectsByType<PrototypeGoalkeeper>()).ToArray();var states=ai.Select(a=>a.enabled).ToArray();
  try
  {
   m.RestartMatch();await Until(()=>m.IsSprintRace);var button=Object.FindObjectsByType<GameplayActionButton>().Single(b=>b.Action==MobileMatchAction.Sprint);var rect=(RectTransform)button.transform;Vector2 point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));float peak=0;
   for(int i=0;i<10&&m.IsSprintRace;i++)
   {
    InputSystem.QueueStateEvent(touch,new TouchState{touchId=51,phase=UnityEngine.InputSystem.TouchPhase.Began,position=point});await Seconds(.10f);peak=Mathf.Max(peak,m.SprintOff.HumanStrokePower);
    InputSystem.QueueStateEvent(touch,new TouchState{touchId=51,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=point});await Seconds(.14f);
   }
   Check(peak>.4f,"Actual repeated touchscreen strokes reach shared boost");await Opening(m);Check(!One<MobileMatchControls>().SprintHeld&&m.SprintOff.HumanStrokePower==0,"Touch stroke state clears after acquisition");
   foreach(var a in ai)a.enabled=false;Park();var players=One<PrototypeRoster>().FieldPlayers.Where(p=>p.Team==PrototypeTeam.Blue).ToArray();var p=players[0];var receiver=players[1];p.Motor.ResetPose(Vector3.zero,Quaternion.LookRotation(Vector3.right));receiver.Motor.ResetPose(Vector3.right*6,Quaternion.identity);Claim(p);await Frames(3);
   Check(p.GetComponent<SwimmerPassing>().TryPassTo(receiver),"Normal physical pass");await Until(()=>ball.Owner==receiver.Interaction,5);Check(select.ActivePlayer==receiver,"Normal reception transfers control");
   InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.LeftShift));await Seconds(.6f);Check(receiver.Motor.Velocity.z>1&&receiver.GetComponent<SwimmerStamina>().IsSprinting,"Existing keyboard movement/sprint after race");InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Seconds(.3f);
   Claim(receiver);await Frames(3);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));await Seconds(.5f);Check(receiver.GetComponent<ShotCharge>().IsCharging,"Normal charged shot still works");InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Frames(3);Check(ball.Owner==null&&ball.ReleaseKind==BallReleaseKind.Shot,"Charge releases physical shot");
   Park();p.Motor.ResetPose(Vector3.zero,Quaternion.identity);var opponent=One<PrototypeRoster>().FieldPlayers.First(x=>x.Team==PrototypeTeam.White);opponent.Motor.ResetPose(Vector3.forward*.9f,Quaternion.LookRotation(Vector3.back));Claim(opponent);select.Select(p);await Seconds(1.1f);p.GetComponent<SwimmerSteal>().TrySteal();Check(ball.Owner==p.Interaction,"Normal legal defense can steal after sprint");
   Park();p.Motor.ResetPose(Vector3.zero,Quaternion.LookRotation(Vector3.right));receiver.Motor.ResetPose(Vector3.right*6,Quaternion.identity);Claim(p);Check(p.GetComponent<LobPassing>().TryPass()&&ball.IsAirborne&&ball.ReleaseKind==BallReleaseKind.Lob,"Existing lob retained");
   var keeper=Object.FindObjectsByType<PrototypeGoalkeeper>(FindObjectsInactive.Include).Single(k=>k.Identity.Team==PrototypeTeam.White);float chance=Float(keeper,"saveChance"),catchChance=Float(keeper,"catchChance");
   try
   {
    ball.ResetForRestart(Vector3.zero);Park();var goal=keeper.DefendedGoal;keeper.enabled=true;Float(keeper,"saveChance",1);Float(keeper,"catchChance",1);keeper.Identity.Motor.ResetPose(goal.transform.position-goal.transform.forward*Float(keeper,"stationDepth"),Quaternion.LookRotation(-goal.transform.forward));
    p.Motor.ResetPose(goal.transform.position-goal.transform.forward*6,Quaternion.LookRotation(goal.transform.forward));Claim(p);int attempts=keeper.SaveAttempts,catches=keeper.Catches;Check(p.Interaction.TryShoot(10,ShotType.Normal),"Keeper fixture actual shot");await Until(()=>keeper.SaveAttempts>attempts,5);Check(keeper.Catches>catches&&ball.Owner==keeper.Identity.Interaction,"Existing swept keeper catch at new visual goal");await Shot("Verified-Keeper-Catch");
   }
   finally{Float(keeper,"saveChance",chance);Float(keeper,"catchChance",catchChance);}
   Check(One<PrototypeRoster>().Members.All(x=>Mathf.Abs(x.Motor.Position.x)<=10.01f&&Mathf.Abs(x.Motor.Position.z)<=12.51f),"Athletes remain within field/start envelope");
   return Save("TouchGameplay-PASS",new{actualSyntheticTouchRepeatedSprint=true,noTapLeak=true,normalKeyboardSwimSprint=true,physicalPassingReceptionSelection=true,chargedShot=true,legalSteal=true,lob=true,actualSweptKeeperCatch=true,keeperChanceTemporarilyControlled=true});
  }
  finally{InputSystem.RemoveDevice(touch);InputSystem.RemoveDevice(keyboard);ball.ResetForRestart(Vector3.zero);for(int i=0;i<ai.Length;i++)ai[i].enabled=states[i];}
 }
 public static async Task<object> Views()
 {
  VerificationInput();Resize(1920);var m=One<PrototypeMatch>();var dev=One<ArenaDeveloperCamera>();var prefs=One<GamePreferences>();var values=prefs.Current;var keyboard=InputSystem.AddDevice<Keyboard>();var canvas=One<Canvas>();var probe=dev.viewpoints[2];Vector3 old=probe.position;Quaternion rotation=probe.rotation;
  try
  {
   m.RestartMatch();await Seconds(1.1f);await Shot("Final-Preparing-Broadcast");await Until(()=>m.IsSprintRace);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftShift));await Seconds(.6f);await Shot("Final-Active-Sprint");InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Opening(m);
   dev.SetView(0);await Shot("Final-Field-Overview");
   var goal=Object.FindObjectsByType<GoalMouth>().OrderBy(g=>g.transform.position.z).Last();m.SetPaused(true);canvas.enabled=false;
   probe.SetPositionAndRotation(goal.transform.position+new Vector3(3.6f,2.2f,-4.6f),Quaternion.LookRotation(goal.transform.position+new Vector3(0,.35f,.45f)-(goal.transform.position+new Vector3(3.6f,2.2f,-4.6f))));dev.SetView(2);await Shot("Final-Goal-Detail-NoHUD");
   probe.SetPositionAndRotation(new Vector3(9.6f,2.5f,-5.5f),Quaternion.LookRotation(new Vector3(12.65f,.75f,-.6f)-new Vector3(9.6f,2.5f,-5.5f)));await Shot("Final-Platform-Detail-NoHUD");
   canvas.enabled=true;m.SetPaused(false);dev.SetView(-1);m.RestartMatch();m.SetPaused(true);canvas.enabled=false;probe.SetPositionAndRotation(new Vector3(1.1f,1.25f,-1.4f),Quaternion.LookRotation(new Vector3(0,.25f,0)-new Vector3(1.1f,1.25f,-1.4f)));dev.SetView(2);await Shot("Final-Cradle-Detail-NoHUD");canvas.enabled=true;m.SetPaused(false);dev.SetView(-1);await Opening(m);
   var bounds=new List<object>();
   foreach(int width in new[]{1920,2340,2400})
   {
    Resize(width);await Frames(5);m.RestartMatch();await Seconds(1.1f);Check(Screen.width==width,"Actual Game-view width");
    foreach(var b in One<Canvas>().GetComponentsInChildren<GameplayActionButton>()){var v=new Vector3[4];((RectTransform)b.transform).GetWorldCorners(v);Check(v.All(p=>p.x>=0&&p.x<=Screen.width&&p.y>=0&&p.y<=Screen.height),"Sprint control bounds");}
    await Shot("Final-Sprint-Layout-"+width);await Opening(m);bounds.Add(new{width,height=1080});
   }
   foreach(int quality in new[]{0,1,2}){prefs.SetGraphics(quality);await Frames(5);Check(One<PoolWaterVisuals>().RuntimeMaterial.shader.isSupported,"Original water shader supported");await Shot("Final-Quality-"+quality);}
   var refs=Object.FindObjectsByType<PoolsideReferee>();foreach(var referee in refs){float min=referee.GetComponentsInChildren<Renderer>().Min(r=>r.bounds.min.y);Check(Mathf.Abs(min-.70f)<.09f,"Feet align elevated surface");Check(Mathf.Abs(Mathf.Abs(referee.transform.position.x)-12.65f)<.001f,"Referee stays centered on protected lane");}
   return Save("Views-PASS",new{actualGameView=true,preparationAndActiveRace=true,overview=true,goalPlatformCradleCloseupsHUDTemporarilyHidden=true,ratios=bounds,allThreeQualityPresets=true,refereeFeetAndLaneAlignment=true});
  }
  finally{canvas.enabled=true;m.SetPaused(false);dev.SetView(-1);probe.SetPositionAndRotation(old,rotation);prefs.SetGraphics((int)values.graphics);InputSystem.RemoveDevice(keyboard);}
 }
 sealed class MemoryRepository:IProfileRepository
 {
  public string json;
  public LocalProfile Load()=>json==null?null:LocalProfileCodec.FromJson(json);
  public void Save(LocalProfile profile)=>json=JsonUtility.ToJson(profile);
 }
 static Transform Path(string p)=>One<Canvas>().transform.Find(p);
 const string Menu="MenuOverlay/MenuSafeArea/";
 static UnityEngine.UI.Button NamedButton(string n)=>Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.name==n);
 public static object BootEntry()
 {
  Check(!Application.isPlaying,"Edit Mode");Resize(1920);
  Check(EditorApplication.ExecuteMenuItem("Tools/Water Polo/Open Main Lobby"),"Production Open Main Lobby command");Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="MainLobby","Opened production lobby");
  Check(EditorApplication.ExecuteMenuItem("Tools/Water Polo/Arena/Open Olympic Match Arena"),"Production arena open command");
  Save("HubUserSaveBaseline",new{hash=File.Exists(HubApplication.SavePath)?Hash(HubApplication.SavePath):null});
  Check(EditorApplication.ExecuteMenuItem("Tools/Water Polo/Play Hub Entry"),"Production Play Hub Entry command");return new{bootMenuExecuted=true};
 }
 static async Task LaunchDivision(Mouse mouse)
 {
  await Click(mouse,One<HubView>().home.play.transform);await Click(mouse,NamedButton("Mode_division_1").transform);await Click(mouse,NamedButton("LaunchMode").transform);
  await Until(()=>One<PrototypeMatch>()!=null&&One<HubMatchBridge>()!=null);await Frames(8);Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="OlympicMatchArena","Current production arena launch");
 }
 public static async Task<object> HubLoop()
 {
  VerificationInput();await Until(()=>One<HubView>()!=null&&One<HubView>().Service!=null);var app=HubApplication.Instance;var original=app.Progression;var repository=new MemoryRepository();var service=new HubProgression(original.Catalog,repository);app.UseVerificationProfile(service);var mouse=InputSystem.AddDevice<Mouse>();
  try
  {
   var old=One<HubView>();app.OpenLobby();await Until(()=>One<HubView>()!=null&&One<HubView>()!=old);await Frames(5);int coins=service.Snapshot.coins;
   await LaunchDivision(mouse);Check(One<PrototypeRoster>().Members.Count==14&&One<PrototypeMatch>().SprintOff!=null,"Hub arena has equipment and all athletes");await Shot("Hub-Launched-Current-Arena");
   string first=service.Snapshot.activeMatch.id;await Click(mouse,Path("MatchSafeArea/PauseButton"));await Click(mouse,Path(Menu+"PausePanel/Restart"));await Click(mouse,Path(Menu+"ConfirmationPanel/Cancel"));Check(service.Snapshot.activeMatch.id==first,"Cancel preserves session");
   await Click(mouse,Path(Menu+"PausePanel/Restart"));await Click(mouse,Path(Menu+"ConfirmationPanel/Confirm"));Check(service.Snapshot.activeMatch.id!=first&&service.Snapshot.coins==coins&&service.Snapshot.played==0,"Restart creates fresh session without rewards");Check(One<PrototypeMatch>().Phase==MatchPhase.SprintSetup,"Restart starts fresh Q1 sprint");
   await Click(mouse,Path("MatchSafeArea/PauseButton"));await Click(mouse,Path(Menu+"PausePanel/Exit"));await Shot("Hub-Exit-Confirmation");await Click(mouse,Path(Menu+"ConfirmationPanel/Confirm"));await Until(()=>One<HubView>()!=null&&One<HubView>().Page=="Lobby");
   Check(service.Snapshot.activeMatch==null&&service.Snapshot.pendingResult==null&&service.Snapshot.played==0&&service.Snapshot.coins==coins,"Abandoned sprint grants no rewards");Check(One<SprintBallReleaseVisual>()==null&&Time.timeScale==1&&!AudioListener.pause,"Holder removed and lobby unfrozen");
   await LaunchDivision(mouse);var m=One<PrototypeMatch>();var ball=One<BallController>();string session=service.Snapshot.activeMatch.id;
   await Opening(m);var ai=Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>().Concat(Object.FindObjectsByType<PrototypeGoalkeeper>()).ToArray();var states=ai.Select(a=>a.enabled).ToArray();
   try{foreach(var a in ai)a.enabled=false;await Score(PrototypeTeam.Blue);await Until(()=>m.IsPlaying);}
   finally{for(int i=0;i<ai.Length;i++)ai[i].enabled=states[i];}
   for(int q=1;q<=4;q++)
   {
    if(q>1)await Opening(m);Check(m.SprintOff.Starts==q&&m.SprintOff.Acquisitions==q,"Real sprint acquisition each official period");
    Check(service.Snapshot.activeMatch.id==session&&service.Snapshot.played==0&&service.Snapshot.coins==coins,"No reward/session turnover during periods");
    ball.ResetForRestart(Vector3.zero);m.ShotClock.ResetPossession();m.AdvanceLiveTime(m.Clock.Remaining);if(q<4)One<MatchMenuController>().Continue();
   }
   Check(m.Phase==MatchPhase.FullTime&&m.Result=="WIN"&&service.Snapshot.played==1&&service.Snapshot.wins==1,"One official four-period victory");await Shot("Official-Four-Sprint-Victory");
   var result=service.Snapshot;Check(result.activeMatch==null&&result.pendingResult!=null,"Bridge saves result at full time");int earned=result.coins,history=result.history.Count;long revision=result.revision;var bridge=One<HubMatchBridge>();
   Check(bridge.TrySaveResult()&&bridge.TrySaveResult(),"Bridge retry safe");Check(!service.CompleteMatch(result.pendingResult.id,9,0,out _),"Receipt replay refused");Check(service.Snapshot.coins==earned&&service.Snapshot.history.Count==history&&service.Snapshot.revision==revision,"No duplicate reward writes");
   One<MatchMenuController>().Continue();await Until(()=>One<HubView>()!=null&&One<HubView>().Page=="Result");Check(Time.timeScale==1&&!AudioListener.pause,"Results remain responsive");await Shot("Official-Reward-Result");
   var reloaded=new HubProgression(original.Catalog,repository);Check(reloaded.Snapshot.coins==earned&&reloaded.Snapshot.played==1&&reloaded.Snapshot.pendingResult.id==result.pendingResult.id,"Serialized profile reload retains balance and receipt");
   await Click(mouse,NamedButton("Continue").transform);await Until(()=>One<HubView>().Page=="Lobby");await Shot("Returned-MainLobby");
   string baseline=(string)JObject.Parse(File.ReadAllText(Out+"HubUserSaveBaseline.json"))["hash"];Check((File.Exists(HubApplication.SavePath)?Hash(HubApplication.SavePath):null)==baseline,"Real user profile unchanged");
   return Save("HubLoop-PASS",new{bootAndProductionLaunchMenus=true,actualPointerUnlockedDivisionLaunch=true,all14Athletes=true,restartCancelConfirmFreshSprintSession=true,abandonSprintNoRewardsOrStrayHolder=true,allFourRealSprintAcquisitions=true,acceleratedPeriodTransitions=true,fullLengthMatchPlayed=false,oneOfficialVictoryReceipt=true,retriesAndReplayNoDuplicateRewards=true,serializedProfileReload=true,resultsAndReturnLobby=true,realUserProfileHashUnchanged=true});
  }
  finally{InputSystem.RemoveDevice(mouse);if(One<PrototypeMatch>()!=null){One<PrototypeMatch>().SetPaused(false);app.ReturnToLobby(true);await Until(()=>One<HubView>()!=null);}app.UseVerificationProfile(original);app.OpenLobby();}
 }
 public static async Task<object> LoggedHubLoop()
 {
  var errors=new List<string>();void Logged(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+stack);}
  Application.logMessageReceived+=Logged;
  try{await HubLoop();await Frames(10);Check(errors.Count==0,"No runtime error during fresh hub/scene teardown pass");return Save("Fresh-Hub-Console-PASS",new{newRuntimeErrors=errors.Count,exitDuringSprintAndFullTime=true,retainedHistoricalTeardownExceptionFixed=true});}
  finally{Application.logMessageReceived-=Logged;Save("Fresh-Hub-Errors",errors);}
 }
}


