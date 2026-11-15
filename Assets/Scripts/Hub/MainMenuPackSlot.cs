using System;
using TMPro;
using UnityEngine;

namespace ToduStudios.WaterPolo.Hub
{
    public enum MenuPackState { Empty, Waiting, Unlocking, Ready }
    /// <summary>Renders a detached existing pack record. All actions go back to HubView/HubProgression.</summary>
    public sealed class MainMenuPackSlot : MonoBehaviour
    {
        public UnityEngine.UI.Button button;
        public UnityEngine.UI.Image packImage;
        public TMP_Text status, action, emptyHint;
        public RoundedPanelGraphic panel;
        public string PackId { get; private set; }
        public MenuPackState State { get; private set; }
        public void Refresh(RewardPack pack, HubProgression service, MainMenuArt art)
        {
            PackId = pack?.instanceId;
            State = pack == null ? MenuPackState.Empty : pack.startUtc == 0 ? MenuPackState.Waiting : service.Remaining(pack) == 0 ? MenuPackState.Ready : MenuPackState.Unlocking;
            button.interactable = pack != null;
            var sprite = art.PackSprite(pack);
            packImage.sprite = sprite; packImage.gameObject.SetActive(sprite != null);
            emptyHint.gameObject.SetActive(pack == null || sprite == null);
            emptyHint.text = pack == null ? "EMPTY\nREWARD SLOT" : "REWARD\nPACK";
            status.text = State == MenuPackState.Empty ? "Earn in matches" : State == MenuPackState.Waiting ? "WAITING" : State == MenuPackState.Ready ? "READY TO OPEN" : TimeSpan.FromSeconds(service.Remaining(pack)).ToString(@"mm\:ss");
            action.text = State == MenuPackState.Waiting ? "UNLOCK" : State == MenuPackState.Ready ? "OPEN" : State == MenuPackState.Unlocking ? "UNLOCKING" : "";
            action.color = State == MenuPackState.Ready ? HubWidgets.Gold : Color.white;
            panel.color = State == MenuPackState.Empty ? new Color(.018f, .045f, .085f, .66f) : State == MenuPackState.Ready ? new Color(.075f, .22f, .28f, .97f) : new Color(.02f, .075f, .14f, .92f);
        }
    }
}
