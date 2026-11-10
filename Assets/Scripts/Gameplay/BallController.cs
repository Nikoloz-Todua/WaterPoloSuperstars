using System;
using UnityEngine;

namespace ToduStudios.WaterPolo
{
    public enum BallState { Free, Held, Released }
    public enum BallReleaseKind { None, Shot, Pass, Drop, Lob }

    /// <summary>Exclusive ownership and hybrid 3D flight/water simulation for the single match ball.</summary>
    [DefaultExecutionOrder(-140), DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class BallController : MonoBehaviour
    {
        [SerializeField] private PoolBounds poolBounds;
        [SerializeField] private PrototypeMatch match;
        private bool Live => match==null || match.IsPlaying;
        public PrototypeMatch Match => match;
        public event Action<SwimmerBallInteraction, BallReleaseKind> Released;
        public event Action Impact;
        public event Action Saved;
        public event Action<BallImpactEvent> Contact;
        public event Action<GoalMouth, Vector3, float> Netted;
        [SerializeField] private BallFlightProfile flight = new BallFlightProfile();
        public ShotType CurrentShot { get; private set; }
        public int WaterBounces { get; private set; }
        private Vector3 previousAirPosition;
        private GoalMouth nettedGoal;
        private Vector3 netVelocity;
        public Vector3 PlayPosition => Owner!=null?Owner.GetComponent<SwimmerMotor>().Position:PhysicsPosition;
        private bool Eligible(SwimmerBallInteraction player) => player != null && player.isActiveAndEnabled &&
            (player.GetComponent<SwimmerIdentity>() == null || player.GetComponent<SwimmerIdentity>().EligibleForPlay);
        private bool CanRelease(SwimmerBallInteraction player, BallReleaseKind kind) =>
            match != null && match.Rules != null ? match.Rules.CanRelease(player, kind) : Live;
        [SerializeField] private GoalMouth[] goalMouths = new GoalMouth[0];
        [Tooltip("Ball centre above the swimmer-root waterline; radius is 0.14m in the prototype.")]
        [SerializeField] private float surfaceOffset = 0.07f;
        [SerializeField, Min(0f)] private float releaseProtection = 0.6f;
        [SerializeField, Min(0f)] private float waterDrag = 0.65f;
        [SerializeField, Range(0f, 1f)] private float edgeRestitution = 0.45f;
        [SerializeField, Min(1f)] private float carrySharpness = 24f;
        [SerializeField,Min(0.1f)] private float receivingHeight = 0.65f;

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
        public int MotionRevision { get; private set; }
        public Vector3 MotionOrigin { get; private set; }
        public BallReleaseKind ReleaseKind { get; private set; }
        public float ReleaseStartedAt { get; private set; }
        public SwimmerBallInteraction LastReleaser { get; private set; }
        public bool IsAirborne { get; private set; }
        public Vector3 LandingPoint
        {
            get
            {
                if(!IsAirborne)return PhysicsPosition;
                float gravity=Mathf.Max(0.1f,-Physics.gravity.y);Vector3 velocity=Velocity;
                float flight=(velocity.y+Mathf.Sqrt(velocity.y*velocity.y+2f*gravity*Mathf.Max(0f,PhysicsPosition.y-SurfaceHeight)))/gravity;
                Vector3 point=poolBounds.ClampPosition(PhysicsPosition+velocity*flight,Radius);point.y=SurfaceHeight;return point;
            }
        }
        public Vector3 Velocity => body != null && !body.isKinematic ? body.linearVelocity : Vector3.zero;
        public event Action<SwimmerBallInteraction, SwimmerBallInteraction> PossessionChanged;
        public bool CanCollect => Live && isActiveAndEnabled && !simulationPaused && Owner == null && Time.time>=collectAfter && Velocity.magnitude<=flight.maximumReceivingSpeed &&
            ((!IsAirborne && State==BallState.Free) || (IsAirborne && Time.time>=collectAfter && PhysicsPosition.y<=SurfaceHeight+receivingHeight && Velocity.y<=0f));
        public int ShotSequence { get; private set; }
        public Vector3 ShotOrigin { get; private set; }
        // Eligibility outlives the short Released cooldown state, allowing long shots to score.
        public bool IsScoringShot { get; private set; }
        public SwimmerBallInteraction IntendedReceiver => intendedReceiver != null ? intendedReceiver.Interaction : null;
        public float SurfaceHeight => poolBounds.Waterline + surfaceOffset;
        // Unobstructed linear-drag travel budget for tactical shot assessment, not a guaranteed trajectory.
        public float EstimateFlatTravelRange(float speed) => waterDrag>0f?Mathf.Max(0f,speed)/waterDrag:Mathf.Infinity;
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
            if (!CanCollect || !Eligible(claimant) || claimant.CarryAnchor == null)
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
            RestoreSurfacePhysics();
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
            NoteMotionOrigin(body.position);
            NotifyPossessionChanged(previousOwner);
        }

        public bool TryAwardRestart(SwimmerBallInteraction claimant)
        {
            if(match==null || match.IsPaused || !Eligible(claimant))return false;
            bool centre=match.IsCentreRestart && match.CentreTaker?.Interaction==claimant;
            bool foul=match.Rules!=null && match.Rules.IsRulePhase && match.Rules.Taker?.Interaction==claimant;
            if(!centre && !foul)return false;
            ResetForRestart(claimant.GetComponent<SwimmerMotor>().Position);
            AssignOwner(claimant);
            return true;
        }

        public bool TryShoot(SwimmerBallInteraction claimant, float speed)
            => TryShoot(claimant,speed,speed>=18f?ShotType.Powerful:ShotType.Normal);

        public bool TryShoot(SwimmerBallInteraction claimant, float speed, ShotType shot)
        {
            if (!CanRelease(claimant, BallReleaseKind.Shot) || simulationPaused || State != BallState.Held || Owner != claimant || !Eligible(claimant))
                return false;
            CurrentShot=shot;
            Release(claimant, Mathf.Max(0f, speed), claimant.GetComponent<SwimmerMotor>().Facing, BallReleaseKind.Shot);
            float rise=shot==ShotType.High?flight.highReleaseHeight:shot==ShotType.Skip?flight.skipReleaseHeight:
                shot==ShotType.Powerful?flight.powerfulReleaseHeight:flight.normalReleaseHeight;
            Vector3 origin=body.position+Vector3.up*rise;
            float range=ShotDistance(claimant);
            float vertical=0.15f;
            if(shot==ShotType.High)
            {float time=Mathf.Clamp(range/Mathf.Max(1f,speed),0.15f,1.5f);vertical=(0.48f-rise-Physics.gravity.y*time*time*0.5f)/time;}
            else if(shot==ShotType.Skip)
            {float maximum=Mathf.Sqrt(2f*rise/Mathf.Max(0.1f,-Physics.gravity.y))*0.9f;float time=Mathf.Clamp(range*0.55f/Mathf.Max(1f,speed),0.1f,maximum);vertical=(-rise-Physics.gravity.y*time*time*0.5f)/time;}
            BeginFlight(origin,body.linearVelocity+Vector3.up*vertical);
            return true;
        }

        private float ShotDistance(SwimmerBallInteraction player)
        {
            var identity=player.GetComponent<SwimmerIdentity>();
            foreach(var goal in goalMouths)
                if(goal!=null && identity!=null && goal.ScoringTeam==identity.Team)
                    return Mathf.Clamp(Vector3.ProjectOnPlane(goal.transform.position-body.position,Vector3.up).magnitude,2f,18f);
            return 8f;
        }
        private void BeginFlight(Vector3 origin, Vector3 velocity)
        {
            IsAirborne=true;body.constraints=RigidbodyConstraints.None;body.useGravity=true;body.linearDamping=flight.airDrag;
            body.position=origin;body.linearVelocity=velocity;previousAirPosition=origin;NoteMotionOrigin(origin);
        }
        public Vector3 PredictPosition(float seconds)
        {
            seconds=Mathf.Clamp(seconds,0f,flight.maximumPrediction);
            if(Owner!=null)return PlayPosition+Owner.GetComponent<SwimmerMotor>().Velocity*seconds;
            if(IsAirborne)
            {
                float landing=BallTrajectory.TimeToHeight(PhysicsPosition.y-SurfaceHeight,Velocity.y,Mathf.Max(0.1f,-Physics.gravity.y));
                Vector3 point=BallTrajectory.Position(PhysicsPosition,Velocity,Mathf.Min(seconds,landing),true);
                if(seconds>=landing)
                {
                    point.y=SurfaceHeight;
                    Vector3 outgoing=WaterResponse(Velocity+Physics.gravity*landing,point,out bool skip);
                    float rest=seconds-landing;
                    if(skip)
                    {
                        float secondLanding=2f*outgoing.y/Mathf.Max(0.1f,-Physics.gravity.y);
                        point=BallTrajectory.Position(point,outgoing,Mathf.Min(rest,secondLanding),true);
                        if(rest>secondLanding){point.y=SurfaceHeight;point+=FlatDisplacement(Vector3.ProjectOnPlane(outgoing,Vector3.up)*flight.shotWaterRetention,rest-secondLanding);}
                    }
                    else point+=FlatDisplacement(outgoing,rest);
                }
                return ClampBallPoint(point);
            }
            return ClampBallPoint(PhysicsPosition+FlatDisplacement(Velocity,seconds));
        }
        private Vector3 FlatDisplacement(Vector3 velocity,float seconds)
        {float drag=Mathf.Max(0.0001f,waterDrag);return velocity*((1f-Mathf.Exp(-drag*seconds))/drag);}
        private Vector3 ClampBallPoint(Vector3 point)
        {Vector3 clamped=poolBounds.ClampPosition(point,Radius);clamped.y=Mathf.Max(SurfaceHeight,point.y);var pocket=FindPocket(point);return pocket!=null?pocket.ClampPocket(point,Radius):clamped;}

        public StealResult TrySteal(SwimmerBallInteraction claimant, float reach, float facingDot, float protection, float minimumExposure)
        {
            if (!Live || !isActiveAndEnabled || simulationPaused || State != BallState.Held || Owner == null || claimant == null || !claimant.isActiveAndEnabled || claimant.CarryAnchor == null)
                return StealResult.Unavailable;
            var attacker = claimant.GetComponent<SwimmerIdentity>();
            var carrier = Owner.GetComponent<SwimmerIdentity>();
            if (attacker == null || carrier == null || !attacker.EligibleForPlay || !carrier.EligibleForPlay || attacker.Team == carrier.Team) return StealResult.NoOpponent;
            // Keep the near-goal keeper protection; a keeper carrying into the pool uses normal steal rules.
            if (!carrier.IsFieldPlayer && carrier.TryGetComponent<PrototypeGoalkeeper>(out var keeper) && keeper.PossessionProtected) return StealResult.Protected;
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
            if (!CanRelease(claimant, BallReleaseKind.Pass) || simulationPaused || State != BallState.Held || Owner != claimant || !Eligible(claimant))
                return false;
            if(match!=null && match.IsCentreRestart && (receiver==null || !match.IsCentreReceiver(receiver.Identity)))return false;
            direction = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (direction.sqrMagnitude < 0.0001f || speed <= 0f ||
                (receiver != null && !receiver.CanReceiveFrom(claimant))) return false;
            Release(claimant, speed, direction.normalized, BallReleaseKind.Pass);
            intendedReceiver = receiver;
            receiveAfter = Time.time + Mathf.Max(0.02f, minimumFlightTime);
            receiveUntil = Time.time + Mathf.Max(minimumFlightTime + 0.1f, receivingWindow);
            previousPassPosition = body.position;
            return true;
        }

        public bool TryLob(SwimmerBallInteraction claimant,Vector3 direction,float distance,float flightDuration,SwimmerPassing receiver)
        {
            if(!CanRelease(claimant,BallReleaseKind.Lob) || simulationPaused || State!=BallState.Held || Owner!=claimant || !Eligible(claimant) || flightDuration<0.2f || distance<=0f ||
                (receiver!=null && !receiver.CanReceiveFrom(claimant)))return false;
            direction=Vector3.ProjectOnPlane(direction,Vector3.up).normalized;if(direction.sqrMagnitude<0.5f)return false;
            Release(claimant,distance/flightDuration,direction,BallReleaseKind.Lob);
            Vector3 end=poolBounds.ClampPosition(body.position+direction*distance,Radius);
            Vector3 horizontal=Vector3.ProjectOnPlane(end-body.position,Vector3.up)/flightDuration;
            BeginFlight(body.position,horizontal+Vector3.up*(Mathf.Max(0.1f,-Physics.gravity.y)*flightDuration*0.5f));
            intendedReceiver=receiver;receiveAfter=Time.time+0.1f;receiveUntil=Time.time+flightDuration+1.2f;previousPassPosition=body.position;
            return true;
        }

        private void RestoreSurfacePhysics()
        {
            IsAirborne=false;
            if(body==null)return;
            // Set the water pose before locking Y: PhysX otherwise retains the underwater landing pose.
            var interpolation=body.interpolation;body.interpolation=RigidbodyInterpolation.None;
            body.useGravity=false;body.constraints=RigidbodyConstraints.None;
            if(poolBounds!=null){Vector3 position=body.position;position.y=SurfaceHeight;body.position=position;transform.position=position;}
            if(!body.isKinematic){Vector3 velocity=body.linearVelocity;velocity.y=0f;body.linearVelocity=velocity;}
            Physics.SyncTransforms();
            body.constraints=RigidbodyConstraints.FreezePositionY;body.linearDamping=waterDrag;
            body.interpolation=interpolation;
        }

        public void Drop(SwimmerBallInteraction claimant)
        {
            if ((match == null || !match.IsPaused) && State == BallState.Held && Owner == claimant && claimant != null)
                Release(claimant, 0f, claimant.transform.forward, BallReleaseKind.Drop);
        }

        private void Release(SwimmerBallInteraction claimant, float speed, Vector3 launchDirection, BallReleaseKind kind)
        {
            RestoreSurfacePhysics();
            WaterBounces=0;nettedGoal=null;
            SwimmerBallInteraction previousOwner = Owner;
            intendedReceiver = null;
            Vector3 direction = Vector3.ProjectOnPlane(launchDirection, Vector3.up).normalized;
            // Start beyond the capsule; at pool edges the position is clamped and collision stays
            // ignored until separation, so the shot cannot bounce off its own swimmer.
            Vector3 releasePoint = speed > 0f
                ? claimant.GetComponent<SwimmerMotor>().Position + direction * claimant.ReleaseDistance
                : transform.position;
            // Keep release spawning on the playing side; a teleport into a net is not a goal.
            Vector3 spawn = poolBounds.ClampPosition(releasePoint, Radius);
            spawn.y = SurfaceHeight;
            body.position = spawn;
            ShotOrigin = spawn;
            NoteMotionOrigin(spawn);
            ReleaseKind = kind;
            ReleaseStartedAt = Time.time;
            LastReleaser = claimant;
            IsScoringShot = speed > 0f && (match == null || (!match.IsCentreRestart && match.Phase != MatchPhase.FreeThrowReady));
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
            Released?.Invoke(claimant, kind);
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
                if (!body.isKinematic) {body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
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
            NoteMotionOrigin(spawn);
        }

        /// <summary>Receives a swept contact already validated by the interception observer, including released-shot cooldown.</summary>
        public bool TryCatchContact(SwimmerBallInteraction claimant, Vector3 contact)
        {
            var identity = claimant != null ? claimant.GetComponent<SwimmerIdentity>() : null;
            if (!(Live || match!=null && !match.IsPaused && match.Phase==MatchPhase.PenaltyFlight) || !isActiveAndEnabled || simulationPaused || Owner != null || State == BallState.Held || identity == null || !identity.EligibleForPlay || identity.IsFieldPlayer || !claimant.isActiveAndEnabled || claimant.CarryAnchor == null)
                return false;
            Vector3 incoming=Velocity;
            body.position = contact;
            AssignOwner(claimant);
            Saved?.Invoke();
            Contact?.Invoke(new BallImpactEvent(BallImpactKind.Save,contact,incoming,Vector3.zero));
            return true;
        }

        public bool TryDeflectContact(SwimmerBallInteraction claimant, Vector3 contact, Vector3 outgoingVelocity)
        {
            var identity = claimant != null ? claimant.GetComponent<SwimmerIdentity>() : null;
            if (!(Live || match!=null && !match.IsPaused && match.Phase==MatchPhase.PenaltyFlight) || !isActiveAndEnabled || simulationPaused || Owner != null || State == BallState.Held || identity == null || !identity.EligibleForPlay || identity.IsFieldPlayer || !claimant.isActiveAndEnabled) return false;
            intendedReceiver = null; RestoreOwnerCollision();
            Vector3 incoming=Velocity;
            if(contact.y>SurfaceHeight+0.04f || outgoingVelocity.y>0.1f)BeginFlight(contact,outgoingVelocity);
            else {RestoreSurfacePhysics();contact.y=SurfaceHeight;outgoingVelocity.y=0f;body.position=contact;body.linearVelocity=outgoingVelocity;}
            body.angularVelocity = Vector3.zero;
            State = BallState.Released; collectAfter = Time.time + 0.15f;
            // Keep the original shot sequence/eligibility. A real deflection may still enter the net.
            NoteMotionOrigin(contact);
            Saved?.Invoke();
            Contact?.Invoke(new BallImpactEvent(BallImpactKind.Save,contact,incoming,outgoingVelocity));
            return true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if(match!=null && !match.BallInMotion)return;
            Impact?.Invoke();
            if(collision.contactCount>0)Contact?.Invoke(new BallImpactEvent(BallImpactKind.Frame,collision.GetContact(0).point,collision.relativeVelocity,Velocity));
        }

        private void NoteMotionOrigin(Vector3 position) { MotionRevision++; MotionOrigin = position; }

        private void ResolveWaterImpact()
        {
            Vector3 incoming=Velocity, end=body.position;
            float fraction=previousAirPosition.y>end.y?Mathf.Clamp01((previousAirPosition.y-SurfaceHeight)/(previousAirPosition.y-end.y)):1f;
            Vector3 contact=Vector3.Lerp(previousAirPosition,end,fraction);contact.y=SurfaceHeight;
            Vector3 outgoing=WaterResponse(incoming,contact,out bool skip);
            if(skip)
            {
                WaterBounces++;
                // Integrate only the remainder after the actual surface crossing, preserving the travelled segment.
                float rest=Time.fixedDeltaTime*(1f-fraction);
                Vector3 next=BallTrajectory.Position(contact,outgoing,rest,true);next.y=Mathf.Max(next.y,SurfaceHeight+0.002f);
                BeginFlight(next,outgoing+Physics.gravity*rest);NoteMotionOrigin(contact);previousPassPosition=next;
            }
            else
            {
                RestoreSurfacePhysics();body.position=contact;body.linearVelocity=outgoing;State=BallState.Free;
                previousPassPosition=contact;
            }
            Contact?.Invoke(new BallImpactEvent(skip?BallImpactKind.Skip:BallImpactKind.Water,contact,incoming,outgoing));
        }
        private Vector3 WaterResponse(Vector3 incoming,Vector3 contact,out bool skip)
        {
            Vector3 horizontal=Vector3.ProjectOnPlane(incoming,Vector3.up);
            skip=ReleaseKind==BallReleaseKind.Shot && CurrentShot==ShotType.Skip && WaterBounces==0 &&
                horizontal.magnitude>5f && -incoming.y>0.7f && -incoming.y<horizontal.magnitude*0.9f && FindPocket(contact)==null;
            Vector3 outgoing=horizontal*(ReleaseKind==BallReleaseKind.Lob?flight.lobWaterRetention:skip?flight.skipRetention:flight.shotWaterRetention);
            if(skip)outgoing.y=Mathf.Clamp(-incoming.y*flight.skipRestitution,0.6f,2.7f);
            return outgoing;
        }

        public void CaptureGoal(GoalMouth goal)
        {
            if(goal==null || nettedGoal!=null || Owner!=null)return;
            float speed=Velocity.magnitude;netVelocity=Velocity*0.28f;nettedGoal=goal;
            IsScoringShot=false;intendedReceiver=null;IsAirborne=false;body.isKinematic=true;
            Netted?.Invoke(goal,body.position,speed);
            Contact?.Invoke(new BallImpactEvent(BallImpactKind.Net,body.position,netVelocity/0.28f,netVelocity));
        }
        private void AdvanceNet()
        {
            Vector3 next=body.position+netVelocity*Time.fixedDeltaTime;
            Vector3 local=nettedGoal.transform.InverseTransformPoint(next);
            local.x=Mathf.Clamp(local.x,-nettedGoal.Width*0.5f+Radius,nettedGoal.Width*0.5f-Radius);
            local.y=Mathf.Clamp(local.y,SurfaceHeight-nettedGoal.transform.position.y,nettedGoal.Height-Radius);
            local.z=Mathf.Clamp(local.z,Radius,nettedGoal.Depth-Radius);
            body.MovePosition(nettedGoal.transform.TransformPoint(local));
            netVelocity*=Mathf.Exp(-6f*Time.fixedDeltaTime);
            netVelocity.y-=5f*Time.fixedDeltaTime;
        }

        private void FixedUpdate()
        {
            if(nettedGoal!=null){AdvanceNet();return;}
            if (simulationPaused) return;
            if (State == BallState.Held)
            {
                if (!Eligible(Owner))
                    ResetFree();
                return;
            }

            if(IsAirborne)
            {
                if(body.position.y<=SurfaceHeight && body.linearVelocity.y<=0f)
                {
                    ResolveWaterImpact();
                    if(IsAirborne)return;
                }
                else
                {
                    // Finite prototype air volume: reflect at pool sides; physical posts/athletes still collide in 3D.
                    Vector3 position=body.position,clamped=ClampBallPoint(position);
                    Vector3 airVelocity=body.linearVelocity;
                    if(Mathf.Abs(clamped.x-position.x)>0.001f)airVelocity.x*=-edgeRestitution;
                    if(Mathf.Abs(clamped.z-position.z)>0.001f)airVelocity.z*=-edgeRestitution;
                    body.position=clamped;body.linearVelocity=airVelocity;previousAirPosition=clamped;
                    TryReceivePass();return;
                }
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
            if (!intendedReceiver.Identity.EligibleForPlay || !intendedReceiver.isActiveAndEnabled || !intendedReceiver.Interaction.isActiveAndEnabled || Time.time > receiveUntil)
            {
                intendedReceiver = null;
                return false;
            }
            Vector3 from = previousPassPosition;
            Vector3 current = body.position;
            previousPassPosition = current;
            if(current.y>SurfaceHeight+receivingHeight || Velocity.magnitude>flight.maximumReceivingSpeed || (IsAirborne && Velocity.y>0f))return false;
            if (Time.time < receiveAfter || Owner != null) return false;
            Vector3 target = intendedReceiver.Identity.Motor.Position;
            Vector3 travel = current - from;
            travel.y = 0f;
            Vector3 offset = target - from;
            offset.y = 0f;
            float fraction = travel.sqrMagnitude > 0.0001f
                ? Mathf.Clamp01(Vector3.Dot(offset, travel) / travel.sqrMagnitude) : 0f;
            Vector3 contact = Vector3.Lerp(from,current,fraction);
            if(contact.y>SurfaceHeight+receivingHeight)return false;
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
            // timeScale changes do not retroactively zero this frame's deltaTime. Freeze carry immediately.
            if (match!=null && match.IsPaused)return;
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
            RestoreSurfacePhysics();
            SwimmerBallInteraction previousOwner = Owner;
            intendedReceiver = null;
            nettedGoal=null;WaterBounces=0;
            simulationPaused = false;
            IsScoringShot = false;
            ReleaseKind = BallReleaseKind.None; LastReleaser = null;
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
            NoteMotionOrigin(body.position);
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
