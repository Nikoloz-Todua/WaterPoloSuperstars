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
        [SerializeField] private Slider controlOpacity;
        [SerializeField] private TMP_Text controlOpacityValue,controlSizeValue;
        [SerializeField] private TMP_Text cameraViewValue;
        [SerializeField] private Graphic lengthwiseChoice,sidelineChoice;
        [SerializeField] private Slider height;
        [SerializeField] private TMP_Text heightValue;
        [SerializeField] private TMP_Text autoReplayLabel;
        private void OnEnable()
        {
            if(height!=null) height.onValueChanged.AddListener(preferences.SetHeight);
            master.onValueChanged.AddListener(preferences.SetMaster);sfx.onValueChanged.AddListener(preferences.SetSfx);
            crowd.onValueChanged.AddListener(preferences.SetCrowd);zoom.onValueChanged.AddListener(preferences.SetZoom);
            smoothing.onValueChanged.AddListener(preferences.SetSmoothing);preferences.Changed+=Refresh;Refresh();
            if(controlOpacity!=null)controlOpacity.onValueChanged.AddListener(preferences.SetControlOpacity);
        }
        private void OnDisable()
        {
            if(height!=null) height.onValueChanged.RemoveListener(preferences.SetHeight);
            master.onValueChanged.RemoveListener(preferences.SetMaster);sfx.onValueChanged.RemoveListener(preferences.SetSfx);
            crowd.onValueChanged.RemoveListener(preferences.SetCrowd);zoom.onValueChanged.RemoveListener(preferences.SetZoom);
            smoothing.onValueChanged.RemoveListener(preferences.SetSmoothing);preferences.Changed-=Refresh;
            if(controlOpacity!=null)controlOpacity.onValueChanged.RemoveListener(preferences.SetControlOpacity);
        }
        private void Refresh()
        {
            var v=preferences.Current;
            if(autoReplayLabel!=null)autoReplayLabel.text=v.automaticGoalReplays?"AUTO REPLAY ON":"AUTO REPLAY OFF";
            if(height!=null){height.SetValueWithoutNotify(v.cameraHeight);heightValue.text=$"{v.cameraHeight:0.00}x";}
            master.SetValueWithoutNotify(v.master);sfx.SetValueWithoutNotify(v.sfx);crowd.SetValueWithoutNotify(v.crowd);
            zoom.SetValueWithoutNotify(v.zoom);smoothing.SetValueWithoutNotify(v.smoothing);
            masterValue.text=$"{v.master:P0}";sfxValue.text=$"{v.sfx:P0}";crowdValue.text=$"{v.crowd:P0}";
            zoomValue.text=$"{v.zoom:0.00}x";smoothingValue.text=$"{v.smoothing:0.00}x";qualityValue.text=v.graphics.ToString().ToUpperInvariant();
            crowdRow.SetActive(preferences.SupportsCrowd);
            if(controlOpacity!=null){controlOpacity.SetValueWithoutNotify(v.controlOpacity);controlOpacityValue.text=$"{v.controlOpacity:P0}";}
            if(controlSizeValue!=null)controlSizeValue.text=v.controlSize==0?"SMALL":v.controlSize==2?"LARGE":"NORMAL";
            if(cameraViewValue!=null)cameraViewValue.text=v.cameraView==MatchCameraView.Lengthwise?"LENGTHWISE":"SIDELINE BROADCAST";
            if(lengthwiseChoice!=null)lengthwiseChoice.color=v.cameraView==MatchCameraView.Lengthwise?new Color(.035f,.36f,.57f,.96f):new Color(.045f,.1f,.17f,.96f);
            if(sidelineChoice!=null)sidelineChoice.color=v.cameraView==MatchCameraView.SidelineBroadcast?new Color(.035f,.36f,.57f,.96f):new Color(.045f,.1f,.17f,.96f);
        }
        public void Low()=>preferences.SetGraphics(0);
        public void Medium()=>preferences.SetGraphics(1);
        public void High()=>preferences.SetGraphics(2);
        public void SmallControls()=>preferences.SetControlSize(0);
        public void NormalControls()=>preferences.SetControlSize(1);
        public void LargeControls()=>preferences.SetControlSize(2);
        public void Lengthwise()=>preferences.SetCameraView(0);
        public void Sideline()=>preferences.SetCameraView(1);
    }
}
