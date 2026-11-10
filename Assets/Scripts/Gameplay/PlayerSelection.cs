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
        private SwimmerIdentity lastFieldPlayer;
        [SerializeField] private PrototypeTeamTactics tacticalTeam;
        [SerializeField] private bool contextualSwitching = true;
        [SerializeField, Min(0f)] private float returnPenaltySeconds = 0.8f;
        private SwimmerIdentity previousManualPlayer;
        private float lastManualSwitch = float.NegativeInfinity;

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
            lastFieldPlayer = ActivePlayer;
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
            player.EligibleForPlay && player.Team == humanTeam && roster != null && roster.Contains(player) &&
            (player.IsFieldPlayer || (ball != null && ball.Owner == player.Interaction));

        private void OnPossessionChanged(SwimmerBallInteraction previous, SwimmerBallInteraction next)
        {
            if (InputLocked) return;
            SwimmerIdentity carrier = next != null ? next.GetComponent<SwimmerIdentity>() : null;
            if (IsEligible(carrier) && carrier != ActivePlayer) Select(carrier);
            else if (ActivePlayer != null && !ActivePlayer.IsFieldPlayer && !IsEligible(ActivePlayer)) SelectFieldFallback();
        }

        public bool Select(SwimmerIdentity player)
        {
            if (ball != null && ball.Match != null && ball.Match.Rules != null && !ball.Match.Rules.CanSelect(player)) return false;
            if (InputLocked || !IsEligible(player))
                return false;
            if (player == ActivePlayer) return true;
            if (ActivePlayer != null) ActivePlayer.Motor.StopMotion();
            ActivePlayer = player;
            if (player.IsFieldPlayer) lastFieldPlayer = player;
            ActivePlayer.Motor.StopMotion();
            ApplySelection();
            return true;
        }

        public void SelectNext()
        {
            if (InputLocked) return;
            if (contextualSwitching && tacticalTeam != null)
            {
                SwimmerIdentity best = null;
                float bestScore = float.NegativeInfinity;
                foreach (var candidate in roster.FieldPlayers)
                {
                    if (candidate == ActivePlayer || !IsEligible(candidate)) continue;
                    float score = ContextualSelection.Score(candidate, tacticalTeam);
                    if (candidate == previousManualPlayer && Time.time - lastManualSwitch < returnPenaltySeconds) score -= 1.5f;
                    if (score > bestScore + 0.0001f || (Mathf.Abs(score - bestScore) <= 0.0001f && best != null &&
                        string.CompareOrdinal(candidate.PlayerId, best.PlayerId) < 0)) { best = candidate; bestScore = score; }
                }
                if (best != null)
                {
                    var previous = ActivePlayer;
                    if (Select(best)) { previousManualPlayer = previous; lastManualSwitch = Time.time; }
                    return;
                }
            }
            int index = -1;
            var fields = roster.FieldPlayers;
            for (int i = 0; i < fields.Count; i++) if (fields[i] == ActivePlayer || (ActivePlayer != null && !ActivePlayer.IsFieldPlayer && fields[i] == lastFieldPlayer)) index = i;
            for (int offset = 1; offset <= fields.Count; offset++)
            {
                var candidate = fields[(index + offset) % fields.Count];
                if (IsEligible(candidate)) { Select(candidate); return; }
            }
        }

        private void SelectFieldFallback()
        {
            if (lastFieldPlayer != null && lastFieldPlayer.IsFieldPlayer && IsEligible(lastFieldPlayer)) { Select(lastFieldPlayer); return; }
            foreach (var field in roster.FieldPlayers) if (IsEligible(field)) { Select(field); return; }
        }

        public void RefreshEligibility()
        {
            if (!InputLocked && !IsEligible(ActivePlayer)) SelectFieldFallback();
            ApplySelection();
        }

        public void SetInputLocked(bool locked)
        {
            InputLocked = locked;
            if (!locked && !IsEligible(ActivePlayer)) SelectFieldFallback();
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
