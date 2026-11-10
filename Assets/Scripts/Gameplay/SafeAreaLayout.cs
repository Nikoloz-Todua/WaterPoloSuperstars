using UnityEngine;

namespace ToduStudios.WaterPolo
{
    [DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaLayout : MonoBehaviour
    {
        private Rect previous;
        private Vector2 screenSize;
        private RectTransform rect;
        private void OnEnable(){rect=GetComponent<RectTransform>();Refresh();}
        private void Update(){if(previous!=Screen.safeArea || screenSize!=new Vector2(Screen.width,Screen.height))Refresh();}
        private void Refresh()=>Apply(Screen.safeArea,new Vector2(Screen.width,Screen.height));
        // Public geometry path also permits deterministic Editor safe-area tests.
        public void Apply(Rect safe,Vector2 size)
        {
            if(rect==null)rect=GetComponent<RectTransform>();
            if(size.x<=0f || size.y<=0f)return;
            previous=safe;screenSize=size;
            rect.anchorMin=new Vector2(Mathf.Clamp01(safe.xMin/size.x),Mathf.Clamp01(safe.yMin/size.y));
            rect.anchorMax=new Vector2(Mathf.Clamp01(safe.xMax/size.x),Mathf.Clamp01(safe.yMax/size.y));
            rect.offsetMin=rect.offsetMax=Vector2.zero;
        }
    }
}
