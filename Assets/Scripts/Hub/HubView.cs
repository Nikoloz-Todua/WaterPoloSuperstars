using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ToduStudios.WaterPolo.Hub
{
    /// <summary>Navigation/presentation only; progression changes go through HubProgression.</summary>
    public sealed partial class HubView : MonoBehaviour
    {
        public RectTransform content,modal; public TMP_Text header,toast; public CanvasGroup pageFade;
        HubProgression service;string page="Lobby",selectedPlayer,shopTab="Featured";int selectedSlot=-1;float toastUntil,nextTimer;
        InputAction back;Action confirmation;string confirmationId;
        public string Page=>page;
        public HubProgression Service=>service;
        public void Show(string target){page=target;Render();}
        void Start()
        {
            foreach(var label in GetComponentsInChildren<TMP_Text>(true)){label.overflowMode=TextOverflowModes.Truncate;label.fontSizeMax=label.fontSize;label.fontSizeMin=Mathf.Min(20,label.fontSize);label.enableAutoSizing=true;}
            service=HubApplication.Ensure().Progression;if(service==null){header.text=HubApplication.Instance.Error;return;}
            foreach(var b in GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                string route=b.gameObject.name;if(route.StartsWith("Nav_")){string destination=route.Substring(4);b.onClick.AddListener(()=>Show(destination));}
            }
            back=new InputAction("HubBack",InputActionType.Button,"<Keyboard>/escape");back.AddBinding("<Gamepad>/buttonEast");back.Enable();
            service.Changed+=OnChanged;page=service.Snapshot.pendingResult!=null?"Result":"Lobby";Render();
        }
        void OnDestroy(){if(service!=null)service.Changed-=OnChanged;back?.Dispose();}
        void OnChanged(){/* Transactions are rendered by their initiating action, never during a partially constructed panel. */}
        void Update()
        {
            if(service!=null&&HubApplication.Instance!=null&&!string.IsNullOrEmpty(HubApplication.Instance.Error)){Toast(HubApplication.Instance.Error);HubApplication.Instance.ClearError();}
            if(back!=null&&back.WasPressedThisFrame()){if(modal.gameObject.activeSelf)CloseModal();else Show("Lobby");}
            if(pageFade!=null)pageFade.alpha=Mathf.MoveTowards(pageFade.alpha,1,Time.unscaledDeltaTime*7);
            if(toast!=null&&Time.unscaledTime>toastUntil)toast.text="";
            if(service!=null&&Time.unscaledTime>nextTimer){nextTimer=Time.unscaledTime+1;UpdatePackTimers();}
        }
        void Feedback(bool success,string message){Toast(message);if(success)Render();}
        public void Toast(string message){toast.text=message;toastUntil=Time.unscaledTime+5;}
        void CloseModal(){confirmation=null;modal.gameObject.SetActive(false);HubWidgets.Clear(modal);}
        void Confirm(string title,string detail,Action action)
        {
            CloseModal();modal.gameObject.SetActive(true);confirmation=action;
            var p=HubWidgets.Panel(modal,"Confirmation",.25f,.25f,.5f,.5f,HubWidgets.Navy);
            HubWidgets.Text(p,"Heading",title,.07f,.7f,.86f,.2f,34,HubWidgets.Gold);
            HubWidgets.Text(p,"Details",detail,.07f,.3f,.86f,.36f,25);
            HubWidgets.Button(p,"Cancel","CANCEL",.07f,.08f,.4f,.16f,CloseModal);
            HubWidgets.Button(p,"Confirm","CONFIRM",.53f,.08f,.4f,.16f,()=>{var call=confirmation;CloseModal();call?.Invoke();},HubWidgets.Palette[0]);
        }
        void Render()
        {
            if(service==null)return;var p=service.Snapshot;
            header.text=$"{p.displayName}   /   LV {p.level}   •   {p.xp%100}/100 XP       {p.trophies} TROPHIES          {p.coins:N0} COINS     {p.gems} GEMS";
            HubWidgets.Clear(content);pageFade.alpha=.25f;
            if(page!="Lobby")HubWidgets.Button(content,"Back","‹  LOBBY",0,.9f,.16f,.09f,()=>Show("Lobby"));
            if(page!="Lobby")HubWidgets.Text(content,"PageTitle",page.ToUpperInvariant(),.2f,.9f,.77f,.09f,37,HubWidgets.Cyan);
            switch(page)
            {
                case "Lobby":Lobby(p);break;case "Play":Modes();break;case "Collection":Collection(p);break;case "Details":Details(p);break;
                case "Tactics":Tactics(p);break;case "Shop":Shop(p);break;case "Season Pass":Season(p);break;case "Profile":Profile(p);break;
                case "Trophies":Trophies(p);break;case "Friends":Social(false,p);break;case "Clubs":Social(true,p);break;case "Settings":Settings();break;case "Result":Result(p);break;
            }
        }
        void Lobby(LocalProfile p)
        {
            HubWidgets.Text(content,"Brand","WATER POLO\nSUPERSTARS",.02f,.73f,.46f,.24f,60).fontStyle=FontStyles.Bold;
            HubWidgets.Text(content,"Studio","TODUSTUDIOS   /   OWN THE CURRENT",.025f,.67f,.49f,.07f,22,HubWidgets.Cyan);
            var portrait=HubWidgets.Panel(content,"FeaturedAthlete",.49f,.24f,.31f,.73f,Color.Lerp(HubWidgets.Navy,HubWidgets.Palette[p.accentColor],.32f));HubWidgets.Athlete(portrait,HubWidgets.Palette[p.capColor],HubWidgets.Palette[p.swimwearColor]);
            var captain=service.Catalog.Player(p.lineup[1]);HubWidgets.Text(portrait,"Name",captain.name.ToUpperInvariant(),.04f,.02f,.92f,.12f,27,Color.white,TextAlignmentOptions.Center);
            HubWidgets.Text(content,"Squad","YOUR SQUAD\nTIDEBREAK WAVE\n7 STARTERS • "+p.tactics.ToUpperInvariant()+" DEFENSE",.025f,.38f,.43f,.24f,32);
            HubWidgets.Button(content,"Play","PLAY  ›",.025f,.23f,.42f,.14f,()=>Show("Play"),HubWidgets.Palette[0]);
            var season=HubWidgets.Panel(content,"SeasonPreview",.82f,.43f,.17f,.53f,HubWidgets.Card);
            HubWidgets.Text(season,"Title","TIDEBREAK\nSEASON 01",.09f,.64f,.82f,.3f,30,HubWidgets.Gold);
            HubWidgets.Text(season,"XP",p.seasonXp+" SEASON XP\nFree rewards await",.09f,.3f,.82f,.3f,23);
            HubWidgets.Button(season,"Season","VIEW PASS",.08f,.05f,.84f,.19f,()=>Show("Season Pass"));
            Packs(p);
        }
        void Modes()
        {
            string[] titles={"QUICK MATCH","TRAINING","ONLINE","CAREER / TOURNAMENT"};string[] desc={"Full 7 vs 7 • four periods\nEarn coins, XP, trophies & a free pack","Practice the complete match\nNo economy or progression rewards","Coming Later — Online\nAccounts and real matchmaking required","Roadmap preview\nComing later"};
            for(int i=0;i<4;i++){int index=i;float x=(i%2)*.51f,y=i<2?.47f:.06f;var c=HubWidgets.Panel(content,"Mode"+i,x,y,.49f,.36f,HubWidgets.Card);HubWidgets.Text(c,"Title",titles[i],.05f,.7f,.9f,.23f,33,HubWidgets.Cyan);HubWidgets.Text(c,"Info",desc[i],.05f,.3f,.9f,.35f,24);HubWidgets.Button(c,"Launch"+i,i<2?"START":"COMING LATER",.05f,.05f,.9f,.2f,i<2?(Action)(()=>HubApplication.Instance.Launch(index==0?"quick":"training")):null,HubWidgets.Palette[0],i<2);}
        }
        void Result(LocalProfile p)
        {
            var r=p.pendingResult;if(r==null){Show("Lobby");return;}
            var c=HubWidgets.Panel(content,"RewardResult",.12f,.09f,.76f,.76f,HubWidgets.Card);
            HubWidgets.Text(c,"Result",r.result+"   •   "+r.blue+" : "+r.white,.08f,.7f,.84f,.2f,52,HubWidgets.Gold,TextAlignmentOptions.Center);
            HubWidgets.Text(c,"Reward",$"+{r.coins} COINS    +{r.xp} XP    {r.trophies:+0;-0;0} TROPHIES\n{r.pack}\nRewards saved locally • receipt recorded once",.08f,.3f,.84f,.35f,29,Color.white,TextAlignmentOptions.Center);
            HubWidgets.Button(c,"Continue","CONTINUE TO LOBBY",.2f,.09f,.6f,.16f,()=>{bool ok=service.DismissResult(out var m);if(ok)Show("Lobby");else Toast(m);},HubWidgets.Palette[0]);
        }
        void Social(bool clubs,LocalProfile p)
        {
            var card=HubWidgets.Panel(content,"OfflinePreview",.06f,.08f,.88f,.76f,HubWidgets.Card);
            HubWidgets.Text(card,"Badge",clubs?"W / CLUBHOUSE":"W / CREW",.08f,.65f,.8f,.22f,54,HubWidgets.Gold);
            HubWidgets.Text(card,"About",clubs?"YOUR FUTURE CLUB STARTS HERE\nClub badge, member roster & invitations will appear here.\nPreview only • no shared club has been created.":p.displayName+" • LOCAL PROFILE\nYour future friends list and invitations will appear here.\nNo friends or online status are simulated.",.08f,.28f,.84f,.36f,29);
            HubWidgets.Button(card,"Future",clubs?"JOIN / CREATE — COMING LATER":"ADD FRIEND — COMING LATER",.08f,.08f,.84f,.14f,null,null,false);
        }
        void Settings()
        {
            HubWidgets.Text(content,"Info","LOCAL PRESENTATION SETTINGS\nMatch camera, SFX and graphics controls remain in the match Pause > Settings menu.",.05f,.6f,.9f,.23f,30);
            var prefs=GetComponent<GamePreferences>();float volume=prefs.Current.master;
            HubWidgets.Button(content,"VolumeDown","VOLUME −",.1f,.35f,.3f,.12f,()=>{prefs.SetMaster(prefs.Current.master-.1f);prefs.Save();Render();});
            HubWidgets.Button(content,"VolumeUp","VOLUME +",.6f,.35f,.3f,.12f,()=>{prefs.SetMaster(prefs.Current.master+.1f);prefs.Save();Render();});
            HubWidgets.Text(content,"Volume",Mathf.RoundToInt(volume*100)+"%",.42f,.35f,.16f,.12f,38,HubWidgets.Cyan,TextAlignmentOptions.Center);
            HubWidgets.Text(content,"Local","SAVE STATUS: LOCAL ONLY\nNo cloud connection • no network required",.1f,.09f,.8f,.2f,27,HubWidgets.Muted);
        }
    }
}
