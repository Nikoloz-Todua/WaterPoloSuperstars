using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using ToduStudios.WaterPolo.WaterVisuals;
using Object=UnityEngine.Object;

namespace ToduStudios.WaterPolo.Editor
{
    /// <summary>Adds authored equipment to the existing field, retaining all goal detectors/colliders and arena assets.</summary>
    public static class AuthenticFieldSetup
    {
        public const string Root="Assets/Arena/AuthenticField",Name="Competition Field Equipment",Recovery="Assets/Scenes/Recovery/OlympicMatchArena_PreAuthenticField.unity";
        static PoolBounds pool;
        static Material white,red,yellow,green,navy,metal,net,cyan;
        [MenuItem("Tools/Water Polo/Arena/Set Up Authentic Field and Period Sprint-Offs")]
        public static void SetUp()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||scene.isDirty||scene.path!=OlympicArenaSetup.ScenePath){Debug.LogWarning("Open saved OlympicMatchArena outside Play Mode. Nothing changed.");return;}
            if(GameObject.Find(Name)!=null){Debug.Log("Authentic field already configured; authored tuning preserved.");return;}
            var match=Object.FindAnyObjectByType<PrototypeMatch>();var roster=Object.FindAnyObjectByType<PrototypeRoster>();pool=Object.FindAnyObjectByType<PoolBounds>();var goals=Object.FindObjectsByType<GoalMouth>();
            if(match==null||roster.Members.Count!=14||goals.Length!=2||Mathf.Abs(goals[0].transform.position.z-goals[1].transform.position.z)-pool.Length>.001f)throw new InvalidOperationException("Existing 14-athlete, two-goal configured field required.");
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Recovery)==null&&!AssetDatabase.CopyAsset(scene.path,Recovery))throw new InvalidOperationException("Recovery copy failed; nothing changed.");
            Folder(Root+"/Meshes");Folder(Root+"/Materials");Folder(Root+"/Prefabs");
            white=Material("Competition White",new Color(.93f,.96f,.98f),.25f,.48f);red=Material("Competition Red",new Color(.78f,.045f,.065f),.08f,.4f);
            yellow=Material("Competition Yellow",new Color(1f,.72f,.045f),.08f,.4f);green=Material("Competition Green",new Color(.035f,.5f,.24f),.08f,.4f);
            navy=Material("Navy Non Slip",new Color(.022f,.052f,.085f),.15f,.24f);metal=Material("Brushed Alloy",new Color(.35f,.44f,.51f),.72f,.48f);
            net=Material("Woven Goal Cord",new Color(.63f,.77f,.8f),.05f,.32f);cyan=Material("Sprint Cyan",new Color(.1f,.7f,.85f),.18f,.48f);
            var env=Object.FindAnyObjectByType<ArenaQualityController>().transform;var root=Child(Name,env);root.position=new Vector3(pool.transform.position.x,pool.Waterline,pool.transform.position.z);
            var old=env.Find("Water Polo Field Markers — no race lanes");
            if(old==null)throw new InvalidOperationException("Expected existing visual marker group.");
            if(old.GetComponentsInChildren<Collider>(true).Length!=0)throw new InvalidOperationException("Unexpected gameplay collider in legacy marker group.");
            old.gameObject.SetActive(false);BuildRopes(root,goals);BuildPlatforms(root,goals);BuildGoals(goals);
            var sprint=Get<PeriodSprintOff>(match.gameObject);Assign(sprint,"match",match);Assign(sprint,"roster",roster);Assign(sprint,"selection",Object.FindAnyObjectByType<PlayerSelection>());Assign(sprint,"ball",Object.FindAnyObjectByType<BallController>());Assign(sprint,"pool",pool);Array(sprint,"teams",Object.FindObjectsByType<PrototypeTeamTactics>().Cast<Object>().ToArray());Assign(match,"periodSprint",sprint);
            BuildApparatus(root,match);
            // Scene objects remain ordinary native assets; the equipment prefab is a reusable visual source.
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,Root+"/Prefabs/CompetitionFieldEquipment.prefab");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("Authentic field, goal visuals, elevated official lanes and four-period sprint integration saved. Original goal colliders and arena retained.");
        }
        static void BuildRopes(Transform root,GoalMouth[] goals)
        {
            var group=Child("Measured Competition Ropes",root);var batches=new Dictionary<Material,MeshData>();foreach(var m in new[]{red,yellow,green,white,metal})batches[m]=new MeshData();
            float north=goals.Max(g=>g.transform.position.z)-root.position.z,south=goals.Min(g=>g.transform.position.z)-root.position.z,halfWidth=pool.Width*.5f;
            foreach(int side in new[]{-1,1})
            {
                int count=Mathf.CeilToInt((north-south)/.22f);
                for(int i=0;i<count;i++)
                {
                    float z=Mathf.Lerp(south,north,(i+.5f)/count),fromGoal=Mathf.Min(z-south,north-z);var m=fromGoal<2?red:fromGoal<6?yellow:green;if(Mathf.Abs(z)<.16f)m=white;
                    batches[m].Buoy(new Vector3(side*halfWidth,0,z),Vector3.forward,.19f,.06f);
                }
                batches[metal].Bar(new Vector3(side*halfWidth,0,south-.3f),new Vector3(side*halfWidth,0,north+.3f),.012f,8);
                foreach(float z in new[]{south-.3f,north+.3f})
                {
                    batches[metal].Bar(new Vector3(side*halfWidth,0,z),new Vector3(side*(halfWidth+1.15f),.18f,z),.016f,8);
                    batches[metal].Box(new Vector3(side*(halfWidth+1.15f),.18f,z),new Vector3(.14f,.025f,.22f));
                }
            }
            foreach(var goal in goals)
            {
                float side=Mathf.Sign(goal.transform.position.z-root.position.z),z=goal.transform.position.z-root.position.z+side*.3f;
                for(int i=0;i<92;i++)
                {
                    float x=Mathf.Lerp(-halfWidth,halfWidth,(i+.5f)/92);if(Mathf.Abs(x)<goal.Width*.5f+.12f)continue;
                    bool goalArea=Mathf.Abs(x)<goal.Width*.5f+goal.PostThickness+2f;bool entry=x>halfWidth-2f;
                    batches[goalArea||entry?red:white].Buoy(new Vector3(x,0,z),Vector3.right,.19f,.06f);
                }
                // Same east-side corner and existing waiting area; no excluded-player coordinates change.
                float end=z+side*.8f;
                for(int i=0;i<9;i++)batches[red].Buoy(new Vector3(halfWidth-2+(i+.5f)*2/9,0,end),Vector3.right,.19f,.06f);
                for(int i=0;i<5;i++)batches[red].Buoy(new Vector3(halfWidth-2,0,Mathf.Lerp(z,end,(i+.5f)/5)),Vector3.forward,.13f,.06f);
                foreach(float distance in new[]{0f,2f,5f,6f})foreach(int edge in new[]{-1,1})
                {
                    float markZ=goal.transform.position.z-root.position.z-side*distance;var m=distance==0?white:distance==6?yellow:red;
                    batches[m].Box(new Vector3(edge*(halfWidth+.25f),.018f,markZ),new Vector3(.42f,.045f,.085f));
                    var marker=Child((side>0?"North ":"South ")+distance+"m Reference "+edge,group);marker.localPosition=new Vector3(edge*halfWidth,0,markZ);
                }
            }
            foreach(int edge in new[]{-1,1})batches[white].Box(new Vector3(edge*(halfWidth+.25f),.018f,0),new Vector3(.42f,.045f,.085f));
            foreach(var batch in batches)Geometry(group,"Ropes "+batch.Key.name,batch.Value,batch.Key);
        }
        static void BuildPlatforms(Transform root,GoalMouth[] goals)
        {
            var platforms=Child("Elevated Referee Platforms",root);float half=pool.Length*.5f;
            foreach(int side in new[]{-1,1})
            {
                var lane=Child(side<0?"West Platform":"East Platform",platforms);float x=side*(pool.Width*.5f+2.65f);var deck=new MeshData();var trim=new MeshData();
                deck.Box(new Vector3(x,.42f,0),new Vector3(1f,.56f,pool.Length+1f));
                trim.Box(new Vector3(x-side*.48f,.705f,0),new Vector3(.035f,.025f,pool.Length+1f));
                trim.Box(new Vector3(x+side*.51f,.50f,0),new Vector3(.035f,.06f,pool.Length+1f));
                for(int end=-1;end<=1;end+=2)
                {
                    // Four shallow access steps sit beyond the officiating travel, outside the field.
                    for(int step=0;step<4;step++)deck.Box(new Vector3(x,.18f+(4-step)*.13f*.5f,end*(half+.64f+step*.3f)),new Vector3(1f,(4-step)*.13f,.31f));
                }
                for(float z=-half;z<=half+.01f;z+=2.5f)trim.Bar(new Vector3(x+side*.53f,.7f,z),new Vector3(x+side*.53f,1.45f,z),.023f,8);
                trim.Bar(new Vector3(x+side*.53f,1.45f,-half),new Vector3(x+side*.53f,1.45f,half),.025f,8);
                Geometry(lane,lane.name+" Non Slip Walking Deck",deck,navy);Geometry(lane,lane.name+" Edge Trim and Outer Safety Rail",trim,metal);
                var colors=new Dictionary<Material,MeshData>{{red,new MeshData()},{yellow,new MeshData()},{green,new MeshData()},{white,new MeshData()}};
                for(int end=-1;end<=1;end+=2)
                {
                    foreach(var section in new[]{new Vector3(0,2,0),new Vector3(2,6,1),new Vector3(6,half,2)})
                    {
                        var m=section.z==0?red:section.z==1?yellow:green;
                        colors[m].Box(new Vector3(x-side*.33f,.705f,end*(half-(section.x+section.y)*.5f)),new Vector3(.25f,.016f,section.y-section.x));
                    }
                    foreach(float d in new[]{0f,2f,5f,6f})
                    {
                        float z=end*(half-d);var m=d==0?white:d==6?yellow:red;
                        colors[m].Box(new Vector3(x,.716f,z),new Vector3(.96f,.018f,.09f));
                    }
                }
                colors[white].Box(new Vector3(x,.716f,0),new Vector3(.96f,.018f,.10f));
                foreach(var color in colors)Geometry(lane,lane.name+" Markers "+color.Key.name,color.Value,color.Key);
            }
            foreach(var referee in Object.FindObjectsByType<PoolsideReferee>())
            {
                var so=new SerializedObject(referee);float side=so.FindProperty("deckSide").floatValue;
                float feet=referee.GetComponentsInChildren<Renderer>().Min(r=>r.bounds.min.y)-referee.transform.position.y;
                so.FindProperty("deckHeight").floatValue=.70f-feet;so.FindProperty("deckOffset").floatValue=2.65f;so.FindProperty("travelFraction").floatValue=.49f;so.ApplyModifiedPropertiesWithoutUndo();
                referee.transform.position=new Vector3(pool.transform.position.x+Mathf.Sign(side)*(pool.Width*.5f+2.65f),pool.Waterline+.70f-feet,referee.transform.position.z);
            }
        }
        static void BuildGoals(GoalMouth[] goals)
        {
            foreach(var goal in goals)
            {
                // Rendering only: original posts, crossbar, back/side colliders and GoalMouth stay active.
                foreach(var renderer in goal.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
                var root=Child("Competition Goal Visual",goal.transform);var frame=new MeshData();var support=new MeshData();var cords=new MeshData();float w=goal.Width,h=goal.Height,d=goal.Depth,p=goal.PostThickness;
                foreach(int side in new[]{-1,1})
                {
                    frame.SquareBar(new Vector3(side*(w+p)*.5f,-p*.5f,p*.5f),new Vector3(side*(w+p)*.5f,h+p*.5f,p*.5f),p,.005f);
                    support.Bar(new Vector3(side*(w*.5f+p+.045f),h-.08f,.11f),new Vector3(side*(w*.5f+p+.045f),.11f,d+.06f),.025f,8);
                    support.Box(new Vector3(side*(w*.5f+p+.13f),-.035f,d*.52f),new Vector3(.18f,.13f,d+.22f));
                    for(float y=-.12f;y<=h;y+=.12f)cords.Bar(new Vector3(side*w*.5f,y,.08f),new Vector3(side*w*.5f,y,d),.006f,6);
                    for(float z=.08f;z<=d+.02f;z+=.12f)cords.Bar(new Vector3(side*w*.5f,-.12f,z),new Vector3(side*w*.5f,h-.04f*Mathf.Sin(z/d*Mathf.PI),z),.006f,6);
                }
                frame.SquareBar(new Vector3(-w*.5f-p,h+p*.5f,p*.5f),new Vector3(w*.5f+p,h+p*.5f,p*.5f),p,.005f);
                for(float x=-w*.5f;x<=w*.5f+.02f;x+=.12f)
                {
                    // Back cord has a small natural bow; no simulated rope chain.
                    Vector3 previous=new Vector3(x,-.12f,d);
                    for(int j=1;j<=8;j++){float y=Mathf.Lerp(-.12f,h,j/8f),sag=.045f*Mathf.Sin(j/8f*Mathf.PI)*Mathf.Sin((x+w*.5f)/w*Mathf.PI);Vector3 next=new Vector3(x,y,d+sag);cords.Bar(previous,next,.006f,6);previous=next;}
                    cords.Bar(new Vector3(x,h,.08f),new Vector3(x,h,d),.006f,6);
                    cords.Bar(new Vector3(x,-.12f,.08f),new Vector3(x,-.12f,d),.006f,6);
                }
                for(float y=-.12f;y<=h+.02f;y+=.12f)cords.Bar(new Vector3(-w*.5f,y,d),new Vector3(w*.5f,y,d),.006f,6);
                for(float z=.12f;z<=d;z+=.12f)cords.Bar(new Vector3(-w*.5f,h,z),new Vector3(w*.5f,h,z),.006f,6);
                Geometry(root,"Rigid White Frame",frame,white);Geometry(root,"Float Supports and Stays",support,metal);var woven=Geometry(root,"Enclosing Woven Net",cords,net);
                var reaction=goal.GetComponent<GoalNetReaction>();if(reaction!=null){Array(reaction,"strands",new Object[]{woven});var so=new SerializedObject(reaction);so.FindProperty("maximumDeformation").floatValue=.08f;so.ApplyModifiedPropertiesWithoutUndo();}
            }
        }
        static void BuildApparatus(Transform root,PrototypeMatch match)
        {
            var parent=Child("Retracting Period Ball Release",root);var cradle=Child("Cradle",parent);Vector3 center=match.PeriodCenter-root.position;
            var data=new MeshData();
            for(int i=0;i<28;i++){float a=i*Mathf.PI*2/28,b=(i+1)*Mathf.PI*2/28;data.Bar(center+new Vector3(Mathf.Cos(a)*.10f,.32f,Mathf.Sin(a)*.10f),center+new Vector3(Mathf.Cos(b)*.10f,.32f,Mathf.Sin(b)*.10f),.012f,6);}
            data.Bar(center+Vector3.up*(-.65f),center+Vector3.up*.30f,.018f,8);data.Box(center+Vector3.up*(-.70f),new Vector3(.25f,.10f,.25f));Geometry(cradle,"Open Cradle and Retracting Stem",data,metal);
            Transform Cue(string name,Material color){var m=new MeshData();for(int i=0;i<32;i++){float a=i*Mathf.PI*2/32,b=(i+1)*Mathf.PI*2/32;m.Bar(new Vector3(Mathf.Cos(a)*.58f,0,Mathf.Sin(a)*.58f),new Vector3(Mathf.Cos(b)*.58f,0,Mathf.Sin(b)*.58f),.018f,6);}return Geometry(parent,name,m,color);}
            var view=parent.gameObject.AddComponent<SprintBallReleaseVisual>();Assign(view,"match",match);Assign(view,"cradle",cradle);Assign(view,"blueCue",Cue("Blue Designated Sprinter",cyan));Assign(view,"whiteCue",Cue("White Designated Sprinter",yellow));cradle.gameObject.SetActive(false);
        }
        [MenuItem("Tools/Water Polo/Verification/Audit Current Production Field")]
        public static void Validate(){var p=Object.FindAnyObjectByType<PoolBounds>();var r=Object.FindAnyObjectByType<PrototypeRoster>();Debug.Log($"Field {p.Width} x {p.Length} m; water {p.Waterline}; {r.Members.Count} athletes; {Object.FindObjectsByType<GoalMouth>().Length} original goal detectors; equipment {GameObject.Find(Name)!=null}.");}
        [MenuItem("Tools/Water Polo/Recovery/Open Pre-M17 Arena Scene")]
        public static void OpenRecovery()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||SceneManager.GetActiveScene().isDirty){Debug.LogWarning("Stop Play Mode and save intended edits before opening recovery.");return;}
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(Recovery)!=null)EditorSceneManager.OpenScene(Recovery);
        }
        static Material Material(string name,Color color,float metallic,float smooth)
        {
            string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m!=null)return m;
            m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color};m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);AssetDatabase.CreateAsset(m,path);return m;
        }
        static Transform Geometry(Transform parent,string name,MeshData data,Material material)
        {
            var mesh=data.Mesh(name);string path=Root+"/Meshes/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing==null)AssetDatabase.CreateAsset(mesh,path);else{Object.DestroyImmediate(mesh);mesh=existing;}
            var t=Child(name,parent);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=t.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;return t;
        }
        static Transform Child(string name,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
        static T Get<T>(GameObject go) where T:Component{var c=go.GetComponent<T>();return c!=null?c:go.AddComponent<T>();}
        static void Assign(Object o,string n,Object value)=>Milestone9PrototypeSetup.Assign(o,n,value);
        static void Array(Object o,string n,Object[] values){var s=new SerializedObject(o);var p=s.FindProperty(n);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];s.ApplyModifiedPropertiesWithoutUndo();}
        static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;int slash=path.LastIndexOf('/');Folder(path.Substring(0,slash));AssetDatabase.CreateFolder(path.Substring(0,slash),path.Substring(slash+1));}
        internal sealed class MeshData
        {
            readonly List<Vector3> v=new List<Vector3>();readonly List<int> tri=new List<int>();
            public void Add(MeshData data,Matrix4x4 transform)
            {int offset=v.Count;foreach(var p in data.v)v.Add(transform.MultiplyPoint3x4(p));foreach(int i in data.tri)tri.Add(i+offset);}
            void Face(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal)
            {
                if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)<0){var x=b;b=d;d=x;}
                int n=v.Count;v.Add(a);v.Add(b);v.Add(c);v.Add(d);tri.Add(n);tri.Add(n+1);tri.Add(n+2);tri.Add(n);tri.Add(n+2);tri.Add(n+3);
            }
            public void Box(Vector3 center,Vector3 size)
            {
                Vector3 h=size*.5f;
                for(int axis=0;axis<3;axis++)foreach(int sign in new[]{-1,1}){Vector3 a=Vector3.zero,u=Vector3.zero,w=Vector3.zero;a[axis]=h[axis]*sign;u[(axis+1)%3]=h[(axis+1)%3];w[(axis+2)%3]=h[(axis+2)%3];Vector3 normal=Vector3.zero;normal[axis]=sign;Face(center+a-u-w,center+a+u-w,center+a+u+w,center+a-u+w,normal);}
            }
            public void Bar(Vector3 a,Vector3 b,float radius,int sides)
            {
                Vector3 direction=(b-a).normalized,u=Vector3.Cross(direction,Vector3.up).normalized;if(u.sqrMagnitude<.01f)u=Vector3.right;Vector3 w=Vector3.Cross(direction,u).normalized;
                for(int i=0;i<sides;i++){float x=i*Mathf.PI*2/sides,y=(i+1)*Mathf.PI*2/sides;Vector3 p=(u*Mathf.Cos(x)+w*Mathf.Sin(x))*radius,q=(u*Mathf.Cos(y)+w*Mathf.Sin(y))*radius;Face(a+p,b+p,b+q,a+q,(p+q).normalized);}
            }
            public void SquareBar(Vector3 a,Vector3 b,float thickness,float bevel)
            {
                Vector3 direction=(b-a).normalized,u=Vector3.Cross(direction,Vector3.up).normalized;if(u.sqrMagnitude<.01f)u=Vector3.right;Vector3 w=Vector3.Cross(direction,u).normalized;float h=thickness*.5f;
                var ring=new[]{new Vector2(-h+bevel,-h),new Vector2(h-bevel,-h),new Vector2(h,-h+bevel),new Vector2(h,h-bevel),new Vector2(h-bevel,h),new Vector2(-h+bevel,h),new Vector2(-h,h-bevel),new Vector2(-h,-h+bevel)};
                for(int i=0;i<8;i++){var p=u*ring[i].x+w*ring[i].y;var q=u*ring[(i+1)%8].x+w*ring[(i+1)%8].y;Face(a+p,b+p,b+q,a+q,(p+q).normalized);}
                foreach(var end in new[]{a,b})for(int i=0;i<8;i++)
                {
                    Vector3 p=end+u*ring[i].x+w*ring[i].y,q=end+u*ring[(i+1)%8].x+w*ring[(i+1)%8].y;
                    Vector3 normal=end==a?-direction:direction;if(Vector3.Dot(Vector3.Cross(p-end,q-end),normal)<0){var x=p;p=q;q=x;}
                    int n=v.Count;v.Add(end);v.Add(p);v.Add(q);tri.Add(n);tri.Add(n+1);tri.Add(n+2);
                }
            }
            public void Buoy(Vector3 center,Vector3 axis,float length,float radius)
            {
                Vector3 u=Vector3.up,w=Vector3.Cross(axis,u).normalized;float[] profile={.58f,1f,.74f,1f,.58f};
                for(int ring=0;ring<4;ring++)for(int i=0;i<10;i++)
                {float a=i*Mathf.PI*2/10,b=(i+1)*Mathf.PI*2/10;Vector3 p=u*Mathf.Cos(a)+w*Mathf.Sin(a),q=u*Mathf.Cos(b)+w*Mathf.Sin(b);Vector3 c=center+axis*((ring/4f-.5f)*length),d=center+axis*(((ring+1)/4f-.5f)*length);Face(c+p*radius*profile[ring],d+p*radius*profile[ring+1],d+q*radius*profile[ring+1],c+q*radius*profile[ring],(p+q).normalized);}
            }
            public Mesh Mesh(string name){var m=new Mesh{name=name,indexFormat=v.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};m.SetVertices(v);m.SetTriangles(tri,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
        }
    }
}
