using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ToduStudios.WaterPolo
{
    [DisallowMultipleComponent]
    public sealed class MatchSettingsView : MonoBehaviour
    {
        [SerializeField] private GamePreferences preferences;
        [SerializeField] private Slider master,sfx,crowd,zoom,smoothing;
        [SerializeField] private TMP_Text masterValue,sfxValue,crowdValue,zoomValue,smoothingValue,qualityValue;
        [SerializeField] private GameObject crowdRow;
        private void OnEnable()
        {
            master.onValueChanged.AddListener(preferences.SetMaster);sfx.onValueChanged.AddListener(preferences.SetSfx);
            crowd.onValueChanged.AddListener(preferences.SetCrowd);zoom.onValueChanged.AddListener(preferences.SetZoom);
            smoothing.onValueChanged.AddListener(preferences.SetSmoothing);preferences.Changed+=Refresh;Refresh();
        }
        private void OnDisable()
        {
            master.onValueChanged.RemoveListener(preferences.SetMaster);sfx.onValueChanged.RemoveListener(preferences.SetSfx);
            crowd.onValueChanged.RemoveListener(preferences.SetCrowd);zoom.onValueChanged.RemoveListener(preferences.SetZoom);
            smoothing.onValueChanged.RemoveListener(preferences.SetSmoothing);preferences.Changed-=Refresh;
        }
        private void Refresh()
        {
            var v=preferences.Current;
            master.SetValueWithoutNotify(v.master);sfx.SetValueWithoutNotify(v.sfx);crowd.SetValueWithoutNotify(v.crowd);
            zoom.SetValueWithoutNotify(v.zoom);smoothing.SetValueWithoutNotify(v.smoothing);
            masterValue.text=$"{v.master:P0}";sfxValue.text=$"{v.sfx:P0}";crowdValue.text=$"{v.crowd:P0}";
            zoomValue.text=$"{v.zoom:0.00}x";smoothingValue.text=$"{v.smoothing:0.00}x";qualityValue.text=v.graphics.ToString().ToUpperInvariant();
            crowdRow.SetActive(preferences.SupportsCrowd);
        }
        public void Low()=>preferences.SetGraphics(0);
        public void Medium()=>preferences.SetGraphics(1);
        public void High()=>preferences.SetGraphics(2);
    }
}
