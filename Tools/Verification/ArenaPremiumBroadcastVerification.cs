using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using TMPro;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Editor;
using ToduStudios.WaterPolo.Hub;
using ToduStudios.WaterPolo.WaterVisuals;
using Object=UnityEngine.Object;

public static class PremiumBroadcastChecks
{
 const string Out="Logs/ArenaPremium/Broadcast/",Scene="Assets/Scenes/OlympicMatchArena.unity",Recovery="Assets/Scenes/Recovery/OlympicMatchArena_PreMatchBroadcast.unity";
 static T One<T>() where T:Object=>Object.FindAnyObjectByType<T>();
 static void Check(bool p,string why){if(!p)throw new Exception(why);}
 static object Save(string n,object v){Directory.CreateDirectory(Out);File.WriteAllText(Out+n+".json",JsonConvert.SerializeObject(v,Formatting.Indented));return v;}
 static string Hash(string p)=>Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(p)));
 static string Id(Object o)=>GlobalObjectId.GetGlobalObjectIdSlow(o).ToString();
 static T Field<T>(object o,string n)=>(T)o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
 static async Task Until(Func<bool> p,float seconds=12){double end=EditorApplication.timeSinceStartup+seconds;while(!p()){Check(EditorApplication.timeSinceStartup<end,"Timed out waiting for state");await Task.Delay(15);}}
 static async Task Frames(int n){int end=Time.frameCount+n;await Until(()=>Time.frameCount>=end);}
 static async Task Seconds(float n){double end=EditorApplication.timeSinceStartup+n;while(EditorApplication.timeSinceStartup<end)await Task.Delay(15);}
 static async Task Shot(string n){await Seconds(.2f);await Frames(4);ScreenCapture.CaptureScreenshot(Out+n+".png");await Frames(5);}
 static void Resize(int width,bool restore=false)
 {
  var f=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;var asm=typeof(UnityEditor.Editor).Assembly;var t=asm.GetType("UnityEditor.GameView");var w=EditorWindow.GetWindow(t);var selected=t.GetProperty("selectedSizeIndex",f);var sizes=typeof(ScriptableSingleton<>).MakeGenericType(asm.GetType("UnityEditor.GameViewSizes")).GetProperty("instance",f).GetValue(null);var group=sizes.GetType().GetMethod("GetGroup",f).Invoke(sizes,new[]{t.GetProperty("currentSizeGroupType",f).GetValue(w)});int added=SessionState.GetInt("Broadcast.Added",-1),old=SessionState.GetInt("Broadcast.Size",-1);
  if(added>=0){if(old>=0)selected.SetValue(w,old);group.GetType().GetMethod("RemoveCustomSize").Invoke(group,new object[]{added});SessionState.SetInt("Broadcast.Added",-1);}if(restore){SessionState.SetInt("Broadcast.Size",-1);return;}
  if(old<0)SessionState.SetInt("Broadcast.Size",(int)selected.GetValue(w));var size=Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),width,1080,"M19 temporary verification"});group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});added=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1;SessionState.SetInt("Broadcast.Added",added);selected.SetValue(w,added);w.Focus();
 }
 static void Input(){if(SessionState.GetInt("Broadcast.Background",-1)<0){SessionState.SetInt("Broadcast.Background",(int)InputSystem.settings.backgroundBehavior);SessionState.SetInt("Broadcast.EditorInput",(int)InputSystem.settings.editorInputBehaviorInPlayMode);}InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;}
 public static object Direct(){Resize(1920);EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(Scene);return new{entry=Scene};}
 public static async Task<object> Before()
 {
  Input();var m=One<PrototypeMatch>();m.RestartMatch();One<GameplayCamera>().SnapToTarget();await Shot("Before-Opening");await Until(()=>m.IsPlaying&&!m.AwaitingOpeningPossession);await Shot("Before-Lengthwise");One<ArenaDeveloperCamera>().SetView(0);await Shot("Before-Arena");One<ArenaDeveloperCamera>().SetView(-1);return new{actualViews=true,frame=Time.frameCount};
 }
 public static async Task<object> Preview()
 {
  Input();var m=One<PrototypeMatch>();var prefs=One<GamePreferences>();var camera=One<GameplayCamera>();prefs.SetCameraView(0);m.RestartMatch();camera.SnapToTarget();await Shot("After-Opening");await Until(()=>m.IsPlaying&&!m.AwaitingOpeningPossession);await Seconds(.6f);await Shot("Lengthwise-Gameplay");prefs.SetCameraView(1);await Seconds(1.3f);await Shot("Sideline-Gameplay");
  var display=One<ArenaEventDisplay>();m.SetPaused(true);var canvas=One<Canvas>();canvas.enabled=false;var dev=One<ArenaDeveloperCamera>();var probe=dev.viewpoints[2];var pos=probe.position;var rot=probe.rotation;
  try{probe.SetPositionAndRotation(new Vector3(-11.5f,12.2f,0),Quaternion.LookRotation(GameObject.Find("Primary Championship Scoreboard").transform.position-new Vector3(-11.5f,12.2f,0)));dev.SetView(2);await Shot("Scoreboard-Closeup");dev.SetView(0);await Shot("Arena-Scoreboard-Location");}
  finally{probe.SetPositionAndRotation(pos,rot);dev.SetView(-1);canvas.enabled=true;m.SetPaused(false);}
  return new{score=display.primaryScore.text,clock=display.primaryClock.text,view=camera.View.ToString(),hidden=One<BroadcastSightlines>().HiddenGroups,frame=Time.frameCount};
 }
 static Behaviour[] AI()=>Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>().Concat(Object.FindObjectsByType<PrototypeGoalkeeper>()).ToArray();
 static void Disable(Behaviour[] list){foreach(var b in list)b.enabled=false;foreach(var p in One<PrototypeRoster>().Members)p.Motor.StopMotion();}
 static void RestoreAI(Behaviour[] list,bool[] states){for(int i=0;i<list.Length;i++)if(list[i]!=null)list[i].enabled=states[i];}
 static PrototypeTeamTactics Team(PrototypeTeam t)=>Object.FindObjectsByType<PrototypeTeamTactics>().Single(x=>x.Team==t);
 static void Park(){int b=0,w=0;foreach(var p in One<PrototypeRoster>().FieldPlayers)p.Motor.ResetPose(new Vector3(p.Team==PrototypeTeam.Blue?-8:8,0,-10+4*(p.Team==PrototypeTeam.Blue?b++:w++)),Quaternion.identity);}
 static void Claim(SwimmerIdentity p){One<BallController>().ResetForRestart(p.Motor.Position);Check(p.Interaction.TryCollect(),"Actual nearby pickup");}
 static Transform Path(string path)=>One<Canvas>().transform.Find(path);
 static async Task Click(Mouse mouse,Transform t)
 {
  Check(t!=null&&t.gameObject.activeInHierarchy,"Visible pointer target");var r=(RectTransform)t;Vector2 p=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));var data=new PointerEventData(EventSystem.current){position=p};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);Check(hits.Count>0&&hits[0].gameObject.transform.IsChildOf(t),"No invisible blocker on "+t.name);
  InputSystem.QueueStateEvent(mouse,new MouseState{position=p});await Seconds(.04f);InputSystem.QueueStateEvent(mouse,new MouseState{position=p,buttons=1});await Seconds(.06f);InputSystem.QueueStateEvent(mouse,new MouseState{position=p});await Seconds(.08f);
 }
 static async Task Opening(PrototypeMatch m)=>await Until(()=>m.IsPlaying&&!m.AwaitingOpeningPossession);
 static void DisplayMatches()
 {
  var m=One<PrototypeMatch>();var d=One<ArenaEventDisplay>();d.Refresh();Check(d.primaryScore.text==$"{m.BlueScore} : {m.WhiteScore}","Primary actual score");Check(d.scores.All(t=>t.text==$"BLUE   {m.BlueScore} : {m.WhiteScore}   WHITE"),"Six synchronized secondary scores");int s=Mathf.CeilToInt(m.Clock.DisplayRemaining),shot=Mathf.CeilToInt(m.ShotClock.Remaining);Check(d.primaryClock.text==$"Q{m.Clock.Quarter}     {s/60:00}:{s%60:00}     SHOT {shot:00}","Actual display clock and shot clock");
  var hud=One<MatchHUD>();Check(Field<TMP_Text>(hud,"clockLabel").text==$"{s/60:0}:{s%60:00}"&&Field<TMP_Text>(hud,"periodLabel").text==$"Q{m.Clock.Quarter}"&&Field<TMP_Text>(hud,"shotClockLabel").text==$"{shot:00}","HUD and arena clocks agree");Check(Field<TMP_Text>(One<PrototypeScoreboard>(),"scoreLabel").text==d.primaryScore.text,"Screen HUD and arena score agree");
 }
 public static async Task<object> CameraControls()
 {
  Input();var m=One<PrototypeMatch>();var prefs=One<GamePreferences>();var cam=One<GameplayCamera>();var selection=One<PlayerSelection>();var ball=One<BallController>();var menus=One<MatchMenuController>();var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();var touch=InputSystem.AddDevice<Touchscreen>();var gamepad=InputSystem.AddDevice<Gamepad>();var ai=AI();var states=ai.Select(a=>a.enabled).ToArray();var records=new List<object>();
  try
  {
   m.RestartMatch();await Opening(m);Disable(ai);Park();var p=One<PrototypeRoster>().FieldPlayers.First(x=>x.Team==PrototypeTeam.Blue);p.Motor.ResetPose(Vector3.zero,Quaternion.identity);Claim(p);await Frames(4);
   foreach(int mode in new[]{0,1})
   {
    prefs.SetCameraView(mode);await Seconds(1);p.Motor.ResetPose(Vector3.zero,Quaternion.identity);Claim(p);await Frames(3);Vector2 expected=cam.ScreenToWorldMovement(Vector2.right);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D,Key.LeftShift));await Seconds(.35f);Check(Vector2.Dot(new Vector2(p.Motor.Velocity.x,p.Motor.Velocity.z).normalized,expected)>.98f&&p.GetComponent<SwimmerStamina>().IsSprinting,"Screen-right keyboard and normal sprint mode "+mode);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Seconds(.2f);
    p.Motor.ResetPose(Vector3.zero,Quaternion.identity);Claim(p);await Frames(3);expected=cam.ScreenToWorldMovement(Vector2.up);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));await Seconds(.3f);Check(Vector2.Dot(new Vector2(p.Motor.Velocity.x,p.Motor.Velocity.z).normalized,expected)>.98f,"Screen-up keyboard mode "+mode);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Seconds(.2f);
    p.Motor.ResetPose(Vector3.zero,Quaternion.identity);Claim(p);await Frames(3);var joy=One<VirtualSwimJoystick>();var rect=(RectTransform)joy.transform;Vector2 center=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));expected=cam.ScreenToWorldMovement(Vector2.right);
    InputSystem.QueueStateEvent(touch,new TouchState{touchId=91,phase=UnityEngine.InputSystem.TouchPhase.Began,position=center});await Frames(3);InputSystem.QueueStateEvent(touch,new TouchState{touchId=91,phase=UnityEngine.InputSystem.TouchPhase.Moved,position=center+Vector2.right*80});await Seconds(.35f);Check(joy.Value.x>.5f&&Vector2.Dot(new Vector2(p.Motor.Velocity.x,p.Motor.Velocity.z).normalized,expected)>.98f,"Actual touch joystick screen-right mode "+mode);InputSystem.QueueStateEvent(touch,new TouchState{touchId=91,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=center+Vector2.right*80});await Frames(3);Check(joy.Value==Vector2.zero,"Touch releases joystick mode "+mode);await Until(()=>p.Motor.Velocity.magnitude<.05f,1);Check(joy.Value==Vector2.zero,"Touch movement decelerates without stuck input mode "+mode);
    expected=cam.ScreenToWorldMovement(Vector2.up);InputSystem.QueueStateEvent(gamepad,new GamepadState{leftStick=Vector2.up});await Seconds(.3f);Check(Vector2.Dot(new Vector2(p.Motor.Velocity.x,p.Motor.Velocity.z).normalized,expected)>.98f,"Camera-relative gamepad mode "+mode);InputSystem.QueueStateEvent(gamepad,new GamepadState());await Seconds(.2f);
    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.C));await Frames(3);Check(selection.ActivePlayer!=p,"C switching after sprint mode "+mode);InputSystem.QueueStateEvent(keyboard,new KeyboardState());selection.Select(p);records.Add(new{mode=(MatchCameraView)mode,right=cam.ScreenToWorldMovement(Vector2.right).ToString(),up=cam.ScreenToWorldMovement(Vector2.up).ToString(),keyboard=true,touch=true,gamepad=true,normalSprint=true});
   }
   prefs.SetCameraView(0);await Seconds(.7f);menus.OpenPause();menus.OpenSettings();await Frames(4);float clock=m.Clock.Remaining,shot=m.ShotClock.Remaining;Vector3 bp=ball.PhysicsPosition,pp=p.Motor.Position;int starts=m.SprintOff.Starts;
   await Click(mouse,Path("MenuOverlay/MenuSafeArea/SettingsPanel/SidelineCamera"));await Seconds(.7f);Check(cam.View==MatchCameraView.SidelineBroadcast&&cam.ViewBlend>.999f&&m.IsPaused,"Actual settings pointer switches while paused");Check(m.Clock.Remaining==clock&&m.ShotClock.Remaining==shot&&ball.PhysicsPosition==bp&&p.Motor.Position==pp&&m.SprintOff.Starts==starts,"View change affects no gameplay");await Shot("Pause-Camera-Settings");
   InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F6));await Frames(3);Check(cam.View==MatchCameraView.Lengthwise,"F6 toggle in pause");await Frames(3);Check(cam.View==MatchCameraView.Lengthwise,"Held F6 toggles once");InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Frames(3);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F6));await Frames(3);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Seconds(.7f);Check(cam.View==MatchCameraView.SidelineBroadcast,"Second F6 edge returns sideline");
   prefs.Save();string valid=PlayerPrefs.GetString(GamePreferences.StorageKey);Check((int)JObject.Parse(valid)["cameraView"]==1,"Camera stored in existing preferences");prefs.SetCameraView(0);prefs.Reload();Check(prefs.Current.cameraView==MatchCameraView.SidelineBroadcast,"Preference reload");var bad=JObject.Parse(valid);bad["cameraView"]=77;PlayerPrefs.SetString(GamePreferences.StorageKey,bad.ToString());prefs.Reload();Check(prefs.Current.cameraView==MatchCameraView.Lengthwise,"Invalid stored camera falls back lengthwise");PlayerPrefs.SetString(GamePreferences.StorageKey,valid);prefs.Reload();menus.Cancel();menus.Resume();await Frames(4);Check(m.IsPlaying&&!One<MobileMatchControls>().SprintHeld&&One<VirtualSwimJoystick>().Value==Vector2.zero,"Resume clears input and keeps play");
   var dev=One<ArenaDeveloperCamera>();InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F8));await Frames(3);InputSystem.QueueStateEvent(keyboard,new KeyboardState());Check(dev.ViewIndex==0&&!cam.enabled&&cam.Presentation==MatchCameraPresentation.Inspection,"F8 existing inspection intact");dev.SetView(-1);await Frames(4);Check(cam.enabled&&cam.View==MatchCameraView.SidelineBroadcast&&cam.Presentation==MatchCameraPresentation.Live,"Inspection returns selected gameplay camera");
   return Save("CameraControls-PASS",new{modes=records,actualSettingsPointer=true,F6Edges=true,pauseBlendUnscaled=true,noGameStateChange=true,persistenceAndInvalidFallback=true,F8Preserved=true,inputReleased=true});
  }
  finally{InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(touch);InputSystem.RemoveDevice(gamepad);RestoreAI(ai,states);m.SetPaused(false);}
 }
 static async Task PhysicalScore(PrototypeTeam t)
 {
  var m=One<PrototypeMatch>();var goal=Team(t).AttackingGoal;var p=One<PrototypeRoster>().FieldPlayers.First(x=>x.Team==t);Park();p.Motor.ResetPose(goal.transform.position-goal.transform.forward*5+goal.transform.right*.7f,Quaternion.LookRotation(goal.transform.forward));One<PlayerSelection>().Select(p);Claim(p);Check(p.Interaction.TryShoot(22,ShotType.Normal),"Physical goal fixture");await Until(()=>m.Phase==MatchPhase.GoalRestart,5);await Frames(2);DisplayMatches();
 }
 static async Task ResumeGoal(PrototypeMatch m)
 {
  await Until(()=>m.IsCentreRestart);if(m.CentreTaker.Team==PrototypeTeam.Blue){await Until(()=>m.Phase==MatchPhase.CentreRestartReady);Check(One<MobileMatchControls>().Begin(MobileMatchAction.Pass),"Real Blue center restart pass");}await Opening(m);
 }
 static async Task BoardShot(string name,bool freeze=true)
 {
  var m=One<PrototypeMatch>();var dev=One<ArenaDeveloperCamera>();var canvas=One<Canvas>();var probe=dev.viewpoints[2];var pos=probe.position;var rot=probe.rotation;bool paused=m.IsPaused;bool enabled=canvas.enabled;
  try{if(freeze)m.SetPaused(true);canvas.enabled=false;probe.SetPositionAndRotation(new Vector3(-11.5f,12.2f,0),Quaternion.LookRotation(GameObject.Find(MatchBroadcastSetup.Name).transform.position-new Vector3(-11.5f,12.2f,0)));dev.SetView(2);await Shot(name);}
  finally{probe.SetPositionAndRotation(pos,rot);dev.SetView(-1);canvas.enabled=enabled;m.SetPaused(paused);}
 }
 static async Task FreeThrowPass(SwimmerIdentity carrier,SwimmerIdentity receiver)
 {var m=One<PrototypeMatch>();await Until(()=>m.Phase==MatchPhase.FreeThrowReady);receiver.Motor.ResetPose(carrier.Motor.Position+carrier.Motor.Facing*6,Quaternion.identity);Check(carrier.GetComponent<SwimmerPassing>().TryPassTo(receiver),"Real free throw pass");await Until(()=>m.IsPlaying);await Frames(3);}
 public static async Task<object> DisplayAndExclusions()
 {
  Input();var m=One<PrototypeMatch>();var d=One<ArenaEventDisplay>();var prefs=One<GamePreferences>();var ai=AI();var states=ai.Select(a=>a.enabled).ToArray();
  try
  {
   prefs.SetCameraView(1);m.RestartMatch();await Frames(3);DisplayMatches();Check(d.primaryState.text=="PERIOD OPENING"&&d.ActiveExclusions==0,"Initial live zero score, no invented exclusions");await Opening(m);Disable(ai);Park();await Frames(3);DisplayMatches();
   var hud=One<MatchHUD>();for(int sample=0;sample<60;sample++){await Frames(1);string expected=Field<TMP_Text>(hud,"clockLabel").text.PadLeft(5,'0');string time=System.Text.RegularExpressions.Regex.Match(d.primaryClock.text,@"\d\d:\d\d").Value;Check(time==expected&&d.primaryClock.text.EndsWith(Field<TMP_Text>(hud,"shotClockLabel").text),"Rendered live clocks synchronized without explicit Refresh");}
   var blue=One<PrototypeRoster>().FieldPlayers.Where(p=>p.Team==PrototypeTeam.Blue).ToArray();var white=One<PrototypeRoster>().FieldPlayers.Where(p=>p.Team==PrototypeTeam.White).ToArray();var carrier=blue[0];var receiver=blue[2];carrier.Motor.ResetPose(Vector3.zero,Quaternion.LookRotation(Vector3.right));receiver.Motor.ResetPose(Vector3.right*6,Quaternion.identity);white[0].Motor.ResetPose(Vector3.left*.8f,Quaternion.LookRotation(Vector3.right));Claim(carrier);Check(m.Rules.ReportContact(white[0],carrier,IllegalContact.Push),"Actual ordinary foul");d.Refresh();Check(d.ActiveExclusions==0&&d.exclusionDots.All(r=>!r.enabled),"Ordinary foul produces no exclusion dot");await FreeThrowPass(carrier,receiver);
   for(int i=0;i<2;i++)
   {
    Park();carrier.Motor.ResetPose(Vector3.zero,Quaternion.LookRotation(Vector3.right));receiver.Motor.ResetPose(Vector3.right*6,Quaternion.identity);blue[1].Motor.ResetPose(Vector3.forward,Quaternion.identity);white[i].Motor.ResetPose(Vector3.forward+Vector3.left*.6f,Quaternion.LookRotation(Vector3.right));Claim(carrier);Check(m.Rules.ReportContact(white[i],blue[1],IllegalContact.Hold),"Real exclusion "+i);d.Refresh();int index=Array.IndexOf(d.athletes,white[i]);Check(d.exclusionDots[index].enabled&&d.capLabels[index].text==$"#{white[i].CapNumber}\n<size=65%>18s</size>"&&d.ActiveExclusions==i+1,"Correct cap, count and actual exclusion seconds");
    if(i==1){m.SetPaused(true);string text=d.capLabels[index].text;float remaining=white[i].ExclusionRemaining;await Seconds(.3f);Check(remaining==white[i].ExclusionRemaining&&text==d.capLabels[index].text,"Paused exclusion clock stable");await BoardShot("Scoreboard-Active-Exclusions");m.SetPaused(false);}
    await FreeThrowPass(carrier,receiver);
   }
   m.AdvanceLiveTime(19);await Frames(3);d.Refresh();Check(white.Take(2).All(p=>p.EligibleForPlay)&&d.ActiveExclusions==0&&d.exclusionDots.All(r=>!r.enabled),"Legal live-time re-entry clears both indicators");
   // A real goal clears ordinary active exclusions through the existing rules, never via the board.
   await PhysicalScore(PrototypeTeam.Blue);Check(m.BlueScore==1&&m.WhiteScore==0&&d.primaryState.text.Contains("BLUE"),"Blue goal presentation");await BoardShot("Scoreboard-After-Blue-Goal",false);await ResumeGoal(m);await PhysicalScore(PrototypeTeam.White);Check(m.BlueScore==1&&m.WhiteScore==1&&d.primaryState.text.Contains("WHITE"),"White goal presentation");await BoardShot("Scoreboard-After-White-Goal",false);await ResumeGoal(m);
   m.SetPaused(true);d.Refresh();string clock=d.primaryClock.text;float remainingClock=m.Clock.Remaining;await Seconds(.3f);Check(m.Clock.Remaining==remainingClock&&d.primaryClock.text==clock&&d.primaryState.text=="PAUSED","Board follows paused authority");m.SetPaused(false);m.RestartMatch();await Frames(3);DisplayMatches();Check(d.ActiveExclusions==0&&d.athletes.All(p=>p.PersonalFouls==0)&&d.exclusionDots.All(r=>!r.enabled),"Restart clears all scoreboard state");
   return Save("DisplayExclusions-PASS",new{initialZeroLiveValues=true,screenHUDAndSixSecondaryFacesAgree=true,ordinaryFoulNoFalseMarker=true,twoActualOffBallExclusions=true,actualCapsAndRemaining=true,pauseNoCountdown=true,legalTimeReentryClears=true,physicalBlueAndWhiteGoals=true,goalTeamNotification=true,restartNoStaleState=true});
  }
  finally{RestoreAI(ai,states);m.SetPaused(false);}
 }
 static object Framing(string context)
 {
  var cam=One<GameplayCamera>();var lens=cam.GetComponent<Camera>();var p=One<PlayerSelection>().ActivePlayer;var ball=One<BallController>();Vector3 actor=lens.WorldToViewportPoint(p.Motor.Position+Vector3.up*.45f),b=lens.WorldToViewportPoint(ball.PlayPosition);Check(actor.z>0&&actor.x>.10f&&actor.x<.90f&&actor.y>.27f&&actor.y<.85f,"Controlled player in playable frame "+context+actor);Check(b.z>0&&b.x>.10f&&b.x<.90f&&b.y>.27f&&b.y<.85f,"Ball in playable frame "+context+b);Check(cam.transform.position.x<=22.51f&&cam.transform.position.y>=12&&cam.transform.position.y<=26.1f,"Constrained internal camera lane");return new{context,actor=actor.ToString(),ball=b.ToString(),camera=JsonUtility.ToJson(cam.CaptureCameraPose())};
 }
 public static async Task<object> CameraViewsAndPeriods()
 {
  Input();var m=One<PrototypeMatch>();var prefs=One<GamePreferences>();var cam=One<GameplayCamera>();var ai=AI();var states=ai.Select(a=>a.enabled).ToArray();var frames=new List<object>();var periods=new List<object>();
  try
  {
   prefs.SetCameraView(1);m.RestartMatch();await Opening(m);Disable(ai);var p=One<PrototypeRoster>().FieldPlayers.First(x=>x.Team==PrototypeTeam.Blue);
   foreach(int z in new[]{-9,0,9})
   {
    Park();p.Motor.ResetPose(new Vector3(1,0,z),Quaternion.identity);Claim(p);await Seconds(1.2f);frames.Add(Framing("goal/center "+z));await Shot(z<0?"Sideline-Left-Goal":z>0?"Sideline-Right-Goal":"Sideline-Halfway");
   }
   foreach(float x in new[]{-8.5f,8.5f}){Park();p.Motor.ResetPose(new Vector3(x,0,0),Quaternion.identity);Claim(p);await Seconds(1);frames.Add(Framing("near/far sideline "+x));}
   // A loose ball and a distant selected defender must both remain visible.
   Park();p.Motor.ResetPose(new Vector3(-4,0,-8),Quaternion.identity);One<BallController>().ResetForRestart(new Vector3(4,0,8));await Seconds(1.1f);frames.Add(Framing("distant loose ball"));
   var keeper=One<PrototypeRoster>().Members.First(x=>x.Team==PrototypeTeam.Blue&&!x.IsFieldPlayer);Claim(keeper);await Seconds(1);Check(One<PlayerSelection>().ActivePlayer==keeper,"Keeper possession transfers selection");frames.Add(Framing("controlled goalkeeper"));
   RestoreAI(ai,states);m.RestartMatch();
   for(int q=1;q<=4;q++)
   {
    Check(m.Clock.Quarter==q&&m.Phase==MatchPhase.SprintSetup,"New quarter opening");cam.SnapToTarget();await Frames(3);Check(cam.View==MatchCameraView.SidelineBroadcast&&cam.ViewBlend==0&&cam.Presentation==MatchCameraPresentation.PeriodOpening,"Selected sideline yields to original opening");Check(Vector3.Distance(cam.transform.position,new Vector3(36,33,-41))<.02f&&Mathf.Abs(cam.GetComponent<Camera>().fieldOfView-60)<.001f,"M20 preparation overview pose");DisplayMatches();
    await Until(()=>m.IsSprintRace);Check(One<PlayerSelection>().SwitchingLocked&&One<PlayerSelection>().ActivePlayer==m.SprintOff.HumanSprinter,"Designated sprinter stays locked");await Opening(m);await Seconds(.7f);Check(cam.ViewBlend>.999f&&m.SprintOff.Starts==q&&m.SprintOff.Acquisitions==q&&!One<PlayerSelection>().SwitchingLocked,"Genuine acquisition restores selected camera/control");DisplayMatches();periods.Add(new{q,m.EndsSwitched,m.EndSwitchCount,starts=m.SprintOff.Starts,blend=cam.ViewBlend});
    One<BallController>().ResetForRestart(Vector3.zero);m.ShotClock.ResetPossession();m.AdvanceLiveTime(m.Clock.Remaining);await Frames(3);DisplayMatches();if(q<4)One<MatchMenuController>().Continue();
   }
   Check(m.Phase==MatchPhase.FullTime&&m.EndSwitchCount==1&&One<ArenaEventDisplay>().primaryState.text=="FINAL","Final board, four starts and one halftime change");await BoardShot("Scoreboard-Final");One<MatchMenuController>().AskRestart();One<MatchMenuController>().Confirm();Check(m.Phase==MatchPhase.SprintSetup&&cam.View==MatchCameraView.SidelineBroadcast&&m.BlueScore==0&&m.SprintOff.Starts==0,"Restart resets contest, preserves camera preference");
   return Save("CameraViewsPeriods-PASS",new{framing=frames,quarters=periods,m20OpeningOverview=true,allFourActualPickups=true,halftimeOneSwitch=true,finalDisplay=true,restartRetainsCamera=true,periodsAccelerated=true});
  }
  finally{RestoreAI(ai,states);m.SetPaused(false);}
 }
 public static async Task<object> Focused(string suite)
 {
  var errors=new List<string>();var warnings=new List<string>();void Logged(string s,string stack,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors.Add(s+"\n"+stack);if(t==LogType.Warning)warnings.Add(s);}
  Application.logMessageReceived+=Logged;
  try{object result=suite=="controls"?await CameraControls():suite=="display"?await DisplayAndExclusions():await CameraViewsAndPeriods();Check(errors.Count==0,"Fresh runtime errors");return Save(suite+"-FreshConsole-PASS",new{result,newErrors=errors.Count,newWarnings=warnings.Count});}
  finally{Application.logMessageReceived-=Logged;Save(suite+"-FreshErrors",errors);Save(suite+"-FreshWarnings",warnings);}
 }
 public static Task<object> Controls()=>Focused("controls");
 public static Task<object> Displays()=>Focused("display");
 public static Task<object> Periods()=>Focused("periods");
 public static object UILayoutAudit()=>Save("Settings-Layout-Audit",Path("MenuOverlay/MenuSafeArea/SettingsPanel").GetComponentsInChildren<RectTransform>(true).Select(r=>new{r.name,pos=r.anchoredPosition.ToString(),size=r.sizeDelta.ToString()}));
 static float Float(Object o,string name)=>new SerializedObject(o).FindProperty(name).floatValue;
 static void Float(Object o,string name,float value){var s=new SerializedObject(o);s.FindProperty(name).floatValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
 public static async Task<object> Actions()
 {
  Input();var m=One<PrototypeMatch>();var prefs=One<GamePreferences>();prefs.SetCameraView(1);var select=One<PlayerSelection>();var ball=One<BallController>();var keyboard=InputSystem.AddDevice<Keyboard>();var ai=AI();var states=ai.Select(a=>a.enabled).ToArray();var errors=new List<string>();void Logged(string s,string stack,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors.Add(s);}
  Application.logMessageReceived+=Logged;
  try
  {
   m.RestartMatch();await Opening(m);Disable(ai);Park();var players=One<PrototypeRoster>().FieldPlayers.Where(p=>p.Team==PrototypeTeam.Blue).ToArray();var p=players[0];var receiver=players[1];p.Motor.ResetPose(Vector3.zero,Quaternion.identity);receiver.Motor.ResetPose(Vector3.forward*6,Quaternion.identity);Claim(p);await Frames(4);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.B));await Frames(3);InputSystem.QueueStateEvent(keyboard,new KeyboardState());Check(ball.ReleaseKind==BallReleaseKind.Pass,"Actual B pass in sideline view");await Until(()=>ball.Owner==receiver.Interaction,5);Check(select.ActivePlayer==receiver,"Physical reception transfers control");
   InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D));await Seconds(.25f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Seconds(.4f);Check(receiver.Motor.Facing.z>.99f,"Side screen-right steering aims toward right goal");Claim(receiver);await Frames(3);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Space));await Seconds(.55f);Check(receiver.GetComponent<ShotCharge>().IsCharging,"Actual charged shot in sideline");InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Frames(3);Check(ball.Owner==null&&ball.ReleaseKind==BallReleaseKind.Shot,"Physical charged release");prefs.SetCameraView(0);await Until(()=>m.Phase==MatchPhase.GoalRestart,5);Check(m.BlueScore==1&&m.SprintOff.Starts==1,"Goal during camera blend affects no sprint/state");prefs.SetCameraView(1);await ResumeGoal(m);
   Park();p.Motor.ResetPose(Vector3.zero,Quaternion.LookRotation(Vector3.forward));var opponent=One<PrototypeRoster>().FieldPlayers.First(x=>x.Team==PrototypeTeam.White);opponent.Motor.ResetPose(Vector3.forward*.9f,Quaternion.LookRotation(Vector3.back));Claim(opponent);select.Select(p);await Seconds(1.1f);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F));await Frames(3);InputSystem.QueueStateEvent(keyboard,new KeyboardState());Check(ball.Owner==p.Interaction,"Actual defensive F steal in sideline");
   Park();p.Motor.ResetPose(Vector3.zero,Quaternion.identity);receiver.Motor.ResetPose(Vector3.forward*6,Quaternion.identity);Claim(p);await Frames(3);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.L));await Frames(3);InputSystem.QueueStateEvent(keyboard,new KeyboardState());Check(ball.IsAirborne&&ball.ReleaseKind==BallReleaseKind.Lob,"Actual lob retained in sideline");
   var keeper=Object.FindObjectsByType<PrototypeGoalkeeper>(FindObjectsInactive.Include).Single(k=>k.Identity.Team==PrototypeTeam.White);float chance=Float(keeper,"saveChance"),catchChance=Float(keeper,"catchChance");
   try{ball.ResetForRestart(Vector3.zero);Park();var goal=keeper.DefendedGoal;keeper.enabled=true;Float(keeper,"saveChance",1);Float(keeper,"catchChance",1);keeper.Identity.Motor.ResetPose(goal.transform.position-goal.transform.forward*Float(keeper,"stationDepth"),Quaternion.LookRotation(-goal.transform.forward));p.Motor.ResetPose(goal.transform.position-goal.transform.forward*6,Quaternion.LookRotation(goal.transform.forward));Claim(p);int saves=keeper.SaveAttempts,catches=keeper.Catches;Check(p.Interaction.TryShoot(10,ShotType.Normal),"Physical keeper shot");await Until(()=>keeper.SaveAttempts>saves,5);Check(keeper.Catches>catches&&ball.Owner==keeper.Identity.Interaction,"Actual swept keeper catch, camera is display-only");await Shot("Sideline-Keeper-Save");}
   finally{Float(keeper,"saveChance",chance);Float(keeper,"catchChance",catchChance);}
   ball.ResetForRestart(Vector3.zero);m.ShotClock.AwardCentreRestart(PrototypeTeam.Blue);m.AdvanceLiveTime(m.ShotClock.Remaining);Check(m.Phase==MatchPhase.TurnoverRestart,"Actual shot clock violation remains authoritative");await Frames(3);DisplayMatches();await Opening(m);Check(m.SprintOff.Starts==1,"Turnover never creates sprint");
   Check(errors.Count==0,"No new action runtime errors");return Save("Actions-PASS",new{sidelineActualBPass=true,physicalReceptionAndSelection=true,screenRightAimsRightGoal=true,actualSpaceChargeAndRelease=true,goalDuringViewBlend=true,actualFSteal=true,actualLLob=true,physicalSweptKeeperCatch=true,keeperProbabilityTemporarilyControlled=true,shotClockViolation=true,noExtraSprint=true,newRuntimeErrors=errors.Count});
  }
  finally{Application.logMessageReceived-=Logged;Save("Actions-FreshErrors",errors);InputSystem.RemoveDevice(keyboard);RestoreAI(ai,states);m.SetPaused(false);}
 }
 static Rect RectBounds(RectTransform r){var v=new Vector3[4];r.GetWorldCorners(v);return Rect.MinMaxRect(v[0].x,v[0].y,v[2].x,v[2].y);}
 public static async Task<object> LayoutAndOrientation()
 {
  Input();var m=One<PrototypeMatch>();var prefs=One<GamePreferences>();var original=prefs.Current;var cam=One<GameplayCamera>();var lens=cam.GetComponent<Camera>();var dev=One<ArenaDeveloperCamera>();var canvas=One<Canvas>();var mouse=InputSystem.AddDevice<Mouse>();var ai=AI();var states=ai.Select(a=>a.enabled).ToArray();var records=new List<object>();var probe=dev.viewpoints[2];var pos=probe.position;var rot=probe.rotation;
  try
  {
   prefs.SetCameraView(1);m.RestartMatch();await Opening(m);Disable(ai);Park();var p=One<PrototypeRoster>().FieldPlayers.First(x=>x.Team==PrototypeTeam.Blue);p.Motor.ResetPose(Vector3.zero,Quaternion.identity);Claim(p);
   foreach(int width in new[]{1920,2340,2400})
   {
    Resize(width);await Frames(5);await Seconds(.7f);Check(Screen.width==width&&Screen.height==1080,"Actual fixed Game view resolution");records.Add(Framing("aspect "+width));await Shot("Match-HUD-"+width);var controls=canvas.GetComponentsInChildren<GameplayActionButton>().Select(b=>(RectTransform)b.transform).ToArray();foreach(var r in controls){Rect bounds=RectBounds(r);Check(bounds.xMin>=0&&bounds.xMax<=width&&bounds.yMin>=0&&bounds.yMax<=1080,"Control screen bounds");foreach(var other in controls)if(other!=r)Check(!bounds.Overlaps(RectBounds(other)),"Non-overlapping controls");}
    One<MatchMenuController>().OpenPause();One<MatchMenuController>().OpenSettings();await Frames(4);var panel=Path("MenuOverlay/MenuSafeArea/SettingsPanel");var left=panel.Find("LengthwiseCamera");var side=panel.Find("SidelineCamera");var opacity=panel.Find("ControlOpacityRow/Slider");foreach(var choice in new[]{left,side}){Check(!RectBounds((RectTransform)choice).Overlaps(RectBounds((RectTransform)opacity)),"Camera choices clear opacity slider");await Click(mouse,choice);Check(cam.View==(choice==left?MatchCameraView.Lengthwise:MatchCameraView.SidelineBroadcast),"Camera choice reachable at width "+width);}await Shot("Settings-"+width);One<MatchMenuController>().Cancel();One<MatchMenuController>().Resume();await Seconds(.7f);
   }
   foreach(int quality in new[]{0,1,2}){prefs.SetGraphics(quality);await Frames(5);Check(One<ArenaEventDisplay>().primaryScore.GetComponent<Renderer>().enabled&&One<PoolWaterVisuals>().RuntimeMaterial.shader.isSupported,"Essential board/water on quality "+quality);await Shot("Sideline-Quality-"+quality);}
   prefs.SetGraphics((int)original.graphics);m.SetPaused(true);canvas.enabled=false;var board=GameObject.Find(MatchBroadcastSetup.Name).transform;Check(Vector3.Dot(board.forward,Vector3.left)>.999f&&board.position.x<-22,"West wall panel front faces field");
   var display=One<ArenaEventDisplay>();foreach(var t in display.scores.Concat(display.clocks)){Check(t.fontSharedMaterial.GetFloat("_CullMode")==2,"Secondary digital text back faces culled");var front=t.transform.TransformPoint(new Vector3(0,0,-1));Check(Vector3.Dot(front-t.transform.position,-t.transform.forward)>.99f,"Positive front-facing digital transform");}
   var north=Object.FindObjectsByType<TMP_Text>().Where(t=>t.transform.parent.name=="Competition Deck and Clear Official Lanes"&&t.transform.position.z>0&&(t.text=="WATER POLO SUPERSTARS"||t.text=="CHAMPIONSHIP  /  MATCH COURT 01")).ToArray();Check(north.Length==2&&north.All(t=>Quaternion.Angle(t.transform.rotation,Quaternion.Euler(90,0,0))<.01f),"Corrected north deck technical rotation");
   probe.SetPositionAndRotation(new Vector3(0,12,9),Quaternion.LookRotation(new Vector3(0,.18f,18.5f)-new Vector3(0,12,9)));dev.SetView(2);await Shot("Corrected-North-Deck-Signage");dev.SetView(0);await Shot("Arena-Scoreboard-Location");for(int i=1;i<dev.viewpoints.Length;i++){dev.SetView(i);await Shot("F8-Inspection-"+i);Check(lens.transform.position==dev.viewpoints[i].position,"F8 viewpoint still used");}
   dev.SetView(-1);canvas.enabled=true;m.SetPaused(false);prefs.SetCameraView(0);await Seconds(.7f);await Shot("Lengthwise-Verified");prefs.SetCameraView(1);await Seconds(.7f);await Shot("Sideline-Verified");Check(lens.targetTexture==null,"Single output camera with no scoreboard RenderTexture");
   var metrics=board.GetComponentsInChildren<MeshRenderer>().ToArray();return Save("LayoutOrientation-PASS",new{actualGameViews=records,threeRatios=true,actualPointerSettingsAtEveryRatio=true,noCameraOpacityOverlap=true,allQualityPresets=true,frontCullDigitalText=true,noNegativeTextScale=true,northDeckRotationFixed=true,allFiveF8Views=true,scoreboardMeshRenderers=metrics.Length,boardCombinedMeshVertices=metrics.Where(r=>r.GetComponent<MeshFilter>()?.sharedMesh!=null).Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.vertexCount),scoreboardNoExtraLightsOrRenderTextures=true,originalTransparentWater=true});
  }
  finally{InputSystem.RemoveDevice(mouse);probe.SetPositionAndRotation(pos,rot);dev.SetView(-1);canvas.enabled=true;m.SetPaused(false);RestoreAI(ai,states);prefs.SetGraphics((int)original.graphics);}
 }
 public static async Task<object> ExclusionEdgesAndArena()
 {
  Input();var m=One<PrototypeMatch>();var display=One<ArenaEventDisplay>();var cam=One<GameplayCamera>();var ai=AI();var states=ai.Select(a=>a.enabled).ToArray();var dev=One<ArenaDeveloperCamera>();var canvas=One<Canvas>();var probe=dev.viewpoints[2];var pos=probe.position;var rot=probe.rotation;var errors=new List<string>();void Logged(string s,string stack,LogType t){if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors.Add(s);}
  Application.logMessageReceived+=Logged;
  try
  {
   m.RestartMatch();await Opening(m);Disable(ai);var blue=One<PrototypeRoster>().FieldPlayers.Where(p=>p.Team==PrototypeTeam.Blue).ToArray();var white=One<PrototypeRoster>().FieldPlayers.Where(p=>p.Team==PrototypeTeam.White).ToArray();var carrier=blue[0];var receiver=blue[2];var offender=white[0];int index=Array.IndexOf(display.athletes,offender);
   for(int n=1;n<=3;n++)
   {
    Park();carrier.Motor.ResetPose(Vector3.zero,Quaternion.LookRotation(Vector3.right));receiver.Motor.ResetPose(Vector3.right*6,Quaternion.identity);blue[1].Motor.ResetPose(Vector3.forward,Quaternion.identity);offender.Motor.ResetPose(Vector3.forward+Vector3.left*.6f,Quaternion.LookRotation(Vector3.right));Claim(carrier);Check(m.Rules.ReportContact(offender,blue[1],IllegalContact.Hold)&&offender.PersonalFouls==n,"Actual personal exclusion "+n);display.Refresh();
    if(n<3)Check(display.exclusionDots[index].enabled&&display.ActiveExclusions==1,"Temporary active indicator");
    else{Check(offender.PermanentlyRemoved&&!display.exclusionDots[index].enabled&&display.ActiveExclusions==0&&display.capLabels[index].text.Contains("OUT"),"Permanent removal differs from temporary indicator");await BoardShot("Scoreboard-Match-Removal");}
    await FreeThrowPass(carrier,receiver);
    if(n==1){Claim(white[1]);display.Refresh();Check(offender.EligibleForPlay&&display.ActiveExclusions==0&&!display.exclusionDots[index].enabled,"Actual own-team possession regain permits legal re-entry");}
    if(n==2){await PhysicalScore(PrototypeTeam.Blue);display.Refresh();Check(offender.EligibleForPlay&&display.ActiveExclusions==0&&!display.exclusionDots[index].enabled,"Real goal releases existing temporary exclusion");await ResumeGoal(m);}
   }
   RestoreAI(ai,states);m.RestartMatch();await Opening(m);Check(display.ActiveExclusions==0&&display.athletes.All(p=>p.EligibleForPlay),"Restart clears permanent and temporary board state");m.SetPaused(true);canvas.enabled=false;
   probe.SetPositionAndRotation(new Vector3(19,24,-26),Quaternion.LookRotation(new Vector3(-3,5,0)-new Vector3(19,24,-26)));dev.SetView(2);await Shot("Full-Arena-Scoreboard");
   // Inside a gallery's aggregate bounds during a blend/inspection must also cut it away.
   var sightlines=One<BroadcastSightlines>();var group=Field<Renderer[]>(sightlines,"eastGallery");var bounds=group[0].bounds;foreach(var r in group.Skip(1))bounds.Encapsulate(r.bounds);probe.position=bounds.center;probe.LookAt(Vector3.zero);await Frames(4);Check(group.All(r=>r.forceRenderingOff),"Camera inside decorative gallery never renders through it");dev.SetView(-1);await Frames(3);
   var pose=cam.CaptureCameraPose();Check(pose.view==cam.View&&pose.fieldOfView>0,"Read-only replay camera snapshot");Vector3 ball=One<BallController>().PhysicsPosition;float clock=m.Clock.Remaining;cam.CaptureCameraPose();Check(ball==One<BallController>().PhysicsPosition&&clock==m.Clock.Remaining,"Replay snapshot has no live match side effect");var selected=One<PlayerSelection>().ActivePlayer;cam.SetTarget(null);await Frames(3);Check(cam.Target!=null,"Missing camera target has safe eligible fallback");cam.SetTarget(selected.Motor);Check(errors.Count==0,"No new edge runtime errors");
   return Save("ExclusionEdgesArena-PASS",new{actualRegainReentry=true,actualGoalReentry=true,thirdPersonalRemovalClearlySeparate=true,restartClearsPermanentState=true,fullArenaGameView=true,insideGalleryCutaway=true,readOnlyCameraSnapshot=true,noReplayPlaybackClaim=true,newRuntimeErrors=errors.Count});
  }
  finally{Application.logMessageReceived-=Logged;Save("Edges-FreshErrors",errors);probe.SetPositionAndRotation(pos,rot);dev.SetView(-1);canvas.enabled=true;m.SetPaused(false);RestoreAI(ai,states);}
 }
 public static async Task<object> FinalInspectionFrames()
 {
  var m=One<PrototypeMatch>();var dev=One<ArenaDeveloperCamera>();var canvas=One<Canvas>();var lens=One<GameplayCamera>().GetComponent<Camera>();float fov=lens.fieldOfView;
  try{m.SetPaused(true);canvas.enabled=false;dev.SetView(0);lens.fieldOfView=52;await Shot("After-Arena-Matched");lens.fieldOfView=64;await Shot("Full-Arena-Scoreboard");return new{matchingBeforeArenaPose=true,matchedFov=52,additionalWideInspectionFov=64};}
  finally{lens.fieldOfView=fov;dev.SetView(-1);canvas.enabled=true;m.SetPaused(false);}
 }

}
