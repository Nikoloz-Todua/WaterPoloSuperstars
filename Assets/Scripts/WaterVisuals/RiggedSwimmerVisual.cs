using UnityEngine;
using UnityEngine.InputSystem;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    /// <summary>In-place Humanoid presentation. Never moves the motor, releases a ball or decides a save.</summary>
    [DefaultExecutionOrder(180), RequireComponent(typeof(Animator))]
    public sealed class RiggedSwimmerVisual : MonoBehaviour
    {
        public SwimmerIdentity identity;
        public SwimmerMotor motor;
        public BallController ball;
        public PrototypeMatch match;
        public PoolBounds pool;
        public Transform alignmentRoot;
        public WaterInteractionEffects water;
        [Tooltip("Isolated laboratory only: 0 tread, 1 swim, 2 sprint, 3 possession, 4 pass, 5 shoot, 6 block, 7 save.")]
        [Range(0,7)] public int previewState;
        [Min(.02f)] public float idleRippleInterval=2.4f, strokeCooldown=.32f;
        public float headHeight=.26f;
        [Tooltip("Use the lower visual waterline authored for the Milestone 28 pose set.")]
        public bool useAuthoredWaterline;
        public AnimationClip deckWalk;
        Animator animator;
        SwimmerStamina stamina;
        PrototypeGoalkeeper keeper;
        PrototypeTeamTactics tactics;
        PrototypeSwimmerAI swimmerAI;
        ShotCharge charge;
        Transform head,leftHand,rightHand,leftFoot,rightFoot;
        ProneFlutterPose flutter;
        Vector3 lastLeft,lastRight;
        float idleAt,leftAt,rightAt,bubbleAt;
        int previousPreview=-1,saveAttempts;
        BallController.PassReception reception;
        float receiveWeight;
        Quaternion restAlignment;
        float throwAt, throwTurn;
        WaterPoloThrowPose evaluatedPose;
        ThrowMotion followMotion;
        Vector3 followAim;
        float followAt, loadedWeight, carryWeight;
        Vector3 saveContact, blockTarget;
        float saveWeight, blockWeight, receiveRise;
        float keeperRise, keeperReadiness;
        bool keeperShot, keeperSplash;
        float shotLift, shootingLean;
        bool shootingSplash;
        const float FollowSeconds = .22f;
        static readonly int Move=Animator.StringToHash("Move"),Hold=Animator.StringToHash("Hold"),Block=Animator.StringToHash("Block");
        public Animator Animator=>animator;
        public Transform LeftHand=>leftHand;
        public Transform RightHand=>rightHand;
        public Transform LeftFoot=>leftFoot;
        public Transform RightFoot=>rightFoot;
        public bool SupportsEvaluatedGrip => isActiveAndEnabled && evaluatedPose != null && evaluatedPose.Ready;
        public Vector3 EvaluatedBallGrip => SupportsEvaluatedGrip && ball != null
            ? evaluatedPose.Grip(ball.Radius) : transform.position;
        public Vector3 EvaluatedPalm => SupportsEvaluatedGrip ? evaluatedPose.PalmPosition : transform.position;
        public Vector3 EvaluatedPalmNormal => SupportsEvaluatedGrip ? evaluatedPose.PalmDirection : Vector3.zero;
        public float ReceivingHandWeight => receiveWeight;
        internal bool CanReachDryCatch(Vector3 contact, Vector3 velocity) => SupportsEvaluatedGrip &&
            evaluatedPose.CanReceive(contact - (motor.Position - identity.transform.position), velocity, ball.Radius);
        public Vector3 HeadPosition => head != null ? head.position : transform.position;
        public int StrokeEmissions { get; private set; }
        public int ReleaseAnimations { get; private set; }
        public int SaveAnimations { get; private set; }
        public float Locomotion { get; private set; }
        public bool Preview=>identity==null;
        public float PreviewPlaybackSpeed { get; set; } = 1f;
        void Awake()
        {
            if(alignmentRoot!=null)restAlignment=alignmentRoot.localRotation;
            animator=GetComponent<Animator>();animator.applyRootMotion=false;
            // Animator must tick while offscreen too: alignment and hand-entry sampling share its bones.
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            head=animator.GetBoneTransform(HumanBodyBones.Head);
            leftHand=animator.GetBoneTransform(HumanBodyBones.LeftHand);rightHand=animator.GetBoneTransform(HumanBodyBones.RightHand);
            leftFoot=animator.GetBoneTransform(HumanBodyBones.LeftFoot);rightFoot=animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (useAuthoredWaterline) flutter = new ProneFlutterPose(animator, transform);
            if(identity!=null)
            {
                stamina=identity.GetComponent<SwimmerStamina>();keeper=identity.GetComponent<PrototypeGoalkeeper>();
                swimmerAI = identity.GetComponent<PrototypeSwimmerAI>();
                tactics = swimmerAI?.TacticalTeam;
                charge=identity.GetComponent<ShotCharge>();
                evaluatedPose = new WaterPoloThrowPose(animator, identity.transform.forward);
            }
            idleAt=(identity!=null?identity.CapNumber*.173f:.7f);
            if(keeper!=null)saveAttempts=keeper.SaveAttempts;
        }
        void OnEnable(){if(ball!=null)ball.Released+=Released;}
        void OnDisable(){if(ball!=null)ball.Released-=Released;}
        void Released(SwimmerBallInteraction source,BallReleaseKind kind)
        {
            if(identity==null||source!=identity.Interaction||kind==BallReleaseKind.Drop)return;
            if (!ball.IsEvaluatedRelease)
            {
                Vector3 direction=Vector3.ProjectOnPlane(ball.Velocity,Vector3.up);
                throwTurn=Mathf.Clamp(Vector3.SignedAngle(motor.Facing,direction,Vector3.up),-37f,37f);
                throwAt=Time.time+.26f;
                animator.SetTrigger(kind==BallReleaseKind.Pass||kind==BallReleaseKind.Lob?"Pass":"Shoot");
            }
            ReleaseAnimations++;
            if(water!=null && !water.UsesContactSampling)water.EmitAthleteSurface(source.transform.position,.48f,true);
        }
        void Update()
        {
            bool paused = match != null && (match.IsPaused || match.IsPreMatchPresentation ||
                match.GoalPresentationActive && match.GoalPresentation.Phase != GoalPresentationPhase.Celebration && !SettlingKeeper);
            animator.speed = paused ? 0f : Preview ? PreviewPlaybackSpeed
                : motor.IsRetreating ? Mathf.Lerp(1f, 1.28f, motor.Velocity.magnitude / 4f) : 1f;
            if(paused)return;
            bool evaluatedHold = false;
            if(Preview)
            {
                var k=Keyboard.current;
                if(k!=null)for(int i=0;i<8;i++)if(k[(Key)((int)Key.Digit1+i)].wasPressedThisFrame)previewState=i;
                Locomotion=previewState==1?1:previewState==2?2:0;
                animator.SetBool(Hold,previewState==3);animator.SetBool(Block,previewState==6);
                if(previousPreview!=previewState){if(previewState==4)animator.SetTrigger("Pass");if(previewState==5)animator.SetTrigger("Shoot");if(previewState==7)animator.SetTrigger("Save");previousPreview=previewState;}
            }
            else
            {
                float speed=motor.Velocity.magnitude;
                Locomotion=identity.EligibleForPlay?Mathf.Clamp01(speed/2.8f):0;
                if(stamina!=null&&stamina.IsSprinting&&speed>3)Locomotion=1+Mathf.Clamp01((speed-3)/2.5f);
                BallController.PassReception nextReception = default;
                bool reading = identity.IsFieldPlayer &&
                    ball.TryReadReception(identity, out nextReception, includeInterception: true);
                float presentation = 0f;
                if (reading)
                {
                    reception = nextReception;
                    float facing = Vector3.Dot(motor.RenderedFacing, reception.Facing);
                    float alignment = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-.28f, .48f, facing));
                    float timeWeight = reception.Airborne ? Mathf.Lerp(.62f, 1f, 1f - Mathf.InverseLerp(.08f, .55f, reception.Seconds)) : .64f;
                    if (motor.HasManualSwim) timeWeight *= .78f;
                    presentation = timeWeight * alignment;
                    if (reception.Committed && !motor.HasManualSwim)
                        Locomotion = Mathf.Min(Locomotion, .16f + .22f * (1f - alignment));
                }
                receiveWeight = Mathf.MoveTowards(receiveWeight, presentation, Time.deltaTime * (reading ? 7f : 9f));
                evaluatedHold = ball.ControlsEvaluatedGrip(identity.Interaction);
                if (evaluatedHold || followMotion != ThrowMotion.None || motor.IsRetreating)
                    Locomotion = Mathf.Min(Locomotion, .08f);
                if (ball.ProfessionalThrows && keeper != null && !keeper.PursuingLooseBall && (keeper.AtStation || keeper.Recovering))
                    Locomotion = Mathf.Min(Locomotion, .12f);
                animator.SetBool(Hold,!evaluatedHold &&
                    ((identity.Interaction.HasBall && !ball.IsDribbling)||(charge!=null&&charge.IsCharging)));
                animator.SetBool(Block, keeper != null && !keeper.UsesAnimatedHands && keeper.ThreateningShot);
                if (keeper != null && keeper.SaveAttempts != saveAttempts)
                {
                    saveAttempts = keeper.SaveAttempts;
                    if (!keeper.UsesAnimatedHands) animator.SetTrigger("Save");
                    SaveAnimations++;
                }
            }
            animator.SetFloat(Move,Locomotion,evaluatedHold ? .042f : .16f,Time.deltaTime);
            // Empty override states retain their last sampled muscles with write-defaults off.
            // Fade the entire action layer out so the locomotion stroke owns free arms again.
            bool action=receiveWeight > .005f && (!ball.ProfessionalThrows || !SupportsEvaluatedGrip) ||
                animator.GetBool(Hold)||animator.GetBool(Block)||animator.IsInTransition(1)||!animator.GetCurrentAnimatorStateInfo(1).IsName("Free arms");
            animator.SetLayerWeight(1,Mathf.MoveTowards(animator.GetLayerWeight(1),action?1:0,Time.deltaTime*8));
        }
        void LateUpdate()
        {
            if (head == null || alignmentRoot == null || match != null && (match.IsPaused ||
                match.IsPreMatchPresentation || match.GoalPresentationActive &&
                match.GoalPresentation.Phase != GoalPresentationPhase.Celebration && !SettlingKeeper)) return;
            float surface=pool!=null?pool.Waterline:0;
            float turn=Mathf.SmoothStep(0,1,Mathf.Clamp01((throwAt-Time.time)*5f));
            alignmentRoot.localRotation=restAlignment*Quaternion.Euler(0f,throwTurn*turn,0f);
            // Outside the animated hierarchy: correct the mesh waterline, never the swimmer root.
            float now=match!=null?match.PresentationTime:Time.time;
            GoalReaction reaction = default;
            bool reacting = match != null && match.GoalPresentation != null &&
                match.GoalPresentation.TryReadReaction(identity, out reaction);
            float elevation = headHeight;
            if (ball != null && ball.ProfessionalThrows && identity != null)
                UpdateDefensivePresentation();
            if (useAuthoredWaterline)
            {
                float moving = Mathf.Clamp01(animator.GetFloat(Move));
                float support = Mathf.Max(turn, animator.GetBool(Block) ? animator.GetLayerWeight(1) : 0f);
                bool professional = ball != null && ball.ProfessionalThrows;
                if (professional && ball.ControlsEvaluatedGrip(identity.Interaction))
                    support = Mathf.Max(support, Mathf.Max(loadedWeight * .66f, ball.HasPendingThrow ? .9f : 0f));
                elevation = Mathf.Lerp(.15f, professional ? .087f : .12f, moving) + (professional ? .048f : .038f) * support +
                    Mathf.Sin(now * 5.4f) * Mathf.Lerp(.009f, .003f, moving);
                elevation += keeperRise + blockWeight * .095f + reaction.Lift;
                float high = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(surface + .55f, surface + .79f, reception.Point.y));
                float receiveTarget = professional ? receiveWeight * Mathf.Lerp(.055f, .41f, high) : 0f;
                receiveRise = Mathf.MoveTowards(receiveRise, receiveTarget, (receiveTarget > receiveRise ? 2.3f : 1.65f) * Time.deltaTime);
                elevation += receiveRise;
                float rise = 0f;
                float phase = 0f;
                bool hasGrip = professional && ball.ControlsEvaluatedGrip(identity.Interaction);
                if (hasGrip)
                {
                    rise = ball.ShotRise(identity.Interaction, out shootingLean, out phase);
                    if (charge != null && charge.IsCharging && match.IsPlaying)
                        rise += .065f * Mathf.SmoothStep(0f, 1f, charge.NormalizedCharge);
                }
                float before = shotLift;
                shotLift = Mathf.MoveTowards(shotLift, rise, Time.deltaTime * (rise > shotLift ? 5.8f : 2.35f));
                elevation += shotLift;
                if (rise > .13f && phase >= .48f && !shootingSplash)
                {
                    shootingSplash = true;
                    water?.EmitAthleteSurface(motor.Position - transform.forward * .18f,
                        Mathf.Lerp(.48f, .90f, Mathf.InverseLerp(.16f, .54f, rise)), true);
                }
                if (shootingSplash && before > .09f && shotLift <= .09f && rise <= .06f)
                {
                    shootingSplash = false;
                    water?.EmitAthleteSurface(motor.Position, .34f, true);
                }
            }
            Vector3 p=alignmentRoot.position;p.y+=surface+elevation-head.position.y;alignmentRoot.position=p;
            if (useAuthoredWaterline)
            {
                Vector3 facing = identity != null ? identity.transform.forward : alignmentRoot.forward;
                if (shotLift > .08f)
                    flutter?.PressWater(facing, Mathf.InverseLerp(.08f, .48f, shotLift) * .72f);
                else if (reaction.Lift > .09f)
                    flutter?.PressWater(facing, Mathf.InverseLerp(.09f, .33f, reaction.Lift) * .64f);
                else if (keeperShot && keeperRise > .11f)
                    flutter?.PressWater(facing, Mathf.InverseLerp(.11f, .43f, keeperRise) * .80f);
                else
                    flutter?.Apply(facing, animator.GetFloat(Move), animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
            }
            if (ball != null && ball.ProfessionalThrows && SupportsEvaluatedGrip)
            {
                FinishEvaluatedPose();
                if (reacting && !SettlingKeeper)
                    evaluatedPose.ReactToGoal(motor.RenderedFacing, reaction);
            }
            if(water!=null && !water.UsesContactSampling && (identity==null||identity.EligibleForPlay))
            {
                float speed=Preview?Locomotion*2.5f:motor.Velocity.magnitude;
                if(speed<.3f&&now>=idleAt){water.EmitAthleteSurface(alignmentRoot.position,.10f,false);idleAt=now+idleRippleInterval;}
                if(speed>.3f)
                {
                    Entry(leftHand,lastLeft,ref leftAt,surface,now,speed);
                    Entry(rightHand,lastRight,ref rightAt,surface,now,speed);
                    if(now>=bubbleAt){water.EmitAthleteBubbles(leftFoot.position,speed);water.EmitAthleteBubbles(rightFoot.position,speed);bubbleAt=now+(Locomotion>1?.28f:.5f);}
                }
            }
            lastLeft=leftHand.position;lastRight=rightHand.position;
        }

        void FinishEvaluatedPose()
        {
            Vector3 forward = identity.transform.forward;
            bool active = match == null || match.IsPlaying || match.Rules != null && match.Rules.OrdinaryFreeThrow;
            if (ball.ControlsEvaluatedGrip(identity.Interaction))
            {
                followMotion = ThrowMotion.None;
                ThrowMotion motion = ball.GetThrowMotion(out float phase, out Vector3 queuedAim);
                bool protectedCarry = active && motor.UsesProtectedCarry;
                float readiness = charge != null && charge.IsCharging ? 1f : protectedCarry ? motor.ProtectedCarry :
                    motor.IsRetreating ? .72f : 0f;
                // The queued standard throw starts from the wind-up already visible at button release.
                if (motion != ThrowMotion.Shot)
                    loadedWeight = Mathf.MoveTowards(loadedWeight, readiness, Time.deltaTime * 10f);
                float carry = protectedCarry ? 0f : motor.IsRetreating ? .55f :
                    Mathf.InverseLerp(.42f, 1.65f, motor.Velocity.magnitude) * .64f;
                carryWeight = Mathf.MoveTowards(carryWeight, carry, Time.deltaTime * 5f);
                float gather = ball.GetGatherProgress(out Vector3 gatheringAt, out Vector3 arrival);
                Vector3 aim = motion != ThrowMotion.None ? queuedAim : forward;
                evaluatedPose.Apply(ball.Radius, ball.SurfaceHeight, forward, aim, loadedWeight,
                    gather, gatheringAt, motion, phase, 0f, carryWeight, arrival, shootingLean);
                if (ball.CommitEvaluatedGrip(identity.Interaction, evaluatedPose.Grip(ball.Radius)))
                {
                    followMotion = motion;
                    followAim = aim;
                    followAt = Time.time;
                }
                return;
            }

            loadedWeight = 0f;
            carryWeight = 0f;
            if (followMotion != ThrowMotion.None)
            {
                float follow = (Time.time - followAt) / FollowSeconds;
                if (active && follow < 1f)
                {
                    evaluatedPose.Apply(ball.Radius, ball.SurfaceHeight, forward, followAim, 0f, 1f,
                        default, followMotion, 1f, Mathf.Max(.001f, follow), lateralLean: shootingLean);
                    return;
                }
                followMotion = ThrowMotion.None;
            }

            bool keeperStation = keeper != null &&
                (keeper.AtStation && !keeper.PursuingLooseBall && (!keeper.Stopped ||
                    match?.GoalPresentation?.Phase == GoalPresentationPhase.Celebration) || SettlingKeeper);
            if ((!active && !keeperStation) || !identity.EligibleForPlay)
            {
                evaluatedPose.ResetFingers();
                return;
            }
            if (receiveWeight > .005f)
            {
                float reach = reception.Airborne ? 1f - Mathf.InverseLerp(.08f, .30f, reception.Seconds) : 0f;
                evaluatedPose.Receive(ball.Radius, forward, reception.Incoming, reception.Point,
                    reception.LookAt, receiveWeight, reach);
                return;
            }
            evaluatedPose.ResetFingers();
            if (keeperStation)
            {
                evaluatedPose.Keeper(forward, ball.transform.position, saveContact, saveWeight,
                    keeperReadiness, keeperShot, ball.Velocity, ball.Radius, ball.SurfaceHeight);
            }
            else if (blockWeight > 0f)
                evaluatedPose.Guard(forward, blockTarget, blockWeight);
            else if (swimmerAI != null && swimmerAI.TryObservePlay(out Vector3 tracking, out bool support))
            {
                Vector3 gaze = tracking - head.position;
                float swim = Mathf.InverseLerp(.12f, .7f, Locomotion);
                float visibility = Mathf.InverseLerp(-.22f, .36f, Vector3.Dot(forward, gaze.normalized));
                float glance = .5f + .5f * Mathf.Cos(Time.time * 2f + identity.CapNumber * .71f);
                float weight = Mathf.Lerp(support ? .87f : .53f, (.23f + .30f * glance) * visibility, swim);
                evaluatedPose.Look(forward, gaze, weight, Mathf.Lerp(55f, 42f, swim));
            }
            else if (!keeperStation && blockWeight <= 0f && Locomotion > .2f)
                evaluatedPose.Look(forward, forward + Vector3.down * .14f, Mathf.Clamp01(Locomotion) * .53f, 36f);
            ball.CommitEvaluatedDribble(identity.Interaction);
        }

        void UpdateDefensivePresentation()
        {
            blockWeight = tactics != null ? tactics.BlockPose(identity, out blockTarget) : 0f;
            if (keeper == null) return;
            float drive = 0f;
            if (keeper.TryGetSavePresentation(out Vector3 contact, out float weight, out bool shot))
            {
                saveContact = saveWeight < .02f ? contact : Vector3.Lerp(saveContact, contact,
                    1f - Mathf.Exp(-30f * Time.deltaTime));
                drive = weight;
                keeperShot = shot;
            }
            if (drive <= 0f && saveWeight < .01f) keeperShot = false;
            saveWeight = Mathf.MoveTowards(saveWeight, drive, Time.deltaTime * (drive > saveWeight ? 15f : 4.5f));
            keeperReadiness = Mathf.MoveTowards(keeperReadiness, keeper.Readiness, Time.deltaTime * 4f);
            float high = Mathf.InverseLerp(ball.SurfaceHeight + .26f, ball.SurfaceHeight + 1.25f, saveContact.y);
            float rise = saveWeight * (keeperShot ? Mathf.Lerp(.08f, .46f, high) : Mathf.Lerp(.02f, .13f, high));
            keeperRise = Mathf.MoveTowards(keeperRise, rise, Time.deltaTime * (rise > keeperRise ? 8.5f : 1.85f));
            if (!keeperSplash && keeperShot && keeperRise > .25f)
            {
                keeperSplash = true;
                water?.EmitAthleteSurface(motor.Position, .56f, true);
            }
            else if (keeperSplash && keeperRise < .08f)
            {
                keeperSplash = false;
                water?.EmitAthleteSurface(motor.Position, .28f, true);
            }
        }

        internal bool TryReadKeeperPalms(out Vector3 left, out Vector3 right)
        {
            left = right = default;
            return SupportsEvaluatedGrip && evaluatedPose.TryKeeperPalms(out left, out right);
        }

        private bool SettlingKeeper => keeper != null && (saveWeight > 0f || keeperRise > 0f);

        void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || !SupportsEvaluatedGrip || !ball.ProfessionalThrows) return;
            Vector3 grip = EvaluatedBallGrip;
            Gizmos.color = new Color(.28f, .91f, 1f);
            Gizmos.DrawLine(rightHand.position, grip);
            Gizmos.DrawWireSphere(grip, ball.Radius);
            if (pool == null) return;
            Vector3 waterline = new Vector3(grip.x, pool.Waterline, grip.z);
            Gizmos.DrawLine(waterline - Vector3.right * .5f, waterline + Vector3.right * .5f);
        }
        void Entry(Transform hand,Vector3 last,ref float at,float surface,float now,float speed)
        {
            if(last.y>=surface&&hand.position.y<surface&&now>=at)
            {
                water.EmitAthleteSurface(hand.position,Locomotion>1?.34f:.22f,true);
                water.EmitAthleteBubbles(hand.position,speed);at=now+strokeCooldown;StrokeEmissions++;
            }
        }
        void OnAnimatorIK(int layer)
        {
            if(layer!=1||identity==null||ball==null)return;
            if (ball.ControlsEvaluatedGrip(identity.Interaction) || followMotion != ThrowMotion.None ||
                (ball.ProfessionalThrows && receiveWeight > .005f && SupportsEvaluatedGrip))
            {
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
                return;
            }
            bool holding=identity.Interaction.HasBall && (!ball.IsDribbling || (charge != null && charge.IsCharging));
            if (!holding && receiveWeight <= .005f) { animator.SetIKPositionWeight(AvatarIKGoal.RightHand,0); return; }
            Vector3 target=holding?identity.Interaction.HeldBallPoint:ball.RaisedHand(identity.Interaction,ball.PhysicsPosition);
            var shoulder=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            target=shoulder.position+Vector3.ClampMagnitude(target-shoulder.position,.53f);
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand,holding?1f:receiveWeight);animator.SetIKPosition(AvatarIKGoal.RightHand,target);
        }

    }
}
