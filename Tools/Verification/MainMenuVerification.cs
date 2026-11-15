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
using ToduStudios.WaterPolo.Editor;
using ToduStudios.WaterPolo.Hub;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class MainMenuVerification
{
    static T One<T>() where T:Object=>Object.FindAnyObjectByType<T>();
    static void Check(bool value,string text){if(!value)throw new Exception("Main menu: "+text);}
    static object Save(string name,object data){Directory.CreateDirectory("Logs/MainMenu");File.WriteAllText("Logs/MainMenu/"+name+".json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));return data;}
    static string Hash(string path){using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));}
    public static object Setup()
    {
        Check(!Application.isPlaying,"Saved Edit Mode required");MainMenuSetup.Open();
        var baseline=JObject.Parse(File.ReadAllText("Logs/MainMenu/Preservation-before.json"));var home=One<MainMenuHomeView>();Check(home!=null,"Saved home wiring exists");
        Check(AssetDatabase.AssetPathToGUID(MainMenuSetup.ScenePath)==(string)baseline["sceneGuid"],"MainLobby scene GUID retained");
        Check(Hash(MainMenuSetup.RecoveryPath)==(string)baseline["sceneHash"],"Exact original lobby recovery scene");
        Check(Hash(OlympicArenaSetup.ScenePath)==(string)baseline["arenaHash"] && Hash(Milestone12HubSetup.BootPath)==(string)baseline["bootHash"],"Arena/Boot scene bytes untouched");
        Check(Hash("Assets/Resources/HubCatalog.asset")== (string)baseline["catalogHash"] && Hash("ProjectSettings/EditorBuildSettings.asset")== (string)baseline["buildHash"],"Catalog/build configuration retained");
        foreach(var record in baseline["pngs"])
        {
            string path=(string)record["path"];Check(Hash(path)==(string)record["hash"] && AssetDatabase.AssetPathToGUID(path)==(string)record["guid"],"PNG pixels and GUID unchanged: "+path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);Check(importer.textureType==TextureImporterType.Sprite && !importer.mipmapEnabled && !importer.isReadable && importer.npotScale==TextureImporterNPOTScale.None,"UI sprite import: "+path);
            Check(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Count()==1,"One trimmed sprite: "+path);
        }
        var fields=typeof(MainMenuArt).GetFields().Where(f=>f.FieldType==typeof(Sprite)).ToArray();Check(fields.Length==21 && fields.All(f=>f.GetValue(home.art)!=null),"All 21 serialized sprites assigned");
        foreach(var button in ArtButtons(home))
        {
            Check(button.GetComponentsInChildren<TMP_Text>(true).All(t=>t.name=="RealClaimCount"),"Only real notification counts may accompany baked button lettering");
            var image=button.GetComponentInChildren<UnityEngine.UI.Image>();Check(image!=null,"Button graphics");
            var graphic=button.GetComponentsInChildren<UnityEngine.UI.Image>().FirstOrDefault(i=>i.sprite!=null);Check(graphic!=null && graphic.preserveAspect,"Artwork proportions retained");
        }
        Check(Object.FindObjectsByType<Canvas>().Length==1 && Object.FindObjectsByType<EventSystem>().Length==1 && One<Canvas>().GetComponent<UnityEngine.UI.GraphicRaycaster>()!=null,"One working Canvas/EventSystem");
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))Check(!t.GetComponents<Component>().Any(c=>c==null),"No missing scripts");
        foreach(var graphic in Object.FindObjectsByType<RoundedPanelGraphic>(FindObjectsInactive.Include))Check(graphic.GetComponent<CanvasRenderer>()!=null,"Custom panel CanvasRenderer");
        Check(home.slots.Length==4 && One<HubView>().content.gameObject.activeSelf==false,"Four persistent display slots, secondary page hidden");
        string before=Hash(MainMenuSetup.ScenePath);MainMenuSetup.SetUp();Check(Hash(MainMenuSetup.ScenePath)==before,"Repeat setup preserves scene bytes");
        Check(!PlayerSettings.allowedAutorotateToPortrait && !PlayerSettings.allowedAutorotateToPortraitUpsideDown && PlayerSettings.allowedAutorotateToLandscapeLeft && PlayerSettings.allowedAutorotateToLandscapeRight,"Landscape mobile orientation");
        return Save("Setup-PASS",new{passed=true,sprites=21,missingSprites=0,sourcePngsUnchanged=true,sourceGuidsUnchanged=true,sceneGuidRetained=true,recoveryExact=true,arenaAndBootUnchanged=true,economyCatalogUnchanged=true,repeatSafe=true,oneCanvasEventSystem=true});
    }
    static UnityEngine.UI.Button[] ArtButtons(MainMenuHomeView h)=>new[]{h.ranking,h.shop,h.team,h.battlePass,h.missions,h.friends,h.clubs,h.worldTournament,h.play};

    public static async Task<object> Capture()
    {
        await Until(()=>One<HubView>()!=null && One<HubView>().Service!=null,10);var view=One<HubView>();view.Show("Lobby");await Shot("First-Home");
        return new{scene=SceneManager.GetActiveScene().name,page=view.Page,frame=Time.frameCount,profileAndCurrencyLive=true};
    }
    public static async Task<object> FeedbackSmoke()
    {
        await Until(()=>One<HubView>()!=null && One<HubView>().Service!=null,10);var view=One<HubView>();view.Show("Lobby");var home=view.home;var mouse=InputSystem.AddDevice<Mouse>();
        try
        {
            var p=view.Service.Snapshot;Check(home.displayName.text==p.displayName && home.coins.text==p.coins.ToString("N0") && home.diamonds.text==p.gems.ToString("N0"),"Final live top bar values");
            Check(Mathf.Abs(home.levelFill.rectTransform.anchorMax.x-(p.xp%100)/100f)<.001f,"Final XP geometry");
            await Mouse(mouse,Point((RectTransform)home.shop.transform),true);await Seconds(.1f);Check(home.shop.GetComponent<MenuButtonFeedback>().artwork.localScale.x<.99f,"Final pointer press feedback");
            await Mouse(mouse,Point((RectTransform)home.shop.transform),false);Check(view.Page=="Shop","Final pointer route");await Back(mouse,view);
            return Save("FeedbackSmoke-PASS",new{passed=true,actualPointer=true,pressScale=true,liveProfileAndCurrencies=true,correctXpBar=true});
        }
        finally{InputSystem.RemoveDevice(mouse);view.Show("Lobby");}
    }

    sealed class MemoryRepository:IProfileRepository
    {public string json;public LocalProfile Load()=>json==null?null:LocalProfileCodec.FromJson(json);public void Save(LocalProfile p)=>json=JsonUtility.ToJson(p);}
    public static async Task<object> Interactions()
    {
        Check(Application.isPlaying && One<HubView>()!=null,"Boot/lobby Play required");var app=HubApplication.Instance;var original=app.Progression;string saveHash=File.Exists(HubApplication.SavePath)?Hash(HubApplication.SavePath):null;
        var repository=new MemoryRepository();long now=1800000000;var seed=new HubProgression(original.Catalog,repository,()=>now);var profile=seed.Snapshot;
        profile.displayName="Wave Captain";profile.coins=12345;profile.gems=73;profile.level=2;profile.xp=145;profile.seasonXp=120;profile.wins=1;profile.goals=1;profile.packs.Clear();
        profile.packs.Add(new RewardPack{instanceId="ui-waiting",rarity="Common",duration=45,outcome=0});
        profile.packs.Add(new RewardPack{instanceId="ui-unlocking",rarity="Rare",duration=45,startUtc=now-10,outcome=1});
        profile.packs.Add(new RewardPack{instanceId="ui-ready",rarity="Epic",duration=45,startUtc=now-50,outcome=2});repository.Save(profile);
        var isolated=new HubProgression(original.Catalog,repository,()=>now);app.UseVerificationProfile(isolated);var mouse=InputSystem.AddDevice<Mouse>();var keys=InputSystem.AddDevice<Keyboard>();
        try
        {
            var prior=One<HubView>();app.OpenLobby();await Until(()=>One<HubView>()!=null && One<HubView>()!=prior && One<HubView>().Service==isolated,8);await Seconds(.2f);
            var view=One<HubView>();var home=view.home;int first=Time.frameCount;await Frames(5);Check(Time.frameCount>first,"Advancing Play Mode");
            Check(home.displayName.text==profile.displayName && home.coins.text==profile.coins.ToString("N0") && home.diamonds.text==profile.gems.ToString("N0"),"Top bar uses real service values");
            Check(Mathf.Abs(home.levelFill.rectTransform.anchorMax.x-.45f)<.001f,"XP progress has the actual fractional width");
            Check(home.slots.Select(s=>s.State).SequenceEqual(new[]{MenuPackState.Waiting,MenuPackState.Unlocking,MenuPackState.Ready,MenuPackState.Empty}),"All four real pack states");
            Check(home.slots[0].packImage.sprite==home.art.commonPack && home.slots[1].packImage.sprite==home.art.rarePack && home.slots[2].packImage.sprite==home.art.epicPack && !home.slots[3].packImage.gameObject.activeSelf,"Art follows owned rarity, empty has no fake pack");
            Check(home.art.PackSprite(new RewardPack{rarity="Legendary"})==home.art.legendaryPack,"Legendary mapping available");
            Check(home.missionBadge.activeSelf && home.missionCount.text=="2" && home.seasonBadge.activeSelf && home.seasonCount.text=="2","Only actual claimable rewards show badges");await Shot("Fixture-Pack-States");
            await Mouse(mouse,Point((RectTransform)home.shop.transform),true);await Seconds(.08f);Check(home.shop.GetComponent<MenuButtonFeedback>().artwork.localScale.x<.99f,"Real pointer press scales art");await Mouse(mouse,Point((RectTransform)home.shop.transform),false);await Until(()=>view.Page=="Shop",2);
            await Back(mouse,view);
            foreach(var route in new[]{new{button=home.ranking,page="Trophies"},new{button=home.team,page="Tactics"},new{button=home.battlePass,page="Season Pass"},new{button=home.missions,page="Missions"},new{button=home.settings,page="Settings"},new{button=home.profile,page="Profile"}})
            {await Click(mouse,(RectTransform)route.button.transform);Check(view.Page==route.page && view.content.gameObject.activeInHierarchy,"Route "+route.page);await Back(mouse,view);}
            await Click(mouse,(RectTransform)home.team.transform);await Click(mouse,(RectTransform)Button("Nav_Collection").transform);Check(view.Page=="Collection","Existing collection remains accessible from team navigation");await Back(mouse,view);
            foreach(var route in new[]{new{button=home.friends,title="FRIENDS"},new{button=home.clubs,title="CLUBS"},new{button=home.worldTournament,title="WORLD TOURNAMENT"},new{button=home.messages,title="MESSAGES"}})
            {
                await Click(mouse,(RectTransform)route.button.transform);Check(view.modal.gameObject.activeSelf && view.modal.GetComponentsInChildren<TMP_Text>().Any(t=>t.text==route.title),"Reusable Coming Soon: "+route.title);
                await Click(mouse,(RectTransform)home.play.transform);Check(view.Page=="Lobby","Modal blocks underlying PLAY");
                await Shot("Popup-"+route.title.Replace(" ",""));await Click(mouse,(RectTransform)Button("CloseComingSoon").transform);Check(!view.modal.gameObject.activeSelf,"Popup closes");
            }
            await Click(mouse,(RectTransform)home.messages.transform);await PressKey(keys,Key.Escape);Check(!view.modal.gameObject.activeSelf,"Escape closes popup");
            await Click(mouse,(RectTransform)home.coinPlus.transform);Check(view.Page=="Shop" && view.content.Find("Offer_coin-bundle")!=null,"Coins plus opens actual Coins shop tab");await Back(mouse,view);
            await Click(mouse,(RectTransform)home.diamondPlus.transform);Check(view.Page=="Shop" && view.content.Find("FutureStore")!=null,"Diamond plus opens existing unavailable premium store");await Back(mouse,view);
            await Click(mouse,(RectTransform)home.profile.transform);view.content.GetComponentInChildren<TMP_InputField>().text="Captain Current";await Click(mouse,(RectTransform)Button("SaveProfile").transform);await Back(mouse,view);Check(home.displayName.text=="Captain Current","Existing profile edit updates top bar");

            await Click(mouse,(RectTransform)home.slots[0].transform);Check(view.modal.gameObject.activeSelf,"Waiting pack opens existing unlock confirmation");await Click(mouse,(RectTransform)Button("Confirm").transform);
            Check(isolated.Snapshot.packs.Single(p=>p.instanceId=="ui-waiting").startUtc==now && home.slots[0].State==MenuPackState.Unlocking,"Unlock saved through existing authority");
            Check(!isolated.StartPack("ui-waiting",out _),"Duplicate unlock refused");
            Check(new HubProgression(original.Catalog,repository,()=>now).Remaining(isolated.Snapshot.packs[0])==45,"Pack timer persists on reload");
            string timer=home.slots[1].status.text;now+=2;await Seconds(1.1f);Check(home.slots[1].status.text!=timer,"Visible countdown updates without rebuilding UI");
            int coins=isolated.Snapshot.coins,gems=isolated.Snapshot.gems;await Click(mouse,(RectTransform)home.slots[2].transform);await Click(mouse,(RectTransform)Button("Confirm").transform);
            Check(!isolated.Snapshot.packs.Any(p=>p.instanceId=="ui-ready") && isolated.Snapshot.coins==coins+original.Catalog.packOutcomes[2].reward.coins && isolated.Snapshot.gems==gems+original.Catalog.packOutcomes[2].reward.gems,"Ready pack claim updates real inventory/currency once");
            int after=isolated.Snapshot.coins;Check(!isolated.ClaimPack("ui-ready",out _) && isolated.Snapshot.coins==after,"Duplicate pack reward refused");await Click(mouse,(RectTransform)Button("Cancel").transform);
            int missionsBefore=int.Parse(home.missionCount.text);await Click(mouse,(RectTransform)home.missions.transform);await Click(mouse,(RectTransform)Button("Claim_first-win").transform);await Back(mouse,view);Check(int.Parse(home.missionCount.text)==missionsBefore-1 && isolated.Snapshot.claimedRewards.Contains("achievement:first-win"),"Mission badge reflects existing achievement claim");
            now+=100;await Seconds(1.1f);Check(home.slots[0].State==MenuPackState.Ready,"Countdown completion presents ready state");
            Check(new HubProgression(original.Catalog,repository,()=>now).Snapshot.packs.Count==2,"Claim removal persists");
            await Click(mouse,(RectTransform)home.play.transform);Check(view.Page=="Play" && Button("Launch0")!=null,"PLAY retains existing mode selection");await Back(mouse,view);
            Check(saveHash==null?!File.Exists(HubApplication.SavePath):Hash(HubApplication.SavePath)==saveHash,"Production profile bytes untouched during fixture");
            return Save("Interactions-PASS",new{passed=true,actualPointer=true,keyboardBack=true,routes=10,comingSoon=4,realProfileAndBalances=true,editableProfile=true,packStates=4,rarityImages=true,emptyNotOwned=true,unlockPersistence=true,countdownUpdates=true,claimOnce=true,inventoryReload=true,realNotificationBadges=true,pressFeedback=true,productionSaveUnchanged=true});
        }
        finally{InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keys);app.UseVerificationProfile(original);app.ReturnToLobby(false);}
    }

    public static async Task<object> Layouts()
    {
        Check(Application.isPlaying && One<HubView>()!=null,"Lobby Play required");var view=One<HubView>();view.Show("Lobby");
        const BindingFlags flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
        var assembly=typeof(UnityEditor.Editor).Assembly;var type=assembly.GetType("UnityEditor.GameView");var window=EditorWindow.GetWindow(type);var selected=type.GetProperty("selectedSizeIndex",flags);int original=(int)selected.GetValue(window);
        var sizesType=assembly.GetType("UnityEditor.GameViewSizes");var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);var sizes=singleton.GetProperty("instance",flags).GetValue(null);var group=sizesType.GetMethod("GetGroup",flags).Invoke(sizes,new[]{type.GetProperty("currentSizeGroupType",flags).GetValue(window)});var gt=group.GetType();var added=new List<int>();var results=new List<object>();
        var safe=view.GetComponent<SafeAreaLayout>();bool enabled=safe.enabled;var mouse=InputSystem.AddDevice<Mouse>();
        try
        {
            foreach(int width in new[]{1920,2340,2400})
            {
                var size=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"),"FixedResolution"),width,1080,"Main menu verification"});gt.GetMethod("AddCustomSize").Invoke(group,new[]{size});int index=(int)gt.GetMethod("GetTotalCount").Invoke(group,null)-1;added.Add(index);selected.SetValue(window,index);window.Repaint();await Seconds(.25f);
                Check(Screen.width==width && Screen.height==1080,"Actual Game view resolution");safe.enabled=false;var rect=new Rect(70,24,width-140,1032);safe.Apply(rect,new Vector2(width,1080));Canvas.ForceUpdateCanvases();await Frames(4);
                var home=view.home;var buttons=ArtButtons(home).Concat(new[]{home.profile,home.settings,home.messages,home.diamondPlus,home.coinPlus}).Concat(home.slots.Select(s=>s.button)).ToArray();var bounds=new List<Rect>();
                foreach(var button in buttons)
                {
                    var b=Bounds((RectTransform)button.transform);Check(b.xMin>=rect.xMin-1 && b.xMax<=rect.xMax+1 && b.yMin>=rect.yMin-1 && b.yMax<=rect.yMax+1,"Safe area bounds: "+button.name);Check(b.width>=60 && b.height>=60,"Touch target size: "+button.name);bounds.Add(b);
                }
                for(int a=0;a<bounds.Count;a++)for(int b=a+1;b<bounds.Count;b++)Check(!bounds[a].Overlaps(bounds[b]),"Interactive elements do not overlap: "+buttons[a].name+" / "+buttons[b].name);
                foreach(var text in home.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();Check(!text.isTextOverflowing,"Text fits: "+text.name);}
                foreach(var image in home.GetComponentsInChildren<UnityEngine.UI.Image>().Where(i=>i.sprite!=null))Check(image.preserveAspect,"All PNG proportions preserved");
                var pngButtons=ArtButtons(home);float minPlay=Bounds((RectTransform)home.play.transform).width*Bounds((RectTransform)home.play.transform).height;
                Check(minPlay>Bounds((RectTransform)home.ranking.transform).width*Bounds((RectTransform)home.ranking.transform).height*1.2f,"PLAY is prominent");
                await Shot("Home-"+width);await Click(mouse,(RectTransform)home.shop.transform);Check(view.Page=="Shop","Pointer Shop at "+width);await Shot("Shop-"+width);await Back(mouse,view);
                await Click(mouse,(RectTransform)home.messages.transform);await Shot("ComingSoon-"+width);await Click(mouse,(RectTransform)Button("CloseComingSoon").transform);
                results.Add(new{width,height=1080,safeInsets=true,allControlsVisible=true,noControlOverlap=true,textFits=true,pngAspectPreserved=true,pointerRouting=true});
            }
            return Save("Layouts-PASS",new{passed=true,screens=results,realProfileScreenshots=true,deviceTested=false});
        }
        finally{InputSystem.RemoveDevice(mouse);selected.SetValue(window,original);for(int i=added.Count-1;i>=0;i--)gt.GetMethod("RemoveCustomSize").Invoke(group,new object[]{added[i]});await Seconds(.15f);safe.Apply(Screen.safeArea,new Vector2(Screen.width,Screen.height));safe.enabled=enabled;window.Repaint();view.Show("Lobby");}
    }
    static Rect Bounds(RectTransform rect){var corners=new Vector3[4];rect.GetWorldCorners(corners);return Rect.MinMaxRect(corners.Min(c=>c.x),corners.Min(c=>c.y),corners.Max(c=>c.x),corners.Max(c=>c.y));}
    static UnityEngine.UI.Button Button(string name)=>Object.FindObjectsByType<UnityEngine.UI.Button>().Single(b=>b.name==name);
    static async Task Back(Mouse mouse,HubView view){await Click(mouse,(RectTransform)Button("Back").transform);Check(view.Page=="Lobby","Back returns home");}
    static Vector2 Point(RectTransform rect)=>RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
    static async Task Mouse(Mouse mouse,Vector2 position,bool down){InputSystem.QueueStateEvent(mouse,new MouseState{position=position,buttons=(ushort)(down?1:0)});await Seconds(.07f);}
    static async Task Click(Mouse mouse,RectTransform rect){var point=Point(rect);await Mouse(mouse,point,false);await Mouse(mouse,point,true);await Mouse(mouse,point,false);await Seconds(.08f);}
    static async Task PressKey(Keyboard keyboard,Key key){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));await Seconds(.07f);InputSystem.QueueStateEvent(keyboard,new KeyboardState());await Seconds(.07f);}
    static async Task Shot(string name){await Seconds(.2f);await Frames(5);ScreenCapture.CaptureScreenshot("Logs/MainMenu/"+name+".png");await Frames(5);}
    static async Task Seconds(float seconds){var watch=Stopwatch.StartNew();while(watch.Elapsed.TotalSeconds<seconds)await Task.Delay(10);}
    static async Task Frames(int count){int frame=Time.frameCount+count;await Until(()=>Time.frameCount>=frame,10);}
    static async Task Until(Func<bool> condition,float timeout){var watch=Stopwatch.StartNew();while(!condition()){if(watch.Elapsed.TotalSeconds>timeout)throw new TimeoutException("Expected main menu transition did not occur.");await Task.Delay(10);}}
}
