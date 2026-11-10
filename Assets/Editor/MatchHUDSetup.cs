using ToduStudios.WaterPolo;
using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using UnityEditor.Events;

namespace ToduStudios.WaterPolo.Editor
{
    internal static class MatchHUDSetup
    {
        public static void Configure(PrototypeMatch match,PlayerSelection selection,PrototypeTeamTactics humanTeam)
        {
            var canvas=UnityEngine.Object.FindAnyObjectByType<PrototypeScoreboard>().GetComponentInParent<Canvas>();
            if(canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>()==null)canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var events=UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if(events==null)
            {
                var root=new GameObject("PrototypeEventSystem",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
                events=root.GetComponent<UnityEngine.EventSystems.EventSystem>();
            }
            var module=events.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();if(module!=null && module.actionsAsset==null)module.AssignDefaultActions();
            var hud=canvas.GetComponent<MatchHUD>();if(hud==null)hud=canvas.gameObject.AddComponent<MatchHUD>();
            Assign(hud,"match",match);Assign(hud,"selection",selection);Assign(hud,"humanTeam",humanTeam);
            var clock=Label(canvas.transform,"MatchClock",new Vector2(0f,1f),new Vector2(180f,-52f),new Vector2(280f,52f),32f);
            var shot=Label(canvas.transform,"ShotClock",new Vector2(1f,1f),new Vector2(-140f,-52f),new Vector2(200f,52f),32f);
            var strategy=Label(canvas.transform,"DefenseMode",new Vector2(0f,0f),new Vector2(220f,28f),new Vector2(420f,45f),24f);
            Assign(hud,"clockLabel",clock);Assign(hud,"shotClockLabel",shot);Assign(hud,"strategyLabel",strategy);
            var panel=Rect(canvas.transform,"MatchPanel",new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(880f,370f));
            var image=panel.GetComponent<UnityEngine.UI.Image>();if(image==null)image=panel.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=new Color(0.015f,0.035f,0.065f,0.96f);
            var title=Label(panel,"Title",new Vector2(0.5f,0.5f),new Vector2(0f,70f),new Vector2(820f,150f),40f);
            var next=Button(panel,"Continue","CONTINUE / START",new Vector2(0f,-95f),match.ContinueMatch);
            var restart=Button(panel,"Restart","RESTART MATCH",new Vector2(-205f,-95f),match.RestartMatch);
            var back=Button(panel,"Return","RETURN TO PROTOTYPE",new Vector2(205f,-95f),match.ReturnToPrototype);
            Assign(hud,"panel",panel.gameObject);Assign(hud,"panelTitle",title);Assign(hud,"continueButton",next);Assign(hud,"restartButton",restart);Assign(hud,"returnButton",back);
            panel.gameObject.SetActive(false);
            var staminaHUD=canvas.GetComponent<ActiveStaminaHUD>();if(staminaHUD==null)staminaHUD=canvas.gameObject.AddComponent<ActiveStaminaHUD>();
            var stamina=Rect(canvas.transform,"StaminaBar",new Vector2(1f,0f),new Vector2(-170f,42f),new Vector2(260f,18f));
            var background=stamina.GetComponent<UnityEngine.UI.Image>();if(background==null)background=stamina.gameObject.AddComponent<UnityEngine.UI.Image>();background.color=new Color(0.02f,0.04f,0.08f,0.9f);background.raycastTarget=false;
            var fill=stamina.Find("Fill") as RectTransform;
            if(fill==null)
            {
                fill=new GameObject("Fill",typeof(RectTransform),typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();fill.SetParent(stamina,false);fill.anchorMin=Vector2.zero;fill.anchorMax=Vector2.one;fill.offsetMin=fill.offsetMax=Vector2.zero;fill.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            }
            var staminaLabel=Label(canvas.transform,"StaminaLabel",new Vector2(1f,0f),new Vector2(-170f,75f),new Vector2(300f,40f),22f);
            Assign(staminaHUD,"selection",selection);Assign(staminaHUD,"fill",fill.GetComponent<UnityEngine.UI.Image>());Assign(staminaHUD,"label",staminaLabel);
        }
        internal static RectTransform Rect(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size)
        {
            var rect=parent.Find(name) as RectTransform;
            if(rect!=null)return rect;
            rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=anchor;rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
        }
        internal static TMP_Text Label(Transform parent,string name,Vector2 anchor,Vector2 position,Vector2 size,float fontSize)
        {
            var rect=Rect(parent,name,anchor,position,size);var label=rect.GetComponent<TextMeshProUGUI>();if(label==null)label=rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font=TMP_Settings.defaultFontAsset;label.fontSize=fontSize;label.alignment=TextAlignmentOptions.Center;label.color=Color.white;label.raycastTarget=false;return label;
        }
        private static GameObject Button(Transform parent,string name,string caption,Vector2 position,UnityAction action)
        {
            var rect=Rect(parent,name,new Vector2(0.5f,0.5f),position,new Vector2(375f,65f));
            var button=rect.GetComponent<UnityEngine.UI.Button>();
            if(button==null)
            {
                var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=new Color(0.07f,0.28f,0.48f);
                button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;UnityEventTools.AddPersistentListener(button.onClick,action);
            }
            Label(rect,"Label",new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(365f,60f),24f).text=caption;
            return rect.gameObject;
        }
        private static void Assign(UnityEngine.Object target,string field,UnityEngine.Object value)=>Milestone9PrototypeSetup.Assign(target,field,value);
    }
}
