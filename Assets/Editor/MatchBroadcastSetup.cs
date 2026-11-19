using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using ToduStudios.WaterPolo.WaterVisuals;
using Object=UnityEngine.Object;
using MeshData=ToduStudios.WaterPolo.Editor.AuthenticFieldSetup.MeshData;

namespace ToduStudios.WaterPolo.Editor
{
    public static class MatchBroadcastSetup
    {
        public const string Root="Assets/Arena/MatchBroadcast",Name="Primary Championship Scoreboard",Recovery="Assets/Scenes/Recovery/OlympicMatchArena_PreMatchBroadcast.unity";
        static readonly Color Cyan=new Color(.22f,.85f,1f),Navy=new Color(.018f,.043f,.082f,.96f),Card=new Color(.045f,.1f,.17f,.96f);
        static Material textMaterial;
        [MenuItem("Tools/Water Polo/Recovery/Arena/Earlier Arena Passes/Set Up Live Scoreboard and Broadcast Cameras")]
        public static void SetUp()
        {
            var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||scene.isDirty||scene.path!=OlympicArenaSetup.ScenePath)throw new InvalidOperationException("Open saved OlympicMatchArena outside Play Mode.");
            var arena=Object.FindAnyObjectByType<ArenaQualityController>();if(arena.transform.Find(Name)!=null){Debug.Log("M19 display/cameras already configured; authored tuning preserved.");return;}
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Recovery)==null&&!AssetDatabase.CopyAsset(scene.path,Recovery))throw new InvalidOperationException("Native recovery failed.");
            Folder(Root+"/Materials");Folder(Root+"/Meshes");Folder(Root+"/Prefabs");
            var font=TMP_Settings.defaultFontAsset;textMaterial=new Material(font.material){name="Arena Text Front Faces"};textMaterial.SetFloat("_CullMode",(float)CullMode.Back);textMaterial.SetFloat("_ZTestMode",(float)CompareFunction.LessEqual);AssetDatabase.CreateAsset(textMaterial,Root+"/Materials/Arena Text Front Faces.mat");
            var housing=Mat("Navy Display Housing",new Color(.025f,.055f,.09f),.4f,.35f);var alloy=Mat("Display Anodized Frame",new Color(.28f,.42f,.56f),.70f,.42f);var screen=Mat("LED Screen",new Color(.003f,.012f,.022f),0,.20f);var glow=Mat("Cyan Display Edge",new Color(.09f,.42f,.65f),.1f,.4f,new Color(.1f,.55f,.85f)*1.6f);var amber=Mat("Active Exclusion Amber",new Color(1f,.56f,.12f),.1f,.3f,new Color(1f,.35f,.03f)*.4f);
            var root=Child(Name,arena.transform);root.localPosition=new Vector3(-23.15f,11.8f,0);root.localRotation=Quaternion.Euler(0,-90,0);
            var body=new MeshData();body.Box(Vector3.zero,new Vector3(15.8f,7.6f,.48f));Geometry(root,"LED Structural Housing",body,housing);
            var frame=new MeshData();foreach(int s in new[]{-1,1}){frame.SquareBar(new Vector3(s*7.83f,-3.72f,-.27f),new Vector3(s*7.83f,3.72f,-.27f),.10f,.012f);frame.SquareBar(new Vector3(-7.83f,s*3.72f,-.27f),new Vector3(7.83f,s*3.72f,-.27f),.10f,.012f);}
            foreach(float x in new[]{-7.2f,0,7.2f}){frame.Box(new Vector3(x,0,.38f),new Vector3(.18f,6.7f,.26f));frame.Box(new Vector3(x,0,.56f),new Vector3(.48f,.8f,.14f));}Geometry(root,"Open Frame and Wall Mounts",frame,alloy);
            var face=new MeshData();face.Box(new Vector3(0,0,-.25f),new Vector3(15.48f,7.28f,.025f));Geometry(root,"LED Panel Face",face,screen);
            var edges=new MeshData();foreach(int s in new[]{-1,1})edges.Box(new Vector3(0,s*3.65f,-.28f),new Vector3(15.4f,.035f,.025f));Geometry(root,"Cyan Frame Traces",edges,glow);
            Text(root,"ChampionshipTitle","WATER POLO SUPERSTARS  /  CHAMPIONSHIP",new Vector3(0,3.0f,-.30f),new Vector2(14.5f,.55f),7f,Cyan);
            Text(root,"BlueTeam","BLUE",new Vector3(-5.2f,1.6f,-.30f),new Vector2(3.3f,1.2f),15f,Cyan);Text(root,"WhiteTeam","WHITE",new Vector3(5.2f,1.6f,-.30f),new Vector2(3.3f,1.2f),15f,Color.white);
            var score=Text(root,"LiveScore","0 : 0",new Vector3(0,1.5f,-.31f),new Vector2(6.5f,2.0f),27f,Color.white);
            var clock=Text(root,"LiveClock","Q1     08:00     SHOT 28",new Vector3(0,-.15f,-.31f),new Vector2(14.3f,1.0f),11.8f,Color.white);
            var state=Text(root,"LiveState","PERIOD OPENING",new Vector3(0,-1.0f,-.31f),new Vector2(14.3f,.55f),6.5f,Cyan);
            var roster=Object.FindAnyObjectByType<PrototypeRoster>().Members.OrderBy(p=>p.Team).ThenBy(p=>p.CapNumber).ThenBy(p=>p.PlayerId).ToArray();var labels=new TMP_Text[roster.Length];var dots=new Renderer[roster.Length];int b=0,w=0;
            Text(root,"BlueCapsHeading","BLUE CAPS",new Vector3(-6.35f,-1.96f,-.31f),new Vector2(2.1f,.55f),5.3f,Cyan);Text(root,"WhiteCapsHeading","WHITE CAPS",new Vector3(-6.35f,-2.75f,-.31f),new Vector2(2.1f,.55f),5.3f,Color.white);
            for(int i=0;i<roster.Length;i++)
            {
                var p=roster[i];int column=p.Team==PrototypeTeam.Blue?b++:w++;float y=p.Team==PrototypeTeam.Blue?-1.96f:-2.75f,x=-4.1f+column*1.6f;
                labels[i]=Text(root,"Cap "+p.PlayerId,"#"+p.CapNumber,new Vector3(x,y,-.31f),new Vector2(1.65f,.58f),5.8f,p.Team==PrototypeTeam.Blue?Cyan:Color.white);
                var dot=GameObject.CreatePrimitive(PrimitiveType.Sphere);dot.name="Active Exclusion "+p.PlayerId;dot.transform.SetParent(root,false);dot.transform.localPosition=new Vector3(x-.70f,y+.29f,-.34f);dot.transform.localScale=Vector3.one*.12f;Object.DestroyImmediate(dot.GetComponent<Collider>());dots[i]=dot.GetComponent<Renderer>();dots[i].sharedMaterial=amber;dots[i].shadowCastingMode=ShadowCastingMode.Off;dots[i].enabled=false;
            }
            Text(root,"Legend","AMBER = ACTIVE EXCLUSION     /     OUT = MATCH REMOVAL",new Vector3(0,-3.3f,-.31f),new Vector2(14.3f,.4f),3.7f,new Color(.54f,.7f,.82f));
            var observer=Object.FindAnyObjectByType<ArenaEventDisplay>();observer.primaryScore=score;observer.primaryClock=clock;observer.primaryState=state;observer.athletes=roster;observer.capLabels=labels;observer.exclusionDots=dots;PrefabUtility.RecordPrefabInstancePropertyModifications(observer);
            // Original secondary displays retain their existing bindings, with front-face text only.
            var venue=GameObject.Find(PremiumArenaSetup.Name).transform;
            foreach(var text in venue.GetComponentsInChildren<TMP_Text>(true))
            {
                if(text.GetComponentInParent<ArenaEventDisplay>()!=null&&text.transform.IsChildOf(venue.Find("Championship Scoreboards and Presentation Screens")))text.fontSharedMaterial=textMaterial;
                if(text.transform.parent.name=="Competition Deck and Clear Official Lanes"&&text.transform.position.z>0&&(text.text=="WATER POLO SUPERSTARS"||text.text=="CHAMPIONSHIP  /  MATCH COURT 01")){text.transform.rotation=Quaternion.Euler(90,0,0);PrefabUtility.RecordPrefabInstancePropertyModifications(text.transform);}
            }
            BuildSettings();var camera=Object.FindAnyObjectByType<GameplayCamera>();var sightlines=camera.gameObject.AddComponent<BroadcastSightlines>();Assign(sightlines,"output",camera.GetComponent<Camera>());Assign(sightlines,"ball",Object.FindAnyObjectByType<BallController>());Assign(sightlines,"selection",Object.FindAnyObjectByType<PlayerSelection>());Assign(sightlines,"primaryDisplay",root);
            var galleries=GameObject.Find(SprintPolishSetup.Name).transform.Find("Suspended Competition Light Galleries").GetComponentsInChildren<Renderer>();Array(sightlines,"westGallery",galleries.Where(r=>r.name.EndsWith("-1")).Cast<Object>().ToArray());Array(sightlines,"eastGallery",galleries.Where(r=>r.name.EndsWith(" 1")).Cast<Object>().ToArray());
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,Root+"/Prefabs/ChampionshipScoreboard.prefab");EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("M19 primary live scoreboard, exclusion bindings, camera settings and bounded gallery sightlines saved.");
        }
        public static void ApplyReviewedLayout()
        {
            var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||scene.isDirty||scene.path!=OlympicArenaSetup.ScenePath)throw new InvalidOperationException("Saved arena required.");
            var root=GameObject.Find(Name).transform;var display=Object.FindAnyObjectByType<ArenaEventDisplay>();
            foreach(var text in root.GetComponentsInChildren<TMP_Text>()){text.fontSizeMax=text.fontSize;text.fontSizeMin=text.fontSize*.45f;text.enableAutoSizing=true;}
            int b=0,w=0;for(int i=0;i<display.athletes.Length;i++){var p=display.athletes[i];int column=p.Team==PrototypeTeam.Blue?b++:w++;var pos=display.capLabels[i].transform.localPosition;pos.x=-4.1f+column*1.6f;display.capLabels[i].transform.localPosition=pos;pos.x-=.70f;pos.y+=.29f;pos.z=-.34f;display.exclusionDots[i].transform.localPosition=pos;}
            var camera=Object.FindAnyObjectByType<GameplayCamera>();var so=new SerializedObject(camera);var side=so.FindProperty("sideline");side.FindPropertyRelative("distance").floatValue=22;side.FindPropertyRelative("ballWeight").floatValue=.65f;so.ApplyModifiedPropertiesWithoutUndo();Assign(camera.GetComponent<BroadcastSightlines>(),"primaryDisplay",root);
            ArrangeSettings(Object.FindAnyObjectByType<Canvas>().transform.Find("MenuOverlay/MenuSafeArea/SettingsPanel"));
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,Root+"/Prefabs/ChampionshipScoreboard.prefab");EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        static void BuildSettings()
        {
            var canvas=Object.FindAnyObjectByType<Canvas>();var panel=canvas.transform.Find("MenuOverlay/MenuSafeArea/SettingsPanel");var view=panel.GetComponent<MatchSettingsView>();
            UIField(panel,"CameraViewHeading","CAMERA VIEW  /  F6",new Vector2(-280,-137),new Vector2(480,30),21,Cyan);
            var value=UIField(panel,"CameraViewValue","LENGTHWISE",new Vector2(-280,-174),new Vector2(480,30),19,Color.white);Assign(view,"cameraViewValue",value);
            Assign(view,"lengthwiseChoice",Button(panel,"LengthwiseCamera","LENGTHWISE",new Vector2(-402,-220),new Vector2(226,60),view.Lengthwise).GetComponent<RoundedPanelGraphic>());
            Assign(view,"sidelineChoice",Button(panel,"SidelineCamera","SIDELINE",new Vector2(-157,-220),new Vector2(226,60),view.Sideline).GetComponent<RoundedPanelGraphic>());
            ArrangeSettings(panel);
        }
        static void ArrangeSettings(Transform panel)
        {
            void Move(string n,float x,float y,float width=0,float height=0){var r=(RectTransform)panel.Find(n);r.anchoredPosition=new Vector2(x,y);if(width>0&&height>0)r.sizeDelta=new Vector2(width,height);}
            Move("masterRow",-280,210);Move("sfxRow",-280,110);Move("crowdRow",-280,10);Move("AudioNote",-280,-83,480,35);panel.Find("AudioNote").GetComponent<TMP_Text>().fontSize=15;
            Move("zoomRow",280,210);Move("smoothingRow",280,110);Move("GraphicsValue",280,8);Move("Low",120,-43);Move("Medium",280,-43);Move("High",440,-43);Move("GraphicsNote",280,-94,500,35);panel.Find("GraphicsNote").GetComponent<TMP_Text>().fontSize=15;
            Move("CameraViewHeading",-280,-137);Move("CameraViewValue",-280,-174);Move("LengthwiseCamera",-402,-220);Move("SidelineCamera",-157,-220);
            Move("ControlHeading",280,-137,480,30);Move("ControlSizeValue",280,-174);Move("SmallControls",120,-220);Move("NormalControls",280,-220);Move("LargeControls",440,-220);Move("ControlOpacityRow",-280,-297);Move("Back",0,-400,480,60);
        }
        static Material Mat(string n,Color color,float metallic,float smooth,Color emission=default){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=n,color=color};m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);if(emission.maxColorComponent>0){m.SetColor("_EmissionColor",emission);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;m.EnableKeyword("_EMISSION");}AssetDatabase.CreateAsset(m,Root+"/Materials/"+n+".mat");return m;}
        static Transform Child(string n,Transform p){var t=new GameObject(n).transform;t.SetParent(p,false);return t;}
        static void Geometry(Transform p,string name,MeshData data,Material material){var mesh=data.Mesh(name);AssetDatabase.CreateAsset(mesh,Root+"/Meshes/"+name+".asset");var t=Child(name,p);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;}
        static TMP_Text Text(Transform p,string name,string text,Vector3 pos,Vector2 size,float font,Color color){var t=Child(name,p);t.localPosition=pos;var v=t.gameObject.AddComponent<TextMeshPro>();v.font=TMP_Settings.defaultFontAsset;v.fontSharedMaterial=textMaterial;v.text=text;v.fontSize=font;v.fontSizeMax=font;v.fontSizeMin=font*.45f;v.enableAutoSizing=true;v.color=color;v.fontStyle=FontStyles.Bold;v.alignment=TextAlignmentOptions.Center;v.textWrappingMode=TextWrappingModes.NoWrap;v.rectTransform.sizeDelta=size;v.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;return v;}
        static RectTransform Rect(string n,Transform p,Vector2 pos,Vector2 size){var r=new GameObject(n,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false);r.anchorMin=r.anchorMax=Vector2.one*.5f;r.anchoredPosition=pos;r.sizeDelta=size;return r;}
        static TMP_Text UIField(Transform p,string n,string value,Vector2 pos,Vector2 size,float font,Color color){var r=Rect(n,p,pos,size);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.text=value;t.fontSize=font;t.color=color;t.alignment=TextAlignmentOptions.Center;t.textWrappingMode=TextWrappingModes.NoWrap;t.raycastTarget=false;return t;}
        static RectTransform Button(Transform p,string n,string caption,Vector2 pos,Vector2 size,UnityAction action){var r=Rect(n,p,pos,size);var g=r.gameObject.AddComponent<RoundedPanelGraphic>();g.color=Card;g.raycastTarget=true;var button=r.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=g;button.navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.None};UnityEventTools.AddPersistentListener(button.onClick,action);UIField(r,"Label",caption,Vector2.zero,size-new Vector2(12,8),20,Color.white);return r;}
        static void Assign(Object o,string n,Object v)=>Milestone9PrototypeSetup.Assign(o,n,v);
        static void Array(Object o,string n,Object[] values){var so=new SerializedObject(o);var p=so.FindProperty(n);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
        static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;int slash=path.LastIndexOf('/');Folder(path.Substring(0,slash));AssetDatabase.CreateFolder(path.Substring(0,slash),path.Substring(slash+1));}
    }
}
