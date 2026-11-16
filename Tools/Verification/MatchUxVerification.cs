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
using Object = UnityEngine.Object;

public static class MatchUxVerification
{
 const string Out="Logs/MatchUX/";
 const string Scene="Assets/Scenes/OlympicMatchArena.unity";
 static T One<T>() where T:Object=>Object.FindAnyObjectByType<T>();
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static object Save(string name,object value){Directory.CreateDirectory(Out);File.WriteAllText(Out+name+".json",JsonConvert.SerializeObject(value,Formatting.Indented));return value;}
 static string Hash(string path)=>Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(path)));
 static T Field<T>(object o,string n)=>(T)o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
 static string Id(Object o)=>GlobalObjectId.GetGlobalObjectIdSlow(o).ToString();
 static Dictionary<string,string> Protected()
 {
  var records=new Dictionary<string,string>();
  foreach(var c in Object.FindObjectsByType<Component>(FindObjectsInactive.Include))
  {
   if(c==null || c.GetComponentInParent<Canvas>(true)!=null || c is EventSystem || c is UnityEngine.InputSystem.UI.InputSystemUIInputModule)continue;
   // Every existing world component (including all transforms/rigs) is protected.
   records[Id(c)]=EditorJsonUtility.ToJson(c);
  }
  return records;
 }
 public static object Baseline()
 {
  Check(!Application.isPlaying,"Saved Edit Mode required");for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)Check(!UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty,"Unsaved scene edits");
  OlympicArenaSetup.Open();
  var files=new[]{"Assets/Scenes/Boot.unity","Assets/Scenes/MainLobby.unity","Assets/Scenes/PrototypePool.unity","ProjectSettings/EditorBuildSettings.asset","Assets/Resources/HubCatalog.asset","Assets/Resources/DivisionCatalog.asset","Assets/Resources/HubMatchDestination.asset"};
  Save("Baseline",new{sceneGuid=AssetDatabase.AssetPathToGUID(Scene),sceneHash=Hash(Scene),files=files.Select(p=>new{path=p,hash=Hash(p)}),records=Protected()});
  var canvas=One<Canvas>();
  Save("Hierarchy-Before",canvas.GetComponentsInChildren<Transform>(true).Select(t=>new{name=t.name,parent=t.parent?.name,active=t.gameObject.activeSelf,components=t.GetComponents<Component>().Select(c=>c?.GetType().Name)}).ToArray());
  return new{scene=Scene,dirty=canvas.gameObject.scene.isDirty,athletes=One<PrototypeRoster>().Members.Count,canvases=Object.FindObjectsByType<Canvas>().Length,clock=JsonUtility.ToJson(One<PrototypeMatch>().Clock)};
 }
 public static object Structure()
 {
  Check(!Application.isPlaying,"Edit Mode");var original=JObject.Parse(File.ReadAllText(Out+"Baseline.json"));
  Check(Hash(MatchUxSetup.Recovery)==(string)original["sceneHash"],"Exact native recovery");Check(AssetDatabase.AssetPathToGUID(Scene)==(string)original["sceneGuid"],"Scene GUID retained");
  foreach(var f in original["files"])Check(Hash((string)f["path"])==(string)f["hash"],"Untouched "+f["path"]);
  var current=Protected();var differences=((JObject)original["records"]).Properties().Where(p=>!current.ContainsKey(p.Name)||current[p.Name]!=(string)p.Value).Select(p=>p.Name).ToArray();
  Save("WorldDifferences",differences);Check(differences.Length==0,"All original world/physics/rig/AI components preserved");
  Check(Object.FindObjectsByType<Canvas>().Length==1&&Object.FindObjectsByType<EventSystem>().Length==1,"One Canvas/EventSystem");
  foreach(var g in One<Canvas>().GetComponentsInChildren<RoundedPanelGraphic>(true))Check(g.GetComponent<CanvasRenderer>()!=null,"CanvasRenderer present");
  foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"No missing scripts");
  string hash=Hash(Scene);MatchUxSetup.SetUp();Check(Hash(Scene)==hash,"Repeat setup preserves tuning");EditorSceneManager.OpenScene(Scene);Check(One<MatchControlPresentation>()!=null,"Saved/reloaded wiring");
  return Save("Structure-PASS",new{protectedWorldComponents=current.Count,exactRecovery=true,sameSceneGuid=true,oneCanvasAndEventSystem=true,repeatSafe=true,sceneReload=true,entryEconomyAndLegacyScenesUnchanged=true});
 }
 static async Task Until(Func<bool> p,float seconds=12){double end=EditorApplication.timeSinceStartup+seconds;while(!p()){Check(EditorApplication.timeSinceStartup<end,"Timed out waiting for expected state");await Task.Delay(15);}}
 static async Task Frames(int n){int end=Time.frameCount+n;await Until(()=>Time.frameCount>=end);}
 static async Task Seconds(float seconds){double end=EditorApplication.timeSinceStartup+seconds;while(EditorApplication.timeSinceStartup<end)await Task.Delay(15);}
 static async Task Shot(string name){await Seconds(.25f);await Frames(4);ScreenCapture.CaptureScreenshot(Out+name+".png");await Frames(5);}
 static void Resize(int width,bool restore=false)
 {
  var flags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;var asm=typeof(UnityEditor.Editor).Assembly;var type=asm.GetType("UnityEditor.GameView");var win=EditorWindow.GetWindow(type);var selected=type.GetProperty("selectedSizeIndex",flags);var sizes=typeof(ScriptableSingleton<>).MakeGenericType(asm.GetType("UnityEditor.GameViewSizes")).GetProperty("instance",flags).GetValue(null);var group=sizes.GetType().GetMethod("GetGroup",flags).Invoke(sizes,new[]{type.GetProperty("currentSizeGroupType",flags).GetValue(win)});
  int added=SessionState.GetInt("MatchUX.Added",-1),old=SessionState.GetInt("MatchUX.Size",-1);
  if(added>=0){if(old>=0)selected.SetValue(win,old);group.GetType().GetMethod("RemoveCustomSize").Invoke(group,new object[]{added});SessionState.SetInt("MatchUX.Added",-1);}
  if(restore){SessionState.SetInt("MatchUX.Size",-1);return;}
  if(old<0)SessionState.SetInt("MatchUX.Size",(int)selected.GetValue(win));
  var size=Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),width,1080,"M16 temporary verification"});group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});added=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1;SessionState.SetInt("MatchUX.Added",added);selected.SetValue(win,added);win.Focus();
 }
 public static object Direct(){Resize(1920);EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(Scene);return new{entry=Scene};}
 public static async Task<object> Preview(){Check(Application.isPlaying,"Play Mode");One<PrototypeMatch>().RestartMatch();await Seconds(.35f);await Shot("First-Live");One<MatchMenuController>().OpenPause();await Shot("First-Pause");return new{frame=Time.frameCount,paused=One<PrototypeMatch>().IsPaused};}
 public static object VerificationInput()
 {
  if(SessionState.GetInt("MatchUX.Background",-1)<0){SessionState.SetInt("MatchUX.Background",(int)InputSystem.settings.backgroundBehavior);SessionState.SetInt("MatchUX.EditorInput",(int)InputSystem.settings.editorInputBehaviorInPlayMode);}
  InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;return new{testInputFocusOverride=true};
 }
 sealed class Fixture:IDisposable
 {
  public PrototypeMatch match=One<PrototypeMatch>();public BallController ball=One<BallController>();public PrototypeRoster roster=One<PrototypeRoster>();public PlayerSelection selection=One<PlayerSelection>();public MatchMenuController menus=One<MatchMenuController>();
  public SwimmerIdentity[] blue,white;Behaviour[] ai;bool[] enabled;public bool resetOnDispose=true;
  public Fixture(){Check(Application.isPlaying,"Actual arena Play Mode");ai=Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>().Concat(Object.FindObjectsByType<PrototypeGoalkeeper>()).ToArray();enabled=ai.Select(a=>a.enabled).ToArray();foreach(var a in ai)a.enabled=false;blue=roster.FieldPlayers.Where(p=>p.Team==PrototypeTeam.Blue).ToArray();white=roster.FieldPlayers.Where(p=>p.Team==PrototypeTeam.White).ToArray();Reset();}
  public void Reset(){match.RestartMatch();Park();}
  public void Park(){int b=0,w=0;foreach(var p in roster.FieldPlayers)p.Motor.ResetPose(new Vector3(p.Team==PrototypeTeam.Blue?-8:8,0,-11+4*(p.Team==PrototypeTeam.Blue?b++:w++)),Quaternion.identity);}
  public void Claim(SwimmerIdentity p){ball.ResetForRestart(p.Motor.Position);Check(p.Interaction.TryCollect(),"Range-checked collection");}
  public PrototypeTeamTactics Team(PrototypeTeam t)=>Object.FindObjectsByType<PrototypeTeamTactics>().Single(x=>x.Team==t);
  public void Dispose(){if(match!=null&&resetOnDispose)match.RestartMatch();for(int i=0;i<ai.Length;i++)if(ai[i]!=null)ai[i].enabled=enabled[i];}
 }
 static async Task Click(Mouse mouse,Transform target)
 {
  await Frames(2); // Wait for the newly loaded uGUI page to finish its first layout.
  Check(target!=null&&target.gameObject.activeInHierarchy,"Visible pointer target");var r=(RectTransform)target;Vector2 p=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));
  var data=new PointerEventData(EventSystem.current){position=p};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);Check(hits.Any()&&hits[0].gameObject.transform.IsChildOf(target),"Pointer raycast reaches "+target.name);
  InputSystem.QueueStateEvent(mouse,new MouseState{position=p});await Seconds(.05f);InputSystem.QueueStateEvent(mouse,new MouseState{position=p,buttons=1});await Seconds(.06f);InputSystem.QueueStateEvent(mouse,new MouseState{position=p});await Seconds(.08f);
 }
 static Transform Path(string path)=>One<Canvas>().transform.Find(path);
 const string Menu="MenuOverlay/MenuSafeArea/";
 static async Task Escape(Keyboard keyboard){InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));await Seconds(.08f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Seconds(.08f);}
 public static async Task<object> MenusAndSettings()
 {
  VerificationInput();using(var f=new Fixture())
  {
   var mouse=InputSystem.AddDevice<Mouse>();var keyboard=InputSystem.AddDevice<Keyboard>();var prefs=One<GamePreferences>();bool had=PlayerPrefs.HasKey(GamePreferences.StorageKey);string saved=PlayerPrefs.GetString(GamePreferences.StorageKey,"");
   try
   {
    await Seconds(.12f);await Click(mouse,Path("MatchSafeArea/PauseButton"));Check(f.match.IsPaused&&f.menus.Page==MatchMenuPage.Pause&&Time.timeScale==0,"Actual pointer Pause");float clock=f.match.Clock.Remaining,shot=f.match.ShotClock.Remaining;await Shot("Pause-Verified");
    await Click(mouse,Path(Menu+"PausePanel/ControlsButton"));Check(f.menus.Page==MatchMenuPage.Controls,"Controls page reachable");await Shot("Controls-Verified");await Click(mouse,Path(Menu+"ControlsPanel/Close"));Check(f.menus.Page==MatchMenuPage.Pause,"Controls X returns to Pause");
    await Click(mouse,Path(Menu+"PausePanel/Settings"));Check(f.menus.Page==MatchMenuPage.Settings,"Settings reachable");
    Path(Menu+"SettingsPanel/masterRow/Slider").GetComponent<UnityEngine.UI.Slider>().value=.63f;Path(Menu+"SettingsPanel/sfxRow/Slider").GetComponent<UnityEngine.UI.Slider>().value=.42f;
    Path(Menu+"SettingsPanel/ControlOpacityRow/Slider").GetComponent<UnityEngine.UI.Slider>().value=.7f;
    await Click(mouse,Path(Menu+"SettingsPanel/LargeControls"));await Click(mouse,Path(Menu+"SettingsPanel/Low"));Check(Mathf.Abs(AudioListener.volume-.63f)<.001f&&One<MatchAtmosphere>().SfxVolume==.42f,"Real audio settings");Check(One<WaterInteractionEffects>().Quality==WaterEffectsQuality.Off,"Real graphics preset");
    Check(prefs.Current.controlSize==2&&Mathf.Abs(Path("MatchSafeArea/MobileControls/RightThumb").localScale.x-1.12f)<.001f&&Mathf.Abs(Path("MatchSafeArea/MobileControls/RightThumb").GetComponent<CanvasGroup>().alpha-.7f)<.001f,"Control size and opacity applied");
    await Shot("Settings-Verified");await Click(mouse,Path(Menu+"SettingsPanel/Close"));prefs.Reload();Check(prefs.Current.controlSize==2&&Mathf.Abs(prefs.Current.controlOpacity-.7f)<.001f,"Preferences persisted");
    Check(f.match.Clock.Remaining==clock&&f.match.ShotClock.Remaining==shot,"Both clocks stay frozen through menus");
    await Click(mouse,Path(Menu+"PausePanel/Restart"));Check(f.menus.Page==MatchMenuPage.RestartConfirmation,"Restart confirmation");await Shot("Restart-Confirmation");await Click(mouse,Path(Menu+"ConfirmationPanel/Close"));Check(f.match.IsPaused&&f.menus.Page==MatchMenuPage.Pause,"Restart X cancels");
    await Click(mouse,Path(Menu+"PausePanel/Exit"));Check(f.menus.Page==MatchMenuPage.ExitConfirmation,"Exit confirmation");await Click(mouse,Path(Menu+"ConfirmationPanel/Cancel"));Check(f.match.IsPaused,"Exit cancel preserves pause");
    await Click(mouse,Path(Menu+"PausePanel/Restart"));await Click(mouse,Path(Menu+"ConfirmationPanel/Confirm"));Check(!f.match.IsPaused&&f.match.IsPlaying&&f.match.Clock.Quarter==1&&f.match.BlueScore==0&&f.match.WhiteScore==0,"Confirmed restart resets current authority");
    for(int i=0;i<3;i++){await Escape(keyboard);Check(f.match.IsPaused,"Escape pauses");await Escape(keyboard);Check(!f.match.IsPaused&&Time.timeScale==1,"Escape resumes");}
    Check(Object.FindObjectsByType<MatchMenuController>().Length==1&&Object.FindObjectsByType<Canvas>().Length==1,"No duplicate menus");
    return Save("Menus-PASS",new{actualInputSystemMouseAndKeyboard=true,controlsPageAndX=true,pauseAndEscape=true,settingsAppliedAndPersisted=true,restartConfirmAndCancel=true,exitCancel=true,clocksStopped=true,repeatedResume=true,noDuplicateMenus=true});
   }
   finally{InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);if(had)PlayerPrefs.SetString(GamePreferences.StorageKey,saved);else PlayerPrefs.DeleteKey(GamePreferences.StorageKey);PlayerPrefs.Save();prefs.Reload();}
  }
 }
 static async Task Goal(Fixture f,SwimmerIdentity player)
 {
  f.Park();var goal=f.Team(player.Team).AttackingGoal;player.Motor.ResetPose(goal.transform.position-goal.transform.forward*5+goal.transform.right*.7f,Quaternion.LookRotation(goal.transform.forward));f.selection.Select(player);f.Claim(player);Check(player.Interaction.TryShoot(22,ShotType.Normal),"Real normal shot released");await Until(()=>f.match.Phase==MatchPhase.GoalRestart,6);
 }
 public static async Task<object> EventsAndClocks()
 {
  using(var f=new Fixture())
  {
   var banner=One<MatchNotificationBanner>();f.match.AdvanceLiveTime(3);f.match.SetPaused(true);await Frames(3);Check(Field<TMP_Text>(One<MatchHUD>(),"clockLabel").text=="7:42","6x authoritative period display");f.match.SetPaused(false);
   f.Reset();f.Claim(f.blue[0]);f.match.AdvanceLiveTime(23);f.match.SetPaused(true);await Frames(3);var shotLabel=Field<TMP_Text>(One<MatchHUD>(),"shotClockLabel");Check(shotLabel.text=="05"&&shotLabel.color.r>.9f&&shotLabel.color.g<.5f,"Low shot-clock warning from authority");f.match.SetPaused(false);
   f.Reset();f.Claim(f.blue[0]);f.match.AdvanceLiveTime(28);Check(f.match.Phase==MatchPhase.TurnoverRestart&&banner.Title=="SHOT CLOCK VIOLATION","Real shot-clock expiration drives banner");await Shot("Shot-Clock-Violation");await Until(()=>f.match.IsPlaying);
   f.Reset();await Goal(f,f.blue[0]);Check(f.match.BlueScore==1&&banner.Title=="GOAL","Blue goal notification uses actual score");await Shot("Blue-Goal");await Until(()=>f.match.IsCentreRestart);Check(banner.Title=="CENTER RESTART","Unified centre event");await Until(()=>f.match.IsPlaying);
   f.Reset();await Goal(f,f.white[0]);await Until(()=>f.match.Phase==MatchPhase.CentreRestartReady);Check(f.match.CentreTaker.Team==PrototypeTeam.Blue&&banner.Title=="CENTER RESTART","Human centre ready instruction");await Shot("Blue-Center-Restart");float clock=f.match.Clock.Remaining,shot=f.match.ShotClock.Remaining;await Seconds(.4f);Check(f.match.Clock.Remaining==clock&&f.match.ShotClock.Remaining==shot,"Centre clocks stopped");Check(One<MobileMatchControls>().Begin(MobileMatchAction.Pass),"UI PASS legally restarts");await Frames(4);Check(f.match.IsPlaying&&f.match.Clock.Remaining<clock,"Clock starts on physical release");
   f.Reset();f.blue[0].Motor.ResetPose(Vector3.zero,Quaternion.identity);f.white[0].Motor.ResetPose(Vector3.back*.9f,Quaternion.identity);f.Claim(f.blue[0]);Check(f.match.Rules.ReportContact(f.white[0],f.blue[0],IllegalContact.Push),"Ordinary foul fixture");Check(banner.Title=="FREE THROW","Free throw banner");await Shot("Free-Throw");
   f.Reset();f.blue[0].Motor.ResetPose(Vector3.zero,Quaternion.identity);f.blue[1].Motor.ResetPose(Vector3.right*3,Quaternion.identity);f.white[0].Motor.ResetPose(new Vector3(3,0,-.9f),Quaternion.identity);f.Claim(f.blue[0]);Check(f.match.Rules.ReportContact(f.white[0],f.blue[1],IllegalContact.Hold),"Real off-ball exclusion");Check(banner.Title=="EXCLUSION","Exclusion priority");f.menus.OpenPause();float excluded=f.white[0].ExclusionRemaining;await Seconds(.4f);Check(f.match.Phase==MatchPhase.FreeThrowSetup&&f.white[0].ExclusionRemaining==excluded,"Exclusion/setup pause");f.menus.Resume();await Shot("Exclusion");await Until(()=>f.match.Phase==MatchPhase.FreeThrowReady);Check(banner.Title=="EXCLUSION","Ready signal cannot replace important exclusion");Check(f.blue[0].GetComponent<SwimmerPassing>().TryPass(),"Free throw remains legal");
   f.Reset();var goal=f.Team(PrototypeTeam.Blue).AttackingGoal;Vector3 spot=goal.transform.position-goal.transform.forward*5;spot.y=0;f.blue[0].Motor.ResetPose(spot,Quaternion.LookRotation(goal.transform.forward));f.white[0].Motor.ResetPose(spot-goal.transform.forward*.9f,Quaternion.LookRotation(goal.transform.forward));f.Claim(f.blue[0]);Check(f.blue[0].GetComponent<ShotCharge>().BeginCharge(),"Shooting intent");Check(f.match.Rules.ReportContact(f.white[0],f.blue[0],IllegalContact.Push)&&f.match.Phase==MatchPhase.PenaltySetup&&banner.Title=="PENALTY","Authoritative penalty notification");await Shot("Penalty");await Until(()=>f.match.Phase==MatchPhase.PenaltyReady);Check(f.blue[0].Interaction.TryShoot(22),"Penalty release");f.menus.OpenPause();Vector3 ballPos=f.ball.PhysicsPosition,velocity=f.ball.Velocity;await Seconds(.4f);Check(f.match.Phase==MatchPhase.PenaltyFlight&&Vector3.Distance(ballPos,f.ball.PhysicsPosition)<.0001f&&f.ball.Velocity==velocity,"Penalty flight frozen with original velocity");f.menus.Resume();await Seconds(.15f);Check(Vector3.Distance(ballPos,f.ball.PhysicsPosition)>.1f,"Penalty physical flight resumes");
   f.Reset();var phases=new List<string>();
   for(int q=1;q<=4;q++)
   {
    f.ball.ResetForRestart(Vector3.zero);f.match.ShotClock.ResetPossession();f.match.AdvanceLiveTime(f.match.Clock.Remaining);await Frames(3);Check(f.menus.Page==MatchMenuPage.Transition,"Period transition menu");phases.Add(f.match.Phase.ToString());await Shot(q==2?"Halftime":q==4?"Full-Time":"Period-"+q);
    if(q<4){f.menus.Continue();Check(f.match.Clock.Quarter==q+1,"Next quarter correct");if(q==2)Check(f.match.EndsSwitched&&f.match.EndSwitchCount==1,"Exactly one halftime side swap");}
   }
   Check(f.match.Phase==MatchPhase.FullTime&&Field<TMP_Text>(f.menus,"transitionTitle").text=="DRAW","Correct full-time draw");
   Check(!Path("MatchSafeArea/EventCard").gameObject.activeSelf&&!Path("MatchSafeArea/GoalLabel").gameObject.activeSelf,"Retired overlapping feeds inactive");
   return Save("Events-PASS",new{periodDisplayRatio=6,independent28SecondPossession=true,lowClockWarning=true,realShotClockViolation=true,physicalGoalsBothTeams=true,centerRestartAndClocks=true,ordinaryFoul=true,exclusionPriority=true,penaltyPauseAndPhysicalResume=true,periodPhases=phases,halftimeSwapsOnce=true,fullTimeDraw=true});
  }
 }
 static Rect ScreenRect(RectTransform r){var v=new Vector3[4];r.GetWorldCorners(v);return Rect.MinMaxRect(v.Min(x=>x.x),v.Min(x=>x.y),v.Max(x=>x.x),v.Max(x=>x.y));}
 static void Bounds(bool menus)
 {
  var interactive=menus?One<Canvas>().GetComponentsInChildren<UnityEngine.UI.Selectable>().Where(b=>b.gameObject.activeInHierarchy).Select(b=>(RectTransform)b.transform).ToArray():One<Canvas>().GetComponentsInChildren<GameplayActionButton>().Where(b=>b.gameObject.activeInHierarchy).Select(b=>(RectTransform)b.transform).Concat(new[]{(RectTransform)One<VirtualSwimJoystick>().transform,Path("MatchSafeArea/PauseButton") as RectTransform}).ToArray();
  foreach(var r in interactive){var bounds=ScreenRect(r);Check(bounds.xMin>=69&&bounds.xMax<=Screen.width-69&&bounds.yMin>=23&&bounds.yMax<=Screen.height-23,"Safe-area bounds "+r.name);}
  for(int i=0;i<interactive.Length;i++)for(int j=i+1;j<interactive.Length;j++)Check(!ScreenRect(interactive[i]).Overlaps(ScreenRect(interactive[j])),"No overlapping interactive regions: "+interactive[i].name+" / "+interactive[j].name);
  foreach(var t in One<Canvas>().GetComponentsInChildren<TMP_Text>()){t.ForceMeshUpdate();Check(!t.isTextOverflowing,"No text overflow: "+t.name+" / "+t.text);}
 }
 public static async Task<object> Layouts()
 {
  using(var f=new Fixture())
  {
   var prefs=One<GamePreferences>();var value=prefs.Current;var results=new List<object>();var safe=Object.FindObjectsByType<SafeAreaLayout>();
   try
   {
    prefs.SetControlSize(2);prefs.SetControlOpacity(.88f);
    foreach(int width in new[]{1920,2340,2400})
    {
     Resize(width);await Frames(6);Check(Screen.width==width&&Screen.height==1080,"Actual Game-view size");foreach(var a in safe){a.enabled=false;a.Apply(new Rect(70,24,width-140,1032),new Vector2(width,1080));}
     f.Reset();f.Claim(f.blue[0]);await Frames(4);Bounds(false);await Shot("Layout-"+width+"-Attack");
     f.ball.ResetForRestart(f.white[0].Motor.Position);f.Claim(f.white[0]);f.selection.Select(f.blue[0]);await Frames(4);Bounds(false);await Shot("Layout-"+width+"-Defense");
     f.menus.OpenPause();await Frames(4);Bounds(true);await Shot("Layout-"+width+"-Pause");f.menus.OpenSettings();await Frames(4);Bounds(true);await Shot("Layout-"+width+"-Settings");f.menus.Cancel();f.menus.OpenControls();await Frames(4);Bounds(true);await Shot("Layout-"+width+"-Controls");f.menus.Cancel();f.menus.AskRestart();await Frames(4);Bounds(true);await Shot("Layout-"+width+"-Confirm");f.menus.Cancel();f.menus.Resume();
     results.Add(new{width,height=1080,simulatedInsets="70 side / 24 vertical",controlSize="Large",attackDefenseAndFourModalViews=true,interactiveBoundsAndNonoverlap=true,textOverflow=false});
     foreach(var a in safe)a.enabled=true;
    }
    return Save("Layouts-PASS",new{actualGameViews=18,sizes=results,deviceTested=false});
   }
   finally{foreach(var a in safe)if(a!=null)a.enabled=true;prefs.SetControlSize(value.controlSize);prefs.SetControlOpacity(value.controlOpacity);prefs.Save();Resize(0,true);}
  }
 }
 public static async Task<object> TouchAndGamepad()
 {
  VerificationInput();Resize(1920);using(var f=new Fixture())
  {
   var pad=InputSystem.AddDevice<Gamepad>();var touch=InputSystem.AddDevice<Touchscreen>();
   try
   {
    f.blue[0].Motor.ResetPose(Vector3.zero,Quaternion.identity);f.selection.Select(f.blue[0]);f.ball.ResetForRestart(new Vector3(8,0,8));await Frames(4);
    InputSystem.QueueStateEvent(pad,new GamepadState{leftStick=Vector2.up});await Seconds(.65f);Check(f.blue[0].Motor.Velocity.z>1,"Existing gamepad movement");InputSystem.QueueStateEvent(pad,new GamepadState());await Seconds(.2f);
    f.Claim(f.blue[0]);await Frames(4);InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));await Seconds(.3f);Check(f.blue[0].GetComponent<ShotCharge>().IsCharging,"Gamepad South charges");InputSystem.QueueStateEvent(pad,new GamepadState());await Seconds(.08f);Check(f.ball.Owner==null&&f.ball.ReleaseKind==BallReleaseKind.Shot,"Gamepad release shoots");
    f.Reset();f.blue[0].Motor.ResetPose(Vector3.zero,Quaternion.LookRotation(Vector3.right));f.selection.Select(f.blue[0]);f.ball.ResetForRestart(new Vector3(8,0,8));await Frames(4);
    var stick=One<VirtualSwimJoystick>();var r=(RectTransform)stick.transform;Vector2 center=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));Vector2 drag=center+new Vector2(75,0);var sprint=Object.FindObjectsByType<GameplayActionButton>().Single(b=>b.Action==MobileMatchAction.Sprint);var sr=(RectTransform)sprint.transform;Vector2 sprintPoint=RectTransformUtility.WorldToScreenPoint(null,sr.TransformPoint(sr.rect.center));
    InputSystem.QueueStateEvent(touch,new TouchState{touchId=11,phase=UnityEngine.InputSystem.TouchPhase.Began,position=center});await Seconds(.06f);
    InputSystem.QueueStateEvent(touch,new TouchState{touchId=11,phase=UnityEngine.InputSystem.TouchPhase.Moved,position=drag});await Seconds(.2f);
    InputSystem.QueueStateEvent(touch,new TouchState{touchId=12,phase=UnityEngine.InputSystem.TouchPhase.Began,position=sprintPoint});await Seconds(.25f);
    Check(stick.Value.x>.8f&&One<MobileMatchControls>().SprintHeld&&f.blue[0].GetComponent<SwimmerStamina>().IsSprinting,"Simultaneous synthetic touchscreen joystick and sprint via actual UI module");
    InputSystem.QueueStateEvent(touch,new TouchState{touchId=12,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=sprintPoint});InputSystem.QueueStateEvent(touch,new TouchState{touchId=11,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=drag});await Seconds(.3f);Check(stick.Value==Vector2.zero&&!One<MobileMatchControls>().SprintHeld,"Both real UI-module touches release");
    return Save("TouchGamepad-PASS",new{inputSystemSyntheticTouchscreen=true,twoSimultaneousPointerIds=true,joystickSprintRelease=true,existingGamepadMovementAndChargedShot=true,physicalTouchscreenTested=false});
   }
   finally{InputSystem.RemoveDevice(pad);InputSystem.RemoveDevice(touch);}
  }
 }
 sealed class MemoryRepository:IProfileRepository
 {
  public string json;
  public LocalProfile Load()=>json==null?null:LocalProfileCodec.FromJson(json);
  public void Save(LocalProfile profile)=>json=JsonUtility.ToJson(profile);
 }
 public static object BootEntry()
 {
  Check(!Application.isPlaying,"Edit Mode");Resize(1920);EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Boot.unity");
  return Save("HubUserSaveBaseline",new{hash=File.Exists(HubApplication.SavePath)?Hash(HubApplication.SavePath):null,entry="Boot"});
 }
 static UnityEngine.UI.Button NamedButton(string name)=>Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.name==name);
 static async Task LaunchDivision(Mouse mouse)
 {
  await Click(mouse,One<HubView>().home.play.transform);await Click(mouse,NamedButton("Mode_division_1").transform);await Click(mouse,NamedButton("LaunchMode").transform);await Until(()=>One<PrototypeMatch>()!=null&&One<HubMatchBridge>()!=null);await Frames(8);Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="OlympicMatchArena","Current destination scene");
 }
 static async Task FinishMatch(bool blueWin)
 {
  using(var f=new Fixture())
  {
   // Fixture's Restart goes through the real HubMatchBridge.NewMatch session path.
   if(blueWin)await Goal(f,f.blue[0]);else await Goal(f,f.white[0]);await Until(()=>f.match.IsPlaying || f.match.Phase==MatchPhase.CentreRestartReady);
   if(f.match.Phase==MatchPhase.CentreRestartReady)Check(f.match.TryCentrePass(f.match.CentreTaker),"Human restart fixture pass");
   for(int q=1;q<=4;q++){f.ball.ResetForRestart(Vector3.zero);f.match.ShotClock.ResetPossession();f.match.AdvanceLiveTime(f.match.Clock.Remaining);if(q<4)f.menus.Continue();}
   Check(f.match.Phase==MatchPhase.FullTime&&Field<TMP_Text>(f.menus,"transitionTitle").text==(blueWin?"VICTORY":"DEFEAT"),"Correct final outcome presentation");
   // Do not Dispose while the official FullTime receipt is being reviewed: disposal restarts a match.
   await Shot(blueWin?"Official-Victory":"Official-Defeat");
   f.resetOnDispose=false;await FinishReceipt();
  }
 }
 static async Task FinishReceipt()
 {
  var app=HubApplication.Instance;var service=app.Progression;var snapshot=service.Snapshot;var bridge=One<HubMatchBridge>();
  Check(snapshot.pendingResult!=null&&snapshot.activeMatch==null,"Bridge saved official result at FullTime");string receipt=snapshot.pendingResult.id;int coins=snapshot.coins;int history=snapshot.history.Count;long revision=snapshot.revision;
  Check(bridge.TrySaveResult()&&bridge.TrySaveResult(),"Bridge retries accepted without duplicate transactions");Check(service.Snapshot.coins==coins&&service.Snapshot.history.Count==history&&service.Snapshot.revision==revision,"Repeated bridge submit has no reward side effects");
  Check(!service.CompleteMatch(receipt,9,0,out _),"Receipt replay rejected by authority");Check(service.Snapshot.coins==coins,"Replay cannot alter balance");
  One<MatchMenuController>().AskExit();Check(Field<TMP_Text>(One<MatchMenuController>(),"confirmationBody").text.Contains("completed"),"Completed exit describes preserved result");One<MatchMenuController>().Cancel();
  One<MatchMenuController>().Continue();await Until(()=>One<HubView>()!=null&&One<HubView>().Page=="Result");Check(Time.timeScale==1&&!AudioListener.pause,"Lobby not left frozen");await Shot("Official-Reward-Result");
 }
 public static async Task<object> HubLoop()
 {
  VerificationInput();await Until(()=>One<HubView>()!=null&&One<HubView>().Service!=null);var app=HubApplication.Instance;var original=app.Progression;var repository=new MemoryRepository();var service=new HubProgression(original.Catalog,repository);app.UseVerificationProfile(service);var mouse=InputSystem.AddDevice<Mouse>();
  try
  {
   var view=One<HubView>();app.OpenLobby();await Until(()=>One<HubView>()!=null&&One<HubView>()!=view);await Frames(5);int startCoins=service.Snapshot.coins;
   await LaunchDivision(mouse);Check(One<MatchControlPresentation>()!=null&&One<PrototypeRoster>().Members.Count==14,"Normal lobby launches new UI and all14 athletes");await Shot("Hub-Launched-Current-Arena");
   string first=service.Snapshot.activeMatch.id;await Click(mouse,Path("MatchSafeArea/PauseButton"));await Click(mouse,Path(Menu+"PausePanel/Restart"));await Click(mouse,Path(Menu+"ConfirmationPanel/Cancel"));Check(service.Snapshot.activeMatch.id==first,"Cancelled restart keeps session");
   await Click(mouse,Path(Menu+"PausePanel/Restart"));await Click(mouse,Path(Menu+"ConfirmationPanel/Confirm"));await Frames(4);Check(service.Snapshot.activeMatch.id!=first&&service.Snapshot.coins==startCoins&&service.Snapshot.played==0,"Restart creates one fresh official session without rewards");
   await Click(mouse,Path("MatchSafeArea/PauseButton"));await Click(mouse,Path(Menu+"PausePanel/Exit"));await Shot("Hub-Exit-Confirmation");await Click(mouse,Path(Menu+"ConfirmationPanel/Confirm"));await Until(()=>One<HubView>()!=null&&One<HubView>().Page=="Lobby");Check(service.Snapshot.activeMatch==null&&service.Snapshot.pendingResult==null&&service.Snapshot.coins==startCoins&&service.Snapshot.played==0,"Abandonment returns home with no completed rewards");Check(Time.timeScale==1&&!AudioListener.pause,"Exit unfreezes lobby");
   await LaunchDivision(mouse);await FinishMatch(true);Check(service.Snapshot.played==1&&service.Snapshot.wins==1,"One completed official win");
   await Click(mouse,NamedButton("Continue").transform);await Until(()=>One<HubView>().Page=="Lobby");
   await LaunchDivision(mouse);await FinishMatch(false);Check(service.Snapshot.played==2&&service.Snapshot.wins==1&&service.Snapshot.history.Count==2,"Win and defeat recorded once each");
   var reloaded=new HubProgression(original.Catalog,repository);Check(reloaded.Snapshot.played==2&&reloaded.Snapshot.coins==service.Snapshot.coins&&reloaded.Snapshot.pendingResult!=null,"Saved profile reload retains exact receipt/balance");
   await Click(mouse,NamedButton("Continue").transform);await Until(()=>One<HubView>().Page=="Lobby");await Shot("Returned-MainLobby");
   int earned=service.Snapshot.coins;await Click(mouse,One<HubView>().home.play.transform);await Click(mouse,NamedButton("Mode_training").transform);await Click(mouse,NamedButton("LaunchMode").transform);await Until(()=>One<PrototypeMatch>()!=null);await Frames(8);
   var training=One<PrototypeMatch>();foreach(var ai in Object.FindObjectsByType<PrototypeSwimmerAI>())ai.enabled=false;
   for(int q=1;q<=4;q++){One<BallController>().ResetForRestart(Vector3.zero);training.ShotClock.ResetPossession();training.AdvanceLiveTime(training.Clock.Remaining);if(q<4)One<MatchMenuController>().Continue();}
   Check(service.Snapshot.coins==earned&&service.Snapshot.played==2&&service.Snapshot.pendingResult.coins==0,"Training completion grants no economy/progression");One<MatchMenuController>().Continue();await Until(()=>One<HubView>()!=null&&One<HubView>().Page=="Result");await Click(mouse,NamedButton("Continue").transform);await Until(()=>One<HubView>().Page=="Lobby");
   string baseline=(string)JObject.Parse(File.ReadAllText(Out+"HubUserSaveBaseline.json"))["hash"];Check((File.Exists(HubApplication.SavePath)?Hash(HubApplication.SavePath):null)==baseline,"User profile bytes unchanged");
   return Save("HubLoop-PASS",new{bootMainLobby=true,actualPointerDivision1Launch=true,currentArenaAnd14Athletes=true,restartCancelAndConfirmSessions=true,abandonmentNoRewards=true,officialWinAndDefeat=true,trainingNoRewards=true,fullTimeBridgeSaved=true,receiptReplayDeduplicated=true,resultsAndReturnLobby=true,profileReload=true,userDiskProfileHashUnchanged=true,acceleratedAuthoritativeClockTransitions=true,fullLengthMatchPlayed=false});
  }
  finally{InputSystem.RemoveDevice(mouse);if(One<PrototypeMatch>()!=null){One<PrototypeMatch>().SetPaused(false);app.ReturnToLobby(true);await Until(()=>One<HubView>()!=null);}app.UseVerificationProfile(original);app.OpenLobby();}
 }
 public static async Task<object> ChargeAndSelectionFeedback()
 {
  VerificationInput();using(var f=new Fixture())
  {
   var touch=InputSystem.AddDevice<Touchscreen>();var keyboard=InputSystem.AddDevice<Keyboard>();
   try
   {
    f.blue[0].Motor.ResetPose(Vector3.zero,Quaternion.LookRotation(Vector3.right));f.Claim(f.blue[0]);await Frames(4);
    var shoot=Object.FindObjectsByType<GameplayActionButton>().Single(b=>b.Action==MobileMatchAction.Shoot);var rect=(RectTransform)shoot.transform;Vector2 point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));int sequence=f.ball.ShotSequence;
    InputSystem.QueueStateEvent(touch,new TouchState{touchId=21,phase=UnityEngine.InputSystem.TouchPhase.Began,position=point});await Seconds(.3f);
    Check(f.blue[0].GetComponent<ShotCharge>().IsCharging&&Field<GameObject>(One<ShotPowerBar>(),"panel").activeInHierarchy,"Actual touch hold and visible power bar");await Shot("Charged-Touch-Shot");
    InputSystem.QueueStateEvent(touch,new TouchState{touchId=21,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=point});await Frames(4);Check(f.ball.ShotSequence==sequence+1&&f.ball.Owner==null&&f.ball.ReleaseKind==BallReleaseKind.Shot,"Touch release fires exactly once");
    InputSystem.QueueStateEvent(touch,new TouchState{touchId=21,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=point});await Frames(3);Check(f.ball.ShotSequence==sequence+1,"Duplicate release cannot fire again");
    f.Reset();f.Claim(f.blue[0]);await Frames(4);sequence=f.ball.ShotSequence;InputSystem.QueueStateEvent(touch,new TouchState{touchId=22,phase=UnityEngine.InputSystem.TouchPhase.Began,position=point});await Seconds(.15f);f.menus.OpenPause();InputSystem.QueueStateEvent(touch,new TouchState{touchId=22,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=point});await Seconds(.2f);f.menus.Resume();await Seconds(.2f);Check(f.ball.Owner==f.blue[0].Interaction&&f.ball.ShotSequence==sequence&&!f.blue[0].GetComponent<ShotCharge>().IsCharging,"Actual touch released during pause cannot shoot on resume");
    var previous=f.selection.ActivePlayer;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.C));await Seconds(.12f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());Check(f.selection.ActivePlayer!=previous,"Existing C switching");
    var identity=f.selection.ActivePlayer;var ring=new SerializedObject(identity).FindProperty("selectionIndicator").objectReferenceValue as GameObject;var baseScale=Field<Vector3[]>(One<MatchControlPresentation>(),"scales")[Array.IndexOf(Field<SwimmerIdentity[]>(One<MatchControlPresentation>(),"athletes"),identity)];Check(ring.activeSelf&&ring.transform.localScale.x>baseScale.x*1.05f,"Existing green ring pulses on actual control switch");await Seconds(.4f);Check(Vector3.Distance(ring.transform.localScale,baseScale)<.0001f,"Ring returns to authored scale");
    var keeper=f.roster.Members.Single(p=>p.Team==PrototypeTeam.Blue&&!p.IsFieldPlayer);f.Claim(keeper);await Frames(4);Check(f.selection.ActivePlayer==keeper&&Field<TMP_Text>(One<ActiveStaminaHUD>(),"label").text.Contains("GOALKEEPER"),"Keeper identity feedback");await Shot("Selected-Goalkeeper");
    return Save("Feedback-PASS",new{actualSyntheticTouchChargeAndRelease=true,visibleShotPower=true,oneShotPerRelease=true,pauseCancelsActualTouch=true,keyboardCSwitch=true,existingRingPulseAndRestore=true,keeperIdentity=true});
   }
   finally{InputSystem.RemoveDevice(touch);InputSystem.RemoveDevice(keyboard);}
  }
 }
 public static object Restore()
 {
  Check(!Application.isPlaying,"Stop verification Play Mode first");Resize(0,true);int background=SessionState.GetInt("MatchUX.Background",-1),input=SessionState.GetInt("MatchUX.EditorInput",-1);
  if(background>=0)InputSystem.settings.backgroundBehavior=(InputSettings.BackgroundBehavior)background;if(input>=0)InputSystem.settings.editorInputBehaviorInPlayMode=(InputSettings.EditorInputBehaviorInPlayMode)input;SessionState.EraseInt("MatchUX.Background");SessionState.EraseInt("MatchUX.EditorInput");
  EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Boot.unity");
  return Save("Restored",new{normalEntry="Boot",originalGameViewRestored=true,inputFocusOverridesRestored=true,playStopped=true});
 }
 public static object ApplyMissingClose(){MatchUxSetup.SetUp();return new{landingClose=Path(Menu+"LandingPanel/Close")!=null};}
 public static async Task<object> TransitionBack()
 {
  VerificationInput();using(var f=new Fixture())
  {
   var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();
   try
   {
    for(int pass=0;pass<2;pass++)
    {
     f.Reset();for(int q=1;q<=4;q++)
     {
      f.ball.ResetForRestart(Vector3.zero);f.match.ShotClock.ResetPossession();f.match.AdvanceLiveTime(f.match.Clock.Remaining);await Frames(3);
      if(q==4&&pass==0)await Click(mouse,Path(Menu+"TransitionPanel/Close"));else await Escape(keyboard);
      if(q<4)Check(f.match.IsPlaying&&f.match.Clock.Quarter==q+1,"Escape continues correct quarter");else Check(f.match.Phase==MatchPhase.PreMatch&&f.menus.Page==MatchMenuPage.Landing,"Full-time X/Escape returns to standalone menu");
      if(q==2)Check(f.match.EndSwitchCount==1&&f.match.EndsSwitched,"Back at halftime switches ends once");
     }
     if(pass==0)await Click(mouse,Path(Menu+"LandingPanel/Close"));else await Escape(keyboard);
     Check(f.match.IsPlaying&&f.menus.Page==MatchMenuPage.None,"Standalone menu X/Escape starts fresh play");
    }
    return Save("TransitionBack-PASS",new{actualEscapeAndX=true,periodAndHalftimeCorrect=true,fullTimeStandaloneClose=true,landingClose=true});
   }
   finally{InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);}
  }
 }
}
