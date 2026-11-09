using System;
using UnityEngine;

namespace ToduStudios.WaterPolo
{
    public enum BallState { Free, Held, Released }

    /// <summary>Authoritative ownership and flat-water prototype simulation for one ball.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class BallController : MonoBehaviour
    {
        [SerializeField] private PoolBounds poolBounds;
        [SerializeField] private GoalMouth[] goalMouths = new GoalMouth[0];
        [Tooltip("Ball centre above the swimmer-root waterline; radius is 0.14m in the prototype.")]
        [SerializeField] private float surfaceOffset = 0.07f;
        [SerializeField, Min(0f)] private float releaseProtection = 0.6f;
        [SerializeField, Min(0f)] private float waterDrag = 0.65f;
        [SerializeField, Range(0f, 1f)] private float edgeRestitution = 0.45f;
        [SerializeField, Min(1f)] private float carrySharpness = 24f;

        private Rigidbody body;
        private SphereCollider ballCollider;
        private Collider ignoredOwnerCollider;
        private float collectAfter;
        private bool simulationPaused;
        private SwimmerPassing intendedReceiver;
        private float receiveAfter, receiveUntil;
        private Vector3 previousPassPosition;

        public BallState State { get; private set; } = BallState.Free;
        public SwimmerBallInteraction Owner { get; private set; }
        public float OwnershipStartedAt { get; private set; }
        public event Action<SwimmerBallInteraction, SwimmerBallInteraction> PossessionChanged;
        public bool CanCollect => isActiveAndEnabled && !simulationPaused && State == BallState.Free && Owner == null;
        public int ShotSequence { get; private set; }
        public Vector3 ShotOrigin { get; private set; }
        // Eligibility outlives the short Released cooldown state, allowing long shots to score.
        public bool IsScoringShot { get; private set; }
        public SwimmerBallInteraction IntendedReceiver => intendedReceiver != null ? intendedReceiver.Interaction : null;
        public float SurfaceHeight => poolBounds.Waterline + surfaceOffset;
        public float Radius => ballCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        public Vector3 PhysicsPosition => body != null ? body.position : transform.position;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            ballCollider = GetComponent<SphereCollider>();
            if (poolBounds == null)
            {
                Debug.LogError("BallController requires an assigned PoolBounds.", this);
                enabled = false;
                return;
            }
            body.useGravity = false;
            body.isKinematic = false;
            body.constraints = RigidbodyConstraints.FreezePositionY;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearDamping = waterDrag;
            body.position = SurfacePosition(body.position);
        }

        public bool TryCollect(SwimmerBallInteraction claimant)
        {
            if (!CanCollect || claimant == null || !claimant.isActiveAndEnabled || claimant.CarryAnchor == null)
                return false;
            // Compare both physics poses; an interpolated render Transform can lag after movement/reset.
            claimant.TryGetComponent<SwimmerMotor>(out var motor);
            Vector3 separation = body.position - (motor != null ? motor.Position : claimant.transform.position);
            separation.y = 0f;
            if (separation.sqrMagnitude > claimant.PickupDistance * claimant.PickupDistance)
                return false;
            AssignOwner(claimant);
            return true;
        }

        private void AssignOwner(SwimmerBallInteraction claimant)
        {
            SwimmerBallInteraction previousOwner = Owner;
            // The ball is the only ownership authority: a second claim sees Held immediately.
            intendedReceiver = null;
            RestoreOwnerCollision();
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.None;
            ballCollider.enabled = false;
            Owner = claimant;
            OwnershipStartedAt = Time.time;
            State = BallState.Held;
            IsScoringShot = false;
            NotifyPossessionChanged(previousOwner);
        }

        public bool TryShoot(SwimmerBallInteraction claimant, float speed)
        {
            if (simulationPaused || State != BallState.Held || Owner != claimant || claimant == null)
                return false;
            Release(claimant, Mathf.Max(0f, speed), claimant.transform.forward);
            return true;
        }

