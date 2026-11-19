using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using TMPro;
using ToduStudios.WaterPolo;
using ToduStudios.WaterPolo.Editor;
using ToduStudios.WaterPolo.Hub;
using ToduStudios.WaterPolo.WaterVisuals;
using Object=UnityEngine.Object;

public static class MatchExchangeVerification
{
 const string Out="Logs/PenaltiesExclusionsCamera/";
 static T One<T>() where T:Object=>Object.FindAnyObjectByType<T>();
 static void Check(bool p,string why){if(!p)throw new Exception(why);}
 static async Task Until(Func<bool> p,float seconds=12){double end=EditorApplication.timeSinceStartup+seconds;while(!p()){Check(EditorApplication.timeSinceStartup<end,"Timed out waiting for state");await Task.Delay(15);}}
 static async Task Frames(int n){int end=Time.frameCount+n;await Until(()=>Time.frameCount>=end);}
 static async Task Seconds(float n){double end=EditorApplication.timeSinceStartup+n;while(EditorApplication.timeSinceStartup<end)await Task.Delay(15);}
 static async Task Shot(string n){await Seconds(.2f);await Frames(4);ScreenCapture.CaptureScreenshot(Out+n+".png");await Frames(5);}
 static void Input(){if(SessionState.GetInt("Broadcast.Background",-1)<0){SessionState.SetInt("Broadcast.Background",(int)InputSystem.settings.backgroundBehavior);SessionState.SetInt("Broadcast.EditorInput",(int)InputSystem.settings.editorInputBehaviorInPlayMode);}InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;}
 public static async Task<object> WhitePenalty()
 {
  Input();var m=One<PrototypeMatch>();m.RestartMatch();await Until(()=>m.IsPlaying&&!m.AwaitingOpeningPossession);
  var ai=AI();var states=ai.Select(a=>a.enabled).ToArray();
  try
  {
   Disable(ai);Park();
   var all=One<PrototypeRoster>().FieldPlayers;var shooter=all.First(p=>p.Team==PrototypeTeam.White);
   var defender=all.First(p=>p.Team==PrototypeTeam.Blue);
   var goal=Team(shooter.Team).AttackingGoal;var dir=goal.transform.forward;var point=goal.transform.position-dir*3;
   shooter.Motor.ResetPose(point,Quaternion.LookRotation(Quaternion.Euler(0,30,0)*dir));
   defender.Motor.ResetPose(point-dir*.82f,Quaternion.LookRotation(dir));Claim(shooter);
   await Seconds(.85f);
   var brain=shooter.GetComponent<PrototypeSwimmerAI>();brain.enabled=true;
   await Until(()=>brain.Behaviour==SwimmerAIBehaviour.Shoot);
   Check(m.Rules.ReportContact(defender,shooter,IllegalContact.Push),"White shooting action");
   brain.enabled=false;
   await Until(()=>m.Phase==MatchPhase.PenaltyReady);
   Check(m.Rules.Taker==shooter&&defender.PersonalFouls==1,"Correct AI taker and charged defender");
   await Until(()=>m.Phase==MatchPhase.PenaltyFlight||m.Phase==MatchPhase.GoalRestart);
   await Until(()=>m.Phase==MatchPhase.GoalRestart||m.IsPlaying);
   return new{whiteAI=true,whiteScore=m.WhiteScore,phase=m.Phase.ToString()};
  }finally{RestoreAI(ai,states);}
 }
 public static async Task<object> Height()
 {
  Input();var m=One<PrototypeMatch>();m.RestartMatch();await Until(()=>m.IsPlaying&&!m.AwaitingOpeningPossession);
  var mouse=InputSystem.AddDevice<Mouse>();var prefs=One<GamePreferences>();var original=prefs.Current;
  try
  {
   const string menus="MenuOverlay/MenuSafeArea/";
   await Click(mouse,Path("MatchSafeArea/PauseButton"));await Click(mouse,Path(menus+"PausePanel/Settings"));
   await Click(mouse,Path(menus+"SettingsPanel/SidelineCamera"));await Seconds(.8f);
   var slider=Path(menus+"SettingsPanel/CameraHeightRow/Slider").GetComponent<UnityEngine.UI.Slider>();
   var cam=One<GameplayCamera>();var clock=m.Clock.Remaining;
   async Task<float> Set(float t)
   {
    var r=(RectTransform)slider.transform;
    var local=new Vector3(Mathf.Lerp(r.rect.xMin,r.rect.xMax,t),r.rect.center.y);
    var pos=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(local));
    InputSystem.QueueStateEvent(mouse,new MouseState{position=pos});await Seconds(.08f);
    InputSystem.QueueStateEvent(mouse,new MouseState{position=pos,buttons=1});await Seconds(.08f);
    InputSystem.QueueStateEvent(mouse,new MouseState{position=pos});await Seconds(.8f);
    return cam.transform.position.y;
   }
   float low=await Set(.05f),high=await Set(.95f);
   Check(high>low+1f&&clock==m.Clock.Remaining,"Independent height without clock advancement");
   prefs.Save();prefs.Reload();Check(Mathf.Abs(prefs.Current.cameraHeight-slider.value)<.002f,"Height persists");
   await Shot("Sideline-Height-Settings");
   return new{low,high,paused=true};
  }finally{prefs.SetHeight(original.cameraHeight);prefs.SetCameraView((int)original.cameraView);prefs.Save();m.SetPaused(false);InputSystem.RemoveDevice(mouse);}
 }
 public static async Task<object> Discipline()
 {
  Input();var m=One<PrototypeMatch>();m.RestartMatch();await Until(()=>m.IsPlaying&&!m.AwaitingOpeningPossession);
  var ai=AI();var states=ai.Select(p=>p.enabled).ToArray();
  try
  {
   Disable(ai);
   var roster=One<PrototypeRoster>();var blue=roster.FieldPlayers.Where(p=>p.Team==PrototypeTeam.Blue).ToArray();
   var offender=roster.FieldPlayers.First(p=>p.Team==PrototypeTeam.White);var oldCap=offender.CapNumber;
   for(int incident=1;incident<=3;incident++)
   {
    Park();var goal=Team(PrototypeTeam.White).DefendedGoal;
    var edge=goal.transform.position-goal.transform.forward*2+Vector3.right*7;
    offender.Motor.ResetPose(edge,Quaternion.identity);blue[1].Motor.ResetPose(edge+Vector3.left*.6f,Quaternion.identity);
    blue[0].Motor.ResetPose(Vector3.zero,Quaternion.identity);Claim(blue[0]);
    Check(m.Rules.ReportContact(offender,blue[1],IllegalContact.Hold),"Exclusion action");
    Check(offender.PersonalFouls==incident&&!offender.EligibleForPlay,"Single personal and gate");
    Check(!m.Rules.ReportContact(offender,blue[1],IllegalContact.Hold),"Duplicate denied");
    m.SetPaused(true);var remaining=offender.ExclusionRemaining;await Seconds(.2f);
    Check(remaining==18f&&remaining==offender.ExclusionRemaining,"Paused clock");m.SetPaused(false);
    if(incident==3){Check(offender.PermanentlyRemoved,"Third foul out");await Shot("Third-Personal-Out");}
    await Until(()=>!offender.AwaitingExit,9);
    if(incident==1)await Shot("Excluded-Reentry-Area");
    await Until(()=>m.Phase==MatchPhase.FreeThrowReady||m.IsPlaying,8);
    if(!m.IsPlaying)Check(m.Rules.Taker.GetComponent<SwimmerPassing>().TryPass(),"Free throw release");
    await Until(()=>m.IsPlaying,5);
    if(incident==2)Claim(roster.FieldPlayers.First(p=>p.Team==PrototypeTeam.White&&p!=offender));
    else m.AdvanceLiveTime(offender.ExclusionRemaining+.01f);
    await Until(()=>offender.EligibleForPlay,3);
    if(incident<3)Check(offender.CapNumber==oldCap,"Own reentry");
    else Check(offender.CapNumber!=oldCap&&offender.PersonalFouls==0,"Valid substitute after third");
   }
   return new{passed=true,oldCap,newCap=offender.CapNumber,active=m.Rules.ActiveFields(PrototypeTeam.White)};
  }finally{RestoreAI(ai,states);}
 }

 public static async Task<object> Exchange()
 {
  Input();var m=One<PrototypeMatch>();m.RestartMatch();await Until(()=>m.IsPlaying&&!m.AwaitingOpeningPossession);
  var roster=One<PrototypeRoster>();var mouse=InputSystem.AddDevice<Mouse>();var ai=AI();var states=ai.Select(x=>x.enabled).ToArray();
  try
  {
   Disable(ai);Park();
   var slot=roster.Members.First(p=>p.Team==PrototypeTeam.Blue&&p.IsFieldPlayer);
   var owner=roster.FieldPlayers.First(p=>p.Team==PrototypeTeam.White);Claim(owner);
   var swaps=new List<string>();m.Rules.SubstitutionStage+=(p,s)=>swaps.Add(s);
   const string menus="MenuOverlay/MenuSafeArea/";
   await Click(mouse,Path("MatchSafeArea/PauseButton"));
   await Click(mouse,Path(menus+"PausePanel/Substitutions"));
   var panel=Path(menus+"SubstitutionPanel");var title=panel.Find("Outgoing").GetComponentInChildren<TMP_Text>();
   for(int i=0;i<7&&!title.text.Contains("#"+slot.CapNumber+" ");i++)await Click(mouse,panel.Find("Outgoing"));
   Check(title.text.Contains("#"+slot.CapNumber+" "),"Outgoing cap");
   await Click(mouse,panel.Find("Confirm"));Check(!slot.EligibleForPlay,"Outgoing gated");
   await Click(mouse,panel.Find("Close"));await Click(mouse,Path(menus+"PausePanel/Resume"));
   await Shot("Substitute-Waiting");await Until(()=>slot.EligibleForPlay,13);
   Check(swaps.Contains("Hand contact")&&swaps.Contains("Entering")&&slot.CollectionDefinitionId!=null,"Flying exchange");
   await Shot("Substitute-Reentry");
   return new {cap=slot.CapNumber,slot.CollectionDefinitionId,events=swaps.ToArray(),active=m.Rules.ActiveFields(PrototypeTeam.Blue)};
  }finally{RestoreAI(ai,states);InputSystem.RemoveDevice(mouse);}
 }
 static Behaviour[] AI()=>Object.FindObjectsByType<PrototypeSwimmerAI>().Cast<Behaviour>().Concat(Object.FindObjectsByType<PrototypeGoalkeeper>()).ToArray();
 static void Disable(Behaviour[] list){foreach(var b in list)b.enabled=false;foreach(var p in One<PrototypeRoster>().Members)p.Motor.StopMotion();}
 static void RestoreAI(Behaviour[] list,bool[] states){for(int i=0;i<list.Length;i++)if(list[i]!=null)list[i].enabled=states[i];}
 static PrototypeTeamTactics Team(PrototypeTeam t)=>Object.FindObjectsByType<PrototypeTeamTactics>().Single(x=>x.Team==t);
 static void Park(){int b=0,w=0;foreach(var p in One<PrototypeRoster>().FieldPlayers)p.Motor.ResetPose(new Vector3(p.Team==PrototypeTeam.Blue?-8:8,0,-10+4*(p.Team==PrototypeTeam.Blue?b++:w++)),Quaternion.identity);}
 static void Claim(SwimmerIdentity p){One<BallController>().ResetForRestart(p.Motor.Position);Check(p.Interaction.TryCollect(),"Actual nearby pickup");}
 static Transform Path(string path)=>One<Canvas>().transform.Find(path);
 static async Task Click(Mouse mouse,Transform t)
 {
  Check(t!=null&&t.gameObject.activeInHierarchy,"Visible pointer target");var r=(RectTransform)t;Vector2 p=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));var data=new PointerEventData(EventSystem.current){position=p};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);Check(hits.Count>0&&hits[0].gameObject.transform.IsChildOf(t),"No invisible blocker on "+t.name);
  InputSystem.QueueStateEvent(mouse,new MouseState{position=p});await Seconds(.04f);InputSystem.QueueStateEvent(mouse,new MouseState{position=p,buttons=1});await Seconds(.06f);InputSystem.QueueStateEvent(mouse,new MouseState{position=p});await Seconds(.08f);
 }
}
