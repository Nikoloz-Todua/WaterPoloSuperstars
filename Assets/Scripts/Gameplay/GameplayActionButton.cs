using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ToduStudios.WaterPolo
{
    public enum MobileMatchAction { Shoot,Pass,Sprint,Switch,Steal,Lob,Collect,Strategy,ShotMode }
    /// <summary>Pointer ownership supports real holds and simultaneous joystick/sprint/shoot touches.</summary>
    [DisallowMultipleComponent]
    public sealed class GameplayActionButton : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler,ICancelHandler
    {
        [SerializeField] private MobileMatchControls controls;
        [SerializeField] private MobileMatchAction action;
        [SerializeField] private Graphic background;
        [SerializeField] private TMP_Text label;
        private float passPressedAt;
        private Color baseColor;
        private int pointer=int.MinValue;
        private bool available,initialized,availabilitySet;
        public MobileMatchAction Action=>action;
        public bool Available=>available;
        private void Awake(){baseColor=background.color;initialized=true;}
        public void SetAvailable(bool value)
        {
            if(!initialized){baseColor=background.color;initialized=true;}
            if(availabilitySet && value==available)return;availabilitySet=true;available=value;
            background.color=value?baseColor:new Color(baseColor.r,baseColor.g,baseColor.b,baseColor.a*0.7f);
            label.color=value?Color.white:new Color(0.72f,0.8f,0.86f,0.85f);
            if(!value)CancelInput();
        }
        public void SetCaption(string caption){if(label.text!=caption)label.text=caption;}
        public void OnPointerDown(PointerEventData e)
        {
            if(pointer!=int.MinValue || e.button!=PointerEventData.InputButton.Left || !available || !controls.CanUse(action))return;
            if(action==MobileMatchAction.Pass)passPressedAt=Time.unscaledTime;
            else if(!controls.Begin(action))return;
            pointer=e.pointerId;background.color=Color.Lerp(baseColor,new Color(0.28f,0.85f,1f,baseColor.a),0.45f);
        }
        public void OnPointerUp(PointerEventData e)
        {
            if(pointer!=e.pointerId)return;
            pointer=int.MinValue;
            if (action==MobileMatchAction.Pass) controls.ReleasePass(Time.unscaledTime-passPressedAt >= .32f);
            else controls.End(action,false);
            if(available)background.color=baseColor;
        }
        public void OnPointerExit(PointerEventData e){if(pointer==e.pointerId)CancelInput();}
        public void OnCancel(BaseEventData e)=>CancelInput();
        public void CancelInput()
        {if(pointer==int.MinValue)return;pointer=int.MinValue;controls.End(action,true);if(available)background.color=baseColor;}
        private void OnDisable()=>CancelInput();
    }
}
