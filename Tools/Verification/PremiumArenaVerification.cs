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
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using ToduStudios.WaterPolo.Hub;
using Object=UnityEngine.Object;

public static class PremiumArenaVerification
{
 const string Out="Logs/PremiumArena/";
 static T One<T>()where T:Object=>Object.FindAnyObjectByType<T>();
 static void Check(bool b,string s){if(!b)throw new Exception("Premium arena: "+s);}
 static string Id(Object o)=>o==null?"null":GlobalObjectId.GetGlobalObjectIdSlow(o).ToString();
 static string Hash(string p){using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p)));}
 static object Save(string p,object o){Directory.CreateDirectory(Out);File.WriteAllText(Out+p+".json",JsonConvert.SerializeObject(o,Formatting.Indented));return o;}
 static string Stable(Object o){var j=JObject.Parse(EditorJsonUtility.ToJson(o));foreach(var p in j.Descendants().OfType<JProperty>().Where(p=>p.Name=="instanceID").ToArray())p.Value=0;var r=new List<string>();using(var so=new SerializedObject(o)){var p=so.GetIterator();while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference)r.Add(p.propertyPath+"="+Id(p.objectReferenceValue));}return j.ToString(Formatting.None)+string.Join(";",r);}
 static Dictionary<string,string> Protected()
 {
  var result=new Dictionary<string,string>();
  foreach(var m in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))if(!(m is ArenaEventDisplay)&&!(m is ArenaQualityController)&&!(m is TMPro.TextMeshPro)&&!(m is UniversalAdditionalCameraData))result[Id(m)]=Stable(m);
  foreach(var c in Object.FindObjectsByType<Collider>(FindObjectsInactive.Include))result[Id(c)]=Stable(c)+Stable(c.transform);
  foreach(var r in Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Include))result[Id(r)]=Stable(r);
  var camera=One<GameplayCamera>();var lens=camera.GetComponent<Camera>();result[Id(camera.transform)]=Stable(camera.transform);result[Id(lens)]=lens.fieldOfView+"|"+lens.nearClipPlane+"|"+lens.farClipPlane+"|"+lens.cullingMask+"|"+lens.rect+"|"+lens.orthographic;
  foreach(var a in Object.FindObjectsByType<SwimmerIdentity>(FindObjectsInactive.Include))foreach(var t in a.GetComponentsInChildren<Transform>(true))result[Id(t)]=Stable(t);
  foreach(var r in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include))result[Id(r)]=Stable(r);
  foreach(var f in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include).Where(f=>f.name.StartsWith("Audience")))result[Id(f)]=Stable(f)+Stable(f.GetComponent<Renderer>())+Stable(f.transform);
  return result;
 }
 public static object Baseline()
 {
  Check(!Application.isPlaying,"Saved Edit Mode required");for(int i=0;i<SceneManager.sceneCount;i++)Check(!SceneManager.GetSceneAt(i).isDirty,"Do not discard unsaved scene edits");OlympicArenaSetup.Open();
  var files=new[]{"Assets/Scenes/Boot.unity","Assets/Scenes/MainLobby.unity","ProjectSettings/EditorBuildSettings.asset","Assets/Resources/HubMatchDestination.asset","Assets/Resources/HubCatalog.asset"}.Concat(Directory.GetFiles("Assets/WaterVisuals/Materials","*.mat")).Concat(Directory.GetFiles("Assets/SwimmerAnimationPrototype","*",SearchOption.AllDirectories).Where(p=>!p.EndsWith(".meta"))).ToArray();
  Save("Baseline",new{sceneGuid=AssetDatabase.AssetPathToGUID(OlympicArenaSetup.ScenePath),sceneHash=Hash(OlympicArenaSetup.ScenePath),files=files.Select(p=>new{path=p,hash=Hash(p)}).ToArray(),records=Protected()});
  var env=One<ArenaQualityController>();var cam=One<GameplayCamera>().GetComponent<Camera>();var data=cam.GetUniversalAdditionalCameraData();
  return new{scene=SceneManager.GetActiveScene().name,dirty=SceneManager.GetActiveScene().isDirty,renderPipeline=GraphicsSettings.currentRenderPipeline?.name,ambient=RenderSettings.ambientSkyColor.ToString(),camera=new{cam.allowHDR,cam.fieldOfView,data.renderPostProcessing,mask=data.volumeLayerMask.value,position=cam.transform.position.ToString()},pipelines=env.pipelines.Select(p=>new{p.name,hdr=((UniversalRenderPipelineAsset)p).supportsHDR,additional=((UniversalRenderPipelineAsset)p).additionalLightsRenderingMode.ToString()}).ToArray(),hierarchy=Enumerable.Range(0,env.transform.childCount).Select(i=>new{env.transform.GetChild(i).name,children=env.transform.GetChild(i).childCount}).ToArray(),volumes=Object.FindObjectsByType<Volume>().Select(v=>new{v.name,v.isGlobal,profile=v.sharedProfile?.name}).ToArray()};
 }
 static async Task Until(Func<bool> p,float timeout=10){double end=EditorApplication.timeSinceStartup+timeout;while(!p()){Check(EditorApplication.timeSinceStartup<end,"Timeout");await Task.Delay(15);}}
 static async Task Frames(int n){int end=Time.frameCount+n;await Until(()=>Time.frameCount>=end);}
 static async Task Seconds(float n){double end=EditorApplication.timeSinceStartup+n;while(EditorApplication.timeSinceStartup<end)await Task.Delay(15);}
 static async Task Shot(string name){await Frames(4);ScreenCapture.CaptureScreenshot(Out+name+".png");await Frames(5);}
 static object Window(bool restore=false)
 {
  var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic;var asm=typeof(UnityEditor.Editor).Assembly;var type=asm.GetType("UnityEditor.GameView");var win=EditorWindow.GetWindow(type);var prop=type.GetProperty("selectedSizeIndex",flags);var sizes=typeof(ScriptableSingleton<>).MakeGenericType(asm.GetType("UnityEditor.GameViewSizes")).GetProperty("instance",flags).GetValue(null);var group=sizes.GetType().GetMethod("GetGroup",flags).Invoke(sizes,new[]{type.GetProperty("currentSizeGroupType",flags).GetValue(win)});
  if(restore){int old=SessionState.GetInt("PremiumArena.OriginalSize",-1),added=SessionState.GetInt("PremiumArena.AddedSize",-1);if(old>=0)prop.SetValue(win,old);if(added>=0)group.GetType().GetMethod("RemoveCustomSize").Invoke(group,new object[]{added});SessionState.SetInt("PremiumArena.AddedSize",-1);return true;}
  if(SessionState.GetInt("PremiumArena.AddedSize",-1)<0){SessionState.SetInt("PremiumArena.OriginalSize",(int)prop.GetValue(win));var size=Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),1920,1080,"Premium arena comparison"});group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});SessionState.SetInt("PremiumArena.AddedSize",(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1);}
  prop.SetValue(win,SessionState.GetInt("PremiumArena.AddedSize",0));win.Focus();return true;
 }
 public static object Focus()=>Window();
 public static object Restore(){Window(true);EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Boot.unity");return new{entry="Boot"};}
 public static Task<object> Before()=>Views(false);
 public static Task<object> After()=>Views(true);
 static async Task<object> Views(bool after)
 {
  Check(Application.isPlaying&&One<PrototypeMatch>()!=null,"Actual arena Play Mode");Window();var m=One<PrototypeMatch>();m.RestartMatch();await Frames(8);var prefs=One<GamePreferences>();int q=(int)prefs.Current.graphics;var cam=One<ArenaDeveloperCamera>();float speed=Time.timeScale;
  try
  {
   prefs.SetGraphics(2);await Frames(5);Time.timeScale=0;var prefix=after?"After":"Before";
   cam.SetView(-1);await Shot(prefix+"-Broadcast");cam.SetView(0);await Shot(prefix+"-Overview");cam.SetView(3);await Shot(prefix+"-Benches");cam.SetView(4);await Shot(prefix+"-Reverse");
   var t=cam.viewpoints[2];Vector3 p=t.position;Quaternion r=t.rotation;
   try{t.SetPositionAndRotation(new Vector3(2,4,-20),Quaternion.LookRotation(new Vector3(0,10,18)-new Vector3(2,4,-20)));cam.SetView(2);await Shot(prefix+"-Hall");}finally{t.SetPositionAndRotation(p,r);}
   return Save(prefix+"Views",new{actualGameView=true,resolution="1920x1080",views=5});
  }finally{Time.timeScale=speed;cam.SetView(-1);prefs.SetGraphics(q);}
 }
 public static object Structure()
 {
  Check(!Application.isPlaying,"Saved Edit Mode");OlympicArenaSetup.Open();var original=JObject.Parse(File.ReadAllText(Out+"Baseline.json"));
  Check(Hash(PremiumArenaSetup.Recovery)==(string)original["sceneHash"],"Exact recovery");Check(AssetDatabase.AssetPathToGUID(OlympicArenaSetup.ScenePath)==(string)original["sceneGuid"],"Same scene GUID");
  foreach(var f in original["files"])Check(Hash((string)f["path"])==(string)f["hash"],"Untouched entry/rig/water asset "+f["path"]);
  var records=Protected();var changes=((JObject)original["records"]).Properties().Where(p=>!records.TryGetValue(p.Name,out var value)||value!=(string)p.Value).Select(p=>new{id=p.Name,old=(string)p.Value,current=records.TryGetValue(p.Name,out var value)?value:"MISSING"}).ToArray();if(changes.Length>0)Save("ProtectedDifferences",changes);Check(changes.Length==0,"Protected original script/rig/physics/crowd/camera state (see differences)");
  var env=One<ArenaQualityController>();var root=env.transform.Find(PremiumArenaSetup.Name);Check(root!=null&&env.transform.Find(ArenaVenuePolishSetup.Name)==null,"One active new venue, old replacement retired");
  Check(One<PrototypeRoster>().Members.Count==14&&Object.FindObjectsByType<RiggedSwimmerVisual>().Length==14,"14 existing rigged athletes");Check(root.GetComponentsInChildren<Collider>(true).Length==0,"No venue colliders");
  var display=root.GetComponent<ArenaEventDisplay>();Check(display.match==One<PrototypeMatch>()&&display.scores.Length==6&&display.clocks.Length==6,"Six live display faces");
  foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"No missing script");
  foreach(var r in root.GetComponentsInChildren<Renderer>(true))Check(r.sharedMaterials.All(m=>m!=null),"Material references");
  Check(root.GetComponent<ChampionshipPresentation>().preferences==env.preferences&&env.fineProps!=null,"Quality observer wiring");
  var dependencies=AssetDatabase.GetDependencies(OlympicArenaSetup.ScenePath,true);Check(!dependencies.Contains(ArenaVenuePolishSetup.PrefabPath)&&!dependencies.Contains("Assets/Arena/Prefabs/BlueTeamBench.prefab")&&!dependencies.Contains("Assets/Arena/Prefabs/WhiteTeamBench.prefab"),"Retired venue/bench prefabs are no longer production scene dependencies");
  string scene=Hash(OlympicArenaSetup.ScenePath);PremiumArenaSetup.SetUp();ArenaVenuePolishSetup.SetUp();Check(Hash(OlympicArenaSetup.ScenePath)==scene,"Both setup commands preserve authored arena");
  env=One<ArenaQualityController>();root=env.transform.Find(PremiumArenaSetup.Name);
  var presentation=root.GetComponent<ChampionshipPresentation>();var probe=env.GetComponentInChildren<ReflectionProbe>();
  Check(presentation.viewCamera==One<GameplayCamera>().GetComponent<UniversalAdditionalCameraData>(),"Saved camera rendering observer reference");
  return Save("Structure-PASS",new{protectedRecords=((JObject)original["records"]).Count,athletes=14,sixLiveDisplays=true,noMissingReferences=true,exactRecovery=true,repeatSafe=true,unchangedRigWaterAndHubAssets=true,retiredVenueDependencies=false,renderers=root.GetComponentsInChildren<Renderer>(true).Length,uniqueMeshes=root.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).Distinct().Count(),referencedVertices=root.GetComponentsInChildren<MeshFilter>(true).Sum(f=>f.sharedMesh.vertexCount),standRenderers=env.transform.Find("Spectator Stands").GetComponentsInChildren<Renderer>().Length,standReferencedVertices=env.transform.Find("Spectator Stands").GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.vertexCount),totalLights=Object.FindObjectsByType<Light>().Length,reflection=AssetDatabase.GetAssetPath(probe.customBakedTexture),profiles=presentation.profiles.Select(p=>p.name).ToArray()});
 }
 static UnityEngine.UI.Button Button(string n)=>Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.name==n);
 static async Task Click(Mouse mouse,RectTransform rect)
 {
  Vector2 p=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));InputSystem.QueueStateEvent(mouse,new MouseState{position=p});await Seconds(.07f);InputSystem.QueueStateEvent(mouse,new MouseState{position=p,buttons=1});await Seconds(.07f);InputSystem.QueueStateEvent(mouse,new MouseState{position=p});await Seconds(.07f);
 }
 public static async Task<object> Gameplay()
 {
  Check(Application.isPlaying&&One<PrototypeMatch>()!=null,"Actual production arena");var m=One<PrototypeMatch>();var ball=One<BallController>();var roster=One<PrototypeRoster>();var select=One<PlayerSelection>();var visuals=Object.FindObjectsByType<RiggedSwimmerVisual>();var prefs=One<GamePreferences>();int q=(int)prefs.Current.graphics;
  var ais=Object.FindObjectsByType<PrototypeSwimmerAI>();var aEnabled=ais.Select(a=>a.enabled).ToArray();var keepers=Object.FindObjectsByType<PrototypeGoalkeeper>();var kEnabled=keepers.Select(k=>k.enabled).ToArray();var bg=InputSystem.settings.backgroundBehavior;var ed=InputSystem.settings.editorInputBehaviorInPlayMode;
  InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();
  try
  {
   m.RestartMatch();prefs.SetGraphics(2);foreach(var a in ais)a.enabled=false;foreach(var k in keepers)k.enabled=false;Check(roster.Members.Count==14&&visuals.Length==14&&Object.FindObjectsByType<Canvas>().Length==1,"Preserved roster/rigs/HUD");
   var shooter=roster.FieldPlayers.First(p=>p.Team==PrototypeTeam.Blue);select.Select(shooter);shooter.Motor.ResetPose(new Vector3(0,0,-4),Quaternion.identity);ball.ResetForRestart(new Vector3(8,0,8));Vector3 start=shooter.Motor.Position;
   InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.LeftShift));await Seconds(.7f);Check(Vector3.Distance(start,shooter.Motor.Position)>.6f&&shooter.GetComponent<SwimmerStamina>().IsSprinting,"Actual keyboard movement/sprint");InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Frames(4);
   await Click(mouse,(RectTransform)Button("PauseButton").transform);Check(m.IsPaused,"Original pointer Pause");float clock=m.PresentationTime;await Seconds(.15f);Check(m.PresentationTime==clock,"Paused clock");InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Escape));await Seconds(.09f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Seconds(.09f);Check(!m.IsPaused,"Escape resumes");
   int b=0,w=0;foreach(var p in roster.FieldPlayers)p.Motor.ResetPose(new Vector3(p.Team==PrototypeTeam.Blue?-8:8,0,-10+3.5f*(p.Team==PrototypeTeam.Blue?b++:w++)),Quaternion.identity);
   var receiver=roster.FieldPlayers.First(p=>p.Team==PrototypeTeam.Blue&&p!=shooter);shooter.Motor.ResetPose(new Vector3(-2,0,-3),Quaternion.identity);receiver.Motor.ResetPose(new Vector3(-2,0,2),Quaternion.identity);ball.ResetForRestart(shooter.Motor.Position);Check(shooter.Interaction.TryCollect()&&shooter.GetComponent<SwimmerPassing>().TryPassTo(receiver),"Physical pickup/pass");await Until(()=>ball.Owner==receiver.Interaction,4);Check(select.ActivePlayer==receiver,"Reception/selection");
   var goal=Object.FindObjectsByType<PrototypeTeamTactics>().Single(t=>t.Team==PrototypeTeam.Blue).AttackingGoal;shooter.Motor.ResetPose(goal.transform.position-goal.transform.forward*5,Quaternion.LookRotation(goal.transform.forward));select.Select(shooter);ball.ResetForRestart(shooter.Motor.Position);Check(shooter.Interaction.TryCollect()&&shooter.Interaction.TryShoot(22,ShotType.Normal),"Real shot");await Until(()=>m.BlueScore==1,5);
   var display=One<ArenaEventDisplay>();display.Refresh();Check(display.scores.All(t=>t.text.Contains("1 : 0")),"Six venue faces show actual score");await Shot("Verified-Scoring");
   m.RestartMatch();for(int i=0;i<ais.Length;i++)ais[i].enabled=aEnabled[i];for(int i=0;i<keepers.Length;i++)keepers[i].enabled=kEnabled[i];await Seconds(.5f);Check(roster.Members.All(p=>Mathf.Abs(p.Motor.Position.x)<10&&Mathf.Abs(p.Motor.Position.z)<13),"AI bounds");
   foreach(var r in Object.FindObjectsByType<PoolsideReferee>())Check(Mathf.Abs(Mathf.Abs(r.transform.position.x)-12.65f)<.05f&&Mathf.Abs(r.transform.position.y-.18f)<.01f,"Clear original referee walking lanes");
   return Save("Gameplay-PASS",new{athletes=14,riggedVisualsPreserved=true,actualKeyboardSwimSprint=true,pointerPauseEscape=true,physicalPassReception=true,physicalShotGoal=true,sixLiveScores=true,aiBounds=true,refereePaths=true,originalHUD=true});
  }
  finally{InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);InputSystem.settings.backgroundBehavior=bg;InputSystem.settings.editorInputBehaviorInPlayMode=ed;m.SetPaused(false);m.RestartMatch();prefs.SetGraphics(q);for(int i=0;i<ais.Length;i++)ais[i].enabled=aEnabled[i];for(int i=0;i<keepers.Length;i++)keepers[i].enabled=kEnabled[i];}
 }
 public static async Task<object> PresetsAndGrade()
 {
  var p=One<ChampionshipPresentation>();var prefs=One<GamePreferences>();int original=(int)prefs.Current.graphics;var c=One<GameplayCamera>().GetComponent<Camera>();var data=c.GetUniversalAdditionalCameraData();var results=new List<object>();
  try
  {
   for(int q=0;q<3;q++)
   {
    prefs.SetGraphics(q);await Frames(6);Check(data.renderPostProcessing&&c.allowHDR&&UniversalRenderPipeline.asset.supportsHDR,"HDR post-processing enabled");Check((data.volumeLayerMask.value&(1<<p.gameObject.layer))!=0&&p.volume.isGlobal&&p.volume.sharedProfile==p.profiles[q],"Volume mask and preset");
    Check(p.profiles[q].TryGet<Tonemapping>(out var tone)&&tone.mode.overrideState&&tone.mode.value==TonemappingMode.Neutral,"Neutral highlight rolloff");Check(p.profiles[q].TryGet<Bloom>(out var bloom)&&bloom.intensity.overrideState,"Bloom wiring");Check(q>0?bloom.intensity.value>0:bloom.intensity.value==0,"Bloom budget");
    Check(p.poolLights.Count(l=>l.enabled)==(q==0?2:4)&&p.technicalLights.Count(l=>l.enabled)==(q==0?0:2),"Focused light budgets");Check(One<PoolWaterVisuals>().qualityIndex==q&&One<PoolWaterVisuals>().RuntimeMaterial.shader.isSupported,"Original water preset supported");
    await Shot("Preset-"+q);results.Add(new{level=q,poolLights=q==0?2:4,technicalLights=q==0?0:2,bloom=bloom.intensity.value,water=true});
   }
   await Shot("Grade-On");bool enabled=p.volume.enabled;p.volume.enabled=false;await Shot("Grade-Off");p.volume.enabled=enabled;await Frames(4);
   var stack=data.volumeStack??VolumeManager.instance.stack;Check(stack.GetComponent<Tonemapping>().mode.value==TonemappingMode.Neutral,"Resolved actual Volume stack");
   return Save("Presets-PASS",new{presets=results,HDR=true,postProcessing=true,globalVolumeResolved=true,bloomABCaptured=true,extraRealtimeShadows=false,hardwarePerformanceTested=false});
  }finally{prefs.SetGraphics(original);p.volume.enabled=true;}
 }
 sealed class MemoryRepository:IProfileRepository{string json;public LocalProfile Load()=>json==null?null:LocalProfileCodec.FromJson(json);public void Save(LocalProfile p)=>json=JsonUtility.ToJson(p);}
 public static async Task<object> HubEntry()
 {
  await Until(()=>One<HubView>()!=null&&One<HubView>().Service!=null);var app=HubApplication.Instance;var original=app.Progression;string hash=File.Exists(HubApplication.SavePath)?Hash(HubApplication.SavePath):null;var isolated=new HubProgression(original.Catalog,new MemoryRepository());app.UseVerificationProfile(isolated);
  var mouse=InputSystem.AddDevice<Mouse>();var bg=InputSystem.settings.backgroundBehavior;var ed=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
  try
  {
   var old=One<HubView>();app.OpenLobby();await Until(()=>One<HubView>()!=null&&One<HubView>()!=old);await Frames(5);await Click(mouse,(RectTransform)One<HubView>().home.play.transform);await Click(mouse,(RectTransform)Button("Mode_training").transform);await Click(mouse,(RectTransform)Button("LaunchMode").transform);await Until(()=>SceneManager.GetActiveScene().name=="OlympicMatchArena");await Frames(8);
   Check(One<ChampionshipPresentation>()!=null&&One<PrototypeRoster>().Members.Count==14&&Object.FindObjectsByType<RiggedSwimmerVisual>().Length==14,"Existing menu launches current premium arena/rigs");await Shot("Hub-Launched-Arena");await Click(mouse,(RectTransform)Button("PauseButton").transform);Check(One<PrototypeMatch>().IsPaused,"Normal match pause");var menus=One<MatchMenuController>();menus.AskExit();menus.Confirm();await Until(()=>One<HubView>()!=null&&One<HubView>().Service==isolated);await Frames(4);Check(One<HubView>().Page=="Lobby"&&isolated.Snapshot.activeMatch==null,"Returns to same home");Check((File.Exists(HubApplication.SavePath)?Hash(HubApplication.SavePath):null)==hash,"User save bytes preserved");await Shot("Returned-Lobby");
   return Save("HubEntry-PASS",new{pointerPlayModesTrainingLaunch=true,currentArena=true,pauseExitLobby=true,noMenuEdits=true,userProfileHashUnchanged=true});
  }finally{InputSystem.RemoveDevice(mouse);InputSystem.settings.backgroundBehavior=bg;InputSystem.settings.editorInputBehaviorInPlayMode=ed;app.UseVerificationProfile(original);app.OpenLobby();}
 }
 sealed class MeshData{public Vector3[] vertices;public int[] indices;}
 static bool Blocked(Vector3 a,Vector3 b,MeshFilter[] filters,Dictionary<Mesh,MeshData> meshes)
 {
  Vector3 dir=(b-a).normalized;float length=Vector3.Distance(a,b);
  foreach(var f in filters)
  {
   var r=f.GetComponent<Renderer>();if(r==null||!r.enabled||!f.gameObject.activeInHierarchy||!r.bounds.IntersectRay(new Ray(a,dir),out float entry)||entry>length)continue;
   var mesh=f.sharedMesh;if(mesh==null)continue;if(!meshes.TryGetValue(mesh,out var data)){data=new MeshData{vertices=mesh.vertices,indices=mesh.triangles};meshes.Add(mesh,data);}
   var matrix=f.transform.worldToLocalMatrix;Vector3 origin=matrix.MultiplyPoint3x4(a),direction=matrix.MultiplyVector(dir);
   for(int i=0;i<data.indices.Length;i+=3)
   {
    Vector3 v=data.vertices[data.indices[i]],e1=data.vertices[data.indices[i+1]]-v,e2=data.vertices[data.indices[i+2]]-v,p=Vector3.Cross(direction,e2);float det=Vector3.Dot(e1,p);if(Mathf.Abs(det)<.000001f)continue;float inverse=1/det;Vector3 t=origin-v;float u=Vector3.Dot(t,p)*inverse;if(u<0||u>1)continue;Vector3 q=Vector3.Cross(t,e1);float w=Vector3.Dot(direction,q)*inverse;if(w<0||u+w>1)continue;float hit=Vector3.Dot(e2,q)*inverse;if(hit>.05f&&hit<length-.08f)return true;
   }
  }
  return false;
 }
 public static async Task<object> Sightlines()
 {
  var m=One<PrototypeMatch>();var ball=One<BallController>();var roster=One<PrototypeRoster>();var player=roster.FieldPlayers.First(p=>p.Team==PrototypeTeam.Blue);var camera=One<GameplayCamera>();var dev=One<ArenaDeveloperCamera>();var env=One<ArenaQualityController>();var filters=env.transform.Find(PremiumArenaSetup.Name).GetComponentsInChildren<MeshFilter>().Concat(env.transform.Find("Spectator Stands").GetComponentsInChildren<MeshFilter>()).Where(f=>f.GetComponent<TMPro.TextMeshPro>()==null).ToArray();var meshes=new Dictionary<Mesh,MeshData>();int rays=0;
  try
  {
   m.RestartMatch();dev.SetView(-1);
   foreach(float z in new[]{-9f,0,9f})
   {
    player.Motor.ResetPose(new Vector3(0,0,z),Quaternion.identity);One<PlayerSelection>().Select(player);ball.ResetForRestart(player.Motor.Position);player.Interaction.TryCollect();camera.SnapToTarget();await Frames(3);
    foreach(var p in roster.Members.Select(a=>a.Motor.Position+Vector3.up*.35f).Concat(Object.FindObjectsByType<GoalMouth>().Select(g=>g.transform.position+Vector3.up*.4f)))
    {
     var viewport=camera.GetComponent<Camera>().WorldToViewportPoint(p);if(viewport.z>0&&viewport.x>0&&viewport.x<1&&viewport.y>0&&viewport.y<1){Check(!Blocked(camera.transform.position,p,filters,meshes),"Venue sightline clear");rays++;}
    }
    await Shot("Framing-"+(z<0?"South":z>0?"North":"Centre"));
   }
   foreach(int side in new[]{-1,1})Check(!Blocked(new Vector3(side*12.65f,.95f,-12.5f),new Vector3(side*12.65f,.95f,12.5f),filters,meshes),"Official path clear of venue props");
   return Save("Sightlines-PASS",new{sampledVisibleTargets=rays,broadcastActionPositions=3,newVenueAndSeatingOcclusions=0,officialLanePropsClear=true,exhaustiveCameraPathProof=false});
  }finally{m.RestartMatch();dev.SetView(-1);}
 }
 public static async Task<object> FinalDetails()
 {
  Window();var dev=One<ArenaDeveloperCamera>();var t=dev.viewpoints[2];Vector3 pos=t.position;Quaternion rotation=t.rotation;var canvas=One<Canvas>();bool canvasEnabled=canvas.enabled;var prefs=One<GamePreferences>();int quality=(int)prefs.Current.graphics;float speed=Time.timeScale;var captures=new List<string>();
  try
  {
   prefs.SetGraphics(2);One<PrototypeMatch>().RestartMatch();await Frames(6);Time.timeScale=0;
   foreach(var view in new[]{new{name="White-Team-Area",from=new Vector3(12,4,2),at=new Vector3(16,.8f,7)},new{name="Officials-and-Deck",from=new Vector3(-11,4,-4),at=new Vector3(-16.2f,.85f,0)},new{name="Jumbotron-Detail",from=new Vector3(3,5,10),at=new Vector3(0,8.8f,22.4f)},new{name="Clear-Water",from=new Vector3(4,2,-9),at=new Vector3(0,-.2f,-3)}})
   {
    t.SetPositionAndRotation(view.from,Quaternion.LookRotation(view.at-view.from));dev.SetView(2);await Shot("Final-"+view.name);captures.Add(view.name);
   }
   canvas.enabled=false;dev.SetView(0);await Shot("Final-Venue-Inspection-NoHUD");canvas.enabled=canvasEnabled;dev.SetView(-1);
   // One additional wide phone ratio. The original selected Game-view size is restored immediately.
   var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic;var asm=typeof(UnityEditor.Editor).Assembly;var type=asm.GetType("UnityEditor.GameView");var win=EditorWindow.GetWindow(type);var selected=type.GetProperty("selectedSizeIndex",flags);int old=(int)selected.GetValue(win);var sizes=typeof(ScriptableSingleton<>).MakeGenericType(asm.GetType("UnityEditor.GameViewSizes")).GetProperty("instance",flags).GetValue(null);var group=sizes.GetType().GetMethod("GetGroup",flags).Invoke(sizes,new[]{type.GetProperty("currentSizeGroupType",flags).GetValue(win)});int added=-1;
   try
   {
    var size=Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),2400,1080,"Premium wide phone check"});group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});added=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1;selected.SetValue(win,added);await Frames(6);Check(Screen.width==2400&&Screen.height==1080,"Actual 20:9 Game view");
    foreach(var b in Object.FindObjectsByType<UnityEngine.UI.Button>()){var corners=new Vector3[4];((RectTransform)b.transform).GetWorldCorners(corners);Check(corners.All(c=>c.x>=-1&&c.x<=Screen.width+1&&c.y>=-1&&c.y<=Screen.height+1),"Visible HUD button bounds");}await Shot("Final-Wide-2400");
   }
   finally{selected.SetValue(win,old);if(added>=0)group.GetType().GetMethod("RemoveCustomSize").Invoke(group,new object[]{added});}
   return Save("FinalDetails-PASS",new{actualGameView=true,detailViews=captures,cleanInspectionOnlyHUDTemporarilyHidden=true,landscape16by9And20by9=true,visibleHUDBounds=true,hardwareTouchTested=false});
  }finally{canvas.enabled=canvasEnabled;Time.timeScale=speed;t.SetPositionAndRotation(pos,rotation);dev.SetView(-1);prefs.SetGraphics(quality);}
 }
 public static async Task<object> GradeComparison()
 {
  var p=One<ChampionshipPresentation>();var prefs=One<GamePreferences>();int q=(int)prefs.Current.graphics;float speed=Time.timeScale;try{prefs.SetGraphics(2);await Frames(4);Time.timeScale=0;await Shot("Grade-On");p.volume.enabled=false;await Shot("Grade-Off");return new{sameFrozenGameView=true};}finally{p.volume.enabled=true;Time.timeScale=speed;prefs.SetGraphics(q);}
 }
 public static object GradePixels()
 {
  var a=new Texture2D(2,2);var b=new Texture2D(2,2);try{a.LoadImage(File.ReadAllBytes(Out+"Grade-On.png"));b.LoadImage(File.ReadAllBytes(Out+"Grade-Off.png"));Check(a.width==b.width&&a.height==b.height,"Matching grade capture sizes");var x=a.GetPixels32();var y=b.GetPixels32();long r=0,g=0,blue=0;for(int i=0;i<x.Length;i++){r+=Math.Abs(x[i].r-y[i].r);g+=Math.Abs(x[i].g-y[i].g);blue+=Math.Abs(x[i].b-y[i].b);}var mean=new[]{(double)r/x.Length,(double)g/x.Length,(double)blue/x.Length};Check(mean.Sum()>.1,"Visible grade difference");return Save("Grade-AB",new{sameFrozenGameView=true,width=a.width,height=a.height,meanPerChannelDelta=mean});}finally{Object.DestroyImmediate(a);Object.DestroyImmediate(b);}
 }
}
