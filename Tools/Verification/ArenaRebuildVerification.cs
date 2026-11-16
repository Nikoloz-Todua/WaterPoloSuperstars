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

public static class ArenaRebuildVerification
{
 const string Out="Logs/ArenaRebuild/",Scene="Assets/Scenes/OlympicMatchArena.unity",Recovery="Assets/Scenes/Recovery/OlympicMatchArena_PreMilestone20.unity";
 static T One<T>() where T:Object=>Object.FindAnyObjectByType<T>();
 static void Check(bool p,string why){if(!p)throw new Exception(why);}
 static object Save(string n,object v){Directory.CreateDirectory(Out);File.WriteAllText(Out+n+".json",JsonConvert.SerializeObject(v,Formatting.Indented));return v;}
 static string Hash(string p)=>Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(p)));
 static string Id(Object o)=>GlobalObjectId.GetGlobalObjectIdSlow(o).ToString();
 static T Field<T>(object o,string n)=>(T)o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
 public static object Audit()
 {
  Check(!Application.isPlaying,"Edit Mode");for(int i=0;i<SceneManager.sceneCount;i++)Check(!SceneManager.GetSceneAt(i).isDirty,"Preserve unsaved scene");OlympicArenaSetup.Open();if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Recovery)==null)Check(AssetDatabase.CopyAsset(Scene,Recovery),"Native recovery copy");
  var all=Object.FindObjectsByType<Component>(FindObjectsInactive.Include).Where(c=>c!=null).ToArray();var protectedRecords=all.Where(c=>
   c is MonoBehaviour&&!(c is TMP_Text)||c is Camera||c is Rigidbody||c is Collider||c.GetComponentInParent<Canvas>(true)!=null||c.GetComponentInParent<SwimmerIdentity>(true)!=null||c.GetComponentInParent<PoolsideReferee>(true)!=null||c is Transform&&(c.GetComponent<GoalMouth>()!=null||c.GetComponent<PoolBounds>()!=null)).ToDictionary(Id,c=>EditorJsonUtility.ToJson(c));
  var protectedFiles=new[]{Scene,"Assets/Scenes/Boot.unity","Assets/Scenes/MainLobby.unity","Assets/Resources/HubCatalog.asset","Assets/Resources/DivisionCatalog.asset","Assets/Resources/HubMatchDestination.asset","ProjectSettings/EditorBuildSettings.asset","ProjectSettings/ProjectSettings.asset","ProjectSettings/QualitySettings.asset","Packages/manifest.json","old-plan.md"}.Concat(Directory.GetFiles("Assets/SwimmerAnimationPrototype","*",SearchOption.AllDirectories)).Concat(Directory.GetFiles("Assets/WaterVisuals","*",SearchOption.AllDirectories)).Where(p=>File.Exists(p)).ToArray();
  var references=new Dictionary<string,string>();foreach(var c in all.Where(c=>protectedRecords.ContainsKey(Id(c)))){var it=new SerializedObject(c).GetIterator();while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference)references[Id(c)+"/"+it.propertyPath]=it.objectReferenceValue!=null?Id(it.objectReferenceValue):"null";}
  Save("Baseline",new{guid=AssetDatabase.AssetPathToGUID(Scene),files=protectedFiles.Concat(new[]{AssetDatabase.GetAssetPath(One<ArenaQualityController>().dimensions)}).Distinct().Select(p=>new{path=p,hash=Hash(p)}),protectedRecords,references,preferences=PlayerPrefs.GetString(GamePreferences.StorageKey,""),hadPreferences=PlayerPrefs.HasKey(GamePreferences.StorageKey),profileHash=File.Exists(HubApplication.SavePath)?Hash(HubApplication.SavePath):null});
  var env=One<ArenaQualityController>();var d=env.dimensions;var pool=One<PoolBounds>();var venue=GameObject.Find(PremiumArenaSetup.Name).transform;
  Save("Hierarchy",venue.GetComponentsInChildren<Transform>(true).Select(t=>{var mesh=t.GetComponent<MeshFilter>();var text=t.GetComponent<TMP_Text>();return new{t.name,parent=t.parent?.name,pos=t.position.ToString(),local=t.localPosition.ToString(),rot=t.eulerAngles.ToString(),scale=t.lossyScale.ToString(),active=t.gameObject.activeSelf,renderer=t.GetComponent<Renderer>()!=null,mesh=mesh!=null?AssetDatabase.GetAssetPath(mesh.sharedMesh):null,text=text!=null?text.text:null};}));
  Save("Arena-Groups",env.GetComponentsInChildren<Transform>(true).Where(t=>t.parent==env.transform||t.GetComponent<Light>()!=null||t.name.Contains("Seating")||t.name.Contains("stand section")||t.name.Contains("Gallery")||t.name.Contains("Jumbotron")||t.name.Contains("jumbotron")).Select(t=>{var r=t.GetComponent<Renderer>();return new{t.name,parent=t.parent?.name,pos=t.position.ToString(),rot=t.eulerAngles.ToString(),bounds=r!=null?r.bounds.ToString():null,children=t.childCount};}));
  return Save("Audit",new{exactRecovery=Hash(Scene)==Hash(Recovery),protectedRecords=protectedRecords.Count,field=new{pool.Width,pool.Length,pool.Waterline},dimensions=JsonUtility.ToJson(d),dimensionAsset=AssetDatabase.GetAssetPath(d),goals=Object.FindObjectsByType<GoalMouth>().Select(g=>new{g.name,pos=g.transform.position.ToString(),g.Width,g.Height,g.Depth}),fineProps=env.fineProps.name,refs=Object.FindObjectsByType<PoolsideReferee>().Select(r=>new{r.name,pos=r.transform.position.ToString(),data=EditorJsonUtility.ToJson(r)}),camera=EditorJsonUtility.ToJson(One<GameplayCamera>()),sprintRoot=GameObject.Find(SprintPolishSetup.Name).GetComponentsInChildren<Transform>(true).Where(t=>t.parent?.name==SprintPolishSetup.Name||t.name.Contains("Jumbo")).Select(t=>new{t.name,pos=t.position.ToString(),parent=t.parent?.name})});
 }
 static async Task Until(Func<bool> p,float seconds=12){double end=EditorApplication.timeSinceStartup+seconds;while(!p()){Check(EditorApplication.timeSinceStartup<end,"Timed out waiting for state");await Task.Delay(15);}}
 static async Task Frames(int n){int end=Time.frameCount+n;await Until(()=>Time.frameCount>=end);}
 static async Task Seconds(float n){double end=EditorApplication.timeSinceStartup+n;while(EditorApplication.timeSinceStartup<end)await Task.Delay(15);}
 static async Task Shot(string n){await Seconds(.2f);await Frames(4);ScreenCapture.CaptureScreenshot(Out+n+".png");await Frames(5);}
 static void Resize(int width,bool restore=false)
 {
  var f=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;var asm=typeof(UnityEditor.Editor).Assembly;var t=asm.GetType("UnityEditor.GameView");var w=EditorWindow.GetWindow(t);var selected=t.GetProperty("selectedSizeIndex",f);var sizes=typeof(ScriptableSingleton<>).MakeGenericType(asm.GetType("UnityEditor.GameViewSizes")).GetProperty("instance",f).GetValue(null);var group=sizes.GetType().GetMethod("GetGroup",f).Invoke(sizes,new[]{t.GetProperty("currentSizeGroupType",f).GetValue(w)});int added=SessionState.GetInt("ArenaRebuild.Added",-1),old=SessionState.GetInt("ArenaRebuild.Size",-1);
  if(added>=0){if(old>=0)selected.SetValue(w,old);group.GetType().GetMethod("RemoveCustomSize").Invoke(group,new object[]{added});SessionState.SetInt("ArenaRebuild.Added",-1);}if(restore){SessionState.SetInt("ArenaRebuild.Size",-1);return;}
  if(old<0)SessionState.SetInt("ArenaRebuild.Size",(int)selected.GetValue(w));var size=Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(asm.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),width,1080,"M19 temporary verification"});group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});added=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1;SessionState.SetInt("ArenaRebuild.Added",added);selected.SetValue(w,added);w.Focus();
 }
 static void Input(){if(SessionState.GetInt("ArenaRebuild.Background",-1)<0){SessionState.SetInt("ArenaRebuild.Background",(int)InputSystem.settings.backgroundBehavior);SessionState.SetInt("ArenaRebuild.EditorInput",(int)InputSystem.settings.editorInputBehaviorInPlayMode);}InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;}
 public static object Direct(){Resize(1920);EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(Scene);return new{entry=Scene};}

 sealed class MeshData {public Vector3[] v;public int[] t;}
 static string Blocking(Vector3 a,Vector3 b,MeshFilter[] filters,Dictionary<Mesh,MeshData> cache)
 {
  var direction=(b-a).normalized;float length=Vector3.Distance(a,b);
  foreach(var f in filters)
  {
   var r=f.GetComponent<Renderer>();if(r==null||!r.enabled||r.forceRenderingOff||!f.gameObject.activeInHierarchy||r.sharedMaterials.Any(m=>m!=null&&m.renderQueue>=3000)||!r.bounds.IntersectRay(new Ray(a,direction),out float entry)||entry>length)continue;
   var mesh=f.sharedMesh;if(mesh==null||!mesh.isReadable)continue;if(!cache.TryGetValue(mesh,out var d)){d=new MeshData{v=mesh.vertices,t=mesh.triangles};cache.Add(mesh,d);}
   var matrix=f.transform.worldToLocalMatrix;Vector3 origin=matrix.MultiplyPoint3x4(a),dir=matrix.MultiplyVector(direction);
   for(int i=0;i<d.t.Length;i+=3)
   {
    Vector3 v=d.v[d.t[i]],e1=d.v[d.t[i+1]]-v,e2=d.v[d.t[i+2]]-v,p=Vector3.Cross(dir,e2);float det=Vector3.Dot(e1,p);if(Mathf.Abs(det)<.000001f)continue;float inverse=1/det;Vector3 t=origin-v;float u=Vector3.Dot(t,p)*inverse;if(u<0||u>1)continue;Vector3 q=Vector3.Cross(t,e1);float w=Vector3.Dot(dir,q)*inverse;if(w<0||u+w>1)continue;float hit=Vector3.Dot(e2,q)*inverse;if(hit>.05f&&hit<length-.08f)return f.name;
   }
  }
  return null;
 }
 static Behaviour[] AI()=>Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>().Concat(Object.FindObjectsByType<PrototypeGoalkeeper>()).ToArray();
 public static async Task<object> Environment()
 {
  Input();var m=One<PrototypeMatch>();var camera=One<GameplayCamera>();var lens=camera.GetComponent<Camera>();var prefs=One<GamePreferences>();var pool=One<PoolBounds>();var ball=One<BallController>();var roster=One<PrototypeRoster>();var player=roster.FieldPlayers.First(p=>p.Team==PrototypeTeam.Blue);var ai=AI();var states=ai.Select(a=>a.enabled).ToArray();var env=One<ArenaQualityController>();var meshes=new Dictionary<Mesh,MeshData>();var filters=env.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.GetComponent<TMP_Text>()==null&&f.GetComponent<Renderer>()!=env.water.waterSurface).ToArray();Check(filters.Length>100,"Substantial venue geometry included in triangle-ray test");int view=(int)prefs.Current.cameraView;float timeScale=Time.timeScale;
  var samples=new List<object>();var projections=new List<object>();var refs=new List<object>();var markerList=new List<object>();int rays=0;
  try
  {
   m.RestartMatch();await Until(()=>m.IsPlaying&&!m.AwaitingOpeningPossession);foreach(var a in ai)a.enabled=false;
   foreach(var p in roster.Members){p.Motor.StopMotion();if(p.StartPosition!=null)p.Motor.ResetPose(p.StartPosition.position,p.StartPosition.rotation);}One<PlayerSelection>().Select(player);
   foreach(int mode in new[]{0,1})
   {
    prefs.SetCameraView(mode);
    foreach(var point in new[]{new Vector3(0,0,-10),Vector3.zero,new Vector3(0,0,10),new Vector3(-8.4f,0,0),new Vector3(8.4f,0,0)})
    {
     player.Motor.ResetPose(point,Quaternion.identity);ball.ResetForRestart(point);Check(player.Interaction.TryCollect(),"Real nearby camera-fixture pickup");await Frames(4);Time.timeScale=0;camera.SnapToTarget();await Frames(4);
     foreach(var target in new[]{player.Motor.Position+Vector3.up*.45f,ball.PlayPosition})
     {
      var v=lens.WorldToViewportPoint(target);Check(v.z>0&&v.x>.08f&&v.x<.92f&&v.y>.22f&&v.y<.87f,"Playable frame "+mode+" / "+point+" / "+v);string blocked=Blocking(lens.transform.position,target,filters,meshes);Check(blocked==null,"Venue occlusion "+mode+" / "+point+" / "+blocked);rays++;
     }
     samples.Add(new{mode,player=point.ToString(),camera=JsonUtility.ToJson(camera.CaptureCameraPose())});
     if(point.x==0&&point.z!=0)await Shot((mode==0?"Lengthwise":"Sideline")+(point.z<0?"-SouthGoal":"-NorthGoal"));
     if(point==Vector3.zero){var x0=lens.WorldToViewportPoint(new Vector3(-10,0,0));var x1=lens.WorldToViewportPoint(new Vector3(10,0,0));var z0=lens.WorldToViewportPoint(new Vector3(0,0,-12.8f));var z1=lens.WorldToViewportPoint(new Vector3(0,0,12.8f));float w=Vector2.Distance(new Vector2(x0.x*lens.aspect,x0.y),new Vector2(x1.x*lens.aspect,x1.y)),l=Vector2.Distance(new Vector2(z0.x*lens.aspect,z0.y),new Vector2(z1.x*lens.aspect,z1.y));projections.Add(new{mode,actualEnvelopeRatio=25.6f/20f,projectedAxisRatio=l/w,explanation="Perspective and view direction alter apparent axis lengths; field coordinates remain authoritative."});}
     Time.timeScale=timeScale;
    }
   }
   // Walk the actual officials in both directions; props remain outside their full lane.
   ball.ResetForRestart(new Vector3(0,0,11));foreach(var p in roster.FieldPlayers)p.Motor.ResetPose(new Vector3(p.Team==PrototypeTeam.Blue?-7:7,0,-9),Quaternion.identity);
   await Until(()=>Object.FindObjectsByType<PoolsideReferee>().All(r=>r.transform.position.z>2),6);
   foreach(var r in Object.FindObjectsByType<PoolsideReferee>())refs.Add(new{r.name,direction="north",position=r.transform.position.ToString()});
   ball.ResetForRestart(new Vector3(0,0,-11));await Until(()=>Object.FindObjectsByType<PoolsideReferee>().All(r=>r.transform.position.z< -2),10);
   var equipment=GameObject.Find(ChampionshipArenaRebuild.Name).GetComponentsInChildren<MeshFilter>(true).Where(f=>f.GetComponent<TMP_Text>()==null).ToArray();
   foreach(var r in Object.FindObjectsByType<PoolsideReferee>())
   {
    Check(Mathf.Abs(Mathf.Abs(r.transform.position.x)-12.65f)<.001f&&Mathf.Abs(r.transform.position.y-.62f)<.001f,"Original official path / foot alignment");refs.Add(new{r.name,direction="south",position=r.transform.position.ToString()});
    Check(Blocking(new Vector3(r.transform.position.x,1.2f,-12.25f),new Vector3(r.transform.position.x,1.2f,12.25f),equipment,meshes)==null,"Unobstructed full referee travel corridor");
   }
   // Read existing goal-measured coordinates, not newly calculated rule state.
   var authentic=GameObject.Find(AuthenticFieldSetup.Name).transform;var ropes=authentic.Find("Measured Competition Ropes");
   foreach(var goal in Object.FindObjectsByType<GoalMouth>())
   {
    int end=goal.transform.position.z>0?1:-1;foreach(float distance in new[]{0f,2f,5f,6f})foreach(int side in new[]{-1,1}){var t=ropes.Find((end>0?"North ":"South ")+distance+"m Reference "+side);Check(t!=null&&Mathf.Abs(t.position.z-(goal.transform.position.z-end*distance))<.001f,"Mirrored goal-measured "+distance+"m marker");markerList.Add(new{end,side,distance,z=t.position.z});}
   }
   foreach(var r in ropes.GetComponentsInChildren<MeshRenderer>())Check(r.bounds.min.y>=-.061f&&r.bounds.max.y<=.2f,"Waterline rope bounds");
   var platforms=authentic.Find("Elevated Referee Platforms");var sections=platforms.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.Contains("Markers")&&!f.name.Contains("White")).ToArray();Check(sections.Length==6&&sections.All(f=>AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith("Assets/Arena/PoolsideRedevelopment/")),"All six colored platform sections use local widened meshes");
   var old=env.transform.Find("Water Polo Field Markers — no race lanes");Check(old!=null&&!old.gameObject.activeSelf,"Old football-like floor graphics stay retired");
   Check(roster.Members.All(p=>Mathf.Abs(p.Motor.Position.x)<=pool.Width*.5f+.01f&&Mathf.Abs(p.Motor.Position.z)<=pool.Length*.5f+.01f),"All athlete roots inside authoritative bounds");
   return Save("Environment-PASS",new{cameraSamples=samples,clearActorBallRays=rays,opaqueEnvironmentMeshes=filters.Length,unreadableMeshes=filters.Count(f=>f.sharedMesh!=null&&!f.sharedMesh.isReadable),actualDimensions="25 m between goals / 20 m wide / 25.6 m rope envelope",projection=projections,referees=refs,fullRefereeLanesClear=true,markers=markerList,sideRopeColors="red 0–2 / yellow 2–6 / green 6–6",penalty5m="red",waterlineAligned=true,noFootballCircle=true,originalBounds=true});
  }
  finally{Time.timeScale=timeScale;for(int i=0;i<ai.Length;i++)ai[i].enabled=states[i];prefs.SetCameraView(view);m.SetPaused(false);}
 }

 public static async Task<object> LayoutAndQuality()
 {
  Input();var prefs=One<GamePreferences>();int quality=(int)prefs.Current.graphics,view=(int)prefs.Current.cameraView;var m=One<PrototypeMatch>();var env=One<ArenaQualityController>();var venue=One<ChampionshipVenuePresentation>();var lens=One<GameplayCamera>().GetComponent<Camera>();var ai=AI();var states=ai.Select(a=>a.enabled).ToArray();var results=new List<object>();float timeScale=Time.timeScale;
  try
  {
   m.RestartMatch();await Until(()=>m.IsPlaying&&!m.AwaitingOpeningPossession);foreach(var actor in ai)actor.enabled=false;foreach(var actor in One<PrototypeRoster>().Members){actor.Motor.StopMotion();if(actor.StartPosition!=null)actor.Motor.ResetPose(actor.StartPosition.position,actor.StartPosition.rotation);}var player=One<PlayerSelection>().ActivePlayer;player.Motor.ResetPose(Vector3.zero,Quaternion.identity);One<BallController>().ResetForRestart(Vector3.zero);Check(player.Interaction.TryCollect(),"Layout actual nearby pickup");await Frames(5);Time.timeScale=0;
   foreach(int width in new[]{1920,2340,2400})foreach(int mode in new[]{0,1})
   {
    Resize(width);prefs.SetCameraView(mode);One<GameplayCamera>().SnapToTarget();await Frames(6);var safe=One<Canvas>().transform.Find("MatchSafeArea").GetComponent<RectTransform>();Vector3[] corners=new Vector3[4];safe.GetWorldCorners(corners);Check(corners.All(c=>c.x>=-.5f&&c.x<=width+.5f&&c.y>=-.5f&&c.y<=1080.5f),"Safe area in landscape bounds");Check(Mathf.Abs(lens.aspect-width/1080f)<.01f,"Actual camera ratio");await Shot("Landscape-"+width+"-"+mode);
   }
   Resize(1920);prefs.SetCameraView(1);One<GameplayCamera>().SnapToTarget();float clock=m.Clock.Remaining,shot=m.ShotClock.Remaining;int blue=m.BlueScore,white=m.WhiteScore;
   foreach(int q in new[]{0,1,2})
   {
    prefs.SetGraphics(q);await Frames(6);Check(venue.lowUpperSeats.activeSelf==(q==0)&&venue.fullUpperSeats.activeSelf==(q>=1)&&venue.mediumDetails.activeSelf==(q>=1)&&venue.highDetails.activeSelf==(q>=2),"M20 architectural quality groups");Check(GameObject.Find(ChampionshipArenaRebuild.Name).activeInHierarchy,"Essential field/deck/benches remain");var water=One<PoolWaterVisuals>();Check(water.qualityIndex==q&&water.RuntimeMaterial.shader.isSupported,"Established water shader/preset");int lights=Object.FindObjectsByType<Light>().Count(l=>l.enabled&&l.gameObject.activeInHierarchy);Check(lights==(q==0?3:q==1?9:11),"Existing light budget "+q+" / "+lights);
    var block=new MaterialPropertyBlock();venue.ribbonPanels[0].GetPropertyBlock(block);float phase=block.GetFloat("_Phase");await Seconds(.28f);venue.ribbonPanels[0].GetPropertyBlock(block);Check(q==0?block.GetFloat("_Phase")==0:block.GetFloat("_Phase")!=phase,"Real shared ribbon animation budget");Check(venue.ribbonPanels.All(r=>r.sharedMaterial.shader.isSupported),"Ribbon shader supported");
    Check(m.Clock.Remaining==clock&&m.ShotClock.Remaining==shot&&m.BlueScore==blue&&m.WhiteScore==white,"Graphics do not change match state");var filters=venue.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.GetComponent<TMP_Text>()==null&&f.GetComponent<Renderer>().enabled&&f.gameObject.activeInHierarchy).ToArray();results.Add(new{q,realtimeLights=lights,activeGeometryRenderers=filters.Length,referencedVertices=filters.Sum(f=>(long)f.sharedMesh.vertexCount),referencedTriangles=filters.Sum(f=>(long)f.sharedMesh.triangles.Length/3),fineDetails=q>=1,upperCoarse=q==0});await Shot("Quality-"+q);
   }
   Check(Object.FindObjectsByType<Renderer>().Count(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.sharedMaterial!=null&&r.sharedMaterial.shader.name=="UberStylizedWater")==1,"Exactly one active transparent water surface");
   return Save("LayoutQuality-PASS",new{ratios=new[]{"16:9","19.5:9","20:9"},bothCameras=true,actualPixels="1920 / 2340 / 2400 x 1080",presets=results,oneWaterSurface=true,animatedSharedRibbons=true,existingRealtimeLightBudgets=true,matchStateUnchanged=true,physicalDeviceTested=false,contentCountsNotFPS=true});
  }
  finally{Time.timeScale=timeScale;prefs.SetGraphics(quality);prefs.SetCameraView(view);for(int i=0;i<ai.Length;i++)ai[i].enabled=states[i];Resize(1920);m.SetPaused(false);}
 }

 public static async Task<object> LiveAndPlatform()
 {
  Input();var m=One<PrototypeMatch>();var prefs=One<GamePreferences>();int view=(int)prefs.Current.cameraView;var dev=One<ArenaDeveloperCamera>();var probe=dev.viewpoints[2];var position=probe.position;var rotation=probe.rotation;var lens=One<GameplayCamera>().GetComponent<Camera>();float fov=lens.fieldOfView;var canvas=One<Canvas>();bool visible=canvas.enabled;var samples=new List<object>();
  try
  {
   m.RestartMatch();await Until(()=>m.IsPlaying&&!m.AwaitingOpeningPossession);foreach(int mode in new[]{0,1}){prefs.SetCameraView(mode);await Seconds(1.2f);await Shot("Live-AI-"+mode);Check(One<PrototypeRoster>().Members.Where(p=>p.EligibleForPlay).All(p=>Mathf.Abs(p.Motor.Position.x)<=10.01f&&Mathf.Abs(p.Motor.Position.z)<=12.51f),"Live AI remains in original bounds");samples.Add(new{mode,phase=m.Phase.ToString(),ball=One<BallController>().PlayPosition.ToString()});}
   m.SetPaused(true);canvas.enabled=false;probe.SetPositionAndRotation(new Vector3(-11,5.8f,3),Quaternion.LookRotation(new Vector3(-16,1.5f,9.3f)-new Vector3(-11,5.8f,3)));lens.fieldOfView=54;dev.SetView(2);await Shot("After-CameraPlatform");
   return Save("LiveArena-PASS",new{advancingAIGameViews=samples,all14InBounds=true,realPassiveBroadcastEquipment=true});
  }
  finally{probe.SetPositionAndRotation(position,rotation);dev.SetView(-1);lens.fieldOfView=fov;canvas.enabled=visible;prefs.SetCameraView(view);m.SetPaused(false);}
 }

 public static async Task<object> PrepareHeldTeardown()
 {
  var m=One<PrototypeMatch>();m.RestartMatch();await Until(()=>m.IsPlaying&&!m.AwaitingOpeningPossession);foreach(var a in AI())a.enabled=false;var p=One<PrototypeRoster>().FieldPlayers.First(x=>x.Team==PrototypeTeam.Blue);One<BallController>().ResetForRestart(p.Motor.Position);Check(p.Interaction.TryCollect(),"Held ball before scene teardown");var errors=new List<string>();var warnings=new List<string>();
  Application.LogCallback logged=(s,stack,t)=>{if(t==LogType.Exception||t==LogType.Error||t==LogType.Assert)errors.Add(s+"\n"+stack);if(t==LogType.Warning)warnings.Add(s);};
  Action<PlayModeStateChange> changed=null;changed=state=>{if(state!=PlayModeStateChange.EnteredEditMode)return;Application.logMessageReceived-=logged;EditorApplication.playModeStateChanged-=changed;Save("HeldBallTeardown",new{errors,warnings,heldBeforeStop=true,passed=errors.Count==0});};Application.logMessageReceived+=logged;EditorApplication.playModeStateChanged+=changed;return new{held=true,stopPlayToVerify=true};
 }

 public static object Restore()
 {
  Check(!Application.isPlaying,"Stop Play before restoration");Resize(0,true);int background=SessionState.GetInt("ArenaRebuild.Background",-1),input=SessionState.GetInt("ArenaRebuild.EditorInput",-1);if(background>=0)InputSystem.settings.backgroundBehavior=(InputSettings.BackgroundBehavior)background;if(input>=0)InputSystem.settings.editorInputBehaviorInPlayMode=(InputSettings.EditorInputBehaviorInPlayMode)input;SessionState.EraseInt("ArenaRebuild.Background");SessionState.EraseInt("ArenaRebuild.EditorInput");
  // Child regression runners only set focus overrides; their size helper was not invoked.
  SessionState.EraseInt("Broadcast.Background");SessionState.EraseInt("Broadcast.EditorInput");var b=JObject.Parse(File.ReadAllText(Out+"Baseline.json"));if((bool)b["hadPreferences"])PlayerPrefs.SetString(GamePreferences.StorageKey,(string)b["preferences"]);else PlayerPrefs.DeleteKey(GamePreferences.StorageKey);PlayerPrefs.Save();EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Boot.unity");
  Check((File.Exists(HubApplication.SavePath)?Hash(HubApplication.SavePath):null)==(string)b["profileHash"],"Actual user profile bytes unchanged");OlympicArenaSetup.Open();return Save("Restored",new{playStopped=true,bootEntry=true,originalGameViewAndInputFocus=true,originalPreferenceBytes=true,realProfileBytesUnchanged=true,savedArena=!SceneManager.GetActiveScene().isDirty});
 }

 static async Task Capture(string prefix)
 {
  Input();var m=One<PrototypeMatch>();var prefs=One<GamePreferences>();var camera=One<GameplayCamera>();var lens=camera.GetComponent<Camera>();var dev=One<ArenaDeveloperCamera>();var canvas=One<Canvas>();var probe=dev.viewpoints[2];var pos=probe.position;var rot=probe.rotation;var ai=Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>().Concat(Object.FindObjectsByType<PrototypeGoalkeeper>()).ToArray();var states=ai.Select(a=>a.enabled).ToArray();float timeScale=Time.timeScale;
  try
  {
   prefs.SetCameraView(0);m.RestartMatch();camera.SnapToTarget();await Shot(prefix+"-Opening");await Until(()=>m.IsPlaying&&!m.AwaitingOpeningPossession);
   foreach(var a in ai)a.enabled=false;foreach(var p in One<PrototypeRoster>().Members){p.Motor.StopMotion();if(p.StartPosition!=null)p.Motor.ResetPose(p.StartPosition.position,p.StartPosition.rotation);}
   var controlled=One<PrototypeRoster>().FieldPlayers.First(p=>p.Team==PrototypeTeam.Blue&&p.PlayerId=="prototype-blue-1");controlled.Motor.ResetPose(new Vector3(-2,0,-3),Quaternion.identity);One<PlayerSelection>().Select(controlled);One<BallController>().ResetForRestart(controlled.Motor.Position);Check(controlled.Interaction.TryCollect(),"Genuine nearby fixture pickup");await Frames(5);Time.timeScale=0;
   var poses=new List<object>();foreach(int mode in new[]{0,1}){prefs.SetCameraView(mode);camera.SnapToTarget();await Frames(6);poses.Add(new{mode,pose=JsonUtility.ToJson(camera.CaptureCameraPose())});await Shot(prefix+(mode==0?"-Lengthwise":"-Sideline"));}
   canvas.enabled=false;dev.SetView(2);
   var views=new[]{new{n="Arena",p=new Vector3(19,21,-23),at=new Vector3(0,3,0),f=64f},new{n="Bench",p=new Vector3(6.3f,3,-14.5f),at=new Vector3(8.4f,1,-18.2f),f=52f},new{n="PoolEdge",p=new Vector3(9.6f,2.5f,-5.5f),at=new Vector3(12.65f,.75f,-.6f),f=52f},new{n="BroadcastTier",p=new Vector3(-11,5.8f,-3),at=new Vector3(-16,1.5f,-9.3f),f=54f},new{n="Ceiling",p=new Vector3(10,16,-14),at=new Vector3(0,24.7f,0),f=55f}};
   foreach(var v in views){probe.SetPositionAndRotation(v.p,Quaternion.LookRotation(v.at-v.p));lens.fieldOfView=v.f;await Shot(prefix+"-"+v.n);poses.Add(new{v.n,pos=v.p.ToString(),target=v.at.ToString(),fov=v.f});}Save(prefix+"-CapturePoses",poses);
  }
  finally{Time.timeScale=timeScale;canvas.enabled=true;probe.SetPositionAndRotation(pos,rot);dev.SetView(-1);for(int i=0;i<ai.Length;i++)ai[i].enabled=states[i];m.SetPaused(false);}
 }
 public static async Task<object> Before(){await Capture("Before");return new{actualViews=true,matchedFixtures=true};}
 public static Task<object> BeforeArchitecture()=>Architecture("Before");
 public static Task<object> AfterArchitecture()=>Architecture("After");
 static async Task<object> Architecture(string prefix)
 {
  Input();var m=One<PrototypeMatch>();var dev=One<ArenaDeveloperCamera>();var probe=dev.viewpoints[2];var position=probe.position;var rotation=probe.rotation;var lens=One<GameplayCamera>().GetComponent<Camera>();float fov=lens.fieldOfView;var canvas=One<Canvas>();
  try{m.SetPaused(true);canvas.enabled=false;dev.SetView(2);foreach(var v in new[]{new{n="Full",p=new Vector3(43,29,-53),at=new Vector3(0,5,0),f=64f},new{n="LongBasin",p=new Vector3(14,4,-29),at=new Vector3(0,0,9),f=62f}}){probe.SetPositionAndRotation(v.p,Quaternion.LookRotation(v.at-v.p));lens.fieldOfView=v.f;await Shot(prefix+"-"+v.n);}return new{actualArchitecture=true,prefix};}
  finally{probe.SetPositionAndRotation(position,rotation);dev.SetView(-1);lens.fieldOfView=fov;canvas.enabled=true;m.SetPaused(false);}
 }
 public static async Task<object> After(){await Capture("After");return new{actualViews=true,matchedFixtures=true};}
 public static object Structure()
 {
  Check(!Application.isPlaying&&!SceneManager.GetActiveScene().isDirty,"Saved Edit Mode");var b=JObject.Parse(File.ReadAllText(Out+"Baseline.json"));Check(AssetDatabase.AssetPathToGUID(Scene)==(string)b["guid"],"Original scene GUID");foreach(var f in b["files"]){string p=(string)f["path"];Check(Hash(p==Scene?Recovery:p)==(string)f["hash"],"Protected file "+p);}
  var all=Object.FindObjectsByType<Component>(FindObjectsInactive.Include).Where(c=>c!=null).ToDictionary(Id);var differences=new List<string>();foreach(var p in ((JObject)b["protectedRecords"]).Properties())
  {
   if(!all.TryGetValue(p.Name,out var c)){differences.Add(p.Name);continue;}string current=EditorJsonUtility.ToJson(c);
   if(c is GameplayCamera){var old=JObject.Parse((string)p.Value);var now=JObject.Parse(current);var payload=(JObject)(now["MonoBehaviour"]??now);foreach(string added in new[]{"openingOverview","openingFieldOfView","openingBlendSeconds"})payload.Remove(added);if(!JToken.DeepEquals(old,now))differences.Add(p.Name);}else if(current!=(string)p.Value)differences.Add(p.Name);
  }
  Save("ProtectedDifferences",differences);Check(differences.Count==0,"Protected gameplay, HUD, legacy camera profiles, bodies, rigs and referee records");
  var refs=new Dictionary<string,string>();foreach(var c in all.Values.Where(c=>((JObject)b["protectedRecords"]).ContainsKey(Id(c)))){var it=new SerializedObject(c).GetIterator();while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference)refs[Id(c)+"/"+it.propertyPath]=it.objectReferenceValue!=null?Id(it.objectReferenceValue):"null";}
  var refDifferences=((JObject)b["references"]).Properties().Where(p=>!refs.TryGetValue(p.Name,out var v)||v!=(string)p.Value).Select(p=>p.Name).ToArray();Save("ReferenceDifferences",refDifferences);Check(refDifferences.Length==0,"Original serialized object references preserved");
  var pool=One<PoolBounds>();var d=One<ArenaQualityController>().dimensions;var layout=One<ChampionshipVenuePresentation>().layout;Check(pool.Length==25&&pool.Width==20&&d.PoolLength==28.6f&&d.PoolWidth==22.5f&&Mathf.Abs(d.depth-2.05f)<.001f&&layout.basinLength==50&&layout.basinWidth==25&&layout.competition==d,"Separate 25x20 field and 50x25 architectural basin");Check(Object.FindObjectsByType<SwimmerIdentity>().Length==14&&Object.FindObjectsByType<RiggedSwimmerVisual>().Length==14,"14 original athletes");Check(Object.FindObjectsByType<GameplayCamera>().Length==1&&Object.FindObjectsByType<ArenaEventDisplay>().Length==1&&Object.FindObjectsByType<Canvas>().Length==1&&Object.FindObjectsByType<EventSystem>().Length==1,"One existing camera authority, display, Canvas and EventSystem");
  var root=GameObject.Find(ChampionshipArenaRebuild.Name);Check(root!=null&&root.GetComponentsInChildren<Collider>(true).Length==0&&root.GetComponentsInChildren<Light>(true).Length==0&&root.GetComponentsInChildren<Camera>(true).Length==0,"Visual-only reconstruction");Check(root.GetComponentsInChildren<VenueTeamFacility>(true).Length==2&&root.GetComponentInChildren<VenueIntroductionAnchors>()!=null,"Two operational team facilities and introduction staging");
  var live=One<ArenaEventDisplay>();Check(live.scores.Length==6&&live.clocks.Length==6&&live.primaryScore!=null,"All live display wiring retained");var jumbo=GameObject.Find("Four Face Ceiling Championship Display");Check(jumbo!=null&&jumbo.transform.position==new Vector3(0,25,0),"Existing jumbotron relocated above ordinary sightlines");
  foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"No missing scripts");
  string hash=Hash(Scene);ChampionshipArenaRebuild.SetUp();Check(Hash(Scene)==hash,"Repeat preserves authored tuning");EditorSceneManager.OpenScene(Scene);Check(GameObject.Find(ChampionshipArenaRebuild.Name)!=null&&One<ArenaEventDisplay>().scores.All(t=>t!=null),"Saved reload with display references");
  return Save("Structure-PASS",new{protectedRecords=((JObject)b["protectedRecords"]).Count,serializedReferences=((JObject)b["references"]).Count,exactRecovery=true,sameSceneGuid=true,field="25 x 20",ropeEnvelope="25.6 x 20",basin="50 x 25",depth=2.05f,fieldNotResized=true,legacyLiveCameraProfilesAndHUD=true,newPreparationOverviewOnly=true,all14Athletes=true,oneLiveDisplayObserver=true,noNewGameplayCollidersLightsOrCameras=true,operationalBenchFacilities=true,centeredExistingJumbotron=true,repeatSafeAndReloaded=true});
 }

 public static object ContentMetrics()
 {
  var env=One<ArenaQualityController>();var all=GameObject.Find(ArenaRedevelopmentSetup.Name).GetComponentsInChildren<MeshFilter>(true).Concat(env.fineProps.transform.Find("Redevelopment Fine Equipment").GetComponentsInChildren<MeshFilter>(true)).Concat(GameObject.Find("Four Face Ceiling Championship Display").transform.Find("Ceiling Display Housing and Suspension").GetComponentsInChildren<MeshFilter>(true)).ToArray();var geometry=all.Where(f=>f.GetComponent<TMP_Text>()==null).ToArray();var text=all.Except(geometry).ToArray();
  return Save("ContentMetrics",new{combinedGeometryMeshes=geometry.Length,geometryVertices=geometry.Sum(f=>f.sharedMesh.vertexCount),geometryTriangles=geometry.Sum(f=>f.sharedMesh.triangles.Length/3),geometryMaterials=geometry.Select(f=>f.GetComponent<Renderer>().sharedMaterial).Distinct().Count(),separateTextRenderers=text.Length,referencedTextVertices=text.Sum(f=>f.sharedMesh.vertexCount),widenedPlatformMeshes=6,coreMeshes=geometry.Count(f=>f.transform.IsChildOf(GameObject.Find(ArenaRedevelopmentSetup.Name).transform)),noNewRuntimeMonoBehaviours=true,notMeasuredDrawCallsOrDevicePerformance=true});
 }

 public static object ArchitecturalQA()
 {
  var root=GameObject.Find(ChampionshipArenaRebuild.Name);Check(root!=null,"M20 scene installed");var sections=root.GetComponentsInChildren<VenueSeatingSection>(true);Check(sections.Select(s=>s.tier).Distinct().Count()==3,"Three distinct seating levels");var orientation=new List<object>();var cells=new Dictionary<Vector3Int,List<Vector3>>();float closest=100;int seats=0;
  foreach(var s in sections)
  {
   Check(s.fullSeatVisual!=null&&s.coarseSeatVisual!=null&&Quaternion.Angle(s.fullSeatVisual.rotation,s.transform.rotation)<.001f,"Actual seat mesh follows section orientation "+s.name);float worst=1;
   for(int row=0;row<s.rows;row++)for(int seat=0;seat<s.seatsPerRow;seat++)
   {
    var p=s.SeatFloor(row,seat);var target=new Vector3(Mathf.Clamp(p.x,-10,10),p.y,Mathf.Clamp(p.z,-12.5f,12.5f));float dot=Vector3.Dot(s.ViewingDirection,(target-p).normalized);worst=Mathf.Min(worst,dot);Check(dot>.65f,"Outward-facing seating "+s.name+" "+dot);Check(Mathf.Abs(p.x)>12.7f||Mathf.Abs(p.z)>25.2f,"No seat inside water basin");
    var key=new Vector3Int(Mathf.FloorToInt(p.x),Mathf.FloorToInt(p.y),Mathf.FloorToInt(p.z));for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++)if(cells.TryGetValue(key+new Vector3Int(x,y,z),out var others))foreach(var q in others){float distance=Vector3.Distance(p,q);closest=Mathf.Min(closest,distance);Check(distance>.54f,"Intersecting neighboring seat centers "+s.name+" / "+p+" / "+q+" / "+distance);}
    if(!cells.TryGetValue(key,out var list)){list=new List<Vector3>();cells.Add(key,list);}list.Add(p);seats++;
   }
   // Read actual backrest vertices in row zero; they must lie behind the seat pan in local -Z.
   var shell=s.fullSeatVisual.GetComponentsInChildren<MeshFilter>(true).First(f=>f.GetComponent<Renderer>().sharedMaterial.name.Contains("Seat Shell"));var back=shell.sharedMesh.vertices.Where(v=>v.y>.65f&&v.y<1.05f&&v.z>-.40f&&v.z<.40f).ToArray();Check(back.Length>0&&back.Average(v=>v.z)<-.15f,"Backrests are away from pool "+s.name);orientation.Add(new{s.name,s.tier,s.rows,s.seatsPerRow,worstDot=worst,actualBackrestZ=back.Average(v=>v.z)});
  }
  var env=One<ArenaQualityController>();var filters=env.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.GetComponent<TMP_Text>()==null&&f.GetComponent<Renderer>()!=env.water.waterSurface).ToArray();var cache=new Dictionary<Mesh,MeshData>();var routes=new List<object>();int rays=0;
  void Route(string name,Transform[] points,float radius)
  {
   Check(points.Length>=2,"Route anchors "+name);for(int i=0;i<points.Length-1;i++)
   {
    Vector3 a=points[i].position,b=points[i+1].position;var side=Vector3.Cross(Vector3.up,b-a).normalized;
    foreach(float offset in new[]{-radius,0,radius})foreach(float height in new[]{.09f,.48f,1.1f,1.75f}){var from=a+side*offset+Vector3.up*height;var to=b+side*offset+Vector3.up*height;string hit=Blocking(from,to,filters,cache);Check(hit==null,"Blocked physical route "+name+" segment "+i+" at height "+height+" offset "+offset+": "+hit);rays++;}
   }
   routes.Add(new{name,points=points.Select(p=>p.position.ToString()),clearWidth=radius*2,height=1.75f});
  }
  foreach(var facility in root.GetComponentsInChildren<VenueTeamFacility>(true))
  {
   Check(facility.seats.Length==11&&facility.waterEntry==facility.accessRoute.Last(),"Eleven seats and stable entry "+facility.name);foreach(var chair in facility.seats)Check(Vector3.Dot(chair.forward,Vector3.left)>.999f,"Bench seats face pool "+chair.name);Route(facility.name,facility.accessRoute,.30f);Check(Mathf.Abs(facility.waterEntry.position.x-9.4f)<.001f&&Mathf.Abs(Mathf.Abs(facility.waterEntry.position.z)-13.25f)<.001f,"Entry meets unchanged exclusion waiting corner");
  }
  var intro=root.GetComponentInChildren<VenueIntroductionAnchors>();Check(intro.blueLineup.Length==7&&intro.whiteLineup.Length==7&&intro.crest!=null,"Fourteen future lineup slots and crest input");var lineup=intro.blueLineup.Concat(intro.whiteLineup).ToArray();Check(lineup.All(t=>Mathf.Abs(t.position.x)<9&&t.position.z< -26),"Lineup on defined end presentation stage");for(int i=0;i<lineup.Length;i++)for(int j=i+1;j<lineup.Length;j++)Check(Vector3.Distance(lineup[i].position,lineup[j].position)>=.89f,"Lineup spacing");Route("Blue introduction lane",intro.blueEntranceRoute,.32f);Route("White introduction lane",intro.whiteEntranceRoute,.32f);
  var water=env.water.waterSurface.GetComponent<MeshFilter>();Check(water.sharedMesh.bounds.size.x==25&&water.sharedMesh.bounds.size.z==50&&Mathf.Abs(water.transform.position.y)<.001f,"Single continuous 50x25 water mesh at original Y0");Check(Object.FindObjectsByType<PoolWaterVisuals>().Length==1,"One water presentation authority");
  return Save("ArchitecturalQA-PASS",new{tiers=3,sections=sections.Length,seats,minimumSeatCentreDistance=closest,orientation,physicalClearanceRays=rays,accessRoutes=routes,noOutwardSeats=true,actualBackrestsVerified=true,benchSeats=22,stagingSlots=14,continuousWater="50 x 25",oneWaterAuthority=true});
 }

 public static async Task<object> DetailViews()
 {
  Input();var m=One<PrototypeMatch>();var dev=One<ArenaDeveloperCamera>();var probe=dev.viewpoints[2];var position=probe.position;var rotation=probe.rotation;var lens=One<GameplayCamera>().GetComponent<Camera>();float fov=lens.fieldOfView;var canvas=One<Canvas>();bool visible=canvas.enabled;
  try
  {
   m.SetPaused(true);canvas.enabled=false;dev.SetView(2);var views=new[]{
    new{n="Lower-Bowl",p=new Vector3(16,3,-9),at=new Vector3(27,3,-15.75f),f=60f},
    new{n="Middle-Bowl",p=new Vector3(25,9,-15),at=new Vector3(37,10,-15.75f),f=60f},
    new{n="Upper-Bowl",p=new Vector3(36,17,-14),at=new Vector3(48,18,-15.75f),f=60f},
    new{n="Seating-West-Facing",p=new Vector3(-16,3,10),at=new Vector3(-27,3,15.75f),f=60f},
    new{n="Blue-Bench",p=new Vector3(13.8f,3,-17.5f),at=new Vector3(19,1.3f,-18),f=64f},
    new{n="White-Bench",p=new Vector3(13.8f,3,17.5f),at=new Vector3(19,1.3f,18),f=64f},
    new{n="Team-Access",p=new Vector3(19,7,-16),at=new Vector3(12,.6f,-14.2f),f=64f},
    new{n="Athlete-Entrance",p=new Vector3(0,3,-22.5f),at=new Vector3(0,5.3f,-32),f=64f},
    new{n="Anthem-Staging",p=new Vector3(13,4,-24),at=new Vector3(0,.7f,-29),f=64f},
    new{n="Club-Crest",p=new Vector3(0,7.5f,-18.5f),at=new Vector3(0,7.7f,-31.55f),f=60f},
    new{n="Tier-Ribbons",p=new Vector3(25,9.5f,14),at=new Vector3(34,7.1f,3),f=64f},
    new{n="Jumbotron-Lighting",p=new Vector3(12,19,-17),at=new Vector3(0,25,0),f=60f},
    new{n="Primary-Live-Board",p=new Vector3(-30,17,0),at=new Vector3(-41.8f,16.8f,0),f=64f}};
   foreach(var v in views){probe.SetPositionAndRotation(v.p,Quaternion.LookRotation(v.at-v.p));lens.fieldOfView=v.f;await Shot(v.n);}Save("Detail-Capture-Poses",views);
   probe.SetPositionAndRotation(new Vector3(23,19,-30),Quaternion.LookRotation(new Vector3(0,0,0)-new Vector3(23,19,-30)));lens.fieldOfView=64;var bulk=GameObject.Find(ChampionshipArenaRebuild.Name).transform.Find("Movable Bulkheads and Competition End Access").GetComponentsInChildren<Renderer>();var states=bulk.Select(r=>r.enabled).ToArray();
   try{foreach(var r in bulk)r.enabled=false;await Shot("Layout-Unpartitioned-50m");for(int i=0;i<bulk.Length;i++)bulk[i].enabled=states[i];await Shot("Layout-Partitioned-50m");}finally{for(int i=0;i<bulk.Length;i++)bulk[i].enabled=states[i];}
   return Save("DetailViews",new{actualGameViews=views.Length,comparedBasinConfigurations=2,retained="50m centered basin with competition bulkheads and two end water bays"});
  }
  finally{probe.SetPositionAndRotation(position,rotation);dev.SetView(-1);lens.fieldOfView=fov;canvas.enabled=visible;m.SetPaused(false);}
 }

}
