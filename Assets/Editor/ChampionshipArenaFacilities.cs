using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using ToduStudios.WaterPolo.WaterVisuals;
using Object=UnityEngine.Object;

namespace ToduStudios.WaterPolo.Editor
{
    public static partial class ChampionshipArenaRebuild
    {
        static partial void BuildIntroduction()
        {
            var entrance=Child("E Athlete Entrance and Anthem Presentation",root);var b=new ChampionshipArenaBatch(entrance);var staging=entrance.gameObject.AddComponent<VenueIntroductionAnchors>();
            b.Bevel(navy,new Vector3(0,.42f,-28.8f),new Vector3(20,.56f,5.3f),.12f);b.Floor(rubber,new Vector3(0,.715f,-28.8f),new Vector2(19.8f,5.1f));b.Box(metal,new Vector3(0,.69f,-26.17f),new Vector3(20,.045f,.08f));
            foreach(int side in new[]{-1,1})
            {
                b.Bevel(cobalt,new Vector3(side*10.3f,5.2f,-32.2f),new Vector3(1.3f,9.2f,1.4f),.15f);b.Box(led,new Vector3(side*9.62f,5.2f,-31.45f),new Vector3(.075f,8.6f,.04f));b.Box(gold,new Vector3(side*10.3f,9.82f,-31.45f),new Vector3(1.2f,.07f,.05f));
                // Two broad real backstage lanes, closed at the far service end but open towards the stage.
                b.Box(glass,new Vector3(side*7.2f,2.85f,-38.3f),new Vector3(.35f,4.3f,12.4f));b.Floor(rubber,new Vector3(side*3.6f,.70f,-38.3f),new Vector2(6.8f,12.4f));b.Box(navy,new Vector3(side*3.6f,5,-38.3f),new Vector3(7.2f,.3f,12.4f));
                for(int i=0;i<5;i++)b.Box(led,new Vector3(side*6.97f,3.8f,-33.6f-i*2.2f),new Vector3(.025f,.055f,1.4f));
                b.Box(navy,new Vector3(side*3.6f,2.8f,-44.6f),new Vector3(7.2f,4.3f,.2f));
                for(int i=0;i<4;i++)b.Box(metal,new Vector3(side*(10.7f+i*.45f),.715f,-28.8f),new Vector3(.08f,.025f,2.5f));
            }
            b.Box(glass,new Vector3(0,2.85f,-38.3f),new Vector3(.25f,4.3f,12.4f));b.Bevel(navy,new Vector3(0,7.7f,-32.3f),new Vector3(21.8f,6.0f,1.3f),.18f);b.Box(screen,new Vector3(0,7.7f,-31.62f),new Vector3(20.5f,5.25f,.04f));
            foreach(float y in new[]{5.0f,10.4f})b.Box(led,new Vector3(0,y,-31.57f),new Vector3(20.7f,.055f,.025f));b.Save(Root+"/Meshes",true);
            var display=Child("Replaceable Club Crest Screen",entrance).gameObject.AddComponent<VenueCrestDisplay>();var image=Child("Club Artwork Image",display.transform);image.localPosition=new Vector3(0,7.55f,-31.55f);var imageMesh=new ChampionshipArenaMesh();imageMesh.Face(new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0),Vector3.forward,Vector2.zero,Vector2.right,Vector2.one,Vector2.up);var mesh=imageMesh.Mesh("Club Crest Unit Panel");AssetDatabase.CreateAsset(mesh,Root+"/Meshes/Club Crest Unit Panel.asset");image.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=image.gameObject.AddComponent<MeshRenderer>();var material=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="Replaceable Club Crest Artwork"};AssetDatabase.CreateAsset(material,Root+"/Materials/Replaceable Club Crest Artwork.mat");renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.enabled=false;display.image=renderer;
            display.fallback=Label("WPS",display.transform,new Vector3(0,7.6f,-31.5f),Quaternion.Euler(0,180,0),new Vector2(4.2f,2.4f),12,white.color);display.teamCaption=Label("BLUE TEAM  /  INTRODUCTION",display.transform,new Vector3(0,5.55f,-31.5f),Quaternion.Euler(0,180,0),new Vector2(12,.48f),3.5f,ice.color);
            Label("WATER POLO\nSUPERSTARS",entrance,new Vector3(-6.9f,8.0f,-31.5f),Quaternion.Euler(0,180,0),new Vector2(5.5f,2.4f),4.8f,white.color);Label("CHAMPIONSHIP\nNIGHT",entrance,new Vector3(6.9f,8.0f,-31.5f),Quaternion.Euler(0,180,0),new Vector2(5.5f,2.4f),4.8f,white.color);
            staging.crest=display;staging.blueLineup=new Transform[7];staging.whiteLineup=new Transform[7];var marks=new ChampionshipArenaBatch(Child("Anthem Standing Marks",entrance));
            for(int i=0;i<7;i++)
            {
                float x=-7.2f+i*1.05f;staging.blueLineup[i]=Anchor("Blue Lineup "+(i+1),entrance,new Vector3(x,.72f,-28.6f));staging.whiteLineup[i]=Anchor("White Lineup "+(i+1),entrance,new Vector3(-x,.72f,-28.6f));
                foreach(float px in new[]{x,-x}){marks.Box(ice,new Vector3(px,.726f,-28.6f),new Vector3(.36f,.012f,.025f));marks.Box(gold,new Vector3(px,.726f,-28.85f),new Vector3(.08f,.012f,.025f));}
            }
            marks.Save(Root+"/Meshes");
            staging.blueEntranceRoute=new[]{new Vector3(-3.6f,.7f,-43),new Vector3(-3.6f,.7f,-33),new Vector3(-3.6f,.72f,-29),new Vector3(11.8f,.7f,-28.8f),new Vector3(15.3f,.7f,-28.8f),new Vector3(15.3f,.7f,-16)}.Select((p,i)=>Anchor("Blue Introduction Route "+i,entrance,p)).ToArray();
            staging.whiteEntranceRoute=new[]{new Vector3(3.6f,.7f,-43),new Vector3(3.6f,.7f,-33),new Vector3(3.6f,.72f,-29),new Vector3(11.8f,.7f,-28.8f),new Vector3(15.3f,.7f,-28.8f),new Vector3(15.3f,.7f,17)}.Select((p,i)=>Anchor("White Introduction Route "+i,entrance,p)).ToArray();
            staging.introductionCamera=Anchor("Future Introduction Camera Anchor",entrance,new Vector3(0,3.5f,-22.5f));staging.introductionCamera.LookAt(new Vector3(0,3,-31));
        }
        static Vector3[] RibbonPolygon(float sideX,float endZ)
        {
            return new[]{new Vector3(sideX,0,-31.7f),new Vector3(sideX,0,31.7f),new Vector3(18.8f,0,endZ),new Vector3(-18.8f,0,endZ),new Vector3(-sideX,0,31.7f),new Vector3(-sideX,0,-31.7f),new Vector3(-18.8f,0,-endZ),new Vector3(18.8f,0,-endZ)};
        }
        static partial void BuildRibbons()
        {
            var technology=Child("G Inter Tier Digital Ribbons",root);
            for(int level=0;level<2;level++)
            {
                float y=level==0?7.1f:14.7f;var b=new ChampionshipArenaBatch(Child("Tier Ribbon "+(level+1),technology));var points=RibbonPolygon(level==0?32.2f:43.2f,level==0?46.8f:57.8f);float running=0;
                for(int i=0;i<points.Length;i++)
                {
                    var a=points[i]+Vector3.up*y;var z=points[(i+1)%points.Length]+Vector3.up*y;var direction=(z-a).normalized;var inward=Vector3.Cross(Vector3.up,direction);if(Vector3.Dot(inward,-(a+z)*.5f)<0)inward=-inward;float length=Vector3.Distance(a,z);int panels=Mathf.CeilToInt(length/4.3f);
                    for(int panel=0;panel<panels;panel++)
                    {
                        float t0=(float)panel/panels,t1=(float)(panel+1)/panels;Vector3 p=Vector3.Lerp(a,z,t0)+direction*.025f,q=Vector3.Lerp(a,z,t1)-direction*.025f;float u0=(running+length*t0)/8,u1=(running+length*t1)/8;
                        b.Part(ribbon).Face(p-Vector3.up*.48f,q-Vector3.up*.48f,q+Vector3.up*.48f,p+Vector3.up*.48f,inward,new Vector2(u0,0),new Vector2(u1,0),new Vector2(u1,1),new Vector2(u0,1));
                        b.Box(navy,(p+q)*.5f-inward*.10f,new Vector3(Vector3.Distance(p,q),1.1f,.20f),Quaternion.LookRotation(-inward));
                    }
                    b.Tube(metal,a+Vector3.up*.55f,z+Vector3.up*.55f,.035f);b.Tube(ice,a-Vector3.up*.55f,z-Vector3.up*.55f,.025f);
                    int labels=Mathf.Max(1,Mathf.FloorToInt(length/12));for(int n=0;n<labels;n++){var p=Vector3.Lerp(a,z,(n+.5f)/labels)+inward*.035f;Label((n+i)%2==0?"WATER POLO SUPERSTARS":"CHAMPIONSHIP NIGHT",b.root,p,Quaternion.LookRotation(-inward),new Vector2(8.5f,.55f),3.7f,white.color);}
                    running+=length;
                }
                ribbons.AddRange(b.Save(Root+"/Meshes").Where(r=>r.sharedMaterial==ribbon));
            }
        }
        static partial void BuildMedia()
        {
            var b=new ChampionshipArenaBatch(Child("Media Platforms Commentary and Officials",root));
            foreach(int end in new[]{-1,1})
            {
                Vector3 p=new Vector3(-19.0f,3.1f,end*8.7f);b.Bevel(navy,p-Vector3.up*1.2f,new Vector3(4.4f,2.4f,4),.1f);b.Floor(rubber,p+Vector3.up*.02f,new Vector2(4.4f,4));
                foreach(float z in new[]{-1.9f,1.9f}){b.Tube(metal,p+new Vector3(-2.05f,0,z),p+new Vector3(-2.05f,1.1f,z),.025f);b.Tube(metal,p+new Vector3(-2.05f,1.1f,z),p+new Vector3(1.9f,1.1f,z),.025f);}
                CameraProp(b,p+Vector3.up*.03f,Quaternion.LookRotation(Vector3.right));BenchChair(b,p+new Vector3(-1.1f,.03f,.7f),Quaternion.LookRotation(Vector3.right));
                for(int i=0;i<12;i++)b.Box(concrete,new Vector3(-21.6f-i*.26f,.7f+(12-i)*.1f,end*8.7f),new Vector3(.27f,(12-i)*.2f,1.0f));
                b.Tube(navy,new Vector3(-21.1f,.74f,end*8.7f),new Vector3(-21.1f,.74f,end*27.0f),.035f);Label("BROADCAST",b.root,p+new Vector3(2.25f,-.6f,0),Quaternion.Euler(0,-90,0),new Vector2(3.5f,.35f),2.6f,white.color);
            }
            // Officials table is rebuilt in the west central opening, facing the unobstructed field.
            b.Bevel(navy,new Vector3(-16.5f,1.12f,0),new Vector3(1.65f,.84f,5.6f),.08f);b.Bevel(metal,new Vector3(-16.5f,1.56f,0),new Vector3(1.85f,.08f,5.8f),.04f);
            for(int i=-1;i<=1;i++){b.Bevel(screen,new Vector3(-16.25f,1.88f,i*1.6f),new Vector3(.13f,.48f,.7f),.03f);BenchChair(b,new Vector3(-17.5f,.7f,i*1.6f),Quaternion.LookRotation(Vector3.right));}
            Label("MATCH OPERATIONS",b.root,new Vector3(-15.64f,1.13f,0),Quaternion.Euler(0,-90,0),new Vector2(5.2f,.35f),2.8f,white.color);
            b.Save(Root+"/Meshes",true);
            var booth=new ChampionshipArenaBatch(Child("Upper Commentary Gallery",medium));booth.Bevel(navy,new Vector3(36,13.3f,0),new Vector3(3,1.3f,6.7f),.09f);booth.Box(glass,new Vector3(34.45f,14.6f,0),new Vector3(.06f,1.8f,6.1f));booth.Box(metal,new Vector3(34.4f,15.6f,0),new Vector3(.08f,.08f,6.4f));Label("MEDIA  /  COMMENTARY",booth.root,new Vector3(34.42f,13.3f,0),Quaternion.Euler(0,90,0),new Vector2(6.1f,.48f),3,white.color);booth.Save(Root+"/Meshes");
        }
        static void CameraProp(ChampionshipArenaBatch b,Vector3 p,Quaternion r)
        {
            Vector3 P(float x,float y,float z)=>p+r*new Vector3(x,y,z);
            for(int i=0;i<3;i++){float a=i*Mathf.PI*2/3;b.Tube(metal,P(0,1.1f,0),P(Mathf.Cos(a)*.46f,.02f,Mathf.Sin(a)*.46f),.027f,8);}
            b.Bevel(navy,P(0,1.24f,0),new Vector3(.43f,.33f,.58f),.05f,r);b.Tube(navy,P(0,1.25f,.27f),P(0,1.25f,.66f),.09f,10);b.Tube(screen,P(0,1.25f,.66f),P(0,1.25f,.68f),.075f,10);b.Box(rubber,P(.23f,1.36f,-.14f),new Vector3(.035f,.20f,.23f),r);b.Tube(metal,P(.17f,1.12f,-.18f),P(.17f,.95f,-.50f),.016f);
        }
    }
}
