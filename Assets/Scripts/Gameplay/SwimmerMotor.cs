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
        public void OverrideSpeed(float speed) => speedOverride = Mathf.Max(0.1f, speed);
        public void ClearSpeedOverride() => speedOverride = -1f;
        public void SetMovementBounds(ISwimmerMovementBounds bounds) => movementBounds = bounds;
        private Vector3 ClampMovement(Vector3 position) => movementBounds != null ? movementBounds.ClampMovement(position) : poolBounds.ClampPosition(position, boundaryClearance);

        public Vector3 Velocity => velocity;
        public float MaxSpeed => (speedOverride > 0f ? speedOverride : maxSpeed) * (stamina!=null?stamina.SpeedMultiplier:1f);
        public Vector3 Position => body != null ? body.position : transform.position;
        public Vector3 Facing => body!=null?body.rotation*Vector3.forward:transform.forward;

        public void StopMotion()
        {
            moveInput = Vector2.zero;
            velocity = Vector3.zero;
            aimDirection = Vector3.zero;
            ClearSpeedOverride();
            if(stamina!=null)stamina.StopSprint();
        }

        public void ResetPose(Vector3 position, Quaternion rotation)
        {
            if (body == null) body = GetComponent<Rigidbody>();
            moveInput = Vector2.zero;
            velocity = Vector3.zero;
            aimDirection = Vector3.zero;
            ClearSpeedOverride();
            body.position = ClampMovement(position);
            body.rotation = rotation;
        }

        public void SetMoveInput(Vector2 direction)
        {
            // Preserve analogue stick strength while limiting keyboard diagonals to unit length.
            moveInput = Vector2.ClampMagnitude(direction, 1f);
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
            if (identity != null && identity.Rules != null && !identity.Rules.CanMove(identity))
            {
                if (moveInput.sqrMagnitude > 0.01f && identity.Rules.IsRulePhase && identity.Rules.Taker == identity)
                    aimDirection = new Vector3(moveInput.x, 0f, moveInput.y);
                moveInput = Vector2.zero; velocity = Vector3.zero; if(stamina!=null)stamina.StopSprint();
            }
            float step = Time.fixedDeltaTime;
            if(stamina!=null)stamina.Simulate(moveInput.sqrMagnitude>0.01f,step);
            Vector3 targetVelocity = new Vector3(moveInput.x, 0f, moveInput.y) * MaxSpeed;
            float changeRate = moveInput.sqrMagnitude > 0f ? acceleration : deceleration;
            if(stamina!=null)changeRate*=stamina.AccelerationMultiplier;
            velocity = Vector3.MoveTowards(velocity, targetVelocity, changeRate * step);

            Vector3 previous = body.position;
            Vector3 next = ClampMovement(previous + velocity * step);
            // Report actual movement, removing blocked velocity so releasing at a wall cannot drift.
            velocity = new Vector3((next.x - previous.x) / step, 0f, (next.z - previous.z) / step);
            body.MovePosition(next);

            Vector3 turnDirection = aimDirection.sqrMagnitude > 0f ? aimDirection : velocity;
            if (turnDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion facing = Quaternion.LookRotation(turnDirection, Vector3.up);
                body.MoveRotation(Quaternion.RotateTowards(body.rotation, facing, turnSpeed * step));
            }
        }

        private void OnDisable()
        {
            if(stamina!=null)stamina.StopSprint();
            moveInput = Vector2.zero;
            velocity = Vector3.zero;
            aimDirection = Vector3.zero;
            ClearSpeedOverride();
        }
    }
}
