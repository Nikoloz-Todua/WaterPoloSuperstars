using System;
using System.Linq;
using TMPro;
using ToduStudios.WaterPolo.Hub;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ToduStudios.WaterPolo.Editor
{
    public static class Milestone12HubSetup
    {
        public const string BootPath="Assets/Scenes/Boot.unity",LobbyPath="Assets/Scenes/MainLobby.unity",MatchPath="Assets/Scenes/PrototypePool.unity";
        [MenuItem("Tools/Water Polo/Set Up Milestone 12 Hub")]
        public static void SetUp()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before hub setup.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save intended scene edits before hub setup; nothing discarded.");
            var original=EditorSceneManager.GetSceneManagerSetup();
            try
            {
                if(!AssetDatabase.IsValidFolder("Assets/Resources"))AssetDatabase.CreateFolder("Assets","Resources");
                var catalog=AssetDatabase.LoadAssetAtPath<HubCatalog>("Assets/Resources/HubCatalog.asset");
                if(catalog==null){catalog=ScriptableObject.CreateInstance<HubCatalog>();catalog.Seed();AssetDatabase.CreateAsset(catalog,"Assets/Resources/HubCatalog.asset");}
                CreateBoot();CreateLobby();
                var matchScene=EditorSceneManager.OpenScene(MatchPath,OpenSceneMode.Single);var match=UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
                if(match==null||UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>().Members.Count!=14)throw new InvalidOperationException("Existing full-team match required.");
                if(match.GetComponent<HubMatchBridge>()==null){match.gameObject.AddComponent<HubMatchBridge>();EditorSceneManager.MarkSceneDirty(matchScene);EditorSceneManager.SaveScene(matchScene);}
                var paths=new[]{BootPath,LobbyPath,MatchPath};var extras=EditorBuildSettings.scenes.Where(x=>!paths.Contains(x.path)).ToArray();
                EditorBuildSettings.scenes=paths.Select(x=>new EditorBuildSettingsScene(x,true)).Concat(extras).ToArray();
                EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(BootPath);
                AssetDatabase.SaveAssets();Debug.Log("M12 hub configured. Boot is Editor entry; direct match testing remains available from Tools > Water Polo > Play PrototypePool Directly.");
            }
            finally{EditorSceneManager.RestoreSceneManagerSetup(original);}
        }
        static RectTransform Canvas(string name)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=go.GetComponent<UnityEngine.Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            var camera=new GameObject("MenuCamera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=HubWidgets.Navy;camera.cullingMask=0;
            HubWidgets.Panel(go.transform,"Ocean",0,0,1,1,HubWidgets.Navy);
            for(int i=0;i<6;i++){var stripe=HubWidgets.Panel(go.transform,"Current"+i,-.15f,.05f+i*.18f,1.3f,.06f,new Color(.04f,.3f,.42f,.22f));stripe.localRotation=Quaternion.Euler(0,0,-12);}
            var safe=HubWidgets.Rect(go.transform,"SafeArea",0,0,1,1);safe.gameObject.AddComponent<SafeAreaLayout>();return safe;
        }
        static void CreateBoot()
        {
            if(RepairTypography(BootPath))return;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var safe=Canvas("BootCanvas");
            HubWidgets.Text(safe,"Title","WATER POLO\nSUPERSTARS",.2f,.42f,.6f,.37f,82,Color.white,TextAlignmentOptions.Center).fontStyle=FontStyles.Bold;
            HubWidgets.Text(safe,"Brand","TODUSTUDIOS",.25f,.3f,.5f,.09f,28,HubWidgets.Cyan,TextAlignmentOptions.Center);
            var indicator=HubWidgets.Panel(safe,"Loading",.487f,.2f,.026f,.045f,HubWidgets.Gold);
            var status=HubWidgets.Text(safe,"Status","Preparing your next wave…",.15f,.1f,.7f,.08f,26,HubWidgets.Muted,TextAlignmentOptions.Center);
            var boot=safe.gameObject.AddComponent<BootScreen>();boot.status=status;boot.indicator=indicator;EditorSceneManager.SaveScene(scene,BootPath);
        }
        static void CreateLobby()
        {
            if(RepairTypography(LobbyPath))return;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var safe=Canvas("LobbyCanvas");
            var top=HubWidgets.Panel(safe,"ProfileBar",.02f,.895f,.96f,.085f,HubWidgets.Card);
            var header=HubWidgets.Text(top,"Balances","CAPTAIN   /   LV 1          0 TROPHIES          600 COINS     20 GEMS",.035f,.06f,.85f,.88f,27);
            HubWidgets.Button(top,"Nav_Settings","SETTINGS",.88f,.1f,.1f,.8f,null);
            var nav=HubWidgets.Panel(safe,"Navigation",.02f,.07f,.13f,.8f,HubWidgets.Card);
            string[] pages={"Lobby","Shop","Collection","Tactics","Season Pass","Trophies","Profile","Clubs","Friends"};
            for(int i=0;i<pages.Length;i++)HubWidgets.Button(nav,"Nav_"+pages[i],pages[i].ToUpperInvariant(),.06f,.9f-i*.1f,.88f,.085f,null,i==0?HubWidgets.Palette[0]:HubWidgets.Card);
            var content=HubWidgets.Rect(safe,"Page",.175f,.075f,.805f,.79f);var fade=content.gameObject.AddComponent<CanvasGroup>();
            // Saved Edit Mode presentation; runtime supplies authoritative profile content and callbacks.
            HubWidgets.Text(content,"Brand","WATER POLO\nSUPERSTARS",.02f,.65f,.5f,.3f,60).fontStyle=FontStyles.Bold;
            HubWidgets.Text(content,"Studio","TODUSTUDIOS / OWN THE CURRENT",.02f,.55f,.5f,.1f,24,HubWidgets.Cyan);
            var art=HubWidgets.Rect(content,"FeaturedAthlete",.52f,.2f,.42f,.72f);HubWidgets.Athlete(art,HubWidgets.Palette[0],HubWidgets.Palette[1]);
            HubWidgets.Button(content,"Nav_Play","PLAY  ›",.02f,.26f,.43f,.16f,null,HubWidgets.Palette[0]);
            var toast=HubWidgets.Text(safe,"Feedback","OFFLINE FIRST • YOUR NEXT WAVE STARTS HERE",.18f,.012f,.8f,.045f,23,HubWidgets.Gold);
            var modal=HubWidgets.Panel(safe,"Modal",0,0,1,1,new Color(.005f,.02f,.05f,.94f));modal.GetComponent<RoundedPanelGraphic>().raycastTarget=true;modal.gameObject.SetActive(false);
            safe.gameObject.AddComponent<GamePreferences>();var view=safe.gameObject.AddComponent<HubView>();view.content=content;view.modal=modal;view.header=header;view.toast=toast;view.pageFade=fade;
            EditorSceneManager.SaveScene(scene,LobbyPath);
        }
        [MenuItem("Tools/Water Polo/Play Hub Entry")]
        public static void PlayHub(){EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(BootPath);EditorApplication.isPlaying=true;}
        [MenuItem("Tools/Water Polo/Play PrototypePool Directly")]
        public static void PlayMatchDirectly()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty){Debug.LogWarning("Save intended edits before direct match verification.");return;}
            EditorSceneManager.playModeStartScene=null;EditorSceneManager.OpenScene(MatchPath);EditorApplication.isPlaying=true;
        }
        [MenuItem("Tools/Water Polo/Use Boot as Editor Play Entry")]
        public static void SetBootEntry()=>EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(BootPath);
        static bool RepairTypography(string path)
        {
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(path)==null)return false;
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);bool dirty=false;
            foreach(var root in scene.GetRootGameObjects())foreach(var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if(label.overflowMode==TextOverflowModes.Ellipsis){label.overflowMode=TextOverflowModes.Truncate;dirty=true;}
            }
            if(dirty){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}return true;
        }
    }
}
