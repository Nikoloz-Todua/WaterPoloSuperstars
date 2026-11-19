using System;
using System.Linq;
using System.Collections.Generic;
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
    /// <summary>Presentation-only authoring around the configured competition field. Never rebuilds the basin or match.</summary>
    public static class ArenaRedevelopmentSetup
    {
        public const string Root="Assets/Arena/PoolsideRedevelopment",Name="Championship Poolside Redevelopment",Recovery="Assets/Scenes/Recovery/OlympicMatchArena_PreArenaRedevelopment.unity";
        static Material navy,metal,white,rubber,cobalt,led,screen,cord;
        static Material textMaterial;
        static ArenaDimensions dimensions;
        static int meshNumber;
        [MenuItem("Tools/Water Polo/Recovery/Arena/Earlier Arena Passes/Apply Championship Poolside Redevelopment")]
        public static void SetUp()
        {
            RequireSaved();var scene=SceneManager.GetActiveScene();var arena=Object.FindAnyObjectByType<ArenaQualityController>();
            if(arena.transform.Find(Name)!=null){Debug.Log("Poolside redevelopment already installed; authored tuning preserved.");return;}
            var pool=Object.FindAnyObjectByType<PoolBounds>();dimensions=arena.dimensions;
            if(pool.Width!=20||pool.Length!=25||dimensions.fieldWidth!=pool.Width||dimensions.fieldLength!=pool.Length)throw new InvalidOperationException("Audited 25 x 20 field required. This visual pass does not resize gameplay.");
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Recovery)==null&&!AssetDatabase.CopyAsset(scene.path,Recovery))throw new InvalidOperationException("Native recovery copy failed.");
            Folder(Root+"/Materials");Folder(Root+"/Meshes");Folder(Root+"/Prefabs");Folder(Root+"/Textures");meshNumber=0;
            navy=Mat("Marine Enamel",Hex("#14243B"),.28f,.36f);metal=Mat("Marine Alloy",Hex("#698294"),.76f,.42f);white=Mat("Moulded Competition Polymer",Hex("#DCE6EA"),.04f,.36f);
            rubber=Mat("Non Slip Event Rubber",Hex("#22374E"),.02f,.24f);rubber.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(PremiumArenaSetup.Root+"/Textures/FineRubber.png"));rubber.mainTextureScale=Vector2.one*3;EditorUtility.SetDirty(rubber);
            cobalt=Mat("Championship Cobalt",Hex("#184D8A"),.28f,.37f);led=Mat("Architectural Ice LED",Hex("#2D9FCC"),.12f,.42f,Hex("#4BC6EE")*.7f);screen=Mat("Ribbon LED Midnight",Hex("#061525"),.04f,.28f);cord=Mat("Graphite Goal Cord",Hex("#425C70"),.05f,.30f);
            textMaterial=AssetDatabase.LoadAssetAtPath<Material>(MatchBroadcastSetup.Root+"/Materials/Arena Text Front Faces.mat");
            var root=Child(Name,arena.transform);var fine=Child("Redevelopment Fine Equipment",arena.fineProps.transform);
            var venue=GameObject.Find(PremiumArenaSetup.Name).transform;
            // Retain all old visual objects/assets for scene recovery; only replaced renderers are retired.
            RetireVisuals(venue.Find("Blue and White Team Technical Areas"));
            foreach(var r in arena.fineProps.GetComponentsInChildren<Renderer>(true))if(!r.transform.IsChildOf(fine))r.enabled=false;
            foreach(var t in venue.Find("Competition Deck and Clear Official Lanes").GetComponentsInChildren<TMP_Text>(true))if(t.text=="WATER POLO SUPERSTARS"||t.text=="CHAMPIONSHIP  /  MATCH COURT 01")t.GetComponent<Renderer>().enabled=false;
            PoolEdge(root);RefereeDetails(root);Advertising(root);Benches(root,fine);CameraPlatforms(root,fine);SeatingAccess(root);CenterDisplay(venue);
            foreach(var goal in Object.FindObjectsByType<GoalMouth>()){var net=goal.transform.Find("Competition Goal Visual/Enclosing Woven Net");if(net!=null)net.GetComponent<Renderer>().sharedMaterial=cord;}
            WidenPlatformSections();RetargetExistingLights(venue);
            SaveVisualCopy(root,Root+"/Prefabs/ChampionshipPoolside.prefab");
            SaveVisualCopy(fine,Root+"/Prefabs/PoolsideFineEquipment.prefab");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("Championship poolside redevelopment saved. Field, basin, cameras, gameplay/HUD and live display bindings preserved.");
        }
        static void PoolEdge(Transform root)
        {
            var b=new Batch(Child("Sloped Competition Skirt and Overflow Edge",root));float w=dimensions.PoolWidth*.5f,l=dimensions.PoolLength*.5f;
            foreach(int s in new[]{-1,1})
            {
                Vector3 a=new Vector3(s*(w+.05f),.20f,-l),c=new Vector3(s*12.12f,.68f,-l);
                b.Quad(rubber,a,c,c+Vector3.forward*(2*l),a+Vector3.forward*(2*l),Vector3.up);
                b.Box(white,new Vector3(s*(w+.04f),.20f,0),new Vector3(.10f,.10f,2*l+.1f));b.Box(metal,new Vector3(s*(w+.17f),.20f,0),new Vector3(.10f,.035f,2*l));
                for(float z=-l+.10f;z<l;z+=.18f)b.Box(navy,new Vector3(s*(w+.17f),.223f,z),new Vector3(.075f,.012f,.05f));
                foreach(int end in new[]{-1,1})
                {
                    float z=end*12.8f;b.Box(metal,new Vector3(s*11.16f,.21f,z),new Vector3(.20f,.05f,.28f));b.Box(navy,new Vector3(s*11.16f,.25f,z),new Vector3(.10f,.035f,.18f));
                    for(int i=0;i<16;i++){float q=i*Mathf.PI*.5f;Vector3 p=new Vector3(s*(10.78f+i*.014f),.16f+Mathf.Cos(q)*.028f,z+Mathf.Sin(q)*.028f);float q2=(i+1)*Mathf.PI*.5f;Vector3 p2=new Vector3(s*(10.78f+(i+1)*.014f),.16f+Mathf.Cos(q2)*.028f,z+Mathf.Sin(q2)*.028f);b.Tube(metal,p,p2,.008f,6);}
                }
            }
            foreach(int s in new[]{-1,1})
            {
                b.Quad(rubber,new Vector3(-w,.20f,s*(l+.05f)),new Vector3(w,.20f,s*(l+.05f)),new Vector3(w,.64f,s*15.15f),new Vector3(-w,.64f,s*15.15f),Vector3.up);
                b.Box(white,new Vector3(0,.20f,s*(l+.04f)),new Vector3(2*w,.10f,.10f));b.Box(metal,new Vector3(0,.21f,s*(l+.17f)),new Vector3(2*w,.035f,.10f));
                b.Box(navy,new Vector3(0,.35f,s*15.20f),new Vector3(25.0f,.34f,.18f));
                Label("WATER POLO SUPERSTARS  /  CHAMPIONSHIP SERIES",b.parent,new Vector3(0,.53f,s*15.08f),Quaternion.Euler(0,s>0?0:180,0),new Vector2(12,.28f),2.4f,Hex("#C9E2ED"));
            }
            b.Save();
        }
        static void RefereeDetails(Transform root)
        {
            var b=new Batch(Child("Official Lane Support and Access",root));
            foreach(int s in new[]{-1,1})
            {
                b.Box(navy,new Vector3(s*13.05f,.38f,0),new Vector3(.08f,.52f,25.5f));
                for(float z=-11.7f;z<12;z+=2.6f){b.Box(metal,new Vector3(s*13.02f,.40f,z),new Vector3(.24f,.36f,.06f));b.Box(rubber,new Vector3(s*12.65f,.729f,z),new Vector3(.84f,.004f,.025f));}
                foreach(int end in new[]{-1,1})
                {
                    b.Box(navy,new Vector3(s*12.65f,.18f,end*14.2f),new Vector3(1.3f,.08f,.65f));
                    for(int i=0;i<3;i++)b.Box(metal,new Vector3(s*12.65f,.73f-i*.13f,end*(13.08f+i*.30f)),new Vector3(.88f,.022f,.04f));
                }
            }
            b.Save();
        }
        static void Advertising(Transform root)
        {
            var group=Child("Layered Advertising and LED Architecture",root);
            foreach(int s in new[]{-1,1})
            {
                var b=new Batch(Child(s<0?"West Perimeter":"East Perimeter",group));float x=s*13.65f;
                b.Box(navy,new Vector3(x,.66f,0),new Vector3(.22f,.86f,28.0f));b.Box(metal,new Vector3(x,.235f,0),new Vector3(.28f,.045f,28));b.Box(led,new Vector3(x-s*.125f,1.065f,0),new Vector3(.025f,.025f,27.9f));
                for(int k=-4;k<=4;k++){float z=k*3.1f;b.Box(screen,new Vector3(x-s*.12f,.66f,z),new Vector3(.018f,.65f,2.96f));Label(k%2==0?"WATER POLO SUPERSTARS":"TODUSTUDIOS  /  CHAMPIONSHIP",b.parent,new Vector3(x-s*.135f,.66f,z),Quaternion.Euler(0,s>0?90:-90,0),new Vector2(2.75f,.42f),2.5f,Hex("#C9E2ED"));}
                foreach(int half in new[]{-1,1})
                {
                    float z=half*9f;b.Box(navy,new Vector3(s*14.5f,1.05f,z),new Vector3(.30f,1.65f,9.8f));b.Box(cobalt,new Vector3(s*14.33f,1.1f,z),new Vector3(.035f,1.1f,9.55f));
                    b.Box(metal,new Vector3(s*14.5f,1.92f,z),new Vector3(.38f,.10f,9.9f));b.Box(screen,new Vector3(s*14.28f,1.91f,z),new Vector3(.026f,.34f,9.6f));b.Box(led,new Vector3(s*14.26f,2.10f,z),new Vector3(.028f,.024f,9.7f));
                    Label("CHAMPIONSHIP SERIES   /   WATER POLO SUPERSTARS",b.parent,new Vector3(s*14.245f,1.91f,z),Quaternion.Euler(0,s>0?90:-90,0),new Vector2(9.2f,.27f),2.2f,Hex("#42BDEB"));
                    for(int k=-2;k<=2;k++){float zz=z+k*1.85f;b.Box(metal,new Vector3(s*14.68f,.95f,zz),new Vector3(.08f,1.5f,.07f));b.Box(navy,new Vector3(s*14.34f,.6f,zz),new Vector3(.04f,.7f,.04f));}
                }
                b.Save();
            }
            foreach(int end in new[]{-1,1})
            {
                var b=new Batch(Child(end<0?"South Goal Backdrop":"North Goal Backdrop",group));
                b.Box(navy,new Vector3(0,.80f,end*15.65f),new Vector3(26.9f,.88f,.22f));b.Box(screen,new Vector3(0,.80f,end*15.52f),new Vector3(26.6f,.64f,.025f));b.Box(metal,new Vector3(0,1.24f,end*15.65f),new Vector3(27.1f,.055f,.30f));
                foreach(float x in new[]{-9f,-4.5f,0,4.5f,9f})Label(x==0?"CHAMPIONSHIP SERIES":"WATER POLO SUPERSTARS",b.parent,new Vector3(x,.80f,end*15.49f),Quaternion.Euler(0,end>0?0:180,0),new Vector2(4.1f,.42f),3.0f,Hex("#DCEAF0"));b.Save();
            }
        }
        static void Benches(Transform root,Transform fine)
        {
            foreach(int end in new[]{-1,1})
            {
                var b=new Batch(Child(end<0?"Blue End Corner Bench":"White End Corner Bench",root));var f=new Batch(Child(end<0?"Blue Bench Equipment":"White Bench Equipment",fine));
                Vector3 origin=new Vector3(8,.72f,end*18.05f);Quaternion rotation=Quaternion.Euler(0,end<0?0:180,0);
                b.Box(navy,new Vector3(8,.45f,end*18.05f),new Vector3(6.3f,.54f,2.5f));b.Box(rubber,new Vector3(8,.725f,end*18.05f),new Vector3(6.2f,.035f,2.4f));
                b.Box(metal,new Vector3(8,.69f,end*16.82f),new Vector3(6.3f,.045f,.04f));b.Box(cobalt,new Vector3(8,.46f,end*16.79f),new Vector3(6.12f,.32f,.035f));
                foreach(float x in new[]{5.25f,10.7f})for(int step=0;step<3;step++)b.Box(rubber,new Vector3(x,.18f+(step+1)*.09f,end*(16.25f+step*.19f)),new Vector3(.65f,(step+1)*.18f,.21f));
                for(int i=0;i<7;i++)Chair(b,origin+Vector3.right*((i-3)*.78f),rotation);
                Label(end<0?"BLUE  /  TEAM BENCH":"WHITE  /  TEAM BENCH",b.parent,new Vector3(8,.46f,end*16.76f),Quaternion.Euler(0,end>0?0:180,0),new Vector2(5.7f,.24f),2.1f,Hex("#E1EBEF"));
                for(int i=0;i<3;i++)f.Box(white,new Vector3(11.7f,.9f+i*.05f,end*18.4f),new Vector3(.60f,.045f,.48f));
                f.Bevel(white,new Vector3(11.7f,.54f,end*18.4f),new Vector3(.80f,.72f,.65f),.055f);f.Bevel(cobalt,new Vector3(11.7f,.93f,end*18.4f),new Vector3(.84f,.10f,.69f),.035f);
                f.Bevel(navy,new Vector3(4.4f,.52f,end*18.1f),new Vector3(.65f,.68f,.9f),.04f);foreach(int s in new[]{-1,1})f.Box(metal,new Vector3(4.4f+s*.23f,.52f,end*18.1f),new Vector3(.04f,.70f,.92f));
                for(int i=0;i<5;i++){Vector3 p=new Vector3(11.4f+i*.14f,1.02f,end*18.4f);f.Tube(white,p,p+Vector3.up*.19f,.045f,8);f.Tube(cobalt,p+Vector3.up*.19f,p+Vector3.up*.22f,.03f,8);}
                // Distinct lower camera/equipment deck in front of the raised team bench.
                b.Box(navy,new Vector3(8,.28f,end*16.32f),new Vector3(2.1f,.20f,.85f));CameraProp(f,new Vector3(8,.39f,end*16.32f),Quaternion.LookRotation(new Vector3(-8,0,-end*16.32f)));
                b.Save(true);f.Save();
            }
            // Reuse both low live timing boards, now beside the end-corner benches rather than across their backs.
            var displays=GameObject.Find(PremiumArenaSetup.Name).transform.Find("Championship Scoreboards and Presentation Screens");
            foreach(Transform t in displays)if(t.name.EndsWith("deck timing screen")){float end=Mathf.Sign(t.position.z);t.localPosition=new Vector3(-5.2f,1.35f,end*18.0f);t.localScale=new Vector3(.52f,.8f,1);}
        }
        static void Chair(Batch b,Vector3 origin,Quaternion rotation)
        {
            Vector3 P(float x,float y,float z)=>origin+rotation*new Vector3(x,y,z);
            b.Bevel(white,P(0,.46f,0),new Vector3(.54f,.055f,.48f),.04f,rotation);
            foreach(int s in new[]{-1,1}){b.Tube(metal,P(s*.23f,.43f,-.19f),P(s*.29f,.015f,-.28f),.018f,8);b.Tube(metal,P(s*.23f,.43f,.18f),P(s*.28f,.015f,.29f),.018f,8);b.Tube(metal,P(s*.23f,.42f,.18f),P(s*.23f,.96f,.30f),.018f,8);b.Tube(metal,P(s*.27f,.16f,-.24f),P(s*.27f,.16f,.25f),.013f,6);}
            b.Bevel(white,P(0,.67f,.245f),new Vector3(.50f,.17f,.045f),.025f,rotation*Quaternion.Euler(12,0,0));
            for(int i=0;i<4;i++)b.Bevel(white,P(0,.79f+i*.06f,.27f+i*.012f),new Vector3(.49f-i*.007f,.040f,.045f),.013f,rotation*Quaternion.Euler(12,0,0));
            foreach(int s in new[]{-1,1})b.Tube(white,P(s*.22f,.70f,.25f),P(s*.22f,.99f,.31f),.026f,8);
        }
        static void CameraPlatforms(Transform root,Transform fine)
        {
            foreach(int s in new[]{-1,1})
            {
                var b=new Batch(Child(s<0?"West Broadcast Platform":"East Broadcast Platform",root));var f=new Batch(Child(s<0?"West Broadcast Rig":"East Broadcast Rig",fine));Vector3 p=new Vector3(s*16.35f,1.45f,-s*9.3f);
                b.Box(navy,p-Vector3.up*.60f,new Vector3(2.6f,1.2f,2.7f));b.Box(rubber,p,new Vector3(2.6f,.08f,2.7f));
                foreach(int z in new[]{-1,1}){b.Tube(metal,p+new Vector3(-s*1.25f,.04f,z*1.30f),p+new Vector3(-s*1.25f,.90f,z*1.30f),.024f,8);b.Tube(metal,p+new Vector3(s*1.25f,.04f,z*1.30f),p+new Vector3(s*1.25f,.90f,z*1.30f),.024f,8);b.Tube(metal,p+new Vector3(-1.25f,.9f,z*1.30f),p+new Vector3(1.25f,.9f,z*1.30f),.025f,8);}
                for(int i=0;i<6;i++)b.Box(rubber,new Vector3(s*(17.9f+i*.20f),.18f+(6-i)*.105f,-s*9.3f),new Vector3(.21f,(6-i)*.21f,.90f));
                CameraProp(f,p+Vector3.up*.05f,Quaternion.LookRotation(new Vector3(-s,0,0)));Chair(f,p+new Vector3(s*.65f,.05f,.65f),Quaternion.LookRotation(new Vector3(-s,0,0)));
                Label("BROADCAST  /  CAMERA",b.parent,p+new Vector3(-s*1.32f,-.45f,0),Quaternion.Euler(0,s>0?90:-90,0),new Vector2(2.45f,.35f),2.0f,Hex("#C0DBE7"));b.Save(true);f.Save();
            }
        }
        static void CameraProp(Batch b,Vector3 p,Quaternion r)
        {
            Vector3 P(float x,float y,float z)=>p+r*new Vector3(x,y,z);
            for(int i=0;i<3;i++){float a=i*Mathf.PI*2/3;b.Tube(metal,P(0,1.0f,0),P(Mathf.Cos(a)*.42f,.02f,Mathf.Sin(a)*.42f),.025f,8);}
            b.Bevel(navy,P(0,1.10f,.04f),new Vector3(.34f,.25f,.48f),.04f,r);b.Tube(navy,P(0,1.14f,.23f),P(0,1.14f,.57f),.075f,10);b.Tube(screen,P(0,1.14f,.57f),P(0,1.14f,.585f),.063f,10);b.Box(rubber,P(.18f,1.25f,-.12f),new Vector3(.03f,.15f,.20f),r);b.Tube(metal,P(.14f,1.02f,-.10f),P(.14f,.85f,-.42f),.014f,6);
        }
        static void SeatingAccess(Transform root)
        {
            var b=new Batch(Child("Grandstand Tier Access and Soffits",root));float front=dimensions.PoolWidth*.5f+dimensions.deckWidth+.7f;
            foreach(int side in new[]{-1,1})foreach(int end in new[]{-1,1})
            {
                float z=end*18.25f;for(int i=0;i<12;i++){float x=side*(front-.35f+i*.46f),h=.18f+(i+1)*.225f;b.Box(navy,new Vector3(x,(h+.18f)*.5f,z),new Vector3(.47f,h-.18f,.9f));b.Box(metal,new Vector3(x,h+.012f,z),new Vector3(.44f,.02f,.84f));b.Box(led,new Vector3(x-side*.218f,h+.026f,z),new Vector3(.02f,.018f,.78f));if(i%2==0)b.Tube(metal,new Vector3(x,h,z+end*.47f),new Vector3(x,h+.92f,z+end*.47f),.023f,8);}
                b.Tube(metal,new Vector3(side*(front-.35f),1.35f,z+end*.47f),new Vector3(side*(front+4.7f),3.82f,z+end*.47f),.025f,8);
                for(int i=0;i<5;i++)b.Box(cobalt,new Vector3(side*(front+2.7f),.7f+i*.47f,end*17.9f),new Vector3(5.4f,.07f,.045f));
            }
            b.Save();
        }
        static void CenterDisplay(Transform venue)
        {
            var jumbo=venue.Find("Championship Scoreboards and Presentation Screens/Four Face End Hung Championship Display");
            foreach(var r in jumbo.GetComponentsInChildren<MeshRenderer>(true))if(r.GetComponent<TMP_Text>()==null)r.enabled=false;
            jumbo.localPosition=new Vector3(0,25,0);jumbo.name="Four Face Ceiling Championship Display";
            var observer=Object.FindAnyObjectByType<ArenaEventDisplay>();
            foreach(Transform face in jumbo.Cast<Transform>().Where(t=>t.name.EndsWith("display face")).ToArray())
            {
                Vector3 front=-face.forward;face.localPosition=front*4.43f;face.localScale=Vector3.one;
                foreach(var text in face.GetComponentsInChildren<TMP_Text>(true))
                {
                    bool score=observer.scores.Contains(text),clock=observer.clocks.Contains(text);text.transform.localPosition=new Vector3(0,score?.05f:clock?-1.05f:1.22f,-.045f);text.rectTransform.sizeDelta=new Vector2(8.0f,score?1.35f:clock?.55f:.38f);text.enableAutoSizing=true;text.fontSizeMin=score?3.5f:2f;text.fontSizeMax=score?10f:clock?5.2f:3.1f;text.textWrappingMode=TextWrappingModes.NoWrap;text.ForceMeshUpdate(true);
                }
            }
            var b=new Batch(Child("Ceiling Display Housing and Suspension",jumbo));b.Bevel(navy,Vector3.zero,new Vector3(8.80f,3.6f,8.80f),.12f);
            foreach(int s in new[]{-1,1})
            {
                b.Box(screen,new Vector3(0,0,s*4.415f),new Vector3(8.45f,3.25f,.025f));b.Box(screen,new Vector3(s*4.415f,0,0),new Vector3(.025f,3.25f,8.45f));
                foreach(float y in new[]{-1.77f,1.77f}){b.Box(led,new Vector3(0,y,s*4.45f),new Vector3(8.65f,.035f,.025f));b.Box(led,new Vector3(s*4.45f,y,0),new Vector3(.025f,.035f,8.65f));}
                foreach(int q in new[]{-1,1}){b.Bevel(metal,new Vector3(s*4.35f,0,q*4.35f),new Vector3(.15f,3.8f,.15f),.03f);b.Tube(metal,new Vector3(s*3.6f,1.85f,q*3.6f),new Vector3(s*3.6f,4.45f,q*3.6f),.035f,10);}
                b.Tube(metal,new Vector3(s*13.7f,3.5f,-4),new Vector3(s*13.7f,3.5f,4),.07f,8);
                b.Tube(metal,new Vector3(-13.7f,3.5f,s*3.6f),new Vector3(13.7f,3.5f,s*3.6f),.07f,8);b.Tube(metal,new Vector3(-13.7f,4.3f,s*3.6f),new Vector3(13.7f,4.3f,s*3.6f),.07f,8);
                for(int i=0;i<18;i++)b.Tube(metal,new Vector3(-13.7f+i*1.52f,3.5f,s*3.6f),new Vector3(-13.7f+(i+1)*1.52f,4.3f,s*3.6f),.035f,6);
            }
            b.Save();var accents=GameObject.Find(SprintPolishSetup.Name).transform.Find("Championship Display Highlights");if(accents!=null)RetireVisuals(accents);
            SaveVisualCopy(jumbo,Root+"/Prefabs/CenteredLiveJumbotron.prefab");
        }
        static void WidenPlatformSections()
        {
            var platforms=GameObject.Find(AuthenticFieldSetup.Name).transform.Find("Elevated Referee Platforms");
            foreach(var f in platforms.GetComponentsInChildren<MeshFilter>())
            {
                if(!f.name.Contains("Markers")||f.name.Contains("White")||AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith(Root))continue;float x=f.transform.parent.name.StartsWith("West")?-12.65f:12.65f;var mesh=Object.Instantiate(f.sharedMesh);var vertices=mesh.vertices;
                for(int i=0;i<vertices.Length;i++){float d=vertices[i].x-x;if(Mathf.Abs(Mathf.Abs(d)-.41f)<.006f)vertices[i].x=x+Mathf.Sign(d)*.48f;}mesh.vertices=vertices;mesh.RecalculateBounds();mesh.name=f.name+" Full Width";AssetDatabase.CreateAsset(mesh,Root+"/Meshes/"+mesh.name+".asset");f.sharedMesh=mesh;
            }
        }
        static void RetargetExistingLights(Transform venue)
        {
            var presentation=venue.GetComponent<ChampionshipPresentation>();foreach(var l in presentation.technicalLights){int end=l.transform.position.z<0?-1:1;l.transform.position=new Vector3(8,3.7f,end*17.1f);l.transform.LookAt(new Vector3(8,.9f,end*18.1f));}
            if(presentation.accentLights!=null)foreach(var l in presentation.accentLights)if(l.name=="Display Structure Wash"){float s=Mathf.Sign(l.transform.position.x);l.transform.position=new Vector3(s*6,26.8f,0);l.transform.LookAt(new Vector3(s*4.4f,26.6f,0));l.intensity=5f;l.range=6f;}
        }
        [MenuItem("Tools/Water Polo/Recovery/Arena/Earlier Arena Passes/Refresh Redeveloped Arena Reflection")]
        public static void RefreshReflection()
        {
            RequireSaved();var arena=Object.FindAnyObjectByType<ArenaQualityController>();if(arena.transform.Find(Name)==null)throw new InvalidOperationException("Install redevelopment first.");var probe=arena.GetComponentInChildren<ReflectionProbe>();bool visible=arena.water.waterSurface.enabled;var pipeline=QualitySettings.renderPipeline;int aa=QualitySettings.antiAliasing;arena.water.waterSurface.enabled=false;QualitySettings.renderPipeline=arena.pipelines[2];
            try{WaterVisualTestSetup.BakePoolProbe(probe,arena.transform,arena.transform,Root+"/Textures/PoolsideReflection.exr");}
            finally{arena.water.waterSurface.enabled=visible;QualitySettings.renderPipeline=pipeline;QualitySettings.antiAliasing=aa;}
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        }
        static void RequireSaved(){var s=SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||s.isDirty||s.path!=OlympicArenaSetup.ScenePath)throw new InvalidOperationException("Open saved OlympicMatchArena outside Play Mode.");}
        // Explicit authoring completion for an interrupted first installation; normal repeat setup never rebuilds.
        public static void CompleteAppliedGeometry()
        {
            var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!=OlympicArenaSetup.ScenePath||GameObject.Find(Name)==null)throw new InvalidOperationException("Existing applied arena geometry required outside Play Mode.");
            var arena=Object.FindAnyObjectByType<ArenaQualityController>();var venue=GameObject.Find(PremiumArenaSetup.Name).transform;WidenPlatformSections();RetargetExistingLights(venue);
            SaveVisualCopy(GameObject.Find(Name).transform,Root+"/Prefabs/ChampionshipPoolside.prefab");SaveVisualCopy(arena.fineProps.transform.Find("Redevelopment Fine Equipment"),Root+"/Prefabs/PoolsideFineEquipment.prefab");SaveVisualCopy(venue.Find("Championship Scoreboards and Presentation Screens/Four Face Ceiling Championship Display"),Root+"/Prefabs/CenteredLiveJumbotron.prefab");EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        static void SaveVisualCopy(Transform source,string path)
        {
            var copy=Object.Instantiate(source.gameObject);copy.name=source.name;copy.transform.SetParent(null,true);
            try{if(PrefabUtility.IsPartOfPrefabInstance(copy))PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(copy),PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);PrefabUtility.SaveAsPrefabAsset(copy,path);}
            finally{Object.DestroyImmediate(copy);}
        }
        static void RetireVisuals(Transform root){if(root==null)return;if(root.GetComponentsInChildren<Collider>(true).Length>0||root.GetComponentsInChildren<MonoBehaviour>(true).Any(c=>!(c is TMP_Text)))throw new InvalidOperationException("Only audited visual groups may be retired: "+root.name);foreach(var r in root.GetComponentsInChildren<Renderer>(true))r.enabled=false;}
        static Color Hex(string value){ColorUtility.TryParseHtmlString(value,out var c);return c;}
        static Material Mat(string n,Color color,float metallic,float smooth,Color emission=default){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=n,color=color};m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);if(emission.maxColorComponent>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",emission);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;}AssetDatabase.CreateAsset(m,Root+"/Materials/"+n+".mat");return m;}
        static Transform Child(string n,Transform p){var t=new GameObject(n).transform;t.SetParent(p,false);return t;}
        static void Label(string caption,Transform parent,Vector3 position,Quaternion rotation,Vector2 size,float font,Color color){var t=Child(caption,parent);t.localPosition=position;t.localRotation=rotation;var text=t.gameObject.AddComponent<TextMeshPro>();text.font=TMP_Settings.defaultFontAsset;text.fontSharedMaterial=textMaterial;text.text=caption;text.fontSize=font;text.fontSizeMax=font;text.fontSizeMin=font*.65f;text.enableAutoSizing=true;text.fontStyle=FontStyles.Bold;text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=size;text.textWrappingMode=TextWrappingModes.NoWrap;text.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;}
        static void Folder(string p){if(AssetDatabase.IsValidFolder(p))return;int s=p.LastIndexOf('/');Folder(p.Substring(0,s));AssetDatabase.CreateFolder(p.Substring(0,s),p.Substring(s+1));}
        sealed class Batch
        {
            public readonly Transform parent;readonly Dictionary<Material,Geometry> parts=new Dictionary<Material,Geometry>();public Batch(Transform parent){this.parent=parent;}
            Geometry Get(Material m){if(!parts.TryGetValue(m,out var g)){g=new Geometry();parts.Add(m,g);}return g;}
            public void Box(Material m,Vector3 p,Vector3 size,Quaternion r=default)=>Get(m).Box(p,size,r==default?Quaternion.identity:r);
            public void Bevel(Material m,Vector3 p,Vector3 size,float bevel,Quaternion r=default)=>Get(m).Bevel(p,size,bevel,r==default?Quaternion.identity:r);
            public void Tube(Material m,Vector3 a,Vector3 b,float radius,int sides)=>Get(m).Tube(a,b,radius,sides);
            public void Quad(Material m,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal)=>Get(m).Face(a,b,c,d,normal);
            public void Save(bool shadows=false){foreach(var pair in parts){string name=parent.name+" "+pair.Key.name;var mesh=pair.Value.Mesh(name);AssetDatabase.CreateAsset(mesh,Root+"/Meshes/"+(meshNumber++).ToString("00")+" "+name+".asset");var t=Child(name,parent);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=pair.Key;r.shadowCastingMode=shadows?ShadowCastingMode.On:ShadowCastingMode.Off;GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.ReflectionProbeStatic);}}
        }
        sealed class Geometry
        {
            readonly List<Vector3> vertices=new List<Vector3>();readonly List<int> triangles=new List<int>();readonly List<Vector2> uv=new List<Vector2>();
            public void Face(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal){if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)<0){var p=b;b=d;d=p;}int n=vertices.Count;foreach(var p in new[]{a,b,c,d}){vertices.Add(p);Vector3 q=new Vector3(Mathf.Abs(normal.x),Mathf.Abs(normal.y),Mathf.Abs(normal.z));uv.Add(q.y>q.x&&q.y>q.z?new Vector2(p.x,p.z):q.x>q.z?new Vector2(p.z,p.y):new Vector2(p.x,p.y));}triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            public void Box(Vector3 p,Vector3 size,Quaternion r){Vector3 h=size*.5f;for(int axis=0;axis<3;axis++)foreach(int sign in new[]{-1,1}){Vector3 a=Vector3.zero,u=Vector3.zero,v=Vector3.zero;a[axis]=h[axis]*sign;u[(axis+1)%3]=h[(axis+1)%3];v[(axis+2)%3]=h[(axis+2)%3];Vector3 normal=Vector3.zero;normal[axis]=sign;Face(p+r*(a-u-v),p+r*(a+u-v),p+r*(a+u+v),p+r*(a-u+v),r*normal);}}
            public void Bevel(Vector3 p,Vector3 size,float b,Quaternion r)
            {
                Vector3 h=size*.5f;b=Mathf.Min(b,Mathf.Min(h.x,Mathf.Min(h.y,h.z))*.8f);
                // Chamfer the four vertical corners; rounded outline is evident on seats, cases and housings.
                var ring=new[]{new Vector2(-h.x+b,-h.z),new Vector2(h.x-b,-h.z),new Vector2(h.x,-h.z+b),new Vector2(h.x,h.z-b),new Vector2(h.x-b,h.z),new Vector2(-h.x+b,h.z),new Vector2(-h.x,h.z-b),new Vector2(-h.x,-h.z+b)};
                for(int i=0;i<8;i++){var a=ring[i];var c=ring[(i+1)%8];Vector3 A=new Vector3(a.x,-h.y,a.y),B=new Vector3(c.x,-h.y,c.y);Face(p+r*A,p+r*B,p+r*(B+Vector3.up*size.y),p+r*(A+Vector3.up*size.y),r*new Vector3(a.x+c.x,0,a.y+c.y).normalized);}
                for(int i=1;i<7;i++)foreach(int sign in new[]{-1,1}){Vector3 a=new Vector3(ring[0].x,sign*h.y,ring[0].y),c=new Vector3(ring[i].x,sign*h.y,ring[i].y),d=new Vector3(ring[i+1].x,sign*h.y,ring[i+1].y);Face(p+r*a,p+r*c,p+r*d,p+r*a,r*Vector3.up*sign);}
            }
            public void Tube(Vector3 a,Vector3 b,float radius,int sides){Vector3 axis=(b-a).normalized,u=Vector3.Cross(axis,Vector3.up).normalized;if(u.sqrMagnitude<.01f)u=Vector3.right;Vector3 v=Vector3.Cross(axis,u);for(int i=0;i<sides;i++){float x=i*Mathf.PI*2/sides,y=(i+1)*Mathf.PI*2/sides;Vector3 p=(u*Mathf.Cos(x)+v*Mathf.Sin(x))*radius,q=(u*Mathf.Cos(y)+v*Mathf.Sin(y))*radius;Face(a+p,b+p,b+q,a+q,(p+q).normalized);Face(a,a+q,a+p,a,-axis);Face(b,b+p,b+q,b,axis);}}
            public Mesh Mesh(string name){var m=new Mesh{name=name,indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};m.SetVertices(vertices);m.SetUVs(0,uv);m.SetTriangles(triangles,0);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return m;}
        }
    }
}
