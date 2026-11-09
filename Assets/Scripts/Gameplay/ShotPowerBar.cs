using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>One reusable HUD bar observes the selected player's charge, without controlling shooting or possession.</summary>
    [DefaultExecutionOrder(220), DisallowMultipleComponent]
    public sealed class ShotPowerBar : MonoBehaviour
    {
        [SerializeField] private PlayerSelection selection;
        [SerializeField] private GameObject panel;
        [SerializeField] private UnityEngine.UI.Image fill;
        private SwimmerIdentity observed;
        private ShotCharge charge;
        private void LateUpdate()
        {
            if (selection.ActivePlayer != observed)
            {
                observed = selection.ActivePlayer;
                charge = observed != null ? observed.GetComponent<ShotCharge>() : null;
            }
            bool visible = !selection.InputLocked && charge != null && charge.IsCharging;
            if (panel.activeSelf != visible) panel.SetActive(visible);
            if (!visible) return;
            float amount = charge.NormalizedCharge;
            fill.rectTransform.anchorMax = new Vector2(amount, 1f);
            fill.color = amount < 0.5f ? Color.Lerp(Color.green, Color.yellow, amount * 2f) :
                Color.Lerp(Color.yellow, new Color(1f, 0.18f, 0.05f), (amount - 0.5f) * 2f);
        }
        private void OnDisable() { if (panel != null) panel.SetActive(false); }
    }
}
