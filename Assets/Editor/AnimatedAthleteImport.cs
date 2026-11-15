using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace ToduStudios.WaterPolo.Editor
{
public static class AnimatedAthleteImport
{
 public const string Root="Assets/SwimmerAnimationPrototype";
 [MenuItem("Tools/Water Polo/Animated Athletes/Reimport Prototype Source FBXs")]
 public static void Reimport()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode before reimporting the prototype.");
  for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save intended scene edits first.");
  Debug.Log(Prepare());
 }
 public static string Prepare()
 {
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  var path=Root+"/Models/WaterPoloAthlete.fbx";
  var imp=(ModelImporter)AssetImporter.GetAtPath(path);
  imp.animationType=ModelImporterAnimationType.Human;
  imp.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
  imp.importAnimation=false; imp.optimizeGameObjects=false;imp.preserveHierarchy=true;imp.materialImportMode=ModelImporterMaterialImportMode.None;
  var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
  var desc=imp.humanDescription;
  string[] unity={"Hips","Spine","Chest","UpperChest","Neck","Head","LeftShoulder","RightShoulder","LeftUpperArm","RightUpperArm","LeftLowerArm","RightLowerArm","LeftHand","RightHand","LeftUpperLeg","RightUpperLeg","LeftLowerLeg","RightLowerLeg","LeftFoot","RightFoot","LeftToes","RightToes"};
  string[] bone={"pelvis","spine_01","spine_02","spine_03","neck_01","Head","clavicle_l","clavicle_r","upperarm_l","upperarm_r","lowerarm_l","lowerarm_r","hand_l","hand_r","thigh_l","thigh_r","calf_l","calf_r","foot_l","foot_r","ball_l","ball_r"};
  desc.human=unity.Select((s,i)=>new HumanBone{humanName=s,boneName=bone[i],limit=new HumanLimit{useDefaultValues=true}}).ToArray();
  desc.skeleton=model.GetComponentsInChildren<Transform>().Select(t=>new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray();
  desc.armStretch=.05f;desc.legStretch=.05f;desc.upperArmTwist=.5f;desc.lowerArmTwist=.5f;desc.upperLegTwist=.5f;desc.lowerLegTwist=.5f;desc.feetSpacing=0;
  imp.humanDescription=desc;imp.SaveAndReimport();
  var avatar=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().First();
  foreach(var f in System.IO.Directory.GetFiles(Root+"/Animations","*.fbx"))
  {
   var p=f.Replace('\\','/');var a=(ModelImporter)AssetImporter.GetAtPath(p);
   a.preserveHierarchy=true;a.animationType=ModelImporterAnimationType.Human;a.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;a.sourceAvatar=avatar;
   a.importAnimation=true;a.materialImportMode=ModelImporterMaterialImportMode.None;a.animationCompression=ModelImporterAnimationCompression.Optimal;
   a.animationRotationError=.5f;a.animationPositionError=.5f;a.animationScaleError=.5f;
   var clips=a.defaultClipAnimations;
   foreach(var c in clips){c.name=System.IO.Path.GetFileNameWithoutExtension(p);c.loopTime=!(c.name=="Pass"||c.name=="Shoot"||c.name=="Save");c.loopPose=c.loopTime;c.lockRootPositionXZ=true;c.lockRootHeightY=true;c.lockRootRotation=true;c.keepOriginalPositionY=true;c.keepOriginalOrientation=true;c.keepOriginalPositionXZ=true;}
   a.clipAnimations=clips;a.SaveAndReimport();
  }
  return "Avatar human="+avatar.isHuman+", valid="+avatar.isValid+"; imported ten clips";
 }
}
}
