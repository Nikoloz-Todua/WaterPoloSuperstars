using UnityEngine;

namespace ToduStudios.WaterPolo
{
    public enum RefereeSignal { None, Foul, Exclusion, Penalty, Goal, Period, Restart }
    /// <summary>Purposeful deck movement and rule-event observation. Never decides a foul or touches gameplay physics.</summary>
    [DisallowMultipleComponent]
    public sealed class PoolsideReferee : MonoBehaviour
    {
        [SerializeField] private BallController ball;
        [SerializeField] private PrototypeMatch match;
        [SerializeField] private MatchRules rules;
        [SerializeField] private PoolBounds pool;
        [SerializeField] private MonoBehaviour visual;
        private IRefereePresentation presentation;
        [SerializeField,Min(0.5f)] private float walkingSpeed=2.2f;
        [SerializeField,Min(0.1f)] private float acceleration=3f;
        [SerializeField,Min(1f)] private float moveThreshold=3.5f;
        [SerializeField,Min(0.1f)] private float stopThreshold=0.5f;
        [SerializeField,Min(0.1f)] private float decisionInterval=0.65f;
        [SerializeField] private float deckSide=1f;
        [SerializeField,Min(1f)] private float deckOffset=1.4f;
        [SerializeField] private float deckHeight=0.18f;
        private float targetZ,speed,nextDecision,signalUntil,holdUntil;
        private int lastScore;
        private bool walking;
        public RefereeSignal Signal {get;private set;}
        public float Speed=>speed;
        public float TargetZ=>targetZ;
        public bool Walking=>walking;
        private void Awake(){presentation=visual as IRefereePresentation;targetZ=transform.position.z;lastScore=match.BlueScore+match.WhiteScore;}
        private void OnEnable(){rules.Decided+=Decision;match.Changed+=Phase;match.NewMatch+=ResetObserver;match.CentreRestartSignalled+=CentreSignal;}
        private void OnDisable(){rules.Decided-=Decision;match.Changed-=Phase;match.NewMatch-=ResetObserver;match.CentreRestartSignalled-=CentreSignal;}
        private void ResetObserver(){targetZ=pool.transform.position.z;walking=false;speed=0f;Signal=RefereeSignal.None;lastScore=0;holdUntil=0f;}
        private void CentreSignal(SwimmerIdentity taker)
        {Signal=RefereeSignal.Restart;signalUntil=match.PresentationTime+1.6f;holdUntil=signalUntil;}
        private void Decision(RefereeEvent e)
        {
            if(e.Decision==RefereeDecision.Reentry || e.Decision==RefereeDecision.FreeThrow)return;
            Signal=e.Decision==RefereeDecision.Penalty?RefereeSignal.Penalty:e.Decision==RefereeDecision.Exclusion?RefereeSignal.Exclusion:RefereeSignal.Foul;
            signalUntil=match.PresentationTime+1.8f;holdUntil=match.PresentationTime+0.8f;
        }
        private void Phase()
        {
            int score=match.BlueScore+match.WhiteScore;
            if(score!=lastScore){Signal=RefereeSignal.Goal;signalUntil=match.PresentationTime+2f;holdUntil=signalUntil;}
            else if(match.Phase==MatchPhase.QuarterBreak || match.Phase==MatchPhase.HalfTime || match.Phase==MatchPhase.FullTime)
            {Signal=RefereeSignal.Period;signalUntil=match.PresentationTime+1.6f;targetZ=pool.transform.position.z;}
            lastScore=score;nextDecision=0f;
        }
        private void Update()
        {
            if (match.IsPaused) return;
            if(match.PresentationTime>=signalUntil)Signal=RefereeSignal.None;
            Vector3 action=ball.PlayPosition;
            if(match.PresentationTime>=nextDecision && match.PresentationTime>=holdUntil)
            {
                nextDecision=match.PresentationTime+decisionInterval;
                float z=action.z-pool.transform.position.z;
                // Observe sectors, rather than tracking every small ball displacement.
                float sector=Mathf.Abs(z)<4f?0f:Mathf.Sign(z)*(Mathf.Abs(z)>10f?pool.Length*0.38f:pool.Length*0.22f);
                if(match.Rules!=null && match.Rules.IsPenalty)sector=Mathf.Sign(z)*pool.Length*0.34f;
                if(!match.IsPlaying && !rules.IsRulePhase && match.Phase!=MatchPhase.GoalRestart)sector=0f;
                float desired=pool.transform.position.z+sector;
                if(Mathf.Abs(desired-targetZ)>moveThreshold)targetZ=desired;
            }
            float delta=targetZ-transform.position.z;
            if(!walking && Mathf.Abs(delta)>moveThreshold && match.PresentationTime>=holdUntil)walking=true;
            if(walking && (Mathf.Abs(delta)<stopThreshold || match.PresentationTime<holdUntil))walking=false;
            float desiredSpeed=walking?Mathf.Sign(delta)*Mathf.Min(walkingSpeed,Mathf.Sqrt(2f*acceleration*Mathf.Abs(delta))):0f;
            speed=Mathf.MoveTowards(speed,desiredSpeed,acceleration*Time.deltaTime);
            Vector3 point=transform.position;point.z+=speed*Time.deltaTime;
            point.x=pool.transform.position.x+Mathf.Sign(deckSide)*(pool.Width*0.5f+deckOffset);
            point.y=pool.Waterline+deckHeight;
            point.z=Mathf.Clamp(point.z,pool.transform.position.z-pool.Length*0.43f,pool.transform.position.z+pool.Length*0.43f);
            transform.position=point;
            Vector3 look=action-point;look.y=0f;
            if(look.sqrMagnitude>0.1f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(look),100f*Time.deltaTime);
            presentation?.Animate(speed,Signal);
        }
    }
}
