using UnityEngine;

namespace ToduStudios.WaterPolo
{
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

        public Vector3 Velocity => velocity;
        public float MaxSpeed => maxSpeed;

        public void ResetPose(Vector3 position, Quaternion rotation)
        {
            if (body == null) body = GetComponent<Rigidbody>();
            moveInput = Vector2.zero;
            velocity = Vector3.zero;
            body.position = poolBounds.ClampPosition(position, boundaryClearance);
            body.rotation = rotation;
        }

        public void SetMoveInput(Vector2 direction)
        {
            // Preserve analogue stick strength while limiting keyboard diagonals to unit length.
            moveInput = Vector2.ClampMagnitude(direction, 1f);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            if (poolBounds == null)
            {
                Debug.LogError("SwimmerMotor requires an assigned PoolBounds.", this);
                enabled = false;
                return;
            }

            body.position = poolBounds.ClampPosition(body.position, boundaryClearance);
        }

        private void FixedUpdate()
        {
            float step = Time.fixedDeltaTime;
            Vector3 targetVelocity = new Vector3(moveInput.x, 0f, moveInput.y) * maxSpeed;
            float changeRate = moveInput.sqrMagnitude > 0f ? acceleration : deceleration;
            velocity = Vector3.MoveTowards(velocity, targetVelocity, changeRate * step);

            Vector3 previous = body.position;
            Vector3 next = poolBounds.ClampPosition(previous + velocity * step, boundaryClearance);
            // Report actual movement, removing blocked velocity so releasing at a wall cannot drift.
            velocity = new Vector3((next.x - previous.x) / step, 0f, (next.z - previous.z) / step);
            body.MovePosition(next);

            if (velocity.sqrMagnitude > 0.0001f)
            {
                Quaternion facing = Quaternion.LookRotation(velocity, Vector3.up);
                body.MoveRotation(Quaternion.RotateTowards(body.rotation, facing, turnSpeed * step));
            }
        }

        private void OnDisable()
        {
            moveInput = Vector2.zero;
            velocity = Vector3.zero;
        }
    }
}
