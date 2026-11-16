using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using ToduStudios.WaterPolo.WaterVisuals;
using Object=UnityEngine.Object;

namespace ToduStudios.WaterPolo.Editor
{
    /// <summary>M20 replaces environmental construction only. All field, athlete and rule authorities are retained.</summary>
    public static partial class ChampionshipArenaRebuild
    {
        public const string Root="Assets/Arena/ChampionshipRebuild",Name="Olympic Championship Venue M20",Recovery="Assets/Scenes/Recovery/OlympicMatchArena_PreMilestone20.unity";
        static ChampionshipVenueLayout layout;
        static Material navy,cobalt,metal,white,rubber,concrete,glass,ice,gold,led,screen,tile,seatBlue,seatNavy,seatIce,skin,ribbon,textMaterial;
        static Transform root,medium,high,upperLow,upperFull;
        static readonly List<Renderer> ribbons=new List<Renderer>();
        static readonly List<SeatSlot> slots=new List<SeatSlot>();
        static readonly Dictionary<string,GameObject> sectionPrefabs=new Dictionary<string,GameObject>();
        sealed class SeatSlot{public Vector3 floor;public Quaternion rotation;public int tier,key;}

        [MenuItem("Tools/Water Polo/Arena/Build Milestone 20 Championship Venue")]
        public static void SetUp()
        {
            RequireSaved();var env=Object.FindAnyObjectByType<ArenaQualityController>();var pool=Object.FindAnyObjectByType<PoolBounds>();
            var existing=env.transform.Find(Name);if(existing!=null){if(existing.GetComponent<ChampionshipVenuePresentation>()==null)throw new InvalidOperationException("Incomplete M20 authoring: inspect the scene before continuing. Recovery is preserved.");Debug.Log("M20 venue is installed; authored tuning preserved.");return;}
            if(pool.Width!=20||pool.Length!=25)throw new InvalidOperationException("The validated 25 x 20 competition field must remain unchanged.");
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Recovery)==null&&!AssetDatabase.CopyAsset(OlympicArenaSetup.ScenePath,Recovery))throw new InvalidOperationException("Native recovery scene could not be created.");
            foreach(string folder in new[]{"Materials","Meshes","Prefabs","Settings","Textures"})Folder(Root+"/"+folder);
            layout=AssetDatabase.LoadAssetAtPath<ChampionshipVenueLayout>(Root+"/Settings/ChampionshipVenue.asset");if(layout==null){layout=ScriptableObject.CreateInstance<ChampionshipVenueLayout>();layout.competition=env.dimensions;AssetDatabase.CreateAsset(layout,Root+"/Settings/ChampionshipVenue.asset");}
            Materials();root=Child(Name,env.transform);medium=Child("Medium Venue Detail",root);high=Child("High Venue Detail",root);upperLow=Child("Low Upper Bowl Representation",root);upperFull=Child("Full Upper Bowl Seating",root);ribbons.Clear();slots.Clear();sectionPrefabs.Clear();
            RetireOldEnvironment(env);BuildBasin(env);BuildDeck();BuildTeamFacilities();BuildStadium();BuildIntroduction();BuildRibbons();BuildMedia();BuildRoofAndLights(env);IntegrateDisplays(env);RemapOriginalAudience(env);BuildAdditionalCrowd();
            var presentation=root.gameObject.AddComponent<ChampionshipVenuePresentation>();presentation.layout=layout;presentation.preferences=env.preferences;presentation.lowUpperSeats=upperLow.gameObject;presentation.fullUpperSeats=upperFull.gameObject;presentation.mediumDetails=medium.gameObject;presentation.highDetails=high.gameObject;presentation.ribbonPanels=ribbons.ToArray();
            upperLow.gameObject.SetActive(false);upperFull.gameObject.SetActive(true);
            var camera=Object.FindAnyObjectByType<GameplayCamera>();var overview=Anchor("Period Preparation Architectural Overview",root,new Vector3(36,33,-41));overview.LookAt(new Vector3(0,3,0));var so=new SerializedObject(camera);so.FindProperty("openingOverview").objectReferenceValue=overview;so.FindProperty("openingFieldOfView").floatValue=60;so.FindProperty("openingBlendSeconds").floatValue=.85f;so.ApplyModifiedPropertiesWithoutUndo();
            ConfigureInspection();var probe=env.GetComponentInChildren<ReflectionProbe>();probe.size=new Vector3(106,42,132);probe.transform.position=new Vector3(0,3,0);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            ApplyReviewFixes();Debug.Log("M20 championship venue saved: 50 x 25 architectural basin, unchanged 25 x 20 gameplay field, three seating tiers, operational team gates, entrance and live displays.");
        }
        static void RequireSaved(){if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty||SceneManager.GetActiveScene().path!=OlympicArenaSetup.ScenePath)throw new InvalidOperationException("Open saved OlympicMatchArena outside Play Mode.");}
        static void RetireOldEnvironment(ArenaQualityController env)
        {
            var keep=new HashSet<Renderer>{env.water.waterSurface};
            void Keep(Transform t){if(t!=null)foreach(var r in t.GetComponentsInChildren<Renderer>(true))keep.Add(r);}
            Keep(GameObject.Find(AuthenticFieldSetup.Name).transform);Keep(GameObject.Find(MatchBroadcastSetup.Name).transform);
            var displays=GameObject.Find(PremiumArenaSetup.Name).transform.Find("Championship Scoreboards and Presentation Screens");Keep(displays);
            foreach(var r in env.GetComponentsInChildren<Renderer>(true))if(!keep.Contains(r))r.enabled=false;
            // Old environmental objects/assets remain for recovery. No gameplay hierarchy is removed.
        }
        static void Materials()
        {
            navy=Mat("Midnight Marine Enamel","#0C1C34",.25f,.36f);cobalt=Mat("Cobalt Architectural Enamel","#18487C",.24f,.32f);metal=Mat("Brushed Marine Alloy","#6D8799",.68f,.36f);white=Mat("Competition Polymer","#D7E4E9",.03f,.34f);
            rubber=Mat("Navy Event Rubber","#203850",.01f,.20f);rubber.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(PremiumArenaSetup.Root+"/Textures/FineRubber.png"));rubber.mainTextureScale=Vector2.one*2;
            concrete=Mat("Precast Bowl Ceramic","#46566B",.05f,.24f);concrete.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(PremiumArenaSetup.Root+"/Textures/BasaltCeramic.png"));
            glass=Mat("Acoustic Midnight Panels","#101F31",.32f,.28f);ice=Mat("Ice Blue Inlay","#2889B4",.25f,.34f);gold=Mat("Championship Gold Trim","#BCA86E",.5f,.32f);led=Mat("Frosted Architectural LEDs","#85CDEB",.1f,.25f,new Color(.32f,.8f,1)*1.2f);screen=Mat("LED Black","#041121",0,.18f);
            seatBlue=Mat("Cobalt Seat Shell","#17466D",.04f,.33f);seatNavy=Mat("Midnight Seat Shell","#102C4C",.03f,.30f);seatIce=Mat("Ice Seat Shell","#327C9E",.03f,.30f);
            tile=AssetDatabase.LoadAssetAtPath<Material>(WaterVisualTestSetup.Root+"/Materials/Poolceramic.mat");skin=AssetDatabase.LoadAssetAtPath<Material>("Assets/Arena/Materials/Audienceskin.mat");if(skin==null)skin=Mat("Crowd Skin Existing Palette","#A66D4A",0,.2f);
            textMaterial=AssetDatabase.LoadAssetAtPath<Material>(MatchBroadcastSetup.Root+"/Materials/Arena Text Front Faces.mat");
            ribbon=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Replaceable Ribbon Graphics.mat");if(ribbon==null){ribbon=new Material(Shader.Find("Water Polo/Championship Ribbon")){name="Replaceable Ribbon Graphics",enableInstancing=true};AssetDatabase.CreateAsset(ribbon,Root+"/Materials/Replaceable Ribbon Graphics.mat");}
            foreach(var m in new[]{rubber,concrete})EditorUtility.SetDirty(m);
        }
        static void BuildBasin(ArenaQualityController env)
        {
            var b=new ChampionshipArenaBatch(Child("B Water Basin 50m Installation",root));float w=layout.basinWidth*.5f,l=layout.basinLength*.5f,d=layout.waterDepth,z=layout.basinCenterZ;
            b.Floor(tile,new Vector3(0,-d,z),new Vector2(w*2,l*2));
            foreach(int s in new[]{-1,1})
            {
                b.Box(tile,new Vector3(s*(w+.12f),-d*.5f,z),new Vector3(.24f,d,2*l+.48f));b.Box(tile,new Vector3(0,-d*.5f,z+s*(l+.12f)),new Vector3(w*2,.24f+d, .24f));
                b.Box(white,new Vector3(s*(w+.10f),.12f,z),new Vector3(.26f,.26f,l*2+.4f));b.Box(white,new Vector3(0,.12f,z+s*(l+.1f)),new Vector3(w*2,.26f,.26f));
                b.Box(cobalt,new Vector3(s*(w-.015f),-.18f,z),new Vector3(.018f,.17f,l*2));b.Box(cobalt,new Vector3(0,-.18f,z+s*(l-.015f)),new Vector3(w*2,.17f,.018f));
            }
            b.Save(Root+"/Meshes");var surface=new ChampionshipArenaMesh();surface.Face(new Vector3(-w,0,z-l),new Vector3(w,0,z-l),new Vector3(w,0,z+l),new Vector3(-w,0,z+l),Vector3.up,Vector2.zero,new Vector2(w*2,0),new Vector2(w*2,l*2),new Vector2(0,l*2));var mesh=surface.Mesh("Single Extended Water Surface");string path=Root+"/Meshes/Single Extended Water Surface.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(mesh);}env.water.waterSurface.GetComponent<MeshFilter>().sharedMesh=saved;
            // Exactly one renderer, one waterline and the existing material/reflection/effects authority.
            var bulk=new ChampionshipArenaBatch(Child("Movable Bulkheads and Competition End Access",root));
            foreach(int end in new[]{-1,1})
            {
                float bz=end*layout.bulkheadDistance;
                foreach(var span in new[]{new Vector2(-12.65f,8.7f),new Vector2(10.1f,12.65f)}){float centre=(span.x+span.y)*.5f,width=span.y-span.x;bulk.Bevel(navy,new Vector3(centre,.24f,bz),new Vector3(width,.90f,.95f),.06f);bulk.Floor(rubber,new Vector3(centre,.704f,bz),new Vector2(width,.94f));bulk.Box(white,new Vector3(centre,.63f,bz-end*.48f),new Vector3(width,.11f,.055f));bulk.Box(ice,new Vector3(centre,.53f,bz-end*.49f),new Vector3(width,.045f,.025f));}
                // The 1.4m-wide controlled entry is a real notch, not a ramp through a solid bulkhead.
                bulk.Floor(rubber,new Vector3(9.4f,.704f,bz+end*.32f),new Vector2(1.4f,.30f));
                foreach(int side in new[]{-1,1}){bulk.Box(metal,new Vector3(side*12.42f,.34f,bz),new Vector3(.15f,.68f,1.1f));bulk.Box(navy,new Vector3(side*12.34f,.72f,bz),new Vector3(.32f,.06f,.65f));}
                Label("COMPETITION FIELD  /  WATER POLO SUPERSTARS",bulk.root,new Vector3(0,.32f,bz-end*.49f),Quaternion.Euler(0,end>0?0:180,0),new Vector2(14,.28f),2.3f,white.color);
                // Two slender practice-zone guides stay entirely outside the match boundary/bulkhead.
                for(int guide=-1;guide<=1;guide+=2)for(float q=15.0f;q<24.7f;q+=.28f)bulk.Tube(white,new Vector3(guide*5.7f,.015f,end*q),new Vector3(guide*5.7f,.015f,end*(q+.17f)),.025f,6);
                foreach(float x in new[]{-8f,0,8f}){bulk.Tube(metal,new Vector3(x,.8f,end*25.32f),new Vector3(x,1.35f,end*25.32f),.025f);bulk.Tube(metal,new Vector3(x,1.35f,end*25.32f),new Vector3(x,1.35f,end*25.03f),.025f);}
            }
            bulk.Save(Root+"/Meshes");
        }
        static void BuildDeck()
        {
            var b=new ChampionshipArenaBatch(Child("C Competition Deck Circulation and Coping",root));float w=layout.basinWidth*.5f,l=layout.basinLength*.5f,h=layout.deckHeight;
            foreach(int side in new[]{-1,1})
            {
                b.Floor(rubber,new Vector3(side*(w+21.75f),h,0),new Vector2(43.5f,140));b.Floor(rubber,new Vector3(0,h,side*(l+22.5f)),new Vector2(25,45));
                b.Box(navy,new Vector3(side*(w+.28f),.35f,0),new Vector3(.48f,.7f,50.5f));b.Box(metal,new Vector3(side*(w+.6f),h+.014f,0),new Vector3(.22f,.024f,50.7f));
                for(float z=-25;z<=25;z+=.2f)b.Box(navy,new Vector3(side*(w+.6f),h+.029f,z),new Vector3(.18f,.012f,.045f));
                b.Box(navy,new Vector3(0,.35f,side*(l+.28f)),new Vector3(25,.70f,.48f));b.Box(metal,new Vector3(0,h+.014f,side*(l+.6f)),new Vector3(25.8f,.024f,.22f));
                for(float x=-12.5f;x<=12.5f;x+=.2f)b.Box(navy,new Vector3(x,h+.029f,side*(l+.6f)),new Vector3(.045f,.012f,.18f));
                // Protected officials lane ends before either team corridor; no entry crosses it.
                b.Box(white,new Vector3(side*13.18f,.718f,0),new Vector3(.045f,.018f,25.5f));b.Box(ice,new Vector3(side*13.5f,.712f,0),new Vector3(.035f,.012f,25.5f));
                Label("OFFICIALS  /  KEEP CLEAR",b.root,new Vector3(side*13.38f,h+.02f,0),Quaternion.Euler(90,side>0?90:-90,0),new Vector2(7,.28f),2,white.color);
            }
            b.Save(Root+"/Meshes");BuildAdvertising();
        }
        static void BuildAdvertising()
        {
            var b=new ChampionshipArenaBatch(Child("Pool Level Modular Advertising with Team Gates",root));
            foreach(int side in new[]{-1,1})
            {
                // Open 2.8m team-access gates at the bulkhead crossings; no invisible/transparent walls.
                var spans=side>0?new[]{new Vector2(-26,-15.6f),new Vector2(-12.8f,12.8f),new Vector2(15.6f,26)}:new[]{new Vector2(-26,-3.4f),new Vector2(3.4f,26)};
                foreach(var span in spans)
                {
                    float length=span.y-span.x,z=(span.x+span.y)*.5f;b.Bevel(navy,new Vector3(side*14.65f,1.18f,z),new Vector3(.20f,.96f,length),.03f);b.Box(screen,new Vector3(side*14.53f,1.18f,z),new Vector3(.025f,.73f,length-.08f));b.Box(metal,new Vector3(side*14.65f,1.68f,z),new Vector3(.26f,.04f,length));
                    int count=Mathf.Max(1,Mathf.FloorToInt(length/5));for(int i=0;i<count;i++){float q=Mathf.Lerp(span.x,span.y,(i+.5f)/count);Label(i%2==0?"WATER POLO SUPERSTARS":"CHAMPIONSHIP NIGHT",b.root,new Vector3(side*14.51f,1.18f,q),Quaternion.Euler(0,side>0?90:-90,0),new Vector2(4.65f,.38f),2.5f,white.color);}
                }
                foreach(float z in new[]{-15.6f,-12.8f,12.8f,15.6f})if(side>0){b.Box(metal,new Vector3(side*14.65f,1.12f,z),new Vector3(.27f,.84f,.12f));b.Box(ice,new Vector3(side*14.65f,1.57f,z),new Vector3(.28f,.055f,.14f));}
            }
            b.Save(Root+"/Meshes");
        }
        static void BuildTeamFacilities()
        {
            var teams=Child("D Team Facilities and Water Entry",root);var match=Object.FindAnyObjectByType<PrototypeMatch>();var tactics=Object.FindObjectsByType<PrototypeTeamTactics>();
            foreach(int end in new[]{-1,1})
            {
                var g=Child(end<0?"South Team Facility":"North Team Facility",teams);var b=new ChampionshipArenaBatch(g);Vector3 origin=new Vector3(18.5f,.72f,end*17.0f);var facing=Quaternion.LookRotation(Vector3.left);var facility=g.gameObject.AddComponent<VenueTeamFacility>();facility.defendedEnd=end;facility.match=match;facility.blue=tactics.Single(t=>t.Team==PrototypeTeam.Blue);facility.white=tactics.Single(t=>t.Team==PrototypeTeam.White);facility.bench=Anchor("Bench Centre",g,origin);
                b.Bevel(navy,new Vector3(18.8f,.81f,end*18.4f),new Vector3(3.8f,.22f,7.8f),.08f);b.Floor(rubber,new Vector3(18.8f,.925f,end*18.4f),new Vector2(3.7f,7.7f));
                for(int i=0;i<9;i++)BenchChair(b,new Vector3(18.8f,.93f,end*(14.9f+i*.78f)),facing);
                for(int i=0;i<2;i++)BenchChair(b,new Vector3(20.0f,.93f,end*(15.7f+i*.85f)),facing);
                b.Bevel(navy,new Vector3(20.75f,1.85f,end*18.2f),new Vector3(.18f,2.4f,8.2f),.06f);b.Box(ice,new Vector3(20.64f,1.14f,end*18.2f),new Vector3(.025f,.13f,7.7f));
                facility.teamLabel=Label(end<0?"BLUE  /  TEAM FACILITY":"WHITE  /  TEAM FACILITY",g,new Vector3(20.63f,2.65f,end*18.2f),Quaternion.Euler(0,90,0),new Vector2(7.5f,.42f),3.2f,end<0?ice.color:white.color);
                b.Bevel(white,new Vector3(17.4f,1.13f,end*22.0f),new Vector3(1.0f,.8f,.75f),.055f);b.Box(cobalt,new Vector3(17.4f,1.56f,end*22.0f),new Vector3(1.04f,.08f,.80f));
                for(int i=0;i<5;i++){b.Box(white,new Vector3(17.4f,1.23f+i*.05f,end*23),new Vector3(.65f,.045f,.48f));Bottle(b,new Vector3(17.15f+i*.14f,1.62f,end*22));}
                b.Bevel(navy,new Vector3(18.6f,1.2f,end*23.1f),new Vector3(1.35f,.92f,.8f),.05f);b.Box(metal,new Vector3(18.6f,1.64f,end*23.1f),new Vector3(1.38f,.035f,.83f));
                // Small technical desk and open ball basket, distinct from reserve seats.
                b.Box(navy,new Vector3(16.8f,1.2f,end*20.4f),new Vector3(.65f,.9f,1.3f));b.Box(metal,new Vector3(16.8f,1.68f,end*20.4f),new Vector3(.75f,.065f,1.45f));
                foreach(float y in new[]{.8f,1.16f})foreach(float z in new[]{-1f,1f})b.Tube(metal,new Vector3(16.4f,y,end*23.5f+z*.33f),new Vector3(17.1f,y,end*23.5f+z*.33f),.016f);for(int i=0;i<4;i++)b.Tube(metal,new Vector3(16.4f+i*.23f,.8f,end*23.17f),new Vector3(16.4f+i*.23f,1.16f,end*23.17f),.012f);
                for(int i=0;i<3;i++)b.Part(gold).Ellipsoid(new Vector3(16.55f+i*.2f,1.02f,end*23.5f),Vector3.one*.19f,Quaternion.identity,8,4);
                var route=new[]{new Vector3(17.5f,.94f,end*17),new Vector3(16.9f,.93f,end*15.5f),new Vector3(16.2f,.70f,end*14.2f),new Vector3(14.65f,.70f,end*14.2f),new Vector3(11.2f,.70f,end*14.2f),new Vector3(9.4f,.70f,end*14.2f),new Vector3(9.4f,.25f,end*13.65f),new Vector3(9.4f,0,end*13.25f)};
                Vector3 start=route[1],finish=route[2],sideways=new Vector3(-(finish-start).z,0,(finish-start).x).normalized*.48f;b.Part(rubber).Face(start-sideways,start+sideways,finish+sideways,finish-sideways,Vector3.up);
                facility.accessRoute=route.Select((p,i)=>Anchor("Access "+i,g,p)).ToArray();facility.waterEntry=facility.accessRoute.Last();
                foreach(float x in new[]{10.5f,12f,14f,16f})b.Box(ice,new Vector3(x,.718f,end*14.2f),new Vector3(.32f,.012f,.035f));
                // A real sloped end-entry surface meets the existing red exclusion waiting corner.
                b.Part(rubber).Face(new Vector3(8.8f,.70f,end*14.15f),new Vector3(10,.70f,end*14.15f),new Vector3(10,.12f,end*13.45f),new Vector3(8.8f,.12f,end*13.45f),Vector3.up);
                Label("TEAM ACCESS",g,new Vector3(15.9f,.73f,end*14.2f),Quaternion.Euler(90,90,0),new Vector2(1.8f,.32f),2,white.color);b.Save(Root+"/Meshes",true);
            }
        }
        static void BenchChair(ChampionshipArenaBatch b,Vector3 p,Quaternion r)
        {
            Vector3 P(float x,float y,float z)=>p+r*new Vector3(x,y,z);
            b.Bevel(white,P(0,.45f,0),new Vector3(.54f,.055f,.49f),.045f,r);
            foreach(int side in new[]{-1,1}){b.Tube(metal,P(side*.22f,.44f,.18f),P(side*.28f,.01f,.27f),.019f,8);b.Tube(metal,P(side*.22f,.44f,-.18f),P(side*.28f,.01f,-.29f),.019f,8);b.Tube(metal,P(side*.22f,.43f,-.18f),P(side*.22f,1.0f,-.30f),.018f,8);}
            for(int i=0;i<5;i++)b.Bevel(white,P(0,.69f+i*.06f,-.26f-i*.012f),new Vector3(.50f-i*.007f,.045f,.055f),.015f,r);
        }
        static void Bottle(ChampionshipArenaBatch b,Vector3 p){b.Tube(white,p,p+Vector3.up*.19f,.035f,8);b.Tube(ice,p+Vector3.up*.19f,p+Vector3.up*.22f,.023f,8);}

        // Remaining static construction is kept in separate authoring methods below.
        static partial void BuildStadium();
        static partial void BuildIntroduction();
        static partial void BuildRibbons();
        static partial void BuildMedia();
        static partial void BuildRoofAndLights(ArenaQualityController env);
        static partial void IntegrateDisplays(ArenaQualityController env);
        static partial void RemapOriginalAudience(ArenaQualityController env);
        static partial void BuildAdditionalCrowd();
        static partial void ConfigureInspection();
        static Transform Child(string name,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        static Transform Anchor(string name,Transform parent,Vector3 position){var t=Child(name,parent);t.position=position;return t;}
        static TMP_Text Label(string caption,Transform parent,Vector3 position,Quaternion rotation,Vector2 size,float font,Color color)
        {
            var t=Child(caption,parent);t.localPosition=position;t.localRotation=rotation;var text=t.gameObject.AddComponent<TextMeshPro>();text.font=TMP_Settings.defaultFontAsset;text.fontSharedMaterial=textMaterial;text.text=caption;text.color=color;text.fontSize=font;text.fontSizeMax=font;text.fontSizeMin=font*.6f;text.enableAutoSizing=true;text.fontStyle=FontStyles.Bold;text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=size;text.textWrappingMode=TextWrappingModes.NoWrap;text.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;return text;
        }
        static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;int i=path.LastIndexOf('/');Folder(path.Substring(0,i));AssetDatabase.CreateFolder(path.Substring(0,i),path.Substring(i+1));}
        static Material Mat(string name,string hex,float metallic,float smooth,Color emission=default)
        {
            string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;ColorUtility.TryParseHtmlString(hex,out var c);m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=c,enableInstancing=true};m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);if(emission.maxColorComponent>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",emission);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;}AssetDatabase.CreateAsset(m,path);return m;
        }
    }
}
