using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using ToduStudios.WaterPolo.WaterVisuals;
using Object=UnityEngine.Object;
using MeshData=ToduStudios.WaterPolo.Editor.AuthenticFieldSetup.MeshData;

namespace ToduStudios.WaterPolo.Editor
{
    /// <summary>One additive pass over the accepted M17 arena; never rebuilds its scene or gameplay.</summary>
    public static class SprintPolishSetup
    {
        public const string Root="Assets/Arena/SprintPolish",Name="Championship Lighting Refinement";
        public const string Recovery="Assets/Scenes/Recovery/OlympicMatchArena_PreSprintPolish.unity";
        static Material navy,alloy,white,cobalt,ice;
        [MenuItem("Tools/Water Polo/Arena/Refine Sprint and Championship Lighting")]
        public static void SetUp()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||scene.isDirty||scene.path!=OlympicArenaSetup.ScenePath)throw new InvalidOperationException("Open saved OlympicMatchArena outside Play Mode; nothing rebuilt.");
            var arena=Object.FindAnyObjectByType<ArenaQualityController>();
            if(arena.transform.Find(Name)!=null){Debug.Log("M18 refinement already configured; authored tuning preserved.");return;}
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Recovery)==null&&!AssetDatabase.CopyAsset(scene.path,Recovery))throw new InvalidOperationException("Native scene recovery failed.");
            foreach(string folder in new[]{"Materials","Meshes","Settings","Textures","Prefabs"})Folder(Root+"/"+folder);
            navy=Mat("Luminaires Navy",new Color(.025f,.063f,.11f),.3f,.34f);
            alloy=Mat("Anodized Truss",new Color(.23f,.36f,.49f),.67f,.42f);
            white=Mat("Competition Diffusers",new Color(.68f,.86f,.98f),.08f,.35f,new Color(.44f,.74f,1f)*2.2f);
            cobalt=Mat("Cobalt Architectural Glow",new Color(.025f,.13f,.35f),.2f,.32f,new Color(.024f,.108f,.48f));
            ice=Mat("Ice Blue Edge Glow",new Color(.16f,.56f,.82f),.18f,.4f,new Color(.12f,.5f,.86f)*2f);
            var root=Child(Name,arena.transform);var match=Object.FindAnyObjectByType<PrototypeMatch>();
            BuildEffortMeter(match);RefineApparatus(match);RefineWalkways(arena.transform);BuildLighting(root,arena);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,Root+"/Prefabs/ChampionshipLighting.prefab");
            BakeReflection(arena);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("M18 sprint effort HUD, readable release jaws, colored official decks and championship lighting saved. Opening camera and gameplay assets retained.");
        }
        static void BuildEffortMeter(PrototypeMatch match)
        {
            var canvas=Object.FindAnyObjectByType<Canvas>();var safe=canvas.transform.Find("MatchSafeArea");
            var panel=Panel("SprintEffort",safe,new Vector2(.5f,0),new Vector2(0,70),new Vector2(350,88),new Color(.018f,.052f,.09f,.94f));
            var group=panel.gameObject.AddComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=group.interactable=false;
            Text("Title",panel,"SPRINT",new Vector2(-113,19),new Vector2(100,30),20,new Color(.25f,.84f,1f));
            Text("Instruction",panel,"TAP SHIFT / SPRINT",new Vector2(59,19),new Vector2(210,28),16,new Color(.83f,.9f,.97f));
            var track=Panel("EffortTrack",panel,Vector2.one*.5f,new Vector2(0,-13),new Vector2(308,14),new Color(.10f,.19f,.27f));
            var fill=Panel("ActualEffort",track,Vector2.zero,Vector2.zero,Vector2.zero,new Color(.16f,.8f,1f));fill.anchorMin=Vector2.zero;fill.anchorMax=new Vector2(0,1);fill.offsetMin=fill.offsetMax=Vector2.zero;fill.pivot=new Vector2(0,.5f);
            var glow=Panel("StrokeHighlight",panel,Vector2.one*.5f,new Vector2(0,-13),new Vector2(310,18),new Color(.55f,.9f,1f,0));glow.SetAsFirstSibling();
            var view=panel.gameObject.AddComponent<SprintEffortHUD>();Assign(view,"match",match);Assign(view,"menus",Object.FindAnyObjectByType<MatchMenuController>());Assign(view,"group",group);Assign(view,"fill",fill);Assign(view,"fillGraphic",fill.GetComponent<RoundedPanelGraphic>());Assign(view,"highlight",glow.GetComponent<RoundedPanelGraphic>());
        }
        static void RefineApparatus(PrototypeMatch match)
        {
            var view=Object.FindAnyObjectByType<SprintBallReleaseVisual>();var so=new SerializedObject(view);var cradle=((Transform)so.FindProperty("cradle").objectReferenceValue);
            foreach(var r in cradle.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            Vector3 center=cradle.InverseTransformPoint(match.PeriodCenter);
            var metal=AssetDatabase.LoadAssetAtPath<Material>(AuthenticFieldSetup.Root+"/Materials/Brushed Alloy.mat");
            foreach(int side in new[]{-1,1})
            {
                var jaw=Child(side<0?"Left Release Jaw":"Right Release Jaw",cradle);var mesh=new MeshData();
                for(int i=0;i<16;i++){float a=(-.5f+i/16f)*Mathf.PI,b=(-.5f+(i+1)/16f)*Mathf.PI;mesh.Bar(center+new Vector3(side*Mathf.Cos(a)*.1f,.32f,Mathf.Sin(a)*.1f),center+new Vector3(side*Mathf.Cos(b)*.1f,.32f,Mathf.Sin(b)*.1f),.011f,6);}
                mesh.Bar(center+new Vector3(side*.06f,.23f,0),center+new Vector3(side*.10f,.32f,0),.008f,6);mesh.Box(center+new Vector3(side*.065f,.23f,0),new Vector3(.04f,.04f,.04f));Geometry(jaw,"Release Jaw "+side,mesh,metal);
                so.FindProperty(side<0?"leftJaw":"rightJaw").objectReferenceValue=jaw;
            }
            var cable=Child("Flexible Retraction Cable",cradle);var data=new MeshData();data.Bar(center+new Vector3(0,-.65f,0),center+new Vector3(0,.24f,0),.006f,8);data.Box(center+new Vector3(0,-.7f,0),new Vector3(.21f,.06f,.21f));data.Bar(center+new Vector3(-.065f,.23f,0),center+new Vector3(.065f,.23f,0),.012f,8);Geometry(cable,"Thin Cable and Joint",data,metal);
            so.FindProperty("cable").objectReferenceValue=cable;so.FindProperty("retractionDuration").floatValue=1.05f;so.ApplyModifiedPropertiesWithoutUndo();
        }
        static void RefineWalkways(Transform arena)
        {
            var platforms=arena.Find(AuthenticFieldSetup.Name+"/Elevated Referee Platforms");var source=AssetDatabase.LoadAssetAtPath<Texture2D>(PremiumArenaSetup.Root+"/Textures/MicroFinishNormal.png");
            var deck=Mat("Official Navy Walking Surface",new Color(.045f,.12f,.20f),.10f,.28f);deck.SetTexture("_BumpMap",source);deck.SetFloat("_BumpScale",.10f);deck.EnableKeyword("_NORMALMAP");
            Color[] palette={new Color(.78f,.10f,.16f),new Color(.9f,.72f,.18f),new Color(.12f,.55f,.34f),new Color(.82f,.9f,.96f)};
            string[] names={"Competition Red","Competition Yellow","Competition Green","Competition White"};var mats=palette.Select((c,i)=>Mat("Official "+names[i],c,.08f,.26f,c*.10f)).ToArray();
            float half=Object.FindAnyObjectByType<PoolBounds>().Length*.5f;
            foreach(Transform platform in platforms)
            {
                int side=platform.name.StartsWith("West")?-1:1;float x=side*12.65f;
                platform.Find(platform.name+" Non Slip Walking Deck").GetComponent<Renderer>().sharedMaterial=deck;
                var batches=new[]{new MeshData(),new MeshData(),new MeshData(),new MeshData()};
                foreach(int end in new[]{-1,1})
                {
                    foreach(var s in new[]{new Vector3(0,2,0),new Vector3(2,6,1),new Vector3(6,half,2)})batches[(int)s.z].Box(new Vector3(x,.705f,end*(half-(s.x+s.y)*.5f)),new Vector3(.82f,.016f,s.y-s.x));
                    foreach(float d in new[]{0f,2f,5f,6f})batches[d==0?3:d==6?1:0].Box(new Vector3(x,.716f,end*(half-d)),new Vector3(.96f,.018f,.09f));
                }
                batches[3].Box(new Vector3(x,.716f,0),new Vector3(.96f,.018f,.10f));
                for(int i=0;i<4;i++){var t=platform.Find(platform.name+" Markers "+names[i]);t.GetComponent<MeshFilter>().sharedMesh=Asset(platform.name+" Full Width "+names[i],batches[i]);t.GetComponent<Renderer>().sharedMaterial=mats[i];}
            }
            EditorUtility.SetDirty(deck);
        }
        static void BuildLighting(Transform root,ArenaQualityController arena)
        {
            var rigs=Child("Suspended Competition Light Galleries",root);
            foreach(int side in new[]{-1,1})
            {
                float x=side*13.7f;var frame=new MeshData();var housing=new MeshData();var diffuser=new MeshData();var edges=new MeshData();
                foreach(float y in new[]{10.4f,10.95f})frame.Bar(new Vector3(x,y,-14),new Vector3(x,y,14),.055f,8);
                for(int i=0;i<14;i++)frame.Bar(new Vector3(x,i%2==0?10.4f:10.95f,-14+i*2),new Vector3(x,i%2==0?10.95f:10.4f,-12+i*2),.028f,6);
                foreach(float z in new[]{-12f,-8f,-4f,0,4f,8f,12f})
                {
                    frame.Bar(new Vector3(x,10.95f,z),new Vector3(x,18.2f,z),.016f,6);
                    frame.Bar(new Vector3(x,10.5f,z),new Vector3(x-side*.25f,9.95f,z),.035f,6);
                    Lamp(side,new Vector3(x-side*.25f,9.7f,z),housing,diffuser,edges,frame);
                }
                Geometry(rigs,"Gallery Frame "+side,frame,alloy);Geometry(rigs,"Luminaire Housings "+side,housing,navy);Geometry(rigs,"Competition Lens Arrays "+side,diffuser,white);Geometry(rigs,"Diffuser Edge Rings "+side,edges,ice);
            }
            var medium=Child("Cobalt Architecture",root);var fins=new MeshData();
            ArchitecturalReveals(fins);
            Geometry(medium,"Cobalt Section Grazers",fins,cobalt);
            var high=Child("Championship Display Highlights",root);var bezel=new MeshData();var outlines=new MeshData();
            var jumbo=GameObject.Find(PremiumArenaSetup.Name).transform.Find("Championship Scoreboards and Presentation Screens/Four Face End Hung Championship Display");Vector3 center=root.InverseTransformPoint(jumbo.position);
            JumbotronSpines(bezel,center);
            foreach(int s in new[]{-1,1}){outlines.Box(center+new Vector3(s*6.50f,0,-2.0f),new Vector3(.035f,4.95f,.035f));outlines.Box(center+new Vector3(0,2.58f,s*1.98f),new Vector3(12.85f,.045f,.045f));}
            Geometry(high,"Jumbotron Metal Spine",bezel,alloy);Geometry(high,"Jumbotron Ice Edges",outlines,ice);
            var accents=new Light[4];for(int i=0;i<4;i++)
            {
                int side=i%2==0?-1:1;var light=Child(i<2?"Seating Cobalt Wash":"Display Structure Wash",root).gameObject.AddComponent<Light>();accents[i]=light;light.type=LightType.Spot;light.shadows=LightShadows.None;light.renderMode=LightRenderMode.ForcePixel;light.color=i<2?new Color(.12f,.40f,1f):new Color(.28f,.65f,1f);
                light.transform.localPosition=i<2?new Vector3(side*18,5.5f,0):center+new Vector3(side*6,0,-1);
                Vector3 target=i<2?new Vector3(side*23,10,0):center+new Vector3(side*2,3,0);light.transform.LookAt(root.TransformPoint(target));light.range=i<2?20:10;light.spotAngle=i<2?110:75;light.innerSpotAngle=i<2?65:45;light.intensity=i<2?95:45;
            }
            var presentation=Object.FindAnyObjectByType<ChampionshipPresentation>();presentation.accentLights=accents;presentation.mediumAtmosphere=medium.gameObject;presentation.highAtmosphere=high.gameObject;presentation.refinedLighting=true;
            for(int i=0;i<3;i++){arena.pipelines[i]=Pipeline(i);presentation.profiles[i]=Profile(i);}PrefabUtility.RecordPrefabInstancePropertyModifications(presentation);
            var key=Object.FindObjectsByType<Light>().Single(l=>l.type==LightType.Directional);key.color=new Color(.84f,.91f,.98f);key.intensity=.95f;key.transform.rotation=Quaternion.Euler(58,-28,0);key.shadows=LightShadows.Soft;key.shadowStrength=.60f;key.shadowBias=.035f;key.shadowNormalBias=.25f;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.16f,.27f,.41f);RenderSettings.ambientEquatorColor=new Color(.082f,.17f,.30f);RenderSettings.ambientGroundColor=new Color(.05f,.105f,.17f);RenderSettings.reflectionIntensity=.80f;
        }
        static void Lamp(int side,Vector3 position,MeshData housing,MeshData diffuser,MeshData edges,MeshData frame)
        {
            var body=new MeshData();var lenses=new MeshData();var rim=new MeshData();var vents=new MeshData();
            body.Box(Vector3.zero,new Vector3(.66f,.30f,1.48f));lenses.Box(new Vector3(0,-.16f,0),new Vector3(.54f,.025f,1.32f));
            foreach(float dx in new[]{-.31f,.31f})rim.Box(new Vector3(dx,-.12f,0),new Vector3(.025f,.08f,1.38f));
            foreach(float dz in new[]{-.69f,.69f})rim.Box(new Vector3(0,-.12f,dz),new Vector3(.61f,.08f,.025f));
            for(int j=-2;j<=2;j++)vents.Box(new Vector3(0,.16f,j*.21f),new Vector3(.54f,.025f,.025f));
            // Cross-light the opposite half: lensed faces are visible from the accepted wide angle.
            var matrix=Matrix4x4.TRS(position,Quaternion.Euler(0,0,-side*65f),Vector3.one);housing.Add(body,matrix);diffuser.Add(lenses,matrix);edges.Add(rim,matrix);frame.Add(vents,matrix);
        }
        static void ArchitecturalReveals(MeshData fins)
        {foreach(int side in new[]{-1,1})foreach(float z in new[]{-14.4f,-7.2f,0,7.2f,14.4f})fins.Box(new Vector3(side*23.39f,12.8f,z),new Vector3(.065f,9.6f,.10f));}
        static void JumbotronSpines(MeshData bezel,Vector3 center)
        {foreach(int side in new[]{-1,1}){foreach(float z in new[]{-1.99f,1.99f})bezel.Box(center+new Vector3(side*6.48f,0,z),new Vector3(.05f,5,.05f));foreach(float y in new[]{-2.5f,2.5f})bezel.Box(center+new Vector3(side*6.48f,y,0),new Vector3(.05f,.05f,3.95f));}}
        public static void ApplyReviewedAccents()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Saved Edit Mode required.");
            void Copy(string n,MeshData data){var mesh=data.Mesh(n);var target=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/"+n+".asset");EditorUtility.CopySerialized(mesh,target);EditorUtility.SetDirty(target);Object.DestroyImmediate(mesh);}
            var fins=new MeshData();ArchitecturalReveals(fins);Copy("Cobalt Section Grazers",fins);
            var root=GameObject.Find(Name).transform;var jumbo=GameObject.Find(PremiumArenaSetup.Name).transform.Find("Championship Scoreboards and Presentation Screens/Four Face End Hung Championship Display");var bezel=new MeshData();JumbotronSpines(bezel,root.InverseTransformPoint(jumbo.position));Copy("Jumbotron Metal Spine",bezel);
            var m=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Cobalt Architectural Glow.mat");m.SetColor("_EmissionColor",new Color(.024f,.108f,.48f));m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;m.EnableKeyword("_EMISSION");EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssets();BakeReflection(Object.FindAnyObjectByType<ArenaQualityController>());EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        public static void ApplyReviewedLuminaires()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Saved Edit Mode required.");
            foreach(int side in new[]{-1,1})
            {
                var housing=new MeshData();var diffuser=new MeshData();var edges=new MeshData();var frame=new MeshData();float x=side*13.7f;
                foreach(float y in new[]{10.4f,10.95f})frame.Bar(new Vector3(x,y,-14),new Vector3(x,y,14),.055f,8);
                for(int i=0;i<14;i++)frame.Bar(new Vector3(x,i%2==0?10.4f:10.95f,-14+i*2),new Vector3(x,i%2==0?10.95f:10.4f,-12+i*2),.028f,6);
                foreach(float z in new[]{-12f,-8f,-4f,0,4f,8f,12f}){frame.Bar(new Vector3(x,10.95f,z),new Vector3(x,18.2f,z),.016f,6);frame.Bar(new Vector3(x,10.5f,z),new Vector3(x-side*.25f,9.95f,z),.035f,6);Lamp(side,new Vector3(x-side*.25f,9.7f,z),housing,diffuser,edges,frame);}
                void Copy(string n,MeshData data){var mesh=data.Mesh(n);var target=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/"+n+".asset");EditorUtility.CopySerialized(mesh,target);EditorUtility.SetDirty(target);Object.DestroyImmediate(mesh);}
                Copy("Gallery Frame "+side,frame);Copy("Luminaire Housings "+side,housing);Copy("Competition Lens Arrays "+side,diffuser);Copy("Diffuser Edge Rings "+side,edges);
            }
            BakeReflection(Object.FindAnyObjectByType<ArenaQualityController>());AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        public static void VerifyEmissiveMaterials()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Saved Edit Mode required.");
            foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{Root+"/Materials"}))
            {
                var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));if(m.GetColor("_EmissionColor").maxColorComponent<=0)continue;
                m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;m.EnableKeyword("_EMISSION");EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();BakeReflection(Object.FindAnyObjectByType<ArenaQualityController>());EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        static UniversalRenderPipelineAsset Pipeline(int level)
        {
            string path=Root+"/Settings/Championship_"+level+".asset";if(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path)==null)AssetDatabase.CopyAsset(PremiumArenaSetup.Root+"/Settings/Championship_"+level+".asset",path);
            var p=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);p.supportsHDR=true;p.mainLightShadowmapResolution=level==2?2048:1024;p.shadowCascadeCount=level==2?2:1;p.cascade2Split=.4f;p.shadowDistance=level==0?0:level==1?52:65;p.shadowNormalBias=.25f;p.shadowDepthBias=.8f;p.maxAdditionalLightsCount=level==0?2:4;
            var so=new SerializedObject(p);so.FindProperty("m_MainLightShadowsSupported").boolValue=level>=1;so.FindProperty("m_AdditionalLightShadowsSupported").boolValue=false;so.FindProperty("m_SoftShadowsSupported").boolValue=level>=1;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(p);return p;
        }
        static VolumeProfile Profile(int level)
        {
            string path=Root+"/Settings/ChampionshipGrade_"+level+".asset";if(AssetDatabase.LoadAssetAtPath<VolumeProfile>(path)==null)AssetDatabase.CopyAsset(PremiumArenaSetup.Root+"/Settings/ChampionshipGrade_"+level+".asset",path);
            var p=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);p.TryGet<ColorAdjustments>(out var color);color.postExposure.Override(.03f);color.contrast.Override(14);color.saturation.Override(-2);
            p.TryGet<Bloom>(out var bloom);bloom.intensity.Override(level==0?0:level==1?.19f:.30f);bloom.threshold.Override(1.2f);bloom.scatter.Override(.55f);bloom.highQualityFiltering.Override(false);bloom.maxIterations.Override(level==1?3:4);bloom.downscale.Override(BloomDownscaleMode.Quarter);
            p.TryGet<Vignette>(out var vignette);vignette.intensity.Override(.13f);foreach(var c in p.components)EditorUtility.SetDirty(c);EditorUtility.SetDirty(p);return p;
        }
        static void BakeReflection(ArenaQualityController arena)
        {
            var probe=arena.GetComponentInChildren<ReflectionProbe>();var water=arena.water.waterSurface;bool enabled=water.enabled;var original=QualitySettings.renderPipeline;water.enabled=false;QualitySettings.renderPipeline=arena.pipelines[2];
            try{probe.mode=ReflectionProbeMode.Baked;probe.resolution=128;WaterVisualTestSetup.BakePoolProbe(probe,arena.transform,arena.transform,Root+"/Textures/ChampionshipReflection.exr");}
            finally{water.enabled=enabled;QualitySettings.renderPipeline=original;}
        }
        static Material Mat(string name,Color color,float metallic,float smooth,Color emission=default)
        {
            string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color};m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);if(emission.maxColorComponent>0){m.SetColor("_EmissionColor",emission);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;m.EnableKeyword("_EMISSION");}AssetDatabase.CreateAsset(m,path);return m;
        }
        static Transform Child(string n,Transform p){var t=new GameObject(n).transform;t.SetParent(p,false);return t;}
        static Mesh Asset(string name,MeshData data){var path=Root+"/Meshes/"+name+".asset";var m=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(m!=null)return m;m=data.Mesh(name);AssetDatabase.CreateAsset(m,path);return m;}
        static Transform Geometry(Transform p,string name,MeshData data,Material mat){var t=Child(name,p);t.gameObject.AddComponent<MeshFilter>().sharedMesh=Asset(name,data);var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=true;GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.ReflectionProbeStatic);return t;}
        static void Assign(Object o,string n,Object v)=>Milestone9PrototypeSetup.Assign(o,n,v);
        static RectTransform Panel(string n,Transform p,Vector2 anchor,Vector2 pos,Vector2 size,Color color){var r=new GameObject(n,typeof(RectTransform),typeof(CanvasRenderer)).GetComponent<RectTransform>();r.SetParent(p,false);r.anchorMin=r.anchorMax=anchor;r.pivot=Vector2.one*.5f;r.anchoredPosition=pos;r.sizeDelta=size;var g=r.gameObject.AddComponent<RoundedPanelGraphic>();g.color=color;g.raycastTarget=false;return r;}
        static void Text(string n,Transform p,string caption,Vector2 pos,Vector2 size,float font,Color color){var r=new GameObject(n,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false);r.anchorMin=r.anchorMax=Vector2.one*.5f;r.anchoredPosition=pos;r.sizeDelta=size;var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.text=caption;t.fontSize=font;t.color=color;t.fontStyle=FontStyles.Bold;t.alignment=TextAlignmentOptions.Center;t.textWrappingMode=TextWrappingModes.NoWrap;t.raycastTarget=false;}
        static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;int slash=path.LastIndexOf('/');Folder(path.Substring(0,slash));AssetDatabase.CreateFolder(path.Substring(0,slash),path.Substring(slash+1));}
    }
}
