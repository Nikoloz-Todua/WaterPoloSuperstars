using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ToduStudios.WaterPolo.WaterVisuals;
using Object=UnityEngine.Object;

namespace ToduStudios.WaterPolo.Editor
{
    public static partial class ChampionshipArenaRebuild
    {
        /// <summary>Scoped first visual review: update only M20 meshes, add balcony structure and infill sections.</summary>
        public static void ApplyReviewFixes()
        {
            RequireSaved();var env=Object.FindAnyObjectByType<ArenaQualityController>();root=env.transform.Find(Name);if(root==null)throw new InvalidOperationException("Installed M20 venue required.");layout=AssetDatabase.LoadAssetAtPath<ChampionshipVenueLayout>(Root+"/Settings/ChampionshipVenue.asset");Materials();medium=root.Find("Medium Venue Detail");high=root.Find("High Venue Detail");upperLow=root.Find("Low Upper Bowl Representation");upperFull=root.Find("Full Upper Bowl Seating");
            var permanent=root;var temp=Child("M20 Scoped Mesh Review",null);
            try{root=temp;BuildBasin(env);}finally{root=permanent;Object.DestroyImmediate(temp.gameObject);}
            var bowl=root.Find("F Three Tier Spectator Bowl");
            foreach(var s in bowl.GetComponentsInChildren<VenueSeatingSection>(true))
            {
                var p=s.transform.position;if(s.seatsPerRow!=12||Mathf.Abs(p.z)<20)continue;var inward=new Vector3(-p.x,0,Mathf.Sign(p.z)*8-p.z).normalized;var rotation=Quaternion.LookRotation(inward);s.transform.rotation=rotation;s.fullSeatVisual.SetPositionAndRotation(p,rotation);s.coarseSeatVisual.SetPositionAndRotation(p,rotation);
            }
            if(bowl.Find("Balcony Connections and Corner Infill")==null)
            {
                var connections=Child("Balcony Connections and Corner Infill",bowl);var b=new ChampionshipArenaBatch(connections);
                for(int level=0;level<2;level++)
                {
                    float floor=level==0?6.25f:14.2f;var inner=RibbonPolygon(level==0?32.2f:43.2f,level==0?46.8f:57.8f);var outer=RibbonPolygon(level==0?34.6f:45.6f,level==0?49.5f:60.5f);
                    for(int i=0;i<inner.Length;i++)
                    {
                        int next=(i+1)%inner.Length;Vector3 a=inner[i]+Vector3.up*floor,c=inner[next]+Vector3.up*floor,d=outer[next]+Vector3.up*floor,e=outer[i]+Vector3.up*floor;
                        b.Part(concrete).Face(a,c,d,e,Vector3.up);var normal=Vector3.Cross(Vector3.up,c-a).normalized;if(Vector3.Dot(normal,-(a+c)*.5f)<0)normal=-normal;
                        b.Part(navy).Face(a,c,c-Vector3.up*1.30f,a-Vector3.up*1.30f,normal);b.Tube(metal,a+Vector3.up*.04f,c+Vector3.up*.04f,.035f);
                        float length=Vector3.Distance(a,c);for(int post=0;post<=Mathf.FloorToInt(length/2.3f);post++){var p=Vector3.Lerp(a,c,post/(float)Mathf.Max(1,Mathf.FloorToInt(length/2.3f)));b.Tube(metal,p,p+Vector3.up*1.1f,.024f);}
                        b.Tube(metal,a+Vector3.up*1.1f,c+Vector3.up*1.1f,.026f);
                    }
                }
                b.Save(Root+"/Meshes");sectionPrefabs.Clear();slots.Clear();
                foreach(var s in root.GetComponentsInChildren<VenueSeatingSection>(true))for(int r=0;r<s.rows;r++)for(int c=0;c<s.seatsPerRow;c++)slots.Add(new SeatSlot{floor=s.SeatFloor(r,c),rotation=s.transform.rotation,tier=s.tier,key=slots.Count});
                int number=0;for(int tier=1;tier<3;tier++)foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
                {
                    float offset=layout.tierOffset*tier;var corner=new Vector3(sx*22.2f,0,sz*34.6f);corner+=corner.normalized*offset;var side=new Vector3(sx*(23+offset),0,sz*26.25f);var end=new Vector3(sx*14.1f,0,sz*(38+offset));
                    foreach(float t in new[]{.34f,.67f}){var p=Vector3.Lerp(side,corner,t);p.y=layout.tierHeights[tier];Section(connections,tier,6,p,-new Vector3(p.x,0,p.z).normalized,"Infill"+(++number));}
                    var q=Vector3.Lerp(corner,end,.5f);q.y=layout.tierHeights[tier];Section(connections,tier,6,q,-new Vector3(q.x,0,q.z).normalized,"Infill"+(++number));
                }
            }
            slots.Clear();foreach(var s in root.GetComponentsInChildren<VenueSeatingSection>(true))for(int row=0;row<s.rows;row++)for(int seat=0;seat<s.seatsPerRow;seat++)slots.Add(new SeatSlot{floor=s.SeatFloor(row,seat),rotation=s.transform.rotation,tier=s.tier,key=slots.Count});
            RemapOriginalAudience(env);temp=Child("M20 Crowd Review",null);var savedMedium=medium;var savedHigh=high;try{root=temp;medium=Child("Temporary Medium",temp);high=Child("Temporary High",temp);BuildAdditionalCrowd();}finally{root=permanent;medium=savedMedium;high=savedHigh;Object.DestroyImmediate(temp.gameObject);}
            upperLow.gameObject.SetActive(false);upperFull.gameObject.SetActive(true);var dev=Object.FindAnyObjectByType<ArenaDeveloperCamera>();var p0=new Vector3(43,29,-53);dev.viewpoints[0].SetPositionAndRotation(p0,Quaternion.LookRotation(new Vector3(0,5,0)-p0));
            foreach(var facility in root.GetComponentsInChildren<VenueTeamFacility>())if(facility.seats.Length==0)
            {
                var chairs=new System.Collections.Generic.List<Transform>();for(int i=0;i<9;i++){var t=Anchor("Reserve Seat "+(i+1),facility.transform,new Vector3(18.8f,.93f,facility.defendedEnd*(14.9f+i*.78f)));t.rotation=Quaternion.LookRotation(Vector3.left);chairs.Add(t);}for(int i=0;i<2;i++){var t=Anchor("Coach Seat "+(i+1),facility.transform,new Vector3(20,.93f,facility.defendedEnd*(15.7f+i*.85f)));t.rotation=Quaternion.LookRotation(Vector3.left);chairs.Add(t);}facility.seats=chairs.ToArray();
            }
            var intro=root.GetComponentInChildren<VenueIntroductionAnchors>();intro.blueEntranceRoute[4].position=new Vector3(15.3f,.7f,-28.8f);intro.blueEntranceRoute[5].position=new Vector3(15.3f,.7f,-16);intro.whiteEntranceRoute[4].position=new Vector3(15.3f,.7f,-28.8f);intro.whiteEntranceRoute[5].position=new Vector3(15.3f,.7f,17);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
            Debug.Log("M20 visual review saved: bulkhead entry notches, connected balcony slabs/rails and populated corner infill; original match field unchanged.");
        }
    }
}
