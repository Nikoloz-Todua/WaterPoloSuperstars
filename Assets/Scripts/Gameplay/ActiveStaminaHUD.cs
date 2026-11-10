using UnityEngine;
using TMPro;

namespace ToduStudios.WaterPolo
{
    [DisallowMultipleComponent]
    public sealed class ActiveStaminaHUD : MonoBehaviour
    {
        [SerializeField] private PlayerSelection selection;
        [SerializeField] private UnityEngine.UI.Image fill;
        [SerializeField] private TMP_Text label;
        private SwimmerIdentity observed;
        private SwimmerStamina stamina;
        private void LateUpdate()
        {
            if(observed!=selection.ActivePlayer)
            {
                observed=selection.ActivePlayer;stamina=observed!=null?observed.GetComponent<SwimmerStamina>():null;
                label.text=stamina!=null && stamina.SprintAllowed?"STAMINA [SHIFT]":"GOALKEEPER";
            }
            float value=stamina!=null?stamina.Value:1f;fill.rectTransform.anchorMax=new Vector2(value,1f);
            fill.color=Color.Lerp(new Color(1f,0.2f,0.08f),new Color(0.15f,0.9f,0.45f),value);
        }
    }
}
