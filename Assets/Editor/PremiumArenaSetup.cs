using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace ToduStudios.WaterPolo.Editor
{
    /// <summary>In-place championship-night venue, retaining the pool, athletes, spectators and match authorities.</summary>
    public static class PremiumArenaSetup
    {
        public const string Root="Assets/Arena/ChampionshipNight";
        public const string Name="Championship Night Venue";
        public const string Recovery="Assets/Scenes/Recovery/OlympicMatchArena_PreChampionshipNight.unity";
        public const string PrefabPath=Root+"/Prefabs/ChampionshipNightVenue.prefab";
        static ArenaDimensions d;
        static Material navy,cobalt,slate,graphite,silver,ivory,cyan,led,screen,carpet,ceramic,glass,seat;
        static readonly List<TMP_Text> scores=new List<TMP_Text>(),clocks=new List<TMP_Text>();
        static readonly List<Light> lights=new List<Light>();
        static readonly HashSet<Mesh> temporary=new HashSet<Mesh>();
        static readonly Dictionary<string,Mesh> rounded=new Dictionary<string,Mesh>();
        static Transform detail;
        static float W=>d.PoolWidth/2;
        static float L=>d.PoolLength/2;
        static float Y=>d.deckElevation;

        [MenuItem("Tools/Water Polo/Recovery/Arena/Apply Championship Night Overhaul")]
        public static void SetUp()
        {
            RequireSaved();OlympicArenaSetup.Open();
            if(GameObject.Find(Name)!=null){Debug.Log("Championship Night already installed; authored scene/prefab tuning preserved. Use Open Olympic Match Arena to review.");return;}
            if(!File.Exists(Recovery)&&!AssetDatabase.CopyAsset(OlympicArenaSetup.ScenePath,Recovery))throw new IOException("Could not preserve the current arena scene.");
            foreach(var folder in new[]{Root,Root+"/Materials",Root+"/Textures",Root+"/Meshes",Root+"/Prefabs",Root+"/Settings"})Folder(folder);
            d=AssetDatabase.LoadAssetAtPath<ArenaDimensions>(OlympicArenaSetup.Root+"/Settings/CompetitionDimensions.asset");if(d==null)throw new InvalidOperationException("Arena dimensions missing.");
            Materials();scores.Clear();clocks.Clear();lights.Clear();
            var environment=Object.FindAnyObjectByType<ArenaQualityController>();
            // These are visual-only replacements. Recoveries/old prefab assets retain the earlier artwork.
            foreach(string n in new[]{ArenaVenuePolishSetup.Name,"Team Benches and Coaches","Team Equipment Details","Officials Table"}){var old=environment.transform.Find(n);if(old!=null)Object.DestroyImmediate(old.gameObject);}
            ThemeFoundation(environment.transform);ReplaceSeating(environment.transform.Find("Spectator Stands"));
            var root=Child(Name,null);detail=Child("Fine Operations Props",root);
            Deck(root);TeamAreas(root);Officials(root);Bowl(root);Rigging(root);EventDisplays(root);Combine(detail,"OperationsDetail");
            var display=root.gameObject.AddComponent<ArenaEventDisplay>();display.scores=scores.ToArray();display.clocks=clocks.ToArray();
            var volume=root.gameObject.AddComponent<Volume>();volume.isGlobal=true;volume.priority=20;volume.sharedProfile=Profile(2);
            var presentation=root.gameObject.AddComponent<ChampionshipPresentation>();presentation.volume=volume;presentation.profiles=Enumerable.Range(0,3).Select(Profile).ToArray();presentation.poolLights=lights.ToArray();
            RefineDetails(root);
            var prefab=PrefabUtility.SaveAsPrefabAsset(root.gameObject,PrefabPath);Object.DestroyImmediate(root.gameObject);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,environment.transform);instance.name=Name;
            instance.GetComponent<ArenaEventDisplay>().match=Object.FindAnyObjectByType<PrototypeMatch>();
            instance.GetComponent<ChampionshipPresentation>().preferences=environment.preferences;
            instance.GetComponent<ChampionshipPresentation>().viewCamera=Object.FindAnyObjectByType<GameplayCamera>().GetComponent<UniversalAdditionalCameraData>();
            environment.fineProps=instance.transform.Find("Fine Operations Props").gameObject;
            environment.pipelines=Enumerable.Range(0,3).Select(Pipeline).Cast<RenderPipelineAsset>().ToArray();
            ConfigureLighting();BakeReflection(environment.transform);
            EditorUtility.SetDirty(environment);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            foreach(var m in temporary.ToArray())if(m!=null)Object.DestroyImmediate(m);temporary.Clear();rounded.Clear();
            Debug.Log("Championship Night saved in OlympicMatchArena. Pool/roster/animations/physics/HUD/entry are retained; deck, team operations, seating frames, displays and illumination are upgraded.");
        }
        static void RequireSaved()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save intended scene changes first; venue setup discards nothing.");
        }
        static Color Hex(string s){ColorUtility.TryParseHtmlString(s,out var c);return c;}
        static void Materials()
        {
            navy=Mat("Midnight enamel",Hex("#0B1830"),.3f,.18f);cobalt=Mat("Championship cobalt",Hex("#123B75"),.26f,.1f);
            slate=Mat("Official lane slate",Hex("#43566C"),.26f);graphite=Mat("Architectural graphite",Hex("#101A27"),.22f,.15f);
            silver=Mat("Brushed aluminum",Hex("#7890A2"),.56f,.78f);ivory=Mat("Competition white",Hex("#DDECF3"),.4f,.08f);
            cyan=Mat("Ice blue inlay",Hex("#36AAD0"),.35f,.15f);led=Mat("Ice LED",Hex("#BDEBFF"),.35f,0,Hex("#BDEBFF")*3.1f);
            screen=Mat("Digital midnight",Hex("#07172C"),0,0,Hex("#0C2340")*.3f,true);
            carpet=Mat("Event rubber carpet",Hex("#132B4A"),.12f);ceramic=Mat("Honed basalt ceramic",Hex("#536378"),.23f);glass=Mat("Night acoustic glazing",Hex("#132336"),.5f,.35f);
            seat=Mat("Dark cobalt upholstery",Hex("#193354"),.18f);
            foreach(var m in new[]{carpet,seat}){m.SetTexture("_BaseMap",Texture("FineRubber",false));m.SetTextureScale("_BaseMap",new Vector2(1.6f,1.6f));}
            foreach(var m in new[]{slate,ceramic}){m.SetTexture("_BaseMap",Texture("BasaltCeramic",true));m.SetTextureScale("_BaseMap",Vector2.one);}
            foreach(var m in new[]{carpet,slate,ceramic}){m.SetTexture("_BumpMap",Normal());m.SetFloat("_BumpScale",.16f);m.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(m);}
        }
        static Material Mat(string name,Color c,float rough,float metal=0,Color? emission=null,bool unlit=false)
        {
            string path=Root+"/Materials/"+name.Replace(" ","")+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;
            m=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit")){name=name,color=c,enableInstancing=true};
            if(!unlit){m.SetFloat("_Smoothness",rough);m.SetFloat("_Metallic",metal);}
            if(emission.HasValue&&!unlit){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",emission.Value);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;}
            AssetDatabase.CreateAsset(m,path);return m;
        }
        static Texture2D Texture(string name,bool tiled)
        {
            string path=Root+"/Textures/"+name+".png";
            if(!File.Exists(path))
            {
                const int n=256;var t=new Texture2D(n,n,TextureFormat.RGB24,false);var pixels=new Color32[n*n];var random=new System.Random(tiled?430:861);
                for(int y=0;y<n;y++)for(int x=0;x<n;x++){bool grout=tiled&&(x<2||y<2);float noise=(float)random.NextDouble()*.055f;byte c=(byte)((grout?.61f:.90f+noise)*255);pixels[y*n+x]=new Color32(c,c,c,255);}
                t.SetPixels32(pixels);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());Object.DestroyImmediate(t);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.wrapMode=TextureWrapMode.Repeat;imp.mipmapEnabled=true;imp.isReadable=false;imp.maxTextureSize=256;imp.anisoLevel=2;imp.textureCompression=TextureImporterCompression.CompressedHQ;imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static Texture2D Normal()
        {
            string path=Root+"/Textures/MicroFinishNormal.png";
            if(!File.Exists(path))
            {
                const int n=128;var t=new Texture2D(n,n,TextureFormat.RGB24,false);var p=new Color[n*n];var random=new System.Random(1391);
                for(int i=0;i<p.Length;i++)p[i]=new Color(.5f+((float)random.NextDouble()-.5f)*.09f,.5f+((float)random.NextDouble()-.5f)*.09f,1);
                t.SetPixels(p);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());Object.DestroyImmediate(t);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.NormalMap;imp.wrapMode=TextureWrapMode.Repeat;imp.maxTextureSize=128;imp.textureCompression=TextureImporterCompression.CompressedHQ;imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static void ThemeFoundation(Transform env)
        {
            foreach(var r in env.Find("Non-slip Deck and Referee Safety Paths").GetComponentsInChildren<Renderer>())r.sharedMaterial=r.name.Contains("fascia")?navy:ceramic;
            foreach(var r in env.Find("Aquatic Centre Architecture").GetComponentsInChildren<Renderer>())
            {
                string n=r.name.ToLowerInvariant();
                if(n.Contains("label")||r is TMPro.TMP_SubMesh||r.GetComponent<TMP_Text>()!=null)continue;
                r.sharedMaterial=n.Contains("concourse")?ceramic:n.Contains("glass")||n.Contains("daylight")?glass:n.Contains("white")||n.Contains("wall")||n.Contains("ceiling")?graphite:n.Contains("steel")||n.Contains("beam")||n.Contains("mullion")?silver:navy;
            }
            foreach(var mark in env.Find("Water Polo Field Markers — no race lanes").GetComponentsInChildren<TMP_Text>())
            {
                if(mark.text.Contains("TEAM ACCESS"))Object.DestroyImmediate(mark.gameObject);
                else if(mark.text.EndsWith("m"))mark.color=Hex("#A6BED0");
            }
        }
        static void ReplaceSeating(Transform stands)
        {
            string path=Root+"/Prefabs/ChampionshipSeatBlock.prefab";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(prefab==null)
            {
                var block=Child("Championship six tier seating",null);
                for(int row=0;row<6;row++)
                {
                    float h=.25f+row*.47f,x=row*.92f;Box("Tier plinth",block,new Vector3(x,h/2,0),new Vector3(.92f,h,6.5f),graphite);
                    Box("Tier edge",block,new Vector3(x-.46f,h-.035f,0),new Vector3(.025f,.05f,6.5f),silver);
                    for(int s=0;s<8;s++)
                    {
                        float z=(s-3.5f)*.76f;var m=(s+row)%4==0?cobalt:seat;
                        Bevel("Upholstered seat pan",block,new Vector3(x,h+.12f,z),new Vector3(.62f,.16f,.65f),.055f,m);
                        Bevel("Sculpted seat back",block,new Vector3(x+.27f,h+.43f,z),new Vector3(.14f,.6f,.66f),.045f,m);
                        for(int sign=-1;sign<=1;sign+=2)Box("Seat armrest",block,new Vector3(x+.02f,h+.32f,z+sign*.34f),new Vector3(.48f,.045f,.04f),silver);
                    }
                }
                foreach(int side in new[]{-1,1})
                {
                    for(int i=0;i<7;i++)Bar("Section rail upright",block,new Vector3(i*.82f,.42f+i*.4f,side*3.35f),new Vector3(i*.82f,1.48f+i*.4f,side*3.35f),.045f,silver);
                    Bar("Continuous section handrail",block,new Vector3(-.1f,1.45f,side*3.35f),new Vector3(5.1f,3.95f,side*3.35f),.055f,silver);
                }
                Combine(block,"Seating");prefab=PrefabUtility.SaveAsPrefabAsset(block.gameObject,path);Object.DestroyImmediate(block.gameObject);
            }
            foreach(Transform old in stands.Cast<Transform>().ToArray())Object.DestroyImmediate(old.gameObject);
            for(int side=-1;side<=1;side+=2)for(int b=-2;b<=2;b++)
            {
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,stands);go.name=(side<0?"West":"East")+" stand section "+(b+3);go.transform.localPosition=new Vector3(side*(W+d.deckWidth+.7f),Y,b*7.2f);if(side<0)go.transform.localRotation=Quaternion.Euler(0,180,0);
            }
        }
        static void Deck(Transform root)
        {
            var group=Child("Competition Deck and Clear Official Lanes",root);
            for(int side=-1;side<=1;side+=2)
            {
                Floor("Textured event apron",group,new Vector3(side*(W+4.8f),Y+.004f,0),new Vector2(4.1f,d.PoolLength+2),carpet);
                Floor("Official walkway",group,new Vector3(side*(W+1.4f),Y+.006f,0),new Vector2(1.75f,d.PoolLength+2),slate);
                foreach(float offset in new[]{.50f,2.30f})Floor("Official lane perimeter",group,new Vector3(side*(W+offset),Y+.013f,0),new Vector2(.045f,d.PoolLength+2),ivory);
                Floor("Blue technical separation",group,new Vector3(side*(W+2.45f),Y+.012f,0),new Vector2(.12f,d.PoolLength+2),cyan);
                for(int i=-4;i<=4;i++)Floor("Walkway guide dash",group,new Vector3(side*(W+2.1f),Y+.018f,i*3),new Vector2(.08f,.6f),cyan);
                Floor("End championship carpet",group,new Vector3(0,Y+.007f,side*(L+3.9f)),new Vector2(d.PoolWidth+14,5.8f),carpet);
                foreach(float offset in new[]{1,6.8f})Floor("End carpet boundary",group,new Vector3(0,Y+.018f,side*(L+offset)),new Vector2(d.PoolWidth+14,.065f),silver);
                foreach(float offset in new[]{.05f,.22f})Box("Pool edge inset trim",group,new Vector3(side*(W+offset),Y-.03f,0),new Vector3(.055f,.12f,d.PoolLength+.4f),offset<.1f?ivory:silver);
                Box("End pool trim",group,new Vector3(0,Y-.03f,side*(L+.05f)),new Vector3(d.PoolWidth+.4f,.12f,.055f),ivory);
                for(int i=-3;i<=3;i++)for(int g=0;g<8;g++)Box("Drain grate",group,new Vector3(side*(W+.34f),Y+.009f,i*4+g*.12f),new Vector3(.15f,.013f,.025f),graphite);
                Label("OFFICIALS  /  KEEP CLEAR",group,new Vector3(side*(W+1.35f),Y+.025f,side*-5),Quaternion.Euler(90,side>0?90:-90,0),7.5f,.55f,.23f,ivory.color);
                Label("WATER POLO SUPERSTARS",group,new Vector3(0,Y+.027f,side*(L+3.25f)),Quaternion.Euler(90,side>0?180:0,0),18,1.25f,.75f,ivory.color);
                Label("CHAMPIONSHIP  /  MATCH COURT 01",group,new Vector3(0,Y+.029f,side*(L+4.5f)),Quaternion.Euler(90,side>0?180:0,0),18,.55f,.25f,cyan.color);
                for(int x=-1;x<=1;x+=2){float px=x*(W+4.5f);Bevel("Deck corner protector",group,new Vector3(px,Y+.28f,side*(L+1)),new Vector3(1.8f,.56f,.42f),.08f,navy);Box("Corner light inset",group,new Vector3(px,Y+.47f,side*(L+.77f)),new Vector3(1.5f,.045f,.035f),led);}
            }
            Combine(group,"CompetitionDeck");
        }
        static void TeamAreas(Transform root)
        {
            var group=Child("Blue and White Team Technical Areas",root);
            foreach(int side in new[]{-1,1})
            {
                float z=side*6.8f,x=W+4.8f;string name=side<0?"BLUE":"WHITE";Material team=side<0?cobalt:ivory;
                var zone=Child(name+" technical area",group);
                Floor("Technical zone perimeter",zone,new Vector3(x,Y+.018f,z),new Vector2(3.4f,7.9f),team);
                Floor("Technical zone inset",zone,new Vector3(x,Y+.025f,z),new Vector2(3.28f,7.76f),carpet);
                Bevel("Technical backwall",zone,new Vector3(x+1.1f,Y+.95f,z),new Vector3(.18f,1.9f,7.6f),.06f,navy);
                for(int i=0;i<7;i++)
                {
                    float sz=z+(i-3)*.87f;
                    Bevel("Reserve padded seat",zone,new Vector3(x+.15f,Y+.5f,sz),new Vector3(.72f,.18f,.74f),.065f,seat);
                    Bevel("Reserve seat back",zone,new Vector3(x+.47f,Y+.88f,sz),new Vector3(.16f,.73f,.75f),.055f,seat);
                    Box("Seat team stripe",zone,new Vector3(x+.37f,Y+1.08f,sz),new Vector3(.018f,.12f,.57f),team);
                    Box("Bench pedestal",zone,new Vector3(x+.2f,Y+.24f,sz),new Vector3(.25f,.48f,.30f),silver);
                    foreach(int sign in new[]{-1,1})Bar("Bench armrest",zone,new Vector3(x-.15f,Y+.74f,sz+sign*.38f),new Vector3(x+.42f,Y+.74f,sz+sign*.38f),.045f,silver);
                }
                Box("Technical area header",zone,new Vector3(x+.98f,Y+1.77f,z),new Vector3(.10f,.38f,7.5f),cobalt);
                Box("Backwall LED trim",zone,new Vector3(x+.91f,Y+1.98f,z),new Vector3(.045f,.045f,7.4f),led);
                Label(name+"  /  TEAM TECHNICAL AREA",zone,new Vector3(x+.91f,Y+1.77f,z),Quaternion.Euler(0,90,0),7.1f,.30f,.25f,ivory.color);
                Label("RESERVES  +  STAFF",zone,new Vector3(x-.85f,Y+.04f,z),Quaternion.Euler(90,90,0),5.8f,.4f,.22f,ivory.color);
                // Two access gaps and equipment clusters remain outside the clear referee lane.
                float ez=z-side*3.3f;Bevel("Coach console",zone,new Vector3(x-.95f,Y+.54f,ez),new Vector3(.75f,1.08f,.8f),.06f,navy);
                Box("Coach tablet",detail,new Vector3(x-.95f,Y+1.12f,ez),new Vector3(.56f,.045f,.5f),screen);
                Equipment(detail,new Vector3(x-.75f,Y,z+side*3.25f),team);
                Floor("Substitution route",zone,new Vector3(W+3.5f,Y+.023f,side*(L-1.3f)),new Vector2(1.7f,1.35f),cobalt);
                Label("TEAM ACCESS",zone,new Vector3(W+3.5f,Y+.04f,side*(L-1.3f)),Quaternion.Euler(90,90,0),1.25f,.65f,.19f,ivory.color);
                Combine(zone,name+"TeamZone");
            }
        }
        static void Equipment(Transform parent,Vector3 p,Material team)
        {
            var g=Child("Team equipment cluster",parent);g.localPosition=p;
            Bevel("Insulated cooler",g,new Vector3(0,.37f,0),new Vector3(.78f,.7f,.82f),.07f,ivory);Bevel("Cooler lid",g,new Vector3(0,.76f,0),new Vector3(.84f,.13f,.88f),.04f,team);
            foreach(int s in new[]{-1,1})Bar("Cooler handle",g,new Vector3(s*.45f,.45f,-.2f),new Vector3(s*.45f,.45f,.2f),.055f,graphite);
            Bevel("Gear flight case",g,new Vector3(.95f,.39f,0),new Vector3(.65f,.76f,.86f),.06f,navy);
            foreach(int s in new[]{-1,1})Box("Case band",g,new Vector3(.95f+s*.22f,.39f,0),new Vector3(.055f,.78f,.88f),silver);
            Box("Case handle",g,new Vector3(.95f,.82f,0),new Vector3(.3f,.06f,.10f),graphite);
            for(int i=0;i<3;i++)Bevel("Folded towel",g,new Vector3(.95f,.87f+i*.055f,0),new Vector3(.48f,.055f,.48f),.015f,i==1?team:ivory);
            for(int i=0;i<3;i++){Cylinder("Water bottle",g,new Vector3(-.25f+i*.22f,.97f,0),.06f,.28f,cyan);Cylinder("Bottle cap",g,new Vector3(-.25f+i*.22f,1.13f,0),.04f,.05f,ivory);}
        }
        static void Officials(Transform root)
        {
            var g=Child("Match Operations and Protected Sidelines",root);float x=-W-4.6f;
            Bevel("Timing desk housing",g,new Vector3(x,Y+.58f,0),new Vector3(1.25f,1.16f,5.5f),.09f,navy);
            Bevel("Timing desk worktop",g,new Vector3(x,Y+1.2f,0),new Vector3(1.45f,.12f,5.8f),.045f,silver);
            Box("Timing desk LED reveal",g,new Vector3(x+.64f,Y+.24f,0),new Vector3(.025f,.055f,5.1f),led);
            Label("MATCH OPERATIONS",g,new Vector3(x+.64f,Y+.77f,0),Quaternion.Euler(0,-90,0),4.9f,.5f,.34f,ivory.color);
            for(int i=-1;i<=1;i++)
            {
                float z=i*1.65f;Bevel("Timing terminal",detail,new Vector3(x-.1f,Y+1.43f,z),new Vector3(.16f,.45f,.65f),.04f,graphite);
                Box("Timing terminal screen",detail,new Vector3(x,Y+1.44f,z),new Vector3(.015f,.33f,.51f),screen);
                Bevel("Official chair seat",g,new Vector3(x-1.15f,Y+.52f,z),new Vector3(.68f,.13f,.68f),.045f,seat);Bevel("Official chair back",g,new Vector3(x-1.44f,Y+.86f,z),new Vector3(.13f,.65f,.7f),.04f,seat);
                Bar("Official chair support",g,new Vector3(x-1.15f,Y+.08f,z),new Vector3(x-1.15f,Y+.46f,z),.09f,silver);
            }
            foreach(int s in new[]{-1,1})foreach(float z in new[]{-9.2f,0,9.2f})
            {
                var rail=Child("Sideline LED barrier",g);rail.localPosition=new Vector3(s*(W+3),Y+.6f,z);rail.localRotation=Quaternion.Euler(0,s>0?90:-90,0);
                Bevel("Barrier chassis",rail,Vector3.zero,new Vector3(5.6f,1.02f,.22f),.06f,navy);Box("Barrier digital face",rail,new Vector3(0,0,-.125f),new Vector3(5.4f,.75f,.025f),screen);
                Box("Barrier cyan edge",rail,new Vector3(0,.48f,-.125f),new Vector3(5.4f,.04f,.03f),led);
                Label(z==0?"CHAMPIONSHIP  /  WATER POLO":"WATER POLO SUPERSTARS",rail,new Vector3(0,.02f,-.15f),Quaternion.identity,5.1f,.54f,.27f,ivory.color);
                foreach(float dx in new[]{-2.1f,2.1f})Bevel("Barrier foot",rail,new Vector3(dx,-.48f,.06f),new Vector3(.44f,.08f,.58f),.025f,graphite);
            }
            Combine(g,"MatchOperations");
        }
        static void Bowl(Transform root)
        {
            var g=Child("Grandstand Portals and Championship Architecture",root);float wallX=W+d.deckWidth+7,wallZ=L+d.deckWidth+6;
            foreach(int side in new[]{-1,1})
            {
                for(int section=-2;section<=2;section++)
                {
                    float z=section*7.2f;
                    Bevel("Stand front fascia",g,new Vector3(side*(W+d.deckWidth+.20f),Y+.35f,z),new Vector3(.27f,.72f,6.7f),.055f,navy);
                    Box("Stand fascia luminous reveal",g,new Vector3(side*(W+d.deckWidth+.035f),Y+.58f,z),new Vector3(.035f,.04f,6.5f),led);
                    var title=Child("Section identity "+section,g);title.localPosition=new Vector3(side*(wallX-.30f),5.25f,z);title.localRotation=Quaternion.Euler(0,side>0?90:-90,0);
                    Bevel("Section plaque",title,Vector3.zero,new Vector3(5.8f,1.2f,.16f),.06f,navy);Label((side<0?"A":"B")+(section+3).ToString("00")+"   /   CHAMPIONSHIP",title,new Vector3(0,0,-.10f),Quaternion.identity,5.4f,.7f,.32f,ivory.color);
                    if(section<2)for(int step=0;step<12;step++)
                    {
                        float px=side*(W+d.deckWidth+.36f+step*.46f),h=Y+(step+1)*.235f;
                        Box("Concourse stair",g,new Vector3(px,h/2,z+3.6f),new Vector3(.46f,h,.64f),slate);Box("Step safe nosing",g,new Vector3(px-.16f*side,h+.014f,z+3.6f),new Vector3(.04f,.025f,.58f),ivory);
                    }
                }
                Box("Upper bowl ribbon",g,new Vector3(side*(wallX-.27f),6.8f,0),new Vector3(.18f,1.15f,wallZ*2),navy);
                foreach(float y in new[]{6.18f,7.42f})Box("Upper bowl LED edge",g,new Vector3(side*(wallX-.4f),y,0),new Vector3(.035f,.065f,wallZ*2),led);
                for(int bay=-3;bay<=3;bay++)
                {
                    float z=bay*7.2f;
                    Bevel("Upper structural pier",g,new Vector3(side*(wallX-.30f),18,z),new Vector3(.55f,24,.72f),.10f,navy);
                    for(int rib=-2;rib<=2;rib++)Box("Acoustic fin",g,new Vector3(side*(wallX-.15f),19,z+rib*.98f),new Vector3(.25f,21,.10f),graphite);
                    Box("Pier reveal",g,new Vector3(side*(wallX-.61f),18,z),new Vector3(.03f,19,.07f),cyan);
                }
                var end=Child("Championship end portal",g);end.localPosition=new Vector3(0,0,side*(wallZ-.45f));end.localRotation=Quaternion.Euler(0,side<0?180:0,0);
                foreach(int x in new[]{-1,1})Bevel("Portal pier",end,new Vector3(x*18.4f,9.2f,0),new Vector3(1.5f,18.4f,.6f),.18f,navy);
                Bevel("Portal championship header",end,new Vector3(0,15.9f,0),new Vector3(38.5f,2.4f,.68f),.22f,cobalt);
                Label("WATER POLO SUPERSTARS",end,new Vector3(0,16.1f,-.37f),Quaternion.identity,34,1.5f,1.15f,ivory.color);
                Label("CHAMPIONSHIP  FINALS",end,new Vector3(0,14.85f,-.37f),Quaternion.identity,30,.5f,.35f,cyan.color);
                foreach(float x in new[]{-18.4f,18.4f})Box("Portal edge lighting",end,new Vector3(x,9.2f,-.34f),new Vector3(.07f,16.6f,.04f),led);
                for(int x=-3;x<=3;x++){Box("End acoustic panel",end,new Vector3(x*5.2f,23,.25f),new Vector3(4.5f,12,.2f),graphite);Box("End panel joint",end,new Vector3(x*5.2f+2.4f,23,-.02f),new Vector3(.065f,12,.12f),silver);}
                foreach(int x in new[]{-1,1})
                {
                    var banner=Child("Suspended event banner",g);banner.localPosition=new Vector3(x*16.4f,8.4f,side*(wallZ-2.3f));banner.localRotation=Quaternion.Euler(0,side<0?180:0,0);
                    Bevel("Banner frame",banner,Vector3.zero,new Vector3(2.8f,5.4f,.11f),.055f,navy);Box("Banner cobalt panel",banner,new Vector3(0,.2f,-.075f),new Vector3(2.5f,4.8f,.025f),cobalt);
                    Label("WPS\n\nCHAMPIONSHIP\nSERIES",banner,new Vector3(0,.1f,-.095f),Quaternion.identity,2.35f,4.7f,.39f,ivory.color);Box("Banner illuminated hem",banner,new Vector3(0,-2.4f,-.08f),new Vector3(2.45f,.09f,.03f),led);
                    foreach(float px in new[]{-1.05f,1.05f})Bar("Banner suspension",banner,new Vector3(px,2.7f,0),new Vector3(px,20.8f,0),.025f,silver);
                }
            }
            Combine(g,"ChampionshipBowl");
        }
        static void Rigging(Transform root)
        {
            var g=Child("Perimeter Broadcast Rigging and Pool Light Banks",root);
            foreach(int side in new[]{-1,1})
            {
                float x=side*(W+3.0f);
                foreach(float y in new[]{18.2f,19.25f})Bar("Longitudinal truss chord",g,new Vector3(x,y,-19),new Vector3(x,y,19),.13f,silver);
                for(int i=0;i<16;i++)Bar("Truss web",g,new Vector3(x,i%2==0?18.2f:19.25f,-19+i*2.375f),new Vector3(x,i%2==0?19.25f:18.2f,-19+(i+1)*2.375f),.07f,silver);
                foreach(float z in new[]{-14f,0,14f}){Bar("Roof suspension cable",g,new Vector3(x,19.25f,z),new Vector3(x,29.5f,z),.028f,silver);Bar("Rig cross bracket",g,new Vector3(x-.6f,19.25f,z),new Vector3(x+.6f,19.25f,z),.08f,navy);}
                foreach(float z in new[]{-14f,-7f,0,7f,14f})
                {
                    var lamp=Child("Competition floodlight bank",g);lamp.localPosition=new Vector3(x,17.85f,z);lamp.localRotation=Quaternion.LookRotation(new Vector3(-side*.7f,-1,0));
                    Bevel("Floodlight housing",lamp,Vector3.zero,new Vector3(1.2f,.8f,.35f),.08f,graphite);Box("Floodlight lens",lamp,new Vector3(0,0,.19f),new Vector3(1.03f,.62f,.03f),led);
                    for(int k=-2;k<=2;k++)Box("Floodlight louvre",lamp,new Vector3(k*.20f,0,.22f),new Vector3(.035f,.64f,.035f),silver);
                    if(z==-7||z==7)
                    {
                        var light=Child("Pool focused light",g).gameObject.AddComponent<Light>();light.type=LightType.Spot;light.transform.localPosition=new Vector3(x,17.6f,z);light.transform.LookAt(new Vector3(side*2,0,z*.75f));light.range=31;light.spotAngle=74;light.innerSpotAngle=48;light.intensity=1000;light.color=Hex("#D9EAFF");light.shadows=LightShadows.None;light.renderMode=LightRenderMode.ForcePixel;lights.Add(light);
                    }
                }
            }
            // Existing high roof structure remains; the additional rig stays outside the playing-field sightlines.
            Combine(g,"PerimeterBroadcastRig");foreach(var r in g.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;
        }
        static void EventDisplays(Transform root)
        {
            var g=Child("Championship Scoreboards and Presentation Screens",root);
            var jumbo=Child("Four Face End Hung Championship Display",g);jumbo.localPosition=new Vector3(0,8.9f,L+d.deckWidth+1.1f);
            Bevel("Jumbotron machined housing",jumbo,Vector3.zero,new Vector3(12.8f,5.15f,3.9f),.24f,navy);
            foreach(float x in new[]{-5.6f,5.6f})foreach(float z in new[]{-1.4f,1.4f})Bar("Display suspension cable",jumbo,new Vector3(x,2.6f,z),new Vector3(x,20.5f,z),.045f,silver);
            foreach(int s in new[]{-1,1})
            {
                var f=Child("Long display face",jumbo);f.localPosition=new Vector3(0,0,s*2.0f);f.localRotation=Quaternion.Euler(0,s<0?0:180,0);Face(f,12.3f,4.6f);
                f=Child("Side display face",jumbo);f.localPosition=new Vector3(s*6.45f,0,0);f.localRotation=Quaternion.Euler(0,s<0?90:-90,0);Face(f,3.55f,4.6f);
                Box("Jumbotron lower luminous ring",jumbo,new Vector3(0,-2.46f,s*1.97f),new Vector3(12.1f,.07f,.06f),led);
            }
            Combine(jumbo,"ChampionshipJumbotron");
            foreach(int s in new[]{-1,1})
            {
                var board=Child(s>0?"North deck timing screen":"South deck timing screen",g);board.localPosition=new Vector3(0,Y+1,s*(L+6.1f));board.localRotation=Quaternion.Euler(0,s>0?0:180,0);
                Bevel("Poolside screen chassis",board,Vector3.zero,new Vector3(15,1.8f,.35f),.10f,navy);var f=Child("Digital face",board);f.localPosition=Vector3.back*.2f;Face(f,14.5f,1.55f);Combine(board,s>0?"NorthTimingScreen":"SouthTimingScreen");
            }
        }
        static void Face(Transform p,float w,float h)
        {
            Box("Dark LED display",p,Vector3.zero,new Vector3(w,h,.035f),screen);Box("Silver display bezel",p,new Vector3(0,h/2+.05f,0),new Vector3(w+.10f,.07f,.06f),silver);
            Box("Display cyan rule",p,new Vector3(0,-h/2+.06f,-.035f),new Vector3(w-.16f,.055f,.03f),led);
            Label("WATER POLO SUPERSTARS  /  CHAMPIONSHIP",p,new Vector3(0,h*.32f,-.035f),Quaternion.identity,w*.94f,h*.17f,Mathf.Min(h*.085f,w*.043f),cyan.color);
            scores.Add(Label("BLUE   0 : 0   WHITE",p,new Vector3(0,.02f,-.04f),Quaternion.identity,w*.94f,h*.38f,Mathf.Min(h*.21f,w*.08f),ivory.color));
            clocks.Add(Label("Q1  8:00   |   SHOT 28",p,new Vector3(0,-h*.29f,-.04f),Quaternion.identity,w*.94f,h*.20f,Mathf.Min(h*.115f,w*.045f),ivory.color));
        }
        static UniversalRenderPipelineAsset Pipeline(int level)
        {
            string p=Root+"/Settings/Championship_"+level+".asset";var a=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(p);if(a!=null)return a;
            AssetDatabase.CopyAsset("Assets/Arena/Settings/Arena_"+new[]{"Low","Balanced","High"}[level]+".asset",p);a=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(p);a.supportsHDR=true;a.maxAdditionalLightsCount=4;EditorUtility.SetDirty(a);return a;
        }
        static void RefineDetails(Transform root)
        {
            var upholstery=Mat("Technical area upholstery",Hex("#354E6D"),.24f);upholstery.SetTexture("_BaseMap",Texture("FineRubber",false));EditorUtility.SetDirty(upholstery);
            foreach(var r in root.GetComponentsInChildren<Renderer>())if((r.name.Contains("TeamZone")||r.name.StartsWith("MatchOperations"))&&r.sharedMaterial==seat)r.sharedMaterial=upholstery;
            led.SetColor("_EmissionColor",Hex("#9BD8F4")*2.0f);EditorUtility.SetDirty(led);
            var washes=root.Find("Technical Area Washlights");
            if(washes==null)
            {
                washes=Child("Technical Area Washlights",root);var list=new List<Light>();
                foreach(int side in new[]{-1,1})
                {
                    float z=side*6.8f;var wash=Child("Bench task wash",washes).gameObject.AddComponent<Light>();wash.type=LightType.Spot;wash.transform.localPosition=new Vector3(W+5.65f,Y+2.65f,z);wash.transform.LookAt(new Vector3(W+4.6f,Y+.4f,z));wash.range=6;wash.spotAngle=115;wash.innerSpotAngle=75;wash.intensity=25;wash.color=Hex("#BFDFFF");wash.shadows=LightShadows.None;list.Add(wash);
                    Bevel("Technical wash housing",washes,new Vector3(W+5.65f,Y+2.72f,z),new Vector3(.3f,.12f,2.5f),.04f,graphite);
                    Box("Technical wash diffuser",washes,new Vector3(W+5.65f,Y+2.65f,z),new Vector3(.22f,.035f,2.3f),led);
                    Bar("Technical fixture bracket",washes,new Vector3(W+5.65f,Y+1.9f,z),new Vector3(W+5.65f,Y+2.72f,z),.06f,silver);
                }
                Combine(washes,"TechnicalLighting");root.GetComponent<ChampionshipPresentation>().technicalLights=list.ToArray();
            }
            foreach(var portal in root.GetComponentsInChildren<Transform>().Where(t=>t.name=="Championship end portal").ToArray())
            {
                if(portal.Find("Sculpted water signature")!=null)continue;var signature=Child("Sculpted water signature",portal);signature.localPosition=Vector3.back*.4f;
                for(int line=0;line<3;line++)
                {
                    const int count=64;var vertices=new Vector3[(count+1)*2];var uv=new Vector2[vertices.Length];var triangles=new int[count*6];
                    for(int i=0;i<=count;i++){float x=-12+24f*i/count,y=22+line*1.0f+Mathf.Sin(x*.23f+line*.12f)*.62f;vertices[i*2]=new Vector3(x,y-.08f,0);vertices[i*2+1]=new Vector3(x,y+.08f,0);uv[i*2]=new Vector2((float)i/count,0);uv[i*2+1]=new Vector2((float)i/count,1);if(i<count){int t=i*6;triangles[t]=i*2;triangles[t+1]=i*2+1;triangles[t+2]=(i+1)*2;triangles[t+3]=i*2+1;triangles[t+4]=(i+1)*2+1;triangles[t+5]=(i+1)*2;}}
                    var mesh=new Mesh{name="Original championship water ribbon",vertices=vertices,uv=uv,triangles=triangles};mesh.RecalculateNormals();mesh.RecalculateBounds();temporary.Add(mesh);var band=Child("Sculpted wave ribbon",signature);band.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;band.gameObject.AddComponent<MeshRenderer>().sharedMaterial=line==1?cyan:silver;
                }
                Combine(signature,"ChampionshipWaterSignature");
            }
        }
        // Used by this art pass to apply the reviewed second draft without rebuilding any scene authorities.
        public static void ApplyReviewedRefinements()
        {
            RequireSaved();OlympicArenaSetup.Open();d=AssetDatabase.LoadAssetAtPath<ArenaDimensions>(OlympicArenaSetup.Root+"/Settings/CompetitionDimensions.asset");Materials();
            ThemeFoundation(Object.FindAnyObjectByType<ArenaQualityController>().transform);
            var root=PrefabUtility.LoadPrefabContents(PrefabPath);RefineDetails(root.transform);PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);PrefabUtility.UnloadPrefabContents(root);
            var presentation=Object.FindAnyObjectByType<ChampionshipPresentation>();presentation.viewCamera=Object.FindAnyObjectByType<GameplayCamera>().GetComponent<UniversalAdditionalCameraData>();PrefabUtility.RecordPrefabInstancePropertyModifications(presentation);
            BakeReflection(Object.FindAnyObjectByType<ArenaQualityController>().transform);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            foreach(var m in temporary.ToArray())if(m!=null)Object.DestroyImmediate(m);temporary.Clear();rounded.Clear();
        }
        static VolumeProfile Profile(int level)
        {
            string p=Root+"/Settings/ChampionshipGrade_"+level+".asset";var v=AssetDatabase.LoadAssetAtPath<VolumeProfile>(p);if(v!=null)return v;
            v=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(v,p);
            var tone=v.Add<Tonemapping>(true);tone.mode.value=TonemappingMode.Neutral;
            var color=v.Add<ColorAdjustments>(true);color.postExposure.value=.05f;color.contrast.value=12;color.saturation.value=-6;
            var vignette=v.Add<Vignette>(true);vignette.intensity.value=.13f;vignette.smoothness.value=.5f;
            var bloom=v.Add<Bloom>(true);bloom.intensity.value=level==0?0:level==1?.12f:.22f;bloom.threshold.value=1.1f;bloom.scatter.value=.5f;bloom.highQualityFiltering.value=false;bloom.downscale.value=BloomDownscaleMode.Quarter;bloom.maxIterations.value=4;bloom.filter.value=BloomFilterMode.Dual;
            foreach(var c in v.components)AssetDatabase.AddObjectToAsset(c,v);EditorUtility.SetDirty(v);return v;
        }
        static void ConfigureLighting()
        {
            var key=Object.FindObjectsByType<Light>().Single(l=>l.type==LightType.Directional);key.intensity=.78f;key.color=Hex("#D0E1FF");key.transform.rotation=Quaternion.Euler(66,-32,0);key.shadowStrength=.76f;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=Hex("#344767");RenderSettings.ambientEquatorColor=Hex("#1B2D49");RenderSettings.ambientGroundColor=Hex("#0B1628");RenderSettings.reflectionIntensity=.72f;RenderSettings.skybox=null;RenderSettings.sun=key;
            var cam=Object.FindAnyObjectByType<GameplayCamera>().GetComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Hex("#081323");cam.allowHDR=true;cam.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        }
        [MenuItem("Tools/Water Polo/Arena/Refresh Championship Night Reflection")]
        public static void RefreshReflection(){RequireSaved();OlympicArenaSetup.Open();if(GameObject.Find(Name)==null)throw new InvalidOperationException("Apply the championship venue first.");BakeReflection(Object.FindAnyObjectByType<ArenaQualityController>().transform);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();}
        static void BakeReflection(Transform env)
        {
            var probe=env.GetComponentInChildren<ReflectionProbe>();probe.mode=ReflectionProbeMode.Baked;probe.resolution=128;probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=Hex("#0B1628");probe.boxProjection=true;
            var water=Object.FindAnyObjectByType<PoolWaterVisuals>().waterSurface;bool enabled=water.enabled;water.enabled=false;
            try{WaterVisualTestSetup.BakePoolProbe(probe,env,env,Root+"/Textures/ChampionshipNightReflection.exr");}finally{water.enabled=enabled;}
        }

        static Transform Child(string name,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        static void Box(string name,Transform parent,Vector3 p,Vector3 size,Material m){var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=m;Object.DestroyImmediate(go.GetComponent<Collider>());}
        static void Bar(string n,Transform p,Vector3 a,Vector3 b,float width,Material m){var g=Child(n,p);g.localPosition=(a+b)/2;g.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);Box(n,g,Vector3.zero,new Vector3(width,(b-a).magnitude,width),m);}
        static void Cylinder(string n,Transform p,Vector3 center,float radius,float height,Material m){var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=n;go.transform.SetParent(p,false);go.transform.localPosition=center;go.transform.localScale=new Vector3(radius*2,height/2,radius*2);go.GetComponent<Renderer>().sharedMaterial=m;Object.DestroyImmediate(go.GetComponent<Collider>());}
        static void Floor(string name,Transform parent,Vector3 p,Vector2 size,Material m)
        {
            var mesh=new Mesh{name=name};mesh.vertices=new[]{new Vector3(-size.x/2,0,-size.y/2),new Vector3(size.x/2,0,-size.y/2),new Vector3(-size.x/2,0,size.y/2),new Vector3(size.x/2,0,size.y/2)};mesh.uv=new[]{Vector2.zero,new Vector2(size.x,0),new Vector2(0,size.y),size};mesh.triangles=new[]{0,2,1,1,2,3};mesh.RecalculateNormals();mesh.RecalculateTangents();temporary.Add(mesh);var t=Child(name,parent);t.localPosition=p;t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=m;
        }
        static TMP_Text Label(string value,Transform p,Vector3 pos,Quaternion rot,float width,float height,float meters,Color c)
        {
            var t=Child(value.Split('\n')[0],p);t.localPosition=pos;t.localRotation=rot;var text=t.gameObject.AddComponent<TextMeshPro>();text.text=value;text.fontSize=meters*10;text.enableAutoSizing=true;text.fontSizeMin=meters*6;text.fontSizeMax=meters*10;text.fontStyle=FontStyles.Bold;text.characterSpacing=1.5f;text.alignment=TextAlignmentOptions.Center;text.color=c;text.rectTransform.sizeDelta=new Vector2(width,height);text.textWrappingMode=TextWrappingModes.Normal;text.overflowMode=TextOverflowModes.Truncate;text.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;return text;
        }
        static void Bevel(string name,Transform parent,Vector3 p,Vector3 size,float b,Material m)
        {
            string key=size.ToString("F3")+b.ToString("F3");if(!rounded.TryGetValue(key,out var mesh)){mesh=BeveledMesh(size,b);rounded[key]=mesh;temporary.Add(mesh);}var t=Child(name,parent);t.localPosition=p;t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=m;
        }
        static Mesh BeveledMesh(Vector3 size,float bevel)
        {
            Vector3 h=size/2;float b=Mathf.Min(bevel,Mathf.Min(h.x,Mathf.Min(h.y,h.z))*.85f);var v=new List<Vector3>();var tri=new List<int>();var uv=new List<Vector2>();
            void Face(params Vector3[] points){int start=v.Count;Vector3 normal=Vector3.Cross(points[1]-points[0],points[2]-points[0]);Vector3 center=points.Aggregate(Vector3.zero,(a,p)=>a+p)/points.Length;bool reverse=Vector3.Dot(normal,center)<0;foreach(var p in points){v.Add(p);uv.Add(new Vector2(p.x+p.z,p.y+p.z));}for(int i=1;i<points.Length-1;i++){tri.Add(start);tri.Add(start+(reverse?i+1:i));tri.Add(start+(reverse?i:i+1));}}
            for(int axis=0;axis<3;axis++)for(int sign=-1;sign<=1;sign+=2)
            {
                int u=(axis+1)%3,w=(axis+2)%3;Vector3 A=Vector3.zero,U=Vector3.zero,V=Vector3.zero;A[axis]=sign*h[axis];U[u]=1;V[w]=1;float x=h[u],y=h[w];
                Face(A+U*(-x+b)-V*y,A+U*(x-b)-V*y,A+U*x+V*(-y+b),A+U*x+V*(y-b),A+U*(x-b)+V*y,A+U*(-x+b)+V*y,A-U*x+V*(y-b),A-U*x+V*(-y+b));
            }
            for(int axis=0;axis<3;axis++)for(int s=-1;s<=1;s+=2)for(int t=-1;t<=1;t+=2)
            {
                int u=(axis+1)%3,w=(axis+2)%3;Vector3 a=Vector3.zero,c=Vector3.zero,e=Vector3.zero,f=Vector3.zero;a[axis]=c[axis]=-(h[axis]-b);e[axis]=f[axis]=h[axis]-b;a[u]=e[u]=s*h[u];a[w]=e[w]=t*(h[w]-b);c[u]=f[u]=s*(h[u]-b);c[w]=f[w]=t*h[w];Face(a,c,f,e);
            }
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)Face(new Vector3(x*h.x,y*(h.y-b),z*(h.z-b)),new Vector3(x*(h.x-b),y*h.y,z*(h.z-b)),new Vector3(x*(h.x-b),y*(h.y-b),z*h.z));
            var mesh=new Mesh{name="Chamfered venue piece"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
        }
        static void Combine(Transform group,string name)
        {
            var filters=group.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<TextMeshPro>()==null).ToArray();
            foreach(var batch in filters.GroupBy(f=>f.GetComponent<Renderer>().sharedMaterial))
            {
                var mesh=new Mesh{name=name+" "+batch.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(batch.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=group.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);mesh.RecalculateBounds();
                string path=Root+"/Meshes/"+mesh.name.Replace(" ","")+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;}
                var go=Child(mesh.name,group);go.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;go.gameObject.AddComponent<MeshRenderer>().sharedMaterial=batch.Key;
            }
            foreach(var f in filters)Object.DestroyImmediate(f.gameObject);
            foreach(var t in group.GetComponentsInChildren<Transform>(true).Reverse())if(t!=group&&t.childCount==0&&t.GetComponents<Component>().Length==1)Object.DestroyImmediate(t.gameObject);
        }
        static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;int slash=path.LastIndexOf('/');Folder(path.Substring(0,slash));AssetDatabase.CreateFolder(path.Substring(0,slash),path.Substring(slash+1));}
    }
}
