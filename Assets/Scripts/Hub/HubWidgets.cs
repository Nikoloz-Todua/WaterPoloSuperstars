using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ToduStudios.WaterPolo.Hub
{
    /// <summary>Shared lightweight geometry and controls, expressed in normalized parent bounds.</summary>
    public static class HubWidgets
    {
        public static readonly Color Navy=new Color(.025f,.07f,.15f),Card=new Color(.045f,.13f,.24f),Cyan=new Color(.13f,.88f,.88f),Gold=new Color(1f,.77f,.24f),Muted=new Color(.64f,.79f,.85f);
        public static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(x+w,y+h);r.offsetMin=r.offsetMax=Vector2.zero;return r;
        }
        public static RectTransform Panel(Transform parent,string name,float x,float y,float w,float h,Color color)
        {var r=Rect(parent,name,x,y,w,h);var g=r.gameObject.AddComponent<RoundedPanelGraphic>();g.color=color;g.raycastTarget=false;return r;}
        public static TMP_Text Text(Transform parent,string name,string value,float x,float y,float w,float h,float size=26,Color? color=null,TextAlignmentOptions align=TextAlignmentOptions.Left)
        {
            var r=Rect(parent,name,x,y,w,h);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.text=value;t.fontSize=size;t.enableAutoSizing=true;t.fontSizeMin=Mathf.Min(20,size);t.fontSizeMax=size;t.color=color??Color.white;t.alignment=align;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Truncate;return t;
        }
        public static UnityEngine.UI.Button Button(Transform parent,string name,string caption,float x,float y,float w,float h,Action action,Color? color=null,bool enabled=true)
        {
            var r=Panel(parent,name,x,y,w,h,color??Card);var g=r.GetComponent<RoundedPanelGraphic>();g.raycastTarget=true;
            var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=g;b.interactable=enabled;
            var colors=b.colors;colors.highlightedColor=new Color(1.15f,1.15f,1.15f);colors.pressedColor=new Color(.6f,.8f,.9f);colors.disabledColor=new Color(.45f,.5f,.58f);colors.fadeDuration=.08f;b.colors=colors;
            b.navigation=new Navigation{mode=Navigation.Mode.Automatic};if(action!=null)b.onClick.AddListener(()=>action());
            Text(r,"Label",caption,.05f,.05f,.9f,.9f,24,Color.white,TextAlignmentOptions.Center).fontStyle=FontStyles.Bold;return b;
        }
        public static void Clear(Transform parent)
        {
            for(int i=parent.childCount-1;i>=0;i--){var child=parent.GetChild(i);child.gameObject.SetActive(false);if(Application.isPlaying)UnityEngine.Object.Destroy(child.gameObject);else UnityEngine.Object.DestroyImmediate(child.gameObject);}
        }
        public static RectTransform Scroll(Transform parent,string name,bool horizontal=false)
        {
            var root=Rect(parent,name,0,0,1,.88f);var scroll=root.gameObject.AddComponent<ScrollRect>();var viewport=Rect(root,"Viewport",0,0,1,1);
            viewport.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(1,1,1,.01f);viewport.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            var content=Rect(viewport,"Content",0,0,1,1);content.pivot=new Vector2(0,1);content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.sizeDelta=new Vector2(0,100);
            scroll.viewport=viewport;scroll.content=content;scroll.horizontal=horizontal;scroll.vertical=!horizontal;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=45;
            return content;
        }
        public static void Pool(Transform parent)
        {
            var pool=Panel(parent,"Pool",.04f,.08f,.92f,.85f,new Color(.035f,.39f,.52f));
            for(int i=0;i<8;i++)Panel(pool,"WaterLine"+i,.04f,.08f+i*.11f,.92f,.004f,new Color(.3f,.9f,.95f,.2f));
            Panel(pool,"CentreLine",.03f,.49f,.94f,.008f,new Color(.8f,1,1,.5f));Panel(pool,"Goal",.36f,.03f,.28f,.018f,Gold);
        }
        public static void Athlete(Transform parent,Color cap,Color suit)
        {
            // Original procedural athlete portrait: cap, face, shoulders and ball; no licensed artwork.
            Panel(parent,"Halo",.17f,.03f,.66f,.85f,new Color(.05f,.65f,.75f,.25f));
            Panel(parent,"Shoulders",.19f,.08f,.62f,.36f,suit);Panel(parent,"Neck",.43f,.38f,.14f,.12f,new Color(.82f,.52f,.34f));
            Panel(parent,"Face",.35f,.44f,.3f,.29f,new Color(.94f,.67f,.45f));Panel(parent,"Cap",.33f,.65f,.34f,.15f,cap);
            Panel(parent,"EarLeft",.31f,.5f,.07f,.13f,Color.white);Panel(parent,"EarRight",.62f,.5f,.07f,.13f,Color.white);
            Text(parent,"CapMark","W",.4f,.64f,.2f,.15f,42,Color.white,TextAlignmentOptions.Center);
            Panel(parent,"EyeLeft",.41f,.58f,.035f,.014f,Navy);Panel(parent,"EyeRight",.55f,.58f,.035f,.014f,Navy);
            Panel(parent,"Ball",.65f,.11f,.23f,.24f,Gold);Text(parent,"BallMark","W",.68f,.15f,.17f,.16f,50,Navy,TextAlignmentOptions.Center);
        }
        public static readonly Color[] Palette={new Color(.08f,.48f,.9f),new Color(.03f,.7f,.65f),new Color(.36f,.26f,.77f),new Color(.85f,.24f,.38f)};
    }
}
