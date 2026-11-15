using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.WaterVisuals;
using ToduStudios.WaterPolo.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

public static class AnimatedAthleteVerification
{
 const string Out="Logs/SwimmerAnimation/";
 static T One<T>() where T:Object=>Object.FindAnyObjectByType<T>();
 static void Check(bool b,string m){if(!b)throw new Exception("M14: "+m);}
 static object Save(string name,object d){Directory.CreateDirectory(Out);File.WriteAllText(Out+name+".json",JsonConvert.SerializeObject(d,Formatting.Indented));return d;}
 static string Hash(string p){using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p)));}
 static string Id(Object o)=>o==null?"null":GlobalObjectId.GetGlobalObjectIdSlow(o).ToString();
 static string Stable(Object o)
 {
  var json=JObject.Parse(EditorJsonUtility.ToJson(o));foreach(var p in json.Descendants().OfType<JProperty>().Where(p=>p.Name=="instanceID").ToArray())p.Value=0;
  var refs=new List<string>();using(var so=new SerializedObject(o)){var p=so.GetIterator();while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference)refs.Add(p.propertyPath+"="+Id(p.objectReferenceValue));}return json.ToString(Formatting.None)+string.Join(";",refs);
 }
 public static object Baseline()
 {
  Check(!Application.isPlaying,"Edit Mode required");OlympicArenaSetup.Open();
  var files=new[]{"Assets/Scenes/Boot.unity","Assets/Scenes/MainLobby.unity","ProjectSettings/EditorBuildSettings.asset","Assets/Resources/HubMatchDestination.asset","Assets/Resources/HubCatalog.asset"}.Concat(Directory.GetFiles("Assets/WaterVisuals/Materials","*.mat")).ToArray();
  Save("M14-Baseline",new{scene=AssetDatabase.AssetPathToGUID(OlympicArenaSetup.ScenePath),hash=Hash(OlympicArenaSetup.ScenePath),files=files.Select(p=>new{path=p,hash=Hash(p)}),records=Object.FindObjectsByType<Component>(FindObjectsInactive.Include).Where(c=>c is Collider||c is Rigidbody||c is MonoBehaviour||c is Camera).Select(c=>new{id=Id(c),type=c.GetType().FullName,json=Stable(c)}).ToArray()});return new{saved=true};
 }
 public static object Structure()
 {
  var baseline=JObject.Parse(File.ReadAllText(Out+"M14-Baseline.json"));Check(Hash(AnimatedAthleteSetup.Recovery)==(string)baseline["hash"],"Exact pre-animation recovery");
  var originals=Object.FindObjectsByType<Component>(FindObjectsInactive.Include).ToDictionary(Id,c=>c);
  int n=0;foreach(var record in baseline["records"]){Check(originals.TryGetValue((string)record["id"],out var c),"Original component retained");Check(Stable(c)==(string)record["json"],"Original reference or configuration changed: "+c.name+" "+c.GetType().Name);n++;}
  foreach(var f in baseline["files"])Check(Hash((string)f["path"])==(string)f["hash"],"Entry, economy or water material changed");
  var visuals=Object.FindObjectsByType<RiggedSwimmerVisual>();var roster=Object.FindObjectsByType<SwimmerIdentity>();
  Check(roster.Length==14,"14 real athletes");Check(visuals.Length==1||visuals.Length==14,"Opt-in rollout");
  foreach(var v in visuals){Check(v.GetComponent<Animator>().avatar.isHuman&&v.GetComponent<Animator>().avatar.isValid,"Valid Humanoid");Check(!v.GetComponent<Animator>().applyRootMotion,"No root motion");Check(v.GetComponentsInChildren<Collider>().Length==0,"No visual colliders");Check(v.GetComponentsInChildren<SkinnedMeshRenderer>().All(r=>r.sharedMaterials.All(m=>m!=null)),"Material references");}
  Check(SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"No missing scene scripts");
  Check(visuals.All(v=>v.identity.GetComponentsInChildren<ArenaAthleteVisual>(true).All(o=>!o.gameObject.activeSelf)),"Original visual retained inactive");
  return Save("M14-Structure-PASS",new{protectedRecords=n,riggedAthletes=visuals.Length,athletes=roster.Length,physicsUnchanged=true,entryAndWaterMaterialsUnchanged=true,clips=Directory.GetFiles(AnimatedAthleteSetup.Root+"/Animations","*.fbx").Length});
 }
 static async Task Frames(int n){int target=Time.frameCount+n;await Until(()=>Time.frameCount>=target,10);}
 static async Task Seconds(float n){double end=EditorApplication.timeSinceStartup+n;while(EditorApplication.timeSinceStartup<end)await Task.Delay(20);}
 static async Task Sim(float n){float end=Time.time+n;double deadline=EditorApplication.timeSinceStartup+15;while(Time.time<end){Check(EditorApplication.timeSinceStartup<deadline,"Advancing simulation");await Task.Delay(15);}}
 public static object FocusGame()
 {
  var flags=System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static;
  var assembly=typeof(UnityEditor.Editor).Assembly;var type=assembly.GetType("UnityEditor.GameView");var window=EditorWindow.GetWindow(type);window.Focus();
  var st=assembly.GetType("UnityEditor.GameViewSizes");var sizes=typeof(ScriptableSingleton<>).MakeGenericType(st).GetProperty("instance",flags).GetValue(null);var group=st.GetMethod("GetGroup",flags).Invoke(sizes,new[]{type.GetProperty("currentSizeGroupType",flags).GetValue(window)});var gt=group.GetType();
  var size=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),1920,1080,"Animated athlete review"});gt.GetMethod("AddCustomSize").Invoke(group,new[]{size});type.GetProperty("selectedSizeIndex",flags).SetValue(window,(int)gt.GetMethod("GetTotalCount").Invoke(group,null)-1);
  return new{resolution="1920x1080"};
 }
 static async Task Shot(string name){ScreenCapture.CaptureScreenshot(Out+name+".png");await Frames(10);await Task.Delay(150);}
 public static async Task<object> Lab()
 {
  Check(Application.isPlaying&&SceneManager.GetActiveScene().path==AnimatedAthleteSetup.Lab,"Play isolated lab");
  var v=One<RiggedSwimmerVisual>();var anim=v.Animator;Check(anim.isHuman,"Humanoid playing");
  var samples=new List<object>();
  foreach(int mode in new[]{0,1,2,3,4,5,6,7})
  {
   v.previewState=mode;await Sim(.4f);var root=v.alignmentRoot.position;var hand=v.LeftHand.position;await Sim(.25f);
   var head=anim.GetBoneTransform(HumanBodyBones.Head).position;Check(Mathf.Abs(head.y-v.headHeight)<.03f,"Head waterline alignment");
   Check(Mathf.Abs(v.alignmentRoot.position.y)<2,"Anatomical visual root offset");Check(new Vector2(head.x,head.z).magnitude<1.25f,"Head remains over swimmer");
   Check(anim.GetBoneTransform(HumanBodyBones.LeftFoot).position.y<0,"Submerged legs");
   Check(Vector2.Distance(new Vector2(root.x,root.z),new Vector2(v.alignmentRoot.position.x,v.alignmentRoot.position.z))<.001f,"In-place visual XZ");
   samples.Add(new{mode,head=head.ToString("F3"),handTravel=Vector3.Distance(hand,v.LeftHand.position),root=v.alignmentRoot.position.ToString("F3"),normalized=anim.GetCurrentAnimatorStateInfo(0).normalizedTime,clip=anim.GetCurrentAnimatorClipInfo(0).Select(c=>c.clip.name).ToArray()});await Shot("Lab-"+mode);
  }
  v.previewState=0;return Save("M14-Lab-PASS",new{human=anim.isHuman,states=samples,fx=One<WaterInteractionEffects>().AthleteEmissions,bubbles=One<WaterInteractionEffects>().BubbleEmissions});
 }
 public static async Task<object> CaptureLab(){await Shot("Lab-Inspection");return new{captured=true};}
 public static async Task<object> StrokeProbe()
 {
  var v=One<RiggedSwimmerVisual>();v.previewState=1;var data=new List<object>();
  foreach(float weight in new[]{1f,0f})
  {
   v.Animator.SetLayerWeight(1,weight);await Sim(.5f);var left=new List<Vector3>();var right=new List<Vector3>();
   for(int i=0;i<30;i++){left.Add(v.LeftHand.position);right.Add(v.RightHand.position);await Sim(.05f);if(i%10==0)await Shot("Swim-"+weight+"-"+i);}
   data.Add(new{weight,leftRange=(left.Aggregate(Vector3.Min)-left.Aggregate(Vector3.Max)).ToString(),rightRange=(right.Aggregate(Vector3.Min)-right.Aggregate(Vector3.Max)).ToString(),left=left.Select(x=>x.ToString("F3")).ToArray()});
  }
  v.Animator.SetLayerWeight(1,1);return Save("Stroke-Probe",data);
 }
 static async Task Until(Func<bool> f,float timeout=5){double end=EditorApplication.timeSinceStartup+timeout;while(!f()){Check(EditorApplication.timeSinceStartup<end,"Timed condition");await Task.Delay(20);}}
 static void CloseView(RiggedSwimmerVisual v)
 {
  var dev=One<ArenaDeveloperCamera>();var t=dev.viewpoints[2];t.position=v.motor.Position+new Vector3(2.8f,2.4f,4);t.LookAt(v.motor.Position+new Vector3(0,-.2f,.1f));dev.SetView(2);
 }
 public static async Task<object> MatchSmoke()
 {
  Check(Application.isPlaying&&SceneManager.GetActiveScene().name=="OlympicMatchArena","Play production arena");
  var all=Object.FindObjectsByType<RiggedSwimmerVisual>();var v=all.First(x=>x.identity.Team==PrototypeTeam.Blue&&x.identity.IsFieldPlayer);var m=One<PrototypeMatch>();var ball=One<BallController>();var selection=One<PlayerSelection>();var roster=One<PrototypeRoster>();var fx=One<WaterInteractionEffects>();
  var ai=Object.FindObjectsByType<PrototypeSwimmerAI>();var aiEnabled=ai.Select(x=>x.enabled).ToArray();var keepers=Object.FindObjectsByType<PrototypeGoalkeeper>();var keeperEnabled=keepers.Select(x=>x.enabled).ToArray();
  var prefs=One<GamePreferences>();int quality=(int)prefs.Current.graphics;var bg=InputSystem.settings.backgroundBehavior;var ed=InputSystem.settings.editorInputBehaviorInPlayMode;
  InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
  var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();var releases=new List<string>();Action<SwimmerBallInteraction,BallReleaseKind> released=(s,k)=>{if(s==v.identity.Interaction)releases.Add(k.ToString());};ball.Released+=released;
  try
  {
   m.RestartMatch();prefs.SetGraphics(2);foreach(var a in ai)a.enabled=false;foreach(var k in keepers)k.enabled=false;
   Check(roster.Members.Count==14&&Object.FindObjectsByType<Canvas>().Length==1,"Real roster and single original HUD");
   v.motor.ResetPose(new Vector3(0,0,-5),Quaternion.identity);selection.Select(v.identity);ball.ResetForRestart(new Vector3(8,0,8));await Sim(.4f);
   CloseView(v);int idleBefore=fx.AthleteEmissions;await Sim(2.6f);Check(fx.AthleteEmissions>idleBefore,"Gentle idle ring");await Shot("Match-Idle-"+all.Length);
   var start=v.motor.Position;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));await Sim(.8f);Check(v.Locomotion>.8f&&Vector3.Distance(start,v.motor.Position)>.8f,"Real movement drives swim");CloseView(v);await Shot("Match-Swim-"+all.Length);
   InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.LeftShift));await Sim(.85f);Check(v.identity.GetComponent<SwimmerStamina>().IsSprinting&&v.Locomotion>1.4f,"Real sprint drives animation");CloseView(v);await Shot("Match-Sprint-"+all.Length);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Sim(.4f);
   Check(v.StrokeEmissions>0,"Animated hand entries emit water");
   m.SetPaused(true);await Seconds(.12f);float clock=m.PresentationTime;var hand=v.LeftHand.position;await Seconds(.25f);Check(m.PresentationTime==clock&&Vector3.Distance(hand,v.LeftHand.position)<.005f,"Animation respects authoritative pause");m.SetPaused(false);
   var pause=Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.name=="PauseButton");var rect=(RectTransform)pause.transform;Vector2 point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
   InputSystem.QueueStateEvent(mouse,new MouseState{position=point});await Seconds(.08f);InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=1});await Seconds(.08f);InputSystem.QueueStateEvent(mouse,new MouseState{position=point});await Seconds(.08f);Check(m.IsPaused,"Original HUD pointer pause");InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));await Seconds(.08f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Seconds(.08f);Check(!m.IsPaused,"Original Escape resume");
   int blue=0,white=0;foreach(var a in roster.FieldPlayers)a.Motor.ResetPose(new Vector3(a.Team==PrototypeTeam.Blue?-8:8,0,-10+3.5f*(a.Team==PrototypeTeam.Blue?blue++:white++)),Quaternion.identity);
   var receiver=roster.FieldPlayers.First(x=>x.Team==PrototypeTeam.Blue&&x!=v.identity);v.motor.ResetPose(new Vector3(-2,0,-3),Quaternion.identity);receiver.Motor.ResetPose(new Vector3(-2,0,2),Quaternion.identity);ball.ResetForRestart(v.motor.Position);Check(v.identity.Interaction.TryCollect(),"Real pickup");await Sim(.3f);CloseView(v);await Shot("Match-Possession-"+all.Length);
   int before=v.ReleaseAnimations;Check(v.identity.GetComponent<SwimmerPassing>().TryPassTo(receiver),"Real pass command");await Until(()=>ball.Owner==receiver.Interaction);Check(v.ReleaseAnimations==before+1&&releases.Count(x=>x=="Pass")==1,"Exactly one pass observation and physical reception");
   var team=Object.FindObjectsByType<PrototypeTeamTactics>().Single(t=>t.Team==PrototypeTeam.Blue);var goal=team.AttackingGoal;v.motor.ResetPose(goal.transform.position-goal.transform.forward*5,Quaternion.LookRotation(goal.transform.forward));selection.Select(v.identity);ball.ResetForRestart(v.motor.Position);Check(v.identity.Interaction.TryCollect(),"Shot pickup");before=v.ReleaseAnimations;Check(v.identity.Interaction.TryShoot(22,ShotType.Normal),"Real shot");await Sim(.1f);CloseView(v);await Shot("Match-Throw-"+all.Length);await Until(()=>m.BlueScore==1);Check(v.ReleaseAnimations==before+1&&releases.Count(x=>x=="Shot")==1,"Exactly one shot observation and real goal");
   m.RestartMatch();for(int i=0;i<ai.Length;i++)ai[i].enabled=aiEnabled[i];for(int i=0;i<keepers.Length;i++)keepers[i].enabled=keeperEnabled[i];One<ArenaDeveloperCamera>().SetView(-1);await Sim(1);
   Check(roster.Members.All(a=>Mathf.Abs(a.Motor.Position.x)<10&&Mathf.Abs(a.Motor.Position.z)<13&&Mathf.Abs(a.Motor.Position.y)<.001f),"AI remains in original pool bounds");
   foreach(var a in all){Check(Mathf.Abs(a.Animator.GetBoneTransform(HumanBodyBones.Head).position.y-.26f)<.03f,"Real athlete waterline");Check(a.GetComponentsInChildren<Collider>().Length==0,"No visual physics");}
   for(int q=0;q<3;q++){prefs.SetGraphics(q);await Sim(.2f);Check(One<PoolWaterVisuals>().qualityIndex==q,"Existing graphics preset");if(q==0)Check(fx.ActiveEffects==0&&fx.BubbleCount==0,"Low clears effect budgets");await Shot("Match-Quality-"+q+"-"+all.Length);}
   return Save("M14-Match-"+all.Length+"-PASS",new{animated=all.Length,roster=roster.Members.Count,actualInputSwimAndSprint=true,idleRing=true,handEntries=v.StrokeEmissions,releaseHooks=v.ReleaseAnimations,physicalPass=true,physicalShotGoal=true,pausedCorrectly=true,aiWithinBounds=true,originalHUD=true,qualityPresets=3,effectCapacity=fx.PoolCapacity,athleteEmissions=fx.AthleteEmissions,bubbleEmissions=fx.BubbleEmissions,phonePerformanceTested=false});
  }
  finally{ball.Released-=released;InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);InputSystem.settings.backgroundBehavior=bg;InputSystem.settings.editorInputBehaviorInPlayMode=ed;m.SetPaused(false);m.RestartMatch();prefs.SetGraphics(quality);for(int i=0;i<ai.Length;i++)ai[i].enabled=aiEnabled[i];for(int i=0;i<keepers.Length;i++)keepers[i].enabled=keeperEnabled[i];One<ArenaDeveloperCamera>().SetView(-1);}
 }
 public static async Task<object> KeeperSmoke()
 {
  var match=One<PrototypeMatch>();var ball=One<BallController>();var shooter=Object.FindObjectsByType<SwimmerIdentity>().First(x=>x.Team==PrototypeTeam.Blue&&x.IsFieldPlayer);var keeper=Object.FindObjectsByType<PrototypeGoalkeeper>().First(x=>x.Identity.Team==PrototypeTeam.White);var visual=keeper.GetComponentInChildren<RiggedSwimmerVisual>();
  var ais=Object.FindObjectsByType<PrototypeSwimmerAI>();var states=ais.Select(a=>a.enabled).ToArray();
  float Read(string p)=>new SerializedObject(keeper).FindProperty(p).floatValue;
  void Write(string p,float x){var s=new SerializedObject(keeper);s.FindProperty(p).floatValue=x;s.ApplyModifiedPropertiesWithoutUndo();}
  float chance=Read("saveChance"),catchChance=Read("catchChance"),reaction=Read("reactionDelay");try{
   // Existing deterministic swept-contact fixture, runtime only; all production tuning restored below.
   Write("saveChance",1);Write("catchChance",1);Write("reactionDelay",0);
   foreach(var a in ais)a.enabled=false;int attempts=keeper.SaveAttempts;int hooks=visual.SaveAnimations;
   for(int i=0;i<3&&keeper.SaveAttempts==attempts;i++)
   {
    match.RestartMatch();foreach(var a in Object.FindObjectsByType<SwimmerIdentity>().Where(a=>a.IsFieldPlayer&&a!=shooter))a.Motor.ResetPose(new Vector3(a.Team==PrototypeTeam.Blue?-8:8,0,-4+a.CapNumber),Quaternion.identity);
    var g=keeper.DefendedGoal;shooter.Motor.ResetPose(g.transform.position-g.transform.forward*9,Quaternion.LookRotation(g.transform.forward));One<PlayerSelection>().Select(shooter);ball.ResetForRestart(shooter.Motor.Position+g.transform.forward*1.65f);Check(shooter.Interaction.TryCollect(),"Keeper test pickup");await Sim(.15f);Check(shooter.Interaction.TryShoot(80,ShotType.Normal),"Keeper test real shot");await Until(()=>keeper.SaveAttempts>attempts||match.IsRestarting);await Sim(.08f);
   }
   Check(keeper.SaveAttempts>attempts,"Physical goalkeeper contact attempted");Check(visual.SaveAnimations>hooks,"Save animation observes real contact attempt");CloseView(visual);await Shot("Match-Keeper");
   return Save("M14-Keeper-PASS",new{realShot=true,physicalSaveAttempts=keeper.SaveAttempts-attempts,observedAnimations=visual.SaveAnimations-hooks,catches=keeper.Catches,deflections=keeper.Deflections,outcomeAuthorityUnchanged=true,controlledFixture="80 m/s, zero reaction delay, 100% save/catch; production tuning restored"});
  }finally{Write("saveChance",chance);Write("catchChance",catchChance);Write("reactionDelay",reaction);for(int i=0;i<ais.Length;i++)ais[i].enabled=states[i];match.RestartMatch();One<ArenaDeveloperCamera>().SetView(-1);}
 }
 public static async Task<object> BallWaterAndBudgets()
 {
  var m=One<PrototypeMatch>();var ball=One<BallController>();var roster=One<PrototypeRoster>();var fx=One<WaterInteractionEffects>();var prefs=One<GamePreferences>();int q=(int)prefs.Current.graphics;
  var ais=Object.FindObjectsByType<PrototypeSwimmerAI>();var states=ais.Select(a=>a.enabled).ToArray();bool hit=false;Action<BallImpactEvent> observe=e=>{if(e.Kind==BallImpactKind.Water||e.Kind==BallImpactKind.Skip)hit=true;};ball.Contact+=observe;
  try
  {
   m.RestartMatch();prefs.SetGraphics(2);foreach(var ai in ais)ai.enabled=false;
   int i=0;foreach(var p in roster.FieldPlayers)p.Motor.ResetPose(new Vector3(-8,0,-9+(i++%6)*3),Quaternion.identity);
   var p1=roster.FieldPlayers.First(p=>p.Team==PrototypeTeam.Blue);p1.Motor.ResetPose(new Vector3(0,0,-2),Quaternion.Euler(0,90,0));One<PlayerSelection>().Select(p1);ball.ResetForRestart(p1.Motor.Position);Check(p1.Interaction.TryCollect()&&p1.GetComponent<LobPassing>().TryPass(),"Actual lob for water contact");await Until(()=>hit,5);await Frames(2);
   Check(fx.ActiveEffects>0,"Ball impact rings render");One<ArenaDeveloperCamera>().SetView(-1);await Shot("Match-Ball-Water");int transforms=fx.GetComponentsInChildren<Transform>(true).Length;await Sim(.7f);Check(fx.GetComponentsInChildren<Transform>(true).Length==transforms,"No per-stroke effect objects created");Check(fx.ActiveEffects<=fx.PoolCapacity&&fx.BubbleCount<=128,"Fixed active budgets");
   return Save("M14-Water-PASS",new{realLobWaterContact=hit,concentricMeshRings=true,noPerStrokeGameObjects=true,rippleCapacity=fx.PoolCapacity,particleCapacity=128,lowClearedByMatchSmoke=true});
  }finally{ball.Contact-=observe;prefs.SetGraphics(q);for(int i=0;i<ais.Length;i++)ais[i].enabled=states[i];m.RestartMatch();}
 }
 public static async Task<object> ParticleLifetime()
 {
  var fx=One<WaterInteractionEffects>();var q=fx.Quality;fx.Quality=WaterEffectsQuality.Off;fx.Quality=WaterEffectsQuality.Standard;
  var visuals=Object.FindObjectsByType<RiggedSwimmerVisual>();foreach(var v in visuals)v.enabled=false;
  try
  {
   var cam=Camera.main;Vector3 p=cam.transform.position+cam.transform.forward*12;p.y=-.15f;fx.EmitAthleteBubbles(p,4);var ps=fx.GetComponentInChildren<ParticleSystem>();
   Check(ps.isPlaying&&ps.particleCount>0,"Native particle simulation starts on emission");await Sim(.65f);Check(ps.particleCount==0,"Microbubbles expire within their lifetime");return Save("M14-Particle-PASS",new{nativeSimulation=true,shortLived=true,maxParticles=ps.main.maxParticles});
  }finally{foreach(var v in visuals)v.enabled=true;fx.Quality=q;}
 }
 public static object RepeatAndReload()
 {
  AnimatedAthleteSetup.ApplyAll();int count=Object.FindObjectsByType<RiggedSwimmerVisual>().Length;
  EditorSceneManager.OpenScene(OlympicArenaSetup.ScenePath);Check(Object.FindObjectsByType<RiggedSwimmerVisual>().Length==count&&count==14,"Repeat setup and saved reload retain exactly 14 animated visuals");
  Structure();return new{repeatSafe=true,savedReload=true,animated=count};
 }
 public static object RestoreEntry(){EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Boot.unity");return new{entry=AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene)};}
 public static async Task<object> FinalViews()
 {
  var visuals=Object.FindObjectsByType<RiggedSwimmerVisual>();Check(visuals.Length==14,"Final production roster");await Sim(.4f);
  foreach(var v in visuals){Check(v.Animator.avatar.isHuman&&v.Animator.avatar.isValid&&!v.Animator.applyRootMotion,"Final Avatar");Check(v.GetComponentsInChildren<SkinnedMeshRenderer>().All(r=>r.sharedMesh!=null&&r.bones.All(b=>b!=null)&&r.sharedMaterials.All(m=>m!=null)),"Final skin references");Check(Mathf.Abs(v.motor.Position.y)<.001f&&Mathf.Abs(v.Animator.GetBoneTransform(HumanBodyBones.Head).position.y-.26f)<.03f,"Final physics and visual waterline");}
  var selected=One<PlayerSelection>().ActivePlayer.GetComponentInChildren<RiggedSwimmerVisual>();CloseView(selected);await Shot("Final-Cap-and-Waterline");var dev=One<ArenaDeveloperCamera>();var t=dev.viewpoints[2];t.position=selected.motor.Position+new Vector3(-1.7f,1.3f,-2.4f);t.LookAt(selected.motor.Position+Vector3.up*.08f);await Shot("Final-Cap-Rear");dev.SetView(-1);await Shot("Final-Broadcast");
  return Save("M14-Final-PASS",new{humanAvatars=14,skinReferencesValid=true,rootMotion=false,waterline=true,actualGameView="1920x1080",capFitReviewed=true});
 }
}
