using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Chooses human control, never ball ownership. Inactive bodies remain available to AI.</summary>
    [DefaultExecutionOrder(-60), DisallowMultipleComponent]
    public sealed class PlayerSelection : MonoBehaviour
    {
        [SerializeField] private PrototypeRoster roster;
        [SerializeField] private GameplayCamera gameplayCamera;
        [SerializeField] private BallController ball;
        [SerializeField] private PrototypeTeam humanTeam = PrototypeTeam.Blue;
        public SwimmerIdentity ActivePlayer { get; private set; }
        public bool InputLocked { get; private set; }
        public PrototypeTeam HumanTeam => humanTeam;

        private void Awake()
        {
            if (roster == null || roster.Members.Count == 0)
            {
                Debug.LogError("PlayerSelection needs a populated roster.", this);
                enabled = false;
                return;
            }
            foreach (SwimmerIdentity player in roster.Members)
                if (IsEligible(player)) { ActivePlayer = player; break; }
            if (ActivePlayer == null)
            {
                Debug.LogError("PlayerSelection needs an eligible friendly swimmer.", this);
                enabled = false;
                return;
            }
            if (ball == null) ball = ActivePlayer.Interaction.Ball;
            ApplySelection();
        }

        private void OnEnable()
        {
            if (ball != null) ball.PossessionChanged += OnPossessionChanged;
        }

        private void OnDisable()
        {
            if (ball != null) ball.PossessionChanged -= OnPossessionChanged;
        }

        public bool IsEligible(SwimmerIdentity player) => player != null && player.enabled && player.gameObject.activeInHierarchy &&
            player.IsFieldPlayer && player.Team == humanTeam && roster != null && roster.Contains(player);

        private void OnPossessionChanged(SwimmerBallInteraction previous, SwimmerBallInteraction next)
        {
            if (InputLocked || next == null) return;
            SwimmerIdentity carrier = next.GetComponent<SwimmerIdentity>();
            if (IsEligible(carrier) && carrier != ActivePlayer) Select(carrier);
        }

        public bool Select(SwimmerIdentity player)
        {
            if (InputLocked || !IsEligible(player))
                return false;
            if (player == ActivePlayer) return true;
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
                    for (int offset = 1; offset <= roster.Members.Count; offset++)
                    {
                        SwimmerIdentity candidate = roster.Members[(i + offset) % roster.Members.Count];
                        if (IsEligible(candidate)) { Select(candidate); return; }
                    }
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
                bool controlled = !InputLocked && IsEligible(player) && player == ActivePlayer;
                SwimmerInput movement = player.GetComponent<SwimmerInput>();
                BallInteractionInput interaction = player.GetComponent<BallInteractionInput>();
                if (movement != null) movement.enabled = controlled;
                if (interaction != null) interaction.enabled = controlled;
                if (!controlled) player.Motor.StopMotion();
                player.SetSelected(IsEligible(player) && player == ActivePlayer);
            }
            if (gameplayCamera != null && ActivePlayer != null)
                gameplayCamera.SetTarget(ActivePlayer.Motor);
        }
    }
}
