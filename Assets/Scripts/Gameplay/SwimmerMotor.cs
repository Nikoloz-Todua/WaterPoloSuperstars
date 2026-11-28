using UnityEngine;

namespace ToduStudios.WaterPolo
{
    public interface ISwimmerMovementBounds { Vector3 ClampMovement(Vector3 position); }
    /// <summary>Physics movement only. Human input and future AI supply the same direction.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class SwimmerMotor : MonoBehaviour
    {
        [SerializeField] private PoolBounds poolBounds;
        [SerializeField, Min(0.1f)] private float maxSpeed = 4f;
        [SerializeField, Min(0.1f)] private float acceleration = 10f;
        [SerializeField, Min(0.1f)] private float deceleration = 14f;
        [SerializeField, Min(1f)] private float turnSpeed = 360f;
        [SerializeField, Min(1f)] private float turnAcceleration = 1550f;
        [SerializeField, Range(.2f, 1f)] private float reversalSpeedFraction = .55f;
        [SerializeField, Range(.2f, 1f)] private float retreatSpeedFraction = .54f;
        [SerializeField, Range(0f, 1f)] private float retreatEnterDot = .66f;
        [SerializeField, Range(0f, 1f)] private float retreatExitDot = .32f;
        [SerializeField, Range(.25f, .85f)] private float protectedCarrySpeedFraction = .58f;
        [Tooltip("Conservative horizontal clearance, in metres, for the entire turning swimmer.")]
        [SerializeField, Min(0f)] private float boundaryClearance = 1.1f;

        private Rigidbody body;
        private float swimRadius;
        private Vector2 moveInput;
        private Vector3 velocity;
        private Vector3 aimDirection;
        private Vector3 controlAim;
        private Vector3 retreatDirection;
        private bool explicitFacing;
        private float keepRaisedUntil;
        internal float ProtectedCarry { get; private set; }
        internal bool CanLeadBall { get; private set; }
        internal bool BreaksBallLeading { get; private set; }
        internal float ProtectedCarrySpeed => SwimSpeed * protectedCarrySpeedFraction;
        internal bool UsesProtectedCarry => identity != null && interaction != null && identity.IsFieldPlayer && interaction.HasBall &&
            interaction.Ball.ProfessionalThrows && interaction.Visual != null && interaction.Visual.SupportsEvaluatedGrip;
        public Vector3 ControlAim => controlAim.sqrMagnitude > .5f ? controlAim : Facing;
        private ISwimmerMovementBounds movementBounds;
        private float speedOverride = -1f;
        private SwimmerStamina stamina;
        private SwimmerIdentity identity;
        private SwimmerBallInteraction interaction;
        private PrototypeRoster contactRoster;
        private bool directionalInput;
        private float angularSpeed;
        private bool leavingPeriodLine;
        private float periodAcceleration=1f;
        public void SetPeriodAcceleration(float value)=>periodAcceleration=Mathf.Clamp(value,1f,1.5f);
        public void StageOnGoalLine(Vector3 point,Quaternion rotation)
        {
            if(body==null)body=GetComponent<Rigidbody>();
            StopMotion();leavingPeriodLine=true;body.position=point;body.rotation=rotation;
        }
        public void OverrideSpeed(float speed) => speedOverride = Mathf.Max(0.1f, speed);
        public void ClearSpeedOverride() => speedOverride = -1f;
        public void SetMovementBounds(ISwimmerMovementBounds bounds) => movementBounds = bounds;
        private Vector3 ClampMovement(Vector3 position, bool protectRestart = true)
        {
            if(leavingPeriodLine)
            {
                if(Mathf.Abs(position.z-poolBounds.transform.position.z)>poolBounds.Length*.5f-boundaryClearance)return poolBounds.ClampPosition(position,0f);
                leavingPeriodLine=false;
            }
            Vector3 bounded = movementBounds != null ? movementBounds.ClampMovement(position) : position;
            if (poolBounds.HasPhysicalBasin)
                bounded = poolBounds.ClampSwimmer(body.position, bounded, swimRadius);
            else if (movementBounds == null)
                bounded = poolBounds.ClampPosition(bounded, boundaryClearance);
            return protectRestart && identity != null && identity.Rules != null
                ? identity.Rules.ProtectOrdinaryRestart(identity, bounded, body.position) : bounded;
        }

        public Vector3 Velocity => velocity;
        internal float SwimRadius => swimRadius;
        private float SwimSpeed => speedOverride > 0f ? speedOverride : maxSpeed;
        public float MaxSpeed => SwimSpeed * (stamina != null ? stamina.SpeedMultiplier : 1f);
        public Vector3 Position => body != null ? body.position : transform.position;
        public Vector3 Facing => body!=null?body.rotation*Vector3.forward:transform.forward;
        internal Vector3 RenderedFacing => Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        public bool IsRetreating { get; private set; }
        internal bool HasManualSwim => directionalInput && moveInput.sqrMagnitude > .035f;

        public void StopMotion()
        {
            moveInput = Vector2.zero;
            velocity = Vector3.zero;
            aimDirection = Vector3.zero;
            controlAim = Vector3.zero;
            angularSpeed = 0f;
            ResetCarryMotion();
            ClearSpeedOverride();
            periodAcceleration=1f;
            if(stamina!=null)stamina.StopSprint();
        }

        internal void ClearReceiveHandoffCommands()
        {
            SetMoveInput(Vector2.zero);
            ClearFacingDirection();
            ClearControlAim();
            stamina?.StopSprint();
        }

        public void ResetPose(Vector3 position, Quaternion rotation)
        {
            leavingPeriodLine=false;
            if (body == null) body = GetComponent<Rigidbody>();
            moveInput = Vector2.zero;
            velocity = Vector3.zero;
            aimDirection = Vector3.zero;
            controlAim = Vector3.zero;
            angularSpeed = 0f;
            ResetCarryMotion();
            ClearSpeedOverride();
            body.position = ClampMovement(position, false);
            body.rotation = rotation;
        }

        public void SetMoveInput(Vector2 direction, bool usePossessionFacing = false, bool explicitAim = false)
        {
            // Preserve analogue stick strength while limiting keyboard diagonals to unit length.
            moveInput = Vector2.ClampMagnitude(direction, 1f);
            directionalInput = usePossessionFacing;
            explicitFacing = explicitAim;
        }

        public void SetFacingDirection(Vector3 direction)
        {
            direction.y = 0f;
            aimDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
        }
        public void ClearFacingDirection() => aimDirection = Vector3.zero;
        internal void SetControlAim(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > .035f) controlAim = direction.normalized;
        }
        internal void ClearControlAim() => controlAim = Vector3.zero;

