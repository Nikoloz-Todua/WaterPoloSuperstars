using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Chooses human control, never ball ownership. Inactive bodies remain available to AI.</summary>
    [DefaultExecutionOrder(-60), DisallowMultipleComponent]
    public sealed class PlayerSelection : MonoBehaviour
    {
        [SerializeField] private PrototypeRoster roster;
        [SerializeField] private GameplayCamera gameplayCamera;
        public SwimmerIdentity ActivePlayer { get; private set; }
        public bool InputLocked { get; private set; }

        private void Awake()
        {
            if (roster == null || roster.Members.Count == 0)
            {
                Debug.LogError("PlayerSelection needs a populated roster.", this);
                enabled = false;
                return;
            }
            ActivePlayer = roster.Members[0];
            ApplySelection();
        }

        public bool Select(SwimmerIdentity player)
        {
            if (InputLocked || player == null || !roster.Contains(player) || !player.isActiveAndEnabled)
                return false;
            if (ActivePlayer != null) ActivePlayer.Motor.StopMotion();
            ActivePlayer = player;
            ActivePlayer.Motor.StopMotion();
            ApplySelection();
            return true;
        }

        public void SelectNext()
        {
            if (InputLocked) return;
            for (int i = 0; i < roster.Members.Count; i++)
                if (roster.Members[i] == ActivePlayer)
                {
                    Select(roster.Members[(i + 1) % roster.Members.Count]);
                    return;
                }
        }

        public void SetInputLocked(bool locked)
        {
            InputLocked = locked;
            ApplySelection();
        }

        private void ApplySelection()
        {
            foreach (SwimmerIdentity player in roster.Members)
            {
                bool controlled = !InputLocked && player == ActivePlayer;
                SwimmerInput movement = player.GetComponent<SwimmerInput>();
                BallInteractionInput interaction = player.GetComponent<BallInteractionInput>();
                if (movement != null) movement.enabled = controlled;
                if (interaction != null) interaction.enabled = controlled;
                if (!controlled) player.Motor.StopMotion();
                player.SetSelected(player == ActivePlayer);
            }
            if (gameplayCamera != null && ActivePlayer != null)
                gameplayCamera.SetTarget(ActivePlayer.Motor);
        }
    }
}
