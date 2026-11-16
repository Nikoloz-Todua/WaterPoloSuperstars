using System;
using System.IO;
using System.Linq;
using TMPro;
using ToduStudios.WaterPolo.Hub;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ToduStudios.WaterPolo.Editor
{
    /// <summary>Copies the proven match authorities once, then builds the arena from the visual fixture's materials/construction.</summary>
    public static class OlympicArenaSetup
    {
        public const string ScenePath = "Assets/Scenes/OlympicMatchArena.unity";
        public const string RecoveryPath = "Assets/Scenes/Recovery/PrototypePool_PreArena.unity";
        public const string Root = "Assets/Arena";
        public const string EnvironmentName = "Olympic Arena — WaterVisualTest Foundation";
        static ArenaDimensions dimensions;
        static Material tile, deck, white, steel, navy, blue, aqua, red, yellow, green, glass;

        [MenuItem("Tools/Water Polo/Recovery/Arena/Rebuild Base Olympic Match Arena")]
        public static void SetUp()
        {
            RequireSaved();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                Open();
                var configured=Object.FindAnyObjectByType<ArenaQualityController>();
                if(configured==null || configured.water==null || configured.water.waterSurface==null || Object.FindObjectsByType<ArenaAthleteVisual>().Length!=14)
                    throw new InvalidOperationException("Existing arena is incomplete; inspect and resume its setup. No authored scene was replaced.");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(WaterVisualTestSetup.ScenePath) == null)
                throw new InvalidOperationException("WaterVisualTest must exist and pass its independent visual verification first.");
            Folder(Root); Folder(Root+"/Materials"); Folder(Root+"/Meshes"); Folder(Root+"/Prefabs"); Folder(Root+"/Settings"); Folder(Root+"/Textures");
            Folder("Assets/Scenes/Recovery");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RecoveryPath) == null && !AssetDatabase.CopyAsset("Assets/Scenes/PrototypePool.unity",RecoveryPath))
                throw new IOException("Could not create the original gameplay recovery scene.");
            if (!AssetDatabase.CopyAsset("Assets/Scenes/PrototypePool.unity",ScenePath)) throw new IOException("Could not copy the existing gameplay scene.");
            var scene = EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            PrepareAssets();
            // Retain physics and goals. Retire all old pool/deck render geometry only in the NEW scene.
            var environment = GameObject.Find("Environment").transform;
            foreach (var name in new[]{PoolWaterGameplaySetup.RootName,"WaterSurface","PoolFloor","PoolMarkings"})
                if (environment.Find(name) != null) Object.DestroyImmediate(environment.Find(name).gameObject);
            foreach (var renderer in environment.Find("PoolBoundaries").GetComponentsInChildren<Renderer>(true)) renderer.enabled=false;
            foreach (var name in new[]{"RefereeDeckEast","RefereeDeckWest"})
                if(GameObject.Find("GameplayRealism").transform.Find(name)!=null) Object.DestroyImmediate(GameObject.Find("GameplayRealism").transform.Find(name).gameObject);
            foreach(var goal in Object.FindObjectsByType<GoalMouth>())
                foreach(var r in goal.GetComponentsInChildren<Renderer>(true))
                    if(r.name.StartsWith("EndBoundary") || r.name=="PocketWater" || r.name=="LeftNet" || r.name=="RightNet" || r.name=="BackNet")r.enabled=false;
            ConfigureDimensions();
            BuildEnvironment();
            BuildAthletes();
            ConfigureCamera(true);
            ConfigureHud();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("OlympicMatchArena created. Original scenes preserved. Verify direct arena, then Activate Verified Arena for Lobby.");
        }

        [MenuItem("Tools/Water Polo/Arena/Open Olympic Match Arena")]
        public static void Open() { RequireSaved(); EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single); }

        [MenuItem("Tools/Water Polo/Play Olympic Match Arena")]
        public static void PlayDirect() { Open(); EditorSceneManager.playModeStartScene=null; EditorApplication.isPlaying=true; }

        [MenuItem("Tools/Water Polo/Recovery/Arena/Rebuild From Configured Field Dimensions")]
        public static void ApplyDimensions()
        {
            Open(); PrepareAssets(); ConfigureDimensions();
            var old=GameObject.Find(EnvironmentName); if(old!=null) Object.DestroyImmediate(old);
            BuildEnvironment(); ConfigureCamera(); ConfigureHud();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Water Polo/Recovery/Arena/Activate Verified Arena for Lobby")]
        public static void Activate()
        {
            RequireSaved();
            if (!File.Exists("Logs/Arena/Integration-PASS.json")) throw new InvalidOperationException("Run the arena integration verification before changing the production entry path.");
            var paths=new[]{Milestone12HubSetup.BootPath,Milestone12HubSetup.LobbyPath,ScenePath};
            var extras=EditorBuildSettings.scenes.Where(x=>!paths.Contains(x.path) && x.path!="Assets/Scenes/PrototypePool.unity" && x.path!=RecoveryPath && x.path!=WaterVisualTestSetup.ScenePath).ToArray();
            EditorBuildSettings.scenes=paths.Select(p=>new EditorBuildSettingsScene(p,true)).Concat(extras).ToArray();
            var route=AssetDatabase.LoadAssetAtPath<HubMatchDestination>("Assets/Resources/HubMatchDestination.asset");
            if(route==null){route=ScriptableObject.CreateInstance<HubMatchDestination>();AssetDatabase.CreateAsset(route,"Assets/Resources/HubMatchDestination.asset");}
            route.sceneName="OlympicMatchArena"; EditorUtility.SetDirty(route); AssetDatabase.SaveAssets();
            Milestone12HubSetup.SetBootEntry();
        }

        static void RequireSaved()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save intended scene edits first; arena setup discards nothing.");
        }
        static void PrepareAssets()
        {
            dimensions=AssetDatabase.LoadAssetAtPath<ArenaDimensions>(Root+"/Settings/CompetitionDimensions.asset");
            if(dimensions==null){dimensions=ScriptableObject.CreateInstance<ArenaDimensions>();AssetDatabase.CreateAsset(dimensions,Root+"/Settings/CompetitionDimensions.asset");}
            tile=AssetDatabase.LoadAssetAtPath<Material>(WaterVisualTestSetup.Root+"/Materials/Poolceramic.mat");
            white=AssetDatabase.LoadAssetAtPath<Material>(WaterVisualTestSetup.Root+"/Materials/Whiteenamel.mat");
            steel=AssetDatabase.LoadAssetAtPath<Material>(WaterVisualTestSetup.Root+"/Materials/Brushedsteel.mat");
            navy=AssetDatabase.LoadAssetAtPath<Material>(WaterVisualTestSetup.Root+"/Materials/Competitionblue.mat");
            glass=AssetDatabase.LoadAssetAtPath<Material>(WaterVisualTestSetup.Root+"/Materials/Daylightpanels.mat");
            bool newDeck=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Non-sliplimestone.mat")==null;
            deck=Lit("Non-slip limestone",new Color(.72f,.73f,.69f),.13f);
            if(newDeck){deck.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(WaterVisualTestSetup.Root+"/Textures/PoolTiles.png")); EditorUtility.SetDirty(deck);}
            blue=Lit("Team blue",new Color(.035f,.27f,.65f),.3f); aqua=Lit("Arena turquoise",new Color(.025f,.52f,.58f),.3f);
            green=Lit("Midfield green",new Color(.02f,.45f,.25f),.25f);
            red=Lit("Two metre red",new Color(.83f,.075f,.055f),.25f); yellow=Lit("Six metre gold",new Color(.98f,.65f,.06f),.25f);
        }
        static void ConfigureDimensions()
        {
            var pool=Object.FindAnyObjectByType<PoolBounds>(); Set(pool,"width",dimensions.fieldWidth);Set(pool,"length",dimensions.fieldLength);
            var rulesData=new SerializedObject(Object.FindAnyObjectByType<MatchRules>());rulesData.FindProperty("useCompetitionReentryArea").boolValue=true;rulesData.ApplyModifiedPropertiesWithoutUndo();
            var c=pool.transform.position;float half=dimensions.fieldLength*.5f;
            foreach(var goal in Object.FindObjectsByType<GoalMouth>())
            {
                float sign=Mathf.Sign(goal.transform.position.z-c.z);
                goal.transform.position=new Vector3(c.x,pool.Waterline,c.z+sign*half);
                Set(goal,"postThickness",.08f);
                goal.GetComponent<PrototypeGoalView>().Refresh();
            }
            foreach(var team in Object.FindObjectsByType<PrototypeTeamTactics>())
            {
                var data=new SerializedObject(team);var array=data.FindProperty("formation");var defaults=FieldFormation.Defaults();
                for(int i=0;i<array.arraySize;i++)
                {
                    var e=array.GetArrayElementAtIndex(i);var baseline=defaults.Single(f=>(int)f.role==e.FindPropertyRelative("role").intValue);
                    float shift=half-15f;
                    e.FindPropertyRelative("periodStart").vector2Value=new Vector2(baseline.periodStart.x*dimensions.fieldWidth/20f,baseline.periodStart.y*dimensions.fieldLength/30f);
                    e.FindPropertyRelative("attack").vector2Value=new Vector2(baseline.attack.x*dimensions.fieldWidth/20f,baseline.attack.y+shift);
                    e.FindPropertyRelative("zone").vector2Value=new Vector2(baseline.zone.x*dimensions.fieldWidth/20f,baseline.zone.y-shift);
                }
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach(var player in Object.FindAnyObjectByType<PrototypeRoster>().Members)
            {
                var team=Object.FindObjectsByType<PrototypeTeamTactics>().Single(t=>t.Team==player.Team);
                Vector3 point=player.IsFieldPlayer?team.Formation(player):team.DefendedGoal.transform.position-team.DefendedGoal.transform.forward*1.4f;
                point.y=pool.Waterline;var rot=Quaternion.LookRotation(team.AttackDirection);
                player.StartPosition.SetPositionAndRotation(point,rot); player.transform.SetPositionAndRotation(point,rot);
            }
            var boundaries=GameObject.Find("Environment/PoolBoundaries").transform;
            foreach(Transform edge in boundaries)
            {
                bool side=edge.name.Contains("Left")||edge.name.Contains("Right");float sign=edge.name.Contains("Left")||edge.name.Contains("Near")?-1:1;
                edge.position=c+new Vector3(side?sign*(dimensions.fieldWidth/2+.25f):0,0,side?0:sign*(half+.25f));
                edge.localScale=side?new Vector3(.5f,.5f,dimensions.fieldLength+1):new Vector3(dimensions.fieldWidth,.5f,.5f);
            }
            foreach(var referee in Object.FindObjectsByType<PoolsideReferee>())
            {Set(referee,"deckOffset",dimensions.sideWaterBuffer+1.4f);Set(referee,"deckHeight",dimensions.deckElevation);}
        }

        static void BuildEnvironment()
        {
            var sun=Object.FindAnyObjectByType<Light>();sun.intensity=1.6f;sun.color=new Color(1,.96f,.89f);sun.shadowStrength=.6f;sun.transform.rotation=Quaternion.Euler(48,-35,0);
            RenderSettings.sun=sun;RenderSettings.skybox=null;RenderSettings.fog=false;RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.63f,.72f,.79f);RenderSettings.ambientEquatorColor=new Color(.42f,.53f,.59f);RenderSettings.ambientGroundColor=new Color(.22f,.31f,.35f);RenderSettings.reflectionIntensity=.8f;
            var pool=Object.FindAnyObjectByType<PoolBounds>(); var root=Child(EnvironmentName,null);
            root.position=new Vector3(pool.transform.position.x,pool.Waterline,pool.transform.position.z);
            var shell=Child("Tiled Competition Pool",root);float w=dimensions.PoolWidth,l=dimensions.PoolLength,d=dimensions.depth;
            Surface("Tiled Floor",shell,new Vector3(0,-d,0),Vector3.right*w,Vector3.forward*l,tile);
            Surface("Far tiled wall",shell,new Vector3(0,-d/2,l/2),Vector3.right*w,Vector3.up*d,tile);
            Surface("Near tiled wall",shell,new Vector3(0,-d/2,-l/2),Vector3.left*w,Vector3.up*d,tile);
            Surface("Left tiled wall",shell,new Vector3(-w/2,-d/2,0),Vector3.forward*l,Vector3.up*d,tile);
            Surface("Right tiled wall",shell,new Vector3(w/2,-d/2,0),Vector3.back*l,Vector3.up*d,tile);
            var deckRoot=Child("Non-slip Deck and Referee Safety Paths",root);float dw=dimensions.deckWidth,h=dimensions.deckElevation;
            for(int sign=-1;sign<=1;sign+=2)
            {
                Surface("Side tiled deck "+sign,deckRoot,new Vector3(sign*(w/2+dw/2),h,0),Vector3.right*dw,Vector3.forward*(l+dw*2),deck);
                Surface("End tiled deck "+sign,deckRoot,new Vector3(0,h,sign*(l/2+dw/2)),Vector3.right*w,Vector3.forward*dw,deck);
                Box("Stone fascia",deckRoot,new Vector3(sign*(w/2+dw/2),h-.225f,0),new Vector3(dw,.4f,l+dw*2),deck);
                Box("End fascia",deckRoot,new Vector3(0,h-.225f,sign*(l/2+dw/2)),new Vector3(w,.4f,dw),deck);
                Box("Side coping",shell,new Vector3(sign*(w/2+.12f),.07f,0),new Vector3(.3f,.22f,l+.6f),white);
                Box("End coping",shell,new Vector3(0,.07f,sign*(l/2+.12f)),new Vector3(w,.22f,.3f),white);
                Box("Side drainage channel",deckRoot,new Vector3(sign*(w/2+.52f),h+.005f,0),new Vector3(.2f,.012f,l),steel);
                Box("End drainage channel",deckRoot,new Vector3(0,h+.005f,sign*(l/2+.52f)),new Vector3(w,.012f,.2f),steel);
                Box("Waterline mosaic band",shell,new Vector3(sign*(w/2-.006f),-.15f,0),new Vector3(.012f,.16f,l),navy);
                Box("Referee path inlay",deckRoot,new Vector3(sign*(w/2+1.4f),h+.006f,0),new Vector3(.06f,.009f,dimensions.fieldLength),aqua);
            }
            BuildMarkings(root);
            var architecture=BuildHall(root);
            BuildBenches(root);
            var audience=BuildStands(root);
            var water=Surface("Competition Transparent Water",root,Vector3.zero,Vector3.right*w,Vector3.forward*l,AssetDatabase.LoadAssetAtPath<Material>(WaterVisualTestSetup.Root+"/Materials/CompetitionPool_High.mat"));
            water.gameObject.layer=4;water.shadowCastingMode=ShadowCastingMode.Off;water.receiveShadows=false;
            var visual=root.gameObject.AddComponent<PoolWaterVisuals>();visual.waterSurface=water;visual.viewCamera=Object.FindAnyObjectByType<GameplayCamera>().GetComponent<Camera>();
            visual.useIsolatedTestPipeline=false;visual.keyboardShortcuts=false;
            visual.profiles=CreateWaterProfiles();
            var reflection=Child("High Planar Reflections",root);var manager=reflection.gameObject.AddComponent<PlanarReflectionManager>();manager.runOnEditMode=false;manager.enabled=false;
            var volume=reflection.gameObject.AddComponent<PlanarReflectionVolume>();volume.enabled=false;volume.isGlobal=false;volume.volumeSize=new Vector3(160,100,180);volume.reflectionTargets.Add(water.gameObject);volume.renderScale=.3f;volume.hideReflectionCamera=true;
            visual.reflectionManager=manager;visual.reflectionVolume=volume;
            var probe=Child("Baked Arena Reflection",root).gameObject.AddComponent<ReflectionProbe>();probe.transform.localPosition=Vector3.up*.5f;
            probe.size=new Vector3(w+dw*2,28,l+dw*2);probe.resolution=128;probe.boxProjection=true;probe.cullingMask=~(1<<4);probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=new Color(.56f,.7f,.79f);
            WaterVisualTestSetup.BakePoolProbe(probe,root,root,Root+"/Textures/ArenaReflection.exr");
            var quality=root.gameObject.AddComponent<ArenaQualityController>();quality.dimensions=dimensions;quality.water=visual;quality.preferences=Object.FindAnyObjectByType<GamePreferences>();
            quality.pipelines=CreatePipelines();quality.mediumAudience=audience[0].gameObject;quality.highAudience=audience[1].gameObject;quality.fineProps=root.Find("Team Equipment Details").gameObject;
            var atmosphere=Object.FindAnyObjectByType<MatchAtmosphere>();Set(atmosphere,"crowd",CreateCrowd());
        }

        static void BuildMarkings(Transform root)
        {
            var marks=Child("Water Polo Field Markers — no race lanes",root);float half=dimensions.fieldLength/2,x=dimensions.fieldWidth/2;
            var floats=Child("Boundary Floats",marks);
            for(int side=-1;side<=1;side+=2)
            {
                for(int i=0;i<=Mathf.CeilToInt(dimensions.fieldLength/.32f);i++)
                {
                    float z=Mathf.Lerp(-half,half,(float)i/Mathf.CeilToInt(dimensions.fieldLength/.32f));float fromGoal=half-Mathf.Abs(z);
                    var m=fromGoal<2?red:fromGoal<6?yellow:green;if(Mathf.Abs(z)<.25f)m=white;
                    Box("Side field float",floats,new Vector3(side*x,.025f,z),new Vector3(.11f,.11f,.24f),m);
                }
                for(int i=0;i<56;i++)
                {
                    float px=Mathf.Lerp(-x,x,(float)i/55);if(Mathf.Abs(px)<1.65f)continue;
                    Box("Goal line float",floats,new Vector3(px,.025f,side*(half+.3f)),new Vector3(.26f,.11f,.11f),Mathf.Abs(px)<3.58f || px>x-2?red:white);
                }
                // 2m red / 6m yellow side indicators; 5m penalty reference is a short deck tick.
                foreach(float distance in new[]{0f,2f,5f,6f})
                {
                    float z=side*(half-distance);Material m=distance==2||distance==5?red:distance==6?yellow:white;
                    for(int edge=-1;edge<=1;edge+=2)
                    {
                        Box(distance+"m coping tick",marks,new Vector3(edge*(dimensions.PoolWidth/2+.14f),.19f,z),new Vector3(.65f,.018f,.12f),m);
                        Label(distance+"m",marks,new Vector3(edge*(dimensions.PoolWidth/2+.65f),.2f,z),Quaternion.Euler(90,edge>0?90:-90,0),.36f,navy.color);
                    }
                }
                Box("Centre coping tick",marks,new Vector3(side*(dimensions.PoolWidth/2+.14f),.19f,0),new Vector3(.65f,.018f,.13f),white);
                // Visual access areas only; no unsupported substitute gameplay is introduced.
                Box("Flying substitution separator",marks,new Vector3(x+.78f,.025f,side*half/2),new Vector3(.08f,.10f,half),red);
                Box("Re-entry back rope",marks,new Vector3(x-1,.025f,side*(half+1.05f)),new Vector3(2,.1f,.1f),red);
                Box("Re-entry side rope",marks,new Vector3(x-2,.025f,side*(half+.55f)),new Vector3(.1f,.1f,1.1f),red);
                // Subtle floor outline of the 2m goal area, rather than swimming race lanes.
                Box("Goal-area floor line",marks,new Vector3(0,-dimensions.depth+.014f,side*(half-2)),new Vector3(7.16f,.012f,.045f),red);
                foreach(int edge in new[]{-1,1})Box("Goal-area floor side",marks,new Vector3(edge*3.58f,-dimensions.depth+.014f,side*(half-1)),new Vector3(.045f,.012f,2),red);
                Label(side<0?"BLUE / TEAM ACCESS":"WHITE / TEAM ACCESS",marks,new Vector3(dimensions.PoolWidth/2+2.7f,dimensions.deckElevation+.015f,side*6.5f),Quaternion.Euler(90,90,0),.42f,side<0?new Color(.04f,.25f,.6f):Color.white);
            }
            Box("Centre floor mark",marks,new Vector3(0,-dimensions.depth+.012f,0),new Vector3(dimensions.fieldWidth,.012f,.055f),navy);
            var ring=Child("Centre circle",marks);
            for(int i=0;i<40;i++)
            {
                float a=i*Mathf.PI*2/40,b=(i+1)*Mathf.PI*2/40;
                Bar("Centre ring",ring,new Vector3(Mathf.Cos(a),-dimensions.depth+.02f,Mathf.Sin(a)),new Vector3(Mathf.Cos(b),-dimensions.depth+.02f,Mathf.Sin(b)),.035f,navy);
            }
            Combine(floats,"BoundaryFloats");Combine(ring,"CentreRing");
        }

        static Transform BuildHall(Transform root)
        {
            var hall=Child("Aquatic Centre Architecture",root);float w=dimensions.PoolWidth+dimensions.deckWidth*2+14,l=dimensions.PoolLength+dimensions.deckWidth*2+12;
            float innerWidth=dimensions.PoolWidth+dimensions.deckWidth*2,innerLength=dimensions.PoolLength+dimensions.deckWidth*2;
            for(int side=-1;side<=1;side+=2)
            {
                Surface("Spectator concourse "+side,hall,new Vector3(side*(innerWidth/2+3.5f),dimensions.deckElevation,0),Vector3.right*7,Vector3.forward*l,deck);
                Surface("End safety concourse "+side,hall,new Vector3(0,dimensions.deckElevation,side*(innerLength/2+3)),Vector3.right*innerWidth,Vector3.forward*6,deck);
            }
            for(int side=-1;side<=1;side+=2)
            {
                Box("End arena wall",hall,new Vector3(0,8,side*l/2),new Vector3(w,16,.35f),white);
                Box("End competition fascia",hall,new Vector3(0,2,side*(l/2-.22f)),new Vector3(w,3,.12f),navy);
                Box("Side arena wall",hall,new Vector3(side*w/2,8,0),new Vector3(.35f,16,l),white);
                Box("Side turquoise fascia",hall,new Vector3(side*(w/2-.22f),2,0),new Vector3(.12f,3,l),aqua);
                for(int i=-3;i<=3;i++)
                {
                    Box("High daylight panel",hall,new Vector3(i*6,10,side*(l/2-.25f)),new Vector3(5.6f,6,.08f),glass);
                    Box("Window mullion",hall,new Vector3(i*6,10,side*(l/2-.32f)),new Vector3(.06f,6,.12f),steel);
                    Box("Side daylight panel",hall,new Vector3(side*(w/2-.25f),10,i*6),new Vector3(.08f,6,5.6f),glass);
                }
            }
            var overhead=Child("Roof Trusses and Luminaires",hall);
            // The high hall keeps the adaptive broadcast camera below the ceiling at every end.
            for(int z=-3;z<=3;z++)
            {
                Box("Daylight roof strip",overhead,new Vector3(0,30,z*7),new Vector3(w,.09f,2.2f),glass);
                Box("Structural roof beam",overhead,new Vector3(0,29.5f,z*7+1.5f),new Vector3(w,.25f,.3f),steel);
                for(int s=-1;s<=1;s+=2)Bar("Roof diagonal",overhead,new Vector3(0,29.5f,z*7+1.5f),new Vector3(s*w/2,27.5f,z*7+1.5f),.12f,steel);
                foreach(float x in new[]{-8f,8f})Box("Competition light fixture",overhead,new Vector3(x,27,z*7),new Vector3(2.8f,.12f,.5f),glass);
            }
            Surface("Ceiling",overhead,new Vector3(0,30.2f,0),Vector3.left*w,Vector3.forward*l,white);
            for(int i=-2;i<=2;i++)
            {
                float px=i*8.3f;Box("Brand banner",hall,new Vector3(px,5.2f,l/2-.4f),new Vector3(7.8f,1.5f,.08f),i%2==0?navy:aqua);
                Label(i==0?"WATER POLO SUPERSTARS":"TODUSTUDIOS  /  OWN THE CURRENT",hall,new Vector3(px,5.2f,l/2-.47f),Quaternion.identity,i==0?.43f:.3f,Color.white);
            }
            Label("WATER POLO SUPERSTARS",hall,new Vector3(0,1.7f,-l/2+.25f),Quaternion.Euler(0,180,0),.8f,Color.white);
            Combine(overhead,"RoofStructure");foreach(var renderer in overhead.GetComponentsInChildren<Renderer>())renderer.shadowCastingMode=ShadowCastingMode.Off;return hall;
        }

        static void BuildBenches(Transform root)
        {
            var teams=Child("Team Benches and Coaches",root);var details=Child("Team Equipment Details",root);
            foreach(int sign in new[]{-1,1})
            {
                string team=sign<0?"Blue":"White";string path=Root+"/Prefabs/"+team+"TeamBench.prefab";
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(prefab==null)
                {
                    var bench=Child(team+" Team Bench",null);var fabric=sign<0?blue:white;
                    Box("Team zone mat",bench,new Vector3(0,.012f,0),new Vector3(2.8f,.024f,7),navy);
                    for(int i=0;i<7;i++)
                    {
                        float z=(i-3)*.8f;
                        Box("Substitute seat",bench,new Vector3(.15f,.48f,z),new Vector3(.65f,.15f,.68f),fabric);
                        Box("Seat back",bench,new Vector3(.46f,.85f,z),new Vector3(.12f,.64f,.68f),fabric);
                        foreach(float dz in new[]{-.24f,.24f})Box("Bench legs",bench,new Vector3(.3f,.23f,z+dz),new Vector3(.12f,.46f,.09f),steel);
                    }
                    Box("Coach chair",bench,new Vector3(-.8f,.48f,-2.5f),new Vector3(.7f,.15f,.7f),fabric);
                    Box("Coach back",bench,new Vector3(-.5f,.85f,-2.5f),new Vector3(.12f,.64f,.7f),fabric);
                    Box("Equipment trunk",bench,new Vector3(-.7f,.4f,2.5f),new Vector3(.8f,.8f,.8f),steel);
                    Label(team.ToUpperInvariant()+" TEAM",bench,new Vector3(-1.35f,.8f,0),Quaternion.Euler(0,-90,0),.32f,Color.white);
                    Combine(bench,team+"BenchMesh");prefab=PrefabUtility.SaveAsPrefabAsset(bench.gameObject,path);Object.DestroyImmediate(bench.gameObject);
                }
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,teams);instance.transform.localPosition=new Vector3(dimensions.PoolWidth/2+4.8f,dimensions.deckElevation,sign*6.8f);
                for(int i=0;i<5;i++)
                {
                    var pos=new Vector3(dimensions.PoolWidth/2+3.8f,dimensions.deckElevation+.15f,sign*6.8f+(i-2)*.42f);
                    Sphere("Team bottle",details,pos,new Vector3(.12f,.32f,.12f),aqua);
                    Box("Folded towel",details,pos+new Vector3(.3f,-.07f,0),new Vector3(.25f,.12f,.18f),white);
                }
            }
            Combine(details,"TeamEquipment");
            var table=Child("Officials Table",root);Box("Scorers table",table,new Vector3(-dimensions.PoolWidth/2-4,dimensions.deckElevation+.8f,0),new Vector3(1.2f,.15f,4),navy);
            foreach(float z in new[]{-1.4f,1.4f})Box("Table support",table,new Vector3(-dimensions.PoolWidth/2-4,dimensions.deckElevation+.4f,z),new Vector3(.9f,.8f,.1f),steel);
        }

        static Transform[] BuildStands(Transform root)
        {
            var stands=Child("Spectator Stands",root);var medium=Child("Audience Medium",root);var high=Child("Audience High",root);
            string path=Root+"/Prefabs/TieredSeatingBlock.prefab";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(prefab==null)
            {
                var block=Child("Tiered Seating Block",null);
                for(int row=0;row<6;row++)
                {
                    float y=.25f+row*.47f,x=row*.92f;
                    Box("Tier concrete",block,new Vector3(x,y/2,0),new Vector3(.92f,y,6.5f),deck);
                    for(int seat=0;seat<8;seat++)
                    {
                        float z=(seat-3.5f)*.76f;var color=(row+seat)%4==0?aqua:blue;
                        Box("Seat pan",block,new Vector3(x,y+.12f,z),new Vector3(.56f,.12f,.6f),color);
                        Box("Seat back",block,new Vector3(x+.25f,y+.4f,z),new Vector3(.10f,.5f,.6f),color);
                    }
                }
                foreach(float z in new[]{-3.35f,3.35f})
                {
                    for(int i=0;i<7;i++)Box("Rail post",block,new Vector3(i*.82f,.9f+i*.4f,z),new Vector3(.045f,1.1f,.045f),steel);
                    Bar("Hand rail",block,new Vector3(-.1f,1.45f,z),new Vector3(5.1f,3.95f,z),.045f,steel);
                }
                Combine(block,"SeatingBlock");prefab=PrefabUtility.SaveAsPrefabAsset(block.gameObject,path);Object.DestroyImmediate(block.gameObject);
            }
            for(int side=-1;side<=1;side+=2)
            {
                for(int b=-2;b<=2;b++)
                {
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,stands);instance.transform.localPosition=new Vector3(side*(dimensions.PoolWidth/2+dimensions.deckWidth+.7f),dimensions.deckElevation,b*7.2f);
                    if(side<0)instance.transform.localRotation=Quaternion.Euler(0,180,0);
                    for(int row=0;row<6;row++)for(int seat=0;seat<8;seat++)
                    {
                        int key=(b+2)*48+row*8+seat;if(key%3==0)continue;var parent=key%2==0?medium:high;
                        float x=side*(dimensions.PoolWidth/2+dimensions.deckWidth+.7f+row*.92f);float z=b*7.2f+(seat-3.5f)*.76f;
                        var color=(seat+row)%4==0?white:(seat%3==0?aqua:navy);
                        Sphere("Audience torso",parent,new Vector3(x,dimensions.deckElevation+.82f+row*.47f,z),new Vector3(.38f,.52f,.36f),color);
                        Sphere("Audience head",parent,new Vector3(x,dimensions.deckElevation+1.18f+row*.47f,z),new Vector3(.24f,.26f,.24f),Lit("Audience skin",new Color(.65f,.43f,.29f),.2f));
                    }
                }
            }
            Combine(medium,"AudienceMedium");Combine(high,"AudienceHigh");return new[]{medium,high};
        }

        static void BuildAthletes()
        {
            // The fixture's anatomy is recreated as reusable visual-only prefabs; demonstration objects are never migrated.
            foreach(var p in Object.FindAnyObjectByType<PrototypeRoster>().Members)
            {
                var old=p.transform.Find("Visual");if(old!=null)foreach(var r in old.GetComponentsInChildren<Renderer>())r.enabled=false;
                var path=Root+"/Prefabs/"+p.Team+(p.IsFieldPlayer?"Field":"Keeper")+"Visual.prefab";
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(prefab==null)
                {
                    var rig=Child("Athlete Visual — replace with Blender",null);var skin=AssetDatabase.LoadAssetAtPath<Material>(WaterVisualTestSetup.Root+"/Materials/Testskinlight.mat");
                    var suit=p.Team==PrototypeTeam.Blue?blue:navy;var cap=p.IsFieldPlayer?(p.Team==PrototypeTeam.Blue?blue:white):red;
                    Sphere("Torso",rig,Vector3.zero,new Vector3(.52f,.75f,.3f),skin);Sphere("Swimwear",rig,new Vector3(0,-.4f,0),new Vector3(.46f,.3f,.31f),suit);
                    Sphere("Head",rig,new Vector3(0,.61f,0),new Vector3(.33f,.42f,.34f),skin);Sphere("Cap",rig,new Vector3(0,.73f,-.025f),new Vector3(.35f,.23f,.34f),cap);
                    var la=Limb("Left Arm",rig,new Vector3(-.25f,.24f,0),new Vector3(-.72f,-.12f,.18f),.09f,skin);
                    var ra=Limb("Right Arm",rig,new Vector3(.25f,.24f,0),new Vector3(.72f,-.12f,.18f),.09f,skin);
                    var ll=Limb("Left Leg",rig,new Vector3(-.13f,-.45f,0),new Vector3(-.25f,-1.24f,.08f),.12f,skin);
                    var rl=Limb("Right Leg",rig,new Vector3(.13f,-.45f,0),new Vector3(.25f,-1.24f,-.05f),.12f,skin);
                    var visual=rig.gameObject.AddComponent<ArenaAthleteVisual>();visual.leftArm=la;visual.rightArm=ra;visual.leftLeg=ll;visual.rightLeg=rl;
                    visual.skinSlots=rig.GetComponentsInChildren<Renderer>().Where(r=>r.sharedMaterial==skin).ToArray();visual.capSlots=new[]{rig.Find("Cap").GetComponent<Renderer>()};visual.swimwearSlots=new[]{rig.Find("Swimwear").GetComponent<Renderer>()};
                    prefab=PrefabUtility.SaveAsPrefabAsset(rig.gameObject,path);Object.DestroyImmediate(rig.gameObject);
                }
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,p.transform);instance.transform.localPosition=new Vector3(0,-.42f,0);
                var v=instance.GetComponent<ArenaAthleteVisual>();v.motor=p.Motor;v.identity=p;
                if(!p.IsFieldPlayer){Set(p.GetComponent<PrototypeGoalkeeper>(),"leftArmVisual",v.leftArm);Set(p.GetComponent<PrototypeGoalkeeper>(),"rightArmVisual",v.rightArm);}
            }
        }

        static void ConfigureCamera(bool initialize=false)
        {
            var camera=Object.FindAnyObjectByType<GameplayCamera>();var lens=camera.GetComponent<Camera>();
            if(initialize){Set(camera,"height",14.4f);Set(camera,"distance",10.8f);Set(camera,"pitch",53f);Set(camera,"yaw",0f);lens.fieldOfView=52; lens.farClipPlane=160;lens.backgroundColor=new Color(.56f,.7f,.79f);}
            var developer=camera.GetComponent<ArenaDeveloperCamera>();if(developer==null)developer=camera.gameObject.AddComponent<ArenaDeveloperCamera>();developer.broadcast=camera;
            var views=GameObject.Find(EnvironmentName).transform.Find("Developer Viewpoints");if(views!=null)Object.DestroyImmediate(views.gameObject);
            views=Child("Developer Viewpoints",GameObject.Find(EnvironmentName).transform);
            developer.viewpoints=new[]{View("Arena overview",views,new Vector3(18,15,-23),new Vector3(0,-.4f,0)),View("Clear water anatomy",views,new Vector3(5.3f,4.3f,-8.8f),new Vector3(0,-.45f,-3.5f)),View("Reflection and far goal",views,new Vector3(4,2.1f,-11),new Vector3(0,0,7)),View("Team benches",views,new Vector3(12,4,-2),new Vector3(16,.8f,-7)),View("Reverse end",views,new Vector3(-16,13,21),new Vector3(0,0,-2))};
        }

        static void ConfigureHud()
        {
            var canvas=Object.FindAnyObjectByType<Canvas>();
            foreach(var label in canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                if(label.name=="AudioNote")label.text="Match sounds and indoor crowd atmosphere.";
                if(label.name=="GraphicsNote")label.text="Water detail, reflections and crowd density.\nClear underwater view on every preset.";
            }
            var observer=canvas.GetComponent<ArenaHudReadability>();if(observer!=null)return;
            observer=canvas.gameObject.AddComponent<ArenaHudReadability>();
            var labels=canvas.GetComponentsInChildren<TMP_Text>(true).Where(t=>t.name=="RestartInstruction"||t.name=="Possession"||t.name=="QuickControls"||t.transform.parent.GetComponent<GameplayActionButton>()!=null).ToArray();
            observer.labels=labels;observer.plates=new GameObject[labels.Length];
            for(int i=0;i<labels.Length;i++)
            {
                var label=labels[i];bool action=label.transform.parent.GetComponent<GameplayActionButton>()!=null;
                var source=action?(RectTransform)label.transform.parent:label.rectTransform;
                var plate=new GameObject("Arena contrast / "+source.name,typeof(RectTransform),typeof(RoundedPanelGraphic));var rt=plate.GetComponent<RectTransform>();rt.SetParent(source.parent,false);
                rt.anchorMin=source.anchorMin;rt.anchorMax=source.anchorMax;rt.pivot=source.pivot;rt.anchoredPosition=source.anchoredPosition;rt.sizeDelta=source.sizeDelta+(action?Vector2.zero:new Vector2(20,8));
                rt.SetSiblingIndex(source.GetSiblingIndex());var graphic=plate.GetComponent<RoundedPanelGraphic>();graphic.color=new Color(.015f,.045f,.07f,.82f);graphic.raycastTarget=false;observer.plates[i]=plate;
            }
        }

        static PoolWaterQualityProfile[] CreateWaterProfiles()
        {
            return new[]{"Low","Balanced","High"}.Select((n,i)=>{
                string path=Root+"/Settings/ArenaWater_"+n+".asset";var p=AssetDatabase.LoadAssetAtPath<PoolWaterQualityProfile>(path);
                if(p==null){p=Object.Instantiate(AssetDatabase.LoadAssetAtPath<PoolWaterQualityProfile>(WaterVisualTestSetup.Root+"/Settings/WaterQuality_"+n+".asset"));p.testPipeline=null;p.reflectionScale=.3f;p.costNotes="Arena preset. High uses 0.3-scale planar reflection; Low/Medium use the baked arena cubemap. Device profiling pending.";AssetDatabase.CreateAsset(p,path);}return p;
            }).ToArray();
        }
        static RenderPipelineAsset[] CreatePipelines()
        {
            return new[]{"Low","Balanced","High"}.Select((n,i)=>{
                string path=Root+"/Settings/Arena_"+n+".asset";var p=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if(p==null){AssetDatabase.CopyAsset(WaterVisualTestSetup.Root+"/Settings/PoolVisual_"+n+".asset",path);p=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);p.shadowDistance=i==0?0:i==1?25:45;p.mainLightShadowmapResolution=i==2?2048:1024;EditorUtility.SetDirty(p);}return (RenderPipelineAsset)p;
            }).ToArray();
        }
        static AudioClip CreateCrowd()
        {
            string path=Root+"/ArenaCrowd.wav";if(File.Exists(path))return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            const int rate=22050,seconds=8;var random=new System.Random(1703);float low=0;
            using(var output=new BinaryWriter(File.Create(path)))
            {
                int count=rate*seconds;output.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));output.Write(36+count*2);output.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));output.Write(16);output.Write((short)1);output.Write((short)1);output.Write(rate);output.Write(rate*2);output.Write((short)2);output.Write((short)16);output.Write(System.Text.Encoding.ASCII.GetBytes("data"));output.Write(count*2);
                for(int i=0;i<count;i++){float t=(float)i/rate;low=Mathf.Lerp(low,(float)random.NextDouble()*2-1,.075f);float envelope=Mathf.Sin(Mathf.PI*i/count);float sound=(low*.4f+Mathf.Sin(t*191)*.015f)*envelope;output.Write((short)(sound*short.MaxValue));}
            }
            AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        static Transform Limb(string name,Transform parent,Vector3 start,Vector3 end,float radius,Material material)
        {
            var pivot=Child(name,parent);pivot.localPosition=start;pivot.localRotation=Quaternion.FromToRotation(Vector3.up,end-start);
            var part=Primitive(name+" opaque mesh",pivot,PrimitiveType.Capsule,Vector3.up*Vector3.Distance(start,end)*.5f,new Vector3(radius*2,Vector3.Distance(start,end)*.5f,radius*2),material);return pivot;
        }
        static Transform View(string name,Transform parent,Vector3 pos,Vector3 target){var t=Child(name,parent);t.localPosition=pos;t.rotation=Quaternion.LookRotation(parent.TransformPoint(target)-t.position);return t;}
        static Material Lit(string name,Color color,float smoothness)
        {
            string path=Root+"/Materials/"+name.Replace(" ","")+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;
            m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};m.color=color;m.SetFloat("_Smoothness",smoothness);AssetDatabase.CreateAsset(m,path);return m;
        }
        static Transform Child(string name,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        static Transform Primitive(string name,Transform parent,PrimitiveType type,Vector3 pos,Vector3 size,Material material)
        {var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(go.GetComponent<Collider>());
            if(name.StartsWith("Audience"))go.GetComponent<MeshFilter>().sharedMesh=AudienceMesh();return go.transform;}
        static Mesh AudienceMesh()
        {
            string path=Root+"/Meshes/AudienceEllipsoid.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh!=null && mesh.name=="Bounded low-poly audience v2")return mesh;
            const int segments=8,rings=5;var vertices=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();
            for(int y=0;y<=rings;y++)for(int x=0;x<=segments;x++){float a=x*Mathf.PI*2/segments,b=y*Mathf.PI/rings;vertices.Add(new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a))*.5f);}
            for(int y=0;y<rings;y++)for(int x=0;x<segments;x++){int a=y*(segments+1)+x,b=a+segments+1;triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            bool create=mesh==null;if(create)mesh=new Mesh();else mesh.Clear();mesh.name="Bounded low-poly audience v2";mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();if(create)AssetDatabase.CreateAsset(mesh,path);else EditorUtility.SetDirty(mesh);return mesh;
        }
        static void Box(string n,Transform p,Vector3 pos,Vector3 size,Material m)=>Primitive(n,p,PrimitiveType.Cube,pos,size,m);
        static void Sphere(string n,Transform p,Vector3 pos,Vector3 size,Material m)=>Primitive(n,p,PrimitiveType.Sphere,pos,size,m);
        static void Bar(string n,Transform p,Vector3 a,Vector3 b,float thickness,Material m)
        {var t=Primitive(n,p,PrimitiveType.Cube,(a+b)*.5f,new Vector3(thickness,(b-a).magnitude,thickness),m);t.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
        static void Label(string value,Transform parent,Vector3 pos,Quaternion rotation,float size,Color color)
        {var go=Child(value,parent);go.localPosition=pos;go.localRotation=rotation;var text=go.gameObject.AddComponent<TextMeshPro>();text.text=value;text.fontSize=size*10;text.color=color;text.alignment=TextAlignmentOptions.Center;text.fontStyle=FontStyles.Bold;text.rectTransform.sizeDelta=new Vector2(20,2);text.overflowMode=TextOverflowModes.Overflow;text.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;}
        static MeshRenderer Surface(string name,Transform parent,Vector3 pos,Vector3 right,Vector3 forward,Material material)
        {
            string path=Root+"/Meshes/"+name.Replace(" ","")+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool create=mesh==null;if(create)mesh=new Mesh{name=name};else mesh.Clear();
            mesh.vertices=new[]{(-right-forward)*.5f,(right-forward)*.5f,(-right+forward)*.5f,(right+forward)*.5f};
            mesh.uv=new[]{Vector2.zero,Vector2.right*right.magnitude,Vector2.up*forward.magnitude,new Vector2(right.magnitude,forward.magnitude)};
            mesh.triangles=new[]{0,2,1,1,2,3};mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            if(create)AssetDatabase.CreateAsset(mesh,path);else EditorUtility.SetDirty(mesh);
            var go=Child(name,parent);go.localPosition=pos;go.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=material;return r;
        }
        static void Combine(Transform root,string name)
        {
            var filters=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<MeshRenderer>()!=null && f.GetComponent<TextMeshPro>()==null).ToArray();
            foreach(var group in filters.GroupBy(f=>f.GetComponent<Renderer>().sharedMaterial))
            {
                var mesh=new Mesh{name=name+" "+group.Key.name,indexFormat=IndexFormat.UInt32};
                mesh.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);mesh.RecalculateBounds();
                string path=Root+"/Meshes/"+name+"_"+group.Key.name.Replace(" ","")+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existing==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);}
                var go=Child(mesh.name,root);go.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;go.gameObject.AddComponent<MeshRenderer>().sharedMaterial=group.Key;
            }
            foreach(var f in filters)Object.DestroyImmediate(f.gameObject);
        }
        static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;string parent=path.Substring(0,path.LastIndexOf('/'));Folder(parent);AssetDatabase.CreateFolder(parent,path.Substring(path.LastIndexOf('/')+1));}
        static void Set(Object target,string name,float value){var s=new SerializedObject(target);s.FindProperty(name).floatValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
        static void Set(Object target,string name,Object value){var s=new SerializedObject(target);s.FindProperty(name).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
    }
}
