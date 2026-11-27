using UnityEngine;

namespace ToduStudios.WaterPolo
{
    internal enum ThrowMotion { None, QuickShot, Shot, CompactPass, LongPass, Lob }

    public sealed partial class BallController
    {
        [Header("Professional live throws")]
        [SerializeField] private bool professionalThrows;

        private const float GatherSeconds = .13f;
        private PendingThrow preparation;
        private ThrowMotion throwMotion;
        private Vector3 poseDirection, gatherLocalPoint, evaluatedGrip;
        private Vector3 gatherArrival;
        private float throwStart, throwDuration, gatherUntil;
        private float shotLean, passFacingDeadline;
        private SwimmerMotor facingMotor;
        private bool AligningHumanPass => preparation.Kind == BallReleaseKind.Pass &&
            preparation.Source == BallActionSource.HumanInput && match != null && match.IsPlaying;
        private float PassFacingError => Vector3.Angle(facingMotor.RenderedFacing, poseDirection);
        private bool gripSampled;

        private readonly struct PendingThrow
        {
            public readonly BallReleaseKind Kind;
            public readonly BallActionSource Source;
            public readonly ShotType Shot;
            public readonly Vector3 Direction;
            public readonly float Power, FlightParameter, ReceiveWindow;
            public readonly SwimmerPassing Receiver;
            public readonly PassDelivery Delivery;

            public PendingThrow(BallReleaseKind kind, BallActionSource source, Vector3 direction, float power,
                SwimmerPassing receiver = null, float flightParameter = 0f, float receiveWindow = 0f,
                PassDelivery delivery = PassDelivery.Dry, ShotType shot = ShotType.Normal)
            {
                Kind = kind;
                Source = source;
                Direction = direction;
                Power = power;
                Receiver = receiver;
                FlightParameter = flightParameter;
                ReceiveWindow = receiveWindow;
                Delivery = delivery;
                Shot = shot;
            }
        }

        public bool ProfessionalThrows => professionalThrows;
        public bool HasPendingThrow => preparation.Kind != BallReleaseKind.None;
        public bool IsGathering => UsesEvaluatedGrip && Time.time < gatherUntil;
        public bool IsEvaluatedRelease { get; private set; }

        private bool UsesEvaluatedGrip => professionalThrows && Owner != null &&
            (match == null || match.Phase == MatchPhase.Playing ||
                match.Rules != null && match.Rules.OrdinaryFreeThrow && match.Rules.Taker.Interaction == Owner) &&
            carrierVisual != null && carrierVisual.SupportsEvaluatedGrip;
        private bool CanScheduleThrow => UsesEvaluatedGrip;

        internal bool ControlsEvaluatedGrip(SwimmerBallInteraction player) =>
            UsesEvaluatedGrip && Owner == player && !IsDribbling;

        internal void CommitEvaluatedDribble(SwimmerBallInteraction player)
        {
            if (!UsesEvaluatedGrip || Owner != player || !IsDribbling) return;
            FollowCarryTarget();
            RollVisual();
        }

        internal bool TryReadEvaluatedGrip(SwimmerBallInteraction player, out Vector3 grip)
        {
            grip = evaluatedGrip;
            return gripSampled && ControlsEvaluatedGrip(player);
        }

        private bool CanRequestThrow(SwimmerBallInteraction player, BallReleaseKind kind, BallActionSource source) =>
            !HasPendingThrow && CanPerformThrow(player, kind, source);

        private bool CanPerformThrow(SwimmerBallInteraction player, BallReleaseKind kind, BallActionSource source) =>
            !simulationPaused && State == BallState.Held && Owner == player &&
            Eligible(player) && CanRelease(player, kind) && HasThrowAuthority(player, source);

        private bool HasThrowAuthority(SwimmerBallInteraction player, BallActionSource source)
        {
#if UNITY_EDITOR
            if (source == BallActionSource.Verification)
                return !Hub.HubMatchBridge.Active;
#endif
            var selection = match != null ? match.Selection : null;
            if (selection == null)
                return source == BallActionSource.HumanInput || source == BallActionSource.TacticalAI ||
                    source == BallActionSource.VoluntaryRelease;
            var identity = player.GetComponent<SwimmerIdentity>();
            if (identity == null) return false;
            bool humanTeam = identity.Team == selection.HumanTeam;
            switch (source)
            {
                case BallActionSource.HumanInput:
                case BallActionSource.VoluntaryRelease:
                    return humanTeam && !selection.InputLocked && selection.ActivePlayer == identity;
                case BallActionSource.TacticalAI:
                    return !humanTeam && match.IsPlaying;
                case BallActionSource.RuleRestart:
                    return !humanTeam && (match.IsCentreRestart ||
                        match.Rules != null && match.Rules.IsRulePhase && match.Rules.Taker == identity);
                default:
                    return false;
            }
        }