        internal bool IsRetreatInput(Vector3 input)
        {
            if (input.sqrMagnitude <= .04f) return false;
            Vector3 direction = input.normalized;
            return IsRetreating && Vector3.Dot(direction, retreatDirection) > .94f ||
                Vector3.Dot(direction, Facing) < -(IsRetreating ? Mathf.Min(retreatExitDot, retreatEnterDot) : retreatEnterDot);
        }

        private void ResetCarryMotion()
        {
            IsRetreating = explicitFacing = CanLeadBall = BreaksBallLeading = false;
            retreatDirection = Vector3.zero;
            ProtectedCarry = keepRaisedUntil = 0f;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            var capsule = GetComponent<CapsuleCollider>();
            swimRadius = capsule.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
            stamina = GetComponent<SwimmerStamina>();
            identity = GetComponent<SwimmerIdentity>();
            interaction = GetComponent<SwimmerBallInteraction>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            if (poolBounds == null)
            {
                Debug.LogError("SwimmerMotor requires an assigned PoolBounds.", this);
                enabled = false;
                return;
            }

            body.position = ClampMovement(body.position);
        }

        internal void UseRosterContact(PrototypeRoster owner) => contactRoster = owner;
        internal Vector3 ClampContactPosition(Vector3 position) => ClampMovement(position);
        internal bool UsesProneContact => identity.IsFieldPlayer && !IsRetreating && moveInput.sqrMagnitude > .20f &&
            (!interaction.HasBall || interaction.Ball.IsDribbling);

