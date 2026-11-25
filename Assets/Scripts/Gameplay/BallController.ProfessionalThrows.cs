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
        private float throwStart, throwDuration, gatherUntil;
        private bool gripSampled;

        private readonly struct PendingThrow
        {
            public readonly BallReleaseKind Kind;
            public readonly ShotType Shot;
            public readonly Vector3 Direction;
            public readonly float Power, FlightParameter, ReceiveWindow;
            public readonly SwimmerPassing Receiver;
            public readonly PassDelivery Delivery;

            public PendingThrow(BallReleaseKind kind, Vector3 direction, float power,
                SwimmerPassing receiver = null, float flightParameter = 0f, float receiveWindow = 0f,
                PassDelivery delivery = PassDelivery.Dry, ShotType shot = ShotType.Normal)
            {
                Kind = kind;
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
            (match == null || match.Phase == MatchPhase.Playing) &&
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

        private bool CanRequestThrow(SwimmerBallInteraction player, BallReleaseKind kind) =>
            !HasPendingThrow && CanPerformThrow(player, kind);

        private bool CanPerformThrow(SwimmerBallInteraction player, BallReleaseKind kind) =>
            !simulationPaused && State == BallState.Held && Owner == player &&
            Eligible(player) && CanRelease(player, kind);

        private void BeginGather(Vector3 from)
        {
            gatherLocalPoint = Owner.transform.InverseTransformPoint(from);
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
                action.Kind == BallReleaseKind.Pass && action.Power < 7.5f;
            EndDribble();
            preparation = action;
            throwStart = Mathf.Max(gatherUntil, Time.time);
            poseDirection = action.Direction;
            if (action.Receiver != null)
            {
                Vector3 aim = action.Receiver.Identity.Motor.Position - Owner.transform.position;
                aim.y = 0f;
                if (aim.sqrMagnitude > .01f) poseDirection = aim.normalized;
            }

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

        internal ThrowMotion GetThrowMotion(out float phase, out Vector3 aim)
        {
            aim = poseDirection;
            phase = HasPendingThrow ? Mathf.Clamp01((Time.time - throwStart) / throwDuration) : 0f;
            return HasPendingThrow && Time.time >= throwStart ? throwMotion : ThrowMotion.None;
        }

        internal float GetGatherProgress(out Vector3 point)
        {
            point = Owner != null ? Owner.transform.TransformPoint(gatherLocalPoint) : transform.position;
            return Mathf.Clamp01(1f - (gatherUntil - Time.time) / GatherSeconds);
        }

        internal bool CommitEvaluatedGrip(SwimmerBallInteraction player, Vector3 grip)
        {
            if (!ControlsEvaluatedGrip(player) || simulationPaused || (match != null && match.IsPaused))
                return false;

            evaluatedGrip = grip;
            gripSampled = true;
            float gather = GetGatherProgress(out Vector3 start);
            Vector3 point = gather >= 1f ? grip : Vector3.Lerp(start, grip, Mathf.SmoothStep(0f, 1f, gather));
            body.position = transform.position = point;
            lastCarrierPosition = player.transform.position;
            RollVisual();
            if (!HasPendingThrow || Time.time < throwStart + throwDuration)
                return false;

            PendingThrow action = preparation;
            // Clear before passing through the release path, which can synchronously notify rules and selection.
            CancelPendingThrow();
            if (!CanPerformThrow(player, action.Kind) ||
                (action.Receiver != null && !action.Receiver.CanReceiveFrom(player)))
                return false;

            Vector3 release = poolBounds.ClampPosition(grip, Radius);
            release.y = grip.y;
            IsEvaluatedRelease = true;
            try
            {
                switch (action.Kind)
                {
                    case BallReleaseKind.Shot:
                        LaunchShot(player, action.Direction, action.Power, action.Shot, release);
                        break;
                    case BallReleaseKind.Lob:
                        LaunchLob(player, action.Direction, action.Power, action.FlightParameter, action.Receiver, release);
                        break;
                    default:
                        LaunchDirectedPass(player, action.Direction, action.Power, action.Receiver, action.FlightParameter,
                            action.ReceiveWindow, action.Delivery, release);
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
            if (HasPendingThrow && (!UsesEvaluatedGrip || !CanPerformThrow(Owner, preparation.Kind)))
                CancelPendingThrow();
        }
    }
}
