using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ToduStudios.WaterPolo.Editor
{
    internal static class MobileControlsSetup
    {
        private static readonly Color Navy=new Color(0.025f,0.065f,0.12f,0.94f),Blue=new Color(0.04f,0.4f,0.7f,0.95f),Muted=new Color(0.65f,0.79f,0.88f);
        internal static void Configure(Canvas canvas,PrototypeMatch match)
        {
            var controls=canvas.GetComponent<MobileMatchControls>();
            if(controls!=null && new SerializedObject(controls).FindProperty("interfaceVersion").intValue>=1)return;
            var selection=UnityEngine.Object.FindAnyObjectByType<PlayerSelection>();var roster=UnityEngine.Object.FindAnyObjectByType<PrototypeRoster>();
            var safe=canvas.transform.Find("MatchSafeArea");
            if(controls==null)controls=canvas.gameObject.AddComponent<MobileMatchControls>();
            Assign(controls,"match",match);Assign(controls,"selection",selection);Assign(controls,"ball",UnityEngine.Object.FindAnyObjectByType<BallController>());
            Assign(controls,"menus",canvas.GetComponent<MatchMenuController>());
            Assign(controls,"humanTeam",UnityEngine.Object.FindObjectsByType<PrototypeTeamTactics>().Single(t=>t.Team==selection.HumanTeam));
            var root=Rect(safe,"MobileControls",Vector2.zero,Vector2.zero,Vector2.zero);root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;Assign(controls,"gameplayRoot",root.gameObject);
            var stick=Panel(root,"SwimJoystick",new Vector2(0f,0f),new Vector2(178f,160f),new Vector2(220f,220f),Navy,110f);stick.GetComponent<RoundedPanelGraphic>().raycastTarget=true;
            Panel(stick,"InnerRing",new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(154f,154f),new Color(0.08f,0.26f,0.4f,0.9f),77f);
            var handle=Panel(stick,"Handle",new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(88f,88f),new Color(0.3f,0.77f,0.95f,0.95f),44f);
            var joystick=stick.GetComponent<VirtualSwimJoystick>()??stick.gameObject.AddComponent<VirtualSwimJoystick>();Assign(joystick,"controls",controls);Assign(joystick,"handle",handle);Assign(controls,"joystick",joystick);
            Label(root,"SwimHint","SWIM",new Vector2(0f,0f),new Vector2(178f,24f),new Vector2(220f,32f),18f,Muted);
            Action(root,controls,MobileMatchAction.Shoot,"SHOOT",new Vector2(-130f,205f),new Vector2(158f,158f),Blue);
            Action(root,controls,MobileMatchAction.Steal,"STEAL",new Vector2(-130f,205f),new Vector2(158f,158f),Blue);
            Action(root,controls,MobileMatchAction.Collect,"COLLECT",new Vector2(-130f,80f),new Vector2(150f,66f),Navy);
            Action(root,controls,MobileMatchAction.Pass,"PASS",new Vector2(-304f,116f),new Vector2(132f,104f),Blue);
            Action(root,controls,MobileMatchAction.Lob,"LOB",new Vector2(-460f,116f),new Vector2(132f,104f),Navy);
            Action(root,controls,MobileMatchAction.Sprint,"SPRINT",new Vector2(-304f,252f),new Vector2(132f,104f),Navy);
            Action(root,controls,MobileMatchAction.Switch,"SWITCH",new Vector2(-460f,252f),new Vector2(132f,104f),Navy);
            Action(root,controls,MobileMatchAction.ShotMode,"NORMAL",new Vector2(-130f,80f),new Vector2(150f,66f),Navy);
            Action(root,controls,MobileMatchAction.Strategy,"DEFENSE: PRESS",new Vector2(-310f,363f),new Vector2(430f,64f),Navy);
            var array=new SerializedObject(controls);var property=array.FindProperty("buttons");var buttons=root.GetComponentsInChildren<GameplayActionButton>(true);
            property.arraySize=buttons.Length;for(int i=0;i<buttons.Length;i++)property.GetArrayElementAtIndex(i).objectReferenceValue=buttons[i];array.ApplyModifiedPropertiesWithoutUndo();
            var instruction=Label(safe,"RestartInstruction","",new Vector2(0.5f,1f),new Vector2(0f,-289f),new Vector2(1000f,50f),27f,new Color(0.5f,0.92f,1f));Assign(controls,"restartInstruction",instruction);
            foreach(var player in roster.Members){var input=player.GetComponent<SwimmerInput>();if(input!=null)Assign(input,"mobileControls",controls);}
            // Lift compact status above thumb zones. Preserve existing observers and their references.
            Place(safe.Find("AthleteCard") as RectTransform,new Vector2(220f,337f),new Vector2(376f,116f));
            Place(safe.Find("RulesCard") as RectTransform,new Vector2(-255f,470f),new Vector2(446f,116f));
            Place(safe.Find("StrategyCard") as RectTransform,new Vector2(0f,43f),new Vector2(390f,60f));
            Place(safe.Find("QuickControls") as RectTransform,new Vector2(0f,98f),new Vector2(780f,32f));
            // Repair known first-setup geometry; these labels were created before their final text sizes were assigned.
            foreach(string name in new[]{"BlueTeam","WhiteTeam"})
            {
                var label=safe.Find("ScoreCard/"+name).GetComponent<TMP_Text>();label.margin=Vector4.zero;label.enableAutoSizing=false;
            }
            var pause=safe.Find("PauseButton").GetComponent<RectTransform>();pause.sizeDelta=new Vector2(106f,106f);
            var completion=new SerializedObject(controls);completion.FindProperty("interfaceVersion").intValue=1;completion.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        }
        private static void Action(Transform root,MobileMatchControls controls,MobileMatchAction action,string caption,Vector2 point,Vector2 size,Color color)
        {
            var rect=Panel(root,action.ToString(),new Vector2(1f,0f),point,size,color,action==MobileMatchAction.Shoot || action==MobileMatchAction.Steal?79f:20f);
            var graphic=rect.GetComponent<RoundedPanelGraphic>();graphic.raycastTarget=true;
            var label=Label(rect,"Caption",caption,new Vector2(0.5f,0.5f),Vector2.zero,size-new Vector2(12f,8f),action==MobileMatchAction.Strategy?22f:24f,Color.white);label.fontStyle=FontStyles.Bold;
            var button=rect.GetComponent<GameplayActionButton>()??rect.gameObject.AddComponent<GameplayActionButton>();Assign(button,"controls",controls);Assign(button,"label",label);Assign(button,"background",graphic);
            var data=new SerializedObject(button);data.FindProperty("action").enumValueIndex=(int)action;data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static RectTransform Panel(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size,Color color,float radius)
        {
            var rect=Rect(parent,name,anchor,pos,size);var graphic=rect.GetComponent<RoundedPanelGraphic>()??rect.gameObject.AddComponent<RoundedPanelGraphic>();graphic.color=color;graphic.raycastTarget=false;
            var data=new SerializedObject(graphic);data.FindProperty("radius").floatValue=radius;data.ApplyModifiedPropertiesWithoutUndo();return rect;
        }
        private static TMP_Text Label(Transform parent,string name,string text,Vector2 anchor,Vector2 pos,Vector2 size,float font,Color color)
        {var label=MatchHUDSetup.Label(parent,name,anchor,pos,size,font);label.text=text;label.color=color;label.margin=Vector4.zero;label.textWrappingMode=TextWrappingModes.NoWrap;return label;}
        private static RectTransform Rect(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size)
        {var rect=MatchHUDSetup.Rect(parent,name,anchor,pos,size);rect.anchorMin=rect.anchorMax=anchor;rect.pivot=new Vector2(0.5f,0.5f);rect.anchoredPosition=pos;rect.sizeDelta=size;return rect;}
        private static void Place(RectTransform rect,Vector2 pos,Vector2 size){if(rect==null)return;rect.anchoredPosition=pos;rect.sizeDelta=size;}
        private static void Assign(UnityEngine.Object target,string field,UnityEngine.Object value)=>Milestone9PrototypeSetup.Assign(target,field,value);
    }
}