        private void FixedUpdate()
        {
            if (contactRoster != null && contactRoster.isActiveAndEnabled) return;
            if (PlanMovement(out Vector3 next)) CommitMovement(next);
        }

        internal bool PlanMovement(out Vector3 next)
        {
            next = body.position;
            if (identity != null && !identity.EligibleForPlay) { StopMotion(); return false; }
            if (identity != null && identity.Rules != null && identity.Rules.HoldOutsideAtHorn(identity)) { StopMotion(); return false; }
            if (identity != null && identity.Rules != null && !identity.Rules.CanMove(identity))
            {
                if (moveInput.sqrMagnitude > 0.01f && identity.Rules.IsRulePhase && identity.Rules.Taker == identity)
                    aimDirection = new Vector3(moveInput.x, 0f, moveInput.y);
                moveInput = Vector2.zero; velocity = Vector3.zero; if(stamina!=null)stamina.StopSprint();
            }
            float step = Time.fixedDeltaTime;
            if(stamina!=null)stamina.Simulate(moveInput.sqrMagnitude>0.01f,step);
            Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);
            bool live = interaction != null && interaction.Ball != null && interaction.Ball.Match != null &&
                interaction.Ball.Match.IsPlaying;
            BallController.PassReception reception = default;
            bool receiving = live && identity.IsFieldPlayer && !HasManualSwim &&
                interaction.Ball.TryReadReception(identity, out reception);
            if (receiving)
            {
                Vector3 adjustment = PrototypeTeamTactics.Planar(reception.Centre - body.position);
                // Check the stroke first. Only small launch-pocket corrections should compete with the turn.
                float aligned = Mathf.InverseLerp(-.3f, .55f, Vector3.Dot(Facing, reception.Facing));
                input = Vector3.ClampMagnitude(adjustment * .85f, .36f) * aligned;
            }
            bool sprinting = stamina != null && stamina.IsSprinting;
            Vector3 forward = Facing;
            Vector3 throwFacing = Vector3.zero;
            bool throwing = (live || identity != null && identity.Rules != null && identity.Rules.OrdinaryFreeThrow) &&
                interaction.Ball.TryGetThrowFacing(interaction, out throwFacing);
            bool wasRetreating = IsRetreating;
            IsRetreating = live && directionalInput && (interaction.HasBall || wasRetreating && sprinting) &&
                IsRetreatInput(input);
            if (IsRetreating && (!wasRetreating || Vector3.Dot(input.normalized, retreatDirection) <= .94f))
                retreatDirection = input.normalized;
            if (IsRetreating && sprinting && interaction.HasBall && (!explicitFacing || wasRetreating))
                interaction.Ball.TryReleaseForRetreat(interaction);

