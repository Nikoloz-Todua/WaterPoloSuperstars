using TMPro;
using UnityEngine;

namespace ToduStudios.WaterPolo
{
    [DisallowMultipleComponent]
    public sealed class PrototypeScoreboard : MonoBehaviour
    {
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private bool compactScore;
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private TMP_Text goalLabel;
        [SerializeField] private bool unifiedNotifications;
        private int blue=-1,white=-1;
        private float pulse;

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
            if(blue!=match.BlueScore || white!=match.WhiteScore)
            {
                if(unifiedNotifications && blue>=0 && (match.BlueScore>blue || match.WhiteScore>white))pulse=0.32f;
                else if(match.BlueScore<blue || match.WhiteScore<white){pulse=0f;scoreLabel.rectTransform.localScale=Vector3.one;}
                blue=match.BlueScore;white=match.WhiteScore;
                scoreLabel.text = compactScore?$"{blue} : {white}":$"BLUE {blue} - {white} WHITE";
            }
            goalLabel.gameObject.SetActive(!unifiedNotifications && match.Phase==MatchPhase.GoalRestart);
        }

        private void Update()
        {
            if(!unifiedNotifications || pulse<=0f)return;
            pulse=Mathf.Max(0f,pulse-Time.unscaledDeltaTime);
            scoreLabel.rectTransform.localScale=Vector3.one*(1f+0.09f*Mathf.Sin(pulse/0.32f*Mathf.PI));
        }

        private void OnDisable()
        {
            if (match != null)
                match.Changed -= Refresh;
        }
    }
}
