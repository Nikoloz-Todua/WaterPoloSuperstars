using UnityEngine;
using UnityEngine.EventSystems;

namespace ToduStudios.WaterPolo
{
    [DisallowMultipleComponent]
    public sealed class VirtualSwimJoystick : MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler,ICancelHandler
    {
        [SerializeField] private RectTransform handle;
        [SerializeField] private MobileMatchControls controls;
        [SerializeField,Range(0.05f,0.3f)] private float deadZone=0.12f;
        private int pointer=int.MinValue;
        public Vector2 Value {get;private set;}
        public void OnPointerDown(PointerEventData e)
        {if(pointer!=int.MinValue || e.button!=PointerEventData.InputButton.Left || !controls.CanMove)return;pointer=e.pointerId;OnDrag(e);}
        public void OnDrag(PointerEventData e)
        {
            if(pointer!=e.pointerId || !controls.CanMove)return;
            var rect=(RectTransform)transform;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,e.position,e.pressEventCamera,out var point))return;
            float radius=rect.rect.width*0.32f;var raw=Vector2.ClampMagnitude(point/Mathf.Max(1f,radius),1f);
            Value=raw.magnitude<deadZone?Vector2.zero:raw.normalized*((raw.magnitude-deadZone)/(1f-deadZone));
            handle.anchoredPosition=raw*radius;
        }
        public void OnPointerUp(PointerEventData e){if(pointer==e.pointerId)ResetInput();}
        public void OnCancel(BaseEventData e)=>ResetInput();
        public void ResetInput(){pointer=int.MinValue;Value=Vector2.zero;if(handle!=null)handle.anchoredPosition=Vector2.zero;}
        private void OnDisable()=>ResetInput();
    }
}
