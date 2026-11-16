using UnityEngine;

namespace ToduStudios.WaterPolo
{
    /// <summary>Staging and command adapter delegated by PrototypeMatch. Never sets phases, time or possession.</summary>
    [DisallowMultipleComponent]
    public sealed class PeriodSprintOff : MonoBehaviour
    {
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private PrototypeRoster roster;
        [SerializeField] private PlayerSelection selection;
        [SerializeField] private BallController ball;
        [SerializeField] private PoolBounds pool;
        [SerializeField] private PrototypeTeamTactics[] teams;
        [SerializeField,Min(.5f)] private float preparationSeconds=2f;
        [SerializeField,Range(.1f,.5f)] private float aiReactionMinimum=.16f,aiReactionMaximum=.3f;
        [SerializeField,Range(.12f,.4f)] private float minimumStrokeInterval=.16f;
        [SerializeField,Range(1f,1.5f)] private float maximumAccelerationMultiplier=1.35f;
        [Tooltip("Existing scene roster ID; unavailable athletes use the lowest eligible cap/ID.")]
        [SerializeField] private string preferredBluePlayerId="prototype-blue-1",preferredWhitePlayerId="prototype-white-1";
        [SerializeField,Range(.95f,1.1f)] private float centralLaneOffset=1.05f;
        [SerializeField,Min(8f)] private float maximumRaceSeconds=18f;
        private readonly SwimmerIdentity[] sprinters=new SwimmerIdentity[2];
        private readonly int[] strokes=new int[2];
        private readonly float[] energy=new float[2],lastStroke=new float[2],reaction=new float[2],nextAIStroke=new float[2];
        private float readyAt,startedAt,humanStrokeUntil;
        public SwimmerIdentity HumanSprinter=>sprinters[(int)selection.HumanTeam];
        public SwimmerIdentity Sprinter(PrototypeTeam team)=>sprinters[(int)team];
        public Vector3 Center=>match.PeriodCenter;
        public int Starts {get;private set;}
        public int Acquisitions {get;private set;}
        public int Fallbacks {get;private set;}
        public int HumanStrokeCount=>strokes[(int)selection.HumanTeam];
        public float HumanStrokePower=>energy[(int)selection.HumanTeam];
        public float HumanAcceleration=>Mathf.Lerp(1f,maximumAccelerationMultiplier,HumanStrokePower);
        public bool Ready=>match.PresentationTime>=readyAt;
        public bool ContestExpired=>match.PresentationTime-startedAt>=maximumRaceSeconds||!Eligible(sprinters[0])||!Eligible(sprinters[1]);
        public bool NeedsRestage=>!Eligible(sprinters[0])&&Choose(PrototypeTeam.Blue)!=null||!Eligible(sprinters[1])&&Choose(PrototypeTeam.White)!=null;
        private bool Eligible(SwimmerIdentity p)=>p!=null&&p.enabled&&p.gameObject.activeInHierarchy&&p.IsFieldPlayer&&p.EligibleForPlay&&roster.Contains(p);
        private SwimmerIdentity Choose(PrototypeTeam team)
        {
            string preferred=team==PrototypeTeam.Blue?preferredBluePlayerId:preferredWhitePlayerId;SwimmerIdentity best=null;
            foreach(var p in roster.FieldPlayers)
            {
                if(p.Team!=team||!Eligible(p))continue;
                if(p.PlayerId==preferred)return p;
                if(best==null||p.CapNumber<best.CapNumber||p.CapNumber==best.CapNumber&&string.CompareOrdinal(p.PlayerId,best.PlayerId)<0)best=p;
            }
            return best;
        }
        private PrototypeTeamTactics Team(PrototypeTeam team){foreach(var t in teams)if(t.Team==team)return t;return null;}
        private void OnEnable(){match.PauseChanged+=Paused;match.NewMatch+=NewMatch;}
        private void OnDisable(){match.PauseChanged-=Paused;match.NewMatch-=NewMatch;ClearStrokes();}
        private void NewMatch(){Starts=Acquisitions=Fallbacks=0;strokes[0]=strokes[1]=0;ClearStrokes();}
        private void Paused(bool paused){if(paused)ClearStrokes();}
        private void ClearStrokes()
        {
            humanStrokeUntil=0f;
            for(int i=0;i<2;i++){energy[i]=0f;lastStroke[i]=float.NegativeInfinity;if(sprinters[i]!=null)sprinters[i].Motor.SetPeriodAcceleration(1f);}
        }
        public void Prepare()
        {
            ClearStrokes();
            for(int side=0;side<2;side++)
            {
                var team=(PrototypeTeam)side;var tactics=Team(team);SwimmerIdentity best=Choose(team);
                sprinters[side]=best;
                if(best==null)continue;
                Vector3 line=tactics.DefendedGoal.transform.position;line.y=pool.Waterline;
                Vector3 right=Vector3.Cross(Vector3.up,tactics.AttackDirection);
                float span=Mathf.Min(8.1f,pool.Width*.5f-1.2f);int slot=0;
                foreach(var p in roster.Members)
                {
                    if(p.Team!=team||!p.EligibleForPlay)continue;
                    float x=0f;
                    if(p.IsFieldPlayer)
                    {
                        if(p==best)x=-centralLaneOffset;
                        else {float factor=slot==0?-1f:slot==1?-.68f:slot==2?-.36f:slot==3?.52f:1f;x=span*factor;slot++;}
                    }
                    p.Motor.StageOnGoalLine(line+right*x,Quaternion.LookRotation(tactics.AttackDirection));
                }
            }
            readyAt=match.PresentationTime+preparationSeconds;
            ball.PreparePeriodBall(Center+Vector3.up*.45f);
            // A missing eligible sprinter releases into neutral open play; never invent an athlete or possession.
        }
        public void Begin()
        {
            ClearStrokes();strokes[0]=strokes[1]=0;Starts++;startedAt=match.PresentationTime;
            for(int i=0;i<2;i++)
            {
                // Bounded deterministic variation; the AI uses the same motor, stamina and stroke cap.
                float variation=((match.Clock.Quarter*37+i*19)%17)/16f;
                reaction[i]=startedAt+Mathf.Lerp(aiReactionMinimum,Mathf.Max(aiReactionMinimum,aiReactionMaximum),variation);
                nextAIStroke[i]=reaction[i];
            }
        }
        public void Finish(){Acquisitions++;ClearStrokes();}
        public void EndUnclaimedContest(){Fallbacks++;ClearStrokes();}
        public void Tick()
        {
            for(int i=0;i<2;i++)
            {
                energy[i]=Mathf.Max(0f,energy[i]-Time.deltaTime*.85f);
                if(sprinters[i]!=null)sprinters[i].Motor.SetPeriodAcceleration(Mathf.Lerp(1f,maximumAccelerationMultiplier,energy[i]));
            }
        }
        public bool Stroke(SwimmerIdentity p)
        {
            if(!match.IsSprintRace||match.IsPaused||p==null||p!=sprinters[(int)p.Team])return false;
            int i=(int)p.Team;float now=match.PresentationTime;
            if(now-lastStroke[i]<minimumStrokeInterval)return false;
            lastStroke[i]=now;strokes[i]++;energy[i]=Mathf.Min(1f,energy[i]+.28f);
            if(p==HumanSprinter)humanStrokeUntil=now+.38f;
            return true;
        }
        public Vector2 HumanMovement(Vector2 requested,bool sprintHeld)
        {
            if(!match.IsSprintRace||HumanSprinter==null||selection.ActivePlayer!=HumanSprinter)return requested;
            if(requested.sqrMagnitude>.001f)return requested;
            if(!sprintHeld&&match.PresentationTime>=humanStrokeUntil)return Vector2.zero;
            Vector3 delta=Center-HumanSprinter.Motor.Position;return new Vector2(delta.x,delta.z).normalized;
        }
        public bool HumanSprint=>match.IsSprintRace&&!match.IsPaused&&match.PresentationTime<humanStrokeUntil;
        public void TryHumanPickup(){if(match.IsSprintRace&&HumanSprinter!=null&&selection.ActivePlayer==HumanSprinter&&ball.CanCollect)HumanSprinter.Interaction.TryCollect();}
        public void DriveAI(SwimmerIdentity player)
        {
            if(!match.IsSprintRace||match.IsPaused||!player.IsFieldPlayer||!player.EligibleForPlay)return;
            int i=(int)player.Team;bool racer=player==sprinters[i];
            if(match.PresentationTime<reaction[i]){player.Motor.SetMoveInput(Vector2.zero);return;}
            if(racer&&match.PresentationTime>=nextAIStroke[i]){Stroke(player);nextAIStroke[i]=match.PresentationTime+.31f+((match.Clock.Quarter+i)%3)*.035f;}
            Vector3 target=racer?Center:Team(player.Team).Formation(player);
            Vector3 delta=PrototypeTeamTactics.Planar(target-player.Motor.Position);
            player.Motor.ClearFacingDirection();player.Motor.SetMoveInput(new Vector2(delta.x,delta.z).normalized*Mathf.Clamp01(delta.magnitude/.8f));
            player.GetComponent<SwimmerStamina>().RequestSprint(racer);
            if(ball.CanCollect)player.Interaction.TryCollect();
        }
    }
}
