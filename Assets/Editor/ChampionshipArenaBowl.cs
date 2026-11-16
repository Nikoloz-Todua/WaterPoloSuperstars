using System;
using System.Collections.Generic;
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
        static partial void BuildStadium()
        {
            var bowl=Child("F Three Tier Spectator Bowl",root);var structural=new ChampionshipArenaBatch(Child("Bowl Structure Concourses and Portal Frames",bowl));int sectionNumber=0;
            for(int tier=0;tier<3;tier++)
            {
                float offset=layout.tierOffset*tier;var tierRoot=Child("Tier "+(tier+1)+(tier==0?" Lower Bowl":tier==1?" Middle Bowl":" Upper Bowl"),bowl);
                foreach(int side in new[]{-1,1})foreach(float z in new[]{-26.25f,-15.75f,-5.25f,5.25f,15.75f,26.25f})
                {
                    // West central upper bays are an intentional integrated scoreboard opening.
                    if(side<0&&Mathf.Abs(z)<6&&tier>=1)continue;
                    var facing=Mathf.Abs(z)>20?new Vector3(-side*(23+offset),0,Mathf.Sign(z)*8-z).normalized:new Vector3(-side,0,0);Section(tierRoot,tier,12,new Vector3(side*(23+offset),layout.tierHeights[tier],z),facing,"L"+(++sectionNumber));
                }
                foreach(int end in new[]{-1,1})foreach(float x in new[]{-14.1f,-4.7f,4.7f,14.1f})
                {
                    // A true central tunnel opening beneath the south stand, rather than a door pasted onto a wall.
                    if(tier==0&&end<0&&Mathf.Abs(x)<6)continue;
                    Section(tierRoot,tier,11,new Vector3(x,layout.tierHeights[tier],end*(38+offset)),new Vector3(0,0,-end),"E"+(++sectionNumber));
                }
                foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
                {
                    Vector3 p=new Vector3(sx*22.2f,0,sz*34.6f);Vector3 outwards=p.normalized;p+=outwards*offset;p.y=layout.tierHeights[tier];Section(tierRoot,tier,9,p,-new Vector3(p.x,0,p.z).normalized,"C"+(++sectionNumber));
                }
            }
            // Columns and multi-level gallery voids give the bowl a structural depth, not stacked slabs.
            foreach(int side in new[]{-1,1})foreach(float z in new[]{-31.5f,-21f,-10.5f,0,10.5f,21f,31.5f})
            {
                structural.Bevel(navy,new Vector3(side*52.4f,13.5f,z),new Vector3(1.25f,27,1.0f),.12f);structural.Box(ice,new Vector3(side*51.74f,17,z),new Vector3(.025f,11,.075f));
                foreach(float y in new[]{7.3f,14.8f,23.1f}){structural.Box(concrete,new Vector3(side*47,y-.28f,z),new Vector3(12,.45f,8.7f));structural.Bevel(glass,new Vector3(side*52.2f,y+1.35f,z+3.3f),new Vector3(.3f,2.6f,2.0f),.05f);}
            }
            foreach(int end in new[]{-1,1})foreach(float x in new[]{-18.8f,-9.4f,0,9.4f,18.8f})
            {
                structural.Bevel(navy,new Vector3(x,13.5f,end*67.5f),new Vector3(.95f,27,1.2f),.10f);foreach(float y in new[]{7.3f,14.8f,23.1f})structural.Box(concrete,new Vector3(x,y-.28f,end*62),new Vector3(8.5f,.45f,11));
            }
            foreach(int side in new[]{-1,1})foreach(int end in new[]{-1,1})
            {
                // Broad side access flights sit in the bowl's outer aisles, away from all team/referee routes.
                for(int level=0;level<2;level++)for(int step=0;step<24;step++){float y=.7f+level*7.5f+(step+1)*.30f;structural.Box(concrete,new Vector3(side*(32+level*11+step*.32f),y-.15f,end*33.4f),new Vector3(.34f,.30f,1.7f));structural.Box(led,new Vector3(side*(32+level*11+step*.32f),y+.015f,end*33.4f),new Vector3(.025f,.018f,1.6f));}
            }
            structural.Save(Root+"/Meshes");
        }
        static void Section(Transform parent,int tier,int seats,Vector3 position,Vector3 inward,string id)
        {
            string key=tier+"_"+seats;var prefab=SectionPrefab(tier,seats,key);var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);instance.name="Tier "+(tier+1)+" Section "+id;instance.transform.SetPositionAndRotation(position,Quaternion.LookRotation(inward));
            var marker=instance.GetComponent<VenueSeatingSection>();for(int row=0;row<marker.rows;row++)for(int seat=0;seat<marker.seatsPerRow;seat++)slots.Add(new SeatSlot{floor=marker.SeatFloor(row,seat),rotation=instance.transform.rotation,tier=tier,key=slots.Count});
            if(tier==2)
            {
                // Coarse upper modules and full modules share the same footing and orientation.
                PrefabUtility.UnpackPrefabInstance(instance,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);marker.fullSeatVisual.SetParent(upperFull,true);marker.coarseSeatVisual.SetParent(upperLow,true);
            }
            else instance.transform.Find("Simplified Seat Shells").gameObject.SetActive(false);
        }
        static GameObject SectionPrefab(int tier,int seats,string key)
        {
            if(sectionPrefabs.TryGetValue(key,out var found))return found;var t=Child("Reusable Tier "+(tier+1)+" Seats "+seats,null);var marker=t.gameObject.AddComponent<VenueSeatingSection>();marker.tier=tier;marker.rows=layout.tierRows[tier];marker.seatsPerRow=seats;marker.rowRun=layout.rowRun;marker.rowRise=layout.rowRise;
            float half=(seats-1)*.64f*.5f+.55f;var structure=new ChampionshipArenaBatch(Child("Stepped Structure and Aisles "+key,t));var detail=new ChampionshipArenaBatch(Child("Detailed Seat Shells",t));var simple=new ChampionshipArenaBatch(Child("Simplified Seat Shells",t));marker.fullSeatVisual=detail.root;marker.coarseSeatVisual=simple.root;
            for(int row=0;row<marker.rows;row++)
            {
                float y=row*layout.rowRise,z=-row*layout.rowRun;structure.Box(concrete,new Vector3(0,y-.17f,z-.10f),new Vector3(half*2,.34f,layout.rowRun+.02f));
                foreach(int side in new[]{-1,1})
                {
                    for(int step=0;step<2;step++){float sy=y-step*layout.rowRise*.5f;structure.Box(concrete,new Vector3(side*(half+.36f),sy-.12f,z+step*layout.rowRun*.5f),new Vector3(.64f,.24f,layout.rowRun*.5f));structure.Box(led,new Vector3(side*(half+.36f),sy+.012f,z+step*layout.rowRun*.5f+.20f),new Vector3(.48f,.015f,.025f));}
                    if(row%2==0)structure.Tube(metal,new Vector3(side*(half+.75f),y,z),new Vector3(side*(half+.75f),y+1.05f,z),.022f);
                }
                for(int seat=0;seat<seats;seat++)
                {
                    var p=new Vector3((seat-(seats-1)*.5f)*.64f,y,z);var m=(seat+row)%13==0?white:(seat+row)%7==0?seatIce:(seat+row)%3==0?seatBlue:seatNavy;StadiumSeat(detail,p,m,false);StadiumSeat(simple,p,m,true);
                }
            }
            float depth=(marker.rows-1)*layout.rowRun,top=(marker.rows-1)*layout.rowRise;
            foreach(int side in new[]{-1,1})
            {
                structure.Tube(metal,new Vector3(side*(half+.75f),1.05f,.05f),new Vector3(side*(half+.75f),top+1.05f,-depth),.027f);
                // Sloping reinforced cheek and regular columns meet the slab, not floating stair treads.
                structure.Part(navy).Face(new Vector3(side*(half+.09f),-.34f,.5f),new Vector3(side*(half+.09f),top-.34f,-depth-.5f),new Vector3(side*(half+.09f),-.8f,-depth-.5f),new Vector3(side*(half+.09f),-.8f,.5f),Vector3.right*side);
                for(int row=0;row<marker.rows;row+=3){float y=row*layout.rowRise,bottom=.7f-layout.tierHeights[tier];structure.Box(navy,new Vector3(side*(half-.20f),(y+bottom)*.5f,-row*layout.rowRun),new Vector3(.22f,y-bottom,.30f));}
            }
            structure.Box(navy,new Vector3(0,top-.15f,-depth-.65f),new Vector3(half*2+1.4f,.55f,.5f));structure.Floor(rubber,new Vector3(0,top+.02f,-depth-1.65f),new Vector2(half*2+1.6f,2.1f));
            for(int s=-1;s<=1;s+=2){structure.Tube(metal,new Vector3(s*half,top+.05f,-depth-2.7f),new Vector3(s*half,top+1.1f,-depth-2.7f),.024f);}
            structure.Tube(metal,new Vector3(-half,top+1.1f,-depth-2.7f),new Vector3(half,top+1.1f,-depth-2.7f),.025f);
            structure.Save(Root+"/Meshes");detail.Save(Root+"/Meshes",false,"Detailed Seat Shells "+key);simple.Save(Root+"/Meshes",false,"Simplified Seat Shells "+key);var prefab=PrefabUtility.SaveAsPrefabAsset(t.gameObject,Root+"/Prefabs/SeatingSection_"+key+".prefab");Object.DestroyImmediate(t.gameObject);sectionPrefabs.Add(key,prefab);return prefab;
        }
        static void StadiumSeat(ChampionshipArenaBatch b,Vector3 p,Material material,bool simplified)
        {
            b.Box(metal,p+new Vector3(0,.22f,-.03f),new Vector3(.10f,.44f,.22f));
            if(simplified){b.Box(material,p+new Vector3(0,.46f,0),new Vector3(.50f,.065f,.46f));b.Box(material,p+new Vector3(0,.77f,-.24f),new Vector3(.50f,.52f,.055f),Quaternion.Euler(-8,0,0));return;}
            // Contoured continuous moulded shell; front is +Z, backrest is -Z, for every mirrored section.
            const int strips=5;for(int i=0;i<strips;i++)
            {
                float x0=-.255f+i*.51f/strips,x1=-.255f+(i+1)*.51f/strips;float q0=x0/.255f,q1=x1/.255f;float y0=.455f+.035f*q0*q0,y1=.455f+.035f*q1*q1;
                b.Part(material).Face(p+new Vector3(x0,y0,-.22f),p+new Vector3(x1,y1,-.22f),p+new Vector3(x1,y1,.24f),p+new Vector3(x0,y0,.24f),Vector3.up);
                b.Part(material).Face(p+new Vector3(x0,y0,.24f),p+new Vector3(x1,y1,.24f),p+new Vector3(x1,y1-.06f,.24f),p+new Vector3(x0,y0-.06f,.24f),Vector3.forward);
                float z0=-.23f+.04f*q0*q0,z1=-.23f+.04f*q1*q1;
                b.Part(material).Face(p+new Vector3(x0,.53f,z0),p+new Vector3(x1,.53f,z1),p+new Vector3(x1,1.02f,z1-.075f),p+new Vector3(x0,1.02f,z0-.075f),Vector3.forward);
                b.Part(material).Face(p+new Vector3(x1,.53f,z1-.045f),p+new Vector3(x0,.53f,z0-.045f),p+new Vector3(x0,1.02f,z0-.12f),p+new Vector3(x1,1.02f,z1-.12f),Vector3.back);
                b.Part(material).Face(p+new Vector3(x0,1.02f,z0-.075f),p+new Vector3(x1,1.02f,z1-.075f),p+new Vector3(x1,1.02f,z1-.12f),p+new Vector3(x0,1.02f,z0-.12f),Vector3.up);
            }
            b.Tube(metal,p+new Vector3(-.22f,.38f,-.17f),p+new Vector3(.22f,.38f,-.17f),.018f);b.Box(navy,p+new Vector3(0,.405f,0),new Vector3(.47f,.05f,.41f));
        }
        static partial void RemapOriginalAudience(ArenaQualityController env)
        {
            // Reuse the existing fans' actual meshes/materials, remapping their known old seating cells.
            var old=new List<Vector3>();for(int side=-1;side<=1;side+=2)for(int block=-2;block<=2;block++)for(int row=0;row<6;row++)for(int seat=0;seat<8;seat++){int key=(block+2)*48+row*8+seat;if(key%3==0)continue;old.Add(new Vector3(side*(18.95f+row*.92f),.18f+row*.47f,block*7.2f+(seat-3.5f)*.76f));}
            var targets=slots.Where(s=>s.tier==0).Where((s,i)=>i%6==0).Take(old.Count).ToArray();
            foreach(var audience in new[]{env.mediumAudience,env.highAudience})foreach(var f in audience.GetComponentsInChildren<MeshFilter>(true).ToArray())
            {
                var source=f.sharedMesh;if(source==null||AssetDatabase.GetAssetPath(source).StartsWith(Root))continue;var copy=Object.Instantiate(source);copy.name="Remapped Original "+audience.name+" "+f.name;var vertices=copy.vertices;
                for(int i=0;i<vertices.Length;i++){var v=f.transform.TransformPoint(vertices[i]);int closest=0;float best=float.MaxValue;for(int j=0;j<old.Count;j++){float d=(new Vector2(v.x-old[j].x,v.z-old[j].z)).sqrMagnitude;if(d<best){best=d;closest=j;}}var target=targets[closest%targets.Length];vertices[i]=target.floor+target.rotation*(v-old[closest]);}
                copy.vertices=vertices;copy.RecalculateNormals();copy.RecalculateBounds();string path=Root+"/Meshes/"+copy.name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved==null){AssetDatabase.CreateAsset(copy,path);saved=copy;}else{EditorUtility.CopySerialized(copy,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(copy);}var t=audience.transform.Find(saved.name);if(t==null){t=Child(saved.name,audience.transform);t.gameObject.AddComponent<MeshFilter>();t.gameObject.AddComponent<MeshRenderer>();}t.GetComponent<MeshFilter>().sharedMesh=saved;var r=t.GetComponent<MeshRenderer>();r.sharedMaterial=f.GetComponent<Renderer>().sharedMaterial;r.shadowCastingMode=ShadowCastingMode.Off;
            }
        }
        static partial void BuildAdditionalCrowd()
        {
            var low=new ChampionshipArenaBatch(Child("Lower Bowl Crowd Core",root));var more=new ChampionshipArenaBatch(Child("Middle and Upper Audience",medium));var detail=new ChampionshipArenaBatch(Child("High Audience Density",high));int lowerIndex=0;
            foreach(var s in slots)
            {
                if(s.tier==0){int index=lowerIndex++;if(index%6==0&&index/6<320)continue;}int key=s.key;var b=s.tier==0?low:more;if(key%5==0)continue;if(key%3==0)b=detail;
                var color=key%7==0?white:key%4==0?ice:key%3==0?seatBlue:navy;var position=s.floor+s.rotation*new Vector3(0,.82f,.025f);b.Part(color).Ellipsoid(position,new Vector3(.38f,.52f,.34f),s.rotation,6,3);b.Part(skin).Ellipsoid(s.floor+Vector3.up*1.18f,new Vector3(.23f,.26f,.23f),s.rotation,6,3);
            }
            low.Save(Root+"/Meshes");more.Save(Root+"/Meshes");detail.Save(Root+"/Meshes");
        }
    }
}
