using TMPro;
using UnityEngine;

namespace ToduStudios.WaterPolo
{
    [DisallowMultipleComponent]
    public sealed class PrototypeScoreboard : MonoBehaviour
    {
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private TMP_Text goalLabel;

        private void OnEnable()
        {
            if (match != null)
                match.Changed += Refresh;
            Refresh();
        }

        private void Refresh()
        {
            if (match == null)
                return;
            scoreLabel.text = $"BLUE {match.BlueScore} - {match.WhiteScore} WHITE";
            goalLabel.gameObject.SetActive(match.IsRestarting);
        }

        private void OnDisable()
        {
            if (match != null)
                match.Changed -= Refresh;
        }
    }
}
