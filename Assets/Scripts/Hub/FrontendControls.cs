using System;
using TMPro;
using UnityEngine;

namespace ToduStudios.WaterPolo.Hub
{
    /// <summary>Reusable image hit regions and Back controls; no baked-in art captions.</summary>
    public static class FrontendControls
    {
        public static UnityEngine.UI.Image Picture(Transform parent,string name,Sprite sprite,float x,float y,float w,float h)
        {
            var rect=HubWidgets.Rect(parent,name,x,y,w,h);var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;return image;
        }
        public static UnityEngine.UI.Button ArtButton(Transform parent,string name,Sprite sprite,float x,float y,float w,float h,Action click)
        {
            var root=HubWidgets.Rect(parent,name,x,y,w,h);var hit=root.gameObject.AddComponent<UnityEngine.UI.Image>();hit.color=new Color(1,1,1,.001f);
            var art=Picture(root,"Artwork",sprite,.025f,.025f,.95f,.95f);var button=root.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=art;
            var colors=button.colors;colors.highlightedColor=new Color(1.13f,1.13f,1.13f);colors.selectedColor=new Color(1.10f,1.02f,.78f);colors.pressedColor=new Color(.72f,.86f,1);colors.fadeDuration=.07f;button.colors=colors;
            if(click!=null)button.onClick.AddListener(()=>click());root.gameObject.AddComponent<MenuButtonFeedback>().artwork=art.transform;return button;
        }
        public static UnityEngine.UI.Button Back(Transform parent,string name,float x,float y,float w,float h,Action click)
        {
            var b=HubWidgets.Button(parent,name,"X",x,y,w,h,click,new Color(.08f,.27f,.42f));
            b.GetComponentInChildren<TMP_Text>().fontSize=b.GetComponentInChildren<TMP_Text>().fontSizeMax=38;
            return b;
        }
    }
}
