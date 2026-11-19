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
        Animator animator;
        SwimmerStamina stamina;
        PrototypeGoalkeeper keeper;
        ShotCharge charge;
        Transform head,leftHand,rightHand,leftFoot,rightFoot;
        Vector3 lastLeft,lastRight;
        float idleAt,leftAt,rightAt,bubbleAt;
        int previousPreview=-1,saveAttempts;
        bool reaching;
        Quaternion restAlignment;
        float throwAt, throwTurn;
        static readonly int Move=Animator.StringToHash("Move"),Hold=Animator.StringToHash("Hold"),Block=Animator.StringToHash("Block");
        public Animator Animator=>animator;
        public Transform LeftHand=>leftHand;
        public Transform RightHand=>rightHand;
        public int StrokeEmissions { get; private set; }
        public int ReleaseAnimations { get; private set; }
        public int SaveAnimations { get; private set; }
        public float Locomotion { get; private set; }
        public bool Preview=>identity==null;
        void Awake()
        {
            if(alignmentRoot!=null)restAlignment=alignmentRoot.localRotation;
            animator=GetComponent<Animator>();animator.applyRootMotion=false;
            // Animator must tick while offscreen too: alignment and hand-entry sampling share its bones.
            animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            head=animator.GetBoneTransform(HumanBodyBones.Head);
            leftHand=animator.GetBoneTransform(HumanBodyBones.LeftHand);rightHand=animator.GetBoneTransform(HumanBodyBones.RightHand);
            leftFoot=animator.GetBoneTransform(HumanBodyBones.LeftFoot);rightFoot=animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if(identity!=null){stamina=identity.GetComponent<SwimmerStamina>();keeper=identity.GetComponent<PrototypeGoalkeeper>();charge=identity.GetComponent<ShotCharge>();}
            idleAt=(identity!=null?identity.CapNumber*.173f:.7f);
            if(keeper!=null)saveAttempts=keeper.SaveAttempts;
        }
        void OnEnable(){if(ball!=null)ball.Released+=Released;}
        void OnDisable(){if(ball!=null)ball.Released-=Released;}
        void Released(SwimmerBallInteraction source,BallReleaseKind kind)
        {
            if(identity==null||source!=identity.Interaction||kind==BallReleaseKind.Drop)return;
            Vector3 direction=Vector3.ProjectOnPlane(ball.Velocity,Vector3.up);
            throwTurn=Mathf.Clamp(Vector3.SignedAngle(motor.Facing,direction,Vector3.up),-37f,37f);
            throwAt=Time.time+.26f;
            animator.SetTrigger(kind==BallReleaseKind.Pass||kind==BallReleaseKind.Lob?"Pass":"Shoot");ReleaseAnimations++;
            if(water!=null)water.EmitAthleteSurface(source.transform.position,.48f,true);
        }
        void Update()
        {
            bool paused=match!=null&&(match.IsPaused||match.GoalPresentationActive||match.IsPreMatchPresentation);animator.speed=paused?0:1;if(paused)return;
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
                reaching=ball.ShowReceivingHand(identity.Interaction);
                animator.SetBool(Hold,identity.Interaction.HasBall||(charge!=null&&charge.IsCharging));
                animator.SetBool(Block,keeper!=null&&keeper.ThreateningShot);
                if(keeper!=null&&keeper.SaveAttempts!=saveAttempts){saveAttempts=keeper.SaveAttempts;animator.SetTrigger("Save");SaveAnimations++;}
            }
            animator.SetFloat(Move,Locomotion,.16f,Time.deltaTime);
            // Empty override states retain their last sampled muscles with write-defaults off.
            // Fade the entire action layer out so the locomotion stroke owns free arms again.
            bool action=reaching||animator.GetBool(Hold)||animator.GetBool(Block)||animator.IsInTransition(1)||!animator.GetCurrentAnimatorStateInfo(1).IsName("Free arms");
            animator.SetLayerWeight(1,Mathf.MoveTowards(animator.GetLayerWeight(1),action?1:0,Time.deltaTime*8));
        }
        void LateUpdate()
        {
            if(head==null||alignmentRoot==null||(match!=null&&(match.IsPaused||match.GoalPresentationActive||match.IsPreMatchPresentation)))return;
            float surface=pool!=null?pool.Waterline:0;
            float turn=Mathf.SmoothStep(0,1,Mathf.Clamp01((throwAt-Time.time)*5f));
            alignmentRoot.localRotation=restAlignment*Quaternion.Euler(0f,throwTurn*turn,0f);
            // Outside the animated hierarchy: correct the mesh waterline, never the swimmer root.
            Vector3 p=alignmentRoot.position;p.y+=surface+headHeight-head.position.y;alignmentRoot.position=p;
            float now=match!=null?match.PresentationTime:Time.time;
            if(water!=null&&(identity==null||identity.EligibleForPlay))
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
            bool holding=identity.Interaction.HasBall;
            if (!holding && !reaching) { animator.SetIKPositionWeight(AvatarIKGoal.RightHand,0); return; }
            Vector3 target=holding?identity.Interaction.HeldBallPoint:ball.RaisedHand(identity.Interaction,ball.PhysicsPosition);
            var shoulder=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            target=shoulder.position+Vector3.ClampMagnitude(target-shoulder.position,.53f);
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand,holding?1f:.92f);animator.SetIKPosition(AvatarIKGoal.RightHand,target);
        }
    }
}
