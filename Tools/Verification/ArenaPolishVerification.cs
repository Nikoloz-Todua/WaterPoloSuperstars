using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TMPro;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.WaterVisuals;
using ToduStudios.WaterPolo.Editor;
using ToduStudios.WaterPolo.Hub;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;

public static class ArenaPolishVerification
{
    const string Out="Logs/ArenaPolish/";
    static T One<T>() where T:Object=>Object.FindAnyObjectByType<T>();
    static void Check(bool value,string message){if(!value)throw new Exception("Arena polish: "+message);}
    static string Hash(string path){using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));}
    static object Save(string name,object data){Directory.CreateDirectory(Out);File.WriteAllText(Out+name+".json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));return data;}
    public static object Baseline()
    {
        Check(!Application.isPlaying,"Saved Edit Mode required");OlympicArenaSetup.Open();Directory.CreateDirectory(Out);
        var path=Out+"Before.json";
        if(!File.Exists(path))Save("Before",new{
            sceneGuid=AssetDatabase.AssetPathToGUID(OlympicArenaSetup.ScenePath),sceneHash=Hash(OlympicArenaSetup.ScenePath),
            files=new[]{"Assets/Scenes/Boot.unity","Assets/Scenes/MainLobby.unity","ProjectSettings/EditorBuildSettings.asset","Assets/Resources/HubMatchDestination.asset","Assets/Resources/HubCatalog.asset"}.Select(p=>new{path=p,hash=Hash(p)}).ToArray(),
            scripts=Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include).Select(m=>new{id=GlobalObjectId.GetGlobalObjectIdSlow(m).ToString(),type=m.GetType().FullName,json=EditorJsonUtility.ToJson(m)}).ToArray(),
            physics=Object.FindObjectsByType<Collider>(FindObjectsInactive.Include).Select(c=>new{id=GlobalObjectId.GetGlobalObjectIdSlow(c).ToString(),json=EditorJsonUtility.ToJson(c),position=c.transform.position.ToString("F5"),rotation=c.transform.rotation.ToString("F5"),scale=c.transform.lossyScale.ToString("F5")}).ToArray(),
            athletes=Object.FindObjectsByType<ArenaAthleteVisual>(FindObjectsInactive.Include).Select(v=>new{id=GlobalObjectId.GetGlobalObjectIdSlow(v).ToString(),json=EditorJsonUtility.ToJson(v),pos=v.transform.position.ToString("F5")}).ToArray(),
            audience=Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include).Where(f=>f.name.StartsWith("Audience")).Select(f=>new{id=GlobalObjectId.GetGlobalObjectIdSlow(f).ToString(),mesh=AssetDatabase.GetAssetPath(f.sharedMesh),position=f.transform.position.ToString("F5")}).ToArray()});
        var q=One<ArenaQualityController>();var root=q.transform;
        return new{scene=SceneManager.GetActiveScene().name,children=Enumerable.Range(0,root.childCount).Select(i=>new{name=root.GetChild(i).name,position=root.GetChild(i).position.ToString(),children=root.GetChild(i).childCount}).ToArray(),
            light=Object.FindObjectsByType<Light>().Select(l=>new{l.name,l.intensity,position=l.transform.position.ToString(),rotation=l.transform.eulerAngles.ToString()}).ToArray(),
            camera=One<GameplayCamera>().transform.position.ToString(),viewpoints=One<ArenaDeveloperCamera>().viewpoints.Select(t=>new{t.name,position=t.position.ToString()}).ToArray(),renderers=Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include).Length};
    }
    public static async Task<object> BeforeViews()
    {
        await Until(()=>One<ArenaDeveloperCamera>()!=null,10);await Frames(6);var cam=One<ArenaDeveloperCamera>();cam.SetView(-1);await Shot("Before-Broadcast");cam.SetView(0);await Shot("Before-Overview");cam.SetView(3);await Shot("Before-Benches");cam.SetView(-1);return new{captured=true,advancing=true};
    }
    static string Id(Object value)
    {
        if(value==null)return "null";
        string id=GlobalObjectId.GetGlobalObjectIdSlow(value).ToString();
        string scene=AssetDatabase.AssetPathToGUID(SceneManager.GetActiveScene().path);return id.Replace(scene,"ARENA");
    }
    static string Stable(Object value)
    {
        var json=JObject.Parse(EditorJsonUtility.ToJson(value));
        using(var serialized=new SerializedObject(value))
        {
            var property=serialized.GetIterator();
            while(property.Next(true))if(property.propertyType==SerializedPropertyType.ObjectReference)
            {
                var node=json.SelectToken(property.propertyPath.Replace(".Array.data[","["));
                if(node is JObject reference)reference["instanceID"]=Id(property.objectReferenceValue);
            }
        }
        return json.ToString(Newtonsoft.Json.Formatting.None);
    }
    static Dictionary<string,string> Protected()
    {
        var values=new Dictionary<string,string>();
        foreach(var m in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))if(!(m is ArenaEventDisplay))values[Id(m)]=Stable(m);
        foreach(var c in Object.FindObjectsByType<Collider>(FindObjectsInactive.Include))values[Id(c)]=Stable(c)+c.transform.position.ToString("F6")+c.transform.rotation.ToString("F6")+c.transform.lossyScale.ToString("F6");
        foreach(var body in Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Include))values[Id(body)]=Stable(body);
        foreach(var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))values[Id(camera)]=Stable(camera)+Stable(camera.transform);
        foreach(var v in Object.FindObjectsByType<ArenaAthleteVisual>(FindObjectsInactive.Include))foreach(var t in v.GetComponentsInChildren<Transform>(true))values[Id(t)]=Stable(t);
        foreach(var q in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include).Where(f=>f.name.StartsWith("Audience")))values[Id(q)]=Stable(q)+Stable(q.GetComponent<Renderer>())+q.transform.position.ToString("F6");
        return values;
    }
    public static object Structure()
    {
        Check(!Application.isPlaying,"Saved Edit Mode required");var baseline=JObject.Parse(File.ReadAllText(Out+"Before.json"));
        Check(Hash(ArenaVenuePolishSetup.Recovery)==(string)baseline["sceneHash"],"Exact recovery copy of original current arena");
        foreach(var record in baseline["files"])Check(Hash((string)record["path"])==(string)record["hash"],"Untouched startup/UI/config: "+record["path"]);
        Check(AssetDatabase.AssetPathToGUID(OlympicArenaSetup.ScenePath)==(string)baseline["sceneGuid"],"Same production scene identity");
        Check(EditorSceneManager.playModeStartScene!=null&&EditorSceneManager.playModeStartScene.name=="Boot","Normal Boot Editor entry restored");
        EditorSceneManager.OpenScene(ArenaVenuePolishSetup.Recovery,OpenSceneMode.Single);var previous=Protected();OlympicArenaSetup.Open();var current=Protected();
        // New world-space display text is presentation only; every pre-existing component must still be exact.
        var changes=previous.Where(p=>!current.TryGetValue(p.Key,out var value)||value!=p.Value).Select(p=>new{id=p.Key,before=p.Value,after=current.TryGetValue(p.Key,out var value)?value:"MISSING"}).ToArray();
        if(changes.Length>0)Save("Protected-Differences",changes);Check(changes.Length==0,"Original script/physics/athlete/fan state unchanged (see difference report)");
        var display=One<ArenaEventDisplay>();Check(display!=null&&display.match==One<PrototypeMatch>()&&display.scores.Length==6&&display.clocks.Length==6,"Six live venue display faces wired");
        Check(display.GetComponentsInChildren<Collider>(true).Length==0,"No presentation colliders");
        Check(Object.FindObjectsByType<Light>().Length==1,"Single existing real-time light budget");
        Check(Object.FindObjectsByType<ArenaAthleteVisual>().Length==14&&One<PoolBounds>().Width==20&&One<PoolBounds>().Length==25,"Roster and field preserved");
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))Check(!t.GetComponents<Component>().Any(c=>c==null),"No missing scripts");
        foreach(var r in display.GetComponentsInChildren<Renderer>(true))Check(r.sharedMaterials.All(m=>m!=null),"No missing venue materials");
        string before=Hash(OlympicArenaSetup.ScenePath);ArenaVenuePolishSetup.SetUp();Check(Hash(OlympicArenaSetup.ScenePath)==before&&Object.FindObjectsByType<ArenaEventDisplay>().Length==1,"Repeat-safe setup without duplicates");display=One<ArenaEventDisplay>();
        return Save("Structure-PASS",new{passed=true,sceneGuidPreserved=true,recoveryExact=true,protectedComponents=previous.Count,gameplayAndPhysicsExact=true,athletesAndFansExact=true,menuBootBuildRouteUnchanged=true,newVenueRenderers=display.GetComponentsInChildren<Renderer>(true).Length,newMeshVertices=display.GetComponentsInChildren<MeshFilter>(true).Sum(f=>f.sharedMesh.vertexCount),realTimeLights=1,noNewColliders=true,repeatSafe=true});
    }
    public static async Task<object> AfterViews()
    {
        await Until(()=>One<ArenaDeveloperCamera>()!=null,10);await Frames(6);var cam=One<ArenaDeveloperCamera>();var prefs=One<GamePreferences>();int prior=(int)prefs.Current.graphics;
        try
        {
            prefs.SetGraphics(2);cam.SetView(-1);await Shot("After-Broadcast");
            for(int i=0;i<cam.viewpoints.Length;i++){cam.SetView(i);await Shot("After-View-"+i);}
            cam.SetView(-1);prefs.SetGraphics(0);await Shot("After-Low-Broadcast");
            return Save("Views-Captured",new{advancing=true,inspectionViews=5,highAndLow=true});
        }
        finally{prefs.SetGraphics(prior);cam.SetView(-1);}
    }
    public static async Task<object> GameplaySmoke()
    {
        await Until(()=>One<PrototypeMatch>()!=null,10);await Frames(5);var match=One<PrototypeMatch>();var ball=One<BallController>();var roster=One<PrototypeRoster>();var selection=One<PlayerSelection>();var camera=One<GameplayCamera>();var display=One<ArenaEventDisplay>();var prefs=One<GamePreferences>();int quality=(int)prefs.Current.graphics;
        var background=InputSystem.settings.backgroundBehavior;var editor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var mouse=InputSystem.AddDevice<Mouse>();var keyboard=InputSystem.AddDevice<Keyboard>();var ai=Object.FindObjectsByType<PrototypeSwimmerAI>();var aiEnabled=ai.Select(a=>a.enabled).ToArray();var keepers=Object.FindObjectsByType<PrototypeGoalkeeper>();var keeperEnabled=keepers.Select(k=>k.enabled).ToArray();
        try
        {
            match.RestartMatch();foreach(var a in ai)a.enabled=false;foreach(var k in keepers)k.enabled=false;
            Check(roster.Members.Count==14&&Object.FindObjectsByType<Canvas>().Length==1,"14 athletes and original single HUD Canvas");
            var shooter=roster.FieldPlayers.First(p=>p.Team==PrototypeTeam.Blue);var receiver=roster.FieldPlayers.First(p=>p.Team==PrototypeTeam.Blue&&p!=shooter);selection.Select(shooter);shooter.Motor.ResetPose(new Vector3(-1.5f,0,-3),Quaternion.identity);ball.ResetForRestart(new Vector3(8,0,8));
            Vector3 start=shooter.Motor.Position;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));await Seconds(.35f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Frames(3);Check(Vector3.Distance(start,shooter.Motor.Position)>.15f,"Actual Input System swimming");
            var pause=Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.name=="PauseButton");await Click(mouse,(RectTransform)pause.transform);Check(match.IsPaused,"Existing HUD pause works");float time=match.Clock.Remaining;await Seconds(.2f);Check(match.Clock.Remaining==time,"Authoritative pause preserved");await PressKey(keyboard,UnityEngine.InputSystem.Key.Escape);Check(!match.IsPaused,"Escape resumes");
            int blue=0,white=0;foreach(var p in roster.FieldPlayers)p.Motor.ResetPose(new Vector3(p.Team==PrototypeTeam.Blue?-8:8,0,-10+3.5f*(p.Team==PrototypeTeam.Blue?blue++:white++)),Quaternion.identity);
            shooter.Motor.ResetPose(new Vector3(-2,0,-3),Quaternion.identity);receiver.Motor.ResetPose(new Vector3(-2,0,2),Quaternion.identity);ball.ResetForRestart(shooter.Motor.Position);Check(shooter.Interaction.TryCollect()&&shooter.GetComponent<SwimmerPassing>().TryPassTo(receiver),"Pickup and pass command");await Until(()=>ball.Owner==receiver.Interaction,4);Check(selection.ActivePlayer==receiver,"Physical reception and selection");
            var team=Object.FindObjectsByType<PrototypeTeamTactics>().Single(t=>t.Team==PrototypeTeam.Blue);var goal=team.AttackingGoal;var positions=new List<object>();
            foreach(float z in new[]{-9f,0f,9f})
            {
                ball.ResetForRestart(new Vector3(0,0,z));shooter.Motor.ResetPose(new Vector3(0,0,z),Quaternion.identity);selection.Select(shooter);Check(shooter.Interaction.TryCollect(),"Camera framing possession");camera.SnapToTarget();await Frames(3);
                foreach(var target in roster.Members.Select(p=>p.Motor.Position+Vector3.up*.45f).Concat(Object.FindObjectsByType<GoalMouth>().Select(g=>g.transform.position+Vector3.up*.45f)))
                {
                    var viewport=camera.GetComponent<Camera>().WorldToViewportPoint(target);if(viewport.z>0&&viewport.x>=0&&viewport.x<=1&&viewport.y>=0&&viewport.y<=1)Check(!VenueOccludes(camera.transform.position,target,display.transform),"No venue mesh obstructs visible players/goals");
                }
                await Shot("Final-Broadcast-"+(z<0?"South":z>0?"North":"Center"));positions.Add(new{z,camera=camera.transform.position.ToString()});
            }
            shooter.Motor.ResetPose(goal.transform.position-goal.transform.forward*5,Quaternion.LookRotation(goal.transform.forward));ball.ResetForRestart(shooter.Motor.Position);Check(shooter.Interaction.TryCollect()&&shooter.Interaction.TryShoot(22,ShotType.Normal),"Physical shot");await Until(()=>match.BlueScore==1,5);display.Refresh();Check(display.scores.All(t=>t.text.Contains("1 : 0")),"All venue displays show authoritative score");await Shot("Final-Scoring");
            foreach(var referee in Object.FindObjectsByType<PoolsideReferee>())Check(Mathf.Abs(Mathf.Abs(referee.transform.position.x)-12.65f)<.05f&&Mathf.Abs(referee.transform.position.y-.18f)<.01f,"Referees retain valid deck paths");
            match.SetPaused(true);
            for(int q=0;q<3;q++){prefs.SetGraphics(q);await Frames(5);Check(One<PoolWaterVisuals>().RuntimeMaterial.shader.isSupported&&One<PoolWaterVisuals>().qualityIndex==q,"Water preset renders");await Shot("Final-Preset-"+q);}
            return Save("GameplaySmoke-PASS",new{passed=true,actualAdvancingGameView=true,athletes=14,keyboardSwim=true,pointerPauseAndEscape=true,physicalPassReception=true,physicalShotGoal=true,liveVenueScore=true,framing=positions,noVenueOcclusion=true,refereePathsValid=true,qualityPresets=3,hardwarePerformanceTested=false});
        }
        finally
        {
            InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editor;match.SetPaused(false);match.RestartMatch();prefs.SetGraphics(quality);for(int i=0;i<ai.Length;i++)ai[i].enabled=aiEnabled[i];for(int i=0;i<keepers.Length;i++)keepers[i].enabled=keeperEnabled[i];
        }
    }
    static bool VenueOccludes(Vector3 origin,Vector3 target,Transform venue)
    {
        var ray=new Ray(origin,target-origin);float distance=Vector3.Distance(origin,target);
        foreach(var filter in venue.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<TextMeshPro>()==null))
        {
            if(!filter.GetComponent<Renderer>().bounds.IntersectRay(ray,out float entry)||entry>distance)continue;
            var matrix=filter.transform.worldToLocalMatrix;Vector3 o=matrix.MultiplyPoint3x4(origin),d=matrix.MultiplyVector(ray.direction);var vertices=filter.sharedMesh.vertices;var triangles=filter.sharedMesh.triangles;
            for(int i=0;i<triangles.Length;i+=3)
            {
                var a=vertices[triangles[i]];var edge1=vertices[triangles[i+1]]-a;var edge2=vertices[triangles[i+2]]-a;var p=Vector3.Cross(d,edge2);float determinant=Vector3.Dot(edge1,p);if(Mathf.Abs(determinant)<.000001f)continue;float inverse=1/determinant;var t=o-a;float u=Vector3.Dot(t,p)*inverse;if(u<0||u>1)continue;var cross=Vector3.Cross(t,edge1);float v=Vector3.Dot(d,cross)*inverse;if(v<0||u+v>1)continue;float hit=Vector3.Dot(edge2,cross)*inverse;if(hit>.05f&&hit<distance-.15f)return true;
            }
        }
        return false;
    }
    public static async Task<object> PairedViews()
    {
        const BindingFlags flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        var assembly=typeof(UnityEditor.Editor).Assembly;var type=assembly.GetType("UnityEditor.GameView");var window=EditorWindow.GetWindow(type);var selected=type.GetProperty("selectedSizeIndex",flags);int original=(int)selected.GetValue(window);
        var st=assembly.GetType("UnityEditor.GameViewSizes");var singleton=typeof(ScriptableSingleton<>).MakeGenericType(st);var sizes=singleton.GetProperty("instance",flags).GetValue(null);var group=st.GetMethod("GetGroup",flags).Invoke(sizes,new[]{type.GetProperty("currentSizeGroupType",flags).GetValue(window)});var gt=group.GetType();int added=-1;float originalTime=Time.timeScale;
        try
        {
            var size=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),1920,1080,"Arena polish review"});gt.GetMethod("AddCustomSize").Invoke(group,new[]{size});added=(int)gt.GetMethod("GetTotalCount").Invoke(group,null)-1;selected.SetValue(window,added);await Frames(5);
            Check(Screen.width==1920&&Screen.height==1080,"Actual 1920x1080 Game view");
            foreach(bool after in new[]{false,true})
            {
                string path=after?OlympicArenaSetup.ScenePath:ArenaVenuePolishSetup.Recovery;var op=EditorSceneManager.LoadSceneAsyncInPlayMode(path,new LoadSceneParameters(LoadSceneMode.Single));await Until(()=>op.isDone,10);await Frames(5);
                var prefs=One<GamePreferences>();prefs.SetGraphics(2);Time.timeScale=0;var camera=One<ArenaDeveloperCamera>();
                // Freeze only for still composition. Advancing gameplay is verified separately; no pause UI is opened.
                camera.SetView(-1);await Shot(after?"Compare-After-Broadcast":"Compare-Before-Broadcast");camera.SetView(0);await Shot(after?"Compare-After-Overview":"Compare-Before-Overview");camera.SetView(3);await Shot(after?"Compare-After-Benches":"Compare-Before-Benches");
                if(after)
                {
                    var view=camera.viewpoints[2];var position=view.position;var rotation=view.rotation;
                    try{view.SetPositionAndRotation(new Vector3(1,5,-23),Quaternion.LookRotation(new Vector3(0,21,7)-new Vector3(1,5,-23)));camera.SetView(2);await Shot("Final-Lighting-and-Structure");}
                    finally{view.SetPositionAndRotation(position,rotation);}
                    camera.SetView(2);await Shot("Final-Waterline");camera.SetView(-1);
                }
                Time.timeScale=originalTime;
            }
            return Save("PairedViews-PASS",new{passed=true,actualResolution="1920x1080",beforeAndAfterSameViewpoints=true,venueViews=3,lightingStructureAndWaterline=true});
        }
        finally
        {
            Time.timeScale=originalTime;
            if(SceneManager.GetActiveScene().path!=OlympicArenaSetup.ScenePath){var op=EditorSceneManager.LoadSceneAsyncInPlayMode(OlympicArenaSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Single));await Until(()=>op.isDone,10);}
            selected.SetValue(window,original);if(added>=0)gt.GetMethod("RemoveCustomSize").Invoke(group,new object[]{added});window.Repaint();
        }
    }
    sealed class MemoryRepository:IProfileRepository
    {string json;public LocalProfile Load()=>json==null?null:LocalProfileCodec.FromJson(json);public void Save(LocalProfile profile)=>json=JsonUtility.ToJson(profile);}
    public static async Task<object> ExistingEntrySmoke()
    {
        await Until(()=>One<HubView>()?.Service!=null,10);var app=HubApplication.Instance;var original=app.Progression;string before=File.Exists(HubApplication.SavePath)?Hash(HubApplication.SavePath):null;var isolated=new HubProgression(original.Catalog,new MemoryRepository());app.UseVerificationProfile(isolated);var mouse=InputSystem.AddDevice<Mouse>();
        var background=InputSystem.settings.backgroundBehavior;var editor=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        try
        {
            var old=One<HubView>();app.OpenLobby();await Until(()=>One<HubView>()!=null&&One<HubView>()!=old,8);await Frames(5);
            await Click(mouse,(RectTransform)One<HubView>().home.play.transform);await Click(mouse,(RectTransform)Button("Mode_training").transform);await Click(mouse,(RectTransform)Button("LaunchMode").transform);await Until(()=>SceneManager.GetActiveScene().name=="OlympicMatchArena",8);await Frames(6);Check(One<ArenaEventDisplay>()!=null&&One<PrototypeRoster>().Members.Count==14,"Existing menu launches upgraded production arena");
            await Click(mouse,(RectTransform)Button("PauseButton").transform);Check(One<PrototypeMatch>().IsPaused,"Pause after normal hub entry");var menus=One<MatchMenuController>();menus.AskExit();menus.Confirm();await Until(()=>One<HubView>()?.Service==isolated,8);await Frames(4);Check(One<HubView>().Page=="Lobby"&&isolated.Snapshot.activeMatch==null&&Time.timeScale==1,"Existing exit/abandon returns to same home");Check((File.Exists(HubApplication.SavePath)?Hash(HubApplication.SavePath):null)==before,"User profile unchanged by fixture");
            return Save("ExistingEntry-PASS",new{passed=true,actualPointerPlayTrainingLaunch=true,sameOlympicScene=true,athletes=14,pauseExitLobby=true,noUserDataChange=true,noMenuChanges=true});
        }
        finally{InputSystem.RemoveDevice(mouse);InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editor;app.UseVerificationProfile(original);app.OpenLobby();}
    }
    static UnityEngine.UI.Button Button(string name)=>Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.name==name);
    static Vector2 Point(RectTransform rect)=>RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
    static async Task Mouse(Mouse mouse,Vector2 pos,bool down){InputSystem.QueueStateEvent(mouse,new MouseState{position=pos,buttons=(ushort)(down?1:0)});await Seconds(.07f);}
    static async Task Click(Mouse mouse,RectTransform rect){var point=Point(rect);await Mouse(mouse,point+Vector2.right*8,false);await Mouse(mouse,point,false);await Mouse(mouse,point,true);await Mouse(mouse,point,false);}
    static async Task PressKey(Keyboard keyboard,UnityEngine.InputSystem.Key key){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));await Seconds(.07f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Seconds(.07f);}
    static async Task Seconds(float seconds){var watch=Stopwatch.StartNew();while(watch.Elapsed.TotalSeconds<seconds)await Task.Delay(10);}
    static async Task Shot(string name){await Frames(4);ScreenCapture.CaptureScreenshot(Out+name+".png");await Frames(4);}
    static async Task Frames(int n){int target=Time.frameCount+n;await Until(()=>Time.frameCount>=target,10);}
    static async Task Until(Func<bool> condition,float timeout){var watch=Stopwatch.StartNew();while(!condition()){if(watch.Elapsed.TotalSeconds>timeout)throw new TimeoutException("Arena polish verification timed out");await Task.Delay(10);}}
}
