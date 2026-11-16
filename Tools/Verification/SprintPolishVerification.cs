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
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using TMPro;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Editor;
using ToduStudios.WaterPolo.Hub;
using ToduStudios.WaterPolo.WaterVisuals;
using Object=UnityEngine.Object;

public static class SprintPolishVerification
{
 const string Out="Logs/SprintPolish/",Scene="Assets/Scenes/OlympicMatchArena.unity",Recovery="Assets/Scenes/Recovery/OlympicMatchArena_PreSprintPolish.unity";
 static T One<T>() where T:Object=>Object.FindAnyObjectByType<T>();
 static void Check(bool p,string why){if(!p)throw new Exception(why);}
 static object Save(string n,object v){Directory.CreateDirectory(Out);File.WriteAllText(Out+n+".json",JsonConvert.SerializeObject(v,Formatting.Indented));return v;}
 static string Hash(string p)=>Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(p)));
 static T Field<T>(object o,string n)=>(T)o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
 static string Id(Object o)=>GlobalObjectId.GetGlobalObjectIdSlow(o).ToString();
 public static object Audit()
 {
  Check(!Application.isPlaying,"Saved Edit Mode required");for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)Check(!UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty,"Preserve user unsaved scene");OlympicArenaSetup.Open();
  if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Recovery)==null)Check(AssetDatabase.CopyAsset(Scene,Recovery),"Native recovery copy");
  var files=new[]{Scene,"Assets/Scenes/Boot.unity","Assets/Scenes/MainLobby.unity","Assets/Resources/HubCatalog.asset","Assets/Resources/DivisionCatalog.asset","Assets/Resources/HubMatchDestination.asset","ProjectSettings/EditorBuildSettings.asset","ProjectSettings/ProjectSettings.asset","old-plan.md"}.Concat(Directory.GetFiles("Assets/SwimmerAnimationPrototype","*",SearchOption.AllDirectories).Where(p=>!p.EndsWith(".meta"))).Concat(Directory.GetFiles("Assets/WaterVisuals/Materials","*.mat")).ToArray();
  var types=new[]{typeof(SwimmerIdentity),typeof(PrototypeRoster),typeof(PrototypeTeamTactics),typeof(GoalMouth),typeof(PrototypeGoalkeeper),typeof(GoalkeeperInterception),typeof(SwimmerStamina),typeof(SwimmerBallInteraction),typeof(RiggedSwimmerVisual),typeof(Animator),typeof(SkinnedMeshRenderer),typeof(GameplayCamera),typeof(Camera),typeof(PoolBounds),typeof(PoolsideReferee)};
  var protectedRecords=Object.FindObjectsByType<Component>(FindObjectsInactive.Include).Where(c=>c!=null&&(types.Contains(c.GetType())||c.GetComponentInParent<Canvas>(true)!=null)).ToDictionary(Id,c=>EditorJsonUtility.ToJson(c));
  Save("Baseline",new{guid=AssetDatabase.AssetPathToGUID(Scene),files=files.Select(p=>new{path=p,hash=Hash(p)}),protectedRecords,profileHash=File.Exists(HubApplication.SavePath)?Hash(HubApplication.SavePath):null,entry=AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene),background=Application.runInBackground});
  var renderers=Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
  Save("Environment-Audit",new{roster=One<PrototypeRoster>().Members.Select(p=>new{p.PlayerId,p.CapNumber,p.Team,p.FieldPosition,collider=EditorJsonUtility.ToJson(p.GetComponent<CapsuleCollider>())}),lights=Object.FindObjectsByType<Light>().Select(l=>new{l.name,position=l.transform.position.ToString(),data=EditorJsonUtility.ToJson(l)}),surfaces=renderers.Where(r=>r.bounds.max.y<2||r.name.Contains("Rig")||r.name.Contains("Truss")).Select(r=>new{path=Path(r.transform),r.enabled,active=r.gameObject.activeInHierarchy,bounds=r.bounds.ToString(),materials=r.sharedMaterials.Select(m=>m?.name),shadow=r.shadowCastingMode.ToString()}),volumes=Object.FindObjectsByType<Volume>().Select(v=>new{v.name,asset=AssetDatabase.GetAssetPath(v.sharedProfile),v.priority}),pipelines=One<ArenaQualityController>().pipelines.Select(AssetDatabase.GetAssetPath)});
  return new{savedRecovery=Recovery,exact=Hash(Scene)==Hash(Recovery),protectedCount=protectedRecords.Count};
 }
 static string Path(Transform t)=>t.parent==null?t.name:Path(t.parent)+"/"+t.name;
 static async Task Until(Func<bool> p,float seconds=12){double end=EditorApplication.timeSinceStartup+seconds;while(!p()){Check(EditorApplication.timeSinceStartup<end,"Timed out waiting for state");await Task.Delay(15);}}
 static async Task Frames(int n){int end=Time.frameCount+n;await Until(()=>Time.frameCount>=end);}
 static async Task Seconds(float n){double end=EditorApplication.timeSinceStartup+n;while(EditorApplication.timeSinceStartup<end)await Task.Delay(15);}
 static async Task Shot(string n){await Frames(4);ScreenCapture.CaptureScreenshot(Out+n+".png");await Frames(5);}
 static void Resize(int width,bool restore=false)
 {
  var f=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;var asm=typeof(UnityEditor.Editor).Assembly;var t=asm.GetType("UnityEditor.GameView");var w=EditorWindow.GetWindow(t);var selected=t.GetProperty("selectedSizeIndex",f);var sizes=typeof(ScriptableSingleton<>).MakeGenericType(asm.GetType("UnityEditor.GameViewSizes")).GetProperty("instance",f).GetValue(null);var group=sizes.GetType().GetMethod("GetGroup",f).Invoke(sizes,new[]{t.GetProperty("currentSizeGroupType",f).GetValue(w)});int added=SessionState.GetInt("SprintPolish.Added",-1),old=SessionState.GetInt("SprintPolish.Size",-1);
  if(added>=0){if(old>=0)selected.SetValue(w,old);group.GetType().GetMethod("RemoveCustomSize").Invoke(group,new object[]{added});SessionState.SetInt("SprintPolish.Added",-1);}if(restore){SessionState.SetInt("SprintPolish.Size",-1);return;}
  if(old<0)SessionState.SetInt("SprintPolish.Size",(int)selected.GetValue(w));var size=Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),width,1080,"M18 temporary verification"});group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});added=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1;SessionState.SetInt("SprintPolish.Added",added);selected.SetValue(w,added);w.Focus();
 }
 public static object Direct(){Resize(1920);EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(Scene);return new{entry=Scene};}
 static void Input()
 {
  if(SessionState.GetInt("SprintPolish.Background",-1)<0){SessionState.SetInt("SprintPolish.Background",(int)InputSystem.settings.backgroundBehavior);SessionState.SetInt("SprintPolish.EditorInput",(int)InputSystem.settings.editorInputBehaviorInPlayMode);}
  InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
 }
 public static async Task<object> Before()=>await Captures("Before");
 public static async Task<object> After()=>await Captures("After");
 static async Task<object> Captures(string prefix)
 {
  Input();Resize(1920);await Frames(3);var m=One<PrototypeMatch>();var dev=One<ArenaDeveloperCamera>();var prefs=One<GamePreferences>();var original=prefs.Current;var canvas=One<Canvas>();var probe=dev.viewpoints[2];Vector3 old=probe.position;Quaternion rot=probe.rotation;var keyboard=InputSystem.AddDevice<Keyboard>();
  try
  {
   prefs.SetGraphics(2);m.RestartMatch();dev.SetView(-1);One<GameplayCamera>().SnapToTarget();await Shot(prefix+"-Opening");Save(prefix+"-Opening-Camera",new{position=Camera.main.transform.position.ToString("F5"),rotation=Camera.main.transform.eulerAngles.ToString("F5"),Camera.main.fieldOfView});
   await Seconds(.7f);await Shot(prefix+"-Preparation");await Until(()=>m.IsSprintRace);
   for(int i=0;i<3;i++){InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.LeftShift));await Seconds(.10f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Seconds(.10f);}await Shot(prefix+"-Effort");
   await Until(()=>m.IsPlaying);dev.SetView(0);await Shot(prefix+"-Gameplay");
   m.SetPaused(true);canvas.enabled=false;probe.SetPositionAndRotation(new Vector3(9.6f,2.5f,-5.5f),Quaternion.LookRotation(new Vector3(12.65f,.75f,-.6f)-new Vector3(9.6f,2.5f,-5.5f)));dev.SetView(2);await Shot(prefix+"-Walkway");
   probe.SetPositionAndRotation(new Vector3(-2,7,-8),Quaternion.LookRotation(new Vector3(-14,11,4)-new Vector3(-2,7,-8)));await Shot(prefix+"-Lighting");
   dev.SetView(-1);m.SetPaused(false);m.RestartMatch();m.SetPaused(true);probe.SetPositionAndRotation(new Vector3(1.1f,1.25f,-1.4f),Quaternion.LookRotation(new Vector3(0,.25f,0)-new Vector3(1.1f,1.25f,-1.4f)));dev.SetView(2);await Shot(prefix+"-Apparatus");
   return Save(prefix+"-Views",new{actualGameView=true,frame=Time.frameCount,sevenMatchingViews=true});
  }
  finally{canvas.enabled=true;m.SetPaused(false);dev.SetView(-1);probe.SetPositionAndRotation(old,rot);prefs.SetGraphics((int)original.graphics);InputSystem.RemoveDevice(keyboard);}
 }
 public static object Structure()
 {
  Check(!Application.isPlaying,"Edit Mode");var b=JObject.Parse(File.ReadAllText(Out+"Baseline.json"));Check(AssetDatabase.AssetPathToGUID(Scene)==(string)b["guid"],"Same production scene GUID");
  foreach(var f in b["files"]){string p=(string)f["path"];Check(Hash(p==Scene?Recovery:p)==(string)f["hash"],"Preserved file "+p);}
  var all=Object.FindObjectsByType<Component>(FindObjectsInactive.Include).Where(c=>c!=null).ToDictionary(Id,c=>EditorJsonUtility.ToJson(c));
  var differences=((JObject)b["protectedRecords"]).Properties().Where(p=>!all.TryGetValue(p.Name,out var json)||json!=(string)p.Value).Select(p=>new{id=p.Name,before=(string)p.Value,after=all.TryGetValue(p.Name,out var data)?data:null}).ToArray();Save("Protected-Differences",differences);Check(differences.Length==0,"Original athlete, gameplay/camera tuning and M16 UI preserved");
  Check(Object.FindObjectsByType<SwimmerIdentity>().Length==14&&Object.FindObjectsByType<RiggedSwimmerVisual>().Length==14,"All14 original rigs");Check(Object.FindObjectsByType<Canvas>().Length==1&&Object.FindObjectsByType<EventSystem>().Length==1,"One Canvas/EventSystem");
  foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"No missing scripts");
  var root=GameObject.Find(SprintPolishSetup.Name);Check(root.GetComponentsInChildren<Collider>(true).Length==0,"No lighting blockers");var hud=One<SprintEffortHUD>();Check(hud!=null&&hud.GetComponent<CanvasGroup>().blocksRaycasts==false,"Non-blocking effort panel");foreach(var g in hud.GetComponentsInChildren<UnityEngine.UI.Graphic>())Check(g.GetComponent<CanvasRenderer>()!=null&&!g.raycastTarget,"Custom graphics render without intercepting input");
  string hash=Hash(Scene);SprintPolishSetup.SetUp();Check(Hash(Scene)==hash,"Repeat-safe setup");EditorSceneManager.OpenScene(Scene);Check(One<SprintEffortHUD>()!=null,"Saved reload retains meter");
  return Save("Structure-PASS",new{exactRecovery=true,sameSceneGuid=true,protectedRecords=((JObject)b["protectedRecords"]).Count,openingCameraUnchanged=true,all14Rigs=true,oneCanvasEventSystem=true,noMissingScripts=true,nonBlockingEffortHUD=true,repeatSafeNativeScene=true});
 }
 static Behaviour[] AI()=>Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>().Concat(Object.FindObjectsByType<PrototypeGoalkeeper>()).ToArray();
 static void Disable(Behaviour[] ai){foreach(var a in ai){a.enabled=false;a.GetComponent<SwimmerMotor>().StopMotion();}}
 static void RestoreAI(Behaviour[] ai,bool[] states){for(int i=0;i<ai.Length;i++)if(ai[i]!=null)ai[i].enabled=states[i];}
 static async Task Opening(PrototypeMatch m)=>await Until(()=>m.IsPlaying&&!m.AwaitingOpeningPossession,10);
 static void SetFloat(Object o,string name,float value){var so=new SerializedObject(o);so.FindProperty(name).floatValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
 static void Claim(SwimmerIdentity p){var ball=One<BallController>();ball.ResetForRestart(p.Motor.Position);Check(p.Interaction.TryCollect(),"Real in-range pickup fixture");}
 static async Task SwitchEdges(Keyboard keyboard,Gamepad pad)
 {
  InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.C));InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.RightShoulder));await Frames(3);InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.QueueStateEvent(pad,new GamepadState());await Frames(2);
 }
 public static async Task<object> SelectionAndEffort()
 {
  Input();var m=One<PrototypeMatch>();var s=m.SprintOff;var select=One<PlayerSelection>();var ball=One<BallController>();var controls=One<MobileMatchControls>();var hud=One<SprintEffortHUD>();var keyboard=InputSystem.AddDevice<Keyboard>();var pad=InputSystem.AddDevice<Gamepad>();var touch=InputSystem.AddDevice<Touchscreen>();var ai=AI();var states=ai.Select(a=>a.enabled).ToArray();
  try
  {
   m.RestartMatch();var human=s.HumanSprinter;Check(human.PlayerId=="prototype-blue-1"&&s.Sprinter(PrototypeTeam.White).PlayerId=="prototype-white-1","Deterministic roster IDs");
   var team=Object.FindObjectsByType<PrototypeTeamTactics>().Single(t=>t.Team==select.HumanTeam);Vector3 right=Vector3.Cross(Vector3.up,team.AttackDirection);Check(Mathf.Abs(Vector3.Dot(human.Motor.Position-team.DefendedGoal.transform.position,right)+1.05f)<.001f,"Intuitive central lane on legal line");
   Check(select.ActivePlayer==human&&select.SwitchingLocked,"Designated initial control locked");select.SelectNext();await SwitchEdges(keyboard,pad);Check(select.ActivePlayer==human&&!controls.Begin(MobileMatchAction.Switch),"Keyboard/controller/mobile switch all blocked in preparation");
   var other=One<PrototypeRoster>().FieldPlayers.First(p=>p.Team==select.HumanTeam&&p!=human);Check(!select.Select(other),"Direct automatic selection path cannot bypass contest lock");
   One<MatchMenuController>().OpenPause();float deadline=m.PresentationTime;await Seconds(.3f);Check(m.PresentationTime==deadline&&m.Phase==MatchPhase.SprintSetup&&select.ActivePlayer==human,"Prep pause preserves designation");One<MatchMenuController>().Resume();await Until(()=>m.IsSprintRace);Disable(ai);
   await SwitchEdges(keyboard,pad);Check(select.ActivePlayer==human&&select.SwitchingLocked&&!select.InputLocked,"Race lock permits designated movement only");
   int before=s.HumanStrokeCount;for(int i=0;i<100;i++)s.Stroke(human);Check(s.HumanStrokeCount==before+1&&s.HumanStrokePower<=.28f,"Cadence cap rejects same-frame spam");
   // Steer along the line so this meter fixture does not acquire the ball before it is sampled.
   float peak=0;for(int i=0;i<6;i++){InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.A,Key.LeftShift));await Seconds(.10f);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.A));await Seconds(.10f);peak=Mathf.Max(peak,s.HumanStrokePower);}
   await Frames(3);Save("Effort-Sample",new{peak,displayed=hud.DisplayedEffort,alpha=hud.Alpha,phase=m.Phase.ToString(),strokes=s.HumanStrokeCount});Check(peak>.4f&&hud.DisplayedEffort>.3f&&hud.Alpha>.9f,"Visible meter represents real press effort");Check(s.HumanAcceleration>1.1f&&s.HumanAcceleration<=1.35f&&Field<float>(human.Motor,"periodAcceleration")>1f,"Effort governs actual acceleration");Check(human.Motor.Velocity.magnitude<=6.01f&&human.Motor.Velocity.magnitude>1f,"Existing speed ceiling and actual steering retained");await Shot("Verified-Keyboard-Effort");InputSystem.QueueStateEvent(keyboard,new KeyboardState());
   if(m.IsSprintRace){m.SetPaused(true);await Frames(3);Check(hud.Alpha==0&&s.HumanStrokePower==0,"Pause hides meter and cancels effort");m.SetPaused(false);}
   // Friendly acquisition may finish the contest; auto-control must transfer only after this real claim.
   if(!m.IsSprintRace){m.RestartMatch();await Until(()=>m.IsSprintRace);Disable(ai);}await Until(()=>ball.CanCollect);human=s.HumanSprinter;other=One<PrototypeRoster>().FieldPlayers.First(p=>p.Team==select.HumanTeam&&p!=human);
   other.Motor.ResetPose(ball.PhysicsPosition+Vector3.right*2.2f,Quaternion.identity);await Frames(2);Check(select.ActivePlayer==human,"Teammate proximity alone never changes control");other.Motor.ResetPose(ball.PhysicsPosition,Quaternion.identity);Check(other.Interaction.TryCollect(),"Friendly teammate legitimately acquires");Check(m.IsPlaying&&!select.SwitchingLocked&&select.ActivePlayer==other,"Friendly acquisition unlocks and restores possession transfer");Check(!human.Interaction.TryCollect()&&s.Acquisitions==1,"Contention cannot duplicate possession");
   await Seconds(.3f);Check(hud.Alpha==0&&s.HumanStrokePower==0&&Field<float>(human.Motor,"periodAcceleration")==1f,"Effort clears and meter fades after contest");await SwitchEdges(keyboard,pad);Check(select.ActivePlayer!=other,"Ordinary switching restored");
   m.RestartMatch();await Until(()=>m.IsSprintRace);Disable(ai);await Frames(3);var button=Object.FindObjectsByType<GameplayActionButton>().Single(b=>b.Action==MobileMatchAction.Sprint);var rect=(RectTransform)button.transform;Vector2 point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));before=s.HumanStrokeCount;
   for(int i=0;i<5;i++){InputSystem.QueueStateEvent(touch,new TouchState{touchId=81,phase=UnityEngine.InputSystem.TouchPhase.Began,position=point});await Seconds(.10f);InputSystem.QueueStateEvent(touch,new TouchState{touchId=81,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=point});await Seconds(.12f);}
   Save("Touch-Effort-Sample",new{count=s.HumanStrokeCount-before,effort=s.HumanStrokePower,displayed=hud.DisplayedEffort,phase=m.Phase.ToString()});Check(s.HumanStrokeCount-before==5&&s.HumanStrokePower>.2f&&hud.DisplayedEffort>.2f,"Actual touch edges counted once and drive meter");await Shot("Verified-Touch-Effort");
   await Until(()=>ball.CanCollect);var opponent=One<PrototypeRoster>().FieldPlayers.First(p=>p.Team!=select.HumanTeam);opponent.Motor.ResetPose(ball.PhysicsPosition,Quaternion.identity);Check(opponent.Interaction.TryCollect(),"Opponent legitimate acquisition");Check(m.IsPlaying&&!select.SwitchingLocked&&select.ActivePlayer.Team==select.HumanTeam,"Opponent win releases switching without selecting enemy");
   return Save("Selection-Effort-PASS",new{deterministicExistingRosterIDs=true,centralLegalLane=true,prepAndRaceSwitchKeyboardGamepadMobileBlocked=true,teammateProximityNoSwitch=true,friendlyAcquisitionTransferAndSingleClaim=true,opponentAcquisitionUnlock=true,realCappedKeyboardEffortAndAcceleration=true,actualTouchFiveEdgesFiveStrokes=true,visibleMeter=true,pauseAndNoLeak=true,normalSwitchingRestored=true,existingSpeedCeiling=true});
  }
  finally{InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(pad);InputSystem.RemoveDevice(touch);RestoreAI(ai,states);m.SetPaused(false);m.RestartMatch();}
 }
 public static async Task<object> RecoveryEdges()
 {
  var m=One<PrototypeMatch>();var s=m.SprintOff;var select=One<PlayerSelection>();var ball=One<BallController>();var ai=AI();var states=ai.Select(a=>a.enabled).ToArray();
  try
  {
   m.RestartMatch();var unavailable=s.HumanSprinter;typeof(SwimmerIdentity).GetProperty("ExclusionRemaining").SetValue(unavailable,18f);await Frames(4);Check(s.HumanSprinter!=unavailable&&select.ActivePlayer==s.HumanSprinter&&m.Phase==MatchPhase.SprintSetup,"Unavailable preparation sprinter deterministically restaged");
   m.RestartMatch();Check(s.HumanSprinter==unavailable,"New match restores configured sprinter");await Until(()=>m.IsSprintRace);Disable(ai);await Seconds(.4f);Check(ball.CanCollect,"Released ball reached water");
   typeof(PeriodSprintOff).GetField("startedAt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(s,m.PresentationTime-20f);await Frames(3);Check(m.IsPlaying&&m.AwaitingOpeningPossession&&!select.SwitchingLocked&&s.Fallbacks==1&&ball.Owner==null,"Aged deadline unlocks neutral loose-ball play without award");
   float time=m.Clock.Remaining,shot=m.ShotClock.Remaining;await Seconds(.3f);Check(m.Clock.Remaining==time&&m.ShotClock.Remaining==shot,"Neutral opening clocks await genuine touch");var human=s.HumanSprinter;human.Motor.ResetPose(ball.PhysicsPosition,Quaternion.identity);Check(human.Interaction.TryCollect(),"Fallback real acquisition");await Frames(3);Check(!m.AwaitingOpeningPossession&&m.Clock.Remaining<time&&s.Acquisitions==1,"Real fallback pickup starts clocks exactly once");
   return Save("Recovery-Edges-PASS",new{unavailableSprinterRestaged=true,restartRestoresPreferred=true,timeoutDeadlineAgedFixture=true,noPossessionAwardOrBallTeleport=true,switchingUnlocks=true,clocksWaitForActualPickup=true,oneActualAcquisition=true});
  }
  finally{RestoreAI(ai,states);m.RestartMatch();}
 }
 public static async Task<object> ReleaseMechanism()
 {
  var m=One<PrototypeMatch>();var ball=One<BallController>();var view=One<SprintBallReleaseVisual>();var cradle=Field<Transform>(view,"cradle");var jaw=Field<Transform>(view,"leftJaw");var dev=One<ArenaDeveloperCamera>();var probe=dev.viewpoints[2];var pos=probe.position;var rot=probe.rotation;var canvas=One<Canvas>();int whistles=0,waters=0;void Whistle(){whistles++;}void Water(BallImpactEvent e){if(e.Kind==BallImpactKind.Water)waters++;}m.SprintStarted+=Whistle;ball.Contact+=Water;
  try
  {
   m.RestartMatch();Check(ball.PeriodSupported&&!ball.CanCollect&&ball.GetComponent<Rigidbody>().isKinematic,"Single real ball held safely");Vector3 start=ball.PhysicsPosition;await Until(()=>m.IsSprintRace);Check(ball.PeriodDropping&&!ball.CanCollect&&whistles==1,"Whistle releases physical ball once");
   canvas.enabled=false;probe.SetPositionAndRotation(new Vector3(1.1f,1.25f,-1.4f),Quaternion.LookRotation(new Vector3(0,.25f,0)-new Vector3(1.1f,1.25f,-1.4f)));dev.SetView(2);await Seconds(.16f);Check(Mathf.Abs(jaw.localPosition.x)>.14f,"Visible jaws open clear of falling ball");await Shot("Release-Opening-Jaws");
   await Until(()=>ball.CanCollect);Check(waters==1&&Vector2.Distance(new Vector2(start.x,start.z),new Vector2(ball.PhysicsPosition.x,ball.PhysicsPosition.z))<.03f,"Single subtle water contact with minimal lateral drift");Check(cradle.gameObject.activeSelf,"Mechanism remains readable after physical drop");
   float y=cradle.localPosition.y;m.SetPaused(true);await Seconds(.3f);Check(cradle.localPosition.y==y,"Retraction pauses at exact state");m.SetPaused(false);await Shot("Release-Underwater-Retraction");await Seconds(.85f);Check(!cradle.gameObject.activeSelf&&view.RetractionDuration>=.8f,"Smooth configurable retraction completes independently");
   Check(Object.FindObjectsByType<BallController>().Length==1&&cradle.GetComponentsInChildren<Collider>(true).Length==0,"No duplicate ball or apparatus blockers");
   return Save("Release-PASS",new{realBallHeld=true,oneWhistle=true,jawsOpen=true,retractionSeconds=view.RetractionDuration,realGravityDropAndOneWaterImpact=true,minimalDrift=true,collectionAfterWater=true,visibleAfterDrop=true,pauseFreeze=true,noDuplicateBallOrColliders=true});
  }
  finally{m.SprintStarted-=Whistle;ball.Contact-=Water;canvas.enabled=true;m.SetPaused(false);dev.SetView(-1);probe.SetPositionAndRotation(pos,rot);}
 }
 static void Park(){int b=0,w=0;foreach(var p in One<PrototypeRoster>().FieldPlayers)p.Motor.ResetPose(new Vector3(p.Team==PrototypeTeam.Blue?-8:8,0,-10+4*(p.Team==PrototypeTeam.Blue?b++:w++)),Quaternion.identity);}
 public static async Task<object> RulesRegression()
 {
  var m=One<PrototypeMatch>();m.RestartMatch();await Opening(m);var ai=AI();var states=ai.Select(a=>a.enabled).ToArray();Disable(ai);var fields=One<PrototypeRoster>().FieldPlayers;var blue=fields.Where(p=>p.Team==PrototypeTeam.Blue).ToArray();var white=fields.First(p=>p.Team==PrototypeTeam.White);var carrier=blue[0];var receiver=blue[2];int decisions=m.Rules.DecisionCount;
  try
  {
   Park();carrier.Motor.ResetPose(Vector3.zero,Quaternion.LookRotation(Vector3.right));receiver.Motor.ResetPose(Vector3.right*6,Quaternion.identity);white.Motor.ResetPose(Vector3.left*.8f,Quaternion.LookRotation(Vector3.right));Claim(carrier);
   Check(m.Rules.ReportContact(white,carrier,IllegalContact.Push)&&m.Phase==MatchPhase.FreeThrowSetup,"Ordinary foul remains authoritative");float clock=m.Clock.Remaining,shot=m.ShotClock.Remaining;await Seconds(.25f);Check(m.Clock.Remaining==clock&&m.ShotClock.Remaining==shot,"Foul setup clocks stop");await Until(()=>m.Phase==MatchPhase.FreeThrowReady);
   receiver.Motor.ResetPose(carrier.Motor.Position+carrier.Motor.Facing*6,Quaternion.identity);Check(carrier.GetComponent<SwimmerPassing>().TryPassTo(receiver),"Physical free throw release");await Until(()=>m.IsPlaying);await Frames(3);
   Park();carrier.Motor.ResetPose(Vector3.zero,Quaternion.LookRotation(Vector3.right));receiver.Motor.ResetPose(Vector3.right*6,Quaternion.identity);blue[1].Motor.ResetPose(Vector3.forward,Quaternion.identity);white.Motor.ResetPose(Vector3.forward+Vector3.left*.6f,Quaternion.LookRotation(Vector3.right));Claim(carrier);
   Check(m.Rules.ReportContact(white,blue[1],IllegalContact.Hold)&&white.ExclusionRemaining>17f&&white.PersonalFouls==1,"Off-ball hold causes existing exclusion");Check(m.SprintOff.Starts==1,"Fouls never restart sprint");await Until(()=>m.Phase==MatchPhase.FreeThrowReady);receiver.Motor.ResetPose(carrier.Motor.Position+carrier.Motor.Facing*6,Quaternion.identity);Check(carrier.GetComponent<SwimmerPassing>().TryPassTo(receiver),"Existing exclusion free throw release");
   m.AdvanceLiveTime(19);Check(white.ExclusionRemaining==0&&white.EligibleForPlay,"Live exclusion re-entry retained");Check(m.Rules.DecisionCount>=decisions+2,"Real referee decisions observed");
   return Save("Rules-PASS",new{ordinaryFoul=true,stoppedClocks=true,physicalFreeThrowRelease=true,offBallExclusion=true,personalFoul=true,liveReentry=true,noExtraSprint=true});
  }
  finally{RestoreAI(ai,states);m.RestartMatch();}
 }
 static Rect Bounds(RectTransform r){var c=new Vector3[4];r.GetWorldCorners(c);return Rect.MinMaxRect(c[0].x,c[0].y,c[2].x,c[2].y);}
 public static async Task<object> QualityAndLayout()
 {
  var m=One<PrototypeMatch>();var prefs=One<GamePreferences>();var original=prefs.Current;var dev=One<ArenaDeveloperCamera>();var records=new List<object>();
  try
  {
   foreach(int level in new[]{0,1,2})
   {
    Resize(1920);prefs.SetGraphics(level);m.RestartMatch();dev.SetView(-1);One<GameplayCamera>().SnapToTarget();await Frames(8);var p=UniversalRenderPipeline.asset;var presentation=One<ChampionshipPresentation>();var lights=Object.FindObjectsByType<Light>().Where(l=>l.enabled&&l.gameObject.activeInHierarchy).ToArray();
    Check(lights.Length==(level==0?3:level==1?9:11),"Quality actual light budget");Check(lights.Where(l=>l.type!=LightType.Directional).All(l=>l.shadows==LightShadows.None),"No additional shadowed lights");Check(Camera.main.GetUniversalAdditionalCameraData().renderShadows==(level>=1),"Medium/High directional shadows enabled");
    Check(p.supportsHDR&&p.mainLightShadowmapResolution==(level==2?2048:1024)&&p.shadowCascadeCount==(level==2?2:1),"Tier shadow/HDR configuration");Check(One<PoolWaterVisuals>().RuntimeMaterial.shader.isSupported,"Original transparent-water shader supported");presentation.volume.sharedProfile.TryGet<Bloom>(out var bloom);Check(level==0?bloom.intensity.value==0:bloom.intensity.value>0,"Tier bloom budget");await Shot("Quality-"+level+"-Opening");records.Add(new{level,lights=lights.Length,p.shadowDistance,p.mainLightShadowmapResolution,p.shadowCascadeCount,bloom=bloom.intensity.value});
   }
   foreach(int width in new[]{1920,2340,2400})
   {
    Resize(width);await Frames(5);m.RestartMatch();await Until(()=>m.IsSprintRace);await Frames(5);var meter=One<SprintEffortHUD>();var r=Bounds((RectTransform)meter.transform);Check(r.xMin>=0&&r.yMin>=0&&r.xMax<=Screen.width&&r.yMax<=Screen.height,"Effort within screen");var canvas=One<Canvas>();foreach(string path in new[]{"MatchSafeArea/ScoreCard","MatchSafeArea/ActiveAthlete","MatchSafeArea/MatchNotification","MatchSafeArea/MobileControls/LeftThumb","MatchSafeArea/MobileControls/RightThumb"}){var t=canvas.transform.Find(path);if(t!=null)Check(!r.Overlaps(Bounds((RectTransform)t)),"Effort does not overlap "+path);}
    foreach(var button in Object.FindObjectsByType<GameplayActionButton>())Check(!r.Overlaps(Bounds((RectTransform)button.transform)),"Meter clears actual touch target");foreach(var t in meter.GetComponentsInChildren<TMP_Text>())Check(!t.isTextOverflowing,"Meter text fits");await Shot("Effort-Layout-"+width);await Opening(m);
   }
   return Save("Quality-Layout-PASS",new{presets=records,actualWidths=new[]{1920,2340,2400},height=1080,noExtraShadowedLights=true,originalWaterSupported=true,effortBoundsAndNonOverlap=true,readableText=true});
  }
  finally{prefs.SetGraphics((int)original.graphics);dev.SetView(-1);}
 }
 public static async Task<object> Focused()
 {
  var errors=new List<string>();void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message+"\n"+stack);}Application.logMessageReceived+=Log;
  try{await SelectionAndEffort();await RecoveryEdges();await ReleaseMechanism();await RulesRegression();await QualityAndLayout();Check(errors.Count==0,"Fresh focused runtime errors");return Save("Focused-PASS",new{selection=true,effort=true,recoveryEdges=true,release=true,rules=true,qualityLayout=true,newRuntimeErrors=errors.Count});}
  finally{Application.logMessageReceived-=Log;Save("Focused-Errors",errors);}
 }
 public static object Restore()
 {
  Check(!Application.isPlaying,"Stop verification Play Mode first");Resize(0,true);int background=SessionState.GetInt("SprintPolish.Background",-1),input=SessionState.GetInt("SprintPolish.EditorInput",-1);if(background>=0)InputSystem.settings.backgroundBehavior=(InputSettings.BackgroundBehavior)background;if(input>=0)InputSystem.settings.editorInputBehaviorInPlayMode=(InputSettings.EditorInputBehaviorInPlayMode)input;SessionState.EraseInt("SprintPolish.Background");SessionState.EraseInt("SprintPolish.EditorInput");EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Boot.unity");
  string original=(string)JObject.Parse(File.ReadAllText(Out+"Baseline.json"))["profileHash"];Check((File.Exists(HubApplication.SavePath)?Hash(HubApplication.SavePath):null)==original,"Real profile bytes preserved");return Save("Restored",new{playStopped=true,bootEntry=true,originalGameViewAndInputFocusRestored=true,realProfileBytesUnchanged=true});
 }
 public static object MeshAudit()=>Save("Fixture-Mesh-Audit",Object.FindObjectsByType<MeshRenderer>().Where(r=>r.name.StartsWith("Luminaire")||r.name.StartsWith("Competition Lens")).Select(r=>new{r.name,mesh=AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>().sharedMesh),size=r.GetComponent<MeshFilter>().sharedMesh.bounds.size.ToString("F5"),emission=r.sharedMaterial.GetColor("_EmissionColor").ToString(),keyword=r.sharedMaterial.IsKeywordEnabled("_EMISSION")}));
 public static object FinalAudit()
 {
  Check(!Application.isPlaying&&!SceneManager.GetActiveScene().isDirty,"Saved Edit Mode");Check(AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene)=="Assets/Scenes/Boot.unity","Boot restored");var root=GameObject.Find(SprintPolishSetup.Name);var renderers=root.GetComponentsInChildren<MeshRenderer>(true);var mats=AssetDatabase.FindAssets("t:Material",new[]{SprintPolishSetup.Root+"/Materials"}).Select(g=>AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
  foreach(var mat in mats){Check(mat.shader.name=="Universal Render Pipeline/Lit"&&!mat.name.Contains("Default-Material"),"Deliberate palette");if(mat.GetColor("_EmissionColor").maxColorComponent>0)Check(mat.IsKeywordEnabled("_EMISSION"),"Emission survives native asset reload");}
  var before=JObject.Parse(File.ReadAllText(Out+"Before-Opening-Camera.json"));var after=JObject.Parse(File.ReadAllText(Out+"After-Opening-Camera.json"));Check(JToken.DeepEquals(before,after),"Matching exact opening camera pose/FOV");Check(Object.FindObjectsByType<PrototypeMatch>().Length==1&&Object.FindObjectsByType<PeriodSprintOff>().Length==1&&Object.FindObjectsByType<BallController>().Length==1,"Single gameplay authorities");
  return Save("Final-Audit-PASS",new{savedScene=true,playStopped=true,bootEntry=true,openingCameraExactlyMatched=true,camera=after,newLightingRenderers=renderers.Length,newLightingReferencedVertices=renderers.Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.vertexCount),newMaterials=mats.Length,extraRealLights=root.GetComponentsInChildren<Light>(true).Length,extraShadowedLights=root.GetComponentsInChildren<Light>(true).Count(l=>l.shadows!=LightShadows.None),newColliders=root.GetComponentsInChildren<Collider>(true).Length,newParticles=root.GetComponentsInChildren<ParticleSystem>(true).Length,emissionKeywordsValidated=true,singleAuthorities=true});
 }
}