            bool protectedCarry = live && UsesProtectedCarry;
            bool moving = input.sqrMagnitude > .035f;
            float alignment = moving ? Vector3.Dot(input.normalized, forward) : 1f;
            CanLeadBall = live && !IsRetreating && !explicitFacing && !throwing && moving && alignment >= .86f;
            BreaksBallLeading = explicitFacing || moving && alignment < .72f;
            float speed = MaxSpeed * (IsRetreating ? (sprinting ? .80f : retreatSpeedFraction) : 1f);
            if (protectedCarry)
            {
                bool turning = aimDirection.sqrMagnitude > .5f && Vector3.Dot(forward, aimDirection) < .9986f;
                if (moving || turning || throwing) keepRaisedUntil = Time.time + .13f;
                bool raised = Time.time < keepRaisedUntil || velocity.sqrMagnitude > .09f;
                ProtectedCarry = Mathf.MoveTowards(ProtectedCarry, raised ? 1f : 0f, step * (raised ? 7f : 4.5f));
                if (!interaction.Ball.IsDribbling)
                {
                    // Gather before propulsion. Keep physical contact displacement independent of input.
                    float propulsion = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.55f, .90f, ProtectedCarry));
                    speed = (IsRetreating ? SwimSpeed * retreatSpeedFraction : ProtectedCarrySpeed) * propulsion;
                    if (!moving) input = Vector3.zero;
                }
            }
            else ProtectedCarry = keepRaisedUntil = 0f;

            Vector3 targetVelocity = input * speed;
            bool accelerating = protectedCarry || receiving ? targetVelocity.sqrMagnitude > .0001f : moveInput.sqrMagnitude > 0f;
            float changeRate = accelerating ? acceleration : deceleration;
            if(stamina!=null)changeRate*=stamina.AccelerationMultiplier;
            changeRate*=periodAcceleration;
            if (live && velocity.sqrMagnitude > .16f && targetVelocity.sqrMagnitude > .01f)
            {
                float heading = Vector3.SignedAngle(velocity, targetVelocity, Vector3.up);
                float sharpTurn = Mathf.InverseLerp(38f, 135f, Mathf.Abs(heading));
                float glideRate = turnSpeed * (sprinting ? .62f : .84f);
                Vector3 guided = Quaternion.AngleAxis(Mathf.Clamp(heading, -glideRate * step, glideRate * step),
                    Vector3.up) * velocity.normalized;
                float guidedSpeed = Mathf.MoveTowards(velocity.magnitude,
                    targetVelocity.magnitude * Mathf.Lerp(1f, reversalSpeedFraction, sharpTurn), changeRate * step);
                velocity = guided * guidedSpeed;
            }
            else velocity = Vector3.MoveTowards(velocity, targetVelocity, changeRate * step);

            Vector3 previous = body.position;
            next = ClampMovement(previous + velocity * step);

            Vector3 turnDirection = receiving ? reception.Facing : throwing ? throwFacing : aimDirection.sqrMagnitude > 0f ? aimDirection : IsRetreating ? forward :
                Vector3.ProjectOnPlane((next - previous) / step, Vector3.up);
            if (turnDirection.sqrMagnitude > 0.0001f)
            {
                if (live)
                {
                    float angle = Vector3.SignedAngle(forward, turnDirection, Vector3.up);
                    float maximum = turnSpeed * (receiving || throwing ? 1.55f : sprinting && !IsRetreating ? .78f : 1f);
                    float turnRate = turnAcceleration * (receiving || throwing ? 1.70f : 1f);
                    float target = Mathf.Sign(angle) * Mathf.Min(maximum,
                        Mathf.Sqrt(2f * turnRate * Mathf.Abs(angle)));
                    angularSpeed = Mathf.MoveTowards(angularSpeed, target, turnRate * step);
                    float turn = angularSpeed * step;
                    if (turn * angle >= 0f && Mathf.Abs(turn) > Mathf.Abs(angle))
                    {
                        turn = angle;
                        angularSpeed = 0f;
                    }
                    body.MoveRotation(Quaternion.AngleAxis(turn, Vector3.up) * body.rotation);
                }
                else
                {
                    Quaternion facing = Quaternion.LookRotation(turnDirection, Vector3.up);
                    body.MoveRotation(Quaternion.RotateTowards(body.rotation, facing, turnSpeed * step));
                }
            }
            else angularSpeed = 0f;
            return true;
        }

        internal void CommitMovement(Vector3 next)
        {
            // Motion consumers see the translation committed after pool bounds and player contact.
            velocity = Vector3.ProjectOnPlane((next - body.position) / Time.fixedDeltaTime, Vector3.up);
            body.MovePosition(next);
        }

        private void OnDisable()
        {
            if(stamina!=null)stamina.StopSprint();
            moveInput = Vector2.zero;
            velocity = Vector3.zero;
            aimDirection = Vector3.zero;
            angularSpeed = 0f;
            ResetCarryMotion();
            ClearSpeedOverride();
        }
    }
}
