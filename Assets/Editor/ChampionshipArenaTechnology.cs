using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using ToduStudios.WaterPolo.WaterVisuals;
using Object=UnityEngine.Object;

namespace ToduStudios.WaterPolo.Editor
{
    public static partial class ChampionshipArenaRebuild
    {
        static partial void BuildRoofAndLights(ArenaQualityController env)
        {
            var b=new ChampionshipArenaBatch(Child("Arena Shell Roof Catwalks and Competition Rig",root));float x=layout.hallHalfSize.x,z=layout.hallHalfSize.y,h=layout.roofHeight;
            foreach(int side in new[]{-1,1})
            {
                b.Box(glass,new Vector3(side*(x+.25f),16,0),new Vector3(.5f,33,z*2));b.Box(glass,new Vector3(0,16,side*(z+.25f)),new Vector3(x*2,33,.5f));
                for(int i=-6;i<=6;i++)
                {
                    float q=i*10f;b.Bevel(navy,new Vector3(side*(x-.2f),17,q),new Vector3(1.5f,34,1.2f),.12f);b.Box(cobalt,new Vector3(side*(x-.99f),21,q),new Vector3(.055f,12,.20f));
                }
                for(int i=-5;i<=5;i++){float q=i*9.4f;b.Bevel(navy,new Vector3(q,17,side*(z-.2f)),new Vector3(1.2f,34,1.5f),.12f);b.Box(cobalt,new Vector3(q,21,side*(z-.99f)),new Vector3(.20f,12,.055f));}
            }
            b.Part(glass).Face(new Vector3(-x,h,-z),new Vector3(x,h,-z),new Vector3(x,h,z),new Vector3(-x,h,z),Vector3.down);
            foreach(float q in new[]{-55f,-39f,-23f,-7f,9f,25f,41f,57f})
            {
                foreach(float y in new[]{h-1.4f,h-.25f})b.Tube(metal,new Vector3(-x,y,q),new Vector3(x,y,q),.095f,8);
                for(int i=0;i<28;i++){float a=-x+i*x*2/28,c=-x+(i+1)*x*2/28;b.Tube(metal,new Vector3(a,h-1.4f,q),new Vector3(c,h-.25f,q),.042f);}
            }
            foreach(int side in new[]{-1,1})
            {
                float sx=side*18.2f;foreach(float y in new[]{26.8f,27.8f})b.Tube(metal,new Vector3(sx,y,-27),new Vector3(sx,y,27),.075f,8);
                for(int i=0;i<22;i++)b.Tube(metal,new Vector3(sx,i%2==0?26.8f:27.8f,-27+i*54f/22),new Vector3(sx,i%2==0?27.8f:26.8f,-27+(i+1)*54f/22),.035f);
                foreach(float q in new[]{-24f,-16f,-8f,0,8f,16f,24f})
                {
                    b.Tube(metal,new Vector3(sx,27.8f,q),new Vector3(sx,h-1.3f,q),.024f);var p=new Vector3(sx,26.2f,q);var r=Quaternion.LookRotation(new Vector3(-side*.5f,-1,0));
                    b.Bevel(navy,p,new Vector3(1.65f,.85f,.36f),.07f,r);b.Box(led,p+r*new Vector3(0,0,.20f),new Vector3(1.46f,.66f,.035f),r);for(int i=-3;i<=3;i++)b.Box(metal,p+r*new Vector3(i*.20f,0,.23f),new Vector3(.025f,.67f,.025f),r);
                }
                // Structural service catwalks are above the seating, outside moving gameplay cameras.
                b.Floor(navy,new Vector3(side*46,27.6f,0),new Vector2(1.6f,116));foreach(float q in new[]{-58f,-38f,-18f,2f,22f,42f,58f})b.Tube(metal,new Vector3(side*46,27.6f,q),new Vector3(side*46,28.7f,q),.025f);
                foreach(float dx in new[]{-.75f,.75f})b.Tube(metal,new Vector3(side*46+dx,28.7f,-58),new Vector3(side*46+dx,28.7f,58),.028f);
            }
            b.Save(Root+"/Meshes");
            var lighting=Object.FindAnyObjectByType<ChampionshipPresentation>();
            foreach(var l in lighting.poolLights){int side=Mathf.Sign(l.transform.position.x)>0?1:-1;float q=Mathf.Sign(l.transform.position.z)*8.5f;l.transform.position=new Vector3(side*17.4f,25.7f,q);l.transform.LookAt(new Vector3(side*2,0,q*.6f));l.range=46;l.spotAngle=76;l.innerSpotAngle=50;}
            foreach(var l in lighting.technicalLights){int end=l.transform.position.z>0?1:-1;l.transform.position=new Vector3(17.2f,5.3f,end*18.1f);l.transform.LookAt(new Vector3(18.7f,1.2f,end*18));l.range=13;}
            foreach(var l in lighting.accentLights)
            {
                if(l.name=="Seating Cobalt Wash"){int side=l.transform.position.x>0?1:-1;l.transform.position=new Vector3(side*27,10,0);l.transform.LookAt(new Vector3(side*39,15,0));l.range=53;}
                else if(l.name=="Display Structure Wash"){int side=l.transform.position.x>0?1:-1;l.transform.position=new Vector3(side*8,12.7f,-27.7f);l.transform.LookAt(new Vector3(side*5.5f,6,-32));l.range=15;l.intensity=18;l.spotAngle=67;}
            }
        }
        static partial void IntegrateDisplays(ArenaQualityController env)
        {
            var primary=GameObject.Find("Primary Championship Scoreboard").transform;primary.position=new Vector3(-41.8f,16.8f,0); // Fits the reserved west middle/upper bowl opening.
            var oldVenue=GameObject.Find(PremiumArenaSetup.Name).transform;var displays=oldVenue.Find("Championship Scoreboards and Presentation Screens");var jumbo=displays.Find("Four Face Ceiling Championship Display");
            foreach(var r in jumbo.Find("Ceiling Display Housing and Suspension").GetComponentsInChildren<Renderer>(true))r.enabled=false;
            jumbo.position=new Vector3(0,25,0);var b=new ChampionshipArenaBatch(Child("Central Live Display Housing and Suspension",root));Vector3 p=jumbo.position;
            b.Bevel(navy,p,new Vector3(8.8f,3.6f,8.8f),.16f);
            foreach(int side in new[]{-1,1})
            {
                b.Box(screen,p+new Vector3(0,0,side*4.415f),new Vector3(8.45f,3.25f,.025f));b.Box(screen,p+new Vector3(side*4.415f,0,0),new Vector3(.025f,3.25f,8.45f));
                foreach(float y in new[]{-1.77f,1.77f}){b.Box(led,p+new Vector3(0,y,side*4.45f),new Vector3(8.65f,.035f,.025f));b.Box(led,p+new Vector3(side*4.45f,y,0),new Vector3(.025f,.035f,8.65f));}
                foreach(int q in new[]{-1,1}){b.Bevel(metal,p+new Vector3(side*4.35f,0,q*4.35f),new Vector3(.15f,3.8f,.15f),.03f);b.Tube(metal,p+new Vector3(side*3.6f,1.85f,q*3.6f),new Vector3(side*3.6f,layout.roofHeight-1.4f,q*3.6f),.035f,8);}
            }
            b.Save(Root+"/Meshes");
            foreach(Transform board in displays)if(board.name.EndsWith("deck timing screen")){int end=board.position.z>0?1:-1;board.position=new Vector3(-15.2f,1.65f,end*18.8f);board.rotation=Quaternion.Euler(0,-90,0);}
        }
        static partial void ConfigureInspection()
        {
            var dev=Object.FindAnyObjectByType<ArenaDeveloperCamera>();var p=new[]{new Vector3(46,36,-58),new Vector3(14,4,-29),new Vector3(18.5f,4,-12),new Vector3(31,10,-18),new Vector3(14,20,-17)};var at=new[]{new Vector3(0,5,0),new Vector3(0,0,9),new Vector3(18.5f,1.5f,-18),new Vector3(35,11,0),new Vector3(0,25,0)};
            for(int i=0;i<Mathf.Min(dev.viewpoints.Length,p.Length);i++)dev.viewpoints[i].SetPositionAndRotation(p[i],Quaternion.LookRotation(at[i]-p[i]));
        }
        [MenuItem("Tools/Water Polo/Arena/Refresh Milestone 20 Venue Reflection")]
        public static void RefreshReflection()
        {
            RequireSaved();var env=Object.FindAnyObjectByType<ArenaQualityController>();if(env.transform.Find(Name)==null)throw new InvalidOperationException("Build M20 first.");var probe=env.GetComponentInChildren<ReflectionProbe>();bool visible=env.water.waterSurface.enabled;var pipeline=QualitySettings.renderPipeline;int aa=QualitySettings.antiAliasing;env.water.waterSurface.enabled=false;QualitySettings.renderPipeline=env.pipelines[2];
            try{WaterVisualTestSetup.BakePoolProbe(probe,env.transform,env.transform,Root+"/Textures/Championship50mReflection.exr");}
            finally{env.water.waterSurface.enabled=visible;QualitySettings.renderPipeline=pipeline;QualitySettings.antiAliasing=aa;}
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
    }
}
