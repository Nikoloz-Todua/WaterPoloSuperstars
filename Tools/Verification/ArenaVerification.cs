using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Editor;
using ToduStudios.WaterPolo.Hub;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class ArenaVerification
{
    static void Check(bool value,string message){if(!value)throw new Exception("Arena: "+message);}
    static T One<T>() where T:Object=>Object.FindAnyObjectByType<T>();
    static object Save(string name,object result){Directory.CreateDirectory("Logs/Arena");File.WriteAllText("Logs/Arena/"+name+".json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;}
    static string Hash(string path){using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));}
    public static object Setup()
    {
        Check(!Application.isPlaying,"Edit Mode required");OlympicArenaSetup.Open();
        var roster=One<PrototypeRoster>();Check(roster.Members.Count==14 && roster.Members.Select(p=>p.PlayerId).Distinct().Count()==14,"Distinct original roster");
        Check(One<PoolBounds>().Width==20 && One<PoolBounds>().Length==25,"Competition field dimensions");
        Check(Object.FindObjectsByType<PoolWaterVisuals>().Length==1 && Object.FindObjectsByType<PoolWaterTestSwimmer>().Length==0,"Single surface, no demonstration athletes");
        Check(One<ArenaQualityController>().GetComponentsInChildren<Collider>(true).Length==0,"Arena scenery has no gameplay colliders");
        Check(Object.FindObjectsByType<ArenaAthleteVisual>().Length==14,"Fourteen replaceable full-body visual rigs");
        foreach(var player in roster.Members)Check(player.Interaction.CarryAnchor!=null && player.GetComponent<Rigidbody>()!=null,"Original ball anchors/physics retained");
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))Check(!t.GetComponents<Component>().Any(c=>c==null),"No missing scripts");
        Check(Object.FindObjectsByType<Canvas>().Length==1 && Object.FindObjectsByType<EventSystem>().Length==1,"One Canvas and EventSystem");
        foreach(var g in Object.FindObjectsByType<RoundedPanelGraphic>(FindObjectsInactive.Include))Check(g.GetComponent<CanvasRenderer>()!=null,"UI CanvasRenderer");
        foreach(string name in new[]{"PrototypePool","WaterVisualTest"})Check(Hash("Assets/Scenes/"+name+".unity")==Hash("Temp/ArenaRecovery/"+name+".unity"),"Original "+name+" bytes retained");
        Check(Hash(OlympicArenaSetup.RecoveryPath)==Hash("Assets/Scenes/PrototypePool.unity"),"Independent exact gameplay recovery copy");
        string guid=AssetDatabase.AssetPathToGUID(OlympicArenaSetup.ScenePath),before=Hash(OlympicArenaSetup.ScenePath);OlympicArenaSetup.SetUp();
        Check(before==Hash(OlympicArenaSetup.ScenePath) && guid==AssetDatabase.AssetPathToGUID(OlympicArenaSetup.ScenePath),"Repeated setup preserves scene bytes and identity");
        return Save("Setup-PASS",new{passed=true,athletes=14,fieldsPerTeam=6,keepersPerTeam=1,field="25 x 20 m",singleWater=true,originalScenesUnchanged=true,recoveryExact=true,repeatSafe=true,renderers=Object.FindObjectsByType<Renderer>().Length,vertices=Object.FindObjectsByType<MeshFilter>().Sum(f=>(long)f.sharedMesh.vertexCount)});
    }

    public static async Task<object> Integration()
    {
        Check(Application.isPlaying && SceneManager.GetActiveScene().name=="OlympicMatchArena","Direct arena Play required");
        Directory.CreateDirectory("Logs/Arena");
        var match=One<PrototypeMatch>();var roster=One<PrototypeRoster>();var ball=One<BallController>();var pool=One<PoolBounds>();var selection=One<PlayerSelection>();
        var camera=One<GameplayCamera>();var developer=One<ArenaDeveloperCamera>();var prefs=One<GamePreferences>();var oldPrefs=prefs.Current;
        var ai=Object.FindObjectsByType<PrototypeSwimmerAI>();var keepers=Object.FindObjectsByType<PrototypeGoalkeeper>();var aiEnabled=ai.Select(a=>a.enabled).ToArray();var keeperEnabled=keepers.Select(k=>k.enabled).ToArray();
        var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();int first=Time.frameCount;
        try
        {
            match.RestartMatch();await Seconds(.5f);
            Check(Time.frameCount>first+5 && Time.fixedTime>0,"Advancing player loop and physics");
            Check(roster.Members.Count==14 && Object.FindObjectsByType<BallController>().Length==1 && Object.FindObjectsByType<PrototypeMatch>().Length==1,"One ball/phase authority, fourteen athletes");
            Check(roster.Members.All(p=>Mathf.Abs(p.Motor.Position.x)<pool.Width/2 && Mathf.Abs(p.Motor.Position.z)<pool.Length/2),"AI remains inside new field");
            foreach(var r in Object.FindObjectsByType<PoolsideReferee>())Check(Mathf.Abs(r.transform.position.x)>11.8f && Mathf.Abs(r.transform.position.x)<14.4f && Mathf.Abs(r.transform.position.y-.18f)<.001f,"Referee on unobstructed deck path");
            for(int i=0;i<ai.Length;i++)ai[i].enabled=false;foreach(var k in keepers)k.enabled=false;
            match.RestartMatch();await Frames(4);
            for(int q=0;q<3;q++)
            {
                prefs.SetGraphics(q);await Frames(6);await Shot("Quality-"+q);
                Check(One<PoolWaterVisuals>().qualityIndex==q,"Existing settings select water budget");
                Check(One<PoolWaterVisuals>().RuntimeMaterial.shader.isSupported && !ShaderUtil.GetShaderMessages(One<PoolWaterVisuals>().RuntimeMaterial.shader).Any(m=>m.severity.ToString()=="Error"),"Rendered shader variant compiles");
            }
            var player=selection.ActivePlayer;player.Motor.ResetPose(new Vector3(-1.5f,0,-3.4f),Quaternion.identity);
            Vector3 start=player.Motor.Position;InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.LeftShift));await Seconds(.45f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Frames(4);
            Check(Vector3.Distance(start,player.Motor.Position)>.2f && player.GetComponent<SwimmerStamina>().Value<1,"Real keyboard swim/sprint and stamina");
            var previous=selection.ActivePlayer;await PressKey(keyboard,Key.C);Check(selection.ActivePlayer!=previous,"Real C player switching");
            var joystick=One<VirtualSwimJoystick>();var jr=(RectTransform)joystick.transform;Vector2 jp=Point(jr)+Vector2.right*(jr.rect.width*.22f*One<Canvas>().scaleFactor);
            start=selection.ActivePlayer.Motor.Position;await Mouse(mouse,jp,true);await Seconds(.35f);await Mouse(mouse,jp,false);
            Check(Vector3.Distance(start,selection.ActivePlayer.Motor.Position)>.1f && joystick.Value==Vector2.zero,"Real pointer joystick moves and releases");
            var pause=Object.FindObjectsByType<Button>().Single(b=>b.name=="PauseButton");await Click(mouse,(RectTransform)pause.transform);Check(match.IsPaused,"Pointer Pause requests authoritative suspension");
            float remaining=match.Clock.Remaining;await Seconds(.12f);Check(match.Clock.Remaining==remaining,"Paused clock stationary");await Shot("Pause");
            await PressKey(keyboard,Key.Escape);Check(!match.IsPaused,"Keyboard resumes match");

            var shooter=roster.FieldPlayers.First(p=>p.Team==PrototypeTeam.Blue);var receiver=roster.FieldPlayers.First(p=>p.Team==PrototypeTeam.Blue && p!=shooter);
            Arrange(roster,shooter,receiver);ball.ResetForRestart(shooter.Motor.Position);Check(shooter.Interaction.TryCollect(),"Range checked pickup");
            Check(shooter.GetComponent<SwimmerPassing>().TryPassTo(receiver),"Existing pass command");await Until(()=>ball.Owner==receiver.Interaction,4);Check(selection.ActivePlayer==receiver,"Physical pass reception and auto selection");
            Arrange(roster,shooter,receiver);ball.ResetForRestart(shooter.Motor.Position);Check(shooter.Interaction.TryCollect(),"Recollect for lob");
            Check(shooter.GetComponent<LobPassing>().TryPass(),"Existing lob command");await Seconds(.2f);Check(ball.IsAirborne,"Lob has actual height");await Shot("Lob");
            ball.ResetForRestart(Vector3.zero);
            var team=Object.FindObjectsByType<PrototypeTeamTactics>().Single(t=>t.Team==PrototypeTeam.Blue);var goal=team.AttackingGoal;
            shooter.Motor.ResetPose(goal.transform.position-goal.transform.forward*5,Quaternion.LookRotation(goal.transform.forward));ball.ResetForRestart(shooter.Motor.Position);Check(shooter.Interaction.TryCollect(),"Collect for shot");selection.Select(shooter);
            camera.SnapToTarget();await Frames(5);Vector3 gp=camera.GetComponent<Camera>().WorldToViewportPoint(goal.transform.position+Vector3.up*.45f);Check(gp.z>0 && gp.x>.05f && gp.x<.95f && gp.y>.05f && gp.y<.95f,"Attacking goal in broadcast view");await Shot("Goal-End-North");
            var shoot=Object.FindObjectsByType<GameplayActionButton>().Single(b=>b.Action==MobileMatchAction.Shoot);await Mouse(mouse,Point((RectTransform)shoot.transform),true);await Seconds(.55f);Check(shooter.GetComponent<ShotCharge>().IsCharging,"Pointer holds shot charge");await Mouse(mouse,Point((RectTransform)shoot.transform),false);
            await Until(()=>match.BlueScore==1,4);Check(match.Phase==MatchPhase.GoalRestart,"Physical shot crosses real aligned goal and scores once");await Shot("Goal");
            await Until(()=>match.Phase==MatchPhase.CentreRestartReady,5);Check(match.CentreTaker.Team==PrototypeTeam.White,"Conceding team restart");await Until(()=>match.IsPlaying,3);

            match.RestartMatch();Arrange(roster,shooter,receiver);ball.ResetForRestart(shooter.Motor.Position);Check(shooter.Interaction.TryCollect(),"Possession for contact check");
            var offender=roster.FieldPlayers.First(p=>p.Team==PrototypeTeam.White);offender.Motor.ResetPose(receiver.Motor.Position+Vector3.right*.8f,Quaternion.identity);
            Check(match.Rules.ReportContact(offender,receiver,IllegalContact.Hold),"Authoritative off-ball exclusion");Check(!offender.EligibleForPlay && offender.PersonalFouls==1 && !offender.GetComponent<Collider>().enabled,"Foul history and exclusion preserved");
            Check(offender.Motor.Position.x>8 && offender.Motor.Position.x<10 && Mathf.Abs(offender.Motor.Position.z)>12.5f && Mathf.Abs(offender.Motor.Position.z)<13.5f,"Excluded swimmer in marked water re-entry area");await Shot("Exclusion");
            float stopped=match.Clock.Remaining;await Seconds(.1f);Check(match.Clock.Remaining==stopped,"Rule setup is stopped time");

            match.RestartMatch();for(int i=0;i<4;i++){ball.ResetForRestart(Vector3.zero);match.ShotClock.ResetPossession();match.AdvanceLiveTime(match.Clock.RealDuration);if(i<3)match.ContinueMatch();}
            Check(match.Phase==MatchPhase.FullTime && match.EndSwitchCount==1,"Four periods, one halftime end reversal");
            match.RestartMatch();shooter.Motor.ResetPose(team.DefendedGoal.transform.position-team.DefendedGoal.transform.forward*5,Quaternion.LookRotation(-team.DefendedGoal.transform.forward));selection.Select(shooter);camera.SnapToTarget();await Frames(5);await Shot("Goal-End-South");
            foreach(var p in roster.Members)p.Motor.ResetPose(p.StartPosition.position,p.StartPosition.rotation);selection.Select(shooter);camera.SnapToTarget();
            for(int i=0;i<developer.viewpoints.Length;i++){developer.SetView(i);await Shot("Inspection-"+i);}
            developer.SetView(-1);await Shot("Broadcast");
            var result=new{passed=true,firstFrame=first,lastFrame=Time.frameCount,athletes=14,aiBounds=true,refereeDeck=true,qualityPresets=3,keyboardSwimSprintSwitch=true,pointerJoystickPauseShoot=true,physicalPassReception=true,lobAirborne=true,physicalGoal=true,concedingRestart=true,exclusionAndWaitingArea=true,periods=4,halftimeReversals=1,actualGameView=true,devicePerformanceTested=false};
            return Save("Integration-PASS",result);
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);developer.SetView(-1);
            match.SetPaused(false);match.RestartMatch();for(int i=0;i<ai.Length;i++)ai[i].enabled=aiEnabled[i];for(int i=0;i<keepers.Length;i++)keepers[i].enabled=keeperEnabled[i];
            prefs.SetGraphics((int)oldPrefs.graphics);prefs.Save();
        }
    }

    static void Arrange(PrototypeRoster roster,SwimmerIdentity shooter,SwimmerIdentity receiver)
    {
        int b=0,w=0;foreach(var p in roster.FieldPlayers)p.Motor.ResetPose(new Vector3(p.Team==PrototypeTeam.Blue?-8:8,0,-10+3.5f*(p.Team==PrototypeTeam.Blue?b++:w++)),Quaternion.identity);
        shooter.Motor.ResetPose(new Vector3(-1.5f,0,-3.4f),Quaternion.identity);receiver.Motor.ResetPose(new Vector3(-1.5f,0,3),Quaternion.identity);
    }

    sealed class MemoryRepository:IProfileRepository
    {public string json;public LocalProfile Load()=>json==null?null:JsonUtility.FromJson<LocalProfile>(json);public void Save(LocalProfile p)=>json=JsonUtility.ToJson(p);}
    public static async Task<object> HubFlow()
    {
        Check(Application.isPlaying && One<HubView>()!=null,"Boot must have reached lobby");
        var app=HubApplication.Instance;var original=app.Progression;string saveHash=File.Exists(HubApplication.SavePath)?Hash(HubApplication.SavePath):null;
        var repository=new MemoryRepository();var isolated=new HubProgression(original.Catalog,repository);app.UseVerificationProfile(isolated);
        var mouse=InputSystem.AddDevice<Mouse>();
        try
        {
            var prior=One<HubView>();app.OpenLobby();await Until(()=>One<HubView>()!=null && One<HubView>()!=prior,5);await Frames(5);await Shot("Lobby");
            await Click(mouse,(RectTransform)Object.FindObjectsByType<Button>().Single(b=>b.name=="Play").transform);Check(One<HubView>().Page=="Play","Pointer PLAY opens mode selection");await Shot("Quick-Match");
            await Click(mouse,(RectTransform)Object.FindObjectsByType<Button>().Single(b=>b.name=="Launch0").transform);
            await Until(()=>SceneManager.GetActiveScene().name=="OlympicMatchArena",8);await Seconds(.3f);
            var match=One<PrototypeMatch>();var roster=One<PrototypeRoster>();var ball=One<BallController>();
            Check(roster.Members.Count==14 && roster.Members.Where(p=>p.Team==PrototypeTeam.Blue).All(p=>!string.IsNullOrEmpty(p.CollectionDefinitionId)),"Original roster plus persistent lineup mapping");await Shot("Launched-From-Lobby");
            foreach(var ai in Object.FindObjectsByType<PrototypeSwimmerAI>())ai.enabled=false;foreach(var keeper in Object.FindObjectsByType<PrototypeGoalkeeper>())keeper.enabled=false;
            var shooter=roster.FieldPlayers.First(p=>p.Team==PrototypeTeam.Blue);var receiver=roster.FieldPlayers.First(p=>p.Team==PrototypeTeam.Blue && p!=shooter);Arrange(roster,shooter,receiver);
            var goal=Object.FindObjectsByType<PrototypeTeamTactics>().Single(t=>t.Team==PrototypeTeam.Blue).AttackingGoal;
            shooter.Motor.ResetPose(goal.transform.position-goal.transform.forward*5,Quaternion.LookRotation(goal.transform.forward));ball.ResetForRestart(shooter.Motor.Position);Check(shooter.Interaction.TryCollect() && shooter.Interaction.TryShoot(22,ShotType.Normal),"Real shot in hub match");await Until(()=>match.BlueScore==1,5);await Until(()=>match.IsPlaying,6);
            for(int i=0;i<4;i++){ball.ResetForRestart(Vector3.zero);match.ShotClock.ResetPossession();match.AdvanceLiveTime(match.Clock.RealDuration);if(i<3)match.ContinueMatch();}
            Check(match.Phase==MatchPhase.FullTime && match.EndSwitchCount==1,"Four-period match completes");
            var snapshot=isolated.Snapshot;Check(snapshot.pendingResult!=null && snapshot.played==1 && snapshot.wins==1,"Authoritative bridge persists winning reward once");int balance=snapshot.coins;
            Check(!isolated.CompleteMatch(snapshot.pendingResult.id,1,0,out _),"Duplicate result refused");await Shot("Full-Time");
            await Click(mouse,(RectTransform)Object.FindObjectsByType<Button>().Single(b=>b.name=="Continue" && b.transform.parent.name=="TransitionPanel").transform);
            await Until(()=>One<HubView>()!=null,6);await Frames(5);Check(One<HubView>().Page=="Result" && isolated.Snapshot.coins==balance,"Reward screen after real full-time pointer");await Shot("Rewards");
            Check(new HubProgression(original.Catalog,repository).Snapshot.coins==balance,"Reward profile reload");
            await Click(mouse,(RectTransform)Object.FindObjectsByType<Button>().Single(b=>b.name=="Continue").transform);Check(One<HubView>().Page=="Lobby","Pointer Continue returns to lobby");await Shot("Returned-Lobby");
            Check(isolated.Snapshot.pendingResult==null && new HubProgression(original.Catalog,repository).Snapshot.pendingResult==null,"Dismissed optional result stays absent after copy and reload");
            // Training completion and abandonment preserve reward distinction through the same scene.
            app.Launch("training");await Until(()=>SceneManager.GetActiveScene().name=="OlympicMatchArena",6);await Frames(6);
            var menu=One<MatchMenuController>();menu.OpenPause();menu.AskExit();menu.Confirm();await Until(()=>One<HubView>()!=null,6);await Frames(4);
            Check(isolated.Snapshot.coins==balance && isolated.Snapshot.activeMatch==null && Time.timeScale==1,"Abandoned training returns with no grant or stuck pause");
            Check(saveHash==null?!File.Exists(HubApplication.SavePath):Hash(HubApplication.SavePath)==saveHash,"Production profile bytes untouched");
            return Save("HubFlow-PASS",new{passed=true,bootReachedLobby=true,realPointerPlayAndQuickMatch=true,productionScene="OlympicMatchArena",athletes=14,lineupMapping=true,physicalGoal=true,periods=4,receiptOnce=true,rewardsReload=true,realPointerRewardsAndLobby=true,trainingAbandonNoReward=true,productionProfileUnchanged=true});
        }
        finally {InputSystem.RemoveDevice(mouse);app.UseVerificationProfile(original);app.ReturnToLobby(false);}
    }
    public static async Task<object> CaptureFinal()
    {
        var developer=One<ArenaDeveloperCamera>();Check(developer!=null,"Arena Play required");
        for(int i=0;i<developer.viewpoints.Length;i++){developer.SetView(i);await Shot("Final-Inspection-"+i);}developer.SetView(-1);await Shot("Final-Broadcast");
        return new{captured=true,directory="Logs/Arena"};
    }
    public static async Task<object> Appearance()
    {
        Check(Application.isPlaying && One<ArenaDeveloperCamera>()!=null,"Direct arena Play required");
        var match=One<PrototypeMatch>();var roster=One<PrototypeRoster>();var ball=One<BallController>();var selection=One<PlayerSelection>();var camera=One<GameplayCamera>();var developer=One<ArenaDeveloperCamera>();
        var ai=Object.FindObjectsByType<PrototypeSwimmerAI>();var keepers=Object.FindObjectsByType<PrototypeGoalkeeper>();var aiState=ai.Select(x=>x.enabled).ToArray();var keeperState=keepers.Select(x=>x.enabled).ToArray();
        var prefs=One<GamePreferences>();int quality=(int)prefs.Current.graphics;float timeScale=Time.timeScale;
        var visuals=Object.FindObjectsByType<ArenaAthleteVisual>();var visualState=visuals.Select(v=>v.enabled).ToArray();
        try
        {
            foreach(var a in ai)a.enabled=false;foreach(var k in keepers)k.enabled=false;prefs.SetGraphics(2);match.RestartMatch();
            var shooter=roster.FieldPlayers.First(p=>p.Team==PrototypeTeam.Blue);var team=Object.FindObjectsByType<PrototypeTeamTactics>().Single(t=>t.Team==PrototypeTeam.Blue);
            var framing=new List<object>();
            for(int half=0;half<2;half++)
            {
                if(half==1){for(int i=0;i<2;i++){ball.ResetForRestart(Vector3.zero);match.ShotClock.ResetPossession();match.AdvanceLiveTime(match.Clock.RealDuration);match.ContinueMatch();}}
                var goal=team.AttackingGoal;shooter.Motor.ResetPose(goal.transform.position-goal.transform.forward*5,Quaternion.LookRotation(goal.transform.forward));
                ball.ResetForRestart(shooter.Motor.Position);Check(shooter.Interaction.TryCollect(),"Possession at each actual attacking end");selection.Select(shooter);camera.SnapToTarget();await Seconds(.2f);
                var viewport=camera.GetComponent<Camera>().WorldToViewportPoint(goal.transform.position+Vector3.up*.45f);
                Check(viewport.z>0 && viewport.x>.1f && viewport.x<.9f && viewport.y>.18f && viewport.y<.82f,"Goal has HUD-safe framing after actual end reversal");
                await Shot(half==0?"Verified-North-Attack":"Verified-South-Attack");framing.Add(new{half,goalZ=goal.transform.position.z,viewport=viewport.ToString("F3")});
            }
            match.RestartMatch();shooter.Motor.ResetPose(new Vector3(-1.5f,0,-3.4f),Quaternion.identity);selection.Select(shooter);
            foreach(var v in visuals)v.enabled=false;developer.SetView(1);Time.timeScale=0;await Frames(5);
            var parts=shooter.GetComponentInChildren<ArenaAthleteVisual>().GetComponentsInChildren<Renderer>().Where(r=>r.bounds.max.y<-.025f).ToArray();Check(parts.Length>=4,"Real athlete has fully submerged opaque anatomy");
            var casts=parts.Select(r=>r.shadowCastingMode).ToArray();Color32[] baseline,hidden;
            try
            {
                foreach(var r in parts)r.shadowCastingMode=ShadowCastingMode.Off;
                await Shot("Real-Athlete-Underwater");baseline=Pixels("Real-Athlete-Underwater");
                foreach(var r in parts)r.enabled=false;await Shot("Real-Athlete-Underwater-Hidden");hidden=Pixels("Real-Athlete-Underwater-Hidden");
            }
            finally{for(int i=0;i<parts.Length;i++){parts[i].enabled=true;parts[i].shadowCastingMode=casts[i];}}
            int changed=0;for(int i=0;i<baseline.Length;i++)if(Math.Abs(baseline[i].r-hidden[i].r)+Math.Abs(baseline[i].g-hidden[i].g)+Math.Abs(baseline[i].b-hidden[i].b)>18)changed++;
            Check(changed>200,"Actual submerged athlete contributes pixels through production water with shadows excluded");
            Time.timeScale=timeScale;developer.SetView(-1);match.RestartMatch();
            for(int i=0;i<visuals.Length;i++)visuals[i].enabled=visualState[i];
            await Shot("Final-Broadcast");developer.SetView(0);await Shot("Final-Arena-Overview");developer.SetView(2);await Shot("Final-Reflections");developer.SetView(3);await Shot("Final-Team-Benches");developer.SetView(-1);
            return Save("Appearance-PASS",new{passed=true,framing,whollySubmergedMeshes=parts.Length,submergedVisiblePixels=changed,shadowsExcluded=true,actualGameView=true});
        }
        finally
        {
            Time.timeScale=timeScale;developer.SetView(-1);match.RestartMatch();
            for(int i=0;i<ai.Length;i++)ai[i].enabled=aiState[i];for(int i=0;i<keepers.Length;i++)keepers[i].enabled=keeperState[i];for(int i=0;i<visuals.Length;i++)visuals[i].enabled=visualState[i];prefs.SetGraphics(quality);prefs.Save();
        }
    }
    static Color32[] Pixels(string name)
    {var t=new Texture2D(2,2,TextureFormat.RGB24,false);try{t.LoadImage(File.ReadAllBytes("Logs/Arena/"+name+".png"));return t.GetPixels32();}finally{Object.Destroy(t);}}
    public static async Task<object> ScreenSizes()
    {
        Check(Application.isPlaying && One<ArenaDeveloperCamera>()!=null,"Direct arena required");
        const BindingFlags flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        var assembly=typeof(UnityEditor.Editor).Assembly;var type=assembly.GetType("UnityEditor.GameView");var view=EditorWindow.GetWindow(type);var selected=type.GetProperty("selectedSizeIndex",flags);int original=(int)selected.GetValue(view);
        var sizesType=assembly.GetType("UnityEditor.GameViewSizes");var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);var sizes=singleton.GetProperty("instance",flags).GetValue(null);var group=sizesType.GetMethod("GetGroup",flags).Invoke(sizes,new[]{type.GetProperty("currentSizeGroupType",flags).GetValue(view)});var gt=group.GetType();var added=new List<int>();
        var safes=Object.FindObjectsByType<SafeAreaLayout>();var safe=safes.Single(s=>s.name=="MatchSafeArea");var safeEnabled=safes.Select(s=>s.enabled).ToArray();
        var match=One<PrototypeMatch>();var menus=One<MatchMenuController>();var developer=One<ArenaDeveloperCamera>();var keyboard=InputSystem.AddDevice<Keyboard>();var mouse=InputSystem.AddDevice<Mouse>();var prefs=One<GamePreferences>();int originalQuality=(int)prefs.Current.graphics;
        var results=new List<object>();
        try
        {
            match.RestartMatch();
            foreach(int width in new[]{1920,2340,2400})
            {
                var size=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),width,1080,"Arena verification"});gt.GetMethod("AddCustomSize").Invoke(group,new[]{size});int index=(int)gt.GetMethod("GetTotalCount").Invoke(group,null)-1;added.Add(index);selected.SetValue(view,index);view.Repaint();await Seconds(.2f);
                Check(Screen.width==width && Screen.height==1080,"Actual Game view resolution");var rect=new Rect(70,24,width-140,1032);foreach(var area in safes){area.enabled=false;area.Apply(rect,new Vector2(width,1080));}Canvas.ForceUpdateCanvases();await Frames(4);
                var joystick=(RectTransform)One<VirtualSwimJoystick>().transform;var pause=(RectTransform)Object.FindObjectsByType<Button>().Single(b=>b.name=="PauseButton").transform;
                foreach(var rt in new[]{joystick,pause}.Concat(Object.FindObjectsByType<GameplayActionButton>().Select(b=>(RectTransform)b.transform)))
                {
                    var corners=new Vector3[4];rt.GetWorldCorners(corners);foreach(var c in corners)Check(c.x>=rect.xMin-1 && c.x<=rect.xMax+1 && c.y>=rect.yMin-1 && c.y<=rect.yMax+1,"Controls inside simulated safe area: "+rt.name);
                }
                await Shot("Screen-"+width+"-Broadcast");await Click(mouse,pause);Check(match.IsPaused,"Pointer pause at "+width);await Shot("Screen-"+width+"-Pause");
                await Click(mouse,(RectTransform)Object.FindObjectsByType<Button>().Single(b=>b.name=="Settings" && b.transform.parent.name=="PausePanel").transform);Check(menus.Page==MatchMenuPage.Settings,"Settings pointer");await Shot("Screen-"+width+"-Settings");
                if(width==2340)
                {
                    var quality=One<ArenaQualityController>();int q=0;
                    foreach(string name in new[]{"Low","Medium","High"})
                    {
                        await Click(mouse,(RectTransform)Object.FindObjectsByType<Button>().Single(b=>b.name==name && b.transform.parent.name=="SettingsPanel").transform);
                        Check((int)prefs.Current.graphics==q && quality.water.qualityIndex==q && QualitySettings.renderPipeline==quality.pipelines[q],"Pointer graphics selects complete arena preset");
                        Check(quality.mediumAudience.activeSelf==(q>=1) && quality.highAudience.activeSelf==(q==2) && quality.water.reflectionManager.enabled==(q==2),"Crowd/reflection budget follows preset");q++;
                    }
                    prefs.SetGraphics(originalQuality);
                }
                menus.Back();menus.Resume();results.Add(new{width,height=1080,simulatedSafeInsets=true,pointerPauseSettings=true});
            }
            await PressKey(keyboard,Key.F8);Check(developer.ViewIndex==0,"F8 selects developer inspection");developer.SetView(-1);
            return Save("ScreenSizes-PASS",new{passed=true,screens=results,developerCameraKeyboard=true,phoneHardwareTested=false});
        }
        finally
        {
            developer.SetView(-1);prefs.SetGraphics(originalQuality);menus.Resume();InputSystem.RemoveDevice(keyboard);InputSystem.RemoveDevice(mouse);selected.SetValue(view,original);
            for(int i=added.Count-1;i>=0;i--)gt.GetMethod("RemoveCustomSize").Invoke(group,new object[]{added[i]});await Seconds(.15f);for(int i=0;i<safes.Length;i++){safes[i].Apply(Screen.safeArea,new Vector2(Screen.width,Screen.height));safes[i].enabled=safeEnabled[i];}view.Repaint();
        }
    }
    public static async Task<object> WaterImpact()
    {
        var match=One<PrototypeMatch>();Check(Application.isPlaying && match!=null,"Direct arena required");var ball=One<BallController>();var roster=One<PrototypeRoster>();var prefs=One<GamePreferences>();int quality=(int)prefs.Current.graphics;
        var ai=Object.FindObjectsByType<PrototypeSwimmerAI>();var enabled=ai.Select(a=>a.enabled).ToArray();bool contact=false;
        Action<BallImpactEvent> observe=e=>{if(e.Kind==BallImpactKind.Water)contact=true;};ball.Contact+=observe;
        try
        {
            foreach(var a in ai)a.enabled=false;prefs.SetGraphics(2);match.RestartMatch();
            var player=roster.FieldPlayers.First(p=>p.Team==PrototypeTeam.Blue);var receiver=roster.FieldPlayers.First(p=>p.Team==PrototypeTeam.Blue && p!=player);Arrange(roster,player,receiver);player.Motor.ResetPose(new Vector3(0,0,-2),Quaternion.Euler(0,90,0));ball.ResetForRestart(player.Motor.Position);
            Check(player.Interaction.TryCollect() && player.GetComponent<LobPassing>().TryPass(),"Physical lob for water contact");await Until(()=>contact,4);await Frames(3);
            var effects=One<WaterInteractionEffects>();Check(effects.ActiveEffects>0 && effects.GetComponentsInChildren<Transform>().Any(t=>t.name=="Splash" && t.gameObject.activeInHierarchy),"Bounded splash on authoritative water contact");await Shot("Ball-Water-Splash");
            return Save("WaterImpact-PASS",new{passed=true,physicalWaterContact=true,boundedSplash=true,poolCapacity=effects.PoolCapacity});
        }
        finally{ball.Contact-=observe;match.RestartMatch();for(int i=0;i<ai.Length;i++)ai[i].enabled=enabled[i];prefs.SetGraphics(quality);prefs.Save();}
    }
    public static async Task<object> Keepers()
    {
        var match=One<PrototypeMatch>();Check(Application.isPlaying && match!=null,"Direct arena required");var ball=One<BallController>();var roster=One<PrototypeRoster>();var keepers=Object.FindObjectsByType<PrototypeGoalkeeper>();
        var ai=Object.FindObjectsByType<PrototypeSwimmerAI>();var enabled=ai.Select(a=>a.enabled).ToArray();var tuning=keepers.Select(k=>new{keeper=k,save=Float(k,"saveChance"),catchChance=Float(k,"catchChance"),reaction=Float(k,"reactionDelay")}).ToArray();var results=new List<object>();
        try
        {
            foreach(var a in ai)a.enabled=false;
            foreach(var k in keepers){SetFloat(k,"saveChance",1);SetFloat(k,"catchChance",1);SetFloat(k,"reactionDelay",0);}
            foreach(var keeper in keepers)
            {
                match.RestartMatch();int count=keeper.Catches;var shooter=roster.FieldPlayers.First(p=>p.Team!=keeper.Identity.Team);int b=0,w=0;
                foreach(var player in roster.FieldPlayers)player.Motor.ResetPose(new Vector3(player.Team==PrototypeTeam.Blue?-8:8,0,-10+3.5f*(player.Team==PrototypeTeam.Blue?b++:w++)),Quaternion.identity);
                shooter.Motor.ResetPose(keeper.DefendedGoal.transform.position-keeper.DefendedGoal.transform.forward*9,keeper.DefendedGoal.transform.rotation);ball.ResetForRestart(shooter.Motor.Position+keeper.DefendedGoal.transform.forward*1.65f);Check(shooter.Interaction.TryCollect(),"Keeper fixture legitimate possession");await Seconds(.12f);
                Check(shooter.Interaction.TryShoot(80,ShotType.Normal),"Controlled fast physical shot");await Until(()=>keeper.Catches>count,4);
                Check(ball.Owner==keeper.Identity.Interaction && roster.Members.Count(p=>p.Interaction.HasBall)==1 && match.BlueScore+match.WhiteScore==0,"One authoritative keeper catch, no score");
                if(keeper.Identity.Team==PrototypeTeam.Blue)Check(One<PlayerSelection>().ActivePlayer==keeper.Identity,"Human keeper possession retained");
                results.Add(new{team=keeper.Identity.Team.ToString(),goalZ=keeper.DefendedGoal.transform.position.z,physicalSweptCatch=true});
            }
            await Shot("Goalkeeper-Catch");return Save("Keepers-PASS",new{passed=true,keepers=results,controlledSaveProbability=1,shotSpeed=80,productionTuningRestored=true});
        }
        finally{foreach(var t in tuning){SetFloat(t.keeper,"saveChance",t.save);SetFloat(t.keeper,"catchChance",t.catchChance);SetFloat(t.keeper,"reactionDelay",t.reaction);}match.RestartMatch();for(int i=0;i<ai.Length;i++)ai[i].enabled=enabled[i];}
    }
    static float Float(Object target,string field)=>new SerializedObject(target).FindProperty(field).floatValue;
    static void SetFloat(Object target,string field,float value){var s=new SerializedObject(target);s.FindProperty(field).floatValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
    static Vector2 Point(RectTransform rt)=>RectTransformUtility.WorldToScreenPoint(null,rt.TransformPoint(rt.rect.center));
    static async Task Mouse(Mouse mouse,Vector2 point,bool down){InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=(ushort)(down?1:0)});await Seconds(.08f);}
    static async Task Click(Mouse mouse,RectTransform rt){var p=Point(rt);await Mouse(mouse,p,false);await Mouse(mouse,p,true);await Mouse(mouse,p,false);await Seconds(.1f);}
    static async Task PressKey(Keyboard keys,Key key){InputSystem.QueueStateEvent(keys,new KeyboardState(key));await Seconds(.06f);InputSystem.QueueStateEvent(keys,new KeyboardState());await Seconds(.06f);}
    static async Task Shot(string name){await Seconds(.16f);await Frames(5);ScreenCapture.CaptureScreenshot("Logs/Arena/"+name+".png");await Frames(5);}
    static async Task Frames(int n){int f=Time.frameCount+n;await Until(()=>Time.frameCount>=f,8);}
    static async Task Seconds(float seconds){var watch=Stopwatch.StartNew();while(watch.Elapsed.TotalSeconds<seconds)await Task.Delay(10);}
    static async Task Until(Func<bool> condition,float timeout){var watch=Stopwatch.StartNew();while(!condition()){if(watch.Elapsed.TotalSeconds>timeout)throw new TimeoutException("Arena expected transition timed out");await Task.Delay(10);}}
}
