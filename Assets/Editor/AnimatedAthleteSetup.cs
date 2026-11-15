using System;
using System.IO;
using System.Linq;
using ToduStudios.WaterPolo.WaterVisuals;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace ToduStudios.WaterPolo.Editor
{
    /// <summary>Opt-in visual replacement. Existing controllers, anchors, colliders and original visuals survive.</summary>
    [InitializeOnLoad]
    public static class AnimatedAthleteSetup
    {
        public const string Root="Assets/SwimmerAnimationPrototype";
        public const string Prefab=Root+"/Prefabs/AnimatedWaterPoloAthlete.prefab";
        public const string Lab="Assets/Scenes/AnimatedSwimmerLab.unity";
        public const string Recovery="Assets/Scenes/Recovery/OlympicMatchArena_PreAnimatedAthlete.unity";
        const string Restore="AnimatedAthlete.Restore",Entry="AnimatedAthlete.Entry";
        static AnimatedAthleteSetup()
        {
            EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Restore,false)){EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Entry,""));SessionState.SetBool(Restore,false);}};
        }
        static void RequireSaved()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save intended scene changes first; setup discards nothing.");
        }
        static void Folder(string p){if(AssetDatabase.IsValidFolder(p))return;Folder(Path.GetDirectoryName(p).Replace('\\','/'));AssetDatabase.CreateFolder(Path.GetDirectoryName(p).Replace('\\','/'),Path.GetFileName(p));}
        static Material Mat(string name,Color color)
        {
            string p=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(m!=null)return m;
            m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color,enableInstancing=true};m.SetFloat("_Smoothness",.32f);AssetDatabase.CreateAsset(m,p);return m;
        }
        static AnimationClip Clip(string name)=>AssetDatabase.LoadAllAssetsAtPath(Root+"/Animations/"+name+".fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
        static AnimatorController Controller()
        {
            string p=Root+"/AnimatedSwimmer.controller";var existing=AssetDatabase.LoadAssetAtPath<AnimatorController>(p);if(existing!=null)return existing;
            var c=AnimatorController.CreateAnimatorControllerAtPath(p);
            c.AddParameter("Move",AnimatorControllerParameterType.Float);c.AddParameter("Hold",AnimatorControllerParameterType.Bool);c.AddParameter("Block",AnimatorControllerParameterType.Bool);
            foreach(string t in new[]{"Pass","Shoot","Save"})c.AddParameter(t,AnimatorControllerParameterType.Trigger);
            var tree=new BlendTree{name="In-place treading to head-up crawl",blendType=BlendTreeType.Simple1D,blendParameter="Move",useAutomaticThresholds=false};
            tree.AddChild(Clip("TreadWater"),0);tree.AddChild(Clip("HeadUpSwim"),1);tree.AddChild(Clip("Sprint"),2);AssetDatabase.AddObjectToAsset(tree,c);
            var locomotion=c.layers[0].stateMachine.AddState("Locomotion");locomotion.motion=tree;c.layers[0].stateMachine.defaultState=locomotion;
            var mask=new AvatarMask{name="Upper body actions; legs keep swimming"};
            for(int i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++)mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,false);
            foreach(var b in new[]{AvatarMaskBodyPart.Body,AvatarMaskBodyPart.Head,AvatarMaskBodyPart.LeftArm,AvatarMaskBodyPart.RightArm,AvatarMaskBodyPart.LeftFingers,AvatarMaskBodyPart.RightFingers,AvatarMaskBodyPart.LeftHandIK,AvatarMaskBodyPart.RightHandIK})mask.SetHumanoidBodyPartActive(b,true);
            AssetDatabase.CreateAsset(mask,Root+"/UpperBody.mask");
            c.AddLayer("Ball and goalkeeper presentation");var layers=c.layers;layers[1].defaultWeight=1;layers[1].avatarMask=mask;layers[1].iKPass=true;c.layers=layers;
            var sm=c.layers[1].stateMachine;var empty=sm.AddState("Free arms");sm.defaultState=empty;empty.writeDefaultValues=false;
            var hold=sm.AddState("Possession");hold.motion=Clip("Possession");hold.writeDefaultValues=false;
            var block=sm.AddState("Block");block.motion=Clip("Block");block.writeDefaultValues=false;
            Link(empty,hold,"Hold",true);Link(hold,empty,"Hold",false);Link(empty,block,"Block",true);Link(block,empty,"Block",false);
            foreach(string name in new[]{"Pass","Shoot","Save"})
            {
                var state=sm.AddState(name);state.motion=Clip(name);state.writeDefaultValues=false;
                var enter=sm.AddAnyStateTransition(state);enter.hasExitTime=false;enter.duration=.07f;enter.canTransitionToSelf=false;enter.AddCondition(AnimatorConditionMode.If,0,name);
                var exit=state.AddTransition(empty);exit.hasExitTime=true;exit.exitTime=.95f;exit.duration=.12f;
            }
            EditorUtility.SetDirty(c);return c;
        }
        static void Link(AnimatorState from,AnimatorState to,string condition,bool value){var t=from.AddTransition(to);t.hasExitTime=false;t.duration=.16f;t.AddCondition(value?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,condition);}
        [MenuItem("Tools/Water Polo/Animated Athletes/Set Up Prototype and Lab")]
        public static void Prepare()
        {
            RequireSaved();Folder(Root+"/Materials");Folder(Root+"/Prefabs");
            if(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab)==null)
            {
                AnimatedAthleteImport.Prepare();
                var skin=Mat("Athlete skin",new Color(.69f,.40f,.25f));Mat("Blue team",new Color(.025f,.15f,.62f));Mat("White team",new Color(.92f,.95f,1));Mat("Goalkeeper cap",new Color(.83f,.045f,.045f));
                var wrapper=new GameObject("Animated Athlete Visual");wrapper.transform.localPosition=new Vector3(0,-1.25f,0);
                var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/WaterPoloAthlete.fbx"),wrapper.transform);
                var animator=model.GetComponent<Animator>();if(animator==null)animator=model.AddComponent<Animator>();
                animator.avatar=AssetDatabase.LoadAllAssetsAtPath(Root+"/Models/WaterPoloAthlete.fbx").OfType<Avatar>().First();animator.runtimeAnimatorController=Controller();animator.applyRootMotion=false;
                var visual=model.AddComponent<RiggedSwimmerVisual>();visual.alignmentRoot=wrapper.transform;
                foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>()){r.quality=SkinQuality.Bone4;r.updateWhenOffscreen=true;r.localBounds=new Bounds(new Vector3(0,1,0),Vector3.one*4);}
                Theme(visual,PrototypeTeam.Blue,false);
                PrefabUtility.SaveAsPrefabAsset(wrapper,Prefab);Object.DestroyImmediate(wrapper);
            }
            AssetDatabase.SaveAssets();
            if(!File.Exists(Lab))BuildLab();else EditorSceneManager.OpenScene(Lab);
            Debug.Log("AnimatedSwimmerLab ready. Keys 1 tread, 2 swim, 3 sprint, 4 possession, 5 pass, 6 shoot, 7 block, 8 save. No gameplay physics in this isolated lab.");
        }
        public static void Theme(RiggedSwimmerVisual v,PrototypeTeam team,bool keeper)
        {
            var skin=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Athlete skin.mat");
            var suit=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/"+(team==PrototypeTeam.Blue?"Blue team":"White team")+".mat");
            var cap=keeper?AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Goalkeeper cap.mat"):suit;
            // FBX submesh order is swimwear first, full skin second (checked against vertex ranges).
            foreach(var r in v.GetComponentsInChildren<SkinnedMeshRenderer>()){r.sharedMaterials=r.name.Contains("Mannequin")?new[]{suit,skin}:new[]{cap};if(PrefabUtility.IsPartOfPrefabInstance(r))PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
        }
        static void BuildLab()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Lab Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();camera.tag="MainCamera";
            camera.transform.position=new Vector3(3.2f,2.4f,4.2f);camera.transform.LookAt(new Vector3(0,-.25f,0));camera.nearClipPlane=.05f;camera.fieldOfView=42;
            var light=new GameObject("Lab competition light",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(45,-35,0);
            RenderSettings.ambientLight=new Color(.38f,.46f,.56f);RenderSettings.ambientIntensity=1;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Existing pool ceramic at 2.05 m depth";floor.transform.position=new Vector3(0,-2.12f,0);floor.transform.localScale=new Vector3(12,.14f,12);Object.DestroyImmediate(floor.GetComponent<Collider>());floor.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/WaterVisuals/Materials/Poolceramic.mat");
            var water=GameObject.CreatePrimitive(PrimitiveType.Plane);water.name="Existing clear test water";water.layer=4;water.transform.localScale=Vector3.one*1.2f;Object.DestroyImmediate(water.GetComponent<Collider>());
            var quality=water.AddComponent<PoolWaterVisuals>();quality.enabled=false;quality.waterSurface=water.GetComponent<Renderer>();quality.viewCamera=camera;quality.keyboardShortcuts=false;
            quality.profiles=new[]{"Low","Balanced","High"}.Select(n=>AssetDatabase.LoadAssetAtPath<PoolWaterQualityProfile>("Assets/WaterVisuals/Settings/WaterQuality_"+n+".asset")).ToArray();quality.qualityIndex=1;quality.enabled=true;
            var fx=new GameObject("Bounded water effects").AddComponent<WaterInteractionEffects>();
            var serialized=new SerializedObject(fx);serialized.FindProperty("material").objectReferenceValue=AssetDatabase.FindAssets("WaterFeedback t:Material").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Material>).First();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));var v=instance.GetComponentInChildren<RiggedSwimmerVisual>();v.water=fx;
            EditorSceneManager.SaveScene(scene,Lab);
        }
        [MenuItem("Tools/Water Polo/Animated Athletes/Play Isolated Swimmer Lab")]
        public static void PlayLab(){RequireSaved();Prepare();SessionState.SetString(Entry,AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));SessionState.SetBool(Restore,true);EditorSceneManager.playModeStartScene=null;EditorApplication.isPlaying=true;}
        [MenuItem("Tools/Water Polo/Animated Athletes/Apply to One Match Athlete")]
        public static void ApplyOne()=>Apply(false);
        [MenuItem("Tools/Water Polo/Animated Athletes/Apply to All 14 Match Athletes")]
        public static void ApplyAll()=>Apply(true);
        static void Apply(bool all)
        {
            RequireSaved();if(!File.Exists(Prefab))throw new InvalidOperationException("Set up and verify the isolated prototype first.");OlympicArenaSetup.Open();
            if(!File.Exists(Recovery)&&!AssetDatabase.CopyAsset(OlympicArenaSetup.ScenePath,Recovery))throw new IOException("Could not preserve the current arena.");
            var athletes=Object.FindObjectsByType<SwimmerIdentity>().OrderBy(a=>a.Team).ThenBy(a=>a.Role).ThenBy(a=>a.CapNumber);
            var chosen=all?athletes.ToArray():athletes.Where(a=>a.Team==PrototypeTeam.Blue&&a.IsFieldPlayer).Take(1).ToArray();
            foreach(var a in chosen)
            {
                var v=a.GetComponentInChildren<RiggedSwimmerVisual>(true);
                if(v==null){var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab),a.transform);v=go.GetComponentInChildren<RiggedSwimmerVisual>(true);}
                v.identity=a;v.motor=a.Motor;v.ball=Object.FindAnyObjectByType<BallController>();v.match=Object.FindAnyObjectByType<PrototypeMatch>();v.pool=Object.FindAnyObjectByType<PoolBounds>();v.water=Object.FindAnyObjectByType<WaterInteractionEffects>();Theme(v,a.Team,a.Role==AthleteRole.Goalkeeper);
                foreach(var old in a.GetComponentsInChildren<ArenaAthleteVisual>(true)){old.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(old.gameObject);}
                v.alignmentRoot.gameObject.SetActive(true);PrefabUtility.RecordPrefabInstancePropertyModifications(v);EditorUtility.SetDirty(v);
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log("Applied visual-only prototype to "+chosen.Length+" athletes. Original meshes remain inactive as fallback; all gameplay components and references are retained.");
        }
        [MenuItem("Tools/Water Polo/Animated Athletes/Restore Original Match Visuals")]
        public static void Fallback()
        {
            RequireSaved();OlympicArenaSetup.Open();foreach(var v in Object.FindObjectsByType<RiggedSwimmerVisual>(FindObjectsInactive.Include))v.alignmentRoot.gameObject.SetActive(false);
            foreach(var v in Object.FindObjectsByType<ArenaAthleteVisual>(FindObjectsInactive.Include))v.gameObject.SetActive(true);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
    }
}
