using UnityEngine;
using UnityEngine.EventSystems;

namespace ToduStudios.WaterPolo.Hub
{
    /// <summary>Unscaled, bounded artwork feedback; hit targets and layout never shrink.</summary>
    public sealed class MenuButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, ICancelHandler
    {
        public Transform artwork;
        UnityEngine.UI.Button button;
        bool pressed, hovered;
        int pointer = int.MinValue;
        void Awake() => button = GetComponent<UnityEngine.UI.Button>();
        void Update()
        {
            if (artwork == null) return;
            float target = button != null && button.interactable ? pressed ? .95f : hovered ? 1.025f : 1f : 1f;
            var desired = Vector3.one * target;
            if (artwork.localScale == desired) return;
            artwork.localScale = (artwork.localScale - desired).sqrMagnitude < .000001f ? desired : Vector3.Lerp(artwork.localScale, desired, 1 - Mathf.Exp(-24 * Time.unscaledDeltaTime));
        }
        public void OnPointerDown(PointerEventData e)
        {
            if (pointer != int.MinValue || e.button != PointerEventData.InputButton.Left || button == null || !button.interactable) return;
            pointer = e.pointerId; pressed = true;
        }
        public void OnPointerUp(PointerEventData e) { if (pointer == e.pointerId) { pointer = int.MinValue; pressed = false; } }
        public void OnPointerEnter(PointerEventData e) => hovered = true;
        public void OnPointerExit(PointerEventData e) { hovered = false; if (pointer == e.pointerId) { pointer = int.MinValue; pressed = false; } }
        public void OnCancel(BaseEventData e) => ResetState();
        void OnDisable() => ResetState();
        void ResetState() { pressed = hovered = false; pointer = int.MinValue; if (artwork != null) artwork.localScale = Vector3.one; }
    }
}
