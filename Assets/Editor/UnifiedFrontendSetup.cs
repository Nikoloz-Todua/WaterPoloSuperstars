using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using ToduStudios.WaterPolo.Hub;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace ToduStudios.WaterPolo.Editor
{
    public static class UnifiedFrontendSetup
    {
        public const string RecoveryPath="Assets/Scenes/Recovery/MainLobby_PreUnifiedFrontend.unity";
        public const string ArtPath="Assets/UI/MainMenu/FrontendArt.asset";
        public static readonly Dictionary<string,string> ArtFiles=new Dictionary<string,string>{
            {"division1","Assets/Art/UI/GameModes/Cards/mode_division_1.png"},
            {"premiumLeague","Assets/Art/UI/GameModes/Cards/mode_premium_league.png"},
            {"continentalCup","Assets/Art/UI/GameModes/Cards/mode_continental_cup.png"},
            {"worldChampionsLeague","Assets/Art/UI/GameModes/Cards/mode_world_champions_league.png"},
            {"luckyShot","Assets/Art/UI/GameModes/Cards/mode_lucky_shot.png"},
            {"training","Assets/Art/UI/GameModes/Cards/mode_training.png"},
            {"gameModesBackground","Assets/Art/UI/GameModes/Backgrounds/bg_game_modes.png"},
            {"battlePassBackground","Assets/Art/UI/BattlePass/Backgrounds/bg_battle_pass_purple.png"}};
        [MenuItem("Tools/Water Polo/Main Lobby/Set Up Unified Frontend")]
        public static void SetUp()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before frontend setup.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save intended scene edits first; setup discards nothing.");
            foreach(string path in ArtFiles.Values)if(!File.Exists(path))throw new FileNotFoundException("Supplied frontend image missing",path);
            var scene=EditorSceneManager.OpenScene(MainMenuSetup.ScenePath,OpenSceneMode.Single);var hub=Object.FindAnyObjectByType<HubView>();
            if(hub==null||hub.home==null)throw new InvalidOperationException("Existing asset-driven home is required.");
            if(hub.shell!=null)
            {
                if(hub.shell.art==null||typeof(FrontendArt).GetFields().Where(f=>f.FieldType==typeof(Sprite)).Any(f=>f.GetValue(hub.shell.art)==null))throw new InvalidOperationException("Inspect missing frontend references before repair.");
                return;
            }
            Directory.CreateDirectory("Logs/Frontend");
            if(!File.Exists("Logs/Frontend/Preservation-before.json"))
            {
                var paths=new[]{MainMenuSetup.ScenePath,"Assets/Scenes/Boot.unity","Assets/Scenes/OlympicMatchArena.unity","Assets/Resources/HubCatalog.asset","ProjectSettings/EditorBuildSettings.asset"}.Concat(Directory.GetFiles("Assets/Art/UI","*.png",SearchOption.AllDirectories).Select(p=>p.Replace('\\','/')));
                File.WriteAllText("Logs/Frontend/Preservation-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(paths.Select(p=>new{path=p,hash=Hash(p),guid=AssetDatabase.AssetPathToGUID(p)}),Newtonsoft.Json.Formatting.Indented));
            }
            if(!File.Exists(RecoveryPath)&&!AssetDatabase.CopyAsset(MainMenuSetup.ScenePath,RecoveryPath))throw new IOException("Frontend recovery copy failed.");
            var art=ImportArt();
            if(AssetDatabase.LoadAssetAtPath<DivisionCatalog>("Assets/Resources/DivisionCatalog.asset")==null)AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<DivisionCatalog>(),"Assets/Resources/DivisionCatalog.asset");
            var safe=(RectTransform)hub.transform;var canvas=hub.GetComponentInParent<Canvas>();
            var backdrop=HubWidgets.Rect(canvas.transform,"FrontendBackdrop",0,0,1,1);backdrop.SetSiblingIndex(safe.GetSiblingIndex());
            var bg=FrontendControls.Picture(backdrop,"DeveloperBackground",art.gameModesBackground,0,0,1,1);
            var fit=bg.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();fit.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=art.gameModesBackground.rect.width/art.gameModesBackground.rect.height;
            var tint=FrontendControls.Picture(backdrop,"NavyTint",null,0,0,1,1);tint.color=new Color(.008f,.02f,.06f,.24f);
            var root=HubWidgets.Rect(safe,"UnifiedFrontend",0,0,1,1);var shell=root.gameObject.AddComponent<FrontendShell>();hub.shell=shell;shell.hub=hub;shell.art=art;shell.backdrop=backdrop.gameObject;shell.background=bg;shell.backgroundFit=fit;shell.tint=tint;
            var bar=HubWidgets.Panel(root,"Header",.015f,.865f,.97f,.12f,new Color(.012f,.032f,.085f,.94f));
            shell.close=FrontendControls.Back(bar,"Back",.008f,.11f,.055f,.78f,null);
            FrontendControls.Picture(bar,"ProfileIcon",hub.home.art.profile,.078f,.1f,.052f,.8f);
            shell.profile=HubWidgets.Text(bar,"PlayerSummary","PLAYER",.14f,.13f,.17f,.74f,24);
            shell.title=HubWidgets.Text(bar,"PageTitle","GAME MODE",.315f,.12f,.32f,.76f,40,Color.white,TextAlignmentOptions.Center);shell.title.fontStyle=FontStyles.Bold;
            shell.diamonds=Currency(bar,"Diamonds",hub.home.art.diamond,.655f);
            shell.coins=Currency(bar,"Coins",hub.home.art.coin,.826f);
            hub.content.anchorMin=new Vector2(.025f,.035f);hub.content.anchorMax=new Vector2(.975f,.825f);hub.content.offsetMin=hub.content.offsetMax=Vector2.zero;
            HubWidgets.Clear(hub.content);
            // Retire only the old visual/sidebar objects. Every service and page action is retained.
            if(hub.legacyNavigation!=null)Object.DestroyImmediate(hub.legacyNavigation);
            if(hub.legacyProfileBar!=null)Object.DestroyImmediate(hub.legacyProfileBar);
            hub.legacyNavigation=hub.legacyProfileBar=null;hub.header=shell.profile;
            var toast=hub.toast.rectTransform;toast.anchorMin=new Vector2(.27f,.828f);toast.anchorMax=new Vector2(.73f,.862f);toast.offsetMin=toast.offsetMax=Vector2.zero;
            root.SetSiblingIndex(hub.modal.GetSiblingIndex());hub.modal.SetAsLastSibling();
            root.gameObject.SetActive(false);backdrop.gameObject.SetActive(false);hub.content.gameObject.SetActive(false);hub.home.gameObject.SetActive(true);hub.home.backdrop.SetActive(true);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("Unified frontend saved. Tools > Water Polo > Play Hub Entry. Original home, gameplay, profile balances and pack inventory retained.");
        }
        static TMP_Text Currency(RectTransform parent,string name,Sprite icon,float x)
        {
            var p=HubWidgets.Panel(parent,name,x,.14f,.16f,.72f,new Color(.006f,.018f,.045f,.9f));FrontendControls.Picture(p,"Icon",icon,.015f,.08f,.28f,.84f);
            return HubWidgets.Text(p,"Balance","0",.30f,.08f,.67f,.84f,30,Color.white,TextAlignmentOptions.Center);
        }
        static FrontendArt ImportArt()
        {
            var art=AssetDatabase.LoadAssetAtPath<FrontendArt>(ArtPath);if(art==null){art=ScriptableObject.CreateInstance<FrontendArt>();AssetDatabase.CreateAsset(art,ArtPath);}
            var report=new List<object>();
            foreach(var entry in ArtFiles)
            {
                string path=entry.Value;bool background=entry.Key.EndsWith("Background");
                var source=new Texture2D(2,2);source.LoadImage(File.ReadAllBytes(path));int width=source.width,height=source.height;Object.DestroyImmediate(source);
                var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.alphaSource=TextureImporterAlphaSource.FromInput;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.isReadable=false;imp.npotScale=TextureImporterNPOTScale.None;imp.wrapMode=TextureWrapMode.Clamp;imp.filterMode=FilterMode.Bilinear;imp.sRGBTexture=true;
                int maximum=background?2048:1024;imp.maxTextureSize=maximum;imp.textureCompression=TextureImporterCompression.CompressedHQ;
                var settings=new TextureImporterSettings();imp.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteGenerateFallbackPhysicsShape=false;imp.SetTextureSettings(settings);
                foreach(string platform in new[]{"Standalone","Android","iPhone"}){var p=imp.GetPlatformTextureSettings(platform);p.name=platform;p.overridden=true;p.maxTextureSize=maximum;p.compressionQuality=100;p.format=platform=="Standalone"?TextureImporterFormat.BC7:TextureImporterFormat.ASTC_4x4;imp.SetPlatformTextureSettings(p);}
                imp.SaveAndReimport();typeof(FrontendArt).GetField(entry.Key).SetValue(art,AssetDatabase.LoadAssetAtPath<Sprite>(path));report.Add(new{path,width,height,maximum,opaqueSource=true,sourcePixelsUnchanged=true});
            }
            EditorUtility.SetDirty(art);File.WriteAllText("Logs/Frontend/Sprite-import-report.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));return art;
        }
        static string Hash(string path){using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));}
    }
}