        public StealResult TrySteal(SwimmerBallInteraction claimant, float reach, float facingDot, float protection, float minimumExposure)
        {
            if (!isActiveAndEnabled || simulationPaused || State != BallState.Held || Owner == null || claimant == null || !claimant.isActiveAndEnabled || claimant.CarryAnchor == null)
                return StealResult.Unavailable;
            var attacker = claimant.GetComponent<SwimmerIdentity>();
            var carrier = Owner.GetComponent<SwimmerIdentity>();
            if (attacker == null || carrier == null || attacker.Team == carrier.Team) return StealResult.NoOpponent;
            Vector3 delta = carrier.Motor.Position - attacker.Motor.Position;
            delta.y = 0f;
            if (delta.sqrMagnitude > reach * reach) return StealResult.OutOfRange;
            if (delta.sqrMagnitude < 0.01f || Vector3.Dot(claimant.transform.forward, delta.normalized) < facingDot) return StealResult.WrongFacing;
            if (Time.time - OwnershipStartedAt < protection) return StealResult.Protected;
            if (Vector3.Dot(Owner.transform.forward, -delta.normalized) < minimumExposure) return StealResult.Shielded;
            AssignOwner(claimant);
            return StealResult.Success;
        }

        public bool TryPass(SwimmerBallInteraction claimant, Vector3 direction, float speed,
            SwimmerPassing receiver, float minimumFlightTime, float receivingWindow)
        {
            if (simulationPaused || State != BallState.Held || Owner != claimant || claimant == null)
                return false;
            direction = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (direction.sqrMagnitude < 0.0001f || speed <= 0f ||
                (receiver != null && !receiver.CanReceiveFrom(claimant))) return false;
            Release(claimant, speed, direction.normalized);
            intendedReceiver = receiver;
            receiveAfter = Time.time + Mathf.Max(0.02f, minimumFlightTime);
            receiveUntil = Time.time + Mathf.Max(minimumFlightTime + 0.1f, receivingWindow);
            previousPassPosition = body.position;
            return true;
        }

        public void Drop(SwimmerBallInteraction claimant)
        {
            if (State == BallState.Held && Owner == claimant && claimant != null)
                Release(claimant, 0f, claimant.transform.forward);
        }

        private void Release(SwimmerBallInteraction claimant, float speed, Vector3 launchDirection)
        {
            SwimmerBallInteraction previousOwner = Owner;
            intendedReceiver = null;
            Vector3 direction = Vector3.ProjectOnPlane(launchDirection, Vector3.up).normalized;
            // Start beyond the capsule; at pool edges the position is clamped and collision stays
            // ignored until separation, so the shot cannot bounce off its own swimmer.
            Vector3 releasePoint = speed > 0f
                ? claimant.transform.position + direction * claimant.ReleaseDistance
                : transform.position;
            // Keep release spawning on the playing side; a teleport into a net is not a goal.
            Vector3 spawn = poolBounds.ClampPosition(releasePoint, Radius);
            spawn.y = SurfaceHeight;
            body.position = spawn;
            ShotOrigin = spawn;
            IsScoringShot = speed > 0f;
            if (IsScoringShot) ShotSequence++;
            Owner = null;
            State = BallState.Released;
            collectAfter = Time.time + releaseProtection;
            body.isKinematic = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            ballCollider.enabled = true;
            ignoredOwnerCollider = claimant.GetComponent<Collider>();
            if (ignoredOwnerCollider != null)
                Physics.IgnoreCollision(ballCollider, ignoredOwnerCollider, true);
            body.linearVelocity = direction * speed;
            body.angularVelocity = Vector3.zero;
            NotifyPossessionChanged(previousOwner);
        }

        private Vector3 SurfacePosition(Vector3 position)
        {
            Vector3 clamped = poolBounds.ClampPosition(position, Radius);
            GoalMouth pocket = FindPocket(position);
            if (pocket != null) clamped = pocket.ClampPocket(position, Radius);
            clamped.y = SurfaceHeight;
            return clamped;
        }

        private GoalMouth FindPocket(Vector3 position)
        {
            if (goalMouths == null) return null;
            foreach (GoalMouth mouth in goalMouths)
                if (mouth != null && mouth.isActiveAndEnabled && mouth.AllowsPocket(position, Radius)) return mouth;
            return null;
        }

        public void SetSimulationPaused(bool paused)
        {
            simulationPaused = paused;
            if (paused)
            {
                if (!body.isKinematic) body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
            }
        }

