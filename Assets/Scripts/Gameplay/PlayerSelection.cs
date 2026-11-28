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
        private bool phaseInputLocked;
        public bool InputLocked => phaseInputLocked || (ball != null && ball.Match != null && ball.Match.IsPaused);
        // Selection lock is separate from movement suspension: the sprinter must still be controllable.
        public bool SwitchingLocked=>InputLocked||ball!=null&&ball.Match!=null&&ball.Match.IsPeriodSprint;
        public PrototypeTeam HumanTeam => humanTeam;
        private SwimmerIdentity lastFieldPlayer;
        [SerializeField] private PrototypeTeamTactics tacticalTeam;
        [SerializeField] private bool contextualSwitching = true;
        [SerializeField, Min(0f)] private float returnPenaltySeconds = 0.8f;
        [SerializeField, Min(0f)] private float manualProtectionSeconds = 1.05f;
        [SerializeField, Min(.1f)] private float looseBallSwitchInterval = .30f;
        [SerializeField, Min(.1f)] private float looseBallAdvantage = .60f;
        [SerializeField, Min(.1f)] private float looseBallMaxArrival = 1.60f;
        private SwimmerIdentity previousManualPlayer;
        private float lastManualSwitch = float.NegativeInfinity;
        private float nextRecoveryCheck, protectedUntil;
        private LineRenderer aimChevron;

        private bool ManualChoiceProtected => IsEligible(ActivePlayer) && Time.time < protectedUntil;

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
            if (ball == null && ActivePlayer != null) ball = ActivePlayer.Interaction.Ball;
            lastFieldPlayer = ActivePlayer;
            ApplySelection();
        }

        private void OnEnable()
        {
            if (ball != null) { ball.PossessionChanged += OnPossessionChanged; ball.Released += OnReleased; }
        }

        private void OnDisable()
        {
            if (ball != null) { ball.PossessionChanged -= OnPossessionChanged; ball.Released -= OnReleased; }
            if (aimChevron != null) aimChevron.enabled = false;
        }

        private void LateUpdate()
        {
            bool visible = ball != null && ball.Match != null && ball.Match.IsPlaying && !InputLocked &&
                ActivePlayer != null && ball.Owner == ActivePlayer.Interaction;
            if (!visible)
            {
                if (aimChevron != null) aimChevron.enabled = false;
                return;
            }
            if (aimChevron == null || aimChevron.transform.parent.parent != ActivePlayer.transform)
            {
                var marker = ActivePlayer.transform.Find("SelectionRing");
                if (marker == null || !marker.TryGetComponent<LineRenderer>(out var ring)) return;
                if (aimChevron == null)
                {
                    aimChevron = new GameObject("Possession Facing Chevron").AddComponent<LineRenderer>();
                    aimChevron.useWorldSpace = false;
                    aimChevron.positionCount = 3;
                    aimChevron.widthMultiplier = .046f;
                    aimChevron.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    aimChevron.receiveShadows = false;
                }
                aimChevron.gameObject.layer = marker.gameObject.layer;
                aimChevron.transform.SetParent(marker, false);
                aimChevron.sharedMaterial = ring.sharedMaterial;
                float radius = ring.GetPosition(0).magnitude;
                aimChevron.SetPosition(0, new Vector3(.14f, .012f, radius + .09f));
                aimChevron.SetPosition(1, new Vector3(0f, .012f, radius + .29f));
                aimChevron.SetPosition(2, new Vector3(-.14f, .012f, radius + .09f));
            }
            aimChevron.enabled = true;
        }

        private void OnReleased(SwimmerBallInteraction passer, BallReleaseKind kind)
        {
            if (SwitchingLocked || ManualChoiceProtected || (kind != BallReleaseKind.Pass && kind != BallReleaseKind.Lob) ||
                (ball.Match != null && !ball.Match.IsPlaying) || ball.IntendedReceiver == null ||
                passer.GetComponent<SwimmerIdentity>().Team != humanTeam) return;
            Select(ball.IntendedReceiver.GetComponent<SwimmerIdentity>());
        }

        public bool IsEligible(SwimmerIdentity player) => player != null && player.enabled && player.gameObject.activeInHierarchy &&
            player.EligibleForPlay && player.Team == humanTeam && roster != null && roster.Contains(player) &&
            (player.IsFieldPlayer || (ball != null && ball.Owner == player.Interaction));

        private void OnPossessionChanged(SwimmerBallInteraction previous, SwimmerBallInteraction next)
        {
            if (SwitchingLocked) return;
            SwimmerIdentity carrier = next != null ? next.GetComponent<SwimmerIdentity>() : null;
            if (IsEligible(carrier) && carrier != ActivePlayer && (!ManualChoiceProtected || !carrier.IsFieldPlayer))
                Select(carrier);
            else if (ActivePlayer != null && !ActivePlayer.IsFieldPlayer && !IsEligible(ActivePlayer)) SelectFieldFallback();
        }

        private void Update()
        {
            if (ball == null || ball.Match == null || !ball.Match.IsPlaying || ball.Match.AwaitingOpeningPossession ||
                SwitchingLocked || ManualChoiceProtected || Time.time < nextRecoveryCheck)
                return;
            nextRecoveryCheck = Time.time + looseBallSwitchInterval;

            bool hasSelection = IsEligible(ActivePlayer);
            if (ball.Owner != null)
            {
                OnPossessionChanged(null, ball.Owner);
                return;
            }
            if (!ball.CanRecoverLooseBall) return;

            Vector3 early = ball.PredictPosition(.30f);
            Vector3 late = ball.PredictPosition(.80f);
            if (hasSelection && ball.IsRecoveryCandidate(ActivePlayer.Interaction) &&
                Vector3.ProjectOnPlane(early - ActivePlayer.Motor.Position, Vector3.up).sqrMagnitude < 4f)
                return;
            float currentArrival = hasSelection && ball.IsRecoveryCandidate(ActivePlayer.Interaction)
                ? EstimateArrival(ActivePlayer, early, late) : float.PositiveInfinity;
            float best = looseBallMaxArrival;
            SwimmerIdentity target = null;
            foreach (var player in roster.FieldPlayers)
            {
                if (player == ActivePlayer || !IsEligible(player) || !ball.IsRecoveryCandidate(player.Interaction)) continue;
                float arrival = EstimateArrival(player, early, late);
                if (arrival >= best || arrival + looseBallAdvantage >= currentArrival) continue;
                target = player;
                best = arrival;
            }
            if (target != null) Select(target);
        }

        private static float EstimateArrival(SwimmerIdentity player, Vector3 near, Vector3 farther)
        {
            var motor = player.Motor;
            Vector3 firstGap = Vector3.ProjectOnPlane(near - motor.Position, Vector3.up);
            Vector3 target = Vector3.Lerp(near, farther, Mathf.InverseLerp(1.5f, 5f, firstGap.magnitude));
            Vector3 gap = Vector3.ProjectOnPlane(target - motor.Position, Vector3.up);
            float alignment = Vector3.Dot(motor.Facing, gap.normalized);
            float approach = Vector3.Dot(motor.Velocity, gap.normalized);
            float travel = Mathf.Max(0f, gap.magnitude - Mathf.Min(player.Interaction.PickupDistance, .85f));
            float speed = Mathf.Max(.6f, motor.MaxSpeed * .72f + Mathf.Clamp(approach * .20f, -.45f, .45f));
            return travel / speed + Mathf.InverseLerp(.75f, -1f, alignment) * .32f;
        }

        public bool Select(SwimmerIdentity player)
        {
            if(ball!=null&&ball.Match!=null&&ball.Match.IsPeriodSprint&&player!=ball.Match.SprintOff.HumanSprinter)return false;
            if (ball != null && ball.Match != null && ball.Match.Rules != null && !ball.Match.Rules.CanSelect(player)) return false;
            if (InputLocked || !IsEligible(player))
                return false;
            if (player == ActivePlayer) return true;
            var airborneReceiver = ball != null && ball.IsAirborne && ball.Delivery != PassDelivery.Wet
                ? ball.IntendedReceiver : null;
            if (ActivePlayer != null)
            {
                ball?.CancelHumanThrowFrom(ActivePlayer.Interaction);
                if (ActivePlayer.Interaction == airborneReceiver) ActivePlayer.Motor.ClearReceiveHandoffCommands();
                else ActivePlayer.Motor.StopMotion();
            }
            ActivePlayer = player;
            if (player.IsFieldPlayer) lastFieldPlayer = player;
            if (player.Interaction == airborneReceiver) player.Motor.ClearReceiveHandoffCommands();
            else player.Motor.StopMotion();
            ApplySelection(airborneReceiver);
            return true;
        }

        public void SelectNext()
        {
            if (SwitchingLocked) return;
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
                    SelectManually(best);
                    return;
                }
            }
            int index = -1;
            var fields = roster.FieldPlayers;
            for (int i = 0; i < fields.Count; i++) if (fields[i] == ActivePlayer || (ActivePlayer != null && !ActivePlayer.IsFieldPlayer && fields[i] == lastFieldPlayer)) index = i;
            for (int offset = 1; offset <= fields.Count; offset++)
            {
                var candidate = fields[(index + offset) % fields.Count];
                if (IsEligible(candidate)) { SelectManually(candidate); return; }
            }
        }

        private void SelectManually(SwimmerIdentity player)
        {
            var previous = ActivePlayer;
            if (!Select(player) || player == previous) return;
            previousManualPlayer = previous;
            lastManualSwitch = Time.time;
            protectedUntil = lastManualSwitch + manualProtectionSeconds;
        }

        private void SelectFieldFallback()
        {
            if (lastFieldPlayer != null && lastFieldPlayer.IsFieldPlayer && IsEligible(lastFieldPlayer)) { Select(lastFieldPlayer); return; }
            foreach (var field in roster.FieldPlayers) if (IsEligible(field)) { Select(field); return; }
        }

        public void RestorePossessionSelection()=>OnPossessionChanged(null,ball!=null?ball.Owner:null);

        public void RefreshEligibility()
        {
            if (!InputLocked && !IsEligible(ActivePlayer)) SelectFieldFallback();
            ApplySelection();
        }

        public void SetInputLocked(bool locked)
        {
            phaseInputLocked = locked;
            if (locked) protectedUntil = 0f;
            if (!locked && !IsEligible(ActivePlayer)) SelectFieldFallback();
            ApplySelection();
        }

        private void ApplySelection(SwimmerBallInteraction airborneReceiver = null)
        {
            var match = ball != null ? ball.Match : null;
            bool preserveMotion = match != null && (match.GoalRecoveryActive ||
                match.Rules != null && match.Rules.OrdinaryFreeThrow);
            foreach (SwimmerIdentity player in roster.Members)
            {
                bool controlled = !InputLocked && IsEligible(player) && player == ActivePlayer;
                SwimmerInput movement = player.GetComponent<SwimmerInput>();
                BallInteractionInput interaction = player.GetComponent<BallInteractionInput>();
                if (movement != null) movement.enabled = controlled;
                if (interaction != null) interaction.enabled = controlled;
                if (!controlled && player.Interaction != airborneReceiver && !preserveMotion) player.Motor.StopMotion();
                player.SetSelected(match?.Phase != MatchPhase.GoalRestart && IsEligible(player) && player == ActivePlayer);
            }
            if (gameplayCamera != null && ActivePlayer != null)
                gameplayCamera.SetTarget(ActivePlayer.Motor);
        }
    }
}
