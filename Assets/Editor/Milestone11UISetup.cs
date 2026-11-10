using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ToduStudios.WaterPolo.Editor
{
    /// <summary>Extends the existing Canvas and observers in place. Never rebuilds match objects or other scenes.</summary>
    public static class Milestone11UISetup
    {
        private static readonly Color Navy=new Color(0.022f,0.05f,0.095f,0.96f),Card=new Color(0.045f,0.095f,0.16f,0.97f),Blue=new Color(0.035f,0.38f,0.65f),Cyan=new Color(0.18f,0.84f,0.95f),Muted=new Color(0.62f,0.75f,0.83f);
        [MenuItem("Tools/Water Polo/Set Up Milestone 11 Interface")]
        public static void SetUp()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path!=MovementPrototypeBuilder.ScenePath)
            {Debug.LogWarning("Open PrototypePool, stop Play Mode and save intended edits before M11 setup. Nothing changed.");return;}
            var match=UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
            if(match==null || roster==null || roster.Members.Count!=14 || match.Rules==null)throw new InvalidOperationException("M11 requires the existing fourteen-athlete match and rules.");
            var hud=UnityEngine.Object.FindAnyObjectByType<MatchHUD>();var canvas=hud.GetComponentInParent<Canvas>();
            foreach(var graphic in canvas.GetComponentsInChildren<RoundedPanelGraphic>(true))
                if(graphic.GetComponent<CanvasRenderer>()==null)graphic.gameObject.AddComponent<CanvasRenderer>();
            if(canvas.GetComponent<MatchMenuController>()!=null)
            {MobileControlsSetup.Configure(canvas,match);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("Milestone 11 configured; existing interface tuning preserved, missing mobile controls added.");return;}
            var scaler=Get<CanvasScaler>(canvas.gameObject);scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920f,1080f);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;scaler.matchWidthOrHeight=1f;
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;Get<GraphicRaycaster>(canvas.gameObject);
            var events=UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if(events==null)events=new GameObject("PrototypeEventSystem",typeof(EventSystem)).GetComponent<EventSystem>();
            var module=Get<InputSystemUIInputModule>(events.gameObject);if(module.actionsAsset==null)module.AssignDefaultActions();
            var safe=Stretch(canvas.transform,"MatchSafeArea");Get<SafeAreaLayout>(safe.gameObject);
            // Move existing observers' graphics, retaining all component and serialized references.
            var scoreboard=canvas.GetComponent<PrototypeScoreboard>();var atmosphere=canvas.GetComponent<MatchAtmosphere>();
            var score=Read<TMP_Text>(scoreboard,"scoreLabel");var oldScorePanel=score.transform.parent.gameObject;
            var header=Panel(safe,"ScoreCard",new Vector2(0.5f,1f),new Vector2(0f,-82f),new Vector2(650f,120f),Navy);
            MoveLabel(score,header,new Vector2(0.5f,0.5f),new Vector2(0f,20f),new Vector2(170f,62f),46f);
            score.fontStyle=FontStyles.Bold;Bool(scoreboard,"compactScore",true);
            if(oldScorePanel!=canvas.gameObject && oldScorePanel!=header.gameObject && oldScorePanel.transform.childCount==0)oldScorePanel.SetActive(false);
            Label(header,"BlueTeam","BLUE",new Vector2(0.5f,0.5f),new Vector2(-210f,20f),new Vector2(190f,52f),30f,Cyan).fontStyle=FontStyles.Bold;
            Label(header,"WhiteTeam","WHITE",new Vector2(0.5f,0.5f),new Vector2(210f,20f),new Vector2(190f,52f),30f,Color.white).fontStyle=FontStyles.Bold;
            MoveLabel(Read<TMP_Text>(hud,"clockLabel"),header,new Vector2(0.5f,0.5f),new Vector2(-140f,-32f),new Vector2(300f,40f),25f);
            MoveLabel(Read<TMP_Text>(hud,"shotClockLabel"),header,new Vector2(0.5f,0.5f),new Vector2(190f,-32f),new Vector2(210f,40f),25f);
            var possession=Label(safe,"Possession","LOOSE BALL",new Vector2(0.5f,1f),new Vector2(0f,-166f),new Vector2(600f,35f),21f,Cyan);Assign(hud,"possessionLabel",possession);
            var feed=Panel(safe,"EventCard",new Vector2(0.5f,1f),new Vector2(0f,-216f),new Vector2(1000f,60f),Navy);
            MoveLabel(Read<TMP_Text>(atmosphere,"eventLabel"),feed,new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(960f,52f),24f);
            Assign(atmosphere,"eventPanel",feed.gameObject);Bool(atmosphere,"compactFeed",true);feed.gameObject.SetActive(false);
            var goalLabel=Read<TMP_Text>(scoreboard,"goalLabel");MoveLabel(goalLabel,safe,new Vector2(0.5f,1f),new Vector2(0f,-216f),new Vector2(1000f,60f),38f);goalLabel.text="GOAL!";goalLabel.color=Cyan;
            var athlete=Panel(safe,"AthleteCard",new Vector2(0f,0f),new Vector2(240f,90f),new Vector2(416f,116f),Navy);
            var stamina=canvas.GetComponent<ActiveStaminaHUD>();
            MoveLabel(Read<TMP_Text>(stamina,"label"),athlete,new Vector2(0.5f,0.5f),new Vector2(0f,26f),new Vector2(380f,35f),24f);Bool(stamina,"showAthlete",true);
            Label(athlete,"EnergyHint","ENERGY   /   SHIFT TO SPRINT",new Vector2(0.5f,0.5f),new Vector2(0f,-4f),new Vector2(380f,26f),17f,Muted);
            var staminaBar=(RectTransform)Read<Image>(stamina,"fill").transform.parent;Place(staminaBar,athlete,new Vector2(0.5f,0.5f),new Vector2(0f,-34f),new Vector2(360f,12f));
            var strategy=Panel(safe,"StrategyCard",new Vector2(0.5f,0f),new Vector2(0f,68f),new Vector2(390f,70f),Navy);
            MoveLabel(Read<TMP_Text>(hud,"strategyLabel"),strategy,new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(370f,60f),22f);
            Label(safe,"QuickControls","C SWITCH   B PASS   L LOB   SPACE SHOOT",new Vector2(0.5f,0f),new Vector2(0f,128f),new Vector2(900f,32f),19f,Color.white);
            var status=Panel(safe,"RulesCard",new Vector2(1f,0f),new Vector2(-250f,90f),new Vector2(436f,116f),Navy);
            MoveLabel(Read<TMP_Text>(atmosphere,"rulesLabel"),status,new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(410f,96f),20f);
            var power=canvas.GetComponent<ShotPowerBar>();var powerPanel=Read<GameObject>(power,"panel").GetComponent<RectTransform>();
            Place(powerPanel,safe,new Vector2(0.5f,0f),new Vector2(0f,190f),new Vector2(320f,20f));
            Label(powerPanel,"PowerCaption","SHOT POWER",new Vector2(0.5f,0.5f),new Vector2(0f,28f),new Vector2(320f,26f),18f,Color.white);
            // Legacy transition graphics remain available to old scenes, but M11 delegates navigation to the menu.
            Read<GameObject>(hud,"panel").SetActive(false);
            var overlayRect=Stretch(canvas.transform,"MenuOverlay");var dim=overlayRect.gameObject.AddComponent<Image>();dim.color=new Color(0.005f,0.018f,0.04f,0.78f);
            var group=overlayRect.gameObject.AddComponent<CanvasGroup>();group.alpha=0f;group.blocksRaycasts=group.interactable=false;
            var content=Stretch(overlayRect,"MenuSafeArea");Get<SafeAreaLayout>(content.gameObject);
            var prefs=canvas.gameObject.AddComponent<GamePreferences>();Assign(prefs,"gameplayCamera",UnityEngine.Object.FindAnyObjectByType<GameplayCamera>());
            Assign(prefs,"atmosphere",atmosphere);Assign(prefs,"effects",UnityEngine.Object.FindAnyObjectByType<WaterInteractionEffects>());
            Assign(prefs,"cameraData",UnityEngine.Object.FindAnyObjectByType<GameplayCamera>().GetComponent<UniversalAdditionalCameraData>());
            var menus=canvas.gameObject.AddComponent<MatchMenuController>();Assign(menus,"match",match);Assign(menus,"preferences",prefs);Assign(menus,"overlay",group);Assign(hud,"menus",menus);
            var pause=Panel(content,"PausePanel",new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(640f,720f),Navy);
            Heading(pause,"MATCH PAUSED","WATER POLO SUPERSTARS");
            Button(pause,"Resume","RESUME MATCH",new Vector2(0f,110f),new Vector2(528f,88f),menus.Resume,Blue);
            Button(pause,"Settings","SETTINGS",new Vector2(0f,0f),new Vector2(528f,88f),menus.OpenSettings,Card);
            Button(pause,"Restart","RESTART MATCH",new Vector2(0f,-110f),new Vector2(528f,88f),menus.AskRestart,Card);
            Button(pause,"Exit","EXIT MATCH",new Vector2(0f,-220f),new Vector2(528f,88f),menus.AskExit,Card);
            Label(pause,"Hint","ESC TO RESUME",new Vector2(0.5f,0.5f),new Vector2(0f,-306f),new Vector2(550f,40f),19f,Muted);
            var landing=Panel(content,"LandingPanel",new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(850f,620f),Navy);
            Heading(landing,"WATER POLO\nSUPERSTARS","TODUSTUDIOS  /  MATCH PROTOTYPE",205f);
            Button(landing,"Start","START MATCH",new Vector2(0f,-55f),new Vector2(650f,96f),menus.StartMatch,Blue);
            Button(landing,"Settings","SETTINGS & CONTROLS",new Vector2(0f,-172f),new Vector2(650f,88f),menus.OpenSettings,Card);
            Label(landing,"Version","6 FIELD PLAYERS + GOALKEEPER  /  FOUR PERIODS",new Vector2(0.5f,0.5f),new Vector2(0f,-261f),new Vector2(790f,40f),19f,Muted);
            var confirm=Panel(content,"ConfirmationPanel",new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(850f,450f),Navy);
            var confirmTitle=Label(confirm,"Title","CONFIRM",new Vector2(0.5f,0.5f),new Vector2(0f,125f),new Vector2(760f,70f),38f,Color.white);
            var confirmBody=Label(confirm,"Body","",new Vector2(0.5f,0.5f),new Vector2(0f,20f),new Vector2(760f,115f),25f,Muted);
            Button(confirm,"Cancel","CANCEL",new Vector2(-187f,-124f),new Vector2(330f,88f),menus.Cancel,Card);
            Button(confirm,"Confirm","CONFIRM",new Vector2(187f,-124f),new Vector2(330f,88f),menus.Confirm,new Color(0.65f,0.18f,0.19f));
            var transition=Panel(content,"TransitionPanel",new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(850f,650f),Navy);
            var transTitle=Label(transition,"Title","QUARTER COMPLETE",new Vector2(0.5f,0.5f),new Vector2(0f,220f),new Vector2(790f,80f),42f,Cyan);
            var transBody=Label(transition,"Body","",new Vector2(0.5f,0.5f),new Vector2(0f,100f),new Vector2(790f,130f),29f,Color.white);
            var next=Button(transition,"Continue","CONTINUE MATCH",new Vector2(0f,-35f),new Vector2(650f,88f),menus.Continue,Blue);
            var again=Button(transition,"Restart","PLAY AGAIN",new Vector2(0f,-35f),new Vector2(650f,88f),menus.AskRestart,Blue);
            Button(transition,"Settings","SETTINGS",new Vector2(-170f,-145f),new Vector2(310f,88f),menus.OpenSettings,Card);
            Button(transition,"Exit","EXIT MATCH",new Vector2(170f,-145f),new Vector2(310f,88f),menus.AskExit,Card);
            Label(transition,"Caption","TODUSTUDIOS  /  WATER POLO SUPERSTARS",new Vector2(0.5f,0.5f),new Vector2(0f,-263f),new Vector2(780f,35f),19f,Muted);
            var settings=BuildSettings(content,prefs,menus);
            Assign(menus,"pausePanel",pause.gameObject);Assign(menus,"settingsPanel",settings.gameObject);Assign(menus,"confirmationPanel",confirm.gameObject);
            Assign(menus,"landingPanel",landing.gameObject);Assign(menus,"transitionPanel",transition.gameObject);
            Assign(menus,"confirmationTitle",confirmTitle);Assign(menus,"confirmationBody",confirmBody);Assign(menus,"transitionTitle",transTitle);Assign(menus,"transitionBody",transBody);
            Assign(menus,"continueButton",next.gameObject);Assign(menus,"resultRestartButton",again.gameObject);
            // Pause sits below the modal in draw order. Two geometry bars avoid missing icon-font glyphs.
            var pauseKey=Button(safe,"PauseButton","",new Vector2(-80f,-80f),new Vector2(96f,96f),menus.OpenPause,Navy);
            pauseKey.anchorMin=pauseKey.anchorMax=Vector2.one;
            Panel(pauseKey,"BarLeft",new Vector2(0.5f,0.5f),new Vector2(-10f,5f),new Vector2(9f,32f),Color.white);
            Panel(pauseKey,"BarRight",new Vector2(0.5f,0.5f),new Vector2(10f,5f),new Vector2(9f,32f),Color.white);
            Label(pauseKey,"Shortcut","ESC",new Vector2(0.5f,0.5f),new Vector2(0f,-29f),new Vector2(85f,25f),16f,Muted);Assign(menus,"pauseButton",pauseKey.gameObject);
            foreach(var p in new[]{pause,settings,confirm,landing,transition})p.gameObject.SetActive(false);
            MobileControlsSetup.Configure(canvas,match);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("Milestone 11 interface configured in place. One Canvas, one EventSystem, existing match and ball authorities retained.");
        }
        private static RectTransform BuildSettings(Transform parent,GamePreferences prefs,MatchMenuController menus)
        {
            var panel=Panel(parent,"SettingsPanel",new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(1160f,880f),Navy);
            Label(panel,"Title","SETTINGS",new Vector2(0.5f,0.5f),new Vector2(0f,370f),new Vector2(1000f,70f),40f,Color.white);
            Label(panel,"AudioHeading","AUDIO",new Vector2(0.5f,0.5f),new Vector2(-280f,286f),new Vector2(480f,40f),23f,Cyan);
            Label(panel,"ViewHeading","CAMERA & GRAPHICS",new Vector2(0.5f,0.5f),new Vector2(280f,286f),new Vector2(480f,40f),23f,Cyan);
            var view=panel.gameObject.AddComponent<MatchSettingsView>();Assign(view,"preferences",prefs);
            Setting(panel,view,"master","MASTER VOLUME",-280f,200f,0f,1f);
            Setting(panel,view,"sfx","SOUND EFFECTS",-280f,75f,0f,1f);
            var crowd=Setting(panel,view,"crowd","CROWD / ATMOSPHERE",-280f,-50f,0f,1f);Assign(view,"crowdRow",crowd.gameObject);
            Label(panel,"AudioNote",prefs.SupportsCrowd?"Event-driven match atmosphere":"Original match cues. No crowd clip is installed.",new Vector2(0.5f,0.5f),new Vector2(-280f,-154f),new Vector2(480f,65f),19f,Muted);
            Setting(panel,view,"zoom","CAMERA DISTANCE",280f,200f,0.85f,1.25f);
            Setting(panel,view,"smoothing","CAMERA SMOOTHING",280f,75f,0.65f,1.8f);
            var quality=Label(panel,"GraphicsValue","HIGH",new Vector2(0.5f,0.5f),new Vector2(280f,-20f),new Vector2(470f,35f),21f,Cyan);Assign(view,"qualityValue",quality);
            Button(panel,"Low","LOW",new Vector2(120f,-80f),new Vector2(145f,64f),view.Low,Card);
            Button(panel,"Medium","MEDIUM",new Vector2(280f,-80f),new Vector2(155f,64f),view.Medium,Card);
            Button(panel,"High","HIGH",new Vector2(440f,-80f),new Vector2(145f,64f),view.High,Card);
            Label(panel,"GraphicsNote","Water effects and camera shadows / post effects.\nHigher distance shows more of the pool.",new Vector2(0.5f,0.5f),new Vector2(280f,-158f),new Vector2(500f,70f),18f,Muted);
            Label(panel,"Controls","CONTROLS\nWASD / ARROWS  Swim     SHIFT  Sprint     C  Switch     Z  Press / Zone\nE  Collect     B  Pass     L  Lob     F  Steal     G  Hold (foul risk)\nSPACE  Hold / release shot     Q + SPACE  Skip     R + SPACE  High\nAim by facing with movement. Blue goalkeeper control follows possession.",new Vector2(0.5f,0.5f),new Vector2(0f,-273f),new Vector2(1060f,150f),21f,Color.white);
            Button(panel,"Back","SAVE & BACK",new Vector2(0f,-384f),new Vector2(480f,72f),menus.Cancel,Blue);
            return panel;
        }
        private static RectTransform Setting(Transform parent,MatchSettingsView view,string field,string caption,float x,float y,float min,float max)
        {
            var row=Rect(parent,field+"Row",new Vector2(0.5f,0.5f),new Vector2(x,y),new Vector2(480f,106f));
            Label(row,"Caption",caption,new Vector2(0.5f,0.5f),new Vector2(-52f,28f),new Vector2(370f,35f),21f,Color.white).alignment=TextAlignmentOptions.Left;
            var value=Label(row,"Value","",new Vector2(0.5f,0.5f),new Vector2(190f,28f),new Vector2(100f,35f),20f,Cyan);Assign(view,field+"Value",value);
            var control=Rect(row,"Slider",new Vector2(0.5f,0.5f),new Vector2(0f,-20f),new Vector2(470f,56f));
            var hit=control.gameObject.AddComponent<Image>();hit.color=new Color(0f,0f,0f,0.001f);
            Panel(control,"Track",new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(446f,9f),new Color(0.12f,0.21f,0.3f));
            var fillArea=Rect(control,"FillArea",new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(446f,9f));
            var fill=Stretch(fillArea,"Fill");var fillGraphic=fill.gameObject.AddComponent<Image>();fillGraphic.color=Cyan;fillGraphic.raycastTarget=false;
            var handleArea=Rect(control,"HandleArea",new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(446f,44f));
            var handle=Panel(handleArea,"Handle",new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(30f,38f),Color.white);
            var slider=control.gameObject.AddComponent<Slider>();slider.minValue=min;slider.maxValue=max;slider.fillRect=fill;slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<Graphic>();
            slider.navigation=new Navigation{mode=Navigation.Mode.None};Assign(view,field,slider);return row;
        }
        private static void Heading(Transform panel,string title,string subtitle,float y=250f)
        {
            Label(panel,"Title",title,new Vector2(0.5f,0.5f),new Vector2(0f,y-10f),new Vector2(780f,135f),42f,Color.white).fontStyle=FontStyles.Bold;
            Label(panel,"Subtitle",subtitle,new Vector2(0.5f,0.5f),new Vector2(0f,y-110f),new Vector2(780f,40f),19f,Cyan);
        }
        private static RectTransform Button(Transform parent,string name,string caption,Vector2 position,Vector2 size,UnityAction action,Color color)
        {
            var rect=Panel(parent,name,new Vector2(0.5f,0.5f),position,size,color);var graphic=rect.GetComponent<RoundedPanelGraphic>();graphic.raycastTarget=true;
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=graphic;
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.2f,1.2f,1.2f);colors.pressedColor=new Color(0.65f,0.8f,0.9f);colors.fadeDuration=0.08f;button.colors=colors;
            button.navigation=new Navigation{mode=Navigation.Mode.None};UnityEventTools.AddPersistentListener(button.onClick,action);
            Label(rect,"Label",caption,new Vector2(0.5f,0.5f),Vector2.zero,size-new Vector2(24f,8f),26f,Color.white).fontStyle=FontStyles.Bold;return rect;
        }
        private static RectTransform Panel(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size,Color color)
        {var rect=Rect(parent,name,anchor,pos,size);var graphic=Get<RoundedPanelGraphic>(rect.gameObject);graphic.color=color;graphic.raycastTarget=false;return rect;}
        private static TMP_Text Label(Transform parent,string name,string text,Vector2 anchor,Vector2 pos,Vector2 size,float fontSize,Color color)
        {var label=MatchHUDSetup.Label(parent,name,anchor,pos,size,fontSize);label.text=text;label.color=color;label.textWrappingMode=TextWrappingModes.NoWrap;return label;}
        private static void MoveLabel(TMP_Text label,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,float font)
        {Place(label.rectTransform,parent,anchor,pos,size);label.fontSize=font;label.alignment=TextAlignmentOptions.Center;label.textWrappingMode=TextWrappingModes.NoWrap;label.raycastTarget=false;}
        private static RectTransform Rect(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size)
        {var rect=MatchHUDSetup.Rect(parent,name,anchor,pos,size);Place(rect,parent,anchor,pos,size);return rect;}
        private static void Place(RectTransform rect,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size)
        {rect.SetParent(parent,false);rect.anchorMin=rect.anchorMax=anchor;rect.pivot=new Vector2(0.5f,0.5f);rect.anchoredPosition=pos;rect.sizeDelta=size;rect.localScale=Vector3.one;}
        private static RectTransform Stretch(Transform parent,string name)
        {var rect=Rect(parent,name,Vector2.zero,Vector2.zero,Vector2.zero);rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;}
        private static T Get<T>(GameObject go) where T:Component=>go.GetComponent<T>()??go.AddComponent<T>();
        private static T Read<T>(UnityEngine.Object o,string name) where T:UnityEngine.Object=>(T)new SerializedObject(o).FindProperty(name).objectReferenceValue;
        private static void Assign(UnityEngine.Object target,string field,UnityEngine.Object value)=>Milestone9PrototypeSetup.Assign(target,field,value);
        private static void Bool(UnityEngine.Object target,string field,bool value)
        {var so=new SerializedObject(target);so.FindProperty(field).boolValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    }
}