        internal void CancelHumanThrowFrom(SwimmerBallInteraction player)
        {
            if (Owner == player && preparation.Source == BallActionSource.HumanInput)
                CancelPendingThrow();
        }

        private void BeginGather(Vector3 from, Vector3 arrival = default)
        {
            gatherLocalPoint = Owner.transform.InverseTransformPoint(from);
            gatherArrival = from.y > SurfaceHeight + .12f ? arrival : Vector3.zero;
            gatherUntil = Time.time + GatherSeconds;
            gripSampled = false;
        }

        private void EndDribble()
        {
            if (IsDribbling && UsesEvaluatedGrip)
                BeginGather(transform.position);
            IsDribbling = false;
            swimmingTime = 0f;
        }

        private void QueueThrow(PendingThrow action)
        {
            bool compact = IsDribbling || IsGathering ||
                action.Kind == BallReleaseKind.Pass && action.Power < 7.5f ||
                action.Kind == BallReleaseKind.Shot && action.Source == BallActionSource.HumanInput && action.Power < 13f;
            EndDribble();
            preparation = action;
            throwStart = Mathf.Max(gatherUntil, Time.time);
            poseDirection = action.Direction;
            if (action.Receiver != null && !AligningHumanPass)
            {
                Vector3 aim = action.Receiver.Identity.Motor.Position - Owner.transform.position;
                aim.y = 0f;
                if (aim.sqrMagnitude > .01f) poseDirection = aim.normalized;
            }
            var motor = Owner.GetComponent<SwimmerMotor>();
            facingMotor = motor;
            passFacingDeadline = Time.time + .85f;
            float signedYaw = Vector3.SignedAngle(motor.Facing, poseDirection, Vector3.up);
            float yaw = Mathf.Abs(signedYaw);
            shotLean = Mathf.Clamp(signedYaw / 46f + Vector3.Dot(motor.Velocity,
                Vector3.Cross(Vector3.up, motor.Facing)) * .18f, -1f, 1f);
            if (action.Source == BallActionSource.HumanInput && !AligningHumanPass && yaw > 55f)
                throwStart = Mathf.Max(throwStart, Time.time + (yaw - 48f) / 560f);

            switch (action.Kind)
            {
                case BallReleaseKind.Shot:
                    throwMotion = compact ? ThrowMotion.QuickShot : ThrowMotion.Shot;
                    throwDuration = compact ? .14f : .23f;
                    break;
                case BallReleaseKind.Lob:
                    throwMotion = ThrowMotion.Lob;
                    throwDuration = .20f;
                    break;
                default:
                    throwMotion = compact ? ThrowMotion.CompactPass : ThrowMotion.LongPass;
                    throwDuration = compact ? .12f : .18f;
                    break;
            }
        }

        internal bool TryGetThrowFacing(SwimmerBallInteraction player, out Vector3 facing)
        {
            facing = poseDirection;
            return HasPendingThrow && Owner == player && preparation.Source == BallActionSource.HumanInput;
        }

