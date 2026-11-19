using TMPro;
using UnityEngine;

namespace ToduStudios.WaterPolo
{
    [DefaultExecutionOrder(220)]
    public sealed class PenaltyAimIndicator : MonoBehaviour
    {
        public PrototypeMatch match;
        public PlayerSelection selection;
        public Camera lens;
        public TMP_Text mark;
        void LateUpdate()
        {
            var rules = match.Rules;
            bool visible = !match.IsPaused && match.Phase == MatchPhase.PenaltyReady && rules.Taker != null &&
                selection.HumanTeam == rules.Taker.Team && match.GameplayView.enabled;
            mark.enabled = visible;
            if (!visible) return;
            Vector3 screen = lens.WorldToScreenPoint(rules.PenaltyAimPoint);
            if (screen.z <= 0f) { mark.enabled = false; return; }
            mark.rectTransform.position = screen;
        }
    }
}
