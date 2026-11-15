using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using ToduStudios.WaterPolo.Hub;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ToduStudios.WaterPolo.Editor
{
    public static class MainMenuSetup
    {
        public const string ScenePath = "Assets/Scenes/MainLobby.unity";
        public const string RecoveryPath = "Assets/Scenes/Recovery/MainLobby_PreAssetHome.unity";
        public const string ArtRoot = "Assets/Art/UI/MainMenu/";
        public const string CatalogPath = "Assets/UI/MainMenu/MainMenuArt.asset";
        public static readonly Dictionary<string,string> Assets = new Dictionary<string,string>
        {
            {"ranking","Buttons/btn_ranking.png"},{"shop","Buttons/btn_shop.png"},{"team","Buttons/btn_team.png"},
            {"battlePass","Buttons/btn_battle_pass.png"},{"missions","Buttons/btn_missions.png"},{"worldTournament","Buttons/btn_world_tournament.png"},
            {"friends","Buttons/btn_friends.png"},{"clubs","Buttons/btn_clubs.png"},{"play","Buttons/btn_play.png"},
            {"coin","Icons/icon_coin.png"},{"diamond","Icons/icon_diamond.png"},{"settings","Icons/icon_settings.png"},{"messages","Icons/icon_messages.png"},{"plus","Icons/icon_plus.png"},
            {"commonPack","Cards/card_common.png"},{"rarePack","Cards/card_rare.png"},{"epicPack","Cards/card_epic.png"},{"legendaryPack","Cards/card_legendary.png"},
            {"mascot","Brand/logo_mascot.png"},{"profile","Profile/profile_default.png"},{"background","Backgrounds/bg_main_menu.png"}
        };

        [MenuItem("Tools/Water Polo/Main Lobby/Set Up Asset Home Screen")]
        public static void SetUp()
        {
            RequireSaved();
            ConfigureLandscape();
            foreach(var path in Assets.Values) if(!File.Exists(ArtRoot+path)) throw new FileNotFoundException("Required developer art is missing",ArtRoot+path);
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var existing=Object.FindAnyObjectByType<MainMenuHomeView>(FindObjectsInactive.Include);
            if(existing!=null)
            {
                if(existing.art==null || existing.slots.Length!=4 || existing.hub==null)throw new InvalidOperationException("Existing home screen is incomplete; inspect references before repair.");
                return; // Preserve authored layout, import tuning, scene identity and callbacks.
            }
            Directory.CreateDirectory("Logs/MainMenu");
            if(!File.Exists("Logs/MainMenu/Preservation-before.json"))
            {
                var baseline=new {sceneGuid=AssetDatabase.AssetPathToGUID(ScenePath),sceneHash=Hash(ScenePath),arenaHash=Hash(OlympicArenaSetup.ScenePath),bootHash=Hash(Milestone12HubSetup.BootPath),catalogHash=Hash("Assets/Resources/HubCatalog.asset"),buildHash=Hash("ProjectSettings/EditorBuildSettings.asset"),pngs=Assets.Values.Select(p=>new {path=ArtRoot+p,hash=Hash(ArtRoot+p),guid=AssetDatabase.AssetPathToGUID(ArtRoot+p)}).ToArray()};
                File.WriteAllText("Logs/MainMenu/Preservation-before.json",Newtonsoft.Json.JsonConvert.SerializeObject(baseline,Newtonsoft.Json.Formatting.Indented));
            }
            Folder("Assets/Scenes/Recovery");
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(RecoveryPath)==null && !AssetDatabase.CopyAsset(ScenePath,RecoveryPath))throw new IOException("Lobby recovery copy could not be created.");
            var art=ImportSprites();
            var hub=Object.FindAnyObjectByType<HubView>();if(hub==null || Object.FindObjectsByType<Canvas>().Length!=1)throw new InvalidOperationException("Existing single-Canvas hub required.");
            var safe=(RectTransform)hub.transform;var canvas=safe.GetComponentInParent<Canvas>();
            var backdrop=HubWidgets.Rect(canvas.transform,"MainMenuBackdrop",0,0,1,1);backdrop.SetSiblingIndex(safe.GetSiblingIndex());
            var background=Picture(backdrop,"DeveloperBackground",art.background,0,0,1,1);
            var fit=background.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();fit.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=art.background.rect.width/art.background.rect.height;
            HubWidgets.Panel(backdrop,"QuietNavyTint",0,0,1,1,new Color(.008f,.035f,.075f,.3f));
            var root=HubWidgets.Rect(safe,"MainMenuHome",0,0,1,1);var home=root.gameObject.AddComponent<MainMenuHomeView>();home.art=art;home.hub=hub;home.backdrop=backdrop.gameObject;
            hub.home=home;hub.legacyNavigation=safe.Find("Navigation").gameObject;hub.legacyProfileBar=safe.Find("ProfileBar").gameObject;
            BuildTopBar(root,home);BuildNavigation(root,home);BuildPacks(root,home);
            hub.legacyNavigation.SetActive(false);hub.legacyProfileBar.SetActive(false);hub.content.gameObject.SetActive(false);
            hub.toast.text="";var toast=hub.toast.rectTransform;toast.anchorMin=new Vector2(.27f,.26f);toast.anchorMax=new Vector2(.73f,.315f);toast.offsetMin=toast.offsetMax=Vector2.zero;
            root.SetSiblingIndex(hub.modal.GetSiblingIndex());hub.modal.SetAsLastSibling(); // Existing modal remains above every new touch target.
            var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            if(scaler.uiScaleMode!=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize)throw new InvalidOperationException("Preserve/review unexpected CanvasScaler before changing it.");
            scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Selection.activeGameObject=root.gameObject;
            Debug.Log("Asset-driven MainLobby saved. Tools > Water Polo > Play Hub Entry. Original PNG pixels and all economy/match systems retained.");
        }

        [MenuItem("Tools/Water Polo/Main Lobby/Open MainLobby")]
        public static void Open(){RequireSaved();EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);}

        public static void ConfigureLandscape()
        {
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
            var settings=Resources.FindObjectsOfTypeAll<PlayerSettings>().FirstOrDefault();
            if(settings!=null){EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();}
        }

        static MainMenuArt ImportSprites()
        {
            Folder("Assets/UI/MainMenu");
            var catalog=AssetDatabase.LoadAssetAtPath<MainMenuArt>(CatalogPath);
            if(catalog==null){catalog=ScriptableObject.CreateInstance<MainMenuArt>();AssetDatabase.CreateAsset(catalog,CatalogPath);}
            var reports=new List<object>();
            foreach(var entry in Assets)
            {
                string path=ArtRoot+entry.Value;bool background=entry.Key=="background";
                var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
                if(!source.LoadImage(File.ReadAllBytes(path)))throw new IOException("Cannot read PNG: "+path);
                int width=source.width,height=source.height;
                Rect bounds=background?new Rect(0,0,width,height):VisibleBounds(source);
                Object.DestroyImmediate(source);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
                importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=!background;
                importer.mipmapEnabled=false;importer.isReadable=false;importer.npotScale=TextureImporterNPOTScale.None;
                importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.sRGBTexture=true;
                int maximum=background?2048:entry.Value.StartsWith("Buttons/")?1024:512;
                importer.maxTextureSize=maximum;importer.textureCompression=TextureImporterCompression.CompressedHQ;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteGenerateFallbackPhysicsShape=false;importer.SetTextureSettings(settings);
                foreach(string platform in new[]{"Standalone","Android","iPhone"})
                {
                    var budget=importer.GetPlatformTextureSettings(platform);budget.name=platform;budget.overridden=true;budget.maxTextureSize=maximum;budget.compressionQuality=100;
                    budget.format=platform=="Standalone"?TextureImporterFormat.BC7:TextureImporterFormat.ASTC_4x4;importer.SetPlatformTextureSettings(budget);
                }
                var factory=new SpriteDataProviderFactories();factory.Init();var provider=factory.GetSpriteEditorDataProviderFromObject(importer);provider.InitSpriteEditorDataProvider();
                string name=Path.GetFileNameWithoutExtension(path);var prior=provider.GetSpriteRects().FirstOrDefault(r=>r.name==name);
                var sprite=new SpriteRect{name=name,rect=bounds,alignment=SpriteAlignment.Center,pivot=new Vector2(.5f,.5f),spriteID=prior!=null?prior.spriteID:GUID.Generate()};
                provider.SetSpriteRects(new[]{sprite});
                var pair=new SpriteNameFileIdPair();pair.name=name;pair.SetFileGUID(sprite.spriteID);
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>()?.SetNameFileIdPairs(new[]{pair});provider.Apply();importer.SaveAndReimport();
                var imported=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single();
                typeof(MainMenuArt).GetField(entry.Key).SetValue(catalog,imported);
                reports.Add(new{path,width,height,rect=new[]{bounds.x,bounds.y,bounds.width,bounds.height},maximum,sourcePixelsUnchanged=true});
            }
            EditorUtility.SetDirty(catalog);File.WriteAllText("Logs/MainMenu/Sprite-import-report.json",Newtonsoft.Json.JsonConvert.SerializeObject(reports,Newtonsoft.Json.Formatting.Indented));return catalog;
        }
        static Rect VisibleBounds(Texture2D texture)
        {
            var pixels=texture.GetPixels32();int minX=texture.width,minY=texture.height,maxX=-1,maxY=-1;
            for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>=64){minX=Math.Min(minX,x);minY=Math.Min(minY,y);maxX=Math.Max(maxX,x);maxY=Math.Max(maxY,y);}
            if(maxX<minX)return new Rect(0,0,texture.width,texture.height);
            int margin=Math.Max(8,Mathf.CeilToInt(Mathf.Max(texture.width,texture.height)*.008f));minX=Math.Max(0,minX-margin);minY=Math.Max(0,minY-margin);maxX=Math.Min(texture.width-1,maxX+margin);maxY=Math.Min(texture.height-1,maxY+margin);
            return new Rect(minX,minY,maxX-minX+1,maxY-minY+1);
        }

        static void BuildTopBar(RectTransform root,MainMenuHomeView home)
        {
            var art=home.art;var bar=HubWidgets.Panel(root,"TopBar",.012f,.855f,.976f,.13f,new Color(.012f,.04f,.09f,.85f));
            var profile=HubWidgets.Rect(bar,"Profile",.006f,.06f,.225f,.88f);home.profile=Hit(profile,Picture(profile,"ProfileIcon",art.profile,0,0,.31f,1));
            home.displayName=Label(profile,"DisplayName","PLAYER",.34f,.42f,.66f,.5f,31);
            home.level=Label(profile,"LevelProgress","LEVEL  —",.34f,.17f,.66f,.26f,18,HubWidgets.Muted);
            var progress=HubWidgets.Panel(profile,"LevelTrack",.34f,.08f,.62f,.045f,new Color(.11f,.23f,.34f));
            home.levelFill=Picture(progress,"ExperienceFill",null,0,0,1,1);home.levelFill.color=HubWidgets.Cyan;home.levelFill.type=UnityEngine.UI.Image.Type.Simple;home.levelFill.rectTransform.anchorMax=new Vector2(0,1);
            home.settings=ArtButton(bar,"Settings",art.settings,.24f,.08f,.064f,.84f);
            home.messages=ArtButton(bar,"Messages",art.messages,.311f,.08f,.064f,.84f);
            Picture(bar,"SmallMascot",art.mascot,.405f,.14f,.04f,.72f);
            Label(bar,"Brand","WATER POLO\nSUPERSTARS",.455f,.13f,.14f,.74f,24,Color.white);
            Currency(bar,"Diamonds",art.diamond,art.plus,.615f,.178f,out home.diamonds,out home.diamondPlus);
            Currency(bar,"Coins",art.coin,art.plus,.804f,.185f,out home.coins,out home.coinPlus);
        }
        static void Currency(RectTransform bar,string name,Sprite icon,Sprite plus,float x,float width,out TMP_Text value,out UnityEngine.UI.Button add)
        {
            var panel=HubWidgets.Panel(bar,name,x,.1f,width,.8f,new Color(.008f,.022f,.055f,.78f));
            Picture(panel,name+"Icon",icon,.015f,.12f,.23f,.76f);
            value=Label(panel,name+"Balance","—",.255f,.11f,.43f,.78f,31);value.alignment=TextAlignmentOptions.Center;
            add=ArtButton(panel,name+"Plus",plus,.70f,0,.30f,1);
        }
        static void BuildNavigation(RectTransform root,MainMenuHomeView home)
        {
            var art=home.art;var left=HubWidgets.Rect(root,"LeftNavigation",.018f,.235f,.17f,.60f);
            home.ranking=ArtButton(left,"Ranking",art.ranking,0,.69f,1,.31f);home.shop=ArtButton(left,"Shop",art.shop,0,.345f,1,.31f);home.team=ArtButton(left,"Team",art.team,0,0,1,.31f);
            var right=HubWidgets.Rect(root,"RightNavigation",.795f,.275f,.19f,.55f);
            home.friends=ArtButton(right,"Friends",art.friends,0,.73f,1,.27f);home.clubs=ArtButton(right,"Clubs",art.clubs,0,.415f,1,.27f);
            home.worldTournament=ArtButton(right,"WorldTournament",art.worldTournament,.03f,.045f,.94f,.34f);
            var title=Label(right,"WorldTournamentCaption","WORLD TOURNAMENT",0,0,1,.065f,21);title.alignment=TextAlignmentOptions.Center;title.fontStyle=FontStyles.Bold;
            var bottom=HubWidgets.Rect(root,"BottomLeft",.018f,.028f,.272f,.173f);
            home.battlePass=ArtButton(bottom,"BattlePass",art.battlePass,0,.12f,.64f,.8f);home.missions=ArtButton(bottom,"Missions",art.missions,.685f,0,.315f,1);
            Badge(home.battlePass.transform,"ClaimableSeasonRewards",out home.seasonBadge,out home.seasonCount);
            Badge(home.missions.transform,"CompletedMissions",out home.missionBadge,out home.missionCount);
            home.play=ArtButton(root,"Play",art.play,.738f,.027f,.247f,.185f);
        }
        static void BuildPacks(RectTransform root,MainMenuHomeView home)
        {
            var area=HubWidgets.Rect(root,"RewardPackSlots",.319f,.028f,.365f,.208f);
            home.slots=new MainMenuPackSlot[4];float gap=.027f,width=(1-gap*3)/4;
            for(int i=0;i<4;i++)
            {
                var card=HubWidgets.Panel(area,"PackSlot"+i,i*(width+gap),0,width,1,new Color(.018f,.045f,.085f,.66f));
                var slot=card.gameObject.AddComponent<MainMenuPackSlot>();slot.panel=card.GetComponent<RoundedPanelGraphic>();slot.panel.raycastTarget=true;
                slot.button=card.gameObject.AddComponent<UnityEngine.UI.Button>();slot.button.targetGraphic=slot.panel;SetColors(slot.button);slot.button.interactable=false;
                var image=Picture(card,"OwnedPack",null,.07f,.28f,.86f,.70f);image.gameObject.SetActive(false);slot.packImage=image;
                slot.emptyHint=Label(card,"EmptySlot","EMPTY\nREWARD SLOT",.05f,.34f,.9f,.39f,24,HubWidgets.Muted);slot.emptyHint.alignment=TextAlignmentOptions.Center;
                slot.status=Label(card,"PackState","Earn in matches",.035f,.14f,.93f,.18f,22,HubWidgets.Muted);slot.status.alignment=TextAlignmentOptions.Center;
                slot.action=Label(card,"PackAction","",.04f,.018f,.92f,.14f,24);slot.action.alignment=TextAlignmentOptions.Center;slot.action.fontStyle=FontStyles.Bold;
                var feedback=card.gameObject.AddComponent<MenuButtonFeedback>();feedback.artwork=image.transform;home.slots[i]=slot;
            }
        }
        static void Badge(Transform parent,string name,out GameObject badge,out TMP_Text count)
        {
            var root=HubWidgets.Panel(parent,name,.84f,.77f,.16f,.23f,new Color(.85f,.095f,.12f));
            count=Label(root,"RealClaimCount","",0,0,1,1,22);count.alignment=TextAlignmentOptions.Center;count.fontStyle=FontStyles.Bold;badge=root.gameObject;badge.SetActive(false);
        }
        public static void ApplyReadabilityRefinement()
        {
            RequireSaved();var home=Object.FindAnyObjectByType<MainMenuHomeView>();
            if(home==null)throw new InvalidOperationException("Open saved MainLobby first.");
            home.levelFill.type=UnityEngine.UI.Image.Type.Simple;home.levelFill.rectTransform.anchorMax=new Vector2(0,1);
            foreach(var slot in home.slots)
            {
                slot.emptyHint.fontSize=slot.emptyHint.fontSizeMax=24;slot.status.fontSize=slot.status.fontSizeMax=22;slot.action.fontSize=slot.action.fontSizeMax=24;
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        static UnityEngine.UI.Image Picture(Transform parent,string name,Sprite sprite,float x,float y,float width,float height)
        {
            var r=HubWidgets.Rect(parent,name,x,y,width,height);var image=r.gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;return image;
        }
        static UnityEngine.UI.Button ArtButton(Transform parent,string name,Sprite sprite,float x,float y,float width,float height)
        {
            var root=HubWidgets.Rect(parent,name,x,y,width,height);return Hit(root,Picture(root,"Artwork",sprite,.015f,.015f,.97f,.97f));
        }
        static UnityEngine.UI.Button Hit(RectTransform root,UnityEngine.UI.Image artwork)
        {
            var hit=root.gameObject.AddComponent<UnityEngine.UI.Image>();hit.color=new Color(1,1,1,.001f);hit.raycastTarget=true;
            var button=root.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=artwork;SetColors(button);
            var feedback=root.gameObject.AddComponent<MenuButtonFeedback>();feedback.artwork=artwork.transform;return button;
        }
        static void SetColors(UnityEngine.UI.Button button)
        {
            var c=button.colors;c.normalColor=Color.white;c.highlightedColor=new Color(1.08f,1.08f,1.08f);c.pressedColor=new Color(.82f,.9f,1);c.disabledColor=new Color(.42f,.46f,.53f,.65f);c.fadeDuration=.07f;button.colors=c;
            button.navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.Automatic};
        }
        static TMP_Text Label(Transform parent,string name,string text,float x,float y,float width,float height,float size,Color? color=null)
        {
            var label=HubWidgets.Text(parent,name,text,x,y,width,height,size,color);label.fontSizeMin=Mathf.Min(14,size);return label;
        }
        static void RequireSaved()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before lobby setup.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save intended scene edits first. Lobby setup discards nothing.");
        }
        static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;int slash=path.LastIndexOf('/');Folder(path.Substring(0,slash));AssetDatabase.CreateFolder(path.Substring(0,slash),path.Substring(slash+1));}
        static string Hash(string path){using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));}
    }
}
