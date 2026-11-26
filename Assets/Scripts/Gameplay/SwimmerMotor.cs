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
        [Tooltip("Conservative horizontal clearance, in metres, for the entire turning swimmer.")]
        [SerializeField, Min(0f)] private float boundaryClearance = 1.1f;

        private Rigidbody body;
        private Vector2 moveInput;
        private Vector3 velocity;
        private Vector3 aimDirection;
        private ISwimmerMovementBounds movementBounds;
        private float speedOverride = -1f;
        private SwimmerStamina stamina;
        private SwimmerIdentity identity;
        private SwimmerBallInteraction interaction;
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
        private Vector3 ClampMovement(Vector3 position)
        {
            if(leavingPeriodLine)
            {
                if(Mathf.Abs(position.z-poolBounds.transform.position.z)>poolBounds.Length*.5f-boundaryClearance)return poolBounds.ClampPosition(position,0f);
                leavingPeriodLine=false;
            }
            return movementBounds != null ? movementBounds.ClampMovement(position) : poolBounds.ClampPosition(position, boundaryClearance);
        }

        public Vector3 Velocity => velocity;
        public float MaxSpeed => (speedOverride > 0f ? speedOverride : maxSpeed) * (stamina!=null?stamina.SpeedMultiplier:1f);
        public Vector3 Position => body != null ? body.position : transform.position;
        public Vector3 Facing => body!=null?body.rotation*Vector3.forward:transform.forward;
        public bool IsRetreating { get; private set; }

        public void StopMotion()
        {
            moveInput = Vector2.zero;
            velocity = Vector3.zero;
            aimDirection = Vector3.zero;
            angularSpeed = 0f;
            IsRetreating = false;
            ClearSpeedOverride();
            periodAcceleration=1f;
            if(stamina!=null)stamina.StopSprint();
        }

        public void ResetPose(Vector3 position, Quaternion rotation)
        {
            leavingPeriodLine=false;
            if (body == null) body = GetComponent<Rigidbody>();
            moveInput = Vector2.zero;
            velocity = Vector3.zero;
            aimDirection = Vector3.zero;
            angularSpeed = 0f;
            IsRetreating = false;
            ClearSpeedOverride();
            body.position = ClampMovement(position);
            body.rotation = rotation;
        }

        public void SetMoveInput(Vector2 direction, bool usePossessionFacing = false)
        {
            // Preserve analogue stick strength while limiting keyboard diagonals to unit length.
            moveInput = Vector2.ClampMagnitude(direction, 1f);
            directionalInput = usePossessionFacing;
        }

        public void SetFacingDirection(Vector3 direction)
        {
            direction.y = 0f;
            aimDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
        }
        public void ClearFacingDirection() => aimDirection = Vector3.zero;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
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

        private void FixedUpdate()
        {
            if (identity != null && !identity.EligibleForPlay) { StopMotion(); return; }
            if (identity != null && identity.Rules != null && identity.Rules.HoldOutsideAtHorn(identity)) { StopMotion(); return; }
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
            bool sprinting = stamina != null && stamina.IsSprinting;
            Vector3 forward = Facing;
            float retreatThreshold = IsRetreating ? Mathf.Min(retreatExitDot, retreatEnterDot) : retreatEnterDot;
            IsRetreating = live && directionalInput && (interaction.HasBall || (IsRetreating && sprinting)) &&
                input.sqrMagnitude > .04f && Vector3.Dot(input.normalized, forward) < -retreatThreshold;
            if (IsRetreating && sprinting && interaction.HasBall)
                interaction.Ball.TryReleaseForRetreat(interaction);

            float speed = MaxSpeed * (IsRetreating ? (sprinting ? .80f : retreatSpeedFraction) : 1f);
            Vector3 targetVelocity = input * speed;
            float changeRate = moveInput.sqrMagnitude > 0f ? acceleration : deceleration;
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
            Vector3 next = ClampMovement(previous + velocity * step);
            // Report actual movement, removing blocked velocity so releasing at a wall cannot drift.
            velocity = new Vector3((next.x - previous.x) / step, 0f, (next.z - previous.z) / step);
            body.MovePosition(next);

            Vector3 turnDirection = aimDirection.sqrMagnitude > 0f ? aimDirection : IsRetreating ? forward : velocity;
            if (turnDirection.sqrMagnitude > 0.0001f)
            {
                if (live)
                {
                    float angle = Vector3.SignedAngle(forward, turnDirection, Vector3.up);
                    float maximum = turnSpeed * (sprinting && !IsRetreating ? .78f : 1f);
                    float target = Mathf.Sign(angle) * Mathf.Min(maximum,
                        Mathf.Sqrt(2f * turnAcceleration * Mathf.Abs(angle)));
                    angularSpeed = Mathf.MoveTowards(angularSpeed, target, turnAcceleration * step);
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
        }

        private void OnDisable()
        {
            if(stamina!=null)stamina.StopSprint();
            moveInput = Vector2.zero;
            velocity = Vector3.zero;
            aimDirection = Vector3.zero;
            angularSpeed = 0f;
            IsRetreating = false;
            ClearSpeedOverride();
        }
    }
}