        public void ResetForRestart(Vector3 position)
        {
            ResetFree();
            collectAfter = 0f;
            body.angularVelocity = Vector3.zero;
            Vector3 spawn = poolBounds.ClampPosition(position, Radius);
            spawn.y = SurfaceHeight;
            body.position = spawn;
            ShotOrigin = spawn;
        }

        private void FixedUpdate()
        {
            if (simulationPaused) return;
            if (State == BallState.Held)
            {
                if (Owner == null || !Owner.isActiveAndEnabled)
                    ResetFree();
                return;
            }

            if (TryReceivePass()) return;

            if (State == BallState.Released && Time.time >= collectAfter)
                State = BallState.Free;
            if (Time.time >= collectAfter && ignoredOwnerCollider != null &&
                !ignoredOwnerCollider.bounds.Intersects(ballCollider.bounds))
                RestoreOwnerCollision();

            body.position = SurfacePosition(body.position);
            Vector3 velocity = body.linearVelocity;
            velocity.y = 0f;
            Vector3 next = body.position + velocity * Time.fixedDeltaTime;
            Vector3 constrained = SurfacePosition(next);
            bool inGoalLane = FindPocket(next) != null;
            if (Mathf.Abs(next.x - constrained.x) > 0.0001f)
                velocity.x *= -edgeRestitution;
            if (Mathf.Abs(next.z - constrained.z) > 0.0001f)
            {
                // Reach the back-net limit rather than reflecting a fast valid shot before the line.
                if (inGoalLane) velocity.z = (constrained.z - body.position.z) / Time.fixedDeltaTime;
                else velocity.z *= -edgeRestitution;
            }
            body.linearVelocity = velocity;
        }

        private bool TryReceivePass()
        {
            if (intendedReceiver == null) return false;
            if (!intendedReceiver.isActiveAndEnabled || !intendedReceiver.Interaction.isActiveAndEnabled || Time.time > receiveUntil)
            {
                intendedReceiver = null;
                return false;
            }
            Vector3 from = previousPassPosition;
            Vector3 current = body.position;
            previousPassPosition = current;
            if (Time.time < receiveAfter || Owner != null) return false;
            Vector3 target = intendedReceiver.Identity.Motor.Position;
            Vector3 travel = current - from;
            travel.y = 0f;
            Vector3 offset = target - from;
            offset.y = 0f;
            float fraction = travel.sqrMagnitude > 0.0001f
                ? Mathf.Clamp01(Vector3.Dot(offset, travel) / travel.sqrMagnitude) : 0f;
            Vector3 contact = from + travel * fraction;
            Vector3 separation = target - contact;
            separation.y = 0f;
            float range = intendedReceiver.ReceivingDistance;
            if (separation.sqrMagnitude > range * range) return false;
            SwimmerBallInteraction receiver = intendedReceiver.Interaction;
            // Receive at a point on the actual travelled segment, never at the teammate's position.
            body.position = contact;
            AssignOwner(receiver);
            return true;
        }

        private void LateUpdate()
        {
            if (State != BallState.Held || Owner == null)
                return;
            // Use the interpolated swimmer pose, after physics, for a smooth visible carry.
            float blend = 1f - Mathf.Exp(-carrySharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, Owner.CarryAnchor.position, blend);
        }

        private void RestoreOwnerCollision()
        {
            if (ignoredOwnerCollider != null && ballCollider != null)
                Physics.IgnoreCollision(ballCollider, ignoredOwnerCollider, false);
            ignoredOwnerCollider = null;
        }

        private void ResetFree()
        {
            SwimmerBallInteraction previousOwner = Owner;
            intendedReceiver = null;
            simulationPaused = false;
            IsScoringShot = false;
            Owner = null;
            State = BallState.Free;
            if (body == null || ballCollider == null)
            {
                NotifyPossessionChanged(previousOwner);
                return;
            }
            body.isKinematic = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.linearVelocity = Vector3.zero;
            ballCollider.enabled = true;
            RestoreOwnerCollision();
            NotifyPossessionChanged(previousOwner);
        }

        private void NotifyPossessionChanged(SwimmerBallInteraction previousOwner)
        {
            if (previousOwner != Owner) PossessionChanged?.Invoke(previousOwner, Owner);
        }

        private void OnDisable()
        {
            ResetFree();
        }
    }
}
