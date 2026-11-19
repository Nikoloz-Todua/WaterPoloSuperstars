using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using ToduStudios.WaterPolo.WaterVisuals;

namespace ToduStudios.WaterPolo.Editor
{
    /// <summary>Upgrades only the existing arena Canvas. Native scene recovery precedes edits; repeat calls preserve tuning.</summary>
    public static partial class MatchUxSetup
    {
        public const string Recovery="Assets/Scenes/Recovery/OlympicMatchArena_PreMatchUX.unity";
        static readonly Color Navy=new Color(.018f,.043f,.082f,.95f),Card=new Color(.045f,.1f,.17f,.96f),Cyan=new Color(.22f,.85f,1f),Muted=new Color(.65f,.76f,.85f),Blue=new Color(.035f,.36f,.57f,.96f),Gold=new Color(1f,.78f,.3f);
        [MenuItem("Tools/Water Polo/Recovery/Match UX/Set Up Milestone 16 Match Interface")]
        public static void SetUp()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path!=OlympicArenaSetup.ScenePath)
            {Debug.LogWarning("Open the saved OlympicMatchArena and stop Play Mode before Match UX setup. Nothing changed.");return;}
            var match=UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
            var hud=UnityEngine.Object.FindAnyObjectByType<MatchHUD>();
            if(match==null || roster==null || roster.Members.Count!=14 || hud==null)throw new InvalidOperationException("The existing fourteen-athlete arena and HUD are required.");
            var canvas=hud.GetComponentInParent<Canvas>();
            if(canvas.GetComponent<MatchControlPresentation>()!=null)
            {
                var landing=canvas.transform.Find("MenuOverlay/MenuSafeArea/LandingPanel");
                if(landing.Find("Close")==null)
                {
                    Close(landing,canvas.GetComponent<MatchMenuController>().StartMatch);
                    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                }
                Debug.Log("M16 interface already configured; authored layout and tuning preserved.");return;
            }
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Recovery)==null && !AssetDatabase.CopyAsset(scene.path,Recovery))throw new InvalidOperationException("Could not create native scene recovery; no HUD changes made.");
            var safe=canvas.transform.Find("MatchSafeArea");var menus=canvas.GetComponent<MatchMenuController>();var prefs=canvas.GetComponent<GamePreferences>();var controls=canvas.GetComponent<MobileMatchControls>();
            var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=1f;
            foreach(var g in canvas.GetComponentsInChildren<RoundedPanelGraphic>(true))if(g.GetComponent<CanvasRenderer>()==null)g.gameObject.AddComponent<CanvasRenderer>();
            var oldPlates=canvas.GetComponent<ArenaHudReadability>();
            if(oldPlates!=null){oldPlates.enabled=false;foreach(var plate in oldPlates.plates)if(plate!=null)plate.SetActive(false);}
            Hide(safe,"QuickControls");Hide(safe,"StrategyCard");Hide(safe,"RestartInstruction");
            // Reuse all existing score/timer observers and graphics, with separate fields for period and clock.
            var score=Panel(safe,"ScoreCard",new Vector2(.5f,1),new Vector2(0,-100),new Vector2(700,158),Navy);
            Move(Read<TMP_Text>(canvas.GetComponent<PrototypeScoreboard>(),"scoreLabel").rectTransform,score,new Vector2(.5f,.5f),new Vector2(0,35),new Vector2(190,66));
            Style(Read<TMP_Text>(canvas.GetComponent<PrototypeScoreboard>(),"scoreLabel"),48,Color.white);
            Text(score,"BlueTeam","BLUE",new Vector2(-230,35),new Vector2(200,48),28,Cyan).fontStyle=FontStyles.Bold;
            Text(score,"WhiteTeam","WHITE",new Vector2(230,35),new Vector2(200,48),28,Color.white).fontStyle=FontStyles.Bold;
            Panel(score,"BlueRule",Vector2.one*.5f,new Vector2(-113,35),new Vector2(2,38),Cyan);
            Panel(score,"WhiteRule",Vector2.one*.5f,new Vector2(113,35),new Vector2(2,38),new Color(.6f,.7f,.8f,.4f));
            Panel(score,"Divider",Vector2.one*.5f,new Vector2(0,-7),new Vector2(648,1),new Color(.4f,.6f,.75f,.25f));
            var period=Text(score,"Period","Q1",new Vector2(-277,-29),new Vector2(72,36),25,Cyan);Assign(hud,"periodLabel",period);
            var clock=Read<TMP_Text>(hud,"clockLabel");Move(clock.rectTransform,score,Vector2.one*.5f,new Vector2(-173,-29),new Vector2(130,40));Style(clock,32,Color.white);
            Text(score,"ShotCaption","SHOT",new Vector2(20,-29),new Vector2(80,32),18,Muted);
            var shot=Read<TMP_Text>(hud,"shotClockLabel");Move(shot.rectTransform,score,Vector2.one*.5f,new Vector2(100,-29),new Vector2(72,40));Style(shot,30,Color.white);
            var state=Text(score,"ClockState","LIVE",new Vector2(250,-29),new Vector2(135,32),17,Gold);Assign(hud,"stateLabel",state);
            var possession=Read<TMP_Text>(hud,"possessionLabel");Move(possession.rectTransform,score,Vector2.one*.5f,new Vector2(0,-64),new Vector2(600,25));Style(possession,16,Cyan);
            Bool(canvas.GetComponent<PrototypeScoreboard>(),"unifiedNotifications",true);
            // The old feed and independent GOAL label remain serialized for legacy recovery, inactive in production.
            Hide(safe,"EventCard");Hide(safe,"GoalLabel");
            var banner=Panel(safe,"MatchNotification",new Vector2(.5f,1),new Vector2(0,-238),new Vector2(650,92),Navy);
            var group=Get<CanvasGroup>(banner.gameObject);group.alpha=0;group.blocksRaycasts=group.interactable=false;
            var accent=Panel(banner,"Accent",Vector2.one*.5f,new Vector2(-316,0),new Vector2(4,54),Cyan).GetComponent<RoundedPanelGraphic>();
            var heading=Text(banner,"Title","",new Vector2(0,19),new Vector2(590,34),27,Cyan);heading.fontStyle=FontStyles.Bold;
            var context=Text(banner,"Context","",new Vector2(0,-19),new Vector2(610,28),19,Color.white);
            var notifications=canvas.gameObject.AddComponent<MatchNotificationBanner>();Assign(notifications,"match",match);Assign(notifications,"menus",menus);Assign(notifications,"group",group);Assign(notifications,"title",heading);Assign(notifications,"context",context);Assign(notifications,"accent",accent);
            Assign(canvas.GetComponent<MatchAtmosphere>(),"notifications",notifications);
            // Compact side status, clear of the pool centre and thumb hitboxes.
            var athlete=safe.Find("AthleteCard");Move((RectTransform)athlete,safe,Vector2.zero,new Vector2(194,366),new Vector2(324,76));athlete.GetComponent<RoundedPanelGraphic>().color=Navy;
            var stamina=canvas.GetComponent<ActiveStaminaHUD>();var athleteLabel=Read<TMP_Text>(stamina,"label");Move(athleteLabel.rectTransform,athlete,Vector2.one*.5f,new Vector2(0,12),new Vector2(300,30));Style(athleteLabel,20,Color.white);
            Hide(athlete,"EnergyHint");Move((RectTransform)Read<UnityEngine.UI.Image>(stamina,"fill").transform.parent,athlete,Vector2.one*.5f,new Vector2(0,-19),new Vector2(280,8));
            var rules=safe.Find("RulesCard");Move((RectTransform)rules,safe,new Vector2(0,1),new Vector2(214,-70),new Vector2(360,96));rules.GetComponent<RoundedPanelGraphic>().color=Navy;
            var rulesText=Read<TMP_Text>(canvas.GetComponent<MatchAtmosphere>(),"rulesLabel");Move(rulesText.rectTransform,rules,Vector2.one*.5f,Vector2.zero,new Vector2(336,78));Style(rulesText,18,Muted);rulesText.textWrappingMode=TextWrappingModes.Normal;
            // Two independently scaled thumb groups preserve meaningful hitboxes and never overlap each other.
            var root=safe.Find("MobileControls");var left=Rect(root,"LeftThumb",Vector2.zero,Vector2.zero,Vector2.zero);left.pivot=Vector2.zero;
            var right=Rect(root,"RightThumb",new Vector2(1,0),Vector2.zero,Vector2.zero);right.pivot=new Vector2(1,0);
            var joystick=root.Find("SwimJoystick");Move((RectTransform)joystick,left,Vector2.zero,new Vector2(180,170),new Vector2(240,240));joystick.GetComponent<RoundedPanelGraphic>().color=new Color(.025f,.09f,.15f,.82f);
            Move((RectTransform)joystick.Find("InnerRing"),joystick,Vector2.one*.5f,Vector2.zero,new Vector2(166,166));Move((RectTransform)joystick.Find("Handle"),joystick,Vector2.one*.5f,Vector2.zero,new Vector2(88,88));
            Move((RectTransform)root.Find("SwimHint"),left,Vector2.zero,new Vector2(180,24),new Vector2(240,28));
            foreach(var b in root.GetComponentsInChildren<GameplayActionButton>(true))
            {
                var action=b.Action;bool primary=action==MobileMatchAction.Shoot || action==MobileMatchAction.Steal;
                Vector2 position=primary?new Vector2(-128,215):action==MobileMatchAction.Pass?new Vector2(-310,124):action==MobileMatchAction.Lob?new Vector2(-457,124):action==MobileMatchAction.Sprint?new Vector2(-310,266):action==MobileMatchAction.Switch?new Vector2(-457,266):action==MobileMatchAction.Strategy?new Vector2(-398,385):new Vector2(-128,76);
                Vector2 size=primary?new Vector2(176,176):action==MobileMatchAction.Strategy?new Vector2(280,56):action==MobileMatchAction.ShotMode || action==MobileMatchAction.Collect?new Vector2(154,66):new Vector2(126,104);
                Move((RectTransform)b.transform,right,new Vector2(1,0),position,size);
                var graphic=b.GetComponent<RoundedPanelGraphic>();graphic.color=primary?Blue:Card;Float(graphic,"radius",primary?88:22);
                var caption=Read<TMP_Text>(b,"label");Move(caption.rectTransform,b.transform,Vector2.one*.5f,Vector2.zero,size-new Vector2(12,8));Style(caption,primary?27:action==MobileMatchAction.Strategy?19:22,Color.white);
            }
            Bool(controls,"refinedLayout",true);
            var power=Read<GameObject>(canvas.GetComponent<ShotPowerBar>(),"panel");Move((RectTransform)power.transform,right,new Vector2(1,0),new Vector2(-128,330),new Vector2(168,12));
            var powerCaption=power.transform.Find("PowerCaption").GetComponent<TMP_Text>();Move(powerCaption.rectTransform,power.transform,Vector2.one*.5f,new Vector2(0,22),new Vector2(168,26));Style(powerCaption,16,Gold);powerCaption.text="SHOT POWER";
            // Pause retains all existing authoritative actions and confirmation transaction paths.
            var content=canvas.transform.Find("MenuOverlay/MenuSafeArea");
            var pause=content.Find("PausePanel");Move((RectTransform)pause,content,Vector2.one*.5f,Vector2.zero,new Vector2(650,830));pause.GetComponent<RoundedPanelGraphic>().color=Navy;
            Text(pause,"Title","MATCH PAUSED",new Vector2(0,327),new Vector2(540,64),38,Color.white);
            Text(pause,"Subtitle","WATER POLO SUPERSTARS",new Vector2(0,270),new Vector2(540,34),18,Cyan);
            Assign(menus,"pauseSummary",pause.Find("Subtitle").GetComponent<TMP_Text>());
            Button(pause,"Resume","RESUME MATCH",new Vector2(0,175),new Vector2(530,80),menus.Resume,Blue);
            Button(pause,"Settings","SETTINGS",new Vector2(0,77),new Vector2(530,76),menus.OpenSettings,Card);
            Button(pause,"ControlsButton","CONTROLS",new Vector2(0,-17),new Vector2(530,76),menus.OpenControls,Card);
            Button(pause,"Restart","RESTART MATCH",new Vector2(0,-111),new Vector2(530,76),menus.AskRestart,Card);
            Button(pause,"Exit","EXIT TO MAIN MENU",new Vector2(0,-205),new Vector2(530,76),menus.AskExit,Card);
            Text(pause,"Hint","ESC / START TO RESUME",new Vector2(0,-334),new Vector2(540,32),18,Muted);Close(pause,menus.Resume);
            var controlsPage=Panel(content,"ControlsPanel",Vector2.one*.5f,Vector2.zero,new Vector2(1120,820),Navy);
            Text(controlsPage,"Title","MATCH CONTROLS",new Vector2(0,333),new Vector2(950,60),38,Color.white);
            Text(controlsPage,"Subtitle","Aim by facing with movement. Passing transfers control on reception.",new Vector2(0,270),new Vector2(1010,40),21,Muted);
            Text(controlsPage,"TouchHeading","TOUCH",new Vector2(-270,199),new Vector2(460,42),25,Cyan);
            Text(controlsPage,"KeyboardHeading","KEYBOARD / CONTROLLER",new Vector2(270,199),new Vector2(490,42),25,Cyan);
            var touch=Text(controlsPage,"Touch","Drag joystick   Swim / aim\nHold SPRINT   Faster swimming\nHold / release SHOOT   Charge / fire\nPASS / LOB   Flat / aerial pass\nSWITCH   Change controlled athlete\nSTEAL   Challenge nearby carrier\nCOLLECT   Pick up a loose ball\nNORMAL / SKIP / HIGH   Shot type\nDEFENSE   Toggle Press / Zone",new Vector2(-270,-13),new Vector2(470,360),22,Color.white);touch.alignment=TextAlignmentOptions.Left;touch.lineSpacing=10;
            var keys=Text(controlsPage,"Keyboard","WASD / arrows / left stick   Swim\nSHIFT   Sprint     C   Switch\nSPACE / South   Hold / release shot\nB / North   Pass     L   Lob\nE / West   Collect     F   Steal\nG   Hold (foul risk)     Z   Defense\nQ + SPACE   Skip     R + SPACE   High\nESC / Start   Pause / Back\nKeeper control follows possession",new Vector2(270,-13),new Vector2(490,360),22,Color.white);keys.alignment=TextAlignmentOptions.Left;keys.lineSpacing=10;
            Text(controlsPage,"Tip","Center restart: wait for the whistle, then PASS. Release outside a held button to cancel.",new Vector2(0,-246),new Vector2(1040,50),20,Gold);
            Button(controlsPage,"Back","BACK TO MATCH MENU",new Vector2(0,-335),new Vector2(480,72),menus.Cancel,Blue);Close(controlsPage,menus.Cancel);Assign(menus,"controlsPanel",controlsPage.gameObject);controlsPage.gameObject.SetActive(false);
            var settings=content.Find("SettingsPanel");Hide(settings,"Controls");Close(settings,menus.Cancel);
            var settingsView=settings.GetComponent<MatchSettingsView>();
            Text(settings,"ControlHeading","TOUCH CONTROLS",new Vector2(0,-210),new Vector2(1000,35),22,Cyan);
            var opacity=Opacity(settings);Assign(settingsView,"controlOpacity",opacity);Assign(settingsView,"controlOpacityValue",opacity.transform.parent.Find("Value").GetComponent<TMP_Text>());
            var sizeValue=Text(settings,"ControlSizeValue","NORMAL",new Vector2(280,-258),new Vector2(470,32),20,Cyan);Assign(settingsView,"controlSizeValue",sizeValue);
            Button(settings,"SmallControls","SMALL",new Vector2(120,-315),new Vector2(145,62),settingsView.SmallControls,Card);
            Button(settings,"NormalControls","NORMAL",new Vector2(280,-315),new Vector2(155,62),settingsView.NormalControls,Card);
            Button(settings,"LargeControls","LARGE",new Vector2(440,-315),new Vector2(145,62),settingsView.LargeControls,Card);
            Text(settings,"GraphicsNote","Arena lighting, water, effects and crowd detail.\nChoose a balance of detail and performance.",new Vector2(280,-158),new Vector2(500,70),18,Muted);
            foreach(string n in new[]{"ConfirmationPanel","TransitionPanel","LandingPanel"}){content.Find(n).GetComponent<RoundedPanelGraphic>().color=Navy;}
            Close(content.Find("ConfirmationPanel"),menus.Cancel);Close(content.Find("TransitionPanel"),menus.Continue);
            Close(content.Find("LandingPanel"),menus.StartMatch);
            var presentation=canvas.gameObject.AddComponent<MatchControlPresentation>();Assign(presentation,"preferences",prefs);Assign(presentation,"match",match);Assign(presentation,"selection",UnityEngine.Object.FindAnyObjectByType<PlayerSelection>());Assign(presentation,"menus",menus);
            Assign(presentation,"leftControls",Get<CanvasGroup>(left.gameObject));Assign(presentation,"rightControls",Get<CanvasGroup>(right.gameObject));
            Array(presentation,"athletes",roster.Members.Cast<UnityEngine.Object>().ToArray());Array(presentation,"rings",roster.Members.Select(p=>Read<GameObject>(p,"selectionIndicator")?.transform).Cast<UnityEngine.Object>().ToArray());Array(presentation,"gameplayInformation",new UnityEngine.Object[]{athlete.gameObject,rules.gameObject,score.gameObject});
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("Milestone 16 Match UX saved. Native recovery retained; original world/gameplay objects and one Canvas/EventSystem preserved.");
        }
        static UnityEngine.UI.Slider Opacity(Transform settings)
        {
            var row=Rect(settings,"ControlOpacityRow",Vector2.one*.5f,new Vector2(-280,-288),new Vector2(480,100));
            Text(row,"Caption","CONTROL OPACITY",new Vector2(-70,30),new Vector2(330,32),20,Color.white);
            Text(row,"Value","88%",new Vector2(190,30),new Vector2(100,32),20,Cyan);
            var rect=Rect(row,"Slider",Vector2.one*.5f,new Vector2(0,-20),new Vector2(470,56));var hit=Get<UnityEngine.UI.Image>(rect.gameObject);hit.color=new Color(0,0,0,.001f);
            Panel(rect,"Track",Vector2.one*.5f,Vector2.zero,new Vector2(446,9),Card);
            var area=Rect(rect,"HandleArea",Vector2.one*.5f,Vector2.zero,new Vector2(446,44));var handle=Panel(area,"Handle",Vector2.one*.5f,Vector2.zero,new Vector2(30,38),Cyan);
            var slider=Get<UnityEngine.UI.Slider>(rect.gameObject);slider.minValue=.4f;slider.maxValue=1;slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<RoundedPanelGraphic>();slider.navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.None};return slider;
        }
        [MenuItem("Tools/Water Polo/Match UX/Set Up Milestone 22 Substitutions")]
        public static void SetUpSubstitutions()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path != OlympicArenaSetup.ScenePath)
                throw new InvalidOperationException("Open the saved OlympicMatchArena in Edit Mode.");
            var menus = UnityEngine.Object.FindAnyObjectByType<MatchMenuController>();
            var content = menus.transform.Find("MenuOverlay/MenuSafeArea");
            var safe = menus.transform.Find("MatchSafeArea");
            if (safe.Find("PenaltyAim") == null)
            {
                var mark = Text(safe,"PenaltyAim","+",Vector2.zero,new Vector2(38,38),26,Gold);
                mark.raycastTarget = false;
                var aim = mark.gameObject.AddComponent<PenaltyAimIndicator>();
                aim.match = UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
                aim.selection = UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();
                aim.lens = aim.match.GameplayView.GetComponent<Camera>();
                aim.mark = mark;
                mark.enabled = false;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            var settings = content.Find("SettingsPanel");
            if (settings.Find("CameraHeightRow") == null)
            {
                var row = UnityEngine.Object.Instantiate(settings.Find("zoomRow").gameObject,settings).GetComponent<RectTransform>();
                row.name = "CameraHeightRow";
                row.anchoredPosition = new Vector2(280,-297);
                var slider = row.GetComponentInChildren<UnityEngine.UI.Slider>(true);
                slider.minValue = .8f; slider.maxValue = 1.25f;
                var view = settings.GetComponent<MatchSettingsView>();
                Assign(view,"height",slider);
                foreach (var label in row.GetComponentsInChildren<TMP_Text>(true))
                    if (label.text.Contains("DISTANCE")) label.text="SIDELINE HEIGHT"; else Assign(view,"heightValue",label);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            if (content.Find("SubstitutionPanel") != null) return;
            var pause = content.Find("PausePanel");
            Button(pause, "Substitutions", "SUBSTITUTIONS", new Vector2(0,-290), new Vector2(530,64), menus.OpenSubstitutions, Card);
            var panel = Panel(content,"SubstitutionPanel",Vector2.one*.5f,Vector2.zero,new Vector2(760,680),Navy);
            Text(panel,"Title","SUBSTITUTIONS",new Vector2(0,235),new Vector2(570,60),38,Color.white);
            var outgoing = Button(panel,"Outgoing","OUT",new Vector2(0,110),new Vector2(570,72),menus.NextOutgoing,Card);
            var incoming = Button(panel,"Incoming","IN",new Vector2(0,18),new Vector2(570,72),menus.NextIncoming,Card);
            var message = Text(panel,"Status","",new Vector2(0,-84),new Vector2(590,76),22,Cyan);
            Button(panel,"Confirm","CONFIRM CHANGE",new Vector2(0,-209),new Vector2(500,75),menus.ConfirmReplacement,Blue);
            Close(panel,menus.Cancel);
            Assign(menus,"outgoingLabel",outgoing.GetComponentInChildren<TMP_Text>());
            Assign(menus,"incomingLabel",incoming.GetComponentInChildren<TMP_Text>());
            Assign(menus,"substitutionMessage",message);
            Assign(menus,"substitutionPanel",panel.gameObject);
            panel.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        [MenuItem("Tools/Water Polo/Match UX/Set Up Milestone 23 Goal Replays")]
        public static void SetUpGoalReplays()
        {
            var scene=SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path!=OlympicArenaSetup.ScenePath)
                throw new InvalidOperationException("Open the saved production arena in Edit Mode.");
            var menus=UnityEngine.Object.FindAnyObjectByType<MatchMenuController>();
            if (menus.GetComponent<GoalReplayDirector>() != null)
            {
                var existing=menus.transform.Find("MatchSafeArea/GoalPresentation");
                if (existing!=null && existing.Find("AdvancePlate")==null)
                {
                    StyleAdvanceHint(existing);
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
                return;
            }
            var match=UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
            var safe=menus.transform.Find("MatchSafeArea");
            var prefs=menus.GetComponent<GamePreferences>();
            var director=menus.gameObject.AddComponent<GoalReplayDirector>();
            var overlay=new GameObject("GoalPresentation",typeof(RectTransform)).GetComponent<RectTransform>();
            overlay.SetParent(safe,false);
            overlay.anchorMin=Vector2.zero;
            overlay.anchorMax=Vector2.one;
            overlay.offsetMin=overlay.offsetMax=Vector2.zero;
            var clickArea=overlay.gameObject.AddComponent<UnityEngine.UI.Image>();
            clickArea.color=new Color(0,0,0,0);
            var advance=overlay.gameObject.AddComponent<UnityEngine.UI.Button>();
            advance.targetGraphic=clickArea;
            advance.transition=UnityEngine.UI.Selectable.Transition.None;
            advance.navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.None};
            UnityEventTools.AddPersistentListener(advance.onClick,director.Advance);

            var titleCard=Panel(overlay,"TitleCard",new Vector2(.5f,1),new Vector2(0,-75),new Vector2(530,118),Navy);
            var headline=Text(titleCard,"Headline","GOAL",new Vector2(0,27),new Vector2(500,48),34,Color.white);
            headline.fontStyle=FontStyles.Bold;
            var score=Text(titleCard,"Score","BLUE  0  :  0  WHITE",new Vector2(0,-9),new Vector2(500,35),21,Cyan);
            var detail=Text(titleCard,"Detail","Q1",new Vector2(0,-40),new Vector2(500,26),17,Muted);
            Text(overlay,"AdvanceHint","TAP TO ADVANCE",Vector2.zero,new Vector2(420,60),18,Color.white);
            StyleAdvanceHint(overlay);
            var track=Panel(overlay,"Track",new Vector2(.5f,0),new Vector2(0,70),new Vector2(340,5),new Color(.58f,.72f,.83f,.45f));
            var line=Rect(track,"Progress",new Vector2(0,.5f),Vector2.zero,new Vector2(340,5));
            line.pivot=new Vector2(0,.5f);
            var timeline=line.gameObject.AddComponent<UnityEngine.UI.Image>();
            timeline.color=Cyan;
            timeline.raycastTarget=false;
            var skip=Button(overlay,"SkipAll","SKIP ALL",Vector2.zero,new Vector2(205,56),director.SkipAll,Card);
            Move(skip,overlay,new Vector2(1,0),new Vector2(-140,66),new Vector2(205,56));
            var pause=Button(overlay,"ReplayPause","PAUSE",Vector2.zero,new Vector2(140,56),menus.OpenPause,Card);
            Move(pause,overlay,new Vector2(1,1),new Vector2(-109,-56),new Vector2(140,56));

            Assign(director,"match",match);
            Assign(director,"ball",UnityEngine.Object.FindAnyObjectByType<BallController>());
            Assign(director,"roster",UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>());
            Assign(director,"preferences",prefs);
            Assign(director,"overlay",overlay.gameObject);
            Assign(director,"headline",headline);
            Assign(director,"detail",detail);
            Assign(director,"score",score);
            Assign(director,"timeline",timeline);
            overlay.gameObject.SetActive(false);
            var settings=menus.transform.Find("MenuOverlay/MenuSafeArea/SettingsPanel");
            var toggle=Button(settings,"AutoReplay","AUTO REPLAY ON",new Vector2(340,-379),new Vector2(230,48),prefs.ToggleGoalReplays,Card);
            Assign(settings.GetComponent<MatchSettingsView>(),"autoReplayLabel",toggle.GetComponentInChildren<TMP_Text>());
            var controls=menus.transform.Find("MenuOverlay/MenuSafeArea/ControlsPanel/Tip").GetComponent<TMP_Text>();
            controls.text="Dry pass: tap PASS or B    ?    Wet pass: hold PASS, Q+B, or LB+North";
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void StyleAdvanceHint(Transform overlay)
        {
            var plate=Panel(overlay,"AdvancePlate",new Vector2(.5f,0),new Vector2(0,110),new Vector2(280,40),Navy);
            var prompt=overlay.Find("AdvanceHint").GetComponent<TMP_Text>();
            Move(prompt.rectTransform,plate,Vector2.one*.5f,Vector2.zero,new Vector2(265,40));
            prompt.fontSize=21;
        }

        static void Close(Transform parent,UnityAction action){var r=(RectTransform)parent;var b=Button(parent,"Close","X",new Vector2(r.rect.width*.5f-46,r.rect.height*.5f-46),new Vector2(60,60),action,Card);b.GetComponentInChildren<TMP_Text>().fontSize=24;}
        static void Hide(Transform parent,string name){var t=parent.Find(name);if(t!=null)t.gameObject.SetActive(false);}
        static T Get<T>(GameObject go) where T:Component{var component=go.GetComponent<T>();return component!=null?component:go.AddComponent<T>();}
        static T Read<T>(UnityEngine.Object target,string field) where T:UnityEngine.Object=>new SerializedObject(target).FindProperty(field).objectReferenceValue as T;
        static void Assign(UnityEngine.Object target,string field,UnityEngine.Object value)=>Milestone9PrototypeSetup.Assign(target,field,value);
        static void Bool(UnityEngine.Object o,string n,bool v){var s=new SerializedObject(o);s.FindProperty(n).boolValue=v;s.ApplyModifiedPropertiesWithoutUndo();}
        static void Float(UnityEngine.Object o,string n,float v){var s=new SerializedObject(o);s.FindProperty(n).floatValue=v;s.ApplyModifiedPropertiesWithoutUndo();}
        static void Array(UnityEngine.Object o,string n,UnityEngine.Object[] values){var s=new SerializedObject(o);var p=s.FindProperty(n);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];s.ApplyModifiedPropertiesWithoutUndo();}
        static void Move(RectTransform r,Transform parent,Vector2 anchor,Vector2 position,Vector2 size){r.SetParent(parent,false);r.anchorMin=r.anchorMax=anchor;r.pivot=Vector2.one*.5f;r.anchoredPosition=position;r.sizeDelta=size;}
        static RectTransform Rect(Transform parent,string n,Vector2 anchor,Vector2 pos,Vector2 size){var r=parent.Find(n) as RectTransform;if(r==null)r=new GameObject(n,typeof(RectTransform)).GetComponent<RectTransform>();Move(r,parent,anchor,pos,size);return r;}
        static RectTransform Panel(Transform p,string n,Vector2 a,Vector2 pos,Vector2 size,Color c){var r=Rect(p,n,a,pos,size);var g=Get<RoundedPanelGraphic>(r.gameObject);g.color=c;g.raycastTarget=false;return r;}
        static void Style(TMP_Text t,float size,Color color){t.fontSize=size;t.color=color;t.margin=Vector4.zero;t.enableAutoSizing=false;t.textWrappingMode=TextWrappingModes.NoWrap;t.raycastTarget=false;}
        static TMP_Text Text(Transform p,string n,string caption,Vector2 pos,Vector2 size,float font,Color color){var r=Rect(p,n,Vector2.one*.5f,pos,size);var t=Get<TextMeshProUGUI>(r.gameObject);t.font=TMP_Settings.defaultFontAsset;Style(t,font,color);t.text=caption;t.alignment=TextAlignmentOptions.Center;return t;}
        static RectTransform Button(Transform p,string n,string caption,Vector2 pos,Vector2 size,UnityAction action,Color color)
        {
            var r=Panel(p,n,Vector2.one*.5f,pos,size,color);var g=r.GetComponent<RoundedPanelGraphic>();g.raycastTarget=true;var b=Get<UnityEngine.UI.Button>(r.gameObject);b.targetGraphic=g;
            b.navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.None};var c=b.colors;c.normalColor=Color.white;c.highlightedColor=new Color(1.15f,1.15f,1.15f);c.pressedColor=new Color(.65f,.85f,.95f);c.fadeDuration=.08f;b.colors=c;
            for(int i=b.onClick.GetPersistentEventCount()-1;i>=0;i--)UnityEventTools.RemovePersistentListener(b.onClick,i);UnityEventTools.AddPersistentListener(b.onClick,action);
            Text(r,"Label",caption,Vector2.zero,size-new Vector2(18,8),24,Color.white).fontStyle=FontStyles.Bold;return r;
        }
    }
}