        internal float ShotRise(SwimmerBallInteraction player, out float lean, out float phase)
        {
            lean = phase = 0f;
            if (Owner != player || preparation.Kind != BallReleaseKind.Shot) return 0f;
            phase = Mathf.Clamp01((Time.time - throwStart) / throwDuration);
            lean = shotLean;
            float effort = Mathf.InverseLerp(10f, 22f, preparation.Power);
            float peak = throwMotion == ThrowMotion.QuickShot ? Mathf.Lerp(.19f, .32f, effort)
                : Mathf.Lerp(.32f, .60f, effort);
            return peak * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.08f, .87f, phase));
        }

        internal ThrowMotion GetThrowMotion(out float phase, out Vector3 aim)
        {
            aim = poseDirection;
            if (AligningHumanPass && PassFacingError > 24f)
                throwStart = Mathf.Max(throwStart, Time.time + .001f);
            phase = HasPendingThrow ? Mathf.Clamp01((Time.time - throwStart) / throwDuration) : 0f;
            return HasPendingThrow && Time.time >= throwStart ? throwMotion : ThrowMotion.None;
        }

        internal bool TryReadShotThreat(out SwimmerBallInteraction shooter, out Vector3 origin, out Vector3 direction,
            out int sequence)
        {
            if (preparation.Kind == BallReleaseKind.Shot && Time.time >= throwStart + throwDuration * .32f)
            {
                shooter = Owner;
                origin = PhysicsPosition;
                direction = preparation.Direction;
                sequence = ShotSequence + 1;
                return shooter != null;
            }
            shooter = LastReleaser;
            origin = ShotOrigin;
            direction = Velocity;
            sequence = ShotSequence;
            return Owner == null && shooter != null && IsScoringShot && ReleaseKind == BallReleaseKind.Shot &&
                Time.time <= ReleaseStartedAt + .22f && direction.sqrMagnitude > 16f;
        }

        internal float GetGatherProgress(out Vector3 point, out Vector3 incoming)
        {
            point = Owner != null ? Owner.transform.TransformPoint(gatherLocalPoint) : transform.position;
            incoming = IsGathering ? gatherArrival : Vector3.zero;
            float progress = Mathf.Clamp01(1f - (gatherUntil - Time.time) / GatherSeconds);
            if (incoming.sqrMagnitude > .01f)
            {
                // Let the last part of the incoming motion carry into the absorbing arm.
                // CommitEvaluatedGrip moves the same physical ball along this bounded path.
                Vector3 contactTravel = Vector3.ClampMagnitude(incoming * Mathf.Min(.027f, progress * GatherSeconds), .24f);
                point += contactTravel * (1f - Mathf.SmoothStep(0f, 1f, progress));
            }
            return progress;
        }

        internal bool CommitEvaluatedGrip(SwimmerBallInteraction player, Vector3 grip)
        {
            if (!ControlsEvaluatedGrip(player) || simulationPaused || (match != null && match.IsPaused))
                return false;

            evaluatedGrip = grip;
            gripSampled = true;
            float gather = GetGatherProgress(out Vector3 start, out _);
            Vector3 point = gather >= 1f || (gatherArrival.sqrMagnitude > .01f && (grip - start).sqrMagnitude < .0049f)
                ? grip : Vector3.Lerp(start, grip, Mathf.SmoothStep(0f, 1f, gather));
            body.position = transform.position = point;
            lastCarrierPosition = player.transform.position;
            RollVisual();
            if (!HasPendingThrow || Time.time < throwStart + throwDuration ||
                AligningHumanPass && PassFacingError > 8f)
                return false;

            PendingThrow action = preparation;
            bool directionalPass = AligningHumanPass;
            // Clear before passing through the release path, which can synchronously notify rules and selection.
            CancelPendingThrow();
            if (!CanPerformThrow(player, action.Kind, action.Source) ||
                (!directionalPass && action.Receiver != null && !action.Receiver.CanReceiveFrom(player)))
                return false;

            Vector3 release = poolBounds.ClampPosition(grip, Radius);
            release.y = grip.y;
            IsEvaluatedRelease = true;
            try
            {
                switch (action.Kind)
                {
                    case BallReleaseKind.Shot:
                        LaunchShot(player, action.Direction, action.Power, action.Shot, release, action.Source);
                        break;
                    case BallReleaseKind.Lob:
                        LaunchLob(player, action.Direction, action.Power, action.FlightParameter, action.Receiver, release, action.Source);
                        break;
                    default:
                        LaunchDirectedPass(player, action.Direction, action.Power, action.Receiver, action.FlightParameter,
                            action.ReceiveWindow, action.Delivery, release, action.Source);
                        break;
                }
            }
            finally { IsEvaluatedRelease = false; }
            return true;
        }

        private void CancelPendingThrow() => preparation = default;

        private void CancelThrowOnPause(bool paused)
        {
            if (paused) CancelPendingThrow();
        }

        private void Update()
        {
            if (HasPendingThrow && (!UsesEvaluatedGrip || !CanPerformThrow(Owner, preparation.Kind, preparation.Source) ||
                AligningHumanPass && Time.time > passFacingDeadline))
                CancelPendingThrow();
        }
    }
}
