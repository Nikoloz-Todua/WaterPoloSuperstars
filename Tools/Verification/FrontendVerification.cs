using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TMPro;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Hub;
using ToduStudios.WaterPolo.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class FrontendVerification
{
    static T One<T>() where T:Object=>Object.FindAnyObjectByType<T>();
    static void Check(bool value,string message){if(!value)throw new Exception("Frontend: "+message);}
    static object Save(string name,object data){Directory.CreateDirectory("Logs/Frontend");File.WriteAllText("Logs/Frontend/"+name+".json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));return data;}
    static string Hash(string path){if(!File.Exists(path))return null;using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));}
    sealed class MemoryRepository:IProfileRepository
    {public string json;public bool fail;public LocalProfile Load()=>json==null?null:LocalProfileCodec.FromJson(json);public void Save(LocalProfile p){if(fail)throw new IOException("Fixture write failure");json=JsonUtility.ToJson(p);}}
    sealed class FixtureFocus:IDisposable
    {
        readonly InputSettings.BackgroundBehavior background=InputSystem.settings.backgroundBehavior;
        readonly InputSettings.EditorInputBehaviorInPlayMode editor=InputSystem.settings.editorInputBehaviorInPlayMode;
        public FixtureFocus(){InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;}
        public void Dispose(){InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editor;}
    }
    public static object Setup()
    {
        Check(!Application.isPlaying,"Saved Edit Mode required");UnifiedFrontendSetup.SetUp();var view=One<HubView>();var shell=Object.FindAnyObjectByType<FrontendShell>(FindObjectsInactive.Include);
        Check(view.shell==shell&&shell!=null&&shell.art!=null,"One wired unified shell");
        Check(Object.FindObjectsByType<Canvas>().Length==1&&Object.FindObjectsByType<EventSystem>().Length==1&&Object.FindObjectsByType<HubView>().Length==1,"Single Canvas/EventSystem/navigation authority");
        Check(view.transform.Find("Navigation")==null&&view.transform.Find("ProfileBar")==null,"Legacy sidebar/header visuals retired");
        Check(UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene!=null&&UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene.name=="Boot","Editor startup remains Boot");
        Check(HubApplication.Match=="OlympicMatchArena"&&EditorBuildSettings.scenes.Any(s=>s.enabled&&s.path=="Assets/Scenes/OlympicMatchArena.unity"),"Production arena registered and selected");
        Check(typeof(FrontendArt).GetFields().Where(f=>f.FieldType==typeof(Sprite)).All(f=>f.GetValue(shell.art)!=null),"Eight assigned new sprites");
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))Check(!t.GetComponents<Component>().Any(c=>c==null),"No missing scripts");
        foreach(var graphic in Object.FindObjectsByType<RoundedPanelGraphic>(FindObjectsInactive.Include))Check(graphic.GetComponent<CanvasRenderer>()!=null,"Panel CanvasRenderer");
        var baseline=JArray.Parse(File.ReadAllText("Logs/Frontend/Preservation-before.json"));
        foreach(var item in baseline)
        {
            string path=(string)item["path"];
            if(path==MainMenuSetup.ScenePath){Check(Hash(UnifiedFrontendSetup.RecoveryPath)==(string)item["hash"],"Exact pre-unified recovery");Check(AssetDatabase.AssetPathToGUID(path)==(string)item["guid"],"Same MainLobby GUID");continue;}
            Check(Hash(path)==(string)item["hash"],"Untouched source/scene/config bytes: "+path);Check(AssetDatabase.AssetPathToGUID(path)==(string)item["guid"],"Original asset GUID: "+path);
        }
        foreach(string path in UnifiedFrontendSetup.ArtFiles.Values){var imp=(TextureImporter)AssetImporter.GetAtPath(path);Check(imp.textureType==TextureImporterType.Sprite&&!imp.mipmapEnabled&&!imp.isReadable&&imp.npotScale==TextureImporterNPOTScale.None,"Sprite import settings");}
        string before=Hash(MainMenuSetup.ScenePath);UnifiedFrontendSetup.SetUp();Check(Hash(MainMenuSetup.ScenePath)==before,"Repeat-safe setup preserves tuning/scene bytes");
        return Save("Setup-PASS",new{passed=true,newSprites=8,missing=0,recoveryExact=true,homeSpritesAndSourcesUntouched=true,arenaBootCatalogBuildUnchanged=true,oneCanvasEventSystemController=true,legacyVisualsRetired=true,repeatSafe=true});
    }
    public static object Progression()
    {
        var catalog=Resources.Load<HubCatalog>("HubCatalog");var divisions=Resources.Load<DivisionCatalog>("DivisionCatalog");var repo=new MemoryRepository();var service=new HubProgression(catalog,repo,()=>1800000000);
        Check(service.DivisionUnlocked("division_1")&&!service.DivisionUnlocked("premium_league")&&!service.DivisionUnlocked("continental_cup")&&!service.DivisionUnlocked("world_champions_league"),"Initial unlock order");
        string before=repo.json;Check(!service.BeginDivisionMatch("premium_league",out _)&&repo.json==before,"Card clicks cannot unlock locked divisions");
        Check(service.BeginDivisionMatch("division_1",out _),"Start official run");string run=service.Snapshot.championship.id;var session=service.Snapshot.activeMatch;
        Check(!service.CompleteMatch("wrong-session",5,0,out _),"Reject unbound result");
        Check(service.BeginDivisionMatch("division_1",out _)&&service.Snapshot.championship.id==run,"Match restart preserves current attempt");
        Check(service.CompleteMatch(service.Snapshot.activeMatch.id,1,0,out _),"First official victory");
        Check(!service.DivisionUnlocked("premium_league")&&service.Snapshot.championship.wins==1,"One win insufficient");
        service=new HubProgression(catalog,repo);Check(service.Snapshot.championship.wins==1,"In-progress championship reload");
        int coins=service.Snapshot.coins;string first=service.Snapshot.pendingResult.id;
        Check(!service.CompleteMatch(first,1,0,out _)&&service.Snapshot.coins==coins,"Receipt replay refused without grants");
        service.BeginMatch("training",out _);service.CompleteMatch(service.Snapshot.activeMatch.id,9,0,out _);
        Check(service.Snapshot.coins==coins&&service.Snapshot.championship.wins==1&&service.Snapshot.championship.matches==1,"Training never advances or grants");
        service.BeginDivisionMatch("division_1",out _);service.LeaveMatch(out _);Check(service.Snapshot.championship.matches==1,"Abandon grants no championship result");
        service.BeginDivisionMatch("division_1",out _);service.CompleteMatch(service.Snapshot.activeMatch.id,2,0,out _);
        Check(service.DivisionUnlocked("premium_league")&&service.Snapshot.completedDivisions.SequenceEqual(new[]{"division_1"}),"Validated championship unlocks next exactly once");
        foreach(string id in new[]{"premium_league","continental_cup","world_champions_league"})
        {
            for(int i=0;i<divisions.winsRequired;i++){Check(service.BeginDivisionMatch(id,out _),"Launch unlocked "+id);Check(service.CompleteMatch(service.Snapshot.activeMatch.id,1,0,out _),"Complete "+id);}
            service=new HubProgression(catalog,repo);
        }
        Check(service.Snapshot.completedDivisions.SequenceEqual(divisions.divisions.Select(d=>d.id)),"All four stable IDs persist in required order");
        var lossRepo=new MemoryRepository();var losing=new HubProgression(catalog,lossRepo);
        for(int i=0;i<divisions.matchLimit;i++){losing.BeginDivisionMatch("division_1",out _);losing.CompleteMatch(losing.Snapshot.activeMatch.id,0,0,out _);if(divisions.Ended(losing.Snapshot.championship))break;}
        Check(!losing.DivisionUnlocked("premium_league"),"Draws/failed attempts do not unlock");
        var failed=losing.Snapshot.championship;losing.BeginDivisionMatch("division_1",out _);Check(losing.Snapshot.championship.id!=failed.id&&losing.Snapshot.championship.matches==0,"Failed championship can restart");
        var errorRepo=new MemoryRepository();var atomic=new HubProgression(catalog,errorRepo);before=errorRepo.json;errorRepo.fail=true;Check(!atomic.BeginDivisionMatch("division_1",out _)&&errorRepo.json==before&&atomic.Snapshot.activeMatch==null,"Write failure does not publish progress");
        // Disk migration and recovery use their own unique directory, never production saves.
        string directory="Logs/Frontend/ProfileTests/"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(directory);string path=Path.GetFullPath(directory+"/profile-v1.json");
        var legacy=JObject.Parse(repo.json);legacy["schemaVersion"]=1;legacy.Remove("completedDivisions");legacy.Remove("championship");legacy["activeMatch"]=new JObject{{"id","legacy-session"},{"mode","quick"},{"lineup",legacy["lineup"].DeepClone()}};legacy["pendingResult"]=new JObject{{"id","legacy-reward"},{"result","VICTORY"},{"pack","Tide pack received"},{"blue",1},{"white",0},{"coins",120},{"xp",40},{"trophies",12}};File.WriteAllText(path,legacy.ToString());string oldHash=Hash(path);
        var disk=new HubProgression(catalog,new JsonProfileRepository(path));var p=disk.Snapshot;
        Check(p.schemaVersion==2&&p.completedDivisions.Count==0&&p.coins==(int)legacy["coins"]&&p.packs.Count==legacy["packs"].Count()&&p.lineup.SequenceEqual(legacy["lineup"].Values<string>())&&Hash(path)==oldHash,"Schema-1 migration retains inventory/economy and does not rewrite on load");
        Check(p.activeMatch?.id=="legacy-session"&&p.pendingResult?.id=="legacy-reward"&&p.pendingResult.coins==120,"Migration preserves existing quick session and unacknowledged reward");
        disk.BeginDivisionMatch("division_1",out _);Check(Hash(path+".bak")==oldHash,"First schema-2 atomic write retains original backup");
        var reloaded=new HubProgression(catalog,new JsonProfileRepository(path));Check(reloaded.Snapshot.activeMatch.mode=="division","Schema-2 disk reload");
        var future=JObject.Parse(File.ReadAllText(path));future["schemaVersion"]=999;File.WriteAllText(path,future.ToString());string futureHash=Hash(path);bool refused=false;
        try{new HubProgression(catalog,new JsonProfileRepository(path));}catch(NotSupportedException){refused=true;}
        Check(refused&&Hash(path)==futureHash,"Future schema refused without backup downgrade or overwrite");
        return Save("Progression-PASS",new{passed=true,format=new{divisions.winsRequired,divisions.matchLimit},fourDivisionOrder=true,validatedResultsOnly=true,receiptReplayRefused=true,trainingAndAbandonNoUnlock=true,failedRunRetry=true,reloadAndDiskPersistence=true,v1MigrationPreservesData=true,originalBackupRetained=true,futureSchemaRefused=true,saveFailureAtomic=true});
    }
    public static async Task<object> Capture()
    {
        await Until(()=>One<HubView>()?.Service!=null,10);var v=One<HubView>();v.Show("Play");await Shot("First-GameModes");v.Show("Season Pass");await Shot("First-BattlePass");v.Show("Tactics");await Shot("First-Team");v.Show("Lobby");return new{captured=true};
    }
    public static async Task<object> FinalDetails()
    {
        using var focus=new FixtureFocus();await Until(()=>One<HubView>()?.Service!=null,10);await Frames(4);var v=One<HubView>();v.Show("Lobby");var mouse=InputSystem.AddDevice<Mouse>();
        try
        {
            await Click(mouse,v.home.play);await Click(mouse,Button("Mode_training"));Check(v.Page=="Mode Details"&&v.shell.title.text=="MODE DETAILS"&&v.content.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("no trophies")),"Training is presented as practice, not an official championship");await Shot("Final-Training-Details");await Click(mouse,v.shell.close);Check(v.Page=="Play","Final details nested Back");await Click(mouse,Button("Mode_division_1"));Check(v.shell.title.text=="MODE DETAILS","Shared details heading");await Shot("Final-Division-Details");await Click(mouse,v.shell.close);await Click(mouse,v.shell.close);Check(v.Page=="Lobby","Final mode Back");await Shot("Final-Home");
            return Save("FinalDetails-PASS",new{passed=true,trainingLabelCorrect=true,divisionDetails=true,nestedPointerBack=true});
        }
        finally{InputSystem.RemoveDevice(mouse);v.Show("Lobby");}
    }
    public static async Task<object> BackProbe()
    {
        using var focus=new FixtureFocus();
        var v=One<HubView>();v.Show("Lobby");v.Show("Season Pass");await Frames(5);var mouse=InputSystem.AddDevice<Mouse>();int count=0;UnityEngine.Events.UnityAction call=()=>count++;v.shell.close.onClick.AddListener(call);
        var point=Point((RectTransform)v.shell.close.transform);var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
        try{await Mouse(mouse,point,false);await Mouse(mouse,point,true);var module=EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();var down=new{mouse=mouse.position.ReadValue().ToString(),button=mouse.leftButton.isPressed,point=module.point.action.ReadValue<Vector2>().ToString(),click=module.leftClick.action.ReadValue<float>(),devices=string.Join(",",module.leftClick.action.controls.Select(c=>c.path)),enabled=module.enabled};await Mouse(mouse,point,false);return new{point=point.ToString(),hits=hits.Select(h=>h.gameObject.name).ToArray(),count,page=v.Page,active=v.shell.close.IsActive(),interactable=v.shell.close.IsInteractable(),down};}
        finally{v.shell.close.onClick.RemoveListener(call);InputSystem.RemoveDevice(mouse);v.Show("Lobby");}
    }
    public static async Task<object> Interactions()
    {
        using var focus=new FixtureFocus();
        Check(Application.isPlaying&&One<HubView>()!=null,"Advancing lobby Play required");var app=HubApplication.Instance;var original=app.Progression;string productionHash=Hash(HubApplication.SavePath);var repo=new MemoryRepository();long now=1800000000;
        var seed=new HubProgression(original.Catalog,repo,()=>now);var p=seed.Snapshot;p.displayName="Wave Captain";p.coins=12345;p.gems=73;p.seasonXp=120;p.wins=1;p.goals=1;p.packs.Clear();
        p.packs.Add(new RewardPack{instanceId="waiting",duration=45});p.packs.Add(new RewardPack{instanceId="unlocking",rarity="Rare",duration=45,startUtc=now-10,outcome=1});p.packs.Add(new RewardPack{instanceId="ready",rarity="Epic",duration=45,startUtc=now-50,outcome=2});repo.Save(p);
        var isolated=new HubProgression(original.Catalog,repo,()=>now);app.UseVerificationProfile(isolated);var mouse=InputSystem.AddDevice<Mouse>();var keys=InputSystem.AddDevice<Keyboard>();
        try
        {
            var old=One<HubView>();app.OpenLobby();await Until(()=>One<HubView>()!=null&&One<HubView>()!=old&&One<HubView>().Service==isolated,8);await Frames(4);var v=One<HubView>();var h=v.home;
            Check(h.displayName.text==p.displayName&&h.coins.text==p.coins.ToString("N0")&&h.diamonds.text==p.gems.ToString("N0"),"Live profile/currencies");
            Check(h.slots.Select(s=>s.State).SequenceEqual(new[]{MenuPackState.Waiting,MenuPackState.Unlocking,MenuPackState.Ready,MenuPackState.Empty}),"Real pack state presentation");
            await Click(mouse,h.play);Check(v.Page=="Play","Home PLAY route");Check(v.content.GetComponentsInChildren<UnityEngine.UI.Button>().Length==6,"Exactly six interactive mode cards");
            foreach(string id in new[]{"division_1","premium_league","continental_cup","world_champions_league","lucky_shot","training"})Check(Button("Mode_"+id).transform.Find("Artwork").GetComponent<UnityEngine.UI.Image>().sprite==v.shell.art.Mode(id),"Correct mode image "+id);
            await Click(mouse,Button("Mode_premium_league"));Check(v.modal.gameObject.activeSelf&&v.modal.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("Division 1 championship")),"Locked card explains predecessor");await Click(mouse,Button("CloseModal"));Check(v.Page=="Play"&&!v.modal.gameObject.activeSelf,"Modal X returns to modes");
            var divisionButton=Button("Mode_division_1");var target=Point((RectTransform)divisionButton.transform);await Mouse(mouse,target,true);await Seconds(.10f);Check(divisionButton.GetComponent<MenuButtonFeedback>().artwork.localScale.x<.99f,"Mode artwork has real pointer press feedback");await Mouse(mouse,target,false);Check(v.Page=="Mode Details","Division detail route");await Press(keys,Key.Escape);Check(v.Page=="Play","Escape detail returns to modes");await Click(mouse,Button("Mode_training"));Check(v.Page=="Mode Details","Training detail route");await Click(mouse,v.shell.close);Check(v.Page=="Play","Detail X returns to modes");
            await Click(mouse,Button("Mode_lucky_shot"));Check(v.modal.gameObject.activeSelf,"Lucky Shot honestly unavailable");await Press(keys,Key.Escape);Check(v.Page=="Play"&&!v.modal.gameObject.activeSelf,"Escape closes modal first");await Click(mouse,v.shell.close);Check(v.Page=="Lobby","Mode X returns home");
            foreach(var route in new[]{new{b=h.shop,page="Shop"},new{b=h.team,page="Tactics"},new{b=h.ranking,page="Trophies"},new{b=h.missions,page="Missions"},new{b=h.battlePass,page="Season Pass"},new{b=h.settings,page="Settings"},new{b=h.profile,page="Profile"}})
            {await Click(mouse,route.b);Check(v.Page==route.page&&v.shell.gameObject.activeInHierarchy,"Functional route "+route.page);await Click(mouse,v.shell.close);Check(v.Page=="Lobby","X returns home "+route.page);}
            await Click(mouse,h.team);await Click(mouse,Button("TeamCollection"));Check(v.Page=="Collection","Collection accessible without legacy sidebar");await Click(mouse,Button("Details_wp_001"));Check(v.Page=="Details","Player details route");await Click(mouse,v.shell.close);Check(v.Page=="Collection","Details back to collection");await Click(mouse,Button("TeamLineup"));await Click(mouse,Button("Slot0"));Check(Button("Assign_wp_009")!=null,"Goalkeeper roster role filter");await Click(mouse,Button("Assign_wp_009"));Check(isolated.Snapshot.lineup[0]=="wp_009","Existing lineup assignment persists");await Click(mouse,Button("Press"));Check(isolated.Snapshot.tactics=="Press","Existing live tactics API");await Click(mouse,v.shell.close);Check(v.Page=="Lobby","Team tabs do not trap history");
            await Click(mouse,h.shop);await Click(mouse,Button("Tab1"));int shards=isolated.Snapshot.collection.Single(x=>x.playerId=="wp_002").shards;await Click(mouse,Button("Buy_reef-card"));Check(v.modal.gameObject.activeSelf,"Existing purchase confirmation");await Click(mouse,Button("Confirm"));Check(isolated.Snapshot.collection.Single(x=>x.playerId=="wp_002").shards==shards+15,"Shop transaction grants actual contents");await Click(mouse,v.shell.close);
            await Click(mouse,h.battlePass);Check(v.shell.background.sprite==v.shell.art.battlePassBackground,"Purple background only on Battle Pass");int coins=isolated.Snapshot.coins;await Click(mouse,Button("ClaimTier1"));Check(isolated.Snapshot.coins==coins+isolated.Catalog.SeasonReward(1).coins&&!Button("ClaimTier1").interactable,"Season free reward claim once");await Click(mouse,v.shell.close);
            await Click(mouse,h.missions);await Click(mouse,Button("Claim_first-win"));Check(isolated.Snapshot.claimedRewards.Contains("achievement:first-win")&&!Button("Claim_first-win").interactable,"Mission claim once");await Click(mouse,v.shell.close);
            await Click(mouse,h.profile);v.content.GetComponentInChildren<TMP_InputField>().text="Captain Current";await Click(mouse,Button("SaveProfile"));await Click(mouse,v.shell.close);Check(h.displayName.text=="Captain Current","Profile saved through existing API");
            foreach(var b in new[]{h.friends,h.clubs,h.worldTournament,h.messages}){await Click(mouse,b);Check(v.modal.gameObject.activeSelf,"Unavailable feature responds");await Click(mouse,h.play);Check(v.Page=="Lobby","Modal blocks underlying controls");await Click(mouse,Button("CloseModal"));Check(!v.modal.gameObject.activeSelf,"Modal closes without orphan blocker");}
            await Click(mouse,h.slots[0].button);await Click(mouse,Button("Confirm"));Check(isolated.Snapshot.packs.First().startUtc==now,"Pack unlock uses existing authority");string timer=h.slots[1].status.text;now+=2;await Seconds(1.1f);Check(timer!=h.slots[1].status.text,"Real timer updates");
            await Click(mouse,h.slots[2].button);await Click(mouse,Button("Confirm"));Check(!isolated.Snapshot.packs.Any(x=>x.instanceId=="ready")&&!isolated.ClaimPack("ready",out _),"Ready pack removed and claim-once refused");await Press(keys,Key.Escape);
            Check(new HubProgression(original.Catalog,repo,()=>now).Snapshot.packs.Count==2,"Pack inventory persists across reload");Check(Hash(HubApplication.SavePath)==productionHash,"Production profile bytes untouched by fixture");
            return Save("Interactions-PASS",new{passed=true,actualMouseAndEscape=true,allHomeRoutes=true,sixModeSprites=true,lockedReasons=true,nestedBack=true,modalBlocking=true,teamCollectionLineupTactics=true,shopTransaction=true,seasonClaimOnce=true,missionClaimOnce=true,profileEdit=true,packPersistenceAndTimer=true,productionSaveUnchanged=true});
        }
        finally{InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keys);app.UseVerificationProfile(original);app.ReturnToLobby(false);}
    }
    public static async Task<object> UpgradeAndScroll()
    {
        using var focus=new FixtureFocus();var app=HubApplication.Instance;var original=app.Progression;string productionHash=Hash(HubApplication.SavePath);var repo=new MemoryRepository();var isolated=new HubProgression(original.Catalog,repo);app.UseVerificationProfile(isolated);var mouse=InputSystem.AddDevice<Mouse>();
        try
        {
            var old=One<HubView>();app.OpenLobby();await Until(()=>One<HubView>()!=null&&One<HubView>()!=old,8);await Frames(4);var v=One<HubView>();await Click(mouse,v.home.team);await Click(mouse,Button("TeamCollection"));await Click(mouse,Button("Details_wp_001"));
            int level=isolated.Snapshot.collection.Single(x=>x.playerId=="wp_001").level;await Click(mouse,Button("Upgrade"));Check(v.modal.gameObject.activeSelf,"Upgrade confirmation reachable");await Click(mouse,Button("Confirm"));Check(isolated.Snapshot.collection.Single(x=>x.playerId=="wp_001").level==level+1,"Existing upgrade action still persists");await Click(mouse,v.shell.close);Check(v.Page=="Collection","Upgrade details returns to collection");
            var scroll=v.content.GetComponentInChildren<UnityEngine.UI.ScrollRect>();var from=Point(scroll.viewport);await Mouse(mouse,from,false);await Mouse(mouse,from,true);for(int i=1;i<=8;i++)await Mouse(mouse,from+Vector2.up*(i*35),true);await Mouse(mouse,from+Vector2.up*280,false);Check(scroll.content.anchoredPosition.y>50,"Collection responds to vertical swipe");await Click(mouse,Button("Details_wp_012"));Check(v.Page=="Details"&&!Button("Upgrade").interactable,"Last collection card is reachable and unowned upgrade is disabled");await Click(mouse,v.shell.close);await Click(mouse,v.shell.close);Check(v.Page=="Lobby","Scrolled collection retains back route");
            Check(Hash(HubApplication.SavePath)==productionHash,"Production save untouched");return Save("UpgradeAndScroll-PASS",new{passed=true,actualPointer=true,upgradeTransaction=true,verticalCollectionSwipe=true,lastCardReachable=true,unownedUpgradeDisabled=true,backHistory=true,productionSaveUnchanged=true});
        }
        finally{InputSystem.RemoveDevice(mouse);app.UseVerificationProfile(original);app.ReturnToLobby(false);}
    }
    public static async Task<object> Layouts()
    {
        using var focus=new FixtureFocus();
        Check(Application.isPlaying&&One<HubView>()!=null,"Lobby Play required");var v=One<HubView>();v.Show("Lobby");
        const BindingFlags flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        var assembly=typeof(UnityEditor.Editor).Assembly;var type=assembly.GetType("UnityEditor.GameView");var window=EditorWindow.GetWindow(type);var selected=type.GetProperty("selectedSizeIndex",flags);int original=(int)selected.GetValue(window);
        var sizesType=assembly.GetType("UnityEditor.GameViewSizes");var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);var sizes=singleton.GetProperty("instance",flags).GetValue(null);var group=sizesType.GetMethod("GetGroup",flags).Invoke(sizes,new[]{type.GetProperty("currentSizeGroupType",flags).GetValue(window)});var gt=group.GetType();var added=new List<int>();var results=new List<object>();
        var safe=v.GetComponent<SafeAreaLayout>();bool enabled=safe.enabled;var mouse=InputSystem.AddDevice<Mouse>();var touch=InputSystem.AddDevice<Touchscreen>();
        try
        {
            foreach(int width in new[]{1920,2340,2400})
            {
                var size=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),width,1080,"Frontend verification"});gt.GetMethod("AddCustomSize").Invoke(group,new[]{size});int index=(int)gt.GetMethod("GetTotalCount").Invoke(group,null)-1;added.Add(index);selected.SetValue(window,index);window.Repaint();await Seconds(.25f);
                Check(Screen.width==width&&Screen.height==1080,"Actual Game view resolution");safe.enabled=false;var bounds=new Rect(70,24,width-140,1032);safe.Apply(bounds,new Vector2(width,1080));Canvas.ForceUpdateCanvases();await Frames(4);
                v.Show("Lobby");await Shot("Home-"+width);
                await Click(mouse,v.home.play);Check(v.Page=="Play","Pointer PLAY at "+width);await Frames(4);
                var buttons=v.content.GetComponentsInChildren<UnityEngine.UI.Button>();Check(buttons.Length==6,"Six cards");var regions=new List<Rect>();
                foreach(var b in buttons.Concat(new[]{v.shell.close}))
                {var r=Bounds((RectTransform)b.transform);Check(bounds.Contains(r.min)&&bounds.Contains(r.max),"Safe bounds: "+b.name);Check(r.width>=70&&r.height>=70,"Generous touch target: "+b.name);regions.Add(r);}
                for(int a=0;a<regions.Count;a++)for(int b=a+1;b<regions.Count;b++)Check(!regions[a].Overlaps(regions[b]),"Cards/header do not overlap");
                foreach(var img in v.content.GetComponentsInChildren<UnityEngine.UI.Image>().Where(i=>i.sprite!=null))Check(img.preserveAspect,"Card proportions preserved");
                Check(!v.content.GetComponentsInChildren<TMP_Text>().Any(t=>{t.ForceMeshUpdate();return t.isTextOverflowing;}),"Mode state/hint text fits");await Shot("GameModes-"+width);
                await Click(mouse,Button("Mode_division_1"));await Shot("ModeDetails-"+width);await Click(mouse,v.shell.close);Check(v.Page=="Play","Detail return at "+width);
                if(width==2340)
                {
                    var point=Point((RectTransform)v.shell.close.transform);InputSystem.QueueStateEvent(touch,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Began,position=point});await Seconds(.09f);InputSystem.QueueStateEvent(touch,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=point});await Seconds(.09f);Check(v.Page=="Lobby","Input System touch closes modes");
                }
                else await Click(mouse,v.shell.close);
                Check(v.Page=="Lobby","Mode X returns home before secondary checks: "+v.Page);
                foreach(string page in new[]{"Season Pass","Shop","Tactics","Collection","Trophies","Missions","Profile","Settings"})
                {
                    v.Show(page);await Frames(4);var r=Bounds((RectTransform)v.shell.close.transform);Check(bounds.Contains(r.min)&&bounds.Contains(r.max),"Every page has safe X");
                    foreach(var t in v.shell.GetComponentsInChildren<TMP_Text>()){t.ForceMeshUpdate();Check(!t.isTextOverflowing,"Header fits: "+t.name);}
                    await Shot(page.Replace(" ","")+"-"+width);await Click(mouse,v.shell.close);Check(v.Page=="Lobby","Page "+page+" X returns home at "+width+" (actual "+v.Page+")");
                }
                v.Show("Season Pass");var scroll=v.content.GetComponentInChildren<UnityEngine.UI.ScrollRect>();float start=scroll.content.anchoredPosition.x;var from=Point(scroll.viewport)+Vector2.right*160;await Mouse(mouse,from,true);for(int i=1;i<=8;i++)await Mouse(mouse,from-Vector2.right*(i*55),true);await Mouse(mouse,from-Vector2.right*440,false);Check(scroll.content.anchoredPosition.x<start-50,"Season track responds to pointer swipe");await Shot("BattlePass-Scrolled-"+width);await Click(mouse,v.shell.close);
                v.ShowComingSoon("World Tournament");await Shot("ComingSoon-"+width);await Click(mouse,Button("CloseModal"));Check(!v.modal.gameObject.activeSelf,"Modal X reachable at "+width);
                results.Add(new{width,height=1080,simulatedSafeInsets=true,sixCardsNoOverlap=true,aspectPreserved=true,backPointer=true,seasonSwipe=true});
            }
            return Save("Layouts-PASS",new{passed=true,screens=results,actualGameView=true,simulatedTouch=true,phoneHardwareTested=false});
        }
        finally
        {
            v.Show("Lobby");InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(touch);selected.SetValue(window,original);for(int i=added.Count-1;i>=0;i--)gt.GetMethod("RemoveCustomSize").Invoke(group,new object[]{added[i]});await Seconds(.15f);safe.Apply(Screen.safeArea,new Vector2(Screen.width,Screen.height));safe.enabled=enabled;window.Repaint();
        }
    }
    public static async Task<object> MatchFlow()
    {
        using var focus=new FixtureFocus();
        Check(Application.isPlaying&&One<HubView>()!=null,"Boot/lobby Play required");var app=HubApplication.Instance;var original=app.Progression;string productionHash=Hash(HubApplication.SavePath);var repo=new MemoryRepository();var isolated=new HubProgression(original.Catalog,repo);app.UseVerificationProfile(isolated);var mouse=InputSystem.AddDevice<Mouse>();
        try
        {
            var old=One<HubView>();app.OpenLobby();await Until(()=>One<HubView>()!=null&&One<HubView>()!=old,8);await Frames(5);
            for(int win=0;win<isolated.Divisions.winsRequired;win++)
            {
                var v=One<HubView>();await Click(mouse,v.home.play);await Click(mouse,Button("Mode_division_1"));await Click(mouse,Button("LaunchMode"));await Until(()=>SceneManager.GetActiveScene().name=="OlympicMatchArena",8);await Frames(6);
                var match=One<PrototypeMatch>();var roster=One<PrototypeRoster>();var ball=One<BallController>();Check(roster.Members.Count==14&&roster.Members.Where(p=>p.Team==PrototypeTeam.Blue).All(p=>!string.IsNullOrEmpty(p.CollectionDefinitionId)),"14 athletes and persistent lineup binding");
                string run=isolated.Snapshot.championship.id;match.RestartMatch();Check(isolated.Snapshot.activeMatch.mode=="division"&&isolated.Snapshot.championship.id==run&&isolated.Snapshot.activeMatch.round==win+1,"Arena restart retains official championship context");
                foreach(var ai in Object.FindObjectsByType<PrototypeSwimmerAI>())ai.enabled=false;foreach(var keeper in Object.FindObjectsByType<PrototypeGoalkeeper>())keeper.enabled=false;
                int blue=0,white=0;foreach(var player in roster.FieldPlayers)player.Motor.ResetPose(new Vector3(player.Team==PrototypeTeam.Blue?-8:8,0,-10+3.5f*(player.Team==PrototypeTeam.Blue?blue++:white++)),Quaternion.identity);
                var shooter=roster.FieldPlayers.First(p=>p.Team==PrototypeTeam.Blue);var goal=Object.FindObjectsByType<PrototypeTeamTactics>().Single(t=>t.Team==PrototypeTeam.Blue).AttackingGoal;
                shooter.Motor.ResetPose(goal.transform.position-goal.transform.forward*5,Quaternion.LookRotation(goal.transform.forward));ball.ResetForRestart(shooter.Motor.Position);Check(shooter.Interaction.TryCollect()&&shooter.Interaction.TryShoot(22,ShotType.Normal),"Physical scoring shot");await Until(()=>match.BlueScore==1,5);await Until(()=>match.IsPlaying,6);
                if(win==0)await Shot("Official-Arena");
                for(int period=0;period<4;period++){ball.ResetForRestart(Vector3.zero);match.ShotClock.ResetPossession();match.AdvanceLiveTime(match.Clock.RealDuration);if(period<3)match.ContinueMatch();}
                Check(match.Phase==MatchPhase.FullTime&&match.EndSwitchCount==1,"Four periods with one halftime reversal");var state=isolated.Snapshot;Check(state.pendingResult!=null&&state.pendingResult.divisionId=="division_1"&&state.championship.wins==win+1,"Official bridge validated and saved real result");
                int coins=state.coins;Check(!isolated.CompleteMatch(state.pendingResult.id,1,0,out _)&&isolated.Snapshot.coins==coins,"Result cannot replay rewards");
                await Click(mouse,Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.name=="Continue"&&b.transform.parent.name=="TransitionPanel"));await Until(()=>One<HubView>()!=null,8);await Frames(5);Check(One<HubView>().Page=="Result","Full-time pointer opens unified result");await Shot("Official-Result-"+(win+1));
                await Click(mouse,Button("Continue"));Check(One<HubView>().Page=="Lobby"&&isolated.Snapshot.pendingResult==null,"Reward Continue returns asset home");
            }
            Check(isolated.DivisionUnlocked("premium_league")&&new HubProgression(original.Catalog,repo).DivisionUnlocked("premium_league"),"Two real arena wins unlock Premium and survive reload");
            await Click(mouse,One<HubView>().home.play);Check(Button("Mode_premium_league").transform.Find("Locked")==null,"Unlocked card displays without lock");await Shot("Premium-Unlocked");await Click(mouse,Button("Mode_training"));int balance=isolated.Snapshot.coins,attempts=isolated.Snapshot.championship.matches;await Click(mouse,Button("LaunchMode"));await Until(()=>SceneManager.GetActiveScene().name=="OlympicMatchArena",8);await Frames(6);
            var training=One<PrototypeMatch>();var practiceBall=One<BallController>();foreach(var ai in Object.FindObjectsByType<PrototypeSwimmerAI>())ai.enabled=false;
            for(int period=0;period<4;period++){practiceBall.ResetForRestart(Vector3.zero);training.ShotClock.ResetPossession();training.AdvanceLiveTime(training.Clock.RealDuration);if(period<3)training.ContinueMatch();}
            Check(isolated.Snapshot.coins==balance&&isolated.Snapshot.championship.matches==attempts&&isolated.Snapshot.pendingResult.xp==0,"Real training completion has no rewards or league changes");
            await Click(mouse,Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.name=="Continue"&&b.transform.parent.name=="TransitionPanel"));await Until(()=>One<HubView>()!=null,8);await Frames(4);await Click(mouse,Button("Continue"));Check(One<HubView>().Page=="Lobby"&&Time.timeScale==1,"Training returns to home normally");
            Check(Hash(HubApplication.SavePath)==productionHash,"Production profile untouched by match fixture");
            return Save("MatchFlow-PASS",new{passed=true,realPointerModeDetailsLaunch=true,productionArena="OlympicMatchArena",athletes=14,physicalGoals=2,completedOfficialMatches=2,acceleratedPeriods=true,oneHalftimeReversalPerMatch=true,arenaRestartContext=true,receiptOnce=true,premiumUnlockPersistent=true,trainingCompletionNoRewards=true,unifiedResultAndLobbyReturn=true,productionProfileUnchanged=true});
        }
        finally{InputSystem.RemoveDevice(mouse);app.UseVerificationProfile(original);app.ReturnToLobby(false);}
    }
    static Rect Bounds(RectTransform r){var c=new Vector3[4];r.GetWorldCorners(c);return Rect.MinMaxRect(c.Min(x=>x.x),c.Min(x=>x.y),c.Max(x=>x.x),c.Max(x=>x.y));}
    static UnityEngine.UI.Button Button(string name)=>Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.name==name);
    static Vector2 Point(RectTransform rect)=>RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
    static async Task Mouse(Mouse mouse,Vector2 point,bool down){InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=(ushort)(down?1:0)});await Seconds(.07f);}
    static async Task Click(Mouse mouse,UnityEngine.UI.Button button){var p=Point((RectTransform)button.transform);await Mouse(mouse,p+Vector2.right*8,false);await Mouse(mouse,p,false);await Mouse(mouse,p,true);await Mouse(mouse,p,false);await Seconds(.08f);}
    static async Task Press(Keyboard keys,Key key){InputSystem.QueueStateEvent(keys,new KeyboardState(key));await Seconds(.07f);InputSystem.QueueStateEvent(keys,new KeyboardState());await Seconds(.07f);}
    static async Task Shot(string name){await Seconds(.2f);await Frames(5);ScreenCapture.CaptureScreenshot("Logs/Frontend/"+name+".png");await Frames(5);}
    static async Task Seconds(float seconds){var w=Stopwatch.StartNew();while(w.Elapsed.TotalSeconds<seconds)await Task.Delay(10);}
    static async Task Frames(int n){int f=Time.frameCount+n;await Until(()=>Time.frameCount>=f,10);}
    static async Task Until(Func<bool> condition,float timeout){var w=Stopwatch.StartNew();while(!condition()){if(w.Elapsed.TotalSeconds>timeout)throw new TimeoutException("Frontend expected transition timed out");await Task.Delay(10);}}
}
