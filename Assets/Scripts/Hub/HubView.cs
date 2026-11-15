using System;
using System.Linq;
using System.Collections.Generic;
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
        public MainMenuHomeView home;
        public FrontendShell shell;
        public GameObject legacyNavigation,legacyProfileBar;
        HubProgression service;string page="Lobby",selectedPlayer,shopTab="Featured";int selectedSlot=-1;float toastUntil,nextTimer;
        InputAction back;Action confirmation;string confirmationId;
        readonly Stack<string> history=new Stack<string>();
        public string Page=>page;
        public HubProgression Service=>service;
        public void Show(string target)
        {
            CloseModal();if(target=="Lobby")history.Clear();else if(target!=page)history.Push(page);
            page=target;Render();
        }
        public void ShowSibling(string target){CloseModal();page=target;Render();}
        public void Back()
        {
            if(modal.gameObject.activeSelf){CloseModal();return;}
            if(page=="Result"){bool ok=service.DismissResult(out var message);if(!ok){Toast(message);return;}}
            page=history.Count>0?history.Pop():"Lobby";Render();
        }
        void Start()
        {
            foreach(var label in GetComponentsInChildren<TMP_Text>(true)){if(home!=null && label.transform.IsChildOf(home.transform))continue;label.overflowMode=TextOverflowModes.Truncate;label.fontSizeMax=label.fontSize;label.fontSizeMin=Mathf.Min(20,label.fontSize);label.enableAutoSizing=true;}
            service=HubApplication.Ensure().Progression;if(service==null){header.text=HubApplication.Instance.Error;return;}
            foreach(var b in GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                string route=b.gameObject.name;if(route.StartsWith("Nav_")){string destination=route.Substring(4);b.onClick.AddListener(()=>Show(destination));}
            }
            back=new InputAction("HubBack",InputActionType.Button,"<Keyboard>/escape");back.AddBinding("<Gamepad>/buttonEast");back.Enable();
            service.Changed+=OnChanged;page=service.Snapshot.pendingResult!=null?"Result":"Lobby";Render();
        }
        void OnDestroy(){if(service!=null)service.Changed-=OnChanged;back?.Dispose();}
        void OnChanged(){if(home!=null && page=="Lobby")home.Refresh(service.Snapshot);if(shell!=null&&page!="Lobby")shell.Present(page,service.Snapshot);}
        void Update()
        {
            if(service!=null&&HubApplication.Instance!=null&&!string.IsNullOrEmpty(HubApplication.Instance.Error)){Toast(HubApplication.Instance.Error);HubApplication.Instance.ClearError();}
            if(back!=null&&back.WasPressedThisFrame())Back();
            if(pageFade!=null)pageFade.alpha=Mathf.MoveTowards(pageFade.alpha,1,Time.unscaledDeltaTime*7);
            if(toast!=null&&Time.unscaledTime>toastUntil)toast.text="";
            if(service!=null&&Time.unscaledTime>nextTimer){nextTimer=Time.unscaledTime+1;UpdatePackTimers();}
        }
        void Feedback(bool success,string message){Toast(message);if(success)Render();}
        public void Toast(string message){toast.text=message;toastUntil=Time.unscaledTime+5;}
        void CloseModal(){confirmation=null;modal.gameObject.SetActive(false);HubWidgets.Clear(modal);}
        public void OpenShop(string tab){shopTab=tab;Show("Shop");}
        public void OpenRewardPack(string id)=>PackPanel(id);
        public void ShowComingSoon(string feature,string state="Coming Soon",string explanation=null)
        {
            CloseModal();modal.gameObject.SetActive(true);
            var panel=HubWidgets.Panel(modal,"ComingSoon",.28f,.27f,.44f,.46f,HubWidgets.Navy);
            HubWidgets.Text(panel,"FeatureTitle",feature.ToUpperInvariant(),.07f,.66f,.75f,.22f,36,HubWidgets.Cyan,TextAlignmentOptions.Center);
            FrontendControls.Back(panel,"CloseModal",.84f,.80f,.13f,.17f,CloseModal);
            HubWidgets.Text(panel,"Message",state,.07f,.46f,.86f,.16f,30,Color.white,TextAlignmentOptions.Center);
            string detail=feature=="Lucky Shot"?"An offline shooting challenge is planned. Try Training to practise now.":feature=="World Tournament"?"Future country-based competition. Separate from World Champions League.":"This feature is being prepared. No online service is connected.";
            HubWidgets.Text(panel,"Explanation",explanation??detail,.07f,.27f,.86f,.18f,22,HubWidgets.Muted,TextAlignmentOptions.Center);
            HubWidgets.Button(panel,"CloseComingSoon","CLOSE",.18f,.1f,.64f,.19f,CloseModal,HubWidgets.Palette[0]);
        }
        void Confirm(string title,string detail,Action action)
        {
            CloseModal();modal.gameObject.SetActive(true);confirmation=action;
            var p=HubWidgets.Panel(modal,"Confirmation",.25f,.25f,.5f,.5f,HubWidgets.Navy);
            FrontendControls.Back(p,"CloseModal",.85f,.80f,.12f,.17f,CloseModal);
            HubWidgets.Text(p,"Heading",title,.07f,.7f,.75f,.2f,34,HubWidgets.Gold);
            HubWidgets.Text(p,"Details",detail,.07f,.3f,.86f,.36f,25);
            HubWidgets.Button(p,"Cancel","CANCEL",.07f,.08f,.4f,.16f,CloseModal);
            HubWidgets.Button(p,"Confirm","CONFIRM",.53f,.08f,.4f,.16f,()=>{var call=confirmation;CloseModal();call?.Invoke();},HubWidgets.Palette[0]);
        }
        void Render()
        {
            if(service==null)return;var p=service.Snapshot;
            header.text=$"{p.displayName}   /   LV {p.level}   •   {p.xp%100}/100 XP       {p.trophies} TROPHIES          {p.coins:N0} COINS     {p.gems} GEMS";
            if(home!=null)
            {
                bool showHome=page=="Lobby";
                home.gameObject.SetActive(showHome);home.backdrop.SetActive(showHome);
                if(legacyNavigation!=null)legacyNavigation.SetActive(shell==null&&!showHome);
                if(legacyProfileBar!=null)legacyProfileBar.SetActive(shell==null&&!showHome);
                content.gameObject.SetActive(!showHome);
                if(shell!=null)shell.Present(page,p);
                if(showHome){home.Refresh(p);return;}
            }
            HubWidgets.Clear(content);pageFade.alpha=.25f;
            if(shell!=null&&page!="Play"&&page!="Mode Details"&&page!="Season Pass")HubWidgets.Panel(content,"PageSurface",0,0,1,1,new Color(.018f,.05f,.115f,.96f));
            if(shell==null&&page!="Lobby")HubWidgets.Button(content,"Back","X  LOBBY",0,.9f,.16f,.09f,Back);
            if(shell==null&&page!="Lobby")HubWidgets.Text(content,"PageTitle",page.ToUpperInvariant(),.2f,.9f,.77f,.09f,37,HubWidgets.Cyan);
            if(page=="Tactics"||page=="Collection")TeamTabs();
            switch(page)
            {
                case "Lobby":Lobby(p);break;case "Play":GameModes();break;case "Mode Details":ModeDetails(p);break;case "Collection":Collection(p);break;case "Details":Details(p);break;
                case "Tactics":Tactics(p);break;case "Shop":Shop(p);break;case "Season Pass":Season(p);break;case "Profile":Profile(p);break;
                case "Trophies":Trophies(p);break;case "Missions":Achievements(p,.94f);break;case "Friends":ShowComingSoon("Friends");break;case "Clubs":ShowComingSoon("Clubs");break;case "Settings":Settings();break;case "Result":Result(p);break;
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
        void Result(LocalProfile p)
        {
            var r=p.pendingResult;if(r==null){Show("Lobby");return;}
            var c=HubWidgets.Panel(content,"RewardResult",.12f,.09f,.76f,.76f,HubWidgets.Card);
            HubWidgets.Text(c,"Result",r.result+"   •   "+r.blue+" : "+r.white,.08f,.7f,.84f,.2f,52,HubWidgets.Gold,TextAlignmentOptions.Center);
            HubWidgets.Text(c,"Reward",$"+{r.coins} COINS    +{r.xp} XP    {r.trophies:+0;-0;0} TROPHIES\n{r.pack}\n{r.divisionMessage}\nSaved locally • receipt recorded once",.08f,.28f,.84f,.40f,27,Color.white,TextAlignmentOptions.Center);
            HubWidgets.Button(c,"Continue","CONTINUE TO LOBBY",.2f,.09f,.6f,.16f,()=>{bool ok=service.DismissResult(out var m);if(ok)Show("Lobby");else Toast(m);},HubWidgets.Palette[0]);
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
