using System;
using System.Linq;
using ToduStudios.WaterPolo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ToduStudios.WaterPolo.Editor
{
    public static class GameplayRealismSetup
    {
        [MenuItem("Tools/Water Polo/Set Up Gameplay Realism Expansion")]
        public static void SetUp()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || scene.path!=MovementPrototypeBuilder.ScenePath)
            {Debug.LogWarning("Open PrototypePool, stop Play Mode and save intended edits before realism setup. Nothing changed.");return;}
            var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();var match=UnityEngine.Object.FindAnyObjectByType<PrototypeMatch>();
            if(roster==null || roster.Members.Count!=14 || match.Rules==null)throw new InvalidOperationException("This expansion requires the existing fourteen-athlete M10 scene.");
            var ball=UnityEngine.Object.FindAnyObjectByType<BallController>();var pool=UnityEngine.Object.FindAnyObjectByType<PoolBounds>();
            var camera=UnityEngine.Object.FindAnyObjectByType<GameplayCamera>();var teams=UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>();
            Assign(camera,"match",match);Assign(camera,"roster",roster);Array(camera,"teams",teams.Cast<UnityEngine.Object>().ToArray());
            var root=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="GameplayRealism");
            if(root==null){root=new GameObject("GameplayRealism");SceneManager.MoveGameObjectToScene(root,scene);}
            var white=Material("RefereeWhite",new Color(0.94f,0.96f,0.98f));var dark=Material("RefereeDark",new Color(0.035f,0.05f,0.08f));
            var skin=Material("RefereeSkin",new Color(0.68f,0.46f,0.3f));var deck=Material("ObservationDeck",new Color(0.21f,0.3f,0.36f));
            for(int side=-1;side<=1;side+=2)
            {
                string suffix=side>0?"East":"West";
                var platform=root.transform.Find("RefereeDeck"+suffix);
                if(platform==null)
                {
                    platform=Primitive(root.transform,"RefereeDeck"+suffix,PrimitiveType.Cube,new Vector3(side*(pool.Width*0.5f+1.5f),pool.Waterline+0.08f,0f),new Vector3(2.4f,0.2f,pool.Length+1f),deck);
                }
                var referee=root.transform.Find("Referee"+suffix);
                if(referee==null)
                {
                    referee=Child(root.transform,"Referee"+suffix);referee.position=new Vector3(pool.transform.position.x+side*(pool.Width*0.5f+1.4f),pool.Waterline+0.18f,side*2f);
                    var visuals=Child(referee,"PlaceholderVisual");var visual=visuals.gameObject.AddComponent<RefereeVisual>();
                    var torso=Primitive(visuals,"Torso",PrimitiveType.Cube,new Vector3(0f,1.18f,0f),new Vector3(0.5f,0.62f,0.28f),white);
                    Primitive(visuals,"Shorts",PrimitiveType.Cube,new Vector3(0f,0.8f,0f),new Vector3(0.42f,0.24f,0.26f),dark);
                    Primitive(visuals,"Head",PrimitiveType.Sphere,new Vector3(0f,1.69f,0f),Vector3.one*0.31f,skin);
                    Primitive(visuals,"Whistle",PrimitiveType.Sphere,new Vector3(0.08f,1.55f,0.18f),Vector3.one*0.07f,dark);
                    var leftArm=Limb(visuals,"LeftArm",new Vector3(-0.32f,1.4f,0f),new Vector3(0.14f,0.5f,0.16f),white);
                    var rightArm=Limb(visuals,"RightArm",new Vector3(0.32f,1.4f,0f),new Vector3(0.14f,0.5f,0.16f),white);
                    var leftLeg=Limb(visuals,"LeftLeg",new Vector3(-0.12f,0.72f,0f),new Vector3(0.16f,0.64f,0.18f),dark);
                    var rightLeg=Limb(visuals,"RightLeg",new Vector3(0.12f,0.72f,0f),new Vector3(0.16f,0.64f,0.18f),dark);
                    Assign(visual,"torso",torso);Assign(visual,"leftArm",leftArm);Assign(visual,"rightArm",rightArm);Assign(visual,"leftLeg",leftLeg);Assign(visual,"rightLeg",rightLeg);
                    var observer=referee.gameObject.AddComponent<PoolsideReferee>();Assign(observer,"visual",visual);
                    var data=new SerializedObject(observer);data.FindProperty("deckSide").floatValue=side;data.ApplyModifiedPropertiesWithoutUndo();
                }
                var controller=referee.GetComponent<PoolsideReferee>();Assign(controller,"ball",ball);Assign(controller,"match",match);Assign(controller,"rules",match.Rules);Assign(controller,"pool",pool);
            }
            var effects=root.GetComponent<WaterInteractionEffects>();if(effects==null)effects=root.AddComponent<WaterInteractionEffects>();
            var waterEffects=Material("WaterFeedback",new Color(0.7f,0.94f,1f,0.65f));
            waterEffects.SetFloat("_Surface",1f);waterEffects.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);waterEffects.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            waterEffects.SetFloat("_ZWrite",0f);waterEffects.SetFloat("_Cull",0f);waterEffects.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");waterEffects.renderQueue=3000;EditorUtility.SetDirty(waterEffects);
            Assign(effects,"ball",ball);Assign(effects,"match",match);Assign(effects,"roster",roster);Assign(effects,"pool",pool);Assign(effects,"material",waterEffects);
            var trail=ball.GetComponent<TrailRenderer>();if(trail==null)
            {
                trail=ball.gameObject.AddComponent<TrailRenderer>();trail.time=0.14f;trail.minVertexDistance=0.12f;trail.startWidth=0.075f;trail.endWidth=0.01f;trail.sharedMaterial=waterEffects;
                trail.startColor=new Color(1f,0.9f,0.45f,0.55f);trail.endColor=new Color(1f,1f,1f,0f);trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            trail.emitting=false;Assign(effects,"ballTrail",trail);
            const string physicsPath="Assets/Materials/Prototype/RealismBall.asset";
            var physical=AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(physicsPath);
            if(physical==null){physical=new PhysicsMaterial("RealismBall") {bounciness=0.7f,dynamicFriction=0.04f,staticFriction=0.04f,bounceCombine=PhysicsMaterialCombine.Maximum};AssetDatabase.CreateAsset(physical,physicsPath);}
            ball.GetComponent<Collider>().sharedMaterial=physical;
            foreach(var keeper in UnityEngine.Object.FindObjectsByType<PrototypeGoalkeeper>())
            {Assign(keeper,"leftArmVisual",keeper.transform.Find("Visual/LeftArm"));Assign(keeper,"rightArmVisual",keeper.transform.Find("Visual/RightArm"));}
            foreach(var goal in UnityEngine.Object.FindObjectsByType<GoalMouth>())
            {
                var net=goal.GetComponent<GoalNetReaction>();if(net==null)net=goal.gameObject.AddComponent<GoalNetReaction>();
                Assign(net,"goal",goal);Assign(net,"ball",ball);Assign(net,"match",match);
                var strands=goal.transform.Find("PlaceholderModel/BackNetStrands");Array(net,"strands",strands.Cast<Transform>().Cast<UnityEngine.Object>().ToArray());
                foreach(var collider in goal.GetComponentsInChildren<Collider>())
                    if(collider.name.Contains("Post") || collider.name.Contains("Crossbar"))collider.sharedMaterial=physical;
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("Gameplay realism configured in place: shot profiles, broadcast framing, two deck referees, pooled water feedback and net response.");
        }
        private static Transform Limb(Transform parent,string name,Vector3 point,Vector3 size,Material material)
        {var joint=Child(parent,name);joint.localPosition=point;Primitive(joint,"Mesh",PrimitiveType.Cube,new Vector3(0f,-size.y*0.5f,0f),size,material);return joint;}
        private static Transform Primitive(Transform parent,string name,PrimitiveType type,Vector3 point,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=point;go.transform.localScale=size;
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;return go.transform;
        }
        private static Transform Child(Transform parent,string name){var go=new GameObject(name);go.transform.SetParent(parent,false);return go.transform;}
        private static Material Material(string name,Color color)
        {
            string path="Assets/Materials/Prototype/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Unlit")) {name=name};material.SetColor("_BaseColor",color);AssetDatabase.CreateAsset(material,path);}return material;
        }
        private static void Assign(UnityEngine.Object target,string field,UnityEngine.Object value)=>Milestone9PrototypeSetup.Assign(target,field,value);
        private static void Array(UnityEngine.Object target,string field,UnityEngine.Object[] values)
        {var data=new SerializedObject(target);var property=data.FindProperty(field);property.arraySize=values.Length;for(int i=0;i<values.Length;i++)property.GetArrayElementAtIndex(i).objectReferenceValue=values[i];data.ApplyModifiedPropertiesWithoutUndo();}
    }
}
